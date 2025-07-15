using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/*
===============================================================================================
⚠️ DEPRECATED: UnifiedSkillSystem - Use SimpleHotkeyChanger instead!
===============================================================================================

This complex overlay system has been replaced by SimpleHotkeyChanger which directly
modifies ModularSkillManager hotkeys without creating conflicting systems.

The old system caused issues where:
- Press "1" → Works (legacy system)  
- Press "E" → Works but wrong damage area (overlay system)

New solution:
- SimpleHotkeyChanger directly changes hotkey in ModularSkillManager  
- Only ONE system handles skill execution
- No more dual hotkey conflicts!

To use the new system:
1. Add SimpleHotkeyChanger component to scene
2. Use SkillDetailUI (updated) for UI integration
3. All skills execute through ModularSkillManager with correct mouse positioning

===============================================================================================
*/

// This class is deprecated - use SimpleHotkeyChanger instead
public class UnifiedSkillSystem : MonoBehaviour
{
    private static UnifiedSkillSystem instance;
    
    [Header("🎮 Unified Skill Configuration")]
    [SerializeField] private bool enableDetailedDebug = true;
    [SerializeField] private bool autoFixManaIssues = true;
    [SerializeField] private bool bypassLevelRequirements = true; // For testing
    [SerializeField] private bool autoEquipSkillsOnStart = true;
    
    // Universal hotkey storage
    private static Dictionary<KeyCode, SkillModule> assignedHotkeys = new Dictionary<KeyCode, SkillModule>();
    
    // Available skills for assignment
    [Header("📋 Available Skills")]
    public List<SkillModule> availableSkills = new List<SkillModule>();
    
    private ModularSkillManager skillManager;
    
    public static UnifiedSkillSystem Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find existing instance
                instance = FindFirstObjectByType<UnifiedSkillSystem>();
                
                if (instance == null)
                {
                    // Create new instance
                    GameObject go = new GameObject("UnifiedSkillSystem");
                    instance = go.AddComponent<UnifiedSkillSystem>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Find ModularSkillManager for integration
        FindSkillManager();
        
        // Auto-setup skills from ModularSkillManager if available
        AutoSetupSkillsFromManager();
        
        // Auto-setup default skills if none found
        AutoSetupDefaultSkills();
    }

    void Update()
    {
        // Handle all skill input globally
        HandleSkillInput();
    }

    void InitializeSystem()
    {
        Debug.Log("🎮 Unified Skill System initialized");
        
        // Clear existing assignments
        assignedHotkeys.Clear();
    }

    void FindSkillManager()
    {
        // Find skill manager on player
        var player = FindFirstObjectByType<Character>();
        if (player != null)
        {
            skillManager = player.GetComponent<ModularSkillManager>();
            if (skillManager != null)
            {
                Debug.Log("🔗 Found ModularSkillManager - integrating systems");
                
                // Enable legacy hotkeys for full integration
                skillManager.SetLegacyHotkeysEnabled(true);
                
                // Get available skills from manager
                if (availableSkills.Count == 0 && skillManager.availableSkills.Count > 0)
                {
                    availableSkills.AddRange(skillManager.availableSkills);
                    Debug.Log($"📋 Imported {skillManager.availableSkills.Count} skills from ModularSkillManager");
                }
            }
        }
    }

