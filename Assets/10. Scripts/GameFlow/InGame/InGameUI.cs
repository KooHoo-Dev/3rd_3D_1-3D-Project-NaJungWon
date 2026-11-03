using UnityEngine;

namespace PROJECT3D
{
    // 인게임 UI 묶음 (Canvas에 붙인다)
    // GameManager가 플레이어를 만든 뒤 Init(player)로 한 번에 연결한다
    public class InGameUI : MonoBehaviour
    {
        [SerializeField] private GameInfoUI gameInfo;           // 상단 : 타이머, 경험치, 골드, 킬
        [SerializeField] private PlayerInfoUI playerInfo;       // 우측 하단 : 캐릭터 정보
        [SerializeField] private PlayerStatPanel statPanel;     // 증강 선택 시 : 플레이어 스탯
        [SerializeField] private ArousalListPanel arousalPanel; // 증강 선택 시 : 진화 목록

        // 골드/킬/경험치 시스템이 생기면 여기로 값을 넘긴다
        public GameInfoUI GameInfo => gameInfo;
        public PlayerInfoUI PlayerInfo => playerInfo;

        private Player player; //킬/골드를 보여줄 내 플레이어

        #region UnityMethod

        private void OnDestroy()
        {
            if (player != null) player.OnRecordChanged -= RefreshRecord;
            if (ExpManager.Instance == null) return;
            ExpManager.Instance.OnExpChanged -= gameInfo.SetExp;
            ExpManager.Instance.OnLevelChanged -= playerInfo.SetLevel;
        }

        #endregion

        #region 외부 설정

        public void Init(Player player)
        {
            this.player = player; // 새로 추가
            gameInfo.Init();
            playerInfo.Init(player);
            statPanel.Init(player);
            arousalPanel.Init();

            // 두 창은 증강 선택창이 열릴 때만 보인다
            statPanel.Close();
            arousalPanel.Close();

            // 킬 / 골드 (내 것만), 경험치 바 / 레벨 (공용) =====
            player.OnRecordChanged += RefreshRecord;
            ExpManager.Instance.OnExpChanged += gameInfo.SetExp;
            ExpManager.Instance.OnLevelChanged += playerInfo.SetLevel;
        }

        #endregion

        #region 내부 설정

        private void RefreshRecord()
        {
            gameInfo.SetKillCount(player.KillCount);
            gameInfo.SetGold(player.Gold);
        }

        #endregion
    }
}