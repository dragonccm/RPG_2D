using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Bộ chạy đòn đánh dùng chung (CORE_PROJECT_GUIDELINES 6.2). Gắn lên mọi Character cần đánh:
/// Player (qua PlayerCombat), quái (qua EnemyAttackController), boss (Agis).
/// Chạy 3 pha của AttackDef: Windup (animation + telegraph) → Active (quét/bắn 1 lần) → Recovery (nối đòn / hủy).
/// Một AttackRunner chỉ chạy 1 đòn một lúc. Bị choáng/hất/chết trong Windup → hủy đòn, không gây sát thương.
/// </summary>
[DisallowMultipleComponent]
public class AttackRunner : MonoBehaviour
{
    public enum Phase { Idle, Windup, Active, Recovery }

    public Phase CurrentPhase { get; private set; } = Phase.Idle;
    public AttackDef Current { get; private set; }
    /// <summary>Đòn vừa kết thúc gần nhất (để PlayerCombat biết nhịp kế của chuỗi).</summary>
    public AttackDef Last { get; private set; }
    public float LastFinishTime { get; private set; } = -999f;
    public bool IsBusy => CurrentPhase != Phase.Idle;

    public event Action<AttackDef> Started;
    /// <summary>(def, hoàn tất đủ 3 pha?) — false khi bị hủy giữa chừng.</summary>
    public event Action<AttackDef, bool> Finished;
    public event Action<Character, float, bool> Hit;

    private Character owner;
    private NavMeshAgent agent;
    private Rigidbody2D rb;
    private Animator animator;
    private float nextAllowed;
    private float recoveryElapsed;
    private AttackDef pendingChain;
    private Vector2 pendingChainAim;
    private GameObject telegraphGo;

    private static readonly Collider2D[] buffer = new Collider2D[32];

