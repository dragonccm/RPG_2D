using UnityEngine;
using System.Collections;

/// <summary>
/// File: SkillExecutors.cs
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Enhanced skill execution system with proper player damage prevention
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
    /// Enhanced animation trigger using SkillModule.animationTrigger
    /// </summary>
    protected void TriggerAnimation(Character user)
    {
        var animator = user.GetComponent<Animator>();
        if (animator != null)
        {
            // S? d?ng animationTrigger t? Module thay vì hard-coded
            string trigger = !string.IsNullOrEmpty(Module.animationTrigger) ? 
                            Module.animationTrigger : "Attack";
            animator.SetTrigger(trigger);
        }
        
        // Also trigger PlayerController animation if available
        var playerController = user.GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
        {
            // Use reflection to call TriggerSkillAnimation method with animationTrigger
            var method = playerController.GetType().GetMethod("TriggerSkillAnimation");
            if (method != null)
            {
                string trigger = !string.IsNullOrEmpty(Module.animationTrigger) ? 
                                Module.animationTrigger : "Attack";
                method.Invoke(playerController, new object[] { Module.skillName, trigger });
            }
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
    
    /// <summary>
    /// Enhanced visual effect creation v?i auto-destroy và collision positioning
    /// </summary>
    protected void CreateVisualEffect(Vector3 position)
    {
        if (Module.effectPrefab != null)
        {
            // S? d?ng Enhanced Effect Manager ?? t?o effect v?i auto-destroy
            EnhancedEffectManager.CreateEffectAtPosition(
                Module.effectPrefab, 
                position, 
                Quaternion.identity, 
                null, 
                Module.damageAreaDisplayTime // Use skill's display time as lifetime
            );
        }
    }
    
    /// <summary>
    /// T?o effect va ch?m t?i v? trí chính xác v?i direction
    /// </summary>
    protected void CreateImpactEffect(Vector3 impactPosition, Vector3 impactDirection, GameObject target = null)
    {
        if (Module.effectPrefab != null)
        {
            // T?o effect t?i v? trí va ch?m chính xác
            EnhancedEffectManager.CreateImpactEffect(
                Module.effectPrefab,
                impactPosition,
                impactDirection,
                target,
                Module.damageAreaDisplayTime
            );
        }
    }
    
    /// <summary>
    /// T?o effect theo dõi target (cho projectile)
    /// </summary>
    protected void CreateFollowEffect(Transform target, Vector3 offset = default)
    {
        if (Module.effectPrefab != null && target != null)
        {
            EnhancedEffectManager.CreateFollowEffect(
                Module.effectPrefab,
                target,
                offset,
                Module.damageAreaDisplayTime
            );
        }
    }
    
    /// <summary>
    /// FIXED: Enhanced enemy detection with bulletproof player exclusion
    /// </summary>
    protected Character[] FindEnemiesInRange(Vector2 center, float range, Character caster = null)
    {
        var enemies = new System.Collections.Generic.List<Character>();
        
        // Find ALL Characters in scene and filter properly
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character == null) continue;
            
            // CRITICAL FIX: Multiple layers of player detection
            if (IsPlayerCharacter(character, caster)) continue;
            
            // Calculate distance
            float distance = Vector2.Distance(center, character.transform.position);
            
            if (distance <= range)
            {
                // Check if enemy is alive
                if (character.health != null && character.health.currentValue > 0)
                {
                    enemies.Add(character);
                }
            }
        }
        
        return enemies.ToArray();
    }
    
    /// <summary>
    /// BULLETPROOF player detection method
    /// </summary>
    protected bool IsPlayerCharacter(Character character, Character caster = null)
    {
        // Method 1: Same as caster
        if (caster != null && character == caster) return true;
        
        // Method 2: Has PlayerController component
        var playerController = character.GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
            return true;
        
        // Method 3: Check AttackableCharacter component
        var attackable = character.GetComponent<AttackableCharacter>();
        if (attackable != null && !attackable.CanBeAttacked())
            return true;
        
        // Method 4: Check GameObject name patterns
        string objName = character.gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("hero") || objName.Contains("character"))
            return true;
            
        // Method 5: Check tag
        if (character.gameObject.CompareTag("Player"))
            return true;
        
        return false;
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
                return mouseTargetPosition;
            }
            else
            {
                Vector2 clampedPos = userPos + direction * maxDistance;
                return clampedPos;
            }
        }
        
        // For other skills, normal range validation
        Vector2 validTarget = userPos + direction * Mathf.Min(Vector2.Distance(userPos, mouseTargetPosition), maxDistance);
        return validTarget;
    }
    
    /// <summary>
    /// Enhanced damage area visualization v?i auto-generation và custom prefab support
    /// </summary>
    private GameObject currentMeleeIndicator;

    protected void ShowDamageAreaAtExactPosition(Vector2 exactPosition, float radius, string indicatorName = "DamageAreaIndicator")
    {
        // H?y hi?u ?ng c? n?u còn t?n t?i
        if (currentMeleeIndicator != null)
        {
            Object.Destroy(currentMeleeIndicator);
            currentMeleeIndicator = null;
        }
        GameObject indicator = null;
        
        // ?u tiên s? d?ng custom prefab t? SkillModule
        if (Module.damageZonePrefab != null)
        {
            indicator = Object.Instantiate(Module.damageZonePrefab);
            indicator.name = $"{indicatorName}_Custom_{Time.time:F2}";
            
            // Set position và scale cho custom prefab
            indicator.transform.position = new Vector3(exactPosition.x, exactPosition.y, -0.1f); // Slightly in front for visibility
            
            // C? g?ng scale custom prefab theo radius
            if (radius > 0)
            {
                indicator.transform.localScale = Vector3.one * radius * 2;
            }
            
            // Add enhanced visual effects for custom prefabs
            AddEnhancedVisualEffects(indicator, Module.damageAreaColor);
        }
        else
        {
            // Fallback: Create basic damage zone with enhanced visuals
            indicator = CreateEnhancedDamageZoneIndicator(exactPosition, radius, indicatorName);
        }
        
        currentMeleeIndicator = indicator;
        
        // Lifetime ng?n h?n: ch? t?n t?i 0.2s
        if (indicator != null)
        {
            // Add fade-out effect before destruction
            StartFadeOutEffect(indicator, 0.15f);
            Object.Destroy(indicator, 0.2f);
        }
    }

    /// <summary>
    /// Create enhanced damage zone indicator with better visuals
    /// </summary>
    private GameObject CreateEnhancedDamageZoneIndicator(Vector2 position, float radius, string indicatorName)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = $"{indicatorName}_EXACT_{Time.time:F2}";
        
        // Set exact position with slight z-offset for visibility
        indicator.transform.position = new Vector3(position.x, position.y, -0.1f);
        indicator.transform.localScale = Vector3.one * radius * 2;
        
        // Enhanced material setup
        var renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = CreateEnhancedDamageAreaMaterial(Module.damageAreaColor);
            renderer.material = material;
        }
        
        // Remove collider to avoid interference
        var collider = indicator.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
        
        // Add pulsing effect
        var pulseEffect = indicator.AddComponent<DamageAreaPulseEffect>();
        pulseEffect.Initialize(Module.damageAreaColor, Module.damageAreaDisplayTime);
        
        return indicator;
    }

    /// <summary>
    /// Create enhanced material for damage area visualization
    /// </summary>
    private Material CreateEnhancedDamageAreaMaterial(Color baseColor)
    {
        var material = new Material(Shader.Find("Standard"));
        
        // Enhanced color with better alpha blending
        var enhancedColor = baseColor;
        enhancedColor.a = 0.4f; // More visible alpha
        material.color = enhancedColor;
        
        // Enhanced transparency settings
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        // Add emission for better visibility
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", baseColor * 0.3f);
        
        return material;
    }

    /// <summary>
    /// Add enhanced visual effects to custom prefabs
    /// </summary>
    private void AddEnhancedVisualEffects(GameObject indicator, Color effectColor)
    {
        // Try to find existing renderer and enhance it
        var renderer = indicator.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            var material = renderer.material;
            
            // Enhance existing material
            material.color = new Color(effectColor.r, effectColor.g, effectColor.b, 0.6f);
            
            // Add emission if the material supports it
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", effectColor * 0.2f);
                material.EnableKeyword("_EMISSION");
            }
        }
        
        // Add particle effect for extra visual flair
        AddDamageAreaParticleEffect(indicator, effectColor);
    }

    /// <summary>
    /// Add particle effect to damage area
    /// </summary>
    private void AddDamageAreaParticleEffect(GameObject indicator, Color effectColor)
    {
        var particleSystem = indicator.AddComponent<ParticleSystem>();
        
        var main = particleSystem.main;
        main.startColor = effectColor;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.5f;
        main.maxParticles = 15;
        main.startSize = 0.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 10f;
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(effectColor, 0.0f), new GradientColorKey(effectColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;
    }

    /// <summary>
    /// Start fade-out effect for damage area indicator
    /// </summary>
    private void StartFadeOutEffect(GameObject indicator, float delay)
    {
        var fadeEffect = indicator.AddComponent<FadeOutEffect>();
        fadeEffect.StartFadeOut(delay, 0.5f);
    }

    /// <summary>
    /// Calculate optimal execution delay based on animation length
    /// </summary>
    protected float CalculateOptimalExecutionDelay(float defaultDelay)
    {
        // Use animation length if available, otherwise use default
        if (Module.animationLength > 0)
        {
            // Use 60% of animation length for optimal timing
            return Module.animationLength * 0.6f;
        }
        return defaultDelay;
    }
    
    /// <summary>
    /// Calculate enhanced damage with critical hit system
    /// </summary>
    protected float CalculateEnhancedDamage(float baseDamage, float critChance, float critMultiplier)
    {
        bool isCritical = Random.Range(0f, 1f) < critChance;
        return isCritical ? baseDamage * critMultiplier : baseDamage;
    }
    
    /// <summary>
    /// Create individual hit effect for each enemy hit
    /// </summary>
    protected void CreateIndividualHitEffect(Vector3 position, bool isCritical)
    {
        GameObject hitEffect = new GameObject($"HitEffect_{Time.time:F2}");
        hitEffect.transform.position = position;
        
        // Create particle system for hit effect
        var particleSystem = hitEffect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = isCritical ? Color.yellow : Color.red;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.maxParticles = isCritical ? 15 : 8;
        main.startSize = isCritical ? 0.3f : 0.2f;
        
        var emission = particleSystem.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, isCritical ? 10 : 5),
            new ParticleSystem.Burst(0.1f, isCritical ? 5 : 3)
        });
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        // Auto destroy
        Object.Destroy(hitEffect, 1f);
    }
    
    public virtual void ShowDamageArea(Vector2 position)
    {
        // Default implementation: No action
    }

    public virtual void UpdateDamageArea(Vector2 position)
    {
        // Default implementation: No action
    }

    public virtual void HideDamageArea()
    {
        // Default implementation: No action
    }

    protected Character[] FindSkillTargetsInRange(Vector2 center, float range, Character caster)
    {
        var targets = new System.Collections.Generic.List<Character>();
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in allCharacters)
        {
            if (character == null || character == caster) continue;
            // N?u caster là player thì ch? t?n công enemy
            if (caster.gameObject.CompareTag("Player"))
            {
                if (!character.gameObject.CompareTag("Enemy")) continue;
            }
            // N?u caster là enemy thì ch? t?n công player
            else if (caster.gameObject.CompareTag("Enemy"))
            {
                if (!character.gameObject.CompareTag("Player")) continue;
            }
            // Ki?m tra máu
            if (character.health != null && character.health.currentValue > 0)
            {
                float distance = Vector2.Distance(center, character.transform.position);
                if (distance <= range)
                {
                    targets.Add(character);
                }
            }
        }
        return targets.ToArray();
    }
}

