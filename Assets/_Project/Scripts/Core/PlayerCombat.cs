using UnityEngine;

/// <summary>
/// Combo đánh thường 3 nhịp của player, chạy qua AttackRunner (CORE_PROJECT_GUIDELINES 1.2 / 6.2).
/// Gắn vào Player (cùng PlayerController). Input: chuột trái hoặc phím J.
/// - Chuỗi đòn là dữ liệu: Resources/Attacks/Player_Slash1 → 2 → 3 (AttackDef.next). Sửa nhịp/sát thương bằng asset.
/// - Nối đòn: bấm đánh trong cửa sổ hủy của Recovery (hoặc chainWindow sau đó). Quá hạn → về nhịp 1.
/// - Hướng đánh theo chuột. Sát thương = AttackPower × damageMultiplier, chí mạng theo Character.
/// - Phản hồi (vệt chém, rung, hit-stop) do AttackRunner phát; máu/số sát thương do Character.TakeDamage.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Chuỗi đòn (để trống = Resources/Attacks/Player_Slash1)")]
    [SerializeField] private AttackDef comboHead;

    private PlayerController playerController;
    private Character character;
    private AttackRunner runner;

    /// <summary>Nhịp đang/vừa đánh (0 = nhịp đầu).</summary>
    public int ComboIndex { get; private set; }
    public AttackRunner Runner => runner;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        character = GetComponent<Character>();
        runner = GetComponent<AttackRunner>();
        if (runner == null) runner = gameObject.AddComponent<AttackRunner>();
        if (comboHead == null) comboHead = Resources.Load<AttackDef>("Attacks/Player_Slash1");
        if (comboHead == null) Debug.LogError("[PlayerCombat] Thiếu AttackDef Player_Slash1 (Resources/Attacks) — chạy Tools/RPG/Combat/Build Attack Defs");
    }

    private void OnEnable()
    {
        if (runner == null) return;
        runner.Started += OnAttackStarted;
        runner.Finished += OnAttackFinished;
    }

    private void OnDisable()
    {
        if (runner == null) return;
        runner.Started -= OnAttackStarted;
        runner.Finished -= OnAttackFinished;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) TryAttack();
    }

    private void TryAttack()
    {
        if (comboHead == null || playerController == null) return;
        if (character != null && !character.CanMove()) return;

        Vector2 aim = AimDirection();
        if (runner.IsBusy)
        {
            runner.TryChain(aim); // đang khựng: nối đòn kế nếu đã qua mốc hủy
            return;
        }
        if (playerController.IsBusy()) return; // đang Dash/Charge

        var def = runner.NextInChain() ?? comboHead;
        runner.TryStart(def, aim);
    }

    private void OnAttackStarted(AttackDef def)
    {
        ComboIndex = def.comboIndex;
        playerController.SetBusy(true);
        // Quest Q1: chạm tới nhịp cuối của chuỗi bằng nối đòn (combo trọn)
        if (def.next == null && def.comboIndex > 0 && QuestManager.Instance != null)
            QuestManager.Instance.ReportProgress("q1", "q1_combo");
    }

    private void OnAttackFinished(AttackDef def, bool completed)
    {
        // Nối đòn: Finished(def) phát trước khi đòn kế bắt đầu → SetBusy(false) rồi Started bật lại ngay
        playerController.SetBusy(false);
    }

    /// <summary>Hướng về chuột (plan 5.2); không có chuột → mặt đang quay.</summary>
    private Vector2 AimDirection()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 d = new Vector2(m.x - transform.position.x, m.y - transform.position.y);
            if (d.sqrMagnitude > 0.05f) return d.normalized;
        }
        var sr = GetComponentInChildren<SpriteRenderer>();
        return sr != null && sr.flipX ? Vector2.left : Vector2.right;
    }

    // ---------- Logic thuần để test (P0-6 / S2) ----------

    /// <summary>Nhịp kế: trong window → +1 (vòng qua mảng); ngoài window → về nhịp 0.</summary>
    public static int NextComboIndex(int current, bool withinWindow, int comboLength)
    {
        if (!withinWindow || comboLength <= 1) return 0;
        return (current + 1) % comboLength;
    }
}
