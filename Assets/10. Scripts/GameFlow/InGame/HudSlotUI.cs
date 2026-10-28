using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 인게임 하단 캐릭터 정보의 아이콘 칸 하나 (패시브 / 스킬 / 무기 / 능력치 공용)
    // [아이콘, 쿨타임 글자, 레벨 글자]
    // - 무기/능력치 : 레벨 숫자 (증강이 없으면 빈 글자)
    // - 스킬        : E / Q 고정
    // - 패시브      : 레벨 칸 없음 (levelText가 비어 있음)
    // [변경] levelRoot를 끄고 켜지 않고, 글자만 바꾼다
    public class HudSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text levelText; // 레벨 칸이 없는 칸(패시브)은 비워둔다

        // 빈 칸 : 아이콘 끄고 글자 지우기 (레벨 칸 자체는 그대로 보인다)
        public void SetEmpty()
        {
            icon.enabled = false;
            cooldownText.SetText("");
            SetLevelText(""); // [변경] HideLevel() → 빈 글자
        }

        public void SetIcon(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        // 무기/능력치 증강 레벨
        public void SetLevel(int level)
        {
            SetLevelText(level.ToString());
        }

        // ===== 새로 추가 : 레벨 칸 글자 (스킬은 "E" / "Q") =====
        public void SetLevelText(string text)
        {
            if (levelText == null) return; // 패시브처럼 레벨 칸이 없는 칸
            levelText.SetText(text);
        }
        // ===== 새로 추가 끝 =====

        // 남은 쿨타임(초). 0이면 글자를 지운다
        public void SetCooldown(float remain)
        {
            if (remain <= 0f) cooldownText.SetText("");
            else cooldownText.SetText(remain.ToString("0.0"));
        }
    }
}
