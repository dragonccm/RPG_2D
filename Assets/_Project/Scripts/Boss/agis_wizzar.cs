using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Animation;

public class agis_wizzar : MonoBehaviour
{
    [Header("Boss Configuration")]
    [SerializeField] private float maxHealth = 1000f; // Chỉ để thiết lập, thực tế dùng Character component
    
    [Header("2D Movement")]
    public float moveSpeed = 2f;
    private Rigidbody2D rb2d;
    private SpriteRenderer spriteRenderer;
    private bool facingRight = true;
    
    [Header("Enhanced Teleport System")]
    [Tooltip("Ngưỡng máu để bắt đầu teleport thường xuyên (70% = 0.7)")]
    public float teleportHealthThreshold = 0.7f; // Enabled - tăng ngưỡng để teleport sớm hơn
    [Tooltip("Cooldown teleport bình thường")]
    public float teleportCooldown = 2f; // Giảm từ 3s xuống 2s
    [Tooltip("Cooldown teleport khi máu thấp")]
    public float lowHealthTeleportCooldown = 0.8f; // Teleport rất nhanh khi máu thấp
    [Tooltip("Ngưỡng máu để teleport cực nhanh (30% = 0.3)")]
    public float lowHealthThreshold = 0.3f;
    public float teleportRange = 5f;
    public GameObject teleportEffect;
    
    [Header("Fireball Skill")]
    public GameObject fireballPrefab;
    public float fireballCooldown = 1f;
    public float fireballSpeed = 10f;
    public float fireballDamage = 25f;
    public float fireballRange = 8f;
    public AudioClip fireballCastSound;
    public AudioClip fireballImpactSound;
    
    [Header("Enhanced Berserk Mode")]
    [Tooltip("Ngưỡng máu để kích hoạt chế độ cuồng bạo (40% = 0.4)")]
    public float berserkHealthThreshold = 0.4f; // Enabled - kích hoạt cuồng bạo
    [Tooltip("Tỷ lệ phóng to boss khi cuồng bạo")]
    public float berserkSizeMultiplier = 1.5f; // Boss sẽ to gấp 1.5 lần
    [Tooltip("Thời gian để phóng to boss")]
    public float berserkTransformTime = 1f;
    [Tooltip("Tốc độ tăng di chuyển khi cuồng bạo")]
    public float berserkSpeedMultiplier = 1.3f;
    public float berserkFireballCooldown = 0.3f;
    public int berserkFireballCount = 5; // Tăng từ 3 lên 5
    
    [Header("AOE Skill")]
    public GameObject aoePrefab;
    public float aoeCooldown = 8f;
    public float aoeRadius = 3f;
    public float aoeDamage = 50f;
    public float aoeChargeTime = 2f; // Tăng thời gian warning
    public AudioClip aoeCastSound;
    
    [Header("📢 Enhanced Warning System")]
    [Tooltip("Màu warning cho AOE attack")]
    public Color aoeWarningColor = new Color(1f, 0.2f, 0.2f, 0.5f);
    [Tooltip("Màu warning cho Fireball")]
    public Color fireballWarningColor = new Color(1f, 0.6f, 0f, 0.4f);
    [Tooltip("Thời gian warning trước khi Fireball hit")]
    public float fireballWarningTime = 0.8f;
    
    [Header("AI Settings")]
    public Transform playerTarget;
    public float detectionRange = 15f;
    public float attackRange = 6f;
    public float minDistance = 3f;
    
    [Header("🎯 Boss Mechanics (AAA Game Style)")]
    [Tooltip("Số lần telegraph tấn công trước khi thực sự tấn công")]
    public int telegraphCount = 2;
    [Tooltip("Thời gian nghỉ giữa các đợt tấn công để player có thể phản ứng")]
    public float attackCooldownBetweenPhases = 1.5f;
    [Tooltip("Giảm tốc độ tấn công để cho player thời gian phản ứng")]
    public float baseAttackSpeedMultiplier = 0.7f;
    
