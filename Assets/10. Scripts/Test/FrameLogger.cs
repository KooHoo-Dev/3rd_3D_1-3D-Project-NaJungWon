// ===== 새로 추가 : 프레임 기록 (텍스트 파일) =====
using System.IO;
using UnityEngine;

namespace PROJECT3D
{
    // 일정 간격(기본 1초)마다 게임 시간 / 몹 수 / 경험치 구 수 / 1초 평균 FPS / 프레임 시간을 텍스트 파일에 한 줄씩 쓴다
    //  줄 모양을 DGS 측정 표와 같게 맞춰서(| 게임 시간 | 활성 몬스터 | ... |) 문서에 그대로 붙여 넣을 수 있다
    //  파일 위치 : 에디터 = 프로젝트 폴더/FrameLog, 빌드 = 실행 파일 폴더/FrameLog (이름에 날짜 · 시간)
    //  맨 위에 측정 환경(해상도, CPU, GPU, RAM)과 끝날 때 최저 / 평균 FPS를 남긴다
    public class FrameLogger : MonoBehaviour
    {
        [Header("기록 설정")]
        [SerializeField] private float interval = 1f;  // 평균을 내고 한 줄 쓰는 간격 (초)

        private StreamWriter writer;
        private int frameCount;
        private float elapsed;

        // 전체 요약용
        private float minFps = float.MaxValue;
        private string minFpsLine = "";
        private float fpsSum;
        private int lineCount;

        #region UnityMethod

        private void Start()
        {
            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "FrameLog");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"FrameLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");

            writer = new StreamWriter(path, false);
            writer.WriteLine($"환경 : {Screen.width}x{Screen.height} · {SystemInfo.processorType} · {SystemInfo.graphicsDeviceName} · RAM {SystemInfo.systemMemorySize / 1024}GB");
            writer.WriteLine($"VSync {QualitySettings.vSyncCount} (0 = OFF) · 목표 프레임 {Application.targetFrameRate} (-1 = 제한 없음) · {(Application.isEditor ? "에디터" : "빌드")}");
            writer.WriteLine();
            writer.WriteLine("| 게임 시간 | 활성 몬스터 | 경험치 구 | FPS (1초 평균) | 프레임 시간 |");
            writer.WriteLine("|---|---|---|---|---|");
            writer.Flush();
            Debug.Log($"[FrameLogger] 기록 시작 : {path}");
        }

        private void Update()
        {
            if (writer == null) return;

            frameCount++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < interval) return;

            float fps = frameCount / elapsed;
            float ms = elapsed * 1000f / frameCount;
            string line = $"| {FrameCounter.FormatTime(Time.timeSinceLevelLoad)} | {EnemyRegistry.AliveCount} | {ExpOrb.ActiveCount} | {fps:0.0} | {ms:0.0} ms |";
            writer.WriteLine(line);
            writer.Flush(); // 강제 종료돼도 여기까지는 남게

            if (fps < minFps)
            {
                minFps = fps;
                minFpsLine = line;
            }
            fpsSum += fps;
            lineCount++;

            frameCount = 0;
            elapsed = 0f;
        }

        private void OnDestroy()
        {
            if (writer == null) return;

            if (lineCount > 0)
            {
                writer.WriteLine();
                writer.WriteLine($"평균 FPS {fpsSum / lineCount:0.0} · 최저 FPS {minFps:0.0}");
                writer.WriteLine($"최저 구간 : {minFpsLine}");
            }
            writer.Close();
            writer = null;
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
