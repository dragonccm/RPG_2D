using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S4 v2 (GAME_MASTER_PLAN 5.2, 7): hệ kỹ năng đặc biệt gắn CÂY KỸ NĂNG.
/// Rewrite toàn bộ từ bản cũ — giữ tên class special_move để scene/prefab không vỡ.
///
/// Kiến trúc chuẩn:
/// - MỌI kỹ năng phải mở qua SkillTreeManager (gio_1 dash, khong_1 teleport, lua_1 charge).
///   Chưa mở nút → phím bấm vô hiệu + log 1 lần.
/// - Input theo plan 5.2: Shift = Dash, Q = Teleport, E = Charge (giữ để tích, thả để bùng).
/// - F = tương tác (Campfire/NPC) — không còn là charge key (tránh xung đột bản cũ).
/// - Số liệu từ Character stats: charge dmg = AttackPower × chargeDamageMultiplier (không hard-code).
/// - Nút tier 2–4 sửa HÀNH VI nhìn thấy được (đọc IsUnlocked):
///   gio_2 dash xuyên địch | gio_3 dash gây dmg | gio_4 dash xa + hồi nhanh hơn
///   khong_2 xuyên tường mỏng | khong_3 bản sao dụ địch 2s | khong_4 teleport xa hơn
///   lua_2 burn 3s | lua_3 stun 1s | lua_4 nổ rộng + dmg mạnh hơn
///   than_3a dash hồi −0.15s | than_4 (xử lý ở Character/SkillTreeManager — 60s sống sót 1 lần)
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class special_move : MonoBehaviour
{
    [Header("Dash (Gió — mở gio_1)")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashDistance = 5f;
    public float dashCooldown = 1f;
    public float dashDuration = 0.2f;

    [Header("Teleport (Không — mở khong_1)")]
    public KeyCode teleportKey = KeyCode.Q;
    public float teleportDistance = 8f;
    public float teleportCooldown = 5f;

    [Header("Charge (Lửa — mở lua_1) — GIỮ E tích, thả bùng")]
    public KeyCode chargeKey = KeyCode.E;
    public float chargeDamageMultiplier = 5f;   // dmg = AttackPower × hệ số
    public float chargeAttackRange = 3f;
    public float chargeFullTime = 1f;           // giây để tích đầy
    public float chargeMinFactor = 0.4f;        // bấm nhanh = yếu
    public float chargeMaxFactor = 1.5f;        // tích đầy = mạnh hơn bản gốc
    public float chargeCooldown = 1.5f;

    [Header("Effects (tuỳ chọn prefab)")]
    public GameObject dashEffect;
    public GameObject teleportEffect;
    public GameObject chargeAttackEffect;

    private readonly Dictionary<string, float> cooldownTimers = new Dictionary<string, float>
    {
        { "dash", 0f }, { "teleport", 0f }, { "charge", 0f },
    };

    private Rigidbody2D rb;
    private PlayerController playerController;
    private Character character;
    private Animator animator;
    private Collider2D selfCollider;

    private bool isDashing, isTeleporting, isCharging;
    private float chargeStartTime = -1f;

    // Hệ số mỗi nhánh (đọc từ SkillTreeManager mỗi lần dùng — không cache)
    private float DashCooldownEff
    {
        get
        {
            float cd = dashCooldown;
            if (Unlocked("than_3a")) cd -= 0.15f;
            if (Unlocked("gio_4")) cd *= 0.3f;
            return Mathf.Max(0.1f, cd);
        }
    }
    private float DashDistanceEff => Unlocked("gio_4") ? dashDistance * 1.5f : dashDistance;
    private float TeleportCooldownEff => Unlocked("khong_4") ? Mathf.Max(0.5f, teleportCooldown - 1f) : teleportCooldown;
    private float TeleportDistanceEff => Unlocked("khong_4") ? teleportDistance * 2f : teleportDistance;
    private float ChargeRadiusEff => Unlocked("lua_4") ? chargeAttackRange * 1.5f : chargeAttackRange;

    private static bool Unlocked(string nodeId) =>
        SkillTreeManager.Instance != null && SkillTreeManager.Instance.IsUnlocked(nodeId);

    private static bool WarnNotUnlockedOnce(string nodeId, ref bool warned)
    {
        if (Unlocked(nodeId)) return true;
        if (!warned)
        {
            Debug.Log($"🔒 [{nodeId}] chưa mở trên cây kỹ năng (Tab) — hãy mở nút nhánh tương ứng.");
            warned = true;
        }
        return false;
    }

    private bool warnedDash, warnedTeleport, warnedCharge;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
        selfCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        TickCooldowns();

        bool canAct = character != null && character.CanMove()
            && playerController != null && !playerController.IsBusy();

        // Charge: GIỮ phím để tích — chiếm state Charge, thả để bùng
        if (Unlocked("lua_1") && canAct && Input.GetKeyDown(chargeKey) && cooldownTimers["charge"] <= 0f)
            BeginCharge();
        if (isCharging && Input.GetKeyUp(chargeKey))
            ReleaseCharge();

        if (canAct && !isCharging)
        {
            if (Input.GetKeyDown(dashKey) && WarnNotUnlockedOnce("gio_1", ref warnedDash)
                && cooldownTimers["dash"] <= 0f)
                StartCoroutine(PerformDash());
            else if (Input.GetKeyDown(teleportKey) && WarnNotUnlockedOnce("khong_1", ref warnedTeleport)
                && cooldownTimers["teleport"] <= 0f)
                StartCoroutine(PerformTeleport());
        }

        // Mở khoá lần đầu → xoá cảnh báo
        if (Unlocked("gio_1")) warnedDash = false;
        if (Unlocked("khong_1")) warnedTeleport = false;
        if (Unlocked("lua_1")) warnedCharge = false;
    }

    private void TickCooldowns()
    {
        var keys = new List<string>(cooldownTimers.Keys);
        foreach (var k in keys)
            if (cooldownTimers[k] > 0f) cooldownTimers[k] -= Time.deltaTime;
    }

    // ================== DASH ==================
    private IEnumerator PerformDash()
    {
        isDashing = true;
        cooldownTimers["dash"] = DashCooldownEff;
        playerController.SetBusy(true, PlayerController.PlayerState.Dash);

        Vector2 dir = AimDirection();
        float dist = DashDistanceEff;

        VfxService.Impact(transform.position, new Color(0.75f, 0.68f, 0.55f), 0.8f);
        if (dashEffect != null) Instantiate(dashEffect, transform.position, Quaternion.identity);

        // gio_2: xuyên địch — bỏ qua va chạm với Enemy trong lúc dash
        var ignoredColliders = new List<Collider2D>();
        if (Unlocked("gio_2") && selfCollider != null)
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                var col = enemy.GetComponent<Collider2D>();
                if (col != null) { Physics2D.IgnoreCollision(selfCollider, col, true); ignoredColliders.Add(col); }
            }
        }

        // gio_3: dash gây dmg — mỗi địch chạm đúng 1 lần
        bool dashDamage = Unlocked("gio_3");
        float dashDmg = character.AttackPower * 2f;
        var alreadyHit = new HashSet<Object>();

        Vector2 start = rb.position;
        Vector2 target = start + dir * dist;
        float t = 0f;
        while (t < dashDuration)
        {
            t += Time.deltaTime;
            rb.MovePosition(Vector2.Lerp(start, target, t / dashDuration));

            if (dashDamage)
            {
                foreach (var hitCol in Physics2D.OverlapCircleAll(rb.position, 0.9f))
                {
                    var ch = hitCol.GetComponent<Character>();
                    if (ch != null && ch != character && !alreadyHit.Contains(ch))
                    {
                        alreadyHit.Add(ch);
                        ch.TakeDamageWithKnockback(dashDmg, 5f, dir);
                    }
                }
            }
            yield return null;
        }
        rb.MovePosition(target);

        if (animator != null) animator.SetTrigger("Dash");
        VfxService.Impact(transform.position, new Color(0.75f, 0.68f, 0.55f), 0.6f);

        foreach (var col in ignoredColliders)
            if (col != null) Physics2D.IgnoreCollision(selfCollider, col, false);

        isDashing = false;
        playerController.SetBusy(false);
    }

    // ================== TELEPORT ==================
    private IEnumerator PerformTeleport()
    {
        isTeleporting = true;
        cooldownTimers["teleport"] = TeleportCooldownEff;
        playerController.SetBusy(true, PlayerController.PlayerState.Dash);

        Vector2 dir = AimDirection();
        Vector2 dest = rb.position + dir * TeleportDistanceEff;

        if (teleportEffect != null) Instantiate(teleportEffect, transform.position, Quaternion.identity);

        // Raycast chặn tường. khong_2: bỏ qua "tường mỏng" (bounds ≤ 0.6u — rào/cổng nhỏ)
        float maxDist = TeleportDistanceEff;
        var hits = Physics2D.RaycastAll(rb.position, dir, maxDist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
            if (hit.collider.GetComponent<Character>() != null) continue; // địch không chặn
            var bounds = hit.collider.bounds;
            bool thinWall = Mathf.Max(bounds.extents.x, bounds.extents.y) <= 0.3f;
            if (thinWall && Unlocked("khong_2")) continue; // xuyên qua
            dest = hit.point - dir * 0.5f;                  // dừng trước tường dày
            break;
        }

        yield return new WaitForSeconds(0.1f);
        transform.position = dest;
        if (rb != null) rb.position = dest;
        if (teleportEffect != null) Instantiate(teleportEffect, dest, Quaternion.identity);
        if (animator != null) animator.SetTrigger("Teleport");

        // khong_3 "Phân Thân": bản sao giữ sự chú ý địch 2s
        if (Unlocked("khong_3")) StartCoroutine(DecoyRoutine(dest, 2f));

        yield return new WaitForSeconds(0.2f);
        isTeleporting = false;
        playerController.SetBusy(false);
    }

    private IEnumerator DecoyRoutine(Vector2 pos, float duration)
    {
        var decoy = WorldBuilder.CreateCircleSprite("Decoy_PhansThan", pos, new Color(0.8f, 0.8f, 1f, 0.4f), 0.45f);
        var taunted = new List<(EnemyAIController ai, Transform old)>();
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(e.transform.position, pos) > 12f) continue;
            var ai = e.GetComponent<EnemyAIController>();
            if (ai != null && ai.playerTarget != null)
            {
                taunted.Add((ai, ai.playerTarget));
                ai.playerTarget = decoy.transform;
            }
        }
        yield return new WaitForSeconds(duration);
        foreach (var (ai, old) in taunted)
            if (ai != null) ai.playerTarget = old;
        if (decoy != null) Destroy(decoy);
    }

    // ================== CHARGE (giữ-thả) ==================
    private void BeginCharge()
    {
        isCharging = true;
        chargeStartTime = Time.time;
        playerController.SetBusy(true, PlayerController.PlayerState.Charge);
        if (animator != null) animator.SetTrigger("ChargeAttack");
        if (chargeAttackEffect != null) Instantiate(chargeAttackEffect, transform.position, Quaternion.identity);
        VfxService.Impact(transform.position, new Color(1f, 0.4f, 0.1f), 1f);
    }

    private void ReleaseCharge()
    {
        isCharging = false;
        float held = Mathf.Clamp(Time.time - chargeStartTime, 0f, chargeFullTime);
        float factor = Mathf.Lerp(chargeMinFactor, chargeMaxFactor, held / chargeFullTime);
        cooldownTimers["charge"] = chargeCooldown;

        float dmg = character.AttackPower * chargeDamageMultiplier * factor;
        float radius = ChargeRadiusEff;

        foreach (var hitCol in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            if (!hitCol.CompareTag("Enemy")) continue;
            var enemyChar = hitCol.GetComponent<Character>();
            if (enemyChar == null) continue;

            Vector2 away = (hitCol.transform.position - transform.position).normalized;
            enemyChar.TakeDamageWithKnockback(dmg, 10f, away);
            if (Unlocked("lua_3")) enemyChar.ApplyStun(1f);
            if (Unlocked("lua_2")) StartCoroutine(BurnRoutine(enemyChar, 3f, character.AttackPower * 0.3f));
        }

        VfxService.Shake(0.12f * factor, 0.15f);
        VfxService.Impact(transform.position, new Color(1f, 0.45f, 0.1f), 1.2f + factor);
        playerController.SetBusy(false);
    }

    private IEnumerator BurnRoutine(Character target, float duration, float dmgPerTick)
    {
        float elapsed = 0f;
        const float tick = 1f;
        while (elapsed < duration && target != null && target.health != null && target.health.currentValue > 0f)
        {
            yield return new WaitForSeconds(tick);
            elapsed += tick;
            if (target != null) target.TakeDamage(dmgPerTick);
        }
    }

    // ================== HƯỚNG NHẮM ==================
    /// <summary>Hướng hành động: theo di chuyển → theo chuột → theo mặt lật.</summary>
    private Vector2 AimDirection()
    {
        if (playerController != null && playerController.movement.magnitude > 0.1f)
            return playerController.movement.normalized;
        var cam = Camera.main;
        if (cam != null)
        {
            var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 toMouse = new Vector2(mouseWorld.x - transform.position.x, mouseWorld.y - transform.position.y);
            if (toMouse.sqrMagnitude > 0.05f) return toMouse.normalized;
        }
        return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
    }

    // ================== API GIỮ TƯƠNG THÍCH ==================
    public bool CanUseDash() => cooldownTimers["dash"] <= 0f && !IsAnySpecialMoveActive();
    public bool CanUseTeleport() => cooldownTimers["teleport"] <= 0f && !IsAnySpecialMoveActive();
    public bool CanUseChargeAttack() => cooldownTimers["charge"] <= 0f && !IsAnySpecialMoveActive();
    public float GetDashCooldownProgress() => Mathf.Clamp01(cooldownTimers["dash"] / DashCooldownEff);
    public float GetTeleportCooldownProgress() => Mathf.Clamp01(cooldownTimers["teleport"] / TeleportCooldownEff);
    public float GetChargeAttackCooldownProgress() => Mathf.Clamp01(cooldownTimers["charge"] / chargeCooldown);
    public float GetDashCooldownRemaining() => Mathf.Max(0f, cooldownTimers["dash"]);
    public float GetTeleportCooldownRemaining() => Mathf.Max(0f, cooldownTimers["teleport"]);
    public float GetChargeAttackCooldownRemaining() => Mathf.Max(0f, cooldownTimers["charge"]);
    public void ResetAllCooldowns()
    {
        cooldownTimers["dash"] = 0f; cooldownTimers["teleport"] = 0f; cooldownTimers["charge"] = 0f;
    }
    public bool IsAnySpecialMoveActive() => isDashing || isTeleporting || isCharging;
    public bool IsSkillGated(string nodeId) => !Unlocked(nodeId); // test/UI đọc

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(AimDirectionEditor() * DashDistanceEff));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(AimDirectionEditor() * TeleportDistanceEff));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ChargeRadiusEff);
    }

    private Vector2 AimDirectionEditor() =>
        Application.isPlaying ? AimDirection()
        : (transform.localScale.x > 0 ? Vector2.right : Vector2.left);
}
