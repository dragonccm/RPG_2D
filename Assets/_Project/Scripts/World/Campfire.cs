using UnityEngine;

/// <summary>
/// S3 (GAME_MASTER_PLAN 4.2, 10): đống lửa — checkpoint.
/// Đứng gần + nhấn F: hồi máu full + save game. Là điểm hồi sinh khi chết.
/// </summary>
public class Campfire : MonoBehaviour
{
    [Header("Checkpoint")]
    public string checkpointId = "campfire";
    [SerializeField] private float interactRange = 1.6f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private float nextInteractTime;

    public string CheckpointId => checkpointId;

    private void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;
        if (Time.unscaledTime < nextInteractTime) return;

        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        if (Vector2.Distance(player.transform.position, transform.position) > interactRange) return;

        Interact(player);
        nextInteractTime = Time.unscaledTime + 1f; // chống spam
    }

    public void Interact(PlayerController player)
    {
        var character = player.GetComponent<Character>();
        string detail = "";

        // 1. Hồi máu full
        if (character != null && character.health != null)
        {
            float missing = character.health.maxValue - character.health.currentValue;
            if (missing > 0f)
            {
                character.health.Increase(missing);
                detail += $"+{Mathf.RoundToInt(missing)} HP ";
            }
        }

        // 2. Save
        var skill = player.GetComponent<ModularSkillManager>();
        bool saved = SaveSystem.Save(SaveSystem.CaptureFrom(player, skill, checkpointId));
        detail += saved ? "| đã lưu" : "| LƯU THẤT BẠI";

        // 3. Feedback
        var fx = CombatEffectsManager.Instance;
        if (fx != null) fx.CreateImpactEffect(transform.position, new Color(1f, 0.55f, 0.15f), 1.2f);

        Debug.Log($"🔥 Campfire [{checkpointId}]: {detail}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
