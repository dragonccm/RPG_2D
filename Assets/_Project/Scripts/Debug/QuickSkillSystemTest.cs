using UnityEngine;

/// <summary>
/// Quick Test - Add this to any GameObject and press Space to test skill system
/// </summary>
public class QuickSkillSystemTest : MonoBehaviour
{
    [Header("Quick Test Controls")]
    [SerializeField] private KeyCode testKey = KeyCode.Space;
    [SerializeField] private KeyCode openPanelKey = KeyCode.Tab;
    
    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            QuickTest();
        }
        
        if (Input.GetKeyDown(openPanelKey))
        {
            QuickOpenSkillPanel();
        }
    }
    
    [ContextMenu("? QUICK TEST")]
    public void QuickTest()
    {
        Debug.Log("? QUICK SKILL SYSTEM TEST");
        Debug.Log("========================");
        
        // Test 1: Check ModularSkillManager
        var skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("? NO MODULAR SKILL MANAGER!");
            Debug.LogError("   ? Add ModularSkillManager component to Player");
            return;
        }
        Debug.Log($"? ModularSkillManager: {skillManager.gameObject.name}");
        
        // Test 2: Check Available Skills
        var skills = skillManager.GetAvailableSkills();
        Debug.Log($"?? Available Skills: {skills.Count}");
        
        if (skills.Count == 0)
        {
            Debug.LogWarning("?? NO SKILLS AVAILABLE!");
            Debug.LogWarning("   ? Create SkillModule: Assets > Create > RPG > Skill Module");
            Debug.LogWarning("   ? Assign skills to ModularSkillManager.availableSkills");
            CreateTestSkill(skillManager);
        }
        else
        {
            foreach (var skill in skills)
            {
                Debug.Log($"   - {skill.skillName} (Lv.{skill.requiredLevel})");
            }
        }
        
        // Test 3: Check SkillPanelUI
        var skillPanel = FindFirstObjectByType<SkillPanelUI>();
        if (skillPanel == null)
        {
            Debug.LogError("? NO SKILL PANEL UI!");
            Debug.LogError("   ? Add SkillPanelUI component to Canvas");
            return;
        }
        Debug.Log($"? SkillPanelUI: {skillPanel.gameObject.name}");
        
        // Test 4: Force Create Skill Items
        Debug.Log("?? Testing skill item creation...");
        skillPanel.RecreateSkillItems();
        
        Debug.Log("? QUICK TEST COMPLETE!");
        Debug.Log($"?? Press {openPanelKey} to open skill panel");
    }
    
    [ContextMenu("?? QUICK OPEN SKILL PANEL")]
    public void QuickOpenSkillPanel()
    {
        var skillPanel = FindFirstObjectByType<SkillPanelUI>();
        if (skillPanel == null)
        {
            Debug.LogError("? SkillPanelUI not found!");
            return;
        }
        
        if (skillPanel.IsVisible())
        {
            skillPanel.ClosePanel();
            Debug.Log("?? Closed skill panel");
        }
        else
        {
            skillPanel.OpenPanel();
            Debug.Log("?? Opened skill panel");
        }
    }
    
    private void CreateTestSkill(ModularSkillManager skillManager)
    {
        Debug.Log("?? Creating test skill...");
        
        // Create basic test skill
        var testSkill = ScriptableObject.CreateInstance<SkillModule>();
        testSkill.skillName = "Quick Test Strike";
        testSkill.description = "A test skill created automatically";
        testSkill.damage = 20f;
        testSkill.range = 2f;
        testSkill.cooldown = 1f;
        testSkill.manaCost = 5f;
        testSkill.requiredLevel = 1;
        testSkill.skillType = SkillType.Melee;
        testSkill.skillColor = Color.blue;
        
        // Add to skill manager using reflection
        var skillManagerType = typeof(ModularSkillManager);
        var availableSkillsField = skillManagerType.GetField("availableSkills", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (availableSkillsField != null)
        {
            var currentSkills = availableSkillsField.GetValue(skillManager) as System.Collections.Generic.List<SkillModule>;
            if (currentSkills == null)
            {
                currentSkills = new System.Collections.Generic.List<SkillModule>();
                availableSkillsField.SetValue(skillManager, currentSkills);
            }
            currentSkills.Add(testSkill);
            Debug.Log($"? Added test skill: {testSkill.skillName}");
        }
        else
        {
            Debug.LogError("? Could not access availableSkills field!");
        }
    }
    
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 200, 30), $"Quick Test ({testKey})"))
        {
            QuickTest();
        }
        
        if (GUI.Button(new Rect(10, 50, 200, 30), $"Toggle Panel ({openPanelKey})"))
        {
            QuickOpenSkillPanel();
        }
        
        GUI.Label(new Rect(10, 90, 300, 20), "Quick Skill System Tester");
    }
}