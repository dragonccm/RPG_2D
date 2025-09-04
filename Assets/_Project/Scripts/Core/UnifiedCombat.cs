using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unified combat system to replace multiple damage interfaces and combat controllers
/// Consolidates PlayerCombatController, EnemyCombatController, and damage interfaces
/// </summary>
public class UnifiedCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float baseAttackDamage = 10f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private bool enableDebugLogging = false;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip hitSound;

    private float lastAttackTime;
    private UnifiedAnimator animator;
    private AudioSource audioSource;

    private void Awake()
    {
        animator = GetComponent<UnifiedAnimator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Perform attack with unified damage calculation
    /// </summary>
    public bool PerformAttack(AttackDirection direction = AttackDirection.Down)
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return false; // Still on cooldown
        }

        lastAttackTime = Time.time;

        // Trigger animation
        if (animator != null)
        {
            animator.TriggerAttack(direction);
        }

        // Calculate attack position based on direction
        Vector2 attackPosition = GetAttackPosition(direction);

        // Find targets in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, attackRange, targetLayers);

        bool hitSomething = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // Don't hit self

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = CalculateDamage();
                ApplyDamage(damageable, damage, attackPosition);

                hitSomething = true;

                if (enableDebugLogging)
                {
                    PerformanceUtils.Log(PerformanceUtils.FormatString("💥 Damage dealt: {0} to {1}", damage, hit.gameObject.name));
                }
            }
        }

        if (!hitSomething && enableDebugLogging)
        {
            PerformanceUtils.Log("⚠️ Attack missed - no valid targets");
        }

        return hitSomething;
    }

    /// <summary>
    /// Perform basic attack without direction
    /// </summary>
    public bool PerformBasicAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return false;
        }

        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.TriggerBasicAttack();
        }

        // Use default attack position (forward)
        Vector2 attackPosition = (Vector2)transform.position + GetFacingDirection() * attackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, attackRange, targetLayers);

        bool hitSomething = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = CalculateDamage();
                ApplyDamage(damageable, damage, attackPosition);
                hitSomething = true;
            }
        }

        return hitSomething;
    }

    /// <summary>
    /// Calculate damage with modifiers
    /// </summary>
    private float CalculateDamage()
    {
        float damage = baseAttackDamage;

        // Add modifiers here (stats, equipment, buffs, etc.)
        // Example: damage *= strengthModifier;

        return damage;
    }

    /// <summary>
    /// Apply damage to target with effects
    /// </summary>
    private void ApplyDamage(IDamageable target, float damage, Vector2 hitPosition)
    {
        // Apply damage
        target.TakeDamage(damage);

        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            ObjectPool effectPool = ServiceLocator.Get<ObjectPool>();
            effectPool?.Spawn(hitEffectPrefab, hitPosition, Quaternion.identity);
        }

        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Trigger camera shake or screen effects
        // ServiceLocator.GetService<CameraController>()?.Shake(0.1f, 0.1f);
    }

    /// <summary>
    /// Get attack position based on direction
    /// </summary>
    private Vector2 GetAttackPosition(AttackDirection direction)
    {
        Vector2 basePosition = transform.position;
        float offset = attackRange;

        switch (direction)
        {
            case AttackDirection.Up:
                return basePosition + Vector2.up * offset;
            case AttackDirection.Down:
                return basePosition + Vector2.down * offset;
            case AttackDirection.Left:
                return basePosition + Vector2.left * offset;
            case AttackDirection.Right:
                return basePosition + Vector2.right * offset;
            default:
                return basePosition + Vector2.down * offset;
        }
    }

    /// <summary>
    /// Get current facing direction vector
    /// </summary>
    private Vector2 GetFacingDirection()
    {
        // This could be enhanced to use actual facing direction from movement
        return Vector2.down; // Default facing down
    }

    /// <summary>
    /// Check if attack is on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return Time.time - lastAttackTime < attackCooldown;
    }

    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public float GetRemainingCooldown()
    {
        return Mathf.Max(0, attackCooldown - (Time.time - lastAttackTime));
    }

    /// <summary>
    /// Set attack damage modifier
    /// </summary>
    public void SetDamageModifier(float modifier)
    {
        baseAttackDamage *= modifier;
    }

    /// <summary>
    /// Reset attack damage to base
    /// </summary>
    public void ResetDamageModifier()
    {
        // Reset to original value - this would need to store original value
        // baseAttackDamage = originalBaseDamage;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
