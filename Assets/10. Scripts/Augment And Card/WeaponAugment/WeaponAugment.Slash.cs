using UnityEngine;

namespace PROJECT3D
{
    // 전사 기본 무기 : 검기 베기
    //  - 범위(searchRange) 안에 적이 있으면 가장 가까운 적 방향, 없으면 캐릭터 정면으로 벤다 (캐릭터 기준 수평)
    //  - 플레이어 몸 중심에서 그 방향으로 spawnDistance만큼 떨어진 곳에 직육면체(Cube) 범위를 생성
    //  - 판정 Cube와 이펙트는 몸(BodyPivot) 기울기를 따라 바닥 경사에 맞춰 기운다. 평지에서는 수평
    //  - 투사체 수만큼 벤다 : 첫 번째는 노린 방향, 나머지는 플레이어 둘레로 같은 간격
    //  - 범위 증가 : 탐색 범위 / 생성 거리 / 판정 크기는 수치로, 이펙트 크기는 HitUnit이 오브젝트 scale로 키운다
    public class Slash : WeaponAugment
    {
        [Header("무기 설정")]
        [SerializeField] private SummonHit slashPrefab;          // 베기 범위 (SummonHit, hitShape = Cube)
        [SerializeField] private float     spawnDistance = 1.2f; // 몸 중심에서 이만큼 앞에 생성 (범위 증가 적용)

        #region 외부 설정

        protected override void Attack()
        {
            Vector3 aimDirection = GetTargetOrForwardDirection(); // 부모(WeaponAugment) 공용 조준 재사용

            int slashCount = GetWeaponProjectileCount(); // 무기 기본 개수 + 플레이어 개수(상점 + 능력증강)
            float angleStep = 360f / slashCount;

            for (int i = 0; i < slashCount; i++)
            {
                // 노린 방향에서 i번째 각도만큼 돌린 방향 (i = 0이면 노린 방향 그대로)
                Vector3 direction = Quaternion.Euler(0f, angleStep * i, 0f) * aimDirection;
                FireSlash(direction);
            }
        }

        #endregion

        #region 내부 설정

        // direction 쪽으로 한 번 벤다
        // 몸(BodyPivot) 기울기를 따라 벤다 : 경사면에서는 베는 면도 바닥 경사에 맞춰 기운다 (판정 Cube도 같이)
        private void FireSlash(Vector3 direction)
        {
            Vector3 bodyUp = GetBodyUp();

            // 수평 조준 방향을 기울어진 바닥 면에 눕힌다 (오르막이면 앞이 들리고, 내리막이면 앞이 내려간다)
            Vector3 tiltedDirection = Vector3.ProjectOnPlane(direction, bodyUp);
            if (tiltedDirection.sqrMagnitude < 0.01f) tiltedDirection = direction; // 거의 수직 (일어날 일은 거의 없음)
            tiltedDirection.Normalize();

            Vector3 center = Owner.HitCenter + tiltedDirection * ApplyRangeBonus(spawnDistance); // 기울어진 방향으로 생성

            HitUnit hit = FireHit(slashPrefab, center, tiltedDirection, GetDamage());
            if (hit != null) hit.transform.rotation = Quaternion.LookRotation(tiltedDirection, bodyUp); // 좌우 기울기까지 몸과 같게
        }

        // 몸(BodyPivot)의 위쪽 = 지금 바닥 기울기 (BodyTilt가 기울인다). 없으면 똑바로 위
        private Vector3 GetBodyUp()
        {
            Player player = Owner as Player;
            if (player == null || player.BodyPivot == null) return Vector3.up;
            return player.BodyPivot.up;
        }

        #endregion
    }
}
