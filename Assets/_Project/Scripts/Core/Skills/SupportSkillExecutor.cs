using UnityEngine;
using System.Collections;
using System.Linq;

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
        Object.Destroy(supportEffect, Module.damageAreaDisplayTime);
    }
}
