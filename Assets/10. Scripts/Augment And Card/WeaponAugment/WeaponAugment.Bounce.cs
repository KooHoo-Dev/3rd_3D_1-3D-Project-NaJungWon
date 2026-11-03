using UnityEngine;

namespace PROJECT3D
{
    // 가장 가까운 적에게 튕기는 투사체를 쏜다
    //  - 진화하면 타겟이 존재한다면 무한히 튕기고, 튕기는 속도가 빨라진다 (렉걸리면 바꿀예정)
    public class Bounce : WeaponAugment
    {
        [Header("바운스")]
        [SerializeField] private TargetingHit projectilePrefab;
        [SerializeField] private float searchRange = 8f; // [변경] 적을 찾는 범위 (투사체 프리팹 range에서 옮김. 첫 대상 / 튕길 대상 공통, 범위 증가 적용)
        [SerializeField] private int bounceCount = 3;        // 기본 튕기는 횟수
        [SerializeField] private int arousalBounceCount = int.MaxValue; //진화 후 튕기는 횟수 (임시)
        [SerializeField] private float arousalSpeedRate = 1.5f; // 새로 추가 : 진화 후 투사체 속도 배율

        #region 외부 설정

        protected override void Attack() //쓸때 튕기는 횟수는 제한이없더라도 주변 몹 탐색 횟수는 제한을 둬야함.
        {
            Enemy target = FindTarget(searchRange); // [변경] 투사체 프리팹 범위 → 무기 범위
            if (target == null) return;

            int count = bounceCount + GetExtraProjectileCount(); // [변경] 추가 투사체 수 → 튕김 횟수
            if (isArousal) count = arousalBounceCount;

            // [변경] 여러 발 쏘던 반복문 삭제 : 항상 1발
            HitUnit hit = FireHit(projectilePrefab, GetAimDirection(target), GetDamage());
            if (hit == null) return;

            TargetingHit targetingHit = (TargetingHit)hit;
            targetingHit.SetTarget(target, count, ApplyRangeBonus(searchRange)); // [변경] 튕길 대상 탐색 범위도 무기가 넘겨준다
            if (isArousal) targetingHit.SetSpeedRate(arousalSpeedRate); // 새로 추가 : 진화하면 빨리 튕긴다
        }

        // 탐색 범위 Gizmo용 기본 범위
        protected override float GetBaseSearchRange()
        {
            return searchRange; // [변경] 투사체 프리팹 범위 → 무기 범위
        }

        #endregion
    }
}