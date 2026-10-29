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

        public bool IsInvincible { get; set; } // 무적. 켜져 있으면 데미지x
        

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