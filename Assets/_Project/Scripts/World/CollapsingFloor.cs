using UnityEngine;

/// <summary>
/// Sàn sụp (LEVEL_DESIGN 3.4 / 4): Intact → Cracking (nứt, rung, nháy 1s) → Collapsed (thành hố —
/// bật FallHazard con). Kích hoạt khi player bước lên (collapseOnStep) hoặc gọi Collapse() từ boss phase.
/// restoreAfter > 0 → sàn liền lại sau chừng đó giây (gauntlet chơi lại được).
/// </summary>
public class CollapsingFloor : MonoBehaviour
{
    public enum FloorState { Intact, Cracking, Collapsed }

    [SerializeField] private bool collapseOnStep = true;
    [SerializeField] private float crackTime = 1f;
    [SerializeField] private float restoreAfter = 0f;
    [Tooltip("Vùng rơi — trigger con chứa FallHazard, tắt khi sàn còn nguyên")]
    [SerializeField] private FallHazard hole;
    [SerializeField] private Color crackColor = new Color(1f, 0.55f, 0.35f);
    [SerializeField] private Color collapsedColor = new Color(0.08f, 0.06f, 0.08f);

    public FloorState State { get; private set; } = FloorState.Intact;

    private SpriteRenderer sr;
    private Color baseColor = Color.white;
    private float stateTime;

    /// <summary>Chuyển trạng thái thuần theo thời gian (test được).</summary>
    public static FloorState Next(FloorState s, float elapsed, float crackTime, float restoreAfter)
    {
        switch (s)
        {
            case FloorState.Cracking: return elapsed >= crackTime ? FloorState.Collapsed : s;
            case FloorState.Collapsed: return restoreAfter > 0f && elapsed >= restoreAfter ? FloorState.Intact : s;
            default: return s;
        }
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        if (hole == null) hole = GetComponentInChildren<FallHazard>(true);
        SetState(FloorState.Intact);
    }

    public void Collapse()
    {
        if (State == FloorState.Intact) SetState(FloorState.Cracking);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collapseOnStep && WorldTriggers.TryGetPlayer(other, out _)) Collapse();
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        var next = Next(State, stateTime, crackTime, restoreAfter);
        if (next != State) SetState(next);

        if (State == FloorState.Cracking && sr != null)
        {
            // nháy nhanh dần + rung 1px
            float rate = stateTime > crackTime * 0.6f ? 16f : 8f;
            sr.color = Mathf.FloorToInt(stateTime * rate) % 2 == 0 ? crackColor : baseColor;
        }
    }

    private void SetState(FloorState s)
    {
        State = s;
        stateTime = 0f;
        if (hole != null) hole.gameObject.SetActive(s == FloorState.Collapsed);
        if (sr == null) return;
        switch (s)
        {
            case FloorState.Intact: sr.color = baseColor; break;
            case FloorState.Cracking:
                VfxService.Hit(transform.position, Vector2.up, HitMaterial.Stone);
                break;
            case FloorState.Collapsed:
                sr.color = collapsedColor;
                VfxService.Hit(transform.position, Vector2.down, HitMaterial.Stone, true);
                VfxService.Shake(ShakeLevel.Light);
                break;
        }
    }
}
