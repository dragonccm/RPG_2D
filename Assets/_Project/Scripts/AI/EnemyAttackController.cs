using UnityEngine;

/// <summary>
/// Enhanced Enemy Attack Controller với tích hợp DamageSystemManager
/// Điều khiển hành vi tấn công của kẻ địch với system damage mới
/// </summary>
public class EnemyAttackController : MonoBehaviour
{
    [Header("⚔️ Attack Configuration")]
    [Tooltip("Phạm vi tấn công của kẻ địch")]
    public float attackRange = 2f;
    
    [Tooltip("Thời gian hồi chiêu giữa các đòn tấn công")]
    public float attackCooldown = 1f;
    
    [Tooltip("Loại damage mặc định")]
    public DamageType defaultDamageType = DamageType.Physical;
    
    [Tooltip("Có thể critical hit không")]
    public bool canCriticalHit = false;
    
    [Header("🎯 Advanced Attack Settings")]
    [Tooltip("Damage multiplier cho attack này")]
    [SerializeField] private float damageMultiplier = 1f;
    
    [Tooltip("Knockback force khi tấn công")]
    [SerializeField] protected float knockbackForce = 5f;
    
    [Tooltip("Hiệu ứng tấn công")]
    [SerializeField] private GameObject attackEffect;
    
    [Tooltip("Âm thanh tấn công")]
    [SerializeField] private AudioClip attackSound;
    
    [Header("📊 Statistics")]
    [SerializeField] private int totalAttacks = 0;
    [SerializeField] private int successfulHits = 0;
    [SerializeField] private float totalDamageDealt = 0f;
    
    protected float lastAttackTime;
    private AudioSource audioSource;
    
    // Properties
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public bool IsOnCooldown => Time.time < lastAttackTime + attackCooldown;
    public float CooldownRemaining => Mathf.Max(0f, lastAttackTime + attackCooldown - Time.time);
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Kiểm tra mục tiêu có trong phạm vi tấn công không
    /// </summary>
    public virtual bool IsInAttackRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    /// <summary>
    /// ENHANCED: Thực hiện tấn công với DamageSystemManager
    /// </summary>
    public virtual bool Attack(Transform target)
    {
        // Validation checks
        if (!CanAttack(target))
            return false;
        
        // Update statistics
        totalAttacks++;
        
        // Perform attack animation and effects
        PerformAttackVisuals(target);
        
        // Deal damage through DamageSystemManager
        bool damageDealt = DealDamageToTarget(target);
        
        if (damageDealt)
        {
            successfulHits++;
            
            // Apply knockback if configured
            if (knockbackForce > 0f)
            {
                ApplyKnockback(target);
            }
        }
        
        // Update cooldown
        lastAttackTime = Time.time;
        
        return damageDealt;
    }
    
    /// <summary>
    /// Kiểm tra có thể tấn công không
    /// </summary>
    protected virtual bool CanAttack(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Attack target is null!");
            return false;
        }
        
        if (IsOnCooldown)
        {
            Debug.Log($"[{gameObject.name}] Attack on cooldown. Remaining: {CooldownRemaining:F2}s");
            return false;
        }
        
        if (!IsInAttackRange(target))
        {
            Debug.Log($"[{gameObject.name}] Target {target.name} is out of attack range!");
            return false;
        }
        
