// ===== 새로 추가 : 하늘 적용 메뉴 =====
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PROJECT3D
{
    // SkyGradient 셰이더로 하늘 머티리얼을 만들고(없을 때만) 지금 씬의 Skybox로 지정한다
    //  하늘의 태양 / 낮밤은 씬의 Directional Light 방향을 따른다 → 그 빛을 Lighting 창의 Sun Source로도 지정한다
    public static class SkyboxSetup
    {
        private const string ShaderName = "PROJECT3D/SkyGradient";
        private const string MaterialFolder = "Assets/00. 3D Game/Material/Sky";
        private const string MaterialPath = MaterialFolder + "/M_Sky.mat";

        #region 외부 설정

        [MenuItem("PROJECT3D/Map/하늘 적용")]
        public static void Apply()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[SkyboxSetup] {ShaderName} 셰이더를 찾지 못했습니다. SkyGradient.shader 컴파일 오류를 확인하세요.");
                return;
            }

            // 1. 머티리얼 (이미 있으면 그대로 써서 인스펙터에서 바꾼 색이 유지된다)
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                if (AssetDatabase.IsValidFolder(MaterialFolder) == false)
                    AssetDatabase.CreateFolder("Assets/00. 3D Game/Material", "Sky");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            // 2. 씬 하늘 지정
            RenderSettings.skybox = material;

            // 3. 태양 = 씬에서 첫 번째 Directional Light
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type != LightType.Directional) continue;
                RenderSettings.sun = lights[i];
                break;
            }
            if (RenderSettings.sun == null)
                Debug.LogWarning("[SkyboxSetup] 씬에 Directional Light가 없습니다. 하늘은 머티리얼의 '태양이 없을 때 방향'을 씁니다.");

            DynamicGI.UpdateEnvironment(); // 주변광(Ambient Source = Skybox)도 새 하늘 기준으로 다시 계산
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = material;
            Debug.Log($"[SkyboxSetup] 하늘 적용 완료 : {MaterialPath}");
        }

        #endregion
    }
}
// ===== 새로 추가 끝 =====
