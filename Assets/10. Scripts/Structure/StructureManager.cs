using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

// ===== 새로 추가 : 파일 전체 (구조물 소환 / 상호작용) =====
namespace PROJECT3D
{
    // 구조물 관리
    //  1. 소환 : [변경] 종류마다 자기 주기(표의 SpawnInterval)로 맵의 걸을 수 있는 칸 중 랜덤 자리에 하나
    //           종류별 최대 수(표의 MaxCount)와 맵 전체 최대 수(maxCount)를 둘 다 넘지 않을 때만 소환
    //  2. 상호작용 : 구조물마다 정한 상호작용 거리(Structure.InteractRange) 안의 가장 가까운 구조물에서
    //           F키를 누르고 있는 동안 게이지가 차고, 다 차면 구조물 효과 발동 (떼면 게이지 0)
    //  - 게이지는 StructureGaugeUI (Resources 프리팹, 자기 캔버스 포함)에 표시
    //  - GameManager가 게임 시작 때 StartSpawn을 부른다 (씬에 없으면 GameManager가 만든다)
    public class StructureManager : MonoBehaviour
    {
        private const string GaugeUIKey = "PROJECT3D/Prefabs/UI/StructureGauge"; // 게이지 UI 프리팹 (Resources)

        [Header("소환")]
        [SerializeField] private int maxCount = 4;               // [변경] 3 → 4 : 맵 전체에 동시에 있을 수 있는 구조물 수 (모든 종류 합)
        [SerializeField] private float firstSpawnDelay = 5f;     // 게임 시작 후 첫 소환까지(초) // [변경] 소환 간격은 표의 SpawnInterval (종류마다)
        [SerializeField] private float minPlayerDistance = 10f;  // 플레이어 바로 옆에는 안 나오게
        [SerializeField] private float minStructureGap = 10f;    // 구조물끼리 최소 간격
        [SerializeField] private int maxTryCount = 30;           // 자리 찾기 최대 시도 횟수

        [Header("상호작용")]
        [SerializeField] private float interactTime = 1.5f;      // F키를 이만큼(초) 누르고 있어야 발동

        private readonly List<StructureDef> spawnableDefs = new List<StructureDef>(); // 소환할 수 있는 구조물 (프리팹 있는 줄)
        private readonly List<Structure> spawned = new List<Structure>();             // 내가 소환한 구조물 (올라오는 / 사라지는 중 포함)
        private readonly List<float> spawnTimers = new List<float>(); // [변경] 종류마다 다음 소환까지 남은 시간 (spawnableDefs와 같은 순서)
        private StructureGaugeUI gaugeUI;
        private bool isRunning;

        private Structure interactTarget; // 지금 게이지를 채우는 구조물
        private float holdTime;           // F키를 누르고 있던 시간

        #region UnityMethod

        private void Update()
        {
            if (isRunning == false || LocalPlayer.Instance == null) return;
            if (WorldClock.Instance.IsPaused) return; // 일시정지 중에는 소환 / 게이지 멈춤

            float deltaTime = WorldClock.Instance.DeltaTime;
            UpdateSpawn(deltaTime);
            UpdateInteract(LocalPlayer.Instance, deltaTime);
        }

        #endregion

        #region 외부 설정

        public void StartSpawn()
        {
            spawnableDefs.Clear();
            spawnTimers.Clear(); // [변경]
            StructureTable table = TableLoader.Instance.StructureTable;
            for (int i = 0; i < table.Count; i++)
            {
                StructureDef def = table.Get(i);
                if (def.Prefab == null || def.MaxCount <= 0) continue; // 보스처럼 프리팹이 없거나 최대 수가 0인 줄은 건너뛴다 // [변경]
                spawnableDefs.Add(def);
                spawnTimers.Add(firstSpawnDelay); // [변경] 모든 종류가 첫 소환은 firstSpawnDelay 뒤에
            }

            if (gaugeUI == null)
            {
                StructureGaugeUI prefab = Resources.Load<StructureGaugeUI>(GaugeUIKey);
                if (prefab != null) gaugeUI = Instantiate(prefab);
                else Debug.LogWarning($"[StructureManager] {GaugeUIKey} 경로에 게이지 UI 없음");
            }
            if (gaugeUI != null) gaugeUI.Hide();

            spawned.Clear();
            interactTarget = null;
            holdTime = 0f;
            isRunning = true;
            Debug.Log($"[StructureManager] 소환할 수 있는 구조물 {spawnableDefs.Count}종");
        }

        #endregion

        #region 내부 설정

        #region 소환

