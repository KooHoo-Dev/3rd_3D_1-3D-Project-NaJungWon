using TMPro;
using UnityEngine;

// ===== 새로 추가 : 파일 전체 (몹 머리 위 데미지 숫자. 3rd-3000 FPS 프로젝트 FloatingText 기반) =====
namespace PROJECT3D
{
    // 월드 좌표(몹 머리 위)를 매 프레임 화면 좌표로 바꿔서, 캔버스 위에서 떠오르는 숫자 UI
    //  - DamageTextSpawner가 풀에서 꺼내 캔버스 밑으로 옮긴 뒤 Set으로 내용을 넣는다
    //  - 시간은 WorldClock 기준 (일시정지 / 증강 선택 중에는 멈춘다)
    public class DamageText : MonoBehaviour, IObjectPoolItem
    {
        [SerializeField] private float riseSpeed = 2f; // 초당 위로 올라가는 거리 (월드 m)

        private TMP_Text text;
        private RectTransform rectTransform;
        private RectTransform canvasRect;   // 좌표 변환 기준 캔버스
        private Camera uiCamera;            // Overlay 캔버스면 null, Camera 캔버스면 그 카메라
        private Camera mainCamera;          // 월드 → 화면 변환용

        private Vector3 worldPosition;
        private float remainTime;

        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        #region UnityMethod

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            remainTime -= WorldClock.Instance.DeltaTime;
            if (remainTime <= 0f)
            {
                ReturnToPool();
                return;
            }

            worldPosition += Vector3.up * (riseSpeed * WorldClock.Instance.DeltaTime); // 월드 기준으로 떠오른다
            UpdatePosition();
        }

        #endregion

        #region 외부 설정

        // 출력할 내용 넣기. canvas = 이 글자가 올라갈 캔버스
        public void Set(Canvas canvas, Vector3 position, string content, float duration, Color color)
        {
            canvasRect = canvas.transform as RectTransform;
            uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
            mainCamera = Camera.main;

            worldPosition = position;
            remainTime = duration;
            text.SetText(content);
            text.color = color;

            UpdatePosition(); // 켜지는 첫 프레임부터 제자리에 보이게
        }

        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(this);
        }

        #endregion

        #region 내부 설정

        // 월드 좌표 → 화면 좌표 → 캔버스 좌표
        private void UpdatePosition()
        {
            if (mainCamera == null || canvasRect == null) return;

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            text.enabled = (screenPoint.z > 0f); // z < 0 = 카메라 뒤 → 안 그린다

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint);
            rectTransform.anchoredPosition = localPoint;
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
