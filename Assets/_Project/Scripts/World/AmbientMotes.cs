using UnityEngine;

/// <summary>
/// Hạt môi trường pixel (LEVEL_DESIGN 1.4): đom đóm, lá rơi, tàn lửa, sương dày.
/// Đi theo camera trong 1 vùng bằng khung hình — số hạt cố định, không cấp phát, 1 Update.
/// Hạt dùng PixelSprites (đúng mật độ 64 PPU), không phải hình tròn mềm.
/// </summary>
public class AmbientMotes : MonoBehaviour
{
    public enum Kind { Fireflies, Leaves, Embers, Fog, Dust }

    public Kind kind = Kind.Fireflies;
    [Range(1, 80)] public int count = 24;
    public Color color = new Color(0.75f, 1f, 0.45f);
    [Tooltip("Vùng hạt quanh camera (đơn vị thế giới)")]
    public Vector2 area = new Vector2(22f, 13f);

    private Transform[] motes;
    private SpriteRenderer[] renderers;
    private Vector2[] velocity;
    private float[] seed;
    private Transform cam;

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        motes = new Transform[count];
        renderers = new SpriteRenderer[count];
        velocity = new Vector2[count];
        seed = new float[count];
        Vector2 c = cam != null ? (Vector2)cam.position : (Vector2)transform.position;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Mote");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = kind == Kind.Fog ? PixelSprites.FogPuff : PixelSprites.Pixel;
            sr.sortingLayerName = kind == Kind.Fog ? "Effects" : "Characters";
            sr.sortingOrder = kind == Kind.Fog ? 50 : 30000;
            if (kind == Kind.Fireflies || kind == Kind.Embers) RuntimeMaterials.MakeUnlit(sr); // tự phát sáng
            int px = kind switch { Kind.Leaves => 3, Kind.Fireflies => 2, Kind.Embers => 2, Kind.Fog => 2, _ => 1 };
            go.transform.localScale = new Vector3(px, kind == Kind.Leaves ? 2 : px, 1f);
            motes[i] = go.transform;
            renderers[i] = sr;
            seed[i] = Random.value * 100f;
            go.transform.position = c + new Vector2(Random.Range(-area.x, area.x), Random.Range(-area.y, area.y)) * 0.5f;
            velocity[i] = BaseVelocity(i);
        }
    }

    private Vector2 BaseVelocity(int i) => kind switch
    {
        Kind.Leaves => new Vector2(0.6f + seed[i] % 0.5f, -0.5f - seed[i] % 0.4f),
        Kind.Embers => new Vector2(0f, 0.5f + seed[i] % 0.6f),
        Kind.Fog => new Vector2(0.15f + seed[i] % 0.15f, 0f),
        Kind.Dust => new Vector2(0.1f, 0.05f),
        _ => Vector2.zero,
    };

    private void Update()
    {
        if (motes == null) return;
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        Vector2 c = cam != null ? (Vector2)cam.position : (Vector2)transform.position;
        Vector2 half = area * 0.5f;
        float t = Time.time;
        float dt = Time.deltaTime;

        for (int i = 0; i < motes.Length; i++)
        {
            var tr = motes[i];
            Vector2 p = tr.position;
            Vector2 v = velocity[i];
            if (kind == Kind.Fireflies)
                v = new Vector2(Mathf.Sin(t * 0.7f + seed[i]), Mathf.Cos(t * 0.9f + seed[i] * 1.3f)) * 0.4f;
            else if (kind == Kind.Leaves)
                v.x += Mathf.Sin(t * 2f + seed[i]) * 0.4f;
            p += v * dt;

            // Ra khỏi vùng → quay vòng sang mép đối diện (luôn đủ hạt quanh người chơi)
            if (p.x < c.x - half.x) p.x += area.x; else if (p.x > c.x + half.x) p.x -= area.x;
            if (p.y < c.y - half.y) p.y += area.y; else if (p.y > c.y + half.y) p.y -= area.y;
            tr.position = new Vector3(Mathf.Round(p.x * 64f) / 64f, Mathf.Round(p.y * 64f) / 64f, 0f);

            var col = color;
            if (kind == Kind.Fireflies)
                col.a = color.a * (Mathf.Sin(t * 2.5f + seed[i] * 3f) > 0.2f ? 1f : 0.15f); // nhấp nháy theo bậc
            else if (kind == Kind.Fog)
                col.a = color.a * (0.5f + 0.5f * Mathf.Sin(t * 0.3f + seed[i]));
            renderers[i].color = col;
        }
    }
}
