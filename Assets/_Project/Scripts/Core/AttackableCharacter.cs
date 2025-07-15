using UnityEngine;

/// <summary>
/// Extension cho Character ?? implement IAttackable interface
/// Dùng ?? nh?n di?n enemy cho cursor system
/// </summary>
public class AttackableCharacter : MonoBehaviour, IAttackable
{
    private Character character;
    
    [Header("Attackable Settings")]
    [SerializeField] private bool canBeAttacked = true;
    [SerializeField] private bool isPlayer = false;
    
    void Awake()
    {
        character = GetComponent<Character>();
        
        // BULLETPROOF auto-detection of player status
        DetectPlayerStatus();
    }
    
    /// <summary>
    /// ENHANCED player detection with multiple methods
    /// </summary>
    private void DetectPlayerStatus()
    {
        // Method 1: Check for PlayerController component
        var playerController = GetComponent<MonoBehaviour>();
        if (playerController != null && playerController.GetType().Name == "PlayerController")
        {
            isPlayer = true;
            canBeAttacked = false;
            return;
        }
        
        // Method 2: Check GameObject name patterns
        string objName = gameObject.name.ToLower();
        if (objName.Contains("player") || objName.Contains("hero") || objName.Contains("character") && !objName.Contains("enemy"))
        {
            isPlayer = true;
            canBeAttacked = false;
            return;
        }
        
        // Method 3: Check tag
        if (gameObject.CompareTag("Player"))
        {
            isPlayer = true;
            canBeAttacked = false;
            return;
        }
        
        // Method 4: Check layer
        if (gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isPlayer = true;
            canBeAttacked = false;
            return;
        }
        
        // Default: If none of above, assume it's an enemy that can be attacked
        if (!isPlayer)
        {
            canBeAttacked = true;
        }
    }
    
    public bool CanBeAttacked()
    {
        if (!canBeAttacked) return false;
        
        // Double-check: Never allow players to be attacked
        if (isPlayer) return false;
        
        // Triple-check: Runtime detection
        if (HasPlayerController()) return false;
        
        // Don't attack dead characters
        if (character != null && character.health != null)
        {
            return character.health.currentValue > 0;
        }
        
        return true;
    }
    
    /// <summary>
    /// Runtime check for PlayerController
    /// </summary>
    private bool HasPlayerController()
    {
        var playerController = GetComponent<MonoBehaviour>();
        return playerController != null && playerController.GetType().Name == "PlayerController";
    }
    
    public Vector2 GetPosition()
    {
        return transform.position;
    }
    
    public string GetName()
    {
        return gameObject.name;
    }
    
    /// <summary>
    /// Force set attackable status (with safety checks)
    /// </summary>
    public void SetCanBeAttacked(bool attackable)
    {
        // Safety: Never allow players to be set as attackable
        if (isPlayer || HasPlayerController())
        {
            canBeAttacked = false;
            return;
        }
        
        canBeAttacked = attackable;
    }
    
    /// <summary>
    /// Mark this character as player (with automatic protections)
    /// </summary>
    public void SetAsPlayer(bool player)
    {
        isPlayer = player;
        if (player)
        {
            canBeAttacked = false;
        }
    }
    
    /// <summary>
    /// Force refresh player detection (useful for dynamic objects)
    /// </summary>
    public void RefreshPlayerDetection()
    {
        DetectPlayerStatus();
    }
    
    public Character GetCharacter()
    {
        return character;
    }
    
    /// <summary>
    /// Public getter for debugging
    /// </summary>
    public bool IsPlayer()
    {
        return isPlayer;
    }
    
    void OnValidate()
    {
        // Auto-setup in editor
        if (Application.isPlaying)
        {
            DetectPlayerStatus();
        }
    }
    
    /// <summary>
    /// Debug info for inspector
    /// </summary>
    [ContextMenu("Debug Attackable Status")]
    private void DebugStatus()
    {
        RefreshPlayerDetection();
        
        Debug.Log($"=== AttackableCharacter Debug: {gameObject.name} ===");
        Debug.Log($"Is Player: {isPlayer}");
        Debug.Log($"Can Be Attacked: {canBeAttacked}");
        Debug.Log($"Can Be Attacked (Method): {CanBeAttacked()}");
        Debug.Log($"Has PlayerController: {HasPlayerController()}");
        Debug.Log($"GameObject Tag: {gameObject.tag}");
        Debug.Log($"GameObject Layer: {LayerMask.LayerToName(gameObject.layer)}");
        
        if (character != null && character.health != null)
        {
            Debug.Log($"Health: {character.health.currentValue}/{character.health.maxValue}");
        }
    }
}