    void AutoSetupSkillsFromManager()
    {
        if (skillManager == null || !autoEquipSkillsOnStart) return;

        Debug.Log("🔄 Auto-setting up skills from ModularSkillManager...");

        // Get unlocked slots
        var unlockedSlots = skillManager.GetUnlockedSlots();
        int skillIndex = 0;

        foreach (var slot in unlockedSlots)
        {
            // If slot is empty and we have available skills
            if (!slot.HasSkill() && skillIndex < availableSkills.Count)
            {
                var skill = availableSkills[skillIndex];
                
                // Use the new dynamic assignment method
                if (skillManager.AssignSkillToHotkey(skill, slot.GetHotkey()))
                {
                    // Also assign to unified system
                    assignedHotkeys[slot.GetHotkey()] = skill;
                    
                    Debug.Log($"🎯 Auto-equipped {skill.skillName} to slot {slot.slotIndex} with hotkey {slot.GetHotkey()}");
                    skillIndex++;
                }
            }
            else if (slot.HasSkill())
            {
                // Sync existing skill with unified system
                assignedHotkeys[slot.GetHotkey()] = slot.equippedSkill;
                Debug.Log($"🔗 Synced existing skill {slot.equippedSkill.skillName} with hotkey {slot.GetHotkey()}");
            }
        }
    }

    void AutoSetupDefaultSkills()
    {
        // Only setup defaults if no skills were setup from manager
        if (assignedHotkeys.Count > 0) return;

        // Auto-assign first available skill to key "1" for testing
        if (availableSkills.Count > 0)
        {
            var firstSkill = availableSkills[0];
            AssignSkillToKey(KeyCode.Alpha1, firstSkill);
            Debug.Log($"🎯 Auto-assigned {firstSkill.skillName} to key 1 for testing");
        }
    }

