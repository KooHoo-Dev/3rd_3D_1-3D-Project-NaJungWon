using UnityEngine;

namespace PROJECT3D
{
    // 몹/플레이어 공격의 공통 부모. (FPS Game의 Weapon처럼 공통 도우미를 모아 둔 곳)
    //  - 주인(CombatEntity)과 주인 능력치
    //  - 발사 위치 / 조준 방향 (대상의 몸 중심 HitCenter)
    //  - HitUnit 생성 입구 FireHit 하나 (논타겟/타겟팅/소환 공통. 능력치 보너스도 여기서 넘긴다)
    //  - 바로 데미지 주기
    // 자식 : EnemyAttack(몹 공격 패턴), AugmentCard(증강), UnitSkill(스킬)
    public abstract class UnitAttack : MonoBehaviour
    {
        [Header("공통 공격 설정")]
        [SerializeField] protected Transform firePoint; // 발사 위치. 없으면 이 오브젝트 위치

        // 주인 : 같은 오브젝트(몹) 또는 부모(플레이어 아래 붙는 증강/스킬)에서 찾는다
        // (Action Platformer HitBox의 GetComponentInParent와 같은 방식)
        protected CombatEntity Owner { get; private set; }

        // 주인의 능력치 (몹/플레이어 공통)
        protected StatClass OwnerStat => Owner.BaseStat.unitStat;

        protected Vector3 FirePosition => firePoint != null ? firePoint.position : transform.position;

        // 능력치 배율 (몹은 범위/지속시간 능력치가 0이라 1배)
        protected float RangeRate => 1f + OwnerStat.AttackRange / 100f;
        protected float DurationRate => 1f + OwnerStat.Duration / 100f;

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