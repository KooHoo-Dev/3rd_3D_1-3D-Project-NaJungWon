using UnityEngine;

namespace PROJECT3D
{
    // 공격할 때 재생할 애니메이션 자리
    public enum AnimationSlot
    {
        None,   // 애니메이션 없음
        Attack, // 기본 공격 (기본 무기증강, 몹 공격)
        ESkill, // E 스킬
        QSkill, // Q 스킬
    }

    // 몹/플레이어 공격의 공통 부모.
    // 자식 : EnemyAttack(몹 공격 패턴), AugmentCard(증강), UnitSkill(스킬)
    public abstract class UnitAttack : MonoBehaviour
    {
        [Header("공통 공격 설정")]
        private Transform FirePoint => Owner.FirePivot; // 발사 위치. 없으면 이 오브젝트 위치

        protected CombatEntity Owner { get; private set; }
        protected StatClass OwnerStat => Owner.BaseStat.unitStat;

        protected Vector3 FirePosition => FirePoint != null ? FirePoint.position : transform.position;

        [Header("데미지 계수")]
        [SerializeField] protected StatType mainStat = StatType.Damage; // 데미지에 쓰는 스탯 (기본 공격력)
        [SerializeField] protected float damageRatio = 1f;              // 메인스탯 계수 (1 = 100%)

        // 이 공격 자체의 수치 (무기증강 = 레벨 수치, 스킬 = 스킬 테이블 수치). 몹 공격은 비어 있음(전부 0)
        protected StatClass AttackStat { get; private set; } = new StatClass();

        // 공격범위 배율 : 1 + (공격 범위% + 주인 범위%) / 100
        protected float RangeRate => 1f + (AttackStat.AttackRange + OwnerStat.AttackRange) / 100f;

        // 지속시간 배율 : 1 + (공격 지속% + 주인 지속%) / 100
        protected float DurationRate => 1f + (AttackStat.Duration + OwnerStat.Duration) / 100f;

        [Header("공격 애니메이션 / 시점")]
        [SerializeField] private AnimationSlot animationSlot = AnimationSlot.None; // 공격할 때 재생할 애니메이션 (None = 재생 안 함)
        [SerializeField] private bool useAnimationTiming = false;                  // 켜면 : 애니메이션 진행률(0~1)에서 공격 / 끄면 : attackDelay(초) 후 공격
        [SerializeField] [Range(0f, 1f)] private float animationTiming = 0.5f;     // 공격 애니메이션의 이 지점(0~1)에서 공격
        [SerializeField] private float attackDelay = 0f;                           // 토글을 끈 경우 : 공격까지 시간(초). 0이면 바로
        private const float AnimationWaitLimit = 2f; // 애니메이션 신호가 안 오면 이 시간 뒤에 그냥 공격 (멈춤 방지)

        private float attackDelayTimer;              // 남은 시간
        private Animator ownerAnimator;              // 주인의 애니메이터 (처음 쓸 때 찾는다)
        public bool IsDelaying { get; private set; } // 공격 시점을 기다리는 중인지

        protected Animator OwnerAnimator => ownerAnimator; // 새로 추가 : 자식(무기증강)이 공격 레이어 / 공속 파라미터를 바꿀 때 사용

        [Header("공격 범위 Gizmo")]
        [SerializeField] private bool drawRangeGizmo = true;          // 끄면 이 공격의 범위를 안 그린다
        [SerializeField] private Color rangeGizmoColor = Color.cyan;

        #region UnityMethod

        protected virtual void Awake()
        {
            Owner = GetComponentInParent<CombatEntity>();
            if (Owner == null)
            {
                Debug.LogError($"{name} : 부모 계층에서 CombatEntity를 찾지 못했습니다");
                return;
            }
            ownerAnimator = Owner.GetComponentInChildren<Animator>();
        }

        protected virtual void Update()
        {
            if (IsDelaying == false) return;

            attackDelayTimer -= WorldClock.Instance.DeltaTime;
            if (attackDelayTimer > 0f) return;

            IsDelaying = false;
            OnDelayEnd();
        }

        protected virtual void OnDisable()
        {
            IsDelaying = false; // 꺼지면(몹이 죽어서 풀로 돌아가면) 기다리던 공격은 취소
        }

        // 실행 중 : 범위 증가(상점 / 능력증강 / 무기 레벨) 적용값을 주인 위치에, 실행 전 : 기본값을 이 오브젝트 위치에
        // FindTarget과 같은 중심 / 같은 계산(ApplyRangeBonus)이라 실제로 찾는 범위와 같다
        protected virtual void OnDrawGizmos()
        {
            if (drawRangeGizmo == false) return;

            float baseRange = GetBaseSearchRange();
            if (baseRange <= 0f) return;

            float range = baseRange;
            Vector3 center = transform.position;
            if (Application.isPlaying && Owner != null)
            {
                range = ApplyRangeBonus(baseRange);
                center = Owner.transform.position;
            }

            Gizmos.color = rangeGizmoColor;
            HitGizmo.Draw(center, range, HitShape.Sphere, 0f); // 기존 범위 그리기 재사용
        }

        #endregion

        #region 외부 설정

