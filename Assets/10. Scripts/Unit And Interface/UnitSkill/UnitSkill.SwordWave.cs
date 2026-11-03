using UnityEngine;

namespace PROJECT3D
{
    // 검기 날리기 (논타겟)
    //  - 화면 정중앙(크로스헤어)이 가리키는 곳을 향해 검기 투사체를 한 발 날린다
    //  - 투사체는 NonTargetHit 프리팹 (hitShape = Cube, pierce 체크) → 직육면체 판정으로 관통하며 직진
    //  - 조준은 부모(UnitSkill)의 GetScreenCenterDirection 사용 // [변경]
    public class SkillSwordWave : UnitSkill
    {
        [Header("검기")]
        [SerializeField] private NonTargetHit wavePrefab;   // 검기 투사체 (hitShape를 Cube로)

        #region 외부 설정

        // 공격 시점(애니메이션 / 딜레이 후)에 불린다
        protected override void Use(Vector3 aimDirection)
        {
            Vector3 direction = GetScreenCenterDirection(FirePosition, aimDirection);

            // 몸은 똑바로 선 채 좌우 방향만 검기 방향으로
            Vector3 lookDirection = direction;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f) Owner.transform.rotation = Quaternion.LookRotation(lookDirection);

            FireHit(wavePrefab, direction, GetDamage());
        }

        #endregion
    }
}