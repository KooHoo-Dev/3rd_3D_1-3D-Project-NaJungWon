using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PROJECT3D
{
    public class EnemyStat : BaseStat
    {
        
    }
    
    public abstract partial class Enemy : CombatEntity
    {
        // BaseStat을 Player/Enemy를 나누는게 좋을듯?
        public EnemyStat Stat { get; set; } = new EnemyStat();

        public StatClass Unit => Stat.unitStat;
        public override BaseStat BaseStat => Stat;
        
        private EnemyDef enemyDef;
        public float resist;
        public Vector3 direction;

        [Header("이동 설정")] 
        public float YOffset { get; private set; }
        [SerializeField] private float separationDistance = 1.5f; // 몹끼리 멀어지는 거리
        // 나중에 EnemyManager같은걸로 빼는게 좋을듯? 임시
        private static readonly List<Enemy> activeEnemies = new List<Enemy>(); 

        public void Init(EnemyDef def, Vector3 targetDirection)
        {
            //StatClass unit = Stat.unitStat;
            Unit.MaxHp = def.MaxHp;
            Unit.Damage = def.Damage;
            Unit.AttackInterval = def.AttackInterval;
            Unit.Speed = def.Speed;
            Unit.Defense = def.Armor;
            resist = def.Resist;
            direction = targetDirection;
        }

        protected virtual void OnEnable() => activeEnemies.Add(this);
        protected virtual void OnDisable() => activeEnemies.Remove(this);
        
        protected void Update()
        {
            Move(Time.deltaTime);
            /* 임시코드
            if ((transform.position - target).sqrMagnitude < 0.1f)
            {
                TakeDamage(
                    new CombatEvent()
                    {
                        Damage = 0,
                        HitPosition = (transform.position - target).normalized,
                        Receiver = this,
                        Sender = LocalPlayer.Instance,
                    }
                );
            }
            */
        }
        
        protected virtual void Move(float deltaTime)   // 자식이 override할 통로
        {
            direction = GetChaseDirection() + GetSeparationDirection();
            if (direction.sqrMagnitude < 0.1f) return;
            transform.position += direction.normalized * (Unit.Speed * deltaTime);
            SnapToGround();
        }
        
        // 흐름장에서 지금 서 있는 칸의 추격 방향을 가져온다
        private Vector3 GetChaseDirection()
        {
            FlowField field = FlowFieldManager.Instance.Field;
            if (field == null) return Vector3.zero;

            Vector3 chase = field.GetDirection(transform.position);
            chase.y = 0f; // 좌우 이동만 하고, 높이는 SnapToGround에서 맞춘다
            return chase;
        }

        // 주변 몹에게서 멀어지는 방향. 가까이 있을수록 세게 밀어낸다 (separate패턴 예시)
        private Vector3 GetSeparationDirection()
        {
            Vector3 sum = Vector3.zero;

            foreach (Enemy other in activeEnemies)
            {
                if (other == this) continue;

                Vector3 toOther = transform.position - other.transform.position;
                toOther.y = 0f;

                float distance = toOther.magnitude;
                if (distance > 0f && distance < separationDistance)
                    sum += toOther * (1f - distance / separationDistance);
            }

            return sum;
        }

        // 지상 몹이 계단·경사를 따라가도록 바닥 높이에 붙히는데 현재 부드러운처리없어서 이상함
        private void SnapToGround()
        {
            MapGrid mapGrid = MapGrid.Instance;

            Tile tile = mapGrid.Grid.GetTileOrNull(transform.position);
            if (tile == null || tile.IsWalkable == false) return;

            Vector3 position = transform.position;
            position.y = tile.GroundHeight;
            transform.position = position;
        }

        public override void TakeDamage(CombatEvent combatEvent)
        {
            
        }
        
    }
}