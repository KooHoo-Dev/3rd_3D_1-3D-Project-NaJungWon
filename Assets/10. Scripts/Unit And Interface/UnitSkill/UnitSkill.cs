using UnityEngine;

namespace PROJECT3D
{
    // 스킬의 부모 클래스
    //  - 데미지/범위/지속시간 계산은 부모(UnitAttack)가 한다 (스킬 테이블 수치 + 플레이어 스탯)
    //  - 여기서는 쿨타임과 시전 상태만 관리
    public abstract class UnitSkill : UnitAttack
    {
        public float BaseCooldown { get; private set; }  // 테이블 쿨타임(초)
        public float Cooldown { get; private set; }      // 이번에 적용된 쿨타임(초). 공속 반영
        private float cooldownTimer;                     // 남은 쿨타임

        public bool IsCasting { get; protected set; }    // 스킬 사용 중 여부
        public float CooldownRemain => cooldownTimer;    // UI 표시용

        // 스킬 테이블 수치 넣기
        public void Init(SkillDef def)
        {
            SetAttackStat(def.SkillStat);
            BaseCooldown = def.Cooldown;
        }

        protected virtual void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= WorldClock.Instance.DeltaTime;
        }

        // 키 입력이 들어왔을 때 호출
        // aimDirection = 화면이 바라보는 수평 방향. 쿨타임 중이면 false
        public bool TryUse(Vector3 aimDirection)
        {
            if (cooldownTimer > 0f || IsCasting) return false;

            // 쿨타임 = 테이블 쿨타임 / 플레이어 공속 배율
            Cooldown = (OwnerStat.AttackSpeed <= 0f) ? BaseCooldown : BaseCooldown / OwnerStat.AttackSpeed;
            cooldownTimer = Cooldown;
            Use(aimDirection);
            return true;
        }

        // 실제 스킬 내용. 자식이 반드시 재정의
        protected abstract void Use(Vector3 aimDirection);
    }
}