    // Character system integration - sử dụng hệ thống cũ để xây dựng hệ thống mới
    private Character character;
    private Animator animator;
    private bool isSkillActive = false;
    
    // Enhanced Boss States
    private bool isBerserk = false;
    private bool hasBerserkTriggered = false;
    private Vector3 originalScale;
    private float originalMoveSpeed;

    // Skill cooldown timers
    private float teleportCooldownTimer = 0f;
    private float fireballCooldownTimer = 0f;
    private float aoeCooldownTimer = 0f;

    // Properties utilizing Character system
    public float CurrentHealth => character != null ? character.CurrentHealth : 0f;
    public float MaxHealth => character != null ? character.MaxHealth : maxHealth;
    public bool IsDead => character != null ? character.IsDead : false;
    public float GetHealthPercentage() => character != null ? character.CurrentHealth / character.MaxHealth : 0f;

    void Start()
    {
        // Tích hợp với Character system - tận dụng hệ thống cũ
        character = GetComponent<Character>();
        if (character == null)
        {
            // Tự động thêm Character component nếu chưa có
            character = gameObject.AddComponent<Character>();
            Debug.Log("Agis Wizzar: Added Character component automatically");
        }
        
        // Thiết lập health thông qua Character system
        character.MaxHealth = maxHealth;
        character.CurrentHealth = maxHealth;
        
        // Subscribe to Character events để phản ứng với damage
        character.OnDamageTaken += OnDamageTaken;
        character.OnDeath += OnCharacterDeath;
        
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
            rb2d = gameObject.AddComponent<Rigidbody2D>();
            
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
            
        // CRITICAL FIX: Ensure Boss has proper Animator Controller
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No Animator Controller assigned! Attempting to load BossAnimator.controller...");
            
            // Try to load BossAnimator.controller
            var bossController = Resources.Load<RuntimeAnimatorController>("BossAnimator");
            if (bossController == null)
            {
                // Fallback: try to find any suitable controller
                var controllers = Resources.FindObjectsOfTypeAll<RuntimeAnimatorController>();
                foreach (var controller in controllers)
                {
                    if (controller.name.ToLower().Contains("boss") || controller.name.ToLower().Contains("agis"))
                    {
                        bossController = controller;
                        break;
                    }
                }
            }
            
            if (bossController != null)
            {
                animator.runtimeAnimatorController = bossController;
                Debug.Log($"[{gameObject.name}] Successfully assigned {bossController.name} controller!");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Could not find suitable Animator Controller! Animation will not work!");
            }
        }
            
        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 5f;
        rb2d.freezeRotation = true;
        
        // ENHANCED: Setup EnemyAnimatorController for boss
        var animatorController = GetComponent<EnemyAnimatorController>();
        if (animatorController == null)
        {
            animatorController = gameObject.AddComponent<EnemyAnimatorController>();
            Debug.Log("Agis Wizzar: Added EnemyAnimatorController automatically");
        }
        
