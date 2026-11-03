using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ===== 새로 추가 : 파일 전체 (회전 미니맵, 메가봉크 방식) =====
namespace PROJECT3D
{
    // 미니맵 (MiniMap 오브젝트에 붙인다)
    //  - 내 위치는 항상 정가운데 (PlayerIcon은 가운데에 고정해 두기만 한다)
    //  - 카메라가 바라보는 쪽이 항상 위 : 카메라를 돌리면 아이콘 / 동서남북이 같이 돈다
    //  - 회복 / 자석 제단(Structure.Actives)을 내 위치 기준 방향 / 거리로 표시. 범위 밖이면 가장자리에 붙인다
    //  - BS Project는 위에서 찍는 카메라 + RenderTexture였지만, 아이콘만 그리면 돼서 UI 좌표 계산으로 그린다
    //    (카메라 / 레이어가 따로 필요 없고, 범위 밖 제단도 가장자리에 보여줄 수 있다)
    public class MiniMapUI : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private RectTransform iconRoot;          // 아이콘 부모 (마스크 안. 크기의 절반 = 미니맵 반지름)
        [SerializeField] private RectTransform directionRoot;     // 동서남북 묶음 (통째로 돈다)
        [SerializeField] private RectTransform[] directionTexts;  // 동서남북 글자 (글자는 똑바로 세운다)
        [SerializeField] private Image structureIconTemplate;     // 제단 아이콘 원본 (꺼 둔다. 필요한 만큼 복사해서 쓴다)

        [Header("표시")]
        [SerializeField] private float mapRange = 60f;            // 미니맵 가장자리까지의 월드 거리(m)
        [SerializeField] private float edgePadding = 12f;         // 범위 밖 아이콘을 가장자리에서 이만큼 안쪽에 둔다(픽셀)
        [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f);   // 회복 제단
        [SerializeField] private Color magnetColor = new Color(0.8f, 0.4f, 1f); // 자석 제단

        private readonly List<Image> structureIcons = new List<Image>(); // 복사해 둔 제단 아이콘 (재사용)

        #region UnityMethod

        private void LateUpdate()
        {
            Player player = LocalPlayer.Instance;
            Camera mainCamera = Camera.main;
            if (player == null || mainCamera == null) return;

            // 카메라가 바라보는 방향(수평 각도)이 미니맵의 위쪽
            float yaw = mainCamera.transform.eulerAngles.y;
            UpdateDirection(yaw);
            UpdateStructureIcons(player.transform.position, yaw);
        }

        #endregion

        #region 내부 설정

        // 동서남북 묶음을 카메라 각도만큼 돌리고, 글자는 반대로 돌려 똑바로 세운다
        private void UpdateDirection(float yaw)
        {
            directionRoot.localEulerAngles = new Vector3(0f, 0f, yaw);
            for (int i = 0; i < directionTexts.Length; i++)
            {
                directionTexts[i].localEulerAngles = new Vector3(0f, 0f, -yaw);
            }
        }

        // 제단마다 아이콘 하나씩 (남는 아이콘은 끈다)
        private void UpdateStructureIcons(Vector3 center, float yaw)
        {
            float radius = iconRoot.rect.width * 0.5f;
            float pixelPerMeter = radius / mapRange;
            Quaternion toMap = Quaternion.Euler(0f, -yaw, 0f); // 월드 방향 → 카메라 기준 방향

            int count = Structure.Actives.Count;
            for (int i = 0; i < count; i++)
            {
                Structure structure = Structure.Actives[i];
                Image icon = GetIcon(i);

                Vector3 offset = structure.transform.position - center;
                offset.y = 0f;
                Vector3 local = toMap * offset;
                Vector2 position = new Vector2(local.x, local.z) * pixelPerMeter;

                // 범위 밖이면 그 방향의 가장자리에
                float maxDistance = radius - edgePadding;
                if (position.magnitude > maxDistance) position = position.normalized * maxDistance;

                icon.rectTransform.anchoredPosition = position;
                icon.color = GetColor(structure.Def.Type);
                if (icon.gameObject.activeSelf == false) icon.gameObject.SetActive(true);
            }

            for (int i = count; i < structureIcons.Count; i++)
            {
                if (structureIcons[i].gameObject.activeSelf) structureIcons[i].gameObject.SetActive(false);
            }
        }

        // index번 아이콘 (모자라면 원본을 복사해서 늘린다)
        private Image GetIcon(int index)
        {
            while (structureIcons.Count <= index)
            {
                Image icon = Instantiate(structureIconTemplate, iconRoot);
                icon.transform.SetSiblingIndex(structureIconTemplate.transform.GetSiblingIndex()); // 내 위치 아이콘보다 아래에 그리도록
                structureIcons.Add(icon);
            }
            return structureIcons[index];
        }

        private Color GetColor(StructureType type)
        {
            if (type == StructureType.Heal) return healColor;
            if (type == StructureType.Magnet) return magnetColor;
            return Color.white;
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
