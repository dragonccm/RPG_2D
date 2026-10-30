using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

/// <summary>
/// Bật URP 2D thật sự (trước đây GraphicsSettings không gán pipeline → Built-in RP, mọi Light2D vô tác dụng)
/// và chuyển sprite từ material Built-in "Sprites-Default" sang "Sprite-Lit-Default" để nhận ánh sáng 2D.
/// </summary>
public static class RenderSetup
{
    private const string PipelineAsset = "Assets/_Project/Settings/UniversalRP.asset";
    private const string UrpMaterials = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/";
    private const string UnlitCopy = "Assets/_Project/Resources/Materials/Sprite-Unlit.mat";

    public static Material SpriteLit => AssetDatabase.LoadAssetAtPath<Material>(UrpMaterials + "Sprite-Lit-Default.mat");

    [MenuItem("Tools/RPG/Render/Enable URP 2D + Upgrade Sprites")]
    public static void EnsureAll()
    {
        EnsurePipeline();
        EnsureUnlitMaterial();
        UpgradePrefabs();
        UpgradeSpriteMaterialAssets();
    }

    public static void EnsurePipeline()
    {
        var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAsset);
        if (asset == null) { Debug.LogError("[RenderSetup] Thiếu " + PipelineAsset); return; }
        if (GraphicsSettings.defaultRenderPipeline != asset)
        {
            GraphicsSettings.defaultRenderPipeline = asset;
            Debug.Log("[RenderSetup] Đã gán URP 2D làm render pipeline mặc định");
        }
        AssetDatabase.SaveAssets();
    }

    private static void EnsureUnlitMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(UnlitCopy) != null) return;
        MapArt.EnsureFolder("Assets/_Project/Resources/Materials");
        AssetDatabase.CopyAsset(UrpMaterials + "Sprite-Unlit-Default.mat", UnlitCopy);
    }

    private static bool IsBuiltinSprite(Material m) =>
        m == null || m.name == "Sprites-Default" || (m.shader != null && m.shader.name == "Sprites/Default");

    /// <summary>Đổi material của mọi SpriteRenderer/TilemapRenderer trong các prefab dự án.</summary>
    public static void UpgradePrefabs()
    {
        var lit = SpriteLit;
        if (lit == null) return;
        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is SpriteRenderer) && !(r is TilemapRenderer)) continue;
                if (!IsBuiltinSprite(r.sharedMaterial)) continue;
                r.sharedMaterial = lit;
                dirty = true;
            }
            if (dirty) { PrefabUtility.SaveAsPrefabAsset(root, path); changed++; }
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"[RenderSetup] Prefab chuyển sang Sprite-Lit: {changed}");
    }

    /// <summary>Material asset của dự án dùng shader Sprites/Default → Sprite-Lit-Default của URP 2D.</summary>
    private static void UpgradeSpriteMaterialAssets()
    {
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) return;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == null || m.shader.name != "Sprites/Default") continue;
            m.shader = shader;
            EditorUtility.SetDirty(m);
        }
    }

    /// <summary>Sprite/tile trong scene đang mở → Sprite-Lit (giữ nguyên material tuỳ chỉnh khác).</summary>
    public static void UpgradeOpenScene()
    {
        var lit = SpriteLit;
        if (lit == null) return;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (!(r is SpriteRenderer) && !(r is TilemapRenderer)) continue;
            if (IsBuiltinSprite(r.sharedMaterial)) r.sharedMaterial = lit;
        }
    }
}
