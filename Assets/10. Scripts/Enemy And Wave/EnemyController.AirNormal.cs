using UnityEngine;

namespace PROJECT3D
{
    // 공중 몹 기본 컨트롤러.
    //  - 수평 이동 : 흐름장을 쓰지 않고 플레이어에게 직진한다
    //  - 높이      : 발밑 표면 + hoverHeight 를 유지 (플레이어가 바닥에 있으면 플레이어 위까지)
    //  - 장애물    : 앞쪽 표면이 내 높이보다 높으면 수평 이동을 멈추고 먼저 위로 올라간 뒤 넘어간다
    //  ※ '표면' = 위에서 아래로 레이를 쐈을 때 처음 맞는 지형/장애물 꼭대기
    //  ※ 공격은 프리팹에 붙인 EnemyAttack(SingleShot 등)이 한다
    public class AirNormal : EnemyControllerBase
    {
        [Header("공중 설정")]
        [SerializeField] private float hoverHeight = 3f;       // 발밑 표면에서 떠 있는 기본 높이
        [SerializeField] private float abovePlayer = 2f;       // 플레이어가 바닥에 있을 때 그 위로 뜰 높이
        [SerializeField] private float flyClearance = 1f;      // 장애물 꼭대기와 띄울 여유 높이
        [SerializeField] private float lookAhead = 1f;         // 앞쪽 장애물을 검사할 거리 (보통 칸 크기)
        [SerializeField] private float surfaceRayHeight = 50f; // 표면 레이를 쏘기 시작할 높이 (가장 높은 장애물보다 높게)
        [SerializeField] private LayerMask surfaceMask;        // 지형 + 장애물 레이어 둘 다 체크

        private float aheadSurfaceY = float.MinValue; // 앞쪽 표면 높이 (ApplyMovement에서 재고 UpdateHeight에서 쓴다)
        private CharacterController3D playerMovement; // 플레이어 접지 판정용

        #region UnityMethod

        protected override void OnEnable()
        {
            aheadSurfaceY = float.MinValue; // 풀에서 다시 나올 때 이전 값 지우기 (출현 높이 계산보다 먼저)
            base.OnEnable();
        }

        #endregion

        #region 외부 설정

        // 추적 방향 : 플레이어에게 직진
        protected override Vector3 GetChaseDirection()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return Vector3.zero;

            Vector3 chase = target.transform.position - transform.position;
            chase.y = 0f;
            return chase.normalized;
        }

        // 수평 이동 : 앞쪽이 내 높이보다 높으면 이동 보류
        protected override bool ApplyMovement(Vector3 direction, float deltaTime)
        {
            Vector3 dir = direction.normalized;

            // 앞쪽 표면 높이를 재 둔다. 못 재면(맵 밖) 아주 낮은 값으로 둔다.
            if (TryGetSurfaceHeight(transform.position + dir * lookAhead, out float surfaceY))
                aheadSurfaceY = surfaceY;
            else
                aheadSurfaceY = float.MinValue;

            // 앞쪽 꼭대기 + 여유가 나보다 높다 → 이번 프레임은 상승만 하고 대기
            if (aheadSurfaceY + flyClearance > transform.position.y)
                return false;

            transform.position += dir * (MoveSpeed * deltaTime);
            return true;
        }

        // 목표 고도 : 출현 연출(GetSpawnTargetY)과 UpdateHeight가 같이 쓴다
        protected override float GetTargetHeight()
        {
            if (TryGetSurfaceHeight(transform.position, out float underY) == false)
                return transform.position.y; // 발밑에 아무것도 없으면 지금 높이 유지

            float targetY = underY + hoverHeight;                          // 1. 발밑 표면 위
            targetY = Mathf.Max(targetY, aheadSurfaceY + flyClearance);    // 2. 앞 장애물을 넘을 높이

            if (IsPlayerGrounded())                                        // 3. 플레이어 위 (점프/낙하 중엔 제외)
                targetY = Mathf.Max(targetY, GetTargetOrNull().transform.position.y + abovePlayer); // [변경] LocalPlayer 직접 참조 → 부모 GetTargetOrNull (멀티 때 한 곳만 바꾸면 되게)

            return targetY;
        }

        #endregion

        #region 내부 설정

        // 해당 XZ 지점의 표면(지형/장애물 꼭대기) 높이를 위에서 아래로 레이를 쏴서 잰다
        private bool TryGetSurfaceHeight(Vector3 point, out float surfaceY)
        {
            Vector3 origin = new Vector3(point.x, transform.position.y + surfaceRayHeight, point.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                                surfaceRayHeight * 2f, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                surfaceY = hit.point.y;
                return true;
            }

            surfaceY = 0f;
            return false;
        }

        // 플레이어가 바닥에 붙어 있는지 확인 (나중에 멀티에 맞게 변경할 것)
        private bool IsPlayerGrounded()
        {
            if (playerMovement == null) 
                playerMovement = GetTargetOrNull().GetComponent<CharacterController3D>(); // [변경] 부모 GetTargetOrNull 재사용

            return playerMovement.IsGrounded;
        }

        #endregion
    }
}