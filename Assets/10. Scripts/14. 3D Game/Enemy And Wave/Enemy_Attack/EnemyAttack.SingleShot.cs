using UnityEngine;

namespace PROJECT3D
{
    // 단발 직선탄
    // 대상의 몸 중심을 향해 한 발 쏜다.
    public class SingleShot : EnemyAttack
    {
        [Header("단발 설정")]
        [SerializeField] private HitUnit projectilePrefab;
        
        public override void Fire(CombatEntity target)
        {
            FireHit(projectilePrefab, GetAimDirection(target), OwnerStat.Damage);        
        }
    }
}