    private void Awake()
    {
        owner = GetComponent<Character>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void OnDisable() { Abort(); }

    public bool CanStart(AttackDef def)
    {
        if (def == null || IsBusy || Time.time < nextAllowed) return false;
        if (owner != null && (owner.IsDead || !owner.CanMove())) return false;
        return true;
    }

    /// <summary>Bắt đầu 1 đòn. center != null: tâm vùng trúng cố định (AoE đặt dưới chân mục tiêu).</summary>
    public bool TryStart(AttackDef def, Vector2 aim, Vector2? center = null, GameObject projectilePrefab = null, bool showTelegraph = true)
    {
        if (!CanStart(def)) return false;
        StartCoroutine(RunChain(def, aim, center, projectilePrefab, showTelegraph));
        return true;
    }

    /// <summary>
    /// Phiên bản coroutine để boss tự xâu chuỗi: <c>yield return runner.Perform(def, aim)</c>.
    /// Không kiểm tra cooldown (boss tự quản nhịp); vẫn từ chối nếu đang bận.
    /// </summary>
    public IEnumerator Perform(AttackDef def, Vector2 aim, Vector2? center = null, GameObject projectilePrefab = null, bool showTelegraph = true)
    {
        if (def == null || IsBusy) yield break;
        yield return RunChain(def, aim, center, projectilePrefab, showTelegraph);
    }

    /// <summary>Hủy đòn đang chạy (nối đòn kế): chỉ được khi đang Recovery, đã qua cancelFrom, và có def.next.</summary>
    public bool TryChain(Vector2 aim)
    {
        if (CurrentPhase != Phase.Recovery || Current == null || Current.next == null) return false;
        if (recoveryElapsed < Current.recovery * Current.cancelFrom) return false;
        pendingChain = Current.next;
        pendingChainAim = aim;
        return true;
    }

    /// <summary>Còn trong cửa sổ nối đòn sau khi đòn vừa xong (cho PlayerCombat khi runner đã Idle)?</summary>
    public AttackDef NextInChain()
    {
        if (Last == null || Last.next == null) return null;
        return Time.time - LastFinishTime <= Last.chainWindow ? Last.next : null;
    }

    public void Abort()
    {
        if (!IsBusy) return;
        StopAllCoroutines();
        ClearTelegraph();
        var def = Current;
        CurrentPhase = Phase.Idle;
        Current = null;
        pendingChain = null;
        Finished?.Invoke(def, false);
    }

    // ───────────────────────── chạy đòn ─────────────────────────

    private IEnumerator RunChain(AttackDef def, Vector2 aim, Vector2? center, GameObject prefab, bool tele)
    {
        while (def != null)
        {
            pendingChain = null;
            bool completed = false;
            yield return RunOne(def, aim, center, prefab, tele, ok => completed = ok);
            if (!completed) yield break;
            if (pendingChain == null) yield break;
            def = pendingChain;       // nối đòn: vào ngay, không cooldown
            aim = pendingChainAim;
            center = null;
        }
    }

    private IEnumerator RunOne(AttackDef def, Vector2 aim, Vector2? fixedCenter, GameObject prefab, bool showTelegraph, Action<bool> done)
    {
        if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
        aim.Normalize();
        Current = def;
        CurrentPhase = Phase.Windup;
        Started?.Invoke(def);
        PlayAnimation(def);
        FaceAim(aim);

        if (showTelegraph && def.telegraph) SpawnTelegraph(def, aim, fixedCenter);

        // ── Windup ──
        for (float t = 0f; t < def.windup; t += Time.deltaTime)
        {
            if (Interrupted()) { Cancel(done); yield break; }
            yield return null;
        }
        ClearTelegraph();
        if (Interrupted()) { Cancel(done); yield break; }

        // ── Active ──
        CurrentPhase = Phase.Active;
        Vector2 center = fixedCenter ?? AreaCenter(def, aim);
        Strike(def, aim, center, prefab);
        float activeTime = Mathf.Max(def.active, def.lungeDistance > 0f ? 0.12f : 0f);
        for (float t = 0f; t < activeTime; t += Time.deltaTime)
        {
            if (def.lungeDistance > 0f) Lunge(aim, def.lungeDistance * Time.deltaTime / activeTime);
            yield return null;
        }

        // ── Recovery ──
        CurrentPhase = Phase.Recovery;
        recoveryElapsed = 0f;
        while (recoveryElapsed < def.recovery && pendingChain == null)
        {
            recoveryElapsed += Time.deltaTime;
            yield return null;
        }

        Last = def;
        LastFinishTime = Time.time;
        nextAllowed = pendingChain == null ? Time.time + def.cooldown : 0f;
        CurrentPhase = Phase.Idle;
        Current = null;
        Finished?.Invoke(def, true);
        done(true);
    }

    private void Cancel(Action<bool> done)
    {
        ClearTelegraph();
        var def = Current;
        CurrentPhase = Phase.Idle;
        Current = null;
        nextAllowed = Time.time + 0.3f; // vừa bị ngắt đòn: nghỉ ngắn trước khi thử lại
        Finished?.Invoke(def, false);
        done(false);
    }

    private bool Interrupted() => owner != null && (owner.IsDead || !owner.CanMove());

    // ───────────────────────── vùng trúng ─────────────────────────

    private Vector2 AreaCenter(AttackDef def, Vector2 aim)
    {
        Vector2 pos = transform.position;
        return def.centerOnOwner ? pos : pos + aim * def.reach;
    }

    private void Strike(AttackDef def, Vector2 aim, Vector2 center, GameObject prefabOverride)
    {
        if (def.shape == AttackShape.Projectile)
        {
            Vector2 origin = (Vector2)transform.position + aim * 0.6f + Vector2.up * 0.3f;
            float dmg = owner != null ? owner.AttackPower * def.damageMultiplier : def.damageMultiplier;
            AttackProjectile.Spawn(def, owner, origin, aim, prefabOverride != null ? prefabOverride : def.projectilePrefab);
            return;
        }

        int count;
        var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        if (def.shape == AttackShape.Line)
        {
            Vector2 mid = (Vector2)transform.position + aim * (def.reach * 0.5f);
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            count = Physics2D.OverlapBox(mid, new Vector2(def.reach, def.radius * 2f), angle, filter, buffer);
        }
        else
        {
            count = Physics2D.OverlapCircle(center, def.radius, filter, buffer);
        }

        bool any = false;
        Vector2 ownerPos = transform.position;
        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            if (col == null) continue;
            var target = col.GetComponentInParent<Character>();
            if (target != null)
            {
                if (target == owner || target.IsDead || !IsValidTarget(def.team, target)) continue;
                if (HasHitBefore(i, target)) continue;
                Vector2 dir = ((Vector2)target.transform.position - ownerPos).normalized;
                float dmg = DealDamage(owner, def, target, dir, out bool crit);
                Hit?.Invoke(target, dmg, crit);
                GetComponent<Enemy>()?.RaiseDealDamage(target.gameObject, dmg);
                any = true;
            }
            else if (def.team == AttackTeam.Player)
            {
                // Vật thể thế giới chém được (công tắc, đuốc câu đố...)
                var world = col.GetComponentInParent<IDamageable>();
                if (world == null || world.IsDead) continue;
                world.TakeDamage(owner != null ? owner.AttackPower : 1f);
                VfxService.Hit(col.bounds.center, aim, HitMaterial.Stone);
                any = true;
            }
        }

        if (any && def.team == AttackTeam.Player) PlayerHitFeedback(def, aim);
    }