    void HandleSkillInput()
    {
        // Process all assigned hotkeys
        foreach (var kvp in assignedHotkeys)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                ExecuteSkill(kvp.Value, kvp.Key);
            }
        }
    }

    void ExecuteSkill(SkillModule skill, KeyCode key)
    {
        if (skill == null)
        {
            Debug.LogError($"❌ Skill is null for key {key}!");
            return;
        }

        if (enableDetailedDebug)
        {
            Debug.Log($"🎮 Attempting to execute {skill.skillName} with key {key}");
        }

        // Find player character
        var player = FindFirstObjectByType<Character>();
        if (player == null)
        {
            Debug.LogError("❌ No player Character found!");
            return;
        }

        // Check requirements
        if (!CheckSkillRequirements(skill, player))
        {
            Debug.LogWarning($"❌ Cannot execute {skill.skillName} - requirements not met");
            return;
        }

        // Consume mana
        if (player.mana != null && skill.manaCost > 0)
        {
            player.mana.Decrease(skill.manaCost);
            if (enableDetailedDebug)
            {
                Debug.Log($"💙 Consumed {skill.manaCost} mana. Remaining: {player.mana.currentValue}/{player.mana.maxValue}");
            }
        }

        // FIXED: Proper mouse position to world conversion
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // Set distance from camera
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 targetPos = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        if (enableDetailedDebug)
        {
            Debug.Log($"🎯 FIXED Target position: {targetPos} (from mouse: {Input.mousePosition})");
        }

        // Execute skill
        var executor = skill.CreateExecutor();
        if (executor != null)
        {
            executor.Execute(player, targetPos);
            Debug.Log($"✅ Successfully executed {skill.skillName} with key {key}!");
        }
        else
        {
            Debug.LogError($"❌ Failed to create executor for {skill.skillName}");
        }
    }

    bool CheckSkillRequirements(SkillModule skill, Character player)
    {
        if (enableDetailedDebug)
        {
            Debug.Log($"🔍 Checking requirements for {skill.skillName}:");
            Debug.Log($"   • Skill mana cost: {skill.manaCost}");
            Debug.Log($"   • Skill required level: {skill.requiredLevel}");
            
            if (player.mana != null)
            {
                Debug.Log($"   • Player current mana: {player.mana.currentValue}/{player.mana.maxValue}");
            }
        }

        // Check mana requirement
        if (player.mana != null && player.mana.currentValue < skill.manaCost)
        {
            if (autoFixManaIssues)
            {
                Debug.Log($"🔧 Auto-fixing mana issue: restoring full mana");
                player.mana.Initialize(player.mana.maxValue, player.mana.regenRate);
                return true;
            }
            return false;
        }

        // Check level requirement (bypass if enabled)
        if (!bypassLevelRequirements)
        {
            if (skillManager != null && skillManager.GetPlayerLevel() < skill.requiredLevel)
            {
                return false;
            }
        }

        // Check player status
        if (player.isStunned || player.isBeingKnockedBack)
        {
            return false;
        }

        return true;
    }

    // Public API Methods - Enhanced with complete ModularSkillManager integration
    public static void AssignSkillToKey(KeyCode key, SkillModule skill)
    {
        if (skill == null)
        {
            Debug.LogError("Cannot assign null skill");
            return;
        }

        // Ensure instance exists
        var system = Instance;

        Debug.Log($"🎯 Starting assignment: {skill.skillName} → {key}");

        // Remove old assignments for this skill from both systems
        RemoveSkillFromAllKeys(skill);

        // Remove old assignment for this key from both systems
        if (assignedHotkeys.ContainsKey(key))
        {
            var oldSkill = assignedHotkeys[key];
            Debug.Log($"⚠️ Key {key} was assigned to {oldSkill.skillName}, reassigning to {skill.skillName}");
            
            // Remove old skill from ModularSkillManager
            if (system.skillManager != null)
            {
                int oldSlotIndex = system.skillManager.GetSkillSlotIndex(oldSkill);
                if (oldSlotIndex != -1)
                {
                    system.skillManager.UnequipSkill(oldSlotIndex);
                    Debug.Log($"🔄 Unequipped {oldSkill.skillName} from legacy slot {oldSlotIndex}");
                }
            }
        }

        // Assign to unified system
        assignedHotkeys[key] = skill;

        // CRITICAL FIX: Use new dynamic hotkey assignment for ModularSkillManager
        if (system.skillManager != null)
        {
            bool success = system.skillManager.AssignSkillToHotkey(skill, key);
            if (success)
            {
                Debug.Log($"🔗 Successfully synced {skill.skillName} with ModularSkillManager for key {key}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to sync {skill.skillName} with ModularSkillManager for key {key}");
            }
        }
        
        Debug.Log($"✅ Assignment complete: {skill.skillName} → {key}");
    }

    public static void RemoveKeyAssignment(KeyCode key)
    {
        if (assignedHotkeys.ContainsKey(key))
        {
            var skill = assignedHotkeys[key];
            assignedHotkeys.Remove(key);
            
            // Also remove from ModularSkillManager
            var system = Instance;
            if (system.skillManager != null)
            {
                var slot = system.skillManager.GetSlotByHotkey(key);
                if (slot != null && slot.HasSkill() && slot.equippedSkill == skill)
                {
                    system.skillManager.UnequipSkill(slot.slotIndex);
                    Debug.Log($"🔄 Also unequipped {skill.skillName} from ModularSkillManager slot {slot.slotIndex}");
                }
            }
            
            Debug.Log($"🗑️ Removed key {key} assignment for {skill.skillName}");
        }
    }

    public static KeyCode GetKeyForSkill(SkillModule skill)
    {
        foreach (var kvp in assignedHotkeys)
        {
            if (kvp.Value == skill)
                return kvp.Key;
        }
        return KeyCode.None;
    }

    public static void RemoveSkillFromAllKeys(SkillModule skill)
    {
        var keysToRemove = new List<KeyCode>();
        foreach (var kvp in assignedHotkeys)
        {
            if (kvp.Value == skill)
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
        {
            assignedHotkeys.Remove(key);
        }
    }

    public static Dictionary<KeyCode, SkillModule> GetAllAssignedHotkeys()
    {
        return new Dictionary<KeyCode, SkillModule>(assignedHotkeys);
    }

    public static void ClearAllAssignments()
    {
        assignedHotkeys.Clear();
        Debug.Log("🗑️ Cleared all skill assignments");
    }

    // Debug methods
    [ContextMenu("🔍 Debug All Assignments")]
    public void DebugAllAssignments()
    {
        Debug.Log("=== UNIFIED SKILL SYSTEM DEBUG ===");
        Debug.Log($"Total assigned hotkeys: {assignedHotkeys.Count}");
        Debug.Log($"Available skills: {availableSkills.Count}");
        Debug.Log($"ModularSkillManager found: {skillManager != null}");
        
        if (assignedHotkeys.Count > 0)
        {
            Debug.Log("Current assignments:");
            foreach (var kvp in assignedHotkeys)
            {
                Debug.Log($"  {kvp.Key} → {kvp.Value.skillName}");
            }
        }
        else
        {
            Debug.Log("No skills currently assigned to hotkeys");
        }
        
        Debug.Log("Available skills:");
        for (int i = 0; i < availableSkills.Count; i++)
        {
            Debug.Log($"  {i}: {availableSkills[i].skillName}");
        }

        // Also debug ModularSkillManager if available
        if (skillManager != null)
        {
            skillManager.DebugSkillAssignments();
        }
        
        Debug.Log("=== END DEBUG ===");
    }

    [ContextMenu("🔄 Force Sync with ModularSkillManager")]
    public void ForceSyncWithModularSkillManager()
    {
        if (skillManager == null)
        {
            FindSkillManager();
        }

        if (skillManager != null)
        {
            Debug.Log("🔄 Force syncing with ModularSkillManager...");
            
            // Clear current assignments
            assignedHotkeys.Clear();
            
            // Re-sync from manager
            AutoSetupSkillsFromManager();
            
            Debug.Log("✅ Sync complete!");
        }
        else
        {
            Debug.LogWarning("⚠️ ModularSkillManager not found!");
        }
    }

    [ContextMenu("🎯 Test Skill Assignment")]
    public void TestSkillAssignment()
    {
        if (availableSkills.Count > 0)
        {
            var testSkill = availableSkills[0];
            AssignSkillToKey(KeyCode.E, testSkill);
            Debug.Log($"🎯 Test: Assigned {testSkill.skillName} to key E");
        }
        else
        {
            Debug.LogWarning("⚠️ No available skills to test with");
        }
    }

    [ContextMenu("💙 Give Player Full Mana")]
    public void GivePlayerFullMana()
    {
        var player = FindFirstObjectByType<Character>();
        if (player != null && player.mana != null)
        {
            player.mana.Initialize(player.mana.maxValue, player.mana.regenRate);
            Debug.Log($"💙 Restored full mana: {player.mana.currentValue}/{player.mana.maxValue}");
        }
    }

    [ContextMenu("🎮 Toggle Auto-Equip Skills")]
    public void ToggleAutoEquipSkills()
    {
        autoEquipSkillsOnStart = !autoEquipSkillsOnStart;
        Debug.Log($"🎮 Auto-equip skills on start: {autoEquipSkillsOnStart}");
    }
}

