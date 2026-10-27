using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    // 테스트용 : 테이블의 캐릭터 데이터로 플레이어를 생성한다 (캐릭터 선택이 생기면 지울 것)
    public class PlayerTester : MonoBehaviour
    {
        [SerializeField] private Player playerPrefab;
        [SerializeField] private Transform startPosition;
        [SerializeField] private int testUnitID = 0;
        [Header("테스트용 상점 스탯 (로비 상점이 생기면 지울 것)")]
        [SerializeField] private StatClass testShopStat = new StatClass();
        
        // 플레이어 생성 → 유닛 데이터 넣기 → 컨트롤러에 플레이어 넘기기
        public Player SpawnPlayer()
        {
            UnitDef unitDef = TableLoader.Instance.UnitTable.Get(testUnitID);

            Player player = Instantiate(playerPrefab, startPosition.position, startPosition.rotation);
            player.Init(unitDef, testShopStat);
            
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.Init(player);

            return player;
        }

        private void Update()
        {
            if (SimpleInput.GetKeyDown(Key.Digit1))
            {
                WorldClock.Instance.SetScaleStep(1);
            }
            else if (SimpleInput.GetKeyDown(Key.Digit2))
            {
                WorldClock.Instance.SetScaleStep(2);
            }
            else if (SimpleInput.GetKeyDown(Key.Digit3))
            {
                WorldClock.Instance.SetScaleStep(3);
            }
            else if (SimpleInput.GetKeyDown(Key.Digit4))
            {
                WorldClock.Instance.TogglePause();
            }
        }
        
        
    }
}