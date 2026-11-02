using UnityEngine;
using System;

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
        [Tooltip("내려가는 중이어도 발밑 바닥이 이 거리 안이면 '떨어지기 시작'으로 보지 않는다 (작은 턱/내리막)")]
        [SerializeField] private float nearGroundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;            // 바닥 레이어 (꼭 지정)
        
        [Header("회전 / 기울기")]
        [SerializeField] private float turnSpeed = 15f;  // 이동 방향으로 도는 속도
        [SerializeField] private BodyTilt bodyTilt;      // BodyPivot에 붙인 기울기 연출 (없으면 기울기 안 함)
        private Vector3 groundNormal = Vector3.up;       // 밟고 있는 바닥 방향 (OnControllerColliderHit에서 받는다)
        
        private CharacterController controller;
        private float moveX, moveZ; // 키를 따라 -1 ~ 1 사이를 부드럽게 움직이는 로직에 사용되는 축 값
        private Vector3 velocity;
        private bool jumpRequested;

        # region 상태 알림
        public event Action OnJumpStart;     
        public event Action OnFallStart;     
        public event Action<float> OnLanded; // 착지했을 때 (떨어진 높이 = 공중 최고점 - 착지점)

        private bool wasGrounded = true; // 지난 프레임 접지 상태
        private bool isFallingDown;      // 이번 공중에서 OnFallStart를 이미 보냈는지
        private float airPeakY;          // 공중에 있는 동안 가장 높았던 y
        # endregion
        
        [Header("받아오는값")]
        private Vector3 inputRaw = Vector3.zero;
        private float speed;
        private Quaternion movementRotation = Quaternion.identity;

        public bool IsGrounded { get; private set; }
        private bool useGravity = true; // 중력 적용 여부

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
        
        // 중력 켜기/끄기
        public void SetUseGravity(bool isOn)
        {
            useGravity = isOn;
            velocity.y = 0f; // 끌 때는 떠 있던 속도를 지우고, 켤 때는 멈춘 상태에서 다시 떨어지기 시작
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
            Vector3 horizontalStep = moveVector * (speed * WorldClock.Instance.DeltaTime); // 이번 프레임 수평 이동량을 변수로
            controller.Move(horizontalStep);
            
            RotateToMove(moveVector);
            
            if (useGravity == false) return;
            
            velocity.y += gravity * WorldClock.Instance.DeltaTime;
            Vector3 verticalStep = velocity * WorldClock.Instance.DeltaTime; // 수직 이동량을 변수로
            // 내리막에서 바닥에 붙어 있게 하는 처리
            // 수평으로 먼저 움직이면 내리막에서는 발이 바닥에서 살짝 뜬다.
            // 땅에 붙어 걷는 중(점프 X)이면 수평 이동한 만큼 더 눌러서 경사를 따라 내려가게 한다 (45도 경사까지)
            if (IsGrounded && velocity.y <= 0f) verticalStep.y -= horizontalStep.magnitude;
            
            controller.Move(verticalStep);
            
            // controller의 isGrounded는 마지막 Move 적용 기준이라
            // 마지막 수직이동 바로 뒤에 읽어야 합니다.
            IsGrounded = controller.isGrounded;
            if (IsGrounded && velocity.y < 0f)
            {
                velocity.y = groundStickVelocity;
                jumpCount = 0;
            }

            UpdateAirState(); // 새로 추가 : 하강 시작 / 착지 알림
            
            if (jumpRequested)
            {
                ApplyJump();
            }
            
            // 몸 기울기 (공중이면 똑바로)
            if (IsGrounded == false) groundNormal = Vector3.up;
            if (bodyTilt != null) bodyTilt.SetGroundNormal(groundNormal);
        }

        // 공중 상태 (최고점 기록 → 하강 시작 → 착지)
        private void UpdateAirState()
        {
            float y = transform.position.y;

            // 1. 땅 : 방금 내려왔으면 착지 알림
            if (IsGrounded)
            {
                if (wasGrounded == false) OnLanded?.Invoke(airPeakY - y);
                wasGrounded = true;
                isFallingDown = false;
                return;
            }

            // 2. 공중 : 최고점 기록 (방금 떨어졌으면 지금 높이부터)
            if (wasGrounded) airPeakY = y;
            if (y > airPeakY) airPeakY = y;
            wasGrounded = false;

            // 3. 최고점을 지나 내려가기 시작한 순간 한 번만 알림
            //    걸어서 떨어질 때는 발밑 바닥이 멀어진 순간 (작은 턱을 내려갈 때는 알리지 않는다)
            if (isFallingDown) return;
            if (velocity.y > 0f) return; // 아직 올라가는 중
            if (CheckNearGround()) return; // 발밑이 가까움 (작은 턱)

            isFallingDown = true;
            OnFallStart?.Invoke();
        }

        // 발밑에서 아래로 짧게 검사 (발이 살짝 묻혀 있어도 맞도록 조금 위에서 쏜다)
        private bool CheckNearGround()
        {
            Vector3 feet = controller.bounds.center + Vector3.down * controller.bounds.extents.y;
            Vector3 origin = feet + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, nearGroundDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore);
        }
        
        // 이동 방향 바라보기
        private void RotateToMove(Vector3 moveVector)
        {
            moveVector.y = 0f;
            if (moveVector.sqrMagnitude < 0.01f) return; // 멈춰 있으면 마지막 방향 유지

            Quaternion targetRotation = Quaternion.LookRotation(moveVector);
            float t = 1f - Mathf.Exp(-turnSpeed * WorldClock.Instance.DeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }
        
        // CharacterController가 Move 중에 무언가에 닿으면 유니티가 불러 준다
        // 접지 중에는 groundStickVelocity로 계속 바닥을 누르고 있어서 매 프레임 바닥에 닿는다
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f) groundNormal = hit.normal; // 위쪽을 향한 면만 바닥으로 본다 (벽은 무시)
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
            velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight); // 점프 높이 공식 (v = √(-2 × 중력 × 높이))
            IsGrounded = false;

            isFallingDown = false; // 2단 점프면 다시 최고점을 지날 때 하강 알림
            OnJumpStart?.Invoke();
        }
    }
}