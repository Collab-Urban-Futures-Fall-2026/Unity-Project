// Site B concept builder
// Menu: Tools > Site B > Build Concept On Selected Ground
// Select the "Design Site B" cube first. Units follow the team convention: 1 Unity unit = 1 ft.
// Layout (site local): +X = long edge (106.41 ft), +Z = bridge-wall side, -Z = street / fence side.
// Re-running deletes the previous "SiteB_Concept" and rebuilds. Ctrl+Z undoes a build.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SiteBConceptBuilder
{
    const string SitB = "Assets/Models/Sit B/";
    const string WallFbx = SitB + "Bridge wall/Bridge_Facade/Bridge_Facade.fbx";
    const string Folly = SitB + "Folly structure/wood_folly.blend";
    const string Truck = SitB + "food_truck/food-truck-3c4e0e.fbx";
    const string SeatCW = SitB + "seats_3styles/concrete_wood_seat1.fbx";
    const string SeatC = SitB + "seats_3styles/all_concrete_seat1.fbx";
    const string SeatW = SitB + "seats_3styles/all_wood_seat.fbx";
    const string Trash = SitB + "trashcan/city-trash-can/source/trash can exp.blend";
    const string Tree = "Assets/Supercyan Free Forest Sample/Prefabs/High Quality/Tree/Leaf/Normal/forestpack_tree_1_leaf_1.prefab";
    const string Grass1 = "Assets/Supercyan Free Forest Sample/Prefabs/High Quality/Foliage/Grass/forestpack_foliage_grassPatch_small_1.prefab";
    const string Grass2 = "Assets/Supercyan Free Forest Sample/Prefabs/High Quality/Foliage/Grass/forestpack_foliage_grassPatch_small_2.prefab";
    const string FenceSection = "Assets/Fence Modular System/Prefabs/sectionB_V1_PREFAB.prefab";
    const string FenceGate = "Assets/Fence Modular System/Prefabs/double_gate_V1_PREFAB.prefab";
    const string MatFolder = SitB + "Generated";

    // Set to 0 or 180 to choose which long edge gets the bridge wall.
    const float FlipYaw = 180f;
    // 0 or 180: which way the mural face of Bridge_Facade points.
    const float WallYaw = 0f;
    static Transform root;
    static Material concrete, wood, metal, paving, soil;

    [MenuItem("Tools/Site B/Build Concept On Selected Ground")]
    public static void Build()
    {
        Transform site = Selection.activeTransform;
        if (site != null && (site.name == "SiteB_Concept" || site.GetComponent<Renderer>() == null || site.IsChildOf(GameObject.Find("SiteB_Concept")?.transform)))
            site = null;
        if (site == null)
        {
            var go = GameObject.Find("Design Site B");
            if (go != null) site = go.transform;
        }
        if (site == null)
        {
            EditorUtility.DisplayDialog("Site B", "Select the Site B ground cube first.", "OK");
            return;
        }

        float L = Mathf.Abs(site.lossyScale.x);   // long edge (ft)
        float W = Mathf.Abs(site.lossyScale.z);   // short edge (ft)
        float hx = L / 2f, hz = W / 2f;
        float top = site.position.y + Mathf.Abs(site.lossyScale.y) / 2f;

        var old = GameObject.Find("SiteB_Concept");
        if (old != null) Undo.DestroyObjectImmediate(old);

        MakeMaterials();
        root = new GameObject("SiteB_Concept").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Site B Concept");

        var r = site.GetComponent<Renderer>();
        if (r != null) { Undo.RecordObject(r, "Site B paving"); r.sharedMaterial = paving; }

        // ---- 1. Bridge wall for art installation (+Z edge)
        var wallGrp = Group("Wall for Art Installation");
        var wall = Place(WallFbx, wallGrp, new Vector3(0, 0, hz), WallYaw, L, true, true);
        if (wall != null)
        {
            var b = Bounds(wall);
            wall.transform.position += new Vector3(0, 0, hz + b.extents.z - b.center.z);
        }

        // ---- 2. Planters + trees + benches along the wall
        var wallSide = Group("Wall Planters & Benches");
        float wz = hz - 6f;
        foreach (float x in new[] { -34f, -12f, 8f })
        {
            Box("Planter", wallSide, new Vector3(x, 0, wz), new Vector3(14, 2.5f, 5), 0, concrete);
            Box("Soil", wallSide, new Vector3(x, 2.5f, wz), new Vector3(13, 0.1f, 4), 0, soil);
            Place(Tree, wallSide, new Vector3(x - 3, 2.5f, wz), 0, 16, false);
            Place(Grass1, wallSide, new Vector3(x + 3, 2.5f, wz), 0, 1.5f, false);
            Place(Grass2, wallSide, new Vector3(x + 5, 2.5f, wz), 40, 1.5f, false);
            Place(SeatCW, wallSide, new Vector3(x + 10, 0, wz + 0.5f), 0, 8, true, true);
        }

        // ---- 3. Folly structure (with lighting) – front left
        var follyGrp = Group("Folly Structure");
        var folly = Place(Folly, follyGrp, new Vector3(-38, 0, -12), 0, 11, false);
        if (folly != null)
        {
            var fb = Bounds(folly);
            AddLight(follyGrp, new Vector3(fb.center.x, fb.max.y - 1f, fb.center.z), 25, 3f);
        }
        Place(SeatW, follyGrp, new Vector3(-38, 0, -12), 90, 6, true, true);
        Place(Grass1, follyGrp, new Vector3(-44, 0, -6), 0, 1.5f, false);
        Place(Grass2, follyGrp, new Vector3(-32, 0, -18), 0, 1.5f, false);

        // ---- 4. Skate zone (ledges, rail, manual pad, launch ramp)
        var skate = Group("Skate Elements");
        Box("Ledge A", skate, new Vector3(-12, 0, 6), new Vector3(12, 1.6f, 3), 0, concrete);
        Box("Ledge B", skate, new Vector3(4, 0, 14), new Vector3(10, 1.6f, 3), 0, concrete);
        Box("Manual Pad", skate, new Vector3(16, 0, 6), new Vector3(8, 0.8f, 5), 0, concrete);
        var rail = Group("Grind Rail", skate);
        rail.localPosition = new Vector3(-2, 0, -2);
        rail.localRotation = Quaternion.Euler(0, -15, 0);
        Box("Rail Bar", rail, new Vector3(0, 1.3f, 0), new Vector3(16, 0.15f, 0.15f), 0, metal);
        Box("Rail Post L", rail, new Vector3(-7.5f, 0, 0), new Vector3(0.15f, 1.3f, 0.15f), 0, metal);
        Box("Rail Post R", rail, new Vector3(7.5f, 0, 0), new Vector3(0.15f, 1.3f, 0.15f), 0, metal);
        Box("Launch Deck", skate, new Vector3(40, 0, -6), new Vector3(10, 3, 6), 0, concrete);
        var ramp = Box("Launch Ramp", skate, Vector3.zero, new Vector3(10, 0.4f, 9.4f), 0, concrete);
        ramp.transform.localRotation = Quaternion.Euler(-18, 0, 0);
        ramp.transform.localPosition = new Vector3(40, 1.45f, -6 - 3 - 4.47f);

        // ---- 5. Centre planter with block benches (High Line style)
        var centre = Group("Block Benches");
        Box("Centre Planter", centre, new Vector3(20, 0, -10), new Vector3(12, 2, 7), 0, concrete);
        Box("Soil", centre, new Vector3(20, 2, -10), new Vector3(11, 0.1f, 6), 0, soil);
        Place(Tree, centre, new Vector3(20, 2, -10), 0, 17, false);
        Place(SeatC, centre, new Vector3(20, 0, -15.5f), 0, 10, true, true);
        Place(SeatCW, centre, new Vector3(11, 0, -10), 90, 7, true, true);
        Place(SeatC, centre, new Vector3(-10, 0, -20), 0, 10, true, true);
        Place(SeatW, centre, new Vector3(2, 0, -24), 0, 8, true, true);
        Place(Grass1, centre, new Vector3(17, 2, -9), 0, 1.5f, false);

        // ---- 6. Food trucks (back right)
        var trucks = Group("Food Trucks");
        var coffee = Place(Truck, trucks, new Vector3(28, 0, 21), 0, 20, true, true);
        if (coffee != null) coffee.name = "Coffee Truck";
        var burger = Place(Truck, trucks, new Vector3(44, 0, 14), -30, 20, true, true);
        if (burger != null) burger.name = "Burger Truck";

        // ---- 7. Fence along street (-Z) and the left end, with a double gate (dog + human)
        var fence = Group("Fence");
        FenceRun(fence, new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), -30f);
        FenceRun(fence, new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz - 1f), float.NaN);

        // ---- 8. Trees + planting along the fence
        var green = Group("Trees & Planting");
        foreach (var p in new[] { new Vector3(-46, 0, -27), new Vector3(-4, 0, -28), new Vector3(34, 0, -27) })
        {
            Place(Tree, green, p, Random.Range(0, 360), Random.Range(16f, 20f), false);
            Place(Grass1, green, p + new Vector3(3, 0, 1), 0, 1.5f, false);
            Place(Grass2, green, p + new Vector3(-3, 0, 1), 0, 1.5f, false);
        }

        // ---- 9. Street lamps + trash cans
        var furn = Group("Lamps & Trash Cans");
        foreach (float x in new[] { -25f, 15f, 45f }) Lamp(furn, new Vector3(x, 0, -hz + 2.5f));
        Place(Trash, furn, new Vector3(-20, 0, -hz + 3), 0, 3.5f, false);
        Place(Trash, furn, new Vector3(48, 0, -hz + 3), 0, 3.5f, false);

        // ---- Move the whole build onto the selected ground
        root.SetPositionAndRotation(new Vector3(site.position.x, top, site.position.z),
                                    Quaternion.Euler(0, site.eulerAngles.y + FlipYaw, 0));

        Selection.activeGameObject = root.gameObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log($"Site B concept built on '{site.name}' ({L:0.##} x {W:0.##} ft).");
    }

    // ---------- helpers ----------
    static Transform Group(string name, Transform parent = null)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent != null ? parent : root, false);
        return g;
    }

    // Root is at world origin with no rotation while building, so world bounds == root-local bounds.
    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // Instantiates an asset, scales it uniformly so its height (or longest horizontal side) = size,
    // then sits its bottom-centre on pos. alignX rotates it so its long side runs along local X first.
    static GameObject Place(string path, Transform parent, Vector3 pos, float yaw, float size, bool byLength, bool alignX = false)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) { Debug.LogWarning("Site B: missing asset " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        var b = Bounds(go);
        float extra = 0;
        if (alignX && b.size.z > b.size.x) extra = 90;
        go.transform.localRotation = Quaternion.Euler(0, yaw + extra, 0);

        b = Bounds(go);
        float cur = byLength ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
        if (cur > 0.0001f) go.transform.localScale = Vector3.one * (size / cur);

        b = Bounds(go);
        Vector3 target = parent.TransformPoint(pos);
        go.transform.position += target - new Vector3(b.center.x, b.min.y, b.center.z);
        return go;
    }

    static GameObject Box(string name, Transform parent, Vector3 basePos, Vector3 size, float yaw, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = size;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localPosition = basePos + new Vector3(0, size.y / 2f, 0);
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static void AddLight(Transform parent, Vector3 worldPos, float range, float intensity)
    {
        var lgo = new GameObject("Light");
        lgo.transform.SetParent(parent, true);
        lgo.transform.position = worldPos;
        var l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = new Color(1f, 0.85f, 0.6f);
    }

    static void Lamp(Transform parent, Vector3 pos)
    {
        var g = Group("Street Lamp", parent);
        g.localPosition = pos;
        Box("Pole", g, Vector3.zero, new Vector3(0.4f, 12, 0.4f), 0, metal);
        Box("Head", g, new Vector3(0, 12, 0), new Vector3(1.2f, 0.6f, 1.2f), 0, metal);
        AddLight(g, g.TransformPoint(new Vector3(0, 11.5f, 0)), 30, 2f);
    }

    static void FenceRun(Transform parent, Vector3 a, Vector3 b, float gateX)
    {
        var run = Group("Fence Run", parent);
        Vector3 dir = (b - a).normalized;
        float len = Vector3.Distance(a, b);
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90f;

        // measure one section at 4.5 ft high
        var probe = Place(FenceSection, run, Vector3.zero, 0, 4.5f, false);
        if (probe == null) return;
        var pb = Bounds(probe);
        float seg = Mathf.Max(pb.size.x, pb.size.z);
        bool sectionAlongZ = pb.size.z > pb.size.x;
        Object.DestroyImmediate(probe);
        if (seg < 0.5f) return;

        float gateHalf = 0;
        if (!float.IsNaN(gateX))
        {
            var gate = Place(FenceGate, run, Vector3.zero, yaw + (sectionAlongZ ? 90 : 0), 4.5f, false);
            if (gate != null)
            {
                gate.name = "Double Gate (dog + human)";
                var gb = Bounds(gate);
                gateHalf = Mathf.Max(gb.size.x, gb.size.z) / 2f;
                Vector3 gp = new Vector3(gateX, a.y, a.z);
                gate.transform.position += gp - new Vector3(gb.center.x, gb.min.y, gb.center.z);
            }
        }

        for (float d = seg / 2f; d <= len - seg / 2f + 0.01f; d += seg)
        {
            Vector3 p = a + dir * d;
            if (gateHalf > 0 && Mathf.Abs(p.x - gateX) < gateHalf + seg / 2f) continue;
            Place(FenceSection, run, p, yaw + (sectionAlongZ ? 90 : 0), 4.5f, false);
        }
    }

    static void MakeMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MatFolder))
            AssetDatabase.CreateFolder("Assets/Models/Sit B", "Generated");
        concrete = Mat("SiteB_Concrete", new Color(0.78f, 0.77f, 0.74f), 0.2f);
        paving = Mat("SiteB_Paving", new Color(0.86f, 0.84f, 0.80f), 0.15f);
        wood = Mat("SiteB_Wood", new Color(0.62f, 0.43f, 0.26f), 0.3f);
        metal = Mat("SiteB_Metal", new Color(0.12f, 0.12f, 0.12f), 0.5f);
        soil = Mat("SiteB_Soil", new Color(0.25f, 0.18f, 0.12f), 0.05f);
    }

    static Material Mat(string name, Color c, float smooth)
    {
        string path = MatFolder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
