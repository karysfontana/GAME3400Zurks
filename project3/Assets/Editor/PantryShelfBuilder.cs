// Place this file in Assets/Editor, then choose Tools > Pantry > Build Shelf.
// Editor-only generator. Generated objects need no scripts at runtime.
// Proportions are estimated from the supplied drawing, not measured dimensions.
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class PantryShelfBuilder : EditorWindow
{
    private float width = 2.1f;
    private float height = 2.4f;
    private float depth = 0.42f;
    private bool colliders = true;
    private Material frameMaterial;
    private Material shelfMaterial;
    private Vector3 position = Vector3.zero;
    private string status = "";

    // Bottom to top. The center bay omits the highest shelf in the drawing.
    private static readonly float[] ShelfHeights = { 0.055f, 0.26f, 0.39f, 0.54f, 0.66f, 0.79f, 0.90f };

    [MenuItem("Tools/Pantry/Build Shelf")]
    public static void Open()
    {
        PantryShelfBuilder window = GetWindow<PantryShelfBuilder>("Pantry Shelf");
        window.minSize = new Vector2(370, 380);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Pantry Shelf Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates a complete, editable three-bay shelf and saves a Prefab. " +
            "Defaults follow the reference drawing. Units are metres. No ProBuilder needed.", MessageType.Info);
        width = EditorGUILayout.FloatField("Overall width (X)", width);
        height = EditorGUILayout.FloatField("Overall height (Y)", height);
        depth = EditorGUILayout.FloatField("Overall depth (Z)", depth);
        position = EditorGUILayout.Vector3Field("Floor position", position);
        colliders = EditorGUILayout.Toggle("Add box colliders", colliders);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Optional materials (leave empty for defaults)", EditorStyles.boldLabel);
        frameMaterial = (Material)EditorGUILayout.ObjectField("Frame", frameMaterial, typeof(Material), false);
        shelfMaterial = (Material)EditorGUILayout.ObjectField("Shelves", shelfMaterial, typeof(Material), false);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Each click creates a NEW shelf and a separate asset folder. " +
            "Pivot: floor centre. Front: negative Z. Save your scene after placing the shelf.", MessageType.None);
        bool valid = IsValid();
        if (!valid)
            EditorGUILayout.HelpBox("Use width/height 0.5-20 and depth 0.15-5. Position must be finite.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Generate Shelf + Save Prefab", GUILayout.Height(36)))
                Generate();
        }
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }

    private bool IsValid()
    {
        return Finite(width) && width >= 0.5f && width <= 20f &&
            Finite(height) && height >= 0.5f && height <= 20f &&
            Finite(depth) && depth >= 0.15f && depth <= 5f &&
            Finite(position.x) && Finite(position.y) && Finite(position.z);
    }

    private void Generate()
    {
        if (!IsValid() || EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject root = null;
        string folder = null;
        try
        {
            const string parentFolder = "Assets/PantryShelfGenerated";
            if (!AssetDatabase.IsValidFolder(parentFolder))
                AssetDatabase.CreateFolder("Assets", "PantryShelfGenerated");
            string requestedFolder = AssetDatabase.GenerateUniqueAssetPath(parentFolder + "/Shelf");
            string guid = AssetDatabase.CreateFolder(parentFolder, System.IO.Path.GetFileName(requestedFolder));
            folder = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(folder)) throw new Exception("Could not create the output folder.");

            Material frame = frameMaterial != null ? frameMaterial : MakeMaterial(
                folder + "/Frame.mat", new Color(0.68f, 0.59f, 0.46f), 0.18f);
            Material boards = shelfMaterial != null ? shelfMaterial : MakeMaterial(
                folder + "/Shelves.mat", new Color(0.82f, 0.76f, 0.65f), 0.22f);

            root = new GameObject("Pantry_Shelf");
            root.transform.position = position;
            Transform posts = Group(root.transform, "01_Uprights");
            Transform left = Group(root.transform, "02_Left_Shelves");
            Transform middle = Group(root.transform, "03_Centre_Shelves");
            Transform right = Group(root.transform, "04_Right_Shelves");
            Transform supports = Group(root.transform, "05_Side_Rails");
            Transform top = Group(root.transform, "06_Top_Back_Rails");

            // Thin square uprights, scaled to the requested overall dimensions.
            float post = Mathf.Min(width * 0.021f, depth * 0.14f, height * 0.02f);
            float boardThickness = Mathf.Min(height * 0.009f, depth * 0.08f);
            float railHeight = post * 0.85f;
            float x0 = -width / 2f + post / 2f;
            float span = width - post;
            float[] xs = { x0, x0 + span * 0.36f, x0 + span * 0.56f, x0 + span };
            float z = (depth - post) / 2f;
            Transform[] bays = { left, middle, right };

            for (int column = 0; column < xs.Length; column++)
            {
                Box(posts, "Post_" + (column + 1) + "_Front",
                    new Vector3(xs[column], height / 2f, -z), new Vector3(post, height, post), frame);
                Box(posts, "Post_" + (column + 1) + "_Back",
                    new Vector3(xs[column], height / 2f, z), new Vector3(post, height, post), frame);

                // Side cross rails connect each front/back pair near the top and bottom.
                float[] railYs = { height * 0.04f, height * 0.32f, height - railHeight / 2f };
                for (int n = 0; n < railYs.Length; n++)
                    Box(supports, "Side_Rail_" + (column + 1) + "_" + (n + 1),
                        new Vector3(xs[column], railYs[n], 0f),
                        new Vector3(post, railHeight, depth - 2f * post), frame);
            }

            for (int bay = 0; bay < 3; bay++)
            {
                float bayWidth = xs[bay + 1] - xs[bay] - post;
                float bayX = (xs[bay] + xs[bay + 1]) / 2f;
                int count = bay == 1 ? ShelfHeights.Length - 1 : ShelfHeights.Length;
                for (int level = 0; level < count; level++)
                    Box(bays[bay], "Shelf_" + (level + 1).ToString("00") + "_BottomUp",
                        new Vector3(bayX, height * ShelfHeights[level], 0f),
                        new Vector3(bayWidth, boardThickness, depth), boards);

                Box(top, "Top_Back_Rail_" + (bay + 1),
                    new Vector3(bayX, height - railHeight / 2f, z),
                    new Vector3(bayWidth, railHeight, post), frame);
            }

            string prefabPath = folder + "/Pantry_Shelf.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new Exception("Unity could not save the Prefab.");
            AssetDatabase.SaveAssets();
            Undo.RegisterCreatedObjectUndo(root, "Create Pantry Shelf");
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            status = "Created 7 left + 6 centre + 7 right shelves. Prefab: " + prefabPath;
            Debug.Log(status, root);
        }
        catch (Exception ex)
        {
            // Retain any partial result for inspection; never delete user assets or scene objects.
            if (root != null)
            {
                Undo.RegisterCreatedObjectUndo(root, "Create Pantry Shelf (partial)");
                Selection.activeGameObject = root;
            }
            status = "Generation stopped: " + ex.Message;
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Pantry Shelf", status + "\nPlease share the Console error.", "OK");
        }
    }

    private static Transform Group(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private void Box(Transform parent, string name, Vector3 centre, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = centre;
        part.transform.localScale = size;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!colliders) DestroyImmediate(part.GetComponent<BoxCollider>());
    }

    private static Material MakeMaterial(string path, Color colour, float smoothness)
    {
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        string shaderName = "Standard";
        if (pipeline != null)
        {
            string type = pipeline.GetType().Name;
            if (type.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) >= 0)
                shaderName = "Universal Render Pipeline/Lit";
            else if (type.IndexOf("HDRender", StringComparison.OrdinalIgnoreCase) >= 0)
                shaderName = "HDRP/Lit";
            else throw new Exception("Custom render pipeline: assign your own Frame and Shelves materials.");
        }
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new Exception("Shader not found: " + shaderName + ". Assign existing materials.");
        Material material = new Material(shader);
        material.name = System.IO.Path.GetFileNameWithoutExtension(path);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
        if (material.HasProperty("_Color")) material.SetColor("_Color", colour);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
#endif
