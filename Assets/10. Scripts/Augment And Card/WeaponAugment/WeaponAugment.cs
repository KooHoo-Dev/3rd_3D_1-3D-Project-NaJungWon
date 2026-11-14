using UnityEngine;

namespace PROJECT3D
{
    // 무기증강의 부모 클래스
    // 쿨타임 체크도 부모가 하고, 자식은 Attack()만
    public abstract class WeaponAugment : UnitAttack
    {
        // 애니메이터 파라미터
        private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        private static readonly int IsAiming    = Animator.StringToHash("IsAiming");

        [Header("증강 설정")]
        [SerializeField] protected float searchRange = 10f; // 적을 찾는 범위 (범위 증가 적용)
        [SerializeField] private float fireDistance = 1f; // 몸 중심에서 이만큼 떨어진 곳에서 발사 (범위 증가 적용 안 함)

        [Header("테스트용 : 직접 자식으로 넣었을 때 사용할 증강 ID (-1이면 사용 안 함)")]
        [SerializeField] private int testAugmentID = -1; // 테스트용 증강 ID

        // 내부 설정 : 증강 수치 (자식도 사용)
        protected AugmentLevelDef levelData; // 현재 레벨 수치
        protected bool            isArousal; // 진화 여부

        // 내부 설정 : 공격
        private float timer;        // 다음 공격까지 지난 시간
        private bool  isUnitWeapon; // 유닛 전용 증강 여부 (증강 표 OwnerUnitID가 -1이 아니면 전용)

        #region UnityMethod

        protected virtual void Start()
        {
            if (levelData == null && testAugmentID != -1) //테스트용
            {
                SetLevelData(TableLoader.Instance.AugmentLevelTable.Get(testAugmentID, 1));
            }
        }

        protected override void Update()
        {
            base.Update(); // 공격 시점 처리
            if (levelData == null) return; // 아직 초기화 전
            if (IsDelaying) return;        // 이전 공격의 시점을 기다리는 중

            float attackSpeed = GetWeaponAttackSpeed(); // 무기마다 공속 보정이 다를 수 있어 가상 메서드로 (기본 = 부모 GetAttackSpeed)
            if (attackSpeed <= 0f) return; // 공속 0이면 공격하지 않는 무기

            timer += WorldClock.Instance.DeltaTime;
            if (timer < 1f / attackSpeed) return; // 초당 횟수 → 간격(초)
            timer = 0f;

            // 전용 무기가 스킬 모션 중이면 공격 모션 없이 바로 발사 (공격 레이어가 스킬 모션을 덮지 않게)
            if (isUnitWeapon && IsSkillAnimationPlaying())
            {
                Attack();
                return;
            }

            SetUnitAttackAnimation(); // 전용 무기면 공격 레이어 켜기 + 공격 애니 속도
            StartDelay(); // 애니메이션 / 딜레이 후 Attack
        }

        #endregion

        #region 외부 설정

        #region 무기 설정

        // 테이블에 None이면 공격력을 쓴다
        public void SetMainStat(StatType stat)
        {
            if (stat == StatType.None) mainStat = StatType.Damage;
            else mainStat = stat;
        }

        public void SetLevelData(AugmentLevelDef data)
        {
            levelData = data;
            SetAttackStat(data.AugmentLevelStat); // 부모가 이 수치로 데미지/공속/범위를 계산
            OnLevelChanged();
        }

        public void SetUnitWeapon(bool isUnit)
        {
            isUnitWeapon = isUnit;
        }

        public void SetArousal()
        {
            isArousal = true;
            OnArousal();
        }

        protected virtual void OnLevelChanged() { }
        protected virtual void OnArousal() { } //진화했을 때 바뀌는 것이 있으면 재정의

        #endregion

        #region 공격

        // 공격 시점이 되면 실제 공격
        protected override void OnDelayEnd()
        {
            Attack();
        }

        // 실제 공격 내용. 자식이 반드시 재정의
        protected abstract void Attack();

        #endregion

        #region 수치 계산

        // 무기가 한 번에 쏘는 투사체 수 (무기 기본 개수 + 플레이어 개수). 표 실수로 0이 돼도 최소 1개
        protected int GetWeaponProjectileCount()
        {
            return Mathf.Max(1, GetProjectileCount()); // 부모(UnitAttack) 계산 재사용
        }

