using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// ===== 새로 추가 : 파일 전체 (구조물 부모) =====
namespace PROJECT3D
{
    // 구조물의 부모 클래스 (회복 / 자석 / 보스)
    //  - 나타날 때 : 바닥 아래에서 위로 올라온다 (몹 출현 연출처럼 DoTween)
    //  - 다 올라오면 상호작용 가능 → StructureManager가 F키 게이지가 다 차면 Activate
    //  - Activate : 효과(자식이 재정의) → 다시 바닥 아래로 내려가며 풀로 돌아간다
    public abstract class Structure : MonoBehaviour, IObjectPoolItem
    {
        // 지금 상호작용할 수 있는(다 올라온) 구조물 목록
        public static readonly List<Structure> Actives = new List<Structure>();

        [Header("상호작용")]
        [SerializeField] private float interactRange = 3f;   // 새로 추가 : 플레이어가 이 거리(수평) 안에 들어오면 F키 상호작용 가능
        public float InteractRange => interactRange;
        [SerializeField] private bool drawRangeGizmo = true; // 새로 추가 : 씬 뷰에 상호작용 범위 표시

        [Header("출현 / 사라짐 연출")]
        [SerializeField] private float hideDepth = 3f;       // 숨어 있을 때 서 있는 자리보다 이만큼 아래
        [SerializeField] private float riseDuration = 1f;    // 올라오는 시간
        [SerializeField] private float sinkDuration = 0.8f;  // 내려가는 시간

        public string Key { get; set; }
        public GameObject GameObject => gameObject;

        public StructureDef Def { get; private set; }
        public bool CanInteract { get; private set; } // 다 올라왔고 아직 안 쓴 상태

        private float standY; // 다 올라왔을 때 높이

        #region UnityMethod

        private void OnDisable()
        {
            transform.DOKill(); // 연출 중에 꺼져도 콜백이 남지 않게
            CanInteract = false;
            Actives.Remove(this);
        }

        // 새로 추가 : 상호작용 범위 (수평 거리라 바닥 원으로. 상호작용 가능 = 초록, 올라오는 중 / 사라지는 중 = 노랑)
        private void OnDrawGizmos()
        {
            if (drawRangeGizmo == false || interactRange <= 0f) return;

            Gizmos.color = (Application.isPlaying && CanInteract == false) ? Color.yellow : Color.green;
            HitGizmo.DrawCircle(transform.position, interactRange);
        }

        #endregion

        #region 외부 설정

        #region 연출

        // 풀에서 꺼내 켠 뒤 StructureManager가 호출 (standPosition = 다 올라왔을 때 위치)
        public void Spawn(StructureDef def, Vector3 standPosition)
        {
            Def = def;
            standY = standPosition.y;
            CanInteract = false;

            transform.DOKill();
            transform.position = standPosition + Vector3.down * hideDepth;
            transform.DOMoveY(standY, riseDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishRise);
        }

        // 바닥 아래로 내려가며 사라진다 (다 내려가면 풀로)
        public void Hide()
        {
            CanInteract = false;
            Actives.Remove(this);

            transform.DOKill();
            transform.DOMoveY(standY - hideDepth, sinkDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(ReturnToPool);
        }

        #endregion

        #region 상호작용

        // 게이지가 다 찼을 때 (한 번만)
        public void Activate(Player player)
        {
            if (CanInteract == false) return;

            ApplyEffect(player);
            Hide();
        }

        // 구조물 효과. 자식이 반드시 재정의
        protected abstract void ApplyEffect(Player player);

        #endregion

        #region 풀

        public void ReturnToPool()
        {
            // 풀 없이 씬에 직접 놓은 구조물이라면 그냥 끈다
            if (ObjectPoolManager.Instance == null || string.IsNullOrEmpty(Key))
            {
                gameObject.SetActive(false);
                return;
            }

            ObjectPoolManager.Instance.ReturnToPool(this);
        }

        #endregion

        #endregion

        #region 내부 설정

        private void FinishRise()
        {
            CanInteract = true;
            Actives.Add(this);
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
