using System.Threading;
using Cysharp.Threading.Tasks;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public class PlayerController : PlayerControllerBase
    {
        [Header("Ref")] 
        [SerializeField] private CharacterController3D movement;
        [SerializeField] private CameraController cameraController;

        [SerializeField] private Transform SkillPivot; //스킬 생성 좌표

        public StatClass PlayerStat => Player.Stat.unitStat; //너무 길어서 임시로 뺌
        
        [Header("스킬")]
        [SerializeField] private UnitSkill eSkill;
        [SerializeField] private UnitSkill qSkill;
        // HUD 쿨타임 표시용
        public UnitSkill UnitESkill => eSkill; 
        public UnitSkill UnitQSkill => qSkill; 
        
        // 스킬 사용 중이면 이동/점프/다른 스킬을 막는다
        private bool IsCasting => (eSkill != null && eSkill.IsCasting) || (qSkill != null && qSkill.IsCasting);
        
        // 플레이어 데이터가 다 들어간 뒤 PlayerSpawner가 호출 // [변경] 주석 정정 (PlayerTester → PlayerSpawner)
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
            
            LockCursor();
        }

        private void OnEnable()
        {
            WorldClock.Instance.OnPauseChanged += HandlePauseChanged;
            movement.OnJumpStart += HandleJumpStart;
            movement.OnFallStart += HandleFallStart;
            movement.OnLanded += HandleLanded;
        }

        private void OnDisable()
        {
            // 게임 종료 중에는 WorldClock이 먼저 사라졌을 수 있다
            if (WorldClock.IsInitialized) WorldClock.Instance.OnPauseChanged -= HandlePauseChanged;
            movement.OnJumpStart -= HandleJumpStart;
            movement.OnFallStart -= HandleFallStart;
            movement.OnLanded -= HandleLanded;
        }
        
        private void Update()
        {
            if (WorldClock.Instance.IsPaused) return;
            UpdateMovement();
            UpdateCameraRotate();
            UpdateSkillInput();
            UpdateAnimation();
        }

        private void UpdateMovement()
        {
            if (IsCasting)
            {
                movement.SetValue(Vector2.zero, PlayerStat.Speed, cameraController.YawRotation);
                return;
            }
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            movement.SetValue(inputAxis, PlayerStat.Speed, cameraController.YawRotation);
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
            
            if (SimpleInput.GetKeyDown(Key.E) && eSkill != null) eSkill.TryUse(aimDirection); // [변경] 스킬이 없는 캐릭터(표에 -1)면 무시
            if (SimpleInput.GetKeyDown(Key.Q) && qSkill != null) qSkill.TryUse(aimDirection); // [변경]
        }

        private void UpdateAnimation()
        {
            if (anim == null) return; // 모델이 아직 없으면

            bool isMoving = SimpleInput.GetMoveAxisRaw().sqrMagnitude > 0.01f && IsCasting == false;
            anim.SetBool(IsMove, isMoving);
            anim.SetBool(IsGrounded, movement.IsGrounded); // Fall / Jump → Idle 착지 전환용
            anim.SetBool(Casting, IsCasting);              // 스킬 사용 중에는 Fall로 넘어가지 않게
        }

        // 점프 시작 (2단 점프 포함)
        private void HandleJumpStart()
        {
            anim.SetTrigger(Jump);
        }

        // 최고점을 지나 내려가기 시작 (걸어서 떨어질 때 포함)
        private void HandleFallStart()
        {
            if (anim == null) return;
            if (IsSkillAnimationPlaying()) return; // 새로 추가 : 스킬 모션은 끊지 않는다 (끝나면 애니메이터가 공중이면 Fall로 넘김)
            anim.SetTrigger(Fall);
        }

        // 새로 추가 : 지금 스킬 모션(태그 "Skill")을 재생 중이거나 그쪽으로 넘어가는 중인지
        private bool IsSkillAnimationPlaying()
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Skill")) return true;
            if (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsTag("Skill")) return true;
            return false;
        }

        // 착지 : 남은 공중 트리거 정리 + 낙하 데미지 (계산은 Player가)
        private void HandleLanded(float fallHeight)
        {
            anim.ResetTrigger(Fall);
            anim.ResetTrigger(Jump);
            Player.ApplyFallDamage(fallHeight);
        }
        
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
        
        // 사망 모션
        private const string DeadStateName = "Unit_Dead"; // UnitController의 사망 상태 이름
        private const float DieWaitLimit = 5f;           // 상태 이름이 안 맞아도 이 시간이 지나면 끝난 걸로 (실제 시간)

        // 사망 모션을 틀고 끝날 때까지 기다린다
        // 게임 시간이 멈춰 있어도 Animator는 실제 시간으로 돌아서 모션은 재생된다
        public async UniTask PlayDieAsync(CancellationToken token)
        {
            if (anim == null) return;

            anim.SetTrigger(Die);

            float waited = 0f;
            while (waited < DieWaitLimit)
            {
                if (IsDieAnimationFinished()) return;
                waited += Time.unscaledDeltaTime;
                await UniTask.Yield(token);
            }
        }

        private bool IsDieAnimationFinished()
        {
            if (anim.IsInTransition(0)) return false;
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            return state.IsName(DeadStateName) && state.normalizedTime >= 1f;
        }
    }
}
