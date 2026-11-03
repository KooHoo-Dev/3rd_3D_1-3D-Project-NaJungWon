using UnityEngine;

// ===== 새로 추가 : 파일 전체 (자석 구조물) =====
namespace PROJECT3D
{
    // 자석 구조물 : 맵에 있는 경험치 구슬이 전부 활성화한 플레이어에게 날아간다
    //  - 이미 다른 플레이어를 따라가는 구슬은 그대로 (ExpOrb.AttractAll 재사용)
    public class MagnetStructure : Structure
    {
        #region 외부 설정

        protected override void ApplyEffect(Player player)
        {
            ExpOrb.AttractAll(player);
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
