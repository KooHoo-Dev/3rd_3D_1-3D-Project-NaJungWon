using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    // 테스트용 키 입력 (플레이어 생성은 PlayerSpawner로 옮김)
    public class PlayerTester : MonoBehaviour
    {
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