using UnityEngine;

namespace PROJECT3D
{
    // 맞은 몹 정지 (스턴)
    public class StunOnHit : MonoBehaviour, IHitEffect
    {
        private float stunTime;     // 한 번 멈추는 시간 (0이면 정지 효과 없음)
        private int stunCount;      // 몇 번 멈추는지
        private float stunInterval; // 멈춤 사이 간격

        #region 외부 설정

        // FireHit 뒤에 부른다
        public void Set(float time, int count, float interval)
        {
            stunTime = time;
            stunCount = count;
            stunInterval = interval;
        }

        public void ResetEffect()
        {
            stunTime = 0f; // 정지 효과는 쓰는 공격만 Set으로 켜기
        }

        public void Apply(CombatEntity target)
        {
            if (stunTime <= 0f) return;
            if (target is Enemy enemy && enemy.Controller != null)
                enemy.Controller.Stun(stunTime, stunCount, stunInterval);
        }

        #endregion
    }
}