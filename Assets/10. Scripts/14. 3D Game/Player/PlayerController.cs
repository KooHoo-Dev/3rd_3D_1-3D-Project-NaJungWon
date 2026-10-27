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

        public StatClass PlayerStat => Player.Stat.unitStat; //너무 길어서 임시로 뺌
        
        [Header("스킬")]
        [SerializeField] private UnitSkill eSkill;
        [SerializeField] private UnitSkill qSkill;

        // 스킬 사용 중이면 이동/점프/다른 스킬을 막는다
        private bool IsCasting => (eSkill != null && eSkill.IsCasting) || (qSkill != null && qSkill.IsCasting);
        
        public float speed = 4f;
        
        private void Awake()
        {
            Player = GetComponentInParent<Player>(); //플레이어 정보를 비동기로 정보가 다 Init된 다음에 가져오도록 바꿀것
            //speed = PlayerStat.Speed;
            //eSkill = Player.unitDef.ESkill.UnitSkill;
            //qSkill = Player.unitDef.QSkill.UnitSkill;
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
                movement.SetValue(Vector2.zero, speed, cameraController.YawRotation);
                return;
            }
            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            movement.SetValue(inputAxis, speed, cameraController.YawRotation);
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
