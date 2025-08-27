using System;
using UnityEngine;
using RPG.Animation;
using RPG.Effects;

/// <summary>
/// Enum for 4-way attack direction.
/// </summary>
public enum AttackDirection
{
    Up,
    Down,
    Left,
    Right
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    // Removed smoothMoveTime - using immediate movement for responsiveness
    public float flipSmoothTime = 0.08f; // Thời gian làm mượt lật

    [Header("🎮 4-Directional Attack Settings")]
    [Tooltip("Kích hoạt hệ thống tấn công 4 hướng thay vì flip trái/phải")]
    [SerializeField] private bool use4DirectionalAttacks = true;
    
    [Tooltip("Lưu lại hướng tấn công cuối cùng khi không di chuyển")]
    [SerializeField] private bool rememberLastDirection = true;
    
    [Tooltip("Hiển thị debug cho hướng tấn công hiện tại")]
    [SerializeField] private bool showAttackDirectionDebug = false;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerAnimatorController playerAnimatorController; // Reference to the new controller

    private Character character;
    private ModularSkillManager skillManager;
    private Vector2 movement;
    private Vector2 currentVelocity;
    private bool isBusy = false;
    private bool wasMovingLastFrame = false;
    private bool isPausedByMenu = false; // Theo dõi pause do menu

    // 4-directional attack system
    private Vector2 lastMovementDirection = Vector2.down;
    private AttackDirection currentFacingDirection = AttackDirection.Down;

    // Legacy flip system
    private float targetScaleX = 1f;
    private float currentScaleX = 1f;
    private float scaleVelocity = 0f;

    // Public properties
    public Vector2 Movement => movement;
    public AttackDirection CurrentFacingDirection => currentFacingDirection;

    /// <summary>
    /// Get or set 4-directional attack system enabled state
    /// </summary>
    public bool Use4DirectionalAttacks
    {
        get => use4DirectionalAttacks;
        set => use4DirectionalAttacks = value;
    }

    /// <summary>
    /// Check if player is currently busy with actions
    /// </summary>
    public bool IsBusy() => isBusy;

    /// <summary>
    /// Set busy state externally
    /// </summary>
    public void SetBusy(bool busy)
    {
        isBusy = busy;
        if (busy)
        {
            movement = Vector2.zero;
        }
    }

