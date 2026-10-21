using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    // # CameraController - 카메라 회전과 거리만 담당한다
    // - 캐릭터를 따라다니는 건 부모 오브젝트(CameraPivot을 플레이어 자식으로 둠)가 담당
    // - 마우스 회전은 "엔진 시간" 영역: 카드 선택으로 WorldClock이 정지해도 카메라는 움직여야 한다
    // - 로컬 플레이어 전용. 리모트 플레이어 프리팹에는 붙이지 않는다
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

        private float angleX; // 위아래(pitch)
        private float angleY; // 좌우(yaw)
        
        public Quaternion YawRotation => Quaternion.Euler(0f, angleY, 0f);

        private void Update()
        {
            UpdateAngle();
        }

        private void LateUpdate()
        {
            ApplyRotation();
        }

        private void UpdateAngle()
        {
            Vector2 delta = SimpleInput.GetMouseDelta();
            angleX -= delta.y * verticalSensitivity;
            angleY += delta.x * horizontalSensitivity;
            angleX = Mathf.Clamp(angleX, pitchAngleMin, pitchAngleMax);
        }

        private void ApplyRotation()
        {
            cameraPivot.rotation = Quaternion.Euler(angleX, angleY, 0f);
            cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
        }
    }
}
