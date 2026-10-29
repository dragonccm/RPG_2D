using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    /// <summary>S2 (GAME_MASTER_PLAN 5.4): trạng thái nhân vật thay cờ bool rải rác.</summary>
    public enum PlayerState { Normal, Attack, Dash, Charge, Stunned, Dead }

    public float moveSpeed = 3.5f;
    public float smoothMoveTime = 0.2f; // Thời gian làm mượt di chuyển
    public float flipSmoothTime = 0.08f; // Thời gian làm mượt lật

    public Rigidbody2D rb;
    private Animator animator;
    private Character character; // Reference to Character component
    private ModularSkillManager skillManager; // Reference to skill manager for level up
    public Vector2 movement;
    private Vector2 currentVelocity; // Cho SmoothDamp
    private bool isBusy = false; // Tấn công/phòng thủ thì sẽ true (compat — đọc qua IsBusy())

    private float targetScaleX = 1f;
    private float currentScaleX = 1f;
    private float scaleVelocity = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        character = GetComponent<Character>();
        skillManager = GetComponent<ModularSkillManager>();
        currentScaleX = transform.localScale.x;
        
        // Đảm bảo animator bắt đầu ở trạng thái Idle
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            // Không set bất kỳ trigger nào ở đây
        }
    }

    void Update()
    {
        HandleLevelUpInput(); // Handle level up input first
        HandleUIToggle(); // Handle UI toggle

        // Check if character can move (not stunned, not being knocked back)
        bool canMove = character != null ? character.CanMove() : true;

        if (!isBusy && canMove)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            // P1-1 (GAME_MASTER_PLAN 3.1): chuẩn hoá chéo TẠI NGUỒN —
            // mọi hệ thống đọc .movement (dash/teleport/charge) đều nhận vector |v| ≤ 1
            movement = MovementMath.ClampDiagonal(new Vector2(moveX, moveY));

            // Debug animation state
            if (animator != null)
            {
                var currentState = animator.GetCurrentAnimatorStateInfo(0);
                if (currentState.IsName("Guard") && !Input.GetKey(KeyCode.LeftShift))
                {
                    Debug.LogWarning("🚨 UNEXPECTED Guard animation detected! Forcing back to Idle.");
                    animator.SetBool("IsMoving", false);
                }
            }

            animator.SetBool("IsMoving", movement.sqrMagnitude > 0);

            // Lật nhân vật theo hướng di chuyển trái/phải
            if (moveX > 0.01f)
                targetScaleX = 1f;
            else if (moveX < -0.01f)
                targetScaleX = -1f;

            // Attack with existing animation (phím J for testing)
            if (Input.GetKeyDown(KeyCode.J))
            {
                TriggerAttackAnimation();
            }
        }
        else
        {
            movement = Vector2.zero;
        }

        // Làm mượt lật nhân vật
        currentScaleX = Mathf.SmoothDamp(currentScaleX, targetScaleX, ref scaleVelocity, flipSmoothTime);
        Vector3 scale = transform.localScale;
        scale.x = currentScaleX;
        transform.localScale = scale;
    }

    /// <summary>
    /// Handle UI toggle with Tab key - Updated to use only SkillPanelUI
    /// </summary>
    private void HandleUIToggle()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Method 1: Try UIManager (primary method)
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ToggleSkillPanel();
                Debug.Log("🎮 Toggled skill panel via UIManager");
                return;
            }

            // Method 2: Try SkillPanelUI directly (fallback)
            var skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
            if (skillPanelUI != null)
            {
                skillPanelUI.TogglePanel();
                Debug.Log("🎮 Toggled skill panel via SkillPanelUI");
                return;
            }

            // If no UI system found
            Debug.LogWarning("❌ No UI system found! Please ensure UIManager or SkillPanelUI is in the scene.");
        }
    }

    /// <summary>
    /// Handle level up input - Press V to gain 10 levels
    /// </summary>
    private void HandleLevelUpInput()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            LevelUpBy10();
        }
    }

    /// <summary>
    /// Trigger the existing Attack animation for testing
    /// </summary>
    private void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            isBusy = true;
            animator.SetTrigger("Attack"); // Uses existing animation parameter
            movement = Vector2.zero;

            Debug.Log("🗡️ Triggered Attack animation (J key test)");

            // Auto-reset busy state after animation
            StartCoroutine(ResetBusyAfterDelay(1f)); // Default 1 second
        }
        else
        {
            Debug.LogWarning("Animator not found! Cannot trigger Attack animation.");
        }
    }

    /// <summary>
    /// Increase player level by 10 when V key is pressed
    /// </summary>
    private void LevelUpBy10()
    {
        if (skillManager != null)
        {
            int currentLevel = skillManager.GetPlayerLevel();
            int newLevel = currentLevel + 10;
            skillManager.SetPlayerLevel(newLevel);

            Debug.Log($"🎉 Level Up! {currentLevel} → {newLevel} (Press V)");

            // Show level up effect or animation if needed
            ShowLevelUpEffect();
        }
        else
        {
            Debug.LogWarning("ModularSkillManager not found! Cannot level up.");
        }
    }

    /// <summary>
    /// Show visual effect when leveling up
    /// </summary>
    private void ShowLevelUpEffect()
    {
        // You can add particle effects, animations, or UI notifications here
        // For now, just log the new status
        if (skillManager != null)
        {
            int unlockedSlots = skillManager.GetUnlockedSlots().Count;
            Debug.Log($"✨ New Level: {skillManager.GetPlayerLevel()}");
            Debug.Log($"🎯 Unlocked Skill Slots: {unlockedSlots}/8");

            // Optional: Play level up sound effect here
            // AudioSource.PlayClipAtPoint(levelUpSound, transform.position);

            // Optional: Trigger level up animation
            if (animator != null)
            {
                // Note: You can create a "LevelUp" trigger in animator if desired
                // animator.SetTrigger("LevelUp");
            }
        }
    }

    /// <summary>
    /// Reset busy state after a delay - used for animation timing
    /// </summary>
    private System.Collections.IEnumerator ResetBusyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isBusy = false;
        Debug.Log("Player is no longer busy - can move again");
    }

    void FixedUpdate()
    {
        // Only apply movement if character can move and not busy
        bool canMove = character != null ? character.CanMove() : true;

        if (!isBusy && canMove)
        {
            // P1-1: movement đã chuẩn hoá ở Update (|v| ≤ 1) → tốc độ chéo = tốc độ trục.
            // Bỏ SmoothDamp per-axis: nội suy từng trục riêng làm cong quỹ đạo +
            // lệch tốc độ chéo. Di chuyển thẳng theo vector chuẩn hoá.
            rb.MovePosition(rb.position + movement * (moveSpeed * SpeedMultiplier * Time.fixedDeltaTime));
        }
    }

    // Gọi hàm này ở cuối mỗi animation tấn công hoặc phòng thủ (Animation Event)
    public void EndAction()
    {
        Debug.Log("EndAction called from Animation Event, setting isBusy to false");
        isBusy = false;
    }

    /// <summary>
    /// Called by skill system when using skills - uses skill-specific animation trigger
    /// </summary>
    public void TriggerSkillAnimation(string skillName = "", string animationTrigger = "")
    {
        if (animator != null)
        {
            isBusy = true;
            
            // Sử dụng animationTrigger từ skill hoặc fallback về "Attack"
            string trigger = !string.IsNullOrEmpty(animationTrigger) ? animationTrigger : "Attack";
            animator.SetTrigger(trigger);
            
            movement = Vector2.zero;

            string logMessage = string.IsNullOrEmpty(skillName) ?
                $"🗡️ Triggered {trigger} animation for skill" :
                $"🗡️ Triggered {trigger} animation for {skillName}";
            Debug.Log(logMessage);

            // Auto-reset if no Animation Event is set up
            StartCoroutine(ResetBusyAfterDelay(1f));
        }
    }

    /// <summary>
    /// Force stop player movement (used for external controls like knockback)
    /// </summary>
    public void ForceStopMovement()
    {
        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Check if player is currently busy with actions
    /// </summary>
    // ── Hệ số tốc độ từ môi trường (bùn, băng...) — nhiều nguồn chồng nhau lấy chậm nhất ──
    private readonly System.Collections.Generic.Dictionary<UnityEngine.Object, float> speedModifiers =
        new System.Collections.Generic.Dictionary<UnityEngine.Object, float>();

    /// <summary>Hệ số nhân tốc độ hiện tại (1 = bình thường).</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    public void SetSpeedModifier(UnityEngine.Object source, float multiplier)
    {
        if (source == null) return;
        speedModifiers[source] = Mathf.Clamp(multiplier, 0.05f, 3f);
        RecalculateSpeed();
    }

    public void RemoveSpeedModifier(UnityEngine.Object source)
    {
        if (source != null && speedModifiers.Remove(source)) RecalculateSpeed();
    }

    private void RecalculateSpeed()
    {
        float m = 1f;
        foreach (var v in speedModifiers.Values) m = Mathf.Min(m, v);
        SpeedMultiplier = m;
    }

    public bool IsBusy()
    {
        return isBusy;
    }

    /// <summary>
    /// S2: state hiện tại — isBusy ⇔ state khác Normal.
    /// </summary>
    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    /// <summary>
    /// Set busy state externally (PlayerState tùy chọn — special_move gọi Dash/Charge)
    /// </summary>
    public void SetBusy(bool busy, PlayerState state = PlayerState.Attack)
    {
        isBusy = busy;
        CurrentState = busy ? state : PlayerState.Normal;
        if (busy)
        {
            movement = Vector2.zero;
        }
    }

    /// <summary>
    /// Get current player level (for external access)
    /// </summary>
    public int GetCurrentLevel()
    {
        return skillManager != null ? skillManager.GetPlayerLevel() : 1;
    }

    /// <summary>
    /// Manually set player level (for debugging or cheat codes)
    /// </summary>
    public void SetLevel(int level)
    {
        if (skillManager != null)
        {
            skillManager.SetPlayerLevel(level);
            Debug.Log($"Player level set to: {level}");
        }
    }

    /// <summary>
    /// Check if player has the required components for skill system
    /// </summary>
    public bool ValidateComponents()
    {
        bool isValid = true;

        if (character == null)
        {
            Debug.LogError("Character component missing!");
            isValid = false;
        }

        if (skillManager == null)
        {
            Debug.LogError("ModularSkillManager component missing!");
            isValid = false;
        }

        if (animator == null)
        {
            Debug.LogError("Animator component missing!");
            isValid = false;
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component missing!");
            isValid = false;
        }

        return isValid;
    }

    private void OnValidate()
    {
        // Validate in editor
        if (Application.isPlaying)
        {
            ValidateComponents();
        }
    }
}