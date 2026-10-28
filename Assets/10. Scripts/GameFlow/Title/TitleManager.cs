using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 타이틀 화면
    // 1. 테이블 로드 + 저장 데이터 불러오기
    // 2. 방 만들기 / 방 참가 / 설정 / 나가기
    // [변경] 솔로 버튼 삭제 (방 만들기가 솔로 방을 만든다), 설정 버튼 추가
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("설정 창 (임시)")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsCloseButton;

        private void Awake()
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            exitButton.onClick.AddListener(OnExitClicked);
            settingsCloseButton.onClick.AddListener(OnSettingsCloseClicked);
        }

        private void Start()
        {
            TableLoader.Instance.LoadData();
            // GameSession은 처음 접근할 때 저장 파일을 불러온다
            Debug.Log($"[TitleManager] 보유 골드 : {GameSession.Instance.Data.Gold}");

            settingsPanel.SetActive(false); // 새로 추가
        }

        //멀티 - 방 만들기. 지금은 솔로 방
        // [변경] 원래 OnSoloClicked에 있던 내용
        private void OnCreateRoomClicked()
        {
            GameSession.Instance.CreateSoloRoom();
            SceneLoader.LoadLobby();
        }

        //멀티 - 방 참가. 지금은 솔로와 같다
        private void OnJoinRoomClicked()
        {
            Debug.Log("[TitleManager] 방 참가는 아직 없습니다. 솔로로 진행합니다");
            OnCreateRoomClicked(); // [변경] OnSoloClicked → OnCreateRoomClicked
        }

        // ===== 새로 추가 : 설정 창 열기/닫기 =====
        private void OnSettingsClicked()
        {
            settingsPanel.SetActive(true);
        }

        private void OnSettingsCloseClicked()
        {
            settingsPanel.SetActive(false);
        }
        // ===== 새로 추가 끝 =====

        private void OnExitClicked()
        {
            Application.Quit();
        }
    }
}
