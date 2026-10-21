using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public class PlayerController : PlayerControllerBase
    {
        //임시
        [Header("이동")] 
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -9.81f;

        [Header("카메라")] 
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2f; // 마우스 감도 입니다.
        [SerializeField] private float maxLookAngle = 80f;

        [Header("애니메이터")] 
        [SerializeField] private Animator animator;

        [Header("바닥 확인용")] 
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundMask;
        
        private CharacterController controller;
        private Vector3 velocity;
        private float verticalLookAngle;
        private Quaternion initialCameraRotation;
        private float moveX, moveZ; // 키를 따라 -1 ~ 1 사이를 부드럽게 움직이는 로직에 사용되는 축 값

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            LockCursor();
            initialCameraRotation = cameraTransform.localRotation;
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }
        
        public void Update()
        {
            UpdateMovement();
            UpdateCameraRotation();
            UpdatePlayerInput();
        }
        
        public void UpdateMovement()
        {
            float speed = SimpleInput.GetKey(Key.LeftShift) ? runSpeed : walkSpeed;
            
            Vector2 inputRaw = SimpleInput.GetMoveAxisRaw();
            moveX = SmoothAxis(moveX, inputRaw.x);
            moveZ = SmoothAxis(moveZ, inputRaw.y);

            Vector3 moveVector = (transform.right * moveX) + (transform.forward * moveZ);
            // moveVector의 길이만큼 현재 프레임의 이동량ㄹ이 조정이 됩니다.
            controller.Move(moveVector * (speed * Time.deltaTime));
            
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            
            bool isGrounded = controller.isGrounded;
            
            if (isGrounded && velocity.y < 0f) velocity.y = -2f;

            if (isGrounded && SimpleInput.GetKeyDown(Key.Space))
            {
                velocity.y = Mathf.Sqrt((jumpHeight * -2f * gravity));
            }
        }
        
        private static float SmoothAxis(float current, float target)
        {
            const float AXIS_SPEED = 3f;
            if (target != 0f && current * target < 0f) current = 0f;
            return Mathf.MoveTowards(current, target, AXIS_SPEED * Time.deltaTime);
        }
        
        public void UpdateCameraRotation()
        {
            const float MOUSE_AXIS_SCALE = 0.1f;
            Vector2 delta = SimpleInput.GetMouseDelta() * MOUSE_AXIS_SCALE;
            float mouseX = delta.x * mouseSensitivity;
            float mouseY = delta.y * mouseSensitivity;
            
            Quaternion yawRotation = Quaternion.Euler(0, mouseX, 0);
            transform.rotation *= yawRotation; // y축은 내가 돌아갑니다. 나는 월드 입니다 

            verticalLookAngle -= mouseY;
            verticalLookAngle = Mathf.Clamp(verticalLookAngle, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(verticalLookAngle, 0f , 0f);
        }
        
        public void UpdatePlayerInput()
        {
            if (SimpleInput.GetKeyDown(Key.E))
            {
                
            }

            if (SimpleInput.GetKeyDown(Key.Q))
            {
                
            }
        }
    }
}