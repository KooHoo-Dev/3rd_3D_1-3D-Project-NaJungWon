using Jay;
using UnityEngine;

namespace PROJECT3D
{
    public class TableLoader : SingletonBase<TableLoader>
    {
        public UnitTable UnitTable { get; private set; }
        public SkillTable SkillTable { get; private set; }
        public EnemyTable EnemyTable { get; private set; }
        public WaveTable WaveTable { get; private set; }
        public SpawnRuleTable SpawnRuleTable { get; private set; } // 새로 추가 : 상시 소환 규칙
        public AugmentTable AugmentTable { get; private set; }
        public AugmentLevelTable AugmentLevelTable { get; private set; }
        public ShopTable ShopTable { get; private set; }
        public StructureTable StructureTable { get; private set; }
        public LevelTable LevelTable { get; private set; }
        public bool IsLoaded { get; private set; } // 이미 로드했는지

        #region 외부 설정

        // 여러 씬에서 불러도 처음 한 번만 로드한다
        // (타이틀을 거치지 않고 로비나 인게임 씬에서 바로 테스트할 때도 안전)
        public void LoadData()
        {
            if (IsLoaded) return;

            SkillTable = new SkillTable();
            SkillTable.Load();
            UnitTable = new UnitTable();
            UnitTable.Load(SkillTable);
            AugmentTable = new AugmentTable();
            AugmentTable.Load();
            AugmentLevelTable = new AugmentLevelTable();
            AugmentLevelTable.Load();
            ShopTable = new ShopTable();
            ShopTable.Load();
            EnemyTable = new EnemyTable();
            EnemyTable.Load();
            WaveTable = new WaveTable();
            WaveTable.Load(EnemyTable);
            SpawnRuleTable = new SpawnRuleTable();  // 새로 추가
            SpawnRuleTable.Load(EnemyTable);        // 새로 추가
            StructureTable = new StructureTable(); // [변경] 주석 해제 : 구조물 표 로드
            StructureTable.Load(); // [변경]
            LevelTable = new LevelTable();
            LevelTable.Load(); 
            IsLoaded = true;
            Debug.Log("[TableLoader] 테이블 로드 완료");
        }

        #endregion
    }
}