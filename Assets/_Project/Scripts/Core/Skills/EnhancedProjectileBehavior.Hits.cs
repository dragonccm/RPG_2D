using UnityEngine;
using System.Collections;
using System.Linq;

public partial class EnhancedProjectileBehavior
{
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
        enemy.TakeDamage(finalDamage, isCritical);
        
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
            VfxService.PlayPrefabAimed(skillModule.effectPrefab, position, impactDirection, 1f,
                skillModule.damageAreaDisplayTime > 0f ? skillModule.damageAreaDisplayTime : -1f);
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
        Destroy(hitEffect, 1.5f);
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
