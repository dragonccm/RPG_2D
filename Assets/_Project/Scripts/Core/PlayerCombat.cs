using System.Collections;
using UnityEngine;

/// <summary>
/// S2 (GAME_MASTER_PLAN 5.2, sprint S2): combo đánh thường 3 nhịp của player.
/// Gắn vào Player (cùng PlayerController). Input: chuột trái hoặc phím J.
/// - Nhịp 1/2: 1.0× AttackPower; nhịp 3: 1.5× + screen shake nhẹ.
/// - Combo window 0.8s: nối tiếp nhịp kế; quá hạn → về nhịp 1.
/// - Chí mạng: roll theo Character.CriticalChance / CriticalMultiplier.
/// - Damage + feedback (damage number, impact, hit-stop) đi qua Character.TakeDamage
///   và VfxService — không vẽ trùng hệ thống cũ.
/// - Tái sử dụng FindEnemiesInRange + CalculateEnhancedDamage của SkillExecutorBase
///   (probe nội bộ) để bảo đảm guard "không bắn player".
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Combo (mảng 3 nhịp)")]
    [SerializeField] private float[] comboMultipliers = { 1f, 1f, 1.5f };
    [SerializeField] private float comboWindow = 0.8f;   // giây để nối nhịp kế

    [Header("Tấn công")]
    [SerializeField] private float attackRange = 1.6f;    // bán kính quét quanh điểm trước mặt
    [SerializeField] private float attackReach = 0.7f;    // tâm quét dịch trước mặt bao xa
    [SerializeField] private float attackCooldown = 0.45f;
    [SerializeField] private float windup = 0.25f;        // trễ khớp animation

    private PlayerController playerController;
    private Character character;
    private int comboIndex = 0;
    private float lastAttackTime = -999f;
    private float nextAttackAllowed = 0f;
    private Probe probe;

    private class Probe : SkillExecutorBase
    {
        public Probe(SkillModule module) : base(module) { }
        public override void Execute(Character user, Vector2 targetPosition) { }
        public Character[] EnemiesInRange(Vector2 center, float range, Character caster)
            => FindEnemiesInRange(center, range, caster);
    }

    public int ComboIndex => comboIndex;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        character = GetComponent<Character>();
        probe = new Probe(ScriptableObject.CreateInstance<SkillModule>());
    }

    private void Update()
    {
        bool pressed = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        if (pressed) TryAttack();
        // Combo window hết hạn → reset (checked lazily mỗi frame, không tốn allocation)
        if (comboIndex != 0 && Time.time - lastAttackTime > comboWindow)
            comboIndex = 0;
    }

    private void TryAttack()
    {
        if (playerController == null || playerController.IsBusy()) return;
        if (character != null && !character.CanMove()) return;
        if (Time.time < nextAttackAllowed) return;

        // Combo logic thuần (đã test): advance trong window, else reset
        bool withinWindow = (Time.time - lastAttackTime) <= comboWindow && lastAttackTime > -900f;
        comboIndex = NextComboIndex(comboIndex, withinWindow, comboMultipliers.Length);

        StartCoroutine(AttackRoutine(comboIndex));
        nextAttackAllowed = Time.time + attackCooldown;

        // S4 (plan 10 — Q1): báo khi tới nhịp finisher trong window (combo trọn)
        if (withinWindow && comboIndex == comboMultipliers.Length - 1 && QuestManager.Instance != null)
            QuestManager.Instance.ReportProgress("q1", "q1_combo");
        lastAttackTime = Time.time;
    }

    private static readonly Collider2D[] hitBuffer = new Collider2D[32];

    private Character[] QueryEnemies(Vector2 center, float range)
    {
        int count = Physics2D.OverlapCircleNonAlloc(center, range, hitBuffer);
        var list = new System.Collections.Generic.List<Character>(count);
        for (int i = 0; i < count; i++)
        {
            var col = hitBuffer[i];
            if (col == null) continue;
            var ch = col.GetComponent<Character>() ?? col.GetComponentInParent<Character>();
            if (ch != null && ch != character && !ch.IsDead && !list.Contains(ch))
            {
                // Loại trừ người chơi
                if (ch.CompareTag("Player") || ch.GetComponent<PlayerController>() != null) continue;
                list.Add(ch);
            }
        }
        // Fallback cho môi trường test EditMode hoặc mock không có 2D collider
        if (list.Count == 0 && probe != null)
        {
            return probe.EnemiesInRange(center, range, character);
        }
        return list.ToArray();
    }

    private IEnumerator AttackRoutine(int index)
    {
        playerController.SetBusy(true);
        var animator = playerController.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            // Set combo index parameter nếu animator có hỗ trợ
            animator.SetInteger("ComboIndex", index);
        }

        yield return new WaitForSeconds(windup);

        if (character == null) { playerController.SetBusy(false); yield break; }

        // Tâm quét: hướng về chuột (plan 5.2 "Hướng theo chuột"), không có chuột → theo mặt lật
        Vector2 aim = Vector2.right;
        var cam = Camera.main;
        if (cam != null)
        {
            var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 toMouse = new Vector2(mouseWorld.x - transform.position.x, mouseWorld.y - transform.position.y);
            if (toMouse.sqrMagnitude > 0.05f) aim = toMouse.normalized;
        }
        Vector2 center = (Vector2)transform.position + aim * attackReach;
        var targets = QueryEnemies(center, attackRange);

        bool isFinisher = index == comboMultipliers.Length - 1;
        bool anyHit = false;

        foreach (var target in targets)
        {
            // Chí mạng: roll từ chỉ số Character (plan 5.3: crit mặc định 10% ×2.0)
            float roll = Random.value;
            bool crit = roll < character.CriticalChance;
            float dmg = character.AttackPower * comboMultipliers[index];
            if (crit) dmg *= character.CriticalMultiplier;

            target.TakeDamage(dmg, crit);
            anyHit = true;
        }

        if (anyHit)
        {
            // Máu/tia lửa do Character.TakeDamage của mục tiêu phát; ở đây chỉ vệt chém + rung + hit-stop
            var level = isFinisher ? ShakeLevel.Heavy : ShakeLevel.Light;
            VfxService.Slash(transform.position, aim, isFinisher ? new Color(1f, 0.9f, 0.5f) : Color.white,
                isFinisher ? 1.3f : 1f);
            VfxService.Shake(level);
            VfxService.HitStop(level);
        }

        yield return new WaitForSeconds(Mathf.Max(0.02f, attackCooldown - windup));
        playerController.SetBusy(false);
    }

    // ---------- Logic thuần để test (P0-6 / S2) ----------

    /// <summary>Nhịp kế: trong window → +1 (vòng qua mảng); ngoài window → về nhịp 0.</summary>
    public static int NextComboIndex(int current, bool withinWindow, int comboLength)
    {
        if (!withinWindow || comboLength <= 1) return 0;
        return (current + 1) % comboLength;
    }
}
