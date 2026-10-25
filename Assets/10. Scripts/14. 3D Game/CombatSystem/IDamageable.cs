using UnityEngine;

namespace PROJECT3D
{
    public interface IDamageable
    {
        GameObject GameObject { get; }

        void TakeDamage(CombatEvent combatEvent);
        void TakeHeal(HealEvent healEvent);
    }
}
