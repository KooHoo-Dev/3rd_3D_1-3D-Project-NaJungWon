using UnityEngine;

// ===== 새로 추가 : 파일 전체 (빛의 화살, BS 프로젝트 CuteLauncher 기반) =====
namespace PROJECT3D
{
    // 빛의 화살 : 약한 화살을 아주 빠르게 쏜다
    //  - 범위 안에 적이 있으면 가장 가까운 적을 쫓아가는 화살(TargetingHit), 없으면 쏘지 않는다 // [변경] 정면 직진 화살(NonTargetHit) 삭제
    //  - 투사체 증가 효과는 화살 수 대신 공속으로 받는다 (추가 투사체 1개 = 공속 +projectileSpeedBonus)
    //  - 진화하면 공속이 크게 빨라진다 (BS : 0.25초 → 0.1초 = 2.5배)
    //  - 기본 데미지 / 공격력 계수(damageRatio)는 낮게 두고 공속으로 밸런스를 맞춘다 (계수는 프리팹 인스펙터)
    public class LightArrow : WeaponAugment
    {
        [Header("빛의 화살")]
        [SerializeField] private TargetingHit targetArrowPrefab;   // 쫓아가는 화살
        [SerializeField] private float searchRange = 5f;           // [변경] 적을 찾는 범위 (화살 프리팹 range에서 옮김. 범위 증가 적용)
        [SerializeField] private float projectileSpeedBonus = 0.25f; // 추가 투사체 1개당 공속 +25%
        [SerializeField] private float arousalSpeedRate = 2.5f;      // 진화 후 공속 배율

        #region 외부 설정

        protected override void Attack()
        {
            Enemy target = FindTarget(searchRange); // [변경] 화살 프리팹 범위 → 무기 범위 (EnemyRegistry, 범위 증가 적용)

            if (target == null) return; // [변경] 적이 없으면 쏘지 않는다 (정면 직진 화살 삭제)

            HitUnit hit = FireHit(targetArrowPrefab, GetAimDirection(target), GetDamage());
            if (hit == null) return;

            ((TargetingHit)hit).SetTarget(target, 1, ApplyRangeBonus(searchRange)); // 한 명만 맞히고 사라진다 // [변경] 탐색 범위 전달 (대상이 먼저 죽으면 이 범위에서 다시 찾음)
        }

        // 공속 = 기본(무기 공속 × 플레이어 공속 배율) × (1 + 추가 투사체 × 보너스) × 진화 배율
        protected override float GetWeaponAttackSpeed()
        {
            float attackSpeed = base.GetWeaponAttackSpeed() * (1f + GetExtraProjectileCount() * projectileSpeedBonus);
            if (isArousal) attackSpeed *= arousalSpeedRate;
            return attackSpeed;
        }

        // 탐색 범위 Gizmo용 기본 범위 (부모 UnitAttack이 그린다)
        protected override float GetBaseSearchRange()
        {
            return searchRange; // [변경] 화살 프리팹 범위 → 무기 범위
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
