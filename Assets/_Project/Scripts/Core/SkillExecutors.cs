using UnityEngine;
using System.Collections;
using System.Linq;
using RPG.Effects; // Add namespace reference

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
            // S? d?ng animationTrigger t? Module thay v� hard-coded
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
    /// Enhanced visual effect creation v?i auto-destroy v� collision positioning
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
    /// T?o effect va ch?m t?i v? tr� ch�nh x�c v?i direction
    /// </summary>
    protected void CreateImpactEffect(Vector3 impactPosition, Vector3 impactDirection, GameObject target = null)
    {
        if (Module.effectPrefab != null)
        {
            // T?o effect t?i v? tr� va ch?m ch�nh x�c
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
    /// T?o effect theo d�i target (cho projectile)
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
        // Method 1: Same as caster - CRITICAL for preventing self-damage
        if (caster != null && character == caster) 
        {
            Debug.Log($"??? SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
            return true;
        }
        
        // Method 2: Has PlayerController component
        var playerController = character.GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
        {
            Debug.Log($"?? PLAYER DETECTED: {character.name} has PlayerController");
            return true;
        }
        
        // Method 3: Check AttackableCharacter component
        var attackable = character.GetComponent<AttackableCharacter>();
        if (attackable != null && !attackable.CanBeAttacked())
        {
            Debug.Log($"??? NON-ATTACKABLE: {character.name} cannot be attacked");
            return true;
        }
        
        // Method 4: Check GameObject name patterns
        string objName = character.gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("hero") || objName.Contains("character"))
        {
            Debug.Log($"??? PLAYER NAME PATTERN: {character.name} matches player naming");
            return true;
        }
            
        // Method 5: Check tag
        if (character.gameObject.CompareTag("Player"))
        {
            Debug.Log($"??? PLAYER TAG: {character.name} has Player tag");
            return true;
        }
        
        // Method 6: Check for CoreEnemy component (enemies shouldn't target other enemies)
        var coreEnemy = character.GetComponent<CoreEnemy>();
        if (coreEnemy != null && caster != null)
        {
            var casterCoreEnemy = caster.GetComponent<CoreEnemy>();
            if (casterCoreEnemy != null)
            {
                Debug.Log($"?? ENEMY-TO-ENEMY PREVENTION: {caster.name} (enemy) won't target {character.name} (enemy)");
                return true; // Treat as "player" to prevent enemy-to-enemy damage
            }
        }
        
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
    /// Current melee indicator - Removed old system, using new shaped damage areas
    /// </summary>
    private GameObject currentMeleeIndicator;

    // Removed CreateEnhancedDamageZoneIndicator - using new shaped damage areas instead

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
        var fadeEffect = indicator.AddComponent<MeleeSkillFadeEffect>();
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
            
            // CRITICAL FIX: Use comprehensive enemy/player detection
            bool isValidTarget = false;
            
            // If caster is player, target only enemies
            if (caster.gameObject.CompareTag("Player"))
            {
                // Target must be tagged as Enemy and NOT be a player
                if (character.gameObject.CompareTag("Enemy") && !IsPlayerCharacter(character, caster))
                {
                    isValidTarget = true;
                }
            }
            // If caster is enemy, target only player
            else if (caster.gameObject.CompareTag("Enemy"))
            {
                // Target must be a player (tagged or has PlayerController)
                if (character.gameObject.CompareTag("Player") || IsPlayerCharacter(character, caster))
                {
                    isValidTarget = true;
                }
            }
            // Additional safety: if caster has no tag, use the player detection method
            else
            {
                // Use the comprehensive player detection
                bool casterIsPlayer = IsPlayerCharacter(caster, null);
                bool targetIsPlayer = IsPlayerCharacter(character, caster);
                
                // Players target enemies, enemies target players
                if (casterIsPlayer && !targetIsPlayer)
                {
                    isValidTarget = true;
                }
                else if (!casterIsPlayer && targetIsPlayer)
                {
                    isValidTarget = true;
                }
            }
            
            if (!isValidTarget) continue;
            
            // Check if target is alive
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

    /// <summary>
    /// Show shaped damage area based on damage area shape
    /// </summary>
    private void ShowShapedDamageArea(Vector2 center, Vector2 direction, string areaName)
    {
        if (!Module.showDamageArea) return;
        
        GameObject areaIndicator = null;
        
        switch (Module.damageAreaShape)
        {
            case DamageAreaShape.Semicircle:
                areaIndicator = CreateSemicircleArea(center, direction, areaName);
                break;
            case DamageAreaShape.Rectangle:
                areaIndicator = CreateRectangleArea(center, direction, areaName);
                break;
            case DamageAreaShape.Cone:
                areaIndicator = CreateConeArea(center, direction, areaName);
                break;
            case DamageAreaShape.Circle:
            default:
                areaIndicator = CreateCircleArea(center, areaName);
                break;
        }
        
        if (areaIndicator != null)
        {
            SetupAreaVisuals(areaIndicator);
        }
    }

    /// <summary>
    /// Create semicircle area indicator
    /// </summary>
    private GameObject CreateSemicircleArea(Vector2 center, Vector2 direction, string name)
    {
        GameObject area = new GameObject(name + "_Semicircle");
        area.transform.position = center;
        
        // Create semicircle mesh
        var meshFilter = area.AddComponent<MeshFilter>();
        var meshRenderer = area.AddComponent<MeshRenderer>();
        
        Mesh mesh = CreateSemicircleMesh(Module.range, direction);
        meshFilter.mesh = mesh;
        
        return area;
    }

    /// <summary>
    /// Create rectangle area indicator
    /// </summary>
    private GameObject CreateRectangleArea(Vector2 center, Vector2 direction, string name)
    {
        GameObject area = new GameObject(name + "_Rectangle");
        area.transform.position = center + direction * (Module.directionalAttackSize.x / 2f);
        
        // Set rotation to match direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        area.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Create box collider for visualization
        var boxCollider = area.AddComponent<BoxCollider2D>();
        boxCollider.size = Module.directionalAttackSize;
        boxCollider.isTrigger = true;
        
        // Add mesh renderer for visual
        var meshFilter = area.AddComponent<MeshFilter>();
        var meshRenderer = area.AddComponent<MeshRenderer>();
        
        // Create simple box mesh
        Mesh mesh = CreateBoxMesh(Module.directionalAttackSize);
        meshFilter.mesh = mesh;
        
        return area;
    }

    /// <summary>
    /// Create cone area indicator
    /// </summary>
    private GameObject CreateConeArea(Vector2 center, Vector2 direction, string name)
    {
        GameObject area = new GameObject(name + "_Cone");
        area.transform.position = center;
        
        // Create cone mesh
        var meshFilter = area.AddComponent<MeshFilter>();
        var meshRenderer = area.AddComponent<MeshRenderer>();
        
        Mesh mesh = CreateConeMesh(Module.range, 45f, direction);
        meshFilter.mesh = mesh;
        
        return area;
    }

    /// <summary>
    /// Create circle area indicator
    /// </summary>
    private GameObject CreateCircleArea(Vector2 center, string name)
    {
        GameObject area = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        area.name = name + "_Circle";
        area.transform.position = center;
        area.transform.localScale = Vector3.one * Module.range * 2;
        
        // Remove collider
        var collider = area.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
        
        return area;
    }

    /// <summary>
    /// Create semicircle mesh
    /// </summary>
    private Mesh CreateSemicircleMesh(float radius, Vector2 direction)
    {
        Mesh mesh = new Mesh();
        int segments = 32;
        
        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = Vector3.zero; // Center point
        
        float angleStep = Mathf.PI / segments;
        float startAngle = Mathf.Atan2(direction.y, direction.x) - Mathf.PI / 2;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + i * angleStep;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    /// <summary>
    /// Create box mesh
    /// </summary>
    private Mesh CreateBoxMesh(Vector2 size)
    {
        Mesh mesh = new Mesh();
        
        float halfWidth = size.x / 2f;
        float halfHeight = size.y / 2f;
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, halfHeight, 0),
            new Vector3(-halfWidth, halfHeight, 0)
        };
        
        int[] triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    /// <summary>
    /// Create cone mesh
    /// </summary>
    private Mesh CreateConeMesh(float radius, float angle, Vector2 direction)
    {
        Mesh mesh = new Mesh();
        int segments = 16;
        
        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = Vector3.zero; // Center point
        
        float coneAngle = angle * Mathf.Deg2Rad;
        float startAngle = Mathf.Atan2(direction.y, direction.x) - coneAngle / 2;
        
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = startAngle + (i * coneAngle / segments);
            vertices[i + 1] = new Vector3(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius, 0);
        }
        
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }

    /// <summary>
    /// Setup visual appearance for damage area indicators
    /// </summary>
    private void SetupAreaVisuals(GameObject area)
    {
        var renderer = area.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material areaMaterial = CreateEnhancedDamageAreaMaterial(Module.damageAreaColor);
            renderer.material = areaMaterial;
        }
        
        // Add pulsing effect
        var pulseEffect = area.AddComponent<MeleeSkillDamageEffect>();
        if (pulseEffect != null)
        {
            pulseEffect.Initialize(Module.damageAreaColor, Module.damageAreaDisplayTime);
        }
        
        // Auto-destroy after display time
        Object.Destroy(area, Module.damageAreaDisplayTime);
    }
}

