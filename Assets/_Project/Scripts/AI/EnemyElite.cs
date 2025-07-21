using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Qu?n lý k? thù tinh nhu? v?i kh? n?ng ??c bi?t và thu?c tính m?nh h?n
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
    
    // Các component
    private Enemy enemy;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    
    // Tr?ng thái tinh nhu?
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
        Champion,   // Quán quân
        Legendary,  // Huy?n tho?i
        Mythic      // Th?n tho?i
    }
    
    private void Awake()
    {
        // L?y các component c?n thi?t
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // L?u material g?c
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // ??ng ký s? ki?n
        if (enemy != null)
        {
            enemy.OnDamageTaken += HandleDamageTaken;
            enemy.OnHealthChanged += HandleHealthChanged;
            enemy.OnDeath += HandleDeath;
            enemy.OnDealDamage += HandleDealDamage;
        }
        
        // N?u ng?u nhiên hóa kh? n?ng, ch?n ng?u nhiên
        if (randomizeAbilities)
        {
            RandomizeAbilities();
        }
    }
    
    private void Start()
    {
        // N?u không ph?i tinh nhu?, không làm gì c?
        if (!isElite)
        {
            return;
        }
        
        // Áp d?ng thu?c tính tinh nhu?
        ApplyEliteStats();
        
        // T?o hi?u ?ng tinh nhu?
        CreateEliteEffects();
        
        // B?t ??u tái t?o máu n?u có
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
        // N?u không ph?i tinh nhu?, không làm gì c?
        if (!isElite)
        {
            return;
        }
        
        // X? lý d?ch chuy?n n?u có
        if (hasTeleport)
        {
            HandleTeleport();
        }
        
        // X? lý mi?n nhi?m n?u có
        if (hasImmunity)
        {
            HandleImmunity();
        }
        
        // X? lý cu?ng n? n?u có
        if (hasFrenzy)
        {
            HandleFrenzy();
        }
    }
    
    private void OnDestroy()
    {
        // H?y ??ng ký s? ki?n
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
    /// Ng?u nhiên hóa kh? n?ng
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
        
        // Danh sách kh? n?ng
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
        
        // Xáo tr?n danh sách
        for (int i = 0; i < abilities.Count; i++)
        {
            int randomIndex = Random.Range(i, abilities.Count);
            System.Action temp = abilities[i];
            abilities[i] = abilities[randomIndex];
            abilities[randomIndex] = temp;
        }
        
        // Ch?n s? l??ng kh? n?ng ng?u nhiên
        int abilityCount = Random.Range(minRandomAbilities, maxRandomAbilities + 1);
        abilityCount = Mathf.Min(abilityCount, abilities.Count);
        
        // Kích ho?t kh? n?ng
        for (int i = 0; i < abilityCount; i++)
        {
            abilities[i]();
        }
        
        // ?i?u ch?nh thu?c tính d?a trên c?p b?c
        AdjustStatsByRank();
    }
    
    /// <summary>
    /// ?i?u ch?nh thu?c tính d?a trên c?p b?c
    /// </summary>
    private void AdjustStatsByRank()
    {
        switch (eliteRank)
        {
            case EliteRank.Elite:
                // Gi? nguyên
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
    /// Áp d?ng thu?c tính tinh nhu?
    /// </summary>
    private void ApplyEliteStats()
    {
        // N?u không có component Enemy, không làm gì c?
        if (enemy == null)
        {
            return;
        }
        
        // Áp d?ng thu?c tính
        enemy.SetMaxHealthMultiplier(healthMultiplier);
        enemy.SetDamageMultiplier(damageMultiplier);
        enemy.SetSpeedMultiplier(speedMultiplier);
        enemy.SetExperienceMultiplier(experienceMultiplier);
        enemy.SetCurrencyMultiplier(currencyMultiplier);
        enemy.SetItemDropChanceMultiplier(itemDropChanceMultiplier);
        
        // Áp d?ng gi?m sát th??ng n?u có giáp
        if (hasArmor)
        {
            enemy.SetDamageReduction(damageReduction);
        }
        
        // ??t kích th??c
        transform.localScale *= sizeMultiplier;
        
        // ??t tiêu ?? n?u có
        if (!string.IsNullOrEmpty(eliteTitle))
        {
            enemy.SetNamePrefix(eliteTitle);
        }
        else
        {
            // ??t tiêu ?? d?a trên c?p b?c
            switch (eliteRank)
            {
                case EliteRank.Elite:
                    enemy.SetNamePrefix("Tinh Nhu?");
                    break;
                    
                case EliteRank.Champion:
                    enemy.SetNamePrefix("Quán Quân");
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
    /// X? lý s? ki?n nh?n sát th??ng
    /// </summary>
    private void HandleDamageTaken(Enemy enemy, float damage, float newHealth)
    {
        // N?u không ph?i tinh nhu?, không làm gì c?
        if (!isElite)
        {
            return;
        }
        
        // N?u ?ang mi?n nhi?m, không nh?n sát th??ng
        if (isImmune)
        {
            damageTaken = 0f;
            return;
        }
        
        // N?u có gai, ph?n l?i sát th??ng
        if (hasThorns)
        {
            // Tìm ng??i gây sát th??ng
            GameObject attacker = FindAttacker();
            if (attacker != null)
            {
                // Gây sát th??ng ph?n l?i
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
    /// X? lý s? ki?n thay ??i máu
    /// </summary>
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // N?u không ph?i tinh nhu?, không làm gì c?
        if (!isElite)
        {
            return;
        }
        
        // Tính toán ph?n tr?m máu
        float healthPercent = currentHealth / maxHealth;
        
        // N?u có berserker và máu d??i ng??ng
        if (hasBerserker && !isBerserking && healthPercent <= berserkerHealthThreshold)
        {
            ActivateBerserker();
        }
        
        // N?u có enrage và máu d??i ng??ng
        if (hasEnrage && !isEnraged && healthPercent <= enrageHealthThreshold)
        {
            ActivateEnrage();
        }
    }
    
    /// <summary>
    /// X? lý s? ki?n ch?t
    /// </summary>
    private void HandleDeath()
    {
        // N?u không ph?i tinh nhu?, không làm gì c?
        if (!isElite)
        {
            return;
        }
        
        // N?u có n? khi ch?t
        if (hasExplosionOnDeath)
        {
            Explode();
        }
    }
    
    /// <summary>
    /// X? lý s? ki?n gây sát th??ng
    /// </summary>
    private void HandleDealDamage(GameObject target, float damage)
    {
        // N?u không ph?i tinh nhu?, không làm gì c?
        if (!isElite)
        {
            return;
        }
        
        // N?u có hút máu
        if (hasLifesteal && enemy != null)
        {
            // Tính toán l??ng máu hút
            float healAmount = damage * lifestealPercent;
            
            // H?i máu
            enemy.Heal(healAmount);
        }
    }
    
    /// <summary>
    /// Kích ho?t berserker
    /// </summary>
    private void ActivateBerserker()
    {
        // N?u ?ã kích ho?t, không làm gì c?
        if (isBerserking)
        {
            return;
        }
        
        // C?p nh?t tr?ng thái
        isBerserking = true;
        
        // T?ng sát th??ng và t?c ??
        enemy.SetDamageMultiplier(damageMultiplier * (1f + berserkerDamageBonus));
        enemy.SetSpeedMultiplier(speedMultiplier * (1f + berserkerSpeedBonus));
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("Berserk", true);
        }
        
        // Hi?n th? hi?u ?ng
        ShowBerserkerEffect();
    }
    
    /// <summary>
    /// Kích ho?t enrage
    /// </summary>
    private void ActivateEnrage()
    {
        // N?u ?ã kích ho?t, không làm gì c?
        if (isEnraged)
        {
            return;
        }
        
        // C?p nh?t tr?ng thái
        isEnraged = true;
        
        // T?ng sát th??ng và t?c ??
        enemy.SetDamageMultiplier(damageMultiplier * (1f + enrageDamageBonus));
        enemy.SetSpeedMultiplier(speedMultiplier * (1f + enrageSpeedBonus));
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("Enraged", true);
        }
        
        // Hi?n th? hi?u ?ng
        ShowEnrageEffect();
    }
    
    /// <summary>
    /// X? lý d?ch chuy?n
    /// </summary>
    private void HandleTeleport()
    {
        // N?u ch?a h?t cooldown, không làm gì c?
        if (Time.time < lastTeleportTime + teleportCooldown)
        {
            return;
        }
        
        // Ki?m tra n?u máu th?p ho?c b? bao vây
        bool shouldTeleport = false;
        
        // N?u máu th?p
        if (enemy != null && enemy.GetHealthPercent() < 0.3f)
        {
            shouldTeleport = true;
        }
        
        // N?u b? bao vây
        if (!shouldTeleport)
        {
            int nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Player")).Length;
            if (nearbyEnemies >= 2)
            {
                shouldTeleport = true;
            }
        }
        
        // N?u nên d?ch chuy?n
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
        
        // Tìm v? trí d?ch chuy?n
        Vector2 teleportPosition = FindTeleportPosition();
        
        // Hi?n th? hi?u ?ng t?i v? trí c?
        ShowTeleportEffect(transform.position);
        
        // D?ch chuy?n
        transform.position = teleportPosition;
        
        // Hi?n th? hi?u ?ng t?i v? trí m?i
        ShowTeleportEffect(transform.position);
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetTrigger("Teleport");
        }
    }
    
    /// <summary>
    /// Tìm v? trí d?ch chuy?n
    /// </summary>
    private Vector2 FindTeleportPosition()
    {
        // Tìm m?c tiêu g?n nh?t
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 10f, LayerMask.GetMask("Player"));
        if (colliders.Length > 0)
        {
            // L?y m?c tiêu ??u tiên
            Transform target = colliders[0].transform;
            
            // Tính toán h??ng ng?u nhiên
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            
            // Tính toán v? trí d?ch chuy?n
            Vector2 teleportPosition = (Vector2)target.position + randomDirection * teleportDistance;
            
            // Ki?m tra va ch?m
            RaycastHit2D hit = Physics2D.Raycast(transform.position, teleportPosition - (Vector2)transform.position, teleportDistance, LayerMask.GetMask("Obstacle"));
            if (hit.collider != null)
            {
                // N?u có va ch?m, d?ch chuy?n ??n v? trí va ch?m
                teleportPosition = hit.point - randomDirection * 0.5f;
            }
            
            return teleportPosition;
        }
        
        // N?u không có m?c tiêu, d?ch chuy?n ng?u nhiên
        Vector2 randomOffset = Random.insideUnitCircle.normalized * teleportDistance;
        return (Vector2)transform.position + randomOffset;
    }
    
    /// <summary>
    /// X? lý mi?n nhi?m
    /// </summary>
    private void HandleImmunity()
    {
        // N?u ?ang mi?n nhi?m, không làm gì c?
        if (isImmune)
        {
            return;
        }
        
        // N?u ch?a h?t cooldown, không làm gì c?
        if (Time.time < lastImmunityTime + immunityCooldown)
        {
            return;
        }
        
        // Ki?m tra n?u máu th?p
        if (enemy != null && enemy.GetHealthPercent() < 0.2f)
        {
            ActivateImmunity();
        }
    }
    
    /// <summary>
    /// Kích ho?t mi?n nhi?m
    /// </summary>
    private void ActivateImmunity()
    {
        // C?p nh?t tr?ng thái
        isImmune = true;
        lastImmunityTime = Time.time;
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("Immune", true);
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
        
        // C?p nh?t tr?ng thái
        isImmune = false;
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("Immune", false);
        }
        
        // ?n hi?u ?ng
        HideImmunityEffect();
    }
    
    /// <summary>
    /// X? lý cu?ng n?
    /// </summary>
    private void HandleFrenzy()
    {
        // N?u ?ang cu?ng n?, không làm gì c?
        if (isFrenzied)
        {
            return;
        }
        
        // N?u ch?a h?t cooldown, không làm gì c?
        if (Time.time < lastFrenzyTime + frenzyCooldown)
        {
            return;
        }
        
        // Ki?m tra n?u có m?c tiêu g?n
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 3f, LayerMask.GetMask("Player"));
        if (colliders.Length > 0)
        {
            ActivateFrenzy();
        }
    }
    
    /// <summary>
    /// Kích ho?t cu?ng n?
    /// </summary>
    private void ActivateFrenzy()
    {
        // C?p nh?t tr?ng thái
        isFrenzied = true;
        lastFrenzyTime = Time.time;
        
        // T?ng t?c ?? t?n công
        if (enemy != null && enemy.GetComponent<EnemyRangedAttack>() != null)
        {
            EnemyRangedAttack rangedAttack = enemy.GetComponent<EnemyRangedAttack>();
            rangedAttack.SetCooldownMultiplier(1f - frenzyAttackSpeedBonus);
        }
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("Frenzy", true);
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
        
        // C?p nh?t tr?ng thái
        isFrenzied = false;
        
        // ??t l?i t?c ?? t?n công
        if (enemy != null && enemy.GetComponent<EnemyRangedAttack>() != null)
        {
            EnemyRangedAttack rangedAttack = enemy.GetComponent<EnemyRangedAttack>();
            rangedAttack.SetCooldownMultiplier(1f);
        }
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("Frenzy", false);
        }
        
        // ?n hi?u ?ng
        HideFrenzyEffect();
    }
    
    /// <summary>
    /// Tái t?o máu
    /// </summary>
    private IEnumerator RegenerateHealth()
    {
        while (true)
        {
            // N?u có component Enemy, h?i máu
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
        
        // Tìm t?t c? m?c tiêu trong ph?m vi
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, LayerMask.GetMask("Player"));
        
        // Gây sát th??ng cho t?ng m?c tiêu
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
    /// Tìm ng??i gây sát th??ng
    /// </summary>
    private GameObject FindAttacker()
    {
        // Tìm t?t c? m?c tiêu trong ph?m vi
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("Player"));
        
        // N?u có m?c tiêu, tr? v? m?c tiêu ??u tiên
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
            
            // ??t màu
            ParticleSystem particleSystem = eliteEffect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = GetEliteColor();
            }
        }
        
        // T?o ch? báo tinh nhu?
        if (eliteIndicatorPrefab != null)
        {
            eliteIndicator = Instantiate(eliteIndicatorPrefab, transform.position, Quaternion.identity);
            eliteIndicator.transform.SetParent(transform);
            eliteIndicator.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            
            // ??t màu
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
        
        // H?y ch? báo tinh nhu?
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
        // N?u không có SpriteRenderer, không làm gì c?
        if (spriteRenderer == null)
        {
            return;
        }
        
        // T?o material outline
        outlineMaterial = new Material(Shader.Find("Sprites/Outline"));
        outlineMaterial.SetColor("_OutlineColor", GetEliteColor());
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        
        // Áp d?ng material
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
            // Tính toán kích th??c pulse
            float pulse = 1f + Mathf.Sin(time * pulseRate) * pulseAmount;
            
            // Áp d?ng kích th??c
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
        // ??i màu outline n?u có
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", Color.red);
        }
        
        // ??i màu hi?u ?ng n?u có
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
        // ??i màu outline n?u có
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", new Color(1f, 0.5f, 0f));
        }
        
        // ??i màu hi?u ?ng n?u có
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
        // N?u không có prefab, không làm gì c?
        if (teleportEffectPrefab == null)
        {
            return;
        }
        
        // T?o hi?u ?ng
        GameObject effect = Instantiate(teleportEffectPrefab, position, Quaternion.identity);
        
        // ??t màu
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
        // ??i màu outline n?u có
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", Color.white);
        }
        
        // ??i màu hi?u ?ng n?u có
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
        // ??t l?i màu outline n?u có
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", GetEliteColor());
        }
        
        // ??t l?i màu hi?u ?ng n?u có
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
        // ??i màu outline n?u có
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", new Color(1f, 0f, 1f));
        }
        
        // ??i màu hi?u ?ng n?u có
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
        // ??t l?i màu outline n?u có
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", GetEliteColor());
        }
        
        // ??t l?i màu hi?u ?ng n?u có
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
    /// L?y màu tinh nhu? d?a trên c?p b?c
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
        // V? ph?m vi n? n?u có
        if (hasExplosionOnDeath)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }

    // --- B? sung property và method cho EliteController ---
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