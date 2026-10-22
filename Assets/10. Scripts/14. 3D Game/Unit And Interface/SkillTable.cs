using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class SkillTable
    {
        private const string TableName = "Table/PROJECT3D/Skill.tsv";
        
        private Dictionary<int, SkillDef> skillDefs = new();

        public int Count => skillDefs.Count;
        
        public SkillDef Get(int id) => skillDefs[id];

        public void Load()
        {
            skillDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<SkillDto> rows = TSVReader.ReadTable<SkillDto>(filePath);
            foreach (SkillDto dto in rows)
            {
                Sprite icon = Resources.Load<Sprite>(dto.IconKey);
                UnitSkill skill = Resources.Load<UnitSkill>(dto.PrefabKey);
                if(skill == null) Debug.LogWarning($"[SkillTable] {dto.PrefabKey} 경로에 파일 없음");

                skillDefs[dto.ID] = new SkillDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    Icon = icon,
                    UnitSkill = skill,
                    SkillStat = new StatClass
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
                        GoldGain = dto.GoldGain
                    },
                    Description = dto.Description,
                };
            }
        }
    }
}