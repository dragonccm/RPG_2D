using UnityEngine;

/// <summary>
/// Vùng bùn lầy (LEVEL_DESIGN 4): player đứng trong bị chậm (mặc định 50%).
/// Nhiều vùng chồng nhau lấy hệ số chậm nhất (PlayerController.SetSpeedModifier).
/// Telegraph = art bùn có bọt (animated tile) — vùng luôn nhìn thấy được.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SlowZone : MonoBehaviour
{
    [SerializeField, Range(0.1f, 1f)] private float speedMultiplier = 0.5f;
    [Tooltip("Bọt bùn bắn lên mỗi bước (giây giữa 2 lần)")]
    [SerializeField] private float splashInterval = 0.35f;

    private PlayerController inside;
    private float nextSplash;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!WorldTriggers.TryGetPlayer(other, out var player)) return;
        inside = player;
        player.SetSpeedModifier(this, speedMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!WorldTriggers.TryGetPlayer(other, out var player)) return;
        player.RemoveSpeedModifier(this);
        if (inside == player) inside = null;
    }

    private void OnDisable()
    {
        if (inside != null) inside.RemoveSpeedModifier(this);
        inside = null;
    }

    private Vector2 lastPos;

    private void Update()
    {
        if (inside == null || Time.time < nextSplash) return;
        Vector2 pos = inside.transform.position;
        bool moving = (pos - lastPos).sqrMagnitude > 0.0004f; // player đi bằng MovePosition → đo dịch chuyển
        lastPos = pos;
        if (!moving) return;
        nextSplash = Time.time + splashInterval;
        VfxService.Hit(pos, Vector2.zero, HitMaterial.Stone);
    }
}
