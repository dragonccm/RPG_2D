using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Skill Item Prefab Handler - Handles populating prefab with skill data
/// Works with your custom prefab structure: img > SkillIconBackgr > SkillIcon, etc.
/// </summary>
public class SkillItemPrefabHandler : MonoBehaviour
{
    [Header("Prefab Components - Auto-Find")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image skillIconBackground;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI levelRequirementText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button itemButton;
    [SerializeField] private Image backgroundImage;
    
    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindComponents = true;
    [SerializeField] private bool debugMode = true;
    
    [Header("Visual Settings")]
    [SerializeField] private Color availableColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color equippedColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    
    private SkillModule currentSkill;
    private SkillPanelUI parentPanel;
    private ModularSkillManager skillManager;
    
    void Awake()
    {
        if (autoFindComponents)
        {
            AutoFindPrefabComponents();
        }
        
        // Find required managers
        skillManager = FindFirstObjectByType<ModularSkillManager>();
        
        // Setup button if found
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClicked);
        }
        else
        {
            // Add button component if missing
            itemButton = gameObject.GetComponent<Button>();
            if (itemButton == null)
            {
                itemButton = gameObject.AddComponent<Button>();
                if (debugMode) Debug.Log("Added Button component to skill item prefab");
            }
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }
    
    /// <summary>
    /// Auto-find components based on your prefab structure
    /// img > SkillIconBackgr > SkillIcon
    /// SkillDetail > SkillName, LevelReq, Description
    /// </summary>
    private void AutoFindPrefabComponents()
    {
        if (debugMode) Debug.Log($"?? Auto-finding components in prefab: {gameObject.name}");
        
        // Find skill icon: img > SkillIconBackgr > SkillIcon
        Transform imgTransform = transform.Find("img");
        if (imgTransform != null)
        {
            if (debugMode) Debug.Log("? Found 'img' transform");
            
            Transform iconBackgrTransform = imgTransform.Find("SkillIconBackgr");
            if (iconBackgrTransform != null)
            {
                if (debugMode) Debug.Log("? Found 'SkillIconBackgr' transform");
                skillIconBackground = iconBackgrTransform.GetComponent<Image>();
                
                Transform skillIconTransform = iconBackgrTransform.Find("SkillIcon");
                if (skillIconTransform != null)
                {
                    if (debugMode) Debug.Log("? Found 'SkillIcon' transform");
                    skillIcon = skillIconTransform.GetComponent<Image>();
                }
                else
                {
                    if (debugMode) Debug.LogWarning("? 'SkillIcon' not found under 'SkillIconBackgr'");
                }
            }
            else
            {
                if (debugMode) Debug.LogWarning("? 'SkillIconBackgr' not found under 'img'");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning("? 'img' transform not found");
        }
        
        // Find skill detail components: SkillDetail > SkillName, LevelReq, Description
        Transform skillDetailTransform = transform.Find("SkillDetail");
        if (skillDetailTransform != null)
        {
            if (debugMode) Debug.Log("? Found 'SkillDetail' transform");
            
            Transform skillNameTransform = skillDetailTransform.Find("SkillName");
            if (skillNameTransform != null)
            {
                if (debugMode) Debug.Log("? Found 'SkillName' transform");
                skillNameText = skillNameTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                if (debugMode) Debug.LogWarning("? 'SkillName' not found under 'SkillDetail'");
            }
            
            Transform levelReqTransform = skillDetailTransform.Find("LevelReq");
            if (levelReqTransform != null)
            {
                if (debugMode) Debug.Log("? Found 'LevelReq' transform");
                levelRequirementText = levelReqTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                if (debugMode) Debug.LogWarning("? 'LevelReq' not found under 'SkillDetail'");
            }
            
            Transform descriptionTransform = skillDetailTransform.Find("Description");
            if (descriptionTransform != null)
            {
                if (debugMode) Debug.Log("? Found 'Description' transform");
                descriptionText = descriptionTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                if (debugMode) Debug.LogWarning("? 'Description' not found under 'SkillDetail'");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning("? 'SkillDetail' transform not found");
        }
        
        // Find background image (usually on root)
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            if (debugMode) Debug.LogWarning("? Background Image not found on root GameObject");
        }
        else
        {
            if (debugMode) Debug.Log("? Found background Image component");
        }
        
        // Summary
        int foundComponents = 0;
        foundComponents += skillIcon != null ? 1 : 0;
        foundComponents += skillIconBackground != null ? 1 : 0;
        foundComponents += skillNameText != null ? 1 : 0;
        foundComponents += levelRequirementText != null ? 1 : 0;
        foundComponents += descriptionText != null ? 1 : 0;
        foundComponents += backgroundImage != null ? 1 : 0;
        
        if (debugMode) Debug.Log($"?? Auto-find complete: {foundComponents}/6 components found");
    }
    
    /// <summary>
    /// Initialize this skill item with skill data and parent panel
    /// </summary>
    public void Initialize(SkillModule skill, SkillPanelUI panel)
    {
        currentSkill = skill;
        parentPanel = panel;
        
        if (skill == null)
        {
            if (debugMode) Debug.LogError("? Cannot initialize with null skill!");
            return;
        }
        
        PopulateSkillData();
        UpdateVisualState();
        
        if (debugMode) Debug.Log($"? Initialized skill item: {skill.skillName}");
    }
    
    /// <summary>
    /// Populate all UI components with skill data
    /// </summary>
    private void PopulateSkillData()
    {
        if (currentSkill == null) return;
        
        // Set skill icon
        if (skillIcon != null)
        {
            if (currentSkill.skillIcon != null)
            {
                skillIcon.sprite = currentSkill.skillIcon;
                skillIcon.color = Color.white;
                if (debugMode) Debug.Log($"? Set skill icon for {currentSkill.skillName}");
            }
            else
            {
                skillIcon.sprite = null;
                skillIcon.color = currentSkill.skillColor;
                if (debugMode) Debug.Log($"? Set skill color for {currentSkill.skillName} (no icon)");
            }
        }
        
        // Set skill icon background
        if (skillIconBackground != null)
        {
            skillIconBackground.color = currentSkill.GetSkillTypeColor();
            if (debugMode) Debug.Log($"? Set icon background color for {currentSkill.skillName}");
        }
        
        // Set skill name
        if (skillNameText != null)
        {
            skillNameText.text = currentSkill.skillName;
            if (debugMode) Debug.Log($"? Set skill name: {currentSkill.skillName}");
        }
        
        // Set level requirement
        if (levelRequirementText != null)
        {
            levelRequirementText.text = $"Lv.{currentSkill.requiredLevel}";
            if (debugMode) Debug.Log($"? Set level requirement: Lv.{currentSkill.requiredLevel}");
        }
        
        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = currentSkill.description;
            if (debugMode) Debug.Log($"? Set description for {currentSkill.skillName}");
        }
    }
    
