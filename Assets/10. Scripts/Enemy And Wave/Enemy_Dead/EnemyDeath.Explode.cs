using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 죽은 자리에서 자폭
    // 폭발 범위(구)가 플레이어의 피격 원통에 닿으면 데미지를 준다. (멀티 : 범위 안 플레이어 전부)
    public class Explode : EnemyDeath
    {
        [Header("자폭 설정")]
        [SerializeField] private float explodeRadius = 3f;
        [SerializeField] private float damageMultiplier = 1f;   // 표의 Damage에 곱할 배율
        [SerializeField] private ParticleEffect explosionEffect; // 폭발 이펙트 (없으면 생략)

        #region UnityMethod

        // 씬 화면에 폭발 범위를 그린다 (선택했을 때만)
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황
            Vector3 center = Owner != null ? Owner.HitCenter : transform.position;
            Gizmos.DrawWireSphere(center, explodeRadius);
        }

        #endregion

        #region 외부 설정

        public override void Apply()
        {
            Vector3 center = Owner.HitCenter; // 몸 중심에서 터진다

            ObjectPoolManager.PlayEffect(explosionEffect, center);
            if (CombatSystem.Instance == null) return;

            int damage = Mathf.RoundToInt(Owner.Unit.Damage * damageMultiplier);

            IReadOnlyList<Player> players = PlayerRegistry.All;
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player.Stat.IsDead) continue;
                if (player.IsHitBy(center, explodeRadius, HitShape.Sphere, 0f) == false) continue;

                CombatSystem.Instance.SendDamage(Owner, player, damage, center);
            }
        }

        #endregion
    }
}