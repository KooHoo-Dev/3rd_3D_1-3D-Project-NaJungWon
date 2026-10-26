using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class AugmentLevelTable
    {
        private const string TableName = "Table/PROJECT3D/augmentLevels.tsv";

        // key : 증강 ID, value : 그 증강의 레벨별 데이터 (0번 = 1레벨)
        private Dictionary<int, List<AugmentLevelDef>> augmentLevelDefs = new();

        // id 증강의 level 데이터를 가져온다
        public AugmentLevelDef Get(int id, int level)
        {
            return augmentLevelDefs[id][level - 1];
        }

        // 최대 레벨 = 레벨 데이터의 줄 수. 레벨 데이터가 없는 증강(특수카드)은 0
        public int GetMaxLevel(int id)
        {
            if (augmentLevelDefs.ContainsKey(id) == false) return 0;
            return augmentLevelDefs[id].Count;
        }

        public void Load()
        {
            augmentLevelDefs.Clear();

            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<AugmentLevelDto> rows = TSVReader.ReadTable<AugmentLevelDto>(filePath);

            // TSV는 같은 ID끼리 Level 1, 2, 3 순서대로 적는다
            for (int i = 0; i < rows.Count; i++)
            {
                AugmentLevelDto dto = rows[i];

                StatClass stat = new StatClass();
                stat.MaxHp = dto.MaxHp;
                stat.HealthRegeneration = dto.HealthRegeneration;
                stat.Damage = dto.Damage;
                stat.AttackInterval = dto.AttackInterval;
                stat.Defense = dto.Defense;
                stat.Speed = dto.Speed;
                stat.CritChance = dto.CritChance;
                stat.ProjectileCount = dto.ProjectileCount;
                stat.AttackRange = dto.AttackRange;
                stat.Duration = dto.Duration;
                stat.AcquisitionRange = dto.AcquisitionRange;
                stat.ExperienceGain = dto.ExperienceGain;
                stat.GoldGain = dto.GoldGain;

                AugmentLevelDef def = new AugmentLevelDef();
                def.ID = dto.ID;
                def.Level = dto.Level;
                def.AugmentLevelStat = stat;

                // 처음 나온 ID면 리스트부터 만들어준다
                if (augmentLevelDefs.ContainsKey(dto.ID) == false)
                    augmentLevelDefs.Add(dto.ID, new List<AugmentLevelDef>());

                augmentLevelDefs[dto.ID].Add(def);
            }
        }
    }
}
