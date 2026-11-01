using UnityEngine;

// ===== 새로 추가 : 파일 전체 (전사 기본 무기 검기) =====
namespace PROJECT3D
{
    // 전사 기본 무기 : 검기 베기
    //  - 범위(searchRange) 안에 적이 있으면 가장 가까운 적 방향, 없으면 캐릭터 정면으로 벤다 (캐릭터 기준 수평)
    //  - 플레이어 몸 중심에서 그 방향으로 spawnDistance만큼 떨어진 곳에 직육면체(Cube) 범위를 생성
    //  - 판정 Cube는 눕힌(수평) 그대로 쓴다. 사선 연출이 필요하면 프리팹 안 이펙트 자식만 기울인다 (판정 영향 없음) // [변경]
    //  - 따라가기 / 쓸고 지나가는 판정은 SummonHit 프리팹 설정(followOwner, hitOncePerTarget)으로 정한다 // [변경]
    //  - 투사체 수만큼 벤다 : 첫 번째는 노린 방향, 나머지는 플레이어 둘레로 같은 간격 (BS 마법사 기본 공격과 같은 배치)
    //  - 범위 증가 : 탐색 범위 / 생성 거리 / 판정 크기는 수치로, 이펙트 크기는 HitUnit이 오브젝트 scale로 키운다
    public class Slash : WeaponAugment
    {
        [Header("검기 베기")]
        [SerializeField] private SummonHit slashPrefab;      // 베기 범위 (SummonHit, hitShape = Cube)
        [SerializeField] private float searchRange = 4f;     // 이 무기의 기본 공격 범위 : 이 안의 적을 노린다 (범위 증가 적용)
        [SerializeField] private float spawnDistance = 1.2f; // 몸 중심에서 이만큼 앞에 생성 (범위 증가 적용)

        protected override void Attack()
        {
            Vector3 aimDirection = GetTargetOrForwardDirection(searchRange); // [변경] 부모(WeaponAugment) 공용 조준 재사용

            int slashCount = GetWeaponProjectileCount(); // 무기 기본 개수 + 플레이어 개수(상점 + 능력증강)
            float angleStep = 360f / slashCount;

            for (int i = 0; i < slashCount; i++)
            {
                // 노린 방향에서 i번째 각도만큼 돌린 방향 (i = 0이면 노린 방향 그대로)
                Vector3 direction = Quaternion.Euler(0f, angleStep * i, 0f) * aimDirection;
                FireSlash(direction);
            }
        }

        // direction 쪽으로 한 번 벤다
        private void FireSlash(Vector3 direction)
        {
            Vector3 center = Owner.HitCenter + direction * ApplyRangeBonus(spawnDistance);

            FireHit(slashPrefab, center, direction, GetDamage()); // [변경] 기울이기 삭제. 방향은 FireHit(Launch)에서 진행 방향을 바라보게 된다
        }

        // [변경] 삭제 : GetSlashDirection → 마법사 기본공격도 같은 조준을 써서 부모 GetTargetOrForwardDirection으로 옮김

        // [변경] 탐색 범위 Gizmo는 부모(UnitAttack)가 그린다 (범위 증가 적용값). 여기서는 기본 범위만 알려준다
        protected override float GetBaseSearchRange()
        {
            return searchRange;
        }
    }
}
// ===== 새로 추가 끝 =====
