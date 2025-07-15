using UnityEngine;
using System.Collections;

/// <summary>
/// ENHANCED SKILL EXECUTORS v?i Support skill type và improved damage zones
/// </summary>

// Base class cho t?t c? skill executors
public abstract class SkillExecutorBase : ISkillExecutor
{
    public SkillModule Module { get; protected set; }

    public SkillExecutorBase(SkillModule module)
    {
        Module = module;
    }

    public abstract void Execute(Character user, Vector2 targetPosition);
    
    // Implement ISkillExecutor interface
    public virtual bool CanExecute(Character user)
    {
        return Module.CanExecute(user);
    }
    
    public virtual float GetCooldown()
    {
        return Module.cooldown;
    }
    
    public virtual float GetManaCost()
    {
        return Module.manaCost;
    }
    
    /// <summary>
    /// FIXED: Always use "Attack" animation for compatibility
    /// </summary>
    protected void TriggerAnimation(Character user)
    {
        var animator = user.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Attack"); // ALWAYS use "Attack" trigger
            Debug.Log($"?? Triggered Attack animation for {Module.skillName}");
        }
        
        // Also trigger PlayerController animation if available
        var playerController = user.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TriggerSkillAnimation(Module.skillName);
        }
    }
    
    protected void PlayCastSound(Character user)
    {
        if (Module.castSound != null)
        {
            var audioSource = user.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(Module.castSound);
            }
        }
    }
    
    protected void PlayImpactSound(Character user)
    {
        if (Module.impactSound != null)
        {
            var audioSource = user.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(Module.impactSound);
            }
        }
    }
    
    protected void CreateVisualEffect(Vector3 position)
    {
        if (Module.effectPrefab != null)
        {
            Object.Instantiate(Module.effectPrefab, position, Quaternion.identity);
        }
    }
    
    /// <summary>
    /// Enhanced enemy detection system
    /// </summary>
    protected Character[] FindEnemiesInRange(Vector2 center, float range)
    {
        var enemies = new System.Collections.Generic.List<Character>();
        
        Debug.Log($"?? ENEMY SEARCH: Center={center}, Range={range}");
        
        // Find ALL Characters in scene and filter properly
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        Debug.Log($"?? Found {allCharacters.Length} total Characters in scene");
        
        foreach (var character in allCharacters)
        {
            if (character == null) continue;
            
            // Skip if it's a player (has PlayerController)
            if (character.GetComponent<PlayerController>() != null)
            {
                Debug.Log($"?? Skipping {character.name} (is player)");
                continue;
            }
            
            // Calculate distance
            float distance = Vector2.Distance(center, character.transform.position);
            Debug.Log($"?? Distance to {character.name}: {distance:F2} (range: {range})");
            
            if (distance <= range)
            {
                // Check if enemy is alive
                if (character.health != null && character.health.currentValue > 0)
                {
                    enemies.Add(character);
                    Debug.Log($"? Added {character.name} to enemy list (distance: {distance:F2})");
                }
                else
                {
                    Debug.Log($"?? Skipping {character.name} (dead or no health)");
                }
            }
            else
            {
                Debug.Log($"? {character.name} out of range (distance: {distance:F2} > {range})");
            }
        }
        
        Debug.Log($"?? Final enemy count in range: {enemies.Count}");
        return enemies.ToArray();
    }
    
    /// <summary>
    /// Get raw mouse position without targeting system interference
    /// </summary>
    protected Vector2 GetRawMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // Camera distance
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 rawMousePos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        Debug.Log($"??? RAW MOUSE POSITION: Screen={Input.mousePosition}, World={rawMousePos}");
        return rawMousePos;
    }
    
    /// <summary>
    /// Get valid target position with range validation
    /// </summary>
    protected Vector2 GetValidTargetPosition(Vector2 mouseTargetPosition, Character user)
    {
        Vector2 userPos = user.transform.position;
        Vector2 direction = (mouseTargetPosition - userPos).normalized;
        float maxDistance = Module.range;
        
        // For area skills, use EXACT mouse position (clamped to range)
        if (Module.skillType == SkillType.Area)
        {
            float distanceToMouse = Vector2.Distance(userPos, mouseTargetPosition);
            if (distanceToMouse <= maxDistance)
            {
                Debug.Log($"?? AREA SKILL: Using exact mouse position {mouseTargetPosition}");
                return mouseTargetPosition;
            }
            else
            {
                Vector2 clampedPos = userPos + direction * maxDistance;
                Debug.Log($"?? AREA SKILL: Mouse too far, clamped to {clampedPos}");
                return clampedPos;
            }
        }
        
        // For other skills, normal range validation
        Vector2 validTarget = userPos + direction * Mathf.Min(Vector2.Distance(userPos, mouseTargetPosition), maxDistance);
        Debug.Log($"? TARGET VALIDATION: Mouse={mouseTargetPosition}, User={userPos}, Valid={validTarget}");
        return validTarget;
    }
    
    /// <summary>
    /// Enhanced damage area visualization v?i auto-generation
    /// </summary>
    protected void ShowDamageAreaAtExactPosition(Vector2 exactPosition, float radius, string indicatorName = "DamageAreaIndicator")
    {
        Debug.Log($"?? CREATING DAMAGE AREA AT EXACT POSITION: {exactPosition}");
        Debug.Log($"?? Radius: {radius}, Name: {indicatorName}");
        
        // Check for Enhanced Skill System Manager
        var enhancedManager = Object.FindFirstObjectByType<EnhancedSkillSystemManager>();
        if (enhancedManager != null)
        {
            // Let the manager handle damage zone creation
            Debug.Log($"?? Using EnhancedSkillSystemManager for damage zone creation");
            return;
        }
        
        // Fallback: Create basic damage zone
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = $"{indicatorName}_EXACT_{Time.time:F2}";
        
        // Set exact position
        indicator.transform.position = new Vector3(exactPosition.x, exactPosition.y, 0);
        indicator.transform.localScale = Vector3.one * radius * 2;
        
        Debug.Log($"? Indicator created: Position={indicator.transform.position}, Scale={indicator.transform.localScale}");
        
        // Make it transparent
        var renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = Module.damageAreaColor;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }
        
        // Remove collider
        var collider = indicator.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
        
        // Auto destroy after display time
        Object.Destroy(indicator, Module.damageAreaDisplayTime);
        
        Debug.Log($"? Damage area will be destroyed in {Module.damageAreaDisplayTime} seconds");
    }
}

