// Place in Assets/Editor. Tools > Pantry > Build Pantry Props.
// Five separate prefabs, metres, bottom-centre pivots. No runtime dependencies.
// Plain materials and basic UVs are supplied; painted textures are not included.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class PantryPropsBuilder : EditorWindow
{
    private Vector3 placement = Vector3.zero;
    private string status = "";
    private string folder;
    private Material wood, white, dark, bottleSurface, metal;
    private int meshNumber;
    private static readonly string[] Names = {
        "Produce_Basket", "Ingredient_Bin", "Wooden_Crate", "Open_Bottle", "Sealed_Can"
    };

    [MenuItem("Tools/Pantry/Build Pantry Props")]
    public static void Open()
    {
        PantryPropsBuilder window = GetWindow<PantryPropsBuilder>("Pantry Props");
        window.minSize = new Vector2(390, 410);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Five separate pantry props", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Reference sizes in metres:\n" +
            "Basket 0.331 x 0.242 x 0.204 (W x D x H)\n" +
            "Bin 0.330 x 0.254 x 0.146\nCrate 0.350 x 0.230 x 0.280\n" +
            "Bottle diameter 0.047, height 0.150\nCan diameter 0.100, height 0.119", MessageType.Info);
        placement = EditorGUILayout.Vector3Field("Floor / tabletop position", placement);
        EditorGUILayout.HelpBox("The basket includes both tiers. Bottle: opaque, open mouth. " +
            "Can: sealed metal lid. Each prop gets its own Prefab with editable child parts. " +
            "All objects have bottom-centre pivots.", MessageType.None);
        bool finite = IsFinite(placement.x) && IsFinite(placement.y) && IsFinite(placement.z);
        using (new EditorGUI.DisabledScope(!finite || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Generate ALL 5 + Save Separate Prefabs", GUILayout.Height(38))) Generate(-1);
            EditorGUILayout.Space();
            for (int i = 0; i < Names.Length; i++)
                if (GUILayout.Button("Only: " + Names[i].Replace('_', ' '))) Generate(i);
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    private static bool IsFinite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
    private static Vector2 P(float r, float y) { return new Vector2(r, y); }

    private void Generate(int requested)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        List<GameObject> created = new List<GameObject>();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Pantry Props");
        try
        {
            const string parent = "Assets/PantryPropsGenerated";
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "PantryPropsGenerated");
            string unique = AssetDatabase.GenerateUniqueAssetPath(parent + "/Props");
            string guid = AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(unique));
            folder = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(folder)) throw new Exception("Cannot create output folder.");
            meshNumber = 0;
            wood = MakeMaterial("Natural_Wood_Colour", new Color(.64f,.43f,.24f), 0f, .22f);
            white = MakeMaterial("White_Plastic", new Color(.86f,.87f,.85f), 0f, .30f);
            dark = MakeMaterial("Dark_Trim", new Color(.075f,.082f,.085f), 0f, .28f);
            bottleSurface = MakeMaterial("Opaque_Bottle", new Color(.79f,.76f,.65f), 0f, .38f);
            metal = MakeMaterial("Silver_Metal", new Color(.65f,.68f,.71f), .80f, .42f);
            int first = requested < 0 ? 0 : requested;
            int last = requested < 0 ? 4 : requested;
            for (int i = first; i <= last; i++)
            {
                GameObject root = new GameObject(Names[i]);
                created.Add(root);
                if (i == 0) Basket(root.transform);
                if (i == 1) Bin(root.transform);
                if (i == 2) Crate(root.transform);
                if (i == 3) Bottle(root.transform);
                if (i == 4) Can(root.transform);
                // Save at the origin; apply display spacing only to the scene instance afterwards.
                string path = folder + "/" + Names[i] + ".prefab";
                if (PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction) == null)
                    throw new Exception("Could not save " + Names[i]);
                root.transform.position = placement + (requested < 0 ? new Vector3((i - 2) * .43f, 0f, 0f) : Vector3.zero);
                PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
                Undo.RegisterCreatedObjectUndo(root, "Generate " + Names[i]);
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
            AssetDatabase.SaveAssets();
            Selection.objects = created.ToArray();
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            status = "Saved " + created.Count + " separate prefab(s) in " + folder + ". Save your scene after placement.";
            Debug.Log(status);
        }
        catch (Exception ex)
        {
            status = "Stopped: " + ex.Message;
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Pantry Props", status + "\nShare the Console error for help.", "OK");
        }
        finally { Undo.CollapseUndoOperations(undoGroup); }
    }

    private static Transform Group(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private void Box(Transform parent, string name, Vector3 centre, Vector3 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = centre;
        obj.transform.localScale = size;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        // These are dressing meshes. Remove primitive colliders to keep openings unobstructed.
        DestroyImmediate(obj.GetComponent<BoxCollider>());
    }

    private void MeshPart(Transform parent, string name, Mesh mesh, Material material)
    {
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, folder + "/Mesh_" + (++meshNumber).ToString("000") + "_" + name + ".asset");
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void Basket(Transform root)
    {
        // 16 mm outer panels + 9 mm divider leave two 14.5 cm clear bays.
        const float width = .331f, depth = .242f, thick = .016f, tierHeight = .102f;
        Transform lower = Group(root, "01_Lower_Tier");
        Transform upper = Group(root, "02_Upper_Tier");
        Box(lower, "Bottom", new Vector3(0,.003f,0), new Vector3(width-2*thick,.006f,depth), wood);
        Box(lower, "Front_Lip", new Vector3(0,.025f,-depth/2+thick/2), new Vector3(width-2*thick,.05f,thick), wood);
        Box(lower, "Back", new Vector3(0,tierHeight/2,depth/2-thick/2), new Vector3(width-2*thick,tierHeight,thick), wood);
        // Side panels contain real capsule-shaped hand holes.
        BasketPanel(lower, "Left_Handle_Panel", -width/2+thick/2, -depth/2, depth/2, 0f, thick, true);
        BasketPanel(lower, "Right_Handle_Panel", width/2-thick/2, -depth/2, depth/2, 0f, thick, true);
        BasketPanel(lower, "Centre_Divider", 0f, -depth/2+thick, depth/2-thick, .006f, .009f, false);

        // The rear half is stacked above the lower tier, matching the drawing's stepped silhouette.
        Box(upper, "Bottom", new Vector3(0,tierHeight+.003f,depth/4), new Vector3(width-2*thick,.006f,depth/2), wood);
        Box(upper, "Front_Lip", new Vector3(0,tierHeight+.025f,thick/2), new Vector3(width-2*thick,.05f,thick), wood);
        Box(upper, "Back", new Vector3(0,tierHeight*1.5f,depth/2-thick/2), new Vector3(width-2*thick,tierHeight,thick), wood);
        BasketPanel(upper, "Left_Curved_Panel", -width/2+thick/2, 0f, depth/2, tierHeight, thick, false);
        BasketPanel(upper, "Right_Curved_Panel", width/2-thick/2, 0f, depth/2, tierHeight, thick, false);
        BasketPanel(upper, "Centre_Divider", 0f, thick, depth/2-thick, tierHeight+.006f, .009f, false);
    }

    private void BasketPanel(Transform parent, string name, float x, float front, float back, float baseY, float thick, bool hole)
    {
        // The centre dividers start above the bottom board but finish at the same top height.
        bool divider = name == "Centre_Divider";
        float topBase = divider ? baseY-.006f : baseY;
        Func<float,float> top = z => topBase + .05f + .052f * Mathf.Sin(Mathf.Clamp01((z-front)/(back-front)*2f)*Mathf.PI*.5f);
        List<float> cuts = Samples(front, back, 48);
        const float centreZ = .042f, halfStraight = .017f, radius = .008f;
        if (hole) AddCapsuleCuts(cuts, centreZ, halfStraight, radius);
        Func<float,Vector2> opening = null;
        if (hole) opening = z => {
            float t = Mathf.Abs(z-centreZ)-halfStraight;
            if (t > radius+.0000001f) return new Vector2(-1,-1);
            float extent = t <= 0 ? radius : Mathf.Sqrt(Mathf.Max(0,radius*radius-t*t));
            return new Vector2(baseY+.073f-extent, baseY+.073f+extent);
        };
        MeshPart(parent, name, Panel(x,thick,cuts,z=>baseY,top,opening), wood);
    }

    private void Bin(Transform root)
    {
        // Outside: .330 x .254. Inside opening: .301 x .250, as annotated.
        const float w=.330f, d=.254f, side=.0145f, end=.002f;
        Box(root,"Bottom",new Vector3(0,.003f,0),new Vector3(w,.006f,d),dark);
        BinRing(root,"Lower_Dark_Band",.006f,.020f,w,d,side,end,dark);
        BinRing(root,"White_Walls",.020f,.131f,w,d,side,end,white);
        BinRing(root,"Top_Dark_Rim",.131f,.146f,w,d,side,end,dark);
    }

    private void BinRing(Transform root,string name,float bottom,float top,float w,float d,float side,float end,Material material)
    {
        Transform group=Group(root,name);
        float y=(bottom+top)/2, h=top-bottom;
        Box(group,"Left",new Vector3(-w/2+side/2,y,0),new Vector3(side,h,d),material);
        Box(group,"Right",new Vector3(w/2-side/2,y,0),new Vector3(side,h,d),material);
        Box(group,"Front",new Vector3(0,y,-d/2+end/2),new Vector3(w-2*side,h,end),material);
        Box(group,"Back",new Vector3(0,y,d/2-end/2),new Vector3(w-2*side,h,end),material);
    }

    private void Crate(Transform root)
    {
        const float w=.35f,d=.23f,t=.008f;
        Transform slats=Group(root,"01_Slatted_Walls");
        for(int level=0;level<3;level++)
        {
            float bottom=level*.10f;
            Box(slats,"Front_Slat_"+(level+1),new Vector3(0,bottom+.04f,-d/2+t/2),new Vector3(w,.08f,t),wood);
            Box(slats,"Back_Slat_"+(level+1),new Vector3(0,bottom+.04f,d/2-t/2),new Vector3(w,.08f,t),wood);
            for(int side=-1;side<=1;side+=2)
            {
                string name=(side<0?"Left":"Right")+"_Slat_"+(level+1);
                float x=side*(w/2-t/2);
                if(level<2) Box(slats,name,new Vector3(x,bottom+.04f,0),new Vector3(t,.08f,d-2*t),wood);
                else
                {
                    // Semicircular notch through the underside of the upper slat.
                    // Together with the 2 cm slat gap this forms the side handle opening.
                    const float r=.029f;
                    List<float> cuts=Samples(-d/2+t,d/2-t,32);
                    for(int k=0;k<=32;k++) cuts.Add(-r*Mathf.Cos(Mathf.PI*k/32f));
                    Func<float,float> lower=z=> .20f+(Mathf.Abs(z)<r?Mathf.Sqrt(Mathf.Max(0,r*r-z*z)):0f);
                    MeshPart(slats,name,Panel(x,t,cuts,lower,z=>.28f,null),wood);
                }
            }
        }
        Transform posts=Group(root,"02_Corner_Posts");
        for(int sx=-1;sx<=1;sx+=2)
            for(int sz=-1;sz<=1;sz+=2)
                Box(posts,"Post_"+sx+"_"+sz,new Vector3(sx*(w/2-t-.0125f),.13f,sz*(d/2-t-.0125f)),new Vector3(.025f,.26f,.025f),wood);
        Box(root,"03_Bottom",new Vector3(0,.005f,0),new Vector3(w-2*t,.01f,d-2*t),wood);
    }

    private void Bottle(Transform root)
    {
        // Outside down to the base, inside back up to the mouth. No cap or mouth disk.
        Vector2[] profile={
            P(.0125f,.150f),P(.013f,.1485f),P(.013f,.1465f),P(.0115f,.145f),
            P(.0113f,.135f),P(.012f,.127f),P(.014f,.117f),P(.017f,.107f),
            P(.0205f,.096f),P(.0228f,.086f),P(.0235f,.077f),P(.0235f,.006f),
            P(.023f,.002f),P(.021f,0f),P(0f,0f),P(0f,.003f),
            P(.020f,.003f),P(.0215f,.005f),P(.022f,.008f),P(.022f,.077f),
            P(.0213f,.086f),P(.019f,.096f),P(.0155f,.107f),P(.0125f,.117f),
            P(.0105f,.127f),P(.0098f,.135f),P(.010f,.145f),P(.010f,.150f),P(.0125f,.150f)
        };
        MeshPart(root,"Hollow_Opaque_Bottle",Lathe(profile),bottleSurface);
    }

    private void Can(Transform root)
    {
        // Body is capped separately, so the lid remains independently editable.
        Vector2[] body={
            P(0f,.114f),P(.046f,.114f),P(.0485f,.113f),P(.0485f,.108f),
            P(.049f,.107f),P(.049f,.105f),P(.0485f,.104f),P(.0485f,.015f),
            P(.049f,.014f),P(.049f,.012f),P(.0485f,.011f),P(.0485f,.005f),
            P(.0497f,.004f),P(.050f,.0025f),P(.0495f,.0007f),P(.048f,0f),
            P(.046f,.0015f),P(0f,.0015f)
        };
        Vector2[] lid={
            P(0f,.115f),P(.043f,.115f),P(.044f,.1147f),P(.045f,.115f),
            P(.0465f,.115f),P(.0475f,.116f),P(.048f,.1185f),P(.049f,.119f),
            P(.050f,.118f),P(.050f,.116f),P(.049f,.114f),P(.047f,.1128f),P(0f,.1128f)
        };
        MeshPart(root,"01_Can_Body",Lathe(body),metal);
        MeshPart(root,"02_Sealed_Metal_Lid",Lathe(lid),metal);
    }

    private static List<float> Samples(float a,float b,int steps)
    {
        List<float> list=new List<float>();
        for(int i=0;i<=steps;i++) list.Add(Mathf.Lerp(a,b,(float)i/steps));
        return list;
    }

    private static void AddCapsuleCuts(List<float> list,float centre,float halfStraight,float radius)
    {
        for(int i=0;i<=16;i++)
        {
            float a=Mathf.PI*.5f*i/16f;
            list.Add(centre-halfStraight-radius*Mathf.Cos(a));
            list.Add(centre+halfStraight+radius*Mathf.Sin(a));
        }
    }

    // Extrude a panel along X; optional opening is (lower Y, upper Y) at each Z.
    // Boundary surfaces include the inside of hand holes, not merely dark patches.
    private static Mesh Panel(float x,float thickness,List<float> samples,Func<float,float> bottom,Func<float,float> top,Func<float,Vector2> hole)
    {
        samples.Sort();
        List<float> zs=new List<float>();
        foreach(float z in samples) if(zs.Count==0 || z-zs[zs.Count-1]>.0000001f) zs.Add(z);
        Solid mesh=new Solid();
        float left=x-thickness/2, right=x+thickness/2;
        for(int i=0;i<zs.Count-1;i++)
        {
            float a=zs[i],b=zs[i+1];
            Vector2 mid=hole==null?new Vector2(-1,-1):hole((a+b)/2);
            if(mid.x>=0)
            {
                Vector2 ha=hole(a),hb=hole(b);
                // Numerical tolerance at a capsule endpoint can place it just outside.
                if(ha.x<0) ha=new Vector2(mid.x+(mid.y-mid.x)/2,mid.x+(mid.y-mid.x)/2);
                if(hb.x<0) hb=new Vector2(mid.x+(mid.y-mid.x)/2,mid.x+(mid.y-mid.x)/2);
                Strip(mesh,left,right,a,b,bottom(a),bottom(b),ha.x,hb.x);
                Strip(mesh,left,right,a,b,ha.y,hb.y,top(a),top(b));
            }
            else Strip(mesh,left,right,a,b,bottom(a),bottom(b),top(a),top(b));
        }
        float start=zs[0],end=zs[zs.Count-1];
        mesh.Quad(new Vector3(left,bottom(start),start),new Vector3(right,bottom(start),start),new Vector3(right,top(start),start),new Vector3(left,top(start),start),Vector3.back);
        mesh.Quad(new Vector3(left,bottom(end),end),new Vector3(right,bottom(end),end),new Vector3(right,top(end),end),new Vector3(left,top(end),end),Vector3.forward);
        return mesh.Finish();
    }

    private static void Strip(Solid m,float l,float r,float a,float b,float loA,float loB,float hiA,float hiB)
    {
        Vector3 la=new Vector3(l,loA,a),lb=new Vector3(l,loB,b),ha=new Vector3(l,hiA,a),hb=new Vector3(l,hiB,b);
        Vector3 ra=new Vector3(r,loA,a),rb=new Vector3(r,loB,b),ta=new Vector3(r,hiA,a),tb=new Vector3(r,hiB,b);
        m.Quad(la,lb,hb,ha,Vector3.left);
        m.Quad(ra,rb,tb,ta,Vector3.right);
        m.Quad(ha,hb,tb,ta,Vector3.up);
        m.Quad(la,lb,rb,ra,Vector3.down);
    }

    private class Solid
    {
        private readonly List<Vector3> vertices=new List<Vector3>();
        private readonly List<Vector3> normals=new List<Vector3>();
        private readonly List<Vector2> uv=new List<Vector2>();
        private readonly List<int> triangles=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
        {
            Triangle(a,b,c,facing); Triangle(a,c,d,facing);
        }
        private void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 facing)
        {
            Vector3 cross=Vector3.Cross(b-a,c-a);
            if(cross.sqrMagnitude<1e-20f) return;
            if(Vector3.Dot(cross,facing)<0) { Vector3 temp=b;b=c;c=temp;cross=-cross; }
            Vector3 n=cross.normalized;
            int start=vertices.Count;
            Vector3[] points={a,b,c};
            foreach(Vector3 p in points)
            {
                vertices.Add(p); normals.Add(n);
                if(Mathf.Abs(n.x)>.5f) uv.Add(new Vector2(p.z,p.y)*10f);
                else if(Mathf.Abs(n.y)>.5f) uv.Add(new Vector2(p.x,p.z)*10f);
                else uv.Add(new Vector2(p.x,p.y)*10f);
            }
            triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
        }
        public Mesh Finish()
        {
            Mesh m=new Mesh();m.SetVertices(vertices);m.SetNormals(normals);m.SetUVs(0,uv);m.SetTriangles(triangles,0);
            m.RecalculateBounds();m.RecalculateTangents();return m;
        }
    }

    private static Mesh Lathe(Vector2[] profile)
    {
        const int sides=64,stride=65;
        Vector3[] v=new Vector3[profile.Length*stride],n=new Vector3[profile.Length*stride];
        Vector2[] uv=new Vector2[v.Length];
        List<int> triangles=new List<int>();
        float[] lengths=new float[profile.Length];
        for(int i=1;i<profile.Length;i++) lengths[i]=lengths[i-1]+Vector2.Distance(profile[i],profile[i-1]);
        bool closed=(profile[0]-profile[profile.Length-1]).sqrMagnitude<1e-12f;
        for(int i=0;i<profile.Length;i++)
        {
            Vector2 prev=profile[Mathf.Max(0,i-1)],next=profile[Mathf.Min(profile.Length-1,i+1)];
            if(closed&&(i==0||i==profile.Length-1)) {prev=profile[profile.Length-2];next=profile[1];}
            Vector2 tangent=(next-prev).normalized;
            for(int j=0;j<=sides;j++)
            {
                float angle=2*Mathf.PI*j/sides,s=Mathf.Sin(angle),c=Mathf.Cos(angle);
                int a=i*stride+j,b=a+stride;
                v[a]=new Vector3(profile[i].x*s,profile[i].y,profile[i].x*c);
                n[a]=new Vector3(-tangent.y*s,tangent.x,-tangent.y*c).normalized;
                uv[a]=new Vector2((float)j/sides,lengths[i]/lengths[lengths.Length-1]);
                if(i==profile.Length-1||j==sides) continue;
                if(profile[i].x>0) {triangles.Add(a);triangles.Add(b);triangles.Add(a+1);}
                if(profile[i+1].x>0) {triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);}
            }
        }
        Mesh mesh=new Mesh();mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.SetTriangles(triangles,0);
        mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }

    private Material MakeMaterial(string name,Color colour,float metallic,float smoothness)
    {
        string shaderName="Standard";
        RenderPipelineAsset pipeline=GraphicsSettings.currentRenderPipeline;
        if(pipeline!=null)
        {
            string type=pipeline.GetType().Name;
            if(type.IndexOf("Universal",StringComparison.OrdinalIgnoreCase)>=0) shaderName="Universal Render Pipeline/Lit";
            else if(type.IndexOf("HDRender",StringComparison.OrdinalIgnoreCase)>=0) shaderName="HDRP/Lit";
            else throw new Exception("Unsupported custom render pipeline.");
        }
        Shader shader=Shader.Find(shaderName);
        if(shader==null) throw new Exception("Missing shader: "+shaderName);
        Material m=new Material(shader);m.name=name;
        if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",colour);
        if(m.HasProperty("_Color")) m.SetColor("_Color",colour);
        if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",metallic);
        if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",smoothness);
        if(m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",smoothness);
        AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;
    }
}
#endif