// 1. MELEE SKILL EXECUTOR - C?n chi?n v?i collider t? ??ng
public class MeleeSkillExecutor : SkillExecutorBase
{
    public MeleeSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

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
        // Enhanced timing system - use animation length if available
        float actualDelay = CalculateOptimalExecutionDelay(delay);
        yield return new WaitForSeconds(actualDelay);
        
        // Attack ALWAYS centered on USER position
        Vector2 attackCenter = user.transform.position;
        
        // FIXED: Pass caster parameter to prevent self-damage
        var enemies = FindSkillTargetsInRange(attackCenter, Module.range, user);
        
        // Enhanced feedback for no targets found
        if (enemies.Length == 0)
        {
            Debug.Log($"?? Melee skill '{Module.skillName}' found no valid targets in range {Module.range}");
            
            // Still show damage area for visual feedback
            if (Module.showDamageArea)
            {
                ShowDamageAreaAtExactPosition(attackCenter, Module.range, "MeleeDamageArea_NoTargets");
            }
            
            // Play impact sound and create effect even without targets
            PlayImpactSound(user);
            CreateVisualEffect(user.transform.position); // Enhanced effect creation
            yield break;
        }
        
        Debug.Log($"?? Melee skill '{Module.skillName}' hitting {enemies.Length} enemies");
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            // Calculate damage with enhanced critical system
            float finalDamage = CalculateEnhancedDamage(Module.damage, Module.criticalChance, Module.criticalMultiplier);
            bool isCritical = Random.Range(0f, 1f) < Module.criticalChance;
            
