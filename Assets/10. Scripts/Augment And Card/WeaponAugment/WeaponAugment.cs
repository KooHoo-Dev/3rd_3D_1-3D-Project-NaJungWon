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

        protected virtual void Update()
        {
            if (levelData == null) return; //아직 초기화 전

            float attackSpeed = GetStat(StatType.AttackSpeed);
            if (attackSpeed <= 0f) return; // 공속 0이면 공격하지 않는 무기

            timer += WorldClock.Instance.DeltaTime;
            if (timer < 1f / attackSpeed) return; // 초당 횟수 → 간격(초)
            timer = 0f;
            Attack();
        }

        // 무기 수치 + 플레이어 수치를 합친 최종값
        // 스탯마다 합치는 방법이 다르면 case를 추가한다
        protected float GetStat(StatType type)
        {
            float weaponValue = levelData.AugmentLevelStat.Get(type);
            float ownerValue = OwnerStat.Get(type);

            return type switch
            {
                StatType.Damage => weaponValue + OwnerStat.Get(mainStat) * damageRatio, // 무기 기본 데미지 + 플레이어 메인스탯 × 계수 (예 : 탱커 무기는 최대 체력 × 0.05)
                StatType.AttackSpeed => weaponValue * ownerValue, // 무기 공속 × 플레이어 공속 배율
                StatType.ProjectileCount => weaponValue + ownerValue, // 투사체는 플레이어 자체 투사체와 더하기
                _ => weaponValue + ownerValue
            };
        }

        // 최종 데미지 (int)
        protected int GetDamage()
        {
            return Mathf.RoundToInt(GetStat(StatType.Damage));
        }

        // 실제 공격 내용. 자식이 반드시 재정의
        protected abstract void Attack();
        
        protected virtual void OnLevelChanged() { }
        protected virtual void OnArousal() { } //진화했을 때 바뀌는 것이 있으면 재정의
    }
}