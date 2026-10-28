using UnityEngine;

namespace PROJECT3D
{
    public enum TileKind
    {
        Walkable,       // 경로(통로)
        Blocked,    // 그 밖의 배치 불가
    }

    public enum MoveDirection //이동방향
    {
        None,
        East,
        North,
        West,
        South,
        NorthEast,
        NorthWest,
        SouthWest,
        SouthEast
    }
    
    // 그리드의 한칸 상태.
    public class Tile
    {
        public Vector2Int Coord { get; }
        public TileKind Kind;
        public float GroundHeight; //바닥 높이
        public Vector3 GroundNormal = Vector3.up; //바닥 경사
        
        public int Distance = -1;
        public MoveDirection Direction = MoveDirection.None;
        
        public bool IsWalkable => Kind == TileKind.Walkable;
        public Vector3 DirectionVector => GridDirection.ToVector(Direction);
        
        public Tile(Vector2Int coord, TileKind kind)
        {
            Coord = coord;
            Kind = kind;
        }
    }
}