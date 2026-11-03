using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

// ===== 새로 추가 : 파일 전체 (에셋 기반 용의 둥지 맵 빌더) =====
namespace PROJECT3D
{
    // 메뉴 한 번으로 "용의 둥지 + 무너진 유적" 원형 맵을 에셋으로 만든다. (화이트박스는 컨셉만 가져옴)
    //  1. 바닥 : 높이 함수로 만든 울퉁불퉁한 원형 지형 메쉬 (Ground, MeshCollider) + Proxy 지형 메쉬(언덕) 섞기
    //           원 밖에는 바닥이 없다 → MapGrid가 막힌 칸으로 처리. 원 둘레는 보이지 않는 벽
    //  2. 고원 : 원 여러 개를 겹친 초승달 / 콩팥 모양. 북쪽 18m, 남서 / 남동 10m. 고원마다 비탈 2개, 나머지 면은 절벽 → 바위로 덮는다 // [변경]
    //  3. 용의 둥지 : 가운데 바위 테두리(입구 4개) 안 정중앙에 돌 제단 하나 // [변경]
    //  - 고원마다 오르내리는 비탈 2개 이상 // [변경]
    //  - 뒷면이 빈 메쉬는 장애물에서 빼고, 절벽은 보이는 면이 바깥을 향하게 돌린다 // [변경]
    //  4. 유적 : 무너진 건물 터(벽 + 기둥, 못 지나감), 돌 단(Ground, 올라갈 수 있음), 선돌 무리
    //  5. 흩뿌리기 : 바위 / 나무 / 작은 소품, 풀 (구역마다 합침)
    // 규칙
    //  - 풀을 뺀 모든 오브젝트는 Ground(올라감) 또는 Obstacle(막힘) + 모델 메쉬 그대로 충돌 // [변경]
    //  - [변경] 나무는 줄기만 원통 콜라이더 (가지 / 잎은 안 부딪힘, 캡슐은 쓰지 않음)
    //  - 메쉬 / 머티리얼 / 셰이더(URP 패스 없음 포함)에 문제가 있는 에셋은 건너뛰고 로그로 알려준다
    //  - 크기는 높이와 최대 가로를 같이 정한다 (높이만 맞추면 넓적한 메쉬가 수십 m로 커진다)
    //  - 좌표는 MapGrid 기준 (0,0,0), 200x200, 칸 1m. 같은 시드면 항상 같은 맵
    public static class DragonNestMapBuilder
    {
        #region 설정값

        private const string RootName = "Map_DragonNest_Art";
        private const string OldRootName = "Map_DragonNest";                   // 예전 화이트박스 (지우지 않고 꺼 둔다)
        private const string MeshFolder = "Assets/00. 3D Game/Map/DragonNest"; // 만든 지형 / 풀 메쉬 저장 위치

        private const string AssetRoot = "Assets/99. External Assets/Map/";
        private const string PolyartPath = AssetRoot + "Polyart/PolyartStudio/DreamscapeMountains/Prefabs/";
        private const string PolyartSharedPath = AssetRoot + "Polyart/PolyartStudio/SharedResources/";
        private const string ProxyPath = AssetRoot + "Proxy Games/Low Poly Medieval Kit/Prefabs/";
        private const string ToonPath = AssetRoot + "Toon Fantasy Nature 1/Prefabs/";

        private const int RandomSeed = 2026; // 배치가 마음에 안 들면 이것만 바꾼다

        // 원형 맵
        private const float CenterX = 100f;
        private const float CenterZ = 100f;
        private const float PlayRadius = 86f;   // 오브젝트를 놓는 범위
        private const float WallRadius = 88f;   // 보이지 않는 벽
        private const float GroundRadius = 92f; // 바닥 메쉬 반지름 (벽 바깥으로 조금 더)
        private const int ChunkSize = 40;       // 바닥 메쉬 한 조각 크기 (m)
        private const float CliffLength = 3f;   // 절벽 면 (몹 / 플레이어 모두 못 오름)

        // 고원 (기존 기준표 : 1단 점프 3.5m, 2단 7.5m, 몹 경사 40도. 비탈은 높이 1m당 1.5m → 약 33.7도)
        // [변경] 고원 = 원 여러 개를 겹친 모양 (맵 가장자리를 따라 길게 휜 초승달 / 콩팥 모양). 원 하나로 끝나면 어색해서
        //  조각 : 맵 중심에서 PartArc 거리, PartAngle 각도(0 = +x, 90 = +z)에 반지름 PartRadius인 원. 같은 고원 조각끼리 높이가 같다
        //  비탈 : RampAngle 각도에서 고원 가장자리를 따라 대각선으로 내려가는 통로 (폭 RampWidth). 고원마다 2개
        private static readonly float[] PlateauHeight = { 18f, 10f, 10f }; // 0 : 북쪽 (2층), 1 : 남서, 2 : 남동
        private static readonly int[] PartPlateau = { 0, 0, 0, 0, 0, 1, 1, 1, 2, 2, 2 };
        private static readonly float[] PartAngle = { 55f, 72f, 90f, 108f, 125f, 197f, 217f, 238f, 343f, 323f, 302f };
        private static readonly float[] PartArc = { 70f, 70f, 70f, 70f, 70f, 68f, 68f, 66f, 68f, 68f, 66f };
        private static readonly float[] PartRadius = { 14f, 17f, 18f, 17f, 14f, 13f, 15f, 12f, 13f, 15f, 12f };
        private static readonly int[] RampPlateau = { 0, 0, 1, 1, 2, 2 };
        private static readonly float[] RampAngle = { 60f, 120f, 205f, 232f, 335f, 308f };
        private static readonly float[] RampStart = { 54f, 54f, 54f, 53f, 54f, 53f }; // 비탈이 시작하는 곳 (맵 중심에서 거리 = 고원 안쪽 가장자리)
        // [변경] 비탈이 맵 중심(둥지) 쪽으로 곧게 몰려서, 고원 가장자리를 따라 40도 돌린 대각선으로 바꾼다
        //  같은 고원의 두 비탈은 서로 반대쪽으로 돌린다 (+ : 고원 조각 각도가 작은 쪽으로)
        private static readonly float[] RampTurn = { 40f, -40f, 40f, -40f, -40f, 40f };
        private const float RampWidth = 18f;          // [변경] 12 → 18m
        private const float RampPerHeight = 1.5f;     // 높이 1m당 비탈 길이 (경사 약 33.7도)
        private const float RampLengthScale = 1.4f;   // 새로 추가 : 통로 길이 배율. 대각선이라 고원에서 멀어지는 속도가 느려서 바닥까지 닿게 늘린다

        // 용의 둥지 (가운데 제단)
        private const float BasinRadius = 26f;     // 둥지 바닥이 조금 꺼진 범위
        private const float BasinDepth = 1.5f;
        private const float LairWallRadius = 21f;  // 바위 테두리 반지름
        private const float LairWallHeight = 11f;  // 2단 점프(7.5m)로 못 넘는 높이
        private const float LairGateHalfAngle = 12f;
        private const float LairClear = 27f;       // 흩뿌리기가 둥지 중심에서 이만큼은 비운다
        // [변경] 2단 제단(높이 함수) 삭제 → 가운데 돌 제단 하나 (몹도 오르는 높이)
        private const float AltarSize = 7f;
        private const float AltarHeight = 0.9f;
        private const float OneSidedLimit = 0.15f; // 메쉬 면 방향 합이 이 비율을 넘으면 "한쪽 면만 있는 메쉬"
        // ===== 새로 추가 : 열린 메쉬 / 콜라이더 모양 기준 =====
        private const float OpenEdgeLimit = 0.02f;    // 바닥 말고 다른 곳의 열린 변 길이가 전체 변 길이의 이 비율을 넘으면 "열린 메쉬" (빈 곳이 보인다)
        private const float ClosedEdgeLimit = 0.01f;  // 열린 변이 이 비율보다 적으면 닫힌 메쉬로 본다 (작은 틈은 무시)
        private const float TreeColliderHeight = 10f; // 나무 원통 최소 높이 (2단 점프 7.5m로 못 올라감)
        private const float TreeFootRadius = 0.5f;    // 나무를 땅에 놓을 때 보는 범위 (줄기 둘레)
        // ===== 새로 추가 끝 =====

        // 시작 지점
        private const float StartX = 100f;
        private const float StartZ = 30f;
        private const float StartClear = 14f;

        // 개수
        private const int MoundCount = 10;          // Proxy 지형 메쉬 (언덕)
        private const int RuinCompoundCount = 9;    // 무너진 건물 터
        private const int PlatformCount = 14;       // 올라가는 돌 단
        private const int StoneCircleCount = 3;     // 선돌 무리
        private const int RockCount = 45;
        private const int PlateauRockCount = 10;
        private const int ForestTreeCount = 50;     // 동쪽 숲
        private const int SparseTreeCount = 20;
        private const int DeadTreeCount = 12;       // 둥지 주변 마른 나무
        private const int SmallPropCount = 80;
        private const float ObstacleSpacing = 3.5f; // 장애물 사이 최소 간격 (몹이 지나갈 길)
        private const float PlatformStep = 0.9f;    // 돌 단 한 층 높이 (몹 오르막 한계 1m 아래)

        // 풀
        private const float GrassChunk = 45f;
        private const float GrassStep = 2.2f;
        private const float GrassMaxSlope = 22f;

        #endregion

        #region 실행 중 쓰는 값

        private static int groundLayer;
        private static int obstacleLayer;
        private static int groundMask;
        private static int obstacleMask;
        private static Material terrainMaterial;
        private static int cylinderColliderCount, assetColliderCount; // 새로 추가 : 콜라이더 개수 (로그용)
        private static List<string> noColliderNames = new List<string>(); // 새로 추가 : 콜라이더가 없는 에셋 (충돌 없음)

        // 이미 놓은 것 (x, 반지름, z) → 겹치지 않게
        private static List<Vector3> placed = new List<Vector3>();

        // 에셋 묶음
        private static List<GameObject> moundPool, cliffPool, rockPool, bigRockPool, standingStonePool;
        private static List<GameObject> pillarPool, wallPool, archPool, stairsPool, platformPool, towerPool, castleWallPool;
        private static List<GameObject> treePool, deadTreePool, smallPropPool, grassPool; // [변경] 둥지 재료 / 해골 / 불 / 까마귀 삭제
        private static List<string> skipped = new List<string>(); // 건너뛴 에셋 (이유 포함)

        #endregion

        #region 외부 설정

        #region 메뉴

        [MenuItem("PROJECT3D/Map/드래곤 둥지 (에셋)/0. 에셋 점검")]
        public static void CheckAssets()
        {
            LoadAllPools();
            Debug.Log(GetPoolReport());
        }

        [MenuItem("PROJECT3D/Map/드래곤 둥지 (에셋)/1. 맵 만들기")]
        public static void Build()
        {
            // 1. 레이어 확인
            groundLayer = LayerMask.NameToLayer("Ground");
            obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (groundLayer == -1 || obstacleLayer == -1)
            {
                Debug.LogError("[DragonNestMapBuilder] Ground / Obstacle 레이어가 없습니다.");
                return;
            }
            groundMask = 1 << groundLayer;
            obstacleMask = 1 << obstacleLayer;

            MapGrid mapGrid = Object.FindFirstObjectByType<MapGrid>();
            if (mapGrid == null)
                Debug.LogWarning("[DragonNestMapBuilder] 씬에 MapGrid가 없습니다.");
            else if (mapGrid.transform.position != Vector3.zero || mapGrid.width != 200 || mapGrid.depth != 200)
                Debug.LogWarning("[DragonNestMapBuilder] MapGrid를 위치 (0,0,0), 200x200 으로 맞춰야 좌표가 맞습니다.", mapGrid);

            // 2. 에셋 / 머티리얼 준비
            LoadAllPools();
            terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainTextureGenerator.MaterialPath);
            if (terrainMaterial == null)
                Debug.LogWarning("[DragonNestMapBuilder] 지형 머티리얼(M_Terrain)이 없습니다. 메뉴 '지형 텍스처 만들고 입히기'를 먼저 실행하세요.");

