using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PROJECT3D
{
    public abstract class PlayerControllerBase : MonoBehaviour
    {
        //여기 모델이랑 애니메이션 조작 부분만 담기
        public Player Player;
        public SkinnedMeshRenderer bodyRenderer;
        public Animator anim;
        
        protected static readonly int IsMove = Animator.StringToHash("IsMove");
        protected static readonly int Jump = Animator.StringToHash("Jump");
        protected static readonly int Fall = Animator.StringToHash("Fall");
        protected static readonly int Attack = Animator.StringToHash("Attack");
        protected static readonly int ESkill = Animator.StringToHash("ESkill");
        protected static readonly int QSkill = Animator.StringToHash("QSkill");
        protected static readonly int Casting = Animator.StringToHash("IsCasting");
        protected static readonly int IsGrounded = Animator.StringToHash("IsGrounded"); 
        protected static readonly int Die = Animator.StringToHash("Die");
        protected static readonly int Aiming = Animator.StringToHash("IsAiming"); // 새로 추가 : 조준 스킬을 누르고 있는 동안 (Unit_Aim 모션)
    }
}