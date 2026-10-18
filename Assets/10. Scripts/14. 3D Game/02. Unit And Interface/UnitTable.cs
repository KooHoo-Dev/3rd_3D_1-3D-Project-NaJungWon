using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class UnitTable
    {
        private const string TableName = "Table/PROJECT3D/Units.tsv";
        
        private Dictionary<int, UnitDef> unitDefs = new();

        public int Count => unitDefs.Count;
        
        public UnitDef Get(int id) => unitDefs[id];

        public void Load()
        {
            unitDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<UnitDto> rows = TSVReader.ReadTable<UnitDto>(filePath);
            foreach (UnitDto dto in rows)
            {
                Unit unit = Resources.Load<Unit>(dto.PrefabKey);
                if(unit == null) Debug.LogWarning($"[UnitTable] {dto.PrefabKey} 경로에 파일 없음");
                Sprite sprite = Resources.Load<Sprite>(dto.SpriteKey);
                if(sprite == null) Debug.LogWarning($"[UnitTable] {dto.PrefabKey} 경로에 파일 없음");
                
                unitDefs[dto.ID] = new UnitDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    Unit = unit,
                    Sprite = sprite,
                    UnitType = dto.UnitType,
                    UnitProperty = dto.UnitProperty,
                    UnitStat = new StatClass
                    {
                        MaxHealth = dto.MaxHealth,
                        HealthRegeneration = dto.HealthRegeneration,
                        Attack = dto.Attack,
                        AttackInterval = dto.AttackInterval,
                        Defense = dto.Defense,
                        Speed = dto.Speed,
                        Critical = dto.Critical,
                        ProjectileCount = dto.ProjectileCount,
                        AttackRange = dto.AttackRange,
                        Duration = dto.Duration,
                        AcquisitionRange = dto.AcquisitionRange,
                        ExperienceGain = dto.ExperienceGain,
                        GoldGain = dto.GoldGain,
                    }
                };
            }
        }
    }
}