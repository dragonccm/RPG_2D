using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Comprehensive Diagnostic Tool for Skill System
/// Run this to diagnose "không có thông tin hi?n th?" issues
/// </summary>
public class SkillSystemDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [SerializeField] private bool runDiagnosticOnStart = true;
    [SerializeField] private bool detailedLogging = true;
    
    void Start()
    {
        if (runDiagnosticOnStart)
        {
            Invoke("RunFullDiagnostic", 1f); // Delay ?? cho systems initialize
        }
    }
    
    [ContextMenu("?? RUN FULL DIAGNOSTIC")]
    public void RunFullDiagnostic()
    {
        Debug.Log("=".PadRight(80, '='));
        Debug.Log("?? SKILL SYSTEM COMPREHENSIVE DIAGNOSTIC");
        Debug.Log("=".PadRight(80, '='));
        
        CheckModularSkillManager();
        CheckSkillPanelUI();
        CheckSkillDetailUI();
        CheckUIManager();
        CheckCanvas();
        CheckPrefabs();
        CheckSimpleHotkeyChanger();
        
        Debug.Log("=".PadRight(80, '='));
        Debug.Log("?? DIAGNOSTIC COMPLETE");
        Debug.Log("=".PadRight(80, '='));
    }
    
    private void CheckModularSkillManager()
    {
        Debug.Log("?? CHECKING MODULAR SKILL MANAGER");
        Debug.Log("-".PadRight(40, '-'));
        
        var skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("? ModularSkillManager NOT FOUND!");
            Debug.LogError("   ? Create ModularSkillManager component in scene");
            return;
        }
        
        Debug.Log($"? ModularSkillManager found: {skillManager.gameObject.name}");
        
        // Check available skills
        var availableSkills = skillManager.GetAvailableSkills();
        Debug.Log($"?? Available Skills Count: {availableSkills.Count}");
        
        if (availableSkills.Count == 0)
        {
            Debug.LogWarning("?? NO AVAILABLE SKILLS!");
            Debug.LogWarning("   ? Add SkillModule assets to ModularSkillManager.availableSkills");
            Debug.LogWarning("   ? Create SkillModule: Assets > Create > RPG > Skill Module");
        }
        else
        {
            Debug.Log("? Available Skills:");
            for (int i = 0; i < availableSkills.Count; i++)
            {
                var skill = availableSkills[i];
                if (skill != null)
                {
                    Debug.Log($"   {i}: {skill.skillName} (Lv.{skill.requiredLevel})");
                }
                else
                {
                    Debug.LogWarning($"   {i}: NULL SKILL");
                }
            }
        }
        
        // Check skill slots
        var unlockedSlots = skillManager.GetUnlockedSlots();
        Debug.Log($"?? Unlocked Slots Count: {unlockedSlots.Count}");
        
        foreach (var slot in unlockedSlots)
        {
            if (slot.HasSkill())
            {
                Debug.Log($"   Slot {slot.slotIndex}: {slot.equippedSkill.skillName} ? {slot.GetHotkey()}");
            }
            else
            {
                Debug.Log($"   Slot {slot.slotIndex}: EMPTY ? {slot.GetHotkey()}");
            }
        }
        
        // Check player level
        int playerLevel = skillManager.GetPlayerLevel();
        Debug.Log($"?? Player Level: {playerLevel}");
    }
    
    private void CheckSkillPanelUI()
    {
        Debug.Log("\n?? CHECKING SKILL PANEL UI");
        Debug.Log("-".PadRight(40, '-'));
        
        var skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
        if (skillPanelUI == null)
        {
            Debug.LogError("? SkillPanelUI NOT FOUND!");
            Debug.LogError("   ? Add SkillPanelUI component to Canvas");
            return;
        }
        
        Debug.Log($"? SkillPanelUI found: {skillPanelUI.gameObject.name}");
        Debug.Log($"   Active: {skillPanelUI.gameObject.activeInHierarchy}");
        Debug.Log($"   Visible: {skillPanelUI.IsVisible()}");
        
        // Check UI components
        Debug.Log("?? Checking UI Components:");
        
        // Use reflection to check private fields
        var skillPanelType = typeof(SkillPanelUI);
        var headerField = skillPanelType.GetField("header", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var instructionField = skillPanelType.GetField("instructionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scrollViewField = skillPanelType.GetField("skillScrollView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var contentField = skillPanelType.GetField("skillListContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var closeButtonField = skillPanelType.GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var prefabField = skillPanelType.GetField("skillItemPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var skillItemsField = skillPanelType.GetField("skillItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var header = headerField?.GetValue(skillPanelUI);
        var instructionText = instructionField?.GetValue(skillPanelUI);
        var scrollView = scrollViewField?.GetValue(skillPanelUI);
        var content = contentField?.GetValue(skillPanelUI);
        var closeButton = closeButtonField?.GetValue(skillPanelUI);
        var prefab = prefabField?.GetValue(skillPanelUI);
        var skillItems = skillItemsField?.GetValue(skillPanelUI) as List<GameObject>;
        
        Debug.Log($"   Header: {(header != null ? "?" : "?")}");
        Debug.Log($"   Instruction Text: {(instructionText != null ? "?" : "?")}");
        Debug.Log($"   Scroll View: {(scrollView != null ? "?" : "?")}");
        Debug.Log($"   Content: {(content != null ? "?" : "?")}");
        Debug.Log($"   Close Button: {(closeButton != null ? "?" : "?")}");
        Debug.Log($"   Skill Item Prefab: {(prefab != null ? "?" : "?")}");
        
        if (skillItems != null)
        {
            Debug.Log($"?? Skill Items Created: {skillItems.Count}");
            
            if (skillItems.Count == 0)
            {
                Debug.LogWarning("?? NO SKILL ITEMS CREATED!");
                Debug.LogWarning("   ? Check if ModularSkillManager has available skills");
                Debug.LogWarning("   ? Check if autoCreateSkillItems is enabled");
                Debug.LogWarning("   ? Try right-click SkillPanelUI ? 'Recreate Skill Items'");
            }
        }
        
        // Test prefab population if available
        if (prefab != null)
        {
            Debug.Log("?? Testing prefab population...");
            skillPanelUI.TestPrefabPopulation();
        }
    }
    
    private void CheckSkillDetailUI()
    {
        Debug.Log("\n?? CHECKING SKILL DETAIL UI");
        Debug.Log("-".PadRight(40, '-'));
        
        var skillDetailUI = FindFirstObjectByType<SkillDetailUI>();
        if (skillDetailUI == null)
        {
            Debug.LogError("? SkillDetailUI NOT FOUND!");
            Debug.LogError("   ? Add SkillDetailUI component to Canvas");
            return;
        }
        
        Debug.Log($"? SkillDetailUI found: {skillDetailUI.gameObject.name}");
        Debug.Log($"   Active: {skillDetailUI.gameObject.activeInHierarchy}");
        Debug.Log($"   Visible: {skillDetailUI.IsVisible()}");
    }
    
    private void CheckUIManager()
    {
        Debug.Log("\n?? CHECKING UI MANAGER");
        Debug.Log("-".PadRight(40, '-'));
        
        var uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("?? UIManager NOT FOUND!");
            Debug.LogWarning("   ? UIManager is optional but recommended");
            return;
        }
        
        Debug.Log($"? UIManager found: {uiManager.gameObject.name}");
        
        // Test UI Manager
        if (uiManager != null)
        {
            uiManager.TestAllUIComponents();
        }
    }
    
    private void CheckCanvas()
    {
        Debug.Log("\n??? CHECKING CANVAS SETUP");
        Debug.Log("-".PadRight(40, '-'));
        
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("? NO CANVAS FOUND!");
            Debug.LogError("   ? Create Canvas in scene");
            return;
        }
        
        Debug.Log($"? Canvas found: {canvas.gameObject.name}");
        Debug.Log($"   Render Mode: {canvas.renderMode}");
        Debug.Log($"   Canvas Scaler: {(canvas.GetComponent<UnityEngine.UI.CanvasScaler>() != null ? "?" : "?")}");
        
        // Check for skill UI components in canvas
        var skillPanelInCanvas = canvas.transform.Find("SkillPanelUI");
        var skillDetailInCanvas = canvas.transform.Find("SkillDetailUI");
        
        Debug.Log($"   SkillPanelUI in Canvas: {(skillPanelInCanvas != null ? "?" : "?")}");
        Debug.Log($"   SkillDetailUI in Canvas: {(skillDetailInCanvas != null ? "?" : "?")}");
    }
    
    private void CheckPrefabs()
    {
        Debug.Log("\n?? CHECKING PREFABS");
        Debug.Log("-".PadRight(40, '-'));
        
        var skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
        if (skillPanelUI != null)
        {
            // Get prefab field using reflection
            var skillPanelType = typeof(SkillPanelUI);
            var prefabField = skillPanelType.GetField("skillItemPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var prefab = prefabField?.GetValue(skillPanelUI) as GameObject;
            
            if (prefab != null)
            {
                Debug.Log($"? Skill Item Prefab assigned: {prefab.name}");
                
                // Check prefab structure
                CheckPrefabStructure(prefab);
            }
            else
            {
                Debug.LogWarning("?? No Skill Item Prefab assigned!");
                Debug.LogWarning("   ? Assign prefab to SkillPanelUI.skillItemPrefab");
                Debug.LogWarning("   ? Or disable prefab to use auto-generated items");
            }
        }
    }
    
    private void CheckPrefabStructure(GameObject prefab)
    {
        Debug.Log("?? Checking Prefab Structure:");
        
        // Expected structure based on user description:
        // img > SkillIconBackgr > SkillIcon
        // SkillDetail > SkillName, LevelReq, Description
        
        var img = prefab.transform.Find("img");
        Debug.Log($"   img: {(img != null ? "?" : "?")}");
        
        if (img != null)
        {
            var skillIconBackgr = img.Find("SkillIconBackgr");
            Debug.Log($"   img/SkillIconBackgr: {(skillIconBackgr != null ? "?" : "?")}");
            
            if (skillIconBackgr != null)
            {
                var skillIcon = skillIconBackgr.Find("SkillIcon");
                Debug.Log($"   img/SkillIconBackgr/SkillIcon: {(skillIcon != null ? "?" : "?")}");
            }
        }
        
        var skillDetail = prefab.transform.Find("SkillDetail");
        Debug.Log($"   SkillDetail: {(skillDetail != null ? "?" : "?")}");
        
        if (skillDetail != null)
        {
            var skillName = skillDetail.Find("SkillName");
            var levelReq = skillDetail.Find("LevelReq");
            var description = skillDetail.Find("Description");
            
            Debug.Log($"   SkillDetail/SkillName: {(skillName != null ? "?" : "?")}");
            Debug.Log($"   SkillDetail/LevelReq: {(levelReq != null ? "?" : "?")}");
            Debug.Log($"   SkillDetail/Description: {(description != null ? "?" : "?")}");
        }
        
        // Check for SkillItemPrefabHandler
        var handler = prefab.GetComponent<SkillItemPrefabHandler>();
        Debug.Log($"   SkillItemPrefabHandler: {(handler != null ? "?" : "?? Will be added automatically")}");
    }
    
    private void CheckSimpleHotkeyChanger()
    {
        Debug.Log("\n?? CHECKING SIMPLE HOTKEY CHANGER");
        Debug.Log("-".PadRight(40, '-'));
        
        var hotkeyChanger = FindFirstObjectByType<SimpleHotkeyChanger>();
        if (hotkeyChanger == null)
        {
            Debug.LogWarning("?? SimpleHotkeyChanger NOT FOUND!");
            Debug.LogWarning("   ? Add SimpleHotkeyChanger component to scene");
            Debug.LogWarning("   ? Or it will be created automatically by SkillDetailUI");
        }
        else
        {
            Debug.Log($"? SimpleHotkeyChanger found: {hotkeyChanger.gameObject.name}");
            hotkeyChanger.DebugCurrentAssignments();
        }
    }
    
    [ContextMenu("?? FIX MISSING COMPONENTS")]
    public void FixMissingComponents()
    {
        Debug.Log("?? ATTEMPTING TO FIX MISSING COMPONENTS...");
        
        // Create ModularSkillManager if missing
        var skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager == null)
        {
            var player = FindFirstObjectByType<Character>();
            if (player != null)
            {
                skillManager = player.gameObject.AddComponent<ModularSkillManager>();
                Debug.Log("? Created ModularSkillManager on Player");
            }
            else
            {
                var newSkillManager = new GameObject("ModularSkillManager");
                skillManager = newSkillManager.AddComponent<ModularSkillManager>();
                Debug.Log("? Created ModularSkillManager GameObject");
            }
        }
        
        // Create Canvas if missing
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("? Created Canvas");
        }
        
        // Create SkillPanelUI if missing
        var skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
        if (skillPanelUI == null)
        {
            var skillPanelObj = new GameObject("SkillPanelUI");
            skillPanelObj.transform.SetParent(canvas.transform, false);
            skillPanelUI = skillPanelObj.AddComponent<SkillPanelUI>();
            
            // Setup RectTransform
            var rect = skillPanelObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Debug.Log("? Created SkillPanelUI");
        }
        
        // Create SkillDetailUI if missing
        var skillDetailUI = FindFirstObjectByType<SkillDetailUI>();
        if (skillDetailUI == null)
        {
            var skillDetailObj = new GameObject("SkillDetailUI");
            skillDetailObj.transform.SetParent(canvas.transform, false);
            skillDetailUI = skillDetailObj.AddComponent<SkillDetailUI>();
            
            // Setup RectTransform
            var rect = skillDetailObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.6f, 0.1f);
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Debug.Log("? Created SkillDetailUI");
        }
        
        // Create SimpleHotkeyChanger if missing
        var hotkeyChanger = FindFirstObjectByType<SimpleHotkeyChanger>();
        if (hotkeyChanger == null)
        {
            var hotkeyObj = new GameObject("SimpleHotkeyChanger");
            hotkeyChanger = hotkeyObj.AddComponent<SimpleHotkeyChanger>();
            Debug.Log("? Created SimpleHotkeyChanger");
        }
        
        Debug.Log("?? FIX COMPLETE - Run diagnostic again to verify");
    }
    
    [ContextMenu("?? TEST SKILL CREATION")]
    public void TestSkillCreation()
    {
        Debug.Log("?? TESTING SKILL CREATION...");
        
        var skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("No ModularSkillManager found!");
            return;
        }
        
        // Create test skill if no skills available
        var availableSkills = skillManager.GetAvailableSkills();
        if (availableSkills.Count == 0)
        {
            Debug.Log("Creating test SkillModule...");
            
            // Create test skill asset
            var testSkill = ScriptableObject.CreateInstance<SkillModule>();
            testSkill.skillName = "Test Sword Strike";
            testSkill.description = "A basic sword attack for testing";
            testSkill.damage = 25f;
            testSkill.range = 2f;
            testSkill.cooldown = 1f;
            testSkill.manaCost = 10f;
            testSkill.requiredLevel = 1;
            testSkill.skillType = SkillType.Melee;
            testSkill.skillColor = Color.red;
            
            // Add to available skills (using reflection)
            var skillManagerType = typeof(ModularSkillManager);
            var availableSkillsField = skillManagerType.GetField("availableSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (availableSkillsField != null)
            {
                var currentSkills = availableSkillsField.GetValue(skillManager) as List<SkillModule>;
                if (currentSkills == null)
                {
                    currentSkills = new List<SkillModule>();
                    availableSkillsField.SetValue(skillManager, currentSkills);
                }
                currentSkills.Add(testSkill);
                Debug.Log($"? Added test skill: {testSkill.skillName}");
            }
        }
        
        // Test skill panel creation
        var skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
        if (skillPanelUI != null)
        {
            skillPanelUI.RecreateSkillItems();
            Debug.Log("? Recreated skill items");
        }
    }
}