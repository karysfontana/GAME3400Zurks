#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class DistortedPosterSetup
{
    static readonly string[] Names = {"Poster_01_Riot_Distorted", "Poster_02_Seventeen_Distorted", "Poster_03_PrettyLittleLiars_Distorted"};

    [MenuItem("Tools/Eco/Create Distorted Poster Prefabs")]
    public static void Create()
    {
        string root = null;
        foreach (string guid in AssetDatabase.FindAssets("DistortedPosterSetup t:MonoScript")) {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(p) == "DistortedPosterSetup.cs") {
                root = Path.GetDirectoryName(Path.GetDirectoryName(p)).Replace('\\', '/'); break;
            }
        }
        if (root == null) { Debug.LogError("Distorted poster folder not found."); return; }
        foreach (string sub in new[]{"Materials", "Meshes", "Prefabs"})
            if (!AssetDatabase.IsValidFolder(root+"/"+sub)) AssetDatabase.CreateFolder(root,sub);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) { Debug.LogError("Use URP or Built-in rendering for this setup."); return; }
        Material paper = MaterialAt(root+"/Materials/Paper_White.mat", shader, null, new Color(.97f,.96f,.94f,1));
        foreach (string name in Names) {
            string texturePath = root+"/Textures/"+name+"_BaseColor.png";
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null) {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Trilinear;
                importer.maxTextureSize = 2048;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex == null) { Debug.LogError("Missing " + texturePath); continue; }
            Material print = MaterialAt(root+"/Materials/"+name+".mat",shader,tex,Color.white);
            // Exact source aspect ratios; independent of Unity import downsampling.
            float ratio = name == Names[0] ? 1f : name == Names[1] ? 573f/762f : .75f;
            float aw = Mathf.Min(.5f,.65f*ratio), ah = aw/ratio;
            Mesh generated = Build(aw, ah); generated.name = name+"_Mesh";
            string meshPath = root+"/Meshes/"+name+".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh=generated; AssetDatabase.CreateAsset(mesh,meshPath); }
            else { EditorUtility.CopySerialized(generated,mesh); Object.DestroyImmediate(generated); }
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=new[]{print,paper};
            PrefabUtility.SaveAsPrefabAsset(go,root+"/Prefabs/"+name+".prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Distorted Posters","3 distorted poster prefabs created in Prefabs. Size: 0.5 x 0.65 m.","OK");
    }
    static Material MaterialAt(string path,Shader shader,Texture2D texture,Color color) {
        Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(shader);AssetDatabase.CreateAsset(m,path); }
        m.shader=shader;
        if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",texture);
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",texture);
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);
        if(m.HasProperty("_Color"))m.SetColor("_Color",color);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.08f);
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",0);
        // Thin paper: render both sides; opaque white back hides reverse printing.
        if(m.HasProperty("_Cull"))m.SetFloat("_Cull",0);
        EditorUtility.SetDirty(m);return m;
    }
    static Mesh Build(float width,float height) {
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();
        var front=new List<int>();var paper=new List<int>();
        float x=.25f,y=.325f,a=width/2,b=height/2,z=.0004f;
        System.Action<Vector3,Vector3,Vector3,Vector3,Vector3,bool> q=(p0,p1,p2,p3,normal,printed)=>{
            int o=v.Count;v.AddRange(new[]{p0,p1,p2,p3});n.AddRange(new[]{normal,normal,normal,normal});
            uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});
            (printed?front:paper).AddRange(new[]{o,o+1,o+2,o,o+2,o+3});
        };
        q(new Vector3(-a,-b,z),new Vector3(a,-b,z),new Vector3(a,b,z),new Vector3(-a,b,z),Vector3.forward,true);
        if(b<y-.000001f) {
            q(new Vector3(-x,b,z),new Vector3(x,b,z),new Vector3(x,y,z),new Vector3(-x,y,z),Vector3.forward,false);
            q(new Vector3(-x,-y,z),new Vector3(x,-y,z),new Vector3(x,-b,z),new Vector3(-x,-b,z),Vector3.forward,false);
        }
        if(a<x-.000001f) {
            q(new Vector3(-x,-b,z),new Vector3(-a,-b,z),new Vector3(-a,b,z),new Vector3(-x,b,z),Vector3.forward,false);
            q(new Vector3(a,-b,z),new Vector3(x,-b,z),new Vector3(x,b,z),new Vector3(a,b,z),Vector3.forward,false);
        }
        q(new Vector3(x,-y,-z),new Vector3(-x,-y,-z),new Vector3(-x,y,-z),new Vector3(x,y,-z),Vector3.back,false);
        q(new Vector3(x,-y,z),new Vector3(x,-y,-z),new Vector3(x,y,-z),new Vector3(x,y,z),Vector3.right,false);
        q(new Vector3(-x,-y,-z),new Vector3(-x,-y,z),new Vector3(-x,y,z),new Vector3(-x,y,-z),Vector3.left,false);
        q(new Vector3(-x,y,z),new Vector3(x,y,z),new Vector3(x,y,-z),new Vector3(-x,y,-z),Vector3.up,false);
        q(new Vector3(-x,-y,-z),new Vector3(x,-y,-z),new Vector3(x,-y,z),new Vector3(-x,-y,z),Vector3.down,false);
        Mesh m=new Mesh();m.SetVertices(v);m.SetNormals(n);m.SetUVs(0,uv);m.subMeshCount=2;
        m.SetTriangles(front,0);m.SetTriangles(paper,1);m.RecalculateBounds();m.RecalculateTangents();return m;
    }
}
#endif
