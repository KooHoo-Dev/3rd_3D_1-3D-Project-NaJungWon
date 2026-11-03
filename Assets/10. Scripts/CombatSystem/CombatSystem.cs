using System;
using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    public class CombatSystem : MonoBehaviour
    {
        // [변경] 삭제 : maxEventsPerFrame(읽는 곳 없음), enemyLayer / EnemyMask(콜라이더 판정 시절 값. 지금은 EnemyRegistry로 찾는다)
        public static CombatSystem Instance { get; private set; }

        [SerializeField] private float critMultiplier = 1.5f; // 새로 추가 : 치명타 데미지 배율

        public event Action<CombatEvent> OnCombatEvent;
        public event Action<HealEvent> OnHealEvent;

        // [변경] 삭제 : monsterDic (콜라이더 → 몹 사전. EnemyRegistry로 대체됨)
        private readonly Queue<InGameEvent> inGameEventQueue = new Queue<InGameEvent>();

        #region UnityMethod

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{name}: CombatSystem이 이미 있습니다. 이 컴포넌트는 쓰지 않습니다");
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            int count = inGameEventQueue.Count;

            for (int i = 0; i < count; i++)
            {
                var inGameEvent = inGameEventQueue.Dequeue();

                if (inGameEvent.Receiver is UnityEngine.Object receiverObject && 
                    receiverObject == null) continue;

                switch (inGameEvent.Type)
                {
                    case InGameEvent.EventType.Combat:
                        var combatEvent = inGameEvent as CombatEvent;
                        inGameEvent.Receiver.TakeDamage(combatEvent);
                        OnCombatEvent?.Invoke(combatEvent);
                        break;
                    case InGameEvent.EventType.Heal:
                        var healEvent = inGameEvent as HealEvent;
                        inGameEvent.Receiver.TakeHeal(healEvent);
                        OnHealEvent?.Invoke(healEvent);
                        break;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region 외부 설정

        // 데미지 보내기 창구
        public void SendDamage(CombatEntity sender, CombatEntity receiver, int damage, Vector3 hitPosition)
        {
            if (sender == null || receiver == null)
            {
                Debug.LogWarning($"[CombatSystem] : sender 존재 여부 : {sender != null}, receiver 존재 여부 : {receiver != null}");
                return;
            }
            
            // 치명타 처리
            bool isCritical = false;
            if (UnityEngine.Random.Range(0f, 100f) < sender.BaseStat.unitStat.CritChance)
            {
                isCritical = true;
                damage = Mathf.RoundToInt(damage * critMultiplier);
            }

            CombatEvent combatEvent = new CombatEvent();
            combatEvent.Sender = sender;
            combatEvent.Receiver = receiver;
            combatEvent.Damage = damage;
            combatEvent.HitPosition = hitPosition;
            combatEvent.IsCritical = isCritical; // 새로 추가

            AddInGameEvent(combatEvent);
        }

        public void AddInGameEvent(InGameEvent inGameEvent)
        {
            inGameEventQueue.Enqueue(inGameEvent);
        }

        #endregion
    }
}
