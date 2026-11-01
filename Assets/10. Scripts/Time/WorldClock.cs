using System;
using DG.Tweening;
using Jay;
using UnityEngine;

namespace PROJECT3D
{
    // 게임 안에서 흐르는 시계. 정지/감속/배속/기본
    // 이 게임에는 시계가 두 개 있습니다.
    // - 게임 시간(WorldClock)      : 몹 이동/공격, 투사체, 스킬/무기 쿨타임, 플레이어 이동, 웨이브
    // - 실제 시간(Engine, Device)  : UI, 커서, 카메라, 메뉴 등

    // 보통 일시정지가 있는 게임들은 엔진의 시간을 직접 사용하지 않고
    // 따로 정의를 해줍니다.
    // 이럴경우, 아래와 같은 개발 규칙이 추가됩니다.
    // - 게임 로직은 Time.deltaTime을 쓰지 않는다. 대신 WorldClock의 DeltaTime을 사용한다
    // - Time.timeScale은 건드리지 않는다
    public class WorldClock : SingletonBase<WorldClock>
    {
        [Header("Settings")]
        [SerializeField] private float tickInterval = 1f; // 알림 주기 역할을 하는 필드

        // 배속 단계표. 0번 = 정지, 1번 = 보통, 2번 = 감속(연출), 3번 = 배속 - 나중에 json으로뺄듯?
        [SerializeField] private float[] timeScales = { 0f, 1f, 0.3f, 2f };

        // 지금 고른 배속 단계. int인 이유는 timeScales 배열의 인덱스로 사용함
        public int ScaleStep { get; private set; } = DEFAULT_STEP;

        public float Scale => timeScales[ScaleStep];

        public bool IsPaused => Scale <= 0f;

        // 게임 로직이 써야 하는 시간 간격
        // : Time.deltaTime이 아니라 unscaledDeltaTime를 사용.
        //  Time.timeScale은 건드리지 않기로 했으므로, 누군가 그것을
        //  바꾸더라도 흔들리지 않는 시계가 필요하다.
        //  배속은 우리가 직접 연산한다.
        public float DeltaTime => Time.unscaledDeltaTime * Scale;

        // 게임이 흐른 총 시간 (멈춘 동안은 늘지 않고, 감속 중에는 천천히 는다)
        public float ElapsedTime { get; private set; }

        #region Events(델리게이트)

        public event Action OnTick;                 // 게임시간이 tickInterval 만큼 흐를때마다
        public event Action<bool> OnPauseChanged;   // Pause의 상태 변화 (true = 멈춤)

        #endregion

        // 단계 번호 상수
        public const int PAUSED_STEP = 0;
        public const int DEFAULT_STEP = 1;
        public const int SLOW_STEP = 2;
        public const int FAST_STEP = 3;

        private float tickTimer;
        // 일시정지 직전에 쓰던 배속. 일시정지가 끝나면 되돌아가기 위해 필요한 변수
        private int lastRunningStep = DEFAULT_STEP;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ScaleStep = DEFAULT_STEP;
            // DOTween의 타임 스케일을 WorldClock의 스케일로 바꿔준다
            DOTween.timeScale = Scale;
        }

        private void Update()
        {
            float delta = DeltaTime;
            if (delta <= 0f) return;

            ElapsedTime += delta;
            AdvanceTick(delta);
        }

        #region 시간 진행

        private void AdvanceTick(float delta)
        {
            // 틱타이머를 갱신해주고
            tickTimer += delta;
            // 임계점에 도달하지 않았으면 return
            if (tickTimer < tickInterval) return;

            tickTimer = 0f;
            // OnTick을 구독하는 객체들에게 알려준다
            OnTick?.Invoke();
        }

        #endregion

        #region 배속

        // 배속 단계를 설정해준다. timeScales의 인덱스를 바꿔서 배속 단계를 바꾼다
        public void SetScaleStep(int step)
        {
            // 기본적인 예외처리 로그는 안남기겠습니다(가밸할떄는 남기십쇼)
            if (step < 0 || timeScales.Length <= step) return;
            if (step == ScaleStep) return;

            bool wasPaused = IsPaused;

            if (step == PAUSED_STEP) lastRunningStep = ScaleStep;

            ScaleStep = step;
            DOTween.timeScale = Scale;

            // 멈춤 ↔ 진행이 바뀌었을 때만 알린다 (보통 ↔ 감속처럼 둘 다 진행 중이면 알리지 않는다)
            if (wasPaused != IsPaused) OnPauseChanged?.Invoke(IsPaused);
        }

        public void Pause() => SetScaleStep(PAUSED_STEP);
        public void Resume() => SetScaleStep(lastRunningStep);
        public void SlowMotion() => SetScaleStep(SLOW_STEP);
        public void NormalSpeed() => SetScaleStep(DEFAULT_STEP);

        // 새 판 시작 시 시계 초기화
        public void ResetClock()
        {
            lastRunningStep = DEFAULT_STEP;
            SetScaleStep(DEFAULT_STEP);
            ElapsedTime = 0f;
            tickTimer = 0f;
        }
        
        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        #endregion
    }
}