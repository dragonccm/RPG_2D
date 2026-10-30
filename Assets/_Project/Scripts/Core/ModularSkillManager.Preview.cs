using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Vẽ vùng xem trước kỹ năng theo loại (cận chiến, vùng, đạn, tức thời, hỗ trợ)
public partial class ModularSkillManager
{
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
    /// Vùng xem trước kỹ năng: telegraph pixel-art tồn tại tới khi người gọi Destroy.
    /// </summary>
    private GameObject CreateEnhancedPreviewSphere(Vector2 position, float radius, Color color, string name)
    {
        color.a = Mathf.Clamp(color.a, 0.35f, 0.7f);
        var preview = VfxService.Telegraph(new Vector3(position.x, position.y, -0.1f), radius, 0f, color);
        if (preview != null) preview.name = $"{name}_Preview_{Time.time:F2}";
        return preview;
    }

    /// <summary>
    /// Create enhanced direction line for projectiles
    /// </summary>
    private GameObject CreateEnhancedDirectionLine(Vector2 playerPosition, Vector2 direction, float range, Color skillColor)
    {
        var go = new GameObject("ProjectileDirectionLine_Enhanced");
        go.AddComponent<PixelAimLine>();
        return go;
    }

    /// <summary>
    /// Update enhanced direction line
    /// </summary>
    private void UpdateProjectileDirectionLineEnhanced(Vector2 playerPosition, Vector2 direction, float range, Color skillColor)
    {
        var aim = currentProjectileDirectionLine != null ? currentProjectileDirectionLine.GetComponent<PixelAimLine>() : null;
        if (aim != null) aim.SetLine(playerPosition, playerPosition + direction * range, skillColor);
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
}
