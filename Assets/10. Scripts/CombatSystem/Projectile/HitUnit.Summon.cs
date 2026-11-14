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
        [SerializeField] private ParticleEffect explodeEffect; // 부모(HitUnit)에서 옮김 : 첫 판정 순간 중심에 터뜨릴 폭발 이펙트 (없으면 생략)

        [Header("움직임 / 판정 방식")]
        [SerializeField] private bool followOwner = false;      // 켜면 주인을 따라 움직인다 (끄면 생긴 좌표에 고정)
        [SerializeField] private bool hitOncePerTarget = false; // 켜면 같은 적은 한 번만 때린다 (쓸고 지나가는 판정)

        [Header("범위표시 (DTT Circle Region. 없으면 생략)")]
        [SerializeField] private CircleRegion warningCircle; // 자식에 넣은 Circle 1 Region. delay 동안 Fill Progress 0 → 1, 1이 되는 순간 첫 판정

        private Vector3 followOffset;                                      // 주인과의 위치 차이 (월드 기준이라 주인이 돌아도 방향은 그대로)
        private HashSet<CombatEntity> hitTargets = new HashSet<CombatEntity>(); // 이미 맞힌 적 (hitOncePerTarget용)

        private float currentDuration;
        private float currentDelay; // 이번 생성의 첫 판정까지 시간 (기본 delay. 투사체 폭발은 SetDelay(0)으로 바로)
        private float timer;       // 생기고 나서 지난 시간
        private float nextHitTime; // 다음 판정 시각
        private bool isFollowing;  // 지금 주인을 따라가는 중 (followOwner로 시작, Finish에서 멈춤)
        private ParticleEffect followEffect; // 따라가기형일 때 같이 옮길 폭발 이펙트 (돌진 이펙트가 몸을 따라오게)

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
            if (isFollowing && owner != null && owner.gameObject.activeInHierarchy) // [변경] followOwner → isFollowing (Finish 뒤에는 멈춘 자리에 남는다)
            {
                transform.position = owner.transform.position + followOffset;
                MoveFollowEffect(); // 새로 추가 : 폭발 이펙트도 같이
            }

            UpdateWarning(); // 범위표시 진행도

            float hitEndTime = currentDelay + currentDuration; // 마지막 판정 시각 // [변경] delay → currentDelay

            // 판정 시간이 끝난 뒤(이펙트만 남은 시간)에는 때리지 않는다
            if (timer >= nextHitTime && nextHitTime <= hitEndTime)
            {
                // 첫 판정 순간 폭발 이펙트 (포격 / 지점 폭발. explodeEffect가 없으면 생략)
                if (nextHitTime <= currentDelay) PlayExplodeEffect(); // [변경] PlayEffect → 따라가기형이면 이펙트를 기억해 두고 같이 옮긴다

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
            currentDelay = delay;
            nextHitTime = delay;
            followOffset = from - owner.transform.position; // 생길 때 주인과의 거리를 기억
            hitTargets.Clear();                             // 풀 재사용 시 이전 기록 지우기
            isFollowing = followOwner;
            followEffect = null;
        }

        public override void ApplyBonus(float rangeRate, float durationRate)
        {
            base.ApplyBonus(rangeRate, durationRate); // 반경
            currentDuration = duration * durationRate; // 지속시간
        }

        // 첫 판정까지 시간 바꾸기 (Launch 뒤에 부른다)
        //  - 투사체가 맞은 자리에 만든 폭발은 0으로 : 예고(warning) 없이 바로 폭발 이펙트 + 데미지 + 피격 이펙트
        public void SetDelay(float time)
        {
            currentDelay = time;
            nextHitTime = time;
        }

        // 판정 시간 바꾸기 (ApplyBonus 뒤에 부른다. 지속시간 증가는 적용 안 함)
        //  - 돌진처럼 공격 쪽이 시간을 정하는 범위용 : 돌진 시간 동안 몸을 따라가며 판정
        public void SetDuration(float time)
        {
            currentDuration = time;
        }

        // 지금 바로 판정 끝내기 (돌진이 벽에 막혀 일찍 멈췄을 때 등)
        //  - 따라가기도 멈춘다 → 남은 이펙트는 멈춘 자리에서 lifeTime까지 재생되고 풀로
        public void Finish()
        {
            if (gameObject.activeSelf == false) return; // 이미 풀로 돌아갔으면 무시
            currentDuration = Mathf.Max(0f, timer - currentDelay);
            isFollowing = false;
            followEffect = null; // 이펙트는 멈춘 자리에서 마저 재생
        }

        #endregion

        #region 내부 설정

        // 폭발 이펙트 (따라가기형은 같이 옮긴다)
        // 첫 판정 순간 내 위치 / 방향으로 폭발 이펙트를 띄운다 (돌진 이펙트처럼 방향이 있는 이펙트도 진행 방향을 보게)
        private void PlayExplodeEffect()
        {
            if (explodeEffect == null) return;

            ParticleEffect effect = ObjectPoolManager.Instance.GetObjectOrNull(explodeEffect.gameObject) as ParticleEffect;
            if (effect == null) return;

            effect.transform.SetPositionAndRotation(transform.position, transform.rotation);
            effect.gameObject.SetActive(true);

            if (isFollowing) followEffect = effect; // 따라가는 동안 같이 옮긴다
        }

        // 따라가는 중이면 폭발 이펙트를 내 위치로 (이펙트가 먼저 끝나 풀로 돌아갔으면 놓는다)
        private void MoveFollowEffect()
        {
            if (followEffect == null) return;

            if (followEffect.gameObject.activeSelf == false)
            {
                followEffect = null;
                return;
            }

            followEffect.transform.position = transform.position;
        }

        // 반경은 판정 반경(currentHitRadius)과 같게, 진행도는 생기고 지난 시간 / delay. 첫 판정 뒤에는 끈다
        private void UpdateWarning()
        {
            if (warningCircle == null) return;

            bool isWarning = timer < currentDelay;
            if (warningCircle.gameObject.activeSelf != isWarning) warningCircle.gameObject.SetActive(isWarning);
            if (isWarning == false) return;

            float scale = warningCircle.transform.lossyScale.x;            // 범위 증가로 커진 크기를 빼고 넣어야 판정과 크기가 같다
            warningCircle.Radius = (scale > 0f) ? currentHitRadius / scale : currentHitRadius;
            warningCircle.FillProgress = timer / currentDelay;
        }

        #endregion
    }
}
