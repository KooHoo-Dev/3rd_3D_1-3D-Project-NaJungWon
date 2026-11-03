using System.Collections.Generic;
using UnityEngine;

// ===== 새로 추가 : 파일 전체 (피격 시 모델이 빨갛게 물드는 연출) =====
namespace PROJECT3D
{
    // 맞으면 캐릭터 모델 색이 빨갛게 물들었다가 원래대로 돌아온다
    //  - 모델 머티리얼(lilToon)의 메인 색(_Color)을 MaterialPropertyBlock으로만 바꾼다
    //    → 머티리얼 에셋 / 원래 셰이더는 건드리지 않음 (lilToon 외형 그대로)
    //  - 머티리얼이 여러 개인 렌더러도 머티리얼 칸마다 따로 원래 색 → 빨강으로 섞는다
    //  - Player.Init에서 모델을 만든 뒤 Init(모델)로 렌더러를 모아 두고, 맞을 때마다 Play
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color hitColor = new Color(1f, 0.15f, 0.15f, 1f); // 물드는 색
        [SerializeField] private float flashTime = 0.25f;           // 빨강 → 원래 색으로 돌아오는 시간(초, 게임 시간)
        [SerializeField] private string colorProperty = "_Color";   // 바꿀 색 속성 (lilToon 메인 색. URP Lit이면 _BaseColor)

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Color[]> originalColors = new List<Color[]>(); // 렌더러마다 머티리얼 칸별 원래 색
        private MaterialPropertyBlock block;
        private int colorId;
        private float remainTime; // 남은 연출 시간 (0이면 연출 없음)

        #region UnityMethod

        private void Update()
        {
            if (remainTime <= 0f) return;
            if (WorldClock.Instance.IsPaused) return; // 일시정지 중에는 색도 멈춤

            remainTime -= WorldClock.Instance.DeltaTime;
            if (remainTime <= 0f)
            {
                ClearColor();
                return;
            }

            ApplyColor(remainTime / flashTime); // 1(빨강) → 0(원래 색)
        }

        #endregion

        #region 외부 설정

        // 모델 아래의 렌더러와 원래 색을 모아 둔다 (모델을 만든 뒤 한 번)
        public void Init(GameObject model)
        {
            block = new MaterialPropertyBlock();
            colorId = Shader.PropertyToID(colorProperty);
            renderers.Clear();
            originalColors.Clear();

            Renderer[] found = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < found.Length; i++)
            {
                // 모델 몸통만 (파티클 / 트레일 등은 제외)
                if (found[i] is SkinnedMeshRenderer == false && found[i] is MeshRenderer == false) continue;

                Material[] materials = found[i].sharedMaterials;
                Color[] colors = new Color[materials.Length];
                for (int j = 0; j < materials.Length; j++)
                {
                    bool hasColor = materials[j] != null && materials[j].HasProperty(colorId);
                    colors[j] = hasColor ? materials[j].GetColor(colorId) : Color.white;
                }

                renderers.Add(found[i]);
                originalColors.Add(colors);
            }
        }

        // 맞았을 때 : 처음부터 다시 빨갛게
        public void Play()
        {
            if (renderers.Count == 0) return;

            remainTime = flashTime;
            ApplyColor(1f);
        }

        #endregion

        #region 내부 설정

        // amount 1 = hitColor, 0 = 원래 색
        private void ApplyColor(float amount)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;

                Color[] colors = originalColors[i];
                for (int j = 0; j < colors.Length; j++)
                {
                    renderers[i].GetPropertyBlock(block, j);
                    block.SetColor(colorId, Color.Lerp(colors[j], hitColor, amount));
                    renderers[i].SetPropertyBlock(block, j);
                }
            }
        }

        // 연출 끝 : 머티리얼 원래 값으로 (블록을 비운다)
        private void ClearColor()
        {
            remainTime = 0f;
            block.Clear();
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;

                for (int j = 0; j < originalColors[i].Length; j++)
                {
                    renderers[i].SetPropertyBlock(block, j);
                }
            }
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
