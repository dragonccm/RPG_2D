using UnityEngine;
using System.Collections;

/// <summary>
/// Elite rank enumeration - phải đặt ở đầu file
/// </summary>
public enum EliteRank
{
    Elite,      // Tinh nhuệ cơ bản
    Champion,   // Tinh nhuệ cao cấp
    Legendary,  // Tinh nhuệ huyền thoại
    Mythic      // Tinh nhuệ thần thoại
}

/// <summary>
/// Enhanced Elite Enemy System với tích hợp 2D Effect Manager
/// Quản lý elite enemies với các abilities đặc biệt và hiệu ứng 2D
/// </summary>
public class EnemyElite : MonoBehaviour
{
    [Header("Elite Configuration")]
    [SerializeField] private EliteRank eliteRank = EliteRank.Elite;
    [SerializeField] private string eliteTitle = "Elite";

    [Header("Elite Stats Multipliers")]
    [SerializeField] private float healthMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float speedMultiplier = 1.2f;
    [SerializeField] private float sizeMultiplier = 1.3f;
    [SerializeField] private float experienceMultiplier = 2f;
    [SerializeField] private float currencyMultiplier = 2f;
    [SerializeField] private float itemDropChanceMultiplier = 1.5f;

    [Header("Random Abilities")]
    [SerializeField] private int minRandomAbilities = 2;
    [SerializeField] private int maxRandomAbilities = 4;

    [Header("Regeneration Settings")]
    [SerializeField] private float regenerationAmount = 5f;
    [SerializeField] private float regenerationInterval = 2f;
    [SerializeField] private bool hasRegeneration = false;
    [SerializeField] private bool hasArmor = false;
    [SerializeField] private float damageReduction = 0.2f;

    [Header("Berserker Settings")]
    [SerializeField] private bool hasBerserker = false;
    [SerializeField] private float berserkerHealthThreshold = 0.3f;
    [SerializeField] private float berserkerDamageBonus = 1.5f;
    [SerializeField] private float berserkerSpeedBonus = 1.3f;
    [SerializeField] private bool hasLifesteal = false;
    [SerializeField] private float lifestealPercent = 0.1f;

    [Header("Explosion Settings")]
    [SerializeField] private bool hasExplosionOnDeath = false;
    [SerializeField] private float explosionDamage = 50f;
    [SerializeField] private float explosionRadius = 3f;

    [Header("Thorns Settings")]
    [SerializeField] private bool hasThorns = false;
    [SerializeField] private float thornsDamagePercent = 0.3f;

    [Header("Frenzy Settings")]
    [SerializeField] private float frenzyAttackSpeedBonus = 2f;
    [SerializeField] private float frenzyDuration = 5f;
    [SerializeField] private float frenzyCooldown = 15f;

    [Header("Enrage Settings")]
    [SerializeField] private bool hasEnrage = false;
    [SerializeField] private float enrageHealthThreshold = 0.5f;
    [SerializeField] private float enrageDamageBonus = 1.8f;
    [SerializeField] private float enrageSpeedBonus = 1.5f;

    [Header("Immunity Settings")]
    [SerializeField] private float immunityDuration = 3f;
    [SerializeField] private float immunityCooldown = 20f;

    [Header("Teleport Settings")]
    [SerializeField] private float teleportDistance = 5f;
    [SerializeField] private float teleportCooldown = 8f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject eliteEffectPrefab;
    [SerializeField] private GameObject eliteIndicatorPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float outlineWidth = 0.1f;
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float pulseRate = 2f;
    [SerializeField] private float pulseAmount = 0.3f;

    // Abilities flags
    [SerializeField] private bool hasFrenzy = false;
    [SerializeField] private bool hasImmunity = false;
    [SerializeField] private bool hasTeleport = false;

    // Runtime state
    private bool isBerserking = false;
    private bool isEnraged = false;
    private bool isImmune = false;
    private bool isFrenzied = false;
    private float lastTeleportTime = 0f;
    private float lastImmunityTime = 0f;
    private float lastFrenzyTime = 0f;
    private float damageTaken = 0f;

