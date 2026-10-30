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

    /// <summary>
    /// Điểm đặt lại: đẩy từ điểm vào ra cạnh GẦN NHẤT của vùng vực rồi lùi thêm backoff.
    /// Kể cả khi điểm vào nằm giữa khe (dịch chuyển thẳng vào) vẫn ra được đất, không rơi lặp.
    /// </summary>
    public static Vector2 RespawnPoint(Vector2 entry, Rect hazard, float backoff)
    {
        float toLeft = entry.x - hazard.xMin, toRight = hazard.xMax - entry.x;
        float toBottom = entry.y - hazard.yMin, toTop = hazard.yMax - entry.y;
        float min = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
        if (min < 0f) return entry; // đã ở ngoài vùng
        if (min == toLeft) return new Vector2(hazard.xMin - backoff, entry.y);
        if (min == toRight) return new Vector2(hazard.xMax + backoff, entry.y);
        if (min == toBottom) return new Vector2(entry.x, hazard.yMin - backoff);
        return new Vector2(entry.x, hazard.yMax + backoff);
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

        var b = GetComponent<Collider2D>().bounds;
        var rect = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        Vector2 dest = RespawnPoint(hasEntry ? entryPoint : fallPos, rect, respawnBackoff);
        var rb = player.GetComponent<Rigidbody2D>();
        player.transform.position = dest;
        if (rb != null) { rb.position = dest; rb.linearVelocity = Vector2.zero; }

        VfxService.Impact(dest, new Color(0.75f, 0.68f, 0.55f), 0.6f);
        VfxService.Shake(ShakeLevel.Hurt);
    }
}
