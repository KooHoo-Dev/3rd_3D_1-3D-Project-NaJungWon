using UnityEngine;
#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
#endif

namespace PROJECT3D
{
    // 이미 만들어 둔 인게임 UI(하이어라키)에 스크립트를 붙이고 참조를 연결하는 에디터 툴
    // 인게임 씬의 UI/Canvas 에 붙이고 컴포넌트 우클릭 → Link UI
    // 오브젝트 이름(경로)으로 찾기 때문에 이름을 바꾸면 여기 경로도 같이 바꿔야 한다
    public class InGameUILinker : MonoBehaviour
    {
#if UNITY_EDITOR
        #region 외부 설정

        [ContextMenu("Link UI")]
        private void LinkUI()
        {
            GameInfoUI gameInfo = LinkGameInfo();
            PlayerInfoUI playerInfo = LinkPlayerInfo();
            PlayerStatPanel statPanel = LinkPlayerStat();
            ArousalListPanel arousalPanel = LinkArousalList();
            if (gameInfo == null || playerInfo == null || statPanel == null || arousalPanel == null)
            {
                Debug.LogError("[InGameUILinker] 연결 실패. 위 에러의 경로를 확인하세요");
                return;
            }

            LinkSelectCard(arousalPanel, statPanel);
            InGameUI inGameUI = LinkInGameUI(gameInfo, playerInfo, statPanel, arousalPanel);
            LinkGameManager(inGameUI);

            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[InGameUILinker] 인게임 UI 연결 완료");
        }

        #endregion

        #region 내부 설정

        #region 영역별 연결

