using UnityEngine;

namespace PROJECT3D
{
    public class TableLoader : MonoBehaviour
    {
        private UnitTable unitTable;
        private SkillTable skillTable;
        private EnemyTable enemyTable;
        private WaveTable waveTable;
        private AugmentTable augmentTable;
        private StructureTable structureTable;

        private void Awake()
        {
            skillTable = new SkillTable();
            skillTable.Load();
            for (int i = 0; i < skillTable.Count; i++)
            {
                SkillDef def = skillTable.Get(i);
                Debug.Log($"[TableLoader] Selected SkillDef {def.ID}, {def.Name}, {def.UnitSkill}");
            }
            
            unitTable = new UnitTable();
            unitTable.Load(skillTable);
            for (int i = 0; i < unitTable.Count; i++)
            {
                UnitDef def = unitTable.Get(i);
                Debug.Log($"[TableLoader] Selected UnitDef {def.ID}, {def.Name}, {def.UnitStat.MaxHp}");
            }
            
            augmentTable = new AugmentTable();
            augmentTable.Load();
            for (int i = 0; i < augmentTable.Count; i++)
            {
                AugmentDef def = augmentTable.Get(i);
                Debug.Log($"[TableLoader] Selected AugmentDef {def.ID}, {def.Name}, {def.Type}");
            }

            enemyTable = new EnemyTable();
            enemyTable.Load();

            waveTable = new WaveTable();
            waveTable.Load(enemyTable);
            for (int i = 0; i < waveTable.Count; i++)
            {
                WaveDef def = waveTable.Get(i);
                Debug.Log($"[TableLoader] Selected wavDef {def.ID}, {def.SpawnTimer}, {def.EnemyDef.Name}");
            }
            
            //structureTable = new StructureTable();
            //structureTable.Load();
        }

        public void LoadData()
        {
            
        }
    }
}