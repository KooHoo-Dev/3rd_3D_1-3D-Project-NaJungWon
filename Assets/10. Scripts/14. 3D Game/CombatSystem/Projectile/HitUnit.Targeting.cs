using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 타겟팅 (플레이어 전용) : 대상을 쫓아가 맞히고, 정해진 횟수만큼 다음 대상으로 넘어간다
    //  - FireHit 뒤에 SetTarget(대상, 횟수)을 불러 준다 (횟수는 공격 스크립트가 정한다)
    //  - 대상이 죽거나 꺼지면 탐색 범위 안 가장 가까운 적으로 바꾼다 (이미 맞힌 적 제외)
    //  - 바꿀 적이 없거나 횟수를 다 쓰면 사라진다
    //  - 지형은 무시하고, 가는 길의 다른 적도 맞히지 않는다
    public class TargetingHit : HitUnit
    {
        [Header("타겟팅 설정")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float range = 8f;  // 기본 탐색 범위 (첫 탐색/재탐색 공통, 범위 증가 적용)

        public float BaseRange => range;            // 공격 스크립트가 첫 대상을 찾을 때 쓴다

        private Enemy target;
        private int remainHit;
        private float currentRange;
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>();

        public override void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            base.Launch(from, direction, damage, owner);
            target = null;
            remainHit = 1;          // SetTarget을 안 불렀을 때는 1번
            currentRange = range;
            hitTargets.Clear();
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
            transform.position = Vector3.MoveTowards(transform.position, target.HitCenter, speed * Time.deltaTime);

            if (IsTouching(target) == false) return;

            Hit(target);
            hitTargets.Add(target);
            target = null; // 다음 프레임에 다음 대상을 찾는다

            remainHit--;
            if (remainHit <= 0) ReturnToPool();
        }

        private bool IsAliveTarget()
        {
            return target != null && target.gameObject.activeInHierarchy && target.Stat.IsDead == false;
        }
    }
}