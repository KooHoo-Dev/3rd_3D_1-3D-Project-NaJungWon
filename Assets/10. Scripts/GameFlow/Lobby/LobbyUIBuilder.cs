using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
#endif

namespace PROJECT3D
{
    // 로비 화면 임시 GUI를 코드로 만드는 에디터 툴 (BS Project 로비 배치)
    // 빈 GameObject에 붙이고 컴포넌트 우클릭 → 1. Build UI
    // 화면을 만든 뒤 LobbyManager / 패널 / 칸 스크립트를 붙이고 참조까지 연결한다
    public class LobbyUIBuilder : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font; // 한글 폰트 (비워두면 TMP 기본 폰트)

#if UNITY_EDITOR
        private const string RootName = "Lobby UI";
        private const int PartySlotCount = 4;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        private static readonly Color BackgroundColor = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color FrameColor = new Color(0.16f, 0.14f, 0.12f);
        private static readonly Color PanelColor = new Color(0.20f, 0.17f, 0.15f, 0.98f);
        private static readonly Color SlotColor = new Color(0.03f, 0.03f, 0.03f);
        private static readonly Color OutlineColor = new Color(0.55f, 0.45f, 0.35f);
        private static readonly Color ButtonColor = new Color(0.30f, 0.26f, 0.22f);
        private static readonly Color TextColor = new Color(0.93f, 0.89f, 0.80f);
        private static readonly Color GoldColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color SelectedColor = new Color(1f, 0.80f, 0.25f);

        // 만든 것들을 연결 단계에서 쓰기 위해 잠깐 들고 있는다
        private TMP_Text goldText;
        private PartySlotUI[] partySlots;
        private Button shopButton;
        private Button optionButton;
        private Button startButton;
        private Button backButton;
        private CharacterSelectPanel characterSelectPanel;
        private ShopPanel shopPanel;
        private GameObject optionPanel;
        private Button optionCloseButton;

