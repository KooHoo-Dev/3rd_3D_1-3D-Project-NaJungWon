using System;
using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace PROJECT3D
{
    public enum OfferKind
    {
        New,     // 새 증강 획득
        LevelUp, // 보유 증강 레벨업
        Arousal, // 보유 무기 진화
        Special, // 특수카드 (회복, 골드)
    }

    // 카드 한 장에 담길 정보
    public class AugmentOffer
    {
        public OfferKind Kind;
        public AugmentDef Def;
        public OwnedAugment Target; // LevelUp, Arousal일 때 대상이 되는 보유 증강
    }
    
    // 증강 규칙 담당
    // 1. 어떤 카드를 보여줄지 정한다 (BuildOffers)
    // 2. 고른 카드를 적용한다 (Apply)
    // 3. 보유 중인 증강 목록을 관리한다
    public class AugmentManager : SingletonBase<AugmentManager>
    {
        [SerializeField] private Player player;
        [SerializeField] private Transform weaponRoot; // 무기증강이 생성될 플레이어 자식 오브젝트
        
        [Header("슬롯 설정")]
        [SerializeField] private int maxWeaponCount = 5;
        [SerializeField] private int maxAbilityCount = 6;
        [SerializeField] private int specialCardCount = 2; // 전부 고른 뒤 나오는 특수카드 수

        private List<OwnedAugment> ownedAugments = new List<OwnedAugment>();
        public List<OwnedAugment> OwnedAugments => ownedAugments;
        
        // 증강을 얻거나 레벨업, 진화했을 때 알려주는 이벤트 (슬롯 UI 갱신용)
        public event Action OnAugmentChanged;

        public void Init(Player player, Transform root)
        {
            this.player = player;
            weaponRoot = root;
            GiveStartWeapon(player.unitDef.StartWeaponID); // 새로 추가 : 캐릭터 기본 무기를 들고 시작
        }

        // 새로 추가 : 시작 무기증강 지급. 카드로 새 증강을 고른 것과 같은 흐름(Apply)을 그대로 쓴다
        // (무기 생성 → 1레벨 수치 → 능력치 갱신 → 슬롯 UI 갱신 이벤트)
        private void GiveStartWeapon(int augmentID)
        {
            if (augmentID < 0) return; // -1 = 시작 무기 없음

            AugmentDef def = TableLoader.Instance.AugmentTable.GetOrNull(augmentID);
            if (def == null)
            {
                Debug.LogWarning($"[AugmentManager] 시작 무기 {augmentID}번이 증강 표에 없습니다");
                return;
            }

            AugmentOffer offer = new AugmentOffer();
            offer.Kind = OfferKind.New;
            offer.Def = def;
            Apply(offer);
        }
        
        #region 카드 만들기

        // 레벨업했을 때 보여줄 카드 (새 증강 + 레벨업. 진화는 상자에서만)
        public List<AugmentOffer> BuildLevelUpOffers(int cardCount)
        {
            List<AugmentOffer> candidates = new List<AugmentOffer>();
            
            AddLevelUpOffers(candidates);
            AddNewOffers(candidates);

            // 무기, 능력치 증강을 전부 고르고 만렙까지 찍었으면 특수카드만
            if (candidates.Count == 0)
            {
                AddSpecialOffers(candidates);
                cardCount = specialCardCount;
            }

            // 후보가 cardCount보다 적으면 후보 수만큼만 (3 -> 2 -> 1)
            return PickRandom(candidates, cardCount);
        }

        // 상자를 먹었을 때 보여줄 카드
        // 진화 조건을 만족한 무기가 있으면 진화 카드, 없으면 레벨업과 동일
        public List<AugmentOffer> BuildChestOffers(int cardCount)
        {
            List<AugmentOffer> candidates = new List<AugmentOffer>();
            
            for (int i = 0; i < ownedAugments.Count; i++)
            {
                OwnedAugment owned = ownedAugments[i];
                if (CanArousal(owned) == false) continue;

                AugmentOffer offer = new AugmentOffer();
                offer.Kind = OfferKind.Arousal;
                offer.Def = owned.Def;
                offer.Target = owned;
                candidates.Add(offer);
            }

            // 진화할 수 있는 무기가 없으면 기본 레벨업 카드로
            if (candidates.Count == 0)
                return BuildLevelUpOffers(cardCount);

            return PickRandom(candidates, cardCount);
        }
        
        // 아직 없는 증강 -> 새 증강 카드
        private void AddNewOffers(List<AugmentOffer> candidates)
        {
            foreach (AugmentDef def in TableLoader.Instance.AugmentTable.All)
            {
                if (def.Type == AugmentType.Special) continue; // 특수카드는 따로
                if (HasAugment(def.ID)) continue;               // 이미 있음
                
                // 다른 캐릭터 전용 무기는 나오지 않는다 (-1 = 공용)
                if (def.OwnerUnitID != -1 && def.OwnerUnitID != player.unitDef.ID) continue;

                // 슬롯이 꽉 찼으면 제외
                if (def.Type == AugmentType.Weapon && CountAugment(AugmentType.Weapon) >= maxWeaponCount) continue;
                if (def.Type == AugmentType.Ability && CountAugment(AugmentType.Ability) >= maxAbilityCount) continue;

                AugmentOffer offer = new AugmentOffer();
                offer.Kind = OfferKind.New;
                offer.Def = def;
                candidates.Add(offer);
            }
        }
        
        // 보유 중인 증강 중 만렙이 아닌 것 -> 레벨업 카드
        private void AddLevelUpOffers(List<AugmentOffer> candidates)
        {
            for (int i = 0; i < ownedAugments.Count; i++)
            {
                OwnedAugment owned = ownedAugments[i];
                if (owned.IsMaxLevel()) continue;
                
                AugmentOffer offer = new AugmentOffer();
                offer.Kind = OfferKind.LevelUp;
                offer.Def = owned.Def;
                offer.Target = owned;
                candidates.Add(offer);
            }
        }

        // 특수카드 (회복, 골드)
        private void AddSpecialOffers(List<AugmentOffer> candidates)
        {
            foreach (AugmentDef def in TableLoader.Instance.AugmentTable.All)
            {
                if (def.Type != AugmentType.Special) continue;
                
                AugmentOffer offer = new AugmentOffer();
                offer.Kind = OfferKind.Special;
                offer.Def = def;
                candidates.Add(offer);
            }
        }

        // 진화 조건
        // 무기증강 + 아직 진화 안 함 + 만렙 + 필요 스탯의 능력치증강 보유(1레벨 이상)
        private bool CanArousal(OwnedAugment owned)
        {
            if (owned.Def.Type != AugmentType.Weapon) return false;
            if (owned.IsArousal) return false;
            if (owned.IsMaxLevel() == false) return false;
            if (owned.Def.NeedArousal == StatType.None) return false; // 진화 없는 무기(아직 미개발의 경우(테스트용))

            for (int i = 0; i < ownedAugments.Count; i++)
            {
                AugmentDef ownedDef = ownedAugments[i].Def;
                if (ownedDef.Type == AugmentType.Ability && ownedDef.MainStat == owned.Def.NeedArousal)
                    return true;
            }
            return false;
        }

        // 후보 중에서 겹치지 않게 count장 뽑기. 후보가 적으면 후보 수만큼만
        private List<AugmentOffer> PickRandom(List<AugmentOffer> candidates, int count)
        {
            List<AugmentOffer> result = new List<AugmentOffer>();
            
            while (result.Count < count && candidates.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                result.Add(candidates[index]);
                candidates.RemoveAt(index); // 뽑은 카드는 빼서 중복 방지
            }
            return result;
        }

        #endregion

        #region 카드 적용

        // 카드를 골랐을 때 호출
        public void Apply(AugmentOffer offer)
        {
            switch (offer.Kind)
            {
                case OfferKind.New:
                    AddAugment(offer.Def);
                    break;
                
                case OfferKind.LevelUp:
                    offer.Target.LevelUp();
                    break;
                
                case OfferKind.Arousal:
                    offer.Target.Arousal();
                    break;
                
                case OfferKind.Special:
                    ApplySpecial(offer.Def);
                    break;
            }

            RefreshAbilityStat();
            OnAugmentChanged?.Invoke();
        }

        private void AddAugment(AugmentDef def)
        {
            // 무기증강만 오브젝트 생성. 능력치증강은 데이터만 보관
            WeaponAugment weapon = null;
            if (def.Type == AugmentType.Weapon)
            {
                if (def.Prefab == null)
                {
                    Debug.LogWarning($"[AugmentManager] {def.Name} 프리팹이 없습니다");
                    return;
                }
                weapon = Instantiate(def.Prefab, weaponRoot);
            }

            OwnedAugment owned = new OwnedAugment(def, weapon);
            ownedAugments.Add(owned);
        }

        // 특수카드는 바로 효과만
        private void ApplySpecial(AugmentDef def)
        {
            switch (def.Special)
            {
                case SpecialType.Heal:
                    // Value = 최대 체력의 몇 % 회복할지
                    int healAmount = (int)(player.Stat.unitStat.MaxHp * def.Value / 100f);
                    player.Stat.ApplyHeal(healAmount);
                    break;
                
                case SpecialType.Gold:
                    player.AddGold((int)def.Value); // 골드 획득 (골드 획득량 % 적용)
                    Debug.Log($"[AugmentManager] 골드 {def.Value} 획득");
                    break;
            }
        }

        #endregion

        // 능력치증강들의 스탯을 처음부터 다시 더한다
        private void RefreshAbilityStat()
        {
            StatClass total = new StatClass();
            
            for (int i = 0; i < ownedAugments.Count; i++)
            {
                OwnedAugment owned = ownedAugments[i];
                if (owned.Def.Type != AugmentType.Ability) continue;
                
                total.Add(owned.GetLevelData().AugmentLevelStat);
            }

            player.StatHandler.SetAbilityStat(total);
        }

        private bool HasAugment(int id)
        {
            for (int i = 0; i < ownedAugments.Count; i++)
            {
                if (ownedAugments[i].Def.ID == id) return true;
            }
            return false;
        }

        private int CountAugment(AugmentType type)
        {
            int count = 0;
            for (int i = 0; i < ownedAugments.Count; i++)
            {
                if (ownedAugments[i].Def.Type == type) count++;
            }
            return count;
        }
    }
}