    // Components
    private Enemy enemy;
    private SpriteRenderer spriteRenderer;
    private Character character;
    private Coroutine regenerationCoroutine;
    private Coroutine pulseCoroutine;
    private GameObject eliteEffect;
    private GameObject eliteIndicator;

    void Start()
    {
        InitializeComponents();
        SetupEliteAbilities();
        ApplyEliteStats();
        CreateEliteEffects();

        if (hasRegeneration)
        {
            regenerationCoroutine = StartCoroutine(RegenerateHealth());
        }
    }

    void Update()
    {
        UpdateEliteAbilities();
    }

    void OnDestroy()
    {
        CleanupEliteEffects();
    }

    private void InitializeComponents()
    {
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        character = GetComponent<Character>();

        if (enemy != null)
        {
            enemy.OnDamageTaken += HandleEnemyDamageTaken;
            enemy.OnHealthChanged += HandleHealthChanged;
            enemy.OnDeath += HandleDeath;
            enemy.OnDealDamage += HandleDealDamage;
        }

        if (character != null)
        {
            character.OnDamageTaken += HandleCharacterDamageTaken;
            character.OnDeath += HandleDeath;
        }

        RandomizeAbilities();
    }

    private void SetupEliteAbilities()
    {
        Debug.Log($"Elite {name} spawned with rank: {eliteRank}");

        ApplyEliteStats();

        CreateEliteEffects();

        if (hasRegeneration)
        {
            regenerationCoroutine = StartCoroutine(RegenerateHealth());
        }
    }

    private void UpdateEliteAbilities()
    {
        if (enemy == null || character == null) return;

        float currentTime = Time.time;
        float healthPercent = character.CurrentHealth / character.MaxHealth;

        // Check berserker mode
        if (hasBerserker && !isBerserking && healthPercent <= berserkerHealthThreshold)
        {
            ActivateBerserker();
        }

        // Check enrage mode
        if (hasEnrage && !isEnraged && healthPercent <= enrageHealthThreshold)
        {
            ActivateEnrage();
        }

        // Handle teleport
        if (hasTeleport && currentTime - lastTeleportTime >= teleportCooldown)
        {
            HandleTeleport();
        }

        // Handle immunity
        if (hasImmunity && currentTime - lastImmunityTime >= immunityCooldown)
        {
            HandleImmunity();
        }

        // Handle frenzy
        if (hasFrenzy && currentTime - lastFrenzyTime >= frenzyCooldown)
        {
            HandleFrenzy();
        }
    }

    private void CleanupEliteEffects()
    {
        if (enemy != null)
        {
            enemy.OnDamageTaken -= HandleEnemyDamageTaken;
            enemy.OnHealthChanged -= HandleHealthChanged;
            enemy.OnDeath -= HandleDeath;
            enemy.OnDealDamage -= HandleDealDamage;
        }

        if (character != null)
        {
            character.OnDamageTaken -= HandleCharacterDamageTaken;
            character.OnDeath -= HandleDeath;
        }

        DestroyEliteEffects();
    }

    private void HandleEnemyDamageTaken(Enemy enemy, float damage, float newHealth)
    {
        damageTaken += damage;

        // Flash effect
        StartCoroutine(DamageFlash());

        // Thorns damage
        if (hasThorns)
        {
            ApplyThorns(damage);
        }

        // Check for berserker/enrage triggers
        float healthPercent = newHealth / character.MaxHealth;

        if (hasBerserker && !isBerserking && healthPercent <= berserkerHealthThreshold)
        {
            ActivateBerserker();
        }

        if (hasEnrage && !isEnraged && healthPercent <= enrageHealthThreshold)
        {
            ActivateEnrage();
        }
    }

    private void HandleCharacterDamageTaken(float damage)
    {
        damageTaken += damage;

        // Flash effect
        StartCoroutine(DamageFlash());

        // Thorns damage
        if (hasThorns)
        {
            ApplyThorns(damage);
        }

        // Check for berserker/enrage triggers
        float healthPercent = character.CurrentHealth / character.MaxHealth;

        if (hasBerserker && !isBerserking && healthPercent <= berserkerHealthThreshold)
        {
            ActivateBerserker();
        }

        if (hasEnrage && !isEnraged && healthPercent <= enrageHealthThreshold)
        {
            ActivateEnrage();
        }
    }

