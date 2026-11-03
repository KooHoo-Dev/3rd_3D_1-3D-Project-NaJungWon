using UnityEngine;

// ===== 새로 추가 : 파일 전체 (회복 구조물) =====
namespace PROJECT3D
{
    // 회복 구조물 : 활성화한 플레이어가 최대 체력의 Value% 회복 (Value는 구조물 표)
    public class HealStructure : Structure
    {
        #region 외부 설정

        protected override void ApplyEffect(Player player)
        {
            int healAmount = (int)(player.Stat.unitStat.MaxHp * Def.Value / 100f); // 회복 카드(AugmentManager)와 같은 계산
            player.Stat.ApplyHeal(healAmount);
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
