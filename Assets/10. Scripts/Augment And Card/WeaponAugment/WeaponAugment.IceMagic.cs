using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 빙결 마법 : 범위 안 가까운 적들에게 얼음 투사체를 한 발씩 쏜다
    //  - 투사체 수만큼 가까운 적부터 한 명씩 골라서, 그 적 쪽으로 관통 직진 투사체(NonTargetHit, pierce 켬)를 쏜다
    //    (적이 투사체 수보다 적으면 있는 만큼만)
    //  - 진화 : 맞은 적을 arousalStunTime초 정지
    public class IceMagic : WeaponAugment
    {
        [Header("무기 설정")]
        [SerializeField] private NonTargetHit icePrefab; // 얼음 투사체 (pierce 켬. 사거리 range는 searchRange보다 길게)

        [Header("진화 설정 : 맞은 적 정지")]
        [SerializeField] private float arousalStunTime = 0.5f; // 정지 시간 (지속시간 증가 적용)

        // 내부 설정
        // 이번 공격에 이미 고른 적 (같은 적을 두 번 안 고르게. 재사용)
        private HashSet<CombatEntity> pickedTargets = new HashSet<CombatEntity>();

        #region 외부 설정

        protected override void Attack()
        {
            pickedTargets.Clear();
            int count = GetWeaponProjectileCount(); // 무기 기본 개수 + 플레이어 개수(상점 + 능력증강)

            for (int i = 0; i < count; i++)
            {
                // 이미 고른 적을 빼고 가장 가까운 적 (FindTarget과 같은 탐색, 제외 목록만 추가)
                Enemy target = EnemyRegistry.FindNearest(Owner.transform.position, ApplyRangeBonus(searchRange), pickedTargets);
                if (target == null) return; // 범위 안에 더 없으면 끝 (처음부터 없으면 공격 안 함)

                pickedTargets.Add(target);

                HitUnit hit = FireFromBody(icePrefab, GetFlatDirectionTo(target), target, GetDamage());
                if (hit == null) continue;

                if (isArousal && hit.TryGetComponent(out StunOnHit stun)) stun.Set(arousalStunTime * DurationRate, 1, 0f); // 한 번만 정지
            }
        }

        #endregion
    }
}
