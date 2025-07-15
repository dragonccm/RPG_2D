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
        
        // Auto-detect if this is a player
        if (GetComponent<PlayerController>() != null)
        {
            isPlayer = true;
            canBeAttacked = false; // Players cannot be attacked by default
        }
    }
    
    #region IAttackable Implementation
    
    public bool CanBeAttacked()
    {
        if (!canBeAttacked) return false;
        
        // Don't attack players
        if (isPlayer) return false;
        
        // Don't attack dead characters
        if (character != null && character.health != null)
        {
            return character.health.currentValue > 0;
        }
        
        return true;
    }
    
    public Vector2 GetPosition()
    {
        return transform.position;
    }
    
    public string GetName()
    {
        return gameObject.name;
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Set whether this character can be attacked
    /// </summary>
    public void SetCanBeAttacked(bool attackable)
    {
        canBeAttacked = attackable;
    }
    
    /// <summary>
    /// Mark this character as player
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
    /// Get the Character component
    /// </summary>
    public Character GetCharacter()
    {
        return character;
    }
    
    #endregion
    
    #region Editor Helpers
    
    void OnValidate()
    {
        // Auto-setup based on components
        if (GetComponent<PlayerController>() != null)
        {
            isPlayer = true;
            canBeAttacked = false;
        }
    }
    
    #endregion
}