/// <summary>
/// SIMPLIFIED SkillDetailUI - Uses SimpleHotkeyChanger for direct hotkey modification
/// NO MORE COMPLEX OVERLAY SYSTEMS!
/// </summary>
public class SkillDetailUI : MonoBehaviour
{
    [Header("Skill Information References")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillDescription;

    [Header("Skill Stats References")]
    [SerializeField] private TextMeshProUGUI statDamage;
    [SerializeField] private TextMeshProUGUI statRange;
    [SerializeField] private TextMeshProUGUI statCooldown;
    [SerializeField] private TextMeshProUGUI statManaCost;
    [SerializeField] private TextMeshProUGUI statSpecialEffects;

    [Header("Key Binding System")]
    [SerializeField] private TextMeshProUGUI currentKeyText;
    [SerializeField] private Button assignKeyButton;
    [SerializeField] private Button clearKeyButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private float keyDetectionTimeout = 10f;
    [SerializeField] private bool autoFindComponents = true;

    private SkillModule currentSkill;
    private bool isKeyAssignmentMode = false;
    private bool waitingForKeyInput = false;
    private float keyDetectionStartTime;
    
    // Simple system reference
    private SimpleHotkeyChanger hotkeyChanger;
    
    // Forbidden keys
    private static List<KeyCode> forbiddenKeys = new List<KeyCode>
    {
        KeyCode.Escape, KeyCode.Tab, KeyCode.Return, 
        KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftControl, KeyCode.RightControl, 
        KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.LeftCommand, KeyCode.RightCommand
    };

    void Awake()
    {
        if (autoFindComponents)
        {
            AutoFindUIComponents();
        }
        SetupButtonEvents();
    }

    void Start()
    {
        // Find SimpleHotkeyChanger
        hotkeyChanger = FindFirstObjectByType<SimpleHotkeyChanger>();
        if (hotkeyChanger == null)
        {
            Debug.LogWarning("⚠️ No SimpleHotkeyChanger found! Creating one...");
            var go = new GameObject("SimpleHotkeyChanger");
            hotkeyChanger = go.AddComponent<SimpleHotkeyChanger>();
        }
        
        // Subscribe to hotkey change events
        SimpleHotkeyChanger.OnHotkeyChanged += OnHotkeyChangedCallback;
        
        // Hide panel by default
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (SimpleHotkeyChanger.OnHotkeyChanged != null)
        {
            SimpleHotkeyChanger.OnHotkeyChanged -= OnHotkeyChangedCallback;
        }
    }
    
    private void OnHotkeyChangedCallback(SkillModule skill, KeyCode newKey)
    {
        // Update UI if this is the current skill
        if (currentSkill == skill)
        {
            UpdateCurrentKeyBinding();
            UpdateButtonStates();
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isKeyAssignmentMode)
            {
                ExitKeyAssignmentMode();
            }
            else
            {
                ClosePanel();
            }
        }

        // Update current key binding display
        if (currentSkill != null)
        {
            UpdateCurrentKeyBinding();
        }

        // Handle key assignment input
        if (isKeyAssignmentMode && waitingForKeyInput)
        {
            HandleKeyAssignmentInput();
        }
    }