        private void UpdateSpawn(float deltaTime)
        {
            // 풀로 돌아간(꺼진) 구조물은 목록에서 뺀다
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i].gameObject.activeSelf == false) spawned.RemoveAt(i);
            }

            // [변경] 종류마다 자기 주기로 (주기가 되면 최대 수를 넘지 않을 때만 하나)
            for (int i = 0; i < spawnableDefs.Count; i++)
            {
                spawnTimers[i] -= deltaTime;
                if (spawnTimers[i] > 0f) continue;
                spawnTimers[i] = spawnableDefs[i].SpawnInterval;

                if (spawned.Count >= maxCount) continue;                              // 맵 전체 최대 수
                if (CountSpawned(spawnableDefs[i]) >= spawnableDefs[i].MaxCount) continue; // 이 종류의 최대 수
                Spawn(spawnableDefs[i]);
            }
        }

        // [변경] SpawnRandom → Spawn(def) : 종류를 정해서 소환
        private void Spawn(StructureDef def)
        {
            if (TryFindSpawnPosition(out Vector3 position) == false) return; // 자리를 못 찾으면 다음 주기에

            Structure structure = ObjectPoolManager.Instance.GetObjectOrNull(def.Prefab.gameObject) as Structure;
            if (structure == null) return;

            structure.gameObject.SetActive(true);
            structure.Spawn(def, position); // 켠 뒤에 연출 시작 (꺼진 상태에서는 DoTween이 안 돈다)
            spawned.Add(structure);
        }

        // 맵 전체에서 걸을 수 있는 칸을 랜덤으로 (플레이어 / 다른 구조물과 너무 가까우면 다시)
        private bool TryFindSpawnPosition(out Vector3 position)
        {
            SquareGrid grid = MapGrid.Instance.Grid;
            Vector3 playerPosition = LocalPlayer.Instance.transform.position;

            for (int i = 0; i < maxTryCount; i++)
            {
                Vector2Int coord = new Vector2Int(Random.Range(0, grid.Width), Random.Range(0, grid.Depth));
                Tile tile = grid.GetTileOrNull(coord);
                if (tile == null || tile.IsWalkable == false) continue;

                Vector3 candidate = grid.CellToWorld(coord);
                candidate.y = tile.GroundHeight;

                if (FlatDistanceSqr(candidate, playerPosition) < minPlayerDistance * minPlayerDistance) continue;
                if (IsNearOtherStructure(candidate)) continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        // 새로 추가 : 맵에 있는 이 종류 구조물 수 (올라오는 / 사라지는 중 포함)
        private int CountSpawned(StructureDef def)
        {
            int count = 0;
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i].Def == def) count++;
            }
            return count;
        }

        private bool IsNearOtherStructure(Vector3 position)
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (FlatDistanceSqr(spawned[i].transform.position, position) < minStructureGap * minStructureGap) return true;
            }
            return false;
        }

        #endregion

        #region 상호작용

        private void UpdateInteract(Player player, float deltaTime)
        {
            if (player.Stat.IsDead)
            {
                ResetInteract();
                return;
            }

            Structure nearest = FindNearestStructure(player.transform.position); // [변경] 플레이어 획득 범위 → 구조물마다 정한 상호작용 거리
            if (nearest != interactTarget)
            {
                interactTarget = nearest; // 다른 구조물로 바뀌면 처음부터
                holdTime = 0f;
            }

            if (interactTarget == null)
            {
                if (gaugeUI != null) gaugeUI.Hide();
                return;
            }

            // 누르고 있는 동안만 차고, 떼면 처음부터
            if (SimpleInput.GetKey(Key.F)) holdTime += deltaTime;
            else holdTime = 0f;

            if (gaugeUI != null) gaugeUI.Show(interactTarget.Def.Name, holdTime / interactTime);

            if (holdTime < interactTime) return;

            interactTarget.Activate(player);
            ResetInteract();
        }

        private void ResetInteract()
        {
            interactTarget = null;
            holdTime = 0f;
            if (gaugeUI != null) gaugeUI.Hide();
        }

        // [변경] 각 구조물의 상호작용 거리(InteractRange, 수평) 안에 들어온 것 중 가장 가까운 구조물
        private Structure FindNearestStructure(Vector3 position)
        {
            Structure nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < Structure.Actives.Count; i++)
            {
                Structure structure = Structure.Actives[i];
                float distance = FlatDistanceSqr(structure.transform.position, position);
                float range = structure.InteractRange;
                if (distance > range * range) continue;    // 이 구조물의 상호작용 거리 밖
                if (distance >= nearestDistance) continue; // 더 가까운 구조물이 이미 있음

                nearestDistance = distance;
                nearest = Structure.Actives[i];
            }
            return nearest;
        }

        #endregion

        #region 도우미

        // 높이를 뺀 거리의 제곱
        private float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        #endregion

        #endregion
    }
}
// ===== 새로 추가 끝 =====