    // Một Character có nhiều collider → chỉ tính 1 lần trong cùng lượt quét
    private static bool HasHitBefore(int index, Character target)
    {
        for (int j = 0; j < index; j++)
            if (buffer[j] != null && buffer[j].GetComponentInParent<Character>() == target) return true;
        return false;
    }

    private void PlayerHitFeedback(AttackDef def, Vector2 aim)
    {
        VfxService.Slash(transform.position, aim, def.slashColor, def.slashSize);
        if (def.feedback == HitFeedback.None) return;
        var level = def.feedback == HitFeedback.Heavy ? ShakeLevel.Heavy : ShakeLevel.Light;
        VfxService.Shake(level);
        VfxService.HitStop(level);
    }

    // ───────────────────────── sát thương dùng chung (cả đạn) ─────────────────────────

    /// <summary>Team Player đánh mọi Character không phải player; Enemy/Boss chỉ đánh player.</summary>
    public static bool IsValidTarget(AttackTeam team, Character target)
    {
        bool targetIsPlayer = target.CompareTag("Player") || target.GetComponent<PlayerController>() != null;
        return team == AttackTeam.Player ? !targetIsPlayer : targetIsPlayer;
    }

    public static float DealDamage(Character attacker, AttackDef def, Character target, Vector2 dir, out bool crit)
    {
        float baseAtk = attacker != null ? attacker.AttackPower : 10f;
        float dmg = baseAtk * def.damageMultiplier;
        crit = def.canCrit && attacker != null && UnityEngine.Random.value < attacker.CriticalChance;
        if (crit) dmg *= attacker.CriticalMultiplier;

        if (def.knockback > 0f) target.TakeDamageWithKnockback(dmg, def.knockback, dir, crit);
        else target.TakeDamage(dmg, crit);
        if (def.stunSeconds > 0f && !target.IsDead) target.ApplyStun(def.stunSeconds);
        return dmg;
    }

    // ───────────────────────── animation / telegraph / lunge ─────────────────────────

    private void PlayAnimation(AttackDef def)
    {
        if (string.IsNullOrEmpty(def.animTrigger)) return; // chủ thể tự lo animation (boss)
        var enemy = GetComponent<Enemy>();
        if (enemy != null && enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.PlayAttackAnimation();
            return;
        }
        if (animator == null || string.IsNullOrEmpty(def.animTrigger)) return;
        if (HasParameter(def.animTrigger, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(def.animTrigger);
        if (HasParameter("ComboIndex", AnimatorControllerParameterType.Int)) animator.SetInteger("ComboIndex", def.comboIndex);
    }

    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        if (animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    private void FaceAim(Vector2 aim)
    {
        if (Mathf.Abs(aim.x) < 0.05f || GetComponent<PlayerController>() != null) return; // player tự lật theo input
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = aim.x < 0f;
    }

    private void SpawnTelegraph(AttackDef def, Vector2 aim, Vector2? fixedCenter)
    {
        ClearTelegraph();
        float time = def.windup + 0.05f;
        Vector3 pos = transform.position;
        switch (def.shape)
        {
            case AttackShape.Circle:
                telegraphGo = VfxService.Telegraph(fixedCenter.HasValue ? (Vector3)fixedCenter.Value : (Vector3)AreaCenter(def, aim),
                    def.radius, time, def.telegraphColor);
                break;
            case AttackShape.Line:
                telegraphGo = VfxService.TelegraphRect(pos, aim, def.reach + def.lungeDistance, def.radius * 2f, time, def.telegraphColor);
                break;
            default:
                telegraphGo = VfxService.TelegraphRect(pos, aim, Mathf.Min(def.projectileRange, 6f), Mathf.Max(0.3f, def.radius * 2f), time, def.telegraphColor);
                break;
        }
    }

    private void ClearTelegraph()
    {
        if (telegraphGo != null) Destroy(telegraphGo);
        telegraphGo = null;
    }

    private void Lunge(Vector2 aim, float distance)
    {
        Vector3 delta = (Vector3)(aim * distance);
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Move(delta);
        else if (rb != null) rb.MovePosition(rb.position + (Vector2)delta);
        else transform.position += delta;
    }
}