// 1. MELEE SKILL EXECUTOR - C?n chi�n v?i collider t? ??ng
public class MeleeSkillExecutor : SkillExecutorBase
{
    public MeleeSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger enhanced animation with combat improvements
        TriggerEnhancedMeleeAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Start damage dealing coroutine with delay - use configured delay
        float damageDelay = Module.damageDelay > 0 ? Module.damageDelay : 0.3f;
        user.StartCoroutine(DealMeleeDamageAfterDelay(user, damageDelay));
    }

    private IEnumerator DealMeleeDamageAfterDelay(Character user, float delay)
    {
        // Enhanced timing system - use animation length if available
        float actualDelay = CalculateOptimalExecutionDelay(delay);
        yield return new WaitForSeconds(actualDelay);
        
        // Attack ALWAYS centered on USER position
        Vector2 attackCenter = user.transform.position;
        
        // Get attack direction for 4-directional attacks
        Vector2 attackDirection = Get4DirectionalAttackDirection(user);
        
        // Find targets based on attack type (4-directional or regular)
        Character[] enemies;
        if (Module.use4DirectionalAttack)
        {
            enemies = FindTargetsInShapedArea(attackCenter, attackDirection, user);
        }
        else
        {
            enemies = FindSkillTargetsInRange(attackCenter, Module.range, user);
        }
        
        // DEBUG: Log target detection results
        Debug.Log($"⚔️ MELEE SKILL: {Module.skillName} by {user.name} (Tag: {user.gameObject.tag}) found {enemies.Length} targets");
        foreach (var enemy in enemies)
        {
            Debug.Log($"  Target: {enemy.name} (Tag: {enemy.gameObject.tag})");
        }
        
        // Show combo indicator for multi-hit attacks
        if (enemies.Length > 1 && PlayerDamageIndicatorSystem.Instance != null)
        {
            float comboMultiplier = 1f + (enemies.Length - 1) * 0.2f;
            PlayerDamageIndicatorSystem.Instance.ShowComboIndicator(attackCenter, enemies.Length, comboMultiplier);
        }
        
        // Enhanced feedback for no targets found
        if (enemies.Length == 0)
        {
            // Still show damage area for visual feedback
            if (Module.showDamageArea)
            {
                // Chỉ sử dụng hệ thống mới
                ShowShapedDamageArea(attackCenter, attackDirection, "MeleeDamageArea_NoTargets");
            }
            
            // Play impact sound and create effect even without targets
            PlayImpactSound(user);
            CreateVisualEffect(user.transform.position); // Enhanced effect creation
            yield break;
        }
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            ProcessMeleeHit(user, enemy);
        }
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Create main visual effect at user position
        CreateVisualEffect(user.transform.position);
        
        // Show enhanced damage area
        if (Module.showDamageArea)
        {
            // Chỉ sử dụng hệ thống mới
            ShowShapedDamageArea(attackCenter, attackDirection, "MeleeDamageArea_UserCentered");
        }
    }

    /// <summary>
    /// Get 4-directional attack direction from PlayerController
    /// </summary>
    private Vector2 Get4DirectionalAttackDirection(Character user)
    {
        // Get current facing direction from PlayerController for 4-directional attacks
        var playerController = user.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Try to get current facing direction
            AttackDirection facingDir = playerController.CurrentFacingDirection;
            
            return facingDir switch
            {
                AttackDirection.Up => Vector2.up,
                AttackDirection.Down => Vector2.down,
                AttackDirection.Left => Vector2.left,
                AttackDirection.Right => Vector2.right,
                _ => Vector2.down // Default fallback
            };
        }
        
        // Fallback to facing direction based on scale
        float scaleX = user.transform.localScale.x;
        return scaleX > 0 ? Vector2.right : Vector2.left;
    }

    /// <summary>
    /// Find targets in shaped area based on damage area shape
    /// </summary>
    private Character[] FindTargetsInShapedArea(Vector2 center, Vector2 direction, Character user)
    {
        switch (Module.damageAreaShape)
        {
            case DamageAreaShape.Circle:
                return FindSkillTargetsInRange(center, Module.range, user);
                
            case DamageAreaShape.Semicircle:
                return FindTargetsInSemicircle(center, direction, user);
                
            case DamageAreaShape.Rectangle:
                return FindTargetsInRectangle(center, direction, user);
                
            case DamageAreaShape.Cone:
                return FindTargetsInCone(center, direction, user);
                
            default:
                return FindSkillTargetsInRange(center, Module.range, user);
        }
    }

    /// <summary>
    /// Find targets in semicircle shape
    /// </summary>
    private Character[] FindTargetsInSemicircle(Vector2 center, Vector2 direction, Character user)
    {
        var targets = new System.Collections.Generic.List<Character>();
        float radius = Module.range;
        
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character == null || character == user) continue;
            if (IsPlayerCharacter(character, user)) continue;
            
            Vector2 toTarget = (character.transform.position - user.transform.position);
            float distance = toTarget.magnitude;
            
            if (distance <= radius)
            {
                Vector2 directionToTarget = toTarget.normalized;
                float angle = Vector2.Angle(direction, directionToTarget);
                
                // Sử dụng attackAngle từ SkillModule thay vì hardcode 90 degrees
                float halfAngle = Module.attackAngle / 2f;
                if (angle <= halfAngle)
                {
                    if (character.health != null && character.health.currentValue > 0)
                    {
                        targets.Add(character);
                    }
                }
            }
        }
        
        return targets.ToArray();
    }

    /// <summary>
    /// Find targets in rectangle shape
    /// </summary>
    private Character[] FindTargetsInRectangle(Vector2 center, Vector2 direction, Character user)
    {
        var targets = new System.Collections.Generic.List<Character>();
        
        // Sử dụng attackWidth và range thay vì directionalAttackSize
        Vector2 size = new Vector2(Module.range, Module.attackWidth);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2 boxCenter = center + direction * (size.x / 2f);
        
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character == null || character == user) continue;
            if (IsPlayerCharacter(character, user)) continue;
            
            // Check if character is within rectangle bounds
            Vector2 localPoint = RotatePointAroundPivot(character.transform.position, boxCenter, -angle);
            Vector2 localBoxCenter = boxCenter;
            
            if (Mathf.Abs(localPoint.x - localBoxCenter.x) <= size.x / 2f &&
                Mathf.Abs(localPoint.y - localBoxCenter.y) <= size.y / 2f)
            {
                if (character.health != null && character.health.currentValue > 0)
                {
                    targets.Add(character);
                }
            }
        }
        
        return targets.ToArray();
    }

    /// <summary>
    /// Find targets in cone shape
    /// </summary>
    private Character[] FindTargetsInCone(Vector2 center, Vector2 direction, Character user)
    {
        var targets = new System.Collections.Generic.List<Character>();
        float radius = Module.range;
        // Sử dụng attackAngle từ SkillModule thay vì hardcode
        float coneAngle = Module.attackAngle / 2f;
        
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character == null || character == user) continue;
            if (IsPlayerCharacter(character, user)) continue;
            
            Vector2 toTarget = (character.transform.position - user.transform.position);
            float distance = toTarget.magnitude;
            
            if (distance <= radius)
            {
                Vector2 directionToTarget = toTarget.normalized;
                float angle = Vector2.Angle(direction, directionToTarget);
                
                if (angle <= coneAngle)
                {
                    if (character.health != null && character.health.currentValue > 0)
                    {
                        targets.Add(character);
                    }
                }
            }
        }
        
        return targets.ToArray();
    }

    /// <summary>
    /// Rotate point around pivot by angle (in degrees)
    /// </summary>
    private Vector2 RotatePointAroundPivot(Vector2 point, Vector2 pivot, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        
        Vector2 dir = point - pivot;
        return new Vector2(
            cos * dir.x - sin * dir.y + pivot.x,
            sin * dir.x + cos * dir.y + pivot.y
        );
    }

    /// <summary>
    /// Process individual melee hit with enhanced damage calculation
    /// </summary>
    private void ProcessMeleeHit(Character user, Character enemy)
    {
        if (enemy == null) return;
        
        // Calculate damage with enhanced critical system
        float finalDamage = CalculateEnhancedDamage(Module.damage, Module.criticalChance, Module.criticalMultiplier);
        bool isCritical = Random.Range(0f, 1f) < Module.criticalChance;
        
        // Apply damage with enhanced feedback
        Debug.Log($"⚔️ MELEE HIT: {user.name} deals {finalDamage} damage to {enemy.name}");
        enemy.TakeDamageEnhanced(finalDamage, user.gameObject, DamageType.Physical, isCritical);
        
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
        
        // Create individual impact effects for each enemy
        Vector3 impactDirection = (enemy.transform.position - user.transform.position).normalized;
        CreateImpactEffect(enemy.transform.position, impactDirection, enemy.gameObject);
        CreateIndividualHitEffect(enemy.transform.position, isCritical);
        
        // Show enhanced damage feedback
        ShowDamageImpactFeedback(enemy.transform.position, finalDamage, isCritical);
    }

    /// <summary>
    /// Enhanced animation trigger for melee attacks with combat lunge and damage preview
    /// </summary>
    private void TriggerEnhancedMeleeAnimation(Character user)
    {
        var playerController = user.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Get attack direction and show preview
            Vector2 attackDirection = Get4DirectionalAttackDirection(user);
            Vector2 attackCenter = user.transform.position;
            
            // Show attack preview for enhanced visual feedback
            ShowAttackPreview(user, attackCenter, attackDirection);
            
            // Use enhanced animation method if available
            if (Module.use4DirectionalAttack)
            {
                playerController.TriggerSkillAnimationEnhanced(Module, attackDirection);
            }
            else
            {
                // Standard animation but with enhanced features
                playerController.TriggerSkillAnimation(Module.skillName, Module.animationTrigger);
                
                // Apply lunge effect manually if configured
                if (Module.attackLungeForce > 0f)
                {
                    playerController.ApplyCombatLunge(attackDirection, Module.attackLungeForce);
                }
            }
        }
        else
        {
            // Fallback to standard animation
            TriggerAnimation(user);
        }
    }

    /// <summary>
    /// Show attack preview for enhanced visual feedback
    /// </summary>
    private void ShowAttackPreview(Character user, Vector2 center, Vector2 direction)
    {
        if (PlayerDamageIndicatorSystem.Instance == null) return;
        
        // Determine damage area shape based on skill configuration
        DamageAreaShape shape = GetDamageAreaShape();
        
        // Show preview with appropriate timing
        PlayerDamageIndicatorSystem.Instance.ShowAttackPreview(Module, center, direction, shape);
    }

    /// <summary>
    /// Get damage area shape based on skill configuration
    /// </summary>
    private DamageAreaShape GetDamageAreaShape()
    {
        if (Module.use4DirectionalAttack)
        {
            // Module.damageAreaShape is already an enum, just return it
            return Module.damageAreaShape;
        }
        return DamageAreaShape.Circle;
    }

    /// <summary>
    /// Show damage impact feedback for enhanced combat feel
    /// </summary>
    private void ShowDamageImpactFeedback(Vector3 position, float damage, bool isCritical)
    {
        if (PlayerDamageIndicatorSystem.Instance == null) return;
        
        // Show impact feedback at hit position
        PlayerDamageIndicatorSystem.Instance.ShowImpactFeedback(position, damage, isCritical, 1);
        
        // Show floating damage numbers
        Color damageColor = isCritical ? Color.yellow : Color.white;
        PlayerDamageIndicatorSystem.Instance.ShowDamageNumbers(position, damage, isCritical, damageColor);
    }

    private void ShowShapedDamageArea(Vector2 center, Vector2 direction, string areaName)
    {
        if (!Module.showDamageArea) return;
        
        GameObject areaIndicator = null;
        
        switch (Module.damageAreaShape)
        {
            case DamageAreaShape.Semicircle:
                areaIndicator = CreateSemicircleArea(center, direction, areaName);
                break;
            case DamageAreaShape.Rectangle:
                areaIndicator = CreateRectangleArea(center, direction, areaName);
                break;
            case DamageAreaShape.Cone:
                areaIndicator = CreateConeArea(center, direction, areaName);
                break;
            case DamageAreaShape.Circle:
                areaIndicator = CreateCircleArea(center, areaName);
                break;
        }
        
        if (areaIndicator != null)
        {
            SetupAreaVisuals(areaIndicator);
        }
    }

    private GameObject CreateSemicircleArea(Vector2 center, Vector2 direction, string name)
    {
        GameObject area = new GameObject(name);
        area.transform.position = center;
        
        // Create semicircle mesh
        var meshFilter = area.AddComponent<MeshFilter>();
        var meshRenderer = area.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        float radius = Module.range;
        int segments = 32;
        
        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = Vector3.zero;
        
        float angleStep = Mathf.PI / segments;
        float startAngle = Mathf.Atan2(direction.y, direction.x) - Mathf.PI / 2;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + i * angleStep;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
        
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        return area;
    }

    private GameObject CreateRectangleArea(Vector2 center, Vector2 direction, string name)
    {
        GameObject area = new GameObject(name);
        area.transform.position = center;
        
        var boxCollider = area.AddComponent<BoxCollider2D>();
        boxCollider.size = Module.directionalAttackSize;
        boxCollider.offset = direction * (Module.directionalAttackSize.x / 2f);
        boxCollider.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        
        return area;
    }

    private GameObject CreateConeArea(Vector2 center, Vector2 direction, string name)
    {
        GameObject area = new GameObject(name);
        area.transform.position = center;
        
        var meshFilter = area.AddComponent<MeshFilter>();
        var meshRenderer = area.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        float radius = Module.range;
        float angle = 45f * Mathf.Deg2Rad;
        int segments = 16;
        
        Vector3[] vertices = new Vector3[segments + 2];
        vertices[0] = Vector3.zero;
        
        float startAngle = Mathf.Atan2(direction.y, direction.x) - angle / 2;
        
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = startAngle + (i * angle / segments);
            vertices[i + 1] = new Vector3(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius, 0);
        }
        
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        return area;
    }

    private GameObject CreateCircleArea(Vector2 center, string name)
    {
        GameObject area = new GameObject(name);
        area.transform.position = center;
        
        var circleCollider = area.AddComponent<CircleCollider2D>();
        circleCollider.radius = Module.range;
        
        return area;
    }

    private void SetupAreaVisuals(GameObject area)
    {
        var renderer = area.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material areaMaterial = new Material(Shader.Find("Sprites/Default"));
            areaMaterial.color = Module.damageAreaColor;
            renderer.material = areaMaterial;
        }
        
        // Add pulsing effect
        var pulseEffect = area.AddComponent<MeleeSkillDamageEffect>();
        if (pulseEffect != null)
        {
            pulseEffect.Initialize(Module.damageAreaColor, Module.damageAreaDisplayTime);
        }
        
        // Auto-destroy after display time
        Object.Destroy(area, Module.damageAreaDisplayTime);
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
        // Không tạo damage area trong ShowDamageArea để tránh trùng lặp
        // Damage area sẽ được tạo trong Execute() tại vị trí player
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

