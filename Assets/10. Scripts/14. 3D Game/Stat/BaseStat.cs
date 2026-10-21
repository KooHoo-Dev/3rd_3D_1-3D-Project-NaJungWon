using UnityEngine;

namespace PROJECT3D
{
    [System.Serializable]
    public class BaseStat
    {
        public StatClass baseStat; //모든 스탯 총합
        //[field: SerializeField] public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public bool isDead => (Hp <= 0);
        
        public bool ApplyDamage(int damage)
        {
            Hp = Mathf.Clamp(Hp - damage, 0, baseStat.MaxHp);
            return isDead;
        }

        public void ApplyHeal(int heal)
        {
            Hp = Mathf.Clamp(Hp + heal, 0, baseStat.MaxHp);
        }
        
        public void ResetToFull()
        {
            Hp = baseStat.MaxHp;
        }

        public void UpdateStat()
        {
            //PlayerStatHandler.Instance.ApplyPlayerStatChange(StatClass baseStat);
        }
    }
}