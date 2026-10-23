using UnityEngine;

namespace PROJECT3D
{
    public class PlayerStat : BaseStat
    {
        
    }
    
    public abstract class Player : CombatEntity
    {
        public PlayerStat Stat { get; set; } = new PlayerStat();
        public override BaseStat BaseStat => Stat;
        
        public UnitDef unitDef;

        //public int killCount = 0;

        private void Awake()
        {
            
        }
        
        public override void TakeDamage(CombatEvent combatEvent)
        {
            
        }

        public override void TakeHeal(HealEvent healEvent)
        {
            
        }
    }
}