            // Apply damage with enhanced feedback
            enemy.TakeDamage(finalDamage, isCritical);
            
            // Enhanced knockback with position-based direction
            if (Module.knockbackForce > 0)
            {
                Vector2 knockbackDirection = (enemy.transform.position - user.transform.position).normalized;
                if (knockbackDirection.magnitude < 0.1f) // Prevent zero-direction knockback
                {
                    knockbackDirection = Random.insideUnitCircle.normalized;
                }
                enemy.ApplyKnockback(Module.knockbackForce, knockbackDirection);
            }
            
            // Apply stun if specified
            if (Module.stunDuration > 0)
            {
                enemy.ApplyStun(Module.stunDuration);
            }
            
            // Create individual impact effects for each enemy v?i v? trí chính xác
            Vector3 impactDirection = (enemy.transform.position - user.transform.position).normalized;
            CreateImpactEffect(enemy.transform.position, impactDirection, enemy.gameObject);
            CreateIndividualHitEffect(enemy.transform.position, isCritical);
        }
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Create main visual effect t?i v? trí user
        CreateVisualEffect(user.transform.position);
        
        // Show enhanced damage area ALWAYS at user position for melee
        if (Module.showDamageArea)
        {
            ShowDamageAreaAtExactPosition(attackCenter, Module.range, "MeleeDamageArea_UserCentered");
        }
    }

    private float CalculateOptimalExecutionDelay(float defaultDelay)
    {
        // Placeholder for enhanced timing logic
        return defaultDelay;
    }

    private float CalculateEnhancedDamage(float baseDamage, float criticalChance, float criticalMultiplier)
    {
        bool isCritical = Random.Range(0f, 1f) < criticalChance;
        return isCritical ? baseDamage * criticalMultiplier : baseDamage;
    }

    private void CreateIndividualHitEffect(Vector3 position, bool isCritical)
    {
        // Placeholder for creating individual hit effects
    }

    public override void ShowDamageArea(Vector2 position)
    {
        // Không t?o damage area trong ShowDamageArea ?? tránh trùng l?p
        // Damage area s? ???c t?o trong Execute() t?i v? trí player
    }

    public override void UpdateDamageArea(Vector2 position)
    {
        // Update logic for damage area if needed
    }

    public override void HideDamageArea()
    {
        // Logic to hide the damage area
    }
}

