using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Một ngọn đuốc của TorchPuzzle: chém trúng hoặc nhấn F khi đứng gần để thắp.
/// Ngọn lửa (Light2D) nhỏ dần theo 4 bậc khi sắp tắt — người chơi đọc được thời gian còn lại.
/// </summary>
public class PuzzleTorch : MonoBehaviour, IDamageable
{
    [SerializeField] private float interactRange = 1.4f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private Color litColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color unlitColor = new Color(0.35f, 0.32f, 0.3f);

    private TorchPuzzle puzzle;
    private int index;
    private SpriteRenderer sr;
    private Light2D flame;
    private Transform player;

    public float CurrentHealth => 1f;
    public float MaxHealth => 1f;
    public bool IsDead => false;

    public void Bind(TorchPuzzle owner, int i)
    {
        puzzle = owner;
        index = i;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        flame = GetComponentInChildren<Light2D>();
        if (flame == null)
        {
            var go = new GameObject("Flame");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            flame = go.AddComponent<Light2D>();
            flame.lightType = Light2D.LightType.Point;
            flame.color = litColor;
            flame.pointLightOuterRadius = 3f;
            LightUtil.TargetAllLayers(flame);
        }
    }

    private void Update()
    {
        if (puzzle == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (player == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }
            if (player != null && ((Vector2)(player.position - transform.position)).sqrMagnitude <= interactRange * interactRange)
                Ignite();
        }

        bool lit = puzzle.IsLit(index);
        // 4 bậc lửa theo thời gian còn lại
        float r = puzzle.Remaining01(index);
        float step = lit ? Mathf.Ceil(r * 4f) / 4f : 0f;
        if (flame != null)
        {
            flame.enabled = lit;
            flame.intensity = 0.4f + 1.0f * step;
        }
        if (sr != null) sr.color = lit ? Color.Lerp(unlitColor, litColor, 0.4f + 0.6f * step) : unlitColor;
    }

    private void Ignite()
    {
        bool wasLit = puzzle.IsLit(index);
        puzzle.Light(index);
        if (!wasLit) VfxService.Impact(transform.position + Vector3.up * 0.5f, litColor, 0.6f);
    }

    // Chém trúng đuốc cũng thắp (combat + câu đố)
    public void TakeDamage(float damage) => Ignite();
    public void Heal(float amount) { }
}
