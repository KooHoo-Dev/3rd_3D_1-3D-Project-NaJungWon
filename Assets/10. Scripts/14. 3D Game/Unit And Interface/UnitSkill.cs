using UnityEngine;

namespace PROJECT3D
{
    public abstract class UnitSkill : MonoBehaviour
    {
        private SkillDef skillDef;

        public bool requestSkill = false;
        public bool canActive = false;
        
        public float Timer;
        
        public void Update()
        {
            Timer = Mathf.Clamp(Timer, 0, skillDef.SkillInterval); //여기서 스킬쿨타임에 기본스탯 스킬쿨감 적용한 값 넣어야함
        }

        public void RequestSkill()
        {
            if (requestSkill == false) return;
            requestSkill = false;
            canActive = true;
        }
        
        public virtual void ApplySkill()
        {
            
        }
    }
}