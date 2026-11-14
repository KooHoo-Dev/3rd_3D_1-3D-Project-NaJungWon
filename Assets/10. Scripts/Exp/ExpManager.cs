using System;
using UnityEngine;

namespace PROJECT3D
{
    // 경험치 / 레벨 담당 (인게임 씬에 하나)
    // - 모든 플레이어가 경험치 바 하나를 같이 쓴다 (멀티 : 각자 먹은 경험치를 합산)
    // - 레벨업하면 모두 같이 레벨이 오르고 증강 선택창을 연다
    // - 한 번에 여러 레벨이 오르면 선택창이 닫힐 때마다 하나씩 다시 연다
    // - 시간이 지날수록 큰 경험치 구슬이 나올 확률이 오른다
    public class ExpManager : MonoBehaviour
    {
        [Header("큰 구슬 확률 설정 (시작 + 분당 증가 × 경과 분, 최대값까지)")]
        [SerializeField] private float startBigOrbChance     = 0.05f; // 시작 확률
        [SerializeField] private float bigOrbChancePerMinute = 0.02f; // 1분마다 늘어나는 확률 (10분에 25%)
        [SerializeField] private float maxBigOrbChance       = 0.8f;  // 최대 확률

        [Header("참조 설정")]
        [SerializeField] private AugmentSelectPanel selectPanel; // 레벨업 증강 선택창

        // 외부 공개
        public static ExpManager Instance { get; private set; }                                     // 씬에 하나
        public int   Level       { get; private set; } = 1;                                         // 현재 레벨
        public float CurrentExp  { get; private set; }                                              // 현재 경험치
        public int   RequiredExp => TableLoader.Instance.LevelTable.GetRequiredExp(Level);          // 다음 레벨까지 필요 경험치
        public event Action<float> OnExpChanged;   // 경험치 바 (0 ~ 1)
        public event Action<int>   OnLevelChanged; // 레벨 글자

        // 내부 설정
        private int pendingLevelUp; // 아직 선택창을 열지 못한 레벨업 수

        #region UnityMethod

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

        private void Update()
        {
            // 선택창이 닫혀 있을 때만 남은 레벨업을 하나씩 연다
            if (pendingLevelUp <= 0) return;
            if (selectPanel.IsOpen) return; // activeSelf → IsOpen (씬에 켜진 채 저장돼 있으면 영영 안 열리던 버그)

            pendingLevelUp--;
            selectPanel.OpenLevelUp();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region 외부 설정

        public float GetBigOrbChance()
        {
            float minutes = WorldClock.Instance.ElapsedTime / 60f;
            float chance = startBigOrbChance + bigOrbChancePerMinute * minutes;
            return Mathf.Min(chance, maxBigOrbChance);
        }

        // 경험치 구슬을 먹은 플레이어가 호출 (아직 멀티 계산 안함)
        public void AddExp(float amount, Player collector)
        {
            // 먹은 플레이어의 경험치 획득량 % 적용 (20 = +20%)
            float gainPercent = collector.Stat.unitStat.ExperienceGain;
            CurrentExp += amount * (1f + gainPercent / 100f);

            // 한 번에 여러 레벨이 오를 수도 있어서 while
            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                pendingLevelUp++;
                OnLevelChanged?.Invoke(Level);
            }

            OnExpChanged?.Invoke(CurrentExp / RequiredExp);
        }

        #endregion
    }
}