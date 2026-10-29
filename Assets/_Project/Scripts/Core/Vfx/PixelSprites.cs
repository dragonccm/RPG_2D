using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sprite pixel-art sinh runtime cho VfxService: viền cứng, FilterMode.Point, PPU = 64
/// (cùng mật độ pixel với art Tiny Swords: 1 tile = 64 px = 1 đơn vị). Không gradient, không
/// alpha mịn. Đổi kích thước bằng cách sinh sprite đúng cỡ (cache theo cỡ), không scale lẻ —
/// để 1 pixel effect luôn bằng 1 pixel art. Khi có art vẽ tay, thay trong VfxService.
/// </summary>
public static class PixelSprites
{
    public const float PPU = 64f;
    public const float PixelSize = 1f / PPU;

    private static Sprite pixel;
    private static Sprite spark;
    private static readonly Dictionary<int, Sprite> arcs = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> rings = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> discs = new Dictionary<int, Sprite>();

    /// <summary>1×1 pixel trắng — tint bằng SpriteRenderer.color, nhân scale nguyên để to hơn.</summary>
    public static Sprite Pixel => pixel != null ? pixel : (pixel = Build(1, 1, (x, y) => true, 0.5f, 0.5f));

    /// <summary>Vệt 1×3 pixel, xoay theo hướng bay (tia lửa kim loại).</summary>
    public static Sprite Spark => spark != null ? spark : (spark = Build(1, 3, (x, y) => true, 0.5f, 0.5f));

    private static Sprite dart;

    /// <summary>Phi tiêu 10×2 px hướng +X (bẫy tên khi không có sprite mũi tên).</summary>
    public static Sprite Dart => dart != null ? dart : (dart = Build(10, 2, (x, y) => true, 0.5f, 0.5f));

    /// <summary>
    /// Lưỡi liềm chém hướng +X, pivot ở tâm cung (xoay quanh điểm vung).
    /// lengthUnits = chiều dài cung theo đơn vị thế giới (1 = 1 tile).
    /// </summary>
    public static Sprite Arc(float lengthUnits)
    {
        int grid = Mathf.Clamp(Mathf.RoundToInt(lengthUnits * PPU / 8f) * 8, 32, 256);
        if (arcs.TryGetValue(grid, out var s) && s != null) return s;
        float k = grid / 32f; // tỉ lệ so với lưới gốc 32 px
        float ocx = 12f * k, ir = 13f * k, or = 14f * k, icx = 7f * k, cy = 16f * k;
        s = Build(grid, grid, (x, y) =>
        {
            float ox = x + 0.5f - ocx, oy = y + 0.5f - cy;
            float ix = x + 0.5f - icx, iy = y + 0.5f - cy;
            return ox * ox + oy * oy <= or * or && ix * ix + iy * iy > ir * ir;
        }, 12f / 32f, 0.5f);
        arcs[grid] = s;
        return s;
    }

    /// <summary>Viền tròn dày 2px (răng cưa), bán kính tính bằng pixel art.</summary>
    public static Sprite Ring(int radiusPx)
    {
        radiusPx = Mathf.Clamp(radiusPx / 2 * 2, 4, 640);
        if (rings.TryGetValue(radiusPx, out var s) && s != null) return s;
        int size = radiusPx * 2 + 2;
        float c = size * 0.5f;
        s = Build(size, size, (x, y) =>
        {
            float d = Dist(x, y, c);
            return d >= radiusPx - 1f && d < radiusPx + 1f;
        }, 0.5f, 0.5f);
        rings[radiusPx] = s;
        return s;
    }

    /// <summary>Đĩa dither (caro 2px, khớp độ thô của art) + viền 2px: vùng cảnh báo pixel-art.</summary>
    public static Sprite DitherDisc(int radiusPx)
    {
        radiusPx = Mathf.Clamp(radiusPx / 8 * 8, 8, 640); // làm tròn để cache dùng lại
        if (discs.TryGetValue(radiusPx, out var s) && s != null) return s;
        int size = radiusPx * 2 + 2;
        float c = size * 0.5f;
        s = Build(size, size, (x, y) =>
        {
            float d = Dist(x, y, c);
            if (d >= radiusPx - 2f && d < radiusPx) return true;
            return d < radiusPx - 3f && (((x >> 1) + (y >> 1)) & 1) == 0;
        }, 0.5f, 0.5f);
        discs[radiusPx] = s;
        return s;
    }

    private static Sprite fogPuff;

    /// <summary>
    /// Mây sương pixel: elip dither thưa dần ra mép, KHÔNG viền — không đọc thành "hình tròn".
    /// 96×40 px, mật độ theo nhiễu cố định (không đổi giữa các lần chạy).
    /// </summary>
    public static Sprite FogPuff
    {
        get
        {
            if (fogPuff != null) return fogPuff;
            fogPuff = Build(96, 40, (x, y) =>
            {
                float nx = (x + 0.5f - 48f) / 48f, ny = (y + 0.5f - 20f) / 20f;
                float d = nx * nx + ny * ny;
                if (d >= 1f) return false;
                int h = (x * 73856093) ^ (y * 19349663);
                float noise = ((h >> 4) & 255) / 255f;
                bool checker = (((x >> 1) + (y >> 1)) & 1) == 0;
                return checker && noise > d * 0.9f;
            }, 0.5f, 0.5f);
            return fogPuff;
        }
    }

    private static float Dist(int x, int y, float c)
    {
        float dx = x + 0.5f - c, dy = y + 0.5f - c;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static Sprite Build(int w, int h, System.Func<int, int, bool> solid, float pivotX, float pivotY)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = solid(x, y) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        tex.SetPixels32(px);
        tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(pivotX, pivotY), PPU);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
