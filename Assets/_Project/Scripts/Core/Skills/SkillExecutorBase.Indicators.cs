using UnityEngine;
using System.Collections;
using System.Linq;

public abstract partial class SkillExecutorBase
{
    /// <summary>
    /// Enhanced damage area visualization v?i auto-generation v� custom prefab support
    /// </summary>
    private GameObject currentMeleeIndicator;

    protected void ShowDamageAreaAtExactPosition(Vector2 exactPosition, float radius, string indicatorName = "DamageAreaIndicator")
    {
        // H?y hi?u ?ng c? n?u c�n t?n t?i
        if (currentMeleeIndicator != null)
        {
            Object.Destroy(currentMeleeIndicator);
            currentMeleeIndicator = null;
        }
        GameObject indicator = null;
        
        // ?u ti�n s? d?ng custom prefab t? SkillModule
        if (Module.damageZonePrefab != null)
        {
            indicator = Object.Instantiate(Module.damageZonePrefab);
            indicator.name = $"{indicatorName}_Custom_{Time.time:F2}";
            
            // Set position v� scale cho custom prefab
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

    private GameObject CreateEnhancedDamageZoneIndicator(Vector2 position, float radius, string indicatorName)
    {
        var color = Module.damageAreaColor;
        color.a = Mathf.Clamp(color.a, 0.35f, 0.8f);
        var indicator = VfxService.Telegraph(new Vector3(position.x, position.y, -0.1f), radius,
            Mathf.Max(0.2f, Module.damageAreaDisplayTime), color);
        if (indicator != null) indicator.name = $"{indicatorName}_EXACT_{Time.time:F2}";
        return indicator;
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

}
