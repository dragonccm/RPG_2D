using UnityEngine;

/// <summary>
/// Rương báu (Treasure Chest):
/// - Phím F khi đứng gần để mở rương.
/// - Nhận vật phẩm trực tiếp vào Túi đồ (Inventory) kèm hiệu ứng tia sáng.
/// - Báo cáo tiến trình nhiệm vụ nếu có gắn questId.
/// </summary>
public class TreasureChest : MonoBehaviour
{
    [Header("Cài đặt rương")]
    [SerializeField] private string chestId = "chest_1";
    [SerializeField] private ItemData rewardItem;
    [SerializeField] private int rewardCount = 1;

    [Header("Gắn nhiệm vụ (Tùy chọn)")]
    [SerializeField] private string questId = "";
    [SerializeField] private string objectiveId = "";

    [Header("Màu sắc / Hình ảnh")]
    [SerializeField] private Color closedColor = new Color(0.85f, 0.65f, 0.2f); // Vàng kim
    [SerializeField] private Color openedColor = new Color(0.45f, 0.45f, 0.45f); // Xám bạc

    [Header("Sprite (tuỳ chọn) — có thì đổi sprite thay vì chỉ đổi màu")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    public bool IsOpened { get; private set; }

    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;

    public string Flag => "chest:" + chestId;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        IsOpened = WorldFlags.Has(Flag);
        UpdateVisual();
    }

    private void OnEnable() { WorldFlags.Changed += OnFlagsChanged; }
    private void OnDisable() { WorldFlags.Changed -= OnFlagsChanged; }

    // Load save: rương đã mở ở lần chơi trước → hiện trạng thái mở, không phát thưởng lại
    private void OnFlagsChanged(string flag)
    {
        if (flag != null && flag != Flag) return;
        IsOpened = WorldFlags.Has(Flag);
        UpdateVisual();
    }

    private void Update()
    {
        if (playerInRange && !IsOpened && Input.GetKeyDown(KeyCode.F))
        {
            OpenChest();
        }
    }

    public void OpenChest()
    {
        if (IsOpened) return;

        IsOpened = true;
        WorldFlags.Set(Flag);
        UpdateVisual();

        // Thưởng vật phẩm vào túi đồ
        if (rewardItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(rewardItem, rewardCount);
        }

        // Báo tiến trình nhiệm vụ
        if (!string.IsNullOrEmpty(questId) && !string.IsNullOrEmpty(objectiveId) && QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportProgress(questId, objectiveId);
        }

        VfxService.Impact(transform.position + Vector3.up * 0.5f, Color.yellow, 1.5f);
        VfxService.Shake(ShakeLevel.Light);

        Debug.Log($"🎁 Mở rương [{chestId}]: Đã nhận {rewardItem?.itemName} x{rewardCount}!");
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;
        if (openSprite != null && closedSprite != null)
        {
            spriteRenderer.sprite = IsOpened ? openSprite : closedSprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.color = IsOpened ? openedColor : closedColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;
        }
    }
}
