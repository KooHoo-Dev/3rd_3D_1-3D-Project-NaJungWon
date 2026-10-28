using UnityEngine;

namespace PROJECT3D
{
    // 인게임 플레이어 생성 담당
    // GameSession의 슬롯(고른 캐릭터 + 상점 스탯)대로 플레이어를 만든다
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private Player playerPrefab;
        [SerializeField] private Transform[] startPositions; // 플레이어 시작 위치 (최대 4명)

        // 슬롯 수만큼 생성하고, 내 플레이어를 돌려준다
        public Player SpawnPlayers()
        {
            GameSession session = GameSession.Instance;

            // 로비를 거치지 않고 인게임 씬에서 바로 테스트할 때
            if (session.Slots.Count == 0) session.CreateSoloRoom();

            Player localPlayer = null;

            for (int i = 0; i < session.Slots.Count; i++)
            {
                PlayerSlot slot = session.Slots[i];

                //멀티 - 다른 플레이어는 RemotePlayer로 생성
                if (slot.IsLocal == false) continue;

                localPlayer = Spawn(slot, GetStartPosition(i));
            }

            return localPlayer;
        }

        // 플레이어 생성 → 유닛 데이터 + 상점 스탯 넣기 → 컨트롤러에 플레이어 넘기기
        private Player Spawn(PlayerSlot slot, Transform start)
        {
            UnitDef unitDef = TableLoader.Instance.UnitTable.Get(slot.UnitID);

            Player player = Instantiate(playerPrefab, start.position, start.rotation);
            player.Init(unitDef, slot.ShopStat);

            PlayerController controller = player.GetComponent<PlayerController>();
            controller.Init(player);

            return player;
        }

        // 예외처리 0번 위치
        private Transform GetStartPosition(int index)
        {
            if (index < startPositions.Length) return startPositions[index];
            Debug.LogWarning("[PlayerSawner] 스폰 좌표 수 또는 인덱스 문제 발생");
            return startPositions[0];
        }
    }
}