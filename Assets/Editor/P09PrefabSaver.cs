using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// P09 Modular Humanoid 데모 씬(플레이 중)이나 하이어라키에서 조립한 캐릭터를 프리팹으로 저장한다.
/// - 데모가 만든 런타임 마테리얼 인스턴스("xxx (Instance)")를 원본 마테리얼 에셋으로 되돌린다.
/// - 꺼져 있는 파츠는 삭제한다(사용 중인 본은 유지).
/// - 데모 전용 스크립트(P09.Modular.Humanoid 네임스페이스)는 제거한다.
/// </summary>
public static class P09PrefabSaver
{
    const string MenuPath = "GameObject/P09/선택한 캐릭터를 프리팹으로 저장";
    const string InstanceSuffix = " (Instance)";
    const string DemoNamespace = "P09.Modular.Humanoid";

    [MenuItem(MenuPath, false, 0)]
    static void Save(MenuCommand command)
    {
        var source = command.context as GameObject ?? Selection.activeGameObject;
        if (source == null)
        {
            EditorUtility.DisplayDialog("P09 프리팹 저장", "하이어라키에서 캐릭터를 선택하세요.", "확인");
            return;
        }

        var path = EditorUtility.SaveFilePanelInProject(
            "캐릭터 프리팹 저장", source.name.Replace("(Clone)", "").Trim(), "prefab", "저장할 위치를 선택하세요.", "Assets");
        if (string.IsNullOrEmpty(path)) return;

        // 원본을 잠깐 꺼둔 상태로 복제해서 복제본의 데모 스크립트가 실행되지 않게 한다.
        var wasActive = source.activeSelf;
        source.SetActive(false);
        var copy = Object.Instantiate(source);
        source.SetActive(wasActive);

        try
        {
            copy.name = Path.GetFileNameWithoutExtension(path);
            copy.transform.SetParent(null, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;

            RemoveDemoScripts(copy);
            RemoveInactiveParts(copy);
            var (restored, created) = FixMaterials(copy, path);

            copy.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(copy, path, out var success);
            if (!success)
            {
                EditorUtility.DisplayDialog("P09 프리팹 저장", "프리팹 저장에 실패했습니다. Console을 확인하세요.", "확인");
                return;
            }

            Debug.Log($"[P09PrefabSaver] 저장 완료: {path} (원본 마테리얼 복원 {restored}개, 새 마테리얼 생성 {created}개)");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }
    }

    [MenuItem(MenuPath, true)]
    static bool ValidateSave() => Selection.activeGameObject != null;

    static void RemoveDemoScripts(GameObject root)
    {
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            var ns = behaviour.GetType().Namespace;
            if (ns != null && ns.StartsWith(DemoNamespace))
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
    }

    static void RemoveInactiveParts(GameObject root)
    {
        // 켜져 있는 SkinnedMeshRenderer가 쓰는 본은 지우면 안 된다.
        var usedBones = new HashSet<Transform>();
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!IsActiveUnder(smr.transform, root.transform)) continue;
            if (smr.rootBone != null) usedBones.Add(smr.rootBone);
            foreach (var bone in smr.bones)
            {
                if (bone != null) usedBones.Add(bone);
            }
        }

        var inactive = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t != root.transform && !t.gameObject.activeSelf)
            .ToList();

        foreach (var t in inactive)
        {
            if (t == null) continue; // 상위 오브젝트와 함께 이미 삭제됨
            if (usedBones.Any(bone => bone != null && bone.IsChildOf(t))) continue;
            Object.DestroyImmediate(t.gameObject);
        }
    }

    static bool IsActiveUnder(Transform t, Transform root)
    {
        for (var current = t; current != null && current != root; current = current.parent)
        {
            if (!current.gameObject.activeSelf) return false;
        }
        return true;
    }

    static (int restored, int created) FixMaterials(GameObject root, string prefabPath)
    {
        var cache = new Dictionary<Material, Material>();
        int restored = 0, created = 0;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            var changed = false;

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || AssetDatabase.Contains(material)) continue;

                if (!cache.TryGetValue(material, out var replacement))
                {
                    replacement = FindMaterialAsset(StripInstanceSuffix(material.name));
                    if (replacement != null)
                    {
                        restored++;
                    }
                    else
                    {
                        replacement = CreateMaterialAsset(material, prefabPath);
                        created++;
                    }
                    cache[material] = replacement;
                }

                materials[i] = replacement;
                changed = true;
            }

            if (changed) renderer.sharedMaterials = materials;
        }

        return (restored, created);
    }

    static string StripInstanceSuffix(string name)
    {
        while (name.EndsWith(InstanceSuffix))
        {
            name = name.Substring(0, name.Length - InstanceSuffix.Length);
        }
        return name.Trim();
    }

    static Material FindMaterialAsset(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"t:Material {name}"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && asset.name == name) return asset;
        }
        return null;
    }

    // 원본을 찾지 못한 마테리얼은 프리팹 옆 "<프리팹이름>_Materials" 폴더에 새 에셋으로 저장한다.
    static Material CreateMaterialAsset(Material source, string prefabPath)
    {
        var directory = Path.GetDirectoryName(prefabPath).Replace('\\', '/');
        var folderName = Path.GetFileNameWithoutExtension(prefabPath) + "_Materials";
        var folder = $"{directory}/{folderName}";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(directory, folderName);
        }

        var asset = new Material(source) { name = StripInstanceSuffix(source.name) };
        var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{asset.name}.mat");
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }
}
