using UnityEngine;

namespace PROJECT3D
{
    public class SkillDash : UnitSkill
    {
        //이건 생각좀 해보자
        [Header("대쉬 이펙트")]
        [SerializeField] private ParticleEffect effectPrefab;

        protected override void Use(Vector3 aimDirection)
        {
            Debug.Log($"[SkillDash] {Owner.GameObject.name}가 대쉬스킬을 사용!");
        }
    }
}