        // 상단 : 타이머 / 경험치 바 / 골드 / 킬
        private GameInfoUI LinkGameInfo()
        {
            Transform root = Find(transform, "GameInfo");
            if (root == null) return null;

            Image expBar = FindImage(root, "EXP/ExpBar");
            SetFilled(expBar);

            GameInfoUI ui = root.GetComponent<GameInfoUI>();
            if (ui == null) ui = Undo.AddComponent<GameInfoUI>(root.gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetField(so, "timerText", FindText(root, "Timer/TimerText"));
            SetField(so, "expBar", expBar);
            SetField(so, "goldText", FindText(root, "GoldInfo/GainGoldText"));
            SetField(so, "killCountText", FindText(root, "KillInfo/KillCountText"));
            so.ApplyModifiedProperties();
            return ui;
        }

        // 우측 하단 : 캐릭터 정보
        private PlayerInfoUI LinkPlayerInfo()
        {
            Transform root = Find(transform, "UserInfo/PlayerInfo");
            if (root == null) return null;

            Image hpBar = FindImage(root, "PlayerHp/Hp_Bar");
            SetFilled(hpBar);

            PlayerInfoUI ui = root.GetComponent<PlayerInfoUI>();
            if (ui == null) ui = Undo.AddComponent<PlayerInfoUI>(root.gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetField(so, "playerIcon", FindImage(root, "PlayerIcon/IconMask/Image"));
            SetField(so, "levelText", FindText(root, "LevelIcon/IconMask/LevelText"));
            SetField(so, "hpBar", hpBar);
            SetField(so, "hpText", FindText(root, "PlayerHp/Hp_Text"));
            SetField(so, "passiveSlot", LinkSlot(Find(root, "PassiveSlot"), "passiveIcon"));
            SetArray(so, "skillSlots", LinkSlotsInChildren(Find(root, "SkillInfo")));
            SetArray(so, "weaponSlots", LinkSlotsInChildren(Find(root, "WeaponInfo")));
            SetArray(so, "abilitySlots", LinkSlotsInChildren(Find(root, "AbilityInfo")));
            so.ApplyModifiedProperties();
            return ui;
        }

        // 증강 선택 시 우측 : 플레이어 스탯 (Info 아래 Stat 줄마다 StatName / StatValue)
        private PlayerStatPanel LinkPlayerStat()
        {
            Transform root = Find(transform, "UserInfo/PlayerStat");
            if (root == null) return null;
            Transform info = Find(root, "Info");
            if (info == null) return null;

            TMP_Text[] names = new TMP_Text[info.childCount];
            TMP_Text[] values = new TMP_Text[info.childCount];
            for (int i = 0; i < info.childCount; i++)
            {
                names[i] = FindText(info.GetChild(i), "StatName");
                values[i] = FindText(info.GetChild(i), "StatValue");
            }

            PlayerStatPanel ui = root.GetComponent<PlayerStatPanel>();
            if (ui == null) ui = Undo.AddComponent<PlayerStatPanel>(root.gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetArray(so, "nameTexts", names);
            SetArray(so, "valueTexts", values);
            so.ApplyModifiedProperties();
            return ui;
        }

        // 증강 선택 시 좌측 : 진화 목록 (Slot 아래 ArousalInfo 줄마다)
        private ArousalListPanel LinkArousalList()
        {
            Transform root = Find(transform, "AugmentInfo");
            if (root == null) return null;
            Transform slotRoot = Find(root, "Slot");
            if (slotRoot == null) return null;

            ArousalRowUI[] rows = new ArousalRowUI[slotRoot.childCount];
            for (int i = 0; i < slotRoot.childCount; i++)
            {
                rows[i] = LinkArousalRow(slotRoot.GetChild(i));
            }

            ArousalListPanel ui = root.GetComponent<ArousalListPanel>();
            if (ui == null) ui = Undo.AddComponent<ArousalListPanel>(root.gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetArray(so, "rows", rows);
            so.ApplyModifiedProperties();
            return ui;
        }

        private ArousalRowUI LinkArousalRow(Transform row)
        {
            Transform weaponRoot = Find(row, "WeaponInfo");
            Image[] weaponIcons = new Image[weaponRoot.childCount];
            for (int i = 0; i < weaponRoot.childCount; i++)
            {
                weaponIcons[i] = weaponRoot.GetChild(i).GetComponent<Image>();
            }

            ArousalRowUI ui = row.GetComponent<ArousalRowUI>();
            if (ui == null) ui = Undo.AddComponent<ArousalRowUI>(row.gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetField(so, "abilityIcon", FindImage(row, "AbilityInfo/AbilityIcon"));
            SetArray(so, "weaponIcons", weaponIcons);
            so.ApplyModifiedProperties();
            return ui;
        }

        // 증강 선택창에 좌우 창 연결
        private void LinkSelectCard(ArousalListPanel arousalPanel, PlayerStatPanel statPanel)
        {
            Transform root = Find(transform, "SelectCardSystem");
            if (root == null) return;

            AugmentSelectPanel panel = root.GetComponent<AugmentSelectPanel>();
            SerializedObject so = new SerializedObject(panel);
            SetField(so, "arousalPanel", arousalPanel);
            SetField(so, "statPanel", statPanel);
            so.ApplyModifiedProperties();
        }

        // 이 Canvas에 InGameUI를 붙이고 묶는다
        private InGameUI LinkInGameUI(GameInfoUI gameInfo, PlayerInfoUI playerInfo, PlayerStatPanel statPanel, ArousalListPanel arousalPanel)
        {
            InGameUI ui = GetComponent<InGameUI>();
            if (ui == null) ui = Undo.AddComponent<InGameUI>(gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetField(so, "gameInfo", gameInfo);
            SetField(so, "playerInfo", playerInfo);
            SetField(so, "statPanel", statPanel);
            SetField(so, "arousalPanel", arousalPanel);
            so.ApplyModifiedProperties();
            return ui;
        }

        private void LinkGameManager(InGameUI inGameUI)
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[InGameUILinker] 씬에 GameManager가 없습니다");
                return;
            }

            SerializedObject so = new SerializedObject(gameManager);
            SetField(so, "inGameUI", inGameUI);
            so.ApplyModifiedProperties();
        }

        #endregion

        #region 아이콘 칸 (HudSlotUI)

        // 칸 하나 : Icon(또는 iconName) / Cooldown_Text / Level/LevelText
        private HudSlotUI LinkSlot(Transform slot, string iconName)
        {
            if (slot == null) return null;

            HudSlotUI ui = slot.GetComponent<HudSlotUI>();
            if (ui == null) ui = Undo.AddComponent<HudSlotUI>(slot.gameObject);

            SerializedObject so = new SerializedObject(ui);
            SetField(so, "icon", FindImage(slot, iconName));
            SetField(so, "cooldownText", FindText(slot, "Cooldown_Text"));

            // 패시브 칸처럼 Level이 없는 칸도 있다
            Transform level = slot.Find("Level");
            if (level != null) SetField(so, "levelText", FindText(level, "LevelText")); // [변경] levelRoot 연결 삭제
            so.ApplyModifiedProperties();
            return ui;
        }

        // 부모 아래 칸들을 순서대로
        private HudSlotUI[] LinkSlotsInChildren(Transform parent)
        {
            if (parent == null) return new HudSlotUI[0];

            HudSlotUI[] slots = new HudSlotUI[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                slots[i] = LinkSlot(parent.GetChild(i), "Icon");
            }
            return slots;
        }

        #endregion

        #region 도우미

        private Transform Find(Transform parent, string path)
        {
            Transform found = parent.Find(path);
            if (found == null) Debug.LogError($"[InGameUILinker] '{parent.name}/{path}'를 찾지 못했습니다");
            return found;
        }

        private Image FindImage(Transform parent, string path)
        {
            Transform found = Find(parent, path);
            if (found == null) return null;
            return found.GetComponent<Image>();
        }

        private TMP_Text FindText(Transform parent, string path)
        {
            Transform found = Find(parent, path);
            if (found == null) return null;
            return found.GetComponent<TMP_Text>();
        }

        // 바(Bar)는 fillAmount로 줄어들도록 Filled + 가로 + 왼쪽 기준
        private void SetFilled(Image image)
        {
            if (image == null) return;

            Undo.RecordObject(image, "Set Filled");
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private void SetField(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[InGameUILinker] {so.targetObject.GetType().Name}에 '{fieldName}' 필드가 없습니다");
                return;
            }
            property.objectReferenceValue = value;
        }

        private void SetArray(SerializedObject so, string fieldName, Object[] values)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        #endregion

        #endregion
#endif
    }
}
