using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ModularSkillManager - Integrated with UniversalHotkeyManager
/// </summary>
public class ModularSkillManager : MonoBehaviour
{
    [Header("Skill Slots Configuration")]
    [SerializeField] private int maxSkillSlots = 8; // T?i ?a 8 slots (level 40)
    [SerializeField] private int levelsPerSlot = 5; // M?i 5 level m? 1 slot
    
    [Header("Available Skills")]
    public List<SkillModule> availableSkills = new List<SkillModule>(); // Made public for UI access
    
    [Header("Legacy System Settings")]
    [SerializeField] private bool enableLegacyHotkeys = false; // DISABLED by default
    [SerializeField] private List<SkillSlot> skillSlots = new List<SkillSlot>();
    
    private Character player;
    private Dictionary<ISkillExecutor, float> cooldownTimers = new Dictionary<ISkillExecutor, float>();
    
    // Events
    public System.Action<int> OnSlotUnlocked;
    public System.Action<int, SkillModule> OnSkillEquipped;
    public System.Action<int> OnSkillUnequipped;

    private void Awake()
    {
        player = GetComponent<Character>();
        InitializeSkillSlots();
    }

    private void Start()
    {
        UpdateUnlockedSlots();
        
        Debug.Log($"ModularSkillManager initialized. Legacy hotkeys: {(enableLegacyHotkeys ? "ENABLED" : "DISABLED")}");
        if (!enableLegacyHotkeys)
        {
            Debug.Log("Use UniversalHotkeyManager for skill execution via custom key bindings");
        }
    }

    private void Update()
    {
        UpdateCooldowns();
        
        // Only handle legacy input if enabled
        if (enableLegacyHotkeys)
        {
            HandleSkillInput();
        }
    }

    private void InitializeSkillSlots()
    {
        skillSlots.Clear();
        
        // ?? FIXED: Dynamic hotkey generation based on maxSkillSlots
        KeyCode[] hotkeys = GenerateDynamicHotkeys(maxSkillSlots);
        
        for (int i = 0; i < maxSkillSlots; i++)
        {
            var slot = new SkillSlot(i, hotkeys[i]);
            skillSlots.Add(slot);
        }
        
        Debug.Log($"? Initialized {maxSkillSlots} skill slots with dynamic hotkeys");
    }
    
    /// <summary>
    /// Generate dynamic hotkeys based on slot count - SUPPORTS UNLIMITED SLOTS
    /// </summary>
    private KeyCode[] GenerateDynamicHotkeys(int slotCount)
    {
        KeyCode[] hotkeys = new KeyCode[slotCount];
        
        // Standard number keys: 1-9, 0 (10 keys)
        KeyCode[] numberKeys = { 
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };
        
        // Function keys: F1-F12 (12 keys)
        KeyCode[] functionKeys = {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
        };
        
        // Letter keys: Q,W,E,R,T,Y,U,I,O,P (10 keys)
        KeyCode[] letterKeys = {
            KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
            KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P
        };
        
        // Additional letter keys: A,S,D,F,G,H,J,K,L (9 keys)
        KeyCode[] extraLetterKeys = {
            KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
            KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L
        };
        
        // Mouse buttons (5 keys)
        KeyCode[] mouseKeys = {
            KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6
        };
        
        // Combine all available keys
        System.Collections.Generic.List<KeyCode> allAvailableKeys = new System.Collections.Generic.List<KeyCode>();
        allAvailableKeys.AddRange(numberKeys);      // 1-9, 0
        allAvailableKeys.AddRange(functionKeys);    // F1-F12
        allAvailableKeys.AddRange(letterKeys);      // Q,W,E,R,T,Y,U,I,O,P
        allAvailableKeys.AddRange(extraLetterKeys); // A,S,D,F,G,H,J,K,L
        allAvailableKeys.AddRange(mouseKeys);       // Mouse3-6
        
        // Assign hotkeys up to available keys
        for (int i = 0; i < slotCount; i++)
        {
            if (i < allAvailableKeys.Count)
            {
                hotkeys[i] = allAvailableKeys[i];
            }
            else
            {
                // If we run out of keys, assign None (can be changed dynamically later)
                hotkeys[i] = KeyCode.None;
                Debug.LogWarning($"?? Slot {i} has no default hotkey (total keys available: {allAvailableKeys.Count})");
            }
        }
        
        // Debug log hotkey assignments
        Debug.Log($"?? Generated {slotCount} hotkeys:");
        for (int i = 0; i < System.Math.Min(slotCount, 10); i++) // Show first 10
        {
            Debug.Log($"   Slot {i}: {hotkeys[i]}");
        }
        if (slotCount > 10)
        {
            Debug.Log($"   ... and {slotCount - 10} more slots");
        }
        
        return hotkeys;
    }