    private void HandleHealthChanged(float oldHealth, float newHealth)
    {
        // Additional health-based logic can go here
    }

    private void HandleDeath()
    {
        Debug.Log($"Elite {name} has been defeated!");

        if (hasExplosionOnDeath)
        {
            Explode();
        }

        // Drop enhanced rewards
        DropEliteRewards();

        DestroyEliteEffects();
    }

    private void HandleDealDamage(GameObject target, float damage)
    {
        if (hasLifesteal && target != null)
        {
            Character targetCharacter = target.GetComponent<Character>();
            if (targetCharacter != null)
            {
                float healAmount = damage * lifestealPercent;
                character.Heal(healAmount);

                // Visual feedback for lifesteal
                Effect2DManager.CreateFallbackEffect2D(transform.position, Color.green, 0.8f, 1f);
            }
        }
    }

    private void RandomizeAbilities()
    {
        // Reset all abilities
        hasRegeneration = false;
        hasArmor = false;
        hasBerserker = false;
        hasLifesteal = false;
        hasExplosionOnDeath = false;
        hasThorns = false;
        hasFrenzy = false;
        hasEnrage = false;
        hasImmunity = false;
        hasTeleport = false;

        // Danh sách khả năng
        System.Action[] abilities = new System.Action[]
        {
            () => hasRegeneration = true,
            () => hasArmor = true,
            () => hasBerserker = true,
            () => hasLifesteal = true,
            () => hasExplosionOnDeath = true,
            () => hasThorns = true,
            () => hasFrenzy = true,
            () => hasEnrage = true,
            () => hasImmunity = true,
            () => hasTeleport = true
        };

        // Xáo trộn danh sách
        for (int i = 0; i < abilities.Length; i++)
        {
            int randomIndex = Random.Range(i, abilities.Length);
            System.Action temp = abilities[i];
            abilities[i] = abilities[randomIndex];
            abilities[randomIndex] = temp;
        }

        // Chọn số lượng khả năng ngẫu nhiên
        int abilityCount = Random.Range(minRandomAbilities, maxRandomAbilities + 1);
        abilityCount = Mathf.Min(abilityCount, abilities.Length);

        // Kích hoạt khả năng
        for (int i = 0; i < abilityCount; i++)
        {
            abilities[i]();
        }

        // Điều chỉnh thuộc tính dựa trên cấp bậc
        AdjustStatsByRank();
    }

    private void AdjustStatsByRank()
    {
        switch (eliteRank)
        {
            case EliteRank.Elite:
                // Giữ nguyên
                break;

            case EliteRank.Champion:
                healthMultiplier *= 1.5f;
                damageMultiplier *= 1.3f;
                speedMultiplier *= 1.2f;
                sizeMultiplier *= 1.2f;
                experienceMultiplier *= 1.5f;
                currencyMultiplier *= 1.5f;
                itemDropChanceMultiplier *= 1.3f;
                break;

            case EliteRank.Legendary:
                healthMultiplier *= 2f;
                damageMultiplier *= 1.6f;
                speedMultiplier *= 1.4f;
                sizeMultiplier *= 1.4f;
                experienceMultiplier *= 2f;
                currencyMultiplier *= 2f;
                itemDropChanceMultiplier *= 1.6f;
                break;

            case EliteRank.Mythic:
                healthMultiplier *= 3f;
                damageMultiplier *= 2f;
                speedMultiplier *= 1.6f;
                sizeMultiplier *= 1.6f;
                experienceMultiplier *= 3f;
                currencyMultiplier *= 3f;
                itemDropChanceMultiplier *= 2f;
                break;
        }
    }

