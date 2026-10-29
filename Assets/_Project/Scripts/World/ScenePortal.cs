using UnityEngine;

/// <summary>
/// S3 (GAME_MASTER_PLAN 4.2): cổng dịch chuyển trong cùng scene (M1 ↔ M2).
/// Player chạm trigger → auto-save + teleport tới destination + camera snap.
/// (Scene thật tách file chuyển sang S5 khi có Addressables flow.)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("Điểm đến")]
    [SerializeField] private Transform destination;
    [SerializeField] private bool autosaveOnEnter = true;
    public string portalName = "portal";

    /// <summary>Setter để WorldBuilder/test gán điểm đến lúc dựng map.</summary>
    public Transform destinationRef
    {
        get => destination;
        set => destination = value;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (destination == null) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (autosaveOnEnter)
        {
            var skill = player.GetComponent<ModularSkillManager>();
            SaveSystem.Save(SaveSystem.CaptureFrom(player, skill, portalName));
        }

        other.transform.position = destination.position;

        // Camera snap (tránh lerp bay qua nửa map)
        var cam = Camera.main;
        if (cam != null)
        {
            var camFollow = cam.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.SnapTo(other.transform.position);
            else cam.transform.position = new Vector3(
                destination.position.x, destination.position.y, cam.transform.position.z);
        }

        Debug.Log($"🌀 Portal [{portalName}]: teleport tới {destination.position}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        if (destination != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destination.position);
        }
    }
}
