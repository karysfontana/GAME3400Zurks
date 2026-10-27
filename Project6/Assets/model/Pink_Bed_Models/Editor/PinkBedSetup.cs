#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class PinkBedSetup
{
    [MenuItem("Tools/Eco/Create Pink Bed Prefabs")]
    public static void CreatePrefabs()
    {
        string[] scripts = AssetDatabase.FindAssets("PinkBedSetup t:MonoScript");
        string folder = null;
        foreach (string guid in scripts) {
            string script = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(script) == "PinkBedSetup.cs") {
                folder = Path.GetDirectoryName(Path.GetDirectoryName(script)).Replace('\\', '/'); break;
            }
        }
        if (folder == null) { Debug.LogError("PinkBedSetup folder not found."); return; }
        string matFolder = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder(folder, "Materials");
        string prefabFolder = folder + "/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder)) AssetDatabase.CreateFolder(folder, "Prefabs");
        string[] names = {"BlushFabric", "IvoryFabric", "WarmWood", "BlushQuilt"};
        var materials = new System.Collections.Generic.Dictionary<string,Material>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        foreach (string name in names) {
            string basePath = folder + "/Textures/" + name;
            SetTexture(basePath + "_BaseColor.png", false, true);
            SetTexture(basePath + "_Normal.png", true, false);
            SetTexture(basePath + "_MetallicSmoothness.png", false, false);
            string assetPath = matFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, assetPath); }
            mat.shader = shader;
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_BaseColor.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_Normal.png");
            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "_MetallicSmoothness.png");
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            mat.SetTexture("_BumpMap", normal); mat.SetFloat("_BumpScale", 0.4f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.EnableKeyword("_METALLICGLOSSMAP");
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);
            if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 1f);
            if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 0f);
            EditorUtility.SetDirty(mat); materials[name] = mat;
        }
        foreach (string name in new[]{"Bed_Neat", "Bed_Messy"}) {
            string path = folder + "/" + name + ".obj";
            ModelImporter imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp != null) {
                imp.globalScale = 1f; imp.importNormals = ModelImporterNormals.Import;
                imp.importTangents = ModelImporterTangents.CalculateMikk;
                imp.SaveAndReimport();
            }
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) { Debug.LogError("Cannot load " + path); continue; }
            GameObject go = Object.Instantiate(source); go.name = name;
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true)) {
                Material[] slots = renderer.sharedMaterials;
                for (int i=0; i<slots.Length; i++) {
                    string oldName = slots[i] == null ? "" : slots[i].name;
                    string key = null;
                    foreach (string candidate in names) if (oldName.Contains(candidate)) key = candidate;
                    if (key == null) {
                        string part = renderer.gameObject.name;
                        key = part.Contains("Quilt") ? "BlushQuilt" : part.Contains("Pink") ? "BlushFabric" :
                            (part.Contains("Ivory") || part.Contains("Mattress_and")) ? "IvoryFabric" : "WarmWood";
                    }
                    slots[i] = materials[key];
                }
                renderer.sharedMaterials = slots;
            }
            BoxCollider body = go.AddComponent<BoxCollider>();
            body.center = new Vector3(0,.32f,0); body.size = new Vector3(1.96f,.64f,2.10f);
            GameObject head = new GameObject("Headboard_Collider"); head.transform.SetParent(go.transform, false);
            BoxCollider back = head.AddComponent<BoxCollider>();
            back.center = new Vector3(0,1.05f,1.08f); back.size = new Vector3(2.1f,.90f,.18f);
            PrefabUtility.SaveAsPrefabAsset(go, prefabFolder + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Pink Beds", "Created Bed_Neat and Bed_Messy in the Prefabs folder.", "OK");
    }
    static void SetTexture(string path, bool normal, bool srgb) {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb; importer.wrapMode = TextureWrapMode.Repeat;
        importer.maxTextureSize = 1024; importer.SaveAndReimport();
    }
}
#endif
