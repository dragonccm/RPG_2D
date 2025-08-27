using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enhanced Player Damage Indicator System
/// Provides the best possible visual feedback for player attacks
/// Inspired by AAA games like DMC, Bayonetta, Hades, etc.
/// </summary>
public class PlayerDamageIndicatorSystem : MonoBehaviour
{
    [Header("🎯 Core Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas indicatorCanvas;
    [SerializeField] private bool enableSystem = true;
    
    [Header("⚔️ Attack Preview")]
    [Tooltip("Show damage area before attack lands")]
    public bool showAttackPreview = true;
    [Tooltip("Preview duration before attack hits")]
    public float previewDuration = 0.3f;
    [Tooltip("Preview fade in duration")]
    public float previewFadeIn = 0.1f;
    
    [Header("💥 Impact Feedback")]
    [Tooltip("Show hit confirmation when damage lands")]
    public bool showImpactFeedback = true;
    [Tooltip("Impact effect duration")]
    public float impactDuration = 0.8f;
    [Tooltip("Hit stop duration for impactful hits")]
    public float hitStopDuration = 0.1f;
    
    [Header("🔗 Combo System")]
    [Tooltip("Show combo count and multipliers")]
    public bool showComboIndicators = true;
    [Tooltip("Time window for combo counting")]
    public float comboWindow = 2f;
    [Tooltip("Combo multiplier per hit")]
    public float comboMultiplier = 0.1f;
    
    [Header("🎨 Visual Customization")]
    public Color previewColor = new Color(1f, 1f, 0f, 0.4f);
    public Color impactColor = new Color(1f, 0.3f, 0.1f, 0.8f);
    public Color criticalColor = new Color(1f, 0.1f, 0.8f, 0.9f);
    public Color comboColor = new Color(0.2f, 1f, 0.3f, 0.8f);
    
    [Header("🚀 Performance")]
    [Tooltip("Maximum simultaneous indicators")]
    public int maxIndicators = 20;
    [Tooltip("Indicator pool size")]
    public int poolSize = 30;
    
    // System state
    private static PlayerDamageIndicatorSystem instance;
    public static PlayerDamageIndicatorSystem Instance => instance;
    
    // Object pools for performance
    private Queue<GameObject> indicatorPool;
    private List<ActiveIndicator> activeIndicators;
    
    // Combo tracking
    private int currentCombo = 0;
    private float lastHitTime = 0f;
    private Coroutine comboResetCoroutine;
    
    // Performance tracking
    private int indicatorsThisFrame = 0;
    private float lastFrameTime = 0f;
    
    #region Initialization
    
    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSystem()
    {
        // Setup camera reference
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        // Setup canvas
        if (indicatorCanvas == null)
            CreateIndicatorCanvas();
        
        // Initialize pools
        indicatorPool = new Queue<GameObject>(poolSize);
        activeIndicators = new List<ActiveIndicator>(maxIndicators);
        
        // Pre-populate object pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject indicator = CreateIndicatorObject();
            indicator.SetActive(false);
            indicatorPool.Enqueue(indicator);
        }
        
        Debug.Log($"✨ PlayerDamageIndicatorSystem initialized with {poolSize} pooled objects");
    }
    
    private void CreateIndicatorCanvas()
    {
        GameObject canvasObj = new GameObject("PlayerDamageIndicatorCanvas");
        canvasObj.transform.SetParent(transform);
        
        indicatorCanvas = canvasObj.AddComponent<Canvas>();
        indicatorCanvas.renderMode = RenderMode.WorldSpace;
        indicatorCanvas.sortingOrder = 100; // Above most UI elements
        
        // Add CanvasScaler for proper scaling
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }
    
    private GameObject CreateIndicatorObject()
    {
        GameObject indicator = new GameObject("DamageIndicator");
        indicator.transform.SetParent(indicatorCanvas.transform);
        
        // Add SpriteRenderer for 2D effects
        var spriteRenderer = indicator.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 101;
        
        // Add animator for effects
        var animator = indicator.AddComponent<DamageIndicatorAnimator>();
        
        return indicator;
    }
    
    #endregion
    
    #region Public API - Main Methods
    
    /// <summary>
    /// Show attack preview before damage lands
    /// </summary>
    public void ShowAttackPreview(SkillModule skill, Vector2 center, Vector2 direction, DamageAreaShape shape)
    {
        if (!enableSystem || !showAttackPreview) return;
        
        StartCoroutine(ShowAttackPreviewCoroutine(skill, center, direction, shape));
    }
    
    /// <summary>
    /// Show impact feedback when damage is dealt
    /// </summary>
    public void ShowImpactFeedback(Vector2 position, float damage, bool isCritical, int enemiesHit)
    {
        if (!enableSystem || !showImpactFeedback) return;
        
        // Update combo
        UpdateCombo(enemiesHit);
        
        // Create impact indicator
        var indicator = GetPooledIndicator();
        if (indicator != null)
        {
            SetupImpactIndicator(indicator, position, damage, isCritical);
        }
        
        // Hit stop for impactful hits
        if (isCritical || enemiesHit > 2)
        {
            StartCoroutine(HitStopEffect());
        }
    }
    
    /// <summary>
    /// Show damage numbers floating up from impact
    /// </summary>
    public void ShowDamageNumbers(Vector2 position, float damage, bool isCritical, Color textColor)
    {
        if (!enableSystem) return;
        
        var indicator = GetPooledIndicator();
        if (indicator != null)
        {
            SetupDamageNumberIndicator(indicator, position, damage, isCritical, textColor);
        }
    }
    
    /// <summary>
    /// Show combo indicator
    /// </summary>
    public void ShowComboIndicator(Vector2 position, int combo, float multiplier)
    {
        if (!enableSystem || !showComboIndicators || combo < 2) return;
        
        var indicator = GetPooledIndicator();
        if (indicator != null)
        {
            SetupComboIndicator(indicator, position, combo, multiplier);
        }
    }
    
    #endregion
    
    #region Preview System
    
    private IEnumerator ShowAttackPreviewCoroutine(SkillModule skill, Vector2 center, Vector2 direction, DamageAreaShape shape)
    {
        var indicator = GetPooledIndicator();
        if (indicator == null) yield break;
        
        // Setup preview indicator
        SetupPreviewIndicator(indicator, skill, center, direction, shape);
        
        // Fade in
        yield return StartCoroutine(FadeIndicator(indicator, 0f, previewColor.a, previewFadeIn));
        
        // Hold for preview duration
        yield return new WaitForSeconds(previewDuration - previewFadeIn);
        
        // Fade out quickly
        yield return StartCoroutine(FadeIndicator(indicator, previewColor.a, 0f, 0.1f));
        
        // Return to pool
        ReturnToPool(indicator);
    }
    
    private void SetupPreviewIndicator(GameObject indicator, SkillModule skill, Vector2 center, Vector2 direction, DamageAreaShape shape)
    {
        var spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        var animator = indicator.GetComponent<DamageIndicatorAnimator>();
        
        indicator.transform.position = new Vector3(center.x, center.y, -1f);
        
        // Create appropriate shape sprite
        spriteRenderer.sprite = CreateShapeSprite(shape, skill);
        spriteRenderer.color = previewColor;
        
        // Setup rotation for directional attacks
        if (shape != DamageAreaShape.Circle)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            indicator.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // Scale based on skill size
        Vector3 scale = GetShapeScale(shape, skill);
        indicator.transform.localScale = scale;
        
        // Start pulsing animation
        animator.StartPulseAnimation(0.2f, 1.2f, previewDuration);
        
        indicator.SetActive(true);
    }
    
    #endregion
    
    #region Impact Feedback
    
    private void SetupImpactIndicator(GameObject indicator, Vector2 position, float damage, bool isCritical)
    {
        var spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        var animator = indicator.GetComponent<DamageIndicatorAnimator>();
        
        indicator.transform.position = new Vector3(position.x, position.y, -1f);
        
        // Choose color and sprite based on critical hit
        Color color = isCritical ? criticalColor : impactColor;
        spriteRenderer.sprite = CreateImpactSprite(isCritical);
        spriteRenderer.color = color;
        
        // Scale based on damage
        float scale = Mathf.Clamp(1f + damage * 0.01f, 0.8f, 2f);
        indicator.transform.localScale = Vector3.one * scale;
        
        // Start impact animation
        animator.StartImpactAnimation(color, impactDuration, isCritical);
        
        indicator.SetActive(true);
        
        // Schedule return to pool
        StartCoroutine(ReturnToPoolAfterDelay(indicator, impactDuration));
    }
    
    private void SetupDamageNumberIndicator(GameObject indicator, Vector2 position, float damage, bool isCritical, Color textColor)
    {
        // Create floating damage number
        var textComponent = indicator.GetComponent<UnityEngine.UI.Text>();
        if (textComponent == null)
        {
            textComponent = indicator.AddComponent<UnityEngine.UI.Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        indicator.transform.position = new Vector3(position.x, position.y, -1f);
        
        // Setup text
        textComponent.text = damage.ToString("F0");
        textComponent.color = textColor;
        textComponent.fontSize = isCritical ? 24 : 18;
        textComponent.fontStyle = isCritical ? FontStyle.Bold : FontStyle.Normal;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        // Start floating animation
        var animator = indicator.GetComponent<DamageIndicatorAnimator>();
        animator.StartFloatingTextAnimation(2f, 1.5f);
        
        indicator.SetActive(true);
        
        StartCoroutine(ReturnToPoolAfterDelay(indicator, 1.5f));
    }
    
    #endregion
    
    #region Combo System
    
    private void UpdateCombo(int enemiesHit)
    {
        float currentTime = Time.time;
        
        if (currentTime - lastHitTime > comboWindow)
        {
            currentCombo = 0;
        }
        
        currentCombo += enemiesHit;
        lastHitTime = currentTime;
        
        // Reset combo after timeout
        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);
        comboResetCoroutine = StartCoroutine(ResetComboAfterDelay());
        
        // Show combo indicator if applicable
        if (currentCombo >= 2)
        {
            float multiplier = 1f + (currentCombo - 1) * comboMultiplier;
            ShowComboIndicator(Vector2.zero, currentCombo, multiplier); // Position will be updated
        }
    }
    
    private void SetupComboIndicator(GameObject indicator, Vector2 position, int combo, float multiplier)
    {
        var textComponent = indicator.GetComponent<UnityEngine.UI.Text>();
        if (textComponent == null)
        {
            textComponent = indicator.AddComponent<UnityEngine.UI.Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        // Position near player
        if (playerCamera != null)
        {
            Vector3 screenPos = playerCamera.WorldToScreenPoint(position);
            screenPos.y += 100f; // Offset upwards
            position = playerCamera.ScreenToWorldPoint(screenPos);
        }
        
        indicator.transform.position = new Vector3(position.x, position.y, -1f);
        
        // Setup combo text
        textComponent.text = $"{combo}x COMBO!\n{multiplier:F1}x";
        textComponent.color = comboColor;
        textComponent.fontSize = 20;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        // Animate combo
        var animator = indicator.GetComponent<DamageIndicatorAnimator>();
        animator.StartComboAnimation(1.5f);
        
        indicator.SetActive(true);
        
        StartCoroutine(ReturnToPoolAfterDelay(indicator, 1.5f));
    }
    
    private IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(comboWindow);
        currentCombo = 0;
    }
    
    #endregion
    
    #region Utility Methods
    
    private GameObject GetPooledIndicator()
    {
        // Performance check
        if (Time.time != lastFrameTime)
        {
            indicatorsThisFrame = 0;
            lastFrameTime = Time.time;
        }
        
        if (indicatorsThisFrame >= 5) // Max 5 indicators per frame
            return null;
        
        if (activeIndicators.Count >= maxIndicators)
            return null;
        
        GameObject indicator;
        if (indicatorPool.Count > 0)
        {
            indicator = indicatorPool.Dequeue();
        }
        else
        {
            indicator = CreateIndicatorObject();
        }
        
        indicatorsThisFrame++;
        return indicator;
    }
    
    private void ReturnToPool(GameObject indicator)
    {
        if (indicator != null)
        {
            indicator.SetActive(false);
            indicatorPool.Enqueue(indicator);
        }
    }
    
    private IEnumerator ReturnToPoolAfterDelay(GameObject indicator, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(indicator);
    }
    
    private IEnumerator FadeIndicator(GameObject indicator, float fromAlpha, float toAlpha, float duration)
    {
        var spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        float elapsed = 0f;
        Color color = spriteRenderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            color.a = alpha;
            spriteRenderer.color = color;
            yield return null;
        }
        
        color.a = toAlpha;
        spriteRenderer.color = color;
    }
    
