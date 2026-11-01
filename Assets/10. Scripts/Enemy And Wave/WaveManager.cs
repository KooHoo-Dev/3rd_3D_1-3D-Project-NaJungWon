using System.Collections.Generic;
using Jay;
using UnityEngine;

namespace PROJECT3D
{
    // 몹 소환 담당
    // - 상시 소환(spawnRules.tsv) : 구간 동안 일정 주기로 조금씩 계속. 몹이 적으면 바로 채움 (무한런의 기본 흐름)
    // - 이벤트 소환(waves.tsv)    : 정해진 시간에 한 번 몰아서 (떼거리, 나중에 보스)
    // 소환 시점의 시간에 맞춰 체력 / 공격력이 강해짐
    public class WaveManager : SingletonBase<WaveManager>
    {
        [Header("Settings")]
        public MapGrid mapGrid;

        [Header("이벤트 소환 (waves.tsv)")]
        private readonly List<WaveDef> waveDefs = new(); // ID 순서대로 정렬된 웨이브 리스트
        private int nextWaveIndex = 0;                   // 다음에 시작할 웨이브 번호
        private float elapsed;                           // 웨이브 시작 후 누적 시간(초)

        [Header("상시 소환 (spawnRules.tsv)")]
        [SerializeField] private int maxAliveEnemies = 800;           // 살아 있는 몹이 이 수 이상이면 더 만들지 않는다
        private readonly List<SpawnRuleDef> spawnRules = new();       // 상시 소환 규칙
        private float[] ruleTimers = new float[0];                    // 규칙마다 다음 소환까지 남은 시간 (규칙과 같은 순서)

        [Header("시간 비례 강화 (1분마다 곱해지는 비율)")]
        [SerializeField] private float hpGrowthPerMinute = 0.25f;     // 0.25 = 1분마다 체력 x1.25 (10분이면 약 9배)
        [SerializeField] private float damageGrowthPerMinute = 0.15f; // 0.15 = 1분마다 공격력 x1.15 (10분이면 약 4배)

        [Header("스폰 위치 설정")]
        [SerializeField] private float minSpawnDistance = 8f; // 플레이어와의 최소거리
        [SerializeField] private float maxSpawnDistance = 14f; // 플레이어와의 최대거리
        [SerializeField] private float minEnemyGap = 2f; // 몹끼리 최소 간격
        [SerializeField] private int maxTryCount = 20; //최대 시도 횟수

        public bool IsWaveActive { get; private set; }

        private List<Enemy> nearEnemies = new List<Enemy>(); // 겹침 검사용 (재사용)

        // 테이블/맵/흐름장이 준비된 뒤 GameManager가 호출
        public void Init(MapGrid map) // [변경] 안 쓰던 FlowField 매개변수 삭제
        {
            mapGrid = map;
            WaveTable waveTable = TableLoader.Instance.WaveTable; // [변경] 필드 → 지역 변수

            waveDefs.Clear();
            for (int i = 0; i < waveTable.Count; i++)
                waveDefs.Add(waveTable.Get(i));

            // 새로 추가 : 상시 소환 규칙
            SpawnRuleTable ruleTable = TableLoader.Instance.SpawnRuleTable;
            spawnRules.Clear();
            for (int i = 0; i < ruleTable.Count; i++)
                spawnRules.Add(ruleTable.Get(i));
            ruleTimers = new float[spawnRules.Count];

            Debug.Log($"[WaveManager] 이벤트 소환 {waveDefs.Count}개, 상시 소환 규칙 {spawnRules.Count}개");
        }

        private void Update()
        {
            if (IsWaveActive == false || LocalPlayer.Instance == null) return;

            float deltaTime = WorldClock.Instance.DeltaTime;
            if (deltaTime <= 0f) return; // 일시정지 중에는 소환 안 함

            elapsed += deltaTime;
            //UpdateSpawnRules(deltaTime); //상시 소환 - 테스트 중이라 임시로 막음
            StartWave();                 // 이벤트 소환
        }

        public void StartWaves()
        {
            elapsed = 0f;
            nextWaveIndex = 0;
            for (int i = 0; i < ruleTimers.Length; i++) ruleTimers[i] = 0f; // 새로 추가 : 시작하자마자 첫 소환
            IsWaveActive = true;
        }

        #region 이벤트 소환

        // 정해진 시간이 지난 웨이브를 한 번씩 소환 (끝까지 나오면 더 안 나온다)
        private void StartWave()
        {
            while (true)
            {
                if (nextWaveIndex == waveDefs.Count) return;
                WaveDef def = waveDefs[nextWaveIndex];
                if (elapsed <= def.SpawnTimer) return;

                SpawnEnemies(def.EnemyDef, def.Count);
                nextWaveIndex++;
            }
        }

        #endregion

        #region 상시 소환

        // 지금 시간에 해당하는 규칙만, 주기가 되면(또는 몹이 너무 적으면) 소환
        private void UpdateSpawnRules(float deltaTime)
        {
            for (int i = 0; i < spawnRules.Count; i++)
            {
                SpawnRuleDef rule = spawnRules[i];
                if (elapsed < rule.StartTime || elapsed >= rule.EndTime) continue; // 이 규칙의 구간이 아님

                ruleTimers[i] -= deltaTime;

                bool isTooFew = EnemyRegistry.AliveCount < rule.MinAlive; // 맵에 몹이 너무 적으면 주기를 안 기다리고 소환
                if (ruleTimers[i] > 0f && isTooFew == false) continue;

                ruleTimers[i] = rule.Interval;
                SpawnEnemies(rule.EnemyDef, rule.Count);
            }
        }

        #endregion

        #region 공용 소환
        
        private void SpawnEnemies(EnemyDef def, int count)
        {
            if (def == null) return;

            // 지금 시간 기준 강화 배율 (1분마다 복리로 곱해진다)
            float minutes = elapsed / 60f;
            float hpRate = Mathf.Pow(1f + hpGrowthPerMinute, minutes);
            float damageRate = Mathf.Pow(1f + damageGrowthPerMinute, minutes);

            for (int i = 0; i < count; i++)
            {
                if (EnemyRegistry.AliveCount >= maxAliveEnemies) return; // 몹이 너무 많으면 나머지는 생략

                // 자리를 못 찾으면 이번 몹은 건너뛰기 (원래는 (0,0,0)에 생성되던 문제)
                if (TrySpawnPosition(out Vector3 spawnPosition) == false) continue;

                // 풀에서 꺼내기 → 위치/데이터 넣기 → 켜기
                Enemy enemy = ObjectPoolManager.Instance.GetObjectOrNull(def.Enemy.gameObject) as Enemy;
                if (enemy == null)
                {
                    Debug.LogWarning("[WaveManager] 생성하려는 enemy가 존재하지 않습니다.");
                    continue;
                }

                enemy.transform.position = spawnPosition;
                enemy.Init(def, hpRate, damageRate); // 시간 비례 강화
                enemy.gameObject.SetActive(true);
            }
        }

        #endregion

        #region 스폰 위치

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

        #endregion
    }
}