            // 3. 예전 화이트박스는 끄고, 이전에 만든 에셋 맵은 지운다 (Ctrl+Z로 되돌릴 수 있음)
            GameObject oldWhitebox = GameObject.Find(OldRootName);
            if (oldWhitebox != null)
            {
                Undo.RecordObject(oldWhitebox, "Disable Whitebox");
                oldWhitebox.SetActive(false);
            }
            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Dragon Nest Map");

            Transform groundGroup = CreateGroup("Ground", root.transform);
            Transform boundaryGroup = CreateGroup("Boundary", root.transform);
            Transform cliffGroup = CreateGroup("Cliffs_Visual", root.transform);
            Transform lairGroup = CreateGroup("Lair", root.transform);
            Transform landmarkGroup = CreateGroup("Landmarks", root.transform);
            Transform ruinGroup = CreateGroup("Ruins", root.transform);
            Transform platformGroup = CreateGroup("Platforms_Walkable", root.transform);
            Transform obstacleGroup = CreateGroup("Obstacles", root.transform);
            Transform treeGroup = CreateGroup("Trees", root.transform);
            Transform propGroup = CreateGroup("Props_Walkable", root.transform);
            Transform grassGroup = CreateGroup("Grass", root.transform);
            Transform markerGroup = CreateGroup("Markers", root.transform);

            placed.Clear();
            Random.InitState(RandomSeed);
            PrepareMeshFolder();
            cylinderColliderCount = 0;
            assetColliderCount = 0;
            noColliderNames.Clear();

            try
            {
                // 4. 바닥 → 큰 것(둥지, 랜드마크, 유적) → 작은 것 → 풀 순서. 뒤에 놓는 것이 앞의 것을 피한다
                Progress("바닥 메쉬", 0.05f);
                BuildGroundMeshes(groundGroup);
                BuildBoundary(boundaryGroup);
                Physics.SyncTransforms(); // 레이로 바닥 높이를 재기 전에 콜라이더 위치 반영

                Progress("Proxy 지형 메쉬", 0.12f);
                ScatterMounds(groundGroup);
                Physics.SyncTransforms();

                Progress("고원 절벽", 0.2f);
                BuildPlateauCliffs(cliffGroup);

                Progress("용의 둥지", 0.28f);
                BuildLair(lairGroup);

                Progress("랜드마크", 0.36f);
                BuildLandmarks(landmarkGroup);

                Progress("유적", 0.45f);
                ScatterRuinCompounds(ruinGroup, platformGroup);
                ScatterPlatforms(platformGroup);
                ScatterStoneCircles(ruinGroup);

                Progress("바위 / 나무", 0.6f);
                ScatterRocks(obstacleGroup);
                ScatterTrees(treeGroup);
                Physics.SyncTransforms(); // 풀이 장애물을 피하려면 장애물 콜라이더 위치가 필요하다

                Progress("풀", 0.8f);
                BuildGrass(grassGroup);
                ScatterSmallProps(propGroup); // [변경] 풀 다음 (소품이 Ground라서 먼저 놓으면 풀이 소품 위에 생긴다)

                BuildMarkers(markerGroup);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log($"[DragonNestMapBuilder] 맵 생성 완료 : 유적 {ruinGroup.childCount}, 돌 단 {platformGroup.childCount}, 장애물 {obstacleGroup.childCount}, 나무 {treeGroup.childCount}, 소품 {propGroup.childCount}, 풀 {grassGroup.childCount}\n"
                + $"콜라이더 : 에셋 것 그대로 {assetColliderCount}, 나무 원통 {cylinderColliderCount}, 콜라이더 없음 {noColliderNames.Count}종 ({string.Join(", ", noColliderNames)})\n" // 새로 추가
                + GetPoolReport());
        }

        // 묶음별 원래 크기 (가로 x 높이 x 세로). 배치 수치를 정할 때 참고
        [MenuItem("PROJECT3D/Map/드래곤 둥지 (에셋)/3. 에셋 크기 표")]
        public static void LogAssetSizes()
        {
            LoadAllPools();
            string report = "[DragonNestMapBuilder] 에셋 원래 크기 (가로 x 높이 x 세로, m)\n";
            report += GetSizeReport("지형 메쉬", moundPool) + GetSizeReport("절벽", cliffPool) + GetSizeReport("바위", rockPool);
            report += GetSizeReport("큰 바위", bigRockPool) + GetSizeReport("선돌", standingStonePool) + GetSizeReport("기둥", pillarPool);
            report += GetSizeReport("벽", wallPool) + GetSizeReport("돌 단", platformPool) + GetSizeReport("부서진 탑", towerPool);
            report += GetSizeReport("성벽", castleWallPool) + GetSizeReport("나무", treePool);
            report += GetSizeReport("작은 소품", smallPropPool) + GetSizeReport("풀", grassPool);
            Debug.Log(report);
        }

        [MenuItem("PROJECT3D/Map/드래곤 둥지 (에셋)/2. 맵 지우기")]
        public static void Clear()
        {
            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);
        }

        #endregion

        #endregion

        #region 내부 설정

        #region 진행 표시

        private static void Progress(string step, float rate)
        {
            EditorUtility.DisplayProgressBar("용의 둥지 맵 만들기", step, rate);
        }

        #endregion

        #region 에셋 불러오기 / 점검

        private static void LoadAllPools()
        {
            skipped.Clear();

            // 바닥에 섞을 Proxy 지형 메쉬 (이름만 Terrain이고 일반 메쉬). 머티리얼은 지형 머티리얼로 바꿔 끼우니 검사 안 함
            moundPool = LoadPrefabs(ProxyPath + "Terrains/Grass Hills", null, null, false);
            // [변경] 삭제 : Small Terrains (눌러 놓으면 모양이 이상했다)

            // 고원 절벽 (보이기만). Polyart 절벽은 원래 셰이더가 Built-in 전용이라 지형 머티리얼로 바꿔 낀다
            cliffPool = LoadPrefabs(PolyartPath + "Stones", new[] { "Cliff" }, null, false);

            // 바위
            rockPool = LoadPrefabs(ToonPath + "Rocks", new[] { "Rock_Medium", "Rock_Large" }, null);
            rockPool.AddRange(LoadPrefabs(PolyartPath + "Stones", new[] { "Prefab_Rock_" }, null));
            bigRockPool = LoadPrefabs(ToonPath + "Rocks", new[] { "Rock_Large" }, null);
            standingStonePool = LoadPrefabs(PolyartPath + "StandingStones", new[] { "RockFormation" }, null);

            // 유적 (Polyart 신전)
            pillarPool = LoadPrefabs(PolyartPath + "Ruins", new[] { "PillarMain", "PillarFull", "PillarHalf", "PillarBroken" }, null);
            wallPool = LoadPrefabs(PolyartPath + "Ruins", new[] { "WallRuin", "Wall_Mid", "Wall_Left", "Wall_Right" }, null);
            archPool = LoadPrefabs(PolyartPath + "Ruins", new[] { "DoorArch" }, null);
            stairsPool = LoadPrefabs(PolyartPath + "Ruins", new[] { "Stairs" }, null);
            platformPool = LoadPrefabs(ToonPath + "Rocks", new[] { "Stone_Slab" }, null);
            platformPool.AddRange(LoadPrefabs(PolyartPath + "Ruins", new[] { "PillarBase_Square", "DoorFrame_WallBase" }, null));

            // Proxy 성 (부서진 탑, 성벽)
            towerPool = LoadPrefabs(ProxyPath + "Buildings", new[] { "Tower broken" }, null);
            castleWallPool = LoadPrefabs(ProxyPath + "Building Parts", new[] { "Castle Wall" }, null);

            // 나무
            treePool = LoadPrefabs(ToonPath + "Vegetation/Trees", new[] { "Pine_Tree", "Aspen_Tree", "Birch_Tree_0", "Oak_Tree" }, null);
            treePool.AddRange(LoadPrefabs(PolyartPath + "Trees", new[] { "Conifer" }, null));
            deadTreePool = LoadPrefabs(ToonPath + "Vegetation/Trees", new[] { "Birch_Tree_Dry", "Tree_Broken" }, null);
            deadTreePool.AddRange(LoadPrefabs(ProxyPath + "Foliage", new[] { "Dead_Tree" }, null));

            // [변경] 삭제 : 둥지 재료 / 해골 / 불 / 까마귀 (둥지 안은 가운데 제단 하나만)

            // 밟고 지나가는 작은 소품 ([변경] Ground + 에셋 콜라이더 그대로)
            smallPropPool = LoadPrefabs(ToonPath + "Rocks", new[] { "Rock_Small" }, null);
            smallPropPool.AddRange(LoadPrefabs(PolyartPath + "Stones", new[] { "SmallRock" }, null));
            smallPropPool.AddRange(LoadPrefabs(ToonPath + "Props", new[] { "Twigs", "Wood_Plank" }, null));

            // 풀 (TFF 잔디 / 꽃만. 종류마다 머티리얼이 달라서 종류가 많으면 구역마다 메쉬 수가 늘어난다)
            grassPool = LoadPrefabs(ToonPath + "Vegetation/Plants", new[] { "Grass_Patch", "Flower_Patch" }, null);

            // [변경] 빈 면이 보이는 메쉬는 뺀다 : 한쪽 면만 있는 메쉬 + 바닥 말고 다른 곳(옆 / 위)이 뚫린 메쉬
            //  바닥만 뚫린 메쉬는 PlaceOnGround가 땅에 묻어서 안 보이므로 쓴다
            //  (절벽은 빼지 않고, 보이는 면을 바깥으로 돌리고 꼭대기를 고원 높이에 맞춘다 / 나무 · 풀은 잎이 원래 판이라 검사 X)
            RemoveOpenMeshes(rockPool);
            RemoveOpenMeshes(bigRockPool);
            RemoveOpenMeshes(standingStonePool);
            RemoveOpenMeshes(pillarPool);
            RemoveOpenMeshes(wallPool);
            RemoveOpenMeshes(archPool);       // 새로 추가
            RemoveOpenMeshes(stairsPool);     // 새로 추가
            RemoveOpenMeshes(platformPool);
            RemoveOpenMeshes(towerPool);
            RemoveOpenMeshes(castleWallPool);
            RemoveOpenMeshes(smallPropPool);  // 새로 추가

            // 새로 추가 : 신전 벽이 전부 빠지면 건물 터를 성벽 조각으로 만든다
            if (Count(wallPool) == 0) wallPool.AddRange(castleWallPool);
        }

        // ===== 새로 추가 : 한쪽 면만 있는 메쉬 찾기 =====
        // 모든 삼각형의 면 방향(법선 x 넓이)을 더한다. 닫힌 메쉬는 서로 상쇄되어 0에 가깝고,
        // 뒷면이 빈 메쉬는 앞쪽으로 크게 남는다. 그 방향이 "보이는 면"이다 (프리팹 기준 수평 방향)
        private static Dictionary<GameObject, Vector3> frontCache = new Dictionary<GameObject, Vector3>();

