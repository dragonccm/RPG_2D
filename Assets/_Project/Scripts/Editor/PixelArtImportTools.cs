using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Quy chuẩn import pixel art (CORE_PROJECT_GUIDELINES 2.1 / 6.11):
/// - Texture trong Assets/_Project/Art: FilterMode.Point, không nén, không mipmap.
/// - Ảnh mới thêm vào Art tự nhận các thiết lập này (chỉ lần import đầu — vẫn chỉnh tay được).
/// - Sprite Atlas cho các thư mục nhiều file PNG rời (deco, VFX) để gộp draw call.
/// Chạy batch: Unity -batchmode -quit -executeMethod PixelArtImportTools.ApplyAll
/// </summary>
public class PixelArtImportTools : AssetPostprocessor
{
    private const string ArtRoot = "Assets/_Project/Art";
    private const string AtlasFolder = "Assets/_Project/Art/Atlases";

    // Atlas → các thư mục nguồn (nhiều PNG nhỏ rời nhau)
    private static readonly Dictionary<string, string[]> Atlases = new Dictionary<string, string[]>
    {
        {
            "Atlas_Deco", new[]
            {
                ArtRoot + "/Tiny Swords (Update 010)/Deco",
                ArtRoot + "/Tiny Swords (Free Pack)/Decorations",
                ArtRoot + "/new asset/PNG/Objects_separetely",
            }
        },
        {
            "Atlas_VFX", new[]
            {
                ArtRoot + "/effect",
                ArtRoot + "/new asset/Dark VFX 1/Separeted frames",
                ArtRoot + "/new asset/Dark VFX 2/Separated Frames",
            }
        },
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot) || !assetImporter.importSettingsMissing) return;
        ApplyRules((TextureImporter)assetImporter);
    }

    private static bool ApplyRules(TextureImporter ti)
    {
        bool changed = false;
        if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; changed = true; }
        if (ti.textureCompression != TextureImporterCompression.Uncompressed)
        {
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }

        // Ghi đè theo platform cũng phải bỏ nén, nếu không bản build vẫn bị nén
        foreach (var platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
        {
            var ps = ti.GetPlatformTextureSettings(platform);
            if (ps.overridden && ps.format != TextureImporterFormat.RGBA32)
            {
                ps.format = TextureImporterFormat.RGBA32;
                ps.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SetPlatformTextureSettings(ps);
                changed = true;
            }
        }
        return changed;
    }

    [MenuItem("Tools/RPG/Pixel Art/Apply Import Rules To Art")]
    public static void ApplyImportRules()
    {
        int changed = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("__MACOSX")) continue;
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                if (ApplyRules(ti)) { ti.SaveAndReimport(); changed++; }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        Debug.Log($"[PixelArtImportTools] Đã chuẩn hoá {changed}/{guids.Length} texture trong {ArtRoot}");
    }

    [MenuItem("Tools/RPG/Pixel Art/Build Sprite Atlases")]
    public static void BuildAtlases()
    {
        if (!AssetDatabase.IsValidFolder(AtlasFolder))
            AssetDatabase.CreateFolder(ArtRoot, "Atlases");

        foreach (var kv in Atlases)
        {
            string path = $"{AtlasFolder}/{kv.Key}.spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = 4,
                enableAlphaDilation = false,
            });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Point,
                generateMipMaps = false,
                readable = false,
                sRGB = true,
            });
            var ps = atlas.GetPlatformSettings("DefaultTexturePlatform");
            ps.maxTextureSize = 4096;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.format = TextureImporterFormat.RGBA32;
            atlas.SetPlatformSettings(ps);

            atlas.Remove(atlas.GetPackables());
            var folders = new List<Object>();
            foreach (var folder in kv.Value)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"[PixelArtImportTools] Thiếu thư mục {folder} cho {kv.Key}");
                    continue;
                }
                folders.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder));
            }
            atlas.Add(folders.ToArray());
            EditorUtility.SetDirty(atlas);
            Debug.Log($"[PixelArtImportTools] {kv.Key}: {folders.Count} thư mục");
        }

        AssetDatabase.SaveAssets();
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    }

    /// <summary>Entry point batchmode: chuẩn hoá import rồi dựng atlas.</summary>
    public static void ApplyAll()
    {
        ApplyImportRules();
        BuildAtlases();
    }
}
