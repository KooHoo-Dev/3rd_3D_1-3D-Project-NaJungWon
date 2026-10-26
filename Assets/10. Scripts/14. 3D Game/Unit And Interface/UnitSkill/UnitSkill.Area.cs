using UnityEngine;

namespace PROJECT3D
{
    // 범위 스킬 : 정한 위치에 범위(SummonHit) 소환
    //  - forwardDistance = 0 : 내 위치 중심
    //  - forwardDistance > 0 : 화면 방향으로 그 거리만큼 앞
    public class AreaSkill : UnitSkill
    {
        [Header("범위 스킬")]
        [SerializeField] private HitUnit zonePrefab;          // SummonHit 프리팹 (반경/대기/이펙트는 프리팹에서)
        [SerializeField] private float forwardDistance = 0f;  // 범위 중심을 얼마나 앞에 둘지

        protected override void Use(Vector3 aimDirection)
        {
            Debug.Log($"[AreaSkill] {Owner.GameObject.name}가 범위스킬을 사용!");
            Vector3 center = Owner.transform.position + aimDirection * forwardDistance;
            FireHit(zonePrefab, center, aimDirection, damage);
        }
    }
}