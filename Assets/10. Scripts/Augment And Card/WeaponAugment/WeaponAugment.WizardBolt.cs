using UnityEngine;

// ===== 새로 추가 : 파일 전체 (마법사 기본 무기 전격, BS 프로젝트 WizardAttackSetting / WizardBullet 기반) =====
namespace PROJECT3D
{
    // 마법사 기본 무기 : 전격 구체
    //  - 조준 : 범위(searchRange) 안 가장 가까운 적 쪽, 없으면 캐릭터 정면 (검기와 같은 공용 조준)
    //  - 투사체 수만큼 플레이어 둘레에 같은 간격으로 생성 → 첫 번째는 조준 방향, 나머지는 바깥쪽으로 직진 (BS와 같은 배치)
    //  - 구체는 관통 직진 투사체(NonTargetHit, pierce 켬). 날아가는 거리 = 프리팹 range (BS : 속도 10 × 3초 = 30)
    //  - 생성 거리(orbitRadius)와 구체 크기는 범위 증가를 받는다 (크기는 HitUnit이 scale로 키움)
    //  - 진화 : 맞은 적을 arousalStunTime초 정지 × arousalStunCount번, arousalStunInterval초 간격 (정지 시간은 지속시간 증가 적용)
    public class WizardBolt : WeaponAugment
    {
        [Header("전격 구체")]
        [SerializeField] private NonTargetHit boltPrefab;    // 구체 투사체 (pierce 켬)
        [SerializeField] private float searchRange = 8f;     // 조준할 적을 찾는 범위 (범위 증가 적용)
        [SerializeField] private float orbitRadius = 1f;     // 플레이어 몸에서 이만큼 떨어진 둘레에 생성 (범위 증가 적용)

        [Header("진화 : 맞은 적 정지")]
        [SerializeField] private float arousalStunTime = 0.5f;     // 한 번 정지 시간 (지속시간 증가 적용)
        [SerializeField] private int arousalStunCount = 3;         // 정지 횟수
        [SerializeField] private float arousalStunInterval = 1f;   // 정지 간격

        protected override void Attack()
        {
            Enemy target = FindTarget(searchRange);              // [변경] 대상을 직접 받아서 첫 구체가 높이까지 조준하게
            Vector3 aimDirection = GetFlatDirectionTo(target);    // [변경] 생성 위치 배치용 수평 방향 (대상 없으면 정면)

            int boltCount = GetWeaponProjectileCount(); // 무기 기본 개수 + 플레이어 개수(상점 + 능력증강)
            float angleStep = 360f / boltCount;

            for (int i = 0; i < boltCount; i++)
            {
                // 조준 방향에서 i번째 각도만큼 돌린 방향 (i = 0이면 조준 방향 그대로)
                Vector3 flatDirection = Quaternion.Euler(0f, angleStep * i, 0f) * aimDirection;

                // [변경] 첫 번째 구체만 대상을 노린다 (나머지는 BS처럼 바깥쪽 수평 직진)
                Enemy boltTarget = null;
                if (i == 0) boltTarget = target;
                FireBolt(flatDirection, boltTarget);
            }
        }

        // 플레이어 둘레의 flatDirection 쪽 지점에서 쏜다
        // [변경] target이 있으면 생성 지점 → 대상 피격 중심으로 (높이 차이 포함), 없으면 flatDirection으로 수평 직진
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

            if (isArousal) hit.SetStun(arousalStunTime * DurationRate, arousalStunCount, arousalStunInterval);
        }

        // 조준 탐색 범위 Gizmo용 기본 범위 (부모 UnitAttack이 그린다)
        protected override float GetBaseSearchRange()
        {
            return searchRange;
        }
    }
}
// ===== 새로 추가 끝 =====
