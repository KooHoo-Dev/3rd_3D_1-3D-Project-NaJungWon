namespace PROJECT3D
{
    // 플레이어가 보유 중인 증강 1개의 상태
    // BS Project에서 CardInfo(카드 UI)가 들고 있던 upgradeCount, isArousal, weaponInstance를 여기로 옮겼다
    // 카드 UI는 그리기만 하고, 레벨/진화 상태는 이 클래스가 관리한다
    public class OwnedAugment
    {
        public AugmentDef Def { get; private set; }
        public int Level { get; private set; }
        public int MaxLevel { get; private set; }
        public bool IsArousal { get; private set; }
        public WeaponAugment Weapon { get; private set; } //무기증강일 때만 있음. 능력치증강은 null

        public OwnedAugment(AugmentDef def, WeaponAugment weapon)
        {
            Def = def;
            Weapon = weapon;
            MaxLevel = TableLoader.Instance.AugmentLevelTable.GetMaxLevel(def.ID);
            Level = 0;
            IsArousal = false;
            if (Weapon != null) Weapon.SetMainStat(def.MainStat);
            LevelUp(); // 1레벨로 시작
        }

        public bool IsMaxLevel()
        {
            return Level >= MaxLevel;
        }

        // 현재 레벨의 수치
        public AugmentLevelDef GetLevelData()
        {
            return TableLoader.Instance.AugmentLevelTable.Get(Def.ID, Level);
        }

        public void LevelUp()
        {
            if (IsMaxLevel()) return;

            Level++;

            // 무기증강이면 무기에 새 수치를 넣어준다
            if (Weapon != null) Weapon.SetLevelData(GetLevelData());
        }

        // 만렙 이후 진화
        public void Arousal()
        {
            IsArousal = true;
            if (Weapon != null) Weapon.SetArousal();
        }
    }
}