using UnityEngine;

namespace PROJECT3D
{
    public abstract class Player
    {
        public UnitDef UnitDef;
        public SkillInfo UnitSkillInfo;

        public class SkillInfo
        {
            public UnitSkill PassiveSkill;
            public UnitSkill ESkill;
            public UnitSkill QSkill;
        }
    }
}