using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 공격 애니메이션 상태(Attack / Skill1 / Skill2)에 붙이는 StateMachineBehaviour
    // - 상태 진행률(normalizedTime)이 공격마다 정한 시점(0~1)을 넘으면 그 공격에 신호를 보낸다
    // - 시점 값은 여기가 아니라 각 공격(UnitAttack의 animationTiming)에 있다
    //   → 기본 컨트롤러 하나 + Override 클립 구조여도 캐릭터/몹마다 시점을 다르게 줄 수 있다
    // - 이 상태의 slot과 같은 슬롯이면서 신호를 기다리는(IsDelaying) 공격에만 보낸다
    public class AttackTimingSender : StateMachineBehaviour
    {
        [SerializeField] private AnimationSlot slot = AnimationSlot.Attack; // 이 상태가 어떤 공격 애니메이션인지

        private CombatEntity owner;                                         // 애니메이터 주인 (플레이어 / 몹)
        private readonly List<UnitAttack> attacks = new List<UnitAttack>(); // 주인이 가진 공격들 (매번 새로 만들지 않고 재사용)

        #region UnityMethod

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (owner == null) owner = animator.GetComponentInParent<CombatEntity>();
            if (owner == null) return;

            // 무기증강은 게임 중에 늘어나서 들어올 때마다 다시 모은다
            owner.GetComponentsInChildren(attacks);

            if (IsSkillSlot()) owner.SetSkillMotion(true); // 스킬 모션 시작 (기본 무기가 이 동안 공격 모션 없이 발사)
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            for (int i = 0; i < attacks.Count; i++)
            {
                UnitAttack attack = attacks[i];
                if (attack.GetAnimationSlot() != slot) continue;                       // 다른 슬롯 공격
                if (attack.IsDelaying == false) continue;                              // 기다리는 중이 아님
                if (stateInfo.normalizedTime < attack.GetAnimationTiming()) continue;  // 아직 공격 시점이 아님

                attack.OnAnimationTiming();
            }
        }

        // 공격 시점 전에 상태를 빠져나가도(다른 애니메이션으로 끊김) 공격은 나가게 한다
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            for (int i = 0; i < attacks.Count; i++)
            {
                UnitAttack attack = attacks[i];
                if (attack.GetAnimationSlot() != slot) continue;
                if (attack.IsDelaying == false) continue;

                attack.OnAnimationTiming();
            }

            // 기본 공격 상태를 빠져나가면 공격 레이어를 끈다 (켜는 건 UnitAttack.PlayAnimation)
            if (slot == AnimationSlot.Attack && layerIndex > 0) animator.SetLayerWeight(layerIndex, 0f);

            if (IsSkillSlot() && owner != null) owner.SetSkillMotion(false); // 스킬 모션 끝
        }

        #endregion

        #region 내부 설정

        // E / Q 스킬 상태인지
        private bool IsSkillSlot()
        {
            return slot == AnimationSlot.ESkill || slot == AnimationSlot.QSkill;
        }

        #endregion
    }
}