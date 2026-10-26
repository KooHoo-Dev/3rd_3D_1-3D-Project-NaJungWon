using UnityEngine;

namespace PROJECT3D
{
    // 무기증강의 부모 클래스
    // 쿨타임 체크도 부모가 하고, 자식은 Attack()만
    public abstract class WeaponAugment : UnitAttack
    {
        protected AugmentLevelDef levelData; //현재 레벨 수치
        protected bool isArousal;            //진화 여부
        private float timer;

        [Header("테스트용 : 직접 자식으로 넣었을 때 사용할 증강 ID (-1이면 사용 안 함)")]
        [SerializeField] private int testAugmentID = -1;

        protected virtual void Start()
        {
            if (levelData == null && testAugmentID != -1) //테스트용
                SetLevelData(TableLoader.Instance.AugmentLevelTable.Get(testAugmentID, 1));
        }
        
        public void SetLevelData(AugmentLevelDef data)
        {
            levelData = data;
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

            timer += WorldClock.Instance.DeltaTime;
            if (timer < levelData.AugmentLevelStat.AttackInterval) return;

            timer = 0f;
            Attack();
        }

        // 무기 공격력 + 플레이어 공격력
        protected int GetDamage()
        {
            return levelData.AugmentLevelStat.Damage + OwnerStat.Damage;
        }

        // 무기 투사체 수 + 플레이어 투사체 증가
        protected int GetProjectileCount()
        {
            return levelData.AugmentLevelStat.ProjectileCount + OwnerStat.ProjectileCount;
        }

        // 실제 공격 내용. 자식이 반드시 재정의
        protected abstract void Attack();
        
        protected virtual void OnLevelChanged() { }
        protected virtual void OnArousal() { } //진화했을 때 바뀌는 것이 있으면 재정의
    }
}