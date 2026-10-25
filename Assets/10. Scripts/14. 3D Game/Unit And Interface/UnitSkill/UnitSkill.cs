using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    public abstract class UnitSkill : UnitAttack
    {
        private CombatEntity owner;

        [Header("스킬 공통 (임시 값)")] 
        [SerializeField] private List<AugmentType> useValueType = new List<AugmentType>(); //사용하는 계수값
        [SerializeField] protected int damage = 50;
        [SerializeField] private float cooldown = 5f;
        private float cooldownTimer;                     // 남은 쿨타임

        public bool IsCasting { get; protected set; }    // 스킬 사용 중 (이동/다른 스킬 막기)
        public float CooldownRemain => cooldownTimer;    // UI 표시용

        protected virtual void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        }

        // 키 입력이 들어왔을 때 부른다. aimDirection = 화면이 바라보는 수평 방향. 쿨타임 중이면 false
        public bool TryUse(Vector3 aimDirection)
        {
            if (cooldownTimer > 0f || IsCasting) return false;

            cooldownTimer = cooldown;
            Use(aimDirection);
            return true;
        }

        // 실제 스킬 내용. 자식이 반드시 재정의
        protected abstract void Use(Vector3 aimDirection);
    }
}