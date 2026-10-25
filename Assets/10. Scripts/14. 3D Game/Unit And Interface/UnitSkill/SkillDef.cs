using UnityEngine;

namespace PROJECT3D
{
    public enum SkillType
    {
        Passive,
        Active,
    }
    
    public class SkillDto
    {
        public int ID;
        public string Name;
        public string IconKey;
        public string PrefabKey;
        public int MaxHp;
        public float HealthRegeneration;
        public int Damage;
        public float AttackInterval;
        public int Defense;
        public float Speed;
        public float CritChance;
        public int ProjectileCount;
        public float AttackRange;
        public float Duration;
        public float AcquisitionRange;
        public float ExperienceGain;
        public float GoldGain;
        public string Description;
    }
    
    [System.Serializable]
    public class SkillDef
    {
        public int ID;
        public string Name;
        public Sprite Icon;
        public UnitSkill UnitSkill;
        public StatClass SkillStat;
        public string Description;
    }
}