    void AutoFindUIComponents()
    {
        Debug.Log("Auto-finding UI components for SkillDetailUI...");

        // Find components by name
        skillIcon = skillIcon ?? transform.Find("SkillIcon")?.GetComponent<Image>();
        skillName = skillName ?? transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        skillDescription = skillDescription ?? transform.Find("SkillDescription")?.GetComponent<TextMeshProUGUI>();
        
        statDamage = statDamage ?? transform.Find("StatDamage")?.GetComponent<TextMeshProUGUI>();
        statRange = statRange ?? transform.Find("StatRange")?.GetComponent<TextMeshProUGUI>();
        statCooldown = statCooldown ?? transform.Find("StatCooldown")?.GetComponent<TextMeshProUGUI>();
        statManaCost = statManaCost ?? transform.Find("StatManaCost")?.GetComponent<TextMeshProUGUI>();
        statSpecialEffects = statSpecialEffects ?? transform.Find("StatSpecialEffects")?.GetComponent<TextMeshProUGUI>();
        
        currentKeyText = currentKeyText ?? transform.Find("CurrentKeyText")?.GetComponent<TextMeshProUGUI>();
        assignKeyButton = assignKeyButton ?? transform.Find("AssignKeyButton")?.GetComponent<Button>();
        clearKeyButton = clearKeyButton ?? transform.Find("ClearKeyButton")?.GetComponent<Button>();
        closeButton = closeButton ?? transform.Find("CloseButton")?.GetComponent<Button>();

        Debug.Log("Auto-find complete for SkillDetailUI");
    }

