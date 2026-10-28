using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PROJECT3D
{
    public class EnemyStat : BaseStat
    {
        
    }
    
    // 몹 본체 : 스탯(표), 체력, 피격/사망 처리, 풀 귀환.
    // 이동 - EnemyControllerBase 자식, 공격 - EnemyAttack 자식, 사망 효과 - EnemyDeath 자식이 실행
    public class Enemy : CombatEntity, IObjectPoolItem
    {
        // BaseStat을 Player/Enemy를 나누는게 좋을듯?
        public EnemyStat Stat { get; set; } = new EnemyStat();

        public StatClass Unit => Stat.unitStat;
        public override BaseStat BaseStat => Stat;

        private EnemyDef enemyDef;
        public float resist;

        [Header("연출")]
        [SerializeField] private ParticleEffect deathParticle; // 몹 공통 사망 연출 (없으면 생략)

        public EnemyControllerBase Controller { get; private set; }
        public bool Ready => enemyDef != null;
        public Vector2Int RegistryCell { get; set; } // EnemyRegistry가 쓰는 '지금 들어 있는 칸' (Spartial Grid용)

        // 풀이 넣어 주는 값 (프리팹 이름)
        public string Key { get; set; }

        private EnemyDeath[] deathEffects; // 죽을 때 실행할 효과들
        
        public void Init(EnemyDef def) //레벨디자인 할거면 여기에 게임 시간 경과당 배율이 있으면 될듯? (배율은 json으로 관리)
        {
            Unit.MaxHp = def.MaxHp;
            Unit.Damage = def.Damage;
            Unit.AttackSpeed = def.AttackSpeed;
            Unit.Speed = def.Speed;
            Unit.Defense = def.Armor;
            resist = def.Resist;
            Stat.ResetToFull();
            
            enemyDef = def;
        }

        public void Awake()
        {
            Controller = GetComponent<EnemyControllerBase>();
            deathEffects = GetComponents<EnemyDeath>(); // 여러 개면 전부 가져옴
        }
        
        public override void TakeDamage(CombatEvent combatEvent)
        {
            if (Stat.IsDead) return; // 이미 죽었으면 무시
            Debug.Log($"[Enemy]: {combatEvent.Receiver.GameObject.name}가 {combatEvent.Sender.GameObject.name}에게 {combatEvent.Damage}를 입음!");
            int damage = CalculateFinalDamage(combatEvent.Damage); // 받는 쪽 보정 (방어력 등은 나중에 여기서)
            if (Stat.ApplyDamage(damage)) Die();
        }

        public override void TakeHeal(HealEvent healEvent)
        {
            
        }

        // 사망 처리
        private void Die()
        {
            if(deathEffects == null)
            {
                ObjectPoolManager.PlayEffect(deathParticle, transform.position);
                foreach (EnemyDeath effect in deathEffects) effect.Apply();
            }

            ReturnToPool();
        }

        public void ReturnToPool()
        {
            // 풀에서 꺼낸 몹이 아니면(Instantiate로 만든 몹) 그냥 끈다
            if (ObjectPoolManager.Instance == null || string.IsNullOrEmpty(Key))
            {
                gameObject.SetActive(false);
                return;
            }

            ObjectPoolManager.Instance.ReturnToPool(this);
        }
    }
}