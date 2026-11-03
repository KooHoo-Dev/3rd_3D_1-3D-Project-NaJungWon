using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PROJECT3D
{
    // 인게임 우측 하단 캐릭터 정보 (집중포화 방식)
    // 캐릭터 아이콘 - 레벨 - 체력 - 패시브 - E/Q 스킬 - 무기 증강 - 능력치 증강
    public class PlayerInfoUI : MonoBehaviour
    {
        [SerializeField] private Image playerIcon;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image hpBar;       // Image Type = Filled
        [SerializeField] private TMP_Text hpText;

        [Header("아이콘 칸")]
        [SerializeField] private HudSlotUI passiveSlot;
        [SerializeField] private HudSlotUI[] skillSlots;   // 0 : E, 1 : Q
        [SerializeField] private HudSlotUI[] weaponSlots;
        [SerializeField] private HudSlotUI[] abilitySlots;

        [Header("조준점")] // [변경]
        [SerializeField] private GameObject crosshair; // 새로 추가 : 화면 중앙 조준점 (조준 스킬로 줌인한 동안에만 켠다)

        private Player player;
        private PlayerController controller; // 새로 추가 : 조준 중인지 확인용
        private UnitSkill[] skills = new UnitSkill[2]; // 쿨타임 표시용 (0 : E, 1 : Q)

        #region UnityMethod

        private void Update()
        {
            if (player == null) return;

            // 체력
            int hp = player.Stat.Hp;
            int maxHp = player.Stat.unitStat.MaxHp;
            if (maxHp > 0) hpBar.fillAmount = (float)hp / maxHp;
            hpText.SetText($"{hp} / {maxHp}");

            // 스킬 쿨타임
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null) skillSlots[i].SetCooldown(skills[i].CooldownRemain);
            }

            // 새로 추가 : 조준점은 줌인(조준) 중에만
            if (crosshair != null && controller != null)
            {
                bool isAiming = controller.IsAiming;
                if (crosshair.activeSelf != isAiming) crosshair.SetActive(isAiming);
            }
        }

        private void OnDestroy()
        {
            if (AugmentManager.IsInitialized)
                AugmentManager.Instance.OnAugmentChanged -= RefreshAugments;
        }

        #endregion

        #region 외부 설정

        public void Init(Player player)
        {
            this.player = player;
            UnitDef def = player.unitDef;

            playerIcon.sprite = def.Sprite;
            SetLevel(1); // 레벨 시스템이 생기면 거기서 호출

            // 패시브 (레벨 칸 없음)
            if (def.PassiveSkill != null) passiveSlot.SetIcon(def.PassiveSkill.Icon);
            else passiveSlot.SetEmpty();
            passiveSlot.SetCooldown(0f);
            // [변경] passiveSlot.HideLevel() 삭제 : 패시브는 레벨 칸이 없다

            // E/Q 스킬
            controller = player.GetComponent<PlayerController>(); // [변경] 지역 변수 → 필드 (Update에서 조준 확인)
            if (crosshair != null) crosshair.SetActive(false);   // 새로 추가 : 처음엔 조준점 숨김
            skills[0] = controller.UnitESkill;
            skills[1] = controller.UnitQSkill;
            SetSkillSlot(skillSlots[0], def.ESkill, "E"); // [변경] 키 글자 추가
            SetSkillSlot(skillSlots[1], def.QSkill, "Q"); // [변경] 키 글자 추가

            // 증강은 바뀔 때마다 다시 그린다
            AugmentManager.Instance.OnAugmentChanged += RefreshAugments;
            RefreshAugments();
        }

        public void SetLevel(int level)
        {
            levelText.SetText(level.ToString());
        }

        #endregion

        #region 내부 설정

        #region 스킬 칸

        // [변경] keyText 추가 : 스킬 칸의 레벨 글자는 키(E/Q)로 고정
        private void SetSkillSlot(HudSlotUI slot, SkillDef skillDef, string keyText)
        {
            if (skillDef == null) slot.SetEmpty();
            else
            {
                slot.SetIcon(skillDef.Icon);
                slot.SetCooldown(0f);
            }

            slot.SetLevelText(keyText); // [변경] HideLevel() → E/Q 글자 (스킬이 없어도 키는 표시)
        }

        #endregion

        #region 증강 칸

        // 보유 증강을 무기 칸 / 능력치 칸에 앞에서부터 채운다
        private void RefreshAugments()
        {
            int weaponIndex = 0;
            int abilityIndex = 0;

            for (int i = 0; i < AugmentManager.Instance.OwnedAugments.Count; i++)
            {
                OwnedAugment owned = AugmentManager.Instance.OwnedAugments[i];

                if (owned.Def.Type == AugmentType.Weapon && weaponIndex < weaponSlots.Length)
                {
                    SetAugmentSlot(weaponSlots[weaponIndex], owned);
                    weaponIndex++;
                }
                else if (owned.Def.Type == AugmentType.Ability && abilityIndex < abilitySlots.Length)
                {
                    SetAugmentSlot(abilitySlots[abilityIndex], owned);
                    abilityIndex++;
                }
            }

            // 남은 칸은 비운다
            for (int i = weaponIndex; i < weaponSlots.Length; i++) weaponSlots[i].SetEmpty();
            for (int i = abilityIndex; i < abilitySlots.Length; i++) abilitySlots[i].SetEmpty();
        }

        private void SetAugmentSlot(HudSlotUI slot, OwnedAugment owned)
        {
            slot.SetIcon(owned.Def.Sprite);
            slot.SetCooldown(0f);
            slot.SetLevel(owned.Level);
        }

        #endregion

        #endregion
    }
}
