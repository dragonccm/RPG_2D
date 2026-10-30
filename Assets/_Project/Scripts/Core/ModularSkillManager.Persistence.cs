using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Lưu/nạp thiết lập kỹ năng (cũ) + API phím tắt của ô kỹ năng
public partial class ModularSkillManager
{
    // Save/Load system
    [System.Serializable]
    public class SkillSaveData
    {
        public int[] equippedSkillIDs;
        public int playerLevel;
        public bool legacyHotkeysEnabled;
    }

    public void SaveSkillSetup()
    {
        var saveData = new SkillSaveData();
        saveData.playerLevel = GetPlayerLevel();
        saveData.legacyHotkeysEnabled = enableLegacyHotkeys;
        saveData.equippedSkillIDs = new int[skillSlots.Count];
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].HasSkill())
            {
                saveData.equippedSkillIDs[i] = availableSkills.IndexOf(skillSlots[i].equippedSkill);
            }
            else
            {
                saveData.equippedSkillIDs[i] = -1;
            }
        }
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("SkillSetup", json);
    }

    public void LoadSkillSetup()
    {
        string json = PlayerPrefs.GetString("SkillSetup", "");
        if (string.IsNullOrEmpty(json)) return;
        
        var saveData = JsonUtility.FromJson<SkillSaveData>(json);
        SetPlayerLevel(saveData.playerLevel);
        SetLegacyHotkeysEnabled(saveData.legacyHotkeysEnabled);
        
        for (int i = 0; i < saveData.equippedSkillIDs.Length && i < skillSlots.Count; i++)
        {
            int skillID = saveData.equippedSkillIDs[i];
            if (skillID >= 0 && skillID < availableSkills.Count)
            {
                EquipSkill(i, availableSkills[skillID]);
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSkillSetup();
        }
    }

    public bool UpdateSlotHotkey(int slotIndex, KeyCode newKey)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Count)
        {
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        KeyCode oldKey = slot.hotkey;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (i != slotIndex && skillSlots[i].UsesHotkey(newKey))
            {
                return false;
            }
        }
        
        slot.UpdateHotkey(newKey);
        return true;
    }
    
    public SkillSlot GetSlotByHotkey(KeyCode key)
    {
        return skillSlots.FirstOrDefault(slot => slot.UsesHotkey(key));
    }
    
    public int GetSlotIndexByHotkey(KeyCode key)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].UsesHotkey(key))
                return i;
        }
        return -1;
    }
    
    public bool AssignSkillToHotkey(SkillModule skill, KeyCode key)
    {
        if (skill == null)
        {
            return false;
        }
        
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            return false;
        }
        
        var existingSlot = GetSlotByHotkey(key);
        
        if (existingSlot != null && existingSlot.isUnlocked)
        {
            if (existingSlot.HasSkill())
            {
                existingSlot.UnequipSkill();
            }
            
            bool success = existingSlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(existingSlot.slotIndex, skill);
            }
            return success;
        }
        
        var emptySlot = skillSlots.FirstOrDefault(s => s.isUnlocked && !s.HasSkill());
        if (emptySlot != null)
        {
            emptySlot.UpdateHotkey(key);
            bool success = emptySlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(emptySlot.slotIndex, skill);
            }
            return success;
        }
        
        return false;
    }
    
    public int GetMaxSupportedSlots()
    {
        return 45;
    }
    
    public string GetHotkeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
            case KeyCode.Alpha0: return "0";
            case KeyCode.F1: return "F1";
            case KeyCode.F2: return "F2";
            case KeyCode.F3: return "F3";
            case KeyCode.F4: return "F4";
            case KeyCode.F5: return "F5";
            case KeyCode.F6: return "F6";
            case KeyCode.F7: return "F7";
            case KeyCode.F8: return "F8";
            case KeyCode.F9: return "F9";
            case KeyCode.F10: return "F10";
            case KeyCode.F11: return "F11";
            case KeyCode.F12: return "F12";
            case KeyCode.Mouse3: return "M1";
            case KeyCode.Mouse4: return "M2";
            case KeyCode.Mouse5: return "M3";
            case KeyCode.Mouse6: return "M4";
            case KeyCode.None: return "---";
            default: return key.ToString();
        }
    }
}
