﻿using DTT.AreaOfEffectRegions;
using UnityEngine;

namespace PROJECT3D
{
    // 일정 시간마다 돌진하는 지상 몹
    //  추격 → (쿨타임이 차고 attackRange 안이면) 준비(멈춤 + 범위표시) → 돌진(방향 고정, 빠르게) → 휴식(멈춤) → 추격 ...
    //  돌진 중 플레이어와 부딪히면 한 번 데미지 적용
    //  준비 동안 돌진할 길을 DTT Line Region으로 먼저 긋고, Fill Progress가 1이 되면 그 선 위를 그대로 돌진
    //      - 돌진 거리 = attackRange × 2 (플레이어를 지나 반대편 같은 거리까지)
    //      - 범위표시 길이 = 돌진 거리, 폭 = 내 몸 지름 + 명중 여유 (실제 명중 판정과 같은 폭)
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
        [SerializeField] private float dashCooldown = 5f;  // 돌진 쿨타임
        [SerializeField] private float readyTime = 1f;     // 범위표시가 차오르는 시간 (Fill Progress 0 → 1)
        [SerializeField] private float dashSpeed = 20f;    // 돌진 속도
        [SerializeField] private float restTime = 1f;      // 돌진 후 멈춰 있는 시간

        [Header("돌진 데미지")]
        [SerializeField] private float hitRadius = 0.3f;        // 플레이어와 이 거리 안이면 명중
        [SerializeField] private float damageMultiplier = 2f; // 표의 Damage에 곱할 배율 (BS: 돌진 중 2배)

        [Header("범위표시 (DTT Line Region. 몹 프리팹 자식)")]
        [SerializeField] private LineRegion dashWarning; // Line 3 Region을 자식으로 넣고 연결 (없으면 범위표시 없이 돌진)
        [SerializeField] private float warningHeight = 0.05f; // 바닥에서 살짝 띄우기 (바닥과 겹쳐 깜빡이지 않게)

        private DashState state;
        private Vector3 dashDirection; // 준비 시작 때 정한 돌진 방향 (돌진 중에는 안 바뀐다)
        private Vector3 dashStart;     // 돌진 시작 위치 (간 거리 재기용)
        private float stateTimer;      // 지금 상태에서 남은 시간
        private bool hasHit;           // 이번 돌진에서 이미 맞혔는지 (한 번만 맞히기)

        private float DashDistance => attackRange * 2f; // 돌진 거리

        // 돌진 중일 때만 속도를 변경
        protected override float MoveSpeed => state == DashState.Dash ? dashSpeed : base.MoveSpeed;

        // 범위표시 머티리얼을 몹마다 따로 (풀에서 처음 만들어질 때 한 번)
        protected override void Awake()
        {
            base.Awake();
            
            if (dashWarning != null && dashWarning.gameObject.scene.IsValid() == false)
                dashWarning = Instantiate(dashWarning, transform);

            RegionMaterialUtil.UseOwnMaterials(dashWarning);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ChangeState(DashState.Chase, dashCooldown); // 풀에서 다시 나와도 처음 상태로
            SetWarningActive(false); // 새로 추가
        }

