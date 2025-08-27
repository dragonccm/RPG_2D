using UnityEngine;
using System.Collections;
using RPG.Animation;

/// <summary>
/// Special move system for player - handles dash, teleport, and charge attack abilities
/// This class provides cooldown management and key bindings for special moves
/// </summary>
public class special_move : MonoBehaviour
{
    [Header("Dash Settings")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashDistance = 5f;
    public float dashCooldown = 3f;
    public float dashDuration = 0.2f;
    
    [Header("Teleport Settings")]
    public KeyCode teleportKey = KeyCode.Space;
    public float teleportDistance = 8f;
    public float teleportCooldown = 5f;
    
    [Header("Charge Attack Settings")]
    public KeyCode chargeAttackKey = KeyCode.F;
    public float chargeAttackDamage = 50f;
    public float chargeAttackRange = 3f;
    public float chargeAttackCooldown = 7f;
    public float chargeAttackDuration = 1f;
    
    [Header("Effects")]
    public GameObject dashEffect;
    public GameObject teleportEffect;
    public GameObject chargeAttackEffect;
    
    // Private cooldown timers
    private float dashCooldownTimer = 0f;
    private float teleportCooldownTimer = 0f;
    private float chargeAttackCooldownTimer = 0f;
    
    // Component references
    private Rigidbody2D rb;
    private PlayerController playerController;
    private Character character;
    private Animator animator;
    
    // State tracking
    private bool isDashing = false;
    private bool isTeleporting = false;
    private bool isChargeAttacking = false;
    
    // Movement tracking
    private Vector2 lastMovementDirection = Vector2.right;

    void Start()
    {
        // Get component references
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
        
        // Validate components
        if (rb == null) Debug.LogWarning("special_move: Rigidbody2D not found!");
        if (playerController == null) Debug.LogWarning("special_move: PlayerController not found!");
        if (character == null) Debug.LogWarning("special_move: Character not found!");
    }

    void Update()
    {
        // Update cooldown timers
        UpdateCooldowns();
        
        // Track movement direction
        UpdateMovementDirection();
        
        // Handle input only if player can move
        if (character != null && character.CanMove() && playerController != null && !playerController.IsBusy())
        {
            HandleSpecialMoveInput();
        }
    }
    
    /// <summary>
    /// Update movement direction tracking
    /// </summary>
    private void UpdateMovementDirection()
    {
        // Get movement from PlayerController instead of reading input directly
        if (playerController != null)
        {
            Vector2 currentMovement = playerController.Movement;
            
            if (currentMovement.magnitude > 0.1f)
            {
                lastMovementDirection = currentMovement.normalized;
            }
        }
        else
        {
            // Fallback to direct input if PlayerController is not available
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector2 currentMovement = new Vector2(horizontal, vertical);
            
            if (currentMovement.magnitude > 0.1f)
            {
                lastMovementDirection = currentMovement.normalized;
            }
        }
    }
    
    /// <summary>
    /// Update all cooldown timers
    /// </summary>
    private void UpdateCooldowns()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
            
        if (teleportCooldownTimer > 0)
            teleportCooldownTimer -= Time.deltaTime;
            
        if (chargeAttackCooldownTimer > 0)
            chargeAttackCooldownTimer -= Time.deltaTime;
    }
    
    /// <summary>
    /// Handle input for special moves
    /// </summary>
    private void HandleSpecialMoveInput()
    {
        // Dash
        if (Input.GetKeyDown(dashKey) && CanUseDash())
        {
            UseDash();
        }
        
        // Teleport
        if (Input.GetKeyDown(teleportKey) && CanUseTeleport())
        {
            UseTeleport();
        }
        
        // Charge Attack
        if (Input.GetKeyDown(chargeAttackKey) && CanUseChargeAttack())
        {
            UseChargeAttack();
        }
    }
    
    #region Dash
    
    public bool CanUseDash()
    {
        return dashCooldownTimer <= 0 && !isDashing && !isTeleporting && !isChargeAttacking;
    }
    
    public void UseDash()
    {
        if (!CanUseDash()) return;
        
        StartCoroutine(PerformDash());
    }
    
    private System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;
        
        // Set player busy
        if (playerController != null)
            playerController.SetBusy(true);
        
        // Get dash direction (use last movement direction or forward)
        Vector2 dashDirection = lastMovementDirection;
        if (dashDirection.magnitude < 0.1f)
        {
            // Dash forward based on character facing direction
            dashDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        // Create dash effect
        if (dashEffect != null)
        {
            Instantiate(dashEffect, transform.position, Quaternion.identity);
        }
        
        // Perform dash movement
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + dashDirection * dashDistance;
        
        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dashDuration;
            
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, progress);
            if (rb != null)
                rb.MovePosition(currentPos);
            
