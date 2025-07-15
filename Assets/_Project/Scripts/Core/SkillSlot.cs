using UnityEngine;

// Interface cho skill executors
public interface ISkillExecutor
{
    SkillModule Module { get; }
    void Execute(Character user, Vector2 targetPosition);
    bool CanExecute(Character user);
    float GetCooldown();
    float GetManaCost();
    void ShowDamageArea(Vector2 position);
    void UpdateDamageArea(Vector2 position);
    void HideDamageArea();
}

// Enhanced SkillSlot with dynamic hotkey support
[System.Serializable]
public class SkillSlot
{
    public int slotIndex;
    public SkillModule equippedSkill;
    public ISkillExecutor executor;
    public KeyCode hotkey;
    public bool isUnlocked;
    
    public SkillSlot(int index, KeyCode key)
    {
        slotIndex = index;
        hotkey = key;
        isUnlocked = false;
        equippedSkill = null;
        executor = null;
    }
    
    public bool EquipSkill(SkillModule skill)
    {
        if (!isUnlocked || skill == null) return false;
        
        equippedSkill = skill;
        executor = skill.CreateExecutor();
        return true;
    }
    
    public void UnequipSkill()
    {
        equippedSkill = null;
        executor = null;
    }
    
    public bool HasSkill()
    {
        return equippedSkill != null && executor != null;
    }
    
    public void UnlockSlot()
    {
        isUnlocked = true;
    }
    
    /// <summary>
    /// Update hotkey for this slot - CRITICAL FIX
    /// </summary>
    public void UpdateHotkey(KeyCode newKey)
    {
        hotkey = newKey;
        Debug.Log($"?? Updated slot {slotIndex} hotkey to {newKey}");
    }
    
    /// <summary>
    /// Get current hotkey
    /// </summary>
    public KeyCode GetHotkey()
    {
        return hotkey;
    }
    
    /// <summary>
    /// Check if this slot uses the specified hotkey
    /// </summary>
    public bool UsesHotkey(KeyCode key)
    {
        return hotkey == key;
    }
}