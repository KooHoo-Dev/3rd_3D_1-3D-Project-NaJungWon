using UnityEngine;

namespace PROJECT3D
{
    public class UnitDto
    {
        public int ID;
        public string Name;
        public string PrefabKey;
        public string SpriteKey;
        public string PassiveSkillID;
        public string ESkillID;
        public string QSkillID;
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
    
    [System.Serializable]
    public class UnitDef
    {
        public int ID;
        public string Name;
        public Unit Unit; //몸 데이터. 유닛이아니라 GameObject나 meshRenderer로바꿔야할듯?
        public UnitSkill PassiveSkill;
        public UnitSkill ESkill;
        public UnitSkill QSkill;
        public Sprite Sprite; //캐릭터 아이콘
        public StatClass UnitStat;
    }
}