using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 로비 방의 플레이어 칸 하나 (최대 4칸)
    // - 플레이어가 없으면 빈 칸(+)
    // - 플레이어가 있으면 고른 캐릭터 그림과 이름
    public class PartySlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject emptyRoot;      // 빈 칸일 때 (+)
        [SerializeField] private GameObject characterRoot;  // 플레이어가 있을 때
        [SerializeField] private Image characterIcon;
        [SerializeField] private TMP_Text unitNameText;     // 캐릭터 이름
        [SerializeField] private TMP_Text playerNameText;   // 플레이어 이름

        // 칸 번호와 클릭했을 때 할 일을 받는다
        public void Init(int index, Action<int> onClicked)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked(index));
        }

        // slot이 null이면 빈 칸
        public void Refresh(PlayerSlot slot)
        {
            if (slot == null)
            {
                emptyRoot.SetActive(true);
                characterRoot.SetActive(false);
                return;
            }

            emptyRoot.SetActive(false);
            characterRoot.SetActive(true);

            UnitDef def = TableLoader.Instance.UnitTable.Get(slot.UnitID);
            characterIcon.sprite = def.Sprite;
            unitNameText.SetText(def.Name);
            playerNameText.SetText(slot.Name);
        }
    }
}
