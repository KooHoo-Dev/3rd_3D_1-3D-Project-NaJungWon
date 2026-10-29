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
        public UnitSkill ESkill => eSkill; // 새로 추가 : HUD 쿨타임 표시용
        public UnitSkill QSkill => qSkill; // 새로 추가

        // 스킬 사용 중이면 이동/점프/다른 스킬을 막는다
        private bool IsCasting => (eSkill != null && eSkill.IsCasting) || (qSkill != null && qSkill.IsCasting);
        
        // 플레이어 데이터가 다 들어간 뒤 PlayerTester가 호출
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
        }

        private void OnDisable()
        {
            // 게임 종료 중에는 WorldClock이 먼저 사라졌을 수 있다
            if (WorldClock.IsInitialized) WorldClock.Instance.OnPauseChanged -= HandlePauseChanged;
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

            Vector3 aimDirection = cameraController.YawRotation * Vector3.forward;

            if (SimpleInput.GetKeyDown(Key.E)) eSkill.TryUse(aimDirection);
            if (SimpleInput.GetKeyDown(Key.Q)) qSkill.TryUse(aimDirection);
        }

        private void UpdateAnimation()
        {
            
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
    }
}
