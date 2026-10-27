using UnityEngine;

namespace PROJECT3D
{
    public class PlayerStat : BaseStat
    {
        
    }
    
    public abstract class Player : CombatEntity
    {
        public PlayerStat Stat { get; set; } = new PlayerStat();
        public override BaseStat BaseStat => Stat;

        public Transform BodyPivot; // 캐릭터 모델 넣을곳
        public UnitDef unitDef;

        public bool IsInvincible { get; set; } // 무적. 켜져 있으면 데미지x
        [Header("테스트용 (캐릭터 선택이 생기면 지울 것)")]
        [SerializeField] private int testMaxHp = 500;
        
        //public int killCount = 0;

        private void Start()
        {
            //Stat.unitStat.MaxHp = testMaxHp;
            //Stat.ResetToFull();
        }
        
        public void Init(UnitDef def)
        {
            unitDef = def;
            Instantiate(def.UnitBody, BodyPivot);
            Stat.unitStat = def.UnitStat;
            Stat.ResetToFull();
        }
        
        protected virtual void OnEnable() => PlayerRegistry.Register(this);
        protected virtual void OnDisable() => PlayerRegistry.Unregister(this);
        
        public override void TakeDamage(CombatEvent combatEvent)
        {
            if (IsInvincible) return;
            Debug.Log($"{combatEvent.Receiver.GameObject.name} 가 {combatEvent.Sender.GameObject.name}에게 {combatEvent.Damage}의 데미지를 받음");
        }

        public override void TakeHeal(HealEvent healEvent)
        {
            
        }
    }
}