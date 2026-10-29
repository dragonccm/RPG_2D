using UnityEngine;

/// <summary>
/// Rào rune (LEVEL_DESIGN 4): bức tường năng lượng mỏng chặn đi bộ, nhưng Hư Không (Teleport)
/// xuyên qua được — special_move bỏ qua collider có RuneBarrier khi tính điểm dịch chuyển.
/// Tuỳ chọn tan hẳn khi một world flag được bật (vd câu đố giải xong).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RuneBarrier : MonoBehaviour
{
    [Tooltip("Rào tan vĩnh viễn khi flag này bật (để trống = không bao giờ tan)")]
    [SerializeField] private string dissolveOnFlag = "";
    [SerializeField] private Color runeColor = new Color(0.65f, 0.35f, 1f, 0.9f);

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = false;
    }

    private void Start()
    {
        if (WorldFlags.Has(dissolveOnFlag)) gameObject.SetActive(false);
    }

    private void OnEnable() { WorldFlags.Changed += OnFlagsChanged; }
    private void OnDisable() { WorldFlags.Changed -= OnFlagsChanged; }

    private void OnFlagsChanged(string flag)
    {
        if (string.IsNullOrEmpty(dissolveOnFlag) || !WorldFlags.Has(dissolveOnFlag)) return;
        if (flag != null) VfxService.Impact(transform.position, runeColor, 1.2f);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (sr == null) return;
        // Rune nhấp nháy 3 bậc sáng — luôn đọc được là "rào phép", không phải tường thường
        int step = Mathf.FloorToInt(Time.time * 4f) % 3;
        var c = runeColor;
        c.a = runeColor.a * (step == 0 ? 1f : step == 1 ? 0.8f : 0.6f);
        sr.color = c;
    }
}
