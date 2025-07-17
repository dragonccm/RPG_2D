/// <summary>
/// File: ModularSkillManager.cs
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Core skill management system with modular architecture and hotkey support
/// </summary>

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularSkillManager : MonoBehaviour
{
    [Header("Skill Slots Configuration")]
    [SerializeField] private int maxSkillSlots = 8;
    [SerializeField] private int levelsPerSlot = 5;
    
    [Header("Available Skills")]
    public List<SkillModule> availableSkills = new List<SkillModule>();
    
    [Header("Legacy System Settings")]
    [SerializeField] private bool enableLegacyHotkeys = false;
    [SerializeField] private List<SkillSlot> skillSlots = new List<SkillSlot>();
    
    private Character player;
    private Dictionary<ISkillExecutor, float> cooldownTimers = new Dictionary<ISkillExecutor, float>();
    
    // Events
    public System.Action<int> OnSlotUnlocked;
    public System.Action<int, SkillModule> OnSkillEquipped;
    public System.Action<int> OnSkillUnequipped;

    private bool isSkillActive = false;
    private bool isSkillHeld = false;
    private int activeSkillSlot = -1;
    private GameObject currentPreviewDamageArea = null;
    private GameObject currentProjectileDirectionLine = null;

    private void Awake()
    {
        player = GetComponent<Character>();
        InitializeSkillSlots();
    }

    private void Start()
    {
        UpdateUnlockedSlots();
    }

    private void Update()
    {
        UpdateCooldowns();
        
        if (enableLegacyHotkeys)
        {
            HandleSkillInput();
        }
    }

    private void InitializeSkillSlots()
    {
        skillSlots.Clear();
        
        KeyCode[] hotkeys = GenerateDynamicHotkeys(maxSkillSlots);
        
        for (int i = 0; i < maxSkillSlots; i++)
        {
            var slot = new SkillSlot(i, hotkeys[i]);
            skillSlots.Add(slot);
        }
    }
    
    private KeyCode[] GenerateDynamicHotkeys(int slotCount)
    {
        KeyCode[] hotkeys = new KeyCode[slotCount];
        
        KeyCode[] numberKeys = { 
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };
        
        KeyCode[] functionKeys = {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
        };
        
        KeyCode[] letterKeys = {
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
            KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P
        };
        
        KeyCode[] extraLetterKeys = {
            KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
            KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L
        };
        
        KeyCode[] mouseKeys = {
            KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6
        };
        
        System.Collections.Generic.List<KeyCode> allAvailableKeys = new System.Collections.Generic.List<KeyCode>();
        allAvailableKeys.AddRange(numberKeys);
        allAvailableKeys.AddRange(functionKeys);
        allAvailableKeys.AddRange(letterKeys);
        allAvailableKeys.AddRange(extraLetterKeys);
        allAvailableKeys.AddRange(mouseKeys);
        
        for (int i = 0; i < slotCount; i++)
        {
            if (i < allAvailableKeys.Count)
            {
                hotkeys[i] = allAvailableKeys[i];
            }
            else
            {
                hotkeys[i] = KeyCode.None;
            }
        }
        
        return hotkeys;
    }

    private void UpdateCooldowns()
    {
        // Enhanced cooldown system using SkillSlot's built-in cooldown management
        foreach (var slot in skillSlots)
        {
            if (slot.HasSkill())
            {
                slot.UpdateCooldown();
            }
        }
        
        // Legacy cooldown system (keep for backward compatibility)
        var cooldownList = cooldownTimers.ToList();
        foreach (var pair in cooldownList)
        {
            cooldownTimers[pair.Key] = pair.Value - Time.deltaTime;
            if (cooldownTimers[pair.Key] <= 0)
            {
                cooldownTimers.Remove(pair.Key);
            }
        }
    }

    private void HandleSkillInput()
    {
        if (!enableLegacyHotkeys) return;

        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (!slot.isUnlocked || !slot.HasSkill()) continue;

            var skill = slot.equippedSkill;
            
            // Enhanced input handling with better validation
            if (!slot.CanExecuteSkill(player)) continue;
            
            // Instant skills: Execute immediately on key down
            if (skill.skillType == SkillType.Instant)
            {
                if (Input.GetKeyDown(slot.hotkey))
                {
                    ActivateSkillEnhanced(i);
                }
                continue; // Skip the hold-release logic for instant skills
            }

            // Normal skills: Enhanced hold-release logic
            if (Input.GetKeyDown(slot.hotkey))
            {
                // Phase 1: Start skill with enhanced validation
                StartSkillPreviewEnhanced(i);
            }

            if (Input.GetKey(slot.hotkey))
            {
                // Phase 2: Hold skill with smooth updates
                HoldSkillPreviewEnhanced(i);
            }

            if (Input.GetKeyUp(slot.hotkey))
            {
                // Phase 3: Release skill with enhanced execution
                ActivateSkillEnhanced(i);
                EndSkillPreviewEnhanced(i);
            }
        }
    }

    /// <summary>
    /// Enhanced skill preview start with better validation and visual feedback
    /// </summary>
    private void StartSkillPreviewEnhanced(int slotIndex)
    {
        if (isSkillActive || slotIndex >= skillSlots.Count) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;
        
        // Enhanced validation
        if (!slot.CanExecuteSkill(player))
        {
            Debug.Log($"?? Cannot execute skill '{slot.equippedSkill.skillName}' - conditions not met");
            return;
        }

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // Enhanced skill preview with better visual feedback
        ShowSkillPreviewEnhanced(skill);

        isSkillActive = true;
        activeSkillSlot = slotIndex;
        
        Debug.Log($"?? Started preview for skill '{skill.skillName}' in slot {slotIndex}");
    }

    /// <summary>
    /// Enhanced skill preview hold with smooth updates
    /// </summary>
    private void HoldSkillPreviewEnhanced(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // Smooth preview updates
        UpdateSkillPreviewEnhanced(skill);
        isSkillHeld = true;
    }

    /// <summary>
    /// Enhanced skill preview end with cleanup
    /// </summary>
    private void EndSkillPreviewEnhanced(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        var slot = skillSlots[slotIndex];
        string skillName = slot.HasSkill() ? slot.equippedSkill.skillName : "Unknown";
        
        // Enhanced preview cleanup
        HideSkillPreviewEnhanced();

        isSkillActive = false;
        isSkillHeld = false;
        activeSkillSlot = -1;
        
        Debug.Log($"?? Ended preview for skill '{skillName}'");
    }

    /// <summary>
    /// Enhanced skill activation with better error handling and feedback
    /// </summary>
    private void ActivateSkillEnhanced(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            Debug.LogWarning($"?? Invalid slot index: {slotIndex}");
            return;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) 
        {
            Debug.LogWarning($"?? Slot {slotIndex} is not unlocked or has no skill");
            return;
        }
        
        // Use enhanced SkillSlot execution method
        var skill = slot.equippedSkill;
        Vector2 targetPos = Vector2.zero;
        
        // Enhanced target position calculation
        if (skill.skillType != SkillType.Instant)
        {
            targetPos = GetEnhancedMouseWorldPosition();
        }
        else
        {
            targetPos = player.transform.position;
        }
        
        // Execute using enhanced SkillSlot method
        bool success = slot.TryExecuteSkill(player, targetPos);
        
        if (success)
        {
            // Update legacy cooldown system for compatibility
            if (slot.executor != null)
            {
                cooldownTimers[slot.executor] = slot.executor.GetCooldown();
            }
            
            Debug.Log($"? Successfully activated skill '{skill.skillName}' from slot {slotIndex}");
        }
        else
        {
            Debug.Log($"? Failed to activate skill '{skill.skillName}' from slot {slotIndex}");
        }
    }

    /// <summary>
    /// Enhanced skill preview with better visuals and positioning
    /// </summary>
    private void ShowSkillPreviewEnhanced(SkillModule skill)
    {
        Vector2 playerPosition = transform.position;
        
        // Enhanced skill type handling
        switch (skill.skillType)
        {
            case SkillType.Melee:
                ShowMeleePreviewEnhanced(skill, playerPosition);
                break;
            case SkillType.Area:
                ShowAreaPreviewEnhanced(skill, playerPosition);
                break;
            case SkillType.Projectile:
                ShowProjectilePreviewEnhanced(skill, playerPosition);
                break;
            case SkillType.Instant:
                // Instant skills show brief flash effect
                ShowInstantPreviewEnhanced(skill, playerPosition);
                return;
            default:
                // Support skills show brief indicator
                ShowSupportPreviewEnhanced(skill, playerPosition);
                return;
        }
    }

    /// <summary>
    /// Enhanced melee preview with better visual feedback
    /// </summary>
    private void ShowMeleePreviewEnhanced(SkillModule skill, Vector2 playerPosition)
    {
        if (skill.damageZonePrefab != null)
        {
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = Object.Instantiate(skill.damageZonePrefab);
                currentPreviewDamageArea.name = "MeleePreviewArea_Enhanced";
                
                // Add enhanced visual effects
                AddPreviewEnhancements(currentPreviewDamageArea, skill.damageAreaColor);
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(playerPosition.x, playerPosition.y, -0.1f);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.range * 2;
        }
        else
        {
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = CreateEnhancedPreviewSphere(playerPosition, skill.range, skill.damageAreaColor, "MeleePreview");
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(playerPosition.x, playerPosition.y, -0.1f);
        }

        currentPreviewDamageArea.SetActive(true);
    }

    /// <summary>
    /// Enhanced area preview with smooth mouse tracking
    /// </summary>
    private void ShowAreaPreviewEnhanced(SkillModule skill, Vector2 playerPosition)
    {
        Vector2 mousePos = GetEnhancedMouseWorldPosition();
        Vector2 validPos = GetValidTargetPositionEnhanced(mousePos, playerPosition, skill);
        
        if (skill.damageZonePrefab != null)
        {
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = Object.Instantiate(skill.damageZonePrefab);
                currentPreviewDamageArea.name = "AreaPreviewArea_Enhanced";
                
                AddPreviewEnhancements(currentPreviewDamageArea, skill.damageAreaColor);
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, -0.1f);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.areaRadius * 2;
        }
        else
        {
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = CreateEnhancedPreviewSphere(validPos, skill.areaRadius, skill.damageAreaColor, "AreaPreview");
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, -0.1f);
        }

        currentPreviewDamageArea.SetActive(true);
    }

    /// <summary>
    /// Enhanced projectile preview with better trajectory visualization
    /// </summary>
    private void ShowProjectilePreviewEnhanced(SkillModule skill, Vector2 playerPosition)
    {
        Vector2 mousePos = GetEnhancedMouseWorldPosition();
        Vector2 direction = (mousePos - playerPosition).normalized;
        
        if (skill.damageZonePrefab != null)
        {
            if (currentProjectileDirectionLine == null)
            {
                currentProjectileDirectionLine = Object.Instantiate(skill.damageZonePrefab);
                currentProjectileDirectionLine.name = "ProjectileDirection_Enhanced";
                
                AddPreviewEnhancements(currentProjectileDirectionLine, skill.skillColor);
            }
            
            // Enhanced positioning and rotation
            UpdateProjectileDirectionEnhanced(playerPosition, direction, skill.range);
        }
        else
        {
            if (currentProjectileDirectionLine == null)
            {
                currentProjectileDirectionLine = CreateEnhancedDirectionLine(playerPosition, direction, skill.range, skill.skillColor);
            }
            
            UpdateProjectileDirectionLineEnhanced(playerPosition, direction, skill.range, skill.skillColor);
        }

        currentProjectileDirectionLine.SetActive(true);
        
        // Hide damage area for projectiles
        if (currentPreviewDamageArea != null)
        {
            currentPreviewDamageArea.SetActive(false);
        }
    }

    /// <summary>
    /// Enhanced instant skill preview with brief flash effect
    /// </summary>
    private void ShowInstantPreviewEnhanced(SkillModule skill, Vector2 playerPosition)
    {
        // Create brief flash effect for instant skills
        GameObject flashEffect = new GameObject($"InstantPreview_{skill.skillName}");
        flashEffect.transform.position = new Vector3(playerPosition.x, playerPosition.y, -0.1f);
        
        var particleSystem = flashEffect.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = skill.skillColor;
        main.startLifetime = 0.3f;
        main.startSpeed = 2f;
        main.maxParticles = 20;
        main.startSize = 0.2f;
        
        var emission = particleSystem.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 15)
        });
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        // Auto destroy
        Object.Destroy(flashEffect, 1f);
    }

    /// <summary>
    /// Enhanced support skill preview with gentle indicator
    /// </summary>
    private void ShowSupportPreviewEnhanced(SkillModule skill, Vector2 playerPosition)
    {
        // Create gentle indicator for support skills
        GameObject supportIndicator = new GameObject($"SupportPreview_{skill.skillName}");
        supportIndicator.transform.position = new Vector3(playerPosition.x, playerPosition.y, -0.1f);
        
        var particleSystem = supportIndicator.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = new Color(skill.skillColor.r, skill.skillColor.g, skill.skillColor.b, 0.6f);
        main.startLifetime = 1f;
        main.startSpeed = 0.8f;
        main.maxParticles = 12;
        main.startSize = 0.15f;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 8f;
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(1f);
        
        // Auto destroy
        Object.Destroy(supportIndicator, 1.5f);
    }

    /// <summary>
    /// Enhanced mouse world position with better accuracy
    /// </summary>
    private Vector2 GetEnhancedMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    /// <summary>
    /// Enhanced target position validation with smoother clamping
    /// </summary>
    private Vector2 GetValidTargetPositionEnhanced(Vector2 mousePos, Vector2 playerPos, SkillModule skill)
    {
        Vector2 direction = (mousePos - playerPos).normalized;
        float distance = Vector2.Distance(mousePos, playerPos);
        float maxRange = skill.range;

        if (distance <= maxRange)
        {
            return mousePos;
        }
        else
        {
            // Smooth clamping to range
            return Vector2.Lerp(playerPos, mousePos, maxRange / distance);
        }
    }

    /// <summary>
    /// Create enhanced preview sphere with better visuals
    /// </summary>
    private GameObject CreateEnhancedPreviewSphere(Vector2 position, float radius, Color color, string name)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"{name}_Enhanced_{Time.time:F2}";
        sphere.transform.position = new Vector3(position.x, position.y, -0.1f);
        sphere.transform.localScale = Vector3.one * radius * 2;
        
        // Remove collider
        var collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
        
        // Enhanced material
        SetEnhancedPreviewMaterial(sphere, color);
        AddPreviewEnhancements(sphere, color);
        
        return sphere;
    }

    /// <summary>
    /// Add visual enhancements to preview objects
    /// </summary>
    private void AddPreviewEnhancements(GameObject previewObject, Color baseColor)
    {
        // Add pulsing effect
        var pulseEffect = previewObject.AddComponent<DamageAreaPulseEffect>();
        pulseEffect.Initialize(baseColor, float.MaxValue); // Infinite duration for preview
        
        // Add subtle particle effect
        var particleSystem = previewObject.AddComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
        main.startLifetime = 1f;
        main.startSpeed = 0.5f;
        main.maxParticles = 8;
        main.startSize = 0.05f;
        
        var emission = particleSystem.emission;
        emission.rateOverTime = 5f;
        
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
    }

    /// <summary>
    /// Enhanced preview material setup
    /// </summary>
    private void SetEnhancedPreviewMaterial(GameObject previewObject, Color skillColor)
    {
        var renderer = previewObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            var previewColor = skillColor;
            previewColor.a = 0.25f; // Enhanced alpha for better visibility
            material.color = previewColor;
            
            // Enhanced transparency settings
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            
            // Add emission for glow effect
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", skillColor * 0.2f);
            
            renderer.material = material;
        }
    }

    /// <summary>
    /// Create enhanced direction line for projectiles
    /// </summary>
    private GameObject CreateEnhancedDirectionLine(Vector2 playerPosition, Vector2 direction, float range, Color skillColor)
    {
        GameObject directionLine = new GameObject("ProjectileDirectionLine_Enhanced");
        var lineRenderer = directionLine.AddComponent<LineRenderer>();
        
        // Enhanced line renderer setup
        lineRenderer.material = CreateEnhancedLineMaterial(skillColor);
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        
        // Add glow effect
        lineRenderer.material.EnableKeyword("_EMISSION");
        lineRenderer.material.SetColor("_EmissionColor", skillColor * 0.5f);
        
        return directionLine;
    }

    /// <summary>
    /// Update enhanced direction line
    /// </summary>
    private void UpdateProjectileDirectionLineEnhanced(Vector2 playerPosition, Vector2 direction, float range, Color skillColor)
    {
        var lineRenderer = currentProjectileDirectionLine.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, -0.1f);
            Vector3 endPos = startPos + (Vector3)(direction * range);
            
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            lineRenderer.material.color = skillColor;
        }
    }

    /// <summary>
    /// Create enhanced line material with glow effect
    /// </summary>
    private Material CreateEnhancedLineMaterial(Color color)
    {
        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        
        // Add emission for glow
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.3f);
        }
        
        return material;
    }

    /// <summary>
    /// Enhanced preview update with smooth transitions
    /// </summary>
    private void UpdateSkillPreviewEnhanced(SkillModule skill)
    {
        Vector2 playerPosition = transform.position;

        switch (skill.skillType)
        {
            case SkillType.Area:
                // Smooth area preview updates
                if (currentPreviewDamageArea != null)
                {
                    Vector2 mousePos = GetEnhancedMouseWorldPosition();
                    Vector2 validPos = GetValidTargetPositionEnhanced(mousePos, playerPosition, skill);
                    
                    // Smooth interpolation for better visual feedback
                    Vector3 currentPos = currentPreviewDamageArea.transform.position;
                    Vector3 targetPos = new Vector3(validPos.x, validPos.y, -0.1f);
                    currentPreviewDamageArea.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * 10f);
                }
                break;
                
            case SkillType.Projectile:
                // Smooth projectile direction updates
                if (currentProjectileDirectionLine != null)
                {
                    Vector2 mousePos = GetEnhancedMouseWorldPosition();
                    Vector2 direction = (mousePos - playerPosition).normalized;
                    UpdateProjectileDirectionEnhanced(playerPosition, direction, skill.range);
                }
                break;
        }
    }

    /// <summary>
    /// Enhanced projectile direction update
    /// </summary>
    private void UpdateProjectileDirectionEnhanced(Vector2 playerPosition, Vector2 direction, float range)
    {
        if (currentProjectileDirectionLine == null) return;
        
        Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, -0.1f);
        Vector3 endPos = startPos + (Vector3)(direction * range);
        Vector3 midPos = (startPos + endPos) / 2f;
        
        // Smooth position updates
        Vector3 currentPos = currentProjectileDirectionLine.transform.position;
        currentProjectileDirectionLine.transform.position = Vector3.Lerp(currentPos, midPos, Time.deltaTime * 15f);
        
        // Smooth rotation updates
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            currentProjectileDirectionLine.transform.rotation = Quaternion.Lerp(
                currentProjectileDirectionLine.transform.rotation, 
                targetRotation, 
                Time.deltaTime * 10f
            );
        }
    }

    /// <summary>
    /// Enhanced preview hiding with cleanup
    /// </summary>
    private void HideSkillPreviewEnhanced()
    {
        if (currentPreviewDamageArea != null)
        {
            // Add fade-out effect before hiding
            var fadeEffect = currentPreviewDamageArea.GetComponent<FadeOutEffect>();
            if (fadeEffect == null)
            {
                fadeEffect = currentPreviewDamageArea.AddComponent<FadeOutEffect>();
            }
            fadeEffect.StartFadeOut(0f, 0.2f);
            
            currentPreviewDamageArea.SetActive(false);
        }
        
        if (currentProjectileDirectionLine != null)
        {
            // Add fade-out effect for projectile line
            var fadeEffect = currentProjectileDirectionLine.GetComponent<FadeOutEffect>();
            if (fadeEffect == null)
            {
                fadeEffect = currentProjectileDirectionLine.AddComponent<FadeOutEffect>();
            }
            fadeEffect.StartFadeOut(0f, 0.2f);
            
            currentProjectileDirectionLine.SetActive(false);
        }
    }

    private void StartSkillPreview(int slotIndex)
    {
        if (isSkillActive || slotIndex >= skillSlots.Count) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // Hi?n th? vùng sát th??ng preview
        ShowSkillPreview(skill);

        isSkillActive = true;
        activeSkillSlot = slotIndex;
    }

    private void HoldSkillPreview(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // C?p nh?t vùng sát th??ng preview n?u c?n
        UpdateSkillPreview(skill);

        isSkillHeld = true;
    }

    private void EndSkillPreview(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        // ?n vùng sát th??ng preview
        HideSkillPreview();

        isSkillActive = false;
        isSkillHeld = false;
        activeSkillSlot = -1;
    }

    private void ShowSkillPreview(SkillModule skill)
    {
        Vector2 playerPosition = transform.position;
        
        // C?p nh?t v? trí và kích th??c d?a trên lo?i k? n?ng
        switch (skill.skillType)
        {
            case SkillType.Melee:
                ShowMeleePreview(skill, playerPosition);
                break;
            case SkillType.Area:
                ShowAreaPreview(skill, playerPosition);
                break;
            case SkillType.Projectile:
                ShowProjectilePreview(skill, playerPosition);
                break;
            case SkillType.Instant:
                // Instant skills không c?n preview - s? execute ngay l?p t?c
                HideAllPreviews();
                return;
            default:
                // Support không hi?n th? preview
                HideAllPreviews();
                return;
        }
    }

    private void ShowMeleePreview(SkillModule skill, Vector2 playerPosition)
    {
        // ?u tiên s? d?ng damageZonePrefab t? SkillModule
        if (skill.damageZonePrefab != null)
        {
            // S? d?ng custom prefab cho Melee
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = Object.Instantiate(skill.damageZonePrefab);
                currentPreviewDamageArea.name = "MeleePreviewArea_Custom";
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.range * 2;
        }
        else
        {
            // Fallback: T?o vùng sát th??ng preview m?c ??nh cho Melee
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentPreviewDamageArea.name = "MeleePreviewArea";
                
                // Remove collider
                var collider = currentPreviewDamageArea.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            currentPreviewDamageArea.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.range * 2;
            
            SetPreviewMaterial(currentPreviewDamageArea, skill.damageAreaColor);
        }

        currentPreviewDamageArea.SetActive(true);
    }

    private void ShowAreaPreview(SkillModule skill, Vector2 playerPosition)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 validPos = GetValidTargetPosition(mousePos, playerPosition, skill);
        
        // ?u tiên s? d?ng damageZonePrefab t? SkillModule
        if (skill.damageZonePrefab != null)
        {
            // S? d?ng custom prefab cho Area
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = Object.Instantiate(skill.damageZonePrefab);
                currentPreviewDamageArea.name = "AreaPreviewArea_Custom";
            }
            
            currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.areaRadius * 2;
        }
        else
        {
            // Fallback: T?o vùng sát th??ng preview m?c ??nh cho Area
            if (currentPreviewDamageArea == null)
            {
                currentPreviewDamageArea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentPreviewDamageArea.name = "AreaPreviewArea";
                
                // Remove collider
                var collider = currentPreviewDamageArea.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, 0);
            currentPreviewDamageArea.transform.localScale = Vector3.one * skill.areaRadius * 2;
            
            SetPreviewMaterial(currentPreviewDamageArea, skill.damageAreaColor);
        }

        currentPreviewDamageArea.SetActive(true);
    }

    private void ShowProjectilePreview(SkillModule skill, Vector2 playerPosition)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 direction = (mousePos - playerPosition).normalized;
        
        // ?u tiên s? d?ng damageZonePrefab thay vì LineRenderer cho Projectile
        if (skill.damageZonePrefab != null)
        {
            // S? d?ng custom prefab cho Projectile direction indicator
            if (currentProjectileDirectionLine == null)
            {
                currentProjectileDirectionLine = Object.Instantiate(skill.damageZonePrefab);
                currentProjectileDirectionLine.name = "ProjectileDirectionPrefab";
            }
            
            // ??t v? trí và h??ng cho custom prefab
            Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
            Vector3 endPos = startPos + (Vector3)(direction * skill.range);
            Vector3 midPos = (startPos + endPos) / 2f;
            
            currentProjectileDirectionLine.transform.position = midPos;
            
            // Xoay prefab theo h??ng
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                currentProjectileDirectionLine.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            
            // Scale theo range
            float rangeScale = skill.range / 2f;
            currentProjectileDirectionLine.transform.localScale = new Vector3(rangeScale, 0.2f, 1f);
        }
        else
        {
            // Fallback: T?o ???ng th?ng LineRenderer nh? c?
            if (currentProjectileDirectionLine == null)
            {
                currentProjectileDirectionLine = new GameObject("ProjectileDirectionLine");
                var lineRendererComponent = currentProjectileDirectionLine.AddComponent<LineRenderer>();
                
                // Thi?t l?p LineRenderer
                lineRendererComponent.material = CreateLineMaterial(skill.skillColor);
                lineRendererComponent.startWidth = 0.1f;
                lineRendererComponent.endWidth = 0.05f;
                lineRendererComponent.positionCount = 2;
                lineRendererComponent.useWorldSpace = true;
            }

            var lineRenderer = currentProjectileDirectionLine.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
                Vector3 endPos = startPos + (Vector3)(direction * skill.range);
                
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);
                lineRenderer.material.color = skill.skillColor;
            }
        }

        currentProjectileDirectionLine.SetActive(true);
        
        // ?n damage area cho Projectile vì chúng ta ch? hi?n th? ???ng bay
        if (currentPreviewDamageArea != null)
        {
            currentPreviewDamageArea.SetActive(false);
        }
    }

    private void SetPreviewMaterial(GameObject previewObject, Color skillColor)
    {
        var renderer = previewObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            var previewColor = skillColor;
            previewColor.a = 0.15f; // Alpha th?p h?n cho preview
            material.color = previewColor;
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
    }

    private Material CreateLineMaterial(Color color)
    {
        var material = new Material(Shader.Find("Sprites/Default"));
        material.color = color;
        return material;
    }

    private void HideAllPreviews()
    {
        if (currentPreviewDamageArea != null)
        {
            currentPreviewDamageArea.SetActive(false);
        }
        
        if (currentProjectileDirectionLine != null)
        {
            currentProjectileDirectionLine.SetActive(false);
        }
    }

    private void UpdateSkillPreview(SkillModule skill)
    {
        Vector2 playerPosition = transform.position;

        switch (skill.skillType)
        {
            case SkillType.Area:
                // C?p nh?t v? trí Area preview khi chu?t di chuy?n
                if (currentPreviewDamageArea != null)
                {
                    Vector2 mousePos = GetMouseWorldPosition();
                    Vector2 validPos = GetValidTargetPosition(mousePos, playerPosition, skill);
                    currentPreviewDamageArea.transform.position = new Vector3(validPos.x, validPos.y, 0);
                }
                break;
                
            case SkillType.Projectile:
                // C?p nh?t h??ng bay Projectile khi chu?t di chuy?n
                if (currentProjectileDirectionLine != null)
                {
                    Vector2 mousePos = GetMouseWorldPosition();
                    Vector2 direction = (mousePos - playerPosition).normalized;
                    
                    if (skill.damageZonePrefab != null)
                    {
                        // C?p nh?t custom prefab cho Projectile
                        Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
                        Vector3 endPos = startPos + (Vector3)(direction * skill.range);
                        Vector3 midPos = (startPos + endPos) / 2f;
                        
                        currentProjectileDirectionLine.transform.position = midPos;
                        
                        // Xoay prefab theo h??ng
                        if (direction != Vector2.zero)
                        {
                            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                            currentProjectileDirectionLine.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                        }
                    }
                    else
                    {
                        // C?p nh?t LineRenderer fallback
                        var lineRenderer = currentProjectileDirectionLine.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            Vector3 startPos = new Vector3(playerPosition.x, playerPosition.y, 0);
                            Vector3 endPos = startPos + (Vector3)(direction * skill.range);
                            
                            lineRenderer.SetPosition(0, startPos);
                            lineRenderer.SetPosition(1, endPos);
                        }
                    }
                }
                break;
        }
    }

    private void HideSkillPreview()
    {
        HideAllPreviews();
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }

    private Vector2 GetValidTargetPosition(Vector2 mousePos, Vector2 playerPos, SkillModule skill)
    {
        Vector2 direction = (mousePos - playerPos).normalized;
        float distance = Vector2.Distance(mousePos, playerPos);
        float maxRange = skill.range;

        if (distance <= maxRange)
        {
            return mousePos;
        }
        else
        {
            return playerPos + direction * maxRange;
        }
    }

    public void ActivateSkill(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            return;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) 
        {
            return;
        }
        
        var executor = slot.executor;
        
        if (executor == null)
        {
            return;
        }
        
        if (cooldownTimers.ContainsKey(executor)) 
        {
            return;
        }
        
        if (!executor.CanExecute(player)) 
        {
            return;
        }
        
        var skill = slot.equippedSkill;
        Vector2 targetPos = Vector2.zero;
        
        // Instant skills don't need mouse position
        if (skill.skillType != SkillType.Instant)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 10f;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            targetPos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        }
        else
        {
            // For instant skills, use player position as target
            targetPos = player.transform.position;
        }
        
        executor.Execute(player, targetPos);
        cooldownTimers[executor] = executor.GetCooldown();
    }

    public bool EquipSkill(int slotIndex, SkillModule skill)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            return false;
        }
        
        if (skill == null)
        {
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked) 
        {
            return false;
        }
        
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            return false;
        }

        // Remove reference to UnifiedSkillSystem as it's been removed
        
        bool success = slot.EquipSkill(skill);
        if (success)
        {
            OnSkillEquipped?.Invoke(slotIndex, skill);
        }
        
        return success;
    }

    public void UnequipSkill(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) return;
        
        var slot = skillSlots[slotIndex];
        var skillName = slot.HasSkill() ? slot.equippedSkill.skillName : "Unknown";
        slot.UnequipSkill();
        
        OnSkillUnequipped?.Invoke(slotIndex);
    }

    public void UpdateUnlockedSlots()
    {
        int playerLevel = GetPlayerLevel();
        int unlockedSlots = Mathf.Min((playerLevel / levelsPerSlot) + 1, maxSkillSlots);
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            bool wasUnlocked = skillSlots[i].isUnlocked;
            bool shouldBeUnlocked = i < unlockedSlots;
            
            if (!wasUnlocked && shouldBeUnlocked)
            {
                skillSlots[i].UnlockSlot();
                OnSlotUnlocked?.Invoke(i);
            }
        }
    }

    public int GetPlayerLevel()
    {
        return PlayerPrefs.GetInt("PlayerLevel", 1);
    }

    public void SetPlayerLevel(int level)
    {
        PlayerPrefs.SetInt("PlayerLevel", level);
        UpdateUnlockedSlots();
    }

    public List<SkillModule> GetAvailableSkills()
    {
        return availableSkills.Where(skill => GetPlayerLevel() >= skill.requiredLevel).ToList();
    }

    public List<SkillSlot> GetUnlockedSlots()
    {
        return skillSlots.Where(slot => slot.isUnlocked).ToList();
    }

    public SkillSlot GetSlot(int index)
    {
        if (index >= 0 && index < skillSlots.Count)
            return skillSlots[index];
        return null;
    }

    public float GetSkillCooldown(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.HasSkill()) return 0f;
        
        if (cooldownTimers.ContainsKey(slot.executor))
            return cooldownTimers[slot.executor];
        
        return 0f;
    }

    public bool IsSkillOnCooldown(int slotIndex)
    {
        return GetSkillCooldown(slotIndex) > 0f;
    }

    public bool IsSkillEquippedInLegacySystem(SkillModule skill)
    {
        if (skill == null) return false;
        
        return skillSlots.Any(slot => slot.HasSkill() && slot.equippedSkill == skill);
    }

    public int GetSkillSlotIndex(SkillModule skill)
    {
        if (skill == null) return -1;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].HasSkill() && skillSlots[i].equippedSkill == skill)
                return i;
        }
        return -1;
    }

    public void SetLegacyHotkeysEnabled(bool enabled)
    {
        enableLegacyHotkeys = enabled;
    }

    public void AddAvailableSkill(SkillModule skill)
    {
        if (!availableSkills.Contains(skill))
        {
            availableSkills.Add(skill);
        }
    }

    public void RemoveAvailableSkill(SkillModule skill)
    {
        availableSkills.Remove(skill);
    }

    // Save/Load system
    [System.Serializable]
    public class SkillSaveData
    {
        public int[] equippedSkillIDs;
        public int playerLevel;
        public bool legacyHotkeysEnabled;
    }

    public void SaveSkillSetup()
    {
        var saveData = new SkillSaveData();
        saveData.playerLevel = GetPlayerLevel();
        saveData.legacyHotkeysEnabled = enableLegacyHotkeys;
        saveData.equippedSkillIDs = new int[skillSlots.Count];
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].HasSkill())
            {
                saveData.equippedSkillIDs[i] = availableSkills.IndexOf(skillSlots[i].equippedSkill);
            }
            else
            {
                saveData.equippedSkillIDs[i] = -1;
            }
        }
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("SkillSetup", json);
    }

    public void LoadSkillSetup()
    {
        string json = PlayerPrefs.GetString("SkillSetup", "");
        if (string.IsNullOrEmpty(json)) return;
        
        var saveData = JsonUtility.FromJson<SkillSaveData>(json);
        SetPlayerLevel(saveData.playerLevel);
        SetLegacyHotkeysEnabled(saveData.legacyHotkeysEnabled);
        
        for (int i = 0; i < saveData.equippedSkillIDs.Length && i < skillSlots.Count; i++)
        {
            int skillID = saveData.equippedSkillIDs[i];
            if (skillID >= 0 && skillID < availableSkills.Count)
            {
                EquipSkill(i, availableSkills[skillID]);
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSkillSetup();
        }
    }

    public bool UpdateSlotHotkey(int slotIndex, KeyCode newKey)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Count)
        {
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        KeyCode oldKey = slot.hotkey;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (i != slotIndex && skillSlots[i].UsesHotkey(newKey))
            {
                return false;
            }
        }
        
        slot.UpdateHotkey(newKey);
        return true;
    }
    
    public SkillSlot GetSlotByHotkey(KeyCode key)
    {
        return skillSlots.FirstOrDefault(slot => slot.UsesHotkey(key));
    }
    
    public int GetSlotIndexByHotkey(KeyCode key)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].UsesHotkey(key))
                return i;
        }
        return -1;
    }
    
    public bool AssignSkillToHotkey(SkillModule skill, KeyCode key)
    {
        if (skill == null)
        {
            return false;
        }
        
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            return false;
        }
        
        var existingSlot = GetSlotByHotkey(key);
        
        if (existingSlot != null && existingSlot.isUnlocked)
        {
            if (existingSlot.HasSkill())
            {
                existingSlot.UnequipSkill();
            }
            
            bool success = existingSlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(existingSlot.slotIndex, skill);
            }
            return success;
        }
        
        var emptySlot = skillSlots.FirstOrDefault(s => s.isUnlocked && !s.HasSkill());
        if (emptySlot != null)
        {
            emptySlot.UpdateHotkey(key);
            bool success = emptySlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(emptySlot.slotIndex, skill);
            }
            return success;
        }
        
        return false;
    }
    
    public int GetMaxSupportedSlots()
    {
        return 45;
    }
    
    public string GetHotkeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            case KeyCode.Alpha0: return "0";
            case KeyCode.F1: return "F1";
            case KeyCode.F2: return "F2";
            case KeyCode.F3: return "F3";
            case KeyCode.F4: return "F4";
            case KeyCode.F5: return "F5";
            case KeyCode.F6: return "F6";
            case KeyCode.F7: return "F7";
            case KeyCode.F8: return "F8";
            case KeyCode.F9: return "F9";
            case KeyCode.F10: return "F10";
            case KeyCode.F11: return "F11";
            case KeyCode.F12: return "F12";
            case KeyCode.Mouse3: return "M1";
            case KeyCode.Mouse4: return "M2";
            case KeyCode.Mouse5: return "M3";
            case KeyCode.Mouse6: return "M4";
            case KeyCode.None: return "---";
            default: return key.ToString();
        }
    }
}