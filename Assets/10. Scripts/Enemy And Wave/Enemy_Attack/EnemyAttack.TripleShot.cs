using UnityEngine;

namespace PROJECT3D
{
    // 3방향 직선탄
    // 가운데 한 발 + 좌우로 spreadAngle 만큼 벌려서 한 발씩 쏜다.
    public class TripleShot : EnemyAttack
    {
        [Header("3방향 설정")]
        [SerializeField] private HitUnit projectilePrefab;
        [SerializeField] private float spreadAngle = 30f;  // 가운데에서 좌우로 벌어지는 각도

        #region 외부 설정

        public override void Fire(CombatEntity target)
        {
            Vector3 center = GetAimDirection(target);

            Vector3 left = Quaternion.Euler(0f, -spreadAngle, 0f) * center;
            Vector3 right = Quaternion.Euler(0f, spreadAngle, 0f) * center;

            int damage = GetDamage();
            FireHit(projectilePrefab, center, damage);
            FireHit(projectilePrefab, left, damage);
            FireHit(projectilePrefab, right, damage);
        }

        #endregion
    }
}