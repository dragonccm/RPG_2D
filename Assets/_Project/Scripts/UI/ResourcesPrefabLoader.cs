using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Resources Prefab Loader - Automatically loads SkillItem prefab from Resources
/// Analyzes prefab structure and populates with skill data
/// </summary>
public class ResourcesPrefabLoader : MonoBehaviour
{
    [Header("Resources Settings")]
    [SerializeField] private string prefabName = "SkillItem";
    [SerializeField] private string resourcesPath = ""; // Empty = root Resources folder
    [SerializeField] private bool debugMode = true;
    
    [Header("Auto-Population Settings")]
    [SerializeField] private bool autoAnalyzeStructure = true;
    [SerializeField] private bool showStructureInConsole = true;
    
    private GameObject loadedPrefab;
    private PrefabStructureAnalysis structureAnalysis;
    
    public struct PrefabStructureAnalysis
    {
        public bool hasValidStructure;
        public Transform imgTransform;
        public Transform skillIconBackgrTransform;
        public Transform skillIconTransform;
        public Transform skillDetailTransform;
        public Transform skillNameTransform;
        public Transform levelReqTransform;
        public Transform descriptionTransform;
        public Image skillIcon;
        public Image skillIconBackground;
        public TextMeshProUGUI skillNameText;
        public TextMeshProUGUI levelRequirementText;
        public TextMeshProUGUI descriptionText;
        public Button itemButton;
        public Image backgroundImage;
    }
    
    /// <summary>
    /// Load SkillItem prefab from Resources folder
    /// </summary>
    public GameObject LoadSkillItemPrefab()
    {
        if (loadedPrefab != null)
        {
            if (debugMode) Debug.Log($"? Using cached prefab: {loadedPrefab.name}");
            return loadedPrefab;
        }
        
        string fullPath = string.IsNullOrEmpty(resourcesPath) ? prefabName : $"{resourcesPath}/{prefabName}";
        
        if (debugMode) Debug.Log($"?? Loading prefab from Resources: {fullPath}");
        
        loadedPrefab = Resources.Load<GameObject>(fullPath);
        
        if (loadedPrefab == null)
        {
            Debug.LogError($"? Failed to load prefab '{fullPath}' from Resources!");
            Debug.LogError($"   ? Ensure prefab is in Resources/{resourcesPath} folder");
            Debug.LogError($"   ? Check prefab name spelling: '{prefabName}'");
            return null;
        }
        
        if (debugMode) Debug.Log($"? Successfully loaded prefab: {loadedPrefab.name}");
        
        if (autoAnalyzeStructure)
        {
            AnalyzePrefabStructure(loadedPrefab);
        }
        
        return loadedPrefab;
    }
    
