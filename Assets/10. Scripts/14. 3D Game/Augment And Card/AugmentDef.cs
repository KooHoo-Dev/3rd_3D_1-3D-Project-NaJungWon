using UnityEngine;

namespace PROJECT3D
{
    public enum AugmentType //무기증강인지 능력치증강인지
    {
        Weapon,
        Ability,
        Special
    }
    
    public class AugmentDto
    {
        public int ID;
        public string Name;
        public string PrefabKey;
        public string SpriteKey;
        public AugmentType Type; //증강타입
        public StatType NeedArousal; //진화시 필요 스탯
        public int MaxHealth;
        public float HealthRegeneration;
        public int Attack;
        public float AttackInterval;
        public int Defense;
        public float Speed;
        public float Critical;
        public int ProjectileCount;
        public float AttackRange;
        public float Duration;
        public float AcquisitionRange;
        public float ExperienceGain;
        public float GoldGain;
        public string Description; //증강 설명
    }
    
    [System.Serializable]
    public class AugmentDef
    {
        public int ID;
        public string Name;
        public AugmentCard AugmentCard;
        public Sprite Sprite;
        public AugmentType Type; //증강타입
        public StatType NeedArousal; //진화시 필요 스탯
        public StatClass AugmentStat;
        public string Description; //증강 설명
    }
}