using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Chuẩn bị art cho MapBuilder: cắt sheet theo lưới (PPU 64, pivot ở chân vật thể), tạo Tile/AnimatedTile,
/// tra sprite tileset Tiny Swords theo vị trí ô. Mọi asset sinh ra nằm ở Art/Tiles/Generated.
/// </summary>
public static class MapArt
{
    public const string Art = "Assets/_Project/Art/";
    public const string TileFolder = "Assets/_Project/Art/Tiles/Generated";
    private const string TS = Art + "Tiny Swords (Update 010)/";
    private const string FP = Art + "Tiny Swords (Free Pack)/";

    public const string FlatSheet = Art + "Terrain/Ground/Tilemap_Flat.png";
    public const string ElevationSheet = Art + "Terrain/Ground/Tilemap_Elevation.png";

    private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<string, TileBase> tiles = new Dictionary<string, TileBase>();

    public static void Reset()
    {
        cache.Clear();
        tiles.Clear();
    }

    // ───────── Sprite theo nhóm ─────────
    public static Sprite[] Tree(int i) => Grid(FP + $"Decorations/Trees/Tree{i}.png", 192, i <= 2 ? 256 : 192, true);
    public static Sprite[] Bush(int i) => Grid(FP + $"Decorations/Bushes/Bushe{i}.png", 128, 128, true);
    public static Sprite Rock(int i) => Grid(FP + $"Decorations/Rocks/Rock{i}.png", 0, 0, true)[0];
    public static Sprite Deco(int i) => Grid(TS + $"Deco/{i:00}.png", 0, 0, true)[0];
    public static Sprite Building(string name) => Grid(FP + $"Buildings/Blue Buildings/{name}.png", 0, 0, true)[0];
    public static Sprite[] Fire() => Grid(TS + "Effects/Fire/Fire.png", 128, 128, true);
    public static Sprite[] Foam() => Grid(TS + "Terrain/Water/Foam/Foam.png", 192, 192, false);
    public static Sprite Shadow() => Grid(TS + "Terrain/Ground/Shadows.png", 0, 0, false)[0];
    public static Sprite Water() => Grid(TS + "Terrain/Water/Water.png", 0, 0, false)[0];

    /// <summary>6 frame đứng yên của Pawn (hàng đầu sheet 6×6).</summary>
    public static Sprite[] PawnIdle(string color) =>
        Grid(TS + $"Factions/Knights/Troops/Pawn/{color}/Pawn_{color}.png", 192, 192, true).Take(6).ToArray();

    public static Sprite Generated(string name, bool basePivot = true) =>
        Grid(Art + $"Generated/{name}.png", 0, 0, basePivot)[0];

    // ───────── Tileset Tiny Swords ─────────
    /// <summary>Sprite ô (col, rowTop) của sheet 64 px (gốc trên-trái).</summary>
    public static Sprite SheetCell(string sheet, int col, int rowTop)
    {
        var sprites = AllSprites(sheet);
        var tex = sprites[0].texture;
        float x = col * 64f, y = tex.height - (rowTop + 1) * 64f;
        foreach (var s in sprites)
            if (Mathf.Approximately(s.rect.x, x) && Mathf.Approximately(s.rect.y, y)) return s;
        Debug.LogWarning($"[MapArt] Không có ô ({col},{rowTop}) trong {sheet}");
        return null;
    }

    public static TileBase TileFor(Sprite sprite, Tile.ColliderType collider = Tile.ColliderType.None)
    {
        if (sprite == null) return null;
        string key = sprite.texture.name + "_" + sprite.name + "_" + collider;
        if (tiles.TryGetValue(key, out var t) && t != null) return t;
        EnsureFolder(TileFolder);
        string path = $"{TileFolder}/{Sanitize(key)}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = collider;
        EditorUtility.SetDirty(tile);
        tiles[key] = tile;
        return tile;
    }

