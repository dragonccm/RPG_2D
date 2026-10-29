using System;
using UnityEngine;

/// <summary>
/// Công tắc câu đố (Puzzle Switch / Lever / Pressure Pad):
/// - Kích hoạt bằng phím F khi lại gần hoặc chém trúng (IDamageable).
/// - Khi kích hoạt: mở rào cản targetBarrier (cổng đá, cửa gỗ, rào chướng ngại).
/// </summary>
public class PuzzleSwitch : MonoBehaviour, IDamageable
{
    [Header("Cài đặt")]
    [SerializeField] private string switchId = "switch_1";
    [SerializeField] private GameObject targetBarrier; // Rào cản sẽ bị mở/vô hiệu khi kích hoạt
    [SerializeField] private bool oneTimeOnly = true;

    [Header("Gắn nhiệm vụ (Tùy chọn)")]
    [SerializeField] private string questId = "";
    [SerializeField] private string objectiveId = "";

    [Header("Giao diện")]
    [SerializeField] private Color offColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color onColor = new Color(0.2f, 0.9f, 0.3f); // Xanh lá sáng khi mở

    public bool IsActivated { get; private set; }
    public float CurrentHealth => IsActivated ? 0f : 1f;
    public float MaxHealth => 1f;
    public bool IsDead => IsActivated;
    public event Action<PuzzleSwitch> OnSwitchToggled;

    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    private void Update()
    {
        if (playerInRange && !IsActivated && Input.GetKeyDown(KeyCode.F))
        {
            ActivateSwitch();
        }
    }

    public void ActivateSwitch()
    {
        if (IsActivated && oneTimeOnly) return;

        IsActivated = true;
        UpdateVisual();

        if (targetBarrier != null)
        {
            targetBarrier.SetActive(false); // Mở rào chắn
        }

        // Báo tiến trình nhiệm vụ nếu có
        if (!string.IsNullOrEmpty(questId) && !string.IsNullOrEmpty(objectiveId) && QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportProgress(questId, objectiveId);
        }

        VfxService.Impact(transform.position, onColor, 1.2f);
        VfxService.Shake(ShakeLevel.Light);

        OnSwitchToggled?.Invoke(this);
        Debug.Log($"🧩 Công tắc [{switchId}] đã được kích hoạt -> Rào cản mở!");
    }

    public void ResetSwitch()
    {
        IsActivated = false;
        if (targetBarrier != null) targetBarrier.SetActive(true);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = IsActivated ? onColor : offColor;
        }
    }

    // Cho phép chém trúng công tắc để kích hoạt
    public void TakeDamage(float damage)
    {
        if (!IsActivated)
        {
            ActivateSwitch();
        }
    }

    public void Heal(float amount)
    {
        // Công tắc không nhận hồi máu
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
