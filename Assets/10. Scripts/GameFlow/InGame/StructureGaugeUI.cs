using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ===== 새로 추가 : 파일 전체 (구조물 상호작용 게이지) =====
namespace PROJECT3D
{
    // 구조물 상호작용 게이지 (StructureManager가 Resources에서 만들어서 쓴다. 프리팹에 자기 캔버스 포함)
    //  - 구조물 범위 안에 들어오면 보이고, F키를 누르고 있는 동안 게이지가 찬다
    public class StructureGaugeUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel; // 켜고 끌 묶음 (배경 + 게이지 + 글자)
        [SerializeField] private Image fill;       // Image Type = Filled
        [SerializeField] private TMP_Text label;   // "[F] 회복" 같은 안내

        #region 외부 설정

        public void Show(string structureName, float progress)
        {
            if (panel.activeSelf == false) panel.SetActive(true);
            fill.fillAmount = Mathf.Clamp01(progress);
            label.SetText($"[F] {structureName}");
        }

        public void Hide()
        {
            if (panel.activeSelf) panel.SetActive(false);
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
