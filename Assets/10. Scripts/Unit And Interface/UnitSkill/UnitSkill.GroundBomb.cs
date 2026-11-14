using UnityEngine;

namespace PROJECT3D
{
    // 지점 폭발 스킬 : 화면 중앙이 가리키는 바닥(사거리 밖이면 최대 사거리 지점의 바닥)에 범위 공격을 소환
    //  바닥 찾기를 조준선 따라 걷기(반복문) → 화면 중앙 Ray 한 번 + 그 칸 바닥 높이로 단순화
    //      1. 화면 중앙 Ray가 바닥(groundMask)에 닿으면 그 점, 안 닿으면(하늘) Ray 끝 점
    //      2. 그 점이 플레이어에서 maxRange(수평 거리)보다 멀면 → 같은 방향 maxRange 지점으로 당긴다
    //      3. 높이는 MapGrid 칸의 바닥 높이(GroundHeight) → 당겨온 점이 공중이어도 바닥에 생성
    //  - 데미지 / 반경 / 지연 / 이펙트는 zonePrefab(SummonHit)에서
    public class GroundBombSkill : UnitSkill
    {
        [Header("지점 폭발 설정")]
        [SerializeField] private HitUnit   zonePrefab;     // 범위 공격 (SummonHit)
        [SerializeField] private float     maxRange = 15f; // 플레이어에서 최대 사거리 (수평 거리)
        [SerializeField] private LayerMask groundMask;     // 화면 중앙 Ray가 맞을 바닥 레이어

        [Header("조준 중 예상 범위 설정 (반투명 구. 없으면 표시 안 함)")]
        [SerializeField] private GameObject previewPrefab; // 처음 조준할 때 하나만 만들어 두고 켜고 끈다

        // 내부 설정
        private GameObject preview; // 만들어 둔 예상 범위 (조준 중에만 켠다)

        #region 외부 설정

        #region 조준 중 예상 범위

        // 조준 중 매 프레임 : 지금 카메라 기준으로 떨어질 자리를 찾아 범위를 그 위치 / 크기로
        public override void UpdateAim()
        {
            if (previewPrefab == null || zonePrefab == null) return;

            if (preview == null) preview = Instantiate(previewPrefab);

            bool hasRay = MakeScreenCenterRay(out Ray ray); // 발사 때 쓸 Ray와 같은 방식 (지금 카메라)
            preview.transform.position = FindGroundPoint(hasRay, ray, Owner.transform.forward);

            float radius = ApplyRangeBonus(zonePrefab.HitRadius);          // 실제 판정 반경과 같게 (범위 증가 적용)
            preview.transform.localScale = Vector3.one * (radius * 2f);
            if (preview.activeSelf == false) preview.SetActive(true);
        }

        public override void EndAim()
        {
            if (preview != null) preview.SetActive(false);
        }

        private void OnDestroy()
        {
            if (preview != null) Destroy(preview);
        }

        #endregion

        #region 스킬 사용

        protected override void Use(Vector3 aimDirection)
        {
            bool hasRay = TryGetScreenCenterRay(out Ray ray);                   // 사용한 순간 저장해 둔 Ray
            Vector3 groundPoint = FindGroundPoint(hasRay, ray, aimDirection); // Ray를 받아서 (조준 중 예상 범위와 같은 계산)

            // 몸을 시전 지점 쪽으로 돌린다 (수평만. 검기 스킬과 같은 방식)
            Vector3 flatDirection = groundPoint - Owner.transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.01f) Owner.transform.rotation = Quaternion.LookRotation(flatDirection);
            else flatDirection = Owner.transform.forward; // 발밑에 쓸 때

            FireHit(zonePrefab, groundPoint, flatDirection.normalized, GetDamage());
        }

        #endregion

        #endregion

        #region 내부 설정

        // 화면 중앙 Ray로 바닥 지점 찾기 (위 1~3번)
        // Ray를 매개변수로 받는다 (발사 = 저장한 Ray / 조준 중 예상 범위 = 지금 카메라 Ray)
        private Vector3 FindGroundPoint(bool hasRay, Ray ray, Vector3 aimDirection)
        {
            Vector3 ownerPosition = Owner.transform.position;
            Vector3 point;

            if (hasRay)
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

        #endregion
    }
}
