using UnityEngine;

namespace PROJECT3D
{
    public class TableLoader : MonoBehaviour
    {
        private UnitTable unitTable;
        private SkillTable skillTable;
        private EnemyTable enemyTable;
        private AugmentTable augmentTable;
        private StructureTable structureTable;

        private void Awake()
        {
            unitTable = new UnitTable();
            unitTable.Load();
            skillTable = new SkillTable();
            skillTable.Load();
            augmentTable = new AugmentTable();
            augmentTable.Load();
            //structureTable = new StructureTable();
            //structureTable.Load();
        }
    }
}