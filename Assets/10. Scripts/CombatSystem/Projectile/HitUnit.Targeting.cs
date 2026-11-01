using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 타겟팅 (플레이어 전용) : 대상을 쫓아가 맞히고, 정해진 횟수만큼 다음 대상으로 넘어간다
    public class TargetingHit : HitUnit
    {
        [Header("타겟팅 설정")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float range = 8f;  // 기본 탐색 범위 (첫 탐색/재탐색 공통, 범위 증가 적용)

        public float BaseRange => range;

        private Enemy target;
        private int remainHit;
        private float currentRange;
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>(); // 다음 대상 찾을 때 제외용
        private float speedRate = 1f; // 속도 배율

        public override void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            base.Launch(from, direction, damage, owner);
            target = null;
            remainHit = 1;          // SetTarget을 안 불렀을 때는 1번
            currentRange = range;
            hitTargets.Clear();
            speedRate = 1f;         // 풀 재사용 시 초기화
        }

        // 속도 배율 (FireHit 뒤에 부른다. 1 = 프리팹 속도 그대로)
        public void SetSpeedRate(float rate)
        {
            speedRate = rate;
        }

        public override void ApplyBonus(float rangeRate, float durationRate)
        {
            base.ApplyBonus(rangeRate, durationRate); // 반경
            currentRange = range * rangeRate;         // 탐색 범위
        }

        public void SetTarget(Enemy target, int hitCount)
        {
            this.target = target;
            remainHit = hitCount;
        }

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

            Gizmos.color = Color.green;
            float searchRange = range;
            if (Application.isPlaying) searchRange = currentRange;
            HitGizmo.Draw(transform.position, searchRange, HitShape.Sphere, 0f);
        }

        private bool IsAliveTarget()
        {
            return target != null && target.gameObject.activeInHierarchy && target.Stat.IsDead == false;
        }
    }
}