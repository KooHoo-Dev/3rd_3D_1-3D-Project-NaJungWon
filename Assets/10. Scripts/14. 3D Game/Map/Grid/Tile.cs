using UnityEngine;

namespace PROJECT3D
{
    public enum TileKind
    {
        Walkable,       // 경로(통로)
        Blocked,    // 그 밖의 배치 불가
    }

    public enum MoveDirection
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
        public int Distance = -1;
        public MoveDirection Direction = MoveDirection.None;
        
        public float GroundHeight; //바닥 높이
        public Vector3 GroundNormal; //바닥 경사

        public bool IsWalkable => Kind == TileKind.Walkable;
        
        public Tile(Vector2Int coord, TileKind kind)
        {
            Coord = coord;
            Kind = kind;
            //Unit = null;
        }
        
        bool CanMove(Tile from, Tile to, float maxStepUp, float maxDropDown)
        {
            if (!from.IsWalkable || !to.IsWalkable)
                return false;

            float heightDifference = to.GroundHeight - from.GroundHeight;

            if (heightDifference > maxStepUp)
                return false;

            if (-heightDifference > maxDropDown)
                return false;

            return true;
        }
    }
}