        // Check if target has IDamageable
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Target {target.name} is not damageable!");
            return false;
        }
        
        if (damageable.IsDead)
        {
            Debug.Log($"[{gameObject.name}] Target {target.name} is already dead!");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Thực hiện visual effects cho attack
    /// </summary>
    protected virtual void PerformAttackVisuals(Transform target)
    {
        // Face target
        FaceTarget(target);
        
        // Play animation
        PlayAttackAnimation();
        
        // Play sound
        PlayAttackSound();
        
        // Spawn attack effect
        SpawnAttackEffect(target);
    }
    
    /// <summary>
    /// Deal damage through enhanced damage system
    /// </summary>
    protected virtual bool DealDamageToTarget(Transform target)
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null) return false;
        
        // Calculate damage
        float damage = CalculateAttackDamage();
        
        // Use DamageSystemManager for enhanced damage dealing
        bool success = DamageSystemManager.DealDamage(
            gameObject, 
            damageable, 
            damage, 
            defaultDamageType, 
            canCriticalHit
        );
        
        if (success)
        {
            totalDamageDealt += damage;
            Debug.Log($"[{gameObject.name}] Successfully dealt damage to {target.name}");
            
            // Trigger enemy event manually instead of accessing the event directly
            var enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                // Call a public method on Enemy instead of accessing the event
                // This would need to be implemented in Enemy.cs
                NotifyEnemyOfDamageDealt(enemy, target.gameObject, damage);
            }
        }
        
        return success;
    }
    
    /// <summary>
    /// Notify enemy of damage dealt (helper method)
    /// </summary>
    private void NotifyEnemyOfDamageDealt(Enemy enemy, GameObject target, float damage)
    {
        // This is a workaround - ideally Enemy should have a public method for this
        Debug.Log($"[{gameObject.name}] Notified enemy of {damage:F1} damage dealt to {target.name}");
    }
    
    /// <summary>
    /// Calculate attack damage
    /// </summary>
    protected virtual float CalculateAttackDamage()
    {
        float baseDamage = 10f; // Default
        
        // Get damage from Enemy component
        var enemy = GetComponent<Enemy>();
        if (enemy != null)
        {
            baseDamage = enemy.baseDamage;
        }
        
        return baseDamage * damageMultiplier;
    }
    
    /// <summary>
    /// Apply knockback to target
    /// </summary>
    protected virtual void ApplyKnockback(Transform target)
    {
        var character = target.GetComponent<Character>();
        if (character != null)
        {
            Vector2 knockbackDirection = (target.position - transform.position).normalized;
            character.ApplyKnockback(knockbackForce, knockbackDirection);
            
            Debug.Log($"[{gameObject.name}] Applied knockback to {target.name}");
        }
    }
    
    /// <summary>
    /// Face target direction
    /// </summary>
    protected virtual void FaceTarget(Transform target)
    {
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            float dx = target.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
                spriteRenderer.flipX = dx < 0f;
        }
    }
    
    /// <summary>
    /// Play attack animation
    /// </summary>
    protected virtual void PlayAttackAnimation()
    {
        var enemy = GetComponentInParent<Enemy>();
        if (enemy != null && enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.PlayAttackAnimation();
        }
    }
    
    /// <summary>
    /// Play attack sound
    /// </summary>
    protected virtual void PlayAttackSound()
    {
        if (attackSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(attackSound);
        }
    }
    
    /// <summary>
    /// Spawn attack effect
    /// </summary>
    protected virtual void SpawnAttackEffect(Transform target)
    {
        if (attackEffect != null)
        {
            Vector3 effectPosition = Vector3.Lerp(transform.position, target.position, 0.5f);
            var effect = Instantiate(attackEffect, effectPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    /// <summary>
    /// Force reset cooldown (for testing)
    /// </summary>
    [ContextMenu("🔄 Reset Cooldown")]
    public void ResetCooldown()
    {
        lastAttackTime = 0f;
        Debug.Log($"[{gameObject.name}] Attack cooldown reset!");
    }
    
    /// <summary>
    /// Get attack statistics
    /// </summary>
    public string GetAttackStats()
    {
        float hitRate = totalAttacks > 0 ? (successfulHits / (float)totalAttacks) * 100f : 0f;
        float avgDamage = successfulHits > 0 ? totalDamageDealt / successfulHits : 0f;
        
        return $"📊 Attack Stats for {gameObject.name}:\n" +
               $"Total Attacks: {totalAttacks}\n" +
               $"Successful Hits: {successfulHits}\n" +
               $"Hit Rate: {hitRate:F1}%\n" +
               $"Total Damage: {totalDamageDealt:F1}\n" +
               $"Avg Damage: {avgDamage:F1}";
    }
    
    /// <summary>
    /// Reset attack statistics
    /// </summary>
    [ContextMenu("📊 Reset Stats")]
    public void ResetAttackStats()
    {
        totalAttacks = 0;
        successfulHits = 0;
        totalDamageDealt = 0f;
        Debug.Log($"[{gameObject.name}] Attack statistics reset!");
    }
    
    /// <summary>
    /// Gizmos for visualization
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw cooldown indicator
        if (IsOnCooldown)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
    }
}
