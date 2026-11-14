using System.Threading;
using Cysharp.Threading.Tasks;
using Study.Utilities;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public class PlayerController : PlayerControllerBase
    {
        [Header("참조 설정")]
        [SerializeField] private CharacterController3D movement;         // 이동 / 점프 / 중력
        [SerializeField] private CameraController      cameraController; // 마우스 카메라 회전
        [SerializeField] private Transform             SkillPivot;       // 스킬 생성 좌표

        [Header("스킬 설정")]
        [SerializeField] private UnitSkill eSkill; // E 스킬
        [SerializeField] private UnitSkill qSkill; // Q 스킬

        [Header("조준 스킬 설정")]
        [SerializeField] private CinemachineCamera aimCamera;                 // 조준 카메라 (켜 둔 채로 Priority만 바꾼다)
        [SerializeField] private int               aimRestingPriority = 0;    // 평소 Priority
        [SerializeField] private int               aimActivePriority  = 10;   // 조준 중 Priority
        [SerializeField] private float             aimMoveSpeedRate   = 0.5f; // 조준 중 이동 속도 배율

        // 외부 공개
        public StatClass PlayerStat => Player.Stat.unitStat; // 플레이어 스탯 (너무 길어서 임시로 뺌)
        public float     Speed      => PlayerStat.Speed;     // 이동 속도
        public UnitSkill UnitESkill => eSkill;               // E 스킬 (HUD 쿨타임 표시용)
        public UnitSkill UnitQSkill => qSkill;               // Q 스킬 (HUD 쿨타임 표시용)
        public bool      IsAiming   => aimingSkill != null;  // 조준 중인지 (조준점 UI(PlayerInfoUI)가 확인)

        // 내부 설정 : 스킬 / 조준
        // 스킬 사용 중이면 이동 / 점프 / 다른 스킬을 막는다
        private bool      IsCasting => (eSkill != null && eSkill.IsCasting) || (qSkill != null && qSkill.IsCasting);
        private UnitSkill aimingSkill; // 지금 조준 중인 스킬 (null = 조준 안 함)
        private Key       aimingKey;   // 그 스킬의 키 (떼면 사용)

        // 내부 설정 : 사망 모션
        private const float DieWaitLimit = 5f; // 상태 이름이 안 맞아도 이 시간이 지나면 끝난 걸로 (실제 시간)

        #region UnityMethod

        private void OnEnable()
        {
            WorldClock.Instance.OnPauseChanged += HandlePauseChanged;
            movement.OnJumpStart += HandleJumpStart;
            movement.OnFallStart += HandleFallStart;
            movement.OnLanded += HandleLanded;
        }

        private void Update()
        {
            if (WorldClock.Instance.IsPaused) return;
            UpdateMovement();
            UpdateCameraRotate();
            UpdateSkillInput();
            UpdateAnimation();
        }

        private void OnDisable()
        {
            // 게임 종료 중에는 WorldClock이 먼저 사라졌을 수 있다
            if (WorldClock.IsInitialized) WorldClock.Instance.OnPauseChanged -= HandlePauseChanged;
            movement.OnJumpStart -= HandleJumpStart;
            movement.OnFallStart -= HandleFallStart;
            movement.OnLanded -= HandleLanded;
            if (IsAiming) EndAim(); // 조준 중에 꺼지면 카메라 / 모션 되돌리기
        }

        #endregion

        #region 외부 설정

        #region 초기화

        // 플레이어 데이터가 다 들어간 뒤 PlayerSpawner가 호출
        public void Init(Player player)
        {
            Player = player;
            if (PlayerStat.Speed == 0) Debug.Log($"[PlayerController] PlayerStat.Speed가 0임");
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            anim = GetComponentInChildren<Animator>();
            
            // 표에 스킬 프리팹 없으면 인스펙터에 넣은 스킬 그대로 (테스트용)
            if(player.unitDef == null) Debug.LogWarning($"[PlayerController] unitDef가 null임");
            UnitDef def = player.unitDef;

            if (def.ESkill == null || def.ESkill.UnitSkill == null)
            {
                if(def.ESkill == null) Debug.Log($"[PlayerController] def.Eskill이 null임");
                else if(def.ESkill.UnitSkill == null) Debug.Log($"[PlayerController] def.Eskill.UnitSkill이 null임");
            }
            else
            {
                eSkill = Instantiate(def.ESkill.UnitSkill, SkillPivot);
                eSkill.Init(def.ESkill);
            }

            if (def.QSkill == null || def.QSkill.UnitSkill == null)
            {
                if(def.QSkill == null) Debug.Log($"[PlayerController] def.QSkill이 null임");
                else if(def.QSkill.UnitSkill == null) Debug.Log($"[PlayerController] def.QSkill.UnitSkill이 null임");
            }
            else
            {
                qSkill = Instantiate(def.QSkill.UnitSkill, SkillPivot);
                qSkill.Init(def.QSkill);
            }

            // 패시브 스킬 (표에 패시브 프리팹이 있을 때만)
            if (def.PassiveSkill != null && def.PassiveSkill.Passive != null)
            {
                PassiveSkill passive = Instantiate(def.PassiveSkill.Passive, SkillPivot);
                passive.Init(player, def.PassiveSkill);
            }

            LockCursor();
        }

        #endregion

        #region 사망

        // 사망 모션을 틀고 끝날 때까지 기다린다
        // 게임 시간이 멈춰 있어도 Animator는 실제 시간으로 돌아서 모션은 재생된다
        public async UniTask PlayDieAsync(CancellationToken token)
        {
            if (anim == null) return;

            anim.SetTrigger(AnimatorKeys.Die);

            float waited = 0f;
            while (waited < DieWaitLimit)
            {
                if (IsDieAnimationFinished()) return;
                waited += Time.unscaledDeltaTime;
                await UniTask.Yield(token);
            }
        }

        #endregion

        #endregion

        #region 내부 설정

        #region 입력 / 이동

        private void UpdateMovement()
        {
            if (IsCasting)
            {
                movement.SetValue(Vector2.zero, Speed, cameraController.YawRotation);
                return;
            }
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            float moveSpeed = IsAiming ? Speed * aimMoveSpeedRate : Speed; // 조준 중에는 느리게
            movement.SetValue(inputAxis, moveSpeed, cameraController.YawRotation);
            if (SimpleInput.GetKeyDown(Key.Space)) movement.RequestJump();
        }

        private void UpdateCameraRotate()
        {
            Vector2 delta = SimpleInput.GetMouseDelta();
            if (delta.sqrMagnitude < 1) return;
            cameraController.SetMouseDelta(delta);
        }

        private void UpdateSkillInput()
        {
            if (IsCasting) return;

            Vector3 aimDirection = cameraController.GetAimDirection(); // 카메라가 바라보는 3D 방향

            // 조준 중이면 키를 뗐을 때 사용 (다른 스킬 입력은 막는다)
            // 일시정지 중에 뗀 경우도 있어서 GetKeyUp 대신 "안 누르고 있으면"으로 확인
            if (IsAiming)
            {
                if (SimpleInput.GetKey(aimingKey))
                {
                    aimingSkill.UpdateAim(); // 조준 중 스킬별 처리 (GroundBomb 예상 범위 표시 등)
                    return;
                }
                aimingSkill.TryUse(aimDirection); // 조준 끝내기 전에 사용 → 조준 카메라 기준 화면 중앙 Ray가 저장된다
                EndAim();
                return;
            }

            if (SimpleInput.GetKeyDown(Key.E) && eSkill != null) PressSkill(eSkill, Key.E, aimDirection);
            if (SimpleInput.GetKeyDown(Key.Q) && qSkill != null) PressSkill(qSkill, Key.Q, aimDirection);
        }

        #endregion

        #region 조준

        // 스킬 키를 눌렀을 때 (조준 스킬이면 조준 시작, 아니면 바로 사용)
        private void PressSkill(UnitSkill skill, Key key, Vector3 aimDirection)
        {
            if (IsAiming) return; // 같은 프레임에 두 키를 눌렀을 때

            if (skill.IsAimSkill)
            {
                if (skill.CanUse) StartAim(skill, key); // 쿨타임 중이면 조준도 안 들어간다
                return;
            }
            skill.TryUse(aimDirection);
        }

        // 조준 시작 : 조준 카메라 켜기 + Unit_Aim 모션(AttackLayer, 상체만 → 하체는 이동 모션) + 몸을 조준 방향으로
        private void StartAim(UnitSkill skill, Key key)
        {
            aimingSkill = skill;
            aimingKey = key;
            if (aimCamera != null) aimCamera.Priority = aimActivePriority; // Priority 올리기
            if (anim != null) anim.SetBool(AnimatorKeys.IsAiming, true);
            movement.SetAimFacing(true);
            SetAttackLayerWeight(1f); // 조준 자세(상체) 보이게
        }

        // 조준 끝 : 전부 되돌린다 (스킬 사용은 부르는 쪽에서)
        private void EndAim()
        {
            aimingSkill.EndAim(); // 스킬별 조준 끝 처리 (예상 범위 표시 끄기 등)
            aimingSkill = null;
            if (aimCamera != null) aimCamera.Priority = aimRestingPriority; // Priority 내리기
            if (anim != null) anim.SetBool(AnimatorKeys.IsAiming, false);
            movement.SetAimFacing(false);
            SetAttackLayerWeight(0f); // 조준 자세(상체) 끄기
        }

        // AttackLayer(상체) 가중치. Unit_Aim이 AttackLayer에 있어서 조준 중에는 1
        private void SetAttackLayerWeight(float weight)
        {
            AnimatorKeys.SetAttackLayerWeight(anim, weight); // [변경] 레이어 찾기 → AnimatorKeys (무기증강과 같이 씀)
        }

        #endregion

        #region 애니메이션

        private void UpdateAnimation()
        {
            if (anim == null) return; // 모델이 아직 없으면

            bool isMoving = SimpleInput.GetMoveAxisRaw().sqrMagnitude > 0.01f && IsCasting == false;
            anim.SetBool(AnimatorKeys.IsMove, isMoving);
            anim.SetBool(AnimatorKeys.IsGrounded, movement.IsGrounded); // Fall / Jump → Idle 착지 전환용
            anim.SetBool(AnimatorKeys.IsCasting, IsCasting);            // 스킬 사용 중에는 Fall로 넘어가지 않게

            // 조준 중에는 매 프레임 1로 유지 (조준 직전에 나가던 기본공격 상태가 끝나면서 AttackTimingSender가 0으로 끄는 것 덮어쓰기)
            if (IsAiming) SetAttackLayerWeight(1f);
        }

        // 점프 시작 (2단 점프 포함)
        private void HandleJumpStart()
        {
            anim.SetTrigger(AnimatorKeys.Jump);
        }

        // 최고점을 지나 내려가기 시작 (걸어서 떨어질 때 포함)
        private void HandleFallStart()
        {
            if (anim == null) return;
            if (IsSkillAnimationPlaying()) return; // 스킬 모션은 끊지 않는다 (끝나면 애니메이터가 공중이면 Fall로 넘김)
            anim.SetTrigger(AnimatorKeys.Fall);
        }

        // 지금 스킬 모션을 재생 중이거나 그쪽으로 넘어가는 중인지
        // E / Q 스킬 상태의 AttackTimingSender가 켜고 끄는 값
        private bool IsSkillAnimationPlaying()
        {
            return Player != null && Player.IsSkillMotion;
        }

        // 착지 : 남은 공중 트리거 정리 + 낙하 데미지 (계산은 Player가)
        private void HandleLanded(float fallHeight)
        {
            anim.ResetTrigger(AnimatorKeys.Fall);
            anim.ResetTrigger(AnimatorKeys.Jump);
            Player.ApplyFallDamage(fallHeight);
        }

        #endregion

        #region 커서

        private void HandlePauseChanged(bool isPaused)
        {
            if (isPaused) UnlockCursor();
            else LockCursor();
        }

        private void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void UnlockCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        #endregion

        #region 사망

        private bool IsDieAnimationFinished()
        {
            if (anim.IsInTransition(0)) return false;
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            return state.shortNameHash == AnimatorKeys.DeadState && state.normalizedTime >= 1f;
        }

        #endregion

        #endregion
    }
}
