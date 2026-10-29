using UnityEngine;

/// <summary>
/// Vật thể đổi theo story flag (CORE_PROJECT_GUIDELINES 6.7): cổng làng mở sau q1, đò chạy sau
/// mini-boss đầm, NPC chuyển chỗ... showWhenFlag = false: object biến mất khi flag bật (cổng mở);
/// true: object chỉ hiện khi flag bật (cầu, đò, NPC mới).
/// </summary>
public class FlagGate : MonoBehaviour
{
    public string flag = "";
    public bool showWhenFlag = false;
    [Tooltip("Object con bị bật/tắt (trống = chính các renderer/collider của object này)")]
    public GameObject target;

    private void Awake()
    {
        WorldFlags.Changed += OnChanged;
        Apply(false);
    }

    private void OnDestroy() { WorldFlags.Changed -= OnChanged; }

    private void OnChanged(string f)
    {
        if (f == null || f == flag) Apply(f != null);
    }

    private void Apply(bool animate)
    {
        bool visible = WorldFlags.Has(flag) == showWhenFlag;
        if (target != null)
        {
            if (target.activeSelf == visible) return;
            target.SetActive(visible);
        }
        else
        {
            bool changed = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) { changed |= r.enabled != visible; r.enabled = visible; }
            foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.enabled = visible;
            if (!changed) return;
        }
        if (animate) VfxService.Impact(transform.position, new Color(0.8f, 0.75f, 0.6f), 1.2f);
    }
}
