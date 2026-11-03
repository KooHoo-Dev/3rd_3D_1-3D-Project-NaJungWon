using UnityEngine;

namespace PROJECT3D
{
    // 몸(모델)을 바닥 경사에 맞춰 기울이는 연출 전용 컴포넌트
    // - 본체(부모)는 똑바로 선 채로 위치와 좌우 회전만 담당하고, 기울기는 여기서만 한다
    // - 플레이어 : BodyPivot에 붙인다 / 지상몹 : 모델을 묶은 자식 오브젝트(Model)에 붙인다
    // - 바닥 방향(normal)은 본체 쪽에서 SetGroundNormal로 넣어 준다
    public class BodyTilt : MonoBehaviour
    {
        [SerializeField] private float tiltSpeed = 10f;    // 기울기 보간 속도 (클수록 빨리 기운다)
        [SerializeField] private float maxTiltAngle = 30f; // 최대 기울기 각도 (턱 모서리에서 확 꺾이지 않게)

        private Vector3 groundNormal = Vector3.up; // 바닥 방향 (월드 기준)

        #region UnityMethod

        private void OnEnable()
        {
            // 풀에서 다시 나와도 똑바로 선 상태부터
            groundNormal = Vector3.up;
            transform.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            // 1. 월드 기준 바닥 방향을 부모(본체) 기준으로 바꾼다 (본체가 돌아 있어도 맞게)
            Vector3 localNormal = transform.parent.InverseTransformDirection(groundNormal);

            // 2. 위쪽(Vector3.up)에서 바닥 방향으로 기우는 회전값
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, localNormal);

            // 3. 최대 각도까지만
            targetRotation = Quaternion.RotateTowards(Quaternion.identity, targetRotation, maxTiltAngle);

            // 4. 부드럽게 기울이기
            float t = 1f - Mathf.Exp(-tiltSpeed * WorldClock.Instance.DeltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, t);
        }

        #endregion

        #region 외부 설정

        // 본체가 매 프레임 넣어 준다 (공중이면 Vector3.up)
        public void SetGroundNormal(Vector3 normal)
        {
            groundNormal = normal;
        }

        #endregion
    }
}