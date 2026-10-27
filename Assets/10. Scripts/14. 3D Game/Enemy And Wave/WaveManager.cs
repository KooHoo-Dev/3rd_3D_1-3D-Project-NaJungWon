using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace PROJECT3D
{
    public class WaveManager : SingletonBase<WaveManager>
    {
        [Header("Settings")]
        //private readonly MapGrid mapGrid = MapGrid.Instance;
        //private readonly FlowField flowField = FlowFieldManager.Instance.Field;
        //private readonly EnemyTable enemyTable = TableLoader.Instance.EnemyTable;
        //private readonly WaveTable waveTable = TableLoader.Instance.WaveTable;
        public MapGrid mapGrid;
        public FlowField flowField;
        public EnemyTable enemyTable;
        public WaveTable waveTable;

        
        [Header("스폰 웨이브 설정")]
        private readonly List<WaveDef> waveDefs = new(); // ID 순서대로 정렬된 웨이브 리스트
        private int nextWaveIndex = 0;                   // 다음에 시작할 웨이브 번호
        [SerializeField] private float elapsed;          // 웨이브 시작 후 누적 시간(초)
        
        [Header("스폰 위치 설정")] 
        [SerializeField] private float minSpawnDistance = 8f; // 플레이어와의 최소거리
        [SerializeField] private float maxSpawnDistance = 14f; // 플레이어와의 최대거리
        [SerializeField] private float minEnemyGap = 2f; // 몹끼리 최소 간격
        [SerializeField] private int maxTryCount = 20; //최대 시도 횟수
        
        //private float Timer = WorldClock.IsInitialized? WorldClock.Instance.DeltaTime : Time.deltaTime;
        
        public bool IsWaveActive { get; private set; }

        private bool spawnFinished;
        
        // 자리가 부족해서 생성하지 못한 몹 (매 프레임 다시 시도) 이게 필요하다함.
        private readonly List<EnemyDef> pendingEnemyDefs = new();
        
        private List<Enemy> nearEnemies = new List<Enemy>(); // 겹침 검사용 (재사용)
        
        // 테이블/맵/흐름장이 준비된 뒤 GameManager가 호출
        public void Init(MapGrid map, FlowField field)
        {
            mapGrid = map;
            flowField = field;
            enemyTable = TableLoader.Instance.EnemyTable;
            waveTable = TableLoader.Instance.WaveTable;

            waveDefs.Clear();
            for (int i = 0; i < waveTable.Count; i++)
                waveDefs.Add(waveTable.Get(i));
            
            for (int i = 0; i < waveDefs.Count; i++)
            {
                Debug.Log($"waveDefs{i}번 정보 : " +
                          $"ID : {waveDefs[i].ID}, " +
                          $"스폰 시간 : {waveDefs[i].SpawnTimer}," +
                          $"스폰 몬스터 : {waveDefs[i].EnemyDef.Name}," +
                          $"스폰 수 : {waveDefs[i].Count}");
            }
        }
        
        private void Update()
        {
            if (IsWaveActive == false || LocalPlayer.Instance == null) return;
            elapsed += WorldClock.Instance.DeltaTime;
            StartWave();
        }
        
        private void StartWave()
        {
            while (true)
            {
                if (nextWaveIndex == waveDefs.Count) return;
                WaveDef def = waveDefs[nextWaveIndex];
                if (elapsed <= def.SpawnTimer) return;
                
                SpawnWave(def);
                nextWaveIndex++;
            }
        }
        
        public void StartWaves()
        {
            elapsed = 0f;
            nextWaveIndex = 0;
            IsWaveActive = true;
        }

        private void SpawnWave(WaveDef wave)
        {
            EnemyDef def = wave.EnemyDef;
            for (int i = 0; i < wave.Count; i++)
            {
                // 자리를 못 찾으면 이번 몹은 건너뛴다 (원래는 (0,0,0)에 생성되던 문제)
                if (TrySpawnPosition(out Vector3 spawnPosition) == false) continue;

                // 풀에서 꺼내기 → 위치/데이터 넣기 → 켜기
                Enemy enemy = ObjectPoolManager.Instance.GetObjectOrNull(def.Enemy.gameObject) as Enemy;
                if (enemy == null) Debug.LogWarning("[WaveManager] 생성하려는 enemy가 존재하지 않습니다.");

                enemy.transform.position = spawnPosition;
                enemy.Init(def);
                enemy.gameObject.SetActive(true);
            }
        }

        // 스폰 좌표 지정
        private bool TrySpawnPosition(out Vector3 position)
        {
            for (int i = 0; i < maxTryCount; i++)
            {
                // 1. 플레이어 주변 일정 범위에서 후보 위치를 뽑기
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
                
                // 멀티가 아닌 싱글기준코드라 수정해야할듯
                Vector3 candidate = LocalPlayer.Instance.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;

                // 2. 이동 가능한 칸인지 확인
                Tile tile = mapGrid.Grid.GetTileOrNull(candidate);
                if (tile == null || tile.IsWalkable == false) continue;

                candidate.y = tile.GroundHeight;

                // 3. 이미 생성된 몹과 겹치는지 확인
                if (IsOverlappingEnemies(candidate))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }
        
        // 주변에 살아 있는 몹이 있는지 (공간 격자 명부로 주변 칸만 검사)
        private bool IsOverlappingEnemies(Vector3 position)
        {
            EnemyRegistry.FindInRange(position, minEnemyGap, ref nearEnemies);
            return nearEnemies.Count > 0;
        }
    }
}