    void SetupButtonEvents()
    {
        if (assignKeyButton != null)
            assignKeyButton.onClick.AddListener(OnAssignKeyClicked);
        if (clearKeyButton != null)
            clearKeyButton.onClick.AddListener(OnClearKeyClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void ShowSkillDetail(SkillModule skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("Cannot show detail for null skill");
            return;
        }

        currentSkill = skill;
        gameObject.SetActive(true);
        isKeyAssignmentMode = false;

        UpdateSkillDisplay();
        UpdateButtonStates();
        
        Debug.Log($"Showing detail for skill: {skill.skillName}");
    }

    void UpdateSkillDisplay()
    {
        if (currentSkill == null) return;

        // Update skill info
        if (skillIcon != null)
        {
            if (currentSkill.skillIcon != null)
            {
                skillIcon.sprite = currentSkill.skillIcon;
                skillIcon.color = Color.white;
            }
            else
            {
                skillIcon.color = currentSkill.skillColor;
            }
        }

        if (skillName != null)
            skillName.text = currentSkill.skillName;
        if (skillDescription != null)
            skillDescription.text = currentSkill.description;

        // Update stats
        if (statDamage != null)
            statDamage.text = $"Damage: {currentSkill.damage}";
        if (statRange != null)
            statRange.text = $"Range: {currentSkill.range}";
        if (statCooldown != null)
            statCooldown.text = $"Cooldown: {currentSkill.cooldown}s";
        if (statManaCost != null)
            statManaCost.text = $"Mana Cost: {currentSkill.manaCost}";

        // Update special effects
        if (statSpecialEffects != null)
        {
            string effects = "";
            if (currentSkill.stunDuration > 0)
                effects += $"Stun: {currentSkill.stunDuration}s ";
            if (currentSkill.knockbackForce > 0)
                effects += $"Knockback: {currentSkill.knockbackForce} ";
            if (currentSkill.healAmount > 0)
                effects += $"Heal: {currentSkill.healAmount} ";
            if (currentSkill.areaRadius > 0)
                effects += $"Area: {currentSkill.areaRadius} ";
            statSpecialEffects.text = string.IsNullOrEmpty(effects) ? "None" : effects;
        }

        UpdateCurrentKeyBinding();
    }

    void UpdateCurrentKeyBinding()
    {
        if (currentKeyText == null || currentSkill == null || hotkeyChanger == null) return;

        // Get current hotkey from SimpleHotkeyChanger
        KeyCode assignedKey = hotkeyChanger.GetSkillHotkey(currentSkill);
        
        if (assignedKey != KeyCode.None)
        {
            currentKeyText.text = $"Assigned Key: {GetKeyDisplayName(assignedKey)}";
        }
        else
        {
            currentKeyText.text = "No key assigned";
        }
    }

    void UpdateButtonStates()
    {
        if (currentSkill == null || hotkeyChanger == null) return;

        bool hasKey = hotkeyChanger.GetSkillHotkey(currentSkill) != KeyCode.None;
        
        if (clearKeyButton != null)
            clearKeyButton.gameObject.SetActive(hasKey);
    }

    void OnAssignKeyClicked()
    {
        if (currentSkill == null)
        {
            Debug.LogWarning("No skill selected for key assignment");
            return;
        }

        if (isKeyAssignmentMode)
        {
            ExitKeyAssignmentMode();
        }
        else
        {
            EnterKeyAssignmentMode();
        }
    }

    void OnClearKeyClicked()
    {
        if (currentSkill == null || hotkeyChanger == null) return;

        bool success = hotkeyChanger.RemoveSkill(currentSkill);
        if (success)
        {
            Debug.Log($"✅ CLEARED: Removed {currentSkill.skillName} from hotkey");
            UpdateCurrentKeyBinding();
            UpdateButtonStates();
        }
    }

    void EnterKeyAssignmentMode()
    {
        isKeyAssignmentMode = true;
        waitingForKeyInput = true;
        keyDetectionStartTime = Time.time;

        if (assignKeyButton != null)
        {
            var buttonText = assignKeyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Cancel";
        }

        if (currentKeyText != null)
            currentKeyText.text = "Press ANY key to assign (ESC to cancel)";

        Debug.Log($"🎯 SIMPLE ASSIGNMENT: Press key for {currentSkill.skillName}");
    }

    void ExitKeyAssignmentMode()
    {
        isKeyAssignmentMode = false;
        waitingForKeyInput = false;

        if (assignKeyButton != null)
        {
            var buttonText = assignKeyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Assign Key";
        }

        UpdateCurrentKeyBinding();
        Debug.Log("Exited key assignment mode");
    }

    void HandleKeyAssignmentInput()
    {
        // Check timeout
        if (Time.time - keyDetectionStartTime > keyDetectionTimeout)
        {
            Debug.Log("Key assignment timeout");
            ExitKeyAssignmentMode();
            return;
        }

        // Check for any key press
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                if (key == KeyCode.Escape)
                {
                    ExitKeyAssignmentMode();
                    return;
                }

                if (IsKeyUsable(key))
                {
                    // Use SimpleHotkeyChanger for direct assignment
                    bool success = hotkeyChanger.ChangeSkillHotkey(currentSkill, key);
                    if (success)
                    {
                        Debug.Log($"✅ SIMPLE SUCCESS: {currentSkill.skillName} → {key}");
                    }
                    else
                    {
                        Debug.LogWarning($"❌ FAILED: Could not assign {currentSkill.skillName} to {key}");
                    }
                    ExitKeyAssignmentMode();
                }
                else
                {
                    Debug.LogWarning($"Key {key} cannot be used for skills");
                }
                return;
            }
        }
    }

    bool IsKeyUsable(KeyCode key)
    {
        if (forbiddenKeys.Contains(key))
            return false;
        if (key == KeyCode.Mouse3 || key == KeyCode.Mouse4 || key == KeyCode.Mouse5 || key == KeyCode.Mouse6)
            return false;
        return true;
    }

    string GetKeyDisplayName(KeyCode key)
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
            case KeyCode.Space: return "Space";
            case KeyCode.Mouse0: return "Left Click";
            case KeyCode.Mouse1: return "Right Click";
            case KeyCode.Mouse2: return "Middle Click";
            default: return key.ToString();
        }
    }

    public void ClosePanel()
    {
        if (isKeyAssignmentMode)
        {
            ExitKeyAssignmentMode();
        }
        
        gameObject.SetActive(false);
        currentSkill = null;
        Debug.Log("SkillDetailUI panel closed");
    }

    // Public methods for compatibility
    public bool IsVisible() => gameObject.activeInHierarchy;
    public SkillModule GetCurrentSkill() => currentSkill;

    [ContextMenu("🎯 Test Simple Assignment")]
    public void TestSimpleAssignment()
    {
        if (currentSkill != null && hotkeyChanger != null)
        {
            bool success = hotkeyChanger.ChangeSkillHotkey(currentSkill, KeyCode.E);
            Debug.Log($"🎯 Test result: {(success ? "SUCCESS" : "FAILED")} - {currentSkill.skillName} → E");
        }
    }

    [ContextMenu("🔧 Debug Simple System")]
    public void DebugSimpleSystem()
    {
        Debug.Log("=== SIMPLE SKILLDETAILUI DEBUG ===");
        
        if (hotkeyChanger != null)
        {
            hotkeyChanger.DebugCurrentAssignments();
        }
        else
        {
            Debug.LogError("No SimpleHotkeyChanger found!");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
    
    /// <summary>
    /// Test component compatibility (for UIManager)
    /// </summary>
    [ContextMenu("🔧 Test Component")]
    public void TestComponent()
    {
        Debug.Log("=== TESTING SIMPLIFIED SKILLDETAILUI ===");
        Debug.Log($"SimpleHotkeyChanger found: {hotkeyChanger != null}");
        Debug.Log($"Current skill: {currentSkill?.skillName ?? "None"}");
        Debug.Log($"Panel visible: {IsVisible()}");
        Debug.Log("=== TEST COMPLETE ===");
    }
}