using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class LevelTable
    {
        private const string TableName = "Table/PROJECT3D/levels.tsv";

        // 0번 = 1레벨
        private List<LevelDef> levelDefs = new();

        public int Count => levelDefs.Count;

        // 이 레벨에서 다음 레벨까지 필요한 경험치
        // 표보다 높은 레벨이면 마지막 줄 값을 계속 쓴다
        public int GetRequiredExp(int level)
        {
            if (levelDefs.Count == 0)
            {
                Debug.LogWarning("[LevelTable] 레벨 테이블이 비어 있습니다");
                return 10;
            }

            int index = level - 1;
            if (index >= levelDefs.Count) index = levelDefs.Count - 1;

            // 0 이하면 레벨업이 무한 반복되니 최소 1
            return Mathf.Max(1, levelDefs[index].RequiredExp);
        }

        public void Load()
        {
            levelDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<LevelDto> rows = TSVReader.ReadTable<LevelDto>(filePath);

            for (int i = 0; i < rows.Count; i++)
            {
                LevelDto dto = rows[i];

                LevelDef def = new LevelDef();
                def.Level = dto.Level;
                def.RequiredExp = dto.RequiredExp;
                levelDefs.Add(def);
            }
        }
    }
}