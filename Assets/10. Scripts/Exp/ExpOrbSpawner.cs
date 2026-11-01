using UnityEngine;

namespace PROJECT3D
{
    // 몹이 죽으면(Enemy.OnAnyDied) 그 자리에 경험치 구슬을 풀에서 꺼내 놓는다
    // 인게임 씬에 하나 두고 구슬 프리팹을 넣는다
    // 일정 확률(ExpManager.BigOrbChance)로 경험치가 많은 큰 구슬이 나온다
    // 공중몹이 죽으면 공중에서 생겨서 그 칸의 바닥까지 떨어진다
    public class ExpOrbSpawner : MonoBehaviour
    {
        [SerializeField] private ExpOrb orbPrefab;        // 기본 구슬
        [SerializeField] private ExpOrb bigOrbPrefab;     // 큰 구슬 (색/크기만 다른 프리팹)
        [SerializeField] private float bigOrbMultiplier = 5f; // 큰 구슬 경험치 배수
        [SerializeField] private float spawnHeight = 0.5f; // [변경] 떨어진 뒤 바닥에서 띄울 높이

        private void OnEnable()
        {
            Enemy.OnAnyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            Enemy.OnAnyDied -= HandleEnemyDied;
        }

        private void HandleEnemyDied(Enemy enemy)
        {
            if (enemy.Exp <= 0) return; // 경험치 없는 몹 (더미 등)

            bool isBig = Random.value < ExpManager.Instance.GetBigOrbChance();

            ExpOrb prefab = orbPrefab;
            float exp = enemy.Exp;
            if (isBig && bigOrbPrefab != null) // 큰 구슬 프리팹을 아직 안 넣었으면 기본 구슬로
            {
                prefab = bigOrbPrefab;
                exp = enemy.Exp * bigOrbMultiplier;
            }

            IObjectPoolItem item = ObjectPoolManager.Instance.GetObjectOrNull(prefab.gameObject);
            if (item == null) return;

            ExpOrb orb = item as ExpOrb;
            Vector3 position = enemy.transform.position;
            orb.transform.position = position; // [변경] 몹이 죽은 자리에서 생긴다 (공중몹이면 공중)
            orb.Init(exp, GetLandY(position)); // [변경] 떨어질 높이도 넘긴다
            orb.gameObject.SetActive(true);
        }

        // 떨어질 높이 = 이 칸의 바닥 높이 + spawnHeight
        // 지금 위치보다 낮을 때만 떨어지고, 맵 밖이면 그대로
        private float GetLandY(Vector3 position)
        {
            Tile tile = MapGrid.Instance.Grid.GetTileOrNull(position);
            if (tile == null) return position.y;

            float landY = tile.GroundHeight + spawnHeight;
            if (landY < position.y) return landY;
            return position.y;
        }
    }
}