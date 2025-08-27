using UnityEngine;

/// <summary>
/// Manager to initialize and configure PlayerDamageIndicatorSystem
/// Add this to a GameObject in your scene to enable enhanced damage indicators
/// </summary>
public class PlayerDamageIndicatorManager : MonoBehaviour
{
    [Header("🎯 System Configuration")]
    [Tooltip("Enable the enhanced damage indicator system")]
    public bool enableSystem = false; // Disabled by default for cleaner gameplay
    
    [Header("⚔️ Preview Settings")]
    [Tooltip("Show attack preview before damage lands")]
    public bool showAttackPreview = true;
    [Tooltip("Duration of attack preview")]
    [Range(0.1f, 1f)]
    public float previewDuration = 0.3f;
    
    [Header("💥 Impact Settings")]
    [Tooltip("Show impact feedback when damage is dealt")]
    public bool showImpactFeedback = true;
    [Tooltip("Duration of impact effects")]
    [Range(0.2f, 2f)]
    public float impactDuration = 0.8f;
    [Tooltip("Hit stop duration for critical hits")]
    [Range(0f, 0.3f)]
    public float hitStopDuration = 0.1f;
    
    [Header("🔗 Combo Settings")]
    [Tooltip("Show combo indicators")]
    public bool showComboIndicators = true;
    [Tooltip("Time window for combo counting")]
    [Range(1f, 5f)]
    public float comboWindow = 2f;
    
    [Header("🎨 Visual Customization")]
    public Color previewColor = new Color(1f, 1f, 0f, 0.4f);
    public Color impactColor = new Color(1f, 0.3f, 0.1f, 0.8f);
    public Color criticalColor = new Color(1f, 0.1f, 0.8f, 0.9f);
    public Color comboColor = new Color(0.2f, 1f, 0.3f, 0.8f);
    
    [Header("🚀 Performance")]
    [Tooltip("Maximum simultaneous damage indicators")]
    [Range(5, 50)]
    public int maxIndicators = 20;
    [Tooltip("Object pool size for indicators")]
    [Range(10, 100)]
    public int poolSize = 30;
    
    private PlayerDamageIndicatorSystem indicatorSystem;
    
    private void Awake()
    {
        InitializeIndicatorSystem();
    }
    
    private void Start()
    {
        ConfigureIndicatorSystem();
    }
    
    /// <summary>
    /// Initialize the damage indicator system
    /// </summary>
    private void InitializeIndicatorSystem()
    {
        // Check if system already exists
        if (PlayerDamageIndicatorSystem.Instance != null)
        {
            indicatorSystem = PlayerDamageIndicatorSystem.Instance;
            Debug.Log("✅ PlayerDamageIndicatorSystem already exists - using existing instance");
            return;
        }
        
        // Create system GameObject
        GameObject systemObj = new GameObject("PlayerDamageIndicatorSystem");
        systemObj.transform.SetParent(transform);
        
        // Add the system component
        indicatorSystem = systemObj.AddComponent<PlayerDamageIndicatorSystem>();
        
        Debug.Log("✨ PlayerDamageIndicatorSystem initialized successfully!");
    }
    
    /// <summary>
    /// Configure the indicator system with current settings
    /// </summary>
    private void ConfigureIndicatorSystem()
    {
        if (indicatorSystem == null) return;
        
        // Use reflection to set private fields if needed, or make them public
        // For now, the system will use its default settings
        // Future improvement: add public configuration methods to the system
        
        Debug.Log($"🎯 Damage Indicator System configured:" +
                 $"\n- Preview: {showAttackPreview}" +
                 $"\n- Impact: {showImpactFeedback}" +
                 $"\n- Combo: {showComboIndicators}" +
                 $"\n- Max Indicators: {maxIndicators}");
    }
    
    /// <summary>
    /// Enable or disable the entire system
    /// </summary>
    public void SetSystemEnabled(bool enabled)
    {
        enableSystem = enabled;
        if (indicatorSystem != null)
        {
            indicatorSystem.gameObject.SetActive(enabled);
        }
        
        Debug.Log($"🎯 PlayerDamageIndicatorSystem {(enabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Test the system with a sample attack
    private void OnValidate()
    {
        // Clamp values to reasonable ranges
        previewDuration = Mathf.Clamp(previewDuration, 0.1f, 1f);
        impactDuration = Mathf.Clamp(impactDuration, 0.2f, 2f);
        hitStopDuration = Mathf.Clamp(hitStopDuration, 0f, 0.3f);
        comboWindow = Mathf.Clamp(comboWindow, 1f, 5f);
        maxIndicators = Mathf.Clamp(maxIndicators, 5, 50);
        poolSize = Mathf.Clamp(poolSize, 10, 100);
        
        // Ensure pool size is larger than max indicators
        if (poolSize < maxIndicators)
            poolSize = maxIndicators + 10;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw test area
        Gizmos.color = previewColor;
        Gizmos.DrawWireSphere(transform.position, 2f);
        
        Gizmos.color = impactColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}

