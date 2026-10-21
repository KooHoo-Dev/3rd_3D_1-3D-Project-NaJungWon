using UnityEngine;

namespace PROJECT3D
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterController3D : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 1.2f;
        [Header("바닥 확인용")] 
        [Tooltip("접지 중에 아래로 누르는 힘. 0이면 경사면에서 미끄러질 수 있다")]
        [SerializeField] private float groundStickVelocity = -2f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundMask;

        private CharacterController cc;
        private float verticalVelocity;
        private bool jumpRequested;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            
        }
        
        
        public void RequestJump()
        {
            jumpRequested = true;
        }

        public void ResetVerticalVelocity()
        {
            verticalVelocity = 0f;
        }

        private void ApplyJump()
        {
            if (jumpRequested == false) return;
            jumpRequested = false;

            if (IsGrounded == false) return;

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            IsGrounded = false;
        }
    }
}
