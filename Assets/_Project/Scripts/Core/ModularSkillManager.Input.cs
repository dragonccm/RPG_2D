using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Nhập phím kỹ năng + vòng xem trước (bắt đầu/giữ/thả) + kích hoạt kỹ năng có xem trước
public partial class ModularSkillManager
{
    private void HandleSkillInput()
    {
        if (!enableLegacyHotkeys) return;

        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (!slot.isUnlocked || !slot.HasSkill()) continue;

            var skill = slot.equippedSkill;
            
            // Enhanced input handling with better validation
            if (!slot.CanExecuteSkill(player)) continue;
            
            // Instant skills: Execute immediately on key down
            if (skill.skillType == SkillType.Instant)
            {
                if (Input.GetKeyDown(slot.hotkey))
                {
                    ActivateSkillEnhanced(i);
                }
                continue; // Skip the hold-release logic for instant skills
            }

            // Normal skills: Enhanced hold-release logic
            if (Input.GetKeyDown(slot.hotkey))
            {
                // Phase 1: Start skill with enhanced validation
                StartSkillPreviewEnhanced(i);
            }

            if (Input.GetKey(slot.hotkey))
            {
                // Phase 2: Hold skill with smooth updates
                HoldSkillPreviewEnhanced(i);
            }

            if (Input.GetKeyUp(slot.hotkey))
            {
                // Phase 3: Release skill with enhanced execution
                ActivateSkillEnhanced(i);
                EndSkillPreviewEnhanced(i);
            }
        }
    }

    /// <summary>
    /// Enhanced skill preview start with better validation and visual feedback
    /// </summary>
    private void StartSkillPreviewEnhanced(int slotIndex)
    {
        if (isSkillActive || slotIndex >= skillSlots.Count) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;
        
        // Enhanced validation
        if (!slot.CanExecuteSkill(player))
        {
            Debug.Log($"?? Cannot execute skill '{slot.equippedSkill.skillName}' - conditions not met");
            return;
        }

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // Enhanced skill preview with better visual feedback
        ShowSkillPreviewEnhanced(skill);

        isSkillActive = true;
        activeSkillSlot = slotIndex;
        
        Debug.Log($"?? Started preview for skill '{skill.skillName}' in slot {slotIndex}");
    }

    /// <summary>
    /// Enhanced skill preview hold with smooth updates
    /// </summary>
    private void HoldSkillPreviewEnhanced(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) return;

        var skill = slot.equippedSkill;
        if (skill == null || !skill.showDamageArea) return;

        // Smooth preview updates
        UpdateSkillPreviewEnhanced(skill);
        isSkillHeld = true;
    }

    /// <summary>
    /// Enhanced skill preview end with cleanup
    /// </summary>
    private void EndSkillPreviewEnhanced(int slotIndex)
    {
        if (!isSkillActive || slotIndex != activeSkillSlot) return;

        var slot = skillSlots[slotIndex];
        string skillName = slot.HasSkill() ? slot.equippedSkill.skillName : "Unknown";
        
        // Enhanced preview cleanup
        HideSkillPreviewEnhanced();

        isSkillActive = false;
        isSkillHeld = false;
        activeSkillSlot = -1;
        
        Debug.Log($"?? Ended preview for skill '{skillName}'");
    }

    /// <summary>
    /// Enhanced skill activation with better error handling and feedback
    /// </summary>
    private void ActivateSkillEnhanced(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            Debug.LogWarning($"?? Invalid slot index: {slotIndex}");
            return;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) 
        {
            Debug.LogWarning($"?? Slot {slotIndex} is not unlocked or has no skill");
            return;
        }
        
        // Use enhanced SkillSlot execution method
        var skill = slot.equippedSkill;
        Vector2 targetPos = Vector2.zero;
        
        // Enhanced target position calculation
        if (skill.skillType != SkillType.Instant)
        {
            targetPos = GetEnhancedMouseWorldPosition();
        }
        else
        {
            targetPos = player.transform.position;
        }
        
        // Execute using enhanced SkillSlot method
        bool success = slot.TryExecuteSkill(player, targetPos);
        
        if (success)
        {
            // Update legacy cooldown system for compatibility
            if (slot.executor != null)
            {
                cooldownTimers[slot.executor] = slot.executor.GetCooldown();
            }
            
            Debug.Log($"? Successfully activated skill '{skill.skillName}' from slot {slotIndex}");
        }
        else
        {
            Debug.Log($"? Failed to activate skill '{skill.skillName}' from slot {slotIndex}");
        }
    }
}
