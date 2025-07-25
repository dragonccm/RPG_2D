using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Qu?n l� k? th� tinh nhu? v?i kh? n?ng ??c bi?t v� thu?c t�nh m?nh h?n
/// </summary>
public class EnemyElite : MonoBehaviour
{
    [Header("Elite Settings")]
    [SerializeField] private bool isElite = true;
    [SerializeField] private string eliteTitle = "";
    [SerializeField] private EliteRank eliteRank = EliteRank.Elite;
    [SerializeField] private bool randomizeAbilities = false;
    [SerializeField] private int minRandomAbilities = 1;
    [SerializeField] private int maxRandomAbilities = 3;
    
    [Header("Stat Bonuses")]
    [SerializeField] private float healthMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float speedMultiplier = 1.2f;
    [SerializeField] private float sizeMultiplier = 1.3f;
    [SerializeField] private float experienceMultiplier = 2f;
    [SerializeField] private float currencyMultiplier = 2f;
    [SerializeField] private float itemDropChanceMultiplier = 1.5f;
    
    [Header("Elite Abilities")]
    [SerializeField] private bool hasRegeneration = false;
    [SerializeField] private float regenerationAmount = 5f;
    [SerializeField] private float regenerationInterval = 2f;
    
    [SerializeField] private bool hasArmor = false;
    [SerializeField] private float damageReduction = 0.3f;
    
    [SerializeField] private bool hasBerserker = false;
    [SerializeField] private float berserkerHealthThreshold = 0.3f;
    [SerializeField] private float berserkerDamageBonus = 0.5f;
    [SerializeField] private float berserkerSpeedBonus = 0.3f;
    
    [SerializeField] private bool hasLifesteal = false;
    [SerializeField] private float lifestealPercent = 0.2f;
    
    [SerializeField] private bool hasExplosionOnDeath = false;
    [SerializeField] private float explosionDamage = 20f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private GameObject explosionEffectPrefab;
    
    [SerializeField] private bool hasThorns = false;
    [SerializeField] private float thornsDamagePercent = 0.2f;
    
    [SerializeField] private bool hasFrenzy = false;
    [SerializeField] private float frenzyAttackSpeedBonus = 0.3f;
    [SerializeField] private float frenzyDuration = 5f;
    [SerializeField] private float frenzyCooldown = 15f;
    
    [SerializeField] private bool hasEnrage = false;
    [SerializeField] private float enrageHealthThreshold = 0.5f;
    [SerializeField] private float enrageDamageBonus = 0.3f;
    [SerializeField] private float enrageSpeedBonus = 0.2f;
    
    [SerializeField] private bool hasImmunity = false;
    [SerializeField] private float immunityDuration = 2f;
    [SerializeField] private float immunityCooldown = 20f;
    
    [SerializeField] private bool hasTeleport = false;
    [SerializeField] private float teleportDistance = 5f;
    [SerializeField] private float teleportCooldown = 10f;
    [SerializeField] private GameObject teleportEffectPrefab;
    
    [Header("Elite Visuals")]
    [SerializeField] private Color eliteColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private GameObject eliteEffectPrefab;
    [SerializeField] private GameObject eliteIndicatorPrefab;
    [SerializeField] private bool useEliteOutline = true;
    [SerializeField] private float outlineWidth = 1.5f;
    [SerializeField] private bool pulseEffect = true;
    [SerializeField] private float pulseRate = 1f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    // C�c component
    private Enemy enemy;
    // private Animator animator; // Removed direct Animator reference
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    
    // Tr?ng th�i tinh nhu?
    private bool isBerserking = false;
    private bool isEnraged = false;
    private bool isImmune = false;
    private bool isFrenzied = false;
    private float lastTeleportTime = -999f;
    private float lastImmunityTime = -999f;
    private float lastFrenzyTime = -999f;
    private float damageTaken = 0f;
    
    // ??i t??ng hi?u ?ng
    private GameObject eliteEffect;
    private GameObject eliteIndicator;
    
    // Coroutine
    private Coroutine regenerationCoroutine;
    private Coroutine pulseCoroutine;
    
