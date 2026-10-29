using UnityEngine;
using System.Collections;
using System.Linq;

public abstract partial class SkillExecutorBase
{
    /// <summary>
    /// FIXED: Enhanced enemy detection with bulletproof player exclusion
    /// </summary>
    protected Character[] FindEnemiesInRange(Vector2 center, float range, Character caster = null)
    {
        var enemies = new System.Collections.Generic.List<Character>();
        
        // Find ALL Characters in scene and filter properly
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character == null) continue;
            
            // CRITICAL FIX: Multiple layers of player detection
            if (IsPlayerCharacter(character, caster)) continue;
            
            // Calculate distance
            float distance = Vector2.Distance(center, character.transform.position);
            
            if (distance <= range)
            {
                // Check if enemy is alive
                if (character.health != null && character.health.currentValue > 0)
                {
                    enemies.Add(character);
                }
            }
        }
        
        return enemies.ToArray();
    }
    
    /// <summary>
    /// BULLETPROOF player detection method
    /// </summary>
    protected bool IsPlayerCharacter(Character character, Character caster = null)
    {
        // Method 1: Same as caster - CRITICAL for preventing self-damage
        if (caster != null && character == caster) 
        {
            Debug.Log($"??? SELF-DAMAGE PREVENTED: {character.name} == {caster.name}");
            return true;
        }
        
        // Method 2: Has PlayerController component
        var playerController = character.GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
        {
            Debug.Log($"?? PLAYER DETECTED: {character.name} has PlayerController");
            return true;
        }
        
        // Method 3: Check AttackableCharacter component
        var attackable = character.GetComponent<AttackableCharacter>();
        if (attackable != null && !attackable.CanBeAttacked())
        {
            Debug.Log($"??? NON-ATTACKABLE: {character.name} cannot be attacked");
            return true;
        }
        
        // Method 4: Check GameObject name patterns
        string objName = character.gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("hero") || objName.Contains("character"))
        {
            Debug.Log($"??? PLAYER NAME PATTERN: {character.name} matches player naming");
            return true;
        }
            
        // Method 5: Check tag
        if (character.gameObject.CompareTag("Player"))
        {
            Debug.Log($"??? PLAYER TAG: {character.name} has Player tag");
            return true;
        }
        
        // Method 6: Check for CoreEnemy component (enemies shouldn't target other enemies)
        var coreEnemy = character.GetComponent<CoreEnemy>();
        if (coreEnemy != null && caster != null)
        {
            var casterCoreEnemy = caster.GetComponent<CoreEnemy>();
            if (casterCoreEnemy != null)
            {
                Debug.Log($"?? ENEMY-TO-ENEMY PREVENTION: {caster.name} (enemy) won't target {character.name} (enemy)");
                return true; // Treat as "player" to prevent enemy-to-enemy damage
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get raw mouse position without targeting system interference
    /// </summary>
    protected Vector2 GetRawMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // Camera distance
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 rawMousePos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        return rawMousePos;
    }
    
    /// <summary>
    /// Get valid target position with range validation
    /// </summary>
    protected Vector2 GetValidTargetPosition(Vector2 mouseTargetPosition, Character user)
    {
        Vector2 userPos = user.transform.position;
        Vector2 direction = (mouseTargetPosition - userPos).normalized;
        float maxDistance = Module.range;
        
        // For area skills, use EXACT mouse position (clamped to range)
        if (Module.skillType == SkillType.Area)
        {
            float distanceToMouse = Vector2.Distance(userPos, mouseTargetPosition);
            if (distanceToMouse <= maxDistance)
            {
                return mouseTargetPosition;
            }
            else
            {
                Vector2 clampedPos = userPos + direction * maxDistance;
                return clampedPos;
            }
        }
        
        // For other skills, normal range validation
        Vector2 validTarget = userPos + direction * Mathf.Min(Vector2.Distance(userPos, mouseTargetPosition), maxDistance);
        return validTarget;
    }
    
    protected Character[] FindSkillTargetsInRange(Vector2 center, float range, Character caster)
    {
        var targets = new System.Collections.Generic.List<Character>();
        var allCharacters = Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (var character in allCharacters)
        {
            if (character == null || character == caster) continue;
            
            // CRITICAL FIX: Use comprehensive enemy/player detection
            bool isValidTarget = false;
            
            // If caster is player, target only enemies
            if (caster.gameObject.CompareTag("Player"))
            {
                // Target must be tagged as Enemy and NOT be a player
                if (character.gameObject.CompareTag("Enemy") && !IsPlayerCharacter(character, caster))
                {
                    isValidTarget = true;
                }
            }
            // If caster is enemy, target only player
            else if (caster.gameObject.CompareTag("Enemy"))
            {
                // Target must be a player (tagged or has PlayerController)
                if (character.gameObject.CompareTag("Player") || IsPlayerCharacter(character, caster))
                {
                    isValidTarget = true;
                }
            }
            // Additional safety: if caster has no tag, use the player detection method
            else
            {
                // Use the comprehensive player detection
                bool casterIsPlayer = IsPlayerCharacter(caster, null);
                bool targetIsPlayer = IsPlayerCharacter(character, caster);
                
                // Players target enemies, enemies target players
                if (casterIsPlayer && !targetIsPlayer)
                {
                    isValidTarget = true;
                }
                else if (!casterIsPlayer && targetIsPlayer)
                {
                    isValidTarget = true;
                }
            }
            
            if (!isValidTarget) continue;
            
            // Check if target is alive
            if (character.health != null && character.health.currentValue > 0)
            {
                float distance = Vector2.Distance(center, character.transform.position);
                if (distance <= range)
                {
                    targets.Add(character);
                }
            }
        }
        
        return targets.ToArray();
    }
}
