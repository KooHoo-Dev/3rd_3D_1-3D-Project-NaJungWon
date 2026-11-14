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
        // 위치 / 종류
        public Vector2Int Coord { get; } // 칸 좌표
        public TileKind   Kind;          // 이동 가능 / 막힘

        // 높이 (MapGrid가 구울 때 저장)
        public float   GroundHeight;              // 바닥 높이
        public Vector3 GroundNormal = Vector3.up; // 바닥 경사
        public float   SurfaceHeight;             // 맨 위 표면 높이 (바닥 + 장애물 꼭대기)

        // 흐름장 (FlowField가 계산)
        public int           Distance  = -1;                 // 목표 칸까지 거리
        public MoveDirection Direction = MoveDirection.None; // 다음 칸 방향

        // 외부 공개
        public bool    IsWalkable      => Kind == TileKind.Walkable;          // 이동 가능한 칸인지
        public Vector3 DirectionVector => GridDirection.ToVector(Direction); // 다음 칸 방향 (벡터)
        
        public Tile(Vector2Int coord, TileKind kind)
        {
            Coord = coord;
            Kind = kind;
        }
    }
}