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
    // 타이틀 화면 임시 GUI를 코드로 만드는 에디터 툴
    // 빈 GameObject에 붙이고 컴포넌트 우클릭 → 1. Build UI
    // 화면을 만든 뒤 TitleManager를 붙이고 버튼까지 연결한다
    public class TitleUIBuilder : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font; // 한글 폰트 (비워두면 TMP 기본 폰트)

#if UNITY_EDITOR
        private const string RootName = "Title UI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        private static readonly Color BackgroundColor = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color PanelColor = new Color(0.16f, 0.14f, 0.12f, 0.97f);
        private static readonly Color ButtonColor = new Color(0.30f, 0.26f, 0.22f);
        private static readonly Color TextColor = new Color(0.93f, 0.89f, 0.80f);

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[TitleUIBuilder] '{RootName}'가 이미 있습니다. 먼저 2. Clear UI를 실행하세요");
                return;
            }

            CreateEventSystemIfMissing();

            Transform canvas = CreateCanvas();
            CreateBackground(canvas);
            CreateTitle(canvas);
            Button[] buttons = CreateMenuButtons(canvas);
            GameObject settingsPanel = CreateSettingsPanel(canvas, out Button settingsCloseButton);

            LinkTitleManager(canvas.gameObject, buttons, settingsPanel, settingsCloseButton);

            settingsPanel.SetActive(false);

            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Build Title UI");
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[TitleUIBuilder] 타이틀 UI 생성 완료");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log($"[TitleUIBuilder] 지울 '{RootName}'가 없습니다");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[TitleUIBuilder] '{RootName}' 삭제");
        }

        #region 영역

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
        }

        private void CreateTitle(Transform canvas)
        {
            TMP_Text title = NewText("TitleText", canvas, "PROJECT 3D", 120, TextAlignmentOptions.Center);
            RectTransform rect = title.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -200f);
            rect.sizeDelta = new Vector2(1200f, 160f);
        }

        // 0 : 방 생성, 1 : 방 참가, 2 : 설정, 3 : 나가기
        private Button[] CreateMenuButtons(Transform canvas)
        {
            GameObject group = NewUI("MenuButtons", canvas);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(420f, 420f);

            VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            string[] labels = { "방 생성", "방 참가", "설정", "나가기" };
            string[] names = { "CreateRoomButton", "JoinRoomButton", "SettingsButton", "ExitButton" };
            Button[] buttons = new Button[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = NewButton(names[i], group.transform, labels[i], out TMP_Text _);
            }

            return buttons;
        }

        private GameObject CreateSettingsPanel(Transform canvas, out Button closeButton)
        {
            // 뒤를 가리는 반투명 막
            GameObject dim = NewUI("SettingsPanel", canvas);
            dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            Stretch(dim.GetComponent<RectTransform>(), 0f);

            GameObject window = NewPanel("Window", dim.transform, new Vector2(700f, 450f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;

            TMP_Text title = NewText("TitleText", window.transform, "설정", 48, TextAlignmentOptions.Center);
            SetTopStrip(title.rectTransform, -30f, 70f);

            TMP_Text body = NewText("BodyText", window.transform, "준비 중입니다", 32, TextAlignmentOptions.Center);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(600f, 120f);

            closeButton = NewButton("CloseButton", window.transform, "닫기", out TMP_Text _);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 30f);
            closeRect.sizeDelta = new Vector2(220f, 70f);

            return dim;
        }

        #endregion

        #region 연결

        private void LinkTitleManager(GameObject root, Button[] buttons, GameObject settingsPanel, Button settingsCloseButton)
        {
            TitleManager manager = root.AddComponent<TitleManager>();
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("createRoomButton").objectReferenceValue = buttons[0];
            so.FindProperty("joinRoomButton").objectReferenceValue = buttons[1];
            so.FindProperty("settingsButton").objectReferenceValue = buttons[2];
            so.FindProperty("exitButton").objectReferenceValue = buttons[3];
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("settingsCloseButton").objectReferenceValue = settingsCloseButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 타이틀 씬에는 EventSystem이 없어서 버튼이 안 눌리므로 없으면 만든다 (새 Input System용 모듈)
        private void CreateEventSystemIfMissing()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            Debug.LogWarning("[TitleUIBuilder] EventSystem이 없어서 새로 만들었습니다");
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
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = NewText("Label", go.transform, label, 36, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 0f);
            return button;
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

        #endregion
#endif
    }
}
