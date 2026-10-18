using UnityEngine;

namespace PROJECT3D
{
    public enum SkillType
    {
        Passive,
        Active,
    }
    
    public class SkillDto
    {
        public int ID;
        public string Name;
        public string IconKey;
        public string PrefabKey;
        public string Description;
    }
    
    [System.Serializable]
    public class SkillDef
    {
        public int ID;
        public string Name;
        public Sprite Icon;
        public UnitSkill UnitSkill;
        public string Description;
    }
}