            yield return null;
        }
        
        // Animation trigger
        if (animator != null)
            animator.SetTrigger(AnimationParameters.Dash);
        
        isDashing = false;
        
        // Release player busy state
        if (playerController != null)
            playerController.SetBusy(false);
            
        Debug.Log("Dash completed!");
    }
    
    public float GetDashCooldownProgress()
    {
        return Mathf.Clamp01(dashCooldownTimer / dashCooldown);
    }
    
    #endregion
    
    #region Teleport
    
    public bool CanUseTeleport()
    {
        return teleportCooldownTimer <= 0 && !isDashing && !isTeleporting && !isChargeAttacking;
    }
    
    public void UseTeleport()
    {
        if (!CanUseTeleport()) return;
        
        StartCoroutine(PerformTeleport());
    }
    
    private System.Collections.IEnumerator PerformTeleport()
    {
        isTeleporting = true;
        teleportCooldownTimer = teleportCooldown;
        
        // Set player busy
        if (playerController != null)
            playerController.SetBusy(true);
        
        // Create teleport effect at start position
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, transform.position, Quaternion.identity);
        }
        
        // Get teleport direction (use last movement direction or forward)
        Vector2 teleportDirection = lastMovementDirection;
        if (teleportDirection.magnitude < 0.1f)
        {
            // Teleport forward based on character facing direction
            teleportDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        // Calculate teleport destination
        Vector2 teleportDestination = (Vector2)transform.position + teleportDirection * teleportDistance;
        
        // Check for obstacles (optional - can be enhanced)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, teleportDirection, teleportDistance);
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            teleportDestination = hit.point - teleportDirection * 0.5f; // Stop before obstacle
        }
        
        // Brief pause for teleport effect
        yield return new WaitForSeconds(0.1f);
        
        // Instant teleportation
        transform.position = teleportDestination;
        if (rb != null)
            rb.position = teleportDestination;
        
        // Create teleport effect at destination
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, teleportDestination, Quaternion.identity);
        }
        
        // Animation trigger
        if (animator != null)
            animator.SetTrigger(AnimationParameters.Teleport);
        
        yield return new WaitForSeconds(0.2f);
        
        isTeleporting = false;
        
        // Release player busy state
        if (playerController != null)
            playerController.SetBusy(false);
            
        Debug.Log("Teleport completed!");
    }
    
    public float GetTeleportCooldownProgress()
    {
        return Mathf.Clamp01(teleportCooldownTimer / teleportCooldown);
    }
    
    #endregion
    
    #region Charge Attack
    
    public bool CanUseChargeAttack()
    {
        return chargeAttackCooldownTimer <= 0 && !isDashing && !isTeleporting && !isChargeAttacking;
    }
    
    public void UseChargeAttack()
    {
        if (!CanUseChargeAttack()) return;
        
        StartCoroutine(PerformChargeAttack());
    }
    
    private System.Collections.IEnumerator PerformChargeAttack()
    {
        isChargeAttacking = true;
        chargeAttackCooldownTimer = chargeAttackCooldown;
        
        // Set player busy
        if (playerController != null)
            playerController.SetBusy(true);
        
        // Animation trigger
        if (animator != null)
            animator.SetTrigger(AnimationParameters.ChargeAttack);
        
        // Create charge attack effect
        if (chargeAttackEffect != null)
        {
            Instantiate(chargeAttackEffect, transform.position, Quaternion.identity);
        }
        
        // Wait for charge up
        yield return new WaitForSeconds(chargeAttackDuration * 0.5f);
        
        // Deal damage to enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, chargeAttackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                var enemy = hit.GetComponent<Character>();
                if (enemy != null)
                {
                    enemy.TakeDamage(chargeAttackDamage);
                    Debug.Log($"Charge attack hit {hit.name} for {chargeAttackDamage} damage!");
                }
            }
        }
        
        // Wait for attack completion
        yield return new WaitForSeconds(chargeAttackDuration * 0.5f);
        
        isChargeAttacking = false;
        
        // Release player busy state
        if (playerController != null)
            playerController.SetBusy(false);
            
        Debug.Log("Charge attack completed!");
    }
    
    public float GetChargeAttackCooldownProgress()
    {
        return Mathf.Clamp01(chargeAttackCooldownTimer / chargeAttackCooldown);
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Get remaining cooldown time for dash
    /// </summary>
    public float GetDashCooldownRemaining()
    {
        return Mathf.Max(0f, dashCooldownTimer);
    }
    
    /// <summary>
    /// Get remaining cooldown time for teleport
    /// </summary>
    public float GetTeleportCooldownRemaining()
    {
        return Mathf.Max(0f, teleportCooldownTimer);
    }
    
    /// <summary>
    /// Get remaining cooldown time for charge attack
    /// </summary>
    public float GetChargeAttackCooldownRemaining()
    {
        return Mathf.Max(0f, chargeAttackCooldownTimer);
    }
    
    /// <summary>
    /// Reset all cooldowns (for testing/debugging)
    /// </summary>
    public void ResetAllCooldowns()
    {
        dashCooldownTimer = 0f;
        teleportCooldownTimer = 0f;
        chargeAttackCooldownTimer = 0f;
        Debug.Log("All special move cooldowns reset!");
    }
    
    /// <summary>
    /// Check if any special move is currently active
    /// </summary>
    public bool IsAnySpecialMoveActive()
    {
        return isDashing || isTeleporting || isChargeAttacking;
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        // Draw dash range
        Gizmos.color = Color.blue;
        Vector2 dashDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dashDir * dashDistance);
        
        // Draw teleport range
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + dashDir * teleportDistance);
        
        // Draw charge attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chargeAttackRange);
    }
    
    #endregion
}