    public static TileBase AnimatedTileFor(string key, Sprite[] frames, float speed)
    {
        if (tiles.TryGetValue(key, out var t) && t != null) return t;
        EnsureFolder(TileFolder);
        string path = $"{TileFolder}/{Sanitize(key)}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<AnimatedTile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.m_AnimatedSprites = frames;
        tile.m_MinSpeed = speed * 0.8f;
        tile.m_MaxSpeed = speed * 1.2f;
        tile.m_AnimationStartTime = 0f;
        tile.m_TileColliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        tiles[key] = tile;
        return tile;
    }

    // ───────── Cắt sheet ─────────
    private static Sprite[] AllSprites(string path)
    {
        if (cache.TryGetValue(path, out var s) && s.Length > 0 && s[0] != null) return s;
        s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        cache[path] = s;
        return s;
    }

    /// <summary>
    /// Cắt texture thành lưới fw×fh (0 = cả ảnh), PPU 64, lọc Point, không nén.
    /// basePivot: pivot ở pixel đặc thấp nhất của frame đầu (chân vật thể) để Y-sort đúng;
    /// ngược lại pivot giữa (tile, bọt nước). Giữ spriteID cho ô đã có để không gãy tham chiếu cũ.
    /// </summary>
    public static Sprite[] Grid(string path, int fw, int fh, bool basePivot)
    {
        string key = path + "|" + fw + "x" + fh + "|" + basePivot;
        if (cache.TryGetValue(key, out var hit) && hit.Length > 0 && hit[0] != null) return hit;

        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) throw new System.Exception("[MapArt] Thiếu texture " + path);
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        if (fw <= 0) fw = w;
        if (fh <= 0) fh = h;
        int cols = w / fw, rows = h / fh;

        Vector2 pivot = new Vector2(0.5f, 0.5f);
        if (basePivot)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            int bottom = LowestOpaqueRow(tex, 0, h - fh, fw, fh);
            Object.DestroyImmediate(tex);
            pivot = new Vector2(0.5f, Mathf.Clamp01((bottom + 3f) / fh));
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var existing = dp.GetSpriteRects();
        string baseName = Path.GetFileNameWithoutExtension(path).Replace(' ', '_');

        var list = new List<SpriteRect>();
        int n = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rect(c * fw, h - (r + 1) * fh, fw, fh);
                var sr = existing.FirstOrDefault(e => e.rect == rect) ?? new SpriteRect { spriteID = GUID.Generate() };
                sr.rect = rect;
                sr.name = $"{baseName}_{n}";
                sr.alignment = SpriteAlignment.Custom;
                sr.pivot = pivot;
                sr.border = Vector4.zero;
                list.Add(sr);
                n++;
            }

        bool needsWrite = ti.spriteImportMode != SpriteImportMode.Multiple
                          || !Mathf.Approximately(ti.spritePixelsPerUnit, 64f)
                          || ti.filterMode != FilterMode.Point
                          || existing.Length != list.Count
                          || existing.Any(e => !list.Any(l => l.spriteID == e.spriteID && l.name == e.name
                                                          && l.pivot == e.pivot && l.alignment == e.alignment));
        if (needsWrite)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 64f;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            dp.SetSpriteRects(list.ToArray());
            var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            ids?.SetNameFileIdPairs(list.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
            dp.Apply();
            ti.SaveAndReimport();
        }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => SuffixIndex(s.name)).ToArray();
        cache[key] = sprites;
        cache[path] = sprites;
        return sprites;
    }

    private static int LowestOpaqueRow(Texture2D tex, int x0, int y0, int fw, int fh)
    {
        var px = tex.GetPixels32();
        for (int y = 0; y < fh; y++)
            for (int x = 0; x < fw; x++)
                if (px[(y0 + y) * tex.width + x0 + x].a > 40) return y;
        return 0;
    }

    private static int SuffixIndex(string name)
    {
        int i = name.LastIndexOf('_');
        return i >= 0 && int.TryParse(name.Substring(i + 1), out int v) ? v : 0;
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(' ', '_').Replace('(', '_').Replace(')', '_');
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
