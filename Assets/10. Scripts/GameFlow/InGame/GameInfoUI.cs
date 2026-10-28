using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 인게임 상단 정보 : 게임 타이머(중앙), 경험치 바(맨 위), 골드 / 킬 수(우측 상단)
    // 골드, 킬, 경험치는 아직 시스템이 없어서 Set 함수만 열어둔다 (시스템이 생기면 거기서 호출)
    public class GameInfoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Image expBar;      // Image Type = Filled
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text killCountText;

        private int lastSecond = -1; // 초가 바뀔 때만 글자를 다시 쓴다

        public void Init()
        {
            SetGold(0);
            SetKillCount(0);
            SetExp(0f);
            lastSecond = -1;
        }

        private void Update()
        {
            if (WorldClock.IsInitialized == false) return;

            int totalSecond = (int)WorldClock.Instance.ElapsedTime;
            if (totalSecond == lastSecond) return;

            lastSecond = totalSecond;
            int minute = totalSecond / 60;
            int second = totalSecond % 60;
            timerText.SetText($"{minute:00}:{second:00}");
        }

        public void SetGold(int gold)
        {
            goldText.SetText(gold.ToString());
        }

        public void SetKillCount(int count)
        {
            killCountText.SetText(count.ToString());
        }

        // 0 ~ 1 (현재 경험치 / 다음 레벨까지 필요한 경험치)
        public void SetExp(float ratio)
        {
            expBar.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
