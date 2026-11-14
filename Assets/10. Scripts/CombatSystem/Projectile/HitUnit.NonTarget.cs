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

        [Header("폭발 (없으면 맞은 대상에게 직접 데미지 + 피격 이펙트)")]
        [SerializeField] private HitUnit explodePrefab;  // 맞으면 직접 피해 대신 그 자리에 만들 범위 (SummonHit. 폭발 이펙트 / 피격 이펙트는 범위 프리팹에서)

        private Vector3 direction;
        private float currentRange;
        private float moved;                                                     // 지금까지 날아간 거리
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>(); // 이미 맞힌 대상 (관통 시 중복 방지)

        #region UnityMethod

        private void Update()
        {
            float step = speed * WorldClock.Instance.DeltaTime;

            // 이번 프레임에 지나갈 구간에 지형/장애물이 있으면 사라진다
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, blockMask, QueryTriggerInteraction.Ignore))
            {
                SpawnExplosion(hit.point); // 벽/바닥에 닿은 자리에서 폭발 (폭발 프리팹이 없으면 아무것도 안 함)
                ReturnToPool();
                return;
            }

            transform.position += direction * step;
            moved += step;

            List<CombatEntity> targets = FindTouching();
            for (int i = 0; i < targets.Count; i++)
            {
                if (hitTargets.Add(targets[i]) == false) continue; // 이미 맞힌 대상 (Add가 false = 이미 들어 있음)

                // 폭발형 : 직접 피해 대신 지금 자리에 범위를 만들고 사라진다 (데미지 / 피격 이펙트는 범위가 준다)
                if (explodePrefab != null)
                {
                    SpawnExplosion(transform.position);
                    ReturnToPool();
                    return;
                }

                Hit(targets[i]);
                if (pierce == false)
                {
                    ReturnToPool();
                    return;
                }
            }

            if (moved >= currentRange) ReturnToPool();
        }

        #endregion

        #region 외부 설정

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

        #endregion

        #region 내부 설정

        // position에 폭발 범위(explodePrefab) 만들기 (없으면 아무것도 안 함)
        //  - 범위의 예고 시간은 0으로 : 맞은 자리에서 바로 폭발 이펙트 + 범위 데미지 + 적마다 피격 이펙트
        private void SpawnExplosion(Vector3 position)
        {
            HitUnit explosion = SpawnHit(explodePrefab, position, transform.forward);
            if (explosion is SummonHit summon) summon.SetDelay(0f);
        }

        #endregion
    }
}