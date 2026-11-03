using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public class CameraController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float horizontalSensitivity = 1.0f;
        [SerializeField] private float verticalSensitivity = 1.0f;
        [SerializeField] private float pitchAngleMin = -60f; // 아래로 내려다보는 최대 각도
        [SerializeField] private float pitchAngleMax = 60f;  // 위로 올려다보는 최대 각도
        [SerializeField] private float distance = 4f;        // 캐릭터와 카메라의 거리(공전 반경)

        [Header("Ref")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform aimCameraTransform; // 새로 추가 : 조준 카메라 (벽에 가리면 같이 당긴다. 비워 두면 안 함)

        // ===== 새로 추가 : 벽 충돌 (원신처럼 카메라와 캐릭터 사이에 벽이 있으면 캐릭터 쪽으로 붙는다) =====
        [Header("벽 충돌")]
        [SerializeField] private LayerMask obstacleMask;           // 카메라를 막는 레이어 (지형 / 벽 / 장애물). 플레이어 / 몹은 빼기
        [SerializeField] private float collisionRadius = 0.3f;     // 카메라 크기 (구로 쏴서 벽 모서리에 걸쳐도 안 파고들게)
        [SerializeField] private float collisionPadding = 0.2f;    // 벽에서 이만큼 띄운다
        [SerializeField] private float minDistance = 1f;           // 아무리 막혀도 캐릭터에서 이만큼은 떨어진다
        [SerializeField] private float returnSpeed = 6f;           // 벽이 없어졌을 때 원래 거리로 돌아가는 속도 (m/초)

        private float currentDistance; // 지금 카메라 거리 (막히면 줄었다가 천천히 돌아간다)
        private Vector3 aimCameraOffset; // 조준 카메라의 원래 위치 (피벗 기준)
        // ===== 새로 추가 끝 =====

        private Vector2 mouseDelta = Vector2.zero;
        private float angleX; // 위아래(pitch)
        private float angleY; // 좌우(yaw)

        public Quaternion YawRotation => Quaternion.Euler(0f, angleY, 0f);

        #region UnityMethod

        // 새로 추가 : 원래 거리 / 조준 카메라 위치 기억
        private void Awake()
        {
            currentDistance = distance;
            if (aimCameraTransform != null) aimCameraOffset = aimCameraTransform.localPosition;
        }

        private void LateUpdate()
        {
            UpdateAngle();
            mouseDelta = Vector2.zero;

            ApplyRotation();
        }

        #endregion

        #region 외부 설정

        public Vector3 GetAimDirection()
        {
            return Quaternion.Euler(angleX, angleY, 0f) * Vector3.forward;
        }

        public void SetMouseDelta(Vector2 mouseValue)
        {
            mouseDelta = mouseValue;
        }

        #endregion

        #region 내부 설정

        private void UpdateAngle()
        {
            angleX -= mouseDelta.y * verticalSensitivity;
            angleY += mouseDelta.x * horizontalSensitivity;
            angleX = Mathf.Clamp(angleX, pitchAngleMin, pitchAngleMax);
        }

        private void ApplyRotation()
        {
            cameraPivot.rotation = Quaternion.Euler(angleX, angleY, 0f);

            // [변경] 벽에 막히면 캐릭터 쪽으로 붙는다 : 막힐 때는 바로 당기고 (벽 뒤가 안 보이게), 풀리면 천천히 돌아간다
            float allowed = GetAllowedDistance(new Vector3(0f, 0f, -distance));
            if (allowed < currentDistance) currentDistance = allowed;
            else currentDistance = Mathf.MoveTowards(currentDistance, allowed, returnSpeed * Time.unscaledDeltaTime);
            cameraTransform.localPosition = new Vector3(0f, 0f, -currentDistance);

            // 새로 추가 : 조준 카메라도 막히면 같은 방향으로 당긴다 (조준은 정확해야 해서 바로 맞춘다)
            if (aimCameraTransform != null)
            {
                float aimDistance = GetAllowedDistance(aimCameraOffset);
                aimCameraTransform.localPosition = aimCameraOffset.normalized * aimDistance;
            }
        }

        // 새로 추가 : 피벗에서 localOffset 쪽으로 구를 쏴서, 벽에 안 닿고 갈 수 있는 거리 (안 막히면 원래 거리)
        private float GetAllowedDistance(Vector3 localOffset)
        {
            float length = localOffset.magnitude;
            if (length <= 0.01f) return length;

            Vector3 direction = cameraPivot.rotation * (localOffset / length);
            if (Physics.SphereCast(cameraPivot.position, collisionRadius, direction, out RaycastHit hit, length, obstacleMask, QueryTriggerInteraction.Ignore))
                return Mathf.Max(minDistance, hit.distance - collisionPadding);

            return length;
        }

        #endregion
    }
}
