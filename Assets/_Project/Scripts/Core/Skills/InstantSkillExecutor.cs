using UnityEngine;
using System.Collections;
using System.Linq;

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
        Object.Destroy(instantEffect, 2.5f);
    }
}
