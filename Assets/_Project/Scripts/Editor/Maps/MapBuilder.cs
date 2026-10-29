using System.Collections.Generic;
using System.IO;
using System.Linq;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Dựng scene map từ MapSpec (LEVEL_DESIGN 6): địa hình Tiny Swords tự ghép mép (cỏ trên cát trên nước,
/// khối đá nhô có vách + bóng đổ), vật thể y-sort, clutter rải theo seed, cơ chế/NPC/địch theo ký hiệu,
/// ánh sáng 2D theo vùng, hạt môi trường, NavMesh 2D. Scene gốc nhân bản từ World.unity để giữ nguyên
/// player/camera/UI đã cấu hình.
/// Batch: Unity -batchmode -quit -executeMethod MapBuilder.BuildAll [-snapDir dir]
/// </summary>
public static class MapBuilder
{
    public const string MapFolder = "Assets/_Project/Scenes/Maps";
    private const string TemplateScene = "Assets/_Project/Scenes/World.unity";
    private const string MenuScene = "Assets/_Project/Scenes/MainMenu.unity";

    private static readonly Dictionary<string, string> EnemyPrefabs = new Dictionary<string, string>
    {
        { "Goblin", "Assets/_Project/Art/animation/Goblin/Goblin.prefab" },
        { "Archer", "Assets/_Project/Art/animation/Archer/Archer.prefab" },
        { "Lancer", "Assets/_Project/Art/animation/Lancer/Lancer.prefab" },
        { "Sheep", "Assets/_Project/Prefabs/Enemy_Sheep.prefab" },
    };

    // Thứ tự vẽ trong sorting layer "Ground"
    private const int OrderWater = -20, OrderFoam = -15, OrderSand = 0, OrderGrass = 2, OrderMud = 3,
        OrderShadow = 4, OrderElevation = 5, OrderElevationGrass = 6, OrderFace = 7, OrderFlat = 9, OrderClutter = 10;

    [MenuItem("Tools/RPG/Maps/Build All Maps")]
    public static void BuildAll()
    {
        MapArt.Reset();
        MapArt.EnsureFolder(MapFolder);
        RenderSetup.EnsureAll();
        var built = new List<string>();
        foreach (var spec in MapSpecs.All())
        {
            built.Add(Build(spec));
        }
        SetupMenuScene();
        SetBuildSettings(built);
        AssetDatabase.SaveAssets();

        string snapDir = Arg("-snapDir");
        if (!string.IsNullOrEmpty(snapDir))
            foreach (var spec in MapSpecs.All()) Snapshot(spec, snapDir);
        Debug.Log("[MapBuilder] Xong: " + string.Join(", ", built));
    }

    // ═════════════════════════════════════════ BUILD ═════════════════════════════════════════
    public static string Build(MapSpec spec)
    {
        string path = $"{MapFolder}/{spec.sceneName}.unity";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(TemplateScene, path);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        StripTemplate(scene);

        var ctx = new Ctx(spec);
        ctx.Parse();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        BuildTerrain(ctx, grid.transform);
        var props = new GameObject("Props").transform;
        var gameplay = new GameObject("Gameplay").transform;
        BuildMarkers(ctx, props, gameplay);
        BuildClutter(ctx, props);
        BuildLightingAndAmbience(ctx);
        PlacePlayerAndCamera(ctx);
        BakeNavMesh(ctx, grid.transform, path);
        RenderSetup.UpgradeOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MapBuilder] {spec.sceneName}: {spec.Width}×{spec.Height} tile, {ctx.propCount} vật thể, {ctx.clutterCount} clutter");
        return path;
    }

    private static void StripTemplate(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            bool isCharacter = root.GetComponentInChildren<Character>(true) != null;
            bool keep = root.GetComponentInChildren<PlayerController>(true) != null
                        || root.GetComponent<Camera>() != null
                        || (root.GetComponent<Canvas>() != null && !isCharacter) // canvas UI (không phải thanh máu của quái)
                        || root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null
                        || root.name.Contains("GameManager") || root.name.Contains("UIManager")
                        || (root.GetComponent<Light2D>() != null && root.GetComponent<Light2D>().lightType == Light2D.LightType.Global);
            if (!keep) Object.DestroyImmediate(root);
        }
    }

    // ───────── Phân tích lưới ─────────
    private class Ctx
    {
        public readonly MapSpec spec;
        public int W, H;
        public char[,] c;                       // [x, y] — y = 0 ở đáy
        public bool[,] ground, plateau, water, gap, face, blocked, navBlock, occupied;
        public System.Random rng;
        public int propCount, clutterCount;
        public readonly Dictionary<char, int> counters = new Dictionary<char, int>();
        public Transform player;
        public Vector3 start;
        public readonly Dictionary<string, GameObject> chests = new Dictionary<string, GameObject>();
        public readonly List<GameObject> arenaLocks = new List<GameObject>();

        public Ctx(MapSpec s) { spec = s; rng = new System.Random(s.seed); }

        public bool Tower => spec.theme == MapSpec.Theme.Tower;
        public char At(int x, int y) => x < 0 || y < 0 || x >= W || y >= H ? '~' : c[x, y];
        public bool In(bool[,] m, int x, int y) => x >= 0 && y >= 0 && x < W && y < H && m[x, y];
        public Vector3 Center(int x, int y) => new Vector3(x + 0.5f, y + 0.5f, 0f);
        public int Next(char k) { counters.TryGetValue(k, out int n); counters[k] = n + 1; return n; }
        public float Jitter(float a) => (float)(rng.NextDouble() * 2 - 1) * a;

        public void Parse()
        {
            H = spec.rows.Length;
            W = spec.rows.Max(r => r.Length);
            c = new char[W, H];
            ground = new bool[W, H]; plateau = new bool[W, H]; water = new bool[W, H]; gap = new bool[W, H];
            face = new bool[W, H]; blocked = new bool[W, H]; navBlock = new bool[W, H]; occupied = new bool[W, H];
            for (int row = 0; row < H; row++)
            {
                string line = spec.rows[row];
                int y = H - 1 - row;
                for (int x = 0; x < W; x++)
                {
                    char ch = x < line.Length ? line[x] : '~';
                    c[x, y] = ch;
                    water[x, y] = ch == '~';
                    gap[x, y] = ch == 'v';
                    plateau[x, y] = ch == '#';
                    ground[x, y] = !water[x, y] && !gap[x, y];
                }
            }
            // Vách đá: ô ngay dưới mép đáy khối đá nhô
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    if (!plateau[x, y] || In(plateau, x, y - 1) || y == 0) continue;
                    face[x, y - 1] = true;
                }
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    blocked[x, y] = water[x, y] || plateau[x, y] || face[x, y];
        }
    }

    // ═════════════════════════════════════════ TERRAIN ═════════════════════════════════════════
    private static (int col, int row) Auto(bool n, bool s, bool e, bool w)
    {
        bool singleCol = !e && !w, singleRow = !n && !s;
        int col = singleCol ? 3 : (w && e ? 1 : (e ? 0 : 2));
        int row = singleRow ? 3 : (n && s ? 1 : (s ? 0 : 2));
        return (col, row);
    }

    private static Tilemap NewTilemap(Transform grid, string name, string layer, int order, bool render = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingLayerName = layer;
        r.sortingOrder = order;
        r.enabled = render;
        return tm;
    }

    private static void AddNavModifier(Tilemap tm, int area)
    {
        var mod = tm.gameObject.AddComponent<NavMeshModifier>();
        mod.overrideArea = true;
        mod.area = area;
    }

    private static void BuildTerrain(Ctx ctx, Transform grid)
    {
        var spec = ctx.spec;
        int margin = 14;

        // 1. Nước nền (ngoài trời) — tháp dùng màu nền camera làm vực
        if (!ctx.Tower)
        {
            var water = NewTilemap(grid, "00_Water", "Ground", OrderWater);
            water.color = spec.waterTint;
            var wt = MapArt.TileFor(MapArt.Water());
            for (int x = -margin; x < ctx.W + margin; x++)
                for (int y = -margin; y < ctx.H + margin; y++)
                    water.SetTile(new Vector3Int(x, y, 0), wt);

            // Bọt sóng quanh mép đất (animated tile)
            var foam = NewTilemap(grid, "01_Foam", "Ground", OrderFoam);
            foam.animationFrameRate = 1f;
            var ft = MapArt.AnimatedTileFor("foam_anim", MapArt.Foam(), 7f);
            for (int x = 0; x < ctx.W; x++)
                for (int y = 0; y < ctx.H; y++)
                {
                    if (!ctx.ground[x, y]) continue;
                    bool coast = !ctx.In(ctx.ground, x + 1, y) || !ctx.In(ctx.ground, x - 1, y)
                                 || !ctx.In(ctx.ground, x, y + 1) || !ctx.In(ctx.ground, x, y - 1);
                    if (coast) foam.SetTile(new Vector3Int(x, y, 0), ft);
                }
        }

        // 2. Nền phẳng: cát (ngoài trời) / sàn đá (tháp)
        var baseMap = NewTilemap(grid, "02_Ground", "Ground", OrderSand);
        AddNavModifier(baseMap, 0);
        string baseSheet = ctx.Tower ? MapArt.ElevationSheet : MapArt.FlatSheet;
        int baseColOffset = ctx.Tower ? 0 : 5;
        for (int x = 0; x < ctx.W; x++)
            for (int y = 0; y < ctx.H; y++)
            {
                if (!ctx.ground[x, y]) continue;
                var (col, row) = Auto(ctx.In(ctx.ground, x, y + 1), ctx.In(ctx.ground, x, y - 1),
                    ctx.In(ctx.ground, x + 1, y), ctx.In(ctx.ground, x - 1, y));
                if (ctx.Tower && row == 3) row = 4; // hàng đơn của sheet đá nằm ở hàng 4
                baseMap.SetTile(new Vector3Int(x, y, 0), MapArt.TileFor(MapArt.SheetCell(baseSheet, col + baseColOffset, row)));
            }

        // Tháp: mép sàn nổi trên vực có "bề dày" (vách) bên dưới
        if (ctx.Tower)
        {
            var rim = NewTilemap(grid, "02b_FloorRim", "Ground", OrderFace);
            for (int x = 0; x < ctx.W; x++)
                for (int y = 0; y < ctx.H; y++)
                {
                    if (!ctx.ground[x, y] || ctx.In(ctx.ground, x, y - 1)) continue;
                    var (col, row) = Auto(ctx.In(ctx.ground, x, y + 1), false,
                        ctx.In(ctx.ground, x + 1, y), ctx.In(ctx.ground, x - 1, y));
                    int faceRow = row == 3 ? 5 : 3;
                    rim.SetTile(new Vector3Int(x, y - 1, 0), MapArt.TileFor(MapArt.SheetCell(MapArt.ElevationSheet, col, faceRow)));
                }
        }

        // 3. Cỏ phủ lên cát, lùi ≥ 1 ô khỏi nước để luôn có bãi cát viền
        if (!ctx.Tower)
        {
            var grassMap = NewTilemap(grid, "03_Grass", "Ground", OrderGrass);
            var grass = new bool[ctx.W, ctx.H];
            for (int x = 0; x < ctx.W; x++)
                for (int y = 0; y < ctx.H; y++)
                {
                    char ch = ctx.c[x, y];
                    if (!ctx.ground[x, y] || IsSandCell(ctx, x, y) || ch == '#') continue;
                    bool inland = true;
                    for (int dx = -1; dx <= 1 && inland; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (!ctx.In(ctx.ground, x + dx, y + dy)) { inland = false; break; }
                    grass[x, y] = inland;
                }
            for (int x = 0; x < ctx.W; x++)
                for (int y = 0; y < ctx.H; y++)
                {
                    if (!grass[x, y]) continue;
                    var (col, row) = Auto(ctx.In(grass, x, y + 1), ctx.In(grass, x, y - 1),
                        ctx.In(grass, x + 1, y), ctx.In(grass, x - 1, y));
                    grassMap.SetTile(new Vector3Int(x, y, 0), MapArt.TileFor(MapArt.SheetCell(MapArt.FlatSheet, col, row)));
                }
        }

        // 4. Bùn (overlay động)
        var mudMap = NewTilemap(grid, "04_Mud", "Ground", OrderMud);
        var mudTile = MapArt.AnimatedTileFor("mud_anim", new[] { MapArt.Generated("mud_0", false), MapArt.Generated("mud_1", false) }, 1.5f);
        for (int x = 0; x < ctx.W; x++)
            for (int y = 0; y < ctx.H; y++)
                if (ctx.c[x, y] == 'm') mudMap.SetTile(new Vector3Int(x, y, 0), mudTile);

        // 5. Khối đá nhô: bóng đổ, đỉnh đá, cỏ trên đỉnh, vách
        var shadowMap = NewTilemap(grid, "05_Shadow", "Ground", OrderShadow);
        shadowMap.color = new Color(1f, 1f, 1f, 0.55f);
        var elevMap = NewTilemap(grid, "06_Elevation", "Ground", OrderElevation);
        var elevGrass = NewTilemap(grid, "07_ElevationGrass", "Ground", OrderElevationGrass);
        var faceMap = NewTilemap(grid, "08_Cliff", "Ground", OrderFace);
        var shadowTile = MapArt.TileFor(MapArt.Shadow());
        for (int x = 0; x < ctx.W; x++)
            for (int y = 0; y < ctx.H; y++)
            {
                if (!ctx.plateau[x, y]) continue;
                bool n = ctx.In(ctx.plateau, x, y + 1), s = ctx.In(ctx.plateau, x, y - 1);
                bool e = ctx.In(ctx.plateau, x + 1, y), w = ctx.In(ctx.plateau, x - 1, y);
                var (col, row) = Auto(n, s, e, w);
                bool singleRow = row == 3;
                elevMap.SetTile(new Vector3Int(x, y, 0),
                    MapArt.TileFor(MapArt.SheetCell(MapArt.ElevationSheet, col, singleRow ? 4 : row)));
                if (!s && y > 0)
                {
                    faceMap.SetTile(new Vector3Int(x, y - 1, 0),
                        MapArt.TileFor(MapArt.SheetCell(MapArt.ElevationSheet, col, singleRow ? 5 : 3)));
                    shadowMap.SetTile(new Vector3Int(x, y - 1, 0), shadowTile);
                }
                if (!ctx.Tower)
                {
                    bool inner = true;
                    for (int dx = -1; dx <= 1 && inner; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (!ctx.In(ctx.plateau, x + dx, y + dy)) { inner = false; break; }
                    if (inner)
                    {
                        bool gn = InnerPlateau(ctx, x, y + 1), gs = InnerPlateau(ctx, x, y - 1);
                        bool ge = InnerPlateau(ctx, x + 1, y), gw = InnerPlateau(ctx, x - 1, y);
                        var (gc, gr) = Auto(gn, gs, ge, gw);
                        elevGrass.SetTile(new Vector3Int(x, y, 0), MapArt.TileFor(MapArt.SheetCell(MapArt.FlatSheet, gc, gr)));
                    }
                }
            }

        // 6. Va chạm (ẩn): nước, vực, khối đá, vách + vành ngoài map
        var col2d = NewTilemap(grid, "90_Collision", "Ground", 0, render: false);
        var blockTile = MapArt.TileFor(MapArt.Water(), Tile.ColliderType.Grid);
        for (int x = -2; x < ctx.W + 2; x++)
            for (int y = -2; y < ctx.H + 2; y++)
            {
                bool inside = x >= 0 && y >= 0 && x < ctx.W && y < ctx.H;
                if (!inside || ctx.blocked[x, y]) col2d.SetTile(new Vector3Int(x, y, 0), blockTile);
            }
        var tmc = col2d.gameObject.AddComponent<TilemapCollider2D>();
        var rb = col2d.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        col2d.gameObject.AddComponent<CompositeCollider2D>();
        tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
        AddNavModifier(col2d, 1);

        // 7. Khe vực / bùn: vùng trigger theo khối liền nhau
        foreach (var region in Regions(ctx, 'v'))
            AddZone<FallHazard>(grid.parent, "Gap", region);
        // Bùn hình dạng tự do: mỗi dải ngang 1 SlowZone (nhiều nguồn chồng nhau → lấy chậm nhất)
        foreach (var run in Runs(ctx, 'm'))
            AddZone<SlowZone>(grid.parent, "Mud", run);
    }

    /// <summary>Ô cát/đường: ',' 'm', hoặc ký hiệu vật thể nằm giữa đường (đa số láng giềng là ',').</summary>
    private static bool IsSandCell(Ctx ctx, int x, int y)
    {
        char ch = ctx.c[x, y];
        if (ch == ',' || ch == 'm') return true;
        if (ch == '.' || ch == '#' || ch == '~' || ch == 'v') return false;
        int sand = 0, grass = 0;
        foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
        {
            char n = ctx.At(x + d.x, y + d.y);
            if (n == ',' || n == 'm') sand++;
            else if (n == '.') grass++;
        }
        return sand > grass;
    }

    private static bool InnerPlateau(Ctx ctx, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (!ctx.In(ctx.plateau, x + dx, y + dy)) return false;
        return true;
    }

    private static List<RectInt> Regions(Ctx ctx, char ch)
    {
        var seen = new bool[ctx.W, ctx.H];
        var result = new List<RectInt>();
        for (int x = 0; x < ctx.W; x++)
            for (int y = 0; y < ctx.H; y++)
            {
                if (seen[x, y] || ctx.c[x, y] != ch) continue;
                int minX = x, maxX = x, minY = y, maxY = y;
                var q = new Queue<Vector2Int>();
                q.Enqueue(new Vector2Int(x, y));
                seen[x, y] = true;
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                    foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        var n = p + d;
                        if (n.x < 0 || n.y < 0 || n.x >= ctx.W || n.y >= ctx.H || seen[n.x, n.y] || ctx.c[n.x, n.y] != ch) continue;
                        seen[n.x, n.y] = true;
                        q.Enqueue(n);
                    }
                }
                result.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }
        return result;
    }

    private static List<RectInt> Runs(Ctx ctx, char ch)
    {
        var result = new List<RectInt>();
        for (int y = 0; y < ctx.H; y++)
            for (int x = 0; x < ctx.W; x++)
            {
                if (ctx.c[x, y] != ch || (x > 0 && ctx.c[x - 1, y] == ch)) continue;
                int len = 1;
                while (x + len < ctx.W && ctx.c[x + len, y] == ch) len++;
                result.Add(new RectInt(x, y, len, 1));
            }
        return result;
    }

    private static void AddZone<T>(Transform parent, string name, RectInt r) where T : Component
    {
        var go = new GameObject($"{name}_{r.x}_{r.y}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(r.x + r.width * 0.5f, r.y + r.height * 0.5f, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(r.width - 0.1f, r.height - 0.1f);
        go.AddComponent<T>();
    }

    // ═════════════════════════════════════════ PROPS & GAMEPLAY ═════════════════════════════════════════
    private static GameObject Sprite(Transform parent, string name, Sprite sprite, Vector3 pos, bool ySort = true, int flatOrder = OrderFlat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (ySort)
        {
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 1000 - Mathf.RoundToInt(pos.y * 100f);
        }
        else
        {
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = flatOrder;
        }
        return go;
    }

    private static void Animate(GameObject go, Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length < 2) return;
        var loop = go.AddComponent<SpriteLoop>();
        loop.frames = frames;
        loop.fps = fps;
    }

    private static void Solid(GameObject go, float radius, float offsetY = 0.1f)
    {
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = radius;
        c.offset = new Vector2(0f, offsetY);
    }

    private static void Box(GameObject go, Vector2 size, Vector2 offset, bool trigger = false)
    {
        var b = go.AddComponent<BoxCollider2D>();
        b.size = size;
        b.offset = offset;
        b.isTrigger = trigger;
    }

    private static void Set(Object o, string field, object value)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[MapBuilder] {o.GetType().Name} không có field {field}"); return; }
        switch (value)
        {
            case string s: p.stringValue = s; break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case Vector2 v: p.vector2Value = v; break;
            case Object obj: p.objectReferenceValue = obj; break;
            case Object[] arr:
                p.arraySize = arr.Length;
                for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ItemData Item(string id) =>
        AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Resources/Items" })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(i => i != null && i.itemId == id);

    private static Light2D PointLight(Transform parent, Vector3 pos, Color color, float intensity, float radius)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.25f;
        l.falloffIntensity = 0.6f;
        TargetAllLayersSerialized(l);
        return l;
    }

    private static void TargetAllLayersSerialized(Light2D l)
    {
        var so = new SerializedObject(l);
        var p = so.FindProperty("m_ApplyToSortingLayers");
        var layers = SortingLayer.layers;
        p.arraySize = layers.Length;
        for (int i = 0; i < layers.Length; i++) p.GetArrayElementAtIndex(i).intValue = layers[i].id;
        so.ApplyModifiedPropertiesWithoutUndo();
        l.enabled = false;
        l.enabled = true;
    }

    private static void BuildMarkers(Ctx ctx, Transform props, Transform gameplay)
    {
        var spec = ctx.spec;
        // Đọc theo thứ tự hàng (bắc → nam), trái → phải: khớp thứ tự danh sách trong spec
        for (int y = ctx.H - 1; y >= 0; y--)
            for (int x = 0; x < ctx.W; x++)
            {
                char ch = ctx.c[x, y];
                Vector3 p = ctx.Center(x, y);
                Vector3 baseP = new Vector3(p.x, y + 0.2f, 0f);
                switch (ch)
                {
                    case 'T': PlaceTree(ctx, props, x, y, big: true); break;
                    case 't': PlaceTree(ctx, props, x, y, big: false); break;
                    case 'b':
                    {
                        var frames = MapArt.Bush(1 + ctx.rng.Next(4));
                        var go = Sprite(props, "Bush", frames[0], baseP + new Vector3(ctx.Jitter(0.2f), ctx.Jitter(0.15f)));
                        Animate(go, frames, 7f);
                        Solid(go, 0.3f, 0.15f);
                        ctx.navBlock[x, y] = true; ctx.propCount++;
                        break;
                    }
                    case 'r':
                    {
                        var go = Sprite(props, "Rock", MapArt.Rock(1 + ctx.rng.Next(4)), baseP);
                        Solid(go, 0.3f, 0.15f);
                        ctx.navBlock[x, y] = true; ctx.propCount++;
                        break;
                    }
                    case 'H':
                    {
                        var go = Sprite(props, "House", MapArt.Building("House" + (1 + ctx.rng.Next(3))), new Vector3(p.x, y, 0f));
                        Box(go, new Vector2(1.7f, 1.2f), new Vector2(0f, 0.6f));
                        // Cửa sổ sáng ấm
                        PointLight(go.transform, new Vector3(p.x, y + 1.0f, 0f), new Color(1f, 0.72f, 0.35f), 0.7f, 2.2f);
                        for (int dx = -1; dx <= 1; dx++) Mark(ctx, x + dx, y);
                        ctx.propCount++;
                        break;
                    }
                    case 'W':
                    {
                        var go = Sprite(props, "Watchtower", MapArt.Building("Tower"), new Vector3(p.x, y, 0f));
                        Box(go, new Vector2(1.6f, 1.2f), new Vector2(0f, 0.6f));
                        for (int dx = -1; dx <= 1; dx++) Mark(ctx, x + dx, y);
                        ctx.propCount++;
                        break;
                    }
                    case 's':
                    {
                        var go = Sprite(props, "Scarecrow", MapArt.Deco(18), baseP);
                        Solid(go, 0.25f); ctx.propCount++;
                        break;
                    }
                    case 'p': Sprite(props, "Pumpkin", MapArt.Deco(12 + ctx.rng.Next(2)), baseP); ctx.propCount++; break;
                    case 'k': { var go = Sprite(props, "SkullSign", MapArt.Deco(16), baseP); Solid(go, 0.2f); ctx.propCount++; break; }
                    case 'g': BuildSign(ctx, props, baseP); break;
                    case 'L':
                    {
                        var go = Sprite(props, "Lantern", MapArt.Generated("lantern_post"), baseP);
                        Solid(go, 0.15f);
                        PointLight(go.transform, baseP + new Vector3(0.3f, 1.3f, 0f), new Color(1f, 0.7f, 0.3f), 1.1f, 4f);
                        ctx.propCount++;
                        break;
                    }
                    case 'S':
                    {
                        ctx.start = p;
                        var sp = new GameObject("Spawn_" + spec.startSpawnId).AddComponent<MapSpawnPoint>();
                        sp.transform.SetParent(gameplay, false);
                        sp.transform.position = p;
                        sp.spawnId = spec.startSpawnId;
                        break;
                    }
                    case 'F': BuildCampfire(ctx, gameplay, baseP); break;
                    case 'C': BuildChest(ctx, gameplay, baseP); break;
                    case '^':
                    {
                        var go = Sprite(gameplay, "SpikeTrap", MapArt.Generated("spike_plate", false), p, ySort: false, flatOrder: OrderFlat);
                        Box(go, new Vector2(0.9f, 0.9f), Vector2.zero, trigger: true);
                        go.AddComponent<SpikeTrap>();
                        break;
                    }
                    case 'P': BuildSwitch(ctx, gameplay, baseP); break;
                    case 'O': BuildTorch(ctx, gameplay, baseP); break;
                    case 'D':
                    {
                        int i = ctx.Next('D');
                        var go = Sprite(gameplay, "DartStatue", MapArt.Generated("dart_statue"), baseP);
                        Box(go, new Vector2(0.9f, 0.6f), new Vector2(0f, 0.3f));
                        var dt = go.AddComponent<DartTrap>();
                        Vector2 dir = i < spec.dartDirections.Count ? spec.dartDirections[i] : Vector2.down;
                        Set(dt, "direction", dir);
                        Set(dt, "startDelay", (i % 3) * 0.8f);
                        break;
                    }
                    case '%':
                    {
                        int i = ctx.Next('%');
                        var go = Sprite(gameplay, "CrackedRock", MapArt.Rock(4), baseP);
                        go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                        Box(go, new Vector2(0.62f, 0.62f), new Vector2(0f, 0.2f)); // ×1.6 scale ≈ lấp 1 ô
                        var br = go.AddComponent<Breakable>();
                        Set(br, "breakableId", $"{spec.sceneName}_{i}");
                        ctx.navBlock[x, y] = true;
                        break;
                    }
                    case 'R':
                    {
                        var go = Sprite(gameplay, "RuneBarrier", MapArt.Generated("rune_barrier_0", false), p);
                        go.GetComponent<SpriteRenderer>().sortingOrder = 1000 - Mathf.RoundToInt(y * 100f);
                        Animate(go, new[] { MapArt.Generated("rune_barrier_0", false), MapArt.Generated("rune_barrier_1", false) }, 4f);
                        Box(go, new Vector2(1f, 1f), Vector2.zero);
                        var rb = go.AddComponent<RuneBarrier>();
                        Set(rb, "dissolveOnFlag", spec.runeDissolveFlag);
                        ctx.navBlock[x, y] = true;
                        break;
                    }
                    case 'G':
                    {
                        int i = ctx.Next('G');
                        var go = Sprite(gameplay, "Gate", MapArt.Generated("palisade"), baseP);
                        Box(go, new Vector2(1f, 1f), new Vector2(0f, 0.3f));
                        var fg = go.AddComponent<FlagGate>();
                        fg.flag = i < spec.flagGates.Count ? spec.flagGates[i] : "";
                        fg.showWhenFlag = false;
                        break;
                    }
                    case 'A':
                    {
                        var go = Sprite(gameplay, "ArenaLock", MapArt.Generated("palisade"), baseP);
                        Box(go, new Vector2(1f, 1f), new Vector2(0f, 0.3f));
                        go.SetActive(false);
                        ctx.arenaLocks.Add(go);
                        break;
                    }
                    case 'Z':
                    {
                        var go = Sprite(gameplay, "CollapsingFloor", MapArt.Generated("floor_cracked", false), p, ySort: false, flatOrder: OrderFlat);
                        Box(go, new Vector2(0.9f, 0.9f), Vector2.zero, trigger: true);
                        var cf = go.AddComponent<CollapsingFloor>();
                        var hole = new GameObject("Hole");
                        hole.transform.SetParent(go.transform, false);
                        Box(hole, new Vector2(0.8f, 0.8f), Vector2.zero, trigger: true);
                        var fh = hole.AddComponent<FallHazard>();
                        hole.SetActive(false);
                        Set(cf, "hole", fh);
                        Set(cf, "restoreAfter", 6f);
                        break;
                    }
                    case 'N': BuildNpc(ctx, gameplay, baseP); break;
                    case 'X': BuildPortal(ctx, gameplay, x, y); break;
                    case 'Y': BuildStair(ctx, gameplay, x, y); break;
                    case 'B':
                    {
                        var agis = InstantiatePrefab("Assets/_Project/Art/animation/Boss/Boss_Agis.prefab", gameplay, p);
                        if (agis != null)
                        {
                            agis.name = "Boss_Agis";
                            if (agis.GetComponent<YSort>() == null) agis.AddComponent<YSort>();
                            PointLight(agis.transform, p + Vector3.up, new Color(0.8f, 0.3f, 1f), 1.2f, 5f);
                        }
                        break;
                    }
                    case 'M':
                    {
                        var go = Sprite(props, "RedMoonTower", MapArt.Building("Tower"), new Vector3(p.x, y, 0f));
                        go.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                        go.GetComponent<SpriteRenderer>().color = new Color(0.75f, 0.42f, 0.45f);
                        Box(go, new Vector2(1.6f, 0.8f), new Vector2(0f, 0.4f));
                        PointLight(go.transform, new Vector3(p.x, y + 3.5f, 0f), new Color(1f, 0.2f, 0.25f), 1.6f, 8f);
                        for (int dx = -1; dx <= 1; dx++) Mark(ctx, x + dx, y);
                        ctx.propCount++;
                        break;
                    }
                    default:
                        if (ch >= '1' && ch <= '9') BuildEncounter(ctx, gameplay, ch, p);
                        break;
                }
                if (ch != '.' && ch != ',' && ch != '~' && ch != '#' && ch != 'v' && ch != 'm') ctx.occupied[x, y] = true;
            }

        // Rào khoá arena gắn vào encounter tương ứng
        if (ctx.arenaLocks.Count > 0 && !string.IsNullOrEmpty(spec.arenaLockEncounter))
        {
            foreach (var enc in gameplay.GetComponentsInChildren<EncounterGroup>(true))
                if (enc.name == "Encounter_" + spec.arenaLockEncounter)
                    Set(enc, "lockWhileActive", ctx.arenaLocks.Cast<Object>().ToArray());
        }
    }

    private static void Mark(Ctx ctx, int x, int y)
    {
        if (x < 0 || y < 0 || x >= ctx.W || y >= ctx.H) return;
        ctx.navBlock[x, y] = true;
        ctx.occupied[x, y] = true;
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("[MapBuilder] Thiếu prefab " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        return go;
    }

    private static void PlaceTree(Ctx ctx, Transform props, int x, int y, bool big)
    {
        int kind = big ? 1 + ctx.rng.Next(2) : 3 + ctx.rng.Next(2);
        var frames = MapArt.Tree(kind);
        var pos = new Vector3(x + 0.5f + ctx.Jitter(0.25f), y + 0.25f + ctx.Jitter(0.15f), 0f);
        var go = Sprite(props, big ? "Tree" : "SmallTree", frames[0], pos);
        Animate(go, frames, big ? 5f : 6f);
        Solid(go, big ? 0.35f : 0.28f, 0.12f);
        ctx.navBlock[x, y] = true;
        ctx.propCount++;
    }

    private static void BuildSign(Ctx ctx, Transform parent, Vector3 pos)
    {
        int i = ctx.Next('g');
        var go = Sprite(parent, "Sign", MapArt.Deco(17), pos);
        Solid(go, 0.2f);
        if (i < ctx.spec.signs.Count)
            go.AddComponent<DialogueInteractable>().SetBlocks(ctx.spec.signs[i].text, null, null);
        ctx.propCount++;
    }

    private static void BuildCampfire(Ctx ctx, Transform parent, Vector3 pos)
    {
        int i = ctx.Next('F');
        var frames = MapArt.Fire();
        var go = Sprite(parent, $"Campfire_{i}", frames[0], pos);
        Animate(go, frames, 10f);
        Solid(go, 0.25f);
        PointLight(go.transform, pos + new Vector3(0f, 0.4f, 0f), new Color(1f, 0.6f, 0.25f), 1.35f, 4.5f);
        var cf = go.AddComponent<Campfire>();
        cf.checkpointId = $"{ctx.spec.sceneName}_fire_{i}";
        var embers = new GameObject("Embers").AddComponent<AmbientMotes>();
        embers.transform.SetParent(go.transform, false);
        embers.kind = AmbientMotes.Kind.Embers;
        embers.count = 6;
        embers.color = new Color(1f, 0.6f, 0.2f);
        embers.area = new Vector2(1.2f, 2.2f);
    }

    private static void BuildChest(Ctx ctx, Transform parent, Vector3 pos)
    {
        int i = ctx.Next('C');
        var def = i < ctx.spec.chests.Count ? ctx.spec.chests[i] : new ChestDef { id = $"{ctx.spec.sceneName}_chest_{i}", itemId = "potion_hp" };
        var closed = MapArt.Generated("chest_closed");
        var go = Sprite(parent, "Chest_" + def.id, closed, pos);
        Box(go, new Vector2(0.8f, 0.5f), new Vector2(0f, 0.25f));
        Box(go, new Vector2(1.8f, 1.8f), new Vector2(0f, 0.4f), trigger: true);
        var chest = go.AddComponent<TreasureChest>();
        Set(chest, "chestId", def.id);
        Set(chest, "rewardItem", Item(def.itemId));
        Set(chest, "rewardCount", def.count);
        Set(chest, "questId", def.questId);
        Set(chest, "objectiveId", def.objectiveId);
        Set(chest, "closedSprite", closed);
        Set(chest, "openSprite", MapArt.Generated("chest_open"));
        if (!string.IsNullOrEmpty(def.requireFlag))
        {
            var fg = go.AddComponent<FlagGate>();
            fg.flag = def.requireFlag;
            fg.showWhenFlag = true;
        }
        ctx.chests[def.id] = go;
    }

    private static void BuildSwitch(Ctx ctx, Transform parent, Vector3 pos)
    {
        int i = ctx.Next('P');
        var def = i < ctx.spec.switches.Count ? ctx.spec.switches[i] : new SwitchDef { id = $"{ctx.spec.sceneName}_switch_{i}" };
        var off = MapArt.Generated("lever_off");
        var go = Sprite(parent, "Switch_" + def.id, off, pos);
        Box(go, new Vector2(0.7f, 0.4f), new Vector2(0f, 0.2f));
        Box(go, new Vector2(1.8f, 1.8f), new Vector2(0f, 0.4f), trigger: true);
        var sw = go.AddComponent<PuzzleSwitch>();
        Set(sw, "switchId", def.id);
        Set(sw, "questId", def.questId);
        Set(sw, "objectiveId", def.objectiveId);
        Set(sw, "offSprite", off);
        Set(sw, "onSprite", MapArt.Generated("lever_on"));
    }

    private static TorchPuzzle torchPuzzle;

    private static void BuildTorch(Ctx ctx, Transform parent, Vector3 pos)
    {
        var def = ctx.spec.torches ?? new TorchDef();
        if (ctx.Next('O') == 0)
        {
            torchPuzzle = new GameObject("TorchPuzzle_" + def.id).AddComponent<TorchPuzzle>();
            torchPuzzle.transform.SetParent(parent, false);
            Set(torchPuzzle, "puzzleId", def.id);
            Set(torchPuzzle, "window", def.window);
            Set(torchPuzzle, "questId", def.questId);
            Set(torchPuzzle, "objectiveId", def.objectiveId);
        }
        var go = Sprite(torchPuzzle.transform, "Torch", MapArt.Generated("torch_post"), pos);
        Box(go, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.25f));
        Box(go, new Vector2(0.9f, 1.4f), new Vector2(0f, 0.7f), trigger: true);
        go.AddComponent<PuzzleTorch>();
    }

    private static void BuildNpc(Ctx ctx, Transform parent, Vector3 pos)
    {
        int i = ctx.Next('N');
        if (i >= ctx.spec.npcs.Count) return;
        var def = ctx.spec.npcs[i];
        var frames = MapArt.PawnIdle(def.pawnColor);
        var go = Sprite(parent, "NPC_" + def.name, frames[0], pos);
        Animate(go, frames, 6f);
        Solid(go, 0.25f);
        var di = go.AddComponent<DialogueInteractable>();
        di.SetBlocks(def.first, def.quest, def.repeat);
        if (def.reward != null) di.SetReward(def.reward, def.rewardAfterQuest);
        if (!string.IsNullOrEmpty(def.showWhenFlag))
        {
            var fg = go.AddComponent<FlagGate>();
            fg.flag = def.showWhenFlag;
            fg.showWhenFlag = true;
        }
        // Đèn nhỏ quanh NPC: luôn tìm thấy người nói chuyện trong đêm
        PointLight(go.transform, pos + new Vector3(0f, 0.6f, 0f), new Color(1f, 0.85f, 0.6f), 0.6f, 2f);
    }

    private static void BuildPortal(Ctx ctx, Transform parent, int x, int y)
    {
        int i = ctx.Next('X');
        if (i >= ctx.spec.portals.Count) return;
        var def = ctx.spec.portals[i];
        var p = ctx.Center(x, y);
        var go = Sprite(parent, $"Portal_to_{def.targetScene}", MapArt.Generated("portal_stones"), new Vector3(p.x, y + 0.1f, 0f));
        Box(go, new Vector2(1.3f, 0.8f), new Vector2(0f, 0.5f), trigger: true);
        var mp = go.AddComponent<MapPortal>();
        mp.targetScene = def.targetScene;
        mp.targetSpawnId = def.targetSpawnId;
        mp.requiredItemId = def.requiredItemId;
        mp.requiredFlag = def.requiredFlag;
        mp.lockedMessage = def.lockedMessage;
        mp.startQuestOnEnter = def.startQuest;
        PointLight(go.transform, new Vector3(p.x, y + 0.9f, 0f), new Color(0.55f, 0.75f, 1f), 1.2f, 3f);

        var arrive = new GameObject("Spawn_" + def.arrivalSpawnId).AddComponent<MapSpawnPoint>();
        arrive.transform.SetParent(parent, false);
        arrive.transform.position = p + (Vector3)(def.arrivalOffset * 1.6f);
        arrive.spawnId = def.arrivalSpawnId;
    }

    private static void BuildStair(Ctx ctx, Transform parent, int x, int y)
    {
        int i = ctx.Next('Y');
        if (i >= ctx.spec.stairs.Count) return;
        var def = ctx.spec.stairs[i];
        var p = ctx.Center(x, y);
        var go = Sprite(parent, "Stair_to_" + def.toSpawnId, MapArt.Generated("portal_stones"), new Vector3(p.x, y + 0.1f, 0f));
        Box(go, new Vector2(1.3f, 0.8f), new Vector2(0f, 0.5f), trigger: true);
        var sp = go.AddComponent<ScenePortal>();
        sp.portalName = "stair_" + def.toSpawnId;
        PointLight(go.transform, new Vector3(p.x, y + 0.9f, 0f), new Color(0.75f, 0.45f, 1f), 1.2f, 3f);

        var arrive = new GameObject("Spawn_" + def.arrivalSpawnId).AddComponent<MapSpawnPoint>();
        arrive.transform.SetParent(parent, false);
        arrive.transform.position = p + (Vector3)(def.arrivalOffset * 1.6f);
        arrive.spawnId = def.arrivalSpawnId;
        pendingStairs.Add((sp, def.toSpawnId));
    }

    private static readonly List<(ScenePortal portal, string spawnId)> pendingStairs = new List<(ScenePortal, string)>();

    private static void BuildEncounter(Ctx ctx, Transform parent, char key, Vector3 pos)
    {
        if (!ctx.spec.encounters.TryGetValue(key, out var def)) return;
        var go = new GameObject("Encounter_" + def.id);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var enc = go.AddComponent<EncounterGroup>();
        var slots = new List<EncounterGroup.Slot>();
        foreach (var e in def.enemies)
        {
            if (!EnemyPrefabs.TryGetValue(e.prefab, out var path)) continue;
            slots.Add(new EncounterGroup.Slot
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path),
                offset = e.offset,
                elite = e.elite,
                eliteName = e.eliteName,
                rewardItemId = e.rewardItemId,
            });
        }
        enc.Configure(def.id, def.radius, slots, new GameObject[0], "", def.reportClear);
        EditorUtility.SetDirty(enc);
    }

    // ───────── Clutter: chi tiết nhỏ rải theo seed (GUIDELINES 6.8: ≥ 12 / màn hình) ─────────
    private static void BuildClutter(Ctx ctx, Transform props)
    {
        var spec = ctx.spec;
        var flat = new List<int>();
        var tall = new List<string>();
        switch (spec.theme)
        {
            case MapSpec.Theme.Village: flat.AddRange(new[] { 10, 11, 10, 4, 5, 1 }); tall.Add("bush"); break;
            case MapSpec.Theme.Forest: flat.AddRange(new[] { 10, 11, 1, 2, 3, 4, 5, 6, 14 }); tall.AddRange(new[] { "bush", "bush", "smalltree" }); break;
            case MapSpec.Theme.Marsh: flat.AddRange(new[] { 10, 11, 4, 5, 14, 15, 1 }); tall.Add("bush"); break;
            case MapSpec.Theme.Tower: flat.AddRange(new[] { 14, 15, 4, 5 }); break;
        }
        var parent = new GameObject("Clutter").transform;
        parent.SetParent(props, false);

        for (int x = 1; x < ctx.W - 1; x++)
            for (int y = 1; y < ctx.H - 1; y++)
            {
                char ch = ctx.c[x, y];
                if (ctx.occupied[x, y] || ctx.blocked[x, y] || ctx.gap[x, y] || ch == 'm') continue;
                bool path = ch == ',';
                double roll = ctx.rng.NextDouble();
                float density = path ? spec.clutterDensity * 0.25f : spec.clutterDensity;
                if (roll > density) continue;
                // Không rải sát vật thể gameplay (đọc rõ cơ chế)
                bool nearMarker = false;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        char n = ctx.At(x + dx, y + dy);
                        if ("SFC^POD%RGAZNXYB123456789".IndexOf(n) >= 0) nearMarker = true;
                    }
                if (nearMarker) continue;

                var pos = new Vector3(x + 0.5f + ctx.Jitter(0.35f), y + 0.4f + ctx.Jitter(0.3f), 0f);
                if (tall.Count > 0 && !path && ctx.rng.NextDouble() < 0.18)
                {
                    string kind = tall[ctx.rng.Next(tall.Count)];
                    if (kind == "bush")
                    {
                        var frames = MapArt.Bush(1 + ctx.rng.Next(4));
                        var go = Sprite(parent, "Bush", frames[0], pos);
                        Animate(go, frames, 7f);
                    }
                    else
                    {
                        var frames = MapArt.Tree(3 + ctx.rng.Next(2));
                        var go = Sprite(parent, "SmallTree", frames[0], pos);
                        Animate(go, frames, 6f);
                        Solid(go, 0.25f, 0.1f);
                        ctx.navBlock[x, y] = true;
                    }
                }
                else
                {
                    int deco = flat[ctx.rng.Next(flat.Count)];
                    var go = Sprite(parent, "Clutter", MapArt.Deco(deco), pos, ySort: false, flatOrder: OrderClutter);
                    if (ctx.rng.NextDouble() < 0.5) go.GetComponent<SpriteRenderer>().flipX = true;
                }
                ctx.clutterCount++;
            }
    }

    // ═════════════════════════════════════════ LIGHT / PLAYER / NAV ═════════════════════════════════════════
    private static void BuildLightingAndAmbience(Ctx ctx)
    {
        var spec = ctx.spec;
        foreach (var l in Object.FindObjectsByType<Light2D>())
        {
            if (l.lightType != Light2D.LightType.Global) continue;
            l.color = spec.globalLight;
            l.intensity = spec.globalIntensity;
            TargetAllLayersSerialized(l);
            EditorUtility.SetDirty(l);
        }
        if (spec.motes.Count == 0) return;
        var amb = new GameObject("Ambience").transform;
        foreach (var m in spec.motes)
        {
            var go = new GameObject("Motes_" + m.kind);
            go.transform.SetParent(amb, false);
            var am = go.AddComponent<AmbientMotes>();
            am.kind = m.kind;
            am.count = m.count;
            am.color = m.color;
        }
    }

    private static void PlacePlayerAndCamera(Ctx ctx)
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = ctx.start;
            ctx.player = player.transform;
        }
        var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            cam.transform.position = new Vector3(ctx.start.x, ctx.start.y, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ctx.spec.background;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null && player != null) follow.target = player.transform;
        }

        // Cầu thang trong map: trỏ tới spawn point cùng scene
        foreach (var (portal, spawnId) in pendingStairs)
        {
            var target = Object.FindObjectsByType<MapSpawnPoint>().FirstOrDefault(s => s.spawnId == spawnId);
            if (target != null) portal.destinationRef = target.transform;
            EditorUtility.SetDirty(portal);
        }
        pendingStairs.Clear();
    }

    private static void BakeNavMesh(Ctx ctx, Transform grid, string scenePath)
    {
        // Ô vật cản (thân cây, nhà, khe vực) chỉ dành cho NavMesh — không có collider
        var nav = NewTilemap(grid, "91_NavBlock", "Ground", 0, render: false);
        var blockTile = MapArt.TileFor(MapArt.Water(), Tile.ColliderType.Grid);
        for (int x = 0; x < ctx.W; x++)
            for (int y = 0; y < ctx.H; y++)
                if (ctx.navBlock[x, y] || ctx.gap[x, y] || ctx.c[x, y] == 'm')
                    nav.SetTile(new Vector3Int(x, y, 0), blockTile);
        AddNavModifier(nav, 1);

        var go = new GameObject("Navigation");
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.16f;
        surface.hideEditorLogs = true;
        var ext = go.AddComponent<CollectSources2d>();
        typeof(NavMeshExtension).GetMethod("ConnectToVcam",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(ext, new object[] { true });
        surface.BuildNavMesh();

        if (surface.navMeshData != null)
        {
            string dataPath = Path.ChangeExtension(scenePath, null) + "_NavMesh.asset";
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
        }
        else Debug.LogWarning($"[MapBuilder] {ctx.spec.sceneName}: bake NavMesh thất bại");
    }

    // ═════════════════════════════════════════ MENU / BUILD SETTINGS / SNAPSHOT ═════════════════════════════════════════
    private static void SetupMenuScene()
    {
        var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        if (Object.FindAnyObjectByType<MainMenuController>() == null)
        {
            new GameObject("MainMenu").AddComponent<MainMenuController>();
            if (Object.FindAnyObjectByType<Camera>() == null)
            {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.05f, 0.09f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void SetBuildSettings(List<string> maps)
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScene, true) };
        scenes.AddRange(maps.Select(m => new EditorBuildSettingsScene(m, true)));
        scenes.Add(new EditorBuildSettingsScene(TemplateScene, false)); // scene cũ — giữ để đối chiếu, không build
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    public static void Snapshot(MapSpec spec, string dir)
    {
        var scene = EditorSceneManager.OpenScene($"{MapFolder}/{spec.sceneName}.unity", OpenSceneMode.Single);
        Directory.CreateDirectory(dir);
        float aspect = 16f / 9f;
        var center = new Vector2(spec.Width * 0.5f, spec.Height * 0.5f);
        float size = Mathf.Max(spec.Height * 0.5f, spec.Width * 0.5f / aspect) + 1f;
        Render(Path.Combine(dir, spec.sceneName + "_full.png"), center, size, 1920, 1080, spec.background);
        var spawn = Object.FindObjectsByType<MapSpawnPoint>().FirstOrDefault(s => s.spawnId == spec.startSpawnId);
        if (spawn != null)
            Render(Path.Combine(dir, spec.sceneName + "_start.png"), spawn.transform.position, 5f, 1280, 720, spec.background);
    }

    private static void Render(string file, Vector2 center, float size, int w, int h, Color bg)
    {
        var go = new GameObject("SnapCam");
        var cam = go.AddComponent<Camera>();
        var main = Camera.main;
        if (main != null) cam.CopyFrom(main);
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.transform.position = new Vector3(center.x, center.y, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(rt);
    }

    private static string Arg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
