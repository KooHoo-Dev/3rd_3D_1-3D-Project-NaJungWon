using UnityEngine;

namespace PROJECT3D
{
    public class ParticleEffect : MonoBehaviour, IObjectPoolItem
    {
        [SerializeField] private ParticleSystem particle;
        [SerializeField] private float lifeTime = 0f;     // 0 = 파티클이 끝나면 귀환, 0보다 크면 이 시간 뒤 귀환

        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        private float remainTime; // 귀환까지 남은 시간 (시간 기준일 때만 사용)

        #region UnityMethod

        private void Awake()
        {
            // 인스펙터에서 안 넣었으면 자식에서 찾는다
            if (particle == null) particle = GetComponentInChildren<ParticleSystem>();
        }

        private void OnEnable()
        {
            remainTime = lifeTime;
        }

        private void Update()
        {
            // 시간 기준 : 시간이 다 되면 귀환
            if (remainTime > 0f)
            {
                remainTime -= WorldClock.Instance.DeltaTime;
                if (remainTime <= 0f) ReturnToPool();
                return;
            }

            // 파티클 기준 : 자식 파티클까지 전부 끝났으면 귀환
            // (IsAlive(true) = 자식 포함, 아직 화면에 남은 입자가 있으면 true)
            if (particle == null || particle.IsAlive(true) == false) ReturnToPool();
        }

        #endregion

        #region 외부 설정

        // 지속시간 직접 설정
        public void SetLifeTime(float time)
        {
            remainTime = time;
        }

        public void ReturnToPool()
        {
            // 풀 없이 씬에 직접 놓은 이펙트라면 그냥 끈다
            if (ObjectPoolManager.Instance == null || string.IsNullOrEmpty(Key))
            {
                gameObject.SetActive(false);
                return;
            }

            ObjectPoolManager.Instance.ReturnToPool(this);
        }

        #endregion
    }
}