// 1. MELEE SKILL EXECUTOR - C?n chi?n v?i collider t? ??ng
public class MeleeSkillExecutor : SkillExecutorBase
{
    public MeleeSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        Debug.Log($"?? EXECUTING MELEE SKILL: {Module.skillName} with range {Module.range}");
        Debug.Log($"?? User position: {user.transform.position}");

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger animation
        TriggerAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Start damage dealing coroutine with delay
        user.StartCoroutine(DealMeleeDamageAfterDelay(user, 0.3f));
    }

    private IEnumerator DealMeleeDamageAfterDelay(Character user, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Attack ALWAYS centered on USER position
        Vector2 attackCenter = user.transform.position;
        Debug.Log($"?? MELEE ATTACK CENTER: {attackCenter}");
        
        var enemies = FindEnemiesInRange(attackCenter, Module.range);
        
        Debug.Log($"?? Melee attack from {user.name} found {enemies.Length} enemies within range {Module.range}");
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            // Calculate damage with potential critical
            float finalDamage = Module.damage;
            bool isCritical = Random.Range(0f, 1f) < Module.criticalChance;
            if (isCritical)
            {
                finalDamage *= Module.criticalMultiplier;
                Debug.Log($"?? Critical hit on {enemy.name}! {finalDamage} damage");
            }
            
            enemy.TakeDamage(finalDamage, isCritical);
            Debug.Log($"?? Dealt {finalDamage} damage to {enemy.name}");
            
            // Apply knockback if specified
            if (Module.knockbackForce > 0)
            {
                Vector2 knockbackDirection = (enemy.transform.position - user.transform.position).normalized;
                enemy.ApplyKnockback(Module.knockbackForce, knockbackDirection);
            }
            
            // Apply stun if specified
            if (Module.stunDuration > 0)
            {
                enemy.ApplyStun(Module.stunDuration);
            }
        }
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Create visual effect
        CreateVisualEffect(user.transform.position);
        
        // Show damage area ALWAYS at user position for melee
        if (Module.showDamageArea)
        {
            ShowDamageAreaAtExactPosition(attackCenter, Module.range, "MeleeDamageArea_UserCentered");
        }
    }
}

// 2. PROJECTILE SKILL EXECUTOR - Phóng chiêu v?i range indicator
public class ProjectileSkillExecutor : SkillExecutorBase
{
    public ProjectileSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        Debug.Log($"?? EXECUTING PROJECTILE SKILL: {Module.skillName}");
        Debug.Log($"?? User position: {user.transform.position}");
        Debug.Log($"?? Target: {targetPosition}");

        // Use RAW mouse position for projectiles
        Vector2 rawMousePos = GetRawMouseWorldPosition();
        Vector2 validTarget = GetValidTargetPosition(rawMousePos, user);

