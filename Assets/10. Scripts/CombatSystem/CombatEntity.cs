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

        // E / Q 스킬 모션 재생 중인지 (AttackTimingSender가 스킬 상태에 들어갈 때 +1, 나갈 때 -1)
        // 개수로 세는 이유 : E → Q로 바로 이어지면 Q 들어가기가 E 나가기보다 먼저 불린다
        private int skillMotionCount;
        public bool IsSkillMotion => skillMotionCount > 0;

        #region UnityMethod

        // 씬 화면에 피격 원통을 그린다 (Scene 창, 또는 Game 창의 Gizmos 버튼을 켜면 보인다)
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = hurtGizmoColor;
            HitGizmo.DrawCylinder(HitCenter, hurtRadius, hurtHalfHeight);
        }

        #endregion

        #region 외부 설정

        #region 스킬 모션

        public void SetSkillMotion(bool isOn)
        {
            skillMotionCount += isOn ? 1 : -1;
            if (skillMotionCount < 0) skillMotionCount = 0;
        }

        #endregion

        #region 피격

        public abstract void TakeDamage(CombatEvent combatEvent);

        public abstract void TakeHeal(HealEvent healEvent );

        // "받는 쪽 사정"에 따라 최종 데미지를 계산하는 함수. 기본은 보정이 없음.
        // 단순히 예상 데미지를 출력하는것에도 활용합니다.
        public virtual int CalculateFinalDamage(int damage)
        {
            return damage;
        }

        #endregion

        #region 피격 판정

        // 공격 범위가 내 피격 원통에 닿는지. 투사체/범위 공격/자폭/돌진 모두 이걸로 판정.
        public bool IsHitBy(Vector3 center, float radius, HitShape info, float height)
        {
            return IsHitBy(center, radius, info, height, Quaternion.identity, 0f); // 회전 / 두께 없는 판정
        }

        // rotation = 공격 범위의 회전, depth = 앞뒤 절반 두께 (둘 다 Cube만 사용)
        public bool IsHitBy(Vector3 center, float radius, HitShape info, float height, Quaternion rotation, float depth)
        {
            Vector3 toMe = HitCenter - center;

            // 사각형 판정
            // 내 위치를 공격 범위 기준 좌표로 바꾸면 회전 없는 상자로 비교할 수 있다
            //  x(좌우) = 반경, y(위아래) = 높이, z(앞뒤) = 두께 (각각 절반 길이)
            if (info == HitShape.Cube)
            {
                Vector3 local = Quaternion.Inverse(rotation) * toMe;
                return Mathf.Abs(local.x) <= radius + hurtRadius
                       && Mathf.Abs(local.y) <= height + hurtHalfHeight
                       && Mathf.Abs(local.z) <= depth + hurtRadius; // 앞뒤 두께 + 내 몸 두께
            }

            float dy = Mathf.Abs(toMe.y);      // 높이차
            toMe.y = 0f;
            float dxz = toMe.magnitude;        // 바닥 평면 거리

            if (info == HitShape.Column)
                return dxz <= radius + hurtRadius && dy <= height + hurtHalfHeight;

            float outXZ = Mathf.Max(0f, dxz - hurtRadius);     // 원통 옆면 바깥으로 나간 거리
            float outY = Mathf.Max(0f, dy - hurtHalfHeight);   // 원통 위/아래 바깥으로 나간 거리
            return outXZ * outXZ + outY * outY <= radius * radius;
        }

        #endregion

        #endregion
    }

}