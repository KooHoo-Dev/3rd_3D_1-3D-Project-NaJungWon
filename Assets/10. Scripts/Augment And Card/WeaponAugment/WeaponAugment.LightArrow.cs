using UnityEngine;

// ===== 새로 추가 : 파일 전체 (빛의 화살, BS 프로젝트 CuteLauncher 기반) =====
namespace PROJECT3D
{
    // 빛의 화살 : 약한 화살을 아주 빠르게 쏜다
    //  - 범위 안에 적이 있으면 가장 가까운 적을 쫓아가는 화살(TargetingHit), 없으면 캐릭터 정면으로 직진하는 화살(NonTargetHit)
    //  - 투사체 증가 효과는 화살 수 대신 공속으로 받는다 (추가 투사체 1개 = 공속 +projectileSpeedBonus)
    //  - 진화하면 공속이 크게 빨라진다 (BS : 0.25초 → 0.1초 = 2.5배)
    //  - 기본 데미지 / 공격력 계수(damageRatio)는 낮게 두고 공속으로 밸런스를 맞춘다 (계수는 프리팹 인스펙터)
    public class LightArrow : WeaponAugment
    {
        [Header("빛의 화살")]
        [SerializeField] private TargetingHit targetArrowPrefab;   // 적이 있을 때 : 쫓아가는 화살 (탐색 범위는 이 프리팹의 range)
        [SerializeField] private NonTargetHit straightArrowPrefab; // 적이 없을 때 : 정면으로 직진하는 화살
        [SerializeField] private float projectileSpeedBonus = 0.25f; // 추가 투사체 1개당 공속 +25%
        [SerializeField] private float arousalSpeedRate = 2.5f;      // 진화 후 공속 배율

        protected override void Attack()
        {
            Enemy target = FindTarget(targetArrowPrefab.BaseRange); // 기존 탐색 재사용 (EnemyRegistry, 범위 증가 적용)

            if (target == null)
            {
                FireHit(straightArrowPrefab, GetForward(), GetDamage());
                return;
            }

            HitUnit hit = FireHit(targetArrowPrefab, GetAimDirection(target), GetDamage());
            if (hit == null) return;

            ((TargetingHit)hit).SetTarget(target, 1); // 한 명만 맞히고 사라진다
        }

        // 공속 = 기본(무기 공속 × 플레이어 공속 배율) × (1 + 추가 투사체 × 보너스) × 진화 배율
        protected override float GetWeaponAttackSpeed()
        {
            float attackSpeed = base.GetWeaponAttackSpeed() * (1f + GetExtraProjectileCount() * projectileSpeedBonus);
            if (isArousal) attackSpeed *= arousalSpeedRate;
            return attackSpeed;
        }

        // 탐색 범위 Gizmo용 기본 범위 (= 쫓아가는 화살 프리팹의 탐색 범위. 부모 UnitAttack이 그린다)
        protected override float GetBaseSearchRange()
        {
            if (targetArrowPrefab == null) return 0f;
            return targetArrowPrefab.BaseRange;
        }

        // 캐릭터 정면 (수평)
        private Vector3 GetForward()
        {
            Vector3 forward = Owner.transform.forward;
            forward.y = 0f;
            return forward.normalized;
        }
    }
}
// ===== 새로 추가 끝 =====
