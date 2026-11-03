using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 로비 화면 교통정리
    // - 방 플레이어 칸 4개 (내 칸을 누르면 캐릭터 선택 창)
    // - 능력치 업그레이드(상점) 창, 옵션 창
    // - 보유 골드 표시
    // - 게임 시작 / 타이틀로
    public class LobbyManager : MonoBehaviour
    {
        // ===== 새로 추가 : 방 플레이어 칸 =====
        [Header("방 플레이어 칸")]
        [SerializeField] private PartySlotUI[] partySlots;
        // ===== 새로 추가 끝 =====

        [Header("창")]
        [SerializeField] private CharacterSelectPanel characterSelectPanel;
        [SerializeField] private ShopPanel shopPanel;
        [SerializeField] private GameObject optionPanel;      // 새로 추가 (임시)
        [SerializeField] private Button optionCloseButton;    // 새로 추가

        [Header("버튼 / 표시")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private Button shopButton;           // 새로 추가
        [SerializeField] private Button optionButton;         // 새로 추가
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        [Header("테스트용 (게임 골드 획득이 생기면 지울 것)")]
        [SerializeField] private int testGold = 1000;

        #region UnityMethod

        private void Awake()
        {
            shopButton.onClick.AddListener(OnShopClicked);                 // 새로 추가
            optionButton.onClick.AddListener(OnOptionClicked);             // 새로 추가
            optionCloseButton.onClick.AddListener(OnOptionCloseClicked);   // 새로 추가
            startButton.onClick.AddListener(OnStartClicked);
            backButton.onClick.AddListener(OnBackClicked);

            // ===== 새로 추가 : 플레이어 칸 클릭 연결 =====
            for (int i = 0; i < partySlots.Length; i++)
            {
                partySlots[i].Init(i, OnPartySlotClicked);
            }
            // ===== 새로 추가 끝 =====
        }

        private void OnEnable()
        {
            GameSession.Instance.OnDataChanged += Refresh; // [변경] RefreshGold → Refresh
        }

        private void Start()
        {
            // 타이틀을 거치지 않고 로비 씬에서 바로 테스트할 때를 위해
            TableLoader.Instance.LoadData();
            if (GameSession.Instance.Slots.Count == 0) GameSession.Instance.CreateSoloRoom();

            characterSelectPanel.Init();
            shopPanel.Init();
            CloseAllPanels(); // 새로 추가
            Refresh();        // [변경] RefreshGold → Refresh
        }

        private void OnDisable()
        {
            if (GameSession.IsInitialized)
                GameSession.Instance.OnDataChanged -= Refresh; // [변경] RefreshGold → Refresh
        }

        #endregion

        #region 외부 설정

        // 테스트용 : 플레이 중 인스펙터에서 컴포넌트 우클릭 -> Add Test Gold
        [ContextMenu("Add Test Gold")]
        private void AddTestGold()
        {
            GameSession.Instance.AddGold(testGold);
        }

        #endregion

        #region 내부 설정

        #region 화면

        // [변경] RefreshGold → Refresh : 골드 + 플레이어 칸 다시 그리기
        private void Refresh()
        {
            goldText.SetText($"골드: {GameSession.Instance.Data.Gold}");

            // ===== 새로 추가 : 플레이어 칸 =====
            for (int i = 0; i < partySlots.Length; i++)
            {
                PlayerSlot slot = null;
                if (i < GameSession.Instance.Slots.Count) slot = GameSession.Instance.Slots[i];
                partySlots[i].Refresh(slot);
            }
            // ===== 새로 추가 끝 =====
        }

        // ===== 새로 추가 : 창 모두 닫기 =====
        private void CloseAllPanels()
        {
            characterSelectPanel.Close();
            shopPanel.Close();
            optionPanel.SetActive(false);
        }
        // ===== 새로 추가 끝 =====

        #endregion

        #region 버튼

        // ===== 새로 추가 : 플레이어 칸 / 상점 / 옵션 버튼 =====
        // 플레이어 칸 클릭 : 내 칸이면 캐릭터 선택 창
        private void OnPartySlotClicked(int index)
        {
            if (index >= GameSession.Instance.Slots.Count)
            {
                Debug.Log("[LobbyManager] 빈 칸입니다 (멀티 초대는 아직 없음)");
                return;
            }

            if (GameSession.Instance.Slots[index].IsLocal == false) return; // 다른 플레이어 칸

            CloseAllPanels();
            characterSelectPanel.Open();
        }

        private void OnShopClicked()
        {
            CloseAllPanels();
            shopPanel.Open();
        }

        private void OnOptionClicked()
        {
            CloseAllPanels();
            optionPanel.SetActive(true);
        }

        private void OnOptionCloseClicked()
        {
            optionPanel.SetActive(false);
        }
        // ===== 새로 추가 끝 =====

        private void OnStartClicked()
        {
            // 고른 캐릭터와 상점 스탯을 내 슬롯에 확정하고 인게임으로
            GameSession.Instance.RefreshLocalSlot();
            GameSession.Instance.Save();
            SceneLoader.LoadInGame();
        }

        private void OnBackClicked()
        {
            SceneLoader.LoadTitle();
        }

        #endregion

        #endregion
    }
}
