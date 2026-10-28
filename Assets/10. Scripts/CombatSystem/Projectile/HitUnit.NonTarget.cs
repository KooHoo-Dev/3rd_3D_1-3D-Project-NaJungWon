using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 논타겟 : 정한 방향으로 직진
    //  - pierce = false : 처음 맞힌 대상에서 사라진다
    //  - pierce = true  : 사거리 끝까지 날아가며 닿는 모든 적에게 피해 (관통 제한 없음)
    //  - 사거리 끝 / 지형·장애물에 닿으면 사라진다
    public class NonTargetHit : HitUnit
    {
        [Header("논타겟 설정")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float range = 15f;      // 기본 사거리 (범위 증가 적용)
        [SerializeField] private bool pierce = false;    // 관통 여부
        [SerializeField] private LayerMask blockMask;    // 지형 + 장애물 레이어

        private Vector3 direction;
        private float currentRange;
        private float moved;                                                     // 지금까지 날아간 거리
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>(); // 이미 맞힌 대상 (관통 시 중복 방지)

        public override void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            base.Launch(from, direction, damage, owner);
            this.direction = direction.normalized;
            currentRange = range;
            moved = 0f;
            hitTargets.Clear();
        }

        public override void ApplyBonus(float rangeRate, float durationRate)
        {
            base.ApplyBonus(rangeRate, durationRate); // 반경
            currentRange = range * rangeRate;         // 사거리
        }

        private void Update()
        {
            float step = speed * WorldClock.Instance.DeltaTime;

            // 이번 프레임에 지나갈 구간에 지형/장애물이 있으면 사라진다
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, blockMask, QueryTriggerInteraction.Ignore))
            {
                ReturnToPool();
                return;
            }

            transform.position += direction * step;
            moved += step;

            List<CombatEntity> targets = FindTouching();
            for (int i = 0; i < targets.Count; i++)
            {
                if (hitTargets.Add(targets[i]) == false) continue; // 이미 맞힌 대상 (Add가 false = 이미 들어 있음)

                Hit(targets[i]);
                if (pierce == false)
                {
                    ReturnToPool();
                    return;
                }
            }

            if (moved >= currentRange) ReturnToPool();
        }
    }
}