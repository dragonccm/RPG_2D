using UnityEngine;

/// <summary>
/// Đá / tường nứt (LEVEL_DESIGN 4): chặn đường tới khi bị Hỏa Kích (Charge) phá.
/// special_move.ReleaseCharge gọi Break() cho mọi Breakable trong bán kính.
/// Khi người chơi đã học Hỏa Kích (lua_1), vết nứt nhấp nháy cam — gợi ý có thể phá.
/// Trạng thái lưu qua WorldFlags ("break:id").
/// </summary>
public class Breakable : MonoBehaviour
{
    [SerializeField] private string breakableId = "rock_1";
    [Tooltip("Sprite đống đá vụn sau khi phá (tuỳ chọn). Không có → ẩn hẳn sprite.")]
    [SerializeField] private Sprite rubbleSprite;
    [SerializeField] private Color hintColor = new Color(1f, 0.6f, 0.25f);

    public string Flag => "break:" + breakableId;
    public bool IsBroken { get; private set; }

    private SpriteRenderer sr;
    private Color baseColor = Color.white;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    private void Start()
    {
        if (WorldFlags.Has(Flag)) ApplyBroken();
    }

    private void OnEnable() { WorldFlags.Changed += OnFlagsChanged; }
    private void OnDisable() { WorldFlags.Changed -= OnFlagsChanged; }

    private void OnFlagsChanged(string flag)
    {
        if ((flag == null || flag == Flag) && WorldFlags.Has(Flag) && !IsBroken) ApplyBroken();
    }

    public void Break()
    {
        if (IsBroken) return;
        Vector3 p = transform.position;
        VfxService.Hit(p, Vector2.up, HitMaterial.Stone, true);
        VfxService.Hit(p, Vector2.down, HitMaterial.Stone, true);
        VfxService.Impact(p, new Color(0.6f, 0.55f, 0.5f), 1.4f);
        VfxService.Shake(ShakeLevel.Heavy);
        WorldFlags.Set(Flag); // ApplyBroken qua OnFlagsChanged
        if (!IsBroken) ApplyBroken();
    }

    private void ApplyBroken()
    {
        IsBroken = true;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (sr != null)
        {
            if (rubbleSprite != null) { sr.sprite = rubbleSprite; sr.color = baseColor; }
            else sr.enabled = false;
        }
    }

    private void Update()
    {
        if (IsBroken || sr == null) return;
        bool canBreak = SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked("lua_1");
        if (!canBreak) { sr.color = baseColor; return; }
        // Nhấp nháy theo bậc, 1.2s/chu kỳ, 0.2s sáng
        bool glow = Mathf.Repeat(Time.time, 1.2f) < 0.2f;
        sr.color = glow ? hintColor : baseColor;
    }
}
