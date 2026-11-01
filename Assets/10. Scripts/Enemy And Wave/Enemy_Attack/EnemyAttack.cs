using UnityEngine;

namespace PROJECT3D
{
    // 몹 공격 패턴의 부모. 몹 프리팹에 자식을 하나 붙인다.
    // - 쿨타임/사거리 판정은 몹 컨트롤러가 하고, 때가 되면 StartFire()를 부른다
    // - 발사/데미지 도우미는 UnitAttack()에 있다
    // - 몹 공격은 논타겟만 쓴다 (유도탄 X)
    public abstract class EnemyAttack : UnitAttack
    {
        private CombatEntity delayedTarget; // 공격 시점에 공격할 대상

        // 애니메이션 / 딜레이 후에 Fire (설정이 없으면 바로)
        public void StartFire(CombatEntity target)
        {
            if (IsDelaying) return; // 이전 공격의 시점을 아직 기다리는 중
            delayedTarget = target;
            StartDelay();
        }

        // 공격 시점이 되면 실제 공격
        protected override void OnDelayEnd()
        {
            if (delayedTarget == null) return;
            Fire(delayedTarget);
        }

        // 실제 공격 내용. 자식이 반드시 재정의
        public abstract void Fire(CombatEntity target);
    }
}