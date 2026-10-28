#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class MedicationPropSetup
{
    [MenuItem("Tools/Eco/Create Medication Prop Prefabs")]
    public static void CreatePrefabs()
    {
        string[] scripts = AssetDatabase.FindAssets("MedicationPropSetup t:MonoScript");
        string folder = null;
        foreach (string guid in scripts) {
            string script = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(script) == "MedicationPropSetup.cs") {
                folder = Path.GetDirectoryName(Path.GetDirectoryName(script)).Replace('\\', '/'); break;
            }
        }
        if (folder == null) { Debug.LogError("MedicationPropSetup folder not found."); return; }
        string matFolder = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder(folder, "Materials");
        string prefabFolder = folder + "/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder)) AssetDatabase.CreateFolder(folder, "Prefabs");
        string[] names = {"Carton_Print", "Carton_Side", "Bottle_Label", "White_Plastic"};
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
        foreach (string name in new[]{"Medication_Box_Horizontal", "Medication_Bottle"}) {
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
                        key = part.Contains("Carton_Print") ? "Carton_Print" :
                            part.Contains("Carton_Side") ? "Carton_Side" :
                            part.Contains("Bottle_Label") ? "Bottle_Label" : "White_Plastic";
                    }
                    slots[i] = materials[key];
                }
                renderer.sharedMaterials = slots;
            }
            if (name == "Medication_Box_Horizontal") {
                BoxCollider collider = go.AddComponent<BoxCollider>();
                collider.center = new Vector3(0,.0325f,0);
                collider.size = new Vector3(.115f,.065f,.03f);
            } else {
                CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
                collider.direction = 1; collider.center = new Vector3(0,.046f,0);
                collider.height = .092f; collider.radius = .023f;
            }
            PrefabUtility.SaveAsPrefabAsset(go, prefabFolder + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Medication Props", "Created the horizontal medication box and bottle in Prefabs.", "OK");
    }
    static void SetTexture(string path, bool normal, bool srgb) {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb; importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None; importer.SaveAndReimport();
    }
}
#endif