// 2. PROJECTILE SKILL EXECUTOR - Ph�ng chi�u v?i range indicator
public class ProjectileSkillExecutor : SkillExecutorBase
{
    public ProjectileSkillExecutor(SkillModule module) : base(module) { }

    public override void Execute(Character user, Vector2 targetPosition)
    {
        if (!Module.CanExecute(user)) return;

        // ENHANCED: Use targeting system for enemy-specific targeting
        Vector2 finalTargetPosition;
        
        // Check if user has targeting system
        var targetingSystem = user.GetComponent<TargetingSystem>();
        if (targetingSystem != null && targetingSystem.currentTarget != null)
        {
            // Use targeted enemy position
            finalTargetPosition = targetingSystem.currentTarget.transform.position;
            Debug.Log($"?? PROJECTILE TARGETING: Using targeted enemy {targetingSystem.currentTarget.name}");
        }
        else
        {
            // Fallback to mouse position targeting
            Vector2 rawMousePos = GetRawMouseWorldPosition();
            finalTargetPosition = GetValidTargetPosition(rawMousePos, user);
            Debug.Log($"?? PROJECTILE TARGETING: Using mouse position targeting");
        }

        // Use mana
        if (user.mana != null)
            user.mana.Decrease(Module.manaCost);

        // Trigger animation
        TriggerAnimation(user);
        
        // Play cast sound
        PlayCastSound(user);

        // Start projectile creation with delay
        user.StartCoroutine(CreateProjectileAfterDelay(user, finalTargetPosition, 0.2f));
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
        // Projectile kh�ng hi?n th? damage area t?i v? tr� b?t ??u
        // Thay v�o ?� c� th? hi?n th? trajectory ho?c kh�ng hi?n th? g�
    }

