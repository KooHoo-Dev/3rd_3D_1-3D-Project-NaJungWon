using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 버프/디버프 하나. 값은 % (예 : 공격력 20 = 공격력 +20%, 디버프는 음수로 -20)
    public class StatBuff
    {
        public StatClass Percent;  // 스탯별 증감 %
        public float RemainTime;   // 남은 시간 (0 이하가 되면 사라짐)

        public StatBuff(StatClass percent, float duration)
        {
            Percent = percent;
            RemainTime = duration;
        }
    }

    // 플레이어 스탯 총합 계산기 (플레이어 프리팹에 붙인다. 플레이어마다 하나)
    // 최종 = (캐릭터 + 능력증강 + 스킬) × (1 + 상점%) × (1 + 버프%)
    // - 재료 스탯이 바뀔 때마다 Recalculate()로 처음부터 다시 계산한다 (빼기를 하지 않는다)
    // - 결과는 player.Stat.unitStat에 "값만" 덮어쓴다 (다른 스크립트가 들고 있는 참조가 그대로 유지됨)
    public class PlayerStatHandler : MonoBehaviour
    {
        public StatClass ChrStat = new StatClass();     // 캐릭터 기본 스탯 (테이블 값 복사본)
        public StatClass AbilityStat = new StatClass(); // 능력치 증강 스탯 (합)
        public StatClass SkillStat = new StatClass();   // 스킬/패시브로 인한 상승치 (합)
        public StatClass ShopStat = new StatClass();    // 상점 구매 스탯 (%)
        public StatClass BuffStat = new StatClass();    // 지금 걸린 버프/디버프 % 합 (확인용)

        private Player player;
        private List<StatBuff> buffs = new List<StatBuff>();

        #region UnityMethod

        private void Update()
        {
            if (buffs.Count == 0) return;

            bool removed = false;
            float delta = WorldClock.Instance.DeltaTime;

            // 뒤에서부터 돌아야 지우면서 돌아도 안전하다
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                buffs[i].RemainTime -= delta;
                if (buffs[i].RemainTime <= 0f)
                {
                    buffs.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed) Recalculate();
        }

        #endregion

        #region 외부 설정

        #region 초기화

        // 플레이어 생성 직후 Player.Init에서 호출
        public void Init(Player player, StatClass chrStat, StatClass shopStat)
        {
            this.player = player;
            ChrStat.CopyFrom(chrStat);   // 캐릭터 기본 스탯
            ShopStat.CopyFrom(shopStat); // 로비에서 산 상점 스탯
            Recalculate();
            player.Stat.ResetToFull();
        }

        #endregion

        #region 재료 스탯 바꾸기 (바꾼 뒤 항상 다시 계산)

        public void SetAbilityStat(StatClass total)
        {
            AbilityStat.CopyFrom(total);
            Recalculate();
        }

        // 버프/디버프 추가 (디버프는 음수 %로)
        public void AddBuff(StatClass percent, float duration)
        {
            buffs.Add(new StatBuff(percent, duration));
            Recalculate();
        }

        #endregion

        #region 계산

        public void Recalculate()
        {
            if (player == null) return;

            // 1. 버프 % 합
            BuffStat.Clear();
            for (int i = 0; i < buffs.Count; i++)
            {
                BuffStat.Add(buffs[i].Percent);
            }

            // 2. 더하기 단계 : 캐릭터 + 능력증강 + 스킬
            StatClass sum = new StatClass();
            sum.Add(ChrStat);
            sum.Add(AbilityStat);
            sum.Add(SkillStat);

            StatClass final = player.Stat.unitStat;
            // 3. 스탯마다 곱할지 더할지 정해서 계산
            for (int i = 1; i < (int)StatType.Count; i++)
            {
                StatType type = (StatType)i;
                float baseValue = sum.Get(type);
                float shop = ShopStat.Get(type);
                float buff = BuffStat.Get(type);
                // 수치형 : 기본값 × (1 + 상점%) × (1 + 버프%)
                if (IsMultiplyStat(type)) final.Set(type, baseValue * Rate(shop) * Rate(buff));
                else final.Set(type, baseValue + shop + buff); // %형/개수 : 그냥 더한다
            }
        }

        #endregion

        #endregion

        #region 내부 설정

        // 상점/버프를 곱하는 스탯인지 체크
        private bool IsMultiplyStat(StatType type)
        {
            return type switch
            {
                StatType.MaxHp
                    or StatType.Damage
                    or StatType.AttackSpeed
                    or StatType.Defense
                    or StatType.Speed => true,
                _ => false
            };
        }

        // % → 배율 (20 → 1.2, -30 → 0.7). 디버프가 겹쳐 0 이하가 되지 않게 최소 0.1배
        private float Rate(float percent)
        {
            return Mathf.Max(0.1f, 1f + percent / 100f);
        }

        #endregion
    }
}