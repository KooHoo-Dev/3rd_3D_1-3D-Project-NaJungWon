using UnityEngine;

namespace PROJECT3D
{
    // 투사체 스킬 : 화면 정중앙이 가리키는 곳으로 HitUnit 한 발
    //  - 맞으면 터지게 하려면 NonTargetHit 프리팹의 explodePrefab에 범위(SummonHit) 프리팹을 넣는다
    public class FireBallSkill : UnitSkill
    {
        [Header("투사체 스킬 설정")]
        [SerializeField] private HitUnit fireBallPrefab; // 발사할 투사체 (NonTargetHit. 폭발은 프리팹의 explodePrefab)

        #region 외부 설정

        protected override void Use(Vector3 aimDirection)
        {
            Debug.Log($"[ProjectileSkill] {Owner.GameObject.name}가 투사체 발사 스킬을 사용!");
            Vector3 direction = GetScreenCenterDirection(FirePosition, aimDirection);
            FireHit(fireBallPrefab, direction, GetDamage());
        }

        #endregion
    }
}