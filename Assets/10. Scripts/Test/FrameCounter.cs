using UnityEngine;

namespace PROJECT3D
{
    // 게임 화면 왼쪽 위에 1초 평균 FPS / 프레임 시간 / 몹 수 / 경험치 구 수 / 게임 시간을 표시한다
    //  DGS 빌드 측정(1초 평균 FPS · 활성 몬스터 수, VSync OFF)과 같은 기준으로 비교하려고 만든 테스트용 스크립트
    //  씬의 빈 오브젝트에 붙이면 된다 (UI 설정 필요 없음, OnGUI로 그림)
    public class FrameCounter : MonoBehaviour
    {
        [Header("측정 설정")]
        [SerializeField] private bool turnOffVSync = true;  // VSync OFF + 프레임 제한 없음 (DGS 측정과 같은 조건)
        [SerializeField] private float interval = 1f;       // 평균을 내는 시간 (초)
        [SerializeField] private int fontSize = 24;

        private int frameCount;      // 이번 구간 프레임 수
        private float elapsed;       // 이번 구간 시간 (Time.timeScale 영향 없는 실제 시간)
        private float averageFps;
        private float frameMs;
        private GUIStyle style;

        // 새로 추가 : 씬에 붙이지 않아도 게임이 시작되면 측정 오브젝트를 자동으로 만든다 (끄려면 false)
        private static readonly bool AutoCreate = true;

        // 새로 추가 : 첫 씬이 열린 뒤 한 번만 실행. 측정 오브젝트(화면 표시 + 파일 기록)를 만들고 씬이 바뀌어도 남긴다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnStart()
        {
            if (AutoCreate == false) return;
            if (FindFirstObjectByType<FrameCounter>() != null) return; // 씬에 직접 붙여 둔 게 있으면 그걸 쓴다

            GameObject measure = new GameObject("FrameMeasure");
            measure.AddComponent<FrameCounter>();
            measure.AddComponent<FrameLogger>();
            DontDestroyOnLoad(measure);
        }

        #region UnityMethod

        private void Awake()
        {
            if (turnOffVSync)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
        }

        private void Update()
        {
            frameCount++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < interval) return;

            averageFps = frameCount / elapsed;
            frameMs = elapsed * 1000f / frameCount;
            frameCount = 0;
            elapsed = 0f;
        }

        private void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = fontSize;
                style.normal.textColor = Color.yellow;
            }

            string text = $"FPS {averageFps:0.0} ({frameMs:0.0} ms)\n"
                        + $"몹 {EnemyRegistry.AliveCount}  경험치 구 {ExpOrb.ActiveCount}\n"
                        + $"시간 {FormatTime(Time.timeSinceLevelLoad)}";
            GUI.Label(new Rect(10f, 10f, 600f, fontSize * 4f), text, style);
        }

        #endregion

        // 초 → "분:초" (예 : 187초 → 3:07)
        public static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}