using UnityEngine;

namespace PROJECT3D
{
    // 몹이 죽을 때 일어나는 효과의 공통 부모
    // - Enemy가 죽는 순간 붙어 있는 효과를 실행
    public abstract class EnemyDeath : MonoBehaviour
    {
        protected Enemy Owner { get; private set; }

        #region UnityMethod

        protected virtual void Awake()
        {
            Owner = GetComponent<Enemy>();
        }

        #endregion

        #region 외부 설정

        // 죽은 순간 한 번 호출. 이 시점에는 아직 몹이 켜져 있다(위치 사용 가능).
        public abstract void Apply();

        #endregion
    }
}
