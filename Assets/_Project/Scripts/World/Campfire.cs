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
    private UnityEngine.Rendering.Universal.Light2D fireLight;

    public string CheckpointId => checkpointId;

    private void Awake()
    {
        EnsureLight();
    }

    private void EnsureLight()
    {
        fireLight = GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        if (fireLight == null)
        {
            var lightGo = new GameObject("CampfireLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            fireLight = lightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            fireLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            fireLight.color = new Color(1f, 0.65f, 0.25f, 1f); // Cam vàng ấm
            fireLight.intensity = 1.35f;
            fireLight.pointLightOuterRadius = 4.5f;
            LightUtil.TargetAllLayers(fireLight);
        }
    }

    private void Update()
    {
        // Hiệu ứng bập bùng ấm áp của ngọn lửa
        if (fireLight != null)
        {
            fireLight.intensity = 1.3f + Mathf.Sin(Time.time * 7f) * 0.12f + Random.Range(-0.04f, 0.04f);
        }

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
        VfxService.Impact(transform.position, new Color(1f, 0.55f, 0.15f), 1.2f);

        Debug.Log($"🔥 Campfire [{checkpointId}]: {detail}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
