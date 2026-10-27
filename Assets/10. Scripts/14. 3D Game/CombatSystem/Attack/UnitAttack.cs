using UnityEngine;

namespace PROJECT3D
{
    // 몹/플레이어 공격의 공통 부모.
    //  - 주인(CombatEntity)과 주인 능력치
    //  - 발사 위치 / 조준 방향 (대상의 몸 중심 HitCenter)
    //  - HitUnit 생성 입구 FireHit 하나 (논타겟/타겟팅/소환 공통)
    // 자식 : EnemyAttack(몹 공격 패턴), AugmentCard(증강), UnitSkill(스킬)
    public abstract class UnitAttack : MonoBehaviour
    {
        [Header("공통 공격 설정")]
        [SerializeField] protected Transform firePoint; // 발사 위치. 없으면 이 오브젝트 위치

        // 주인 : 같은 오브젝트(몹) 또는 부모(플레이어)에서 찾는다
        protected CombatEntity Owner { get; private set; }
        protected StatClass OwnerStat => Owner.BaseStat.unitStat; // 주인의 능력치 (몹/플레이어 공통)

        protected Vector3 FirePosition => firePoint != null ? firePoint.position : transform.position;

        [Header("데미지 계수")]
        [SerializeField] protected StatType mainStat = StatType.Damage; // 데미지에 쓰는 스탯 (기본 공격력)
        [SerializeField] protected float damageRatio = 1f;              // 메인스탯 계수 (1 = 100%)

        // 이 공격 자체의 수치 (무기증강 = 레벨 수치, 스킬 = 스킬 테이블 수치). 몹 공격은 비어 있음(전부 0)
        protected StatClass AttackStat { get; private set; } = new StatClass();

        public void SetAttackStat(StatClass stat)
        {
            if (stat == null) AttackStat = new StatClass();
            else AttackStat = stat;
        }
        
        #region 최종 스탯 수치 (기본 스탯 + 주인 스탯)
        
        protected int GetDamage() // 공격력 : 공격 기본 데미지 + 주인 메인스탯 × 계수
        {
            float damage = AttackStat.Damage + OwnerStat.Get(mainStat) * damageRatio;
            return Mathf.RoundToInt(damage);
        }

        // 공격속도 : 공격 공속(초당 횟수) × 주인 공속 배율
        protected float GetAttackSpeed() => AttackStat.AttackSpeed * OwnerStat.AttackSpeed;
        
        // 투사체 개수 : 공격 개수 + 주인 추가 개수
        protected int GetProjectileCount() => AttackStat.ProjectileCount + OwnerStat.ProjectileCount;

        // 공격범위 배율 : 1 + (공격 범위% + 주인 범위%) / 100
        protected float RangeRate => 1f + (AttackStat.AttackRange + OwnerStat.AttackRange) / 100f;

        // 지속시간 배율 : 1 + (공격 지속% + 주인 지속%) / 100
        protected float DurationRate => 1f + (AttackStat.Duration + OwnerStat.Duration) / 100f;

        #endregion
        
        protected virtual void Awake()
        {
            Owner = GetComponentInParent<CombatEntity>();
            if (Owner == null) Debug.LogError($"{name} : 부모 계층에서 CombatEntity를 찾지 못했습니다");
        }

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
    }
}