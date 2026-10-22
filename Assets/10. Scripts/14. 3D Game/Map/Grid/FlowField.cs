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
            FillDirections();
        }

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

        // 방향 받아오기
        public Vector3 GetDirection(Vector3 worldPosition)
        {
            Tile tile = grid.GetTileOrNull(worldPosition);
            if (tile == null || !tile.IsWalkable) return Vector3.zero;
            return tile.DirectionVector;
        }

        // 범위 받아오기
        public bool IsReachable(Vector3 worldPosition)
        {
            Tile tile = grid.GetTileOrNull(worldPosition);
            return tile != null && tile.IsWalkable && tile.Distance >= 0;
        }
    }
}
