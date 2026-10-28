using UnityEngine;

namespace PROJECT3D
{
    // 방향 변환만 모아 둔 도구(직선 네 방향, 대각선 네 방향)
    public static class GridDirection
    {
        public static readonly MoveDirection[] All =
        {
            MoveDirection.East, 
            MoveDirection.North,
            MoveDirection.West, 
            MoveDirection.South,
            MoveDirection.NorthEast, 
            MoveDirection.NorthWest,
            MoveDirection.SouthWest, 
            MoveDirection.SouthEast
        };

        public static Vector2Int ToOffset(MoveDirection direction)
        {
            return direction switch
            {
                MoveDirection.East => new Vector2Int(1, 0),
                MoveDirection.North => new Vector2Int(0, 1),
                MoveDirection.West => new Vector2Int(-1, 0),
                MoveDirection.South => new Vector2Int(0, -1),
                MoveDirection.NorthEast => new Vector2Int(1, 1),
                MoveDirection.NorthWest => new Vector2Int(-1, 1),
                MoveDirection.SouthWest => new Vector2Int(-1, -1),
                MoveDirection.SouthEast => new Vector2Int(1, -1),
                _ => Vector2Int.zero
            };
        }

        public static Vector3 ToVector(MoveDirection direction)
        {
            Vector2Int offset = ToOffset(direction);
            return new Vector3(offset.x, 0f, offset.y).normalized;
        }
    }
}