        #region 외부 설정

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[LobbyUIBuilder] '{RootName}'가 이미 있습니다. 먼저 2. Clear UI를 실행하세요");
                return;
            }

            CreateEventSystemIfMissing();

            Transform canvas = CreateCanvas();
            CreateBackground(canvas);
            CreateGold(canvas);
            CreatePartySlots(canvas);
            CreateBottomButtons(canvas);
            CreateCharacterPopup(canvas);
            CreateShopPopup(canvas);
            CreateOptionPopup(canvas);

            LinkLobbyManager(canvas.gameObject);

            // 창은 처음에 닫혀 있다
            characterSelectPanel.gameObject.SetActive(false);
            shopPanel.gameObject.SetActive(false);
            optionPanel.SetActive(false);

            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Build Lobby UI");
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[LobbyUIBuilder] 로비 UI 생성 완료");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log($"[LobbyUIBuilder] 지울 '{RootName}'가 없습니다");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[LobbyUIBuilder] '{RootName}' 삭제");
        }

        #endregion

        #region 내부 설정

        #region 영역 - 기본 화면

        private Transform CreateCanvas()
        {
            GameObject go = new GameObject(RootName, typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        private void CreateBackground(Transform canvas)
        {
            GameObject bg = NewUI("Background", canvas);
            bg.AddComponent<Image>().color = BackgroundColor;
            Stretch(bg.GetComponent<RectTransform>(), 0f);

            // 안쪽 테두리 판
            GameObject frame = NewUI("Frame", bg.transform);
            frame.AddComponent<Image>().color = FrameColor;
            frame.AddComponent<Outline>().effectColor = OutlineColor;
            Stretch(frame.GetComponent<RectTransform>(), 30f);
        }

        // 우측 상단 골드
        private void CreateGold(Transform canvas)
        {
            goldText = NewText("GoldText", canvas, "골드: 0", 44, TextAlignmentOptions.Right);
            goldText.color = GoldColor;
            RectTransform rect = goldText.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-80f, -60f);
            rect.sizeDelta = new Vector2(500f, 70f);
        }

        // 가운데 플레이어 칸 4개
        private void CreatePartySlots(Transform canvas)
        {
            GameObject group = NewUI("PartySlots", canvas);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 50f);
            rect.sizeDelta = new Vector2(1600f, 600f);

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 80f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            partySlots = new PartySlotUI[PartySlotCount];
            for (int i = 0; i < PartySlotCount; i++)
            {
                partySlots[i] = CreatePartySlot(group.transform, i);
            }
        }

        private PartySlotUI CreatePartySlot(Transform parent, int index)
        {
            GameObject slot = NewUI($"PartySlot_{index}", parent);
            Image bg = slot.AddComponent<Image>();
            bg.color = SlotColor;
            slot.AddComponent<Outline>().effectColor = OutlineColor;
            Button button = slot.AddComponent<Button>();
            button.targetGraphic = bg;

            // 빈 칸 : +
            GameObject emptyRoot = NewUI("EmptyRoot", slot.transform);
            Stretch(emptyRoot.GetComponent<RectTransform>(), 0f);
            TMP_Text plus = NewText("PlusText", emptyRoot.transform, "+", 140, TextAlignmentOptions.Center);
            plus.color = new Color(0.5f, 0.5f, 0.5f);
            Stretch(plus.rectTransform, 0f);

            // 플레이어가 있을 때 : 플레이어 이름 / 캐릭터 그림 / 캐릭터 이름
            GameObject characterRoot = NewUI("CharacterRoot", slot.transform);
            Stretch(characterRoot.GetComponent<RectTransform>(), 0f);

            TMP_Text playerName = NewText("PlayerNameText", characterRoot.transform, "Player", 30, TextAlignmentOptions.Center);
            SetTopStrip(playerName.rectTransform, -20f, 50f);

            GameObject iconGo = NewUI("CharacterIcon", characterRoot.transform);
            Image icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 20f);
            iconRect.sizeDelta = new Vector2(300f, 300f);

            TMP_Text unitName = NewText("UnitNameText", characterRoot.transform, "캐릭터", 36, TextAlignmentOptions.Center);
            SetBottomStrip(unitName.rectTransform, 30f, 60f);

            characterRoot.SetActive(false);

            // 연결
            PartySlotUI ui = slot.AddComponent<PartySlotUI>();
            SerializedObject so = new SerializedObject(ui);
            SetField(so, "button", button);
            SetField(so, "emptyRoot", emptyRoot);
            SetField(so, "characterRoot", characterRoot);
            SetField(so, "characterIcon", icon);
            SetField(so, "unitNameText", unitName);
            SetField(so, "playerNameText", playerName);
            so.ApplyModifiedPropertiesWithoutUndo();
            return ui;
        }

        // 하단 버튼 : 좌측 [능력치 업그레이드] [옵션], 우측 [시작하기] [나가기]
        private void CreateBottomButtons(Transform canvas)
        {
            shopButton = NewButton("ShopButton", canvas, "능력치 업그레이드", out TMP_Text _);
            SetCorner(shopButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(140f, 70f), new Vector2(340f, 70f));

            optionButton = NewButton("OptionButton", canvas, "옵션", out TMP_Text _);
            SetCorner(optionButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(510f, 70f), new Vector2(180f, 70f));

            startButton = NewButton("StartButton", canvas, "시작하기", out TMP_Text _);
            SetCorner(startButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-350f, 70f), new Vector2(240f, 70f));

            backButton = NewButton("BackButton", canvas, "나가기", out TMP_Text _);
            SetCorner(backButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-90f, 70f), new Vector2(220f, 70f));
        }

        #endregion

        #region 영역 - 창 (캐릭터 선택 / 상점 / 옵션)

        private void CreateCharacterPopup(Transform canvas)
        {
            GameObject window = CreateWindow(canvas, "CharacterPopup", "캐릭터 선택", new Vector2(1000f, 700f), out Button closeButton);
            Transform grid = CreateGrid(window.transform, new Vector2(170f, 200f), 130f);

            TMP_Text info = NewText("InfoText", window.transform, "", 30, TextAlignmentOptions.Center);
            SetBottomStrip(info.rectTransform, 25f, 90f);

            CharacterSlotUI template = CreateCharacterSlotTemplate(window.transform);

            characterSelectPanel = window.AddComponent<CharacterSelectPanel>();
            SerializedObject so = new SerializedObject(characterSelectPanel);
            SetField(so, "slotPrefab", template);
            SetField(so, "slotParent", grid);
            SetField(so, "infoText", info);
            SetField(so, "closeButton", closeButton);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 캐릭터 칸 견본 (꺼둔 채로 두고 CharacterSelectPanel이 복사해서 쓴다)
        private CharacterSlotUI CreateCharacterSlotTemplate(Transform parent)
        {
            GameObject slot = NewUI("CharacterSlotTemplate", parent);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 200f);
            Image bg = slot.AddComponent<Image>();
            bg.color = OutlineColor;
            Button button = slot.AddComponent<Button>();
            button.targetGraphic = bg;

            // 선택 표시 : 노란 테두리 (안쪽 판보다 먼저 만들어서 뒤에 깔린다)
            GameObject selectedMark = NewUI("SelectedMark", slot.transform);
            Image markImage = selectedMark.AddComponent<Image>();
            markImage.color = SelectedColor;
            markImage.raycastTarget = false;
            Stretch(selectedMark.GetComponent<RectTransform>(), 0f);

            GameObject inner = NewUI("Inner", slot.transform);
            Image innerImage = inner.AddComponent<Image>();
            innerImage.color = SlotColor;
            innerImage.raycastTarget = false;
            Stretch(inner.GetComponent<RectTransform>(), 6f);

            Image icon = NewIcon("Icon", slot.transform, new Vector2(0f, -15f), new Vector2(130f, 130f));

            TMP_Text nameText = NewText("NameText", slot.transform, "이름", 26, TextAlignmentOptions.Center);
            SetBottomStrip(nameText.rectTransform, 10f, 40f);

            CharacterSlotUI ui = slot.AddComponent<CharacterSlotUI>();
            SerializedObject so = new SerializedObject(ui);
            SetField(so, "button", button);
            SetField(so, "icon", icon);
            SetField(so, "nameText", nameText);
            SetField(so, "selectedMark", selectedMark);
            so.ApplyModifiedPropertiesWithoutUndo();

            slot.SetActive(false);
            return ui;
        }

        private void CreateShopPopup(Transform canvas)
        {
            GameObject window = CreateWindow(canvas, "ShopPopup", "능력치 업그레이드", new Vector2(1100f, 860f), out Button closeButton);
            Transform grid = CreateGrid(window.transform, new Vector2(180f, 215f), 30f);

            ShopSlotUI template = CreateShopSlotTemplate(window.transform);

            shopPanel = window.AddComponent<ShopPanel>();
            SerializedObject so = new SerializedObject(shopPanel);
            SetField(so, "slotPrefab", template);
            SetField(so, "slotParent", grid);
            SetField(so, "closeButton", closeButton);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 상점 칸 견본 : 아이콘 / 이름 / 단계 / 수치 / 비용, 칸 전체가 구매 버튼
        private ShopSlotUI CreateShopSlotTemplate(Transform parent)
        {
            GameObject slot = NewUI("ShopSlotTemplate", parent);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 215f);
            Image bg = slot.AddComponent<Image>();
            bg.color = SlotColor;
            slot.AddComponent<Outline>().effectColor = OutlineColor;
            Button buyButton = slot.AddComponent<Button>();
            buyButton.targetGraphic = bg;

            Image icon = NewIcon("Icon", slot.transform, new Vector2(0f, -12f), new Vector2(80f, 80f));

            TMP_Text nameText = NewText("NameText", slot.transform, "스탯", 24, TextAlignmentOptions.Center);
            SetTopStrip(nameText.rectTransform, -96f, 32f);

            TMP_Text levelText = NewText("LevelText", slot.transform, "0 / 5", 22, TextAlignmentOptions.Center);
            SetTopStrip(levelText.rectTransform, -128f, 28f);

            TMP_Text valueText = NewText("ValueText", slot.transform, "0 → 10", 22, TextAlignmentOptions.Center);
            SetTopStrip(valueText.rectTransform, -156f, 28f);

            TMP_Text costText = NewText("CostText", slot.transform, "100", 24, TextAlignmentOptions.Center);
            costText.color = GoldColor;
            SetBottomStrip(costText.rectTransform, 8f, 32f);

            ShopSlotUI ui = slot.AddComponent<ShopSlotUI>();
            SerializedObject so = new SerializedObject(ui);
            SetField(so, "buyButton", buyButton);
            SetField(so, "icon", icon);
            SetField(so, "nameText", nameText);
            SetField(so, "levelText", levelText);
            SetField(so, "valueText", valueText);
            SetField(so, "costText", costText);
            so.ApplyModifiedPropertiesWithoutUndo();

            slot.SetActive(false);
            return ui;
        }

        private void CreateOptionPopup(Transform canvas)
        {
            optionPanel = CreateWindow(canvas, "OptionPopup", "옵션", new Vector2(700f, 450f), out optionCloseButton);

            TMP_Text body = NewText("BodyText", optionPanel.transform, "준비 중입니다", 32, TextAlignmentOptions.Center);
            RectTransform rect = body.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(600f, 120f);
        }

        // 가운데 창 + 제목 + 우측 상단 X 버튼
        private GameObject CreateWindow(Transform canvas, string name, string title, Vector2 size, out Button closeButton)
        {
            GameObject window = NewPanel(name, canvas, size);
            window.AddComponent<Outline>().effectColor = OutlineColor;
            RectTransform rect = window.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 30f);

            TMP_Text titleText = NewText("TitleText", window.transform, title, 44, TextAlignmentOptions.Center);
            SetTopStrip(titleText.rectTransform, -20f, 70f);

            closeButton = NewButton("CloseButton", window.transform, "X", out TMP_Text _);
            SetCorner(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-15f, -15f), new Vector2(70f, 70f));

            return window;
        }

        // 창 안의 칸 배치 영역 (제목 아래 ~ bottomSpace 위)
        private Transform CreateGrid(Transform window, Vector2 cellSize, float bottomSpace)
        {
            GameObject go = NewUI("Grid", window);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(30f, bottomSpace);
            rect.offsetMax = new Vector2(-30f, -110f);

            GridLayoutGroup grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(16f, 16f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            return go.transform;
        }

        #endregion

        #region 연결

        private void LinkLobbyManager(GameObject root)
        {
            LobbyManager manager = root.AddComponent<LobbyManager>();
            SerializedObject so = new SerializedObject(manager);
            SetArray(so, "partySlots", partySlots);
            SetField(so, "characterSelectPanel", characterSelectPanel);
            SetField(so, "shopPanel", shopPanel);
            SetField(so, "optionPanel", optionPanel);
            SetField(so, "optionCloseButton", optionCloseButton);
            SetField(so, "goldText", goldText);
            SetField(so, "shopButton", shopButton);
            SetField(so, "optionButton", optionButton);
            SetField(so, "startButton", startButton);
            SetField(so, "backButton", backButton);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetField(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[LobbyUIBuilder] {so.targetObject.GetType().Name}에 '{fieldName}' 필드가 없습니다");
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

        private void CreateEventSystemIfMissing()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Debug.LogWarning("[LobbyUIBuilder] EventSystem이 없어서 새로 만들었습니다");
        }

        #endregion

        #region 도우미

        private GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private GameObject NewPanel(string name, Transform parent, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            go.AddComponent<Image>().color = PanelColor;
            go.GetComponent<RectTransform>().sizeDelta = size;
            return go;
        }

        private TMP_Text NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            GameObject go = NewUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = TextColor;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button NewButton(string name, Transform parent, string label, out TMP_Text labelText)
        {
            GameObject go = NewUI(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = ButtonColor;
            go.AddComponent<Outline>().effectColor = OutlineColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = NewText("Label", go.transform, label, 32, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);
            return button;
        }

        // 부모 상단 가운데에 붙는 아이콘 (스프라이트 자리)
        private Image NewIcon(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = NewUI(name, parent);
            Image image = go.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        // 부모 상단에 가로로 꽉 찬 띠
        private void SetTopStrip(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(0f, height);
        }

        // 부모 하단에 가로로 꽉 찬 띠
        private void SetBottomStrip(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(0f, height);
        }

        // 화면 모서리에 붙이기 (corner = 앵커이자 피벗. 예: (1,0) = 우측 하단)
        private void SetCorner(RectTransform rect, Vector2 corner, Vector2 position, Vector2 size)
        {
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        #endregion

        #endregion
#endif
    }
}
