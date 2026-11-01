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
        [SerializeField] private Image[] cardIcon; //케이스와 동일
        [SerializeField] private Image arousalEffect; //진화시 카드장식
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject newText; // 새로 추가 : 처음 고르는 증강일 때만 켜는 "New!" 표시
        [SerializeField] private Button rerollButton; // 새로 추가 : 카드 아래 리롤 버튼 (테스트용. 횟수 제한 없음)

        // 새로 추가 : 리롤 버튼을 눌렀을 때 할 일 (선택창이 이 카드 번호로 넣어준다)
        public void SetReroll(Action onReroll)
        {
            if (rerollButton == null) return;
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => onReroll());
        }

        // 데이터를 받아 카드를 채운다. 클릭하면 onClicked로 자기 데이터를 넘겨준다
        public void Bind(AugmentOffer offer, Action<AugmentOffer> onClicked)
        {
            AugmentDef def = offer.Def;
            
            // 증강 타입에 맞는 카드 테두리만 켠다
            for (int i = 0; i < cardCase.Length; i++)
            {
                cardCase[i].SetActive(i == (int)def.Type);
                cardIcon[i].sprite = def.Sprite;
            }
            
            nameText.text = def.Name;

            if (offer.Kind == OfferKind.Arousal)
                descriptionText.text = def.ArousalDescription;
            else if (offer.Kind == OfferKind.LevelUp)
                descriptionText.text = GetLevelUpStatText(offer.Target); // [변경] 레벨업은 설명 없이 바뀌는 수치만
            else
                descriptionText.text = def.Description;

            levelText.text = GetLevelText(offer);
            if (newText != null) newText.SetActive(offer.Kind == OfferKind.New); // 새로 추가 : 카드 3장을 재사용하니 매번 켜고 끈다

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked(offer));
        }

        private string GetLevelText(AugmentOffer offer)
        {
            if (offer.Kind == OfferKind.New) return ""; // [변경] "NEW" 글자 대신 newText 오브젝트를 켠다
            if (offer.Kind == OfferKind.LevelUp) return $"Lv.{offer.Target.Level} → {offer.Target.Level + 1}";
            if (offer.Kind == OfferKind.Arousal) return "진화!";
            return "";
        }

        // ===== 새로 추가 : 레벨업 카드의 수치 변화 (예 : 공격력 10 → 15) =====
        // 지금 레벨과 다음 레벨의 레벨 데이터(augmentLevels.tsv)를 비교해서, 값이 바뀌는 스탯만 한 줄씩
        // 스탯 이름 / 단위는 스탯 창(PlayerStatPanel)과 같은 것을 쓴다
        private string GetLevelUpStatText(OwnedAugment target)
        {
            if (target == null || target.Level >= target.MaxLevel) return "";

            AugmentLevelTable levelTable = TableLoader.Instance.AugmentLevelTable;
            StatClass now = levelTable.Get(target.Def.ID, target.Level).AugmentLevelStat;
            StatClass next = levelTable.Get(target.Def.ID, target.Level + 1).AugmentLevelStat;

            string result = "";
            for (int i = 1; i < (int)StatType.Count; i++) // 0번 None은 건너뛴다
            {
                StatType type = (StatType)i;
                float nowValue = now.Get(type);
                float nextValue = next.Get(type);
                if (Mathf.Approximately(nowValue, nextValue)) continue; // 안 바뀌는 스탯은 안 보여준다

                if (result != "") result += "\n"; // [변경] 설명 없이 수치만 쓰니까 첫 줄은 줄바꿈 없이
                result += $"{PlayerStatPanel.GetStatName(type)} {PlayerStatPanel.GetStatValueText(type, nowValue)} → {PlayerStatPanel.GetStatValueText(type, nextValue)}";
            }
            return result;
        }
        // ===== 새로 추가 끝 =====
    }
}