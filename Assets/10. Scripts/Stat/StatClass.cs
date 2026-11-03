using UnityEngine;

namespace PROJECT3D
{
    public enum StatType
    {
        None,
        MaxHp, // 최대 체력
        HealthRegeneration, // 체력 재생
        Damage, // 공격력
        AttackSpeed, // 공격 속도 (초당 공격 횟수. 2.5 = 1초에 2.5번)
        Defense, // 방어력
        Speed, // 이동 속도
        CritChance, // 크리티컬 확률%
        ProjectileCount, // 투사체 증가
        AttackRange, // 공격 범위 증가% (0 = 배율 1. 모든 무기 / 스킬의 탐색 범위 + 투사체 크기 / 판정 반경에 곱해진다) // [변경] 주석
        Duration, // 지속시간 증가%
        AcquisitionRange, // 획득 범위 증가%
        ExperienceGain, // 경험치 획득량 증가%
        GoldGain, //골드 획득량 증가%
        Count, // 스탯 개수 (for문 끝 번호용)
    }

    [System.Serializable]
    public class StatClass
    {
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

        #region 외부 설정

        #region 값 접근

        // StatType 번호로 값 읽기 (for문용)
        public float Get(StatType type)
        {
            return type switch
            {
                StatType.MaxHp => MaxHp,
                StatType.HealthRegeneration => HealthRegeneration,
                StatType.Damage => Damage,
                StatType.AttackSpeed => AttackSpeed,
                StatType.Defense => Defense,
                StatType.Speed => Speed,
                StatType.CritChance => CritChance,
                StatType.ProjectileCount => ProjectileCount,
                StatType.AttackRange => AttackRange,
                StatType.Duration => Duration,
                StatType.AcquisitionRange => AcquisitionRange,
                StatType.ExperienceGain => ExperienceGain,
                StatType.GoldGain => GoldGain,
                _ => 0f
            };
        }

        // StatType 번호로 값 넣기
        public void Set(StatType type, float value)
        {
            switch (type)
            {
                case StatType.MaxHp: MaxHp = Mathf.RoundToInt(value); break;
                case StatType.HealthRegeneration: HealthRegeneration = value; break;
                case StatType.Damage: Damage = Mathf.RoundToInt(value); break;
                case StatType.AttackSpeed: AttackSpeed = value; break;
                case StatType.Defense: Defense = Mathf.RoundToInt(value); break;
                case StatType.Speed: Speed = value; break;
                case StatType.CritChance: CritChance = value; break;
                case StatType.ProjectileCount: ProjectileCount = Mathf.RoundToInt(value); break;
                case StatType.AttackRange: AttackRange = value; break;
                case StatType.Duration: Duration = value; break;
                case StatType.AcquisitionRange: AcquisitionRange = value; break;
                case StatType.ExperienceGain: ExperienceGain = value; break;
                case StatType.GoldGain: GoldGain = value; break;
            }
        }

        #endregion

        #region 통째로 계산

        public void Add(StatClass other)
        {
            for (int i = 1; i < (int)StatType.Count; i++)
            {
                StatType type = (StatType)i;
                Set(type, Get(type) + other.Get(type));
            }
        }

        // 다른 스탯의 값을 그대로 복사 (참조를 공유하지 않게)
        public void CopyFrom(StatClass other)
        {
            for (int i = 1; i < (int)StatType.Count; i++)
            {
                StatType type = (StatType)i;
                Set(type, other.Get(type));
            }
        }

        // 모두 0으로
        public void Clear()
        {
            for (int i = 1; i < (int)StatType.Count; i++)
            {
                Set((StatType)i, 0f);
            }
        }

        #endregion

        #endregion
    }
}