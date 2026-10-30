using UnityEngine;
using System.Collections;
using System.Linq;

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
            enemy.TakeDamage(finalDamage, isCritical);
            
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
            ShowDamageAreaAtExactPosition(targetPosition, Module.areaRadius, "AreaDamageIndicator_MouseClick");
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
