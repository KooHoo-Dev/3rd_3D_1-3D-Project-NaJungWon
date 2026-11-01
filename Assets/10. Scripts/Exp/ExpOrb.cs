using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 경험치 구슬 (풀에서 꺼내 쓴다)
    // 1. 바닥에 가만히 있다가 플레이어 획득 반경 안에 들어오면 그 플레이어를 따라간다
    // 2. 점점 빨라지면서 따라가고, 충분히 가까워지면(sqrMagnitude) 경험치를 주고 풀로 돌아간다
    // 3. 자석 구조물은 AttractAll로 켜져 있는 구슬을 한 번에 끌어당긴다
    public class ExpOrb : MonoBehaviour, IObjectPoolItem
    {
        // 지금 켜져 있는 구슬 목록 (자석용)
        private static readonly List<ExpOrb> activeOrbs = new List<ExpOrb>();

        [SerializeField] private float startSpeed = 5f;     // 빨려 들어가기 시작할 때 속도
        [SerializeField] private float acceleration = 30f;  // 초당 속도 증가량 (점점 빨라져서 결국 따라잡는다)
        [SerializeField] private float collectHeight = 1f;  // 플레이어 발밑이 아니라 몸통 쪽으로 빨려 들어가게
        private const float CollectDistance = 0.1f;         // 이 거리(sqrMagnitude)보다 가까우면 획득

        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        private float exp;     // 이 구슬의 경험치
        private Player target; // 따라가는 플레이어 (null = 바닥에 가만히 있음)
        private float speed;

        [SerializeField] private float fallSpeed = 10f; // 새로 추가 : 바닥까지 떨어지는 속도
        private float landY;                            // 새로 추가 : 떨어져서 멈출 높이

        // 풀에서 꺼낸 직후 호출
        public void Init(float exp, float landY) // [변경] landY 추가
        {
            this.exp = exp;
            this.landY = landY; // 새로 추가
            target = null;
            speed = startSpeed;
        }

        private void OnEnable()
        {
            activeOrbs.Add(this);
        }

        private void OnDisable()
        {
            activeOrbs.Remove(this);
        }

        private void Update()
        {
            if (WorldClock.Instance.IsPaused) return;

            // 따라가던 플레이어가 죽었거나 사라졌으면 다시 바닥에 둔다
            if (target != null && target.Stat.IsDead) target = null;

            if (target == null)
            {
                Fall(); // 새로 추가 : 따라가기 전에는 바닥까지 떨어진다
                target = FindPlayerInRange();
                if (target == null) return;
            }

            MoveToTarget();
        }

        // 새로 추가 : landY까지 일정 속도로 떨어지기 (이미 바닥이면 그대로)
        private void Fall()
        {
            Vector3 position = transform.position;
            position.y = Mathf.MoveTowards(position.y, landY, fallSpeed * WorldClock.Instance.DeltaTime);
            transform.position = position;
        }

        // 획득 반경 안에 들어온 플레이어 찾기 (반경은 플레이어마다 다르다)
        private Player FindPlayerInRange()
        {
            for (int i = 0; i < PlayerRegistry.All.Count; i++)
            {
                Player player = PlayerRegistry.All[i];
                if (player.Stat.IsDead) continue;

                float range = player.GetPickupRange();
                float distance = (player.transform.position - transform.position).sqrMagnitude;
                if (distance <= range * range) return player;
            }
            return null;
        }

        private void MoveToTarget()
        {
            float delta = WorldClock.Instance.DeltaTime;
            speed += acceleration * delta;

            Vector3 goal = target.transform.position + Vector3.up * collectHeight;
            transform.position = Vector3.MoveTowards(transform.position, goal, speed * delta);

            if ((goal - transform.position).sqrMagnitude <= CollectDistance)
            {
                ExpManager.Instance.AddExp(exp, target);
                ReturnToPool();
            }
        }

        // 자석 : 아직 아무도 안 따라가는 구슬이면 이 플레이어를 따라가게 한다
        public void Attract(Player player)
        {
            if (target == null) target = player;
        }

        // 자석 구조물에서 호출 : 켜져 있는 구슬 전부 끌어당기기
        public static void AttractAll(Player player)
        {
            for (int i = 0; i < activeOrbs.Count; i++)
            {
                activeOrbs[i].Attract(player);
            }
        }

        public void ReturnToPool()
        {
            // 풀 없이 씬에 직접 놓은 구슬이라면 그냥 끈다
            if (ObjectPoolManager.Instance == null || string.IsNullOrEmpty(Key))
            {
                gameObject.SetActive(false);
                return;
            }

            ObjectPoolManager.Instance.ReturnToPool(this);
        }
    }
}