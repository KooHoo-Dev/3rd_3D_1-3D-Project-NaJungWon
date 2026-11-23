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
        [SerializeField] private Vector3 hurtTilt = Vector3.zero;    // 새로 추가 : 원통 기울기 (도, 몸 기준. x = 앞뒤로 숙임, z = 좌우로 기울임)
        [SerializeField] private Color hurtGizmoColor = Color.red;   // 씬 화면에 그릴 색
        [field: SerializeField] public Transform HitPivot { get; private set; }

        public float HurtRadius => hurtRadius;
        public float HurtHalfHeight => hurtHalfHeight;

        // 새로 추가 : 피격 원통의 회전 = 몸 정면 방향(Y 회전) + 기울기
        //  몹은 이동 방향으로 몸(transform)을 돌리므로, 이동하면 피격 원통도 같이 돈다
        public Quaternion HurtRotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Quaternion.Euler(hurtTilt);

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
            Gizmos.matrix = Matrix4x4.TRS(HitCenter, HurtRotation, Vector3.one); // [변경] 원통 회전(정면 방향 + 기울기) 반영
            HitGizmo.DrawCylinder(Vector3.zero, hurtRadius, hurtHalfHeight);
            Gizmos.matrix = Matrix4x4.identity; // 새로 추가 : 다른 Gizmo에 영향 없게 되돌린다
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
        // 공격 모양(info)에 따라 셋 중 하나로 검사한다. 계산은 모양마다 아래 함수에 나눠 둠
        public bool IsHitBy(Vector3 center, float radius, HitShape info, float height, Quaternion rotation, float depth)
        {
            Vector3 toMe = HitCenter - center; // 공격 중심 → 내 원통 중심
            Quaternion hurtRotation = HurtRotation;

            if (info == HitShape.Cube) return IsHitByCube(toMe, radius, height, depth, rotation, hurtRotation);
            if (info == HitShape.Column) return IsHitByColumn(toMe, radius, height, hurtRotation);
            return IsHitBySphere(toMe, radius, hurtRotation);
        }

        // 사각형 공격 : 공격 상자와 내 피격 원통을 감싼 상자(OBB)가 겹치는지 (분리축 검사)
        //  두 상자의 면 방향 6개 축에 각각 그림자(투영)를 내려서, 한 축이라도 안 겹치면 안 맞은 것
        //  공격 상자 절반 크기 : x(좌우) = 반경, y(위아래) = 높이, z(앞뒤) = 두께
        //  내 상자 절반 크기 : 원통 반경 / 절반 높이 / 원통 반경 (회전 = 정면 방향 + 기울기)
        //  (모서리끼리 비스듬히 닿는 축 9개는 생략 → 모서리 근처에서 아주 조금 넉넉하게 맞을 수 있다)
        private bool IsHitByCube(Vector3 toMe, float radius, float height, float depth, Quaternion rotation, Quaternion hurtRotation)
        {
            Vector3 attackHalf = new Vector3(radius, height, depth);
            Vector3 hurtHalf = new Vector3(hurtRadius, hurtHalfHeight, hurtRadius);
            if (IsSeparated(rotation * Vector3.right, toMe, rotation, attackHalf, hurtRotation, hurtHalf)) return false;
            if (IsSeparated(rotation * Vector3.up, toMe, rotation, attackHalf, hurtRotation, hurtHalf)) return false;
            if (IsSeparated(rotation * Vector3.forward, toMe, rotation, attackHalf, hurtRotation, hurtHalf)) return false;
            if (IsSeparated(hurtRotation * Vector3.right, toMe, rotation, attackHalf, hurtRotation, hurtHalf)) return false;
            if (IsSeparated(hurtRotation * Vector3.up, toMe, rotation, attackHalf, hurtRotation, hurtHalf)) return false;
            if (IsSeparated(hurtRotation * Vector3.forward, toMe, rotation, attackHalf, hurtRotation, hurtHalf)) return false;
            return true;
        }

        // 구 공격 : 공격 중심을 내 원통 기준 좌표로 바꿔서, 원통 바깥으로 나간 거리를 공격 반경과 비교 (원통이 기울어도 맞게)
        private bool IsHitBySphere(Vector3 toMe, float radius, Quaternion hurtRotation)
        {
            Vector3 local = Quaternion.Inverse(hurtRotation) * toMe;
            float dy = Mathf.Abs(local.y);                          // 원통 축 방향 거리 (높이차)
            float dxz = new Vector2(local.x, local.z).magnitude;    // 원통 축에서 떨어진 거리

            float outXZ = Mathf.Max(0f, dxz - hurtRadius);     // 원통 옆면 바깥으로 나간 거리
            float outY = Mathf.Max(0f, dy - hurtHalfHeight);   // 원통 위/아래 바깥으로 나간 거리
            return outXZ * outXZ + outY * outY <= radius * radius;
        }

        // 기둥 공격
        // 기둥 공격은 늘 세로로 서 있고, 내 원통은 눕거나 기울 수 있다(hurtTilt). 높이와 바닥 평면을 나눠서 본다
        //  1. 높이 : 내 원통이 세로로 차지하는 절반 높이를 구해, 기둥 높이와 겹치는지
        //  2. 바닥 평면 : 위에서 내려다본 내 원통 축(선분)과 기둥 중심 사이 가장 가까운 거리 ≤ 기둥 반경 + 내 반경
        //  세운 원통(기울기 0)이면 예전 계산과 똑같고, 눕힌 원통은 위에서 볼 때 머리 · 꼬리 끝 모서리만 조금 둥글게 본다
        //  toMe = 기둥 중심 → 내 원통 중심, columnHalfHeight = 기둥 절반 높이
        private bool IsHitByColumn(Vector3 toMe, float columnRadius, float columnHalfHeight, Quaternion hurtRotation)
        {
            Vector3 axis = hurtRotation * Vector3.up; // 내 원통 축 방향 (길이 1)

            // 1. 높이 겹침
            float upAmount = Mathf.Abs(axis.y);                        // 축이 위를 향한 정도 (세움 = 1, 눕힘 = 0)
            float sideAmount = Mathf.Sqrt(1f - upAmount * upAmount);   // 축이 옆으로 누운 정도
            float myHalfHeight = upAmount * hurtHalfHeight + sideAmount * hurtRadius; // 내 원통이 세로로 차지하는 절반 높이
            if (Mathf.Abs(toMe.y) > columnHalfHeight + myHalfHeight) return false;

            // 2. 바닥 평면 거리 (위에서 내려다본 모양)
            Vector2 center = new Vector2(toMe.x, toMe.z);                     // 기둥 중심 → 내 원통 중심
            Vector2 halfAxis = new Vector2(axis.x, axis.z) * hurtHalfHeight;  // 위에서 본 내 축 선분의 절반 (세운 원통이면 0)

            // 내 축 선분(center - halfAxis ~ center + halfAxis) 위에서 기둥 중심(원점)과 가장 가까운 점
            float t = 0f;
            float lengthSqr = halfAxis.sqrMagnitude;
            if (lengthSqr > 0.0001f)
                t = Mathf.Clamp(-Vector2.Dot(center, halfAxis) / lengthSqr, -1f, 1f);
            Vector2 closest = center + halfAxis * t;

            float reach = columnRadius + hurtRadius;
            return closest.sqrMagnitude <= reach * reach;
        }

        // OBB 분리축 검사
        // axis 방향으로 두 상자의 그림자 길이 합보다 중심 거리가 길면 떨어져 있다 (= 안 겹침)
        private static bool IsSeparated(Vector3 axis, Vector3 centerGap, Quaternion rotationA, Vector3 halfA, Quaternion rotationB, Vector3 halfB)
        {
            float shadowA = Mathf.Abs(Vector3.Dot(rotationA * Vector3.right, axis)) * halfA.x
                          + Mathf.Abs(Vector3.Dot(rotationA * Vector3.up, axis)) * halfA.y
                          + Mathf.Abs(Vector3.Dot(rotationA * Vector3.forward, axis)) * halfA.z;
            float shadowB = Mathf.Abs(Vector3.Dot(rotationB * Vector3.right, axis)) * halfB.x
                          + Mathf.Abs(Vector3.Dot(rotationB * Vector3.up, axis)) * halfB.y
                          + Mathf.Abs(Vector3.Dot(rotationB * Vector3.forward, axis)) * halfB.z;
            return Mathf.Abs(Vector3.Dot(centerGap, axis)) > shadowA + shadowB;
        }

        #endregion

        #endregion
    }

}