    /// <summary>
    /// Analyze prefab structure to understand its hierarchy
    /// </summary>
    public PrefabStructureAnalysis AnalyzePrefabStructure(GameObject prefab)
    {
        if (debugMode) Debug.Log($"?? ANALYZING PREFAB STRUCTURE: {prefab.name}");
        Debug.Log("=".PadRight(50, '='));
        
        var analysis = new PrefabStructureAnalysis();
        
        // Find img hierarchy: img > SkillIconBackgr > SkillIcon
        analysis.imgTransform = prefab.transform.Find("img");
        if (analysis.imgTransform != null)
        {
            if (debugMode) Debug.Log("? Found: img");
            
            analysis.skillIconBackgrTransform = analysis.imgTransform.Find("SkillIconBackgr");
            if (analysis.skillIconBackgrTransform != null)
            {
                if (debugMode) Debug.Log("? Found: img/SkillIconBackgr");
                analysis.skillIconBackground = analysis.skillIconBackgrTransform.GetComponent<Image>();
                
                analysis.skillIconTransform = analysis.skillIconBackgrTransform.Find("SkillIcon");
                if (analysis.skillIconTransform != null)
                {
                    if (debugMode) Debug.Log("? Found: img/SkillIconBackgr/SkillIcon");
                    analysis.skillIcon = analysis.skillIconTransform.GetComponent<Image>();
                }
                else
                {
                    if (debugMode) Debug.LogWarning("? Missing: img/SkillIconBackgr/SkillIcon");
                }
            }
            else
            {
                if (debugMode) Debug.LogWarning("? Missing: img/SkillIconBackgr");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning("? Missing: img");
        }
        
        // Find skill detail hierarchy: SkillDetail > SkillName, LevelReq, Description
        analysis.skillDetailTransform = prefab.transform.Find("SkillDetail");
        if (analysis.skillDetailTransform != null)
        {
            if (debugMode) Debug.Log("? Found: SkillDetail");
            
            analysis.skillNameTransform = analysis.skillDetailTransform.Find("SkillName");
            if (analysis.skillNameTransform != null)
            {
                if (debugMode) Debug.Log("? Found: SkillDetail/SkillName");
                analysis.skillNameText = analysis.skillNameTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                if (debugMode) Debug.LogWarning("? Missing: SkillDetail/SkillName");
            }
            
            analysis.levelReqTransform = analysis.skillDetailTransform.Find("LevelReq");
            if (analysis.levelReqTransform != null)
            {
                if (debugMode) Debug.Log("? Found: SkillDetail/LevelReq");
                analysis.levelRequirementText = analysis.levelReqTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                if (debugMode) Debug.LogWarning("? Missing: SkillDetail/LevelReq");
            }
            
            analysis.descriptionTransform = analysis.skillDetailTransform.Find("Description");
            if (analysis.descriptionTransform != null)
            {
                if (debugMode) Debug.Log("? Found: SkillDetail/Description");
                analysis.descriptionText = analysis.descriptionTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                if (debugMode) Debug.LogWarning("? Missing: SkillDetail/Description");
            }
        }
        else
        {
            if (debugMode) Debug.LogWarning("? Missing: SkillDetail");
        }
        
        // Find root components
        analysis.itemButton = prefab.GetComponent<Button>();
        analysis.backgroundImage = prefab.GetComponent<Image>();
        
        if (debugMode)
        {
            Debug.Log($"? Root Button: {(analysis.itemButton != null ? "Found" : "Missing")}");
            Debug.Log($"? Root Image: {(analysis.backgroundImage != null ? "Found" : "Missing")}");
        }
        
        // Determine if structure is valid
        analysis.hasValidStructure = 
            analysis.skillIcon != null &&
            analysis.skillNameText != null &&
            analysis.levelRequirementText != null;
        
        if (debugMode)
        {
            Debug.Log("=".PadRight(50, '='));
            Debug.Log($"?? STRUCTURE ANALYSIS: {(analysis.hasValidStructure ? "VALID" : "INVALID")}");
            Debug.Log($"   Essential Components Found: {GetFoundComponentsCount(analysis)}/6");
        }
        
        if (showStructureInConsole)
        {
            ShowStructureTree(prefab.transform, 0);
        }
        
        structureAnalysis = analysis;
        return analysis;
    }
    
    /// <summary>
    /// Show complete prefab structure tree in console
    /// </summary>
    private void ShowStructureTree(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        string componentInfo = "";
        
        // Check for important components
        if (parent.GetComponent<Image>() != null) componentInfo += " [Image]";
        if (parent.GetComponent<TextMeshProUGUI>() != null) componentInfo += " [TextMeshProUGUI]";
        if (parent.GetComponent<Button>() != null) componentInfo += " [Button]";
        
        Debug.Log($"?? {indent}{parent.name}{componentInfo}");
        
        for (int i = 0; i < parent.childCount; i++)
        {
            ShowStructureTree(parent.GetChild(i), depth + 1);
        }
    }
    
    /// <summary>
    /// Count how many essential components were found
    /// </summary>
    private int GetFoundComponentsCount(PrefabStructureAnalysis analysis)
    {
        int count = 0;
        if (analysis.skillIcon != null) count++;
        if (analysis.skillIconBackground != null) count++;
        if (analysis.skillNameText != null) count++;
        if (analysis.levelRequirementText != null) count++;
        if (analysis.descriptionText != null) count++;
        if (analysis.itemButton != null) count++;
        return count;
    }
    
    /// <summary>
    /// Create and populate skill item from loaded prefab
    /// </summary>
    public GameObject CreatePopulatedSkillItem(SkillModule skill, Transform parent, SkillPanelUI parentPanel)
    {
        GameObject prefab = LoadSkillItemPrefab();
        if (prefab == null)
        {
            Debug.LogError("? Cannot create skill item - no prefab loaded!");
            return null;
        }
        
        // Instantiate prefab
        GameObject itemInstance = Instantiate(prefab, parent);
        itemInstance.name = $"SkillItem_{skill.skillName}";
        
        if (debugMode) Debug.Log($"?? Created skill item instance: {itemInstance.name}");
        
        // Add ResourcesPrefabPopulator component to handle data population
        ResourcesPrefabPopulator populator = itemInstance.GetComponent<ResourcesPrefabPopulator>();
        if (populator == null)
        {
            populator = itemInstance.AddComponent<ResourcesPrefabPopulator>();
        }
        
        // Use cached structure analysis
        populator.SetStructureAnalysis(structureAnalysis);
        populator.PopulateWithSkillData(skill, parentPanel);
        
        return itemInstance;
    }
    
    /// <summary>
    /// Get structure analysis (cached)
    /// </summary>
    public PrefabStructureAnalysis GetStructureAnalysis()
    {
        return structureAnalysis;
    }
    
    /// <summary>
    /// Check if prefab is loaded and valid
    /// </summary>
    public bool HasValidPrefab()
    {
        return loadedPrefab != null && structureAnalysis.hasValidStructure;
    }
    
    [ContextMenu("?? Test Load Prefab")]
    public void TestLoadPrefab()
    {
        GameObject prefab = LoadSkillItemPrefab();
        if (prefab != null)
        {
            Debug.Log($"? Test successful: Loaded {prefab.name}");
        }
        else
        {
            Debug.LogError("? Test failed: Could not load prefab");
        }
    }
    
    [ContextMenu("?? Force Reload Prefab")]
    public void ForceReloadPrefab()
    {
        loadedPrefab = null;
        structureAnalysis = new PrefabStructureAnalysis();
        LoadSkillItemPrefab();
    }
}