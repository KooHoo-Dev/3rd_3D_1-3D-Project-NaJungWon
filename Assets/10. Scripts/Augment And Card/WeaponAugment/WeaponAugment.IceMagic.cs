using System.Collections.Generic;
using UnityEngine;

// ===== 새로 추가 : 파일 전체 (빙결 마법, BS 프로젝트 IciclesShooting 기반) =====
namespace PROJECT3D
{
    // 빙결 마법 : 범위 안 가까운 적들에게 얼음 투사체를 한 발씩 쏜다
    //  - 범위(searchRange, 범위 증가 적용) 안에 적이 없으면 공격하지 않는다
    //  - 투사체 수만큼 가까운 적부터 한 명씩 골라서, 그 적 쪽으로 관통 직진 투사체(NonTargetHit, pierce 켬)를 쏜다
    //    (적이 투사체 수보다 적으면 있는 만큼만)
    //  - 빛의 화살처럼 투사체 수를 공속으로 바꾸는 처리는 없다 (기본 무기 공속 그대로)
    //  - 진화 : 맞은 적을 arousalStunTime초 정지 (BS : 맞은 적 0.5초 정지. 정지 시간은 지속시간 증가 적용)
    public class IceMagic : WeaponAugment
    {
        [Header("빙결 마법")]
        [SerializeField] private NonTargetHit icePrefab;   // 얼음 투사체 (pierce 켬. 사거리 range는 searchRange보다 길게)
        [SerializeField] private float searchRange = 10f;  // 적을 찾는 범위 (범위 증가 적용)

        [Header("진화 : 맞은 적 정지")]
        [SerializeField] private float arousalStunTime = 0.5f; // 정지 시간 (지속시간 증가 적용)

        private HashSet<CombatEntity> pickedTargets = new HashSet<CombatEntity>(); // 이번 공격에 이미 고른 적 (같은 적을 두 번 안 고르게. 재사용)

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

                HitUnit hit = FireHit(icePrefab, GetAimDirection(target), GetDamage());
                if (hit == null) continue;

                if (isArousal && hit.TryGetComponent(out StunOnHit stun)) stun.Set(arousalStunTime * DurationRate, 1, 0f); // 한 번만 정지 // [변경] hit.SetStun → 투사체에 붙은 StunOnHit
            }
        }

        // 탐색 범위 Gizmo용 기본 범위 (부모 UnitAttack이 그린다)
        protected override float GetBaseSearchRange()
        {
            return searchRange;
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