        #region 최종 스탯 수치 (기본 스탯 + 주인 스탯)

        public void SetAttackStat(StatClass stat)
        {
            if (stat == null) AttackStat = new StatClass();
            else AttackStat = stat;
        }

        protected int GetDamage() // 공격력 : 공격 기본 데미지 + 주인 메인스탯 × 계수
        {
            float damage = AttackStat.Damage + OwnerStat.Get(mainStat) * damageRatio;
            return Mathf.RoundToInt(damage);
        }

        // 공격속도 : 공격 공속(초당 횟수) × 주인 공속 배율
        protected float GetAttackSpeed() => AttackStat.AttackSpeed * OwnerStat.AttackSpeed;

        // 투사체 개수 : 공격 개수 + 주인 추가 개수
        protected int GetProjectileCount() => AttackStat.ProjectileCount + OwnerStat.ProjectileCount;

        #endregion

        #region 공격 시점 처리 (애니메이션 → 실제 공격)

        public AnimationSlot GetAnimationSlot() => animationSlot;
        public float GetAnimationTiming()  => animationTiming;

        // 자식이 공격을 시작할 때 호출 : 애니메이션을 틀고, 공격 시점이 되면 OnDelayEnd가 불린다
        protected void StartDelay()
        {
            PlayAnimation();

            // 1. 애니메이션 진행률 방식 : AttackTimingSender의 신호(OnAnimationTiming)를 기다린다
            if (useAnimationTiming)
            {
                IsDelaying = true;
                attackDelayTimer = AnimationWaitLimit; // 신호가 끝내 안 오면 이 시간 뒤에 공격
                return;
            }

            // 2. 초 방식 : 0이면 바로 공격
            if (attackDelay <= 0f)
            {
                OnDelayEnd();
                return;
            }

            IsDelaying = true;
            attackDelayTimer = attackDelay;
        }

        // 애니메이션 상태(AttackTimingSender)가 공격 시점에 호출
        public void OnAnimationTiming()
        {
            if (IsDelaying == false) return; // 기다리는 공격이 없으면 무시

            IsDelaying = false;
            OnDelayEnd();
        }

        // 공격 시점이 됐을 때 실제 공격. 자식이 재정의
        protected virtual void OnDelayEnd() { }

        #endregion

        #region 공격 도우미

        // 발사 위치에서 대상의 피격 원통 중심을 향하는 방향
        protected Vector3 GetAimDirection(CombatEntity target)
        {
            return (target.HitCenter - FirePosition).normalized;
        }

        // 범위 증가(%)를 적용한 값 (탐색 범위 등 공격 스크립트가 직접 쓰는 범위용)
        protected float ApplyRangeBonus(float baseRange)
        {
            return baseRange * RangeRate;
        }

        // 대상에게 바로 데미지 (근접 공격 등 생성 오브젝트가 없는 공격용)
        protected void DealDamage(CombatEntity target, int damage)
        {
            if (target == null || CombatSystem.Instance == null) return;
            CombatSystem.Instance.SendDamage(Owner, target, damage, target.HitCenter);
        }

        // 범위(범위 증가 적용) 안 가장 가까운 적. 타겟팅 / 적 위치 소환의 대상 고르기. 없으면 null
        protected Enemy FindTarget(float baseRange)
        {
            return EnemyRegistry.FindNearest(Owner.transform.position, ApplyRangeBonus(baseRange), null);
        }

        // HitUnit 생성 (발사 위치에서)
        protected HitUnit FireHit(HitUnit prefab, Vector3 direction, int damage)
        {
            return FireHit(prefab, FirePosition, direction, damage);
        }

        // HitUnit 생성 (원하는 위치에서. 소환 위치, 궤도 지점 등)
        // 풀에서 꺼내기(꺼진 상태) → Launch → 능력치 보너스 → 켜기. 실패하면 null
        // 종류별 추가 설정(타겟팅의 대상/횟수 등)이 필요하면 돌려받아서 쓴다
        protected HitUnit FireHit(HitUnit prefab, Vector3 from, Vector3 direction, int damage)
        {
            HitUnit hit = ObjectPoolManager.Instance.GetObjectOrNull(prefab.gameObject) as HitUnit;
            if (hit == null) return null;

            hit.Launch(from, direction, damage, Owner);
            hit.ApplyBonus(RangeRate, DurationRate);
            hit.gameObject.SetActive(true);
            return hit;
        }

        #endregion

        #region Gizmo설정

        // 이 공격의 기본 탐색 범위 (범위 증가 적용 전). FindTarget처럼 범위로 대상을 찾는 공격만 재정의. 0이면 안 그림
        protected virtual float GetBaseSearchRange() => 0f;

        #endregion

        #endregion

        #region 내부 설정

        // 공격 애니메이션 트리거
        private void PlayAnimation()
        {
            if (animationSlot == AnimationSlot.None) return;
            if (ownerAnimator == null)
            {
                Debug.LogWarning($"[UnitAttack] 애니메이터를 찾을 수 없음");
                return; // [변경] 로그 뒤에 바로 null에 SetTrigger 하던 것 방지
            }
            ownerAnimator.SetTrigger(animationSlot.ToString());
        }

        #endregion
    }
}