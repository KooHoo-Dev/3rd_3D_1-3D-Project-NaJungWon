using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 증강 선택창의 카드 한 장
    // BS Project처럼 카드마다 오브젝트를 만들지 않고, 3장을 재사용하면서 Bind로 내용만 바꾼다
    public class AugmentCardUI : MonoBehaviour
    {
        [SerializeField] private GameObject[] cardCase; //AugmentType 순서. 0: 무기 1: 능력치 2: 특수
        [SerializeField] private Button button;
        [SerializeField] private Image cardIcon;
        [SerializeField] private Image arousalEffect; //진화시 카드장식
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text descriptionText;

        // 데이터를 받아 카드를 채운다. 클릭하면 onClicked로 자기 데이터를 넘겨준다
        public void Bind(AugmentOffer offer, Action<AugmentOffer> onClicked)
        {
            AugmentDef def = offer.Def;
            
            // 증강 타입에 맞는 카드 테두리만 켠다
            for (int i = 0; i < cardCase.Length; i++)
            {
                cardCase[i].SetActive(i == (int)def.Type);
            }

            cardIcon.sprite = def.Sprite;
            nameText.text = def.Name;

            if (offer.Kind == OfferKind.Arousal)
                descriptionText.text = def.ArousalDescription;
            else
                descriptionText.text = def.Description;

            levelText.text = GetLevelText(offer);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked(offer));
        }

        private string GetLevelText(AugmentOffer offer)
        {
            if (offer.Kind == OfferKind.New) return "NEW";
            if (offer.Kind == OfferKind.LevelUp) return $"Lv.{offer.Target.Level} → {offer.Target.Level + 1}";
            if (offer.Kind == OfferKind.Arousal) return "진화!";
            return "";
        }
    }
}