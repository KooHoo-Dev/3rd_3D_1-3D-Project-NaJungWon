using System;
using UnityEngine;

namespace PROJECT3D
{
    public class PlayerStat : BaseStat
    {

    }

    public abstract class Player : CombatEntity
    {
        [Header("참조 설정")]
        public Transform BodyPivot; // 캐릭터 모델 넣을 곳

        [Header("낙하 데미지 설정")]
        [SerializeField] private float safeFallHeight     = 4f; // 이 높이까지는 데미지 없음
        [SerializeField] private float fallDamagePerMeter = 5f; // 넘은 높이 1m당 데미지

        [Header("획득 설정")]
        [SerializeField] private float basePickupRange = 3f; // 기본 획득 반경

        [Header("피격 무적 설정")]
        [SerializeField] private float hitInvincibleTime = 0.5f; // 맞은 뒤 이 시간(게임 시간) 동안 데미지 무시

        // 외부 공개
        public PlayerStat        Stat         { get; set; } = new PlayerStat(); // 플레이어 스탯
        public PlayerStatHandler StatHandler  { get; private set; }             // 스탯 총합 계산기
        public override BaseStat BaseStat     => Stat;                          // 공용 스탯 (CombatEntity)
        public UnitDef           unitDef;                                       // 캐릭터 표 데이터
        public bool              IsInvincible { get; set; }                     // 무적. 켜져 있으면 데미지 x
        public int               KillCount    { get; private set; }             // 내가 죽인 몹 수
        public int               Gold         { get; private set; }             // 이번 판에 번 골드

        public event Action OnRecordChanged; // 처치 수 / 골드가 바뀌었을 때
        public event Action OnDead;          // 죽었을 때
        public event Action OnKill;          // 몹을 처치할 때마다 (패시브 등)

        // 내부 설정
        private float    hitInvincibleEndTime; // 피격 무적이 끝나는 시각 (WorldClock.ElapsedTime 기준)
        private float    regenBuffer;          // 체력 재생 누적량 (Hp가 정수라 1 이상 쌓이면 회복)
        private HitFlash hitFlash;             // 피격 시 모델 빨갛게

        #region UnityMethod

        protected virtual void OnEnable() => PlayerRegistry.Register(this);

        // 체력 재생
        protected virtual void Update()
        {
            UpdateHealthRegen();
        }

        // 초당 HealthRegeneration만큼 회복. 게임 시간 기준이라 일시정지 중에는 멈춘다
        private void UpdateHealthRegen()
        {
            if (WorldClock.IsInitialized == false) return;

            float regen = Stat.unitStat.HealthRegeneration;
            if (Stat.IsDead || regen <= 0f || Stat.Hp >= Stat.unitStat.MaxHp)
            {
                regenBuffer = 0f; // 꽉 찬 동안 쌓아 두지 않는다
                return;
            }

            regenBuffer += regen * WorldClock.Instance.DeltaTime;
            if (regenBuffer < 1f) return;

            int heal = (int)regenBuffer; // 정수만큼 회복하고 나머지는 남겨 둔다
            regenBuffer -= heal;
            Stat.ApplyHeal(heal);
        }

        protected virtual void OnDisable() => PlayerRegistry.Unregister(this);

        #endregion

        #region 외부 설정

        #region 초기화 / 스탯

        public void Init(UnitDef def, StatClass shopStat)
        {
            unitDef = def;
            GameObject body = Instantiate(def.UnitBody, BodyPivot); // [변경] 피격 연출에 넘기려고 변수로

            // 피격 시 모델이 빨갛게 물드는 연출 (프리팹에 HitFlash가 있을 때만)
            hitFlash = GetComponent<HitFlash>();
            if (hitFlash != null) hitFlash.Init(body);

            // 테이블 스탯을 그대로 쓰지 않고 핸들러가 복사해서 최종 스탯을 계산
            StatHandler = GetComponent<PlayerStatHandler>();
            StatHandler.Init(this, def.UnitStat, shopStat);
        }

        public float GetPickupRange()
        {
            return basePickupRange * (1f + Stat.unitStat.AcquisitionRange / 100f);
        }

        #endregion

        #region 기록

        public void AddKill()
        {
            KillCount++;
            OnRecordChanged?.Invoke();
            OnKill?.Invoke(); // 새로 추가
        }

        // 내 골드 획득량 % 적용 (20 = +20%)
        // 얻을 때마다 보유 골드(GameSession)에도 바로 더한다
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            float gainPercent = Stat.unitStat.GoldGain;
            int gain = Mathf.RoundToInt(amount * (1f + gainPercent / 100f)); // 계산값을 변수로
            Gold += gain;                                   // 이번 판 획득량 (HUD 표시용)
            if (GameSession.Instance != null) GameSession.Instance.AddGold(gain); // 보유 골드에 바로 반영
            OnRecordChanged?.Invoke();
        }

        #endregion

        #region 피격

        public override void TakeDamage(CombatEvent combatEvent)
        {
            if (IsInvincible) return;
            if (Stat.IsDead) return; // 이미 죽었으면 무시
            if (IsHitInvincible()) return; // 방금 맞았으면 무시 (여러 마리 데미지가 한 번에 몰리는 것 방지)

            int damage = CalculateFinalDamage(combatEvent.Damage); // 받는 쪽 보정 (방어력 등은 나중에 여기서)
            hitInvincibleEndTime = WorldClock.Instance.ElapsedTime + hitInvincibleTime; // 피격 무적 시작
            if (hitFlash != null) hitFlash.Play(); // 피격 연출
            if (Stat.ApplyDamage(damage)) Die();
            Debug.Log($"{combatEvent.Receiver.GameObject.name} 가 {combatEvent.Sender.GameObject.name}에게 {combatEvent.Damage}의 데미지를 받음");
        }

        // 피격 무적 중인지 (게임 시간 기준이라 일시정지 중에는 무적 시간도 멈춘다)
        public bool IsHitInvincible()
        {
            return WorldClock.Instance.ElapsedTime < hitInvincibleEndTime;
        }

        public override void TakeHeal(HealEvent healEvent)
        {

        }

        // 착지했을 때 떨어진 높이로 낙하 데미지
        public void ApplyFallDamage(float fallHeight)
        {
            if (fallHeight <= safeFallHeight) return;
            if (CombatSystem.Instance == null) return;

            int damage = Mathf.RoundToInt((fallHeight - safeFallHeight) * fallDamagePerMeter);
            CombatSystem.Instance.SendDamage(this, this, damage, HitCenter);
        }

        #endregion

        #endregion

        #region 내부 설정

        private void Die()
        {
            Debug.Log($"[Player] {name} 사망");
            OnDead?.Invoke();
        }

        #endregion
    }
}