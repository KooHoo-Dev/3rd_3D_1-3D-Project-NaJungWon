using UnityEngine;

namespace PROJECT3D
{
    public enum StatType
    {
        None,
        MaxHp, // 최대 체력
        HealthRegeneration, // 체력 재생
        Damage, // 공격력
        AttackInterval, //공격 속도
        Defense, // 방어력
        Speed, // 이동 속도
        CritChance, // 크리티컬 확률%
        ProjectileCount, // 투사체 증가
        AttackRange, // 공격 범위 증가%
        Duration, // 지속시간 증가%
        AcquisitionRange, // 획득 범위 증가%
        ExperienceGain, // 경험치 획득량 증가%
        GoldGain, //골드 획득량 증가%
    }
    
    public class StatClass
    {
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
    }
}