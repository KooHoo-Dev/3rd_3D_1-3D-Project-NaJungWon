using UnityEngine;

namespace PROJECT3D
{
    public enum AugmentType //무기증강/능력치증강/특수카드
    {
        Weapon,
        Ability,
        Special
    }

    public enum SpecialType //특수카드 효과 (회복/골드획득)
    {
        None,
        Heal,
        Gold
    }
    
    public class AugmentDto
    {
        public int ID;
        public string Name;
        public string PrefabKey; //무기증강만 사용
        public string SpriteKey;
        public AugmentType Type; //증강타입
        public int OwnerUnitID; //전용 캐릭터 ID. -1이면 공용
        public StatType NeedArousal; //무기증강 : 진화에 필요한 능력치증강 스탯. None이면 진화 없음
        public StatType MainStat; //능력치증강 : 올려줄 스탯
        public SpecialType Special; //특수카드
        public float Value; //특수카드 : 수치 (회복 %, 골드량)
        public string Description; //증강 설명
        public string ArousalDescription; //진화 카드 설명
    }
    
    [System.Serializable]
    public class AugmentDef
    {
        public int ID;
        public string Name;
        public WeaponAugment Prefab; //무기증강만 사용
        public Sprite Sprite;
        public AugmentType Type;
        public int OwnerUnitID;
        public StatType MainStat;
        public StatType NeedArousal;
        public SpecialType Special;
        public float Value;
        public string Description;
        public string ArousalDescription;
    }
}