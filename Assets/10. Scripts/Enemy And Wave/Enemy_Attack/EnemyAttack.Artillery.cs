using UnityEngine;

// 포격 몹 공격
namespace PROJECT3D
{
    // 포격 : 공격 시점의 대상 발밑 바닥에 범위 공격(SummonHit)을 떨어뜨린다
    //  - 사거리(몹 컨트롤러 attackRange) 안에 들어오면 몹은 멈춰서 공속마다 쏜다
    //  - 떨어지는 자리 높이는 대상이 밟고 있는 면 (바닥 + 장애물 위) // [변경] 칸 바닥 높이 → 밟는 면
    //  - 범위표시 / 예고 시간 / 데미지 판정은 zonePrefab(SummonHit)이 한다
    //      SummonHit의 delay = 예고 시간. 그동안 Circle Region의 Fill Progress가 0 → 1, 1이 되면 판정
    public class Artillery : EnemyAttack
    {
        [Header("포격 설정")]
        [SerializeField] private HitUnit zonePrefab; // SummonHit 프리팹
        [SerializeField] private float groundOffset = 0.01f; // 바닥에서 살짝 띄우기 (바닥과 딱 겹치면 범위표시가 안 보임)
        [SerializeField] private float surfaceRayHeight = 1.5f; // 새로 추가 : 대상 발밑 이 높이에서 아래로 쏴서 밟고 있는 면을 찾는다 (머리 위 지붕은 안 맞게 낮게)
        [SerializeField] private float surfaceRayDistance = 5f; // 새로 추가 : 아래로 찾는 최대 거리

        #region 외부 설정

        public override void Fire(CombatEntity target)
        {
            Vector3 point = target.transform.position;
            point.y = GetSurfaceHeight(point) + groundOffset; // [변경] 칸 바닥 높이 → 장애물 위까지 포함한 실제 밟는 면 높이

            FireHit(zonePrefab, point, Vector3.forward, GetDamage()); // 방향 = 정면 고정
        }

        #endregion

        #region 내부 설정

        // 새로 추가 : 대상이 밟고 있는 면의 높이 (바닥 + 장애물)
        //  - 플레이어가 장애물(바위 / 건물 등) 위에 올라가도 그 위에 포격이 떨어지게 (높은 곳에서 시간 끄는 것 방지)
        //  - 칸 바닥 높이(MapGrid)는 장애물을 빼고 구워져 있어서, 장애물 위에서는 발밑이 아니라 그 아래 땅에 떨어졌다
        //  - 못 찾으면 예전처럼 칸 바닥 높이
        private float GetSurfaceHeight(Vector3 point)
        {
            MapGrid map = MapGrid.Instance;
            LayerMask mask = map.groundMask | map.obstacleMask;
            Vector3 start = point + Vector3.up * surfaceRayHeight;
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, surfaceRayDistance, mask, QueryTriggerInteraction.Ignore))
                return hit.point.y;

            Tile tile = map.Grid.GetTileOrNull(point);
            if (tile != null) return tile.GroundHeight;
            return point.y;
        }

        #endregion
    }
}