        Debug.Log($"??? Projectile targeting RAW mouse: {rawMousePos}");
        Debug.Log($"? Projectile final target: {validTarget}");

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger animation
        TriggerAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Start projectile creation with delay
        user.StartCoroutine(CreateProjectileAfterDelay(user, validTarget, 0.2f));
    }

    private IEnumerator CreateProjectileAfterDelay(Character user, Vector2 targetPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"?? Creating projectile from {user.transform.position} to {targetPosition}");
        
        // Create projectile
        GameObject projectile = null;
        if (Module.projectilePrefab != null)
        {
            projectile = Object.Instantiate(Module.projectilePrefab, user.transform.position, Quaternion.identity);
        }
        else
        {
            // Create default projectile
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = $"Projectile_{Module.skillName}";
            projectile.transform.position = user.transform.position;
            projectile.transform.localScale = Vector3.one * 0.3f;
            
            // Style the projectile
            var renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Module.skillColor;
            }
            
            // Remove collider, we'll handle collision manually
            var collider = projectile.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        // Add improved projectile behavior
        var projectileBehavior = projectile.AddComponent<EnhancedProjectileBehavior>();
        projectileBehavior.Initialize(Module, user, targetPosition);
    }
}

// 3. AREA SKILL EXECUTOR - AoE v?i vùng sát th??ng chính xác
public class AreaSkillExecutor : SkillExecutorBase
{
    public AreaSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        Debug.Log($"?? EXECUTING AREA SKILL: {Module.skillName}");
        Debug.Log($"?? User position: {user.transform.position}");
        Debug.Log($"?? Target position: {targetPosition}");

        // Force use RAW mouse position
        Vector2 rawMousePos = GetRawMouseWorldPosition();
        Vector2 validTarget = GetValidTargetPosition(rawMousePos, user);
        
        Debug.Log($"??? Using RAW mouse position: {rawMousePos}");
        Debug.Log($"? Final validated target: {validTarget}");

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger animation
        TriggerAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Start area damage after delay
        user.StartCoroutine(AreaDamageAfterDelay(user, validTarget, 0.5f));
    }

    private IEnumerator AreaDamageAfterDelay(Character user, Vector2 targetPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"?? AREA ATTACK AT EXACT TARGET: {targetPosition} (radius: {Module.areaRadius})");
        
        // Area damage EXACTLY at target position (mouse click)
        var enemies = FindEnemiesInRange(targetPosition, Module.areaRadius);
        Debug.Log($"?? Area attack hit {enemies.Length} enemies at {targetPosition} with radius {Module.areaRadius}");
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            float finalDamage = Module.damage;
            bool isCritical = Random.Range(0f, 1f) < Module.criticalChance;
            if (isCritical)
            {
                finalDamage *= Module.criticalMultiplier;
            }
            
            enemy.TakeDamage(finalDamage, isCritical);
            Debug.Log($"?? Area damage: {finalDamage} to {enemy.name}");
            
            // Apply stun if specified
            if (Module.stunDuration > 0)
            {
                enemy.ApplyStun(Module.stunDuration);
            }
        }
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Create visual effect at target position
        CreateVisualEffect(targetPosition);
        
        // Damage area EXACTLY at mouse click position
        if (Module.showDamageArea)
        {
            ShowDamageAreaAtExactPosition(targetPosition, Module.areaRadius, "AreaDamageIndicator_MouseClick");
        }
    }
}

// 4. SUPPORT SKILL EXECUTOR - H? tr? không c?n v? vùng
public class SupportSkillExecutor : SkillExecutorBase
{
    public SupportSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        Debug.Log($"? EXECUTING SUPPORT SKILL: {Module.skillName}");

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger animation
        TriggerAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Start support effect after delay
        user.StartCoroutine(ApplySupportEffectAfterDelay(user, 0.3f));
    }

    private IEnumerator ApplySupportEffectAfterDelay(Character user, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"? Support effect applied to {user.name}");
        
        // Apply healing if specified
        if (Module.healAmount > 0)
        {
            user.Heal(Module.healAmount);
            Debug.Log($"?? Healed {user.name} for {Module.healAmount} health");
        }
        
        // Apply buff effects (you can extend this)
        // TODO: Add buff system integration here
        
        // Create visual effect at user position
        CreateVisualEffect(user.transform.position);
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Support skills don't show damage areas but can show special effects
        if (Module.showDamageArea)
        {
            CreateSupportVisualEffect(user.transform.position);
        }
    }
    
    /// <summary>
    /// T?o visual effect ??c bi?t cho Support skills
    /// </summary>
    private void CreateSupportVisualEffect(Vector2 position)
    {
        GameObject supportEffect = new GameObject($"SupportEffect_{Module.skillName}");
        supportEffect.transform.position = new Vector3(position.x, position.y, 0);
        
        // T?o particle system cho support effect
        var particleSystem = supportEffect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = Module.skillColor;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.maxParticles = 20;
        main.startSize = 0.5f;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 10f;
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1f;
        
        // Auto destroy after effect
        Object.Destroy(supportEffect, Module.damageAreaDisplayTime);
        
        Debug.Log($"? Created support visual effect at {position}");
    }
}

