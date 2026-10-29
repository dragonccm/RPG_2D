using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// SkillPanelUI Component - Qu?n lý UI panel chính theo c?u trúc hierarchy
/// ???c thi?t k? ?? g?n vào SkillPanelUI GameObject trong Canvas
/// </summary>
public class SkillPanelUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private TextMeshProUGUI header;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private ScrollRect skillScrollView;
    [SerializeField] private Transform skillListContent;
    [SerializeField] private Button closeButton;

    [Header("Skill Detail Panel")]
    [SerializeField] private SkillDetailUI skillDetailUI;

    [Header("Prefab Settings - Auto Resources Loading")]
    [SerializeField] private GameObject skillItemPrefab; // Legacy - will be overridden by Resources loader
    [SerializeField] private bool autoCreateSkillItems = true;
    [SerializeField] private bool useResourcesLoader = true; // New option
    [SerializeField] private string resourcesPrefabName = "SkillItem"; // Prefab name in Resources

    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindComponents = true;

    private ModularSkillManager skillManager;
    private List<GameObject> skillItems = new List<GameObject>();
    private ResourcesPrefabLoader resourcesLoader; // New component

    void Awake()
    {
        if (autoFindComponents)
        {
            AutoFindUIComponents();
        }

        SetupUIElements();
    }

    void Start()
    {
        skillManager = FindFirstObjectByType<ModularSkillManager>();
        if (skillManager == null)
        {
            Debug.LogError("ModularSkillManager not found! SkillPanelUI will not work properly.");
            return;
        }

        // Initialize Resources loader if enabled
        if (useResourcesLoader)
        {
            InitializeResourcesLoader();
        }

        // Tìm SkillDetailUI n?u ch?a ???c gán
        if (skillDetailUI == null)
        {
            skillDetailUI = FindFirstObjectByType<SkillDetailUI>();
        }

        if (autoCreateSkillItems)
        {
            CreateSkillItems();
        }

        // ?n panel khi start
        gameObject.SetActive(false);
    }

    void Update()
    {
        // ?óng b?ng Escape (Tab ?ã ???c handle trong PlayerController)
        if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
        }

        // Refresh skill items periodically
        if (gameObject.activeInHierarchy && Time.time % 1f < Time.deltaTime)
        {
            RefreshSkillItems();
        }
    }

    /// <summary>
    /// T? ??ng tìm và gán các UI components theo hierarchy structure
    /// </summary>
    private void AutoFindUIComponents()
    {
        Debug.Log("Auto-finding UI components for SkillPanelUI...");

        if (header == null)
            header = transform.Find("Header")?.GetComponent<TextMeshProUGUI>();

        if (instructionText == null)
            instructionText = transform.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();

        if (skillScrollView == null)
            skillScrollView = transform.Find("SkillScrollView")?.GetComponent<ScrollRect>();

        if (skillListContent == null)
        {
            Transform scrollView = transform.Find("SkillScrollView");
            if (scrollView != null)
            {
                Transform viewport = scrollView.Find("Viewport");
                if (viewport != null)
                {
                    skillListContent = viewport.Find("Content");
                }
            }
        }

        if (closeButton == null)
            closeButton = transform.Find("CloseButton")?.GetComponent<Button>();

        // ? Setup LayoutGroup for skillListContent if missing
        SetupSkillListContentLayout();

        Debug.Log("Auto-find complete for SkillPanelUI");
    }

    /// <summary>
    /// Setup proper layout for skillListContent
    /// </summary>
    private void SetupSkillListContentLayout()
    {
        if (skillListContent == null) return;

        // Add VerticalLayoutGroup if missing
        VerticalLayoutGroup verticalLayout = skillListContent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = skillListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        // Configure VerticalLayoutGroup
        verticalLayout.spacing = 5f;
        verticalLayout.padding = new RectOffset(5, 5, 5, 5);
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;

        // Add ContentSizeFitter if missing
        ContentSizeFitter contentFitter = skillListContent.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = skillListContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        
        // Configure ContentSizeFitter
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Ensure RectTransform is properly set for Content
        RectTransform contentRect = skillListContent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
        }

        Debug.Log("? Setup LayoutGroup for skillListContent");
    }

    /// <summary>
    /// Setup các UI elements ban ??u
    /// </summary>
    private void SetupUIElements()
    {
        // Setup header text
        if (header != null)
            header.text = "SKILL MANAGEMENT";

        // Setup instruction text
        if (instructionText != null)
            instructionText.text = "Click on skills to view details and assign keys. Press Tab or X to close.";

        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    /// <summary>
    /// T?o skill items t? available skills
    /// </summary>
    private void CreateSkillItems()
    {
        if (skillManager == null || skillListContent == null) return;

        // Clear existing items
        ClearSkillItems();

        var availableSkills = skillManager.GetAvailableSkills();
        foreach (var skill in availableSkills)
        {
            CreateSkillItem(skill);
        }

        Debug.Log($"Created {skillItems.Count} skill items");
    }

    /// <summary>
    /// T?o m?t skill item
    /// </summary>
    private void CreateSkillItem(SkillModule skill)
    {
        GameObject itemObj = null;

        // Try Resources loader first (new method)
        if (useResourcesLoader && resourcesLoader != null && resourcesLoader.HasValidPrefab())
        {
            Debug.Log($"?? Creating skill item using RESOURCES LOADER for {skill.skillName}");
            
            itemObj = resourcesLoader.CreatePopulatedSkillItem(skill, skillListContent, this);
            
            if (itemObj != null)
            {
                Debug.Log($"? RESOURCES: Successfully created and populated {skill.skillName}");
            }
            else
            {
                Debug.LogWarning($"?? RESOURCES: Failed to create item for {skill.skillName} - falling back to manual prefab");
            }
        }
        
        // Fallback to manual prefab assignment (original method)
        if (itemObj == null && skillItemPrefab != null)
        {
            Debug.Log($"?? Creating skill item using MANUAL PREFAB for {skill.skillName}");
            
            itemObj = Instantiate(skillItemPrefab, skillListContent);
            
            // Get or add SkillItemPrefabHandler
            SkillItemPrefabHandler prefabHandler = itemObj.GetComponent<SkillItemPrefabHandler>();
            if (prefabHandler == null)
            {
                prefabHandler = itemObj.AddComponent<SkillItemPrefabHandler>();
                Debug.Log($"?? Added SkillItemPrefabHandler to manual prefab: {skill.skillName}");
            }
            
            // Initialize the prefab handler with skill data
            prefabHandler.Initialize(skill, this);
            
            Debug.Log($"? MANUAL: Successfully created prefab item for {skill.skillName}");
        }
        
        // Final fallback: create basic skill item (original method)
        if (itemObj == null)
        {
            Debug.Log($"?? Creating BASIC skill item for {skill.skillName} (no prefab available)");
            
            itemObj = CreateBasicSkillItem(skill);
            
            // Setup basic skill item component
            SkillItemComponent skillItem = itemObj.GetComponent<SkillItemComponent>();
            if (skillItem == null)
            {
                skillItem = itemObj.AddComponent<SkillItemComponent>();
            }
            skillItem.Initialize(skill, this);
            
            Debug.Log($"? BASIC: Successfully created basic item for {skill.skillName}");
        }

        if (itemObj != null)
        {
            skillItems.Add(itemObj);
            Debug.Log($"?? Added {skill.skillName} to skill items list (Total: {skillItems.Count})");
        }
        else
        {
            Debug.LogError($"? FAILED to create skill item for {skill.skillName}!");
        }
    }

    /// <summary>
    /// T?o basic skill item n?u không có prefab
    /// </summary>
    private GameObject CreateBasicSkillItem(SkillModule skill)
    {
        GameObject itemObj = new GameObject($"SkillItem_{skill.skillName}");
        itemObj.transform.SetParent(skillListContent, false);

        // ? FIXED: Proper RectTransform setup
        RectTransform rect = itemObj.AddComponent<RectTransform>();
        
        // Anchor for top-stretch layout (works with VerticalLayoutGroup)
        rect.anchorMin = new Vector2(0, 1);     // Top-left
        rect.anchorMax = new Vector2(1, 1);     // Top-right  
        rect.pivot = new Vector2(0.5f, 1);      // Top-center pivot
        
        // Set explicit size
        rect.sizeDelta = new Vector2(0, 80);    // Full width, 80px height
        rect.anchoredPosition = Vector2.zero;   // Reset position
        
        // ? Add LayoutElement for LayoutGroup control
        LayoutElement layoutElement = itemObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 80;
        layoutElement.preferredHeight = 80;
        layoutElement.flexibleWidth = 1;
        
        // Add background Image
        Image background = itemObj.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        // Add Button for interaction - IMPORTANT: This must be clickable
        Button button = itemObj.AddComponent<Button>();
        
        // ??M B?O button có th? click ???c
        button.interactable = true;
        button.targetGraphic = background; // Set target graphic ?? button ho?t ??ng
        
        // T?o ColorBlock cho button states
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.2f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.35f, 0.8f);
        button.colors = colors;

        // ? Add HorizontalLayoutGroup for proper content arrangement
        HorizontalLayoutGroup horizontalLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.padding = new RectOffset(10, 10, 10, 10);
        horizontalLayout.spacing = 10;
        horizontalLayout.childControlWidth = false;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandHeight = false;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;

        // Create skill icon
        CreateSkillIcon(skill, itemObj.transform);

        // Create skill name text
        CreateSkillNameText(skill, itemObj.transform);

        // Create level requirement text  
        CreateLevelRequirementText(skill, itemObj.transform);

        // Create description text
        CreateDescriptionText(skill, itemObj.transform);

        Debug.Log($"?ã Created properly sized skill item for {skill.skillName} - Size: {rect.sizeDelta}");
        return itemObj;
    }

    /// <summary>
    /// Create skill icon with proper layout
    /// </summary>
    private void CreateSkillIcon(SkillModule skill, Transform parent)
    {
        GameObject iconObj = new GameObject("SkillIcon");
        iconObj.transform.SetParent(parent, false);
        
        // Layout element for fixed size
        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.minWidth = 60;
        iconLayout.preferredWidth = 60;
        iconLayout.minHeight = 60;
        iconLayout.preferredHeight = 60;
        
        Image iconImage = iconObj.AddComponent<Image>();
        if (skill.skillIcon != null)
        {
            iconImage.sprite = skill.skillIcon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = skill.skillColor;
        }
        
        // Add outline for better visibility
        Outline outline = iconObj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(1, 1);
    }

    /// <summary>
    /// Create skill name text with proper layout
    /// </summary>
    private void CreateSkillNameText(SkillModule skill, Transform parent)
    {
        GameObject nameObj = new GameObject("SkillName");
        nameObj.transform.SetParent(parent, false);
        
        // Layout element for flexible width
        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;
        nameLayout.minHeight = 25;

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = skill.skillName;
        nameText.fontSize = 16;
        nameText.color = Color.white;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.raycastTarget = false; // Tránh ch?n click events
        nameText.overflowMode = TextOverflowModes.Ellipsis;
    }

    /// <summary>
    /// Create level requirement text with proper layout
    /// </summary>
    private void CreateLevelRequirementText(SkillModule skill, Transform parent)
    {
        GameObject levelObj = new GameObject("LevelReq");
        levelObj.transform.SetParent(parent, false);
        
        // Layout element for fixed width
        LayoutElement levelLayout = levelObj.AddComponent<LayoutElement>();
        levelLayout.minWidth = 60;
        levelLayout.preferredWidth = 60;
        levelLayout.minHeight = 25;

        TextMeshProUGUI levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.text = $"Lv.{skill.requiredLevel}";
        levelText.fontSize = 14;
        levelText.color = Color.yellow;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.raycastTarget = false; // Tránh ch?n click events
    }

    /// <summary>
    /// Create description text (optional, can be hidden in compact mode)
    /// </summary>
    private void CreateDescriptionText(SkillModule skill, Transform parent)
    {
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(parent, false);
        
        // Layout element for flexible width
        LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.flexibleWidth = 0.5f;
        descLayout.minHeight = 25;

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = skill.description;
        descText.fontSize = 12;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        descText.alignment = TextAlignmentOptions.Left;
        descText.raycastTarget = false; // Tránh ch?n click events
        descText.overflowMode = TextOverflowModes.Ellipsis;
        
        // Hide description if too long or in compact mode
        if (skill.description.Length > 50)
        {
            descObj.SetActive(false);
        }
    }

    /// <summary>
    /// Clear all skill items
    /// </summary>
    private void ClearSkillItems()
    {
        foreach (var item in skillItems)
        {
            if (item != null)
                DestroyImmediate(item);
        }
        skillItems.Clear();
    }

    /// <summary>
    /// Refresh skill items
    /// </summary>
    private void RefreshSkillItems()
    {
        foreach (var itemObj in skillItems)
        {
            if (itemObj != null)
            {
                // Try ResourcesPrefabPopulator first (new method)
                var resourcesPopulator = itemObj.GetComponent<ResourcesPrefabPopulator>();
                if (resourcesPopulator != null)
                {
                    resourcesPopulator.RefreshVisualState();
                }
                else
                {
                    // Try prefab handler (manual prefab method)
                    var prefabHandler = itemObj.GetComponent<SkillItemPrefabHandler>();
                    if (prefabHandler != null)
                    {
                        prefabHandler.RefreshDisplay();
                    }
                    else
                    {
                        // Fallback to basic skill item component
                        var skillItem = itemObj.GetComponent<SkillItemComponent>();
                        if (skillItem != null)
                            skillItem.RefreshDisplay();
                    }
                }
            }
        }
    }

    /// <summary>
    /// X? lý khi skill item ???c click
    /// </summary>
    public void OnSkillItemClicked(SkillModule skill)
    {
        Debug.Log($"?ã OnSkillItemClicked called for: {(skill != null ? skill.skillName : "NULL")}");
        
        if (skill == null)
        {
            Debug.LogWarning("Không th? show detail cho null skill");
            return;
        }

        // Ensure SkillDetailUI is found
        if (skillDetailUI == null)
        {
            skillDetailUI = FindFirstObjectByType<SkillDetailUI>();
            Debug.Log($"?ã Auto-found SkillDetailUI: {skillDetailUI != null}");
        }

        if (skillDetailUI != null)
        {
            Debug.Log($"?ang show detail cho skill: {skill.skillName}");
            skillDetailUI.ShowSkillDetail(skill);
            
            // Verify it opened
            if (skillDetailUI.IsVisible())
            {
                Debug.Log($"SkillDetailUI is now visible");
            }
            else
            {
                Debug.LogWarning("SkillDetailUI failed to become visible");
            }
        }
        else
        {
            Debug.LogError("SkillDetailUI not found! Cannot show skill detail.");
            
            // Try to find and create if needed
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Debug.Log("Attempting to create SkillDetailUI...");
                CreateEmergencySkillDetailUI(canvas.transform);
            }
        }
    }

    /// <summary>
    /// Emergency method to create SkillDetailUI if missing
    /// </summary>
    private void CreateEmergencySkillDetailUI(Transform parent)
    {
        GameObject detailObj = new GameObject("SkillDetailUI");
        detailObj.transform.SetParent(parent, false);
        
        RectTransform rect = detailObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.6f, 0.1f);
        rect.anchorMax = new Vector2(0.95f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image background = detailObj.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        
        skillDetailUI = detailObj.AddComponent<SkillDetailUI>();
        
        Debug.Log("Created emergency SkillDetailUI");
    }

    /// <summary>
    /// M? panel
    /// </summary>
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        if (autoCreateSkillItems)
        {
            CreateSkillItems();
        }
        Debug.Log("SkillPanelUI opened");
    }

    /// <summary>
    /// ?óng panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        
        // ?óng detail panel n?u ?ang m?
        if (skillDetailUI != null && skillDetailUI.IsVisible())
        {
            skillDetailUI.ClosePanel();
        }

        Debug.Log("SkillPanelUI closed");
    }

    /// <summary>
    /// Toggle panel
    /// </summary>
    public void TogglePanel()
    {
        if (gameObject.activeInHierarchy)
            ClosePanel();
        else
            OpenPanel();
    }

    // Public getters
    public bool IsVisible() => gameObject.activeInHierarchy;
    public ModularSkillManager GetSkillManager() => skillManager;

    /// <summary>
    /// Context menu ?? test component
    /// </summary>
    [ContextMenu("Test Component")]
    public void TestComponent()
    {
        Debug.Log("=== TESTING SKILLPANELUI COMPONENT ===");
        Debug.Log($"Header: {(header != null ? "Có" : "Không")}");
        Debug.Log($"InstructionText: {(instructionText != null ? "Có" : "Không")}");
        Debug.Log($"SkillScrollView: {(skillScrollView != null ? "Có" : "Không")}");
        Debug.Log($"SkillListContent: {(skillListContent != null ? "Có" : "Không")}");
        Debug.Log($"CloseButton: {(closeButton != null ? "Có" : "Không")}");
        Debug.Log($"SkillDetailUI: {(skillDetailUI != null ? "Có" : "Không")}");
        Debug.Log($"SkillManager: {(skillManager != null ? "Có" : "Không")}");
        Debug.Log($"Skill Items Count: {skillItems.Count}");
        Debug.Log("=== TEST COMPLETE ===");
    }

    [ContextMenu("Refresh Auto-Find")]
    public void RefreshAutoFind()
    {
        AutoFindUIComponents();
        Debug.Log("Auto-find refreshed for SkillPanelUI");
    }

    [ContextMenu("Recreate Skill Items")]
    public void RecreateSkillItems()
    {
        CreateSkillItems();
        Debug.Log("Skill items recreated");
    }

    [ContextMenu("?ã Test Prefab Population")]
    public void TestPrefabPopulation()
    {
        Debug.Log("=== TESTING PREFAB POPULATION ===");
        
        if (skillItemPrefab == null)
        {
            Debug.LogWarning("?ã No skill item prefab assigned!");
            return;
        }
        
        if (skillManager == null)
        {
            Debug.LogWarning("?ã No ModularSkillManager found!");
            return;
        }
        
        var availableSkills = skillManager.GetAvailableSkills();
        Debug.Log($"?ã Available skills: {availableSkills.Count}");
        
        if (availableSkills.Count > 0)
        {
            var testSkill = availableSkills[0];
            Debug.Log($"?ã Testing with skill: {testSkill.skillName}");
            
            // Create test item
            GameObject testItem = Instantiate(skillItemPrefab, skillListContent);
            
            // Add handler
            SkillItemPrefabHandler handler = testItem.GetComponent<SkillItemPrefabHandler>();
            if (handler == null)
            {
                handler = testItem.AddComponent<SkillItemPrefabHandler>();
            }
            
            // Initialize
            handler.Initialize(testSkill, this);
            
            Debug.Log("?ã Test item created and populated!");
            
            // Clean up after 5 seconds
            Destroy(testItem, 5f);
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }

    /// <summary>
    /// Initialize Resources loader system
    /// </summary>
    private void InitializeResourcesLoader()
    {
        Debug.Log("?? Initializing Resources Prefab Loader...");
        
        // Get or create ResourcesPrefabLoader component
        resourcesLoader = GetComponent<ResourcesPrefabLoader>();
        if (resourcesLoader == null)
        {
            resourcesLoader = gameObject.AddComponent<ResourcesPrefabLoader>();
            Debug.Log("? Added ResourcesPrefabLoader component");
        }
        
        // Set prefab name if different from default
        if (!string.IsNullOrEmpty(resourcesPrefabName) && resourcesPrefabName != "SkillItem")
        {
            var loaderType = typeof(ResourcesPrefabLoader);
            var prefabNameField = loaderType.GetField("prefabName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabNameField != null)
            {
                prefabNameField.SetValue(resourcesLoader, resourcesPrefabName);
                Debug.Log($"?? Set prefab name to: {resourcesPrefabName}");
            }
        }
        
        // Attempt to load prefab
        GameObject loadedPrefab = resourcesLoader.LoadSkillItemPrefab();
        if (loadedPrefab != null)
        {
            Debug.Log($"? Successfully loaded prefab from Resources: {loadedPrefab.name}");
            
            // Override manual prefab assignment
            skillItemPrefab = loadedPrefab;
        }
        else
        {
            Debug.LogWarning("?? Failed to load prefab from Resources - will use manual assignment or fallback");
            useResourcesLoader = false; // Disable if loading failed
        }
    }

    [ContextMenu("?? Test Resources Loader")]
    public void TestResourcesLoader()
    {
        Debug.Log("=== TESTING RESOURCES LOADER ===");
        
        if (!useResourcesLoader)
        {
            Debug.LogWarning("?? Resources loader is disabled!");
            Debug.LogWarning("   ? Enable 'useResourcesLoader' in SkillPanelUI");
            return;
        }
        
        InitializeResourcesLoader();
        
        if (resourcesLoader != null)
        {
            resourcesLoader.TestLoadPrefab();
            
            if (resourcesLoader.HasValidPrefab())
            {
                Debug.Log("? Resources loader test: SUCCESS");
                
                // Test creating a skill item if skills available
                if (skillManager != null)
                {
                    var skills = skillManager.GetAvailableSkills();
                    if (skills.Count > 0)
                    {
                        var testSkill = skills[0];
                        Debug.Log($"?? Testing skill item creation with: {testSkill.skillName}");
                        
                        GameObject testItem = resourcesLoader.CreatePopulatedSkillItem(testSkill, skillListContent, this);
                        if (testItem != null)
                        {
                            Debug.Log("? Test skill item created successfully!");
                            
                            // Clean up after 5 seconds
                            Destroy(testItem, 5f);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("? Resources loader test: FAILED");
            }
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    [ContextMenu("?? Force Resources Reload")]
    public void ForceResourcesReload()
    {
        if (resourcesLoader != null)
        {
            resourcesLoader.ForceReloadPrefab();
            Debug.Log("?? Forced resources reload");
        }
        else
        {
            Debug.LogWarning("?? No resources loader available");
        }
    }
    
    [ContextMenu("?? Debug Resources System")]
    public void DebugResourcesSystem()
    {
        Debug.Log("=== RESOURCES SYSTEM DEBUG ===");
        Debug.Log($"Use Resources Loader: {useResourcesLoader}");
        Debug.Log($"Resources Prefab Name: {resourcesPrefabName}");
        Debug.Log($"Resources Loader Component: {(resourcesLoader != null ? "?" : "?")}");
        Debug.Log($"Manual Prefab Assignment: {(skillItemPrefab != null ? "?" : "?")}");
        
        if (resourcesLoader != null)
        {
            Debug.Log($"Has Valid Prefab: {resourcesLoader.HasValidPrefab()}");
            var analysis = resourcesLoader.GetStructureAnalysis();
            Debug.Log($"Structure Analysis Valid: {analysis.hasValidStructure}");
        }
        
        Debug.Log($"Total Skill Items: {skillItems.Count}");
        Debug.Log("=== END DEBUG ===");
    }
}

/// <summary>
/// Component for individual skill items in the skill list
/// </summary>
public class SkillItemComponent : MonoBehaviour
{
    private SkillModule skill;
    private SkillPanelUI parentPanel;
    private Button button;
    private Image background;

    public void Initialize(SkillModule skillModule, SkillPanelUI panel)
    {
        skill = skillModule;
        parentPanel = panel;

        // Get or add button component
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            Debug.LogWarning($"Added missing Button component to {gameObject.name}");
        }

        // Get background image
        background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            Debug.LogWarning($"Added missing Image component to {gameObject.name}");
        }

        // Setup button properties
        button.interactable = true;
        button.targetGraphic = background;

        // Setup button click event v?i debug
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            Debug.Log($"Skill item clicked: {skill.skillName}");
            if (parentPanel != null && skill != null)
            {
                parentPanel.OnSkillItemClicked(skill);
            }
            else
            {
                Debug.LogError($"Missing references - Parent: {parentPanel != null}, Skill: {skill != null}");
            }
        });

        RefreshDisplay();
        
        Debug.Log($"SkillItemComponent initialized for {skill.skillName} - Button clickable: {button.interactable}");
    }

    public void RefreshDisplay()
    {
        if (skill == null || parentPanel == null) return;

        var skillManager = parentPanel.GetSkillManager();
        if (skillManager == null) return;

        // Update background color based on availability and equipped status
        if (background != null)
        {
            bool isAvailable = skillManager.GetPlayerLevel() >= skill.requiredLevel;
            bool isEquipped = IsSkillEquipped(skillManager);

            if (isEquipped)
                background.color = new Color(0.2f, 0.8f, 0.2f, 0.8f); // Green - equipped
            else if (isAvailable)
                background.color = new Color(0.2f, 0.2f, 0.3f, 0.8f); // Default - available
            else
                background.color = new Color(0.5f, 0.2f, 0.2f, 0.8f); // Red - locked
        }
    }

    private bool IsSkillEquipped(ModularSkillManager skillManager)
    {
        var slots = skillManager.GetUnlockedSlots();
        foreach (var slot in slots)
        {
            if (slot.HasSkill() && slot.equippedSkill == skill)
                return true;
        }
        return false;
    }

    public SkillModule GetSkill() => skill;
    
    // Test method
    [ContextMenu("Test Click")]
    public void TestClick()
    {
        Debug.Log($"Testing click for {skill?.skillName}");
        if (parentPanel != null && skill != null)
        {
            parentPanel.OnSkillItemClicked(skill);
        }
    }
}