using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 상점 칸 하나 : 스탯 한 종류의 [이름 / 단계 / 수치 / 비용 / 구매]
    public class ShopSlotUI : MonoBehaviour
    {
        [SerializeField] private Button buyButton;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText; // 2 / 5
        [SerializeField] private TMP_Text valueText; // 10 → 20
        [SerializeField] private TMP_Text costText;  // 200 or MAX

        public StatType Type { get; private set; }

        #region 외부 설정

        public void Bind(StatType type, Action<StatType> onClicked)
        {
            Type = type;

            // 이름과 아이콘은 1단계 데이터에서 가져온다
            ShopDef first = TableLoader.Instance.ShopTable.Get(type, 1);
            nameText.SetText(first.Name);
            icon.sprite = first.Sprite;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onClicked(type));
        }

        // 현재 구매 단계와 보유 골드를 보고 다시 그린다
        public void Refresh()
        {
            ShopTable shopTable = TableLoader.Instance.ShopTable;
            PlayerData data = GameSession.Instance.Data;

            int level = data.GetShopLevel(Type);
            int maxLevel = shopTable.GetMaxLevel(Type);

            float currentValue = 0f;
            if (level > 0) currentValue = shopTable.Get(Type, level).Value;

            levelText.SetText($"{level} / {maxLevel}");

            // 최대 단계
            if (level >= maxLevel)
            {
                valueText.SetText(currentValue.ToString());
                costText.SetText("MAX");
                buyButton.interactable = false;
                return;
            }

            ShopDef next = shopTable.Get(Type, level + 1);
            valueText.SetText($"{currentValue} → {next.Value}");
            costText.SetText(next.Cost.ToString());
            buyButton.interactable = data.Gold >= next.Cost; // 골드가 모자라면 못 누른다
        }

        #endregion
    }
}