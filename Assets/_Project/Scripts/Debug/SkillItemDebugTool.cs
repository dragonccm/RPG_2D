using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug tool ?? test và inspect skill items trong menu SkillPanelUI
/// </summary>
public class SkillItemDebugTool : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField] private bool logSkillItemSizes = true;
    [SerializeField] private bool showOnScreenInfo = true;
    
    [Header("Test Controls")]
    [SerializeField] private KeyCode inspectKey = KeyCode.F1;
    [SerializeField] private KeyCode fixSizesKey = KeyCode.F2;
    [SerializeField] private KeyCode recreateItemsKey = KeyCode.F3;
    
    private SkillPanelUI skillPanelUI;
    
    void Start()
    {
        skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
    }
    
    void Update()
    {
        if (!enableDebugMode) return;
        
        if (Input.GetKeyDown(inspectKey))
        {
            InspectSkillItems();
        }
        
        if (Input.GetKeyDown(fixSizesKey))
        {
            FixSkillItemSizes();
        }
        
        if (Input.GetKeyDown(recreateItemsKey))
        {
            RecreateSkillItems();
        }
    }
    
    [ContextMenu("?? Inspect Skill Items")]
    public void InspectSkillItems()
    {
        Debug.Log("=== ?? INSPECTING SKILL ITEMS ===");
        
        if (skillPanelUI != null)
        {
            InspectSkillPanelItems();
        }
        else
        {
            Debug.LogError("? No SkillPanelUI found!");
        }
        
        Debug.Log("=== ? INSPECTION COMPLETE ===");
    }
    
    private void InspectSkillPanelItems()
    {
        Debug.Log("?? Inspecting SkillPanelUI items...");
        
        var skillManager = skillPanelUI.GetSkillManager();
        if (skillManager == null)
        {
            Debug.LogError("? SkillManager is null!");
            return;
        }
        
        var availableSkills = skillManager.GetAvailableSkills();
        Debug.Log($"Available skills: {availableSkills.Count}");
        
        // Find skill items in hierarchy
        var skillItems = GameObject.FindGameObjectsWithTag("SkillItem");
        if (skillItems.Length == 0)
        {
            // Try alternative search
            var skillItemComponents = FindObjectsByType<SkillItemComponent>(FindObjectsSortMode.None);
            skillItems = new GameObject[skillItemComponents.Length];
            for (int i = 0; i < skillItemComponents.Length; i++)
            {
                skillItems[i] = skillItemComponents[i].gameObject;
            }
        }
        
        Debug.Log($"Found {skillItems.Length} skill item GameObjects");
        
        foreach (var item in skillItems)
        {
            InspectSingleSkillItem(item);
        }
    }
    
    private void InspectSingleSkillItem(GameObject item)
    {
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning($"? {item.name} missing RectTransform!");
            return;
        }
        
        var layoutElement = item.GetComponent<LayoutElement>();
        var image = item.GetComponent<Image>();
        var button = item.GetComponent<Button>();
        
        Debug.Log($"?? {item.name}:");
        Debug.Log($"   Size: {rect.sizeDelta}");
        Debug.Log($"   Anchors: Min({rect.anchorMin}) Max({rect.anchorMax})");
        Debug.Log($"   Position: {rect.anchoredPosition}");
        Debug.Log($"   Active: {item.activeInHierarchy}");
        Debug.Log($"   LayoutElement: {(layoutElement != null ? $"Min:{layoutElement.minHeight} Pref:{layoutElement.preferredHeight}" : "None")}");
        Debug.Log($"   Image: {(image != null ? $"Color:{image.color}" : "None")}");
        Debug.Log($"   Button: {(button != null ? $"Interactable:{button.interactable}" : "None")}");
        
        if (logSkillItemSizes && rect.sizeDelta.magnitude < 10f)
        {
            Debug.LogError($"?? {item.name} has very small size: {rect.sizeDelta}");
        }
    }
    
    [ContextMenu("?? Fix Skill Item Sizes")]
    public void FixSkillItemSizes()
    {
        Debug.Log("=== ?? FIXING SKILL ITEM SIZES ===");
        
        // Fix SkillPanelUI items
        if (skillPanelUI != null)
        {
            FixSkillPanelItemSizes();
        }
        
        Debug.Log("=== ? SIZE FIXING COMPLETE ===");
    }
    
    private void FixSkillPanelItemSizes()
    {
        var skillItems = FindObjectsByType<SkillItemComponent>(FindObjectsSortMode.None);
        
        foreach (var skillItemComp in skillItems)
        {
            var rect = skillItemComp.GetComponent<RectTransform>();
            if (rect == null) continue;
            
            // Fix anchors and size
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 80);
            rect.anchoredPosition = Vector2.zero;
            
            // Ensure LayoutElement exists
            var layoutElement = skillItemComp.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = skillItemComp.gameObject.AddComponent<LayoutElement>();
            }
            
            layoutElement.minHeight = 80;
            layoutElement.preferredHeight = 80;
            layoutElement.flexibleWidth = 1;
            
            Debug.Log($"? Fixed size for {skillItemComp.gameObject.name}");
        }
    }
    
    [ContextMenu("?? Recreate Skill Items")]
    public void RecreateSkillItems()
    {
        Debug.Log("=== ?? RECREATING SKILL ITEMS ===");
        
        if (skillPanelUI != null)
        {
            skillPanelUI.RecreateSkillItems();
            Debug.Log("? Recreated SkillPanelUI items");
        }
        
        Debug.Log("=== ? RECREATION COMPLETE ===");
    }
    
    void OnGUI()
    {
        if (!enableDebugMode || !showOnScreenInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("?? Skill Item Debug Tool");
        GUILayout.Label($"F1 - Inspect Items | F2 - Fix Sizes | F3 - Recreate");
        
        if (skillPanelUI != null)
        {
            GUILayout.Label($"SkillPanelUI: {(skillPanelUI.IsVisible() ? "Visible" : "Hidden")}");
        }
        
        var skillItems = FindObjectsByType<SkillItemComponent>(FindObjectsSortMode.None);
        GUILayout.Label($"Skill Items: {skillItems.Length}");
        
        if (GUILayout.Button("?? Inspect Now"))
        {
            InspectSkillItems();
        }
        
        if (GUILayout.Button("?? Fix Sizes Now"))
        {
            FixSkillItemSizes();
        }
        
        GUILayout.EndArea();
    }
}