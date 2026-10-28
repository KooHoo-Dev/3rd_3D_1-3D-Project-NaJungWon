using Study.Utilities;
using UnityEngine;

namespace PROJECT3D
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterController3D : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private int jumpCount;
        [SerializeField] private int maxJumpCount = 2;
        [Header("바닥 확인용")] 
        [Tooltip("접지 중에 아래로 누르는 힘. 0이면 경사면에서 미끄러질 수 있다")]
        [SerializeField] private float groundStickVelocity = -2f;
        //[SerializeField] private float groundCheckDistance = 0.1f;
        //[SerializeField] private LayerMask groundMask;
        
        private CharacterController controller;
        private float moveX, moveZ; // 키를 따라 -1 ~ 1 사이를 부드럽게 움직이는 로직에 사용되는 축 값
        private Vector3 velocity;
        private bool jumpRequested;
        
        [Header("받아오는값")]
        private Vector3 inputRaw = Vector3.zero;
        private float speed;
        private Quaternion movementRotation = Quaternion.identity;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void SetValue(Vector2 moveValue, float speedValue, Quaternion rotationValue)
        {
            inputRaw = moveValue;
            speed = speedValue;
            movementRotation = rotationValue;
        }
        
        public void RequestJump()
        {
            if (jumpCount >= maxJumpCount) return;
            jumpRequested = true;
        }
        
        private void Update()
        {
            moveX = SmoothAxis(moveX, inputRaw.x);
            moveZ = SmoothAxis(moveZ, inputRaw.y);
            
            Vector3 moveVector = movementRotation * new Vector3(moveX, 0f, moveZ);
            controller.Move(moveVector * (speed * WorldClock.Instance.DeltaTime));

            velocity.y += gravity * WorldClock.Instance.DeltaTime;
            controller.Move(velocity * WorldClock.Instance.DeltaTime);
            
            // controller의 isGrounded는 마지막 Move 적용 기준이라
            // 마지막 수직이동 바로 뒤에 읽어야 합니다.
            IsGrounded = controller.isGrounded;
            if (IsGrounded && velocity.y < 0f)
            {
                velocity.y = groundStickVelocity;
                jumpCount = 0;
            }
            
            if (jumpRequested)
            {
                ApplyJump();
            }
        }
        
        private static float SmoothAxis(float current, float target)
        {
            // 아래값은 보간 속도(?)를 의미한다고 생각하시면 됩니다. 댐핑값
            const float AXIS_SPEED = 3f;
            if (target != 0f && current * target < 0f) current = 0f;
            return Mathf.MoveTowards(current, target, AXIS_SPEED * WorldClock.Instance.DeltaTime);
        }

        public void ResetVerticalVelocity()
        {
            velocity.y = 0f;
        }

        private void ApplyJump()
        {
            if (jumpRequested == false) return;
            jumpRequested = false;

            if (jumpCount >= maxJumpCount) return;
            jumpCount++;
            velocity.y = Mathf.Sqrt(jumpHeight * groundStickVelocity * gravity);
            IsGrounded = false;
        }
    }
}
