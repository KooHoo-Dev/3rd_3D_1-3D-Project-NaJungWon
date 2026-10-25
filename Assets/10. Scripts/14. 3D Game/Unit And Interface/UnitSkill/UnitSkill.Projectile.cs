using UnityEngine;

namespace PROJECT3D
{
    // 투사체 스킬 : 화면 방향으로 HitUnit 한 발
    public class ProjectileSkill : UnitSkill
    {
        [Header("투사체 스킬")]
        [SerializeField] private HitUnit projectilePrefab;

        protected override void Use(Vector3 aimDirection)
        {
            FireHit(projectilePrefab, aimDirection, damage);
        }
    }
}