    /// <summary>
    /// Update visual state based on availability and equipped status
    /// </summary>
    private void UpdateVisualState()
    {
        if (currentSkill == null || skillManager == null) return;
        
        bool isAvailable = skillManager.GetPlayerLevel() >= currentSkill.requiredLevel;
        bool isEquipped = IsSkillEquipped();
        
        Color targetColor;
        if (isEquipped)
        {
            targetColor = equippedColor;
            if (debugMode) Debug.Log($"?? {currentSkill.skillName} is equipped");
        }
        else if (isAvailable)
        {
            targetColor = availableColor;
            if (debugMode) Debug.Log($"?? {currentSkill.skillName} is available");
        }
        else
        {
            targetColor = lockedColor;
            if (debugMode) Debug.Log($"?? {currentSkill.skillName} is locked");
        }
        
        // Apply color to background
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }
        
        // Update text colors based on availability
        if (levelRequirementText != null)
        {
            levelRequirementText.color = isAvailable ? Color.green : Color.red;
        }
        
        if (skillNameText != null)
        {
            skillNameText.color = isAvailable ? Color.white : Color.gray;
        }
        
        if (descriptionText != null)
        {
            descriptionText.color = isAvailable ? new Color(0.8f, 0.8f, 0.8f) : Color.gray;
        }
    }
    
    /// <summary>
    /// Check if current skill is equipped in any slot
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
    /// Handle item click
    /// </summary>
    private void OnItemClicked()
    {
        if (currentSkill == null)
        {
            if (debugMode) Debug.LogWarning("? No skill assigned to this item!");
            return;
        }
        
        if (parentPanel == null)
        {
            if (debugMode) Debug.LogWarning("? No parent panel assigned!");
            return;
        }
        
        if (debugMode) Debug.Log($"??? Clicked skill item: {currentSkill.skillName}");
        parentPanel.OnSkillItemClicked(currentSkill);
    }
    
    /// <summary>
    /// Refresh the item display (called by SkillPanelUI)
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateVisualState();
    }
    
    /// <summary>
    /// Get the skill module assigned to this item
    /// </summary>
    public SkillModule GetSkill()
    {
        return currentSkill;
    }
    
    /// <summary>
    /// Test method for debugging
    /// </summary>
    [ContextMenu("?? Test Item Click")]
    public void TestItemClick()
    {
        OnItemClicked();
    }
    
    /// <summary>
    /// Debug method to show component status
    /// </summary>
    [ContextMenu("?? Debug Component Status")]
    public void DebugComponentStatus()
    {
        Debug.Log("=== SKILL ITEM PREFAB HANDLER DEBUG ===");
        Debug.Log($"Skill: {currentSkill?.skillName ?? "NULL"}");
        Debug.Log($"Parent Panel: {parentPanel != null}");
        Debug.Log($"Skill Manager: {skillManager != null}");
        Debug.Log($"Skill Icon: {skillIcon != null}");
        Debug.Log($"Skill Icon Background: {skillIconBackground != null}");
        Debug.Log($"Skill Name Text: {skillNameText != null}");
        Debug.Log($"Level Requirement Text: {levelRequirementText != null}");
        Debug.Log($"Description Text: {descriptionText != null}");
        Debug.Log($"Background Image: {backgroundImage != null}");
        Debug.Log($"Item Button: {itemButton != null}");
        Debug.Log("=== END DEBUG ===");
    }
}