// Legacy Executors (for backward compatibility)
public class HealSkillExecutor : SupportSkillExecutor
{
    public HealSkillExecutor(SkillModule module) : base(module) { }
}

public class StunSkillExecutor : MeleeSkillExecutor
{
    public StunSkillExecutor(SkillModule module) : base(module) { }
}

public class BuffSkillExecutor : SupportSkillExecutor
{
    public BuffSkillExecutor(SkillModule module) : base(module) { }
}

// ENHANCED Projectile Behavior Component
public class EnhancedProjectileBehavior : MonoBehaviour
{
    private SkillModule skillModule;
    private Character caster;
    private Vector2 direction;
    private float travelTime = 0f;
    private float maxLifetime = 5f;
    private float hitRadius = 0.5f;
    private Vector2 startPosition;

    public void Initialize(SkillModule module, Character user, Vector2 targetPosition)
    {
        skillModule = module;
        caster = user;
        startPosition = user.transform.position;
        
        direction = (targetPosition - startPosition).normalized;
        
        // Calculate max lifetime based on range and speed
        maxLifetime = skillModule.range / Mathf.Max(skillModule.speed, 0.1f);
        
        // Set rotation to face direction
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        Debug.Log($"?? PROJECTILE INITIALIZED: {skillModule.skillName}");
        Debug.Log($"?? Start: {startPosition}, Target: {targetPosition}, Direction: {direction}");
        Debug.Log($"? Max lifetime: {maxLifetime}, Speed: {skillModule.speed}");
    }

    private void Update()
    {
        if (skillModule == null)
        {
            Debug.LogWarning("Projectile missing skill module, destroying");
            Destroy(gameObject);
            return;
        }

        // Move projectile
        float moveDistance = skillModule.speed * Time.deltaTime;
        transform.position += (Vector3)direction * moveDistance;
        travelTime += Time.deltaTime;

        // Enhanced collision detection
        var enemies = FindEnemiesInCollisionRange();
        if (enemies.Length > 0)
        {
            var targetEnemy = enemies[0]; // Hit first enemy found
            HitEnemy(targetEnemy);
            return;
        }

        // Check if projectile has exceeded its range
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= skillModule.range || travelTime >= maxLifetime)
        {
            Debug.Log($"?? Projectile {skillModule.skillName} exceeded range ({distanceTraveled:F2}/{skillModule.range}) or lifetime ({travelTime:F2}/{maxLifetime})");
            Destroy(gameObject);
        }
    }
    
    private Character[] FindEnemiesInCollisionRange()
    {
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        var enemiesInRange = new System.Collections.Generic.List<Character>();
        
        foreach (var character in allCharacters)
        {
            // Skip if it's the caster
            if (character == caster) continue;
            
            // Skip if it's a player (has PlayerController)
            if (character.GetComponent<PlayerController>() != null) continue;
            
            // Skip if dead
            if (character.health == null || character.health.currentValue <= 0) continue;
            
            float distance = Vector2.Distance(transform.position, character.transform.position);
            if (distance <= hitRadius)
            {
                enemiesInRange.Add(character);
                Debug.Log($"?? Projectile collision detected with {character.name} at distance {distance:F2}");
            }
        }
        
        return enemiesInRange.ToArray();
    }
    
    private void HitEnemy(Character enemy)
    {
        Debug.Log($"?? PROJECTILE HIT: {skillModule.skillName} hit {enemy.name}");
        
        // Calculate damage
        float finalDamage = skillModule.damage;
        bool isCritical = Random.Range(0f, 1f) < skillModule.criticalChance;
        if (isCritical)
        {
            finalDamage *= skillModule.criticalMultiplier;
            Debug.Log($"?? Projectile critical hit! {finalDamage} damage");
        }
        
        enemy.TakeDamage(finalDamage, isCritical);
        
        // Apply knockback
        if (skillModule.knockbackForce > 0)
        {
            enemy.ApplyKnockback(skillModule.knockbackForce, direction);
        }
        
        // Play impact sound
        if (skillModule.impactSound != null && caster != null)
        {
            var audioSource = caster.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(skillModule.impactSound);
            }
        }
        
        // Create impact effect
        if (skillModule.effectPrefab != null)
        {
            Instantiate(skillModule.effectPrefab, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // Draw projectile collision range for debugging
        if (skillModule != null)
        {
            Gizmos.color = skillModule.skillColor;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}