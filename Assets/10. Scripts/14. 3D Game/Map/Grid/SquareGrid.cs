using UnityEngine;

namespace PROJECT3D
{
    // 미니 디펜스와 같은 역할: 타일 보관과 좌표 변환을 담당한다.
    public class SquareGrid
    {
        public int Width { get; }
        public int Depth { get; }
        public float CellSize { get; }
        public Vector3 Origin { get; }
        private readonly Tile[,] tiles;

        public SquareGrid(int width, int depth, float cellSize, Vector3 origin)
        {
            Width = Mathf.Max(1, width);
            Depth = Mathf.Max(1, depth);
            CellSize = Mathf.Max(0.01f, cellSize);
            Origin = origin;
            tiles = new Tile[Width, Depth];

            for (int z = 0; z < Depth; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    tiles[x, z] = new Tile(new Vector2Int(x, z),  TileKind.Walkable);
                }
            }
        }

        public bool InBounds(int x, int z)
        {
            return x >= 0 && z >= 0 && x < Width && z < Depth;
        }

        public Tile GetTileOrNull(int x, int z)
        {
            if (!InBounds(x, z)) return null;
            return tiles[x, z];
        }

        public Tile GetTileOrNull(Vector2Int coord)
        {
            return GetTileOrNull(coord.x, coord.y);
        }

        public Tile GetTileOrNull(Vector3 worldPosition)
        {
            return GetTileOrNull(WorldToCell(worldPosition));
        }

        // 화면의 가로·세로가 아니라 월드의 가로·깊이 좌표를 사용한다.
        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - Origin.x) / CellSize);
            int z = Mathf.FloorToInt((worldPosition.z - Origin.z) / CellSize);
            // 범위 밖 좌표는 끝 칸으로 보정하지 않고 조회할 때 null을 반환한다.
            return new Vector2Int(x, z);
        }

        public Vector3 CellToWorld(Vector2Int coord)
        {
            return Origin + new Vector3(
                (coord.x + 0.5f) * CellSize, 0f,
                (coord.y + 0.5f) * CellSize);
        }

        public Tile GetNeighborOrNull(Tile tile, MoveDirection direction)
        {
            Vector2Int offset = GridDirection.ToOffset(direction);
            return GetTileOrNull(tile.Coord + offset);
        }

        // 목표가 바위 위에 있을 때 쓸 대체 칸이다. 탐색 범위 안의 평면 거리 기준이다.
        public Tile FindNearestWalkable(Vector2Int coord, int radius)
        {
            Tile nearest = null;
            int nearestDistance = int.MaxValue;
            for (int z = coord.y - radius; z <= coord.y + radius; z++)
            for (int x = coord.x - radius; x <= coord.x + radius; x++)
            {
                Tile tile = GetTileOrNull(x, z);
                if (tile == null || !tile.IsWalkable) continue;
                int dx = x - coord.x;
                int dz = z - coord.y;
                int distance = dx * dx + dz * dz;
                if (distance >= nearestDistance) continue;
                nearest = tile;
                nearestDistance = distance;
            }
            return nearest;
        }
    }
}