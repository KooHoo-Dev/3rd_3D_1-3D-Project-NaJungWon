using UnityEngine;
using UnityEngine.Serialization;

namespace PROJECT3D
{
    public abstract class AugmentCard : MonoBehaviour
    {
        private AugmentDef augmentDef;

        public int augmentLevel = 0;
        public GameObject projectile;
        public GameObject hitParticle;

        public bool isArousal;
        
        public void Update()
        {
            if (augmentDef.Type != AugmentType.Weapon) return;
            
            if (isArousal == false) ApplyNormalState();
            else ApplyArousalState();
        }

        protected virtual void ApplyAbilityState()
        {
            
        }

        protected virtual void ApplyNormalState()
        {
            
        }

        protected virtual void ApplyArousalState()
        {
            
        }
    }
}