// 2. PROJECTILE SKILL EXECUTOR - Phóng chiêu v?i range indicator
public class ProjectileSkillExecutor : SkillExecutorBase
{
    public ProjectileSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        // Use RAW mouse position for projectiles
        Vector2 rawMousePos = GetRawMouseWorldPosition();
        Vector2 validTarget = GetValidTargetPosition(rawMousePos, user);

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

    public override void ShowDamageArea(Vector2 position)
    {
        // Projectile không hi?n th? damage area t?i v? trí b?t ??u
        // Thay vào ?ó có th? hi?n th? trajectory ho?c không hi?n th? gì
    }

    public override void UpdateDamageArea(Vector2 position)
    {
        // Projectile có th? c?p nh?t trajectory preview
    }

    public override void HideDamageArea()
    {
        // ?n trajectory preview n?u có
    }
}

// 3. AREA SKILL EXECUTOR - AoE v?i vùng sát th??ng chính xác
public class AreaSkillExecutor : SkillExecutorBase
{
    public AreaSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        // Force use RAW mouse position
        Vector2 rawMousePos = GetRawMouseWorldPosition();
        Vector2 validTarget = GetValidTargetPosition(rawMousePos, user);

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
        
        // Area damage EXACTLY at target position (mouse click)
        // FIXED: Pass caster parameter to prevent self-damage
        var enemies = FindSkillTargetsInRange(targetPosition, Module.areaRadius, user);
        
