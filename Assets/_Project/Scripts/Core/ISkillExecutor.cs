using UnityEngine;

/// <summary>
/// Interface for all skill executors in the enhanced skill system
/// Defines the contract that all skill executors must implement
/// </summary>
public interface ISkillExecutor
{
    /// <summary>
    /// Get the skill module associated with this executor
    /// </summary>
    SkillModule Module { get; }
    
    /// <summary>
    /// Execute the skill with the given user and target position
    /// </summary>
    /// <param name="user">The character using the skill</param>
    /// <param name="targetPosition">The target position for the skill</param>
    void Execute(Character user, Vector2 targetPosition);
    
    /// <summary>
    /// Check if the skill can be executed by the given user
    /// </summary>
    /// <param name="user">The character attempting to use the skill</param>
    /// <returns>True if the skill can be executed, false otherwise</returns>
    bool CanExecute(Character user);
    
    /// <summary>
    /// Get the cooldown time for this skill
    /// </summary>
    /// <returns>Cooldown time in seconds</returns>
    float GetCooldown();
    
    /// <summary>
    /// Get the mana cost for this skill
    /// </summary>
    /// <returns>Mana cost</returns>
    float GetManaCost();
    
    /// <summary>
    /// Show damage area preview at the specified position
    /// </summary>
    /// <param name="position">Position to show the damage area</param>
    void ShowDamageArea(Vector2 position);
    
    /// <summary>
    /// Update damage area preview position
    /// </summary>
    /// <param name="position">New position for the damage area</param>
    void UpdateDamageArea(Vector2 position);
    
    /// <summary>
    /// Hide the damage area preview
    /// </summary>
    void HideDamageArea();
}