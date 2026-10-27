using UnityEngine;

namespace PROJECT3D
{
    public class TableLoader : MonoBehaviour
    {
        public static TableLoader Instance;
        public UnitTable UnitTable { get; private set; }
        public SkillTable SkillTable { get; private set; }
        public EnemyTable EnemyTable { get; private set; }
        public WaveTable WaveTable { get; private set; }
        public AugmentTable AugmentTable { get; private set; }
        public AugmentLevelTable AugmentLevelTable { get; private set; }
        public StructureTable StructureTable { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            //LoadData();
        }

        public void LoadData()
        {
            SkillTable = new SkillTable();
            SkillTable.Load();
            UnitTable = new UnitTable();
            UnitTable.Load(SkillTable);
            AugmentTable = new AugmentTable();
            AugmentTable.Load();
            AugmentLevelTable = new AugmentLevelTable();
            AugmentLevelTable.Load();
            EnemyTable = new EnemyTable();
            EnemyTable.Load();
            WaveTable = new WaveTable();
            WaveTable.Load(EnemyTable);
            /*
            for (int i = 0; i < SkillTable.Count; i++)
            {
                SkillDef def = SkillTable.Get(i);
                Debug.Log($"[TableLoader] Selected SkillDef {def.ID}, {def.Name}, {def.UnitSkill}");
            }
            for (int i = 0; i < UnitTable.Count; i++)
            {
                UnitDef def = UnitTable.Get(i);
                Debug.Log($"[TableLoader] Selected UnitDef {def.ID}, {def.Name}, {def.UnitStat.MaxHp}");
            }
            for (int i = 0; i < AugmentTable.Count; i++)
            {
                AugmentDef def = AugmentTable.Get(i);
                Debug.Log($"[TableLoader] Selected AugmentDef {def.ID}, {def.Name}, {def.Type}");
            }
            for (int i = 0; i < WaveTable.Count; i++)
            {
                WaveDef def = WaveTable.Get(i);
                Debug.Log($"[TableLoader] Selected wavDef {def.ID}, {def.SpawnTimer}, {def.EnemyDef.Name}");
            }
            */
            //structureTable = new StructureTable();
            //structureTable.Load();
        }
    }
}