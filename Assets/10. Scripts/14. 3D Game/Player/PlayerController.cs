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
        public float speed = 4f;
        
        //[Header("Settings")]
        //float Timer = WorldClock.IsInitialized? WorldClock.Instance.DeltaTime : Time.deltaTime;

        private void Awake()
        {
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
            if (SimpleInput.GetKeyDown(Key.E))
            {
                // E스킬
                
            }

            if (SimpleInput.GetKeyDown(Key.Q))
            {
                // Q스킬
            }
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
