using UnityEngine;

namespace PROJECT3D
{
    // 투사체 스킬 : 화면 정중앙이 가리키는 곳으로 HitUnit 한 발 // [변경]
    //  - 맞으면 터지게 하려면 NonTargetHit 프리팹의 explodePrefab에 범위(SummonHit) 프리팹을 넣는다 // [변경] 폭발 이펙트 / 피격 이펙트는 범위 프리팹(explodeEffect / impactEffect)에서
    public class FireBallSkill : UnitSkill
    {
        [Header("투사체 스킬")]
        [SerializeField] private HitUnit fireBallPrefab;

        #region 외부 설정

        protected override void Use(Vector3 aimDirection)
        {
            Debug.Log($"[ProjectileSkill] {Owner.GameObject.name}가 투사체 발사 스킬을 사용!");
            Vector3 direction = GetScreenCenterDirection(FirePosition, aimDirection); // [변경] 발사 위치 → 화면 중앙
            FireHit(fireBallPrefab, direction, GetDamage());
        }

        #endregion
    }
}