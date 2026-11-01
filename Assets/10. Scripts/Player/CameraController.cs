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

        private Vector2 mouseDelta = Vector2.zero;
        private float angleX; // 위아래(pitch)
        private float angleY; // 좌우(yaw)
        
        public Quaternion YawRotation => Quaternion.Euler(0f, angleY, 0f);

        public Vector3 GetAimDirection()
        {
            return Quaternion.Euler(angleX, angleY, 0f) * Vector3.forward;
        }
        
        private void LateUpdate()
        {
            UpdateAngle();
            mouseDelta = Vector2.zero;
            
            ApplyRotation();
        }
        
        public void SetMouseDelta(Vector2 mouseValue)
        {
            mouseDelta = mouseValue;
        }
        
        private void UpdateAngle()
        {
            angleX -= mouseDelta.y * verticalSensitivity;
            angleY += mouseDelta.x * horizontalSensitivity;
            angleX = Mathf.Clamp(angleX, pitchAngleMin, pitchAngleMax);
        }

        
        private void ApplyRotation()
        {
            cameraPivot.rotation = Quaternion.Euler(angleX, angleY, 0f);
            cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
        }
    }
}
