using System.Collections.Generic;
using UnityEngine;

namespace PROJECT3D
{
    // 증강 선택창 좌측의 진화 가능 무기 목록 (집중포화 좌측 상단 방식)
    // 능력치 증강 한 줄마다 "이 능력치를 가지고 있으면 진화하는 무기들"을 보여준다
    // (무기의 NeedArousal == 능력치 증강의 MainStat)
    public class ArousalListPanel : MonoBehaviour
    {
        [SerializeField] private ArousalRowUI[] rows;

        private List<AugmentDef> abilities = new List<AugmentDef>(); // 줄 순서대로 능력치 증강
        private List<AugmentDef> weapons = new List<AugmentDef>();   // 진화가 있는 무기 증강

        // 테이블에서 능력치 증강 / 무기 증강을 나눠 담는다 (게임 시작 시 한 번)
        public void Init()
        {
            abilities.Clear();
            weapons.Clear();

            foreach (AugmentDef def in TableLoader.Instance.AugmentTable.All)
            {
                if (def.Type == AugmentType.Ability) abilities.Add(def);
                else if (def.Type == AugmentType.Weapon && def.NeedArousal != StatType.None) weapons.Add(def);
            }

            if (abilities.Count > rows.Length)
                Debug.LogWarning($"[ArousalListPanel] 능력치 증강({abilities.Count})이 줄 수({rows.Length})보다 많습니다");
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Refresh()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                // 능력치 증강 수보다 많은 줄은 끈다
                if (i >= abilities.Count)
                {
                    rows[i].gameObject.SetActive(false);
                    continue;
                }

                AugmentDef ability = abilities[i];
                rows[i].gameObject.SetActive(true);
                rows[i].SetAbility(ability.Sprite, IsOwned(ability.ID));

                // 이 능력치로 진화하는 무기들
                int weaponCount = 0;
                for (int w = 0; w < weapons.Count; w++)
                {
                    if (weapons[w].NeedArousal != ability.MainStat) continue;
                    if (weaponCount >= rows[i].WeaponIconCount) break;

                    rows[i].SetWeapon(weaponCount, weapons[w].Sprite, IsOwned(weapons[w].ID));
                    weaponCount++;
                }
                rows[i].HideWeaponsFrom(weaponCount);
            }
        }

        private bool IsOwned(int augmentID)
        {
            List<OwnedAugment> owned = AugmentManager.Instance.OwnedAugments;
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i].Def.ID == augmentID) return true;
            }
            return false;
        }
    }
}