        // Enhanced feedback for area skills
        if (enemies.Length == 0)
        {
            Debug.Log($"?? Area skill '{Module.skillName}' found no targets at {targetPosition}");
        }
        else
        {
            Debug.Log($"?? Area skill '{Module.skillName}' hitting {enemies.Length} enemies in area");
        }
        
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
            
            // Apply stun if specified
            if (Module.stunDuration > 0)
            {
                enemy.ApplyStun(Module.stunDuration);
            }
            
            // Create individual impact effects t?i v? trí t?ng enemy
            Vector3 impactDirection = (enemy.transform.position - (Vector3)targetPosition).normalized;
            if (impactDirection.magnitude < 0.1f) impactDirection = Vector3.up; // Fallback direction
            
            CreateImpactEffect(enemy.transform.position, impactDirection, enemy.gameObject);
            CreateIndividualHitEffect(enemy.transform.position, isCritical);
        }
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Create main visual effect EXACTLY at target position (mouse click)
        CreateVisualEffect(targetPosition);
        
        // Damage area EXACTLY at mouse click position
        if (Module.showDamageArea)
        {
            ShowDamageAreaAtExactPosition(targetPosition, Module.areaRadius, "AreaDamageIndicator_MouseClick");
        }
    }

    public override void ShowDamageArea(Vector2 position)
    {
        // Không t?o damage area t?i v? trí chu?t cho Area skill
        // Ch? hi?n th? khi skill ???c execute
    }

    public override void UpdateDamageArea(Vector2 position)
    {
        // C?p nh?t v? trí vùng sát th??ng theo mouse nh?ng trong ph?m vi h?p l?
    }

    public override void HideDamageArea()
    {
        // ?n vùng sát th??ng area
    }
}

// 4. SUPPORT SKILL EXECUTOR - H? tr? không c?n v? vùng
public class SupportSkillExecutor : SkillExecutorBase
{
    public SupportSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

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
        
