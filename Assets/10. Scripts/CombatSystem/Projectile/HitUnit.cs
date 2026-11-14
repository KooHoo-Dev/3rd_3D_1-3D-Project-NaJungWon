using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 공격으로 생성되는 오브젝트(투사체, 소환 범위)의 공통 부모
    // 자식 : NonTargetHit(논타겟 직진), TargetingHit(타겟 추적), SummonHit(지정 위치 소환)
    public abstract class HitUnit : MonoBehaviour, IObjectPoolItem
    {
        protected CombatEntity owner;
        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        [Header("공격 범위")]
        [SerializeField] private HitShape hitShape = HitShape.Sphere;
        [SerializeField] private float hitRadius = 0.2f;
        [SerializeField] private float hitHeight = 1f;          // 기둥 / 직육면체일 때만 사용 (절반 높이)
        [SerializeField] private float hitDepth = 0.5f;         // 직육면체일 때만 사용 (앞뒤 절반 두께)
        [SerializeField] private ParticleEffect impactEffect;   // 맞은 자리에 터뜨릴 이펙트 (없으면 생략)
        [SerializeField] private Color rangeGizmoColor = Color.yellow;
        
        public float HitRadius => hitRadius; // 프리팹 기본 반경 (스킬 조준 중 예상 범위 표시용)

        protected int damage;
        protected float currentHitRadius;        // 이번 생성의 반경 (범위 증가 적용 후)
        protected float currentHitDepth;         // 이번 생성의 앞뒤 두께 (직육면체. 범위 증가 적용 후)
        private Vector3 baseScale;               // 프리팹 원래 크기 (범위 증가만큼 키워서 이펙트도 크게 보이게)
        private float bonusRangeRate = 1f;       // 이번에 받은 범위 배율 (이어서 만드는 HitUnit에 그대로 넘긴다)
        private float bonusDurationRate = 1f;    // 이번에 받은 지속시간 배율

        private List<Enemy> enemyBuffer = new List<Enemy>();               // 격자 검색 결과 (재사용)
        private List<CombatEntity> touching = new List<CombatEntity>();     // 이번에 닿은 대상 (재사용)
        private TrailRenderer[] trails; // 꼬리 (풀 재사용 시 이전 위치에서 선이 이어지지 않게 비운다)

        private IHitEffect[] hitEffects; // 이 프리팹에 붙은 부가 효과들(스턴/슬로우 등등) (맞힐 때마다 Apply)

        #region UnityMethod

        protected virtual void Awake()
        {
            baseScale = transform.localScale; // 프리팹 원래 크기 저장
            trails = GetComponentsInChildren<TrailRenderer>(true);
            hitEffects = GetComponents<IHitEffect>();
        }

        // 풀에서 꺼내 켤 때 (FireHit이 Launch로 자리를 옮긴 뒤에 켠다) 지난번 꼬리 점들을 지운다
        protected virtual void OnEnable()
        {
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].Clear();
            }
        }

        // 씬 화면에 공격 범위를 그리기
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = rangeGizmoColor;
            HitGizmo.Draw(transform.position, Application.isPlaying ? currentHitRadius : hitRadius, hitShape, hitHeight, transform.rotation, Application.isPlaying ? currentHitDepth : hitDepth); // [변경] 범위 증가 적용 두께 전달
        }

        #endregion

        #region 외부 설정

        #region 발사 설정

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
            currentHitDepth = hitDepth;          
            transform.localScale = baseScale;    // 이전에 키운 크기 되돌리기 (풀 재사용)
            bonusRangeRate = 1f;    
            bonusDurationRate = 1f; 
            // 스턴 초기화 → 부가 효과 전부 초기화 (쓰는 공격만 발사 뒤에 다시 켠다)
            for (int i = 0; i < hitEffects.Length; i++)
            {
                hitEffects[i].ResetEffect();
            }
        }

        // 주인의 능력치 보너스 적용 (rangeRate = 1 + 범위 증가%, durationRate = 1 + 지속시간 증가%)
        // 기본은 반경만. 사거리/지속시간이 있는 자식은 override 해서 추가로 적용한다
        public virtual void ApplyBonus(float rangeRate, float durationRate)
        {
            // 판정 크기 키우기
            currentHitRadius = hitRadius * rangeRate;
            currentHitDepth = hitDepth * rangeRate;          
            // 판정과 별개로, 보이는 크기 키우기
            transform.localScale = baseScale * rangeRate;    
            bonusRangeRate = rangeRate;       
            bonusDurationRate = durationRate; 
        }

        // 이 HitUnit 자리에서 다른 HitUnit을 만들기 (투사체가 맞은 자리에 폭발 범위 등)
        protected HitUnit SpawnHit(HitUnit prefab, Vector3 position, Vector3 direction)
        {
            if (prefab == null) return null;

            HitUnit hit = ObjectPoolManager.Instance.GetObjectOrNull(prefab.gameObject) as HitUnit;
            if (hit == null) return null;

            hit.Launch(position, direction, damage, owner);
            hit.ApplyBonus(bonusRangeRate, bonusDurationRate);
            hit.gameObject.SetActive(true);
            return hit;
        }

        #endregion

        #region 판정

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
                    if (players[i].IsHitBy(transform.position, currentHitRadius, hitShape, hitHeight, transform.rotation, currentHitDepth))
                        touching.Add(players[i]);
                }
                return touching;
            }

            EnemyRegistry.FindHitTargets(transform.position, currentHitRadius, hitShape, hitHeight, transform.rotation, currentHitDepth, ref enemyBuffer);
            for (int i = 0; i < enemyBuffer.Count; i++)
                touching.Add(enemyBuffer[i]);
            return touching;
        }

        // 한 대상만 닿았는지 (타겟팅용)
        protected bool IsTouching(CombatEntity target)
        {
            return target.IsHitBy(transform.position, currentHitRadius, hitShape, hitHeight, transform.rotation, currentHitDepth);
        }

        // 한 대상에게 데미지 + 이펙트
        protected void Hit(CombatEntity target)
        {
            CombatSystem.Instance.SendDamage(owner, target, damage, transform.position);
            if (impactEffect != null) ObjectPoolManager.PlayEffect(impactEffect, GetImpactPoint(target)); // 범위 중심 → 맞은 대상 표면 (범위 공격에서 이펙트가 한 자리에 겹치던 것)

            // ApplyStun → 붙어 있는 부가 효과(IHitEffect) 전부 적용 (스턴 등)
            for (int i = 0; i < hitEffects.Length; i++)
            {
                hitEffects[i].Apply(target);
            }
        }

        #endregion

        #region 풀

        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReturnToPool(this);
        }

        #endregion

        #endregion

        #region 내부 설정

        #region 판정

        // 피격 이펙트 위치 = 대상 피격 원통 표면 중 이 공격 쪽 지점 (큰 몹이어도 몸에 가려지지 않게)
        // 높이는 대상 몸 중심 그대로. 공격이 대상 중심에 딱 겹치면 몸 중심
        private Vector3 GetImpactPoint(CombatEntity target)
        {
            Vector3 toAttack = transform.position - target.HitCenter;
            toAttack.y = 0f;
            if (toAttack.sqrMagnitude < 0.01f) return target.HitCenter;

            return target.HitCenter + toAttack.normalized * target.HurtRadius;
        }

        #endregion
        
        #endregion
    }
}