    public override void UpdateDamageArea(Vector2 position)
    {
        // Projectile c� th? c?p nh?t trajectory preview
    }

    public override void HideDamageArea()
    {
        // ?n trajectory preview n?u c�
    }
}

// 3. AREA SKILL EXECUTOR - AoE v?i v�ng s�t th??ng ch�nh x�c
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
        
        // DEBUG: Enhanced feedback for area skills
        Debug.Log($"?? AREA SKILL: {Module.skillName} by {user.name} (Tag: {user.gameObject.tag}) at {targetPosition}");
        Debug.Log($"   Found {enemies.Length} targets in radius {Module.areaRadius}");
        
        if (enemies.Length == 0)
        {
            Debug.Log($"?? Area skill '{Module.skillName}' found no valid targets at {targetPosition}");
        }
        else
        {
            foreach (var enemy in enemies)
            {
                Debug.Log($"  ?? Target: {enemy.name} (Tag: {enemy.gameObject.tag})");
            }
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
            
            Debug.Log($"?? AREA HIT: {user.name} deals {finalDamage} damage to {enemy.name}");
            enemy.TakeDamageEnhanced(finalDamage, user.gameObject, DamageType.Magic, isCritical);
            
            // Apply stun if specified
            if (Module.stunDuration > 0)
            {
                enemy.ApplyStun(Module.stunDuration);
            }
            
            // Create individual impact effects t?i v? tr� t?ng enemy
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
            // Use Effect2DManager for area skills instead of old system
            Effect2DManager.CreateWarningIndicator2D(targetPosition, Module.areaRadius, 0.3f, Module.damageAreaColor);
        }
    }

    public override void ShowDamageArea(Vector2 position)
    {
        // Kh�ng t?o damage area t?i v? tr� chu?t cho Area skill
        // Ch? hi?n th? khi skill ???c execute
    }

    public override void UpdateDamageArea(Vector2 position)
    {
        // C?p nh?t v? tr� v�ng s�t th??ng theo mouse nh?ng trong ph?m vi h?p l?
    }

    public override void HideDamageArea()
    {
        // ?n v�ng s�t th??ng area
    }
}

