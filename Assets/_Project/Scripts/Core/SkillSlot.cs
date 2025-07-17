using UnityEngine;

/// <summary>
/// Enhanced SkillSlot with dynamic hotkey support and improved executor management
/// Represents a single skill slot that can hold a skill and its executor
/// </summary>
[System.Serializable]
public class SkillSlot
{
    [Header("Slot Configuration")]
    public int slotIndex;
    public SkillModule equippedSkill;
    public ISkillExecutor executor;
    public KeyCode hotkey;
    public bool isUnlocked;
    
    [Header("Slot Status")]
    [SerializeField] private float lastUsedTime;
    [SerializeField] private bool isOnCooldown;
    
    public SkillSlot(int index, KeyCode key)
    {
        slotIndex = index;
        hotkey = key;
        isUnlocked = false;
        equippedSkill = null;
        executor = null;
        lastUsedTime = 0f;
        isOnCooldown = false;
    }
    
    /// <summary>
    /// Equip a skill to this slot with improved validation
    /// </summary>
    public bool EquipSkill(SkillModule skill)
    {
        if (!isUnlocked || skill == null) 
        {
            Debug.LogWarning($"Cannot equip skill to slot {slotIndex}: Slot not unlocked or skill is null");
            return false;
        }
        
        // Unequip current skill first if any
        if (HasSkill())
        {
            UnequipSkill();
        }
        
        equippedSkill = skill;
        executor = skill.CreateExecutor();
        
        if (executor == null)
        {
            Debug.LogError($"Failed to create executor for skill {skill.skillName} in slot {slotIndex}");
            equippedSkill = null;
            return false;
        }
        
        Debug.Log($"? Successfully equipped skill '{skill.skillName}' to slot {slotIndex} with hotkey {GetHotkeyDisplayName()}");
        return true;
    }
    
    /// <summary>
    /// Unequip current skill from this slot
    /// </summary>
    public void UnequipSkill()
    {
        if (equippedSkill != null)
        {
            Debug.Log($"?? Unequipped skill '{equippedSkill.skillName}' from slot {slotIndex}");
        }
        
        equippedSkill = null;
        executor = null;
        ResetCooldown();
    }
    
    /// <summary>
    /// Check if this slot has a skill equipped
    /// </summary>
    public bool HasSkill()
    {
        return equippedSkill != null && executor != null;
    }
    
    /// <summary>
    /// Unlock this slot for use
    /// </summary>
    public void UnlockSlot()
    {
        isUnlocked = true;
        Debug.Log($"?? Unlocked skill slot {slotIndex} with hotkey {GetHotkeyDisplayName()}");
    }
    
    /// <summary>
    /// Update hotkey for this slot with validation
    /// </summary>
    public void UpdateHotkey(KeyCode newKey)
    {
        if (newKey == hotkey) return;
        
        KeyCode oldKey = hotkey;
        hotkey = newKey;
        
        Debug.Log($"?? Updated slot {slotIndex} hotkey from {GetHotkeyDisplayName(oldKey)} to {GetHotkeyDisplayName()}");
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
    
    /// <summary>
    /// Execute the skill in this slot
    /// </summary>
    public bool TryExecuteSkill(Character user, Vector2 targetPosition)
    {
        if (!CanExecuteSkill(user))
        {
            return false;
        }
        
        executor.Execute(user, targetPosition);
        SetCooldown();
        lastUsedTime = Time.time;
        
        Debug.Log($"? Executed skill '{equippedSkill.skillName}' from slot {slotIndex}");
        return true;
    }
    
    /// <summary>
    /// Check if the skill in this slot can be executed
    /// </summary>
    public bool CanExecuteSkill(Character user)
    {
        if (!isUnlocked || !HasSkill())
        {
            return false;
        }
        
        if (isOnCooldown)
        {
            return false;
        }
        
        return executor.CanExecute(user);
    }
    
    /// <summary>
    /// Set skill on cooldown
    /// </summary>
    private void SetCooldown()
    {
        if (HasSkill() && executor.GetCooldown() > 0)
        {
            isOnCooldown = true;
        }
    }
    
    /// <summary>
    /// Reset cooldown status
    /// </summary>
    public void ResetCooldown()
    {
        isOnCooldown = false;
    }
    
    /// <summary>
    /// Update cooldown status (should be called every frame)
    /// </summary>
    public void UpdateCooldown()
    {
        if (isOnCooldown && HasSkill())
        {
            float elapsedTime = Time.time - lastUsedTime;
            if (elapsedTime >= executor.GetCooldown())
            {
                isOnCooldown = false;
            }
        }
    }
    
    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public float GetRemainingCooldown()
    {
        if (!isOnCooldown || !HasSkill())
        {
            return 0f;
        }
        
        float elapsedTime = Time.time - lastUsedTime;
        float remainingTime = executor.GetCooldown() - elapsedTime;
        return Mathf.Max(0f, remainingTime);
    }
    
    /// <summary>
    /// Check if skill is currently on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    /// <summary>
    /// Get display name for hotkey
    /// </summary>
    public string GetHotkeyDisplayName(KeyCode? customKey = null)
    {
        KeyCode keyToDisplay = customKey ?? hotkey;
        
        return keyToDisplay switch
        {
            KeyCode.Alpha1 => "1",
            KeyCode.Alpha2 => "2",
            KeyCode.Alpha3 => "3",
            KeyCode.Alpha4 => "4",
            KeyCode.Alpha5 => "5",
            KeyCode.Alpha6 => "6",
            KeyCode.Alpha7 => "7",
            KeyCode.Alpha8 => "8",
            KeyCode.Alpha9 => "9",
            KeyCode.Alpha0 => "0",
            KeyCode.F1 => "F1",
            KeyCode.F2 => "F2",
            KeyCode.F3 => "F3",
            KeyCode.F4 => "F4",
            KeyCode.F5 => "F5",
            KeyCode.F6 => "F6",
            KeyCode.F7 => "F7",
            KeyCode.F8 => "F8",
            KeyCode.F9 => "F9",
            KeyCode.F10 => "F10",
            KeyCode.F11 => "F11",
            KeyCode.F12 => "F12",
            KeyCode.Mouse3 => "M1",
            KeyCode.Mouse4 => "M2",
            KeyCode.Mouse5 => "M3",
            KeyCode.Mouse6 => "M4",
            KeyCode.None => "---",
            _ => keyToDisplay.ToString()
        };
    }
    
    /// <summary>
    /// Get skill information for UI display
    /// </summary>
    public string GetSkillInfo()
    {
        if (!HasSkill())
        {
            return $"[{GetHotkeyDisplayName()}] Empty Slot";
        }
        
        string cooldownText = isOnCooldown ? $" (CD: {GetRemainingCooldown():F1}s)" : "";
        return $"[{GetHotkeyDisplayName()}] {equippedSkill.skillName}{cooldownText}";
    }
    
    /// <summary>
    /// Validate slot integrity
    /// </summary>
    public bool ValidateSlot()
    {
        if (HasSkill())
        {
            if (executor == null)
            {
                Debug.LogError($"Slot {slotIndex} has skill but no executor - fixing...");
                executor = equippedSkill.CreateExecutor();
                return executor != null;
            }
        }
        
        return true;
    }
}