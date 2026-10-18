using UnityEngine;

namespace PROJECT3D
{
    public enum AugmentType //무기증강인지 능력치증강인지
    {
        Weapon,
        Ability,
        Special
    }
    
    public enum StatType
    {
        None,
        MaxHealth, // 최대 체력
        HealthRegeneration, // 체력 재생
        Attack, // 공격력
        AttackInterval, //공격 속도
        Defense, // 방어력
        Speed, // 이동 속도
        Critical, // 크리티컬 확률%
        ProjectileCount, // 투사체 증가
        AttackRange, // 공격 범위 증가%
        Duration, // 지속시간 증가%
        AcquisitionRange, // 획득 범위 증가%
        ExperienceGain, // 경험치 획득량 증가%
        GoldGain, //골드 획득량 증가%
    }
    
    public class StatClass
    {
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