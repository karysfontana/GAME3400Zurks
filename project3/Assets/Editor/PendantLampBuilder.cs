// Save in Assets/Editor. Open Tools > Pantry > Build Pendant Lamp.
// Dimensions are in metres. Ceiling pivot at Y=0; lamp hangs downward.
// Meshes and materials are saved as assets. No runtime scripts or Light components.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class PendantLampBuilder : EditorWindow
{
    private Vector3 ceilingPosition = new Vector3(0f, 2.4f, 0f);
    private Material metalOverride;
    private string status = "";
    private const int Segments = 64;

    [MenuItem("Tools/Pantry/Build Pendant Lamp")]
    public static void Open()
    {
        PendantLampBuilder window = GetWindow<PendantLampBuilder>("Pendant Lamp");
        window.minSize = new Vector2(380f, 285f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Pendant Lamp - reference dimensions", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Height: 0.627 m | Shade diameter: 0.125 m\n" +
            "Canopy diameter: 0.082 m | Straight cord: 0.400 m\n" +
            "Hollow metal shade, open bottom, visible unlit bulb.", MessageType.Info);
        ceilingPosition = EditorGUILayout.Vector3Field("Ceiling attachment", ceilingPosition);
        metalOverride = (Material)EditorGUILayout.ObjectField("Optional metal material", metalOverride, typeof(Material), false);
        EditorGUILayout.HelpBox("The model hangs DOWN from this position. " +
            "Each click creates a new model and Prefab. No Light components are added.", MessageType.None);
        bool valid = Finite(ceilingPosition.x) && Finite(ceilingPosition.y) && Finite(ceilingPosition.z);
        using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Generate Lamp + Save Prefab", GUILayout.Height(38f))) Build();
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
    private static Vector2 P(float radius, float y) { return new Vector2(radius, y); }

    private void Build()
    {
        GameObject root = null;
        try
        {
            const string baseFolder = "Assets/PendantLampGenerated";
            if (!AssetDatabase.IsValidFolder(baseFolder)) AssetDatabase.CreateFolder("Assets", "PendantLampGenerated");
            string unique = AssetDatabase.GenerateUniqueAssetPath(baseFolder + "/Lamp");
            string guid = AssetDatabase.CreateFolder(baseFolder, System.IO.Path.GetFileName(unique));
            string folder = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(folder)) throw new Exception("Could not create asset folder.");

            Material metal = metalOverride != null ? metalOverride : MaterialAsset(folder, "Dark_Metal", new Color(0.23f, 0.24f, 0.25f), 0.65f, 0.32f);
            Material cord = MaterialAsset(folder, "Black_Cord", new Color(0.045f, 0.043f, 0.040f), 0f, 0.20f);
            Material bulb = MaterialAsset(folder, "Unlit_Bulb_Surface", new Color(0.88f, 0.86f, 0.79f), 0f, 0.35f);
            Material socket = MaterialAsset(folder, "Socket", new Color(0.16f, 0.14f, 0.11f), 0.45f, 0.25f);
            root = new GameObject("Pantry_Pendant_Lamp");
            root.transform.position = ceilingPosition;

            // Profile points are (radius, downward Y). The top is flush to the ceiling.
            Part(root, folder, "01_Ceiling_Canopy", new Vector2[] {
                P(0f,0f), P(.0385f,0f), P(.0405f,-.001f), P(.041f,-.003f),
                P(.041f,-.006f), P(.0405f,-.010f), P(.039f,-.016f),
                P(.0365f,-.023f), P(.0325f,-.030f), P(.027f,-.036f),
                P(.020f,-.041f), P(.012f,-.045f), P(.006f,-.047f), P(0f,-.047f)
            }, metal);
            Part(root, folder, "02_Canopy_Cord_Grip", new Vector2[] {
                P(0f,-.041f), P(.0065f,-.041f), P(.007f,-.044f),
                P(.0063f,-.053f), P(.004f,-.059f), P(.0015f,-.061f), P(0f,-.061f)
            }, metal);
            Part(root, folder, "03_Cord_40cm", new Vector2[] {
                P(0f,-.058f), P(.0013f,-.058f), P(.0013f,-.458f), P(0f,-.458f)
            }, cord);
            Part(root, folder, "04_Lower_Cord_Grip", new Vector2[] {
                P(0f,-.456f), P(.002f,-.456f), P(.003f,-.463f),
                P(.0055f,-.470f), P(.006f,-.477f), P(.010f,-.491f), P(0f,-.491f)
            }, metal);

            // Upper lamp holder: stepped silhouette with small raised collars.
            Part(root, folder, "05_Lamp_Holder", new Vector2[] {
                P(0f,-.480f), P(.009f,-.480f), P(.012f,-.483f),
                P(.017f,-.485f), P(.018f,-.488f), P(.017f,-.491f),
                P(.0135f,-.493f), P(.013f,-.500f), P(.0125f,-.510f),
                P(.014f,-.518f), P(.017f,-.521f), P(.017f,-.524f),
                P(.014f,-.526f), P(0f,-.526f)
            }, metal);

            // Shade outer contour, from top to the open lower lip.
            // A reversed inset contour creates real inside faces and wall thickness.
            Vector2[] outer = new Vector2[] {
                P(.0105f,-.487f), P(.0115f,-.506f), P(.0135f,-.520f),
                P(.018f,-.528f), P(.026f,-.534f), P(.036f,-.539f),
                P(.045f,-.546f), P(.0525f,-.554f), P(.058f,-.564f),
                P(.0613f,-.575f), P(.0625f,-.587f), P(.062f,-.598f),
                P(.0605f,-.610f), P(.0585f,-.622f), P(.059f,-.625f),
                P(.059f,-.627f)
            };
            const float wall = .0012f;
            List<Vector2> shell = new List<Vector2>(outer);
            for (int i = outer.Length - 1; i >= 0; i--)
                shell.Add(P(outer[i].x - wall, outer[i].y));
            shell.Add(outer[0]); // Close only the small top annulus, never the bottom opening.
            Part(root, folder, "06_Hollow_Metal_Shade", shell.ToArray(), metal);

            Part(root, folder, "07_Inside_Bulb_Socket", new Vector2[] {
                P(0f,-.514f), P(.0085f,-.514f), P(.0095f,-.517f),
                P(.0095f,-.544f), P(.008f,-.547f), P(0f,-.547f)
            }, socket);
            Part(root, folder, "08_Bulb_No_Emission", new Vector2[] {
                P(0f,-.539f), P(.0075f,-.539f), P(.008f,-.550f),
                P(.010f,-.557f), P(.014f,-.565f), P(.019f,-.574f),
                P(.0225f,-.583f), P(.0235f,-.591f), P(.0225f,-.599f),
                P(.019f,-.606f), P(.0135f,-.611f), P(.007f,-.614f), P(0f,-.615f)
            }, bulb);

            string prefabPath = folder + "/Pantry_Pendant_Lamp.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new Exception("Could not save the Prefab.");
            AssetDatabase.SaveAssets();
            Undo.RegisterCreatedObjectUndo(root, "Generate Pendant Lamp");
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            status = "Created! Height 62.7 cm, shade diameter 12.5 cm. Prefab: " + prefabPath;
            Debug.Log(status, root);
        }
        catch (Exception ex)
        {
            if (root != null) Undo.RegisterCreatedObjectUndo(root, "Generate Pendant Lamp (partial)");
            status = "Stopped: " + ex.Message;
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Pendant Lamp", status + "\nPlease share the Console error.", "OK");
        }
    }

    private static void Part(GameObject root, string folder, string name, Vector2[] profile, Material material)
    {
        Mesh mesh = Revolve(profile, name);
        AssetDatabase.CreateAsset(mesh, folder + "/" + name + ".asset");
        GameObject part = new GameObject(name);
        part.transform.SetParent(root.transform, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Mesh Revolve(Vector2[] profile, string name)
    {
        int stride = Segments + 1;
        Vector3[] vertices = new Vector3[profile.Length * stride];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        List<int> indices = new List<int>();
        float[] distance = new float[profile.Length];
        for (int i = 1; i < profile.Length; i++)
            distance[i] = distance[i - 1] + Vector2.Distance(profile[i - 1], profile[i]);
        bool closed = (profile[0] - profile[profile.Length - 1]).sqrMagnitude < 1e-12f;
        for (int i = 0; i < profile.Length; i++)
        {
            Vector2 prev = profile[Mathf.Max(i - 1, 0)];
            Vector2 next = profile[Mathf.Min(i + 1, profile.Length - 1)];
            if (closed && (i == 0 || i == profile.Length - 1))
            {
                prev = profile[profile.Length - 2];
                next = profile[1];
            }
            Vector2 tangent = (next - prev).normalized;
            for (int j = 0; j <= Segments; j++)
            {
                float angle = 2f * Mathf.PI * j / Segments;
                float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                int n = i * stride + j;
                vertices[n] = new Vector3(profile[i].x * sin, profile[i].y, profile[i].x * cos);
                normals[n] = new Vector3(-tangent.y * sin, tangent.x, -tangent.y * cos).normalized;
                uv[n] = new Vector2((float)j / Segments, distance[i] / distance[distance.Length - 1]);
                if (i == profile.Length - 1 || j == Segments) continue;
                int a = n, b = n + stride;
                // Outward winding along the descending outside contour; inside reverses automatically.
                if (profile[i].x > 0f) { indices.Add(a); indices.Add(b); indices.Add(a + 1); }
                if (profile[i + 1].x > 0f) { indices.Add(a + 1); indices.Add(b); indices.Add(b + 1); }
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Material MaterialAsset(string folder, string name, Color colour, float metallic, float smoothness)
    {
        string shaderName = "Standard";
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null)
        {
            string type = pipeline.GetType().Name;
            if (type.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) >= 0) shaderName = "Universal Render Pipeline/Lit";
            else if (type.IndexOf("HDRender", StringComparison.OrdinalIgnoreCase) >= 0) shaderName = "HDRP/Lit";
            else throw new Exception("Unsupported custom render pipeline.");
        }
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new Exception("Missing shader: " + shaderName);
        Material material = new Material(shader);
        material.name = name;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
        if (material.HasProperty("_Color")) material.SetColor("_Color", colour);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
        if (material.HasProperty("_EmissiveColor")) material.SetColor("_EmissiveColor", Color.black);
        material.DisableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        AssetDatabase.CreateAsset(material, folder + "/" + name + ".mat");
        return material;
    }
}
#endif
