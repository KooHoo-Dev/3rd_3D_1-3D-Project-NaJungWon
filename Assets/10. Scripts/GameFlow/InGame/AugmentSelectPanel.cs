using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 증강 선택창 (레벨업, 상자 공용)
    // 카드는 미리 3장 배치해두고, 열 때마다 Bind로 내용만 바꿔서 재사용
    public class AugmentSelectPanel : MonoBehaviour
    {
        [SerializeField] private AugmentCardUI[] cards;

        // ===== 새로 추가 : 선택창과 같이 열리는 창 =====
        [Header("선택창과 같이 열리는 창 (메가봉크 방식)")]
        [SerializeField] private ArousalListPanel arousalPanel; // 좌측 : 진화 가능 무기 목록
        [SerializeField] private PlayerStatPanel statPanel;     // 우측 : 플레이어 스탯
        // ===== 새로 추가 끝 =====

        // ===== 새로 추가 : 열림 상태 (레벨업 선택창 안 뜨는 버그 수정) =====
        // 원인 : 씬에 SelectCardSystem이 켜진 채로 저장돼 있어서, ExpManager가 "이미 열려 있음(activeSelf)"으로 보고 영영 안 열었다
        // 수정 : 오브젝트 켜짐 여부 대신 이 값으로 열림을 판단하고, 씬에서 켜져 있어도 시작할 때 스스로 끈다
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            // Show에서 처음 켜질 때도 Awake가 불리므로, 열리는 중(IsOpen)이면 끄지 않는다
            if (IsOpen == false) gameObject.SetActive(false);
        }
        // ===== 새로 추가 끝 =====

        // 레벨업했을 때
        [ContextMenu("Open LevelUp")]
        public void OpenLevelUp()
        {
            isChest = false; // 새로 추가 : 리롤 때 같은 규칙으로 다시 뽑으려고 기억
            List<AugmentOffer> offers = AugmentManager.Instance.BuildLevelUpOffers(cards.Length);
            Show(offers);
        }

        // 상자를 먹었을 때
        [ContextMenu("Open Chest")]
        public void OpenChest()
        {
            isChest = true; // 새로 추가
            List<AugmentOffer> offers = AugmentManager.Instance.BuildChestOffers(cards.Length);
            Show(offers);
        }

        private void Show(List<AugmentOffer> offers)
        {
            if (offers.Count == 0) return; // 보여줄 카드가 없음 (나중에 삭제할것. 테스트용)

            shownOffers = offers; // 새로 추가 : 리롤 때 지금 보이는 카드와 겹치지 않게

            for (int i = 0; i < cards.Length; i++)
            {
                // 카드 수가 적으면 남는 칸은 끈다
                if (i < offers.Count)
                {
                    cards[i].gameObject.SetActive(true);
                    cards[i].Bind(offers[i], Select);

                    int cardIndex = i; // 새로 추가 : 람다가 i를 그대로 잡으면 마지막 값이 되므로 복사
                    cards[i].SetReroll(() => Reroll(cardIndex));
                }
                else
                {
                    cards[i].gameObject.SetActive(false);
                }
            }

            IsOpen = true; // 새로 추가 : SetActive(true)보다 먼저 (첫 Awake에서 꺼지지 않게)
            gameObject.SetActive(true);
            arousalPanel.Open(); // 새로 추가 : 좌측 진화 목록
            statPanel.Open();    // 새로 추가 : 우측 플레이어 스탯
            WorldClock.Instance.Pause(); // 고르는 동안 게임 시간 정지
        }

        public void Close()
        {
            IsOpen = false; // 새로 추가
            gameObject.SetActive(false);
            arousalPanel.Close(); // 새로 추가
            statPanel.Close();    // 새로 추가
            WorldClock.Instance.Resume();
        }

        // ===== 새로 추가 : 카드 리롤 (테스트용. 횟수 제한 없음) =====
        private bool isChest;                    // 지금 창이 상자로 열렸는지 (레벨업 / 상자 규칙 구분)
        private List<AugmentOffer> shownOffers;  // 지금 보이는 카드들

        // cardIndex번 카드만 다른 카드로 바꾼다
        //  - 열 때와 같은 규칙(BuildLevelUpOffers / BuildChestOffers)으로 후보 전체를 섞어서 받고
        //  - 지금 보이는 카드(바꿀 카드 포함)와 같은 것은 건너뛰고 첫 번째 것을 쓴다
        //  - 바꿀 후보가 없으면 그대로 둔다
        private void Reroll(int cardIndex)
        {
            List<AugmentOffer> candidates;
            if (isChest) candidates = AugmentManager.Instance.BuildChestOffers(int.MaxValue);
            else candidates = AugmentManager.Instance.BuildLevelUpOffers(int.MaxValue);

            for (int i = 0; i < candidates.Count; i++)
            {
                if (IsShown(candidates[i])) continue;

                shownOffers[cardIndex] = candidates[i];
                cards[cardIndex].Bind(candidates[i], Select);
                return;
            }

            Debug.Log("[AugmentSelectPanel] 리롤할 다른 카드가 없습니다");
        }

        // 같은 증강의 같은 종류(새로 / 레벨업 / 진화 / 특수) 카드가 이미 보이는지
        private bool IsShown(AugmentOffer offer)
        {
            for (int i = 0; i < shownOffers.Count; i++)
            {
                if (shownOffers[i].Def.ID == offer.Def.ID && shownOffers[i].Kind == offer.Kind) return true;
            }
            return false;
        }
        // ===== 새로 추가 끝 =====

        // 카드를 클릭했을 때
        private void Select(AugmentOffer offer)
        {
            AugmentManager.Instance.Apply(offer);
            Close();
        }
    }
}