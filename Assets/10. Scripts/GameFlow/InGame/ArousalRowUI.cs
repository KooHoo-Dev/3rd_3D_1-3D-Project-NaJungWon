using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 진화 목록의 한 줄 : [능력치 증강 아이콘] [그 능력치로 진화하는 무기 아이콘들]
    // 보유한 증강은 밝게, 없는 증강은 흐리게
    public class ArousalRowUI : MonoBehaviour
    {
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image[] weaponIcons;

        private static readonly Color OwnedColor = Color.white;
        private static readonly Color NotOwnedColor = new Color(1f, 1f, 1f, 0.3f);

        public int WeaponIconCount => weaponIcons.Length;

        public void SetAbility(Sprite sprite, bool owned)
        {
            abilityIcon.sprite = sprite;
            abilityIcon.color = owned ? OwnedColor : NotOwnedColor;
        }

        public void SetWeapon(int index, Sprite sprite, bool owned)
        {
            weaponIcons[index].gameObject.SetActive(true);
            weaponIcons[index].sprite = sprite;
            weaponIcons[index].color = owned ? OwnedColor : NotOwnedColor;
        }

        // index 번부터 남은 무기 칸을 끈다
        public void HideWeaponsFrom(int index)
        {
            for (int i = index; i < weaponIcons.Length; i++)
            {
                weaponIcons[i].gameObject.SetActive(false);
            }
        }
    }
}
