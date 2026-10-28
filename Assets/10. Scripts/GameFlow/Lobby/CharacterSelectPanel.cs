using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 로비 캐릭터 선택 창 (내 플레이어 칸을 누르면 열린다)
    // 칸을 손으로 만들지 않고 유닛 테이블 데이터 수만큼 만든다
    public class CharacterSelectPanel : MonoBehaviour
    {
        [SerializeField] private CharacterSlotUI slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private TMP_Text infoText; // 고른 캐릭터 정보
        [SerializeField] private Button closeButton; // 새로 추가

        private List<CharacterSlotUI> slots = new List<CharacterSlotUI>();

        // ===== 새로 추가 : 닫기 버튼 =====
        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
        }
        // ===== 새로 추가 끝 =====

        public void Init()
        {
            if (slots.Count > 0) return; // 이미 만들었음

            foreach (UnitDef def in TableLoader.Instance.UnitTable.All)
            {
                CharacterSlotUI slot = Instantiate(slotPrefab, slotParent);
                slot.gameObject.SetActive(true); // 새로 추가 : 씬 안의 꺼진 견본을 복사한 경우를 위해
                slot.Bind(def, Select);
                slots.Add(slot);
            }

            Refresh();
        }

        // ===== 새로 추가 : 창 열기/닫기 =====
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

        // 칸을 클릭했을 때 (OnDataChanged로 로비의 플레이어 칸도 바뀐다)
        private void Select(UnitDef def)
        {
            GameSession.Instance.SelectUnit(def.ID);
            Refresh();
        }

        private void Refresh()
        {
            int selectedID = GameSession.Instance.Data.SelectedUnitID;

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetSelected(slots[i].Def.ID == selectedID);
            }

            UnitDef def = TableLoader.Instance.UnitTable.Get(selectedID);
            infoText.SetText($"{def.Name}\n" +
                             $"체력 {def.UnitStat.MaxHp} · 공격력 {def.UnitStat.Damage} · 방어력 {def.UnitStat.Defense}");
        }
    }
}
