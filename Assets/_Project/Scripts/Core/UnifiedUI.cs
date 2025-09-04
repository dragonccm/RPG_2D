using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Unified UI system to replace multiple UI controllers
/// Consolidates PlayerUIController, EnemyUIController, and various UI managers
/// </summary>
public class UnifiedUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Health Display")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthBarFill;

    [Header("Mana Display")]
    [SerializeField] private Slider manaBar;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Experience Display")]
    [SerializeField] private Slider expBar;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Damage Numbers")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Transform damageNumberParent;

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogging = false;

    private Dictionary<GameObject, Coroutine> activePanels = new Dictionary<GameObject, Coroutine>();
    private ObjectPool damageNumberPool;

    private void Awake()
    {
        InitializeUI();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        // Hide all panels initially
        if (skillPanel != null) skillPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Initialize damage number pool
        if (damageNumberPrefab != null)
        {
            GameObject poolObject = new GameObject("DamageNumberPool");
            poolObject.transform.parent = transform;
            damageNumberPool = poolObject.AddComponent<ObjectPool>();
            damageNumberPool.Initialize(damageNumberPrefab, 5);
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to unified game events
        GameEvents.OnHealthChanged += UpdateHealthDisplay;
        GameEvents.OnManaChanged += UpdateManaDisplay;
        GameEvents.OnExperienceChanged += UpdateExperienceDisplay;
        GameEvents.OnLevelUp += ShowLevelUpPanel;
        GameEvents.OnPlayerDeath += ShowGameOverPanel;
        GameEvents.OnDamageDealt += OnDamageDealtHandler;
        GameEvents.OnHealingReceived += OnHealingReceivedHandler;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnHealthChanged -= UpdateHealthDisplay;
        GameEvents.OnManaChanged -= UpdateManaDisplay;
        GameEvents.OnExperienceChanged -= UpdateExperienceDisplay;
        GameEvents.OnLevelUp -= ShowLevelUpPanel;
        GameEvents.OnPlayerDeath -= ShowGameOverPanel;
        GameEvents.OnDamageDealt -= OnDamageDealtHandler;
        GameEvents.OnHealingReceived -= OnHealingReceivedHandler;
    }

    /// <summary>
    /// Handler for damage dealt events
    /// </summary>
    private void OnDamageDealtHandler(IDamageable target, float damage, IDamageDealer dealer)
    {
        if (target != null)
        {
            ShowDamageNumber(damage, target.Transform.position);
        }
    }

    /// <summary>
    /// Handler for healing received events
    /// </summary>
    private void OnHealingReceivedHandler(float healing)
    {
        // Use player position for healing numbers
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ShowHealingNumber(healing, player.transform.position);
        }
    }

    /// <summary>
    /// Update health bar display
    /// </summary>
    public void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = PerformanceUtils.FormatString("{0}/{1}", Mathf.RoundToInt(currentHealth), Mathf.RoundToInt(maxHealth));
        }

        // Change color based on health percentage
        if (healthBarFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            if (healthPercent > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("❤️ Health updated: {0}/{1}", currentHealth, maxHealth));
        }
    }

    /// <summary>
    /// Update mana bar display
    /// </summary>
    public void UpdateManaDisplay(float currentMana, float maxMana)
    {
        if (manaBar != null)
        {
            manaBar.value = currentMana / maxMana;
        }

        if (manaText != null)
        {
            manaText.text = PerformanceUtils.FormatString("{0}/{1}", Mathf.RoundToInt(currentMana), Mathf.RoundToInt(maxMana));
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔵 Mana updated: {0}/{1}", currentMana, maxMana));
        }
    }

    /// <summary>
    /// Update experience bar display
    /// </summary>
    public void UpdateExperienceDisplay(float currentExp, float maxExp, int level)
    {
        if (expBar != null)
        {
            expBar.value = currentExp / maxExp;
        }

        if (expText != null)
        {
            expText.text = PerformanceUtils.FormatString("{0}/{1}", Mathf.RoundToInt(currentExp), Mathf.RoundToInt(maxExp));
        }

        if (levelText != null)
        {
            levelText.text = PerformanceUtils.FormatString("Lv. {0}", level);
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("⭐ EXP updated: {0}/{1} (Level {2})", currentExp, maxExp, level));
        }
    }

    /// <summary>
    /// Show damage number at position
    /// </summary>
    public void ShowDamageNumber(float damage, Vector2 position)
    {
        if (damageNumberPrefab == null) return;

        GameObject damageObj = damageNumberPool?.Spawn(damageNumberPrefab, position, Quaternion.identity);
        if (damageObj == null) return;

        var textComponent = damageObj.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = Mathf.RoundToInt(damage).ToString();
            textComponent.color = Color.red;
        }

        // Auto-return to pool after animation
        StartCoroutine(ReturnDamageNumberToPool(damageObj, 1f));
    }

    /// <summary>
    /// Show healing number at position
    /// </summary>
    public void ShowHealingNumber(float healing, Vector2 position)
    {
        if (damageNumberPrefab == null) return;

        GameObject healingObj = damageNumberPool?.Spawn(damageNumberPrefab, position, Quaternion.identity);
        if (healingObj == null) return;

        var textComponent = healingObj.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = "+" + Mathf.RoundToInt(healing).ToString();
            textComponent.color = Color.green;
        }

        StartCoroutine(ReturnDamageNumberToPool(healingObj, 1f));
    }

    private System.Collections.IEnumerator ReturnDamageNumberToPool(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        damageNumberPool?.Return(obj);
    }

    /// <summary>
    /// Toggle skill panel visibility
    /// </summary>
    public void ToggleSkillPanel()
    {
        if (skillPanel != null)
        {
            TogglePanel(skillPanel);
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🎯 Skill panel toggled");
        }
    }

    /// <summary>
    /// Toggle pause panel visibility
    /// </summary>
    public void TogglePausePanel()
    {
        if (pausePanel != null)
        {
            TogglePanel(pausePanel);
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("⏸️ Pause panel toggled");
        }
    }

    /// <summary>
    /// Show level up panel
    /// </summary>
    public void ShowLevelUpPanel()
    {
        if (levelUpPanel != null)
        {
            ShowPanel(levelUpPanel, 3f); // Auto-hide after 3 seconds
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("⬆️ Level up panel shown");
        }
    }

    /// <summary>
    /// Show game over panel
    /// </summary>
    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            ShowPanel(gameOverPanel); // Don't auto-hide
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("💀 Game over panel shown");
        }
    }

    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    private void TogglePanel(GameObject panel)
    {
        if (panel.activeSelf)
        {
            HidePanel(panel);
        }
        else
        {
            ShowPanel(panel);
        }
    }

    /// <summary>
    /// Show panel (with optional auto-hide)
    /// </summary>
    private void ShowPanel(GameObject panel, float autoHideDelay = 0f)
    {
        panel.SetActive(true);

        if (autoHideDelay > 0f)
        {
            if (activePanels.ContainsKey(panel))
            {
                StopCoroutine(activePanels[panel]);
            }

            activePanels[panel] = StartCoroutine(HidePanelAfterDelay(panel, autoHideDelay));
        }
    }

    /// <summary>
    /// Hide panel
    /// </summary>
    private void HidePanel(GameObject panel)
    {
        panel.SetActive(false);

        if (activePanels.ContainsKey(panel))
        {
            StopCoroutine(activePanels[panel]);
            activePanels.Remove(panel);
        }
    }

    private System.Collections.IEnumerator HidePanelAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        HidePanel(panel);
    }

    /// <summary>
    /// Update UI element text
    /// </summary>
    public void UpdateText(TextMeshProUGUI textComponent, string format, params object[] args)
    {
        if (textComponent != null)
        {
            textComponent.text = PerformanceUtils.FormatString(format, args);
        }
    }

    /// <summary>
    /// Update UI slider value
    /// </summary>
    public void UpdateSlider(Slider slider, float value, float maxValue)
    {
        if (slider != null)
        {
            slider.value = value / maxValue;
        }
    }

    /// <summary>
    /// Flash UI element
    /// </summary>
    public void FlashElement(Image element, Color flashColor, float duration)
    {
        if (element == null) return;

        StartCoroutine(FlashElementCoroutine(element, flashColor, duration));
    }

    private System.Collections.IEnumerator FlashElementCoroutine(Image element, Color flashColor, float duration)
    {
        Color originalColor = element.color;
        element.color = flashColor;

        yield return new WaitForSeconds(duration);

        element.color = originalColor;
    }
}
