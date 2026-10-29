using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float aoeChargeTime = 1f;
    public AudioClip aoeCastSound;
    
    [Header("AI Settings")]
    public Transform playerTarget;
    public float detectionRange = 15f;
    public float attackRange = 6f;
    public float minDistance = 3f;
    
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

    private bool isDowned = false;
    private bool endingChosen = false;
    private GameObject endingPromptGo;

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
            
        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 5f;
        rb2d.freezeRotation = true;
        
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
        if (isDowned)
        {
            HandleEndingChoice();
            return;
        }

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
                animator.SetBool("IsMoving", true);
        }
        else
        {
            if (animator != null)
                animator.SetBool("IsMoving", false);
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
                // Máu rất thấp - teleport rất thường xuyên (80% chance)
                shouldTeleport = Random.Range(0f, 1f) < 0.8f;
            }
            else if (healthPercent <= teleportHealthThreshold)
            {
                // Máu thấp - teleport thường xuyên (50% chance)
                shouldTeleport = Random.Range(0f, 1f) < 0.5f;
            }
            
            if (shouldTeleport)
            {
                StartCoroutine(TeleportSkill());
                return;
            }
        }
        
        // Fireball skill với logic berserk
        if (fireballCooldownTimer <= 0 && distanceToPlayer <= fireballRange)
        {
            StartCoroutine(FireballSkill());
            return;
        }
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
        
        if (animator != null)
            animator.SetTrigger("Teleport");
            
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
            VfxService.PlayPrefab(teleportEffect, position, Quaternion.identity, 1f);
        }
        else
        {
            // Fallback: Teleport effect đơn giản
            VfxService.Impact(position, new Color(0.5f, 0f, 1f), 1.5f);
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
        
        // TELEGRAPH: Boss chớp vàng và hiện cảnh báo trước khi bắn 0.35s
        if (spriteRenderer != null) spriteRenderer.color = Color.yellow;
        if (playerTarget != null)
        {
            VfxService.Telegraph(playerTarget.position, 0.6f, 0.35f, new Color(1f, 0.8f, 0.2f, 0.8f));
        }
        yield return new WaitForSeconds(0.35f);
        if (spriteRenderer != null) spriteRenderer.color = isBerserk ? Color.red : Color.white;

        Debug.Log($"Agis Wizzar casting fireball! Berserk: {isBerserk}");
        
        if (isBerserk)
        {
            // Berserk mode - bắn nhiều fireball
            fireballCooldownTimer = berserkFireballCooldown;
            
            for (int i = 0; i < berserkFireballCount; i++)
            {
                if (playerTarget == null) break;
                Vector2 targetPos = (Vector2)playerTarget.position + Random.insideUnitCircle * 1f;
                LaunchFireball2D(targetPos);
                
                yield return new WaitForSeconds(0.15f); // Giảm delay giữa các fireball
            }
        }
        else
        {
            // Normal mode
            fireballCooldownTimer = fireballCooldown;
            if (playerTarget != null) LaunchFireball2D(playerTarget.position);
        }
        
        yield return new WaitForSeconds(0.2f);
        isSkillActive = false;
    }

    void LaunchFireball2D(Vector2 targetPosition)
    {
        Vector2 spawnPosition = (Vector2)transform.position + Vector2.up * 0.5f;
        
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
            animator.SetTrigger("CastFireball");
            
        Debug.Log($"Fireball launched towards {targetPosition}!");
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
        
        Debug.Log("Agis Wizzar casting AOE!");
        
        PlaySound(aoeCastSound);
        
        if (animator != null)
            animator.SetTrigger("AOECast");
        
        Vector2 aoePosition = playerTarget.position;
        GameObject chargeEffect = CreateAOEChargeEffect(aoePosition);
            
        yield return new WaitForSeconds(aoeChargeTime);
        
        DealAOEDamage2D(aoePosition);
        
        yield return new WaitForSeconds(0.5f);
        isSkillActive = false;
        
        Debug.Log("Agis Wizzar used AOE attack!");
    }

    GameObject CreateAOEChargeEffect(Vector2 position)
    {
        if (aoePrefab != null)
        {
            GameObject effect = VfxService.PlayPrefab(aoePrefab, position, Quaternion.identity, 0.1f, 0f);
            
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
            return VfxService.Telegraph(position, aoeRadius, aoeChargeTime + 1f, new Color(1f, 0.15f, 0.15f, 0.8f));
        }
    }

    void DealAOEDamage2D(Vector2 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, aoeRadius);
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float distance = Vector2.Distance(center, hit.transform.position);
                    float damageMultiplier = 1f - (distance / aoeRadius * 0.5f);
                    float finalDamage = aoeDamage * damageMultiplier;
                    
                    damageable.TakeDamage(finalDamage);
                    
                    Debug.Log($"AOE hit {hit.name} for {finalDamage} damage!");
                }
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
            animator.SetBool("IsBerserk", true);
            animator.SetTrigger("EnterBerserk");
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
        
        // Screen shake effect (nếu có camera shake system)
        // CameraShaker.Instance?.ShakeOnce(0.5f, 4f, 0.1f, 1f);
    }
    
    /// <summary>
    /// Tạo hiệu ứng transform cho berserk mode - SỬ DỤNG 2D
    /// </summary>
    void CreateBerserkTransformEffect(Vector3 position)
    {
        // Nếu có prefab cho berserk effect, sử dụng nó
        if (teleportEffect != null) // Tạm dùng teleportEffect làm berserk effect
        {
            VfxService.PlayPrefab(teleportEffect, position, Quaternion.identity, 1.5f);
        }
        else
        {
            // Fallback: Berserk transform effect
            VfxService.Impact(position, Color.red, 2f);
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
            animator.SetTrigger("Death");
            
        if (rb2d != null)
            rb2d.linearVelocity = Vector2.zero;
            
        // Dừng tất cả coroutines chiến đấu
        StopAllCoroutines();

        // Báo cáo tiến trình quest Q4
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportProgress("q4", "q4_agis");
        }

        // Tác động rung chấn màn hình hoành tráng khi boss gục
        VfxService.Shake(ShakeLevel.Boss);
        VfxService.Impact(transform.position, Color.yellow, 2.5f);

        // Thưởng cuộn bí kíp Hư Không cho người chơi
        if (InventoryManager.Instance != null)
        {
            var scroll = Resources.Load<ItemData>("Scroll_ShadowTeleport");
            if (scroll != null) InventoryManager.Instance.AddItem(scroll, 1);
        }
        
        isDowned = true;
        ShowEndingChoicePrompt();
    }

    private void ShowEndingChoicePrompt()
    {
        if (endingPromptGo != null) return;
        endingPromptGo = new GameObject("EndingChoicePrompt");
        endingPromptGo.transform.SetParent(transform, false);
        endingPromptGo.transform.localPosition = new Vector3(0, 1.8f, 0);

        var tm = endingPromptGo.AddComponent<TextMesh>();
        tm.text = "[F] Tha thứ   |   [J] Kết liễu";
        tm.characterSize = 0.22f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.9f, 0.3f, 1f);
    }

    private void HandleEndingChoice()
    {
        if (endingChosen) return;

        Transform pc = playerTarget;
        if (pc == null)
        {
            var playerCtrl = FindFirstObjectByType<PlayerController>();
            if (playerCtrl != null) pc = playerCtrl.transform;
        }

        if (pc != null && Vector2.Distance(transform.position, pc.position) <= 4.0f)
        {
            if (endingPromptGo != null) endingPromptGo.SetActive(true);

            if (Input.GetKeyDown(KeyCode.F))
            {
                endingChosen = true;
                if (endingPromptGo != null) Destroy(endingPromptGo);
                if (DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.StartDialogue(DialogueDatabase.AgisEndingForgive);
                }
                Debug.Log("🕊️ Người chơi chọn [THA THỨ] Agis - Đêm Trăng Đỏ khép lại trong ánh bình minh.");
                Destroy(gameObject, 5f);
            }
            else if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            {
                endingChosen = true;
                if (endingPromptGo != null) Destroy(endingPromptGo);
                if (DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.StartDialogue(DialogueDatabase.AgisEndingDestroy);
                }
                VfxService.Shake(ShakeLevel.Boss);
                VfxService.Impact(transform.position, Color.red, 3.0f);
                Debug.Log("⚔️ Người chơi chọn [KẾT LIỄU] Agis - Ác mộng Trăng Đỏ bị chém vỡ thành tro bụi.");
                Destroy(gameObject, 3f);
            }
        }
        else
        {
            if (endingPromptGo != null) endingPromptGo.SetActive(false);
        }
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
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Debug.Log($"Fireball hit {other.name} for {damage} damage!");
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
        VfxService.Impact(transform.position, Color.red, 1.2f);
    }
}