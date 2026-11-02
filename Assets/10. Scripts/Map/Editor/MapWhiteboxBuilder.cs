using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PROJECT3D
{
    // 메뉴 한 번으로 산맥 화이트박스를 만든다. (산맥에 잊힌 용의 유적)
    // 지형 = 평지 + 경사 + 절벽(돌아가는 곳)만. 길을 막는 건 지형 위 오브젝트가 한다.
    // 지형 위 오브젝트는 두 종류
    //   1. 막는 장애물 (Obstacle) : 무너진 집, 부러진 벽, 기둥, 큰 바위, 나무 → 몹이 무조건 돌아서 추적
    //   2. 오르는 지형물 (Ground) : 무너진 단 + 잔해 경사로, 낮은 잔해 더미 → 몹도 타고 올라와서 추적
    // 지형과 랜드마크(레어, 신전, 망루)는 손으로 놓고, 나머지 오브젝트는 규칙을 지키며 랜덤으로 뿌린다.
    // 같은 시드면 항상 같은 배치가 나온다. 배치가 마음에 안 들면 RandomSeed만 바꾼다.
    // 좌표는 MapGrid가 (0,0,0), 200x200, 칸 1m 라는 기준이다.
    public static class MapWhiteboxBuilder
    {
        private const string RootName = "Map_DragonNest";
        private const string MaterialFolder = "Assets/00. 3D Game/Material/Whitebox";

        // 지형 규격
        private const float TopHeight = 20f;           // 2층(산 정상) 높이
        private const float SlopeRunPerHeight = 1.5f;  // 높이 1m당 경사 수평 길이 → 약 33.7도 (몹 한계 40도 아래)
        private const float SlopeFillMargin = 1f;      // 경사 아래를 채우는 두께에 더하는 여유

        // 오르는 지형물 규격
        private const float RampAngle = 30f;           // 잔해 경사로 각도 (몹 40도, 플레이어 45도 아래)
        private const float RampWidth = 4f;

        // 용의 레어 (맵 가운데 원형 벽, 나중에 보스 등장 장소)
        private const float LairCenterX = 100f;
        private const float LairCenterZ = 100f;
        private const float LairRadius = 18f;
        private const float LairWallHeight = 10f;      // 2단 점프(8m)로 넘지 못하는 높이
        private const int LairWallCount = 24;          // 15도마다 한 조각
        private const float LairClearDistance = 6f;    // 레어 벽 바깥으로 비워 두는 거리 (입구 주변 통로)

        // 랜덤 배치
        private const int RandomSeed = 1234;
        private const int WalkablePropCount = 16;      // 밟고 넘는 작은 돌 개수
        private const float ObjectSpacing = 3f;        // 오브젝트 사이 최소 간격 (몹이 지나갈 길)
        private const float PlayAreaMin = 12f;         // 외곽 벽 안쪽 배치 범위
        private const float PlayAreaMax = 188f;
        private const float PlayerStartX = 100f;
        private const float PlayerStartZ = 62f;
        private const float PlayerStartClear = 8f;     // 시작 지점 주변 비우기

        // 막는 장애물 종류 (Obstacle → 몹은 모두 돌아간다)
        // 높이 기준 : 1단 점프 약 3.5m, 2단 점프 약 7.5m
        private enum BlockType
        {
            // 플레이어만 올라가는 장애물 (지형 오브젝트와 50:50)
            RockCluster, // 큰 바위(9~12) + 옆 작은 바위(3~6.5) → 옆 바위를 밟고 꼭대기까지
            Foundation,  // 무너진 건물 터 (3m 바닥 + 6m 모서리)
            FlatRock,    // 넓고 평평한 바위 (3~3.5 또는 5~6.5)
            LowWall,     // 두꺼운 낮은 담 (2.5~3.5, 두께 2)

            // 플레이어도 돌아가는 장애물 (50:50 밖, 따로 적게)
            RuinTower,   // 무너진 탑 (높이 10~14)
            RuinHall,    // 두꺼운 ㄴ자 벽 건물 (높이 9~11)
            Pillar,      // 기둥 (높이 10)

            Tree,        // 나무 (얇고 김, 동쪽 숲에 많이)
        }

        // ===== 개수 조절 : 아래 숫자만 바꾼다 =====
        // 플레이어만 올라가는 장애물 수 = 지형 오브젝트(몹도 올라오는 무너진 단 + 잔해 더미) 수 → 50:50
        private const int ClimbPairCount = 30;
        private const float PlatformRatio = 0.6f;   // 지형 오브젝트 중 무너진 단 비율 (나머지는 잔해 더미)

        // 50:50 밖에서 따로 놓는 것
        private const int RuinTowerCount = 3;
        private const int RuinHallCount = 3;
        private const int PillarCount = 8;
        private const int TreeCount = 45;

        // 플레이어만 올라가는 장애물 종류 (큰 것부터)
        private static readonly BlockType[] ClimbObstacleTypes =
        {
            BlockType.RockCluster, BlockType.Foundation, BlockType.FlatRock, BlockType.LowWall,
        };

        // 플레이어만 올라가는 장애물 안에서의 비율 : 큰 것일수록 작게 (30개 기준 개수)
        private static float GetClimbObstacleWeight(BlockType type)
        {
            switch (type)
            {
                case BlockType.RockCluster: return 1f;  // 약 4개 (가장 큼)
                case BlockType.Foundation: return 1.5f; // 약 6개
                case BlockType.FlatRock: return 2f;     // 약 9개
                case BlockType.LowWall: return 2.5f;    // 약 11개 (가장 작음)
            }
            return 0f;
        }

        private static int groundLayer;
        private static int obstacleLayer;
        private static Material groundMaterial;
        private static Material slopeMaterial;
        private static Material obstacleMaterial;
        private static Material treeMaterial;
        private static Material climbMaterial;
        private static Material lairMaterial;
        private static Material terrainMaterial;      // 바닥, 고원, 언덕 (지형 머티리얼이 있으면 그것)
        private static Material terrainSlopeMaterial; // 산비탈 (지형 머티리얼이 있으면 그것)

        // 이미 놓은 오브젝트가 차지한 바닥 영역 (x, z 평면). 새 오브젝트가 겹치지 않게 검사한다.
        private static List<Rect> placedAreas = new List<Rect>();

        [MenuItem("PROJECT3D/Map/화이트박스 만들기")]
        public static void Build()
        {
            // 1. 레이어 확인
            groundLayer = LayerMask.NameToLayer("Ground");
            obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (groundLayer == -1 || obstacleLayer == -1)
            {
                Debug.LogError("Ground / Obstacle 레이어가 없습니다.");
                return;
            }

            // 2. MapGrid 기준점 확인 (값은 바꾸지 않고 알려주기만 한다)
            MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
            if (mapGrid == null)
                Debug.LogWarning("씬에 MapGrid가 없습니다.");
            else if (mapGrid.transform.position != Vector3.zero || mapGrid.width != 200 || mapGrid.depth != 200)
                Debug.LogWarning("MapGrid를 위치 (0,0,0), 200x200 으로 맞춰야 좌표가 맞습니다.", mapGrid);

            // 3. 머티리얼 준비
            groundMaterial = GetOrCreateMaterial("WB_Ground", new Color(0.6f, 0.6f, 0.6f));
            slopeMaterial = GetOrCreateMaterial("WB_Slope", new Color(0.9f, 0.8f, 0.2f));
            obstacleMaterial = GetOrCreateMaterial("WB_Obstacle", new Color(0.8f, 0.25f, 0.25f));
            treeMaterial = GetOrCreateMaterial("WB_Tree", new Color(0.25f, 0.6f, 0.3f));
            climbMaterial = GetOrCreateMaterial("WB_Climb", new Color(0.85f, 0.7f, 0.5f));
            lairMaterial = GetOrCreateMaterial("WB_Lair", new Color(0.35f, 0.2f, 0.45f));

            // 지형 텍스처 머티리얼이 만들어져 있으면 지형에 쓰고, 없으면 화이트박스 색
            Material terrain = AssetDatabase.LoadAssetAtPath<Material>(TerrainTextureGenerator.MaterialPath);
            terrainMaterial = (terrain != null) ? terrain : groundMaterial;
            terrainSlopeMaterial = (terrain != null) ? terrain : slopeMaterial;

            // 4. 이전 화이트박스를 지우고 새로 만든다 (Ctrl+Z로 되돌릴 수 있음)
            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Whitebox");

            Transform ground = CreateGroup("Ground", root.transform);
            Transform boundary = CreateGroup("Boundary", root.transform);
            Transform lair = CreateGroup("Lair", root.transform);
            Transform landmarks = CreateGroup("Landmarks", root.transform);
            Transform climbs = CreateGroup("Obstacles_Climb", root.transform);
            Transform blocks = CreateGroup("Obstacles_Block", root.transform);
            Transform walkableProps = CreateGroup("Props_Walkable", root.transform);
            Transform markers = CreateGroup("Markers", root.transform);

            placedAreas.Clear();
            Random.InitState(RandomSeed);

            // 5. 지형 → 손으로 놓는 것 → 랜덤 배치 순서 (뒤에 만드는 것은 앞의 것 위에 올라가고, 겹치지 않게 피한다)
            BuildFloorAndWalls(ground, boundary);
            BuildTops(ground);
            BuildSlopes(ground);
            BuildHills(ground);

            BuildLair(lair);
            BuildLandmarks(landmarks);

            ScatterBlockType(BlockType.RuinTower, blocks, RuinTowerCount); // 플레이어도 돌아가는 큰 것 (적게)
            ScatterBlockType(BlockType.RuinHall, blocks, RuinHallCount);
            ScatterClimbPairs(blocks, climbs);                             // 플레이어만 오르는 장애물 : 지형 오브젝트 = 50:50
            ScatterBlockType(BlockType.Pillar, blocks, PillarCount);
            ScatterBlockType(BlockType.Tree, blocks, TreeCount);
            ScatterWalkableProps(walkableProps);
            BuildMarkers(markers);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"화이트박스 생성 완료 : 막는 장애물 {blocks.childCount}개, 오르는 지형물 {climbs.childCount}개 (경사로 포함)");
        }

        #region 배치 데이터 : 지형

        private static void BuildFloorAndWalls(Transform ground, Transform boundary)
        {
            CreateBox("Floor_1F", ground, new Vector3(100f, -0.5f, 100f), new Vector3(200f, 1f, 200f), groundLayer, terrainMaterial);

            // 외곽 벽 : 정상 20m + 2단 점프 8m 보다 높게 (꼭대기 40)
            CreateBox("Wall_W", boundary, new Vector3(5f, 20f, 100f), new Vector3(10f, 40f, 200f), obstacleLayer, obstacleMaterial);
            CreateBox("Wall_E", boundary, new Vector3(195f, 20f, 100f), new Vector3(10f, 40f, 200f), obstacleLayer, obstacleMaterial);
            CreateBox("Wall_S", boundary, new Vector3(100f, 20f, 5f), new Vector3(200f, 40f, 10f), obstacleLayer, obstacleMaterial);
            CreateBox("Wall_N", boundary, new Vector3(100f, 20f, 195f), new Vector3(200f, 40f, 10f), obstacleLayer, obstacleMaterial);
        }

        private static void BuildTops(Transform ground)
        {
            // 범위 (x 최소, x 최대, z 최소, z 최대, 높이). 모두 외곽 벽에 붙어 있다.
            CreateTop("Top_North", ground, 10f, 190f, 150f, 190f, TopHeight);
            CreateTop("Top_SW", ground, 10f, 50f, 10f, 50f, TopHeight);
            CreateTop("Top_SE", ground, 150f, 190f, 10f, 50f, TopHeight);
        }

        private static void BuildSlopes(Transform ground)
        {
            // 고원 모서리 위 경사 윗변 가운데 점(x, z), 경사 폭, 높이, 내려가는 방향
            // 경사 폭 = 그 고원 면의 너비 → 다리가 아니라 산비탈
            CreateHillSlope("Slope_North_W", ground, 45f, 150f, 70f, TopHeight, Vector3.back);   // x 10~80
            CreateHillSlope("Slope_North_E", ground, 155f, 150f, 70f, TopHeight, Vector3.back);  // x 120~190 (가운데 x 80~120은 절벽)
            CreateHillSlope("Slope_SW", ground, 50f, 30f, 40f, TopHeight, Vector3.right);        // 남서 봉우리 동쪽 면 전체
            CreateHillSlope("Slope_SE", ground, 150f, 30f, 40f, TopHeight, Vector3.left);        // 남동 봉우리 서쪽 면 전체
        }

        // 1층 평원 위 언덕 (높이 6~10m). 경사 폭 = 언덕 면 너비, 경사가 없는 면은 절벽(돌아가는 곳)
        private static void BuildHills(Transform ground)
        {
            // 서쪽 언덕 (10m) : 동쪽, 남쪽 면이 산비탈
            CreateTop("Hill_W", ground, 20f, 45f, 78f, 100f, 10f);
            CreateHillSlope("Hill_W_SlopeE", ground, 45f, 89f, 22f, 10f, Vector3.right);   // x 45~60
            CreateHillSlope("Hill_W_SlopeS", ground, 32.5f, 78f, 25f, 10f, Vector3.back);  // z 63~78

            // 동쪽 언덕 (10m) : 서쪽, 남쪽 면이 산비탈
            CreateTop("Hill_E", ground, 158f, 182f, 88f, 108f, 10f);
            CreateHillSlope("Hill_E_SlopeW", ground, 158f, 98f, 20f, 10f, Vector3.left);   // x 143~158
            CreateHillSlope("Hill_E_SlopeS", ground, 170f, 88f, 24f, 10f, Vector3.back);   // z 73~88

            // 남쪽 계곡 언덕 (6m) : 북쪽 면이 산비탈
            CreateTop("Hill_S", ground, 88f, 112f, 14f, 34f, 6f);
            CreateHillSlope("Hill_S_SlopeN", ground, 100f, 34f, 24f, 6f, Vector3.forward); // z 34~43

            // 남동 능선 (6m) : 남, 북 양쪽 면이 산비탈
            CreateTop("Ridge_SE", ground, 128f, 150f, 60f, 66f, 6f);
            CreateHillSlope("Ridge_SE_SlopeS", ground, 139f, 60f, 22f, 6f, Vector3.back);    // z 51~60
            CreateHillSlope("Ridge_SE_SlopeN", ground, 139f, 66f, 22f, 6f, Vector3.forward); // z 66~75

            // 서쪽 능선 (6m) : 남, 북 양쪽 면이 산비탈. 레어 서문과 8m 떨어져 있다.
            CreateTop("Ridge_W", ground, 61f, 75f, 100f, 106f, 6f);
            CreateHillSlope("Ridge_W_SlopeS", ground, 68f, 100f, 14f, 6f, Vector3.back);     // z 91~100
            CreateHillSlope("Ridge_W_SlopeN", ground, 68f, 106f, 14f, 6f, Vector3.forward);  // z 106~115
        }

        #endregion

        #region 배치 데이터 : 용의 레어 / 랜드마크

        private static void BuildLair(Transform lair)
        {
            // 가운데 둥지 단 (0.9m → 몹도 그냥 올라온다). 나중에 보스가 여기서 나온다.
            CreateRuinPlatform("Lair_Dais", lair, LairCenterX, LairCenterZ, 8f, 8f, 0.9f, Vector3.zero);

            // 둘레 벽 : 24조각 중 동(0) 북(6) 서(12) 남(18) 조각을 비워서 입구 4개 (폭 약 4.5m)
            for (int i = 0; i < LairWallCount; i++)
            {
                if (i % 6 == 0) continue;

                float angle = i * 360f / LairWallCount * Mathf.Deg2Rad;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 position = new Vector3(LairCenterX, LairWallHeight * 0.5f, LairCenterZ) + outward * LairRadius;

                // 조각 폭 5 (원 둘레 한 칸 4.7 보다 살짝 넓게 → 틈 없이 이어짐)
                GameObject wall = CreateBox("Lair_Wall_" + i.ToString("00"), lair, position, new Vector3(5f, LairWallHeight, 2f), obstacleLayer, lairMaterial);
                wall.transform.rotation = Quaternion.LookRotation(outward); // 앞(z)을 바깥으로 → 폭(x)이 원을 따라 놓인다
            }

            // 안쪽 무너진 기둥 4개 (대각선)
            CreatePillar("Lair_Pillar_01", lair, 108.5f, 108.5f, 6f);
            CreatePillar("Lair_Pillar_02", lair, 91.5f, 108.5f, 6f);
            CreatePillar("Lair_Pillar_03", lair, 91.5f, 91.5f, 6f);
            CreatePillar("Lair_Pillar_04", lair, 108.5f, 91.5f, 6f);
        }

        // 손으로 놓는 랜드마크 (랜덤 배치보다 먼저 놓아서 자리를 차지한다)
        private static void BuildLandmarks(Transform landmarks)
        {
            // 레어 북문 ~ 북쪽 절벽 사이 기둥 길
            CreatePillar("Colonnade_01", landmarks, 92f, 126f, 10f);
            CreatePillar("Colonnade_02", landmarks, 108f, 126f, 10f);
            CreatePillar("Colonnade_03", landmarks, 92f, 134f, 4f);
            CreatePillar("Colonnade_04", landmarks, 108f, 134f, 4f);
            CreatePillar("Colonnade_05", landmarks, 92f, 142f, 10f);
            CreatePillar("Colonnade_06", landmarks, 108f, 142f, 4f);

            // 북쪽 정상 잊힌 신전 (단 + 기둥 + 앞 벽)
            CreateRuinPlatform("Temple_N1", landmarks, 100f, 176f, 16f, 12f, 3f, Vector3.back, 6f);
            CreatePillar("Temple_Pillar1", landmarks, 94f, 180f, 6f);
            CreatePillar("Temple_Pillar2", landmarks, 106f, 180f, 6f);
            CreateObstacleBox("Temple_FrontWall", landmarks, 100f, 160f, new Vector3(14f, 4f, 2f), 0f);

            // 남서 / 남동 봉우리 망루
            CreateRuinPlatform("Tower_SW", landmarks, 20f, 40f, 8f, 8f, 4f, Vector3.right);
            CreateRuinPlatform("Tower_SE", landmarks, 180f, 40f, 8f, 8f, 4f, Vector3.left);
        }

        private static void BuildMarkers(Transform markers)
        {
            // 나중에 코드에서 쓸 위치 표시 (빈 오브젝트)
            CreateMarker("PlayerStart", markers, PlayerStartX, PlayerStartZ);
            CreateMarker("BossSpawnPoint", markers, LairCenterX, LairCenterZ); // 레어 둥지 단 위
            CreateMarker("NestPoint_N", markers, 100f, 176f);                  // 신전 단 위
            CreateMarker("NestPoint_SW", markers, 20f, 40f);                   // 망루 위
            CreateMarker("NestPoint_SE", markers, 180f, 40f);
        }

        #endregion

        #region 랜덤 배치

        // 오르는 지형물 : 무너진 단(3~4m + 경사로)과 낮은 잔해 더미(0.9m). 평평한 곳에만 놓는다.
        // 플레이어만 오르는 장애물과 지형 오브젝트를 한 개씩 번갈아 놓아서 수를 50:50으로 맞춘다.
        // (한쪽을 먼저 다 놓으면 나중 쪽이 자리를 못 찾아 수가 줄어든다)
        private static void ScatterClimbPairs(Transform blocks, Transform climbs)
        {
            // 1. 장애물 순서표 : 큰 것부터, 비율대로 개수를 나눈다
            List<BlockType> obstacleOrder = new List<BlockType>();
            float weightSum = 0f;
            foreach (BlockType type in ClimbObstacleTypes) weightSum += GetClimbObstacleWeight(type);
            foreach (BlockType type in ClimbObstacleTypes)
            {
                int count = Mathf.RoundToInt(ClimbPairCount * GetClimbObstacleWeight(type) / weightSum);
                for (int i = 0; i < count; i++) obstacleOrder.Add(type);
            }

            // 2. 장애물 하나, 지형 하나씩 번갈아 놓기 (지형은 앞쪽이 무너진 단, 뒤쪽이 잔해 더미)
            int pairCount = obstacleOrder.Count;
            int platformCount = Mathf.RoundToInt(pairCount * PlatformRatio);
            int obstacleMade = 0;
            int terrainMade = 0;

            for (int i = 0; i < pairCount; i++)
            {
                if (TryPlaceBlock(obstacleOrder[i], blocks, obstacleOrder[i] + "_" + i.ToString("00"))) obstacleMade++;
                if (TryPlaceTerrain(i < platformCount, climbs, i)) terrainMade++;
            }

            // 3. 한쪽이 자리를 못 찾아 모자라면 작은 것(낮은 담 / 잔해 더미)으로 채워서 수를 맞춘다
            for (int extra = 0; extra < 20 && obstacleMade < terrainMade; extra++)
            {
                if (TryPlaceBlock(BlockType.LowWall, blocks, "LowWall_Extra_" + extra)) obstacleMade++;
            }
            for (int extra = 0; extra < 20 && terrainMade < obstacleMade; extra++)
            {
                if (TryPlaceTerrain(false, climbs, 100 + extra)) terrainMade++;
            }

            Debug.Log($"플레이어만 오르는 장애물 {obstacleMade}개 : 지형 오브젝트 {terrainMade}개");
            if (obstacleMade != terrainMade) Debug.LogWarning("공간이 모자라 50:50을 맞추지 못했습니다. ClimbPairCount를 줄여 보세요.");
        }

        private static void ScatterBlockType(BlockType type, Transform parent, int count)
        {
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                if (TryPlaceBlock(type, parent, type + "_" + i.ToString("00"))) made++;
            }
            if (made < count) Debug.LogWarning($"{type}을(를) {made}/{count}개만 놓았습니다.");
        }

        // 막는 장애물 하나를 자리가 날 때까지 여러 번 시도해서 놓는다
        private static bool TryPlaceBlock(BlockType type, Transform parent, string name)
        {
            for (int tryCount = 0; tryCount < 80; tryCount++)
            {
                float x = Random.Range(PlayAreaMin, PlayAreaMax);
                float z = Random.Range(PlayAreaMin, PlayAreaMax);

                bool placed = false;
                switch (type)
                {
                    case BlockType.RuinTower: placed = TryRuinTower(name, parent, x, z); break;
                    case BlockType.RuinHall: placed = TryRuinHall(name, parent, x, z); break;
                    case BlockType.RockCluster: placed = TryRockCluster(name, parent, x, z); break;
                    case BlockType.Foundation: placed = TryFoundation(name, parent, x, z); break;
                    case BlockType.FlatRock: placed = TryFlatRock(name, parent, x, z); break;
                    case BlockType.LowWall: placed = TryLowWall(name, parent, x, z); break;
                    case BlockType.Pillar: placed = TryPillar(name, parent, x, z); break;
                    case BlockType.Tree: placed = TryTree(name, parent, x, z); break;
                }
                if (placed) return true;
            }
            return false;
        }

        // 지형 오브젝트 하나 (무너진 단 3~4m + 경사로, 또는 잔해 더미 0.9m). 평평한 곳에만 놓는다.
        private static bool TryPlaceTerrain(bool isPlatform, Transform climbs, int index)
        {
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

            for (int tryCount = 0; tryCount < 80; tryCount++)
            {
                float x = Random.Range(PlayAreaMin, PlayAreaMax);
                float z = Random.Range(PlayAreaMin, PlayAreaMax);

                if (isPlatform)
                {
                    float sizeX = Random.Range(6, 13);
                    float sizeZ = Random.Range(6, 13);
                    float height = (Random.value < 0.5f) ? 3f : 4f;
                    Vector3 rampDirection = directions[Random.Range(0, directions.Length)];

                    Rect area = GetPlatformArea(x, z, sizeX, sizeZ, height, rampDirection);
                    if (IsAreaFree(area) == false || IsFlat(area, 0.05f) == false) continue;

                    CreateRuinPlatform("Platform_" + index.ToString("00"), climbs, x, z, sizeX, sizeZ, height, rampDirection);
                    return true;
                }
                else
                {
                    float sizeX = Random.Range(4, 9);
                    float sizeZ = Random.Range(4, 9);

                    Rect area = GetPlatformArea(x, z, sizeX, sizeZ, 0.9f, Vector3.zero);
                    if (IsAreaFree(area) == false || IsFlat(area, 0.05f) == false) continue;

                    CreateRuinPlatform("Rubble_" + index.ToString("00"), climbs, x, z, sizeX, sizeZ, 0.9f, Vector3.zero);
                    return true;
                }
            }
            return false;
        }

        // 서쪽(x < 120)은 폐허, 동쪽은 숲. 반대쪽 구역에는 30% 확률로만 놓는다.
        private static bool IsPreferredSide(float x, bool wantEast)
        {
            bool isEast = x > 120f;
            if (isEast == wantEast) return true;
            return Random.value < 0.3f;
        }

        private static void ScatterWalkableProps(Transform walkableProps)
        {
            int made = 0;
            for (int tryCount = 0; tryCount < WalkablePropCount * 40 && made < WalkablePropCount; tryCount++)
            {
                float x = Random.Range(PlayAreaMin, PlayAreaMax);
                float z = Random.Range(PlayAreaMin, PlayAreaMax);

                // 작은 돌 또는 쓰러진 통나무 (높이 0.4 → 플레이어 Step Offset 0.45 안쪽)
                Vector3 size = (Random.value < 0.6f) ? new Vector3(1.5f, 0.4f, 1.5f) : new Vector3(4f, 0.4f, 0.8f);
                Rect area = GetSquareArea(x, z, size.x * 0.5f);
                if (IsAreaFree(area) == false) continue;
                if (IsGentle(area) == false) continue;

                CreateWalkableBox("SmallProp_" + made.ToString("00"), walkableProps, x, z, size);
                made++;
            }
        }

        // ----- 플레이어도 돌아가야 하는 것 (2단 점프 7.5m 보다 높음) -----

        // 무너진 탑 : 넓고 높은 덩어리
        private static bool TryRuinTower(string name, Transform parent, float x, float z)
        {
            if (IsPreferredSide(x, false) == false) return false;

            Vector3 size = new Vector3(Random.Range(8f, 12f), Random.Range(10f, 14f), Random.Range(8f, 12f));
            Rect area = GetSquareArea(x, z, Mathf.Max(size.x, size.z) * 0.5f);
            if (IsAreaFree(area) == false || IsGentle(area) == false) return false;

            CreateObstacleBox(name, parent, x, z, size, Random.Range(0, 4) * 90f);
            return true;
        }

        // 두꺼운 ㄴ자 벽 건물 : 길이 14~18, 두께 3, 높이 9~11. 방향은 0/90/180/270 중 하나
        private static bool TryRuinHall(string name, Transform parent, float x, float z)
        {
            if (IsPreferredSide(x, false) == false) return false;

            float length = Random.Range(14f, 18f);
            float thickness = 3f;
            float height = Random.Range(9f, 11f);
            Rect area = GetSquareArea(x, z, length * 0.5f);
            if (IsAreaFree(area) == false || IsGentle(area) == false) return false;

            float angle = Random.Range(0, 4) * 90f;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 center = new Vector3(x, 0f, z);

            // 건물 안쪽 기준으로 아래 벽(x 방향), 왼쪽 벽(z 방향)
            float edge = length * 0.5f - thickness * 0.5f;
            Vector3 wall1 = center + rotation * new Vector3(0f, 0f, -edge);
            Vector3 wall2 = center + rotation * new Vector3(-edge, 0f, 0f);
            CreateObstacleBox(name + "_Wall1", parent, wall1.x, wall1.z, new Vector3(length, height, thickness), angle);
            CreateObstacleBox(name + "_Wall2", parent, wall2.x, wall2.z, new Vector3(thickness, height, length), angle);
            return true;
        }

        // ----- 섞인 것 : 몸통은 못 오르지만, 옆 바위를 밟으면 플레이어만 꼭대기까지 -----

        // 큰 바위(높이 9~12) + 옆에 붙은 작은 바위(높이 3~6.5)
        private static bool TryRockCluster(string name, Transform parent, float x, float z)
        {
            float mainSize = Random.Range(8f, 12f);
            float mainHeight = Random.Range(9f, 12f);
            float sideSize = Random.Range(5f, 7f);
            float sideHeight = Random.Range(3f, 6.5f);

            // 옆 바위는 큰 바위에 1m 겹쳐 붙인다
            float sideOffset = mainSize * 0.5f + sideSize * 0.5f - 1f;
            Rect area = GetSquareArea(x, z, sideOffset + sideSize * 0.5f);
            if (IsAreaFree(area) == false || IsGentle(area) == false) return false;

            float angle = Random.Range(0, 4) * 90f;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 side = new Vector3(x, 0f, z) + rotation * new Vector3(sideOffset, 0f, 0f);

            CreateObstacleBox(name + "_Main", parent, x, z, new Vector3(mainSize, mainHeight, mainSize), angle);
            CreateObstacleBox(name + "_Side", parent, side.x, side.z, new Vector3(sideSize, sideHeight, sideSize), angle);
            placedAreas.Add(area);
            return true;
        }

        // ----- 몹은 돌아가고, 플레이어는 올라가서 버티는 것 (넓고 평평한 윗면) -----

        // 무너진 건물 터 : 3m 바닥(1단 점프) + 한쪽 모서리 6m 단(2단 점프)
        private static bool TryFoundation(string name, Transform parent, float x, float z)
        {
            if (IsPreferredSide(x, false) == false) return false;

            float baseSize = Random.Range(8f, 12f);
            float upperSize = Random.Range(4f, 6f);
            Rect area = GetSquareArea(x, z, baseSize * 0.5f);
            if (IsAreaFree(area) == false || IsFlat(area, 0.3f) == false) return false;

            float angle = Random.Range(0, 4) * 90f;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            float corner = baseSize * 0.5f - upperSize * 0.5f;
            Vector3 upper = new Vector3(x, 0f, z) + rotation * new Vector3(corner, 0f, corner);

            CreateObstacleBox(name + "_Base", parent, x, z, new Vector3(baseSize, 3f, baseSize), angle);
            CreateObstacleBox(name + "_Upper", parent, upper.x, upper.z, new Vector3(upperSize, 6f, upperSize), angle);
            return true;
        }

        // 넓고 평평한 바위 : 낮은 것(3~3.5, 1단 점프) 또는 높은 것(5~6.5, 2단 점프)
        private static bool TryFlatRock(string name, Transform parent, float x, float z)
        {
            float height = (Random.value < 0.5f) ? Random.Range(3f, 3.5f) : Random.Range(5f, 6.5f);
            Vector3 size = new Vector3(Random.Range(6f, 10f), height, Random.Range(6f, 10f));
            float half = Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.5f; // 돌려도 들어가는 크기
            Rect area = GetSquareArea(x, z, half);
            if (IsAreaFree(area) == false || IsFlat(area, 0.5f) == false) return false;

            CreateObstacleBox(name, parent, x, z, size, Random.Range(0f, 90f));
            return true;
        }

        // 두꺼운 낮은 담 : 플레이어는 점프로 넘고, 몹은 돌아간다
        private static bool TryLowWall(string name, Transform parent, float x, float z)
        {
            Vector3 size = new Vector3(Random.Range(8f, 12f), Random.Range(2.5f, 3.5f), 2f);
            Rect area = GetSquareArea(x, z, size.x * 0.5f);
            if (IsAreaFree(area) == false || IsGentle(area) == false) return false;

            CreateObstacleBox(name, parent, x, z, size, Random.Range(0f, 180f));
            return true;
        }

        // ----- 작은 것 -----

        // 높이 10 = 2단 점프로 못 올라감 (플레이어도 돌아가는 쪽)
        private static bool TryPillar(string name, Transform parent, float x, float z)
        {
            Rect area = GetSquareArea(x, z, 1f);
            if (IsAreaFree(area) == false || IsGentle(area) == false) return false;
            CreatePillar(name, parent, x, z, 10f);
            return true;
        }

        private static bool TryTree(string name, Transform parent, float x, float z)
        {
            if (IsPreferredSide(x, true) == false) return false;

            Rect area = GetSquareArea(x, z, 0.6f);
            if (IsAreaFree(area) == false || IsGentle(area) == false) return false;
            CreateTree(name, parent, x, z);
            return true;
        }

        #endregion

        #region 배치 검사

        private static Rect GetSquareArea(float x, float z, float half)
        {
            return Rect.MinMaxRect(x - half, z - half, x + half, z + half);
        }

        // 단 + 경사로가 차지하는 영역 (경사로 폭은 단 폭 안쪽이라 길이 방향만 늘린다)
        private static Rect GetPlatformArea(float x, float z, float sizeX, float sizeZ, float height, Vector3 rampDirection)
        {
            float minX = x - sizeX * 0.5f;
            float maxX = x + sizeX * 0.5f;
            float minZ = z - sizeZ * 0.5f;
            float maxZ = z + sizeZ * 0.5f;

            if (rampDirection != Vector3.zero)
            {
                float run = GetRampRun(height);
                float endX = x + rampDirection.x * (sizeX * 0.5f + run);
                float endZ = z + rampDirection.z * (sizeZ * 0.5f + run);
                minX = Mathf.Min(minX, endX);
                maxX = Mathf.Max(maxX, endX);
                minZ = Mathf.Min(minZ, endZ);
                maxZ = Mathf.Max(maxZ, endZ);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        // 맵 안쪽인지, 레어/시작 지점에서 떨어져 있는지, 다른 오브젝트와 간격이 있는지
        private static bool IsAreaFree(Rect area)
        {
            if (area.xMin < PlayAreaMin || area.xMax > PlayAreaMax) return false;
            if (area.yMin < PlayAreaMin || area.yMax > PlayAreaMax) return false;

            if (GetDistanceToArea(area, LairCenterX, LairCenterZ) < LairRadius + LairClearDistance) return false;
            if (GetDistanceToArea(area, PlayerStartX, PlayerStartZ) < PlayerStartClear) return false;

            foreach (Rect placed in placedAreas)
            {
                Rect spaced = new Rect(placed.x - ObjectSpacing, placed.y - ObjectSpacing,
                    placed.width + ObjectSpacing * 2f, placed.height + ObjectSpacing * 2f);
                if (spaced.Overlaps(area)) return false;
            }
            return true;
        }

        // 점에서 영역까지 가장 가까운 거리
        private static float GetDistanceToArea(Rect area, float x, float z)
        {
            float closestX = Mathf.Clamp(x, area.xMin, area.xMax);
            float closestZ = Mathf.Clamp(z, area.yMin, area.yMax);
            return Vector2.Distance(new Vector2(closestX, closestZ), new Vector2(x, z));
        }

        // 영역 안 바닥 높이 차이가 tolerance 이하인가 (평평한 곳에만 놓는 것들)
        private static bool IsFlat(Rect area, float tolerance)
        {
            GetGroundRange(area, out float lowest, out float highest);
            return highest - lowest <= tolerance;
        }

        // 영역 안이 경사 정도인가 (절벽에 걸쳐 있으면 false). 장애물용
        private static bool IsGentle(Rect area)
        {
            GetGroundRange(area, out float lowest, out float highest);
            float allowed = Mathf.Max(area.width, area.height) * 0.7f + 0.5f; // 33.7도 경사 기울기 0.67 + 여유
            return highest - lowest <= allowed;
        }

        // 영역의 네 모서리 + 가운데 바닥 높이 중 가장 낮은 값, 높은 값
        private static void GetGroundRange(Rect area, out float lowest, out float highest)
        {
            float[] sampleX = { area.xMin + 0.1f, area.xMax - 0.1f, area.xMin + 0.1f, area.xMax - 0.1f, area.center.x };
            float[] sampleZ = { area.yMin + 0.1f, area.yMin + 0.1f, area.yMax - 0.1f, area.yMax - 0.1f, area.center.y };

            lowest = float.MaxValue;
            highest = float.MinValue;
            for (int i = 0; i < sampleX.Length; i++)
            {
                float y = GetGroundY(sampleX[i], sampleZ[i]);
                if (y < lowest) lowest = y;
                if (y > highest) highest = y;
            }
        }

        #endregion

        #region 생성 함수 : 지형

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent);
            return group.transform;
        }

        // 바닥부터 꽉 찬 고원 / 언덕 상자
        private static void CreateTop(string name, Transform parent, float minX, float maxX, float minZ, float maxZ, float height)
        {
            Vector3 position = new Vector3((minX + maxX) * 0.5f, height * 0.5f, (minZ + maxZ) * 0.5f);
            Vector3 scale = new Vector3(maxX - minX, height, maxZ - minZ);
            CreateBox(name, parent, position, scale, groundLayer, terrainMaterial);
        }

        // 산비탈 : 높이 height → 바닥 0, 기울기는 SlopeRunPerHeight 로 통일 (약 33.7도)
        private static void CreateHillSlope(string name, Transform parent, float edgeX, float edgeZ, float width, float height, Vector3 downDirection)
        {
            Vector3 highEdge = new Vector3(edgeX, height, edgeZ);
            CreateRamp(name, parent, highEdge, width, height, height * SlopeRunPerHeight, downDirection, terrainSlopeMaterial);
        }

        // 기울인 상자로 경사를 만든다. (산비탈, 잔해 경사로 공용)
        // 윗면이 (높은 모서리) → (내려가는 방향으로 run 만큼, height 만큼 아래) 를 잇도록 놓으면
        // 두께 부분은 위쪽 단 안과 아래쪽 바닥 아래로 묻혀서 틈이 생기지 않는다.
        private static void CreateRamp(string name, Transform parent, Vector3 highEdge, float width, float height, float run, Vector3 downDirection, Material material)
        {
            float slopeLength = Mathf.Sqrt(run * run + height * height);
            float slopeAngle = Mathf.Atan2(height, run) * Mathf.Rad2Deg;

            // 경사 아래 삼각형(단 벽 - 바닥 - 경사면)을 꽉 채우는 두께 (아래가 비면 터널이 된다)
            // 경사면에서 가장 먼 점 = 단 아래 모서리. 그 거리 = 높이 x 수평길이 / 경사길이
            float thickness = height * run / slopeLength + SlopeFillMargin;

            // 앞(z)을 내려가는 방향으로 두고, x축으로 숙여서 앞쪽이 내려가게 한다
            Quaternion rotation = Quaternion.LookRotation(downDirection) * Quaternion.Euler(slopeAngle, 0f, 0f);

            Vector3 lowEdge = highEdge + downDirection * run + Vector3.down * height;
            Vector3 topCenter = (highEdge + lowEdge) * 0.5f;

            Vector3 slopeUp = rotation * Vector3.up;
            Vector3 center = topCenter - slopeUp * (thickness * 0.5f);

            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent);
            ramp.transform.position = center;
            ramp.transform.rotation = rotation;
            ramp.transform.localScale = new Vector3(width, thickness, slopeLength);
            SetupObject(ramp, groundLayer, material);
        }

        private static float GetRampRun(float height)
        {
            return height / Mathf.Tan(RampAngle * Mathf.Deg2Rad);
        }

        #endregion

        #region 생성 함수 : 지형 위 오브젝트

        // 오르는 지형물 : 무너진 단 (Ground) + 한쪽 잔해 경사로
        // 높이 0.9 이하는 몹이 그냥 올라오므로 경사로 없이 (rampDirection = Vector3.zero)
        // 단은 바닥부터 꽉 찬 상자라 아래로 지나가는 틈이 없다.
        private static void CreateRuinPlatform(string name, Transform parent, float x, float z, float sizeX, float sizeZ, float height, Vector3 rampDirection, float rampWidth = RampWidth)
        {
            float baseY = GetGroundY(x, z);
            Vector3 position = new Vector3(x, baseY + height * 0.5f, z);
            CreateBox(name, parent, position, new Vector3(sizeX, height, sizeZ), groundLayer, climbMaterial);
            placedAreas.Add(GetPlatformArea(x, z, sizeX, sizeZ, height, rampDirection));

            if (rampDirection == Vector3.zero) return;

            // 경사로 윗변 = 경사로를 붙일 쪽 가장자리의 가운데
            float edgeX = x + rampDirection.x * sizeX * 0.5f;
            float edgeZ = z + rampDirection.z * sizeZ * 0.5f;
            Vector3 highEdge = new Vector3(edgeX, baseY + height, edgeZ);
            CreateRamp(name + "_Ramp", parent, highEdge, rampWidth, height, GetRampRun(height), rampDirection, climbMaterial);
        }

        // 막는 장애물 (Obstacle). size.y = 지면 위로 올라온 높이
        // 경사 위에 놓여도 아래쪽에 틈이 생기지 않게, 둘레에서 가장 낮은 바닥까지 내려서 만든다.
        private static void CreateObstacleBox(string name, Transform parent, float x, float z, Vector3 size, float angleY)
        {
            float halfSize = Mathf.Max(size.x, size.z) * 0.5f;
            float bottomY = GetLowestGroundY(x, z, halfSize);
            float topY = GetGroundY(x, z) + size.y;
            float height = topY - bottomY;

            Vector3 position = new Vector3(x, bottomY + height * 0.5f, z);
            GameObject box = CreateBox(name, parent, position, new Vector3(size.x, height, size.z), obstacleLayer, obstacleMaterial);
            box.transform.rotation = Quaternion.Euler(0f, angleY, 0f);

            // 90도 단위가 아니게 돌렸으면 모서리가 튀어나오므로 대각선 길이로 영역을 잡는다
            float areaHalf = halfSize;
            if (angleY % 90f != 0f) areaHalf = Mathf.Sqrt(size.x * size.x + size.z * size.z) * 0.5f;
            placedAreas.Add(GetSquareArea(x, z, areaHalf));
        }

        // 기둥 (2x2)
        private static void CreatePillar(string name, Transform parent, float x, float z, float height)
        {
            CreateObstacleBox(name, parent, x, z, new Vector3(2f, height, 2f), 0f);
        }

        // 나무 : 기둥만 콜라이더 (캡슐 기본 높이 2 → y 스케일 2 이면 높이 4, 반지름 0.6)
        private static void CreateTree(string name, Transform parent, float x, float z)
        {
            float groundY = GetGroundY(x, z);

            GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tree.name = name;
            tree.transform.SetParent(parent);
            tree.transform.position = new Vector3(x, groundY + 2f, z);
            tree.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
            SetupObject(tree, obstacleLayer, treeMaterial);
            placedAreas.Add(GetSquareArea(x, z, 0.6f));
        }

        // 밟고 넘는 작은 것 (Ground)
        private static void CreateWalkableBox(string name, Transform parent, float x, float z, Vector3 size)
        {
            float groundY = GetGroundY(x, z);
            Vector3 position = new Vector3(x, groundY + size.y * 0.5f, z);
            CreateBox(name, parent, position, size, groundLayer, groundMaterial);
            placedAreas.Add(GetSquareArea(x, z, Mathf.Max(size.x, size.z) * 0.5f));
        }

        // 위치 표시용 빈 오브젝트 (바닥 위)
        private static void CreateMarker(string name, Transform parent, float x, float z)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = new Vector3(x, GetGroundY(x, z), z);
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, int layer, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = scale;
            SetupObject(box, layer, material);
            return box;
        }

        #endregion

        #region 공용

        // 위에서 아래로 쏴서 그 자리의 지형(Ground) 높이를 찾는다 (MapGrid 굽기와 같은 방식)
        private static float GetGroundY(float x, float z)
        {
            Vector3 start = new Vector3(x, 100f, z);
            int groundMask = 1 << groundLayer;
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 200f, groundMask))
                return hit.point.y;

            Debug.LogWarning($"({x}, {z}) 에서 바닥을 찾지 못했습니다.");
            return 0f;
        }

        // 가운데와 앞뒤좌우 4곳 중 가장 낮은 바닥 높이 (경사 위 장애물의 밑바닥 맞추기용)
        private static float GetLowestGroundY(float x, float z, float halfSize)
        {
            float lowest = GetGroundY(x, z);
            float[] offsetX = { halfSize, -halfSize, 0f, 0f };
            float[] offsetZ = { 0f, 0f, halfSize, -halfSize };

            for (int i = 0; i < offsetX.Length; i++)
            {
                float y = GetGroundY(x + offsetX[i], z + offsetZ[i]);
                if (y < lowest) lowest = y;
            }
            return lowest;
        }

        private static void SetupObject(GameObject target, int layer, Material material)
        {
            target.layer = layer;
            target.isStatic = true;
            target.GetComponent<MeshRenderer>().sharedMaterial = material;
            Physics.SyncTransforms(); // 방금 만든 콜라이더를 바로 레이 검사에 반영
        }

        // 씬에 저장되도록 머티리얼을 에셋 파일로 만든다. 이미 있으면 그대로 사용.
        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            if (AssetDatabase.IsValidFolder(MaterialFolder) == false)
                AssetDatabase.CreateFolder("Assets/00. 3D Game/Material", "Whitebox");

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return material;
        }

        #endregion
    }
}
