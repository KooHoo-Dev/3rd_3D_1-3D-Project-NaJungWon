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

        #region 외부 설정

        protected override void Use(Vector3 aimDirection)
        {
            Debug.Log($"[AreaSkill] {Owner.GameObject.name}가 범위스킬을 사용!");
            // [변경] 조준이 3D 방향이 되어서, 바닥 범위는 수평 방향만 쓴다 (범위가 공중/땅속에 생기지 않게)
            Vector3 flatDirection = aimDirection;
            flatDirection.y = 0f;
            flatDirection.Normalize();

            Vector3 center = Owner.transform.position + flatDirection * forwardDistance;
            FireHit(zonePrefab, center, flatDirection, GetDamage());
        }

        #endregion
    }
}