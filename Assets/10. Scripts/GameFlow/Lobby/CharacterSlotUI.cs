using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 캐릭터 선택 칸 하나 (Mini Defence BuildMenuSlot 방식)
    // 무엇을 보여줄지는 데이터(UnitDef)가 정하고, 이 스크립트는 그리기만 한다
    public class CharacterSlotUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectedMark; // 선택했을 때 켜지는 표시

        public UnitDef Def { get; private set; }

        #region 외부 설정

        public void Bind(UnitDef def, Action<UnitDef> onClicked)
        {
            Def = def;

            icon.sprite = def.Sprite;
            nameText.SetText(def.Name);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked(def));

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            selectedMark.SetActive(selected);
        }

        #endregion
    }
}