    private void ApplyEliteStats()
    {
        if (character != null)
        {
            character.MaxHealth *= healthMultiplier;
            character.CurrentHealth = character.MaxHealth;
        }

        if (enemy != null)
        {
            enemy.baseDamage *= damageMultiplier;
        }

        // Apply size multiplier
        transform.localScale *= sizeMultiplier;

        // Apply other stat changes here as needed
    }

    private void CreateEliteEffects()
    {
        // Tạo hiệu ứng tinh nhuệ - SỬ DỤNG 2D EFFECT MANAGER
        if (eliteEffectPrefab != null)
        {
            eliteEffect = Effect2DManager.CreateFollowEffect2D(eliteEffectPrefab, transform, Vector3.zero, 1f, false);

            // Đặt màu dựa trên rank
            var spriteRenderer = eliteEffect?.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = GetEliteColor();
            }
        }
        else
        {
            // Fallback: Elite effect đơn giản
            eliteEffect = Effect2DManager.CreateFallbackEffect2D(transform.position, GetEliteColor(), 1.5f, float.MaxValue);
            eliteEffect.transform.SetParent(transform);
            eliteEffect.transform.localPosition = Vector3.zero;
        }

        // Tạo chỉ báo tinh nhuệ
        if (eliteIndicatorPrefab != null)
        {
            eliteIndicator = Effect2DManager.CreateFollowEffect2D(eliteIndicatorPrefab, transform, Vector3.up * 2f, 1f, false);
        }

        // Tạo outline
        CreateOutline();

