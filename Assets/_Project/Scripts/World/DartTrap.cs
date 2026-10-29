using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bẫy tên từ tượng đá (LEVEL_DESIGN 4): theo chu kỳ, tượng sáng mắt (telegraph 0.6s) rồi bắn
/// 1 mũi tên thẳng theo hướng đặt. Tên trúng Character (player lẫn quái) hoặc dừng ở vật cản.
/// Mũi tên dùng lại (pool nội bộ), di chuyển bằng raycast không cấp phát.
/// </summary>
public class DartTrap : MonoBehaviour
{
    [Header("Bắn")]
    [SerializeField] private Vector2 direction = Vector2.down;
    [SerializeField] private float interval = 2.5f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private float telegraphTime = 0.6f;
    [SerializeField] private float speed = 9f;
    [SerializeField] private float range = 12f;
    [SerializeField] private float damage = 15f;

    [Header("Hình ảnh")]
    [Tooltip("Sprite mũi tên (hướng +X). Trống → phi tiêu pixel sinh sẵn.")]
    [SerializeField] private Sprite dartSprite;
    [SerializeField] private Color eyeGlow = new Color(1f, 0.3f, 0.2f);

    private class Dart
    {
        public Transform tr;
        public float travelled;
        public bool active;
    }

    private readonly List<Dart> darts = new List<Dart>(4);
    private readonly RaycastHit2D[] rayBuffer = new RaycastHit2D[8];
    private SpriteRenderer sr;
    private Color baseColor = Color.white;
    private float nextFire;
    private bool telegraphing;

    public static bool InTelegraph(float now, float nextFire, float telegraphTime) =>
        now >= nextFire - telegraphTime && now < nextFire;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
        direction.Normalize();
    }

    private void OnEnable()
    {
        nextFire = Time.time + startDelay + interval;
    }

    private void Update()
    {
        float now = Time.time;
        telegraphing = InTelegraph(now, nextFire, telegraphTime);
        if (sr != null)
        {
            // Mắt tượng nhấp nháy nhanh dần trước khi bắn
            bool on = telegraphing && Mathf.FloorToInt((now - (nextFire - telegraphTime)) * 10f) % 2 == 0;
            sr.color = on ? eyeGlow : baseColor;
        }

        if (now >= nextFire)
        {
            nextFire = now + interval;
            Fire();
        }

        for (int i = 0; i < darts.Count; i++)
            if (darts[i].active) Step(darts[i], Time.deltaTime);
    }

    private void Fire()
    {
        Dart d = null;
        foreach (var x in darts) if (!x.active) { d = x; break; }
        if (d == null)
        {
            var go = new GameObject("Dart");
            go.transform.SetParent(transform, false);
            var dsr = go.AddComponent<SpriteRenderer>();
            dsr.sprite = dartSprite != null ? dartSprite : PixelSprites.Dart;
            dsr.color = dartSprite != null ? Color.white : new Color(0.85f, 0.8f, 0.7f);
            dsr.sortingLayerName = "Characters";
            d = new Dart { tr = go.transform };
            darts.Add(d);
        }
        d.active = true;
        d.travelled = 0f;
        d.tr.gameObject.SetActive(true);
        d.tr.position = transform.position + (Vector3)(direction * 0.6f);
        d.tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        VfxService.Hit(d.tr.position, direction, HitMaterial.Stone);
    }

    private void Step(Dart d, float dt)
    {
        float dist = speed * dt;
        Vector2 from = d.tr.position;
        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.Raycast(from, direction, filter, rayBuffer, dist);
        for (int i = 0; i < n; i++)
        {
            var col = rayBuffer[i].collider;
            if (col == null || col.transform.IsChildOf(transform)) continue;
            var ch = col.GetComponentInParent<Character>();
            if (ch != null)
            {
                ch.TakeDamageWithKnockback(damage, 4f, direction);
                VfxService.Hit(rayBuffer[i].point, direction, HitMaterial.Flesh);
            }
            else
            {
                VfxService.Hit(rayBuffer[i].point, -direction, HitMaterial.Stone);
            }
            Retire(d);
            return;
        }

        d.tr.position = from + direction * dist;
        d.travelled += dist;
        if (d.travelled >= range) Retire(d);
    }

    private static void Retire(Dart d)
    {
        d.active = false;
        d.tr.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 dir = direction.sqrMagnitude > 0 ? (Vector3)direction.normalized : Vector3.down;
        Gizmos.DrawLine(transform.position, transform.position + dir * range);
    }
}
