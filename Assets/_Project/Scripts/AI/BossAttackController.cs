using UnityEngine;

/// <summary>
/// Enhanced Boss Attack Controller - Specialized attack system for bosses
/// Extends EnemyAttackController with boss-specific features
/// </summary>
public class BossAttackController : EnemyAttackController
{
    [Header("?? Boss Specific Settings")]
    [Tooltip("Damage multiplier cho boss attacks")]
    [SerializeField] private float bossDamageMultiplier = 2f;
    
    [Tooltip("Boss có th? t?n công nhi?u m?c tiêu cùng lúc")]
    [SerializeField] public bool canAttackMultipleTargets = true;
    
    [Tooltip("S? l??ng target t?i ?a cho multi-target attacks")]
    [SerializeField] public int maxTargets = 3;
    
    [Tooltip("Ph?m vi t?n công cho multi-target")]
    [SerializeField] private float multiTargetRange = 4f;
    
    [Header("?? Special Attacks")]
    [Tooltip("Chance ?? s? d?ng special attack")]
    [SerializeField] public float specialAttackChance = 0.3f;
    
    [Tooltip("Cooldown cho special attacks")]
    [SerializeField] private float specialAttackCooldown = 5f;
    
    [Tooltip("Damage multiplier cho special attacks")]
    [SerializeField] private float specialDamageMultiplier = 3f;
    
    [Header("??? Area Attack Settings")]
    [Tooltip("Prefab cho area attack effect")]
    [SerializeField] private GameObject areaAttackPrefab;
    
    [Tooltip("Radius cho area attacks")]
    [SerializeField] private float areaAttackRadius = 3f;
    
    [Tooltip("Delay tr??c khi area attack gây damage")]
    [SerializeField] private float areaAttackDelay = 1f;
    
    private float lastSpecialAttackTime;
    private bool isPerformingSpecialAttack = false;
    
    // Properties
    public bool CanUseSpecialAttack => Time.time >= lastSpecialAttackTime + specialAttackCooldown;
    public float SpecialAttackCooldownRemaining => Mathf.Max(0f, lastSpecialAttackTime + specialAttackCooldown - Time.time);
    
    /// <summary>
    /// Enhanced boss attack with chance for special attacks
    /// </summary>
    public override bool Attack(Transform target)
    {
        if (!CanAttack(target))
            return false;
        
        // Decide attack type
        bool useSpecialAttack = CanUseSpecialAttack && Random.Range(0f, 1f) < specialAttackChance;
        
        if (useSpecialAttack)
        {
            return PerformSpecialAttack(target);
        }
        else if (canAttackMultipleTargets)
        {
            return PerformMultiTargetAttack(target);
        }
        else
        {
            return PerformSingleTargetAttack(target);
        }
    }
    
    /// <summary>
    /// Perform single target attack
    /// </summary>
    protected virtual bool PerformSingleTargetAttack(Transform target)
    {
        // Use enhanced damage calculation for boss
        return base.Attack(target);
    }
    
    /// <summary>
    /// Perform multi-target attack
    /// </summary>
    protected virtual bool PerformMultiTargetAttack(Transform primaryTarget)
    {
        Debug.Log($"[{gameObject.name}] Performing multi-target attack!");
        
        // Find all targets in range
        var targets = FindTargetsInRange(primaryTarget.position, multiTargetRange);
        
        if (targets.Count == 0)
        {
            return PerformSingleTargetAttack(primaryTarget);
        }
        
        bool anyHit = false;
        int targetCount = 0;
        
        // Attack each target
        foreach (var target in targets)
        {
            if (targetCount >= maxTargets) break;
            
            if (AttackTarget(target, bossDamageMultiplier))
            {
                anyHit = true;
                targetCount++;
            }
        }
        
        // Visual effects for multi-target
        SpawnMultiTargetEffect(primaryTarget.position);
        
        // Update cooldown
        lastAttackTime = Time.time;
        
        return anyHit;
    }
    
    /// <summary>
    /// Perform special attack with enhanced effects
    /// </summary>
    protected virtual bool PerformSpecialAttack(Transform target)
    {
        if (isPerformingSpecialAttack) return false;
        
        Debug.Log($"[{gameObject.name}] Performing SPECIAL ATTACK!");
        
        // Start special attack coroutine
        StartCoroutine(SpecialAttackSequence(target));
        
        lastSpecialAttackTime = Time.time;
        return true;
    }
    
    /// <summary>
    /// Special attack sequence
    /// </summary>
    protected virtual System.Collections.IEnumerator SpecialAttackSequence(Transform target)
    {
        isPerformingSpecialAttack = true;
        
        // Phase 1: Charge up
        Debug.Log($"[{gameObject.name}] Charging special attack...");
        SpawnChargeEffect();
        yield return new WaitForSeconds(0.5f);
        
        // Phase 2: Area attack
        Vector3 attackCenter = target.position;
        SpawnAreaAttackIndicator(attackCenter);
        yield return new WaitForSeconds(areaAttackDelay);
        
        // Phase 3: Deal damage
        PerformAreaAttack(attackCenter);
        
        // Phase 4: Cleanup
        yield return new WaitForSeconds(0.5f);
        isPerformingSpecialAttack = false;
        
        Debug.Log($"[{gameObject.name}] Special attack completed!");
    }
    
