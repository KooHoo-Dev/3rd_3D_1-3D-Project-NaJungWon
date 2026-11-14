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
        public float Cooldown; // 쿨타임 (스킬용)
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
        public PassiveSkill Passive; // 패시브 스킬 프리팹 (E / Q 스킬이면 null)
        public UnitSkill UnitSkill;
        public StatClass SkillStat;
        public float Cooldown; // 쿨타임(초). 공속과 따로
        public string Description;
    }
}