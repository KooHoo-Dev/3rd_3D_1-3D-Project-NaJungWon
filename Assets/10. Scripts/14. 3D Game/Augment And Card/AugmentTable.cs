using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class AugmentTable
    {
        private const string TableName = "Table/PROJECT3D/augments.tsv";

        private Dictionary<int, AugmentDef> augmentDefs = new();

        public AugmentDef Get(int id) => augmentDefs[id];

        public IReadOnlyCollection<AugmentDef> GetDefs() => augmentDefs.Values;
        
        public int Count => augmentDefs.Count;

        public void Load()
        {
            augmentDefs.Clear();
            
            string path = Application.streamingAssetsPath + "/" + TableName;
            List<AugmentDto> rows = TSVReader.ReadTable<AugmentDto>(path);

            foreach (AugmentDto dto in rows)
            {
                AugmentCard prefab = Resources.Load<AugmentCard>(dto.PrefabKey);
                if (prefab == null) Debug.LogWarning($"[AugmentTable] {dto.PrefabKey} 경로에 파일 없음");
                Sprite sprite = Resources.Load<Sprite>(dto.SpriteKey);
                if (sprite == null) Debug.LogWarning($"[AugmentTable] {dto.SpriteKey} 경로에 파일 없음");

                augmentDefs[dto.ID] = new AugmentDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    AugmentCard = prefab,
                    Sprite = sprite,
                    Type = dto.Type,
                    NeedArousal = dto.NeedArousal,
                    AugmentStat = new StatClass
                    {
                        MaxHp = dto.MaxHp,
                        HealthRegeneration = dto.HealthRegeneration,
                        Damage = dto.Damage,
                        AttackInterval = dto.AttackInterval,
                        Defense = dto.Defense,
                        Speed = dto.Speed,
                        CritChance = dto.CritChance,
                        ProjectileCount = dto.ProjectileCount,
                        AttackRange = dto.AttackRange,
                        Duration = dto.Duration,
                        AcquisitionRange = dto.AcquisitionRange,
                        ExperienceGain = dto.ExperienceGain,
                        GoldGain = dto.GoldGain,
                    },
                    Description = dto.Description
                };
            }
        }
    }
}