    /// <summary>
    /// Force stop player movement
    /// </summary>
    public void ForceStopMovement()
    {
        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void TriggerDirectionalAttackAnimation(AttackDirection direction)
    {
        if (playerAnimatorController == null) 
        {
            Debug.LogError($"❌ [{gameObject.name}] PlayerAnimatorController is null! Attack animation cannot be triggered.");
            return;
        }
        
        Debug.Log($"🎯 [{gameObject.name}] Triggering directional attack: {direction}");
        playerAnimatorController.TriggerAttack(direction);
        
        if (showAttackDirectionDebug)
        {
            Debug.Log($"🎯 Attack Direction: {direction}");
        }
    }

    /// <summary>
    /// Called by skill system when using skills
    /// </summary>
    public void TriggerSkillAnimation(string skillName = "", string animationTrigger = "")
    {
        if (animator != null)
        {
            isBusy = true;
            ForceStopMovement(); // Stop movement immediately when attacking
            
            // Check if the skill uses 4-directional attacks
            bool shouldUse4Directional = use4DirectionalAttacks;
            
            // Override if skill specifically requests 4-directional
            if (skillManager != null && HasActiveSkillWith4Directional())
            {
                shouldUse4Directional = true;
            }
            
            if (shouldUse4Directional)
            {
                Debug.Log($"🎯 [{gameObject.name}] Using 4-directional attack system, direction: {currentFacingDirection}");
                TriggerDirectionalAttackAnimation(currentFacingDirection);
            }
            else
            {
                string trigger = !string.IsNullOrEmpty(animationTrigger) ? animationTrigger : "Attack";
                Debug.Log($"🗡️ [{gameObject.name}] Using basic attack system, trigger: {trigger}");
                animator.SetTrigger(trigger);
            }
            
            string logMessage = string.IsNullOrEmpty(skillName) ?
                $"🗡️ Triggered animation for skill" :
                $"🗡️ Triggered animation for {skillName}";
                
            if (shouldUse4Directional)
            {
                logMessage += $" (Direction: {currentFacingDirection})";
            }
            
            Debug.Log(logMessage);

            // FALLBACK: Auto-reset busy state if OnActionComplete event doesn't fire
            StartCoroutine(ResetBusyAfterDelay(1.5f));
        }
    }

    /// <summary>
    /// Enhanced method to determine attack direction and trigger animation for skills
    /// </summary>
    public void TriggerSkillAnimationWithDirection(SkillModule skill, Vector2 targetDirection)
    {
        if (animator == null) return;
        
        isBusy = true;
        
        if (skill != null && skill.use4DirectionalAttack)
        {
            AttackDirection attackDir = GetDirectionFromVector(targetDirection);
            TriggerDirectionalAttackAnimation(attackDir);
            
            Debug.Log($"🗡️ Skill {skill.skillName} triggered with direction: {attackDir}");
        }
        else
        {
            string trigger = !string.IsNullOrEmpty(skill?.animationTrigger) ? skill.animationTrigger : "Attack";
            animator.SetTrigger(trigger);
            
            Debug.Log($"🗡️ Skill {skill?.skillName} triggered with animation: {trigger}");
        }
        
        // StartCoroutine(ResetBusyAfterDelay(1f)); // This is now handled by OnActionComplete event
    }

    /// <summary>
    /// Convert Vector2 direction to AttackDirection enum
    /// </summary>
    private AttackDirection GetDirectionFromVector(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return currentFacingDirection;
        
        Vector2 normalizedDir = direction.normalized;
        
        if (Mathf.Abs(normalizedDir.y) > Mathf.Abs(normalizedDir.x))
        {
            return normalizedDir.y > 0 ? AttackDirection.Up : AttackDirection.Down;
        }
        else
        {
            return normalizedDir.x > 0 ? AttackDirection.Right : AttackDirection.Left;
        }
    }

    /// <summary>
    /// Handle attack input - Press J for testing
    /// </summary>
    private void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            isBusy = true;
            ForceStopMovement(); // Stop movement immediately when attacking
            
            if (use4DirectionalAttacks)
            {
                TriggerDirectionalAttackAnimation(currentFacingDirection);
            }
            else
            {
                animator.SetTrigger(AnimationParameters.Attack);
            }
            
            string directionText = use4DirectionalAttacks ? $" ({currentFacingDirection})" : "";
            Debug.Log($"🗡️ Triggered Attack animation{directionText}");

            // FALLBACK: Auto-reset busy state if OnActionComplete event doesn't fire
            StartCoroutine(ResetBusyAfterDelay(1.2f));
        }
    }

    /// <summary>
    /// Reset busy state after a delay (fallback mechanism)
    /// </summary>
    private System.Collections.IEnumerator ResetBusyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only reset if still busy (animation event might have already reset it)
        if (isBusy)
        {
            isBusy = false;
            Debug.Log("⚠️ Fallback: Player is no longer busy (animation event didn't fire)");
        }
        else
        {
            Debug.Log("ℹ️ Animation event already reset busy state - fallback not needed");
        }
    }

    /// <summary>
    /// Check if any equipped skill uses 4-directional attacks
    /// </summary>
    private bool HasActiveSkillWith4Directional()
    {
        if (skillManager == null) return false;
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        foreach (var slot in unlockedSlots)
        {
            if (slot.HasSkill() && slot.equippedSkill.use4DirectionalAttack)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Handle UI toggle with Tab key and Escape key
    /// </summary>
    private void HandleUIToggle()
    {
        // Handle Tab key for Skill Panel
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ToggleSkillPanel();
                
                // Pause game when opening skill panel
                HandlePauseOnMenuOpen();
                return;
            }

            var skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
            if (skillPanelUI != null)
            {
                skillPanelUI.TogglePanel();
                
                // Pause game when opening skill panel
                HandlePauseOnMenuOpen();
                return;
            }

            Debug.LogWarning("❌ No UI system found!");
        }
        
        // Handle Escape key for Pause Menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.TogglePauseMenu();
                return;
            }
            
            // Fallback: Look for PauseMenu directly
            var pauseMenu = FindFirstObjectByType<PauseMenu>();
            if (pauseMenu != null)
            {
                pauseMenu.TogglePause();
                return;
            }
            
            // Try UI.PauseMenu namespace
            var pauseMenuUI = FindFirstObjectByType<UI.PauseMenu>();
            if (pauseMenuUI != null)
            {
                pauseMenuUI.TogglePause();
                return;
            }
            
            Debug.LogWarning("❌ No Pause Menu found!");
        }
    }
    
    /// <summary>
    /// Handle pausing game when opening menus
    /// </summary>
    private void HandlePauseOnMenuOpen()
    {
        // Check if any menu is currently open
        bool isAnyMenuOpen = false;
        
        var uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            var skillPanel = FindFirstObjectByType<SkillPanelUI>();
            if (skillPanel != null && skillPanel.IsVisible())
            {
                isAnyMenuOpen = true;
            }
        }
        
        // Get pause menu reference
        var pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (pauseMenu == null)
        {
            var uiPauseMenu = FindFirstObjectByType<UI.PauseMenu>();
            if (uiPauseMenu != null)
            {
                // Handle different PauseMenu types
            var pauseMenuComponent = uiPauseMenu.GetComponent<PauseMenu>();
            if (pauseMenuComponent != null)
            {
                pauseMenu = pauseMenuComponent;
            }
            }
        }
        
        if (pauseMenu != null)
        {
            if (isAnyMenuOpen && !isPausedByMenu)
            {
                // Pause game when menu opens
                pauseMenu.Pause();
                isPausedByMenu = true;
                Debug.Log("🎮 Game paused due to menu opening");
            }
            else if (!isAnyMenuOpen && isPausedByMenu)
            {
                // Resume game when menu closes (only if it was paused by menu)
                pauseMenu.Resume();
                isPausedByMenu = false;
                Debug.Log("🎮 Game resumed - menu closed");
            }
        }
    }

    /// <summary>
    /// Handle level up input - Press V to gain levels
    /// </summary>
    private void HandleLevelUpInput()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (skillManager != null)
            {
                int currentLevel = skillManager.GetPlayerLevel();
                int newLevel = currentLevel + 10;
                skillManager.SetPlayerLevel(newLevel);
                Debug.Log($"🎉 Level Up! {currentLevel} → {newLevel}");
            }
        }
    }

    /// <summary>
    /// Called by Animation Events to end action state
    /// </summary>
    public void EndAction()
    {
        isBusy = false;
    }

    /// <summary>
    /// Update facing direction based on movement input
    /// </summary>
    private void UpdateFacingDirection()
    {
        if (movement.sqrMagnitude > 0.01f)
        {
            Vector2 moveDirection = movement.normalized;
            
            if (rememberLastDirection)
            {
                lastMovementDirection = moveDirection;
            }
            
            if (Mathf.Abs(moveDirection.y) > Mathf.Abs(moveDirection.x))
            {
                currentFacingDirection = moveDirection.y > 0 ? AttackDirection.Up : AttackDirection.Down;
            }
            else
            {
                currentFacingDirection = moveDirection.x > 0 ? AttackDirection.Right : AttackDirection.Left;
            }
        }
        else if (!rememberLastDirection)
        {
            currentFacingDirection = AttackDirection.Down;
        }
    }

    /// <summary>
    /// Update visual direction based on 4-directional or flip system
    /// </summary>
    private void UpdateVisualDirection()
    {
        if (!use4DirectionalAttacks)
        {
            if (movement.x > 0.01f)
                targetScaleX = 1f;
            else if (movement.x < -0.01f)
                targetScaleX = -1f;
            
            currentScaleX = Mathf.SmoothDamp(currentScaleX, targetScaleX, ref scaleVelocity, flipSmoothTime);
            Vector3 scale = transform.localScale;
            scale.x = currentScaleX;
            transform.localScale = scale;
        }
        // The 4-directional logic is now handled by PlayerAnimatorController
    }

    /// <summary>
    /// Reset pause state when game starts
    /// </summary>
    private void ResetPauseState()
    {
        isPausedByMenu = false;
        
        // Ensure game is not paused on start
        var pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (pauseMenu == null)
        {
            var uiPauseMenu = FindFirstObjectByType<UI.PauseMenu>();
            if (uiPauseMenu != null)
            {
                // Handle different PauseMenu types
                var pauseMenuComponent = uiPauseMenu.GetComponent<PauseMenu>();
                if (pauseMenuComponent != null)
                {
                    pauseMenu = pauseMenuComponent;
                }
            }
        }
        
        if (pauseMenu != null && pauseMenu.IsPaused)
        {
            pauseMenu.Resume();
            Debug.Log("🎮 Game resumed on start");
        }
    }

    /// <summary>
    /// Apply combat lunge force when attacking
    /// </summary>
    public void ApplyCombatLunge(Vector2 direction, float force)
    {
        if (rb != null && force > 0f)
        {
            // Giảm lực lùi lại để tránh bị lùi xa quá mức
            float adjustedForce = force * 0.5f; // Giảm 50% lực lùi
            Vector2 lungeVelocity = direction.normalized * adjustedForce;
            rb.AddForce(lungeVelocity, ForceMode2D.Impulse);
            
            // Start coroutine to gradually reduce lunge force
            StartCoroutine(ReduceLungeForce());
        }
    }

    /// <summary>
    /// Gradually reduce lunge force after applying
    /// </summary>
    private System.Collections.IEnumerator ReduceLungeForce()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (rb != null)
        {
            // Gradually reduce velocity
            for (int i = 0; i < 10; i++)
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 0.1f);
                yield return new WaitForFixedUpdate();
            }
        }
    }

    /// <summary>
    /// Enhance skill animation with direction and lunge
    /// </summary>
    public void TriggerSkillAnimationEnhanced(SkillModule skill, Vector2 targetDirection)
    {
        if (animator == null) return;
        
        isBusy = true;
        ForceStopMovement(); // Stop movement immediately when attacking
        
        // Apply combat lunge if configured
        if (skill.attackLungeForce > 0f)
        {
            Vector2 attackDirection = targetDirection.normalized;
            if (attackDirection == Vector2.zero)
            {
                attackDirection = GetCurrentFacingDirectionVector();
            }
            
            ApplyCombatLunge(attackDirection, skill.attackLungeForce);
        }
        
        // Trigger appropriate animation
        if (skill != null && skill.use4DirectionalAttack)
        {
            AttackDirection attackDir = GetDirectionFromVector(targetDirection);
            TriggerDirectionalAttackAnimation(attackDir);
            
            Debug.Log($"🗡️ Enhanced Skill {skill.skillName} triggered with direction: {attackDir}");
        }
        else
        {
            string trigger = !string.IsNullOrEmpty(skill?.animationTrigger) ? skill.animationTrigger : "Attack";
            animator.SetTrigger(trigger);
            
            Debug.Log($"🗡️ Enhanced Skill {skill?.skillName} triggered with animation: {trigger}");
        }
        
        movement = Vector2.zero;
        
        // Use skill's damage delay if available
        float delay = skill?.damageDelay > 0 ? skill.damageDelay : 1f;
        StartCoroutine(ResetBusyAfterDelay(delay));
    }

    /// <summary>
    /// Get current facing direction as Vector2
    /// </summary>
    private Vector2 GetCurrentFacingDirectionVector()
    {
        return currentFacingDirection switch
        {
            AttackDirection.Up => Vector2.up,
            AttackDirection.Down => Vector2.down,
            AttackDirection.Left => Vector2.left,
            AttackDirection.Right => Vector2.right,
            _ => Vector2.down
        };
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

    #region Animation Event Handlers
    /// <summary>
    /// Called by animation events when attack hits
    /// </summary>
    public void OnAttackHit()
    {
        Debug.Log($"🗡️ [{gameObject.name}] Attack Hit Event");
        // Attack hit logic can be handled here
    }
    
    /// <summary>
    /// Called by animation events when action completes
    /// </summary>
    public void OnActionComplete()
    {
        Debug.Log($"✅ [{gameObject.name}] Action Complete Event - Resetting isBusy");
        isBusy = false; // Reset busy state
        
        // Stop any pending fallback coroutines since animation event fired correctly
        try
        {
            StopCoroutine(nameof(ResetBusyAfterDelay));
        }
        catch (System.Exception)
        {
            // Ignore if coroutine not running
        }
    }
    
    /// <summary>
    /// Called by animation events for footstep sounds
    /// </summary>
    public void OnFootstep()
    {
        Debug.Log($"👣 [{gameObject.name}] Footstep Event");
        // Footstep sound logic can be handled here
    }
    
    /// <summary>
    /// Generic animation event handler
    /// </summary>
    public void OnAnimationEvent()
    {
        Debug.Log($"🎬 [{gameObject.name}] Animation Event");
    }
    
    /// <summary>
    /// Animation start event handler
    /// </summary>
    public void OnAnimationStart()
    {
        Debug.Log($"▶️ [{gameObject.name}] Animation Start Event");
    }
    
    /// <summary>
    /// Animation end event handler
    /// </summary>
    public void OnAnimationEnd()
    {
        Debug.Log($"⏹️ [{gameObject.name}] Animation End Event");
    }
    #endregion
    
    /// <summary>
    /// Test direct attack animation bypassing skill system
    /// </summary>
    public void TestDirectAttackAnimation()
    {
        Debug.Log("🧪 Testing direct attack animation");
        
        if (animator == null)
        {
            Debug.LogError("❌ Animator is null!");
            return;
        }
        
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("❌ No Animator Controller assigned!");
            return;
        }
        
        isBusy = true;
        ForceStopMovement();
        
        if (use4DirectionalAttacks && playerAnimatorController != null)
        {
            Debug.Log($"🎯 Triggering 4-directional attack: {currentFacingDirection}");
            playerAnimatorController.TriggerAttack(currentFacingDirection);
        }
        else
        {
            Debug.Log("🗡️ Triggering basic attack");
            animator.SetTrigger("Attack");
        }
        
        // Fallback reset
        StartCoroutine(ResetBusyAfterDelay(1.2f));
    }

    private void OnValidate()
    {
        // Only validate in editor during play mode and if components are assigned
        if (Application.isPlaying && isActiveAndEnabled)
        {
            ValidateComponents();
        }
    }

    void Awake()
    {
        // Get required components
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        
        // Auto-setup PlayerAnimatorController if missing
        playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController == null)
        {
            playerAnimatorController = gameObject.AddComponent<PlayerAnimatorController>();
            Debug.Log($"[{gameObject.name}] Auto-added PlayerAnimatorController component");
        }
        
        character = GetComponent<Character>();
        
        // Setup responsive movement - high drag for immediate stop
        if (rb != null)
        {
            rb.linearDamping = 10f; // High drag for immediate stop when no input
            rb.angularDamping = 10f; // Prevent rotation
        }
        skillManager = GetComponent<ModularSkillManager>();
        
        // Initialize
        currentScaleX = transform.localScale.x;
        
        // Validate components
        ValidateComponents();
    }
    
    void Start()
    {
        // Reset pause state when game starts
        ResetPauseState();
    }

    void Update()
    {
        // Handle input and movement
        HandleInput();
        HandleMovement();
        HandleUIToggle();
        HandleLevelUpInput();
    }

    void FixedUpdate()
    {
        // Apply movement with physics
        ApplyMovement();
    }

    /// <summary>
    /// Handle player input for movement and actions
    /// </summary>
    private void HandleInput()
    {
        // Get movement input - allow limited movement during combat
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 inputMovement = new Vector2(horizontal, vertical);
        
        // Prevent movement during attacks
        if (isBusy)
        {
            movement = Vector2.zero;
        }
        else
        {
            movement = inputMovement;
        }

        // Handle attack input (J key for testing)
        if (Input.GetKeyDown(KeyCode.J) && !isBusy)
        {
            TriggerAttackAnimation();
        }
        
        // Handle direct attack test (K key)
        if (Input.GetKeyDown(KeyCode.K) && !isBusy)
        {
            TestDirectAttackAnimation();
        }
    }

    /// <summary>
    /// Handle movement logic and animation updates
    /// </summary>
    private void HandleMovement()
    {
        // Update movement state
        bool isMoving = movement.sqrMagnitude > 0.01f;
        
        // Update facing direction
        UpdateFacingDirection();
        
        // Update visual direction (sprite flipping or 4-directional)
        UpdateVisualDirection();
        
        // Track movement state changes
        if (isMoving != wasMovingLastFrame)
        {
            wasMovingLastFrame = isMoving;
            if (isMoving)
            {
                Debug.Log("🚶 Player started moving");
            }
            else
            {
                Debug.Log("⏸️ Player stopped moving");
            }
        }
    }

    /// <summary>
    /// Apply movement to Rigidbody2D - Responsive and snappy movement
    /// </summary>
    private void ApplyMovement()
    {
        if (rb == null) return;

        // Calculate target velocity - immediate response for snappy movement
        Vector2 targetVelocity = movement * moveSpeed;
        
        // Apply immediate movement without smoothing for responsiveness
        rb.linearVelocity = targetVelocity;
        
        // Update current velocity for other systems
        currentVelocity = targetVelocity;
    }
}