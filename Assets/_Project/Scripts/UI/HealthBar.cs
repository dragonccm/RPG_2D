using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Settings")]
    [SerializeField] private bool showMana = true;
    [SerializeField] private bool showText = false; // M?c ??nh false cho enemy
    [SerializeField] private bool showName = false; // M?c ??nh false cho enemy

    public Character target { get; private set; }

    private void Awake()
    {
        // T? ??ng tìm components n?u ch?a ???c assign
        if (healthSlider == null)
            healthSlider = transform.Find("HealthSlider")?.GetComponent<Slider>();
        
        if (manaSlider == null && showMana)
            manaSlider = transform.Find("ManaSlider")?.GetComponent<Slider>();
        
        if (healthText == null && showText)
            healthText = transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
        
        if (manaText == null && showText && showMana)
            manaText = transform.Find("ManaText")?.GetComponent<TextMeshProUGUI>();
        
        if (nameText == null && showName)
            nameText = transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
    }

    public void Initialize(Character character)
    {
        if (character == null)
        {
            Debug.LogError("Character is null in HealthBar Initialize");
            return;
        }

        // Unsubscribe from previous target n?u có
        if (target != null)
        {
            target.health.OnValueChanged -= UpdateHealthDisplay;
            if (showMana && target.mana != null)
                target.mana.OnValueChanged -= UpdateManaDisplay;
        }

        target = character;

        // Subscribe to new target
        target.health.OnValueChanged += UpdateHealthDisplay;
        if (showMana && target.mana != null)
            target.mana.OnValueChanged += UpdateManaDisplay;

        // Initial update
        UpdateHealthDisplay(target.health.currentValue, target.health.maxValue);
        if (showMana && target.mana != null)
            UpdateManaDisplay(target.mana.currentValue, target.mana.maxValue);

        // Set name - ch? n?u showName = true
        if (showName && nameText != null)
            nameText.text = target.name;

        // ?n/hi?n các components theo settings
        SetupUIVisibility();
    }

    private void SetupUIVisibility()
    {
        // ?n mana slider n?u không c?n hi?n th?
        if (manaSlider != null)
            manaSlider.gameObject.SetActive(showMana);
        
        // ?n text components n?u không c?n hi?n th?
        if (healthText != null)
            healthText.gameObject.SetActive(showText);
        
        if (manaText != null)
            manaText.gameObject.SetActive(showMana && showText);
        
        if (nameText != null)
            nameText.gameObject.SetActive(showName);
    }

    private void UpdateHealthDisplay(float currentValue, float maxValue)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxValue;
            healthSlider.value = currentValue;
        }

        // Update text ch? n?u showText = true
        if (showText && healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentValue)}/{Mathf.Ceil(maxValue)}";
        }

        // Destroy health bar n?u target ch?t
        if (currentValue <= 0)
        {
            StartCoroutine(DestroyAfterDelay(1f));
        }
    }

    private void UpdateManaDisplay(float currentValue, float maxValue)
    {
        if (!showMana) return;

        if (manaSlider != null)
        {
            manaSlider.maxValue = maxValue;
            manaSlider.value = currentValue;
        }

        if (showText && manaText != null)
        {
            manaText.text = $"{Mathf.Ceil(currentValue)}/{Mathf.Ceil(maxValue)}";
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Cleanup subscriptions
        if (target != null)
        {
            target.health.OnValueChanged -= UpdateHealthDisplay;
            if (showMana && target.mana != null)
                target.mana.OnValueChanged -= UpdateManaDisplay;
        }
    }

    // Public methods ?? configure health bar
    public void SetShowMana(bool show)
    {
        showMana = show;
        if (manaSlider != null)
            manaSlider.gameObject.SetActive(show);
        if (manaText != null)
            manaText.gameObject.SetActive(show && showText);
    }

    public void SetShowText(bool show)
    {
        showText = show;
        if (healthText != null)
            healthText.gameObject.SetActive(show);
        if (manaText != null)
            manaText.gameObject.SetActive(show && showMana);
    }

    public void SetShowName(bool show)
    {
        showName = show;
        if (nameText != null)
            nameText.gameObject.SetActive(show);
    }

    // Method ?? setup cho enemy (ch? slider)
    public void SetupForEnemy()
    {
        SetShowMana(false);
        SetShowText(false);
        SetShowName(false);
    }

    // Method ?? setup cho player (full UI)
    public void SetupForPlayer()
    {
        SetShowMana(true);
        SetShowText(true);
        SetShowName(false);
    }

    // Method ?? setup cho target (slider + name)
    public void SetupForTarget()
    {
        SetShowMana(false);
        SetShowText(true);
        SetShowName(true);
    }
}