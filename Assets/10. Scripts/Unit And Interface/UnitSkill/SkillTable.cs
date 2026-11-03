using System.Collections.Generic;
using System.IO;
using Jay.FileIO;
using UnityEngine;

namespace PROJECT3D
{
    public class SkillTable
    {
        private const string TableName = "Table/PROJECT3D/skills.tsv";
        
        private Dictionary<int, SkillDef> skillDefs = new();

        public int Count => skillDefs.Count;

        #region 외부 설정

        // [변경] 없는 ID(-1 = 스킬 없음 등)면 null (원래는 바로 에러). 받는 쪽(UnitTable)이 null을 경고로 처리한다
        public SkillDef Get(int id)
        {
            if (skillDefs.TryGetValue(id, out SkillDef def)) return def;
            return null;
        }

        public void Load()
        {
            skillDefs.Clear();
            string filePath = Path.Combine(Application.streamingAssetsPath, TableName);
            List<SkillDto> rows = TSVReader.ReadTable<SkillDto>(filePath);
            foreach (SkillDto dto in rows)
            {
                Sprite icon = Resources.Load<Sprite>(dto.IconKey);
                UnitSkill skill = Resources.Load<UnitSkill>(dto.PrefabKey);
                PassiveSkill passive = Resources.Load<PassiveSkill>(dto.PrefabKey); // 새로 추가 : 패시브 스킬 프리팹
                if(skill == null && passive == null) Debug.LogWarning($"[SkillTable] {dto.PrefabKey} 경로에 파일 없음"); // [변경] 패시브면 경고 안 함

                skillDefs[dto.ID] = new SkillDef()
                {
                    ID = dto.ID,
                    Name = dto.Name,
                    Icon = icon,
                    UnitSkill = skill,
                    Passive = passive, // 새로 추가
                    SkillStat = new StatClass
                    {
                        MaxHp = dto.MaxHp,
                        HealthRegeneration = dto.HealthRegeneration,
                        Damage = dto.Damage,
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
                    Cooldown = dto.Cooldown,
                    Description = dto.Description,
                };
            }
        }

        #endregion
    }
}