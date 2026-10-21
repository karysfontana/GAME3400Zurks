using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaundromatBenchAsset
{
    [InitializeOnLoad]
    public static class BenchMaterialSetup
    {
        static BenchMaterialSetup() { EditorApplication.delayCall += Configure; }

        [MenuItem("Tools/Laundromat Bench/Refresh Materials")]
        public static void Configure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Configure;
                return;
            }
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string shaderName = pipeline == null ? "Standard" : "Universal Render Pipeline/Lit";
            if (pipeline != null && !pipeline.GetType().Name.Contains("Universal"))
            {
                Debug.LogWarning("Laundromat Bench supports Built-in and URP. Please assign a suitable shader for this render pipeline.");
                return;
            }
            Shader shader = Shader.Find(shaderName);
            if (shader == null) return;
            string[] ids = { "6f2e9367f0a27234f52b71e9fe949eaa", "f9781c8ddb45fbe6c242f970ed21ab46", "17ebd4697aa6a5827741f9c1c4f22d82" };
            bool changed = false;
            foreach (string id in ids)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
                if (material == null || material.shader == shader) continue;
                material.shader = shader;
                EditorUtility.SetDirty(material);
                changed = true;
            }
            if (changed) AssetDatabase.SaveAssets();
        }
    }
}
