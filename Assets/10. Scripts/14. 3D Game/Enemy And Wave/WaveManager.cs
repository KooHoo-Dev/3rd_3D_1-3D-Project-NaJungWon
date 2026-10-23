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
        
        // 자리가 부족해서 생성하지 못한 몹 (매 프레임 다시 시도) 이게 필요하다함
        private readonly List<EnemyDef> pendingEnemyDefs = new();
        // 이미 생성된 몹 목록 (스폰 겹침 검사에 사용. 나중에 몹이 죽으면 여기서도 제거)
        // 아마 나중에 지우고 풀링시스템으로 다시쓸듯?
        private readonly List<Enemy> spawnedEnemies = new();
        
        private void Start()
        {
            //test용 임시 코드
            mapGrid = MapGrid.Instance;
            flowField = FlowFieldManager.Instance.Field;
            enemyTable = TableLoader.Instance.EnemyTable;
            waveTable = TableLoader.Instance.WaveTable;
            
            TableLoader.Instance.LoadData();
            
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
            if (mapGrid == null || LocalPlayer.Instance == null) return;
            elapsed += Time.deltaTime;
            StartWave();
        }
        
        private void StartWave()
        {
            // 무한루프 방지용 임시 코드
            int index = 0;
            while (true)
            {
                if (index == 5) break;
                if (nextWaveIndex == waveDefs.Count) return;
                WaveDef def = waveDefs[nextWaveIndex];
                if (elapsed <= def.SpawnTimer) return;
                
                SpawnWave(def);
                nextWaveIndex++;
                index++;
            }
        }

        private void SpawnWave(WaveDef wave)
        {
            EnemyDef def = wave.EnemyDef;
            for (int i = 0; i < wave.Count; i++)
            {
                Enemy enemy = Instantiate(def.Enemy);
                TrySpawnPosition(enemy, out Vector3 spawnPosition);
                enemy.Init(def, flowField.GetDirection(spawnPosition));
                enemy.transform.position = spawnPosition;
            }
        }

        // 스폰 좌표 지정
        private bool TrySpawnPosition(Enemy enemy ,out Vector3 position)
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

                candidate.y = tile.GroundHeight; //바닥좌표 + 몹 y좌표 높이 넣어야함 + 다른 이동에서도 y수치를 GroundHeight + 몹y Offset넣어야할듯

                // 3. 이미 생성된 몹과 겹치는지 확인
                if (IsOverlappingEnemies(candidate))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }
        
        private bool IsOverlappingEnemies(Vector3 position)
        {
            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                Vector3 toOther = spawnedEnemies[i].transform.position - position;
                if (toOther.sqrMagnitude < minEnemyGap * minEnemyGap)
                    return true;
            }

            return false;
        }
    }
}