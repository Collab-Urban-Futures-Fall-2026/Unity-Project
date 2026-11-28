// Tools > Site B > Fix Moss & Stones Materials
// Creates URP Lit materials from the kit's 2k textures and remaps every material slot in the 4 FBX files.
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MossStonesMaterialFixer
{
    const string Root = "Assets/Models/Sit B/FBX/FBX/FBX/";
    const string Tex = Root + "textures/2k/";
    const string MatDir = Root + "Materials";

    [MenuItem("Tools/Site B/Fix Moss & Stones Materials")]
    public static void Fix()
    {
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Materials");

        SetNormal(Tex + "Moss_Normal.jpg");
        SetNormal(Tex + "Stones_Normal.jpg");

        var moss = MakeMat("Moss", Tex + "Moss_Color.png", Tex + "Moss_Normal.jpg", true);
        var stones = MakeMat("Stones", Tex + "Stones_Color.jpg", Tex + "Stones_Normal.jpg", false);

        string[] fbxs = System.IO.Directory.GetFiles(Root + "3d", "*.fbx")
            .Select(f => f.Replace('\\', '/')).ToArray();
        Debug.Log("[MossFix v2] FBX files: " + fbxs.Length);

        foreach (var path in fbxs)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var sm in r.sharedMaterials) if (sm != null) names.Add(sm.name);
            foreach (var mn in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()) names.Add(mn.name);
            Debug.Log($"[MossFix] {System.IO.Path.GetFileName(path)} material slots: " + string.Join(", ", names));
            foreach (var mName in names)
            {
                var m = new { name = mName };
                string n = m.name.ToLower();
                bool isMoss = n.Contains("moss") || n.Contains("scatter");
                bool isStone = n.Contains("stone") || n.Contains("rock");
                Material target = isMoss ? moss : isStone ? stones :
                    (path.ToLower().Contains("stones") ? stones : moss);
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), target);
                Debug.Log($"[MossFix] {System.IO.Path.GetFileName(path)}: '{m.name}' -> {target.name}");
            }
            imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();

        int fixedCount = 0;
        // Also fix instances already in the open scene (by mesh/object name)
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(r.gameObject);
            string sp = src != null ? AssetDatabase.GetAssetPath(src) : "";
            if (!sp.Replace('\\','/').Contains("Sit B/FBX/FBX/FBX/3d")) continue;
            fixedCount++;
            string n = (r.name + " " + (r.GetComponent<MeshFilter>()?.sharedMesh?.name ?? "")).ToLower();
            bool stone = n.Contains("stone") || n.Contains("rock") || sp.ToLower().Contains("stones");
            if (n.Contains("moss")) stone = false;
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++) arr[i] = stone ? stones : moss;
            Undo.RecordObject(r, "Moss fix");
            r.sharedMaterials = arr;
        }
        Debug.Log("[MossFix v2] Done. Scene renderers fixed: " + fixedCount);
    }

    static void SetNormal(string p)
    {
        var ti = AssetImporter.GetAtPath(p) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.NormalMap)
        { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
    }

    static Material MakeMat(string name, string color, string normal, bool cutout)
    {
        string p = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, p);
        }
        var c = AssetDatabase.LoadAssetAtPath<Texture2D>(color);
        var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
        m.SetTexture("_BaseMap", c);
        m.SetTexture("_MainTex", c);
        if (nm != null) { m.SetTexture("_BumpMap", nm); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Smoothness", 0.15f);
        if (cutout)
        {
            // Moss_Color.png carries alpha for the hanging strands
            m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.4f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0);
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