    // Enum c?p b?c tinh nhu?
    public enum EliteRank
    {
        Elite,      // Tinh nhu? th??ng
        Champion,   // Qu�n qu�n
        Legendary,  // Huy?n tho?i
        Mythic      // Th?n tho?i
    }
    
    private void Awake()
    {
        // L?y c�c component c?n thi?t
        enemy = GetComponent<Enemy>();
        // animator = GetComponent<Animator>(); // Removed direct Animator reference
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // L?u material g?c
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // ??ng k� s? ki?n
        if (enemy != null)
        {
            enemy.OnDamageTaken += HandleDamageTaken;
            enemy.OnHealthChanged += HandleHealthChanged;
            enemy.OnDeath += HandleDeath;
            enemy.OnDealDamage += HandleDealDamage;
        }
        
        // N?u ng?u nhi�n h�a kh? n?ng, ch?n ng?u nhi�n
        if (randomizeAbilities)
        {
            RandomizeAbilities();
        }
    }
    
    private void Start()
    {
        // N?u kh�ng ph?i tinh nhu?, kh�ng l�m g� c?
        if (!isElite)
        {
            return;
        }
        
        // �p d?ng thu?c t�nh tinh nhu?
        ApplyEliteStats();
        
        // T?o hi?u ?ng tinh nhu?
        CreateEliteEffects();
        
        // B?t ??u t�i t?o m�u n?u c�
        if (hasRegeneration)
        {
            regenerationCoroutine = StartCoroutine(RegenerateHealth());
        }
        
        // T?o outline n?u c?n
        if (useEliteOutline)
        {
            CreateOutline();
        }
        
        // B?t ??u hi?u ?ng pulse n?u c?n
        if (pulseEffect)
        {
            pulseCoroutine = StartCoroutine(PulseEffect());
        }
    }
    
    private void Update()
    {
        // N?u kh�ng ph?i tinh nhu?, kh�ng l�m g� c?
        if (!isElite)
        {
            return;
        }
        
        // X? l� d?ch chuy?n n?u c�
        if (hasTeleport)
        {
            HandleTeleport();
        }
        
        // X? l� mi?n nhi?m n?u c�
        if (hasImmunity)
        {
            HandleImmunity();
        }
        
        // X? l� cu?ng n? n?u c�
        if (hasFrenzy)
        {
            HandleFrenzy();
        }
    }
    
