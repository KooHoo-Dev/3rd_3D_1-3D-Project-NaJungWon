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

        private void Start()
        {
            UnitDef unitDef = TableLoader.Instance.UnitTable.Get(testUnitID);

            Player player = Instantiate(playerPrefab, startPosition.position, startPosition.rotation);
            player.Init(unitDef);

            FlowFieldManager.Instance.players.Add(player.transform);
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