using UnityEngine;
using UnityEngine.Serialization; // 새로 추가 : FormerlySerializedAs

namespace PROJECT3D
{
    // 빛의 화살 : 약한 화살을 아주 빠르게 쏜다
    //  - 범위 안에 적이 있으면 가장 가까운 적을 쫓아가는 화살(TargetingHit), 없으면 쏘지 않는다
    //  - 투사체 증가 효과는 화살 수 대신 공속으로 받는다 (추가 투사체 1개 = 공속 +projectileSpeedBonus)
    //  - 진화하면 공속이 크게 빨라진다
    //  - 기본 데미지 / 공격력 계수(damageRatio)는 낮게 두고 공속으로 밸런스를 맞춘다 (계수는 프리팹 인스펙터)
    public class LightArrow : WeaponAugment
    {
        [Header("무기 설정")]
        [SerializeField] private TargetingHit targetArrowPrefab;            // 쫓아가는 화살
        [SerializeField] private float        projectileSpeedBonus = 0.25f; // 추가 투사체 1개당 공속 +25%

        [Header("진화 설정")]
        [SerializeField] private float arousalAttackSpeedRate = 2.5f; // 진화 후 공속 배율

        #region 외부 설정

        protected override void Attack()
        {
            Enemy target = FindTarget(searchRange);

            if (target == null) return;

            HitUnit hit = FireFromBody(targetArrowPrefab, GetFlatDirectionTo(target), target, GetDamage());
            if (hit == null) return;

            ((TargetingHit)hit).SetTarget(target, 1, ApplyRangeBonus(searchRange)); // 한 명만 맞히고 사라진다
        }

        // 공속 = 기본(무기 공속 × 플레이어 공속 배율) × (1 + 추가 투사체 × 보너스) × 진화 배율
        protected override float GetWeaponAttackSpeed()
        {
            float attackSpeed = base.GetWeaponAttackSpeed() * (1f + GetExtraProjectileCount() * projectileSpeedBonus);
            if (isArousal) attackSpeed *= arousalAttackSpeedRate;
            return attackSpeed;
        }

        #endregion
    }
}
