using UnityEngine;

/// <summary>
/// Khe vực / hố sụp (LEVEL_DESIGN 4: Dash Gap, Collapsing Floor).
/// - Đi bộ vào → rơi: mất % máu tối đa, đặt lại ở mép nơi vừa bước vào.
/// - Đang Dash/Teleport (special_move.IsAirborne) → bay qua an toàn; dừng giữa khe thì vẫn rơi.
/// Collider phải là trigger phủ đúng vùng vực; viền đá quanh vực là telegraph (art).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FallHazard : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float damagePercent = 0.1f;
    [Tooltip("Lùi khỏi mép bao xa khi đặt lại (đơn vị thế giới)")]
    [SerializeField] private float respawnBackoff = 0.6f;

    private Vector2 entryPoint;
    private bool hasEntry;
    private float nextFallTime;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>Điểm đặt lại: lùi từ điểm vào ra xa tâm vực.</summary>
    public static Vector2 RespawnPoint(Vector2 entry, Vector2 hazardCenter, float backoff)
    {
        Vector2 outward = entry - hazardCenter;
        if (outward.sqrMagnitude < 0.0001f) outward = Vector2.down;
        return entry + outward.normalized * backoff;
    }

    public static bool ShouldFall(bool airborne) => !airborne;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!WorldTriggers.TryGetPlayer(other, out var player)) return;
        var sm = player.GetComponent<special_move>();
        // Chỉ ghi điểm vào khi đang đi bộ (dash vào giữa khe không phải "mép an toàn")
        if (sm == null || !sm.IsAirborne || !hasEntry)
        {
            entryPoint = player.transform.position;
            hasEntry = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextFallTime) return;
        if (!WorldTriggers.TryGetPlayer(other, out var player)) return;
        var sm = player.GetComponent<special_move>();
        if (!ShouldFall(sm != null && sm.IsAirborne)) return;

        nextFallTime = Time.time + 0.5f;
        Fall(player);
    }

    private void Fall(PlayerController player)
    {
        Vector2 fallPos = player.transform.position;
        VfxService.Hit(fallPos, Vector2.down, HitMaterial.Stone, true);

        var character = player.GetComponent<Character>();
        if (character != null && character.health != null)
            character.TakeDamage(character.health.maxValue * damagePercent);

        Vector2 center = GetComponent<Collider2D>().bounds.center;
        Vector2 dest = RespawnPoint(hasEntry ? entryPoint : fallPos, center, respawnBackoff);
        var rb = player.GetComponent<Rigidbody2D>();
        player.transform.position = dest;
        if (rb != null) { rb.position = dest; rb.linearVelocity = Vector2.zero; }

        VfxService.Impact(dest, new Color(0.75f, 0.68f, 0.55f), 0.6f);
        VfxService.Shake(ShakeLevel.Hurt);
    }
}