        protected override void Move(float deltaTime)
        {
            stateTimer -= deltaTime;

            switch (state)
            {
                case DashState.Chase:
                    // attackRange 안이면 멈춰서 돌진 준비
                    if (IsTargetInRange(attackRange))
                    {
                        IsMoving = false;
                        LookAtTarget();
                        if (stateTimer <= 0f) StartReady();
                    }
                    else
                    {
                        base.Move(deltaTime); // 멀면 기본 추격 (흐름장 + 분리)
                    }
                    break;

                case DashState.Ready:
                    IsMoving = false; // 제자리에서 준비
                    LookDirection = dashDirection; // 돌진할 쪽을 바라본다
                    UpdateWarning();               // 범위표시가 차오른다
                    if (stateTimer <= 0f)
                    {
                        SetWarningActive(false);   // 다 차면 끄고 그 선 위로 돌진
                        dashStart = transform.position;
                        ChangeState(DashState.Dash, 0f);
                    }
                    break;

                case DashState.Dash:
                    // 정해 둔 방향으로만 빠르게 이동
                    IsMoving = ApplyMovement(dashDirection, deltaTime);
                    TryHitTarget();

                    // 돌진 거리만큼 갔거나 벽에 막혔으면 돌진 종료
                    if (GetDashMoved() >= DashDistance || IsMoving == false)
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

        // 돌진 준비 시작 : 이 순간 플레이어 쪽으로 방향 고정
        private void StartReady()
        {
            CombatEntity target = GetTargetOrNull();
            if (target == null) return;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            dashDirection = toTarget.normalized;

            hasHit = false;
            ChangeState(DashState.Ready, readyTime);
            SetWarningActive(true);
            UpdateWarning();
        }

        // 돌진 시작 위치에서 수평으로 간 거리
        private float GetDashMoved()
        {
            Vector3 moved = transform.position - dashStart;
            moved.y = 0f;
            return moved.magnitude;
        }

        // 범위표시
        private void SetWarningActive(bool active)
        {
            if (dashWarning == null) return;
            if (dashWarning.gameObject.activeSelf != active) dashWarning.gameObject.SetActive(active);
        }

        // 내 발밑 바닥에서 돌진 방향으로 선을 긋는다. 진행도 = 준비 시간이 지난 비율
        // 몸은 천천히 돌아가므로(UpdateRotation), 자식이어도 위치 / 방향은 매 프레임 월드 기준으로 다시 맞춘다
        private void UpdateWarning()
        {
            if (dashWarning == null) return;

            Vector3 position = transform.position;
            Tile tile = MapGrid.Instance.Grid.GetTileOrNull(position); // 기존 Grid로 바닥 높이
            if (tile != null) position.y = tile.GroundHeight + warningHeight;
            dashWarning.transform.SetPositionAndRotation(position, Quaternion.LookRotation(dashDirection));

            // 범위표시의 월드 크기를 항상 1로 맞추고(부모 크기를 되돌림), 길이 / 폭은 월드 값 그대로 넣는다
            Transform parent = dashWarning.transform.parent;
            Vector3 parentScale = parent.lossyScale;
            dashWarning.transform.localScale = new Vector3(1f / Mathf.Max(parentScale.x, 0.01f),
                1f / Mathf.Max(parentScale.y, 0.01f),
                1f / Mathf.Max(parentScale.z, 0.01f));
            dashWarning.Angle = 0f; // 방향은 위에서 회전으로 맞춤
            dashWarning.Length = DashDistance;
            dashWarning.Width = (Enemy.HurtRadius + hitRadius) * 2f; // 명중 판정(TryHitTarget)과 같은 폭
            dashWarning.FillProgress = (readyTime > 0f) ? 1f - stateTimer / readyTime : 1f;
        }

        // 죽으면 범위표시도 끈다 (죽은 뒤에는 Move가 안 불려서 남아 있게 됨)
        public override void StartDead()
        {
            base.StartDead();
            SetWarningActive(false);
        }
        // ===== 새로 추가 끝 =====
        
        // 돌진 중 내 몸이 대상의 피격 원통에 닿으면 한 번 데미지
        private void TryHitTarget()
        {
            if (hasHit) return;

            CombatEntity target = GetTargetOrNull();
            if (target == null || CombatSystem.Instance == null) return;

            // 내 몸을 기둥 공격 범위로 사용
            if (IsTouchingTarget(target, hitRadius) == false) return;

            int damage = Mathf.RoundToInt(Enemy.Unit.Damage * damageMultiplier);
            CombatSystem.Instance.SendDamage(Enemy, target, damage, target.HitCenter);
            hasHit = true;
        }

        protected override void UpdateAnimation()
        {
            base.UpdateAnimation();
            bool isDash = state == DashState.Dash;
            Animator.SetBool(IsDash, isDash);
            if (isDash) Animator.SetBool(IsMove, false);
        }

        // 돌진 거리(보라 원. 멈추는 거리 attackRange는 부모의 빨간 원) + 돌진 명중 범위(몸 + hitRadius) Gizmo
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected(); // 공격 사거리 / 접촉 범위

            Gizmos.color = Color.magenta;
            HitGizmo.DrawCircle(transform.position, attackRange * 2f);  // 새로 추가 : 돌진 거리 (멈추는 거리 attackRange 원은 부모가 그린다)
        }
    }
}
