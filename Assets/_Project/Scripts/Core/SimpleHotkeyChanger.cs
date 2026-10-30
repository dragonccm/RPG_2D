using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SIMPLE HOTKEY CHANGER - Direct ModularSkillManager manipulation
/// NO OVERLAY SYSTEMS - Just change the hotkey directly in the existing slot!
/// </summary>
public class SimpleHotkeyChanger : MonoBehaviour
{
    [Header("Simple Hotkey System")]
    [SerializeField] private bool enableSimpleSystem = true;
    [SerializeField] private bool showDebugLogs = true;
    
    private ModularSkillManager skillManager;
    
    // UI Integration
    public static System.Action<SkillModule, KeyCode> OnHotkeyChanged;
    
    private void Start()
    {
        // Find the skill manager
        skillManager = FindFirstObjectByType<ModularSkillManager>();
        
        if (skillManager != null)
        {
            Debug.Log("?? SimpleHotkeyChanger found ModularSkillManager - ready to change hotkeys!");
        }
        else
        {
            Debug.LogError("? No ModularSkillManager found! SimpleHotkeyChanger will not work.");
        }
    }
    
    /// <summary>
    /// MAIN METHOD: Change hotkey for a skill directly in ModularSkillManager
    /// This replaces the old complex overlay system
    /// </summary>
    public bool ChangeSkillHotkey(SkillModule skill, KeyCode newKey)
    {
        if (!enableSimpleSystem)
        {
            Debug.LogWarning("Simple hotkey system is disabled!");
            return false;
        }
        
        if (skillManager == null)
        {
            Debug.LogError("No ModularSkillManager available!");
            return false;
        }
        
        if (skill == null)
        {
            Debug.LogError("Cannot change hotkey for null skill!");
            return false;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"?? SIMPLE CHANGE: {skill.skillName} ? {newKey}");
        }
        
        // Step 1: Find current slot with this skill
        int currentSlotIndex = FindSlotWithSkill(skill);
        
        // Step 2: Check if new key is already used
        int conflictSlotIndex = FindSlotWithHotkey(newKey);
        
