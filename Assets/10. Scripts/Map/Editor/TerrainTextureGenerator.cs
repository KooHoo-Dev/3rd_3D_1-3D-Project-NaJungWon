using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PROJECT3D
{
    // 지형 바닥 텍스처(풀, 숲 바닥, 흙, 바위, 큰 얼룩)를 코드로 그려서 PNG로 저장하고,
    // WorldTerrain 셰이더 머티리얼을 만들어 화이트박스 지형(Ground 그룹)에 입힌다.
    // 텍스처는 위아래/좌우 끝이 이어지도록(반복해도 이음매가 안 보이게) 만든다.
    public static class TerrainTextureGenerator
    {
        public const string TextureFolder = "Assets/00. 3D Game/Textures/Terrain";
        public const string MaterialFolder = "Assets/00. 3D Game/Material/Terrain";
        public const string MaterialPath = MaterialFolder + "/M_Terrain.mat";
        private const string ShaderName = "PROJECT3D/WorldTerrain";
        private const string MapRootName = "Map_DragonNest";

        private const int TextureSize = 512;
        private const int MacroSize = 256;

        #region 외부 설정

        [MenuItem("PROJECT3D/Map/지형 텍스처 만들고 입히기")]
        public static void GenerateAndApply()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"{ShaderName} 셰이더를 찾지 못했습니다. WorldTerrain.shader 컴파일 오류를 확인하세요.");
                return;
            }

            CreateFolder(TextureFolder);
            CreateFolder(MaterialFolder);

            // 1. 텍스처 그리기 → PNG 저장
            Texture2D grass = SaveTexture("T_Grass", DrawGrass(), true);
            Texture2D forest = SaveTexture("T_ForestGround", DrawForestGround(), true);
            Texture2D dirt = SaveTexture("T_Dirt", DrawDirt(), true);
            Texture2D rock = SaveTexture("T_Rock", DrawRock(), true);
            Texture2D macro = SaveTexture("T_MacroNoise", DrawMacro(), false);

            // 2. 머티리얼 (이미 있으면 텍스처만 다시 연결)
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetTexture("_GrassTex", grass);
            material.SetTexture("_ForestTex", forest);
            material.SetTexture("_DirtTex", dirt);
            material.SetTexture("_RockTex", rock);
            material.SetTexture("_MacroTex", macro);

            // 이미 있던 머티리얼은 예전 값이 저장돼 있으므로 기본값을 다시 넣는다
            material.SetFloat("_Brightness", 1.15f);
            material.SetFloat("_MacroStrength", 0.2f);
            material.SetFloat("_SlopeBlend", 0.15f);
            material.SetFloat("_SlopeDirtAmount", 0.45f);
            material.SetFloat("_FlatDirtAmount", 0.12f);
            material.SetFloat("_SlopeFadeHeight", 4f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            // 3. 씬의 화이트박스 지형에 입히기
            ApplyToMap(material);
        }

        [MenuItem("PROJECT3D/Map/지형 머티리얼만 다시 입히기")]
        public static void ApplyOnly()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogWarning("지형 머티리얼이 없습니다. '지형 텍스처 만들고 입히기'를 먼저 실행하세요.");
                return;
            }
            ApplyToMap(material);
        }

        #endregion

        #region 내부 설정

        #region 지형에 입히기

        // Map_DragonNest/Ground 아래 모든 지형(바닥, 고원, 언덕, 산비탈)에 머티리얼을 입힌다
        private static void ApplyToMap(Material material)
        {
            GameObject root = GameObject.Find(MapRootName);
            Transform ground = (root != null) ? root.transform.Find("Ground") : null;
            if (ground == null)
            {
                Debug.LogWarning("씬에 Map_DragonNest/Ground가 없습니다. 화이트박스를 먼저 만드세요. (머티리얼은 만들어 두었습니다)");
                return;
            }

            int count = 0;
            foreach (MeshRenderer meshRenderer in ground.GetComponentsInChildren<MeshRenderer>())
            {
                Undo.RecordObject(meshRenderer, "Apply Terrain Material");
                meshRenderer.sharedMaterial = material;
                count++;
            }
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"지형 머티리얼을 {count}개 지형에 입혔습니다.");
        }

        #endregion

        #region 텍스처 그리기

        // 풀 : 진한 초록 ~ 밝은 초록 + 풀잎 점 + 군데군데 마른 풀
        private static Color[] DrawGrass()
        {
            Color dark = new Color(0.32f, 0.52f, 0.18f);
            Color light = new Color(0.52f, 0.74f, 0.28f);
            Color dry = new Color(0.74f, 0.72f, 0.40f);

            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (float)x / TextureSize;
                    float v = (float)y / TextureSize;

                    float tone = Fbm(u, v, 4, 4, 11);       // 넓은 밝기 변화
                    float blade = Fbm(u, v, 64, 2, 12);     // 풀잎 점
                    float dryPatch = Fbm(u, v, 6, 3, 13);   // 마른 풀 얼룩

                    Color color = Color.Lerp(dark, light, tone);
                    color = Color.Lerp(color, dry, Mathf.SmoothStep(0f, 1f, (dryPatch - 0.6f) * 4f) * 0.5f);
                    color *= 0.9f + blade * 0.2f;
                    pixels[y * TextureSize + x] = Opaque(color);
                }
            }
            return pixels;
        }

        // 숲 바닥 : 어두운 이끼 초록 + 갈색 낙엽
        private static Color[] DrawForestGround()
        {
            Color moss = new Color(0.24f, 0.42f, 0.16f);
            Color mossLight = new Color(0.38f, 0.58f, 0.22f);
            Color leaf = new Color(0.60f, 0.45f, 0.24f);

            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (float)x / TextureSize;
                    float v = (float)y / TextureSize;

                    float tone = Fbm(u, v, 4, 4, 21);
                    float leaves = Fbm(u, v, 32, 3, 22);    // 낙엽 덩어리
                    float detail = Fbm(u, v, 96, 2, 23);

                    Color color = Color.Lerp(moss, mossLight, tone);
                    float leafAmount = Mathf.Clamp01((leaves - 0.55f) * 5f);
                    color = Color.Lerp(color, leaf, leafAmount * 0.7f);
                    color *= 0.9f + detail * 0.2f;
                    pixels[y * TextureSize + x] = Opaque(color);
                }
            }
            return pixels;
        }

        // 흙 : 갈색 + 작은 자갈
        private static Color[] DrawDirt()
        {
            // 풀과 섞였을 때 너무 튀지 않게 밝고 따뜻한 흙색
            Color dark = new Color(0.56f, 0.45f, 0.28f);
            Color light = new Color(0.74f, 0.62f, 0.42f);
            Color pebble = new Color(0.78f, 0.75f, 0.68f);

            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (float)x / TextureSize;
                    float v = (float)y / TextureSize;

                    float tone = Fbm(u, v, 4, 5, 31);
                    float pebbles = Fbm(u, v, 48, 2, 32);
                    float grain = Fbm(u, v, 128, 1, 33);

                    Color color = Color.Lerp(dark, light, tone);
                    float pebbleAmount = Mathf.Clamp01((pebbles - 0.68f) * 8f);
                    color = Color.Lerp(color, pebble, pebbleAmount);
                    color *= 0.9f + grain * 0.2f;
                    pixels[y * TextureSize + x] = Opaque(color);
                }
            }
            return pixels;
        }

        // 바위 : 회색 + 갈라진 틈(어두운 선) + 가로 지층 줄무늬
        private static Color[] DrawRock()
        {
            Color dark = new Color(0.48f, 0.46f, 0.43f);
            Color light = new Color(0.76f, 0.73f, 0.68f);
            Color crackColor = new Color(0.30f, 0.28f, 0.26f);

            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (float)x / TextureSize;
                    float v = (float)y / TextureSize;

                    float tone = Fbm(u, v, 3, 5, 41);
                    float crackNoise = Fbm(u, v, 8, 4, 42);
                    float crack = 1f - Mathf.Abs(crackNoise * 2f - 1f);           // 값이 0.5 근처인 곳이 선이 된다
                    float strata = Mathf.Sin((v * 6f + tone * 0.5f) * Mathf.PI * 2f) * 0.5f + 0.5f; // 지층 (6번 반복 → 이어짐)
                    float grain = Fbm(u, v, 128, 1, 43);

                    Color color = Color.Lerp(dark, light, tone * 0.7f + strata * 0.3f);
                    color = Color.Lerp(color, crackColor, Mathf.Clamp01((crack - 0.9f) * 10f) * 0.7f);
                    color *= 0.9f + grain * 0.2f;
                    pixels[y * TextureSize + x] = Opaque(color);
                }
            }
            return pixels;
        }

        // 큰 얼룩 : 흑백 (셰이더에서 밝기 변화, 흙 패치, 숲 경계 흔들기에 쓴다)
        private static Color[] DrawMacro()
        {
            Color[] pixels = new Color[MacroSize * MacroSize];
            for (int y = 0; y < MacroSize; y++)
            {
                for (int x = 0; x < MacroSize; x++)
                {
                    float value = Fbm((float)x / MacroSize, (float)y / MacroSize, 4, 4, 51);
                    pixels[y * MacroSize + x] = new Color(value, value, value, 1f);
                }
            }
            return pixels;
        }

        private static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        #endregion

        #region 이어지는 노이즈

        // 여러 겹 노이즈 (0~1). u, v 는 0~1, period 는 한 장 안에 들어가는 격자 수 (끝이 이어진다)
        private static float Fbm(float u, float v, int period, int octaves, int seed)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += TileNoise(u * period, v * period, period, seed + i * 101) * amplitude;
                total += amplitude;
                amplitude *= 0.5f;
                period *= 2;
            }
            return sum / total;
        }

        // 격자점 값을 부드럽게 섞는 노이즈. 격자 번호를 period 로 나눈 나머지로 써서 끝과 처음이 같다
        private static float TileNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;

            // 부드러운 보간 곡선
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash(Wrap(x0, period), Wrap(y0, period), seed);
            float b = Hash(Wrap(x0 + 1, period), Wrap(y0, period), seed);
            float c = Hash(Wrap(x0, period), Wrap(y0 + 1, period), seed);
            float d = Hash(Wrap(x0 + 1, period), Wrap(y0 + 1, period), seed);

            float bottom = Mathf.Lerp(a, b, fx);
            float top = Mathf.Lerp(c, d, fx);
            return Mathf.Lerp(bottom, top, fy);
        }

        private static int Wrap(int value, int period)
        {
            int result = value % period;
            if (result < 0) result += period;
            return result;
        }

        // 격자점마다 고정된 0~1 값
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        #endregion

        #region 저장

        private static Texture2D SaveTexture(string name, Color[] pixels, bool isColor)
        {
            int size = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            string path = TextureFolder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            // 반복, 밉맵, 색(sRGB) 여부 설정
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.sRGBTexture = isColor;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void CreateFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
            string name = Path.GetFileName(folder);
            CreateFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        #endregion

        #endregion
    }
}
