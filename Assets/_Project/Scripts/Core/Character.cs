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

    [SerializeField] private bool enableHitStop = true;
    
    [Header("??? Enhanced Damage Protection")]
    [SerializeField] private float damageReduction = 0f;
    [SerializeField] private bool hasShield = false;
    [SerializeField] private float shieldHealth = 0f;
    [SerializeField] public float maxShieldHealth = 50f;
    [SerializeField] private float shieldRegenDelay = 3f;
    [SerializeField] private float shieldRegenRate = 10f;
    
    [Header("?? Boss Damage Specific")]
    [SerializeField] public float bossDamageReduction = 0.1f; // 10% reduction from boss attacks
    [SerializeField] public bool enableBossProtection = true;
    [SerializeField] public float maxDamagePerHit = 50f; // Cap damage per single hit
    [SerializeField] public bool enableDamageCap = true;
    
    public bool isStunned { get; private set; }
    public bool isBeingKnockedBack { get; private set; }
    public bool isPoisoned { get; private set; }
    public bool hasActiveShield => shieldHealth > 0f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine currentKnockbackCoroutine;
    private Coroutine currentFlashCoroutine;
    private Coroutine currentPoisonCoroutine;
    private Coroutine shieldRegenCoroutine;
    private float lastDamageTime;

    public System.Action<float> OnDamageTaken;
    public System.Action OnDeath;
    public System.Action<float> OnShieldDamaged;
    public System.Action OnShieldBroken;
    public System.Action OnShieldRestored;

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

    /// <summary>
    /// Transform of this damageable object
    /// </summary>
    public UnityEngine.Transform Transform => transform;

    /// <summary>
    /// Player level
    /// </summary>
    public int Level
    {
        get
        {
            var skillManager = ServiceLocator.GetService<ModularSkillManager>();
            return skillManager != null ? skillManager.GetPlayerLevel() : 1;
        }
        set
        {
            var skillManager = ServiceLocator.GetService<ModularSkillManager>();
            if (skillManager != null)
            {
                skillManager.SetPlayerLevel(value);
            }
        }
    }

    /// <summary>
    /// Player experience
    /// </summary>
    public float Experience
    {
        get
        {
            // For now, return 0 as experience is managed by skill system
            // This can be expanded to integrate with the experience system
            return 0f;
        }
        set
        {
            // For now, ignore experience setting as it's managed by skill system
            // This can be expanded to integrate with the experience system
        }
    }

    // Enhanced properties
    public float ShieldHealth => shieldHealth;
    public float MaxShieldHealth => maxShieldHealth;
    public float ShieldPercentage => maxShieldHealth > 0 ? shieldHealth / maxShieldHealth : 0f;
    public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
    public float TotalEffectiveHealth => CurrentHealth + shieldHealth;

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
        
        // Initialize shield
        if (hasShield)
        {
            shieldHealth = maxShieldHealth;
        }
        
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Register with DamageSystemManager if available
        if (DamageSystemManager.Instance != null)
        {
            DamageSystemManager.OnDamageDealt += OnDamageDealtToSelf;
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
        
        // Handle shield regeneration
        HandleShieldRegeneration();
    }

    /// <summary>
    /// ENHANCED: Take damage with improved boss protection
    /// </summary>
    public void TakeDamage(float damage)
    {
        TakeDamageEnhanced(damage, null, DamageType.Physical, false);
    }
    
    /// <summary>
    /// Enhanced damage method with source tracking
    /// </summary>
    public void TakeDamageEnhanced(float damage, GameObject source = null, DamageType damageType = DamageType.Physical, bool isCritical = false)
    {
        if (isStunned || godModeEnabled) 
        {
            if (godModeEnabled)
                Debug.Log($"??? {gameObject.name} in God Mode - damage blocked!");
            return;
        }
        
        // Calculate damage reductions
        float finalDamage = CalculateFinalDamage(damage, source, damageType);
        
        // Apply damage cap if enabled
        if (enableDamageCap && finalDamage > maxDamagePerHit)
        {
            Debug.Log($"??? Damage capped from {finalDamage:F1} to {maxDamagePerHit:F1}");
            finalDamage = maxDamagePerHit;
        }
        
        // Apply damage to shield first
        if (hasActiveShield)
        {
            float damageToShield = Mathf.Min(finalDamage, shieldHealth);
            float damageToHealth = finalDamage - damageToShield;
            
            // Damage shield
            shieldHealth = Mathf.Max(0f, shieldHealth - damageToShield);
            OnShieldDamaged?.Invoke(damageToShield);
            
            if (shieldHealth <= 0f)
            {
                Debug.Log($"??? {gameObject.name}'s shield broken!");
                OnShieldBroken?.Invoke();
                StopShieldRegeneration();
                StartShieldRegeneration();
            }
            
            // Apply remaining damage to health
            if (damageToHealth > 0f)
            {
                health.Decrease(damageToHealth);
                Debug.Log($"?? {gameObject.name} took {damageToHealth:F1} health damage (shield absorbed {damageToShield:F1})");
            }
            else
            {
                Debug.Log($"??? {gameObject.name}'s shield absorbed all {damageToShield:F1} damage");
            }
        }
        else
        {
            // Direct health damage
            health.Decrease(finalDamage);
            Debug.Log($"?? {gameObject.name} took {finalDamage:F1} damage");
        }
        
        // Trigger effects
        TriggerDamageEffects(finalDamage, source, isCritical);
        
        // Update last damage time
        lastDamageTime = Time.time;
        
        // Trigger events
        OnDamageTaken?.Invoke(finalDamage);
        
        if (health.currentValue <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Calculate final damage with all reductions
    /// </summary>
    private float CalculateFinalDamage(float baseDamage, GameObject source, DamageType damageType)
    {
        float finalDamage = baseDamage;
        
        // Apply base defense
        finalDamage = Mathf.Max(1f, finalDamage - defense);
        
        // Apply damage reduction
        finalDamage *= (1f - damageReduction);
        
        // Check if damage is from boss
        bool isBossAttack = IsBossAttack(source);
        
        if (isBossAttack && enableBossProtection)
        {
            // Apply boss damage reduction
            finalDamage *= (1f - bossDamageReduction);
            Debug.Log($"??? Boss damage reduced by {bossDamageReduction * 100f}%");
        }
        
        // Apply damage type modifiers
        finalDamage = ApplyDamageTypeModifiers(finalDamage, damageType);
        
        return finalDamage;
    }
    
    /// <summary>
    /// Check if attack comes from boss
    /// </summary>
    private bool IsBossAttack(GameObject source)
    {
        if (source == null) return false;
        
        // Check for boss components
        return source.GetComponent<EnemyBoss>() != null ||
               source.GetComponent<BossAttackController>() != null ||
               source.name.ToLower().Contains("boss") ||
               source.CompareTag("Boss");
    }
    
    /// <summary>
    /// Apply damage type modifiers
    /// </summary>
    private float ApplyDamageTypeModifiers(float damage, DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Fire:
                // Fire damage has chance to apply burn
                if (Random.Range(0f, 1f) < 0.2f)
                {
                    ApplyPoison(5f, 3f); // Burn effect
                }
                break;
            case DamageType.Ice:
                // Ice damage has chance to slow
                if (Random.Range(0f, 1f) < 0.3f)
                {
                    moveSpeed *= 0.7f; // Temporary slow
                    StartCoroutine(RemoveSlowAfterDelay(2f));
                }
                break;
            case DamageType.Lightning:
                // Lightning has chance to stun
                if (Random.Range(0f, 1f) < 0.15f)
                {
                    ApplyStun(0.5f);
                }
                break;
        }
        
        return damage;
    }
    
    /// <summary>
    /// Trigger damage visual and audio effects
    /// </summary>
    private void TriggerDamageEffects(float damage, GameObject source, bool isCritical)
    {
        TriggerDamageFlash();
        
        // Safe CombatEffectsManager usage
        var effectsManager = FindCombatEffectsManager();
        
        if (showDamageNumbers && effectsManager != null)
        {
            Vector3 damagePosition = transform.position + Vector3.up * 1.5f;
            effectsManager.ShowDamageNumber(damage, damagePosition, isCritical);
        }
        

        
        if (enableHitStop && isCritical && effectsManager != null)
        {
            effectsManager.HitStop(0.1f);
        }
        
        if (effectsManager != null)
        {
            Color effectColor = hasActiveShield ? Color.cyan : (isCritical ? Color.yellow : Color.red);
            effectsManager.CreateImpactEffect(
                transform.position, 
                effectColor, 
                isCritical ? 1.5f : 1f
            );
        }
    }
    
    /// <summary>
    /// Handle shield regeneration
    /// </summary>
    private void HandleShieldRegeneration()
    {
        if (!hasShield || hasActiveShield) return;
        
        // Start regen after delay
        if (Time.time >= lastDamageTime + shieldRegenDelay && shieldRegenCoroutine == null)
        {
            StartShieldRegeneration();
        }
    }
    
    /// <summary>
    /// Start shield regeneration
    /// </summary>
    private void StartShieldRegeneration()
    {
        if (shieldRegenCoroutine != null)
            StopCoroutine(shieldRegenCoroutine);
        
        shieldRegenCoroutine = StartCoroutine(ShieldRegenerationCoroutine());
    }
    
    /// <summary>
    /// Stop shield regeneration
    /// </summary>
    private void StopShieldRegeneration()
    {
        if (shieldRegenCoroutine != null)
        {
            StopCoroutine(shieldRegenCoroutine);
            shieldRegenCoroutine = null;
        }
    }
    
    /// <summary>
    /// Shield regeneration coroutine
    /// </summary>
    private IEnumerator ShieldRegenerationCoroutine()
    {
        Debug.Log($"??? {gameObject.name} shield regeneration started");
        
        while (shieldHealth < maxShieldHealth)
        {
            shieldHealth = Mathf.Min(maxShieldHealth, shieldHealth + shieldRegenRate * Time.deltaTime);
            
            if (shieldHealth >= maxShieldHealth)
            {
                OnShieldRestored?.Invoke();
                Debug.Log($"??? {gameObject.name} shield fully restored!");
            }
            
            yield return null;
        }
        
        shieldRegenCoroutine = null;
    }
    
    /// <summary>
    /// Remove slow effect after delay
    /// </summary>
    private IEnumerator RemoveSlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        moveSpeed /= 0.7f; // Restore speed
    }
    
    /// <summary>
    /// Damage system manager event handler
    /// </summary>
    private void OnDamageDealtToSelf(IDamageable target, float damage, GameObject attacker)
    {
        if (target == this)
        {
            // Additional processing when this character takes damage
            Debug.Log($"?? DamageSystem: {gameObject.name} received {damage:F1} damage from {attacker?.name ?? "Unknown"}");
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
            TakeDamageEnhanced(tickDamage, null, DamageType.Poison);

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
        TakeDamageEnhanced(damage, null, DamageType.Physical, isCritical);
        
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

    public void SetCombatEffectsEnabled(bool damageNumbers, bool hitStop)
    {
        showDamageNumbers = damageNumbers;
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
    
    /// <summary>
    /// Restore shield to full
    /// </summary>
    public void RestoreShield()
    {
        if (hasShield)
        {
            shieldHealth = maxShieldHealth;
            OnShieldRestored?.Invoke();
            Debug.Log($"??? {gameObject.name} shield restored to full!");
        }
    }
    
    /// <summary>
    /// Enable/disable shield
    /// </summary>
    public void SetShieldEnabled(bool enabled)
    {
        hasShield = enabled;
        if (enabled && shieldHealth <= 0f)
        {
            shieldHealth = maxShieldHealth;
            OnShieldRestored?.Invoke();
        }
        else if (!enabled)
        {
            shieldHealth = 0f;
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
        
        if (hasShield)
        {
            shieldHealth = maxShieldHealth;
        }
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
    
    /// <summary>
    /// Get character status for debugging
    /// </summary>
    public string GetCharacterStatus()
    {
        return $"?? {gameObject.name} Status:\n" +
               $"Health: {CurrentHealth:F1}/{MaxHealth:F1} ({HealthPercentage:P1})\n" +
               $"Shield: {ShieldHealth:F1}/{MaxShieldHealth:F1} ({ShieldPercentage:P1})\n" +
               $"Mana: {CurrentMana:F1}/{MaxMana:F1}\n" +
               $"Defense: {Defense:F1}\n" +
               $"God Mode: {(GodModeEnabled ? "?" : "?")}\n" +
               $"Stunned: {(isStunned ? "??" : "?")}\n" +
               $"Poisoned: {(isPoisoned ? "??" : "?")}\n" +
               $"Shield Active: {(hasActiveShield ? "???" : "?")}";
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
        if (shieldRegenCoroutine != null)
        {
            StopCoroutine(shieldRegenCoroutine);
        }
        
        // Unregister from DamageSystemManager
        if (DamageSystemManager.Instance != null)
        {
            DamageSystemManager.OnDamageDealt -= OnDamageDealtToSelf;
        }
    }
}