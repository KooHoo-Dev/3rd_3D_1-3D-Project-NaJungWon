using UnityEngine;

namespace PROJECT3D
{
    [System.Serializable]
    public class BaseStat
    {
        public StatClass unitStat = new StatClass(); //모든 스탯 총합

        public int Hp { get; private set; }
        public bool IsDead => (Hp <= 0);

        #region 외부 설정

        #region 체력

        public bool ApplyDamage(int damage)
        {
            Hp = Mathf.Clamp(Hp - damage, 0, unitStat.MaxHp);
            return IsDead;
        }

        public void ApplyHeal(int heal)
        {
            Hp = Mathf.Clamp(Hp + heal, 0, unitStat.MaxHp);
        }

        public void ResetToFull()
        {
            Hp = unitStat.MaxHp;
        }

        #endregion

        #region 스탯

        public void UpdateStat()
        {
            //PlayerStatHandler.Instance.ApplyPlayerStatChange(StatClass baseStat);
        }

        #endregion

        #endregion
    }
}