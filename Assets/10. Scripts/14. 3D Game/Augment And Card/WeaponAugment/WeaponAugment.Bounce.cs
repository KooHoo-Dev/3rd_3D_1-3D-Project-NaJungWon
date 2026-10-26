using UnityEngine;

namespace PROJECT3D
{
    // 가장 가까운 적에게 튕기는 투사체를 쏜다
    // 진화하면 타겟이 존재한다면 무한히 튕긴다 (렉걸리면 바꿀예정)
    public class Bounce : WeaponAugment
    {
        [Header("바운스")]
        [SerializeField] private TargetingHit projectilePrefab;
        [SerializeField] private int bounceCount = 3;        //튕기는 횟수
        [SerializeField] private int arousalBounceCount = int.MaxValue; //진화 후 튕기는 횟수

        protected override void Attack() //쓸때 튕기는 횟수는 제한이없더라도 주변 몹 탐색 횟수는 제한을 둬야함.
        {
            Enemy target = FindTarget(projectilePrefab.BaseRange);
            if (target == null) return;

            int count = bounceCount;
            if (isArousal) count = arousalBounceCount;

            for (int i = 0; i < GetProjectileCount(); i++)
            {
                HitUnit hit = FireHit(projectilePrefab, GetAimDirection(target), GetDamage());
                if (hit == null) continue;

                TargetingHit targetingHit = (TargetingHit)hit;
                targetingHit.SetTarget(target, count);
            }
        }
    }
}