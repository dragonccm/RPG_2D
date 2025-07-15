using UnityEngine;

/// <summary>
/// Universal Hotkey Manager - Works with UnifiedSkillSystem MonoBehaviour
/// </summary>
public class UniversalHotkeyManager : MonoBehaviour
{
    [Header("Universal Hotkey System")]
    [SerializeField] private bool enableUniversalHotkeys = true;
    
    private ModularSkillManager skillManager;
    private UnifiedSkillSystem unifiedSystemInstance;
    
    private void Awake()
    {
        // Find ModularSkillManager
        skillManager = FindFirstObjectByType<ModularSkillManager>();
        
        // Get UnifiedSkillSystem instance
        unifiedSystemInstance = UnifiedSkillSystem.Instance;
        
        if (skillManager != null)
        {
            Debug.Log("?? Found ModularSkillManager - integrating systems");
        }
        else
        {
            Debug.LogWarning("?? No ModularSkillManager found - UniversalHotkeys will work independently");
        }
    }
    
    private void Start()
    {
        Debug.Log($"?? UniversalHotkeyManager initialized. System enabled: {enableUniversalHotkeys}");
    }
    
    /// <summary>
    /// Public method to assign skill to key (for UI)
    /// </summary>
    public bool AssignSkillToKey(KeyCode key, SkillModule skill)
    {
        if (!enableUniversalHotkeys)
        {
            Debug.LogWarning("Universal hotkeys are disabled!");
            return false;
        }
        
        UnifiedSkillSystem.AssignSkillToKey(key, skill);
        return true;
    }
    
    /// <summary>
    /// Get skill assigned to key
    /// </summary>
    public SkillModule GetSkillForKey(KeyCode key)
    {
        if (unifiedSystemInstance != null)
        {
            // Look through the assignments to find skill for this key
            var assignments = UnifiedSkillSystem.GetAllAssignedHotkeys();
            return assignments.ContainsKey(key) ? assignments[key] : null;
        }
        return null;
    }
    
    /// <summary>
    /// Enable/disable universal hotkeys
    /// </summary>
    public void SetUniversalHotkeysEnabled(bool enabled)
    {
        enableUniversalHotkeys = enabled;
        Debug.Log($"Universal hotkeys {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Debug current assignments
    /// </summary>
    [ContextMenu("?? Debug Universal Assignments")]
    public void DebugAssignments()
    {
        if (unifiedSystemInstance != null)
        {
            unifiedSystemInstance.DebugAllAssignments();
        }
        else
        {
            Debug.LogWarning("No UnifiedSkillSystem instance found");
        }
    }
    
    /// <summary>
    /// Clear all assignments
    /// </summary>
    [ContextMenu("?? Clear All Assignments")]
    public void ClearAllAssignments()
    {
        UnifiedSkillSystem.ClearAllAssignments();
    }
}