    private void UpdateCooldowns()
    {
        var cooldownList = cooldownTimers.ToList();
        foreach (var pair in cooldownList)
        {
            cooldownTimers[pair.Key] = pair.Value - Time.deltaTime;
            if (cooldownTimers[pair.Key] <= 0)
            {
                cooldownTimers.Remove(pair.Key);
            }
        }
    }

    private void HandleSkillInput()
    {
        // Only process if legacy hotkeys are enabled
        if (!enableLegacyHotkeys) return;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (slot.isUnlocked && slot.HasSkill() && Input.GetKeyDown(slot.hotkey))
            {
                Debug.Log($"Legacy hotkey {slot.hotkey} pressed for {slot.equippedSkill.skillName}");
                ActivateSkill(i);
            }
        }
    }

    public void ActivateSkill(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            Debug.LogWarning($"Skill slot index {slotIndex} is out of range. Available slots: {skillSlots.Count}");
            return;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked || !slot.HasSkill()) 
        {
            Debug.Log($"Slot {slotIndex} is locked or has no skill equipped");
            return;
        }
        
        var executor = slot.executor;
        
        // Check if executor is null
        if (executor == null)
        {
            Debug.LogError($"Executor for slot {slotIndex} is null!");
            return;
        }
        
        // Check cooldown
        if (cooldownTimers.ContainsKey(executor)) 
        {
            Debug.Log($"Skill in slot {slotIndex} is on cooldown");
            return;
        }
        
        // Check if can execute
        if (!executor.CanExecute(player)) 
        {
            Debug.Log($"Cannot execute skill in slot {slotIndex} - requirements not met");
            return;
        }
        
        // CRITICAL FIX: Proper mouse position conversion with Z-coordinate
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // IMPORTANT: Set camera distance
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 targetPos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        Debug.Log($"??? FIXED MOUSE POSITION: Screen={Input.mousePosition}, World={targetPos}");
        
        // Execute skill
        executor.Execute(player, targetPos);
        
        // Start cooldown
        cooldownTimers[executor] = executor.GetCooldown();
        
        Debug.Log($"Activated skill: {slot.equippedSkill.skillName} in slot {slotIndex} via LEGACY SYSTEM");
    }

    public bool EquipSkill(int slotIndex, SkillModule skill)
    {
        if (slotIndex >= skillSlots.Count) 
        {
            Debug.LogError($"Cannot equip skill - slot index {slotIndex} is out of range");
            return false;
        }
        
        if (skill == null)
        {
            Debug.LogError("Cannot equip null skill");
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        if (!slot.isUnlocked) 
        {
            Debug.Log($"Cannot equip {skill.skillName} - slot {slotIndex} is locked");
            return false;
        }
        
        // Check level requirement
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            Debug.Log($"Cannot equip {skill.skillName}. Required level: {skill.requiredLevel}");
            return false;
        }

        // IMPORTANT: When equipping to legacy system, remove from Universal system
        var unifiedSystemInstance = FindFirstObjectByType<UnifiedSkillSystem>();
        if (unifiedSystemInstance != null)
        {
            var universalKey = UnifiedSkillSystem.GetKeyForSkill(skill);
            if (universalKey != KeyCode.None)
            {
                UnifiedSkillSystem.RemoveKeyAssignment(universalKey);
                Debug.Log($"Removed {skill.skillName} from Universal key {universalKey} when equipping to legacy slot {slotIndex}");
            }
        }
        
        bool success = slot.EquipSkill(skill);
        if (success)
        {
            OnSkillEquipped?.Invoke(slotIndex, skill);
            Debug.Log($"Equipped {skill.skillName} to legacy slot {slotIndex}");
        }
        else
        {
            Debug.LogError($"Failed to equip {skill.skillName} to legacy slot {slotIndex}");
        }
        
        return success;
    }

    public void UnequipSkill(int slotIndex)
    {
        if (slotIndex >= skillSlots.Count) return;
        
        var slot = skillSlots[slotIndex];
        var skillName = slot.HasSkill() ? slot.equippedSkill.skillName : "Unknown";
        slot.UnequipSkill();
        
        OnSkillUnequipped?.Invoke(slotIndex);
        Debug.Log($"Unequipped skill {skillName} from legacy slot {slotIndex}");
    }

    public void UpdateUnlockedSlots()
    {
        int playerLevel = GetPlayerLevel();
        int unlockedSlots = Mathf.Min((playerLevel / levelsPerSlot) + 1, maxSkillSlots);
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            bool wasUnlocked = skillSlots[i].isUnlocked;
            bool shouldBeUnlocked = i < unlockedSlots;
            
            if (!wasUnlocked && shouldBeUnlocked)
            {
                skillSlots[i].UnlockSlot();
                OnSlotUnlocked?.Invoke(i);
                Debug.Log($"Unlocked skill slot {i} at level {playerLevel}");
            }
        }
    }

    public int GetPlayerLevel()
    {
        // TODO: Implement proper level system
        // For now, return a test value
        return PlayerPrefs.GetInt("PlayerLevel", 1);
    }

    public void SetPlayerLevel(int level)
    {
        PlayerPrefs.SetInt("PlayerLevel", level);
        UpdateUnlockedSlots();
        Debug.Log($"Player level set to {level}");
    }

    public List<SkillModule> GetAvailableSkills()
    {
        return availableSkills.Where(skill => GetPlayerLevel() >= skill.requiredLevel).ToList();
    }

    public List<SkillSlot> GetUnlockedSlots()
    {
        return skillSlots.Where(slot => slot.isUnlocked).ToList();
    }

    public SkillSlot GetSlot(int index)
    {
        if (index >= 0 && index < skillSlots.Count)
            return skillSlots[index];
        return null;
    }

    public float GetSkillCooldown(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || !slot.HasSkill()) return 0f;
        
        if (cooldownTimers.ContainsKey(slot.executor))
            return cooldownTimers[slot.executor];
        
        return 0f;
    }

    public bool IsSkillOnCooldown(int slotIndex)
    {
        return GetSkillCooldown(slotIndex) > 0f;
    }

    /// <summary>
    /// Check if skill is equipped in ANY slot
    /// </summary>
    public bool IsSkillEquippedInLegacySystem(SkillModule skill)
    {
        if (skill == null) return false;
        
        return skillSlots.Any(slot => slot.HasSkill() && slot.equippedSkill == skill);
    }

    /// <summary>
    /// Get slot index where skill is equipped (-1 if not equipped)
    /// </summary>
    public int GetSkillSlotIndex(SkillModule skill)
    {
        if (skill == null) return -1;
        
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].HasSkill() && skillSlots[i].equippedSkill == skill)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Enable/disable legacy hotkey system
    /// </summary>
    public void SetLegacyHotkeysEnabled(bool enabled)
    {
        enableLegacyHotkeys = enabled;
        Debug.Log($"Legacy hotkeys {(enabled ? "ENABLED" : "DISABLED")}");
        
        if (!enabled)
        {
            Debug.Log("All skill input now handled by UniversalHotkeyManager");
        }
    }

    // Method ?? th?m skill m?i v?o available list
    public void AddAvailableSkill(SkillModule skill)
    {
        if (!availableSkills.Contains(skill))
        {
            availableSkills.Add(skill);
        }
    }

    // Method ?? remove skill kh?i available list
    public void RemoveAvailableSkill(SkillModule skill)
    {
        availableSkills.Remove(skill);
    }

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

    /// <summary>
    /// Context menu for debugging - Enhanced with dynamic hotkey info
    /// </summary>
    [ContextMenu("?? Debug Skill Assignments")]
    public void DebugSkillAssignments()
    {
        Debug.Log("=== MODULAR SKILL MANAGER DEBUG ===");
        Debug.Log($"Legacy Hotkeys Enabled: {enableLegacyHotkeys}");
        Debug.Log($"Player Level: {GetPlayerLevel()}");
        Debug.Log($"Available Skills: {availableSkills.Count}");
        
        Debug.Log("Current Slot Assignments:");
        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (slot.isUnlocked)
            {
                string skillName = slot.HasSkill() ? slot.equippedSkill.skillName : "Empty";
                string hotkeyInfo = $"Hotkey: {slot.GetHotkey()}";
                Debug.Log($"  Slot {i}: {skillName} | {hotkeyInfo} | Status: {(slot.HasSkill() ? "Equipped" : "Empty")}");
            }
            else
            {
                Debug.Log($"  Slot {i}: LOCKED | Hotkey: {slot.GetHotkey()}");
            }
        }
        
        // Show Universal hotkeys for comparison (if available)
        Debug.Log("=== UNIVERSAL HOTKEY COMPARISON SKIPPED ===");
        Debug.Log("Note: Cross-system consistency check requires UnifiedSkillSystem instance");
        
        Debug.Log("=== END DEBUG ===");
    }

    /// <summary>
    /// Migrate all legacy assignments to Universal system
    /// </summary>
    [ContextMenu("?? Migrate to Universal System")]
    public void MigrateToUniversalSystem()
    {
        // Migrate skills to universal system (simplified)
        Debug.Log("?? Migration to Universal system not available in this version");
        Debug.Log("Use UnifiedSkillSystem directly for custom key bindings");
        
        // Disable legacy system
        SetLegacyHotkeysEnabled(false);
        
        Debug.Log($"?? Legacy system disabled");
    }

    /// <summary>
    /// Update hotkey for a skill slot - CRITICAL FIX
    /// </summary>
    public bool UpdateSlotHotkey(int slotIndex, KeyCode newKey)
    {
        if (slotIndex < 0 || slotIndex >= skillSlots.Count)
        {
            Debug.LogError($"Invalid slot index {slotIndex}");
            return false;
        }
        
        var slot = skillSlots[slotIndex];
        KeyCode oldKey = slot.hotkey;
        
        // Check if any other slot is using this key
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (i != slotIndex && skillSlots[i].UsesHotkey(newKey))
            {
                Debug.LogWarning($"Key {newKey} is already used by slot {i}");
                return false;
            }
        }
        
        // Update the hotkey
        slot.UpdateHotkey(newKey);
        
        Debug.Log($"?? Updated slot {slotIndex} hotkey: {oldKey} ? {newKey}");
        return true;
    }
    
    /// <summary>
    /// Find slot by hotkey
    /// </summary>
    public SkillSlot GetSlotByHotkey(KeyCode key)
    {
        return skillSlots.FirstOrDefault(slot => slot.UsesHotkey(key));
    }
    
    /// <summary>
    /// Get slot index by hotkey (-1 if not found)
    /// </summary>
    public int GetSlotIndexByHotkey(KeyCode key)
    {
        for (int i = 0; i < skillSlots.Count; i++)
        {
            if (skillSlots[i].UsesHotkey(key))
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Assign skill to specific hotkey (finds or creates slot)
    /// </summary>
    public bool AssignSkillToHotkey(SkillModule skill, KeyCode key)
    {
        if (skill == null)
        {
            Debug.LogError("Cannot assign null skill");
            return false;
        }
        
        // Check level requirement
        if (GetPlayerLevel() < skill.requiredLevel)
        {
            Debug.Log($"Cannot assign {skill.skillName}. Required level: {skill.requiredLevel}");
            return false;
        }
        
        // Find if any slot already uses this key
        var existingSlot = GetSlotByHotkey(key);
        
        if (existingSlot != null && existingSlot.isUnlocked)
        {
            // Unequip existing skill if any
            if (existingSlot.HasSkill())
            {
                Debug.Log($"?? Unequipping {existingSlot.equippedSkill.skillName} from slot {existingSlot.slotIndex}");
                existingSlot.UnequipSkill();
            }
            
            // Equip new skill
            bool success = existingSlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(existingSlot.slotIndex, skill);
                Debug.Log($"? Assigned {skill.skillName} to existing slot {existingSlot.slotIndex} with hotkey {key}");
            }
            return success;
        }
        
        // Find empty unlocked slot and update its hotkey
        var emptySlot = skillSlots.FirstOrDefault(s => s.isUnlocked && !s.HasSkill());
        if (emptySlot != null)
        {
            // Update hotkey and equip skill
            emptySlot.UpdateHotkey(key);
            bool success = emptySlot.EquipSkill(skill);
            if (success)
            {
                OnSkillEquipped?.Invoke(emptySlot.slotIndex, skill);
                Debug.Log($"? Assigned {skill.skillName} to empty slot {emptySlot.slotIndex} with new hotkey {key}");
            }
            return success;
        }
        
        Debug.LogWarning($"?? No available slots to assign {skill.skillName} to hotkey {key}");
        return false;
    }
    
    /// <summary>
    /// Get total available hotkey slots (for UI purposes)
    /// </summary>
    public int GetMaxSupportedSlots()
    {
        // Numbers (10) + Function keys (12) + Letters (19) + Mouse (4) = 45 slots total
        return 45;
    }
    
    /// <summary>
    /// Get hotkey display name for UI
    /// </summary>
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
    
    /// <summary>
    /// Validate maxSkillSlots setting and provide recommendations
    /// </summary>
    [ContextMenu("?? Validate Max Skill Slots")]
    public void ValidateMaxSkillSlots()
    {
        Debug.Log("=== MAX SKILL SLOTS VALIDATION ===");
        Debug.Log($"Current maxSkillSlots: {maxSkillSlots}");
        Debug.Log($"Max supported slots: {GetMaxSupportedSlots()}");
        
        if (maxSkillSlots <= 8)
        {
            Debug.Log("? Standard configuration (legacy compatible)");
        }
        else if (maxSkillSlots <= GetMaxSupportedSlots())
        {
            Debug.Log("? Extended configuration supported");
            Debug.Log($"   Using slots 0-{maxSkillSlots-1} with dynamic hotkeys");
        }
        else
        {
            Debug.LogWarning($"?? Configuration exceeds recommended maximum!");
            Debug.LogWarning($"   Some slots (#{GetMaxSupportedSlots()}-{maxSkillSlots-1}) will have no default hotkeys");
            Debug.LogWarning($"   Consider using {GetMaxSupportedSlots()} or fewer slots for optimal experience");
        }
        
        // Show hotkey mapping for current configuration
        Debug.Log("\n?? Current Hotkey Mapping:");
        KeyCode[] currentHotkeys = GenerateDynamicHotkeys(maxSkillSlots);
        for (int i = 0; i < System.Math.Min(maxSkillSlots, 15); i++)
        {
            Debug.Log($"   Slot {i}: {GetHotkeyDisplayName(currentHotkeys[i])} ({currentHotkeys[i]})");
        }
        if (maxSkillSlots > 15)
        {
            Debug.Log($"   ... and {maxSkillSlots - 15} more slots");
        }
        
        Debug.Log("=== END VALIDATION ===");
    }
    
    /// <summary>
    /// Recommend optimal maxSkillSlots based on player level system
    /// </summary>
    [ContextMenu("?? Recommend Optimal Configuration")]
    public void RecommendOptimalConfiguration()
    {
        Debug.Log("=== OPTIMAL CONFIGURATION RECOMMENDATIONS ===");
        
        int currentLevel = GetPlayerLevel();
        int maxPossibleLevel = 100; // Assume max level 100
        
        // Calculate theoretical max slots at max level
        int maxTheoreticalSlots = Mathf.Min((maxPossibleLevel / levelsPerSlot) + 1, maxSkillSlots);
        
        Debug.Log($"Current Level: {currentLevel}");
        Debug.Log($"Current Unlocked Slots: {GetUnlockedSlots().Count}");
        Debug.Log($"Max Theoretical Slots (Lv.{maxPossibleLevel}): {maxTheoreticalSlots}");
        
        // Recommendations based on available skills
        int availableSkillsCount = availableSkills.Count;
        Debug.Log($"Available Skills: {availableSkillsCount}");
        
        int recommendedSlots = Mathf.Max(availableSkillsCount, 12); // At least 12 or number of skills
        recommendedSlots = Mathf.Min(recommendedSlots, GetMaxSupportedSlots());
        
        Debug.Log($"\n?? RECOMMENDATIONS:");
        Debug.Log($"   Recommended maxSkillSlots: {recommendedSlots}");
        Debug.Log($"   Reason: Supports all {availableSkillsCount} skills with room for growth");
        
        if (maxSkillSlots < recommendedSlots)
        {
            Debug.Log($"   ?? Consider increasing from {maxSkillSlots} to {recommendedSlots}");
        }
        else if (maxSkillSlots > recommendedSlots + 10)
        {
            Debug.Log($"   ?? Consider reducing from {maxSkillSlots} to {recommendedSlots} for better UI");
        }
        else
        {
            Debug.Log($"   ? Current setting ({maxSkillSlots}) is reasonable");
        }
        
        Debug.Log("=== END RECOMMENDATIONS ===");
    }
}