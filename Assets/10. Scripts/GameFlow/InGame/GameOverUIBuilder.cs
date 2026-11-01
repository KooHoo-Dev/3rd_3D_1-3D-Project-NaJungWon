using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
#endif

namespace PROJECT3D
{
    // 패배 결과창 화면을 코드로 만드는 에디터 툴
    // 인게임 씬의 빈 GameObject에 붙이고 컴포넌트 우클릭 → 1. Build UI
    // 화면만 만든다. GameOverPanel은 직접 붙이고 참조를 연결한다
    public class GameOverUIBuilder : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font; // 한글 폰트 (비워두면 TMP 기본 폰트)

#if UNITY_EDITOR
        private const string RootName = "GameOver UI";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        private static readonly Color DimColor = new Color(0.10f, 0.00f, 0.02f, 0.78f);       // 화면 전체 붉은 막
        private static readonly Color EmblemColor = new Color(0.60f, 0.05f, 0.08f, 0.60f);    // 문양 자리
        private static readonly Color BandColor = new Color(0.45f, 0.02f, 0.04f, 0.55f);      // 가운데 가로 띠
        private static readonly Color LineColor = new Color(0.85f, 0.08f, 0.10f, 0.90f);      // 띠 위아래 선
        private static readonly Color ResultTextColor = new Color(0.86f, 0.13f, 0.16f);       // "패 배"
        private static readonly Color LabelColor = new Color(0.75f, 0.65f, 0.62f);            // 기록 이름
        private static readonly Color TextColor = new Color(0.93f, 0.89f, 0.80f);             // 기록 값 / 버튼 글자
        private static readonly Color ButtonColor = new Color(0.12f, 0.15f, 0.19f);
        private static readonly Color ButtonLineColor = new Color(0.72f, 0.60f, 0.36f);       // 버튼 테두리

        [ContextMenu("1. Build UI")]
        private void BuildUI()
        {
            if (GameObject.Find(RootName) != null)
            {
                Debug.LogWarning($"[GameOverUIBuilder] '{RootName}'가 이미 있습니다. 먼저 2. Clear UI를 실행하세요");
                return;
            }

            if (FindAnyObjectByType<EventSystem>() == null)
                Debug.LogWarning("[GameOverUIBuilder] 씬에 EventSystem이 없습니다. 버튼이 눌리지 않습니다");

            Transform canvas = CreateCanvas();
            GameObject resultPanel = CreateResultPanel(canvas);
            CreateEmblem(resultPanel.transform);
            CreateResultBand(resultPanel.transform);
            CreateRecordList(resultPanel.transform);
            CreateLobbyButton(resultPanel.transform);

            resultPanel.SetActive(false);

            Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Build GameOver UI");
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[GameOverUIBuilder] 패배 결과창 생성 완료");
        }

        [ContextMenu("2. Clear UI")]
        private void ClearUI()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log($"[GameOverUIBuilder] 지울 '{RootName}'가 없습니다");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[GameOverUIBuilder] '{RootName}' 삭제");
        }

        #region 영역

        private Transform CreateCanvas()
        {
            GameObject go = new GameObject(RootName, typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // 인게임 HUD보다 위에 그린다

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        // 화면 전체를 덮는 붉은 막 (결과창 전체의 부모. 처음엔 꺼둔다)
        private GameObject CreateResultPanel(Transform canvas)
        {
            GameObject panel = NewUI("ResultPanel", canvas);
            panel.AddComponent<Image>().color = DimColor; // 뒤 게임 화면 클릭도 막아준다
            Stretch(panel.GetComponent<RectTransform>(), 0f);
            return panel;
        }

        // 띠 위의 문양 (스프라이트 자리를 색 블록으로 대신)
        private void CreateEmblem(Transform parent)
        {
            GameObject emblem = NewUI("Emblem", parent);
            emblem.AddComponent<Image>().color = EmblemColor;
            SetCenter(emblem.GetComponent<RectTransform>(), new Vector2(0f, 240f), new Vector2(170f, 170f));
        }

        // 가운데 가로 띠 + "패 배"
        private void CreateResultBand(Transform parent)
        {
            GameObject band = NewUI("ResultBand", parent);
            band.AddComponent<Image>().color = BandColor;
            RectTransform bandRect = band.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, 0.5f);
            bandRect.anchorMax = new Vector2(1f, 0.5f);
            bandRect.pivot = new Vector2(0.5f, 0.5f);
            bandRect.anchoredPosition = new Vector2(0f, 60f);
            bandRect.sizeDelta = new Vector2(0f, 170f);

            CreateBandLine("TopLine", band.transform, 1f);
            CreateBandLine("BottomLine", band.transform, 0f);

            TMP_Text resultText = NewText("ResultText", band.transform, "패 배", 140, TextAlignmentOptions.Center);
            resultText.color = ResultTextColor;
            Stretch(resultText.rectTransform, 0f);
        }

        // 띠의 위(y = 1) / 아래(y = 0) 가장자리에 붙는 얇은 선
        private void CreateBandLine(string name, Transform band, float y)
        {
            GameObject line = NewUI(name, band);
            line.AddComponent<Image>().color = LineColor;
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, y);
            rect.anchorMax = new Vector2(1f, y);
            rect.pivot = new Vector2(0.5f, y);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 3f);
        }

        // 띠 아래 기록 3줄 : 처치 수 / 획득 골드 / 생존 시간
        private void CreateRecordList(Transform parent)
        {
            GameObject list = NewUI("RecordList", parent);
            SetCenter(list.GetComponent<RectTransform>(), new Vector2(0f, -110f), new Vector2(520f, 170f));

            VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateRecordRow("KillRow", list.transform, "처치 수", "0");
            CreateRecordRow("GoldRow", list.transform, "획득 골드", "0");
            CreateRecordRow("TimeRow", list.transform, "생존 시간", "00:00");
        }

        // 한 줄 : 왼쪽 Label / 오른쪽 Value
        private void CreateRecordRow(string name, Transform parent, string label, string value)
        {
            GameObject row = NewUI(name, parent);

            TMP_Text labelText = NewText("Label", row.transform, label, 34, TextAlignmentOptions.Left);
            labelText.color = LabelColor;
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            TMP_Text valueText = NewText("Value", row.transform, value, 34, TextAlignmentOptions.Right);
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0.5f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = Vector2.zero;
        }

        // 맨 아래 "나가기" 버튼 (로비로)
        private void CreateLobbyButton(Transform parent)
        {
            Button button = NewButton("LobbyButton", parent, "나가기", out TMP_Text _);
            SetCenter(button.GetComponent<RectTransform>(), new Vector2(0f, -260f), new Vector2(240f, 56f));

            Outline outline = button.gameObject.AddComponent<Outline>(); // 시안의 금색 테두리
            outline.effectColor = ButtonLineColor;
            outline.effectDistance = new Vector2(2f, -2f);
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
            go.AddComponent<Image>().color = ButtonColor;
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

            labelText = NewText("Label", go.transform, label, 28, TextAlignmentOptions.Center);
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

        // 부모 정중앙 기준으로 위치 / 크기
        private void SetCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        #endregion
#endif
    }
}