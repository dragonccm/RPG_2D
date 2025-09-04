using UnityEngine;

/// <summary>
/// Unified animation system to replace multiple animation controllers
/// Consolidates PlayerAnimatorController, EnemyAnimatorController, and direct animator calls
/// </summary>
public class UnifiedAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool enableDebugLogging = false;

    // Cached parameter hashes for performance
    private static readonly int isMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int speedHash = Animator.StringToHash("Speed");
    private static readonly int facingDirectionHash = Animator.StringToHash("FacingDirection");
    private static readonly int attackUpHash = Animator.StringToHash("AttackUp");
    private static readonly int attackDownHash = Animator.StringToHash("AttackDown");
    private static readonly int attackLeftHash = Animator.StringToHash("AttackLeft");
    private static readonly int attackRightHash = Animator.StringToHash("AttackRight");
    private static readonly int hurtHash = Animator.StringToHash("Hurt");
    private static readonly int dieHash = Animator.StringToHash("Die");
    private static readonly int attackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// Set movement parameters
    /// </summary>
    public void SetMovement(bool isMoving, float speed)
    {
        if (animator == null) return;

        animator.SetBool(isMovingHash, isMoving);
        animator.SetFloat(speedHash, speed);
    }

    /// <summary>
    /// Set facing direction for 4-directional animations
    /// </summary>
    public void SetFacingDirection(float direction)
    {
        if (animator == null || !HasParameter(facingDirectionHash)) return;

        animator.SetFloat(facingDirectionHash, direction);
    }

    /// <summary>
    /// Trigger attack animation with direction support
    /// </summary>
    public void TriggerAttack(AttackDirection direction = AttackDirection.Down)
    {
        if (animator == null) return;

        int triggerHash;
        bool shouldFlip = false;

        switch (direction)
        {
            case AttackDirection.Up:
                triggerHash = attackUpHash;
                break;
            case AttackDirection.Down:
                triggerHash = attackDownHash;
                break;
            case AttackDirection.Left:
                triggerHash = attackLeftHash;
                shouldFlip = true;
                break;
            case AttackDirection.Right:
                triggerHash = attackLeftHash;
                break;
            default:
                triggerHash = attackDownHash;
                break;
        }

        // Handle sprite flipping for left/right attacks
        if (shouldFlip || direction == AttackDirection.Right)
        {
            FlipSprite(direction == AttackDirection.Left);
        }

        if (HasParameter(triggerHash))
        {
            animator.SetTrigger(triggerHash);
        }
        else
        {
            // Fallback to basic attack
            animator.SetTrigger(attackHash);
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🎯 Attack triggered: {0}", direction));
        }
    }

    /// <summary>
    /// Trigger basic attack animation
    /// </summary>
    public void TriggerBasicAttack()
    {
        if (animator == null) return;

        animator.SetTrigger(attackHash);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🗡️ Basic attack triggered");
        }
    }

    /// <summary>
    /// Trigger hurt animation
    /// </summary>
    public void TriggerHurt()
    {
        if (animator == null) return;

        animator.SetTrigger(hurtHash);
    }

    /// <summary>
    /// Trigger death animation
    /// </summary>
    public void TriggerDeath()
    {
        if (animator == null) return;

        animator.SetTrigger(dieHash);
    }

    /// <summary>
    /// Trigger custom animation by name
    /// </summary>
    public void TriggerAnimation(string triggerName)
    {
        if (animator == null) return;

        animator.SetTrigger(triggerName);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🎬 Custom animation triggered: {0}", triggerName));
        }
    }

    /// <summary>
    /// Set animation parameter by name
    /// </summary>
    public void SetParameter(string paramName, bool value)
    {
        if (animator == null) return;

        animator.SetBool(paramName, value);
    }

    /// <summary>
    /// Set animation parameter by name
    /// </summary>
    public void SetParameter(string paramName, float value)
    {
        if (animator == null) return;

        animator.SetFloat(paramName, value);
    }

    /// <summary>
    /// Set animation parameter by name
    /// </summary>
    public void SetParameter(string paramName, int value)
    {
        if (animator == null) return;

        animator.SetInteger(paramName, value);
    }

    /// <summary>
    /// Check if animator has parameter
    /// </summary>
    private bool HasParameter(int paramHash)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (var param in animator.parameters)
        {
            if (param.nameHash == paramHash)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if animator has parameter by name
    /// </summary>
    private bool HasParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Flip sprite horizontally
    /// </summary>
    private void FlipSprite(bool flip)
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flip;
        }
    }

    /// <summary>
    /// Validate animator setup
    /// </summary>
    public bool IsValid()
    {
        return animator != null && animator.runtimeAnimatorController != null;
    }
}
