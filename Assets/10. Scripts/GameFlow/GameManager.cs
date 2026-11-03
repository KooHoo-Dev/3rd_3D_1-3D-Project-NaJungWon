using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PROJECT3D
{
    // 게임 시작 순서를 한 곳에서 관리하기
    // - 각 매니저는 Awake에서 Instance만 잡고, 데이터는 여기서 Init으로 넣는다
    public class GameManager : MonoBehaviour
    {
        [Header("씬 참조")]
        [SerializeField] private MapGrid mapGrid;
        [SerializeField] private FlowFieldManager flowFieldManager;
        [SerializeField] private PlayerSpawner playerSpawner; // 로비에서 고른 캐릭터 + 상점 스탯으로 플레이어 생성
        [SerializeField] private InGameUI inGameUI;           // 새로 추가 : 인게임 HUD
        [SerializeField] private GameOverPanel gameOverPanel; // 새로 추가 : 패배 결과창
        [SerializeField] private StructureManager structureManager; // 새로 추가 : 구조물 소환 / 상호작용 (비워 두면 시작할 때 기본값으로 만든다)
        public bool IsReady { get; private set; }
        public event Action OnGameReady;           // 준비가 끝났을 때 (로딩 화면 끄기 등)

        private Player localPlayer; // 내 플레이어 (사망 / 기록 확인용)
        private bool isGameOver;    // 패배 처리가 두 번 돌지 않게

        #region UnityMethod

        private void Start()
        {
            BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy()
        {
            if (localPlayer != null) localPlayer.OnDead -= HandlePlayerDead;
        }

        #endregion

        #region 내부 설정

        #region 게임 시작

        private async UniTaskVoid BootAsync(CancellationToken token)
        {
            WorldClock.Instance.ResetClock(); // 이전 판의 정지 / 경과 시간 초기화
            EnemyRegistry.Clear();  // 새로 추가 : 정적 명부는 씬이 바뀌어도 남아서, 이전 판 몹 / 플레이어 기록을 지운다
            PlayerRegistry.Clear(); // 새로 추가
            
            // 1. 테이블 연결
            TableLoader.Instance.LoadData();
            Debug.Log("[GameManager] 1. 테이블 로드 완료");
            await UniTask.Yield(token); // 한 프레임 쉬기 (로딩 화면이 있으면 여기서 갱신된다)

            // 2. 맵 굽기
            await mapGrid.BakeAllAsync(token);
            Debug.Log("[GameManager] 2. 맵 굽기 완료");

            // 3. 흐름장 준비
            flowFieldManager.Init(mapGrid);
            Debug.Log("[GameManager] 3. 흐름장 준비 완료");

            // 4. 플레이어 생성 + 유닛 데이터 + 컨트롤러 연결
            Player player = playerSpawner.SpawnPlayers();
            Debug.Log("[GameManager] 4. 플레이어 생성 완료");

            // 5. 매니저 연결
            flowFieldManager.AddPlayer(player.transform);
            AugmentManager.Instance.Init(player, player.BodyPivot);
            WaveManager.Instance.Init(mapGrid); // [변경] 안 쓰던 흐름장 매개변수 삭제
            inGameUI.Init(player); // 증강 매니저가 준비된 뒤에 연결 (보유 증강 표시)
            localPlayer = player;
            player.OnDead += HandlePlayerDead;  // 죽으면 패배 처리
            Debug.Log("[GameManager] 5. 매니저 연결 완료");

            // 6. 게임 시작
            IsReady = true;
            OnGameReady?.Invoke();
            WaveManager.Instance.StartWaves();

            // 새로 추가 : 구조물 소환 시작 (씬에 안 넣었으면 기본값으로 만든다)
            if (structureManager == null) structureManager = new GameObject("StructureManager").AddComponent<StructureManager>();
            structureManager.StartSpawn();
            Debug.Log("[GameManager] 6. 게임 시작");
        }

        #endregion

        #region 패배

        private void HandlePlayerDead()
        {
            if (isGameOver) return;
            isGameOver = true;
            GameOverAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        // 게임 정지 → 사망 모션 끝까지 → 패배 결과창
        private async UniTaskVoid GameOverAsync(CancellationToken token)
        {
            WorldClock.Instance.Pause(); // 몹 / 투사체 / 웨이브 / 타이머 정지 (커서도 풀린다)

            PlayerController controller = localPlayer.GetComponent<PlayerController>();
            await controller.PlayDieAsync(token);

            gameOverPanel.Open(localPlayer.KillCount, localPlayer.Gold, WorldClock.Instance.ElapsedTime);
        }

        #endregion

        #endregion
    }
}