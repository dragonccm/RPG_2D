using UnityEngine;
using System.Collections;
using System.Linq;

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
        
        // DEBUG: Log target detection results
        Debug.Log($"??? MELEE SKILL: {Module.skillName} by {user.name} (Tag: {user.gameObject.tag}) found {enemies.Length} targets");
        foreach (var enemy in enemies)
        {
            Debug.Log($"  Target: {enemy.name} (Tag: {enemy.gameObject.tag})");
        }
        
        // Enhanced feedback for no targets found
        if (enemies.Length == 0)
        {
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
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            // Calculate damage with enhanced critical system
            float finalDamage = CalculateEnhancedDamage(Module.damage, Module.criticalChance, Module.criticalMultiplier);
            bool isCritical = Random.Range(0f, 1f) < Module.criticalChance;
            
            // Apply damage with enhanced feedback
            Debug.Log($"?? MELEE HIT: {user.name} deals {finalDamage} damage to {enemy.name}");
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
            
            // Create individual impact effects for each enemy v?i v? tr� ch�nh x�c
            Vector3 impactDirection = (enemy.transform.position - user.transform.position).normalized;
            CreateImpactEffect(enemy.transform.position, impactDirection, enemy.gameObject);
            CreateIndividualHitEffect(enemy.transform.position, isCritical);
        }
        
        // Play impact sound
        PlayImpactSound(user);
        
        // Create main visual effect t?i v? tr� user
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
        // Kh�ng t?o damage area trong ShowDamageArea ?? tr�nh tr�ng l?p
        // Damage area s? ???c t?o trong Execute() t?i v? tr� player
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
