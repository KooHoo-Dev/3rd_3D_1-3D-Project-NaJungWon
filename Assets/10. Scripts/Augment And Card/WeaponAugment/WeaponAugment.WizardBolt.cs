using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 마법사 기본 무기 증강
    public class WizardBolt : WeaponAugment
    {
        [Header("전격 구체")]
        [SerializeField] private NonTargetHit boltPrefab;    // 구체 투사체 (pierce 켬)
        [SerializeField] private float searchRange = 8f;     // 조준할 적을 찾는 범위 (범위 증가 적용)
        [SerializeField] private float orbitRadius = 1f;     // 플레이어 몸에서 이만큼 떨어진 둘레에 생성 (범위 증가 적용 -> 범위증가 적용되면 안됨.)

        [Header("진화 : 맞은 적 정지")]
        [SerializeField] private float arousalStunTime = 0.5f;     // 한 번 정지 시간 (지속시간 증가 적용)
        [SerializeField] private int arousalStunCount = 3;         // 정지 횟수
        [SerializeField] private float arousalStunInterval = 1f;   // 정지 간격

        private List<Enemy> enemyBuffer = new List<Enemy>(); // 새로 추가 : 범위 안 몹 (재사용)

        #region 외부 설정

        protected override void Attack()
        {
            Enemy target = FindTarget(searchRange);              // 대상을 직접 받아서 첫 구체가 높이까지 조준하게
            Vector3 aimDirection = GetFlatDirectionTo(target);    // 생성 위치 배치용 수평 방향 (대상 없으면 캐릭터 정면)

            int boltCount = GetWeaponProjectileCount(); // 무기 기본 개수 + 플레이어 개수(상점 + 능력증강)
            float angleStep = 360f / boltCount;
            EnemyRegistry.FindInRange(Owner.transform.position, ApplyRangeBonus(searchRange), ref enemyBuffer); // 새로 추가 : 방향별 대상 찾기용

            for (int i = 0; i < boltCount; i++)
            {
                // 조준 방향에서 i번째 각도만큼 돌린 방향 (i = 0이면 조준 방향 그대로)
                Vector3 flatDirection = Quaternion.Euler(0f, angleStep * i, 0f) * aimDirection;

                // [변경] 첫 번째 구체 = 가장 가까운 적. 나머지 = 자기 방향 칸(±angleStep/2) 안의 가장 가까운 적 (없으면 바깥쪽 수평 직진)
                Enemy boltTarget = target;
                if (i > 0) boltTarget = FindTargetInDirection(flatDirection, angleStep * 0.5f);
                FireBolt(flatDirection, boltTarget);
            }
        }

        // 조준 탐색 범위 Gizmo용 기본 범위 (부모 UnitAttack이 그린다)
        protected override float GetBaseSearchRange()
        {
            return searchRange;
        }

        #endregion

        #region 내부 설정

        //  flatDirection에서 수평 각도 halfAngle 안에 있는 적 중 가장 가까운 적 (없으면 null)
        private Enemy FindTargetInDirection(Vector3 flatDirection, float halfAngle)
        {
            Vector3 center = Owner.transform.position;
            Enemy nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < enemyBuffer.Count; i++)
            {
                Vector3 toEnemy = enemyBuffer[i].transform.position - center;
                toEnemy.y = 0f;
                if (Vector3.Angle(flatDirection, toEnemy) > halfAngle) continue; // 다른 구체의 방향

                float distance = toEnemy.sqrMagnitude;
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                nearest = enemyBuffer[i];
            }
            return nearest;
        }

        // 플레이어 둘레의 flatDirection 쪽 지점에서 쏜다
        // target이 있으면 생성 지점 → 대상 피격 중심으로 (높이 차이 포함), 없으면 flatDirection으로 수평 직진
        private void FireBolt(Vector3 flatDirection, Enemy target)
        {
            Vector3 from = FirePosition + flatDirection * ApplyRangeBonus(orbitRadius);

            Vector3 direction = flatDirection;
            if (target != null)
            {
                Vector3 toTarget = target.HitCenter - from;
                if (toTarget.sqrMagnitude > 0.01f) direction = toTarget.normalized; // 대상이 생성 지점에 딱 붙어 있으면 수평 그대로
            }

            HitUnit hit = FireHit(boltPrefab, from, direction, GetDamage());
            if (hit == null) return;

            if (isArousal && hit.TryGetComponent(out StunOnHit stun)) stun.Set(arousalStunTime * DurationRate, arousalStunCount, arousalStunInterval); // [변경] hit.SetStun → 투사체에 붙은 StunOnHit
        }

        #endregion
    }
}
