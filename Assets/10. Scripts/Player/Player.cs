using System;
using UnityEngine;

namespace PROJECT3D
{
    public class PlayerStat : BaseStat
    {
        
    }
    
    public abstract class Player : CombatEntity
    {
        public PlayerStat Stat { get; set; } = new PlayerStat();
        public PlayerStatHandler StatHandler { get; private set; } // 스탯 총합 계산기
        public override BaseStat BaseStat => Stat;
        public Transform BodyPivot; // 캐릭터 모델 넣을곳
        public UnitDef unitDef;
        
        [Header("낙하 데미지")]
        [SerializeField] private float safeFallHeight = 4f;     // 이 높이까지는 데미지 없음
        [SerializeField] private float fallDamagePerMeter = 5f; // 넘은 높이 1m당 데미지
        [SerializeField] private float basePickupRange = 3f; // 기본 획득 반경

        // ===== 새로 추가 : 피격 무적 =====
        [Header("피격 무적")]
        [SerializeField] private float hitInvincibleTime = 0.5f; // 맞은 뒤 이 시간(게임 시간) 동안 데미지 무시
        private float hitInvincibleEndTime;                      // 피격 무적이 끝나는 시각 (WorldClock.ElapsedTime 기준)
        // ===== 새로 추가 끝 =====

        public bool IsInvincible { get; set; } // 무적. 켜져 있으면 데미지x
        public int KillCount { get; private set; } // 내가 죽인 몹 수
        public int Gold { get; private set; }      // 이번 판에 번 골드
        public event Action OnRecordChanged;
        public event Action OnDead;

        public void Init(UnitDef def, StatClass shopStat)
        {
            unitDef = def;
            Instantiate(def.UnitBody, BodyPivot);

            // 테이블 스탯을 그대로 쓰지 않고 핸들러가 복사해서 최종 스탯을 계산
            StatHandler = GetComponent<PlayerStatHandler>();
            StatHandler.Init(this, def.UnitStat, shopStat);
        }
        
        protected virtual void OnEnable() => PlayerRegistry.Register(this);
        protected virtual void OnDisable() => PlayerRegistry.Unregister(this);
        
        public float GetPickupRange()
        {
            return basePickupRange * (1f + Stat.unitStat.AcquisitionRange / 100f);
        }
        
        public void AddKill()
        {
            KillCount++;
            OnRecordChanged?.Invoke();
        }
        
        // 내 골드 획득량 % 적용 (20 = +20%)
        // 얻을 때마다 보유 골드(GameSession)에도 바로 더한다
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            float gainPercent = Stat.unitStat.GoldGain;
            int gain = Mathf.RoundToInt(amount * (1f + gainPercent / 100f)); // [변경] 계산값을 변수로
            Gold += gain;                                   // 이번 판 획득량 (HUD 표시용)
            if (GameSession.Instance != null) GameSession.Instance.AddGold(gain); // 새로 추가 : 보유 골드에 바로 반영
            OnRecordChanged?.Invoke();
        }
        
        public override void TakeDamage(CombatEvent combatEvent)
        {
            if (IsInvincible) return;
            if (Stat.IsDead) return; // 이미 죽었으면 무시
            if (IsHitInvincible()) return; // 새로 추가 : 방금 맞았으면 무시 (여러 마리 데미지가 한 번에 몰리는 것 방지)

            int damage = CalculateFinalDamage(combatEvent.Damage); // 받는 쪽 보정 (방어력 등은 나중에 여기서)
            hitInvincibleEndTime = WorldClock.Instance.ElapsedTime + hitInvincibleTime; // 새로 추가 : 피격 무적 시작
            if (Stat.ApplyDamage(damage)) Die();
            Debug.Log($"{combatEvent.Receiver.GameObject.name} 가 {combatEvent.Sender.GameObject.name}에게 {combatEvent.Damage}의 데미지를 받음");
        }
        
        // 새로 추가 : 피격 무적 중인지 (게임 시간 기준이라 일시정지 중에는 무적 시간도 멈춘다)
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
        
        private void Die()
        {
            Debug.Log($"[Player] {name} 사망");
            OnDead?.Invoke();
        }
    }
}