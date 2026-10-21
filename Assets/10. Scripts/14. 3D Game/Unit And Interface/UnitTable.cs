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

        public void Load(SkillTable skillTable)
        {
            unitDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<UnitDto> rows = TSVReader.ReadTable<UnitDto>(filePath);
            foreach (UnitDto dto in rows)
            {
                GameObject unitBody = Resources.Load<GameObject>(dto.PrefabKey);
                if(unitBody == null) Debug.LogWarning($"[UnitTable] {dto.PrefabKey} 경로에 파일 없음");
                Sprite sprite = Resources.Load<Sprite>(dto.SpriteKey);
                if(sprite == null) Debug.LogWarning($"[UnitTable] {dto.PrefabKey} 경로에 파일 없음");
                SkillDef passiveSkillDef = skillTable.Get(dto.PassiveSkillID);
                if(passiveSkillDef == null) Debug.LogWarning($"[UnitTable] {dto.PassiveSkillID} 경로에 파일 없음");
                SkillDef eSkillDef = skillTable.Get(dto.ESkillID);
                if(eSkillDef == null) Debug.LogWarning($"[UnitTable] {dto.ESkillID} 경로에 파일 없음");
                SkillDef qSkillDef = skillTable.Get(dto.QSkillID);
                if(qSkillDef == null) Debug.LogWarning($"[UnitTable] {dto.QSkillID} 경로에 파일 없음");
                
                unitDefs[dto.ID] = new UnitDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    UnitBody = unitBody,
                    Sprite = sprite,
                    PassiveSkill = passiveSkillDef,
                    ESkill = eSkillDef,
                    QSkill = qSkillDef,
                    UnitStat = new StatClass
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
                    }
                };
            }
        }
    }
}