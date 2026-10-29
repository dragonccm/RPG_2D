using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sprite pixel-art sinh runtime cho VfxService: viền cứng, FilterMode.Point, PPU = 16
/// (cùng mật độ pixel với nhân vật). Không có gradient mềm / alpha mịn — mọi pixel
/// hoặc đặc hoặc trong suốt. Khi có art vẽ tay, thay trong VfxService, không đổi chỗ gọi.
/// </summary>
public static class PixelSprites
{
    public const float PPU = 16f;
    public const float PixelSize = 1f / PPU;

    private static Sprite pixel;
    private static Sprite spark;
    private static Sprite arc;
    private static readonly Dictionary<int, Sprite> rings = new Dictionary<int, Sprite>();
    private static readonly Dictionary<int, Sprite> discs = new Dictionary<int, Sprite>();

    /// <summary>1×1 pixel trắng — tint bằng SpriteRenderer.color, nhân scale nguyên để to hơn.</summary>
    public static Sprite Pixel => pixel != null ? pixel : (pixel = Build(1, 1, (x, y) => true, 0.5f, 0.5f));

    /// <summary>Vệt 1×3 pixel, xoay theo hướng bay (tia lửa kim loại).</summary>
    public static Sprite Spark => spark != null ? spark : (spark = Build(1, 3, (x, y) => true, 0.5f, 0.5f));

    /// <summary>
    /// Lưỡi liềm chém hướng +X, pivot ở tâm cung (xoay quanh điểm vung). 32×32 px.
    /// </summary>
    public static Sprite Arc
    {
        get
        {
            if (arc != null) return arc;
            arc = Build(32, 32, (x, y) =>
            {
                float ox = x + 0.5f - 12f, oy = y + 0.5f - 16f;
                float ix = x + 0.5f - 7f, iy = y + 0.5f - 16f;
                bool inOuter = ox * ox + oy * oy <= 14f * 14f;
                bool inInner = ix * ix + iy * iy <= 13f * 13f;
                return inOuter && !inInner;
            }, 12f / 32f, 0.5f);
            return arc;
        }
    }

    /// <summary>Viền tròn dày 1px (răng cưa), bán kính tính bằng pixel.</summary>
    public static Sprite Ring(int radiusPx)
    {
        radiusPx = Mathf.Clamp(radiusPx, 3, 160);
        if (rings.TryGetValue(radiusPx, out var s) && s != null) return s;
        int size = radiusPx * 2 + 2;
        float c = size * 0.5f;
        s = Build(size, size, (x, y) =>
        {
            float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
            return d >= radiusPx - 0.5f && d < radiusPx + 0.5f;
        }, 0.5f, 0.5f);
        rings[radiusPx] = s;
        return s;
    }

    /// <summary>Đĩa dither (caro 1px) + viền đặc: vùng cảnh báo kiểu pixel-art.</summary>
    public static Sprite DitherDisc(int radiusPx)
    {
        radiusPx = Mathf.Clamp(radiusPx, 3, 160);
        if (discs.TryGetValue(radiusPx, out var s) && s != null) return s;
        int size = radiusPx * 2 + 2;
        float c = size * 0.5f;
        s = Build(size, size, (x, y) =>
        {
            float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
            if (d >= radiusPx - 0.5f && d < radiusPx + 0.5f) return true;
            return d < radiusPx - 1f && ((x + y) & 1) == 0;
        }, 0.5f, 0.5f);
        discs[radiusPx] = s;
        return s;
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
