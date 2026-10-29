using UnityEngine;
using System.Collections;
using System.Linq;

public partial class EnhancedProjectileBehavior
{
    private Character[] FindEnemiesInCollisionRange()
    {
        var enemiesInRange = new System.Collections.Generic.List<Character>();
        
        // ENHANCED: For enemy projectiles, only target players
        if (IsEnemyCaster(caster))
        {
            // Find all Character components and only target players
            var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                // CRITICAL: Skip the caster to prevent self-damage
                if (character.gameObject == caster.gameObject) 
                {
                    Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
                    continue;
                }
                
                // CRITICAL: Skip if same Character instance
                if (character == caster) 
                {
                    Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: Same caster instance");
                    continue;
                }
                
                // ENHANCED: Only target players for enemy projectiles
                if (!IsPlayerCharacter(character)) continue;
                
                if (character.health == null || character.health.currentValue <= 0) continue;
                
                float distance = Vector2.Distance(transform.position, character.transform.position);
                if (distance <= hitRadius)
                {
                    enemiesInRange.Add(character);
                }
            }
            
            Debug.Log($"?? ENEMY PROJECTILE: Found {enemiesInRange.Count} PLAYER targets in range {hitRadius}");
            return enemiesInRange.ToArray();
        }
        
        // Original logic for player projectiles - target enemies
        var playerCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in playerCharacters)
        {
            // CRITICAL: Skip the caster to prevent self-damage
            if (character.gameObject == caster.gameObject) 
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (Character): {character.name} == {caster.name}");
                continue;
            }
            
            // Skip if same Character instance
            if (character == caster) 
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (Character instance): Same caster");
                continue;
            }
            
            if (IsPlayerCharacter(character)) continue;
            
            if (character.health == null || character.health.currentValue <= 0) continue;
            
            float distance = Vector2.Distance(transform.position, character.transform.position);
            if (distance <= hitRadius)
            {
                enemiesInRange.Add(character);
            }
        }
        
        Debug.Log($"?? PROJECTILE COLLISION: Found {enemiesInRange.Count} Character targets in range {hitRadius}");
        
        return enemiesInRange.ToArray();
    }
    
    /// <summary>
    /// ENHANCED: Check if the caster is an enemy
    /// </summary>
    private bool IsEnemyCaster(Character caster)
    {
        if (caster == null) return false;
        
        // Check for CoreEnemy component
        return caster.GetComponent<CoreEnemy>() != null || 
               caster.gameObject.tag == "Enemy" ||
               caster.gameObject.layer == LayerMask.NameToLayer("Enemy");
    }
    
    /// <summary>
    /// ENHANCED: Find the player target for enemy projectiles
    /// </summary>
    private Character FindPlayerTarget()
    {
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in allCharacters)
        {
            if (IsPlayerCharacter(character))
            {
                return character;
            }
        }
        return null;
    }
    
    /// <summary>
    /// ENHANCED: Find only player targets within collision range for enemy projectiles
    /// </summary>
    private Character[] FindPlayerTargetsInRange()
    {
        var playerTargets = new System.Collections.Generic.List<Character>();
        
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (var character in allCharacters)
        {
            // CRITICAL: Skip the caster to prevent self-damage
            if (character.gameObject == caster.gameObject) 
            {
                Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
                continue;
            }
            
            // CRITICAL: Skip if same Character instance
            if (character == caster) 
            {
                Debug.Log($"??? ENEMY PROJECTILE SELF-DAMAGE PREVENTED: Same caster instance");
                continue;
            }
            
            // Only target actual players
            if (!IsPlayerCharacter(character)) continue;
            
            if (character.health == null || character.health.currentValue <= 0) continue;
            
            float distance = Vector2.Distance(transform.position, character.transform.position);
            if (distance <= hitRadius)
            {
                playerTargets.Add(character);
            }
        }
        
        Debug.Log($"?? ENEMY PROJECTILE: Found {playerTargets.Count} PLAYER targets in range {hitRadius}");
        return playerTargets.ToArray();
    }
    
    /// <summary>
    /// ENHANCED: Direct IDamageable collision detection - now only targets players for enemy projectiles
    /// </summary>
    private IDamageable[] FindIDamageableTargetsInRange()
    {
        var targets = new System.Collections.Generic.List<IDamageable>();
        
        // ENHANCED: For enemy projectiles, only target players
        if (IsEnemyCaster(caster))
        {
            // Find all Character components (players)
            var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                if (IsPlayerCharacter(character))
                {
                    if (character.health == null || character.health.currentValue <= 0) continue;
                    
                    float distance = Vector2.Distance(transform.position, character.transform.position);
                    if (distance <= hitRadius)
                    {
                        targets.Add(character);
                    }
                }
            }
            
            Debug.Log($"?? ENEMY PROJECTILE: Found {targets.Count} PLAYER targets in range {hitRadius}");
            return targets.ToArray();
        }
        
        // Original logic for player projectiles
        var allDamageables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(mb => mb is IDamageable && mb.GetComponent<Character>() == null)
            .Cast<IDamageable>()
            .ToArray();
        
        foreach (var damageable in allDamageables)
        {
            var damageableComponent = damageable as MonoBehaviour;
            if (damageableComponent == null) continue;
            
            // CRITICAL: Check if this is the caster itself - prevent self-damage
            if (caster != null && damageableComponent.gameObject == caster.gameObject) 
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (IDamageable): {damageableComponent.name} == {caster.name}");
                continue;
            }
            
            // CRITICAL: Check if this is the caster's Character component
            if (caster != null && damageableComponent.GetComponent<Character>() == caster)
            {
                Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED (Character): Same caster");
                continue;
            }
            
            // Check if it's a CoreEnemy (enemy shouldn't hit enemy)
            var coreEnemy = damageableComponent.GetComponent<CoreEnemy>();
            if (coreEnemy != null && caster != null)
            {
                var casterCoreEnemy = caster.GetComponent<CoreEnemy>();
                if (casterCoreEnemy != null)
                {
                    Debug.Log($"?? PROJECTILE ENEMY-TO-ENEMY PREVENTION: {caster.name} projectile won't hit {damageableComponent.name} (both CoreEnemy)");
                    continue; // Skip enemy-to-enemy damage
                }
            }
            
            // Skip if target is dead
            if (damageable.IsDead) continue;
            
            float distance = Vector2.Distance(transform.position, damageableComponent.transform.position);
            if (distance <= hitRadius)
            {
                targets.Add(damageable);
            }
        }
        
        Debug.Log($"?? PROJECTILE COLLISION: Found {targets.Count} IDamageable targets in range {hitRadius}");
        
        return targets.ToArray();
    }
    
    /// <summary>
    /// BULLETPROOF player detection for projectiles - Enhanced to prevent self-damage
    /// </summary>
    private bool IsPlayerCharacter(Character character)
    {
        // CRITICAL: Same GameObject as caster - prevent self-damage
        if (character.gameObject == caster.gameObject) 
        {
            Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
            return true;
        }
        
        // CRITICAL: Same Character instance as caster - prevent self-damage
        if (character == caster) 
        {
            Debug.Log($"??? PROJECTILE SELF-DAMAGE PREVENTED: Same Character instance {character.name}");
            return true;
        }
        
        // ENHANCED: Check if this is an enemy - enemies should NOT be treated as players
        var coreEnemy = character.GetComponent<CoreEnemy>();
        if (coreEnemy != null)
        {
            Debug.Log($"?? PROJECTILE ENEMY DETECTED: {character.name} is an enemy (CoreEnemy)");
            return false; // This is an enemy, not a player
        }
        
        // Check for enemy tag
        if (character.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"?? PROJECTILE ENEMY TAG: {character.name} has Enemy tag");
            return false; // This is an enemy, not a player
        }
        
        // Check for enemy layer
        if (character.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log($"?? PROJECTILE ENEMY LAYER: {character.name} on Enemy layer");
            return false; // This is an enemy, not a player
        }
        
        // Has PlayerController - use proper type checking
        var playerController = character.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log($"?? PROJECTILE PLAYER DETECTED: {character.name} has PlayerController");
            return true;
        }
        
        // Player tag check - most reliable
        if (character.gameObject.CompareTag("Player"))
        {
            Debug.Log($"?? PROJECTILE PLAYER TAG: {character.name} has Player tag");
            return true;
        }
        
        // Player layer check (Layer 6 = Player)
        if (character.gameObject.layer == 6)
        {
            Debug.Log($"?? PROJECTILE PLAYER LAYER: {character.name} on Player layer");
            return true;
        }
        
        // Default: if not explicitly identified as player or enemy, assume it's not a player
        Debug.Log($"?? PROJECTILE CHARACTER TYPE: {character.name} is neither player nor enemy");
        return false;
    }
    
}