        // 한쪽 면만 있으면 보이는 면 방향, 닫힌 메쉬면 Vector3.zero
        private static Vector3 GetFrontDirection(GameObject prefab)
        {
            if (frontCache.ContainsKey(prefab)) return frontCache[prefab];

            Vector3 sum = Vector3.zero;
            float area = 0f;
            Matrix4x4 rootInverse = prefab.transform.worldToLocalMatrix;
            Renderer[] renderers = GetLod0Renderers(prefab);
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshFilter filter = renderers[r].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Matrix4x4 toRoot = rootInverse * renderers[r].transform.localToWorldMatrix;
                Vector3[] vertices = filter.sharedMesh.vertices;
                int[] triangles = filter.sharedMesh.triangles;
                for (int t = 0; t + 2 < triangles.Length; t += 3)
                {
                    Vector3 a = toRoot.MultiplyPoint3x4(vertices[triangles[t]]);
                    Vector3 b = toRoot.MultiplyPoint3x4(vertices[triangles[t + 1]]);
                    Vector3 c = toRoot.MultiplyPoint3x4(vertices[triangles[t + 2]]);
                    Vector3 normal = Vector3.Cross(b - a, c - a); // 길이 = 넓이 x 2
                    sum += normal;
                    area += normal.magnitude;
                }
            }