        // Step 3: Handle the change
        if (currentSlotIndex >= 0)
        {
            // Skill is already equipped, just change its hotkey
            return ChangeExistingSkillHotkey(currentSlotIndex, newKey, conflictSlotIndex);
        }
        else
        {
            // Skill not equipped, find empty slot or create new assignment
            return AssignSkillToNewHotkey(skill, newKey, conflictSlotIndex);
        }
    }
    
    /// <summary>
    /// Change hotkey for already equipped skill
    /// </summary>
    private bool ChangeExistingSkillHotkey(int skillSlotIndex, KeyCode newKey, int conflictSlotIndex)
    {
        var skillSlot = skillManager.GetSlot(skillSlotIndex);
        var skill = skillSlot.equippedSkill;
        
        if (conflictSlotIndex >= 0)
        {
            // New key is already used - swap or replace
            var conflictSlot = skillManager.GetSlot(conflictSlotIndex);
            
            if (showDebugLogs)
            {
                Debug.Log($"?? Key {newKey} is used by {conflictSlot.equippedSkill?.skillName ?? "Empty"}");
                Debug.Log($"?? Swapping: {skill.skillName} ({skillSlot.hotkey}) ? {conflictSlot.equippedSkill?.skillName ?? "Empty"} ({newKey})");
            }
            
            // Swap hotkeys
            KeyCode oldKey = skillSlot.hotkey;
            skillSlot.UpdateHotkey(newKey);
            conflictSlot.UpdateHotkey(oldKey);
            
            Debug.Log($"? SWAPPED: {skill.skillName} now uses {newKey}");
        }
        else
        {
            // No conflict, just change the hotkey
            KeyCode oldKey = skillSlot.hotkey;
            skillSlot.UpdateHotkey(newKey);
            
            if (showDebugLogs)
            {
                Debug.Log($"? CHANGED: {skill.skillName} hotkey {oldKey} ? {newKey}");
            }
        }
        
        OnHotkeyChanged?.Invoke(skill, newKey);
        return true;
    }
    
    /// <summary>
    /// Assign skill to new hotkey
    /// </summary>
    private bool AssignSkillToNewHotkey(SkillModule skill, KeyCode newKey, int conflictSlotIndex)
    {
        if (conflictSlotIndex >= 0)
        {
            // Replace existing assignment
            var conflictSlot = skillManager.GetSlot(conflictSlotIndex);
            var oldSkill = conflictSlot.equippedSkill;
            
            if (showDebugLogs && oldSkill != null)
            {
                Debug.Log($"?? REPLACING: {oldSkill.skillName} with {skill.skillName} on key {newKey}");
            }
            
            // Unequip old skill and equip new one
            conflictSlot.UnequipSkill();
            conflictSlot.EquipSkill(skill);
            
            Debug.Log($"? ASSIGNED: {skill.skillName} ? {newKey} (replaced {oldSkill?.skillName ?? "empty"})");
        }
        else
        {
            // Find empty unlocked slot or create new one
            var emptySlot = FindEmptyUnlockedSlot();
            if (emptySlot != null)
            {
                emptySlot.UpdateHotkey(newKey);
                emptySlot.EquipSkill(skill);
                
                if (showDebugLogs)
                {
                    Debug.Log($"? EQUIPPED: {skill.skillName} ? {newKey} in slot {emptySlot.slotIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"?? No available slots to assign {skill.skillName} to {newKey}");
                return false;
            }
        }
        
        OnHotkeyChanged?.Invoke(skill, newKey);
        return true;
    }
    
    /// <summary>
    /// Find slot index that has the specified skill
    /// </summary>
    private int FindSlotWithSkill(SkillModule skill)
    {
        if (skillManager == null) return -1;
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        for (int i = 0; i < unlockedSlots.Count; i++)
        {
            var slot = unlockedSlots[i];
            if (slot.HasSkill() && slot.equippedSkill == skill)
            {
                return slot.slotIndex;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Find slot index that uses the specified hotkey
    /// </summary>
    private int FindSlotWithHotkey(KeyCode key)
    {
        if (skillManager == null) return -1;
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        for (int i = 0; i < unlockedSlots.Count; i++)
        {
            var slot = unlockedSlots[i];
            if (slot.UsesHotkey(key))
            {
                return slot.slotIndex;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Find first empty unlocked slot
    /// </summary>
    private SkillSlot FindEmptyUnlockedSlot()
    {
        if (skillManager == null) return null;
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        foreach (var slot in unlockedSlots)
        {
            if (!slot.HasSkill())
            {
                return slot;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get current hotkey for a skill
    /// </summary>
    public KeyCode GetSkillHotkey(SkillModule skill)
    {
        int slotIndex = FindSlotWithSkill(skill);
        if (slotIndex >= 0)
        {
            var slot = skillManager.GetSlot(slotIndex);
            return slot.GetHotkey();
        }
        return KeyCode.None;
    }
    
    /// <summary>
    /// Remove skill from all slots
    /// </summary>
    public bool RemoveSkill(SkillModule skill)
    {
        int slotIndex = FindSlotWithSkill(skill);
        if (slotIndex >= 0)
        {
            var slot = skillManager.GetSlot(slotIndex);
            slot.UnequipSkill();
            
            if (showDebugLogs)
            {
                Debug.Log($"??? REMOVED: {skill.skillName} from slot {slotIndex}");
            }
            
            OnHotkeyChanged?.Invoke(skill, KeyCode.None);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Debug all current assignments
    /// </summary>
    [ContextMenu("?? Debug Simple Assignments")]
    public void DebugCurrentAssignments()
    {
        if (skillManager == null)
        {
            Debug.LogError("No ModularSkillManager found!");
            return;
        }
        
        Debug.Log("=== SIMPLE HOTKEY CHANGER DEBUG ===");
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        Debug.Log($"Total unlocked slots: {unlockedSlots.Count}");
        
        foreach (var slot in unlockedSlots)
        {
            if (slot.HasSkill())
            {
                Debug.Log($"  Slot {slot.slotIndex}: {slot.equippedSkill.skillName} ? {slot.GetHotkey()}");
            }
            else
            {
                Debug.Log($"  Slot {slot.slotIndex}: Empty ? {slot.GetHotkey()}");
            }
        }
        
        Debug.Log("=== END DEBUG ===");
    }
    
    /// <summary>
    /// Test changing a skill hotkey
    /// </summary>
    [ContextMenu("?? Test Hotkey Change")]
    public void TestHotkeyChange()
    {
        if (skillManager == null || skillManager.GetAvailableSkills().Count == 0)
        {
            Debug.LogWarning("No skills available for testing!");
            return;
        }
        
        var testSkill = skillManager.GetAvailableSkills()[0];
        bool success = ChangeSkillHotkey(testSkill, KeyCode.E);
        
        Debug.Log($"?? Test result: {(success ? "SUCCESS" : "FAILED")} - {testSkill.skillName} ? E");
    }
    
    /// <summary>
    /// Public API for UI integration
    /// </summary>
    public List<SkillModule> GetAllEquippedSkills()
    {
        var result = new List<SkillModule>();
        if (skillManager == null) return result;
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        foreach (var slot in unlockedSlots)
        {
            if (slot.HasSkill())
            {
                result.Add(slot.equippedSkill);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Get available skills from ModularSkillManager
    /// </summary>
    public List<SkillModule> GetAvailableSkills()
    {
        return skillManager?.GetAvailableSkills() ?? new List<SkillModule>();
    }
} 