// 4. SUPPORT SKILL EXECUTOR - H? tr? kh�ng c?n v? v�ng
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
        
        // Create enhanced visual effect t?i v? tr� user
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
        var autoDestroy = supportEffect.AddComponent<RPG.Effects.MeleeSkillAutoDestroy>();
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
        
        // Create enhanced visual effect t?i v? tr� user
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
        var autoDestroy = instantEffect.AddComponent<RPG.Effects.MeleeSkillAutoDestroy>();
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
        
        // ENHANCED: Enemy projectiles always target player
        if (IsEnemyCaster(user))
        {
            // Find player as target for enemy projectiles
            var player = FindPlayerTarget();
            if (player != null)
            {
                direction = ((Vector2)player.transform.position - startPosition).normalized;
                Debug.Log($"?? ENEMY PROJECTILE: {skillModule.skillName} targeting player at {player.transform.position}");
            }
            else
            {
                // Fallback to original direction if no player found
                direction = (targetPosition - startPosition).normalized;
                Debug.LogWarning($"?? ENEMY PROJECTILE: No player found, using fallback direction");
            }
        }
        else
        {
            // Player projectiles use normal targeting
            direction = (targetPosition - startPosition).normalized;
            Debug.Log($"?? PLAYER PROJECTILE: {skillModule.skillName} using mouse targeting");
        }
        
        // Calculate max lifetime based on range and speed
        maxLifetime = skillModule.range / Mathf.Max(skillModule.speed, 0.1f);
        
        // Set rotation to face direction
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        Debug.Log($"?? PROJECTILE CREATED: {skillModule.skillName} by {caster.name} (Tag: {caster.gameObject.tag}) -> direction: {direction}");
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

        // CRITICAL: Enhanced collision detection to prevent enemy self-damage
        if (IsEnemyCaster(caster))
        {
            // Enemy projectiles only target players
            var playerTargets = FindPlayerTargetsInRange();
            if (playerTargets.Length > 0)
            {
                var targetPlayer = FindClosestEnemyToTrajectory(playerTargets);
                if (targetPlayer != null && targetPlayer != caster)
                {
                    HitCharacterEnemy(targetPlayer);
                    return;
                }
            }
        }
        else
        {
            // Player projectiles target enemies
            var characterEnemies = FindEnemiesInCollisionRange();
            if (characterEnemies.Length > 0)
            {
                var targetEnemy = FindClosestEnemyToTrajectory(characterEnemies);
                if (targetEnemy != null && targetEnemy != caster)
                {
                    HitCharacterEnemy(targetEnemy);
                    return;
                }
            }
            
            // Check IDamageable components without Character for player projectiles
            var damageableTargets = FindIDamageableTargetsInRange();
            if (damageableTargets.Length > 0)
            {
                var targetDamageable = FindClosestDamageableToTrajectory(damageableTargets);
                if (targetDamageable != null)
                {
                    HitIDamageableTarget(targetDamageable);
                    return;
                }
            }
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
        var enemiesInRange = new System.Collections.Generic.List<Character>();
        
        // ENHANCED: For enemy projectiles, only target players
        if (IsEnemyCaster(caster))
        {
            // Find all Character components and only target players
            var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                // CRITICAL: Skip the caster to prevent self-damage
                if (character.gameObject == caster.gameObject) 
                {
                    Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
                    continue;
                }
                
                // CRITICAL: Skip if same Character instance
                if (character == caster) 
                {
                    Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: Same caster instance");
                    continue;
                }
                
                // ENHANCED: Only target players for enemy projectiles
                if (!IsPlayerCharacter(character)) continue;
                
                if (character.health == null || character.health.currentValue <= 0) continue;
                
                float distance = Vector2.Distance(transform.position, character.transform.position);
                if (distance <= hitRadius)
                {
                    enemiesInRange.Add(character);
                }
            }
            
            Debug.Log($"?? ENEMY PROJECTILE: Found {enemiesInRange.Count} PLAYER targets in range {hitRadius}");
            return enemiesInRange.ToArray();
        }
        
        // Original logic for player projectiles - target enemies
        var playerCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in playerCharacters)
        {
            // CRITICAL: Skip the caster to prevent self-damage
            if (character.gameObject == caster.gameObject) 
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (Character): {character.name} == {caster.name}");
                continue;
            }
            
            // Skip if same Character instance
            if (character == caster) 
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (Character instance): Same caster");
                continue;
            }
            
            if (IsPlayerCharacter(character)) continue;
            
            if (character.health == null || character.health.currentValue <= 0) continue;
            
            float distance = Vector2.Distance(transform.position, character.transform.position);
            if (distance <= hitRadius)
            {
                enemiesInRange.Add(character);
            }
        }
        
        Debug.Log($"?? PROJECTILE COLLISION: Found {enemiesInRange.Count} Character targets in range {hitRadius}");
        
        return enemiesInRange.ToArray();
    }
    
    /// <summary>
    /// ENHANCED: Check if the caster is an enemy
    /// </summary>
    private bool IsEnemyCaster(Character caster)
    {
        if (caster == null) return false;
        
        // Check for CoreEnemy component
        return caster.GetComponent<CoreEnemy>() != null || 
               caster.gameObject.tag == "Enemy" ||
               caster.gameObject.layer == LayerMask.NameToLayer("Enemy");
    }
    
    /// <summary>
    /// ENHANCED: Find the player target for enemy projectiles
    /// </summary>
    private Character FindPlayerTarget()
    {
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in allCharacters)
        {
            if (IsPlayerCharacter(character))
            {
                return character;
            }
        }
        return null;
    }
    
    /// <summary>
    /// ENHANCED: Find only player targets within collision range for enemy projectiles
    /// </summary>
    private Character[] FindPlayerTargetsInRange()
    {
        var playerTargets = new System.Collections.Generic.List<Character>();
        
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in allCharacters)
        {
            // CRITICAL: Skip the caster to prevent self-damage
            if (character.gameObject == caster.gameObject) 
            {
                Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
                continue;
            }
            
            // CRITICAL: Skip if same Character instance
            if (character == caster) 
            {
                Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: Same caster instance");
                continue;
            }
            
            // Only target actual players
            if (!IsPlayerCharacter(character)) continue;
            
            if (character.health == null || character.health.currentValue <= 0) continue;
            
            float distance = Vector2.Distance(transform.position, character.transform.position);
            if (distance <= hitRadius)
            {
                playerTargets.Add(character);
            }
        }
        
        Debug.Log($"?? ENEMY PROJECTILE: Found {playerTargets.Count} PLAYER targets in range {hitRadius}");
        return playerTargets.ToArray();
    }
    
    /// <summary>
    /// ENHANCED: Direct IDamageable collision detection - now only targets players for enemy projectiles
    /// </summary>
    private IDamageable[] FindIDamageableTargetsInRange()
    {
        var targets = new System.Collections.Generic.List<IDamageable>();
        
        // ENHANCED: For enemy projectiles, only target players
        if (IsEnemyCaster(caster))
        {
            // Find all Character components (players)
            var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                if (IsPlayerCharacter(character))
                {
                    if (character.health == null || character.health.currentValue <= 0) continue;
                    
                    float distance = Vector2.Distance(transform.position, character.transform.position);
                    if (distance <= hitRadius)
                    {
                        targets.Add(character);
                    }
                }
            }
            
            Debug.Log($"?? ENEMY PROJECTILE: Found {targets.Count} PLAYER targets in range {hitRadius}");
            return targets.ToArray();
        }
        
        // Original logic for player projectiles
        var allDamageables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(mb => mb is IDamageable && mb.GetComponent<Character>() == null)
            .Cast<IDamageable>()
            .ToArray();
        
        foreach (var damageable in allDamageables)
        {
            var damageableComponent = damageable as MonoBehaviour;
            if (damageableComponent == null) continue;
            
            // CRITICAL: Check if this is the caster itself - prevent self-damage
            if (caster != null && damageableComponent.gameObject == caster.gameObject) 
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (IDamageable): {damageableComponent.name} == {caster.name}");
                continue;
            }
            
            // CRITICAL: Check if this is the caster's Character component
            if (caster != null && damageableComponent.GetComponent<Character>() == caster)
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (Character): Same caster");
                continue;
            }
            
            // Check if it's a CoreEnemy (enemy shouldn't hit enemy)
            var coreEnemy = damageableComponent.GetComponent<CoreEnemy>();
            if (coreEnemy != null && caster != null)
            {
                var casterCoreEnemy = caster.GetComponent<CoreEnemy>();
                if (casterCoreEnemy != null)
                {
                    Debug.Log($"?? PROJECTILE ENEMY-TO-ENEMY PREVENTION: {caster.name} projectile won't hit {damageableComponent.name} (both CoreEnemy)");
                    continue; // Skip enemy-to-enemy damage
                }
            }
            
            // Skip if target is dead
            if (damageable.IsDead) continue;
            
            float distance = Vector2.Distance(transform.position, damageableComponent.transform.position);
            if (distance <= hitRadius)
            {
                targets.Add(damageable);
            }
        }
        
        Debug.Log($"?? PROJECTILE COLLISION: Found {targets.Count} IDamageable targets in range {hitRadius}");
        
        return targets.ToArray();
    }
    
    /// <summary>
    /// BULLETPROOF player detection for projectiles - Enhanced to prevent self-damage
    /// </summary>
    private bool IsPlayerCharacter(Character character)
    {
        // CRITICAL: Same GameObject as caster - prevent self-damage
        if (character.gameObject == caster.gameObject) 
        {
            Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
            return true;
        }
        
        // CRITICAL: Same Character instance as caster - prevent self-damage
        if (character == caster) 
        {
            Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED: Same Character instance {character.name}");
            return true;
        }
        
        // ENHANCED: Check if this is an enemy - enemies should NOT be treated as players
        var coreEnemy = character.GetComponent<CoreEnemy>();
        if (coreEnemy != null)
        {
            Debug.Log($"?? PROJECTILE ENEMY DETECTED: {character.name} is an enemy (CoreEnemy)");
            return false; // This is an enemy, not a player
        }
        
        // Check for enemy tag
        if (character.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"?? PROJECTILE ENEMY TAG: {character.name} has Enemy tag");
            return false; // This is an enemy, not a player
        }
        
        // Check for enemy layer
        if (character.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log($"?? PROJECTILE ENEMY LAYER: {character.name} on Enemy layer");
            return false; // This is an enemy, not a player
        }
        
        // Has PlayerController - use proper type checking
        var playerController = character.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log($"?? PROJECTILE PLAYER DETECTED: {character.name} has PlayerController");
            return true;
        }
        
        // Player tag check - most reliable
        if (character.gameObject.CompareTag("Player"))
        {
            Debug.Log($"?? PROJECTILE PLAYER TAG: {character.name} has Player tag");
            return true;
        }
        
        // Player layer check (Layer 6 = Player)
        if (character.gameObject.layer == 6)
        {
            Debug.Log($"?? PROJECTILE PLAYER LAYER: {character.name} on Player layer");
            return true;
        }
        
        // Default: if not explicitly identified as player or enemy, assume it's not a player
        Debug.Log($"?? PROJECTILE CHARACTER TYPE: {character.name} is neither player nor enemy");
        return false;
    }
    
    private void HitCharacterEnemy(Character enemy)
    {
        // Calculate damage
        float finalDamage = skillModule.damage;
        bool isCritical = Random.Range(0f, 1f) < skillModule.criticalChance;
        if (isCritical)
        {
            finalDamage *= skillModule.criticalMultiplier;
        }
        
        Debug.Log($"?? PROJECTILE HIT CHARACTER: {caster.name} projectile hits {enemy.name} for {finalDamage} damage");
        enemy.TakeDamageEnhanced(finalDamage, caster.gameObject, DamageType.Physical, isCritical);
        
        // Apply knockback v?i direction c?a projectile
        if (skillModule.knockbackForce > 0)
        {
            enemy.ApplyKnockback(skillModule.knockbackForce, direction);
        }
        
        PlayImpactSoundAndEffects(enemy.transform.position, isCritical);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// ENHANCED: Hit IDamageable target (like CoreEnemy)
    /// </summary>
    private void HitIDamageableTarget(IDamageable target)
    {
        var targetComponent = target as MonoBehaviour;
        if (targetComponent == null) return;
        
        // Calculate damage
        float finalDamage = skillModule.damage;
        bool isCritical = Random.Range(0f, 1f) < skillModule.criticalChance;
        if (isCritical)
        {
            finalDamage *= skillModule.criticalMultiplier;
        }
        
        Debug.Log($"?? PROJECTILE HIT IDAMAGEABLE: {caster.name} projectile hits {targetComponent.name} for {finalDamage} damage");
        target.TakeDamage(finalDamage);
        
        // Apply knockback if target has Rigidbody2D
        if (skillModule.knockbackForce > 0)
        {
            var rb2d = targetComponent.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.AddForce(direction * skillModule.knockbackForce, ForceMode2D.Impulse);
            }
        }
        
        PlayImpactSoundAndEffects(targetComponent.transform.position, isCritical);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Consolidated method for playing impact sound and effects
    /// </summary>
    private void PlayImpactSoundAndEffects(Vector3 position, bool isCritical)
    {
        // Play impact sound
        if (skillModule.impactSound != null && caster != null)
        {
            var audioSource = caster.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(skillModule.impactSound);
            }
        }
        
        // Create enhanced impact effect t?i v? tr� va ch?m ch�nh x�c
        Vector3 impactDirection = direction;
        
        if (skillModule.effectPrefab != null)
        {
            // S? d?ng Enhanced Effect Manager cho impact effect
            EnhancedEffectManager.CreateImpactEffect(
                skillModule.effectPrefab,
                position,
                impactDirection,
                null, // No specific target game object
                skillModule.damageAreaDisplayTime
            );
        }
        
        // Create individual hit effect with critical differentiation
        CreateProjectileHitEffect(position, isCritical);
    }
    
    /// <summary>
    /// Find the enemy closest to the projectile's trajectory line
    /// </summary>
    private Character FindClosestEnemyToTrajectory(Character[] enemies)
    {
        if (enemies == null || enemies.Length == 0) return null;

        Character closestEnemy = null;
        float minDistanceToTrajectory = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            // Calculate distance from enemy to the trajectory line
            Vector2 enemyPos = enemy.transform.position;
            Vector2 projectilePos = transform.position;
            
            // Distance from point to line (startPosition + t * direction)
            float distanceToLine = Vector2.Distance(enemyPos, 
                startPosition + Vector2.Dot(enemyPos - startPosition, direction) * direction);

            // Also consider actual distance to current projectile position
            float distanceToProjectile = Vector2.Distance(enemyPos, projectilePos);

            // Combine both distances for better targeting
            float totalDistance = distanceToLine + distanceToProjectile * 0.5f;

            if (totalDistance < minDistanceToTrajectory)
            {
                minDistanceToTrajectory = totalDistance;
                closestEnemy = enemy;
            }
        }

        Debug.Log($"?? PROJECTILE TARGETING: Closest enemy to trajectory is {closestEnemy?.name} at distance {minDistanceToTrajectory}");
        return closestEnemy;
    }

    /// <summary>
    /// Find the IDamageable closest to the projectile's trajectory line
    /// </summary>
    private IDamageable FindClosestDamageableToTrajectory(IDamageable[] targets)
    {
        if (targets == null || targets.Length == 0) return null;

        IDamageable closestTarget = null;
        float minDistanceToTrajectory = float.MaxValue;

        foreach (var target in targets)
        {
            if (target == null) continue;
            
            var targetComponent = target as MonoBehaviour;
            if (targetComponent == null) continue;

            // Calculate distance from target to the trajectory line
            Vector2 targetPos = targetComponent.transform.position;
            Vector2 projectilePos = transform.position;
            
            // Distance from point to line (startPosition + t * direction)
            float distanceToLine = Vector2.Distance(targetPos, 
                startPosition + Vector2.Dot(targetPos - startPosition, direction) * direction);

            // Also consider actual distance to current projectile position
            float distanceToProjectile = Vector2.Distance(targetPos, projectilePos);

            // Combine both distances for better targeting
            float totalDistance = distanceToLine + distanceToProjectile * 0.5f;

            if (totalDistance < minDistanceToTrajectory)
            {
                minDistanceToTrajectory = totalDistance;
                closestTarget = target;
            }
        }

        Debug.Log($"?? PROJECTILE TARGETING: Closest IDamageable to trajectory is {(closestTarget as MonoBehaviour)?.name} at distance {minDistanceToTrajectory}");
        return closestTarget;
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
        var autoDestroy = hitEffect.AddComponent<RPG.Effects.MeleeSkillAutoDestroy>();
        autoDestroy.Initialize(1.5f, true);
    }

    private void OnDrawGizmos()
    {
        // DISABLED: Projectile debug gizmos for cleaner gameplay
        /*
        if (skillModule != null)
        {
            Gizmos.color = skillModule.skillColor;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
        */
    }
}

