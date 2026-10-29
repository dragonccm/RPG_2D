using UnityEngine;

/// <summary>
/// P1-2 (GAME_MASTER_PLAN 3.2): blob shadow — bóng elip dưới nhân vật.
/// Không dùng realtime shadow của URP (tốn GPU, nhòe pixel art).
/// - SpriteRenderer ellipse đen alpha, sort layer "Shadows" (trên Ground, dưới mọi thực thể).
/// - Khi bị nhấc lên cao (elevation), bóng nhỏ + mờ dần theo độ cao.
/// - Không flip theo hướng quay nhân vật.
/// Gắn tự động vào mọi thực thể qua CharacterDepthSetup (Editor) hoặc AddComponent thủ công.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class BlobShadow : MonoBehaviour
{
    [Header("Kích thước & độ mờ")]
    [SerializeField] private float baseWidth = 0.9f;   // rộng theo footprint sprite
    [SerializeField] private float baseHeight = 0.35f;
    [SerializeField] private float alpha = 0.35f;

    [Header("Độ cao (elevation)")]
    [SerializeField] private float maxLiftHeight = 2f; // đơn vị world — bóng co/mờ hết ở đây
    [SerializeField] private float groundYOffset = -0.55f; // bóng nằm dưới chân sprite

    private SpriteRenderer shadowRenderer;
    private Transform owner;
    private Vector3 shadowBaseLocalPos;

    private void Awake()
    {
        owner = transform;
        shadowRenderer = CreateShadowChild();
        shadowBaseLocalPos = shadowRenderer.transform.localPosition;
    }

    private void LateUpdate()
    {
        if (shadowRenderer == null) return;

        // Độ cao so với "mặt đất" = khoảng cách từ gốc object tới điểm bóng cơ sở.
        // Với top-down 2.5D: dùng trục Y local của cha làm chiều cao (nhân vật nhảy/elevation
        // thường dời transform.position.y của con sprite, không của gốc).
        float lift = Mathf.Clamp(owner.position.y, 0f, maxLiftHeight);
        float t = maxLiftHeight > 0f ? lift / maxLiftHeight : 0f;

        // Bóng co lại + mờ dần khi nâng lên
        float shrink = Mathf.Lerp(1f, 0.55f, t);
        shadowRenderer.transform.localScale = new Vector3(baseWidth * shrink * SpriteWorldWidth, baseHeight * shrink * SpriteWorldWidth, 1f);

        var c = shadowRenderer.color;
        c.a = alpha * Mathf.Lerp(1f, 0.35f, t);
        shadowRenderer.color = c;

        shadowRenderer.transform.localPosition = shadowBaseLocalPos;
    }

    private SpriteRenderer CreateShadowChild()
    {
        var go = new GameObject("ShadowBlob");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, groundYOffset, 0f);
        go.transform.localScale = new Vector3(baseWidth * SpriteWorldWidth, baseHeight * SpriteWorldWidth, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateEllipseSprite();
        sr.sortingLayerName = "Shadows";
        sr.sortingOrder = 0;

        // Bóng không bị flip khi nhân vật lật (con của object bị flip scaleX → bù lại)
        return sr;
    }

    // Sprite 64 px ở 64 PPU rộng 1 đơn vị; baseWidth/baseHeight (đã serialize trong scene) tính theo bóng cũ rộng 0.64 đơn vị
    private const float SpriteWorldWidth = 0.64f;

    private static Sprite ellipse;

    private static Sprite CreateEllipseSprite()
    {
        if (ellipse != null) return ellipse;
        // Elip pixel-art 64×32: lõi đặc, vành dither caro, mép trong suốt — không gradient mềm
        const int W = 64, H = 32;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "BlobShadowTex", filterMode = FilterMode.Point };
        var pixels = new Color32[W * H];
        var solid = new Color32(0, 0, 0, 255);
        var clear = new Color32(0, 0, 0, 0);
        float cx = (W - 1) * 0.5f, cy = (H - 1) * 0.5f;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float nx = (x - cx) / cx, ny = (y - cy) / cy;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                bool checker = (((x >> 1) + (y >> 1)) & 1) == 0;
                bool on = d < 0.62f || (d < 0.88f && checker);
                pixels[y * W + x] = on ? solid : clear;
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        ellipse = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 64f);
        ellipse.name = "BlobShadow";
        return ellipse;
    }

    private void OnDestroy()
    {
        if (shadowRenderer != null)
            Destroy(shadowRenderer.gameObject);
    }
}
