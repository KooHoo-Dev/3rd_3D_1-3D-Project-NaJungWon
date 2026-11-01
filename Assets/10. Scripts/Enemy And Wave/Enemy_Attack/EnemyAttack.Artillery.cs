using UnityEngine;

// 포격 몹 공격
namespace PROJECT3D
{
    // 포격 : 공격 시점의 대상 발밑 바닥에 범위 공격(SummonHit)을 떨어뜨린다
    //  - 사거리(몹 컨트롤러 attackRange) 안에 들어오면 몹은 멈춰서 공속마다 쏜다
    //  - 떨어지는 자리 높이는 MapGrid 칸의 바닥 높이 (경사 / 2층에서도 바닥에 붙게)
    //  - 범위표시 / 예고 시간 / 데미지 판정은 zonePrefab(SummonHit)이 한다
    //      SummonHit의 delay = 예고 시간. 그동안 Circle Region의 Fill Progress가 0 → 1, 1이 되면 판정
    public class Artillery : EnemyAttack
    {
        [Header("포격 설정")]
        [SerializeField] private HitUnit zonePrefab; // SummonHit 프리팹
        [SerializeField] private float groundOffset = 0.01f; // 바닥에서 살짝 띄우기 (바닥과 딱 겹치면 범위표시가 안 보임)

        public override void Fire(CombatEntity target)
        {
            Vector3 point = target.transform.position;

            Tile tile = MapGrid.Instance.Grid.GetTileOrNull(point);
            if (tile != null) point.y = tile.GroundHeight + groundOffset;
            
            FireHit(zonePrefab, point, Vector3.forward, GetDamage()); // 방향 = 정면 고정
        }
    }
}
