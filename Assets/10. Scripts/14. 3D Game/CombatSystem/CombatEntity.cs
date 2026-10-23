using UnityEngine;

namespace PROJECT3D
{
    // # CombatEntity - 전투에 참여하는 모든 개체의 공통 부모(추상 클래스)
    // - Player 개체와 Enemy 개체로 구성되며, 원활한 개발을 위해 Dummy 개체도 포함된다
    // - 개발 서순
    //  1. Player => Dummy
    //  2. Enemy => Player

    public abstract class CombatEntity : MonoBehaviour , IDamageable
    {
        // 전투 개체의 머리 상단 부분 Pivot. Hpbar 또는 이름등을 출력하기 위해 사용됩니다.
        [field: SerializeField] public Transform HeadUpPivot { get; private set; }
        [field: SerializeField] public virtual BaseStat BaseStat { get; }

        //public Collider MainCollider { get; }
        public GameObject GameObject { get; }
        
        public abstract void TakeDamage(CombatEvent combatEvent);

        public abstract void TakeHeal(HealEvent healEvent );

        // "받는 쪽 사정"에 따라 최종 데미지를 계산하는 함수. 기본은 보정이 없음.
        // 단순히 예상 데미지를 출력하는것에도 활용합니다.
        public virtual int CalculateFinalDamage(int damage)
        {
            return damage;
        }
    }

}

