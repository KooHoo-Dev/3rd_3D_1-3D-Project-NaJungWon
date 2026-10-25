using UnityEngine;

namespace PROJECT3D
{
    // 일정 시간마다 돌진하는 지상 몹 (BS Project: DashMonster)
    //  추격 → (쿨타임이 차고 사거리 안이면) 준비(멈춤) → 돌진(방향 고정, 빠르게) → 휴식(멈춤) → 추격 ...
    //  돌진 중 플레이어와 부딪히면 한 번 데미지를 준다.
    //  ※ 돌진 자체가 공격이라 이 몹에는 EnemyAttack 컴포넌트를 붙이지 않는다.
    //    (붙이면 공격 사거리 안에서 Move가 불리지 않아 돌진이 멈춘다)
    public class Dash : GroundNormal
    {
        private enum DashState
        {
            Chase, // 기본 추격 (쿨타임 도는 중)
            Ready, // 멈춰서 돌진 준비 (플레이어에게 보여주는 예고 시간)
            Dash,  // 돌진
            Rest,  // 돌진 후 멈춤
        }

        [Header("돌진 설정")]
        [SerializeField] private float dashCooldown = 5f;  // 돌진 사이 간격 (추격 시간)
        [SerializeField] private float dashRange = 8f;     // 플레이어가 이 거리 안일 때만 돌진 시작
        [SerializeField] private float readyTime = 0.5f;   // 돌진 전 멈춰서 준비하는 시간
        [SerializeField] private float dashSpeed = 20f;    // 돌진 속도
        [SerializeField] private float dashDuration = 0.5f; // 돌진 시간
        [SerializeField] private float restTime = 1f;      // 돌진 후 멈춰 있는 시간

        [Header("돌진 데미지")]
        [SerializeField] private float hitRadius = 0.3f;        // 플레이어와 이 거리 안이면 명중
        [SerializeField] private float damageMultiplier = 2f; // 표의 Damage에 곱할 배율 (BS: 돌진 중 2배)

        private DashState state;
        private float stateTimer;      // 지금 상태에서 남은 시간
        private Vector3 dashDirection; // 준비 시작 때 정한 돌진 방향 (돌진 중에는 안 바뀐다)
        private bool hasHit;           // 이번 돌진에서 이미 맞혔는지 (한 번만 맞히기)

        // 돌진 중일 때만 속도를 바꾼다. 부모의 ApplyMovement가 이 값을 쓴다.
        protected override float MoveSpeed => state == DashState.Dash ? dashSpeed : base.MoveSpeed;

        protected override void OnEnable()
        {
            base.OnEnable();
            ChangeState(DashState.Chase, dashCooldown); // 풀에서 다시 나와도 처음 상태로
        }

        protected override void Move(float deltaTime)
        {
            stateTimer -= deltaTime;

            switch (state)
            {
                case DashState.Chase:
                    base.Move(deltaTime); // 평소에는 기본 추격 (흐름장 + 분리)
                    if (stateTimer <= 0f && IsTargetInDashRange())
                        StartReady();
                    break;

                case DashState.Ready:
                    IsMoving = false; // 제자리에서 준비
                    if (stateTimer <= 0f)
                        ChangeState(DashState.Dash, dashDuration);
                    break;

                case DashState.Dash:
                    // 정해 둔 방향으로만 빠르게 이동 (벽 검사는 ApplyMovement가 그대로 해준다)
                    IsMoving = ApplyMovement(dashDirection, deltaTime);
                    TryHitTarget();

                    // 시간이 끝났거나 벽에 막혔으면 돌진 종료
                    if (stateTimer <= 0f || IsMoving == false)
                        ChangeState(DashState.Rest, restTime);
                    break;

                case DashState.Rest:
                    IsMoving = false;
                    if (stateTimer <= 0f)
                        ChangeState(DashState.Chase, dashCooldown);
                    break;
            }
        }

        private void ChangeState(DashState next, float time)
        {
            state = next;
            stateTimer = time;
        }

        // 돌진 준비 시작 : 이 순간 플레이어 쪽으로 방향을 고정한다
        private void StartReady()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            dashDirection = toTarget.normalized;

            hasHit = false;
            ChangeState(DashState.Ready, readyTime);
        }

        private bool IsTargetInDashRange()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return false;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            return toTarget.sqrMagnitude <= dashRange * dashRange;
        }

        // 돌진 중 내 몸(피격 원통 + 옆 여유)이 대상의 피격 원통에 닿으면 한 번 데미지
        private void TryHitTarget()
        {
            if (hasHit) return;

            CombatEntity target = GetTargetOrNull();
            if (target == null || CombatSystem.Instance == null) return;

            // 내 몸을 기둥 공격 범위로 쓴다 : 반경 = 내 몸 반경 + 여유, 높이 = 내 몸 절반 높이
            float bodyRadius = Enemy.HurtRadius + hitRadius;
            if (target.IsHitBy(Enemy.HitCenter, bodyRadius, HitShape.Column, Enemy.HurtHalfHeight) == false) return;

            int damage = Mathf.RoundToInt(Enemy.Unit.Damage * damageMultiplier);
            CombatSystem.Instance.SendDamage(Enemy, target, damage, target.HitCenter);
            hasHit = true;
        }

        // 애니메이터에 bool 파라미터 "IsDash"가 있어야 동작한다
        protected override void UpdateAnimation()
        {
            base.UpdateAnimation();
            if (Animator == null) return;

            Animator.SetBool("IsDash", state == DashState.Dash);
        }
    }
}