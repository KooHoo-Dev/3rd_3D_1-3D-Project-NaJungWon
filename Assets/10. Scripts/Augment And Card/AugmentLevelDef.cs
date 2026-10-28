using UnityEngine;

namespace PROJECT3D
{
    public class AugmentLevelDto
    {
        public int ID;
        public int Level;
        public int MaxHp;
        public float HealthRegeneration;
        public int Damage;
        public float AttackSpeed;
        public int Defense;
        public float Speed;
        public float CritChance;
        public int ProjectileCount;
        public float AttackRange;
        public float Duration;
        public float AcquisitionRange;
        public float ExperienceGain;
        public float GoldGain;
    }

    [System.Serializable]
    public class AugmentLevelDef
    {
        public int ID;
        public int Level;
        public StatClass AugmentLevelStat;
    }
}