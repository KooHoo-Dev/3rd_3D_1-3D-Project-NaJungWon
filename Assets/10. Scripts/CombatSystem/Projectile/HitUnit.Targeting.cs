using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 타겟팅 (플레이어 전용) : 대상을 쫓아가 맞히고, 정해진 횟수만큼 다음 대상으로 넘어간다
    public class TargetingHit : HitUnit
    {
        [Header("타겟팅 설정")]
        [SerializeField] private float speed = 10f;
        // [변경] 탐색 범위(range / BaseRange) 삭제 : 대상을 찾는 범위는 쏘는 쪽(무기증강)이 정하고 SetTarget으로 넘겨준다

        private Enemy target;
        private int remainHit;
        private float currentRange; // 다음 대상 재탐색 범위 (SetTarget으로 받은 값. 범위 증가는 쏘는 쪽에서 적용)
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>(); // 다음 대상 찾을 때 제외용
        private float speedRate = 1f; // 속도 배율

        #region UnityMethod

        private void Update()
        {
            if (IsAliveTarget() == false)
            {
                target = EnemyRegistry.FindNearest(transform.position, currentRange, hitTargets);
                if (target == null)
                {
                    ReturnToPool();
                    return;
                }
            }

            // 대상의 몸 중심으로 곧장 날아간다
            Vector3 toTarget = target.HitCenter - transform.position;
            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget);
            transform.position = Vector3.MoveTowards(transform.position, target.HitCenter, speed * speedRate * WorldClock.Instance.DeltaTime); // 속도 배율 적용

            if (IsTouching(target) == false) return;

            Hit(target);
            hitTargets.Clear();
            hitTargets.Add(target); // 방금 맞힌 적만 다음 대상에서 빼기
            target = null; // 다음 프레임에 다음 대상을 찾는다

            remainHit--;
            if (remainHit <= 0) ReturnToPool();
        }

        // 공격 범위(부모)에 더해 다음 대상 탐색 범위도 그린다
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (Application.isPlaying == false) return; // [변경] 탐색 범위는 쏠 때 받으니 실행 중에만
            Gizmos.color = Color.green;
            HitGizmo.Draw(transform.position, currentRange, HitShape.Sphere, 0f); // [변경]
        }

        #endregion

        #region 외부 설정

        public override void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            base.Launch(from, direction, damage, owner);
            target = null;
            remainHit = 1;          // SetTarget을 안 불렀을 때는 1번
            currentRange = 0f;      // [변경] SetTarget을 안 불렀으면 재탐색 안 함
            hitTargets.Clear();
            speedRate = 1f;         // 풀 재사용 시 초기화
        }

        // 속도 배율 (FireHit 뒤에 부른다. 1 = 프리팹 속도 그대로)
        public void SetSpeedRate(float rate)
        {
            speedRate = rate;
        }

        // [변경] ApplyBonus 재정의 삭제 (탐색 범위가 없어져서 부모의 반경 처리만 쓴다)

        // [변경] searchRange 추가 : 쏘는 쪽이 대상을 찾은 범위(범위 증가 적용 값)를 그대로 재탐색에 쓴다
        public void SetTarget(Enemy target, int hitCount, float searchRange)
        {
            this.target = target;
            remainHit = hitCount;
            currentRange = searchRange;
        }

        #endregion

        #region 내부 설정

        private bool IsAliveTarget()
        {
            return target != null && target.gameObject.activeInHierarchy && target.Stat.IsDead == false;
        }

        #endregion
    }
}