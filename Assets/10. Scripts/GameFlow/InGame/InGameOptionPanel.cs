using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ===== 새로 추가 : 파일 전체 (인게임 설정창 : ESC로 열고 닫기, 로비로 / 게임 종료) =====
namespace PROJECT3D
{
    // 인게임 설정창 (Option UI 캔버스에 붙인다)
    //  - ESC : 닫혀 있으면 열고(게임 일시정지), 열려 있으면 닫는다(게임 재개)
    //  - 증강 선택 중 / 사망 후 / 이미 다른 이유로 멈춰 있을 때는 열리지 않는다
    //  - 커서는 WorldClock 일시정지에 맞춰 PlayerController가 풀고 잠근다
    public class InGameOptionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;                // 설정창 전체 (처음엔 꺼둠)
        [SerializeField] private Button lobbyButton;              // 로비로
        [SerializeField] private Button quitButton;               // 게임 종료
        [SerializeField] private AugmentSelectPanel selectPanel;  // 증강 선택창 (열려 있으면 설정창을 안 연다)

        public bool IsOpen { get; private set; }

        #region UnityMethod

        private void Awake()
        {
            panel.SetActive(false);
            lobbyButton.onClick.AddListener(GoLobby);
            quitButton.onClick.AddListener(QuitGame);
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Escape) == false) return;

            if (IsOpen) Close();
            else TryOpen();
        }

        #endregion

        #region 외부 설정

        public void Close()
        {
            if (IsOpen == false) return;

            IsOpen = false;
            panel.SetActive(false);
            WorldClock.Instance.Resume(); // 게임 재개 (커서도 다시 잠긴다)
        }

        #endregion

        #region 내부 설정

        private void TryOpen()
        {
            if (selectPanel != null && selectPanel.IsOpen) return;                         // 증강 고르는 중
            if (LocalPlayer.Instance != null && LocalPlayer.Instance.Stat.IsDead) return;   // 패배 처리 중
            if (WorldClock.Instance.IsPaused) return;                                      // 이미 다른 이유로 멈춰 있음

            IsOpen = true;
            lobbyButton.interactable = true;
            quitButton.interactable = true;
            panel.SetActive(true);
            WorldClock.Instance.Pause(); // 일시정지 (커서가 풀린다)
        }

        private void GoLobby()
        {
            lobbyButton.interactable = false; // 두 번 눌러서 씬을 두 번 부르지 않게
            SceneLoader.LoadLobby();
        }

        private void QuitGame()
        {
            quitButton.interactable = false;
            GameSession.Instance.Save(); // 나가기 전에 저장 (이번 판에 번 골드는 이미 보유 골드에 들어가 있다)
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