        // Apply healing if specified
        if (Module.healAmount > 0)
        {
            user.Heal(Module.healAmount);
            Debug.Log($"?? Support skill '{Module.skillName}' healed {user.name} for {Module.healAmount} HP");
        }
        
        // Apply buff effects (you can extend this)
        // TODO: Add buff system integration here
        
        // Create enhanced visual effect t?i v? trí user
        CreateVisualEffect(user.transform.position);
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Support skills show special enhanced effects
        if (Module.showDamageArea)
        {
            CreateEnhancedSupportVisualEffect(user.transform.position);
        }
    }
    
    /// <summary>
    /// T?o enhanced visual effect ??c bi?t cho Support skills
    /// </summary>
    private void CreateEnhancedSupportVisualEffect(Vector2 position)
    {
        GameObject supportEffect = new GameObject($"SupportEffect_{Module.skillName}_{Time.time:F2}");
        supportEffect.transform.position = new Vector3(position.x, position.y, 0);
        
        // T?o enhanced particle system cho support effect
        var particleSystem = supportEffect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = Module.skillColor;
        main.startLifetime = 2.5f;
        main.startSpeed = 1.2f;
        main.maxParticles = 25;
        main.startSize = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 12f;
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.2f;
        
        // Enhanced color over lifetime
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Module.skillColor, 0.0f), 
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(Module.skillColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.8f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.3f),
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // Auto destroy v?i enhanced system
        var autoDestroy = supportEffect.AddComponent<EffectAutoDestroy>();
        autoDestroy.Initialize(Module.damageAreaDisplayTime, true);
    }
}