namespace RPG.Effects
{
    /// <summary>
    /// Helper component for damage area pulsing effect
    /// </summary>
    public class MeleeSkillDamageEffect : MonoBehaviour
    {
        private Color originalColor;
        private float pulseSpeed = 2f;
        private float lifetime = 1f;
        private Material material;
        
        public void Initialize(Color color, float displayTime)
        {
            originalColor = color;
            lifetime = displayTime;
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                material = renderer.material;
            }
        }
        
        void Update()
        {
            if (material != null)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.3f + 0.7f;
                Color pulsedColor = originalColor;
                pulsedColor.a *= pulse;
                material.color = pulsedColor;
            }
            
            lifetime -= Time.deltaTime;
            if (lifetime <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Helper component for fade out effects
    /// </summary>
    public class MeleeSkillFadeEffect : MonoBehaviour
    {
        private Material material;
        private Color originalColor;
        private float fadeSpeed;
        private bool isFading = false;
        
        public void StartFadeOut(float delay, float duration)
        {
            StartCoroutine(FadeOutCoroutine(delay, duration));
        }
        
        private System.Collections.IEnumerator FadeOutCoroutine(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                material = renderer.material;
                originalColor = material.color;
                fadeSpeed = 1f / duration;
                isFading = true;
            }
        }
        
        void Update()
        {
            if (isFading && material != null)
            {
                Color currentColor = material.color;
                currentColor.a -= fadeSpeed * Time.deltaTime;
                material.color = currentColor;
                
                if (currentColor.a <= 0)
                {
                    isFading = false;
                    Destroy(gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Enhanced auto-destroy component for effects
    /// </summary>
    public class MeleeSkillAutoDestroy : MonoBehaviour
    {
        private float lifetime;
        private bool useParticleSystem;
        
        public void Initialize(float destroyTime, bool checkParticleSystem = false)
        {
            lifetime = destroyTime;
            useParticleSystem = checkParticleSystem;
            
            if (useParticleSystem)
            {
                var particleSystem = GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    // Use particle system's actual duration if available
                    lifetime = Mathf.Max(lifetime, particleSystem.main.duration + particleSystem.main.startLifetime.constant);
                }
            }
            
            Destroy(gameObject, lifetime);
        }
    }
    
    /// <summary>
    /// Hướng dẫn thiết lập Animator cho hệ thống tấn công 4 hướng:
    /// 
    /// 1. Trong Animator, thêm parameter:
    ///    - FacingDirection (Integer): Xác định hướng nhìn
    ///      * 1 = Right (Phải)
    ///      * -1 = Left (Trái)
    ///      * 2 = Up (Lên)
    ///      * -2 = Down (Xuống)
    ///    - AttackUp (Trigger): Kích hoạt tấn công lên
    ///    - AttackDown (Trigger): Kích hoạt tấn công xuống 
    ///    - AttackLeft (Trigger): Kích hoạt tấn công trái
    ///    - AttackRight (Trigger): Kích hoạt tấn công phải
    ///
    /// 2. Tạo Blend Tree cho di chuyển và idle:
    ///    - Tạo state mới có Blend Tree type là 2D Freeform Directional
    ///    - Parameter X: MoveX hoặc FacingDirection
    ///    - Parameter Y: MoveY
    ///    - Thêm các motion: Idle, WalkUp, WalkDown, WalkLeft, WalkRight
    ///
    /// 3. Tạo các transitions từ Any State tới các attack states:
    ///    - Any -> AttackUp khi AttackUp được trigger
    ///    - Any -> AttackDown khi AttackDown được trigger
    ///    - Any -> AttackLeft khi AttackLeft được trigger
    ///    - Any -> AttackRight khi AttackRight được trigger
    ///    - Đặt Has Exit Time = false để transitions xảy ra ngay lập tức
    ///
    /// 4. Trong PlayerController:
    ///    - Bật use4DirectionalAttacks = true
    ///    - Đảm bảo rememberLastDirection = true nếu muốn nhớ hướng nhìn cuối cùng
    /// </summary>
    

    
    /// <summary>
    /// Enum cho hướng tấn công 4 chiều
    /// </summary>
    public enum AttackDirection
    {
        Up,
        Down, 
        Left,
        Right
    }
}