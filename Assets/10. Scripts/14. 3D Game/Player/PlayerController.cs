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

        //[Header("Settings")]

        private void Update()
        {
            UpdateMovement();
            UpdateSkillInput();
            UpdateAnimation();
        }

        private void UpdateMovement()
        {
            if (movement == null) return;

            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
            

            float Timer = WorldClock.IsInitialized? WorldClock.Instance.DeltaTime : Time.deltaTime;
            

            if (SimpleInput.GetKeyDown(Key.Space))
            {
                movement.RequestJump();
            }
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
            if (anim == null) return;

            Vector2 inputAxis = SimpleInput.GetMoveAxisRaw();
        }
    }
}
