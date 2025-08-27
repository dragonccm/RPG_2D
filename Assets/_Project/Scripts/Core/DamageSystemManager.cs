using UnityEngine;

/// <summary>
/// Enhanced Damage System Manager - Qu?n lý toàn b? h? th?ng damage
/// </summary>
public class DamageSystemManager : MonoBehaviour
{
    [Header("?? Damage System Configuration")]
    [SerializeField] public bool enableDetailedLogging = true;
    [SerializeField] private bool enableDamageValidation = true;
    [SerializeField] public bool showDamageInConsole = true;
    
    [Header("?? Damage Statistics")]
    [SerializeField] private float totalDamageDealt = 0f;
    [SerializeField] private float totalDamageReceived = 0f;
    [SerializeField] private int totalAttacks = 0;
    [SerializeField] private int successfulHits = 0;
    
    public static DamageSystemManager Instance { get; private set; }
    
    // Events
    public static System.Action<IDamageable, float, GameObject> OnDamageDealt;
    public static System.Action<IDamageable, GameObject> OnTargetKilled;
    public static System.Action<GameObject, float> OnDamageBlocked;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDamageSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeDamageSystem()
    {
        // Subscribe to damage events
        OnDamageDealt += LogDamageDealt;
        OnTargetKilled += LogTargetKilled;
        OnDamageBlocked += LogDamageBlocked;
        
        if (enableDetailedLogging)
            Debug.Log("?? DamageSystemManager initialized successfully!");
    }
    
    /// <summary>
    /// Enhanced damage dealing method v?i validation và logging
    /// </summary>
    public static bool DealDamage(GameObject attacker, IDamageable target, float damage, 
        DamageType damageType = DamageType.Physical, bool canCrit = true)
    {
        if (!ValidateDamageRequest(attacker, target, damage))
            return false;
        
        // Calculate final damage
        float finalDamage = CalculateFinalDamage(attacker, target, damage, damageType, canCrit);
        
        // Apply damage
        target.TakeDamage(finalDamage);
        
        // Update statistics
        if (Instance != null)
        {
            Instance.totalDamageDealt += finalDamage;
            Instance.totalAttacks++;
            Instance.successfulHits++;
        }
        
        // Trigger events
        OnDamageDealt?.Invoke(target, finalDamage, attacker);
        
        // Check if target died
        if (target.IsDead)
        {
            OnTargetKilled?.Invoke(target, attacker);
        }
        
        return true;
    }
    
    /// <summary>
    /// Validation damage request
    /// </summary>
    private static bool ValidateDamageRequest(GameObject attacker, IDamageable target, float damage)
    {
        if (attacker == null)
        {
            Debug.LogWarning("?? DamageSystem: Attacker is null!");
            return false;
        }
        
        if (target == null)
        {
            Debug.LogWarning("?? DamageSystem: Target is null!");
            return false;
        }
        
        if (damage <= 0)
        {
            Debug.LogWarning("?? DamageSystem: Damage must be positive!");
            return false;
        }
        
        if (target.IsDead)
        {
            Debug.LogWarning($"?? DamageSystem: Target {target} is already dead!");
            return false;
        }
        
        // Check if target is player and has God Mode
        var character = (target as MonoBehaviour)?.GetComponent<Character>();
        if (character != null && character.GodModeEnabled)
        {
            Debug.Log($"??? DamageSystem: {character.name} is in God Mode - damage blocked!");
            OnDamageBlocked?.Invoke(attacker, damage);
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Calculate final damage with modifiers
    /// </summary>
    private static float CalculateFinalDamage(GameObject attacker, IDamageable target, 
        float baseDamage, DamageType damageType, bool canCrit)
    {
        float finalDamage = baseDamage;
        
        // Get attacker stats
        var attackerCharacter = attacker.GetComponent<Character>();
        var attackerEnemy = attacker.GetComponent<Enemy>();
        
        // Get target stats
        var targetCharacter = (target as MonoBehaviour)?.GetComponent<Character>();
        
        // Apply attacker damage modifiers
        if (attackerCharacter != null)
        {
            finalDamage *= attackerCharacter.AttackPower / 10f; // Normalize attack power
            
            // Critical hit chance
            if (canCrit && Random.Range(0f, 1f) < attackerCharacter.CriticalChance)
            {
                finalDamage *= attackerCharacter.CriticalMultiplier;
                Debug.Log($"?? CRITICAL HIT! {finalDamage:F1} damage!");
            }
        }
        else if (attackerEnemy != null)
        {
            finalDamage = attackerEnemy.baseDamage * attackerEnemy.DamageMultiplier;
        }
        
        // Apply target defense
        if (targetCharacter != null)
        {
            float defense = targetCharacter.Defense;
            finalDamage = Mathf.Max(1f, finalDamage - defense);
        }
        
        // Damage type modifiers (future expansion)
        finalDamage = ApplyDamageTypeModifiers(finalDamage, damageType);
        
        return finalDamage;
    }
    
    /// <summary>
    /// Apply damage type modifiers
    /// </summary>
    private static float ApplyDamageTypeModifiers(float damage, DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return damage;
            case DamageType.Magic:
                return damage * 1.1f; // Magic deals 10% more damage
            case DamageType.Fire:
                return damage * 1.2f; // Fire deals 20% more damage
            case DamageType.Ice:
                return damage * 0.9f; // Ice deals 10% less damage but slows
            case DamageType.Poison:
                return damage * 0.8f; // Poison deals less initial but DoT
            default:
                return damage;
        }
    }
    
    /// <summary>
    /// Logging methods
    /// </summary>
    private void LogDamageDealt(IDamageable target, float damage, GameObject attacker)
    {
        if (!showDamageInConsole) return;
        
        string targetName = (target as MonoBehaviour)?.name ?? "Unknown";
        string attackerName = attacker?.name ?? "Unknown";
        
        Debug.Log($"?? {attackerName} dealt {damage:F1} damage to {targetName}");
    }
    
    private void LogTargetKilled(IDamageable target, GameObject attacker)
    {
        if (!showDamageInConsole) return;
        
        string targetName = (target as MonoBehaviour)?.name ?? "Unknown";
        string attackerName = attacker?.name ?? "Unknown";
        
        Debug.Log($"?? {attackerName} killed {targetName}!");
    }
    
    private void LogDamageBlocked(GameObject attacker, float damage)
    {
        if (!showDamageInConsole) return;
        
        Debug.Log($"??? {damage:F1} damage blocked!");
    }
    
    /// <summary>
    /// Get damage statistics
    /// </summary>
    public string GetDamageStats()
    {
        float hitRate = totalAttacks > 0 ? (successfulHits / (float)totalAttacks) * 100f : 0f;
        
        return $"?? Damage Stats:\n" +
               $"Total Damage Dealt: {totalDamageDealt:F1}\n" +
               $"Total Damage Received: {totalDamageReceived:F1}\n" +
               $"Total Attacks: {totalAttacks}\n" +
               $"Hit Rate: {hitRate:F1}%";
    }
    
    /// <summary>
    /// Reset statistics
    /// </summary>
    [ContextMenu("?? Reset Statistics")]
    public void ResetStatistics()
    {
        totalDamageDealt = 0f;
        totalDamageReceived = 0f;
        totalAttacks = 0;
        successfulHits = 0;
        
        Debug.Log("?? Damage statistics reset!");
    }
}

/// <summary>
/// Damage types for different effects
/// </summary>
public enum DamageType
{
    Physical,
    Magic,
    Fire,
    Ice,
    Poison,
    Lightning,
    Dark,
    Holy
}