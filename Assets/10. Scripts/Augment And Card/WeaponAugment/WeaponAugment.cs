using UnityEngine;

namespace PROJECT3D
{
    // 무기증강의 부모 클래스
    // 쿨타임 체크도 부모가 하고, 자식은 Attack()만
    public abstract class WeaponAugment : UnitAttack
    {
        [Header("Setting")]
        protected AugmentLevelDef levelData; //현재 레벨 수치
        protected bool isArousal;            //진화 여부
        private float timer;

        [Header("테스트용 : 직접 자식으로 넣었을 때 사용할 증강 ID (-1이면 사용 안 함)")]
        [SerializeField] private int testAugmentID = -1;

        protected virtual void Start()
        {
            if (levelData == null && testAugmentID != -1) //테스트용
            {
                SetLevelData(TableLoader.Instance.AugmentLevelTable.Get(testAugmentID, 1));
            }
        }

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

        public void SetArousal()
        {
            isArousal = true;
            OnArousal();
        }

        protected override void Update()
        {
            base.Update(); // 공격 시점 처리
            if (levelData == null) return; // 아직 초기화 전
            if (IsDelaying) return;        // 이전 공격의 시점을 기다리는 중

            float attackSpeed = GetWeaponAttackSpeed(); // [변경] 무기마다 공속 보정이 다를 수 있어 가상 메서드로 (기본 = 부모 GetAttackSpeed)
            if (attackSpeed <= 0f) return; // 공속 0이면 공격하지 않는 무기

            timer += WorldClock.Instance.DeltaTime;
            if (timer < 1f / attackSpeed) return; // 초당 횟수 → 간격(초)
            timer = 0f;
            StartDelay(); // 애니메이션 / 딜레이 후 Attack
        }

        // 공격 시점이 되면 실제 공격
        protected override void OnDelayEnd()
        {
            Attack();
        }

        // [변경] 삭제 : GetStat / GetDamage
        //  - SetLevelData에서 SetAttackStat(레벨 수치)를 넣기 때문에 부모 UnitAttack의 GetDamage / GetAttackSpeed / GetProjectileCount와 계산이 똑같았다
        //  - 데미지 = 무기 기본 데미지 + 플레이어 메인스탯 × 계수 → 부모 GetDamage() 그대로 사용

        // 새로 추가 : 무기가 한 번에 쏘는 투사체 수 (무기 기본 개수 + 플레이어 개수). 표 실수로 0이 돼도 최소 1개
        protected int GetWeaponProjectileCount()
        {
            return Mathf.Max(1, GetProjectileCount()); // [변경] 부모(UnitAttack) 계산 재사용
        }

        // 새로 추가 : 이 무기의 최종 공속 (초당 횟수). 기본 = 무기 공속 × 플레이어 공속 배율
        // 빛의 화살처럼 투사체 수 / 진화로 공속이 바뀌는 무기는 재정의
        protected virtual float GetWeaponAttackSpeed()
        {
            return GetAttackSpeed();
        }

        // 새로 추가 : 기본 1개를 넘는 추가 투사체 수 (상점 + 능력증강 + 무기 레벨). 투사체 대신 다른 효과로 바꿔 쓰는 무기용
        protected int GetExtraProjectileCount()
        {
            return GetWeaponProjectileCount() - 1;
        }

        // 새로 추가 : 기본 공격 조준 (검기 / 마법사 공용. 원래 검기 안에 있던 것을 옮김)
        // 범위(searchRange, 범위 증가 적용) 안 가장 가까운 적 쪽, 없으면 캐릭터 정면. 수평 방향만
        protected Vector3 GetTargetOrForwardDirection(float searchRange)
        {
            return GetFlatDirectionTo(FindTarget(searchRange)); // [변경] 방향 계산을 아래로 분리 (마법사 전격이 찾은 대상을 같이 쓰려고)
        }

        // 새로 추가 : target 쪽 수평 방향 (target이 null이면 캐릭터 정면)
        protected Vector3 GetFlatDirectionTo(Enemy target)
        {
            Vector3 direction = Owner.transform.forward;

            if (target != null) direction = target.HitCenter - Owner.HitCenter;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Owner.transform.forward; // 적이 몸에 딱 붙어 있을 때
            return direction.normalized;
        }

        // 실제 공격 내용. 자식이 반드시 재정의
        protected abstract void Attack();
        
        protected virtual void OnLevelChanged() { }
        protected virtual void OnArousal() { } //진화했을 때 바뀌는 것이 있으면 재정의
    }
}