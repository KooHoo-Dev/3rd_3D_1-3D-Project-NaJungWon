using UnityEngine;

// ===== 새로 추가 : 파일 전체 (마법사 Q스킬, BS 프로젝트 Wizard_SK.WizardQSkill 기반) =====
namespace PROJECT3D
{
    // 지점 폭발 스킬 : 화면 중앙이 가리키는 바닥(사거리 밖이면 최대 사거리 지점의 바닥)에 범위 공격을 소환
    //  [변경] 바닥 찾기를 조준선 따라 걷기(반복문) → 화면 중앙 Ray 한 번 + 그 칸 바닥 높이로 단순화
    //      1. 화면 중앙 Ray가 바닥(groundMask)에 닿으면 그 점, 안 닿으면(하늘) Ray 끝 점
    //      2. 그 점이 플레이어에서 maxRange(수평 거리)보다 멀면 → 같은 방향 maxRange 지점으로 당긴다
    //      3. 높이는 MapGrid 칸의 바닥 높이(GroundHeight) → 당겨온 점이 공중이어도 바닥에 생성
    //  - 데미지 / 반경 / 지연 / 이펙트는 zonePrefab(SummonHit)에서 (BS : 반경 안 전부에게 한 번 데미지)
    public class GroundBombSkill : UnitSkill
    {
        [Header("지점 폭발 스킬")]
        [SerializeField] private HitUnit zonePrefab;   // 범위 공격 (SummonHit. 바닥 범위라 hitShape = Column 추천)
        [SerializeField] private float maxRange = 15f; // 플레이어에서 최대 사거리 (수평 거리) // [변경] 기준 : 발사 위치 조준선 거리 → 플레이어 수평 거리
        [SerializeField] private LayerMask groundMask; // 새로 추가 : 화면 중앙 Ray가 맞을 바닥 레이어 (Terrain + Ground). 플레이어/몹 레이어는 빼야 함

        protected override void Use(Vector3 aimDirection)
        {
            Vector3 groundPoint = FindGroundPoint(aimDirection);

            // 몸을 시전 지점 쪽으로 돌린다 (수평만. 검기 스킬과 같은 방식)
            Vector3 flatDirection = groundPoint - Owner.transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.01f) Owner.transform.rotation = Quaternion.LookRotation(flatDirection);
            else flatDirection = Owner.transform.forward; // 발밑에 쓸 때

            FireHit(zonePrefab, groundPoint, flatDirection.normalized, GetDamage());
        }

        // [변경] 화면 중앙 Ray로 바닥 지점 찾기 (위 1~3번)
        private Vector3 FindGroundPoint(Vector3 aimDirection)
        {
            Vector3 ownerPosition = Owner.transform.position;
            Vector3 point;

            if (TryGetScreenCenterRay(out Ray ray)) // 부모 화면 중앙 Ray 재사용
            {
                point = ray.GetPoint(AimDistance);                                                     // 1. 안 닿으면 Ray 끝 점
                if (Physics.Raycast(ray, out RaycastHit hit, AimDistance, groundMask)) point = hit.point; // 1. 바닥에 닿은 점
            }
            else
            {
                point = ownerPosition + aimDirection * maxRange; // 카메라가 없으면 받은 방향 최대 사거리
            }

            // 2. 사거리 밖이면 같은 방향 maxRange 지점으로 당긴다
            Vector3 flat = point - ownerPosition;
            flat.y = 0f;
            if (flat.magnitude > maxRange) point = ownerPosition + flat.normalized * maxRange;

            // 3. 그 칸의 바닥 높이 (맵 밖이면 내 발 높이)
            Tile tile = MapGrid.Instance.Grid.GetTileOrNull(point);
            point.y = (tile != null) ? tile.GroundHeight : ownerPosition.y;
            return point;
        }
    }
}
// ===== 새로 추가 끝 =====
