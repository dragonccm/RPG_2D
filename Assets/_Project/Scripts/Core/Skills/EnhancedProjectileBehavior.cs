using UnityEngine;
using System.Collections;
using System.Linq;

// ENHANCED Projectile Behavior (partial: vòng đời / dò mục tiêu / va chạm & hiệu ứng)
public partial class EnhancedProjectileBehavior : MonoBehaviour
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
    
}