        // 이 무기의 최종 공속 (초당 횟수). 기본 = 무기 공속 × 플레이어 공속 배율
        // 빛의 화살처럼 투사체 수 / 진화로 공속이 바뀌는 무기는 재정의
        protected virtual float GetWeaponAttackSpeed()
        {
            return GetAttackSpeed();
        }

        // 기본 1개를 넘는 추가 투사체 수 (상점 + 능력증강 + 무기 레벨). 투사체 대신 다른 효과로 바꿔 쓰는 무기용
        protected int GetExtraProjectileCount()
        {
            return GetWeaponProjectileCount() - 1;
        }

        // 새로 추가 : 탐색 범위 Gizmo용 기본 범위 (부모 UnitAttack이 그린다. 자식 5개에서 옮김)
        protected override float GetBaseSearchRange()
        {
            return searchRange;
        }

        #endregion

        #region 조준

        // 기본 공격 조준
        // 범위(searchRange, 범위 증가 적용) 안 가장 가까운 적 쪽, 없으면 캐릭터 정면. 수평 방향만
        protected Vector3 GetTargetOrForwardDirection()
        {
            return GetFlatDirectionTo(FindTarget(searchRange)); // 방향 계산을 아래로 분리 (마법사 전격이 찾은 대상을 같이 쓰려고)
        }

        // target 쪽 수평 방향 (target이 null이면 캐릭터 정면)
        protected Vector3 GetFlatDirectionTo(Enemy target)
        {
            Vector3 direction = Owner.transform.forward;

            if (target != null) direction = target.HitCenter - Owner.HitCenter;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Owner.transform.forward; // 적이 몸에 딱 붙어 있을 때
            return direction.normalized;
        }

        // 몸 중심(HitCenter)에서 flatDirection 쪽으로 fireDistance만큼 떨어진 발사 위치
        protected Vector3 GetBodyFirePosition(Vector3 flatDirection)
        {
            return Owner.HitCenter + flatDirection * fireDistance;
        }

        // 몸 둘레의 flatDirection 쪽 지점에서 발사
        // target이 있으면 발사 지점 → 대상 피격 중심으로 (높이 차이 포함), 없으면 flatDirection으로 수평 직진
        protected HitUnit FireFromBody(HitUnit prefab, Vector3 flatDirection, Enemy target, int damage)
        {
            Vector3 from = GetBodyFirePosition(flatDirection);

            Vector3 direction = flatDirection;
            if (target != null)
            {
                Vector3 toTarget = target.HitCenter - from;
                if (toTarget.sqrMagnitude > 0.01f) direction = toTarget.normalized; // 대상이 발사 지점에 딱 붙어 있으면 수평 그대로
            }

            return FireHit(prefab, from, direction, damage);
        }

        #endregion

        #endregion

        #region 내부 설정

        // 전용 무기증강이 공격할 때 공격 레이어(상체)를 켜고, 플레이어 공속 배율만큼 공격 애니도 빠르게
        // 끄는 건 AttackTimingSender가 공격 상태를 빠져나갈 때
        private void SetUnitAttackAnimation()
        {
            if (isUnitWeapon == false || OwnerAnimator == null) return;

            int layerIndex = OwnerAnimator.GetLayerIndex("AttackLayer");
            if (layerIndex >= 0) OwnerAnimator.SetLayerWeight(layerIndex, 1f);
            OwnerAnimator.SetFloat(AttackSpeed, Mathf.Max(1f, OwnerStat.AttackSpeed));
        }

        // 지금 스킬 모션 / 조준 중인지 (이때 기본 무기는 공격 모션 없이 바로 발사)
        //  - 스킬 모션 : E / Q 스킬 상태의 AttackTimingSender가 켜고 끄는 값
        //  - 조준 중(IsAiming) : Unit_Aim이 같은 AttackLayer라 공격 트리거가 막혀서, 그대로 두면 2초 대기 뒤에 발사되고
        //    남은 트리거로 조준이 끝난 뒤 공격 모션이 한 번 더 나간다 → 조준 중에도 바로 발사
        private bool IsSkillAnimationPlaying()
        {
            if (Owner.IsSkillMotion) return true;
            return OwnerAnimator != null && OwnerAnimator.GetBool(IsAiming);
        }

        #endregion
    }
}