    private void OnDestroy()
    {
        // H?y ??ng k� s? ki?n
        if (enemy != null)
        {
            enemy.OnDamageTaken -= HandleDamageTaken;
            enemy.OnHealthChanged -= HandleHealthChanged;
            enemy.OnDeath -= HandleDeath;
            enemy.OnDealDamage -= HandleDealDamage;
        }
        
        // D?ng coroutine
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }
        
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        // H?y hi?u ?ng
        DestroyEliteEffects();
    }
    
    /// <summary>
    /// Ng?u nhi�n h�a kh? n?ng
    /// </summary>
    private void RandomizeAbilities()
    {
        // ??t t?t c? kh? n?ng v? false
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
        
        // Danh s�ch kh? n?ng
        List<System.Action> abilities = new List<System.Action>
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
        
        // X�o tr?n danh s�ch
        for (int i = 0; i < abilities.Count; i++)
        {
            int randomIndex = Random.Range(i, abilities.Count);
            System.Action temp = abilities[i];
            abilities[i] = abilities[randomIndex];
            abilities[randomIndex] = temp;
        }
        
        // Ch?n s? l??ng kh? n?ng ng?u nhi�n
        int abilityCount = Random.Range(minRandomAbilities, maxRandomAbilities + 1);
        abilityCount = Mathf.Min(abilityCount, abilities.Count);
        
        // K�ch ho?t kh? n?ng
        for (int i = 0; i < abilityCount; i++)
        {
            abilities[i]();
        }
        
        // ?i?u ch?nh thu?c t�nh d?a tr�n c?p b?c
        AdjustStatsByRank();
    }
    
    /// <summary>
    /// ?i?u ch?nh thu?c t�nh d?a tr�n c?p b?c
    /// </summary>
    private void AdjustStatsByRank()
    {
        switch (eliteRank)
        {
            case EliteRank.Elite:
                // Gi? nguy�n
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
    
    /// <summary>
    /// �p d?ng thu?c t�nh tinh nhu?
    /// </summary>
    private void ApplyEliteStats()
    {
        // N?u kh�ng c� component Enemy, kh�ng l�m g� c?
        if (enemy == null)
        {
            return;
        }
        
        // �p d?ng thu?c t�nh
        enemy.SetMaxHealthMultiplier(healthMultiplier);
        enemy.SetDamageMultiplier(damageMultiplier);
        enemy.SetSpeedMultiplier(speedMultiplier);
        enemy.SetExperienceMultiplier(experienceMultiplier);
        enemy.SetCurrencyMultiplier(currencyMultiplier);
        enemy.SetItemDropChanceMultiplier(itemDropChanceMultiplier);
        
        // �p d?ng gi?m s�t th??ng n?u c� gi�p
        if (hasArmor)
        {
            enemy.SetDamageReduction(damageReduction);
        }
        
        // ??t k�ch th??c
        transform.localScale *= sizeMultiplier;
        
        // ??t ti�u ?? n?u c�
        if (!string.IsNullOrEmpty(eliteTitle))
        {
            enemy.SetNamePrefix(eliteTitle);
        }
        else
        {
            // ??t ti�u ?? d?a tr�n c?p b?c
            switch (eliteRank)
            {
                case EliteRank.Elite:
                    enemy.SetNamePrefix("Tinh Nhu?");
                    break;
                    
                case EliteRank.Champion:
                    enemy.SetNamePrefix("Qu�n Qu�n");
                    break;
                    
                case EliteRank.Legendary:
                    enemy.SetNamePrefix("Huy?n Tho?i");
                    break;
                    
                case EliteRank.Mythic:
                    enemy.SetNamePrefix("Th?n Tho?i");
                    break;
            }
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n nh?n s�t th??ng
    /// </summary>
    private void HandleDamageTaken(Enemy enemy, float damage, float newHealth)
    {
        // N?u kh�ng ph?i tinh nhu?, kh�ng l�m g� c?
        if (!isElite)
        {
            return;
        }
        
        // N?u ?ang mi?n nhi?m, kh�ng nh?n s�t th??ng
        if (isImmune)
        {
            damageTaken = 0f;
            return;
        }
        
        // N?u c� gai, ph?n l?i s�t th??ng
        if (hasThorns)
        {
            // T�m ng??i g�y s�t th??ng
            GameObject attacker = FindAttacker();
            if (attacker != null)
            {
                // G�y s�t th??ng ph?n l?i
                float thornsDamage = damage * thornsDamagePercent;
                IDamageable damageable = attacker.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(thornsDamage);
                }
            }
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n thay ??i m�u
    /// </summary>
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // N?u kh�ng ph?i tinh nhu?, kh�ng l�m g� c?
        if (!isElite)
        {
            return;
        }
        
        // T�nh to�n ph?n tr?m m�u
        float healthPercent = currentHealth / maxHealth;
        
        // N?u c� berserker v� m�u d??i ng??ng
        if (hasBerserker && !isBerserking && healthPercent <= berserkerHealthThreshold)
        {
            ActivateBerserker();
        }
        
        // N?u c� enrage v� m�u d??i ng??ng
        if (hasEnrage && !isEnraged && healthPercent <= enrageHealthThreshold)
        {
            ActivateEnrage();
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n ch?t
    /// </summary>
    private void HandleDeath()
    {
        // N?u kh�ng ph?i tinh nhu?, kh�ng l�m g� c?
        if (!isElite)
        {
            return;
        }
        
        // N?u c� n? khi ch?t
        if (hasExplosionOnDeath)
        {
            Explode();
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n g�y s�t th??ng
    /// </summary>
    private void HandleDealDamage(GameObject target, float damage)
    {
        // N?u kh�ng ph?i tinh nhu?, kh�ng l�m g� c?
        if (!isElite)
        {
            return;
        }
        
        // N?u c� h�t m�u
        if (hasLifesteal && enemy != null)
        {
            // T�nh to�n l??ng m�u h�t
            float healAmount = damage * lifestealPercent;
            
            // H?i m�u
            enemy.Heal(healAmount);
        }
    }
    
    /// <summary>
    /// K�ch ho?t berserker
    /// </summary>
    private void ActivateBerserker()
    {
        // N?u ?� k�ch ho?t, kh�ng l�m g� c?
        if (isBerserking)
        {
            return;
        }
        
        // C?p nh?t tr?ng th�i
        isBerserking = true;
        
        // T?ng s�t th??ng v� t?c ??
        enemy.SetDamageMultiplier(damageMultiplier * (1f + berserkerDamageBonus));
        enemy.SetSpeedMultiplier(speedMultiplier * (1f + berserkerSpeedBonus));
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("Berserk", true);
        }
        
        // Hi?n th? hi?u ?ng
        ShowBerserkerEffect();
    }
    
    /// <summary>
    /// K�ch ho?t enrage
    /// </summary>
    private void ActivateEnrage()
    {
        // N?u ?� k�ch ho?t, kh�ng l�m g� c?
        if (isEnraged)
        {
            return;
        }
        
        // C?p nh?t tr?ng th�i
        isEnraged = true;
        
        // T?ng s�t th??ng v� t?c ??
        enemy.SetDamageMultiplier(damageMultiplier * (1f + enrageDamageBonus));
        enemy.SetSpeedMultiplier(speedMultiplier * (1f + enrageSpeedBonus));
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("Enraged", true);
        }
        
        // Hi?n th? hi?u ?ng
        ShowEnrageEffect();
    }
    
    /// <summary>
    /// X? l� d?ch chuy?n
    /// </summary>
    private void HandleTeleport()
    {
        // N?u ch?a h?t cooldown, kh�ng l�m g� c?
        if (Time.time < lastTeleportTime + teleportCooldown)
        {
            return;
        }
        
        // Ki?m tra n?u m�u th?p ho?c b? bao v�y
        bool shouldTeleport = false;
        
        // N?u m�u th?p
        if (enemy != null && enemy.GetHealthPercent() < 0.3f)
        {
            shouldTeleport = true;
        }
        
        // N?u b? bao v�y
        if (!shouldTeleport)
        {
            int nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Player")).Length;
            if (nearbyEnemies >= 2)
            {
                shouldTeleport = true;
            }
        }
        
        // N?u n�n d?ch chuy?n
        if (shouldTeleport)
        {
            Teleport();
        }
    }
    
    /// <summary>
    /// D?ch chuy?n
    /// </summary>
    private void Teleport()
    {
        // C?p nh?t th?i gian d?ch chuy?n
        lastTeleportTime = Time.time;
        
        // T�m v? tr� d?ch chuy?n
        Vector2 teleportPosition = FindTeleportPosition();
        
        // Hi?n th? hi?u ?ng t?i v? tr� c?
        ShowTeleportEffect(transform.position);
        
        // D?ch chuy?n
        transform.position = teleportPosition;
        
        // Hi?n th? hi?u ?ng t?i v? tr� m?i
        ShowTeleportEffect(transform.position);
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetTrigger("Teleport");
        }
    }
    
    /// <summary>
    /// T�m v? tr� d?ch chuy?n
    /// </summary>
    private Vector2 FindTeleportPosition()
    {
        // T�m m?c ti�u g?n nh?t
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 10f, LayerMask.GetMask("Player"));
        if (colliders.Length > 0)
        {
            // L?y m?c ti�u ??u ti�n
            Transform target = colliders[0].transform;
            
            // T�nh to�n h??ng ng?u nhi�n
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            
            // T�nh to�n v? tr� d?ch chuy?n
            Vector2 teleportPosition = (Vector2)target.position + randomDirection * teleportDistance;
            
            // Ki?m tra va ch?m
            RaycastHit2D hit = Physics2D.Raycast(transform.position, teleportPosition - (Vector2)transform.position, teleportDistance, LayerMask.GetMask("Obstacle"));
            if (hit.collider != null)
            {
                // N?u c� va ch?m, d?ch chuy?n ??n v? tr� va ch?m
                teleportPosition = hit.point - randomDirection * 0.5f;
            }
            
            return teleportPosition;
        }
        
        // N?u kh�ng c� m?c ti�u, d?ch chuy?n ng?u nhi�n
        Vector2 randomOffset = Random.insideUnitCircle.normalized * teleportDistance;
        return (Vector2)transform.position + randomOffset;
    }
    
    /// <summary>
    /// X? l� mi?n nhi?m
    /// </summary>
    private void HandleImmunity()
    {
        // N?u ?ang mi?n nhi?m, kh�ng l�m g� c?
        if (isImmune)
        {
            return;
        }
        
        // N?u ch?a h?t cooldown, kh�ng l�m g� c?
        if (Time.time < lastImmunityTime + immunityCooldown)
        {
            return;
        }
        
        // Ki?m tra n?u m�u th?p
        if (enemy != null && enemy.GetHealthPercent() < 0.2f)
        {
            ActivateImmunity();
        }
    }
    
    /// <summary>
    /// K�ch ho?t mi?n nhi?m
    /// </summary>
    private void ActivateImmunity()
    {
        // C?p nh?t tr?ng th�i
        isImmune = true;
        lastImmunityTime = Time.time;
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("Immune", true);
        }
        
        // Hi?n th? hi?u ?ng
        ShowImmunityEffect();
        
        // T?t mi?n nhi?m sau m?t kho?ng th?i gian
        StartCoroutine(DeactivateImmunity());
    }
    
    /// <summary>
    /// T?t mi?n nhi?m
    /// </summary>
    private IEnumerator DeactivateImmunity()
    {
        // ??i m?t kho?ng th?i gian
        yield return new WaitForSeconds(immunityDuration);
        
        // C?p nh?t tr?ng th�i
        isImmune = false;
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("Immune", false);
        }
        
        // ?n hi?u ?ng
        HideImmunityEffect();
    }
    
    /// <summary>
    /// X? l� cu?ng n?
    /// </summary>
    private void HandleFrenzy()
    {
        // N?u ?ang cu?ng n?, kh�ng l�m g� c?
        if (isFrenzied)
        {
            return;
        }
        
        // N?u ch?a h?t cooldown, kh�ng l�m g� c?
        if (Time.time < lastFrenzyTime + frenzyCooldown)
        {
            return;
        }
        
        // Ki?m tra n?u c� m?c ti�u g?n
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 3f, LayerMask.GetMask("Player"));
        if (colliders.Length > 0)
        {
            ActivateFrenzy();
        }
    }
    
    /// <summary>
    /// K�ch ho?t cu?ng n?
    /// </summary>
    private void ActivateFrenzy()
    {
        // C?p nh?t tr?ng th�i
        isFrenzied = true;
        lastFrenzyTime = Time.time;
        
        // T?ng t?c ?? t?n c�ng
        if (enemy != null && enemy.GetComponent<EnemyRangedAttack>() != null)
        {
            EnemyRangedAttack rangedAttack = enemy.GetComponent<EnemyRangedAttack>();
            rangedAttack.SetCooldownMultiplier(1f - frenzyAttackSpeedBonus);
        }
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("Frenzy", true);
        }
        
        // Hi?n th? hi?u ?ng
        ShowFrenzyEffect();
        
        // T?t cu?ng n? sau m?t kho?ng th?i gian
        StartCoroutine(DeactivateFrenzy());
    }
    
    /// <summary>
    /// T?t cu?ng n?
    /// </summary>
    private IEnumerator DeactivateFrenzy()
    {
        // ??i m?t kho?ng th?i gian
        yield return new WaitForSeconds(frenzyDuration);
        
        // C?p nh?t tr?ng th�i
        isFrenzied = false;
        
        // ??t l?i t?c ?? t?n c�ng
        if (enemy != null && enemy.GetComponent<EnemyRangedAttack>() != null)
        {
            EnemyRangedAttack rangedAttack = enemy.GetComponent<EnemyRangedAttack>();
            rangedAttack.SetCooldownMultiplier(1f);
        }
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("Frenzy", false);
        }
        
        // ?n hi?u ?ng
        HideFrenzyEffect();
    }
    
    /// <summary>
    /// T�i t?o m�u
    /// </summary>
    private IEnumerator RegenerateHealth()
    {
        while (true)
        {
            // N?u c� component Enemy, h?i m�u
            if (enemy != null)
            {
                enemy.Heal(regenerationAmount);
            }
            
            // ??i m?t kho?ng th?i gian
            yield return new WaitForSeconds(regenerationInterval);
        }
    }
    
    /// <summary>
    /// N? khi ch?t
    /// </summary>
    private void Explode()
    {
        // Hi?n th? hi?u ?ng n?
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // T�m t?t c? m?c ti�u trong ph?m vi
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Player"));
        
        // G�y s�t th??ng cho t?ng m?c ti�u
        foreach (Collider2D collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(explosionDamage);
            }
        }
    }
    
    /// <summary>
    /// T�m ng??i g�y s�t th??ng
    /// </summary>
    private GameObject FindAttacker()
    {
        // T�m t?t c? m?c ti�u trong ph?m vi
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("Player"));
        
        // N?u c� m?c ti�u, tr? v? m?c ti�u ??u ti�n
        if (colliders.Length > 0)
        {
            return colliders[0].gameObject;
        }
        
        return null;
    }
    
    /// <summary>
    /// T?o hi?u ?ng tinh nhu?
    /// </summary>
    private void CreateEliteEffects()
    {
        // T?o hi?u ?ng tinh nhu?
        if (eliteEffectPrefab != null)
        {
            eliteEffect = Instantiate(eliteEffectPrefab, transform.position, Quaternion.identity);
            eliteEffect.transform.SetParent(transform);
            eliteEffect.transform.localPosition = Vector3.zero;
            
            // ??t m�u
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = GetEliteColor();
            }
        }
        
        // T?o ch? b�o tinh nhu?
        if (eliteIndicatorPrefab != null)
        {
            eliteIndicator = Instantiate(eliteIndicatorPrefab, transform.position, Quaternion.identity);
            eliteIndicator.transform.SetParent(transform);
            eliteIndicator.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            
            // ??t m�u
            SpriteRenderer indicatorRenderer = eliteIndicator.GetComponent<SpriteRenderer>();
            if (indicatorRenderer != null)
            {
                indicatorRenderer.color = GetEliteColor();
            }
        }
    }
    
    /// <summary>
    /// H?y hi?u ?ng tinh nhu?
    /// </summary>
    private void DestroyEliteEffects()
    {
        // H?y hi?u ?ng tinh nhu?
        if (eliteEffect != null)
        {
            Destroy(eliteEffect);
            eliteEffect = null;
        }
        
        // H?y ch? b�o tinh nhu?
        if (eliteIndicator != null)
        {
            Destroy(eliteIndicator);
            eliteIndicator = null;
        }
    }
    
    /// <summary>
    /// T?o outline
    /// </summary>
    private void CreateOutline()
    {
        // N?u kh�ng c� SpriteRenderer, kh�ng l�m g� c?
        if (spriteRenderer == null)
        {
            return;
        }
        
        // T?o material outline
        outlineMaterial = new Material(Shader.Find("Sprites/Outline"));
        outlineMaterial.SetColor("_OutlineColor", GetEliteColor());
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        
        // �p d?ng material
        spriteRenderer.material = outlineMaterial;
    }
    
    /// <summary>
    /// Hi?u ?ng pulse
    /// </summary>
    private IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        float time = 0f;
        
        while (true)
        {
            // T�nh to�n k�ch th??c pulse
            float pulse = 1f + Mathf.Sin(time * pulseRate) * pulseAmount;
            
            // �p d?ng k�ch th??c
            transform.localScale = originalScale * pulse;
            
            // T?ng th?i gian
            time += Time.deltaTime;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Hi?n th? hi?u ?ng berserker
    /// </summary>
    private void ShowBerserkerEffect()
    {
        // ??i m�u outline n?u c�
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", Color.red);
        }
        
        // ??i m�u hi?u ?ng n?u c�
        if (eliteEffect != null)
        {
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = Color.red;
            }
        }
    }
    
    /// <summary>
    /// Hi?n th? hi?u ?ng enrage
    /// </summary>
    private void ShowEnrageEffect()
    {
        // ??i m�u outline n?u c�
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", new Color(1f, 0.5f, 0f));
        }
        
        // ??i m�u hi?u ?ng n?u c�
        if (eliteEffect != null)
        {
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = new Color(1f, 0.5f, 0f);
            }
        }
    }
    
    /// <summary>
    /// Hi?n th? hi?u ?ng d?ch chuy?n
    /// </summary>
    private void ShowTeleportEffect(Vector3 position)
    {
        // N?u kh�ng c� prefab, kh�ng l�m g� c?
        if (teleportEffectPrefab == null)
        {
            return;
        }
        
        // T?o hi?u ?ng
        GameObject effect = Instantiate(teleportEffectPrefab, position, Quaternion.identity);
        
        // ??t m�u
        ParticleSystem particleSystem = effect.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = GetEliteColor();
        }
        
        // H?y sau m?t kho?ng th?i gian
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// Hi?n th? hi?u ?ng mi?n nhi?m
    /// </summary>
    private void ShowImmunityEffect()
    {
        // ??i m�u outline n?u c�
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", Color.white);
        }
        
        // ??i m�u hi?u ?ng n?u c�
        if (eliteEffect != null)
        {
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = Color.white;
            }
        }
    }
    
    /// <summary>
    /// ?n hi?u ?ng mi?n nhi?m
    /// </summary>
    private void HideImmunityEffect()
    {
        // ??t l?i m�u outline n?u c�
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", GetEliteColor());
        }
        
        // ??t l?i m�u hi?u ?ng n?u c�
        if (eliteEffect != null)
        {
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = GetEliteColor();
            }
        }
    }
    
    /// <summary>
    /// Hi?n th? hi?u ?ng cu?ng n?
    /// </summary>
    private void ShowFrenzyEffect()
    {
        // ??i m�u outline n?u c�
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", new Color(1f, 0f, 1f));
        }
        
        // ??i m�u hi?u ?ng n?u c�
        if (eliteEffect != null)
        {
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = new Color(1f, 0f, 1f);
            }
        }
    }
    
    /// <summary>
    /// ?n hi?u ?ng cu?ng n?
    /// </summary>
    private void HideFrenzyEffect()
    {
        // ??t l?i m�u outline n?u c�
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", GetEliteColor());
        }
        
        // ??t l?i m�u hi?u ?ng n?u c�
        if (eliteEffect != null)
        {
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = GetEliteColor();
            }
        }
    }
    
    /// <summary>
    /// L?y m�u tinh nhu? d?a tr�n c?p b?c
    /// </summary>
    private Color GetEliteColor()
    {
        switch (eliteRank)
        {
            case EliteRank.Elite:
                return eliteColor;
                
            case EliteRank.Champion:
                return new Color(0f, 0.7f, 1f);
                
            case EliteRank.Legendary:
                return new Color(1f, 0.5f, 0f);
                
            case EliteRank.Mythic:
                return new Color(1f, 0f, 1f);
                
            default:
                return eliteColor;
        }
    }
    
    /// <summary>
    /// V? Gizmos ?? debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // V? ph?m vi n? n?u c�
        if (hasExplosionOnDeath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }

    // --- B? sung property v� method cho EliteController ---
    public bool IsElite { get => isElite; set => isElite = value; }
    public void SetElite(bool value) { isElite = value; }
    public void SetEliteRank(EliteRank rank) { eliteRank = rank; }
    public void SetEliteEffectPrefab(GameObject prefab) { eliteEffectPrefab = prefab; }
    public void SetEliteIndicatorPrefab(GameObject prefab) { eliteIndicatorPrefab = prefab; }
    public void SetEliteColor(Color color) { eliteColor = color; }
    public void SetEliteTitle(string title) { eliteTitle = title; }
    public void SetRandomizeAbilities(bool value) { randomizeAbilities = value; }
    public void SetRandomAbilityRange(int min, int max) { minRandomAbilities = min; maxRandomAbilities = max; }
}