using Unity.VisualScripting;
using UnityEngine;

namespace PROJECT3D
{
    public class EnemyStat : BaseStat
    {
        
    }
    
    public abstract partial class Enemy : CombatEntity
    {
        public EnemyStat Stat { get; set; }
        public override BaseStat BaseStat => Stat;
        
        private EnemyDef enemyDef;
    }
}