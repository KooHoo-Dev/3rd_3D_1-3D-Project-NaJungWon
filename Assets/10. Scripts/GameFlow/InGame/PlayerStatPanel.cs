using TMPro;
using UnityEngine;

namespace PROJECT3D
{
    // 증강 선택창 우측의 플레이어 스탯 창 (메가봉크 방식)
    // 줄 번호 i = StatType (i + 1). StatType.None(0)은 건너뛴다
    public class PlayerStatPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] nameTexts;
        [SerializeField] private TMP_Text[] valueTexts;

        private Player player;

        #region 외부 설정

        #region 스탯 창

        public void Init(Player player)
        {
            this.player = player;

            for (int i = 0; i < nameTexts.Length; i++)
            {
                StatType type = (StatType)(i + 1);
                bool hasStat = type < StatType.Count;

                // 줄이 스탯 수보다 많으면 남는 줄은 끈다
                nameTexts[i].transform.parent.gameObject.SetActive(hasStat);
                if (hasStat) nameTexts[i].SetText(GetStatName(type));
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (player == null) return;

            StatClass stat = player.Stat.unitStat;
            for (int i = 0; i < valueTexts.Length; i++)
            {
                StatType type = (StatType)(i + 1);
                if (type >= StatType.Count) break;

                valueTexts[i].SetText(GetStatValueText(type, stat.Get(type)));
            }
        }

        #endregion

        #region 스탯 글자

        public static string GetStatName(StatType type) // [변경] private → public static (증강 카드 레벨업 수치 표시에서도 사용)
        {
            switch (type)
            {
                case StatType.MaxHp: return "최대 체력";
                case StatType.HealthRegeneration: return "체력 재생";
                case StatType.Damage: return "공격력";
                case StatType.AttackSpeed: return "공격 속도";
                case StatType.Defense: return "방어력";
                case StatType.Speed: return "이동 속도";
                case StatType.CritChance: return "치명타 확률";
                case StatType.ProjectileCount: return "투사체 수";
                case StatType.AttackRange: return "공격 범위";
                case StatType.Duration: return "지속 시간";
                case StatType.AcquisitionRange: return "획득 범위";
                case StatType.ExperienceGain: return "경험치 획득";
                case StatType.GoldGain: return "골드 획득";
                default: return "";
            }
        }

        // 스탯 단위에 맞게 글자로
        public static string GetStatValueText(StatType type, float value) // [변경] private → public static (증강 카드에서도 사용)
        {
            switch (type)
            {
                case StatType.MaxHp:
                case StatType.Damage:
                case StatType.Defense:
                case StatType.ProjectileCount:
                    return value.ToString("0");
                case StatType.HealthRegeneration:
                    return $"{value:0.0}/s";
                case StatType.AttackSpeed:
                    return $"{value:0.00}x";   // 공속 배율
                case StatType.Speed:
                    return value.ToString("0.0");
                case StatType.CritChance:
                    return $"{value:0}%";
                default:
                    return $"+{value:0}%";     // 범위, 지속, 획득 범위, 경험치, 골드
            }
        }

        #endregion

        #endregion
    }
}
