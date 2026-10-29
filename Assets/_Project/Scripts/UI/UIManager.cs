using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Manager - Qu?n lý toàn b? h? th?ng UI v?i Legacy Skill UI
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Health UI References")]
    [SerializeField] private Transform healthBG;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider manaBar;
    [SerializeField] private TextMeshProUGUI head;

    [Header("?? Skill UI System")]
    [SerializeField] private SkillDetailUI skillDetailUI;
    [SerializeField] private SkillPanelUI skillPanelUI;

    [Header("Other UI Systems")]
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private NearbyHealthDisplay nearbyHealthDisplay;
    [SerializeField] private TargetingSystem targetingSystem;

    [Header("UI Settings")]
    [SerializeField] private bool showPlayerUI = true;
    [SerializeField] private bool showEnemyHealthBars = true;
    [SerializeField] private bool showTargetHealthBar = true;
    [SerializeField] private bool autoFindComponents = true;

    private static UIManager instance;
    
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<UIManager>();
            return instance;
        }
    }

    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (autoFindComponents)
        {
            AutoFindUIComponents();
        }

        InitializeUI();
    }

    void Start()
    {
        ValidateUIComponents();
        ApplySettings();
    }

    void Update()
    {
        // Tab toggle ?ã ???c x? lý trong PlayerController
    }

    /// <summary>
    /// T? ??ng tìm các UI components theo hierarchy structure
    /// </summary>
    private void AutoFindUIComponents()
    {
        Debug.Log("?? Auto-finding UI components for UIManager...");

        // Tìm Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("? No Canvas found in scene!");
            return;
        }

        Transform canvasTransform = canvas.transform;

        // Health UI
        if (healthBG == null)
            healthBG = canvasTransform.Find("healthBG");

        if (healthBar == null)
            healthBar = canvasTransform.Find("healthBG/healthBar")?.GetComponent<Slider>();

        if (manaBar == null)
            manaBar = canvasTransform.Find("healthBG/manaBar")?.GetComponent<Slider>();

        if (head == null)
            head = canvasTransform.Find("healthBG/head")?.GetComponent<TextMeshProUGUI>();

        // Skill UI components
        if (skillDetailUI == null)
            skillDetailUI = canvasTransform.Find("SkillDetailUI")?.GetComponent<SkillDetailUI>();

        if (skillPanelUI == null)
            skillPanelUI = canvasTransform.Find("SkillPanelUI")?.GetComponent<SkillPanelUI>();

        // Other UI systems
        if (playerUI == null)
            playerUI = FindFirstObjectByType<PlayerUI>();

        if (nearbyHealthDisplay == null)
            nearbyHealthDisplay = FindFirstObjectByType<NearbyHealthDisplay>();

        if (targetingSystem == null)
            targetingSystem = FindFirstObjectByType<TargetingSystem>();

        Debug.Log("? Auto-find complete for UIManager");
    }

    /// <summary>
    /// Initialize UI systems
    /// </summary>
    private void InitializeUI()
    {
        // T? ??ng tìm các component khác n?u ch?a ???c assign
        if (playerUI == null)
            playerUI = FindFirstObjectByType<PlayerUI>();

        if (nearbyHealthDisplay == null)
            nearbyHealthDisplay = FindFirstObjectByType<NearbyHealthDisplay>();

        if (targetingSystem == null)
            targetingSystem = FindFirstObjectByType<TargetingSystem>();

        // Skill UI components
        if (skillDetailUI == null)
            skillDetailUI = FindFirstObjectByType<SkillDetailUI>();

        if (skillPanelUI == null)
            skillPanelUI = FindFirstObjectByType<SkillPanelUI>();
    }

    /// <summary>
    /// Validate all UI components
    /// </summary>
    private void ValidateUIComponents()
    {
        Debug.Log("?? === VALIDATING UI COMPONENTS ===");

        // Health UI validation
        bool healthUIValid = healthBG != null && healthBar != null && manaBar != null;
        Debug.Log($"Health UI: {(healthUIValid ? "?" : "?")}");

        // Skill UI validation
        bool skillUIValid = skillDetailUI != null && skillPanelUI != null;
        Debug.Log($"?? Skill UI: {(skillUIValid ? "?" : "?")}");

        if (skillUIValid)
        {
            Debug.Log("? Using SkillPanelUI + SkillDetailUI system");
        }
        else
        {
            Debug.LogError("? Skill UI system incomplete! Need both SkillPanelUI and SkillDetailUI.");
        }

        // Other systems
        Debug.Log($"PlayerUI: {(playerUI != null ? "?" : "?")}");
        Debug.Log($"NearbyHealthDisplay: {(nearbyHealthDisplay != null ? "?" : "?")}");
        Debug.Log($"TargetingSystem: {(targetingSystem != null ? "?" : "?")}");

        // Warnings for missing components
        if (!healthUIValid)
            Debug.LogWarning("?? Health UI components missing! Player health/mana may not display correctly.");

        if (!skillUIValid)
            Debug.LogWarning("?? Skill UI system incomplete! Skill management will not work.");
    }

    /// <summary>
    /// Apply UI settings
    /// </summary>
    private void ApplySettings()
    {
        // Health UI
        if (healthBG != null)
            healthBG.gameObject.SetActive(showPlayerUI);

        // Enemy health bars
        if (nearbyHealthDisplay != null)
            nearbyHealthDisplay.SetShowWorldSpaceHealthBars(showEnemyHealthBars);

        // Targeting system
        if (targetingSystem != null)
            targetingSystem.enabled = showTargetHealthBar;

        // Skill panels should start hidden
        if (skillPanelUI != null && skillPanelUI.IsVisible())
            skillPanelUI.ClosePanel();

        if (skillDetailUI != null && skillDetailUI.IsVisible())
            skillDetailUI.ClosePanel();
    }

    // Public UI control methods

    /// <summary>
    /// ?? Toggle skill panel v?i Tab key
    /// </summary>
    public void ToggleSkillPanel()
    {
        if (skillPanelUI != null)
        {
            skillPanelUI.TogglePanel();
            Debug.Log($"?? SkillPanelUI toggled - Now: {(skillPanelUI.IsVisible() ? "OPEN" : "CLOSED")}");
        }
        else
        {
            Debug.LogWarning("? SkillPanelUI not found! Cannot toggle skill panel.");
        }
    }

    /// <summary>
    /// Show/hide player UI
    /// </summary>
    public void SetShowPlayerUI(bool show)
    {
        showPlayerUI = show;
        
        if (healthBG != null)
            healthBG.gameObject.SetActive(show);

        if (playerUI != null)
            playerUI.gameObject.SetActive(show);
    }

    /// <summary>
    /// Show/hide enemy health bars
    /// </summary>
    public void SetShowEnemyHealthBars(bool show)
    {
        showEnemyHealthBars = show;
        if (nearbyHealthDisplay != null)
            nearbyHealthDisplay.SetShowWorldSpaceHealthBars(show);
    }

    /// <summary>
    /// Show/hide target health bar
    /// </summary>
    public void SetShowTargetHealthBar(bool show)
    {
        showTargetHealthBar = show;
        if (targetingSystem != null)
        {
            targetingSystem.enabled = show;
            if (!show)
                targetingSystem.ClearTarget();
        }
    }

    /// <summary>
    /// Update health display
    /// </summary>
    public void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// Update mana display
    /// </summary>
    public void UpdateManaDisplay(float currentMana, float maxMana)
    {
        if (manaBar != null)
        {
            manaBar.value = currentMana / maxMana;
        }
    }

    /// <summary>
    /// Update head text (player name/level)
    /// </summary>
    public void UpdateHeadText(string text)
    {
        if (head != null)
        {
            head.text = text;
        }
    }

    // Getters for UI components
    public PlayerUI GetPlayerUI() => playerUI;
    public NearbyHealthDisplay GetNearbyHealthDisplay() => nearbyHealthDisplay;
    public TargetingSystem GetTargetingSystem() => targetingSystem;
    public SkillDetailUI GetSkillDetailUI() => skillDetailUI;
    public SkillPanelUI GetSkillPanelUI() => skillPanelUI;

    // Settings getters
    public bool IsPlayerUIEnabled() => showPlayerUI;
    public bool IsEnemyHealthBarsEnabled() => showEnemyHealthBars;
    public bool IsTargetHealthBarEnabled() => showTargetHealthBar;
    
    public bool IsSkillPanelVisible()
    {
        return skillPanelUI != null && skillPanelUI.IsVisible();
    }

    /// <summary>
    /// Context menu ?? test component
    /// </summary>
    [ContextMenu("?? Test All UI Components")]
    public void TestAllUIComponents()
    {
        Debug.Log("?? === TESTING ALL UI COMPONENTS ===");
        ValidateUIComponents();
        
        if (skillPanelUI != null)
            skillPanelUI.TestComponent();
            
        if (skillDetailUI != null)
            skillDetailUI.TestComponent();
            
        Debug.Log("?? === UI TEST COMPLETE ===");
    }

    [ContextMenu("?? Refresh Auto-Find")]
    public void RefreshAutoFind()
    {
        AutoFindUIComponents();
        Debug.Log("?? Auto-find refreshed for UIManager");
    }

    [ContextMenu("?? Test Skill Panel Toggle")]
    public void TestSkillPanelToggle()
    {
        ToggleSkillPanel();
        string status = IsSkillPanelVisible() ? "VISIBLE" : "HIDDEN";
        Debug.Log($"?? SkillPanelUI is now: {status}");
    }
}