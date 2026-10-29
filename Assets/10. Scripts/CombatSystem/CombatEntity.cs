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
        [field: SerializeField] public virtual BaseStat BaseStat { get; }
        // 전투 개체의 머리 상단 부분 Pivot. Hpbar 또는 이름등을 출력하기 위해 사용됩니다.
        [field: SerializeField] public Transform HeadUpPivot { get; private set; }
        [field: SerializeField] public Transform FirePivot { get; private set; }


        [Header("피격 판정 (원통)")]
        [SerializeField] private float hurtRadius = 0.5f;            // 원통 반경
        [SerializeField] private float hurtHalfHeight = 1f;          // 원통 절반 높이
        [SerializeField] private Color hurtGizmoColor = Color.red;   // 씬 화면에 그릴 색
        [field: SerializeField] public Transform HitPivot { get; private set; }

        public float HurtRadius => hurtRadius;
        public float HurtHalfHeight => hurtHalfHeight;

        // 피격 원통의 중심
        public Vector3 HitCenter => HitPivot != null ? HitPivot.position : transform.position;

        public GameObject GameObject => gameObject;        
        public abstract void TakeDamage(CombatEvent combatEvent);

        public abstract void TakeHeal(HealEvent healEvent );

        // "받는 쪽 사정"에 따라 최종 데미지를 계산하는 함수. 기본은 보정이 없음.
        // 단순히 예상 데미지를 출력하는것에도 활용합니다.
        public virtual int CalculateFinalDamage(int damage)
        {
            return damage;
        }

        // 공격 범위가 내 피격 원통에 닿는지. 투사체/범위 공격/자폭/돌진 모두 이걸로 판정.
        public bool IsHitBy(Vector3 center, float radius, HitShape info, float height)
        {
            Vector3 toMe = HitCenter - center;
            float dy = Mathf.Abs(toMe.y);      // 높이차
            toMe.y = 0f;
            float dxz = toMe.magnitude;        // 바닥 평면 거리

            if (info == HitShape.Column)
                return dxz <= radius + hurtRadius && dy <= height + hurtHalfHeight;

            float outXZ = Mathf.Max(0f, dxz - hurtRadius);     // 원통 옆면 바깥으로 나간 거리
            float outY = Mathf.Max(0f, dy - hurtHalfHeight);   // 원통 위/아래 바깥으로 나간 거리
            return outXZ * outXZ + outY * outY <= radius * radius;
        }

        // 씬 화면에 피격 원통을 그린다 (Scene 창, 또는 Game 창의 Gizmos 버튼을 켜면 보인다)
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = hurtGizmoColor;
            HitGizmo.DrawCylinder(HitCenter, hurtRadius, hurtHalfHeight);
        }
    }

}