// 5. INSTANT SKILL EXECUTOR - Instant skills without targeting
public class InstantSkillExecutor : SkillExecutorBase
{
    public InstantSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger animation immediately
        TriggerAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Apply instant effects immediately - no delay
        ApplyInstantEffects(user);
    }
    
    private void ApplyInstantEffects(Character user)
    {
        // Apply healing if specified
        if (Module.healAmount > 0)
        {
            user.Heal(Module.healAmount);
            Debug.Log($"? Instant heal: {Module.healAmount} HP restored to {user.name}!");
        }
        
        // Apply shield/defense buffs (you can extend this)
        if (Module.knockbackForce > 0)
        {
            // Use knockbackForce as shield amount for instant skills
            // TODO: Implement shield system
            Debug.Log($"??? Shield applied: {Module.knockbackForce} points to {user.name}!");
        }
        
        // Apply speed/movement buffs
        if (Module.speed > 0)
        {
            // TODO: Implement speed buff system
            Debug.Log($"?? Speed buff applied: {Module.speed} to {user.name}!");
        }
        
        // Apply damage buffs
        if (Module.damage > 0)
        {
            // TODO: Implement damage buff system
            Debug.Log($"?? Damage buff applied: {Module.damage} to {user.name}!");
        }
        
        // Create enhanced visual effect t?i v? trí user
        CreateVisualEffect(user.transform.position);
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Show enhanced instant effect visual if enabled
        if (Module.showDamageArea)
        {
            CreateEnhancedInstantVisualEffect(user.transform.position);
        }
    }
    
    /// <summary>
    /// T?o enhanced visual effect ??c bi?t cho Instant skills
    /// </summary>
    private void CreateEnhancedInstantVisualEffect(Vector2 position)
    {
        GameObject instantEffect = new GameObject($"InstantEffect_{Module.skillName}_{Time.time:F2}");
        instantEffect.transform.position = new Vector3(position.x, position.y, 0);
        
        // T?o enhanced particle system cho instant effect
        var particleSystem = instantEffect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = Module.skillColor;
        main.startLifetime = 1.5f;
        main.startSpeed = 3f;
        main.maxParticles = 40;
        main.startSize = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 60f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 20),
            new ParticleSystem.Burst(0.2f, 15),
            new ParticleSystem.Burst(0.4f, 10)
        });
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        // Enhanced velocity over lifetime for burst effect
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(3f);
        
        // Enhanced size over lifetime
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Auto destroy v?i enhanced system
        var autoDestroy = instantEffect.AddComponent<EffectAutoDestroy>();
        autoDestroy.Initialize(2.5f, true);
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
    }

    private void Update()
    {
        if (skillModule == null)
        {
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
            Destroy(gameObject);
        }
    }
    
    private Character[] FindEnemiesInCollisionRange()
    {
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        var enemiesInRange = new System.Collections.Generic.List<Character>();
        
        foreach (var character in allCharacters)
        {
            // CRITICAL FIX: Use bulletproof player detection
            if (IsPlayerCharacter(character)) continue;
            
            // Skip if dead
            if (character.health == null || character.health.currentValue <= 0) continue;
            
            float distance = Vector2.Distance(transform.position, character.transform.position);
            if (distance <= hitRadius)
            {
                enemiesInRange.Add(character);
            }
        }
        
        return enemiesInRange.ToArray();
    }
    
    /// <summary>
    /// BULLETPROOF player detection for projectiles
    /// </summary>
    private bool IsPlayerCharacter(Character character)
    {
        // Same as caster
        if (character == caster) return true;
        
        // Has PlayerController
        var playerController = character.GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
            return true;
        
        // AttackableCharacter check
        var attackable = character.GetComponent<AttackableCharacter>();
        if (attackable != null && !attackable.CanBeAttacked())
            return true;
        
        // Name patterns
        string objName = character.gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("hero") || objName.Contains("character"))
            return true;
            
        // Tag check
        if (character.gameObject.CompareTag("Player"))
            return true;
        
        return false;
    }
    
    private void HitEnemy(Character enemy)
    {
        // Calculate damage
        float finalDamage = skillModule.damage;
        bool isCritical = Random.Range(0f, 1f) < skillModule.criticalChance;
        if (isCritical)
        {
            finalDamage *= skillModule.criticalMultiplier;
        }
        
        enemy.TakeDamage(finalDamage, isCritical);
        
        // Apply knockback v?i direction c?a projectile
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
        
        // Create enhanced impact effect t?i v? trí va ch?m chính xác
        Vector3 impactPosition = transform.position;
        Vector3 impactDirection = direction;
        
        if (skillModule.effectPrefab != null)
        {
            // S? d?ng Enhanced Effect Manager cho impact effect
            EnhancedEffectManager.CreateImpactEffect(
                skillModule.effectPrefab,
                impactPosition,
                impactDirection,
                enemy.gameObject,
                skillModule.damageAreaDisplayTime
            );
        }
        
        // Create individual hit effect with critical differentiation
        CreateProjectileHitEffect(impactPosition, isCritical);
        
        Debug.Log($"?? Projectile '{skillModule.skillName}' hit {enemy.name} at {impactPosition} for {finalDamage} damage");
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// T?o hit effect ??c bi?t cho projectile
    /// </summary>
    private void CreateProjectileHitEffect(Vector3 position, bool isCritical)
    {
        GameObject hitEffect = new GameObject($"ProjectileHit_{skillModule.skillName}_{Time.time:F2}");
        hitEffect.transform.position = position;
        
        // Create particle system for projectile hit
        var particleSystem = hitEffect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = isCritical ? Color.yellow : skillModule.skillColor;
        main.startLifetime = 0.8f;
        main.startSpeed = isCritical ? 5f : 3f;
        main.maxParticles = isCritical ? 20 : 12;
        main.startSize = isCritical ? 0.4f : 0.25f;
        
        var emission = particleSystem.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, isCritical ? 15 : 8),
            new ParticleSystem.Burst(0.1f, isCritical ? 8 : 4)
        });
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        // Auto destroy with enhanced system
        var autoDestroy = hitEffect.AddComponent<EffectAutoDestroy>();
        autoDestroy.Initialize(1.5f, true);
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