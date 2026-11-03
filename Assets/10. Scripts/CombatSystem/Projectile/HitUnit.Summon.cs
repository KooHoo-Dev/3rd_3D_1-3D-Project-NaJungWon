using System.Collections.Generic;
using DTT.AreaOfEffectRegions;
using UnityEngine;

namespace PROJECT3D
{
    // 소환형 범위 : 정한 위치(from)에 생긴다 (direction은 바라보는 방향으로만 쓴다)
    public class SummonHit : HitUnit
    {
        [Header("소환 설정")]
        [SerializeField] private float delay = 0f;          // 생기고 나서 첫 판정까지 (떨어지는 연출 시간 등)
        [SerializeField] private float duration = 0f;       // 지속시간 (지속시간 증가 적용). 0 = 한 번만
        [SerializeField] private float tickInterval = 0.5f; // 지속형일 때 판정 간격
        [SerializeField] private float lifeTime = 0f;       // 판정이 끝나도 이 시간(초)까지는 남아 있다 (베기 이펙트를 보여줄 시간. 0 = 판정 끝나면 바로 사라짐)

        [Header("움직임 / 판정 방식")]
        [SerializeField] private bool followOwner = false;      // 켜면 주인을 따라 움직인다 (끄면 생긴 좌표에 고정)
        [SerializeField] private bool hitOncePerTarget = false; // 켜면 같은 적은 한 번만 때린다 (쓸고 지나가는 판정)

        [Header("범위표시 (DTT Circle Region. 없으면 생략)")]
        [SerializeField] private CircleRegion warningCircle; // 자식에 넣은 Circle 1 Region. delay 동안 Fill Progress 0 → 1, 1이 되는 순간 첫 판정

        private Vector3 followOffset;                                      // 주인과의 위치 차이 (월드 기준이라 주인이 돌아도 방향은 그대로)
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>(); // 이미 맞힌 적 (hitOncePerTarget용)

        private float currentDuration;
        private float timer;       // 생기고 나서 지난 시간
        private float nextHitTime; // 다음 판정 시각

        #region UnityMethod

        protected override void Awake()
        {
            base.Awake();
            RegionMaterialUtil.UseOwnMaterials(warningCircle); // 여러 개가 동시에 떠도 진행도가 따로 보이게
        }

        private void Update()
        {
            timer += WorldClock.Instance.DeltaTime;

            // 따라가기형은 판정 전에 위치부터 맞춘다 (주인이 죽어서 꺼졌으면 그 자리에 멈춤)
            if (followOwner && owner != null && owner.gameObject.activeInHierarchy)
                transform.position = owner.transform.position + followOffset;

            UpdateWarning(); // 범위표시 진행도

            float hitEndTime = delay + currentDuration; // 마지막 판정 시각

            // 판정 시간이 끝난 뒤(이펙트만 남은 시간)에는 때리지 않는다
            if (timer >= nextHitTime && nextHitTime <= hitEndTime)
            {
                // 새로 추가 : 첫 판정 순간 폭발 이펙트 (포격 / 지점 폭발. explodeEffect가 없으면 생략)
                if (nextHitTime <= delay) ObjectPoolManager.PlayEffect(ExplodeEffect, transform.position);

                List<CombatEntity> targets = FindTouching();
                for (int i = 0; i < targets.Count; i++)
                {
                    if (hitOncePerTarget && hitTargets.Add(targets[i]) == false) continue;
                    Hit(targets[i]);
                }

                nextHitTime += tickInterval;
            }

            // 판정이 끝나고, lifeTime도 지나면 풀로 (한 번형은 첫 판정 직후, 지속형은 시간이 다 되면)
            if (timer >= Mathf.Max(hitEndTime, lifeTime)) ReturnToPool();
        }

        #endregion

        #region 외부 설정

        public override void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            base.Launch(from, direction, damage, owner);
            currentDuration = duration;
            timer = 0f;
            nextHitTime = delay;
            followOffset = from - owner.transform.position; // 생길 때 주인과의 거리를 기억
            hitTargets.Clear();                             // 풀 재사용 시 이전 기록 지우기
        }

        public override void ApplyBonus(float rangeRate, float durationRate)
        {
            base.ApplyBonus(rangeRate, durationRate); // 반경
            currentDuration = duration * durationRate; // 지속시간
        }

        #endregion

        #region 내부 설정

        // 반경은 판정 반경(currentHitRadius)과 같게, 진행도는 생기고 지난 시간 / delay. 첫 판정 뒤에는 끈다
        private void UpdateWarning()
        {
            if (warningCircle == null) return;

            bool isWarning = timer < delay;
            if (warningCircle.gameObject.activeSelf != isWarning) warningCircle.gameObject.SetActive(isWarning);
            if (isWarning == false) return;

            float scale = warningCircle.transform.lossyScale.x;            // 범위 증가로 커진 크기를 빼고 넣어야 판정과 크기가 같다
            warningCircle.Radius = (scale > 0f) ? currentHitRadius / scale : currentHitRadius;
            warningCircle.FillProgress = timer / delay;
        }

        #endregion
    }
}
