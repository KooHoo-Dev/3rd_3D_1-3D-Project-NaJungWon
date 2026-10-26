using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 증강 선택창 (레벨업, 상자 공용)
    // 카드는 미리 3장 배치해두고, 열 때마다 Bind로 내용만 바꿔서 재사용
    public class AugmentSelectPanel : MonoBehaviour
    {
        [SerializeField] private AugmentCardUI[] cards;

        // 레벨업했을 때
        [ContextMenu("Open LevelUp")]
        public void OpenLevelUp()
        {
            List<AugmentOffer> offers = AugmentManager.Instance.BuildLevelUpOffers(cards.Length);
            Show(offers);
        }

        // 상자를 먹었을 때
        [ContextMenu("Open Chest")]
        public void OpenChest()
        {
            List<AugmentOffer> offers = AugmentManager.Instance.BuildChestOffers(cards.Length);
            Show(offers);
        }

        private void Show(List<AugmentOffer> offers)
        {
            if (offers.Count == 0) return; // 보여줄 카드가 없음 (나중에 삭제할것. 테스트용)

            for (int i = 0; i < cards.Length; i++)
            {
                // 카드 수가 적으면 남는 칸은 끈다
                if (i < offers.Count)
                {
                    cards[i].gameObject.SetActive(true);
                    cards[i].Bind(offers[i], Select);
                }
                else
                {
                    cards[i].gameObject.SetActive(false);
                }
            }

            gameObject.SetActive(true);
            WorldClock.Instance.Pause(); // 고르는 동안 게임 시간 정지
        }

        public void Close()
        {
            gameObject.SetActive(false);
            WorldClock.Instance.Resume();
        }

        // 카드를 클릭했을 때
        private void Select(AugmentOffer offer)
        {
            AugmentManager.Instance.Apply(offer);
            Close();
        }
    }
}