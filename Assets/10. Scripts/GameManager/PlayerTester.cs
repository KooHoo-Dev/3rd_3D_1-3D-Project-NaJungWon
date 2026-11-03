using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    // 테스트용 키 입력 (플레이어 생성은 PlayerSpawner로 옮김)
    public class PlayerTester : MonoBehaviour
    {
        #region UnityMethod

        private void Update()
        {
            // 새로 추가 : 죽은 뒤에는 배속 / 정지 키 막기 (1번 키가 패배 정지를 풀어서 게임이 계속되던 문제)
            if (LocalPlayer.Instance != null && LocalPlayer.Instance.Stat.IsDead) return;

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

        #endregion
    }
}