using UnityEngine;
using System.Collections;
using System.Linq;

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
