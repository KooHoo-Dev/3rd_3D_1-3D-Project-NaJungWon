using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 공격으로 생성되는 오브젝트(투사체, 소환 범위)의 공통 부모
    //  - 주인, 데미지, 공격 범위(모양/반경), 명중 이펙트, 풀 반납, Gizmo
    //  - "지금 위치에서 닿은 대상 찾기"와 "한 대상 때리기"만 제공한다. 언제 때리고 어떻게 움직일지는 자식이 정한다
    //  - UnitAttack.FireHit 한 곳에서 Launch → ApplyBonus 순서로 부른다. 자식은 필요한 만큼 override 해서 자기 값을 채운다
    // 자식 : NonTargetHit(논타겟 직진), TargetingHit(타겟 추적), SummonHit(지정 위치 소환)
    public abstract class HitUnit : MonoBehaviour, IObjectPoolItem
    {
        protected CombatEntity owner;
        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        [Header("공격 범위")]
        [SerializeField] private HitShape hitShape = HitShape.Sphere;
        [SerializeField] private float hitRadius = 0.2f;
        [SerializeField] private float hitHeight = 1f;          // 기둥일 때만 사용
        [SerializeField] private ParticleEffect impactEffect;   // 맞은 자리에 터뜨릴 이펙트 (없으면 생략)
        [SerializeField] private Color rangeGizmoColor = Color.yellow;

        protected int damage;
        protected float currentHitRadius;        // 이번 생성의 반경 (범위 증가 적용 후)

        private List<Enemy> enemyBuffer = new List<Enemy>();               // 격자 검색 결과 (재사용)
        private List<CombatEntity> touching = new List<CombatEntity>();     // 이번에 닿은 대상 (재사용)

        // 생성 직후 공통 설정. 자식은 override 해서 base.Launch() 뒤에 자기 값을 초기화한다
        // direction : 직진 방향 / 첫 조준 방향. 소환형은 쓰지 않는다
        public virtual void Launch(Vector3 from, Vector3 direction, int damage, CombatEntity owner)
        {
            transform.position = from;
            if (direction.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(direction); // 진행 방향을 바라본다

            this.damage = damage;
            this.owner = owner;
            currentHitRadius = hitRadius;
        }

        // 주인의 능력치 보너스 적용 (rangeRate = 1 + 범위 증가%, durationRate = 1 + 지속시간 증가%)
        // 기본은 반경만. 사거리/지속시간이 있는 자식은 override 해서 추가로 적용한다
        public virtual void ApplyBonus(float rangeRate, float durationRate)
        {
            currentHitRadius = hitRadius * rangeRate;
        }

        // 지금 위치의 공격 범위에 닿은 대상들 (몹이 만든 것 → 플레이어, 플레이어가 만든 것 → 몹)
        protected List<CombatEntity> FindTouching()
        {
            touching.Clear();

            if (owner is Enemy)
            {
                IReadOnlyList<Player> players = PlayerRegistry.All; // 최대 4명이라 전부 검사
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].Stat.IsDead) continue;
                    if (players[i].IsHitBy(transform.position, currentHitRadius, hitShape, hitHeight))
                        touching.Add(players[i]);
                }
                return touching;
            }

            EnemyRegistry.FindHitTargets(transform.position, currentHitRadius, hitShape, hitHeight, ref enemyBuffer);
            for (int i = 0; i < enemyBuffer.Count; i++)
                touching.Add(enemyBuffer[i]);
            return touching;
        }

        // 한 대상만 닿았는지 (타겟팅용)
        protected bool IsTouching(CombatEntity target)
        {
            return target.IsHitBy(transform.position, currentHitRadius, hitShape, hitHeight);
        }

        // 한 대상에게 데미지 + 이펙트
        protected void Hit(CombatEntity target)
        {
            CombatSystem.Instance.SendDamage(owner, target, damage, transform.position);
            if (impactEffect != null) ObjectPoolManager.PlayEffect(impactEffect, transform.position);
        }

        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(this);
        }

        // 씬 화면에 공격 범위를 그린다 (실행 전에는 기본 반경, 실행 중에는 범위 증가 적용 반경)
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = rangeGizmoColor;
            HitGizmo.Draw(transform.position, Application.isPlaying ? currentHitRadius : hitRadius, hitShape, hitHeight);
        }
    }
}