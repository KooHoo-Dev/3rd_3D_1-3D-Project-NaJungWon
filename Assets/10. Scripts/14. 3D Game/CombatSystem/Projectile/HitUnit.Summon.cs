using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 지정 위치 소환 : 정한 위치(from)에 생기고 움직이지 않는다 (direction은 쓰지 않음)
    //  - delay 뒤에 범위 안 전부를 때린다
    //  - duration = 0 : 한 번만 때리고 사라짐
    //  - duration > 0 : 그 시간 동안 tickInterval마다 때림
    //  - 플레이어를 따라다니게 하려면 소환 후 플레이어 자식으로 붙이면 된다 (오라)
    public class SummonHit : HitUnit
    {
        [Header("소환 설정")]
        [SerializeField] private float delay = 0f;          // 생기고 나서 첫 판정까지 (떨어지는 연출 시간 등)
        [SerializeField] private float duration = 0f;       // 지속시간 (지속시간 증가 적용). 0 = 한 번만
        [SerializeField] private float tickInterval = 0.5f; // 지속형일 때 판정 간격

        private float currentDuration;
        private float timer;       // 생기고 나서 지난 시간
        private float nextHitTime; // 다음 판정 시각

        public override void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            base.Launch(from, direction, damage, owner);
            currentDuration = duration;
            timer = 0f;
            nextHitTime = delay;
        }

        public override void ApplyBonus(float rangeRate, float durationRate)
        {
            base.ApplyBonus(rangeRate, durationRate); // 반경
            currentDuration = duration * durationRate; // 지속시간
        }

        private void Update()
        {
            timer += WorldClock.Instance.DeltaTime;

            if (timer >= nextHitTime)
            {
                List<CombatEntity> targets = FindTouching();
                for (int i = 0; i < targets.Count; i++)
                    Hit(targets[i]);

                nextHitTime += tickInterval;
            }

            if (timer >= delay + currentDuration) ReturnToPool(); // 한 번형은 첫 판정 직후, 지속형은 시간이 다 되면
        }
    }
}