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
        [SerializeField] private PlayerTester playerTester; // 테스트용 플레이어 생성 (캐릭터 선택이 생기면 교체)

        public bool IsReady { get; private set; }  // 준비가 끝났는지 (다른 곳에서 확인용)
        public event Action OnGameReady;           // 준비가 끝났을 때 (로딩 화면 끄기 등)

        private void Start()
        {
            BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid BootAsync(CancellationToken token)
        {
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
            Player player = playerTester.SpawnPlayer();
            Debug.Log("[GameManager] 4. 플레이어 생성 완료");

            // 5. 매니저 연결
            flowFieldManager.AddPlayer(player.transform);
            AugmentManager.Instance.Init(player, player.BodyPivot);
            WaveManager.Instance.Init(mapGrid, flowFieldManager.Field);
            Debug.Log("[GameManager] 5. 매니저 연결 완료");

            // 6. 게임 시작
            IsReady = true;
            OnGameReady?.Invoke();
            WaveManager.Instance.StartWaves();
            Debug.Log("[GameManager] 6. 게임 시작");
        }
    }
}