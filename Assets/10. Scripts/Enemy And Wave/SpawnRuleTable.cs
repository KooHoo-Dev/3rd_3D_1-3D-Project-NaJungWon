using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class SpawnRuleTable
    {
        private const string TableName = "Table/PROJECT3D/spawnRules.tsv";

        private List<SpawnRuleDef> ruleDefs = new(); // 표에 적힌 순서 그대로

        public int Count => ruleDefs.Count;

        #region 외부 설정

        public SpawnRuleDef Get(int index) => ruleDefs[index];

        public void Load(EnemyTable enemyTable)
        {
            ruleDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<SpawnRuleDto> rows = TSVReader.ReadTable<SpawnRuleDto>(filePath);
            foreach (SpawnRuleDto dto in rows)
            {
                EnemyDef enemyDef = enemyTable.Get(dto.EnemyID);
                if (enemyDef == null)
                {
                    Debug.LogWarning($"[SpawnRuleTable] {dto.ID}번 규칙의 몹 {dto.EnemyID}이 없습니다");
                    continue;
                }

                ruleDefs.Add(new SpawnRuleDef()
                {
                    ID = dto.ID,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    EnemyDef = enemyDef,
                    Interval = dto.Interval,
                    Count = dto.Count,
                    MinAlive = dto.MinAlive,
                    MaxAlive = dto.MaxAlive, // 새로 추가
                });
            }
        }

        #endregion
    }
}
