using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đường ngắm kỹ năng dạng nét đứt pixel (thay LineRenderer vector — CORE_PROJECT_GUIDELINES 2.1).
/// Các ô vuông 3×3 px cách nhau 8 px, bám lưới pixel, tự đủ số ô theo độ dài; dùng lại ô, không cấp phát.
/// </summary>
public class PixelAimLine : MonoBehaviour
{
    private const float Step = 8f / PixelSprites.PPU;
    private readonly List<SpriteRenderer> dots = new List<SpriteRenderer>(24);

    public void SetLine(Vector2 start, Vector2 end, Color color)
    {
        Vector2 d = end - start;
        float len = d.magnitude;
        int n = Mathf.Clamp(Mathf.FloorToInt(len / Step), 1, 64);
        Vector2 dir = len > 0.0001f ? d / len : Vector2.right;
        while (dots.Count < n) dots.Add(NewDot());
        for (int i = 0; i < dots.Count; i++)
        {
            bool on = i < n;
            var sr = dots[i];
            if (sr.gameObject.activeSelf != on) sr.gameObject.SetActive(on);
            if (!on) continue;
            Vector2 p = start + dir * (Step * (i + 0.5f));
            sr.transform.position = new Vector3(
                Mathf.Round(p.x * PixelSprites.PPU) / PixelSprites.PPU,
                Mathf.Round(p.y * PixelSprites.PPU) / PixelSprites.PPU, 0f);
            // dấu cuối đậm hơn: chỉ điểm rơi
            var c = color;
            c.a = i == n - 1 ? 1f : 0.75f;
            sr.color = c;
            sr.transform.localScale = i == n - 1 ? new Vector3(5f, 5f, 1f) : new Vector3(3f, 3f, 1f);
        }
    }

    private SpriteRenderer NewDot()
    {
        var go = new GameObject("Dot");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSprites.Pixel;
        sr.sortingLayerName = "Effects";
        sr.sortingOrder = 4500;
        RuntimeMaterials.MakeUnlit(sr);
        return sr;
    }
}
