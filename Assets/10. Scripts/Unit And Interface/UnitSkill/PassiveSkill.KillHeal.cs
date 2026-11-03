using UnityEngine;

// ===== 새로 추가 : 파일 전체 (전사 패시브) =====
namespace PROJECT3D
{
    // 처치 회복 : 몹을 처치할 때마다 체력을 healPerKill만큼 회복
    //  - Player.OnKill(막타를 친 플레이어에게만 불린다)에 연결
    public class KillHealPassive : PassiveSkill
    {
        [Header("처치 회복")]
        [SerializeField] private int healPerKill = 2; // 처치 1번당 회복량

        #region UnityMethod

        private void OnDestroy()
        {
            if (Owner != null) Owner.OnKill -= HandleKill;
        }

        #endregion

        #region 외부 설정

        protected override void OnInit()
        {
            Owner.OnKill += HandleKill;
        }

        #endregion

        #region 내부 설정

        private void HandleKill()
        {
            if (Owner.Stat.IsDead) return;
            Owner.Stat.ApplyHeal(healPerKill);
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