        // Bắt đầu pulse effect
        if (spriteRenderer != null)
        {
            pulseCoroutine = StartCoroutine(PulseEffect());
        }
    }

    private void ActivateBerserker()
    {
        if (isBerserking) return;

        isBerserking = true;
        Debug.Log($"Elite {name} entered berserker mode!");

        // Increase damage and speed
        if (enemy != null)
        {
            enemy.baseDamage *= berserkerDamageBonus;
        }

        // Visual effects
        ShowBerserkerEffect();

        // Add berserker visual feedback
        Effect2DManager.CreateFallbackEffect2D(transform.position, Color.red, 2f, 1.5f);
    }

    private void ActivateEnrage()
    {
        if (isEnraged) return;

        isEnraged = true;
        Debug.Log($"Elite {name} became enraged!");

        // Increase damage and speed
        if (enemy != null)
        {
            enemy.baseDamage *= enrageDamageBonus;
        }

        // Visual effects
        ShowEnrageEffect();

        // Add enrage visual feedback
        Effect2DManager.CreateFallbackEffect2D(transform.position, new Color(1f, 0.5f, 0f), 2f, 1.5f);
    }

    private void HandleTeleport()
    {
        if (enemy == null || character == null) return;

        // Find a safe teleport position
        Vector3 newPosition = FindSafeTeleportPosition();

        // Teleport effect at current position
        ShowTeleportEffect(transform.position);

        // Move to new position
        transform.position = newPosition;

        // Teleport effect at new position
        ShowTeleportEffect(transform.position);

        lastTeleportTime = Time.time;
        Debug.Log($"Elite {name} teleported!");
    }

    private Vector3 FindSafeTeleportPosition()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere.normalized;
            Vector3 newPosition = transform.position + randomDirection * teleportDistance;

            // Simple validation - can be enhanced with raycast checks
            return newPosition;
        }

        return transform.position; // Fallback to current position
    }

    private void HandleImmunity()
    {
        if (isImmune) return;

        StartCoroutine(ActivateImmunity());
    }

    private IEnumerator ActivateImmunity()
    {
        isImmune = true;
        lastImmunityTime = Time.time;

        Debug.Log($"Elite {name} activated immunity!");

        // Visual effects
        ShowImmunityEffect();

        // Make immune to damage
        if (character != null)
        {
            // Temporarily disable damage taking
            // This would need integration with damage system
        }

        yield return new WaitForSeconds(immunityDuration);

        isImmune = false;
        HideImmunityEffect();

        Debug.Log($"Elite {name} immunity ended");
    }

    private void HandleFrenzy()
    {
        if (isFrenzied) return;

        StartCoroutine(ActivateFrenzy());
    }

    private IEnumerator ActivateFrenzy()
    {
        isFrenzied = true;
        lastFrenzyTime = Time.time;

        Debug.Log($"Elite {name} entered frenzy!");

        // Increase attack speed
        // This would need integration with attack system

        // Visual effects
        ShowFrenzyEffect();

        yield return new WaitForSeconds(frenzyDuration);

        isFrenzied = false;
        HideFrenzyEffect();

        Debug.Log($"Elite {name} frenzy ended");
    }

    private void ApplyThorns(float damageReceived)
    {
        GameObject attacker = FindAttacker();
        if (attacker != null)
        {
            var attackerCharacter = attacker.GetComponent<Character>();
            if (attackerCharacter != null)
            {
                float thornsDamage = damageReceived * thornsDamagePercent;
                attackerCharacter.TakeDamage(thornsDamage);

                Debug.Log($"Elite {name} thorns dealt {thornsDamage} damage to {attacker.name}");

                // Visual feedback
                Effect2DManager.CreateFallbackEffect2D(attacker.transform.position, Color.yellow, 0.8f, 1f);
            }
        }
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;

        yield return new WaitForSeconds(0.1f);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void DropEliteRewards()
    {
        // Enhanced reward dropping logic
        Debug.Log($"Elite {name} dropped enhanced rewards!");

        // This would integrate with loot system
        // Could drop more currency, items, experience etc.
    }

    /// <summary>
    /// Nổ khi chết - SỬ DỤNG 2D EFFECT MANAGER
    /// </summary>
    private void Explode()
    {
        // Hiển thị hiệu ứng nổ
        if (explosionEffectPrefab != null)
        {
            Effect2DManager.CreateEffect2D(explosionEffectPrefab, transform.position, Quaternion.identity, 1f, true);
        }
        else
        {
            // Fallback: Explosion effect đơn giản
            Effect2DManager.CreateFallbackEffect2D(transform.position, Color.yellow, 2f, 1.5f);
        }

        // Tìm tất cả mục tiêu trong phạm vi
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Player"));

        // Gây sát thương cho các mục tiêu
        foreach (Collider2D collider in colliders)
        {
            Character target = collider.GetComponent<Character>();
            if (target != null)
            {
                target.TakeDamage(explosionDamage);
            }
        }
    }

    /// <summary>
    /// Ẩn hiệu ứng frenzy
    /// </summary>
    private void HideFrenzyEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = GetEliteColor();
        }
    }

    private Color GetEliteColor()
    {
        switch (eliteRank)
        {
            case EliteRank.Elite:
                return Color.yellow;
            case EliteRank.Champion:
                return Color.orange;
            case EliteRank.Legendary:
                return Color.red;
            case EliteRank.Mythic:
                return Color.magenta;
            default:
                return Color.yellow;
        }
    }

    private void CreateOutline()
    {
        // Implementation for creating outline effect
    }

    private void ShowBerserkerEffect()
    {
        // Implementation for showing berserker effect
    }

    private void ShowEnrageEffect()
    {
        // Implementation for showing enrage effect
    }

    private void ShowTeleportEffect(Vector3 position)
    {
        // Implementation for showing teleport effect
    }

    private void ShowImmunityEffect()
    {
        // Implementation for showing immunity effect
    }

    private void HideImmunityEffect()
    {
        // Implementation for hiding immunity effect
    }

    private void ShowFrenzyEffect()
    {
        // Implementation for showing frenzy effect
    }

    private GameObject FindAttacker()
    {
        // Simple implementation to find attacker
        return null;
    }

    private void DestroyEliteEffects()
    {
        if (eliteEffect != null)
            Destroy(eliteEffect);
        if (eliteIndicator != null)
            Destroy(eliteIndicator);
    }

    private IEnumerator PulseEffect()
    {
        while (true)
        {
            yield return null;
        }
    }

    private IEnumerator RegenerateHealth()
    {
        while (true)
        {
            if (character != null && character.CurrentHealth < character.MaxHealth)
            {
                character.Heal(regenerationAmount);
            }
            yield return new WaitForSeconds(regenerationInterval);
        }
    }
}