// Put this file in Assets/Editor. Tools > Pantry > Build Kitchen Details.
// Creates five independent Prefabs with persistent meshes and plain materials.
// Revised: timer has its own matte materials, with emission and specular reflections disabled.
// Metres; fronts face -Z. Scene objects require no runtime generator scripts.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class KitchenDetailsBuilder : EditorWindow
{
    private static readonly string[] Names = { "Wall_Outlet", "Curved_Kitchen_Timer", "Wall_Vent", "Decorative_Wall_Shelf", "Potted_Pilea" };
    private Vector3 placement = Vector3.zero;
    private string folder, status = "";
    private int meshIndex;
    private Material ivory, trim, black, wood, ceramic, soil, green, greenLight, stem;
    private Material timerBody, timerTrim, timerFace, timerMarks;

    [MenuItem("Tools/Pantry/Build Kitchen Details")]
    public static void Open()
    {
        KitchenDetailsBuilder w = GetWindow<KitchenDetailsBuilder>("Kitchen Details");
        w.minSize = new Vector2(405, 430); w.Show();
    }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Five separate kitchen details", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Outlet: 7 x 11.5 cm\nTimer: 7.8 cm across side centres; 6.5 cm between corner positions\n" +
            "Vent: 29.5 x 14.2 cm\nShelf board: 76.2 x 15.24 x 3.81 cm\nPot bowl: diameter 12.7 cm, height 9 cm; foot: 3.25 cm", MessageType.Info);
        placement = EditorGUILayout.Vector3Field("Placement origin", placement);
        EditorGUILayout.HelpBox("Front faces -Z. Each click makes NEW assets. All five objects are static models. " +
            "The potted plant is one Prefab with separate pot, soil, stems and leaves. Materials are plain colours for later texturing.", MessageType.None);
        EditorGUILayout.HelpBox("MATTE TIMER VERSION: no emission, specular highlights or environment reflections on the timer (URP / Built-in). " +
            "Existing scene objects are not replaced: disable the old timer before comparing the new one.", MessageType.Info);
        bool valid = Finite(placement.x) && Finite(placement.y) && Finite(placement.z);
        using (new EditorGUI.DisabledScope(!valid || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Generate ALL 5 + Save Separate Prefabs", GUILayout.Height(38))) Generate(-1);
            EditorGUILayout.Space();
            for (int i = 0; i < Names.Length; i++)
                if (GUILayout.Button("Only: " + Names[i].Replace('_', ' '))) Generate(i);
        }
        if (status.Length > 0) EditorGUILayout.HelpBox(status, MessageType.Info);
    }
    private static bool Finite(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
    private static Vector2 P(float x, float y) { return new Vector2(x, y); }
    private static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    private void Generate(int selected)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        List<GameObject> roots = new List<GameObject>();
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate kitchen details");
        try
        {
            const string parent = "Assets/KitchenDetailsGenerated";
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "KitchenDetailsGenerated");
            string path = AssetDatabase.GenerateUniqueAssetPath(parent + "/Details");
            folder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path)));
            if (string.IsNullOrEmpty(folder)) throw new Exception("Could not create output folder.");
            meshIndex = 0;
            ivory = Mat("Ivory_Plastic", new Color(.83f,.81f,.75f), 0, .30f);
            trim = Mat("Inset_Trim", new Color(.54f,.55f,.53f), .35f, .32f);
            black = Mat("Recess_And_Markings", new Color(.028f,.03f,.026f), 0, .18f);
            wood = Mat("Natural_Wood_Colour", new Color(.62f,.42f,.25f), 0, .24f);
            ceramic = Mat("White_Ceramic", new Color(.87f,.85f,.79f), 0, .38f);
            soil = Mat("Dark_Soil", new Color(.095f,.056f,.032f), 0, .08f);
            green = Mat("Leaf_Green", new Color(.13f,.30f,.09f), 0, .28f);
            greenLight = Mat("Leaf_Light_Green", new Color(.19f,.38f,.12f), 0, .26f);
            stem = Mat("Plant_Stems", new Color(.23f,.32f,.105f), 0, .17f);
            if (selected < 0 || selected == 1)
            {
                // Dedicated materials keep the timer's finish separate from the other four objects.
                timerBody = Mat("Timer_Matte_Body", new Color(.70f,.683f,.633f), 0, 0, true);
                timerTrim = Mat("Timer_Matte_Rim", new Color(.54f,.55f,.53f), 0, 0, true);
                timerFace = Mat("Timer_Matte_Dial", new Color(.70f,.684f,.636f), 0, 0, true);
                timerMarks = Mat("Timer_Matte_Markings", new Color(.028f,.03f,.026f), 0, 0, true);
            }
            int start = selected < 0 ? 0 : selected, end = selected < 0 ? 4 : selected;
            // Measured gaps: the wide wall shelf never overlaps the other preview objects.
            float[] offsets = { -.72f, -.59f, -.34f, .27f, .84f };
            for (int i = start; i <= end; i++)
            {
                GameObject root = new GameObject(Names[i]); roots.Add(root);
                if (i == 0) Outlet(root.transform);
                if (i == 1) Timer(root.transform);
                if (i == 2) Vent(root.transform);
                if (i == 3) Shelf(root.transform);
                if (i == 4) Plant(root.transform);
                if (PrefabUtility.SaveAsPrefabAssetAndConnect(root, folder + "/" + Names[i] + ".prefab", InteractionMode.AutomatedAction) == null)
                    throw new Exception("Could not save " + Names[i]);
                root.transform.position = placement + (selected < 0 ? V(offsets[i],0,0) : Vector3.zero);
                PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
                Undo.RegisterCreatedObjectUndo(root, "Create " + Names[i]);
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
            AssetDatabase.SaveAssets(); Selection.objects = roots.ToArray();
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            status = "Saved " + roots.Count + " independent Prefab(s) in " + folder + ".";
            Debug.Log(status);
        }
        catch (Exception e)
        {
            status = "Stopped: " + e.Message; Debug.LogException(e);
            EditorUtility.DisplayDialog("Kitchen Details", status + "\nShare the Console error for help.", "OK");
        }
        finally { Undo.CollapseUndoOperations(undo); }
    }

    private void Outlet(Transform root)
    {
        const float cy = .0575f;
        Part(root,"01_Rounded_Faceplate",SolidOutline(RoundedRect(.070f,.115f,.0035f),.001f,.006f,.001f),ivory,V(0,cy,0));
        Box(root,"02_Dark_Recess_Backing",V(0,cy,.0006f),V(.032f,.065f,.0004f),black);
        Part(root,"03_Receptacle_With_Six_Recesses",OutletInsert(),ivory,V(0,cy,0));
        // Small centre mounting screw between the sockets.
        Part(root,"04_Centre_Screw",SolidOutline(Circle(.0019f,24),-.0036f,-.0031f,0),trim,V(0,cy-.0045f,0));
        Box(root,"05_Screw_Slot",V(0,cy-.0045f,-.0037f),V(.0023f,.00035f,.00015f),black);
    }

    private void Timer(Transform root)
    {
        const float mid = .039f;
        // Superellipse: at 45 degrees x=y=0.0325; at each side centre the extent is 0.039.
        // Exponent is derived from 2*(.0325/.039)^n=1, preserving both requested measurements.
        float exponent = Mathf.Log(.5f) / Mathf.Log(.0325f/.039f);
        List<Vector2> outline = Superellipse(.039f,.039f,exponent,128);
        Part(root,"01_Bulging_Four_Sided_Housing",SolidOutline(outline,-.0135f,.0215f,.0022f),timerBody,V(0,mid,0));
        Part(root,"02_Dial_Rim",Ring(Circle(.034f,96),Circle(.0318f,96),-.0147f,-.0134f),timerTrim,V(0,mid,0));
        Part(root,"03_Dial_Face",SolidOutline(Circle(.0318f,96),-.0146f,-.0135f,0),timerFace,V(0,mid,0));
        Transform marks = Group(root,"04_Dial_Ticks");
        for (int i=0;i<60;i++)
        {
            float a=2*Mathf.PI*i/60f, len=i%5==0?.0032f:.0014f, r=.0295f-len/2;
            GameObject tick=Box(marks,"Tick_"+i.ToString("00"),V(Mathf.Sin(a)*r,mid+Mathf.Cos(a)*r,-.0149f),V(i%5==0?.00065f:.0003f,len,.00035f),timerMarks);
            tick.transform.localRotation=Quaternion.Euler(0,0,-i*6f);
        }
        List<Vector2> knob = new List<Vector2>();
        AddBezier(knob,P(0,.028f),P(.006f,.024f),P(.009f,.012f),P(.014f,.004f),12);
        AddBezier(knob,P(.014f,.004f),P(.024f,-.010f),P(.024f,-.025f),P(0,-.026f),16);
        AddBezier(knob,P(0,-.026f),P(-.024f,-.025f),P(-.024f,-.010f),P(-.014f,.004f),16);
        AddBezier(knob,P(-.014f,.004f),P(-.009f,.012f),P(-.006f,.024f),P(0,.028f),12);
        // Width is normalized to the annotated 4.25 cm knob width.
        float maxX=0; foreach(Vector2 p in knob) maxX=Mathf.Max(maxX,Mathf.Abs(p.x));
        for(int i=0;i<knob.Count;i++) knob[i]=P(knob[i].x*.02125f/maxX,knob[i].y);
        Part(root,"05_Teardrop_Knob",SolidOutline(knob,-.0215f,-.0147f,.0013f),timerBody,V(0,mid,0));
        Box(root,"06_Pointer_Mark",V(0,mid+.020f,-.0216f),V(.0007f,.008f,.0002f),timerMarks);
    }

    private void Vent(Transform root)
    {
        const float cy=.071f;
        Part(root,"01_Rounded_Vent_Frame",Ring(RoundedRect(.295f,.142f,.012f),RoundedRect(.2565f,.078f,.003f),0,.015f),ivory,V(0,cy,0));
        // Open duct extends 3.25 cm behind the 1.5 cm face frame.
        Transform duct=Group(root,"02_Open_Rear_Duct");
        Box(duct,"Top",V(0,cy+.038f,.03125f),V(.2565f,.002f,.0325f),trim);
        Box(duct,"Bottom",V(0,cy-.038f,.03125f),V(.2565f,.002f,.0325f),trim);
        Box(duct,"Left",V(-.12725f,cy,.03125f),V(.002f,.074f,.0325f),trim);
        Box(duct,"Right",V(.12725f,cy,.03125f),V(.002f,.074f,.0325f),trim);
        Transform blades=Group(root,"03_Individual_Louvres");
        for(int i=0;i<21;i++)
        {
            GameObject b=Box(blades,"Louvre_"+(i+1).ToString("00"),V(Mathf.Lerp(-.122f,.122f,i/20f),cy,.006f),V(.008f,.077f,.0016f),ivory);
            b.transform.localRotation=Quaternion.Euler(0,25f,0);
        }
        Box(root,"04_Upper_Linkage",V(0,cy+.020f,.010f),V(.247f,.002f,.002f),trim);
        Box(root,"05_Lower_Linkage",V(0,cy-.020f,.010f),V(.247f,.002f,.002f),trim);
        Box(root,"06_Adjustment_Tab",V(-.116f,cy-.020f,.002f),V(.005f,.008f,.006f),ivory);
    }

    private void Shelf(Transform root)
    {
        Box(root,"01_Shelf_Board",V(0,.21905f,0),V(.762f,.0381f,.1524f),wood);
        // Side profile in (Z,Y), including the S-shaped front edge drawn in the reference.
        List<Vector2> profile=new List<Vector2>();
        profile.Add(P(.0762f,.20f));
        AddBezier(profile,P(-.062f,.20f),P(-.058f,.148f),P(.043f,.146f),P(.035f,.116f),20);
        AddBezier(profile,P(.035f,.116f),P(.029f,.098f),P(-.034f,.111f),P(-.032f,.072f),16);
        AddBezier(profile,P(-.032f,.072f),P(-.030f,.038f),P(.039f,.046f),P(.016f,.019f),16);
        AddBezier(profile,P(.016f,.019f),P(.008f,.003f),P(.039f,0),P(.060f,0),14);
        profile.Add(P(.060f,0));
        profile.Add(P(.0762f,0));
        Mesh bracket=ExtrudeX(profile,.0351f);
        SaveMesh(bracket,"Curved_Shelf_Bracket");
        MeshObject(root,"02_Left_Curved_Bracket",bracket,wood,V(-.321f,0,0));
        MeshObject(root,"03_Right_Curved_Bracket",bracket,wood,V(.321f,0,0));
    }

    private void Plant(Transform root)
    {
        Transform pot=Group(root,"01_Pot");
        Part(pot,"Pedestal_Foot",Lathe(new Vector2[]{P(0,.0325f),P(.032f,.0325f),P(.034f,.027f),P(.0375f,.005f),P(.0375f,.002f),P(.0355f,0),P(0,0)}),ceramic,Vector3.zero);
        Part(pot,"Hollow_Rounded_Bowl",Lathe(new Vector2[]{
            P(.0625f,.1225f),P(.0635f,.1215f),P(.0635f,.1185f),P(.063f,.080f),P(.061f,.066f),P(.056f,.052f),P(.048f,.042f),P(.037f,.035f),P(.026f,.0325f),P(0,.0325f),
            P(0,.060f),P(.037f,.060f),P(.045f,.064f),P(.052f,.071f),P(.057f,.080f),P(.059f,.095f),P(.0595f,.1195f),P(.0595f,.1225f),P(.0625f,.1225f)
        }),ceramic,Vector3.zero);
        Transform ground=Group(root,"02_Soil");
        Part(ground,"Soil_Surface",Lathe(new Vector2[]{P(0,.1145f),P(.025f,.115f),P(.052f,.114f),P(.059f,.113f),P(.059f,.110f),P(0,.110f)}),soil,Vector3.zero);
        Transform vegetation=Group(root,"03_Plant");
        Transform stems=Group(vegetation,"Stems"),leaves=Group(vegetation,"Individual_Leaves");
        Part(stems,"Main_Stem",Tube(new Vector3[]{V(0,.111f,0),V(.001f,.140f,0),V(-.003f,.169f,.002f),V(.006f,.189f,.003f)},.0015f,.0007f),stem,Vector3.zero);
        Vector3[] centres={V(-.066f,.154f,-.009f),V(-.041f,.179f,.018f),V(-.021f,.200f,.009f),V(.024f,.198f,.012f),V(.054f,.188f,.020f),V(.076f,.151f,-.003f),V(.039f,.150f,-.045f),V(-.015f,.149f,-.051f),V(-.051f,.139f,-.034f),V(.033f,.173f,.035f),V(.012f,.178f,-.020f)};
        float[] radii={.024f,.015f,.0195f,.0215f,.017f,.025f,.021f,.024f,.016f,.016f,.022f};
        Mesh leaf=LeafMesh(); SaveMesh(leaf,"Rounded_Cupped_Leaf");
        for(int i=0;i<centres.Length;i++)
        {
            Vector3 c=centres[i];
            Vector3 normal=V((i%3-1)*.32f,.18f+(i%4)*.16f,-1).normalized;
            Vector3 attach=c-normal*.001f, start=V(0,.131f+(i%5)*.009f,0);
            Vector3 mid=Vector3.Lerp(start,attach,.55f)+V(0,.011f,0);
            Part(stems,"Petiole_"+(i+1).ToString("00"),Tube(new Vector3[]{start,mid,attach},.00085f,.00045f),stem,Vector3.zero);
            GameObject l=MeshObject(leaves,"Leaf_"+(i+1).ToString("00"),leaf,i%3==0?greenLight:green,c);
            l.transform.localRotation=Quaternion.FromToRotation(Vector3.back,normal)*Quaternion.Euler(0,0,i*29f);
            l.transform.localScale=Vector3.one*radii[i];
        }
    }

    private static Transform Group(Transform p,string name)
    { GameObject g=new GameObject(name);g.transform.SetParent(p,false);return g.transform; }
    private GameObject Box(Transform p,string name,Vector3 centre,Vector3 size,Material mat)
    {
        GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);
        g.transform.localPosition=centre;g.transform.localScale=size;g.GetComponent<MeshRenderer>().sharedMaterial=mat;
        DestroyImmediate(g.GetComponent<BoxCollider>());return g;
    }
    private void SaveMesh(Mesh m,string name)
    {m.name=name;AssetDatabase.CreateAsset(m,folder+"/Mesh_"+(++meshIndex).ToString("000")+"_"+name+".asset");}
    private GameObject Part(Transform p,string name,Mesh mesh,Material mat,Vector3 pos)
    {SaveMesh(mesh,name);return MeshObject(p,name,mesh,mat,pos);}
    private static GameObject MeshObject(Transform p,string name,Mesh mesh,Material mat,Vector3 pos)
    {
        GameObject g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;
        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;
    }
    private Material Mat(string name,Color colour,float metallic,float smoothness,bool matte=false)
    {
        string shader="Standard";RenderPipelineAsset pipeline=GraphicsSettings.currentRenderPipeline;
        if(pipeline!=null)
        {
            string t=pipeline.GetType().Name;
            if(t.IndexOf("Universal",StringComparison.OrdinalIgnoreCase)>=0)shader="Universal Render Pipeline/Lit";
            else if(t.IndexOf("HDRender",StringComparison.OrdinalIgnoreCase)>=0)shader="HDRP/Lit";
            else throw new Exception("Custom render pipeline is not supported by the default materials.");
        }
        if(matte && shader=="HDRP/Lit")
            throw new Exception("The matte timer supports URP and Built-in. HDRP requires a separate material setup.");
        Shader found=Shader.Find(shader);if(found==null)throw new Exception("Missing shader: "+shader);
        Material m=new Material(found);m.name=name;
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",colour);
        if(m.HasProperty("_Color"))m.SetColor("_Color",colour);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smoothness);
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smoothness);
        // Explicitly non-emissive for all five models.
        if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);
        if(m.HasProperty("_EmissiveColor"))m.SetColor("_EmissiveColor",Color.black);
        if(m.HasProperty("_EmissionMap"))m.SetTexture("_EmissionMap",null);
        m.DisableKeyword("_EMISSION");
        m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        if(matte)
        {
            bool urp=shader=="Universal Render Pipeline/Lit";
            string reflections=urp?"_EnvironmentReflections":"_GlossyReflections";
            if(!m.HasProperty("_SpecularHighlights") || !m.HasProperty(reflections))
            {
                DestroyImmediate(m);
                throw new Exception("This shader does not expose the required matte controls.");
            }
            m.SetFloat("_SpecularHighlights",0);
            m.SetFloat(reflections,0);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.EnableKeyword(urp?"_ENVIRONMENTREFLECTIONS_OFF":"_GLOSSYREFLECTIONS_OFF");
            if(m.GetFloat("_SpecularHighlights")!=0 || m.GetFloat(reflections)!=0 || m.IsKeywordEnabled("_EMISSION"))
            {
                DestroyImmediate(m);
                throw new Exception("Could not verify the matte timer material settings.");
            }
        }
        AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;
    }

    private static List<Vector2> Circle(float r,int n)
    {List<Vector2> p=new List<Vector2>();for(int i=0;i<n;i++){float a=2*Mathf.PI*i/n;p.Add(P(r*Mathf.Cos(a),r*Mathf.Sin(a)));}return p;}
    private static List<Vector2> Superellipse(float x,float y,float exponent,int count)
    {
        List<Vector2> p=new List<Vector2>();
        for(int i=0;i<count;i++)
        {
            // Polar sampling gives uniform angle and stable axis points for exponent > 2.
            float a=2*Mathf.PI*i/count,c=Mathf.Cos(a),s=Mathf.Sin(a);
            float r=Mathf.Pow(Mathf.Pow(Mathf.Abs(c),exponent)+Mathf.Pow(Mathf.Abs(s),exponent),-1/exponent);
            p.Add(P(x*r*c,y*r*s));
        }
        return p;
    }
    private static List<Vector2> RoundedRect(float w,float h,float r)
    {
        List<Vector2> p=new List<Vector2>();
        for(int q=0;q<4;q++)for(int i=0;i<=12;i++)
        {
            float a=(q+i/12f)*Mathf.PI/2,cx=(q==0||q==3?1:-1)*(w/2-r),cy=(q<2?1:-1)*(h/2-r);
            p.Add(P(cx+r*Mathf.Cos(a),cy+r*Mathf.Sin(a)));
        }
        return p;
    }
    private static void AddBezier(List<Vector2> list,Vector2 a,Vector2 b,Vector2 c,Vector2 d,int steps)
    {
        for(int i=0;i<steps;i++)
        {
            float t=i/(float)steps,u=1-t;Vector2 p=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
            if(list.Count==0||(list[list.Count-1]-p).sqrMagnitude>1e-14f)list.Add(p);
        }
    }
    private static float Area(List<Vector2> p)
    {float a=0;for(int i=0;i<p.Count;i++){Vector2 b=p[(i+1)%p.Count];a+=p[i].x*b.y-b.x*p[i].y;}return a/2;}
    private static float Cross(Vector2 a,Vector2 b,Vector2 c)
    {return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
    private static List<int> Triangulate(List<Vector2> points)
    {
        List<int> ids=new List<int>(),tris=new List<int>();
        for(int i=0;i<points.Count;i++)ids.Add(i);
        if(Area(points)<0)ids.Reverse();
        int guard=points.Count*points.Count;
        while(ids.Count>3&&guard-->0)
        {
            bool clipped=false;
            for(int j=0;j<ids.Count;j++)
            {
                int a=ids[(j+ids.Count-1)%ids.Count],b=ids[j],c=ids[(j+1)%ids.Count];
                if(Cross(points[a],points[b],points[c])<=1e-12f)continue;
                bool occupied=false;
                foreach(int k in ids)
                {
                    if(k==a||k==b||k==c)continue;
                    if(Cross(points[a],points[b],points[k])>=-1e-12f&&Cross(points[b],points[c],points[k])>=-1e-12f&&Cross(points[c],points[a],points[k])>=-1e-12f){occupied=true;break;}
                }
                if(occupied)continue;tris.Add(a);tris.Add(b);tris.Add(c);ids.RemoveAt(j);clipped=true;break;
            }
            if(!clipped)throw new Exception("Could not triangulate a model outline.");
        }
        if(ids.Count!=3)throw new Exception("Incomplete polygon triangulation.");
        tris.AddRange(ids);return tris;
    }
    private static Vector3 XY(Vector2 p,float z){return V(p.x,p.y,z);}
    private static Mesh SolidOutline(List<Vector2> p,float front,float back,float bevel)
    {
        Draft m=new Draft();List<int> face=Triangulate(p);
        float minExtent=float.MaxValue;
        foreach(Vector2 q in p)minExtent=Mathf.Min(minExtent,q.magnitude);
        float factor=1-Mathf.Min(bevel/minExtent,.25f),b=Mathf.Min(bevel,(back-front)*.4f);
        float[] zs=b>0?new float[]{front,front+b,back-b,back}:new float[]{front,back};
        float[] scales=b>0?new float[]{factor,1,1,factor}:new float[]{1,1};
        float sign=Mathf.Sign(Area(p));
        for(int ring=0;ring<zs.Length-1;ring++)for(int i=0;i<p.Count;i++)
        {
            int j=(i+1)%p.Count;Vector2 edge=p[j]-p[i];Vector3 outwards=V(edge.y,-edge.x,0)*sign;
            m.Quad(XY(p[i]*scales[ring],zs[ring]),XY(p[j]*scales[ring],zs[ring]),XY(p[j]*scales[ring+1],zs[ring+1]),XY(p[i]*scales[ring+1],zs[ring+1]),outwards);
        }
        for(int i=0;i<face.Count;i+=3)
        {
            m.Tri(XY(p[face[i]]*scales[0],front),XY(p[face[i+1]]*scales[0],front),XY(p[face[i+2]]*scales[0],front),Vector3.back);
            m.Tri(XY(p[face[i]]*scales[scales.Length-1],back),XY(p[face[i+1]]*scales[scales.Length-1],back),XY(p[face[i+2]]*scales[scales.Length-1],back),Vector3.forward);
        }
        return m.Mesh();
    }
    private static Mesh Ring(List<Vector2> outer,List<Vector2> inner,float front,float back)
    {
        if(outer.Count!=inner.Count)throw new Exception("Ring contours must have matching counts.");
        Draft m=new Draft();
        for(int i=0;i<outer.Count;i++)
        {
            int j=(i+1)%outer.Count;Vector2 e=outer[j]-outer[i],ie=inner[j]-inner[i];
            m.Quad(XY(outer[i],front),XY(outer[j],front),XY(inner[j],front),XY(inner[i],front),Vector3.back);
            m.Quad(XY(outer[i],back),XY(outer[j],back),XY(inner[j],back),XY(inner[i],back),Vector3.forward);
            m.Quad(XY(outer[i],front),XY(outer[j],front),XY(outer[j],back),XY(outer[i],back),V(e.y,-e.x,0));
            m.Quad(XY(inner[i],front),XY(inner[j],front),XY(inner[j],back),XY(inner[i],back),V(-ie.y,ie.x,0));
        }
        return m.Mesh();
    }
    private static Mesh ExtrudeX(List<Vector2> p,float thickness)
    {
        Draft m=new Draft();List<int> tris=Triangulate(p);float sign=Mathf.Sign(Area(p));
        for(int k=0;k<tris.Count;k+=3)
        {
            Vector2 a=p[tris[k]],b=p[tris[k+1]],c=p[tris[k+2]];
            m.Tri(V(-thickness/2,a.y,a.x),V(-thickness/2,b.y,b.x),V(-thickness/2,c.y,c.x),Vector3.left);
            m.Tri(V(thickness/2,a.y,a.x),V(thickness/2,b.y,b.x),V(thickness/2,c.y,c.x),Vector3.right);
        }
        for(int i=0;i<p.Count;i++)
        {
            Vector2 a=p[i],b=p[(i+1)%p.Count],e=b-a;
            m.Quad(V(-thickness/2,a.y,a.x),V(-thickness/2,b.y,b.x),V(thickness/2,b.y,b.x),V(thickness/2,a.y,a.x),V(0,-e.x,e.y)*sign);
        }
        return m.Mesh();
    }

    private struct SocketHole
    {
        public float x,y,r,halfLength;public bool ground;
        public SocketHole(float x,float y,bool ground){this.x=x;this.y=y;this.ground=ground;r=ground?.004f:.0015f;halfLength=ground?0:.004f;}
        public float Low {get{return y-halfLength-r;}}
        public float High {get{return y+halfLength+r;}}
        public float HalfWidth(float at)
        {
            if(ground&&at<=y)return r;
            float d=ground?at-y:Mathf.Max(0,Mathf.Abs(at-y)-halfLength);
            return Mathf.Sqrt(Mathf.Max(0,r*r-d*d));
        }
    }
    private static Mesh OutletInsert()
    {
        List<SocketHole> holes=new List<SocketHole>{new SocketHole(-.00635f,.022f,false),new SocketHole(.00635f,.022f,false),new SocketHole(0,.006f,true),new SocketHole(-.00635f,-.016f,false),new SocketHole(.00635f,-.016f,false),new SocketHole(0,-.029f,true)};
        List<float> ys=new List<float>();for(int i=0;i<=80;i++)ys.Add(Mathf.Lerp(-.0335f,.0335f,i/80f));
        foreach(SocketHole h in holes)for(int i=0;i<=32;i++)ys.Add(Mathf.Lerp(h.Low,h.High,i/32f));
        ys.Sort();List<float> rows=new List<float>();foreach(float y in ys)if(rows.Count==0||y-rows[rows.Count-1]>1e-8f)rows.Add(y);
        Draft m=new Draft();const float front=-.003f,back=.0009f;
        for(int i=0;i<rows.Count-1;i++)
        {
            float a=rows[i],b=rows[i+1],mid=(a+b)/2;
            List<SocketHole> active=holes.FindAll(h=>mid>h.Low&&mid<h.High);active.Sort((h,k)=>h.x.CompareTo(k.x));
            float la=-InsertHalfWidth(a),lb=-InsertHalfWidth(b);
            foreach(SocketHole h in active)
            {
                float ra=h.x-h.HalfWidth(a),rb=h.x-h.HalfWidth(b);
                FaceStrip(m,a,b,la,lb,ra,rb,front,back);
                la=h.x+h.HalfWidth(a);lb=h.x+h.HalfWidth(b);
            }
            FaceStrip(m,a,b,la,lb,InsertHalfWidth(a),InsertHalfWidth(b),front,back);
        }
        foreach(float y in new float[]{-.0335f,.0335f})
            m.Quad(V(-InsertHalfWidth(y),y,front),V(InsertHalfWidth(y),y,front),V(InsertHalfWidth(y),y,back),V(-InsertHalfWidth(y),y,back),y<0?Vector3.down:Vector3.up);
        foreach(SocketHole h in holes)if(h.ground)
            m.Quad(V(h.x-h.r,h.Low,front),V(h.x+h.r,h.Low,front),V(h.x+h.r,h.Low,back),V(h.x-h.r,h.Low,back),Vector3.up);
        return m.Mesh();
    }
    private static float InsertHalfWidth(float y)
    {float d=Mathf.Max(0,Mathf.Abs(y)-.0315f);return .01475f+Mathf.Sqrt(Mathf.Max(0,.002f*.002f-d*d));}
    private static void FaceStrip(Draft m,float a,float b,float la,float lb,float ra,float rb,float front,float back)
    {
        m.Quad(V(la,a,front),V(ra,a,front),V(rb,b,front),V(lb,b,front),Vector3.back);
        m.Quad(V(la,a,back),V(ra,a,back),V(rb,b,back),V(lb,b,back),Vector3.forward);
        m.Quad(V(la,a,front),V(lb,b,front),V(lb,b,back),V(la,a,back),Vector3.left);
        m.Quad(V(ra,a,front),V(rb,b,front),V(rb,b,back),V(ra,a,back),Vector3.right);
    }

    private class Draft
    {
        private readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> triangles=new List<int>();
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing){Tri(a,b,c,facing);Tri(a,c,d,facing);}
        public void Tri(Vector3 a,Vector3 b,Vector3 c,Vector3 facing)
        {
            Vector3 cross=Vector3.Cross(b-a,c-a);if(cross.sqrMagnitude<1e-22f)return;
            if(Vector3.Dot(cross,facing)<0){Vector3 swap=b;b=c;c=swap;cross=-cross;}
            Vector3 n=cross.normalized;int start=vertices.Count;
            foreach(Vector3 p in new Vector3[]{a,b,c})
            {
                vertices.Add(p);normals.Add(n);
                uv.Add((Mathf.Abs(n.z)>.5f?P(p.x,p.y):Mathf.Abs(n.x)>.5f?P(p.z,p.y):P(p.x,p.z))*10f);
            }
            triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
        }
        public Mesh Mesh()
        {
            Mesh m=new Mesh();if(vertices.Count>65535)m.indexFormat=IndexFormat.UInt32;
            m.SetVertices(vertices);m.SetNormals(normals);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateBounds();m.RecalculateTangents();return m;
        }
    }

    private static Mesh Lathe(Vector2[] p)
    {
        const int sides=64,stride=65;Vector3[] v=new Vector3[p.Length*stride],normals=new Vector3[p.Length*stride];
        Vector2[] uv=new Vector2[v.Length];List<int> triangles=new List<int>();float[] lengths=new float[p.Length];
        for(int i=1;i<p.Length;i++)lengths[i]=lengths[i-1]+Vector2.Distance(p[i-1],p[i]);
        bool closed=(p[0]-p[p.Length-1]).sqrMagnitude<1e-12f;
        for(int i=0;i<p.Length;i++)
        {
            Vector2 prev=p[Mathf.Max(0,i-1)],next=p[Mathf.Min(p.Length-1,i+1)];
            if(closed&&(i==0||i==p.Length-1)){prev=p[p.Length-2];next=p[1];}
            Vector2 tangent=(next-prev).normalized;
            for(int j=0;j<=sides;j++)
            {
                float a=2*Mathf.PI*j/sides,s=Mathf.Sin(a),c=Mathf.Cos(a);int k=i*stride+j,b=k+stride;
                v[k]=V(p[i].x*s,p[i].y,p[i].x*c);normals[k]=V(-tangent.y*s,tangent.x,-tangent.y*c).normalized;
                uv[k]=P(j/(float)sides,lengths[i]/lengths[lengths.Length-1]);
                if(i==p.Length-1||j==sides)continue;
                if(p[i].x>0){triangles.Add(k);triangles.Add(b);triangles.Add(k+1);}
                if(p[i+1].x>0){triangles.Add(k+1);triangles.Add(b);triangles.Add(b+1);}
            }
        }
        return MeshData(v,normals,uv,triangles);
    }
    private static Mesh Tube(Vector3[] controls,float radiusStart,float radiusEnd)
    {
        List<Vector3> path=new List<Vector3>();
        for(int seg=0;seg<controls.Length-1;seg++)for(int k=0;k<8;k++)
        {
            float t=k/8f,t2=t*t,t3=t2*t;
            Vector3 a=controls[Mathf.Max(0,seg-1)],b=controls[seg],c=controls[seg+1],d=controls[Mathf.Min(controls.Length-1,seg+2)];
            path.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3));
        }
        path.Add(controls[controls.Length-1]);
        const int sides=10,stride=11;int count=path.Count*stride;
        Vector3[] v=new Vector3[count+2],normals=new Vector3[count+2];Vector2[] uv=new Vector2[count+2];List<int> tri=new List<int>();
        for(int i=0;i<path.Count;i++)
        {
            Vector3 tangent=(path[Mathf.Min(path.Count-1,i+1)]-path[Mathf.Max(0,i-1)]).normalized;
            Vector3 axis=Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.9f?Vector3.forward:Vector3.up;
            Vector3 right=Vector3.Cross(tangent,axis).normalized,up=Vector3.Cross(tangent,right).normalized;
            float r=Mathf.Lerp(radiusStart,radiusEnd,i/(float)(path.Count-1));
            for(int j=0;j<=sides;j++)
            {
                float angle=2*Mathf.PI*j/sides;int a=i*stride+j;
                Vector3 normal=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                v[a]=path[i]+normal*r;normals[a]=normal;uv[a]=P(j/(float)sides,i/(float)(path.Count-1));
            }
        }
        for(int i=0;i<path.Count-1;i++)for(int j=0;j<sides;j++)
        {
            int a=i*stride+j,b=a+stride;Vector3 outwards=normals[a]+normals[a+1];
            Face(v,tri,a,b,a+1,outwards);Face(v,tri,a+1,b,b+1,outwards);
        }
        v[count]=path[0];v[count+1]=path[path.Count-1];
        normals[count]=(path[0]-path[1]).normalized;normals[count+1]=(path[path.Count-1]-path[path.Count-2]).normalized;
        uv[count]=uv[count+1]=P(.5f,.5f);
        for(int j=0;j<sides;j++)
        {Face(v,tri,count,j,j+1,normals[count]);int a=(path.Count-1)*stride+j;Face(v,tri,count+1,a,a+1,normals[count+1]);}
        return MeshData(v,normals,uv,tri);
    }
    private static Mesh LeafMesh()
    {
        const int rings=6,sides=32,stride=33,layer=(rings+1)*stride;
        Vector3[] v=new Vector3[layer*2],n=new Vector3[layer*2];Vector2[] uv=new Vector2[layer*2];List<int> tri=new List<int>();
        for(int face=0;face<2;face++)for(int ring=0;ring<=rings;ring++)for(int j=0;j<=sides;j++)
        {
            float r=ring/(float)rings,a=2*Mathf.PI*j/sides,x=r*Mathf.Cos(a),y=r*Mathf.Sin(a);
            int index=face*layer+ring*stride+j;
            v[index]=V(x,y,-.13f*(1-r*r)+(face==0?0:.025f));
            n[index]=V(.26f*x,.26f*y,-1).normalized*(face==0?1:-1);uv[index]=P(.5f+x*.5f,.5f+y*.5f);
        }
        for(int face=0;face<2;face++)for(int ring=0;ring<rings;ring++)for(int j=0;j<sides;j++)
        {
            int a=face*layer+ring*stride+j,b=a+stride;Vector3 normal=face==0?Vector3.back:Vector3.forward;
            Face(v,tri,a,b,a+1,normal);Face(v,tri,a+1,b,b+1,normal);
        }
        for(int j=0;j<sides;j++)
        {
            int a=rings*stride+j,b=a+layer;Vector3 outwards=V(v[a].x,v[a].y,0);
            Face(v,tri,a,a+1,b,outwards);Face(v,tri,a+1,b+1,b,outwards);
        }
        return MeshData(v,n,uv,tri);
    }
    private static void Face(Vector3[] v,List<int> tri,int a,int b,int c,Vector3 outward)
    {
        Vector3 cross=Vector3.Cross(v[b]-v[a],v[c]-v[a]);if(cross.sqrMagnitude<1e-22f)return;
        if(Vector3.Dot(cross,outward)<0){int swap=b;b=c;c=swap;}tri.Add(a);tri.Add(b);tri.Add(c);
    }
    private static Mesh MeshData(Vector3[] v,Vector3[] n,Vector2[] uv,List<int> tri)
    {
        Mesh m=new Mesh();if(v.Length>65535)m.indexFormat=IndexFormat.UInt32;
        m.vertices=v;m.normals=n;m.uv=uv;m.SetTriangles(tri,0);m.RecalculateBounds();m.RecalculateTangents();return m;
    }
}
#endif