    private IEnumerator HitStopEffect()
    {
        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }
    
    #endregion
    
    #region Shape Creation
    
    private Sprite CreateShapeSprite(DamageAreaShape shape, SkillModule skill)
    {
        switch (shape)
        {
            case DamageAreaShape.Circle:
                return CreateCircleSprite(64);
            case DamageAreaShape.Semicircle:
                return CreateSemicircleSprite(64);
            case DamageAreaShape.Rectangle:
                return CreateRectangleSprite(64, 32);
            case DamageAreaShape.Cone:
                return CreateConeSprite(64);
            case DamageAreaShape.Line:
                return CreateLineSprite(128, 16);
            default:
                return CreateCircleSprite(64);
        }
    }
    
    private Vector3 GetShapeScale(DamageAreaShape shape, SkillModule skill)
    {
        switch (shape)
        {
            case DamageAreaShape.Circle:
                return Vector3.one * skill.range * 2f;
            case DamageAreaShape.Rectangle:
                return new Vector3(skill.range * 2f, skill.attackWidth * 2f, 1f);
            case DamageAreaShape.Line:
                return new Vector3(skill.range * 2f, skill.attackWidth, 1f);
            default:
                return Vector3.one * skill.range * 2f;
        }
    }
    
    private Sprite CreateCircleSprite(int resolution)
    {
        return CreateSimpleSprite(resolution, resolution, (x, y, center, size) =>
        {
            float distance = Vector2.Distance(new Vector2(x, y), center);
            return distance <= size * 0.4f;
        });
    }
    