            Vector3 front = Vector3.zero;
            Vector3 flat = new Vector3(sum.x, 0f, sum.z);
            if (area > 0f && flat.magnitude / area > OneSidedLimit) front = flat.normalized;
            frontCache.Add(prefab, front);
            return front;
        }

        // [변경] RemoveOneSided → RemoveOpenMeshes : 한쪽 면만 있는 메쉬 + 옆 / 위가 뚫린 메쉬를 뺀다
        private static void RemoveOpenMeshes(List<GameObject> pool)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (GetFrontDirection(pool[i]) != Vector3.zero)
                {
                    skipped.Add($"{pool[i].name} : 한쪽 면만 있는 메쉬 (뒷면이 보이지 않음)");
                    pool.RemoveAt(i);
                    continue;
                }

                MeshShape shape = MeasureShape(pool[i], pool[i].transform.worldToLocalMatrix);
                if (shape.openSideLength > shape.edgeLength * OpenEdgeLimit)
                {
                    skipped.Add($"{pool[i].name} : 옆 / 위가 뚫린 메쉬 (열린 변 {shape.openSideLength / shape.edgeLength * 100f:0.0}%)");
                    pool.RemoveAt(i);
                }
            }
        }

        // ===== 새로 추가 : 메쉬 모양 재기 (열린 곳 / 부피) =====
        //  열린 변 = 삼각형 하나에만 붙은 변 (반대쪽 면이 없어서 그 너머가 뚫려 보인다)
        //  같은 위치의 정점은 하나로 본다 (UV / 각진 모서리 때문에 정점이 나뉘어 있어도 실제로는 이어진 면)
        private class MeshShape
        {
            public Bounds bounds;         // space 좌표 기준 크기
            public float volume;          // 부피 (닫힌 메쉬일 때만 믿을 수 있다)
            public float edgeLength;      // 모든 변 길이 합
            public float openLength;      // 열린 변 길이 합 (바닥 포함)
            public float openSideLength;  // 열린 변 길이 합 (바닥 제외 = 놓았을 때 보이는 구멍)

            public bool IsClosed()
            {
                return openLength <= edgeLength * ClosedEdgeLimit;
            }
        }

        // target의 LOD0 메쉬를 toSpace 좌표(프리팹 기준 / holder 기준)로 옮겨서 잰다
        private static MeshShape MeasureShape(GameObject target, Matrix4x4 toSpace)
        {
            MeshShape shape = new MeshShape();
            List<Vector3> points = new List<Vector3>();             // 합친 정점 위치
            Dictionary<Vector3, int> pointIndex = new Dictionary<Vector3, int>();
            Dictionary<long, int> edgeCount = new Dictionary<long, int>(); // 변 (작은 번호 x 100만 + 큰 번호) → 붙은 삼각형 수
            bool hasBounds = false;

            Renderer[] renderers = GetLod0Renderers(target);
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshFilter filter = renderers[r].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Matrix4x4 matrix = toSpace * renderers[r].transform.localToWorldMatrix;
                Vector3[] vertices = filter.sharedMesh.vertices;
                int[] triangles = filter.sharedMesh.triangles;

                // 1. 정점 합치기 (1mm 단위로 반올림한 위치가 같으면 같은 점)
                int[] merged = new int[vertices.Length];
                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertices[v]);
                    Vector3 key = new Vector3(Mathf.Round(point.x * 1000f), Mathf.Round(point.y * 1000f), Mathf.Round(point.z * 1000f));
                    if (pointIndex.ContainsKey(key) == false)
                    {
                        pointIndex.Add(key, points.Count);
                        points.Add(point);
                    }
                    merged[v] = pointIndex[key];

                    if (hasBounds) shape.bounds.Encapsulate(point);
                    else shape.bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }

                // 2. 삼각형마다 부피(원점과 이은 사면체) + 변 세기
                float volume = 0f;
                for (int t = 0; t + 2 < triangles.Length; t += 3)
                {
                    Vector3 a = points[merged[triangles[t]]];
                    Vector3 b = points[merged[triangles[t + 1]]];
                    Vector3 c = points[merged[triangles[t + 2]]];
                    volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;

                    AddEdge(edgeCount, merged[triangles[t]], merged[triangles[t + 1]]);
                    AddEdge(edgeCount, merged[triangles[t + 1]], merged[triangles[t + 2]]);
                    AddEdge(edgeCount, merged[triangles[t + 2]], merged[triangles[t]]);
                }
                shape.volume += Mathf.Abs(volume);
            }

            // 3. 열린 변 길이. 바닥 근처(높이의 5% + 5cm)에 있는 변은 땅에 묻히니 따로 센다
            float bottomY = shape.bounds.min.y + shape.bounds.size.y * 0.05f + 0.05f;
            foreach (KeyValuePair<long, int> pair in edgeCount)
            {
                Vector3 a = points[(int)(pair.Key / 1000000)];
                Vector3 b = points[(int)(pair.Key % 1000000)];
                float length = Vector3.Distance(a, b);
                shape.edgeLength += length;
                if (pair.Value != 1) continue;

                shape.openLength += length;
                if (a.y > bottomY || b.y > bottomY) shape.openSideLength += length;
            }
            return shape;
        }

        private static void AddEdge(Dictionary<long, int> edgeCount, int a, int b)
        {
            long key = (a < b) ? (long)a * 1000000 + b : (long)b * 1000000 + a;
            if (edgeCount.ContainsKey(key)) edgeCount[key]++;
            else edgeCount.Add(key, 1);
        }
        // ===== 새로 추가 끝 =====

        // 보이는 면(front)이 view 방향을 향하도록 돌리는 Y 회전값
        private static float GetFacingYaw(GameObject prefab, Vector3 view)
        {
            Vector3 front = GetFrontDirection(prefab);
            if (front == Vector3.zero) return Random.Range(0f, 360f); // 닫힌 메쉬는 아무 방향
            view.y = 0f;
            return Vector3.SignedAngle(front, view, Vector3.up);
        }
        // ===== 새로 추가 끝 =====

        private static List<GameObject> LoadPrefabs(string folder, string[] includes, string[] excludes)
        {
            return LoadPrefabs(folder, includes, excludes, true);
        }

        // folder 안의 프리팹 중 이름에 includes 중 하나가 들어 있고 excludes는 없는 것. 문제 있는 에셋은 건너뛴다
        // checkMaterials = false : 머티리얼은 검사하지 않는다 (머티리얼을 바꿔 끼우는 것)
        private static List<GameObject> LoadPrefabs(string folder, string[] includes, string[] excludes, bool checkMaterials)
        {
            List<GameObject> result = new List<GameObject>();
            if (AssetDatabase.IsValidFolder(folder) == false)
            {
                skipped.Add($"폴더 없음 : {folder}");
                return result;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (HasAnyName(prefab.name, includes, true) == false) continue;
                if (HasAnyName(prefab.name, excludes, false)) continue;

                string reason;
                if (IsUsable(prefab, checkMaterials, out reason)) result.Add(prefab);
                else skipped.Add($"{prefab.name} : {reason}");
            }
            return result;
        }

        // names가 비어 있으면 whenEmpty를 돌려준다
        private static bool HasAnyName(string name, string[] names, bool whenEmpty)
        {
            if (names == null || names.Length == 0) return whenEmpty;
            for (int i = 0; i < names.Length; i++)
            {
                if (name.Contains(names[i])) return true;
            }
            return false;
        }

        // 메쉬 / 머티리얼 / 셰이더 / 스크립트에 문제가 없는지
        private static bool IsUsable(GameObject prefab, bool checkMaterials, out string reason)
        {
            reason = "";

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                reason = "렌더러 없음";
                return false;
            }

            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null)
                {
                    reason = $"메쉬 없음 ({filters[i].name})";
                    return false;
                }
            }

            for (int i = 0; i < renderers.Length && checkMaterials; i++)
            {
                Material[] materials = renderers[i].sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (IsMaterialUsable(materials[m], out reason) == false)
                    {
                        reason += $" ({renderers[i].name})";
                        return false;
                    }
                }
            }

            Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(children[i].gameObject) > 0)
                {
                    reason = $"스크립트 없음 ({children[i].name})";
                    return false;
                }
            }
            return true;
        }

        private static bool IsMaterialUsable(Material material, out string reason)
        {
            reason = "";
            if (material == null)
            {
                reason = "머티리얼 없음";
                return false;
            }

            Shader shader = material.shader;
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
            {
                reason = $"셰이더 없음 ({material.name})";
                return false;
            }
            if (shader.isSupported == false || ShaderUtil.ShaderHasError(shader))
            {
                reason = $"셰이더 에러 ({material.name} / {shader.name})";
                return false;
            }
            if (HasUrpPass(shader) == false)
            {
                reason = $"URP에서 그려지지 않는 셰이더 - 분홍색 ({material.name} / {shader.name})";
                return false;
            }
            return true;
        }

        // URP가 그리는 패스가 있는지 (Built-in 전용 셰이더는 ForwardBase 같은 패스만 있어서 URP에서 분홍색이 된다)
        //  예 : Polyart 침엽수의 먼 거리 빌보드 LOD (Dreamscape_FoliageBillboard)
        private static bool HasUrpPass(Shader shader)
        {
            if (shader.name == "Standard" || shader.name.StartsWith("Legacy Shaders")) return false;

            ShaderTagId lightModeTag = new ShaderTagId("LightMode");
            for (int i = 0; i < shader.passCount; i++)
            {
                string lightMode = shader.FindPassTagValue(i, lightModeTag).name;
                if (string.IsNullOrEmpty(lightMode)) return true; // 태그 없는 패스는 URP도 그린다
                if (lightMode == "UniversalForward" || lightMode == "UniversalForwardOnly" || lightMode == "SRPDefaultUnlit") return true;
            }
            return false;
        }

        private static string GetPoolReport()
        {
            string report = "[DragonNestMapBuilder] 쓸 수 있는 에셋 수\n";
            report += $"지형 메쉬 {Count(moundPool)}, 절벽 {Count(cliffPool)}, 바위 {Count(rockPool)}, 큰 바위 {Count(bigRockPool)}, 선돌 {Count(standingStonePool)}\n";
            report += $"기둥 {Count(pillarPool)}, 벽 {Count(wallPool)}, 아치 {Count(archPool)}, 계단 {Count(stairsPool)}, 돌 단 {Count(platformPool)}, 부서진 탑 {Count(towerPool)}, 성벽 {Count(castleWallPool)}\n";
            report += $"나무 {Count(treePool)}, 마른 나무 {Count(deadTreePool)}, 작은 소품 {Count(smallPropPool)}, 풀 {Count(grassPool)}\n";
            report += $"건너뛴 에셋 {skipped.Count}개";
            for (int i = 0; i < skipped.Count; i++) report += "\n - " + skipped[i];
            return report;
        }

        private static string GetSizeReport(string title, List<GameObject> pool)
        {
            string text = $"== {title}\n";
            for (int i = 0; i < Count(pool); i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(pool[i]);
                Bounds bounds = GetModelBounds(instance);
                text += $"{pool[i].name} : {bounds.size.x:0.0} x {bounds.size.y:0.0} x {bounds.size.z:0.0}\n";
                Object.DestroyImmediate(instance);
            }
            return text;
        }

        private static int Count(List<GameObject> pool)
        {
            return (pool == null) ? 0 : pool.Count;
        }

        #endregion

        #region 높이 함수 (바닥 모양)

        // (x, z) 지점의 바닥 높이. 바닥 메쉬와 배치가 모두 이 함수를 쓴다
        private static float GetHeight(float x, float z)
        {
            float noise = GetNoise(x, z);

            // 1. 기본 바닥 : 노이즈 + 언덕 (가우스 모양이라 경사가 완만하다. 최대 약 22도)
            float h = noise;
            h += Hill(x, z, 40f, 105f, 9f, 6f);   // 서쪽 언덕
            h += Hill(x, z, 48f, 136f, 6f, 4f);
            h += Hill(x, z, 162f, 100f, 10f, 4f); // 동쪽 숲 언덕
            h += Hill(x, z, 150f, 132f, 8f, 3f);

            // 2. 용의 둥지 : 둥지 바닥은 조금 꺼지고 평평하게 (가운데 제단은 돌 블록으로 놓는다)
            float lairDistance = Distance(x, z, CenterX, CenterZ);
            if (lairDistance < BasinRadius)
            {
                float t = 1f - lairDistance / BasinRadius;
                h = h * (1f - t) - BasinDepth * t * t * (3f - 2f * t); // 가운데로 갈수록 평평하고 낮게
            }

            // 3. 고원 : 가장 높은 값. 위에는 노이즈를 조금만
            float plateau = 0f;
            for (int i = 0; i < PlateauHeight.Length; i++) plateau = Mathf.Max(plateau, GetPlateauHeight(i, x, z));
            h = Mathf.Max(h, plateau + noise * 0.4f);

            return h;
        }

        private static float GetNoise(float x, float z)
        {
            float big = (Mathf.PerlinNoise(x * 0.04f + 11f, z * 0.04f + 23f) - 0.5f) * 2.4f;
            float small = (Mathf.PerlinNoise(x * 0.12f + 57f, z * 0.12f + 91f) - 0.5f) * 0.8f;
            float noise = big + small;

            // 시작 지점 주변은 평평하게
            float startDistance = Distance(x, z, StartX, StartZ);
            if (startDistance < StartClear) noise *= startDistance / StartClear;
            return noise;
        }

        private static float Hill(float x, float z, float centerX, float centerZ, float spread, float height)
        {
            float d = Distance(x, z, centerX, centerZ);
            return height * Mathf.Exp(-(d * d) / (2f * spread * spread));
        }

        // [변경] 고원 높이 : 조각(원) 안은 꼭대기 높이, 밖은 비탈 통로 안이면 완만하게(비탈), 아니면 3m 만에 떨어진다(절벽)
        private static float GetPlateauHeight(int plateau, float x, float z)
        {
            float height = PlateauHeight[plateau];
            float outside = GetPlateauOutside(plateau, x, z);
            if (outside <= 0f) return height;

            float length = IsInRamp(plateau, x, z, 0f) ? height * RampPerHeight : CliffLength;
            return height * Mathf.Clamp01(1f - outside / length);
        }

        // 새로 추가 : 고원 가장자리에서 바깥으로 나간 거리 (조각 중 가장 가까운 것 기준. 안이면 음수)
        private static float GetPlateauOutside(int plateau, float x, float z)
        {
            float outside = 9999f;
            for (int i = 0; i < PartPlateau.Length; i++)
            {
                if (PartPlateau[i] != plateau) continue;
                Vector3 center = GetPartCenter(i);
                outside = Mathf.Min(outside, Distance(x, z, center.x, center.z) - PartRadius[i]);
            }
            return outside;
        }

        // 새로 추가 : 고원 조각(원)의 중심
        private static Vector3 GetPartCenter(int part)
        {
            float radian = PartAngle[part] * Mathf.Deg2Rad;
            return new Vector3(CenterX + Mathf.Cos(radian) * PartArc[part], 0f, CenterZ + Mathf.Sin(radian) * PartArc[part]);
        }

        // 새로 추가 : 이 고원의 비탈 통로 안인지 (margin만큼 넓게 볼 수 있다)
        //  통로 = 시작점(고원 안쪽 가장자리)에서 대각선(RampTurn) 방향으로, 폭 RampWidth, 길이 높이 x 1.5 x RampLengthScale
        private static bool IsInRamp(int plateau, float x, float z, float margin)
        {
            for (int i = 0; i < RampPlateau.Length; i++)
            {
                if (RampPlateau[i] != plateau) continue;

                float radian = RampAngle[i] * Mathf.Deg2Rad;
                Vector3 outDirection = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian)); // 맵 중심 → 고원
                Vector3 start = new Vector3(CenterX, 0f, CenterZ) + outDirection * RampStart[i];
                Vector3 toPoint = new Vector3(x, 0f, z) - start;

                // [변경] 내려가는 방향 = 맵 중심 쪽에서 RampTurn만큼 고원 가장자리를 따라 돌린 대각선
                float turn = RampTurn[i] * Mathf.Deg2Rad;
                Vector3 inDirection = -outDirection;
                Vector3 downDirection = new Vector3(inDirection.x * Mathf.Cos(turn) - inDirection.z * Mathf.Sin(turn), 0f,
                                                    inDirection.x * Mathf.Sin(turn) + inDirection.z * Mathf.Cos(turn));

                float along = Vector3.Dot(toPoint, downDirection);                              // 통로를 따라 내려간 거리
                float side = Mathf.Abs(Vector3.Dot(toPoint, new Vector3(-downDirection.z, 0f, downDirection.x))); // 옆으로 벗어난 거리
                float length = PlateauHeight[plateau] * RampPerHeight * RampLengthScale; // [변경] 대각선이라 바닥까지 더 길게
                // 시작은 고원 안쪽 8m부터 (시작점과 실제 가장자리가 몇 m 어긋나도 절벽 턱이 생기지 않게)
                if (along > -8f - margin && along < length + 3f + margin && side < RampWidth * 0.5f + margin) return true;
            }
            return false;
        }

        // 비탈(경사로) 위인지. 이 구역에는 장애물을 놓지 않는다 (길이 막히지 않게)
        private static bool IsRampArea(float x, float z)
        {
            for (int i = 0; i < PlateauHeight.Length; i++)
            {
                if (IsInRamp(i, x, z, 2f)) return true;
            }
            return false;
        }

        // 경사 (도). 높이 함수로 바로 계산
        private static float GetSlope(float x, float z)
        {
            float dx = GetHeight(x + 0.5f, z) - GetHeight(x - 0.5f, z);
            float dz = GetHeight(x, z + 0.5f) - GetHeight(x, z - 0.5f);
            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
        }

        // 실제 바닥 높이 (Proxy 지형 메쉬, 돌 단까지 포함). 레이가 안 맞으면 높이 함수
        private static float GetGroundY(float x, float z)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(x, 300f, z), Vector3.down, out hit, 600f, groundMask, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return GetHeight(x, z);
        }

        // 반지름 안에서 가장 낮은 / 높은 바닥
        private static float GetLowestGroundY(float x, float z, float radius)
        {
            float lowest = GetGroundY(x, z);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                lowest = Mathf.Min(lowest, GetGroundY(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius));
            }
            return lowest;
        }

        private static float GetHighestGroundY(float x, float z, float radius)
        {
            float highest = GetGroundY(x, z);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                highest = Mathf.Max(highest, GetGroundY(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius));
            }
            return highest;
        }

        private static float Distance(float x1, float z1, float x2, float z2)
        {
            float dx = x1 - x2;
            float dz = z1 - z2;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float DistanceFromCenter(float x, float z)
        {
            return Distance(x, z, CenterX, CenterZ);
        }

        #endregion

        #region 바닥 메쉬 / 경계

        // MapGrid 범위(0 ~ 200)를 40m 조각으로 나눠 만들고, 원 밖 칸은 비운다
        private static void BuildGroundMeshes(Transform parent)
        {
            int chunkCount = Mathf.CeilToInt(200f / ChunkSize);
            for (int cx = 0; cx < chunkCount; cx++)
            {
                for (int cz = 0; cz < chunkCount; cz++)
                {
                    float startX = cx * ChunkSize;
                    float startZ = cz * ChunkSize;
                    Mesh mesh = CreateGroundMesh(startX, startZ);
                    if (mesh == null) continue; // 조각 전체가 원 밖

                    mesh.name = $"Ground_{cx}_{cz}";
                    SaveMesh(mesh);

                    GameObject chunk = new GameObject(mesh.name);
                    chunk.transform.SetParent(parent, false);
                    chunk.transform.position = new Vector3(startX, 0f, startZ);
                    chunk.layer = groundLayer;
                    chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                    MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
                    if (terrainMaterial != null) renderer.sharedMaterial = terrainMaterial;
                    chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
                    SetStatic(chunk);
                }
            }
        }

        // (startX, startZ)에서 ChunkSize 만큼의 바닥 메쉬. 정점 간격 1m. 원 밖 칸(사각형)은 넣지 않는다
        private static Mesh CreateGroundMesh(float startX, float startZ)
        {
            int size = ChunkSize + 1;
            Vector3[] vertices = new Vector3[size * size];
            Vector3[] normals = new Vector3[size * size];
            Vector2[] uvs = new Vector2[size * size];

            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    float worldX = startX + x;
                    float worldZ = startZ + z;
                    int index = z * size + x;
                    vertices[index] = new Vector3(x, GetHeight(worldX, worldZ), z);

                    // 법선을 높이 함수로 직접 계산 → 조각 경계에서도 빛이 끊기지 않는다
                    float dx = GetHeight(worldX + 1f, worldZ) - GetHeight(worldX - 1f, worldZ);
                    float dz = GetHeight(worldX, worldZ + 1f) - GetHeight(worldX, worldZ - 1f);
                    normals[index] = new Vector3(-dx, 2f, -dz).normalized;
                    uvs[index] = new Vector2(worldX / 6f, worldZ / 6f);
                }
            }

            List<int> triangles = new List<int>();
            for (int z = 0; z < ChunkSize; z++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    if (DistanceFromCenter(startX + x + 0.5f, startZ + z + 0.5f) > GroundRadius) continue; // 원 밖

                    int a = z * size + x;
                    int b = a + size;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                }
            }
            if (triangles.Count == 0) return null;

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // 원 둘레 보이지 않는 벽 (나가지 못하게만. 보이는 것 없음)
        private static void BuildBoundary(Transform parent)
        {
            const int segmentCount = 72;
            float length = 2f * Mathf.PI * WallRadius / segmentCount + 1f; // 조각 사이 틈이 없게 조금 길게
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * 360f / segmentCount;
                float radian = angle * Mathf.Deg2Rad;
                GameObject wall = new GameObject($"Wall_{i}");
                wall.transform.SetParent(parent, false);
                wall.transform.position = new Vector3(CenterX + Mathf.Cos(radian) * WallRadius, 30f, CenterZ + Mathf.Sin(radian) * WallRadius);
                wall.transform.rotation = Quaternion.Euler(0f, -angle, 0f); // 상자의 z(길이)가 원을 따라가게
                wall.layer = obstacleLayer;
                wall.AddComponent<BoxCollider>().size = new Vector3(3f, 80f, length);
                SetStatic(wall);
            }
        }

        #endregion

        #region Proxy 지형 메쉬 (언덕 섞기)

        // Proxy 지형 메쉬를 납작하게 눌러 바닥 위에 섞는다. 원래 MeshCollider 그대로 + Ground 레이어 + 지형 머티리얼
        private static void ScatterMounds(Transform parent)
        {
            if (Count(moundPool) == 0) return;

            for (int made = 0, attempt = 0; made < MoundCount && attempt < MoundCount * 40; attempt++)
            {
                float foot = Random.Range(14f, 22f);
                float x = Random.Range(CenterX - PlayRadius, CenterX + PlayRadius);
                float z = Random.Range(CenterZ - PlayRadius, CenterZ + PlayRadius);
                if (CanPlace(x, z, foot * 0.5f, 25f) == false) continue;
                if (GetHeight(x, z) > 4f) continue; // 고원 위 / 가장자리는 빼고 아래 바닥에만

                GameObject prefab = Pick(moundPool);
                GameObject holder = CreateHolder(prefab, parent, "Mound_" + prefab.name);
                Transform model = holder.transform.GetChild(0);

                // 가로 foot, 높이 foot의 0.12 ~ 0.18 (경사가 몹 한계보다 완만하게)
                FitToSize(model, foot, foot * Random.Range(0.12f, 0.18f), foot);
                SetupColliders(holder, model, groundLayer); // [변경] 지형이라 대부분 메쉬 그대로 충돌
                if (terrainMaterial != null) SetMaterial(model.gameObject, terrainMaterial);

                holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                PlaceOnGround(holder, model, x, z, 1f); // [변경] 발밑 전체에서 가장 낮은 곳 - 1m
                SetStatic(holder);

                AddPlaced(x, z, foot * 0.5f);
                made++;
            }
        }

        #endregion

        #region 고원 절벽 (보이기만)

        // 고원 둘레 중 비탈이 아닌 곳(절벽 면)을 바위로 덮는다
        // [변경] 조각(원)마다 둘레를 돌되, 다른 조각 안에 들어가는 곳(조각이 겹친 안쪽)은 건너뛴다
        private static void BuildPlateauCliffs(Transform parent)
        {
            if (Count(cliffPool) == 0 && Count(standingStonePool) == 0) return;

            for (int i = 0; i < PartPlateau.Length; i++)
            {
                int plateau = PartPlateau[i];
                Vector3 center = GetPartCenter(i);
                float radius = PartRadius[i] + 1.5f;
                int count = Mathf.CeilToInt(2f * Mathf.PI * radius / 8f); // 8m마다 하나
                for (int c = 0; c < count; c++)
                {
                    float radian = c * Mathf.PI * 2f / count;
                    float x = center.x + Mathf.Cos(radian) * radius;
                    float z = center.z + Mathf.Sin(radian) * radius;
                    if (GetPlateauOutside(plateau, x, z) < 1f) continue;  // 다른 조각과 겹친 안쪽
                    if (IsInRamp(plateau, x, z, 1f)) continue;            // 비탈은 비워 둔다
                    if (DistanceFromCenter(x, z) > WallRadius) continue;  // 벽 밖은 안 보임

                    Vector3 outward = new Vector3(x - center.x, 0f, z - center.z); // 고원 바깥 = 플레이어가 보는 쪽
                    PlaceCliff(parent, x, z, PlateauHeight[plateau] + Random.Range(-0.5f, 0.8f), 9f, outward); // [변경] 높이 → 꼭대기 높이
                }
            }
        }

        // [변경] view 추가 : 뒷면이 빈 절벽 메쉬는 보이는 면이 view(고원 바깥)를 향하게 돌린다 (빈 뒷면은 고원 안쪽 흙에 묻힌다)
        // [변경] height → topY : 절벽 꼭대기를 고원 윗면 높이에 맞춘다 (예전엔 고원보다 최대 20% 솟아서 고원 위에서 절벽 뒷면 / 윗면이 보였다)
        //  절벽 메쉬는 옆 / 위 / 뒤가 뚫려 있어서 꼭대기가 고원 위로 솟으면 고원 위에서 빈 곳이 보인다
        private static void PlaceCliff(Transform parent, float x, float z, float topY, float width, Vector3 view)
        {
            List<GameObject> pool = (Count(cliffPool) > 0) ? cliffPool : standingStonePool;
            GameObject prefab = Pick(pool);
            GameObject holder = CreateHolder(prefab, parent, "Cliff_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            float baseY = GetLowestGroundY(x, z, width * 0.7f) - 2f; // 새로 추가
            FitToSize(model, width * Random.Range(0.8f, 1.2f), topY - baseY, width * Random.Range(0.6f, 1.0f));
            if (pool == cliffPool && terrainMaterial != null) SetMaterial(model.gameObject, terrainMaterial); // 깨진 셰이더 대신

            holder.transform.rotation = Quaternion.Euler(0f, GetFacingYaw(prefab, view) + Random.Range(-8f, 8f), 0f); // [변경] ±15 → ±8도 (옆 틈이 덜 벌어지게)
            SetupColliders(holder, model, obstacleLayer); // [변경] 보이기만 → Obstacle (모든 오브젝트가 이동에 영향)
            PlaceOnGround(holder, model, x, z, 2f);
            SetStatic(holder);
        }

        #endregion

        #region 용의 둥지 (바위 테두리 + 제단)

        private static void BuildLair(Transform parent)
        {
            // 1. 바위 테두리 : 큰 바위 / 선돌 / 성벽. 막는 것도 바위 자체 콜라이더
            //  [변경] 삭제 : 보이지 않는 상자 벽 조각 (LairWall_Collider). 바위 모양과 달라서 눈으로 보이는 것과 막히는 곳이 달랐다
            const float step = 9f;
            for (float angle = 0f; angle < 360f; angle += step)
            {
                if (IsLairGate(angle)) continue;

                float radian = angle * Mathf.Deg2Rad;
                float x = CenterX + Mathf.Cos(radian) * LairWallRadius;
                float z = CenterZ + Mathf.Sin(radian) * LairWallRadius;

                // 보이는 것 : 큰 바위 사이사이 선돌 / 성벽
                int order = Mathf.RoundToInt(angle / step) % 3;
                List<GameObject> pool = bigRockPool;
                if (order == 1 && Count(standingStonePool) > 0) pool = standingStonePool;
                if (order == 2 && Count(castleWallPool) > 0) pool = castleWallPool;
                if (Count(pool) == 0) continue;

                GameObject prefab = Pick(pool);
                GameObject holder = CreateHolder(prefab, parent, "LairWall_" + prefab.name);
                Transform model = holder.transform.GetChild(0);
                FitToHeight(model, LairWallHeight * Random.Range(1.0f, 1.2f), 9f);
                SetupColliders(holder, model, obstacleLayer); // [변경] 보이기만 → Obstacle
                holder.transform.rotation = Quaternion.Euler(0f, -angle + 90f + Random.Range(-15f, 15f), 0f);
                PlaceOnGround(holder, model, x, z, 1f); // [변경] 발밑 전체(9m 바위)에서 가장 낮은 곳 - 1m. 예전엔 반지름 2m만 봐서 경사 쪽 바닥이 떴다
                SetStatic(holder);
            }

            // 2. [변경] 둥지 안은 비우고 정중앙에 돌 제단 하나 (PillarBase_Square. 없으면 다른 돌 단)
            //  삭제 : 2단 제단 / 가장자리 돌판 / 대각선 기둥 / 나뭇가지 둥지 / 해골 / 불 / 까마귀
            GameObject altarPrefab = FindByName(platformPool, "PillarBase_Square");
            if (altarPrefab == null && Count(platformPool) > 0) altarPrefab = Pick(platformPool);
            if (altarPrefab != null)
            {
                float half = AltarSize * 0.5f;
                float baseY = GetLowestGroundY(CenterX, CenterZ, half) - 0.5f;
                float topY = GetHighestGroundY(CenterX, CenterZ, half) + AltarHeight; // 몹도 오르는 높이
                CreatePlatformBox(parent, altarPrefab, CenterX, CenterZ, AltarSize, AltarSize, baseY, topY, 0f);
            }
        }

        // 새로 추가 : 이름에 name이 들어간 첫 번째 프리팹 (없으면 null)
        private static GameObject FindByName(List<GameObject> pool, string name)
        {
            for (int i = 0; i < Count(pool); i++)
            {
                if (pool[i].name.Contains(name)) return pool[i];
            }
            return null;
        }

        // 동서남북 입구인지
        private static bool IsLairGate(float angle)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(angle, i * 90f)) < LairGateHalfAngle) return true;
            }
            return false;
        }

        #endregion

        #region 랜드마크 (북쪽 신전, 망루)

        private static void BuildLandmarks(Transform parent)
        {
            // 1. 북쪽 고원 신전 (북쪽 가운데 조각 위) : 기둥 두 줄 + 무너진 아치 + 계단 + 벽
            for (float x = 88f; x <= 112f; x += 8f)
            {
                PlaceRuin(parent, pillarPool, x, 167f, Random.Range(6.5f, 9f), "Temple_Pillar");
                PlaceRuin(parent, pillarPool, x, 177f, Random.Range(6.5f, 9f), "Temple_Pillar");
            }
            PlaceArch(parent, 100f, 159f, 8f);
            if (Count(stairsPool) > 0) PlaceDecor(parent, stairsPool, 100f, 156f, 1.2f, 0f, "Temple_Stairs");
            PlaceRuin(parent, wallPool, 84f, 172f, 3f, "Temple_Wall");
            PlaceRuin(parent, wallPool, 116f, 172f, 3f, "Temple_Wall");

            // 2. 남서 / 남동 고지 망루 (가운데 조각 위. Proxy 부서진 탑, 없으면 큰 기둥)
            List<GameObject> towers = (Count(towerPool) > 0) ? towerPool : pillarPool;
            Vector3 southWest = GetPartCenter(6);
            Vector3 southEast = GetPartCenter(9);
            PlaceRuin(parent, towers, southWest.x, southWest.z, 14f, "Tower_SW");
            PlaceRuin(parent, towers, southEast.x, southEast.z, 14f, "Tower_SE");
        }

        // 막는 폐허 (Obstacle, [변경] 에셋 콜라이더 그대로). 흩뿌리기 간격 목록에도 넣는다
        private static GameObject PlaceRuin(Transform parent, List<GameObject> pool, float x, float z, float height, string name)
        {
            if (Count(pool) == 0) return null;

            GameObject prefab = Pick(pool);
            GameObject holder = CreateHolder(prefab, parent, name + "_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            FitToHeight(model, height, Mathf.Max(6f, height)); // 최대 가로 : 벽 6m, 기둥 / 탑은 높이만큼
            SetupColliders(holder, model, obstacleLayer); // [변경] 에셋 콜라이더 그대로

            holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f + Random.Range(-10f, 10f), 0f);
            PlaceOnGround(holder, model, x, z, 0.3f); // [변경] 발밑 전체 기준
            SetStatic(holder);
            AddPlaced(x, z, 2f);
            return holder;
        }

        // 무너진 아치 : 지나갈 수 있게 양쪽 다리에만 기둥 콜라이더
        private static void PlaceArch(Transform parent, float x, float z, float height)
        {
            if (Count(archPool) == 0) return;

            GameObject prefab = Pick(archPool);
            GameObject holder = CreateHolder(prefab, parent, "Temple_Arch_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            FitToHeight(model, height, 10f);

            SetupColliders(holder, model, obstacleLayer); // [변경] 다리 캡슐 2개 → 에셋 콜라이더 그대로 (아치 가운데는 모양대로 지나갈 수 있다)

            PlaceOnGround(holder, model, x, z, 0.3f); // [변경] 발밑 전체 기준
            SetStatic(holder);
            AddPlaced(x, z, 3f);
        }

        // 밟고 올라가는 장식 ([변경] Ground + 에셋 콜라이더 그대로)
        private static void PlaceDecor(Transform parent, List<GameObject> pool, float x, float z, float height, float rotationY, string name)
        {
            GameObject prefab = Pick(pool);
            GameObject holder = CreateHolder(prefab, parent, name + "_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            FitToHeight(model, height, Mathf.Max(2f, height * 4f)); // 최대 가로 : 작은 소품 2m, 계단은 넓게
            SetupColliders(holder, model, groundLayer); // [변경] 보이기만 → Ground (밟고 올라감)
            holder.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            PlaceOnGround(holder, model, x, z, 0.1f); // [변경] 가운데 한 점 → 발밑 전체 기준 (경사에서 소품 바닥이 뜨지 않게)
            SetStatic(holder);
        }

        #endregion

        #region 유적 (건물 터 / 돌 단 / 선돌)

        // 무너진 건물 터 : 사각형 둘레를 벽 조각으로 두르되 군데군데 비운다 (못 지나가는 구조물)
        //  - 남쪽 가운데 한 칸은 항상 비워 입구를 만든다
        //  - 모서리는 기둥, 안에는 가끔 올라가는 돌 단
        private static void ScatterRuinCompounds(Transform ruinParent, Transform platformParent)
        {
            if (Count(wallPool) == 0) return;

            for (int made = 0, attempt = 0; made < RuinCompoundCount && attempt < RuinCompoundCount * 50; attempt++)
            {
                float width = Random.Range(9f, 14f);
                float depth = Random.Range(8f, 11f);
                float x = Random.Range(CenterX - PlayRadius, CenterX + PlayRadius);
                float z = Random.Range(CenterZ - PlayRadius, CenterZ + PlayRadius);
                float radius = Mathf.Max(width, depth) * 0.5f + 2f;
                if (CanPlace(x, z, radius, 18f) == false) continue;

                GameObject compound = new GameObject($"RuinCompound_{made}");
                compound.transform.SetParent(ruinParent, false);

                BuildCompoundEdge(compound.transform, x - width * 0.5f, z - depth * 0.5f, width, true, true);   // 남쪽 (입구)
                BuildCompoundEdge(compound.transform, x - width * 0.5f, z + depth * 0.5f, width, true, false);  // 북쪽
                BuildCompoundEdge(compound.transform, x - width * 0.5f, z - depth * 0.5f, depth, false, false); // 서쪽
                BuildCompoundEdge(compound.transform, x + width * 0.5f, z - depth * 0.5f, depth, false, false); // 동쪽

                // 모서리 기둥 (절반 정도만 남아 있음)
                for (int corner = 0; corner < 4; corner++)
                {
                    if (Random.value < 0.5f) continue;
                    float cornerX = x + ((corner % 2 == 0) ? -0.5f : 0.5f) * width;
                    float cornerZ = z + ((corner < 2) ? -0.5f : 0.5f) * depth;
                    PlaceRuinPiece(compound.transform, pillarPool, cornerX, cornerZ, Random.Range(4.5f, 7f), 3f, Random.Range(0, 4) * 90f);
                }

                // 건물 터 가운데 : 가끔 올라가는 돌 단
                if (Random.value < 0.4f) PlacePlatform(platformParent, x, z + 0.5f, width * 0.45f, depth * 0.45f, 1);

                AddPlaced(x, z, radius);
                made++;
            }
        }

        // 한 변을 3.5m 조각으로 나눠 벽을 세운다. 30%는 무너져 비어 있다. alongX = 이 변이 x 방향인지
        private static void BuildCompoundEdge(Transform parent, float startX, float startZ, float length, bool alongX, bool hasGate)
        {
            int segments = Mathf.Max(2, Mathf.RoundToInt(length / 3.5f));
            float segmentLength = length / segments;
            for (int i = 0; i < segments; i++)
            {
                if (hasGate && i == segments / 2) continue; // 입구
                if (Random.value < 0.3f) continue;          // 무너져서 비어 있음

                float offset = (i + 0.5f) * segmentLength;
                float x = alongX ? startX + offset : startX;
                float z = alongX ? startZ : startZ + offset;
                float height = (Random.value < 0.6f) ? Random.Range(2.2f, 3.3f) : Random.Range(4f, 5.5f); // 낮은 벽은 1단 점프로 넘는다
                PlaceRuinPiece(parent, wallPool, x, z, height, segmentLength * 1.1f, alongX ? 0f : 90f);
            }
        }

        // 유적 조각 하나 (Obstacle, [변경] 에셋 콜라이더 그대로). 모델의 긴 쪽을 rotationY 방향(0 = x축)에 맞춘다
        private static void PlaceRuinPiece(Transform parent, List<GameObject> pool, float x, float z, float height, float maxWidth, float rotationY)
        {
            if (Count(pool) == 0) return;

            GameObject prefab = Pick(pool);
            GameObject holder = CreateHolder(prefab, parent, "Ruin_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            FitToHeight(model, height, maxWidth);
            SetupColliders(holder, model, obstacleLayer); // [변경] 에셋 콜라이더 그대로

            Bounds bounds = GetModelBounds(model.gameObject);
            float turn = (bounds.size.x >= bounds.size.z) ? 0f : 90f; // 모델의 긴 쪽이 x가 아니면 90도 더 돌린다
            holder.transform.rotation = Quaternion.Euler(0f, rotationY + turn + Random.Range(-6f, 6f), 0f);
            PlaceOnGround(holder, model, x, z, 0.3f); // [변경] 발밑 전체 기준. 예전엔 반지름 1.5m만 봐서 긴 벽은 경사 쪽 바닥이 떴다
            SetStatic(holder);
        }

        // 따로 떨어진 돌 단 (올라갈 수 있는 지형물)
        private static void ScatterPlatforms(Transform parent)
        {
            for (int made = 0, attempt = 0; made < PlatformCount && attempt < PlatformCount * 50; attempt++)
            {
                float width = Random.Range(6f, 10f);
                float depth = Random.Range(5f, 8f);
                float x = Random.Range(CenterX - PlayRadius, CenterX + PlayRadius);
                float z = Random.Range(CenterZ - PlayRadius, CenterZ + PlayRadius);
                float radius = Mathf.Max(width, depth) * 0.5f + 1.5f;
                if (CanPlace(x, z, radius, 15f) == false) continue;

                int tiers = (Random.value < 0.4f) ? 2 : 1;
                PlacePlatform(parent, x, z, width, depth, tiers);

                // 단 위 모서리에 부러진 기둥 (가끔)
                if (Random.value < 0.5f)
                {
                    float cornerX = x + Random.Range(-0.35f, 0.35f) * width;
                    float cornerZ = z + Random.Range(-0.35f, 0.35f) * depth;
                    PlaceRuinPiece(parent, pillarPool, cornerX, cornerZ, Random.Range(3f, 5.5f), 2.5f, 0f);
                }

                AddPlaced(x, z, radius);
                made++;
            }
        }

        // 올라갈 수 있는 돌 단 (Ground + [변경] 에셋 콜라이더 그대로. 한 층 0.9m → 몹도 오름). tiers = 2면 위에 작은 단을 하나 더
        private static void PlacePlatform(Transform parent, float x, float z, float width, float depth, int tiers)
        {
            if (Count(platformPool) == 0) return;

            float rotationY = Random.Range(0, 4) * 90f + Random.Range(-8f, 8f);
            float baseY = GetLowestGroundY(x, z, Mathf.Max(width, depth) * 0.5f) - 0.5f; // 아래는 땅에 묻는다
            float topY = GetHighestGroundY(x, z, Mathf.Max(width, depth) * 0.5f) + PlatformStep;
            CreatePlatformBox(parent, Pick(platformPool), x, z, width, depth, baseY, topY, rotationY);

            if (tiers >= 2)
                CreatePlatformBox(parent, Pick(platformPool), x, z, width * 0.55f, depth * 0.55f, topY - 0.3f, topY + PlatformStep, rotationY);

            Physics.SyncTransforms(); // 위에 놓는 것이 단 높이를 읽을 수 있게
        }

        // [변경] prefab을 받는다 (둥지 제단은 PillarBase_Square로 정해서 쓴다)
        private static void CreatePlatformBox(Transform parent, GameObject prefab, float x, float z, float width, float depth, float baseY, float topY, float rotationY)
        {
            float height = topY - baseY;
            GameObject holder = CreateHolder(prefab, parent, "Platform_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            FitToSize(model, width, height, depth); // 돌판을 상자 모양으로 늘린다

            // 단 아래를 baseY에 맞춘다 (holder 기준 아래가 0)
            holder.transform.position = new Vector3(x, baseY, z);
            Bounds bounds = GetModelBounds(model.gameObject);
            model.position += new Vector3(x - bounds.center.x, baseY - bounds.min.y, z - bounds.center.z);
            SetupColliders(holder, model, groundLayer); // [변경] 늘린 상자 그대로 → 에셋 콜라이더 그대로 (모델과 같이 늘어난다)
            holder.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            SetStatic(holder);
        }

        // 선돌 무리 : 둥글게 세운 큰 돌 5 ~ 7개 (옛 신전 흔적. 못 지나감)
        private static void ScatterStoneCircles(Transform parent)
        {
            if (Count(standingStonePool) == 0) return;

            for (int made = 0, attempt = 0; made < StoneCircleCount && attempt < StoneCircleCount * 50; attempt++)
            {
                float x = Random.Range(CenterX - PlayRadius, CenterX + PlayRadius);
                float z = Random.Range(CenterZ - PlayRadius, CenterZ + PlayRadius);
                if (CanPlace(x, z, 9f, 15f) == false) continue;

                GameObject circle = new GameObject($"StoneCircle_{made}");
                circle.transform.SetParent(parent, false);
                int count = Random.Range(5, 8);
                for (int i = 0; i < count; i++)
                {
                    float radian = (i * 360f / count + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
                    float stoneX = x + Mathf.Cos(radian) * 6f;
                    float stoneZ = z + Mathf.Sin(radian) * 6f;
                    PlaceRuinPiece(circle.transform, standingStonePool, stoneX, stoneZ, Random.Range(3.5f, 6f), 3f, -radian * Mathf.Rad2Deg + 90f);
                }

                AddPlaced(x, z, 8f);
                made++;
            }
        }

        #endregion

        #region 흩뿌리기 (바위 / 나무 / 소품)

        private static void ScatterRocks(Transform parent)
        {
            // 1. 바위 : 대부분 1단 점프로 넘는 높이(1.5 ~ 3.2m, 몹은 막힘), 일부는 2단 점프 높이
            for (int made = 0, attempt = 0; made < RockCount && attempt < RockCount * 40; attempt++)
            {
                float x = Random.Range(CenterX - PlayRadius, CenterX + PlayRadius);
                float z = Random.Range(CenterZ - PlayRadius, CenterZ + PlayRadius);
                if (CanPlace(x, z, ObstacleSpacing, 25f) == false) continue;

                bool big = Random.value < 0.25f;
                float height = big ? Random.Range(4.5f, 7f) : Random.Range(1.5f, 3.2f);
                if (PlaceObstacle(parent, big ? bigRockPool : rockPool, x, z, height, "Rock")) made++;
            }

            // 2. 북쪽 고원 위 바위 (신전 자리는 피한다)
            for (int made = 0, attempt = 0; made < PlateauRockCount && attempt < PlateauRockCount * 40; attempt++)
            {
                float x = Random.Range(40f, 160f);
                float z = Random.Range(135f, 188f);
                if (GetPlateauOutside(0, x, z) > -2f) continue; // [변경] 북쪽 고원 안쪽만
                if (Mathf.Abs(x - 100f) < 20f && z > 153f) continue; // 신전 자리
                if (CanPlace(x, z, ObstacleSpacing, 25f) == false) continue;
                if (PlaceObstacle(parent, rockPool, x, z, Random.Range(1.5f, 3.5f), "PlateauRock")) made++;
            }
        }

        // 장애물 하나 (Obstacle + [변경] 에셋 콜라이더 그대로)
        private static bool PlaceObstacle(Transform parent, List<GameObject> pool, float x, float z, float height, string name)
        {
            if (Count(pool) == 0) return false;

            GameObject prefab = Pick(pool);
            GameObject holder = CreateHolder(prefab, parent, name + "_" + prefab.name);
            Transform model = holder.transform.GetChild(0);
            FitToHeight(model, height, Mathf.Max(3f, height * 1.6f)); // 최대 가로 : 넓적한 바위가 너무 커지지 않게
            SetupColliders(holder, model, obstacleLayer); // [변경] 에셋 콜라이더 그대로

            holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Bounds bounds = GetModelBounds(model.gameObject);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            PlaceOnGround(holder, model, x, z, 0.3f); // [변경] 발밑 전체 기준
            SetStatic(holder);

            AddPlaced(x, z, Mathf.Max(radius, 1f));
            return true;
        }

        private static void ScatterTrees(Transform parent)
        {
            // 1. 동쪽 숲 (빽빽하게)
            ScatterTreeArea(parent, treePool, ForestTreeCount, 120f, 186f, 60f, 150f, 3f);
            // 2. 맵 전체 (드문드문)
            ScatterTreeArea(parent, treePool, SparseTreeCount, CenterX - PlayRadius, CenterX + PlayRadius, CenterZ - PlayRadius, CenterZ + PlayRadius, 6f);
            // 3. 둥지 바깥 마른 나무 (드래곤이 지나간 흔적)
            ScatterTreeArea(parent, deadTreePool, DeadTreeCount, CenterX - 45f, CenterX + 45f, CenterZ - 45f, CenterZ + 45f, 4f);
        }

        private static void ScatterTreeArea(Transform parent, List<GameObject> pool, int count, float minX, float maxX, float minZ, float maxZ, float spacing)
        {
            if (Count(pool) == 0) return;

            for (int made = 0, attempt = 0; made < count && attempt < count * 40; attempt++)
            {
                float x = Random.Range(minX, maxX);
                float z = Random.Range(minZ, maxZ);
                if (CanPlace(x, z, spacing, 28f) == false) continue;

                GameObject prefab = Pick(pool);
                GameObject holder = CreateHolder(prefab, parent, "Tree_" + prefab.name);
                Transform model = holder.transform.GetChild(0);
                FitToHeight(model, Random.Range(7f, 12f), 9f);

                // [변경] 줄기 캡슐 → 줄기 원통 (가지 / 잎은 안 부딪힘). 줄기 굵기는 메쉬에서 재고, 높이는 2단 점프로 못 올라가게
                SetLayerRecursive(holder, obstacleLayer);
                AddTreeCollider(holder, model);

                holder.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                PlaceOnGround(holder, model, x, z, 0.2f, true); // 나무는 줄기(피벗)를 그 자리에
                SetStatic(holder);

                AddPlaced(x, z, 1f);
                made++;
            }
        }

        // 밟고 지나가는 작은 돌 / 나뭇가지 ([변경] Ground + 에셋 콜라이더 그대로, 간격 검사 X)
        private static void ScatterSmallProps(Transform parent)
        {
            if (Count(smallPropPool) == 0) return;

            for (int made = 0, attempt = 0; made < SmallPropCount && attempt < SmallPropCount * 20; attempt++)
            {
                float x = Random.Range(CenterX - PlayRadius, CenterX + PlayRadius);
                float z = Random.Range(CenterZ - PlayRadius, CenterZ + PlayRadius);
                if (DistanceFromCenter(x, z) > PlayRadius) continue;
                if (GetSlope(x, z) > 30f) continue;
                if (Distance(x, z, StartX, StartZ) < 6f) continue;

                PlaceDecor(parent, smallPropPool, x, z, Random.Range(0.25f, 0.6f), Random.Range(0f, 360f), "Prop");
                made++;
            }
        }

        // 놓을 수 있는 자리인지 (원 안 / 둥지 / 시작 지점 / 비탈 / 경사 / 절벽 가장자리 / 간격)
        private static bool CanPlace(float x, float z, float radius, float maxSlope)
        {
            if (DistanceFromCenter(x, z) + radius > PlayRadius) return false;
            if (DistanceFromCenter(x, z) < LairClear + radius) return false;
            if (Distance(x, z, StartX, StartZ) < StartClear + radius) return false;
            if (IsRampArea(x, z)) return false;
            if (GetSlope(x, z) > maxSlope) return false;

            // 절벽 가장자리에 걸치지 않게 : 둘레 높이 차이가 크면 안 됨
            float center = GetHeight(x, z);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                float around = GetHeight(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius);
                if (Mathf.Abs(around - center) > 2f) return false;
                if (IsRampArea(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius)) return false;
            }

            for (int i = 0; i < placed.Count; i++)
            {
                Vector3 other = placed[i]; // x, 반지름, z
                if (Distance(x, z, other.x, other.z) < radius + other.y) return false;
            }
            return true;
        }

        private static void AddPlaced(float x, float z, float radius)
        {
            placed.Add(new Vector3(x, radius, z));
        }

        #endregion

        #region 풀 (구역마다 합치기)

        private static void BuildGrass(Transform parent)
        {
            if (Count(grassPool) == 0) return;

            float min = CenterX - PlayRadius;
            int chunkCount = Mathf.CeilToInt(PlayRadius * 2f / GrassChunk);
            for (int cx = 0; cx < chunkCount; cx++)
            {
                for (int cz = 0; cz < chunkCount; cz++)
                {
                    float startX = min + cx * GrassChunk;
                    float startZ = (CenterZ - PlayRadius) + cz * GrassChunk;

                    // 머티리얼마다 따로 모은다 (한 메쉬 = 한 머티리얼)
                    Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();
                    for (float x = startX; x < startX + GrassChunk; x += GrassStep)
                    {
                        for (float z = startZ; z < startZ + GrassChunk; z += GrassStep)
                        {
                            float px = x + Random.Range(-0.8f, 0.8f);
                            float pz = z + Random.Range(-0.8f, 0.8f);
                            if (Random.value > GetGrassChance(px, pz)) continue;
                            if (CanPlaceGrass(px, pz) == false) continue;

                            Matrix4x4 place = Matrix4x4.TRS(new Vector3(px, GetGroundY(px, pz) - 0.05f, pz),
                                                             Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                                                             Vector3.one * Random.Range(0.8f, 1.3f));
                            AddGrassMeshes(Pick(grassPool), place, groups);
                        }
                    }

                    CreateGrassObjects(parent, $"Grass_{cx}_{cz}", groups);
                }
            }
        }

        // 풀이 많은 곳 / 적은 곳을 큰 얼룩으로 나눈다
        private static float GetGrassChance(float x, float z)
        {
            float meadow = Mathf.PerlinNoise(x * 0.05f + 400f, z * 0.05f + 400f);
            return (meadow > 0.45f) ? 0.55f : 0.12f;
        }

        private static bool CanPlaceGrass(float x, float z)
        {
            if (DistanceFromCenter(x, z) > PlayRadius) return false;
            if (GetSlope(x, z) > GrassMaxSlope) return false;
            if (DistanceFromCenter(x, z) < LairWallRadius - 2f) return false; // 둥지 안은 흙바닥
            if (Distance(x, z, StartX, StartZ) < 4f) return false;

            float y = GetGroundY(x, z);
            return Physics.CheckSphere(new Vector3(x, y + 0.6f, z), 0.7f, obstacleMask, QueryTriggerInteraction.Ignore) == false;
        }

        // 프리팹 안의 메쉬들을 place 위치로 옮겨서 머티리얼별 목록에 넣는다
        private static void AddGrassMeshes(GameObject prefab, Matrix4x4 place, Dictionary<Material, List<CombineInstance>> groups)
        {
            Matrix4x4 rootInverse = prefab.transform.worldToLocalMatrix;
            Renderer[] renderers = GetLod0Renderers(prefab); // LOD가 있으면 가장 자세한 것(LOD0)만. 전부 합치면 같은 풀이 3겹으로 겹친다
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i] as MeshRenderer;
                if (renderer == null) continue;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = (filter != null) ? filter.sharedMesh : null;
                if (mesh == null) continue;

                Matrix4x4 local = rootInverse * renderer.transform.localToWorldMatrix; // 프리팹 루트 기준 위치
                Material[] materials = renderer.sharedMaterials;
                for (int sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
                {
                    if (materials[sub] == null) continue;
                    if (groups.ContainsKey(materials[sub]) == false) groups.Add(materials[sub], new List<CombineInstance>());

                    CombineInstance combine = new CombineInstance();
                    combine.mesh = mesh;
                    combine.subMeshIndex = sub;
                    combine.transform = place * local;
                    groups[materials[sub]].Add(combine);
                }
            }
        }

        // LODGroup이 있으면 LOD0 렌더러만, 없으면 전부
        private static Renderer[] GetLod0Renderers(GameObject prefab)
        {
            LODGroup lodGroup = prefab.GetComponentInChildren<LODGroup>(true);
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods.Length > 0 && lods[0].renderers.Length > 0) return lods[0].renderers;
            }
            return prefab.GetComponentsInChildren<Renderer>(true);
        }

        private static void CreateGrassObjects(Transform parent, string name, Dictionary<Material, List<CombineInstance>> groups)
        {
            int index = 0;
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in groups)
            {
                if (pair.Value.Count == 0) continue;

                Mesh mesh = new Mesh();
                mesh.name = $"{name}_{index}";
                mesh.indexFormat = IndexFormat.UInt32; // 정점이 6만 개를 넘을 수 있다
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                SaveMesh(mesh);

                GameObject grass = new GameObject(mesh.name);
                grass.transform.SetParent(parent, false);
                grass.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = grass.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off; // 풀은 그림자 X
                SetStatic(grass);
                index++;
            }
        }

        #endregion

        #region 표시 지점 / 이펙트

        private static void BuildMarkers(Transform parent)
        {
            CreateMarker(parent, "PlayerStart", StartX, StartZ);
            CreateMarker(parent, "BossSpawnPoint", CenterX, CenterZ);
            Vector3 north = GetPartCenter(2);
            Vector3 southWest = GetPartCenter(6);
            Vector3 southEast = GetPartCenter(9);
            CreateMarker(parent, "NestPoint_N", north.x, north.z);
            CreateMarker(parent, "NestPoint_SW", southWest.x + 5f, southWest.z);
            CreateMarker(parent, "NestPoint_SE", southEast.x - 5f, southEast.z);
        }

        private static void CreateMarker(Transform parent, string name, float x, float z)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(x, GetGroundY(x, z), z);
        }
        // [변경] 삭제 : PlaceEffect (둥지 이펙트를 쓰지 않음)

        #endregion

        #region 공용 도우미

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Pick(List<GameObject> pool)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        // 빈 부모(holder, 콜라이더 담당) 아래에 모델을 놓는다. 모델의 원래 콜라이더는 끈다
        private static GameObject CreateHolder(GameObject prefab, Transform parent, string name)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.position = Vector3.zero;
            holder.transform.rotation = Quaternion.identity;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // [변경] 원래 콜라이더는 여기서 건드리지 않는다. SetupColliders / AddTreeCollider가 남길지 지울지 정한다
            return holder;
        }

        // 모델 전체를 감싸는 월드 상자 (메쉬 크기 + Transform으로 직접 계산)
        //  방금 만든 오브젝트는 Renderer.bounds가 아직 0이라 쓰지 않는다. Transform 값은 항상 바로 반영된다
        private static Bounds GetModelBounds(GameObject model)
        {
            Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
            bool hasBounds = false;

            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh == null || filters[i].GetComponent<MeshRenderer>() == null) continue;
                hasBounds = AddMeshBounds(ref bounds, hasBounds, filters[i].sharedMesh.bounds, filters[i].transform.localToWorldMatrix);
            }

            SkinnedMeshRenderer[] skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i].sharedMesh == null) continue;
                hasBounds = AddMeshBounds(ref bounds, hasBounds, skins[i].sharedMesh.bounds, skins[i].transform.localToWorldMatrix);
            }
            return bounds;
        }

        // 메쉬 상자(로컬)의 꼭짓점 8개를 월드로 옮겨서 bounds에 넣는다
        private static bool AddMeshBounds(ref Bounds bounds, bool hasBounds, Bounds local, Matrix4x4 toWorld)
        {
            Vector3 min = local.min;
            Vector3 max = local.max;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 point = toWorld.MultiplyPoint3x4(corner);
                if (hasBounds) bounds.Encapsulate(point);
                else bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            return hasBounds;
        }

        // 높이가 height가 되게 같은 비율로 키운다. 가로(x, z 중 큰 쪽)가 maxWidth를 넘으면 그만큼만 (그때는 높이가 낮아진다)
        //  넓적한 메쉬(예 : 거석 가로 7m, 높이 1m)를 높이만 보고 키우면 가로가 수십 m가 되어 맵을 덮는다
        private static void FitToHeight(Transform model, float height, float maxWidth)
        {
            Bounds bounds = GetModelBounds(model.gameObject);
            if (bounds.size.y < 0.01f) return;

            float scale = height / bounds.size.y;
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            if (width * scale > maxWidth) scale = maxWidth / width;
            model.localScale = model.localScale * scale;
        }

        // 가로 / 높이 / 세로를 각각 맞춘다 (지형 메쉬, 절벽, 돌 단)
        private static void FitToSize(Transform model, float sizeX, float sizeY, float sizeZ)
        {
            Bounds bounds = GetModelBounds(model.gameObject);
            if (bounds.size.x < 0.01f || bounds.size.y < 0.01f || bounds.size.z < 0.01f) return;
            Vector3 scale = model.localScale;
            model.localScale = new Vector3(scale.x * sizeX / bounds.size.x, scale.y * sizeY / bounds.size.y, scale.z * sizeZ / bounds.size.z);
        }

        // [변경] UseMeshColliders → SetupColliders : 에셋에 들어 있는 콜라이더를 그대로 쓴다 (켜기만 한다)
        //  [변경] 삭제 : 부피로 상자 / 원통을 고르는 코드, LOD0 메쉬로 MeshCollider를 새로 만드는 코드 (영역을 이상하게 잡았다)
        //  콜라이더가 없는 에셋은 충돌이 없다 → 맵 만들기 로그에 이름을 남긴다
        //  움직이지 않는 콜라이더라 실행 중 비용은 거의 없다 (굽기 / 플레이어 충돌 / 투사체 레이에만 쓰임)
        private static void SetupColliders(GameObject holder, Transform model, int layer)
        {
            SetLayerRecursive(holder, layer);

            Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = true;

            if (colliders.Length > 0) assetColliderCount++;
            else if (noColliderNames.Contains(holder.name) == false) noColliderNames.Add(holder.name);
        }

        // ===== 새로 추가 : 에셋 콜라이더 정리 =====
        // 나무 원통을 따로 만들 때 에셋 콜라이더(캡슐 / 상자 / 메쉬)를 전부 지운다
        private static void RemoveColliders(Transform model)
        {
            Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) Object.DestroyImmediate(colliders[i]);
        }
        // ===== 새로 추가 끝 =====

        // ===== 새로 추가 : 원통 콜라이더 =====
        // 유니티에는 원통 콜라이더가 없어서, 유니티 기본 원통 메쉬(지름 1, 높이 2)를 볼록(convex) MeshCollider로 쓴다
        //  center / 크기는 holder 기준
        private static void AddCylinderCollider(GameObject holder, Vector3 center, float sizeX, float height, float sizeZ)
        {
            GameObject cylinder = new GameObject("Collider_Cylinder");
            cylinder.transform.SetParent(holder.transform, false);
            cylinder.transform.localPosition = center;
            cylinder.transform.localScale = new Vector3(sizeX, height * 0.5f, sizeZ);
            cylinder.layer = holder.layer;

            MeshCollider collider = cylinder.AddComponent<MeshCollider>();
            collider.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            collider.convex = true;
        }

        // 나무 : 줄기만 원통. 굵기는 바닥 1m 안의 정점에서 재고, 높이는 나무 키와 2단 점프 높이 중 큰 쪽 (위에 올라서지 못하게)
        private static void AddTreeCollider(GameObject holder, Transform model)
        {
            RemoveColliders(model); // 따로 만드니 에셋 콜라이더(캡슐 / 상자 / 메쉬)는 지운다
            Matrix4x4 toHolder = holder.transform.worldToLocalMatrix;
            Bounds bounds = GetModelBounds(model.gameObject);
            float minY = toHolder.MultiplyPoint3x4(bounds.min).y;

            // 1. 바닥 1m 안의 정점 = 줄기 밑동 (낮게 늘어진 가지가 섞일 수 있다)
            List<Vector3> trunkPoints = new List<Vector3>();
            Renderer[] renderers = GetLod0Renderers(model.gameObject);
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshFilter filter = renderers[r].GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Matrix4x4 matrix = toHolder * renderers[r].transform.localToWorldMatrix;
                Vector3[] vertices = filter.sharedMesh.vertices;
                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertices[v]);
                    if (point.y < minY + 1f) trunkPoints.Add(point);
                }
            }

            // 2. 가운데 = 평균, 반지름 = 가운데에서 거리의 중간값 (가지 몇 개가 섞여도 줄기 굵기에 가깝다)
            Vector3 center = Vector3.zero;
            for (int i = 0; i < trunkPoints.Count; i++) center += trunkPoints[i];
            if (trunkPoints.Count > 0) center /= trunkPoints.Count;

            List<float> distances = new List<float>();
            for (int i = 0; i < trunkPoints.Count; i++)
                distances.Add(new Vector2(trunkPoints[i].x - center.x, trunkPoints[i].z - center.z).magnitude);
            distances.Sort();
            float radius = (distances.Count > 0) ? distances[distances.Count / 2] : 0.4f;
            radius = Mathf.Clamp(radius, 0.25f, 1f);

            float height = Mathf.Max(bounds.size.y, TreeColliderHeight);
            AddCylinderCollider(holder, new Vector3(center.x, minY + height * 0.5f, center.z), radius * 2f, height, radius * 2f);
            cylinderColliderCount++;
        }
        // ===== 새로 추가 끝 =====

        // [변경] groundY → sink : 바닥 높이를 받지 않고, 모델 발밑 전체에서 가장 낮은 바닥을 직접 찾아 sink만큼 묻는다
        //  예전엔 부르는 곳마다 반지름 1.5 ~ 4m만 봐서, 긴 벽 / 큰 바위 / 절벽은 경사 아래쪽 바닥이 떠서 뚫린 밑면이 보였다
        // isTree = true : 모델 피벗을 (x, z)에, 줄기 둘레만 본다 / false : 모델 상자 가운데를 (x, z)에
        private static void PlaceOnGround(GameObject holder, Transform model, float x, float z, float sink, bool isTree = false)
        {
            holder.transform.position = new Vector3(x, 0f, z);
            Bounds bounds = GetModelBounds(model.gameObject);

            Vector3 offset = Vector3.zero;
            float radius = TreeFootRadius;
            if (isTree == false)
            {
                offset.x = x - bounds.center.x;
                offset.z = z - bounds.center.z;
                radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude; // 발밑 상자의 모서리까지
            }

            float groundY = GetLowestGroundY(x, z, radius) - sink;
            offset.y = groundY - bounds.min.y;
            holder.transform.position += offset;
        }

        private static void SetMaterial(GameObject model, Material material)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = new Material[renderers[i].sharedMaterials.Length];
                for (int m = 0; m < materials.Length; m++) materials[m] = material;
                renderers[i].sharedMaterials = materials;
            }
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            Transform[] children = target.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++) children[i].gameObject.layer = layer;
        }

        // 움직이지 않는 맵 오브젝트 → 정적 배칭
        private static void SetStatic(GameObject target)
        {
            Transform[] children = target.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                GameObjectUtility.SetStaticEditorFlags(children[i].gameObject, StaticEditorFlags.BatchingStatic);
        }

        // 만든 메쉬는 에셋으로 저장해야 씬을 다시 열어도 남는다
        private static void PrepareMeshFolder()
        {
            if (AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.DeleteAsset(MeshFolder); // 이전에 만든 메쉬 지우기
            if (AssetDatabase.IsValidFolder("Assets/00. 3D Game/Map") == false) AssetDatabase.CreateFolder("Assets/00. 3D Game", "Map");
            AssetDatabase.CreateFolder("Assets/00. 3D Game/Map", "DragonNest");
        }

        private static void SaveMesh(Mesh mesh)
        {
            AssetDatabase.CreateAsset(mesh, $"{MeshFolder}/{mesh.name}.asset");
        }

        #endregion

        #endregion
    }
}
// ===== 새로 추가 끝 =====