        // Lưu trữ scale và speed gốc
        originalScale = transform.localScale;
        originalMoveSpeed = moveSpeed;
            
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }
        
        StartCoroutine(BossUpdateLoop());
        Debug.Log($"Agis Wizzar initialized! Player target: {(playerTarget != null ? playerTarget.name : "NOT FOUND")}, Health: {CurrentHealth}/{MaxHealth}");
    }

    void Update()
    {
        UpdateCooldowns();
        
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log($"Boss Debug - {GetBossStatus()}");
            Debug.Log($"Player: {(playerTarget != null ? playerTarget.name : "NULL")}, " +
                     $"Distance: {(playerTarget != null ? Vector2.Distance(transform.position, playerTarget.position) : 0f)}, " +
                     $"IsSkillActive: {isSkillActive}");
        }
        
        // Debug key để test berserk mode
        if (Input.GetKeyDown(KeyCode.X) && !hasBerserkTriggered)
        {
            Debug.Log("🔥 FORCE BERSERK MODE ACTIVATED! 🔥");
            StartCoroutine(EnterBerserkMode());
        }
    }

    void UpdateCooldowns()
    {
        if (teleportCooldownTimer > 0)
            teleportCooldownTimer -= Time.deltaTime;
            
        if (fireballCooldownTimer > 0)
            fireballCooldownTimer -= Time.deltaTime;
            
        if (aoeCooldownTimer > 0)
            aoeCooldownTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Event handler khi Character component nhận damage
    /// </summary>
    private void OnDamageTaken(float damage)
    {
        Debug.Log($"Agis Wizzar health changed: {CurrentHealth}/{MaxHealth} (took {damage} damage)");
        
        // Kiểm tra điều kiện kích hoạt kỹ năng dựa trên % máu
        CheckHealthBasedSkills();
    }

    /// <summary>
    /// Event handler khi Character component chết
    /// </summary>
    private void OnCharacterDeath()
    {
        Debug.Log("Agis Wizzar defeated through Character system!");
        OnBossDeath();
    }

    /// <summary>
    /// Kiểm tra và kích hoạt kỹ năng dựa trên tỷ lệ máu hiện tại
    /// </summary>
    private void CheckHealthBasedSkills()
    {
        float healthPercent = GetHealthPercentage();
        
        // Enhanced Teleport System - teleport thường xuyên hơn
        if (teleportHealthThreshold > 0 && healthPercent <= teleportHealthThreshold && teleportCooldownTimer <= 0)
        {
            StartCoroutine(TeleportSkill());
        }
        
        // Berserk Mode Activation - chuyển sang cuồng bạo và phóng to
        if (berserkHealthThreshold > 0 && healthPercent <= berserkHealthThreshold && !hasBerserkTriggered)
        {
            StartCoroutine(EnterBerserkMode());
        }
        
        // AOE skill khi máu trung bình
        if (healthPercent <= 0.5f && aoeCooldownTimer <= 0 && !isSkillActive)
        {
            StartCoroutine(AOESkill());
        }
    }

    IEnumerator BossUpdateLoop()
    {
        while (!IsDead)
        {
            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                    Debug.Log("Boss found player again!");
                }
            }
            
            if (playerTarget != null && !isSkillActive)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
                
                if (distanceToPlayer <= detectionRange)
                {
                    FaceTarget2D(playerTarget.position);
                    HandleMovement(distanceToPlayer);
                    HandleSkillUsage(distanceToPlayer);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    void HandleMovement(float distanceToPlayer)
    {
        if (isSkillActive || !character.CanMove()) return;
        
        Vector2 targetPosition = Vector2.zero;
        bool shouldMove = false;
        
        if (distanceToPlayer > attackRange)
        {
            Vector2 direction = (playerTarget.position - transform.position).normalized;
            targetPosition = (Vector2)transform.position + direction * moveSpeed * Time.deltaTime;
            shouldMove = true;
        }
        else if (distanceToPlayer < minDistance)
        {
            Vector2 direction = (transform.position - playerTarget.position).normalized;
            targetPosition = (Vector2)transform.position + direction * moveSpeed * Time.deltaTime;
            shouldMove = true;
        }
        
        if (shouldMove)
        {
            rb2d.MovePosition(targetPosition);
            if (animator != null)
                animator.SetBool(AnimationParameters.IsMoving, true);
        }
        else
        {
            if (animator != null)
                animator.SetBool(AnimationParameters.IsMoving, false);
        }
    }

    void HandleSkillUsage(float distanceToPlayer)
    {
        // Enhanced teleport logic với tần xuất cao hơn
        if (teleportCooldownTimer <= 0 && distanceToPlayer <= teleportRange)
        {
            // Quyết định có teleport không dựa trên health
            float healthPercent = GetHealthPercentage();
            bool shouldTeleport = false;
            
            if (healthPercent <= lowHealthThreshold)
            {
                // Máu rất thấp - teleport rất thường xuyên (60% chance - giảm từ 80%)
                shouldTeleport = Random.Range(0f, 1f) < 0.6f;
            }
            else if (healthPercent <= teleportHealthThreshold)
            {
                // Máu thấp - teleport thường xuyên (35% chance - giảm từ 50%)
                shouldTeleport = Random.Range(0f, 1f) < 0.35f;
            }
            
            if (shouldTeleport)
            {
                StartCoroutine(TelegraphedTeleport());
                return;
            }
        }
        
        // Fireball skill với logic berserk và telegraph
        if (fireballCooldownTimer <= 0 && distanceToPlayer <= fireballRange)
        {
            StartCoroutine(TelegraphedFireballAttack());
            return;
        }
    }

    /// <summary>
    /// Telegraphed Teleport - shows warning before teleporting
    /// </summary>
    IEnumerator TelegraphedTeleport()
    {
        isSkillActive = true;
        
        // Create warning effect at current position
        Effect2DManager.CreateWarningIndicator2D(transform.position, 1.5f, 0.8f, Color.magenta);
        
        yield return new WaitForSeconds(0.8f);
        
        // Now perform the actual teleport
        yield return StartCoroutine(TeleportSkill());
    }

    /// <summary>
    /// Telegraphed Fireball Attack - shows intent before attacking
    /// </summary>
    IEnumerator TelegraphedFireballAttack()
    {
        isSkillActive = true;
        
        // Show casting effect
        Effect2DManager.CreateFallbackEffect2D(transform.position, Color.red, 1.2f, 1f);
        
        // Brief pause to show intent
        yield return new WaitForSeconds(0.3f * baseAttackSpeedMultiplier);
        
        // Now perform the actual fireball attack
        yield return StartCoroutine(FireballSkill());
    }

    /// <summary>
    /// Public method để nhận damage từ bên ngoài - forward đến Character system
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (character != null)
        {
            character.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Public method để heal - forward đến Character system
    /// </summary>
    public void Heal(float amount)
    {
        if (character != null)
        {
            character.Heal(amount);
        }
    }

    IEnumerator TeleportSkill()
    {
        isSkillActive = true;
        
        // Dynamic cooldown dựa trên health
        float healthPercent = GetHealthPercentage();
        if (healthPercent <= lowHealthThreshold)
        {
            teleportCooldownTimer = lowHealthTeleportCooldown; // 0.8s khi máu thấp
        }
        else
        {
            teleportCooldownTimer = teleportCooldown; // 2s bình thường
        }
        
        Debug.Log($"Agis Wizzar is teleporting! Health: {healthPercent:P1}, Cooldown: {teleportCooldownTimer}s");
        
        // ENHANCED: Use EnemyAnimatorController for teleport animation
        var animatorController = GetComponent<EnemyAnimatorController>();
        if (animatorController != null)
        {
            animatorController.PlayTeleportAnimation();
        }
        else if (animator != null)
        {
            animator.SetTrigger(AnimationParameters.Teleport);
        }
            
        CreateTeleportEffect(transform.position);
            
        yield return new WaitForSeconds(0.3f); // Giảm thời gian teleport
        
        Vector2 newPosition = FindSafeTeleportPosition2D();
        
        transform.position = newPosition;
        rb2d.position = newPosition;
        
        CreateTeleportEffect(newPosition);
        
        yield return new WaitForSeconds(0.2f); // Giảm thời gian
        
        isSkillActive = false;
        
        Debug.Log("Agis Wizzar teleported to safety!");
    }

    void CreateTeleportEffect(Vector3 position)
    {
        if (teleportEffect != null)
        {
            // SỬ DỤNG 2D EFFECT MANAGER - PHƯƠNG PHÁP ĐÚNG
            Effect2DManager.CreateEffect2D(teleportEffect, position, Quaternion.identity, 1f, true);
        }
        else
        {
            // Fallback: Teleport effect đơn giản
            Effect2DManager.CreateFallbackEffect2D(position, new Color(0.5f, 0f, 1f, 0.8f), 1.5f, 1f);
        }
    }

    Vector2 FindSafeTeleportPosition2D()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 newPosition = (Vector2)transform.position + randomDirection * teleportRange;
            
            Collider2D hit = Physics2D.OverlapCircle(newPosition, 0.5f);
            if (hit == null || hit.gameObject == gameObject)
            {
                return newPosition;
            }
        }
        
        return transform.position;
    }

    IEnumerator FireballSkill()
    {
        isSkillActive = true;
        
        Debug.Log($"Agis Wizzar casting fireball! Berserk: {isBerserk}");
        
        if (isBerserk)
        {
            // Berserk mode - bắn nhiều fireball
            fireballCooldownTimer = berserkFireballCooldown;
            
            for (int i = 0; i < berserkFireballCount; i++)
            {
                Vector2 targetPos = (Vector2)playerTarget.position + Random.insideUnitCircle * 1f;
                LaunchFireball2D(targetPos);
                
                yield return new WaitForSeconds(0.15f); // Giảm delay giữa các fireball
            }
        }
        else
        {
            // Normal mode
            fireballCooldownTimer = fireballCooldown;
            LaunchFireball2D(playerTarget.position);
        }
        
        yield return new WaitForSeconds(0.2f);
        isSkillActive = false;
    }

    void LaunchFireball2D(Vector2 targetPosition)
    {
        Vector2 spawnPosition = (Vector2)transform.position + Vector2.up * 0.5f;
        
        // Create warning indicator at target position
        float distanceToTarget = Vector2.Distance(spawnPosition, targetPosition);
        float travelTime = distanceToTarget / fireballSpeed;
        float warningTime = Mathf.Min(fireballWarningTime, travelTime * 0.8f);
        
        if (warningTime > 0.2f) // Only show warning if there's enough time
        {
            Effect2DManager.CreateWarningIndicator2D(targetPosition, 1f, warningTime, fireballWarningColor);
        }
        
        GameObject fireball;
        
        if (fireballPrefab != null)
        {
            fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            fireball = CreateDefaultFireball(spawnPosition);
        }
        
        FireballProjectile2D projectile = fireball.GetComponent<FireballProjectile2D>();
        if (projectile == null)
        {
            projectile = fireball.AddComponent<FireballProjectile2D>();
        }
        
        projectile.Initialize(targetPosition, fireballSpeed, fireballDamage, fireballImpactSound);
        
        PlaySound(fireballCastSound);
        
        if (animator != null)
            animator.SetTrigger(AnimationParameters.CastFireball);
            
        Debug.Log($"🔥 Fireball launched towards {targetPosition} with {warningTime:F1}s warning!");
    }

    GameObject CreateDefaultFireball(Vector2 position)
    {
        GameObject fireball = new GameObject("DefaultFireball");
        fireball.transform.position = position;
        
        // Sử dụng SpriteRenderer thay vì tạo texture phức tạp
        var spriteRenderer = fireball.AddComponent<SpriteRenderer>();
        
        // Tạo sprite đơn giản cho fireball
        Texture2D texture = CreateSimpleFireballTexture();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 10;
        
        var collider = fireball.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;
        
        var rb = fireball.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        
        return fireball;
    }

    /// <summary>
    /// Tạo texture đơn giản cho fireball fallback
    /// </summary>
    Texture2D CreateSimpleFireballTexture()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        Vector2 center = Vector2.one * (size * 0.5f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float intensity = 1f - (distance / radius);
                    colors[y * size + x] = new Color(1f, intensity * 0.3f, 0f, intensity);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    IEnumerator AOESkill()
    {
        isSkillActive = true;
        aoeCooldownTimer = aoeCooldown;
        
        Debug.Log("🔥 Agis Wizzar casting AOE! Players have time to dodge!");
        
        PlaySound(aoeCastSound);
        
        if (animator != null)
            animator.SetTrigger(AnimationParameters.AOECast);
        
        // Predict player position but give them time to react
        Vector2 aoePosition = PredictPlayerPosition();
        
        // Create warning indicator first
        GameObject warningIndicator = Effect2DManager.CreateWarningIndicator2D(
            aoePosition, aoeRadius, aoeChargeTime, aoeWarningColor);
        
        // Create charge effect
        GameObject chargeEffect = CreateAOEChargeEffect(aoePosition);
        
        // Wait for charge time - this gives players time to dodge
        yield return new WaitForSeconds(aoeChargeTime);
        
        // Deal damage only if player is still in area
        DealAOEDamage2D(aoePosition);
        
        // Create explosion effect
        CreateAOEExplosionEffect(aoePosition);
        
        yield return new WaitForSeconds(0.5f);
        isSkillActive = false;
        
        Debug.Log("✨ AOE attack completed!");
    }

    /// <summary>
    /// Predict player position with some randomness to make it challenging but fair
    /// </summary>
    private Vector2 PredictPlayerPosition()
    {
        if (playerTarget == null) return transform.position;
        
        Vector2 playerPos = playerTarget.position;
        
        // Add slight prediction based on player movement but keep it fair
        Rigidbody2D playerRb = playerTarget.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.linearVelocity.magnitude > 0.1f)
        {
            // Predict 0.5 seconds ahead but add some randomness
            Vector2 prediction = playerRb.linearVelocity * 0.5f;
            playerPos += prediction + Random.insideUnitCircle * 0.5f;
        }
        
        return playerPos;
    }

    /// <summary>
    /// Create explosion effect at AOE location
    /// </summary>
    private void CreateAOEExplosionEffect(Vector2 position)
    {
        // Create main explosion effect
        Effect2DManager.CreateFallbackEffect2D(position, Color.red, aoeRadius * 1.5f, 1f);
        
        // Create secondary ring effect
        Effect2DManager.CreateFallbackEffect2D(position, Color.orange, aoeRadius * 2f, 0.8f);
    }

    GameObject CreateAOEChargeEffect(Vector2 position)
    {
        if (aoePrefab != null)
        {
            // SỬ DỤNG 2D EFFECT MANAGER cho AOE prefab
            GameObject effect = Effect2DManager.CreateEffect2D(aoePrefab, position, Quaternion.identity, 0.1f, false);
            
            // Scale effect từ từ
            if (effect != null)
            {
                StartCoroutine(ScaleOverTime(effect, Vector3.one * aoeRadius * 2, aoeChargeTime));
                // Destroy sau khi charge time + thêm 1s
                Destroy(effect, aoeChargeTime + 1f);
            }
            
            return effect;
        }
        else
        {
            // Fallback: AOE charge effect đơn giản
            return Effect2DManager.CreateFallbackEffect2D(position, Color.red, aoeRadius, aoeChargeTime + 1f);
        }
    }

    void DealAOEDamage2D(Vector2 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, aoeRadius);
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                float distance = Vector2.Distance(center, hit.transform.position);
                float damageMultiplier = 1f - (distance / aoeRadius * 0.5f);
                float finalDamage = aoeDamage * damageMultiplier;
                
                bool damageDealt = false;
                
                // Method 1: IDamageable interface
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(finalDamage);
                    damageDealt = true;
                    Debug.Log($"💥 AOE hit {hit.name} for {finalDamage:F1} damage via IDamageable!");
                }
                
                // Method 2: Character component
                Character character = hit.GetComponent<Character>();
                if (character != null && !damageDealt)
                {
                    character.TakeDamage(finalDamage);
                    damageDealt = true;
                    Debug.Log($"💥 AOE hit {hit.name} for {finalDamage:F1} damage via Character!");
                }
                
                // Method 3: Removed - Health component doesn't exist in codebase
                
                if (!damageDealt)
                {
                    Debug.LogWarning($"⚠️ AOE hit {hit.name} but could not deal damage! Missing damage interface.");
                }
                
                // Create impact effect on player
                Effect2DManager.CreateFallbackEffect2D(hit.transform.position, Color.yellow, 0.8f, 0.5f);
            }
        }
    }

    IEnumerator ScaleOverTime(GameObject obj, Vector3 targetScale, float duration)
    {
        if (obj == null) yield break;
        
        Vector3 startScale = obj.transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }
        
        if (obj != null)
            obj.transform.localScale = targetScale;
    }

    void FaceTarget2D(Vector2 targetPosition)
    {
        float direction = targetPosition.x - transform.position.x;
        
        if (Mathf.Abs(direction) > 0.1f)
        {
            bool shouldFaceRight = direction > 0;
            
            if (shouldFaceRight != facingRight)
            {
                Flip2D();
            }
        }
    }

    void Flip2D()
    {
        facingRight = !facingRight;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        
        yield return new WaitForSeconds(0.1f);
        
        spriteRenderer.color = originalColor;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    /// <summary>
    /// Kích hoạt chế độ cuồng bạo với hiệu ứng phóng to
    /// </summary>
    IEnumerator EnterBerserkMode()
    {
        hasBerserkTriggered = true;
        isBerserk = true;
        isSkillActive = true; // Tạm dừng các skill khác
        
        Debug.Log("🔥 AGIS WIZZAR ENTERS BERSERK MODE! 🔥");
        
        // Animation trigger
        if (animator != null)
        {
            animator.SetBool(AnimationParameters.IsBerserk, true);
            animator.SetTrigger(AnimationParameters.EnterBerserk);
        }
        
        // Hiệu ứng âm thanh và visual
        StartCoroutine(BerserkTransformEffects());
        
        // Phóng to boss từ từ
        Vector3 targetScale = originalScale * berserkSizeMultiplier;
        yield return StartCoroutine(ScaleOverTime(gameObject, targetScale, berserkTransformTime));
        
        // Tăng tốc độ di chuyển
        moveSpeed = originalMoveSpeed * berserkSpeedMultiplier;
        
        // Bắt đầu hiệu ứng berserk liên tục
        StartCoroutine(BerserkVisualEffect());
        
        // Giảm tất cả cooldowns
        teleportCooldownTimer = Mathf.Min(teleportCooldownTimer, 0.5f);
        fireballCooldownTimer = 0f;
        aoeCooldownTimer = Mathf.Min(aoeCooldownTimer, 2f);
        
        yield return new WaitForSeconds(0.5f);
        isSkillActive = false;
        
        Debug.Log($"Berserk transformation complete! New scale: {transform.localScale}, Speed: {moveSpeed}");
    }
    
    /// <summary>
    /// Hiệu ứng chuyển đổi sang berserk mode
    /// </summary>
    IEnumerator BerserkTransformEffects()
    {
        // Tạo nhiều hiệu ứng berserk
        for (int i = 0; i < 3; i++)
        {
            CreateBerserkTransformEffect(transform.position + Random.insideUnitSphere * 2f);
            yield return new WaitForSeconds(0.2f);
        }
        

    }
    
    /// <summary>
    /// Tạo hiệu ứng transform cho berserk mode - SỬ DỤNG 2D
    /// </summary>
    void CreateBerserkTransformEffect(Vector3 position)
    {
        // Nếu có prefab cho berserk effect, sử dụng nó
        if (teleportEffect != null) // Tạm dùng teleportEffect làm berserk effect
        {
            Effect2DManager.CreateEffect2D(teleportEffect, position, Quaternion.identity, 1.5f, true);
        }
        else
        {
            // Fallback: Berserk transform effect
            Effect2DManager.CreateFallbackEffect2D(position, Color.red, 2f, 1.5f);
        }
    }
    
    /// <summary>
    /// Hiệu ứng visual liên tục khi ở berserk mode
    /// </summary>
    IEnumerator BerserkVisualEffect()
    {
        while (isBerserk && !IsDead)
        {
            if (spriteRenderer != null)
            {
                // Nhấp nháy màu đỏ
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                
                spriteRenderer.color = new Color(1f, 0.3f, 0.3f); // Màu đỏ nhạt
                yield return new WaitForSeconds(0.1f);
                
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    /// <summary>
    /// Xử lý khi boss chết - được gọi từ Character system
    /// </summary>
    private void OnBossDeath()
    {
        Debug.Log("Agis Wizzar defeated through Character system!");
        
        if (animator != null)
            animator.SetTrigger(AnimationParameters.Death);
            
        if (rb2d != null)
            rb2d.linearVelocity = Vector2.zero;
            
        // Dừng tất cả coroutines
        StopAllCoroutines();
        
        // Disable movement và skills
        this.enabled = false;
        
        // Destroy sau một khoảng thời gian
        Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        
        if (playerTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }

    public bool IsBerserk()
    {
        return isBerserk;
    }

    public bool IsSkillActive()
    {
        return isSkillActive;
    }
    
    /// <summary>
    /// Trạng thái chi tiết của boss cho debugging
    /// </summary>
    public string GetBossStatus()
    {
        return $"HP: {GetHealthPercentage():P1} | Berserk: {isBerserk} | Scale: {transform.localScale.x:F2}x | Speed: {moveSpeed:F1}";
    }

    // Cleanup khi destroy
    void OnDestroy()
    {
        if (character != null)
        {
            character.OnDamageTaken -= OnDamageTaken;
            character.OnDeath -= OnCharacterDeath;
        }
    }
}

public class FireballProjectile2D : MonoBehaviour
{
    private Vector2 targetPosition;
    private float speed;
    private float damage;
    private AudioClip impactSound;
    private bool hasTarget = false;
    private Rigidbody2D rb2d;

    public void Initialize(Vector2 target, float projectileSpeed, float projectileDamage, AudioClip impact)
    {
        targetPosition = target;
        speed = projectileSpeed;
        damage = projectileDamage;
        impactSound = impact;
        hasTarget = true;
        
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
            rb2d = gameObject.AddComponent<Rigidbody2D>();
            
        rb2d.gravityScale = 0f;
        rb2d.freezeRotation = true;
        
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb2d.linearVelocity = direction * speed;
        
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if (hasTarget)
        {
            if (Vector2.Distance(transform.position, targetPosition) < 0.5f)
            {
                Explode();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Try multiple ways to deal damage to ensure it works
            bool damageDealt = false;
            
            // Method 1: IDamageable interface
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                damageDealt = true;
                Debug.Log($"🔥 Fireball hit {other.name} for {damage} damage via IDamageable!");
            }
            
            // Method 2: Character component
            Character character = other.GetComponent<Character>();
            if (character != null && !damageDealt)
            {
                character.TakeDamage(damage);
                damageDealt = true;
                Debug.Log($"🔥 Fireball hit {other.name} for {damage} damage via Character!");
            }
            
            // Method 3: Removed - Health component doesn't exist in codebase
            
            if (!damageDealt)
            {
                Debug.LogWarning($"⚠️ Fireball hit {other.name} but could not deal damage! Missing damage interface.");
            }
            
            Explode();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Explode();
        }
    }

    void Explode()
    {
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }
        
        CreateExplosionEffect();
        
        Destroy(gameObject);
    }
    
    void CreateExplosionEffect()
    {
        // SỬ DỤNG 2D EFFECT MANAGER thay vì tạo ParticleSystem
        Effect2DManager.CreateFallbackEffect2D(transform.position, Color.red, 1.2f, 1f);
    }
}