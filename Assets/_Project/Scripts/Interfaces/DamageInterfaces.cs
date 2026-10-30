using UnityEngine;

/// <summary>
/// Interface for objects that can take damage
/// Implement this interface on any object that should be able to receive damage
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Current health of the object
    /// </summary>
    float CurrentHealth { get; }
    
    /// <summary>
    /// Maximum health of the object
    /// </summary>
    float MaxHealth { get; }
    
    /// <summary>
    /// Whether the object is currently dead
    /// </summary>
    bool IsDead { get; }
    
    /// <summary>
    /// Apply damage to this object
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    void TakeDamage(float damage);
    
    /// <summary>
    /// Heal this object
    /// </summary>
    /// <param name="amount">Amount of health to restore</param>
    void Heal(float amount);
}

/// <summary>
/// Interface for objects that can deal damage
/// Implement this interface on weapons, projectiles, etc.
/// </summary>
public interface IDamageDealer
{
    /// <summary>
    /// Base damage amount
    /// </summary>
    float BaseDamage { get; }
    
    /// <summary>
    /// Damage multiplier
    /// </summary>
    float DamageMultiplier { get; }
    
    /// <summary>
    /// Calculate final damage amount
    /// </summary>
    /// <returns>Final damage to apply</returns>
    float CalculateDamage();
}

/// <summary>
/// Interface for objects that can be stunned or have status effects
/// </summary>
public interface IStatusEffectable
{
    /// <summary>
    /// Whether the object is currently stunned
    /// </summary>
    bool IsStunned { get; }
    
    /// <summary>
    /// Apply stun effect for a duration
    /// </summary>
    /// <param name="duration">Duration in seconds</param>
    void ApplyStun(float duration);
    
    /// <summary>
    /// Remove stun effect immediately
    /// </summary>
    void RemoveStun();
}

/// <summary>
/// Interface for objects that can be targeted by AI
/// </summary>
public interface ITargetable
{
    /// <summary>
    /// Transform of the targetable object
    /// </summary>
    Transform Transform { get; }
    
    /// <summary>
    /// Whether this object can currently be targeted
    /// </summary>
    bool CanBeTargeted { get; }
    
    /// <summary>
    /// Priority level for targeting (higher = more likely to be targeted)
    /// </summary>
    int TargetPriority { get; }
}

/// <summary>
/// Events related to damage and health changes
/// </summary>
public static class DamageEvents
{
    /// <summary>
    /// Event triggered when any object takes damage
    /// Parameters: (target, damage, attacker)
    /// </summary>
    public static System.Action<IDamageable, float, IDamageDealer> OnDamageTaken;
    
    /// <summary>
    /// Event triggered when any object is healed
    /// Parameters: (target, healAmount, healer)
    /// </summary>
    public static System.Action<IDamageable, float, GameObject> OnHealed;
    
    /// <summary>
    /// Event triggered when any object dies
    /// Parameters: (target, killer)
    /// </summary>
    public static System.Action<IDamageable, IDamageDealer> OnObjectDied;
}