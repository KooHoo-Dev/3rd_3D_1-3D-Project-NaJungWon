using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 타일에 저장할 거리와 안내 방향을 계산한다. 씬에 붙이지 않는 일반 클래스다.
    // 한 격자의 타일에 결과를 저장하므로, 한 격자에는 하나의 공유 흐름장을 사용한다.
    public class FlowField
    {
        private readonly MapGrid map;
        private readonly SquareGrid grid;
        private readonly Queue<Tile> searchQueue = new Queue<Tile>();

        private const int UnreachedSearchRadius = 10; // 새로 추가 : 플레이어 주변 이 칸 수 안에서 못 닿은 땅을 찾는다
        private const int MaxExtraTargets = 4;        // 새로 추가 : 못 닿은 땅 덩어리를 최대 몇 개까지 이어 줄지

        #region 외부 설정

        #region 계산

        public FlowField(MapGrid map)
        {
            this.map = map;
            grid = map.Grid;
        }

        public void Build(List<Vector3> targets)
        {
            ResetTiles();
            AddTargets(targets);
            FillDistances();
            AddUnreachedTargets(targets); // 새로 추가 : 경사 때문에 못 올라가는 곳에 플레이어가 있을 때
            FillDirections();
        }

        #endregion

        #region 조회

        // 방향 받아오기
        public Vector3 GetDirection(Vector3 worldPosition)
        {
            Tile tile = grid.GetTileOrNull(worldPosition);
            if (tile == null || !tile.IsWalkable) return Vector3.zero;
            return tile.DirectionVector;
        }

        // 새로 추가 : 흐름장이 가리키는 다음 칸의 가운데 (방향이 없으면 false)
        //  - 칸 가장자리에서 방향 그대로 걸으면 옆 칸(턱)으로 들어가 막힐 수 있어서, 막혔을 때 다음 칸 가운데를 향해 걷는다
        public bool TryGetNextPoint(Vector3 worldPosition, out Vector3 point)
        {
            point = default;
            Tile tile = grid.GetTileOrNull(worldPosition);
            if (tile == null || !tile.IsWalkable || tile.Direction == MoveDirection.None) return false;

            Tile next = grid.GetNeighborOrNull(tile, tile.Direction);
            if (next == null) return false;

            point = grid.CellToWorld(next.Coord);
            return true;
        }

        // 범위 받아오기
        public bool IsReachable(Vector3 worldPosition)
        {
            Tile tile = grid.GetTileOrNull(worldPosition);
            return tile != null && tile.IsWalkable && tile.Distance >= 0;
        }

        #endregion

        #endregion

        #region 내부 설정

        private void ResetTiles()
        {
            searchQueue.Clear();
            for (int z = 0; z < grid.Depth; z++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Tile tile = grid.GetTileOrNull(x, z);
                    tile.Distance = -1;
                    tile.Direction = MoveDirection.None;
                }
            }
        }

        private void AddTargets(List<Vector3> targets)
        {
            foreach (Vector3 position in targets)
            {
                Tile tile = grid.GetTileOrNull(position);
                if (tile == null) continue;
                // 플레이어가 막힌 칸에 있으면 주변의 이동 가능한 칸을 대신 쓴다.
                if (!tile.IsWalkable)
                    tile = grid.FindNearestWalkable(tile.Coord, 5);
                if (tile == null || tile.Distance == 0) continue;
                tile.Distance = 0;
                searchQueue.Enqueue(tile);
            }
        }

        // ===== 새로 추가 : 플레이어에게 못 닿는 땅도 플레이어 근처로 모이게 =====
        // 플레이어가 경사 때문에 못 올라가는 곳(Ground 레이어라 이동 가능 칸)에 있으면, 아래 땅은 거리가 없어서(-1) 몹이 멈췄다
        // → 장애물(Obstacle) 위에 있을 때처럼(AddTargets의 FindNearestWalkable) 플레이어와 가장 가까운 "못 닿은 칸"을 목표로 하나 더 넣는다
        //   그 덩어리의 몹은 그 칸(경사 밑)으로 모인다. 덩어리가 여러 개면 MaxExtraTargets번까지 반복
        private void AddUnreachedTargets(List<Vector3> targets)
        {
            foreach (Vector3 position in targets)
            {
                Vector2Int coord = grid.WorldToCell(position);
                for (int i = 0; i < MaxExtraTargets; i++)
                {
                    Tile tile = FindNearestUnreached(coord, UnreachedSearchRadius);
                    if (tile == null) break;

                    tile.Distance = 0;
                    searchQueue.Enqueue(tile);
                    FillDistances(); // 이 칸에서 닿는 땅에 거리 채우기
                }
            }
        }

        // FindNearestWalkable과 같은 방식 + 아직 거리가 없는(-1) 칸만
        private Tile FindNearestUnreached(Vector2Int coord, int radius)
        {
            Tile nearest = null;
            int nearestDistance = int.MaxValue;
            for (int z = coord.y - radius; z <= coord.y + radius; z++)
            {
                for (int x = coord.x - radius; x <= coord.x + radius; x++)
                {
                    Tile tile = grid.GetTileOrNull(x, z);
                    if (tile == null || !tile.IsWalkable || tile.Distance != -1) continue;
                    int dx = x - coord.x;
                    int dz = z - coord.y;
                    int distance = dx * dx + dz * dz;
                    if (distance >= nearestDistance) continue;
                    nearest = tile;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
        // ===== 새로 추가 끝 =====

        private void FillDistances()
        {
            while (searchQueue.Count > 0)
            {
                Tile current = searchQueue.Dequeue();
                foreach (MoveDirection direction in GridDirection.All)
                {
                    Tile neighbor = grid.GetNeighborOrNull(current, direction);
                    if (neighbor == null || neighbor.Distance != -1) continue;

                    // 탐색은 목표에서 바깥으로 퍼진다.
                    // 실제 몹은 이웃에서 현재 칸으로 와야 하므로 반대로 검사한다.
                    if (!map.CanMove(neighbor, current)) continue;
                    neighbor.Distance = current.Distance + 1;
                    searchQueue.Enqueue(neighbor);
                }
            }
        }

        private void FillDirections()
        {
            for (int z = 0; z < grid.Depth; z++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Tile tile = grid.GetTileOrNull(x, z);
                    if (tile.Distance <= 0) continue;
                    int bestDistance = tile.Distance;
                    foreach (MoveDirection direction in GridDirection.All)
                    {
                        Tile neighbor = grid.GetNeighborOrNull(tile, direction);
                        if (!map.CanMove(tile, neighbor)) continue;
                        if (neighbor.Distance < 0 || neighbor.Distance >= bestDistance) continue;
                        bestDistance = neighbor.Distance;
                        tile.Direction = direction;
                    }
                }
            }
        }

        #endregion
    }
}
