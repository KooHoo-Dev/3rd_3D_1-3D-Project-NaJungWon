using UnityEngine;

namespace PROJECT3D
{
    // 애니메이터에 이름(문자열)으로 들어가는 값 모음 (플레이어 / 몹 / 공격 공용)
    //  - 애니메이터 컨트롤러의 파라미터 / 상태 / 레이어 이름을 바꾸면 여기만 고친다
    //  - 파라미터 / 상태는 해시로 미리 만들어 둔다 (매번 문자열로 찾지 않게)
    public static class AnimatorKeys
    {
        #region 파라미터

        // 이동
        public static readonly int IsMove     = Animator.StringToHash("IsMove");
        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int Jump       = Animator.StringToHash("Jump");
        public static readonly int Fall       = Animator.StringToHash("Fall");
        public static readonly int IsDash     = Animator.StringToHash("IsDash");

        // 공격 / 스킬
        public static readonly int Attack      = Animator.StringToHash("Attack");
        public static readonly int ESkill      = Animator.StringToHash("ESkill");
        public static readonly int QSkill      = Animator.StringToHash("QSkill");
        public static readonly int IsCasting   = Animator.StringToHash("IsCasting");
        public static readonly int IsAiming    = Animator.StringToHash("IsAiming");
        public static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");

        // 상태
        public static readonly int Die = Animator.StringToHash("Die");

        #endregion

        #region 애니메이션 상태

        public static readonly int DeadState = Animator.StringToHash("Unit_Dead"); // 사망 상태 (모션이 끝났는지 확인용)

        #endregion

        #region 레이어

        public const string AttackLayer = "AttackLayer"; // 상체 공격 레이어 (번호는 애니메이터마다 달라서 이름으로 찾는다)

        #endregion

        #region 도우미

        // 공격 슬롯 → 트리거 해시 (None이면 0)
        public static int GetSlotTrigger(AnimationSlot slot)
        {
            switch (slot)
            {
                case AnimationSlot.Attack: return Attack;
                case AnimationSlot.ESkill: return ESkill;
                case AnimationSlot.QSkill: return QSkill;
                default: return 0;
            }
        }

        // 공격 레이어 무게 바꾸기 (애니메이터가 없거나 레이어가 없으면 아무것도 안 함)
        public static void SetAttackLayerWeight(Animator animator, float weight)
        {
            if (animator == null) return;

            int layerIndex = animator.GetLayerIndex(AttackLayer);
            if (layerIndex >= 0) animator.SetLayerWeight(layerIndex, weight);
        }

        #endregion
    }
}
