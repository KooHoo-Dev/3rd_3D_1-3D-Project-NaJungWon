using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 패배 결과창 (GameOver UI 캔버스에 붙인다)
    // GameManager가 사망 연출이 끝난 뒤 Open으로 기록을 넘겨 연다
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;          // 결과창 전체 (처음엔 꺼둠)
        [SerializeField] private TMP_Text killCountText;    // 처치 수
        [SerializeField] private TMP_Text goldText;         // 획득 골드
        [SerializeField] private TMP_Text surviveTimeText;  // 생존 시간
        [SerializeField] private Button lobbyButton;        // 로비로 나가기

        #region UnityMethod

        private void Awake()
        {
            panel.SetActive(false);
            lobbyButton.onClick.AddListener(GoLobby);
        }

        #endregion

        #region 외부 설정

        public void Open(int killCount, int gold, float surviveTime)
        {
            killCountText.SetText(killCount.ToString());
            goldText.SetText(gold.ToString());

            int totalSecond = (int)surviveTime;
            int minute = totalSecond / 60;
            int second = totalSecond % 60;
            surviveTimeText.SetText($"{minute:00}:{second:00}");

            lobbyButton.interactable = true;
            panel.SetActive(true);
        }

        #endregion

        #region 내부 설정

        private void GoLobby()
        {
            lobbyButton.interactable = false; // 두 번 눌러서 씬을 두 번 부르지 않게
            SceneLoader.LoadLobby();
        }

        #endregion
    }
}