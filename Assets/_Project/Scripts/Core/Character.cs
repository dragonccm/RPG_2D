using System.Collections;
using UnityEngine;

public class Character : MonoBehaviour, IDamageable
{
    [Header("Health & Mana")]
    public Resource health;
    public Resource mana;
    
    [Header("Player Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxMana = 50f;
    [SerializeField] private float healthRegenRate = 0f;
    [SerializeField] private float manaRegenRate = 5f;
    [SerializeField] private float defense = 0f;
    [SerializeField] private float attackPower = 10f;
    [SerializeField] private float criticalChance = 0.05f;
    [SerializeField] private float criticalMultiplier = 2f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackSpeed = 1f;
    
    [Header("God Mode")]
    [SerializeField] private bool godModeEnabled = false;
    [SerializeField] private float godModeHealth = 99999f;
    [SerializeField] private float godModeMana = 99999f;
    
    [Header("Combat Effects")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private bool enableHitStop = true;
    
    public bool isStunned { get; private set; }
    public bool isBeingKnockedBack { get; private set; }
    public bool isPoisoned { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine currentKnockbackCoroutine;
    private Coroutine currentFlashCoroutine;
    private Coroutine currentPoisonCoroutine;

    public System.Action<float> OnDamageTaken;
    public System.Action OnDeath;

    public float MaxHealth
    {
        get => health.maxValue;
        set => health.maxValue = value;
    }

    public float CurrentHealth
    {
        get => health.currentValue;
        set => health.currentValue = value;
    }

    public float MaxMana
    {
        get => mana.maxValue;
        set => mana.maxValue = value;
    }

    public float CurrentMana
    {
        get => mana.currentValue;
        set => mana.currentValue = value;
    }

    public bool IsDead => health.currentValue <= 0;

    // Player stats properties
    public float Defense { get => defense; set => defense = Mathf.Max(0f, value); }
    public float AttackPower { get => attackPower; set => attackPower = Mathf.Max(1f, value); }
    public float CriticalChance { get => criticalChance; set => criticalChance = Mathf.Clamp01(value); }
    public float CriticalMultiplier { get => criticalMultiplier; set => criticalMultiplier = Mathf.Max(1f, value); }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = Mathf.Max(0.1f, value); }
    public float AttackSpeed { get => attackSpeed; set => attackSpeed = Mathf.Max(0.1f, value); }
    public bool GodModeEnabled { get => godModeEnabled; set => godModeEnabled = value; }

    // Public properties for editor access
    public float KnockbackResistance 
    { 
        get => knockbackResistance; 
        set => knockbackResistance = Mathf.Max(0.1f, value); 
    }
    
    public float DamageFlashDuration 
    { 
        get => damageFlashDuration; 
        set => damageFlashDuration = Mathf.Max(0.1f, value); 
    }
    
    public Color DamageFlashColor 
    { 
        get => damageFlashColor; 
        set => damageFlashColor = value; 
    }
    
    public bool ShowDamageNumbers 
    { 
        get => showDamageNumbers; 
        set => showDamageNumbers = value; 
    }
    
    public bool EnableScreenShake 
    { 
        get => enableScreenShake; 
        set => enableScreenShake = value; 
    }
    
    public bool EnableHitStop 
    { 
        get => enableHitStop; 
        set => enableHitStop = value; 
    }

    protected virtual void Awake()
    {
        health = gameObject.AddComponent<Resource>();
        mana = gameObject.AddComponent<Resource>();
        
        if (godModeEnabled)
        {
            health.Initialize(godModeHealth, healthRegenRate);
            mana.Initialize(godModeMana, manaRegenRate);
            health.currentValue = godModeHealth;
            mana.currentValue = godModeMana;
        }
        else
        {
            health.Initialize(maxHealth, healthRegenRate);
            mana.Initialize(maxMana, manaRegenRate);
        }
        
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    protected virtual void Update()
    {
        if (godModeEnabled)
        {
            // Ensure health and mana stay at maximum in god mode
            if (health.currentValue < godModeHealth)
                health.currentValue = godModeHealth;
            if (mana.currentValue < godModeMana)
                mana.currentValue = godModeMana;
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isStunned || godModeEnabled) return;
        
        // Apply defense reduction
        float actualDamage = Mathf.Max(1f, damage - defense);

        // S4 (plan 7 — than_4 "BẤT DIỆT THÂN"): đòn chí mạng → sống sót 1 lần/60s, còn 30% HP
        if (health.currentValue - actualDamage <= 0f && SkillTreeManager.Instance != null
            && SkillTreeManager.Instance.IsUnlocked("than_4")
            && SkillTreeManager.Instance.TryTriggerDeathDefiance())
        {
            actualDamage = Mathf.Max(0f, health.currentValue - health.maxValue * 0.3f);
            Debug.Log("🌳 BẤT DIỆT THÂN kích hoạt — sống sót với 30% HP!");
            CombatEffectsManager.Instance?.ScreenShake(0.3f, 0.3f);
        }

        health.Decrease(actualDamage);
        TriggerDamageFlash();
        
        // Safe CombatEffectsManager usage
        var effectsManager = FindCombatEffectsManager();
        
        if (showDamageNumbers && effectsManager != null)
        {
            Vector3 damagePosition = transform.position + Vector3.up * 1.5f;
            effectsManager.ShowDamageNumber(damage, damagePosition, isCritical);
        }
        
        if (enableScreenShake && damage > 10f && effectsManager != null)
        {
            float shakeIntensity = Mathf.Clamp(damage * 0.01f, 0.05f, 0.3f);
            effectsManager.ScreenShake(shakeIntensity, 0.1f);
        }
        
        if (enableHitStop && isCritical && effectsManager != null)
        {
            effectsManager.HitStop(0.1f);
        }
        
        if (effectsManager != null)
        {
            effectsManager.CreateImpactEffect(
                transform.position, 
                isCritical ? Color.yellow : Color.red, 
                isCritical ? 1.5f : 1f
            );
        }
        
        OnDamageTaken?.Invoke(damage);
        
        if (health.currentValue <= 0)
        {
            Die();
        }
    }

    public void ApplyPoison(float damagePerSecond, float duration)
    {
        // Stop any existing poison effect
        if (currentPoisonCoroutine != null)
        {
            StopCoroutine(currentPoisonCoroutine);
        }

        currentPoisonCoroutine = StartCoroutine(PoisonCoroutine(damagePerSecond, duration));
    }

    private IEnumerator PoisonCoroutine(float damagePerSecond, float duration)
    {
        isPoisoned = true;
        float elapsedTime = 0f;
        float tickInterval = 0.5f; // Damage every half second
        float tickDamage = damagePerSecond * tickInterval;

        var effectsManager = FindCombatEffectsManager();

        while (elapsedTime < duration)
        {
            if (health.currentValue <= 0) break;

            // Apply poison damage
            TakeDamage(tickDamage);

            // Visual feedback
            if (effectsManager != null)
            {
                effectsManager.CreateImpactEffect(
                    transform.position,
                    new Color(0.5f, 0f, 0.5f), // Purple for poison
                    0.5f
                );
            }

            elapsedTime += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }

        isPoisoned = false;
        currentPoisonCoroutine = null;
    }

    public void TakeDamageWithKnockback(float damage, float knockbackForce, Vector2 knockbackDirection, bool isCritical = false)
    {
        TakeDamage(damage, isCritical);
        
        if (health.currentValue > 0)
        {
            ApplyKnockback(knockbackForce, knockbackDirection);
        }
    }

    public void ApplyKnockback(float force, Vector2 direction)
    {
        if (rb == null || isStunned) return;
        
        float actualForce = force / knockbackResistance;
        
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        
        currentKnockbackCoroutine = StartCoroutine(KnockbackCoroutine(actualForce, direction.normalized));
    }

    private IEnumerator KnockbackCoroutine(float force, Vector2 direction)
    {
        isBeingKnockedBack = true;
        
        Vector2 knockbackVelocity = direction * force;
        rb.linearVelocity = knockbackVelocity;
        
        float knockbackDuration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < knockbackDuration)
        {
            elapsedTime += Time.fixedDeltaTime;
            float progress = elapsedTime / knockbackDuration;
            
            float currentForce = force * Mathf.Exp(-progress * 5f);
            rb.linearVelocity = direction * currentForce;
            
            yield return new WaitForFixedUpdate();
        }
        
        rb.linearVelocity = Vector2.zero;
        isBeingKnockedBack = false;
        currentKnockbackCoroutine = null;
    }

    private void TriggerDamageFlash()
    {
        if (spriteRenderer == null) return;
        
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        currentFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
        currentFlashCoroutine = null;
    }

    public void ApplyStun(float duration)
    {
        if (!isStunned)
        {
            StartCoroutine(StunCoroutine(duration));
        }
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    public void SetKnockbackResistance(float resistance)
    {
        knockbackResistance = Mathf.Max(0.1f, resistance);
    }

    public void SetDamageFlashSettings(float duration, Color flashColor)
    {
        damageFlashDuration = duration;
        damageFlashColor = flashColor;
    }

    public void SetCombatEffectsEnabled(bool damageNumbers, bool screenShake, bool hitStop)
    {
        showDamageNumbers = damageNumbers;
        enableScreenShake = screenShake;
        enableHitStop = hitStop;
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke();
        
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        if (currentPoisonCoroutine != null)
        {
            StopCoroutine(currentPoisonCoroutine);
        }
        
        var effectsManager = FindCombatEffectsManager();
        if (effectsManager != null)
        {
            effectsManager.CreateImpactEffect(
                transform.position, 
                Color.black, 
                2f
            );
        }
        
        Destroy(gameObject);
    }

    public bool CanMove()
    {
        return !isStunned && !isBeingKnockedBack;
    }

    public void Heal(float amount)
    {
        health.Increase(amount);
        
        var effectsManager = FindCombatEffectsManager();
        
        if (showDamageNumbers && effectsManager != null)
        {
            Vector3 healPosition = transform.position + Vector3.up * 1.5f;
            effectsManager.ShowDamageNumber(amount, healPosition, false);
        }
        
        if (effectsManager != null)
        {
            effectsManager.CreateImpactEffect(
                transform.position, 
                Color.green, 
                1f
            );
        }
    }

    public void RestoreMana(float amount)
    {
        if (mana != null)
        {
            mana.Increase(amount);
        }
    }

    public void ToggleGodMode(bool enable)
    {
        godModeEnabled = enable;
        
        if (enable)
        {
            // Store original values
            maxHealth = health.maxValue;
            maxMana = mana.maxValue;
            
            // Set god mode values
            health.maxValue = godModeHealth;
            health.currentValue = godModeHealth;
            mana.maxValue = godModeMana;
            mana.currentValue = godModeMana;
            
            Debug.Log("God Mode: Enabled - 99999 Health & Mana");
        }
        else
        {
            // Restore original values
            health.maxValue = maxHealth;
            mana.maxValue = maxMana;
            
            Debug.Log("God Mode: Disabled - Stats restored");
        }
    }

    public void ResetToDefaultStats()
    {
        health.maxValue = maxHealth;
        health.currentValue = maxHealth;
        mana.maxValue = maxMana;
        mana.currentValue = maxMana;
    }

    /// <summary>
    /// Safe method to find CombatEffectsManager
    /// </summary>
    private CombatEffectsManager FindCombatEffectsManager()
    {
        try
        {
            return CombatEffectsManager.Instance;
        }
        catch
        {
            // If CombatEffectsManager doesn't exist, find it manually
            return FindFirstObjectByType<CombatEffectsManager>();
        }
    }

    private void OnDestroy()
    {
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        if (currentPoisonCoroutine != null)
        {
            StopCoroutine(currentPoisonCoroutine);
        }
    }
}