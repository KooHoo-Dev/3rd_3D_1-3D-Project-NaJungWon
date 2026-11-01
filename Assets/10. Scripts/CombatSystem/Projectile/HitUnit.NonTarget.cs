using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 논타겟 : 정한 방향으로 직진
    public class NonTargetHit : HitUnit
    {
        [Header("논타겟 설정")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float range = 15f;      // 기본 사거리 (범위 증가 적용)
        [SerializeField] private bool pierce = false;    // 관통 여부
        [SerializeField] private LayerMask blockMask;    // 지형 + 장애물 레이어
        // [변경] explodePrefab / explodeEffect는 부모(HitUnit)로 옮김

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
                Explode(hit.point); // [변경] 벽/바닥에 닿은 자리에서 폭발 (폭발 프리팹이 없으면 아무것도 안 함)
                ReturnToPool();
                return;
            }

            transform.position += direction * step;
            moved += step;

            List<CombatEntity> targets = FindTouching();
            for (int i = 0; i < targets.Count; i++)
            {
                if (hitTargets.Add(targets[i]) == false) continue; // 이미 맞힌 대상 (Add가 false = 이미 들어 있음)

                Hit(targets[i]); // [변경] 폭발형이면 Hit 안에서 폭발한다
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