    private Sprite CreateSemicircleSprite(int resolution)
    {
        return CreateSimpleSprite(resolution, resolution, (x, y, center, size) =>
        {
            Vector2 pos = new Vector2(x, y);
            float distance = Vector2.Distance(pos, center);
            return distance <= size * 0.4f && pos.y >= center.y;
        });
    }
    
    private Sprite CreateRectangleSprite(int width, int height)
    {
        return CreateSimpleSprite(width, height, (x, y, center, size) => true);
    }
    
    private Sprite CreateConeSprite(int resolution)
    {
        return CreateSimpleSprite(resolution, resolution, (x, y, center, size) =>
        {
            Vector2 pos = new Vector2(x, y);
            Vector2 direction = (pos - center).normalized;
            float angle = Vector2.Angle(Vector2.right, direction);
            float distance = Vector2.Distance(pos, center);
            return distance <= size * 0.4f && angle <= 45f;
        });
    }
    
    private Sprite CreateLineSprite(int length, int width)
    {
        return CreateSimpleSprite(length, width, (x, y, center, size) => true);
    }
    
    private Sprite CreateSimpleSprite(int width, int height, System.Func<int, int, Vector2, float, bool> isInside)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        float size = Mathf.Min(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (isInside(x, y, center, size))
                {
                    colors[y * width + x] = Color.white;
                }
                else
                {
                    colors[y * width + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f);
    }
    
    private Sprite CreateImpactSprite(bool isCritical)
    {
        int size = isCritical ? 64 : 48;
        return CreateCircleSprite(size);
    }
    
    #endregion
}

/// <summary>
/// Helper class to track active indicators
/// </summary>
[System.Serializable]
public class ActiveIndicator
{
    public GameObject gameObject;
    public float timeCreated;
    public IndicatorType type;
}

/// <summary>
/// Types of damage indicators
/// </summary>
public enum IndicatorType
{
    Preview,
    Impact,
    DamageNumber,
    Combo
}



