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
            Debug.Log($"[ProjectileSkill] {Owner.GameObject.name}가 투사체 발사 스킬을 사용!");
            FireHit(projectilePrefab, aimDirection, GetDamage());
        }
    }
}