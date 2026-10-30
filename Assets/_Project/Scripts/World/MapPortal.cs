using UnityEngine;

/// <summary>
/// Cổng sang map khác (scene khác). Có thể yêu cầu vật phẩm (Chìa Khóa Tháp) hoặc story flag.
/// Không đủ điều kiện → hiện thông báo, không chuyển.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapPortal : MonoBehaviour
{
    public string targetScene = "M2_Forest";
    public string targetSpawnId = "from_village";

    [Header("Điều kiện (tuỳ chọn)")]
    public string requiredItemId = "";
    public string requiredFlag = "";
    [TextArea] public string lockedMessage = "Lối này chưa mở.";

    [Header("Khi đi qua")]
    public string startQuestOnEnter = "";

    private float nextMessage;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public bool CanPass(out string reason)
    {
        reason = "";
        if (!string.IsNullOrEmpty(requiredFlag) && !WorldFlags.Has(requiredFlag)) { reason = lockedMessage; return false; }
        if (!string.IsNullOrEmpty(requiredItemId)
            && (InventoryManager.Instance == null || InventoryManager.Instance.GetItemCount(requiredItemId) <= 0))
        {
            reason = lockedMessage;
            return false;
        }
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!WorldTriggers.TryGetPlayer(other, out var player)) return;
        if (!CanPass(out var reason))
        {
            if (Time.unscaledTime >= nextMessage)
            {
                nextMessage = Time.unscaledTime + 2f;
                InventoryManager.Instance?.Notify(reason);
                Debug.Log($"🚪 {name}: {reason}");
            }
            return;
        }

        if (!string.IsNullOrEmpty(startQuestOnEnter) && QuestManager.Instance != null)
            QuestManager.Instance.StartQuest(startQuestOnEnter);

        VfxService.Impact(player.transform.position, new Color(0.5f, 0.85f, 1f), 1.2f);
        AudioService.Play(SfxId.DoorOpen, player.transform.position);
        GameFlow.Travel(targetScene, targetSpawnId, player);
    }
}
