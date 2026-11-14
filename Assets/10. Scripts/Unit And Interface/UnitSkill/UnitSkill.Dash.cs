using UnityEngine;

namespace PROJECT3D
{
    // 돌진 스킬
    //  - 카메라가 바라보는 방향(aimDirection)으로 dashDistance만큼 빠르게 돌진
    //      범위 프리팹 설정 : followOwner 켬(몸을 따라감), hitOncePerTarget 켬(한 돌진에서 같은 적은 한 번), 자식에 돌진 이펙트, impactEffect = 피격 이펙트
    //      반경은 프리팹 hitRadius × 범위 증가 배율(FireHit이 적용), 판정 시간은 돌진 시간(SetDuration), 벽에 막히면 Finish
    //  - 장애물에 부딪히면 그 앞에서 멈춘다
    //  - 돌진하는 동안 : 중력 X, 무적 (끝나면 다시 중력 적용, 무적 해제)
    public class DashSkill : UnitSkill
    {
        [Header("돌진")]
        [SerializeField] private float dashDistance = 8f;        // 최대 돌진 거리
        [SerializeField] private float dashSpeed = 30f;          // 돌진 속도 (초당 거리)
        [SerializeField] private HitUnit dashHitPrefab;          // 돌진 범위 공격

        private CharacterController ownerController;   // 플레이어를 움직일 컨트롤러 (벽 충돌 처리용)
        private CharacterController3D ownerMovement;   // 중력 끄기/켜기용
        private bool isDashing;
        private Vector3 dashDirection;
        private float remainDistance;  // 남은 돌진 거리
        private SummonHit dashHit;     // 이번 돌진에 띄운 범위 공격 (돌진이 끝나면 Finish)

        #region UnityMethod

        protected override void Update()
        {
            base.Update(); // 쿨타임 / 공격 시점 처리
            if (isDashing == false) return;

            // 1. 이번 프레임 이동 (남은 거리보다 더 가지 않게)
            float step = Mathf.Min(dashSpeed * WorldClock.Instance.DeltaTime, remainDistance);
            Vector3 before = Owner.transform.position;
            ownerController.Move(dashDirection * step); // 벽이 있으면 CharacterController가 그 앞에서 막아 준다
            remainDistance -= step;

            // 2. 다 갔거나, 장애물에 막혀 거의 못 움직였으면 끝
            Vector3 moved = Owner.transform.position - before; // [변경] 3D 이동 거리 그대로 비교 (y를 지우지 않음)
            bool isBlocked = moved.magnitude < step * 0.5f;
            if (remainDistance <= 0f || isBlocked) EndDash();
        }

        // 돌진 중에 꺼지면(플레이어 비활성화 등) 상태를 되돌린다
        protected override void OnDisable()
        {
            base.OnDisable();
            if (isDashing) EndDash();
        }

        #endregion

        #region 외부 설정

        // 공격 시점(애니메이션 / 딜레이 후)에 불린다 → 돌진 시작
        protected override void Use(Vector3 aimDirection)
        {
            if (ownerController == null) ownerController = Owner.GetComponent<CharacterController>();
            if (ownerMovement == null) ownerMovement = Owner.GetComponent<CharacterController3D>();

            // 몸 중심에서 화면 정중앙이 가리키는 지점으로 3D 돌진 (위/아래 대각선 포함)
            dashDirection = GetScreenCenterDirection(Owner.HitCenter, aimDirection);
            if (dashDirection.sqrMagnitude < 0.01f) return;
            dashDirection.Normalize();

            remainDistance = dashDistance;
            isDashing = true;
            IsCasting = true; // 돌진 중에는 이동/점프/다른 스킬 막기

            // 몸은 똑바로 선 채 좌우 방향만 돌진 방향으로 (본체는 기울이지 않는다)
            Vector3 lookDirection = dashDirection;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f) Owner.transform.rotation = Quaternion.LookRotation(lookDirection);
            SetDashState(true); // 무적 + 중력 끄기
            StartDashHit();     // 이펙트 + 판정 → 범위 공격 하나로
        }

        #endregion

        #region 내부 설정

        #region 돌진

        // 발밑에 범위 공격을 띄운다 (돌진 방향을 바라보고, 돌진하는 동안 몸을 따라간다)
        //  - 판정 시간 = 끝까지 갔을 때 걸리는 시간. 벽에 막혀 일찍 끝나면 EndDash에서 Finish
        private void StartDashHit()
        {
            dashHit = null;
            if (dashHitPrefab == null) return;

            dashHit = FireHit(dashHitPrefab, Owner.transform.position, dashDirection, GetDamage()) as SummonHit;
            if (dashHit != null) dashHit.SetDuration(dashDistance / Mathf.Max(dashSpeed, 0.01f));
        }

        private void EndDash()
        {
            isDashing = false;
            IsCasting = false;
            SetDashState(false); // 무적 해제 + 중력 다시 켜기

            // 범위 공격 판정 끝 (따라가기도 멈춰서, 남은 이펙트는 멈춘 자리에서 마저 재생 후 풀로)
            if (dashHit != null) dashHit.Finish();
            dashHit = null;
        }

        // 돌진 중 상태 : 무적 켜기/끄기 + 중력 끄기/켜기
        private void SetDashState(bool isDash)
        {
            Player player = Owner as Player;
            if (player != null) player.IsInvincible = isDash; // 돌진 중에는 데미지를 받지 않는다

            if (ownerMovement != null) ownerMovement.SetUseGravity(isDash == false); // 돌진 중에는 중력 X
        }

        #endregion

        #endregion
    }
}
