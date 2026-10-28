using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 로비 능력치 업그레이드(상점) 창. 상점 테이블의 스탯 종류만큼 칸을 만든다
    // 골드나 구매 단계가 바뀌면 (OnDataChanged) 모든 칸을 다시 그린다
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private ShopSlotUI slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private Button closeButton; // 새로 추가

        private List<ShopSlotUI> slots = new List<ShopSlotUI>();

        // ===== 새로 추가 : 닫기 버튼 =====
        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
        }
        // ===== 새로 추가 끝 =====

        private void OnEnable()
        {
            GameSession.Instance.OnDataChanged += Refresh;
        }

        private void OnDisable()
        {
            if (GameSession.IsInitialized)
                GameSession.Instance.OnDataChanged -= Refresh;
        }

        public void Init()
        {
            if (slots.Count > 0) return; // 이미 만들었음

            List<StatType> statOrder = TableLoader.Instance.ShopTable.StatOrder;
            for (int i = 0; i < statOrder.Count; i++)
            {
                ShopSlotUI slot = Instantiate(slotPrefab, slotParent);
                slot.gameObject.SetActive(true); // 새로 추가 : 씬 안의 꺼진 견본을 복사한 경우를 위해
                slot.Bind(statOrder[i], Buy);
                slots.Add(slot);
            }

            Refresh();
        }

        // ===== 새로 추가 : 창 열기/닫기 =====
        // 열 때마다 다시 그린다 (닫혀 있는 동안 골드가 바뀌었을 수 있음)
        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
        // ===== 새로 추가 끝 =====

        // 구매 버튼을 눌렀을 때 (성공하면 OnDataChanged로 Refresh가 불린다)
        private void Buy(StatType type)
        {
            GameSession.Instance.TryBuyShop(type);
        }

        private void Refresh()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Refresh();
            }
        }
    }
}