    /// <summary>
    /// Perform area attack at specified position
    /// </summary>
    protected virtual void PerformAreaAttack(Vector3 center)
    {
        // Find all targets in area
        var targets = FindTargetsInRange(center, areaAttackRadius);
        
        Debug.Log($"[{gameObject.name}] Area attack hitting {targets.Count} targets!");
        
        // Deal damage to all targets
        foreach (var target in targets)
        {
            AttackTarget(target, specialDamageMultiplier);
        }
        
        // Spawn area effect
        SpawnAreaAttackEffect(center);
    }
    
    /// <summary>
    /// Find all damageable targets in range
    /// </summary>
    protected virtual System.Collections.Generic.List<Transform> FindTargetsInRange(Vector3 center, float range)
    {
        var targets = new System.Collections.Generic.List<Transform>();
        
        // Find all colliders in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, range);
        
        foreach (var collider in colliders)
        {
            // Skip self
            if (collider.transform == transform) continue;
            
            // Check if target is player
            if (collider.CompareTag("Player"))
            {
                var damageable = collider.GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    targets.Add(collider.transform);
                }
            }
        }
        
        return targets;
    }
    
    /// <summary>
    /// Attack specific target with damage multiplier
    /// </summary>
    protected virtual bool AttackTarget(Transform target, float damageMultiplier)
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return false;
        
        // Calculate enhanced boss damage
        float damage = CalculateAttackDamage() * damageMultiplier;
        
        // Deal damage through system
        bool success = DamageSystemManager.DealDamage(
            gameObject,
            damageable,
            damage,
            defaultDamageType,
            canCriticalHit
        );
        
        if (success)
        {
            // Apply enhanced knockback for boss attacks
            ApplyEnhancedKnockback(target, damageMultiplier);
            
            Debug.Log($"[{gameObject.name}] Boss attack dealt {damage:F1} damage to {target.name}");
        }
        
        return success;
    }
    
    /// <summary>
    /// Apply enhanced knockback for boss attacks
    /// </summary>
    protected virtual void ApplyEnhancedKnockback(Transform target, float multiplier)
    {
        var character = target.GetComponent<Character>();
        if (character != null)
        {
            Vector2 knockbackDirection = (target.position - transform.position).normalized;
            float enhancedKnockback = knockbackForce * multiplier;
            character.ApplyKnockback(enhancedKnockback, knockbackDirection);
        }
    }
    
    /// <summary>
    /// Visual effects methods
    /// </summary>
    protected virtual void SpawnChargeEffect()
    {
        // Create charge effect around boss
        var effect = Effect2DManager.CreateFallbackEffect2D(
            transform.position, 
            Color.red, 
            2f, 
            0.5f
        );
    }
    
    protected virtual void SpawnAreaAttackIndicator(Vector3 position)
    {
        // Create warning indicator
        var indicator = Effect2DManager.CreateFallbackEffect2D(
            position, 
            new Color(1f, 0f, 0f, 0.5f), 
            areaAttackRadius * 2f, 
            areaAttackDelay
        );
    }
    
    protected virtual void SpawnAreaAttackEffect(Vector3 position)
    {
        if (areaAttackPrefab != null)
        {
            var effect = Instantiate(areaAttackPrefab, position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        else
        {
            // Fallback effect
            Effect2DManager.CreateFallbackEffect2D(
                position, 
                Color.yellow, 
                areaAttackRadius * 2f, 
                1f
            );
        }
    }
    
    protected virtual void SpawnMultiTargetEffect(Vector3 position)
    {
        // Create multi-target effect
        Effect2DManager.CreateFallbackEffect2D(
            position, 
            Color.cyan, 
            multiTargetRange * 2f, 
            1f
        );
    }
    
    /// <summary>
    /// Override damage calculation for boss
    /// </summary>
    protected override float CalculateAttackDamage()
    {
        return base.CalculateAttackDamage() * bossDamageMultiplier;
    }
    
    /// <summary>
    /// Get boss attack statistics
    /// </summary>
    public string GetBossAttackStats()
    {
        string baseStats = GetAttackStats();
        
        return baseStats + $"\n" +
               $"Special Attack Ready: {(CanUseSpecialAttack ? "?" : "?")}\n" +
               $"Special Cooldown: {SpecialAttackCooldownRemaining:F1}s\n" +
               $"Is Performing Special: {(isPerformingSpecialAttack ? "?" : "?")}";
    }
    
    /// <summary>
    /// Force special attack (for testing)
    /// </summary>
    [ContextMenu("? Force Special Attack")]
    public void ForceSpecialAttack()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            lastSpecialAttackTime = 0f; // Reset cooldown
            PerformSpecialAttack(player.transform);
        }
    }
    
    /// <summary>
    /// Enhanced Gizmos
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // Draw multi-target range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, multiTargetRange);
        
        // Draw area attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, areaAttackRadius);
        
        // Draw special attack indicator
        if (isPerformingSpecialAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 0.5f);
        }
    }
}