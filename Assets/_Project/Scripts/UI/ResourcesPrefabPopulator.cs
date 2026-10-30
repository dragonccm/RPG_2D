using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Resources Prefab Populator - Populates loaded prefab with skill data
/// Uses structure analysis from ResourcesPrefabLoader
/// </summary>
public class ResourcesPrefabPopulator : MonoBehaviour
{
    [Header("Population Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool autoRefreshOnEnable = true;
    
    [Header("Visual Settings")]
    [SerializeField] private Color availableColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color equippedColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    
    private SkillModule currentSkill;
    private SkillPanelUI parentPanel;
    private ModularSkillManager skillManager;
    private ResourcesPrefabLoader.PrefabStructureAnalysis structureAnalysis;
    private bool isInitialized = false;
    
    void Awake()
    {
        // Find required managers
        skillManager = FindFirstObjectByType<ModularSkillManager>();
    }
    
    void OnEnable()
    {
        if (autoRefreshOnEnable && isInitialized)
        {
            RefreshVisualState();
        }
    }
    
    /// <summary>
    /// Set structure analysis from ResourcesPrefabLoader
    /// </summary>
    public void SetStructureAnalysis(ResourcesPrefabLoader.PrefabStructureAnalysis analysis)
    {
        structureAnalysis = analysis;
        
        if (debugMode)
        {
            Debug.Log($"?? Structure analysis set for {gameObject.name}");
            Debug.Log($"   Valid Structure: {analysis.hasValidStructure}");
        }
    }
    
    /// <summary>
    /// Populate prefab with skill data using structure analysis
    /// </summary>
    public void PopulateWithSkillData(SkillModule skill, SkillPanelUI panel)
    {
        if (skill == null)
        {
            Debug.LogError("? Cannot populate with null skill!");
            return;
        }
        
        currentSkill = skill;
        parentPanel = panel;
        
        if (debugMode) Debug.Log($"?? POPULATING SKILL ITEM: {skill.skillName}");
        
        // Check if we have valid structure analysis
        if (!structureAnalysis.hasValidStructure)
        {
            Debug.LogWarning($"?? Invalid structure analysis for {gameObject.name}");
            Debug.LogWarning("   ? Attempting auto-analysis...");
            AttemptAutoAnalysis();
        }
        
        // Populate skill icon
        PopulateSkillIcon();
        
        // Populate skill name
        PopulateSkillName();
        
        // Populate level requirement
        PopulateLevelRequirement();
        
        // Populate description
        PopulateDescription();
        
        // Setup button interaction
        SetupButtonInteraction();
        
        // Update visual state
        RefreshVisualState();
        
        isInitialized = true;
        
        if (debugMode) Debug.Log($"? Population complete for {skill.skillName}");
    }
    
    /// <summary>
    /// Attempt to auto-analyze structure if not provided
    /// </summary>
    private void AttemptAutoAnalysis()
    {
        if (debugMode) Debug.Log("?? Attempting auto-analysis of prefab structure...");
        
        var analysis = new ResourcesPrefabLoader.PrefabStructureAnalysis();
        
        // Try to find components manually
        analysis.imgTransform = transform.Find("img");
        if (analysis.imgTransform != null)
        {
            analysis.skillIconBackgrTransform = analysis.imgTransform.Find("SkillIconBackgr");
            if (analysis.skillIconBackgrTransform != null)
            {
                analysis.skillIconBackground = analysis.skillIconBackgrTransform.GetComponent<Image>();
                analysis.skillIconTransform = analysis.skillIconBackgrTransform.Find("SkillIcon");
                if (analysis.skillIconTransform != null)
                {
                    analysis.skillIcon = analysis.skillIconTransform.GetComponent<Image>();
                }
            }
        }
        
        analysis.skillDetailTransform = transform.Find("SkillDetail");
        if (analysis.skillDetailTransform != null)
        {
            var skillNameTrans = analysis.skillDetailTransform.Find("SkillName");
            if (skillNameTrans != null)
                analysis.skillNameText = skillNameTrans.GetComponent<TextMeshProUGUI>();
                
            var levelReqTrans = analysis.skillDetailTransform.Find("LevelReq");
            if (levelReqTrans != null)
                analysis.levelRequirementText = levelReqTrans.GetComponent<TextMeshProUGUI>();
                
            var descTrans = analysis.skillDetailTransform.Find("Description");
            if (descTrans != null)
                analysis.descriptionText = descTrans.GetComponent<TextMeshProUGUI>();
        }
        
        analysis.itemButton = GetComponent<Button>();
        analysis.backgroundImage = GetComponent<Image>();
        
        analysis.hasValidStructure = analysis.skillIcon != null && analysis.skillNameText != null;
        
        structureAnalysis = analysis;
        
        if (debugMode)
        {
            Debug.Log($"?? Auto-analysis result: {(analysis.hasValidStructure ? "SUCCESS" : "FAILED")}");
        }
    }
    
    /// <summary>
    /// Populate skill icon and icon background
    /// </summary>
    private void PopulateSkillIcon()
    {
        if (structureAnalysis.skillIcon != null)
        {
            if (currentSkill.skillIcon != null)
            {
                structureAnalysis.skillIcon.sprite = currentSkill.skillIcon;
                structureAnalysis.skillIcon.color = Color.white;
                
                if (debugMode) Debug.Log($"   ? Set skill icon sprite: {currentSkill.skillIcon.name}");
            }
            else
            {
                structureAnalysis.skillIcon.sprite = null;
                structureAnalysis.skillIcon.color = currentSkill.skillColor;
                
                if (debugMode) Debug.Log($"   ? Set skill icon color: {currentSkill.skillColor}");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("   ? Skill icon component not found!");
        }
        
        // Set icon background color based on skill type
        if (structureAnalysis.skillIconBackground != null)
        {
            structureAnalysis.skillIconBackground.color = currentSkill.GetSkillTypeColor();
            
            if (debugMode) Debug.Log($"   ? Set icon background color: {currentSkill.GetSkillTypeColor()}");
        }
    }
    
    /// <summary>
    /// Populate skill name text
    /// </summary>
    private void PopulateSkillName()
    {
        if (structureAnalysis.skillNameText != null)
        {
            structureAnalysis.skillNameText.text = currentSkill.skillName;
            
            if (debugMode) Debug.Log($"   ? Set skill name: {currentSkill.skillName}");
        }
        else if (debugMode)
        {
            Debug.LogWarning("   ? Skill name text component not found!");
        }
    }
    
    /// <summary>
    /// Populate level requirement text
    /// </summary>
    private void PopulateLevelRequirement()
    {
        if (structureAnalysis.levelRequirementText != null)
        {
            structureAnalysis.levelRequirementText.text = $"Lv.{currentSkill.requiredLevel}";
            
            // Set color based on player level
            if (skillManager != null)
            {
                bool canUse = skillManager.GetPlayerLevel() >= currentSkill.requiredLevel;
                structureAnalysis.levelRequirementText.color = canUse ? Color.green : Color.red;
            }
            
            if (debugMode) Debug.Log($"   ? Set level requirement: Lv.{currentSkill.requiredLevel}");
        }
        else if (debugMode)
        {
            Debug.LogWarning("   ? Level requirement text component not found!");
        }
    }
    
    /// <summary>
    /// Populate description text
    /// </summary>
    private void PopulateDescription()
    {
        if (structureAnalysis.descriptionText != null)
        {
            structureAnalysis.descriptionText.text = currentSkill.description;
            
            // Set color based on availability
            if (skillManager != null)
            {
                bool canUse = skillManager.GetPlayerLevel() >= currentSkill.requiredLevel;
                structureAnalysis.descriptionText.color = canUse ? new Color(0.8f, 0.8f, 0.8f) : Color.gray;
            }
            
            if (debugMode) Debug.Log($"   ? Set description: {currentSkill.description}");
        }
        else if (debugMode)
        {
            Debug.LogWarning("   ? Description text component not found!");
        }
    }
    
    /// <summary>
    /// Setup button interaction
    /// </summary>
    private void SetupButtonInteraction()
    {
        if (structureAnalysis.itemButton != null)
        {
            // Clear existing listeners
            structureAnalysis.itemButton.onClick.RemoveAllListeners();
            
            // Add click listener
            structureAnalysis.itemButton.onClick.AddListener(OnItemClicked);
            
            // Ensure button is interactable
            structureAnalysis.itemButton.interactable = true;
            
            // Set target graphic for hover effects
            if (structureAnalysis.backgroundImage != null)
            {
                structureAnalysis.itemButton.targetGraphic = structureAnalysis.backgroundImage;
            }
            
            if (debugMode) Debug.Log($"   ? Setup button interaction");
        }
        else
        {
            // Add button component if missing
            var button = gameObject.GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
                structureAnalysis.itemButton = button;
            }
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnItemClicked);
            button.interactable = true;
            
            if (debugMode) Debug.Log($"   ? Created and setup button component");
        }
    }
    
    /// <summary>
    /// Handle item click
    /// </summary>
    private void OnItemClicked()
    {
        if (currentSkill == null || parentPanel == null)
        {
            Debug.LogWarning("? Cannot handle click - missing references!");
            return;
        }
        
        if (debugMode) Debug.Log($"??? Skill item clicked: {currentSkill.skillName}");
        
        parentPanel.OnSkillItemClicked(currentSkill);
    }
    
    /// <summary>
    /// Refresh visual state based on current conditions
    /// </summary>
    public void RefreshVisualState()
    {
        if (currentSkill == null || skillManager == null) return;
        
        bool isAvailable = skillManager.GetPlayerLevel() >= currentSkill.requiredLevel;
        bool isEquipped = IsSkillEquipped();
        
        Color targetColor;
        if (isEquipped)
        {
            targetColor = equippedColor;
        }
        else if (isAvailable)
        {
            targetColor = availableColor;
        }
        else
        {
            targetColor = lockedColor;
        }
        
        // Apply color to background
        if (structureAnalysis.backgroundImage != null)
        {
            structureAnalysis.backgroundImage.color = targetColor;
        }
        
        // Update text colors
        if (structureAnalysis.levelRequirementText != null)
        {
            structureAnalysis.levelRequirementText.color = isAvailable ? Color.green : Color.red;
        }
        
        if (structureAnalysis.skillNameText != null)
        {
            structureAnalysis.skillNameText.color = isAvailable ? Color.white : Color.gray;
        }
        
        if (structureAnalysis.descriptionText != null)
        {
            structureAnalysis.descriptionText.color = isAvailable ? new Color(0.8f, 0.8f, 0.8f) : Color.gray;
        }
        
        if (debugMode)
        {
            string state = isEquipped ? "EQUIPPED" : (isAvailable ? "AVAILABLE" : "LOCKED");
            Debug.Log($"?? Updated visual state: {currentSkill.skillName} ? {state}");
        }
    }
    
    /// <summary>
    /// Check if current skill is equipped
    /// </summary>
    private bool IsSkillEquipped()
    {
        if (skillManager == null || currentSkill == null) return false;
        
        var unlockedSlots = skillManager.GetUnlockedSlots();
        foreach (var slot in unlockedSlots)
        {
            if (slot.HasSkill() && slot.equippedSkill == currentSkill)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get current skill module
    /// </summary>
    public SkillModule GetSkill()
    {
        return currentSkill;
    }
    
    /// <summary>
    /// Check if populator is initialized
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    [ContextMenu("?? Test Population")]
    public void TestPopulation()
    {
        if (currentSkill != null && parentPanel != null)
        {
            PopulateWithSkillData(currentSkill, parentPanel);
            Debug.Log($"?? Test population complete for {currentSkill.skillName}");
        }
        else
        {
            Debug.LogWarning("?? Cannot test - missing skill or parent panel!");
        }
    }
    
    [ContextMenu("?? Test Visual Refresh")]
    public void TestVisualRefresh()
    {
        RefreshVisualState();
        Debug.Log("?? Visual state refreshed");
    }
}