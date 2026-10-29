using UnityEngine;

/// <summary>
/// S3 (GAME_MASTER_PLAN 8.1): buff elite cho kẻ địch — mini-boss.
/// Gắn lên prefab enemy trong scene: to hơn, máu ×3, sát thương ×2.
/// Dữ liệu tuỳ chỉnh per-instance (không đụng prefab gốc).
/// </summary>
public class EliteBuff : MonoBehaviour
{
    [Header("Hệ số buff (áp 1 lần lúc Awake)")]
    [SerializeField] private float healthMultiplier = 3f;
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private float scaleMultiplier = 1.4f;
    [SerializeField] private string eliteName = "Thợ Săn Bóng";

    [Header("Phần thưởng khi hạ (LEVEL_DESIGN 2.2)")]
    [Tooltip("itemId trong Resources/Items — mặc định Cuộn Hư Không")]
    [SerializeField] private string rewardItemId = "scroll_teleport";
    [SerializeField] private string defeatedFlag = "boss:tho_san_bong";

    private bool applied;

    private void Awake()
    {
        if (applied) return;
        applied = true;

        var enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.baseDamage *= damageMultiplier;
            enemy.detectionRange = Mathf.Max(enemy.detectionRange, 14f);
            enemy.chaseRange = Mathf.Max(enemy.chaseRange, 24f);
        }

        var character = GetComponent<Character>();
        if (character != null && character.health != null)
        {
            float max = Mathf.Max(1f, character.health.maxValue) * healthMultiplier;
            float cur = Mathf.Max(1f, character.health.currentValue) * healthMultiplier;
            character.health.Initialize(max, 0f);
            character.health.currentValue = cur;
            character.AttackPower = Mathf.Max(1f, character.AttackPower) * damageMultiplier;
        }

        transform.localScale *= scaleMultiplier;
        gameObject.name = eliteName;

        if (character != null) character.OnDeath += GrantReward;
    }

    private void GrantReward()
    {
        if (WorldFlags.Has(defeatedFlag)) return; // đã nhận ở lần chơi trước
        WorldFlags.Set(defeatedFlag);
        var item = ItemDatabase.Get(rewardItemId);
        if (item != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(item, 1);
            Debug.Log($"🎁 {eliteName} rơi: {item.itemName}");
        }
    }
}
