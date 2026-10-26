using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace PROJECT3D
{
    public class PlayerController : PlayerControllerBase
    {
        [Header("Ref")] 
        [SerializeField] private CharacterController3D movement;
        [SerializeField] private CameraController cameraController;

        public StatClass PlayerStat => LocalPlayer.Instance.Stat.unitStat; //너무 길어서 임시로 뺌
        
        [Header("스킬")]
        [SerializeField] private UnitSkill eSkill;
        [SerializeField] private UnitSkill qSkill;

        // 스킬 사용 중이면 이동/점프/다른 스킬을 막는다
        private bool IsCasting => (eSkill != null && eSkill.IsCasting) || (qSkill != null && qSkill.IsCasting);
        
        public float speed = 4f;
        
        //[Header("Settings")]
        //float Timer = WorldClock.IsInitialized? WorldClock.Instance.DeltaTime : Time.deltaTime;

        private void Awake()
        {
            //eSkill = LocalPlayer.Instance.unitDef.ESkill.UnitSkill;
            //qSkill = LocalPlayer.Instance.unitDef.QSkill.UnitSkill;
            LockCursor();
        }
        
        private void Update()
        {
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
        
        private void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
