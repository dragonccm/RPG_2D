using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyUI - Qu?n lý health bar cho enemy v?i error handling t?t h?n
/// </summary>
public class EnemyUI : MonoBehaviour
{
    [Header("Enemy Health UI - Slider Only")]
    [SerializeField] private Slider healthSlider;
    
    private Character enemyCharacter;
    private bool isInitialized = false;

    void Start()
    {
        InitializeEnemyUI();
    }

    void Update()
    {
        // Th? kh?i t?o l?i n?u ch?a thành công
        if (!isInitialized)
        {
            InitializeEnemyUI();
        }
    }

    private void InitializeEnemyUI()
    {
        // Tìm Character component trong nhi?u cách khác nhau
        enemyCharacter = GetComponentInParent<Character>();
        if (enemyCharacter == null)
            enemyCharacter = GetComponent<Character>();
        if (enemyCharacter == null)
            enemyCharacter = GetComponentInChildren<Character>();

        // N?u v?n không tìm th?y, th? tìm trong parent objects
        if (enemyCharacter == null)
        {
            Transform parent = transform.parent;
            while (parent != null && enemyCharacter == null)
            {
                enemyCharacter = parent.GetComponent<Character>();
                parent = parent.parent;
            }
        }

        // N?u v?n không có Character, t?o m?t component Character c? b?n
        if (enemyCharacter == null)
        {
            Debug.LogWarning($"Enemy {gameObject.name} không có Character component! T?o component m?i...");
            enemyCharacter = gameObject.AddComponent<Character>();
            
            // Setup basic health cho enemy
            if (enemyCharacter.health == null)
            {
                GameObject healthObj = new GameObject("Health");
                healthObj.transform.SetParent(enemyCharacter.transform);
                enemyCharacter.health = healthObj.AddComponent<Resource>();
                enemyCharacter.health.Initialize(100f, 0f); // 100 HP, no regen
            }
        }

        if (enemyCharacter != null)
        {
            SetupUI();
            isInitialized = true;
        }
    }

    private void SetupUI()
    {
        // T? ??ng tìm health slider n?u ch?a có
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
            if (healthSlider == null)
            {
                // T?o basic health slider
                CreateBasicHealthSlider();
            }
        }

        if (enemyCharacter != null && healthSlider != null)
        {
            // Subscribe to health changes
            enemyCharacter.health.OnValueChanged += UpdateHealthUI;

            // Initial update
            UpdateHealthUI(enemyCharacter.health.currentValue, enemyCharacter.health.maxValue);
        }
    }

    private void CreateBasicHealthSlider()
    {
        // T?o m?t GameObject ch?a health slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(transform, false);
        
        // Add RectTransform cho UI
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(80, 10);
        
        // Add Slider component
        healthSlider = sliderObj.AddComponent<Slider>();
        healthSlider.minValue = 0;
        healthSlider.maxValue = 100;
        healthSlider.value = 100;
        
        // T?o background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        background.AddComponent<RectTransform>();
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // T?o fill area và fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        fillArea.AddComponent<RectTransform>();
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fill.AddComponent<RectTransform>();
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.red;
        
        // Setup slider references
        healthSlider.fillRect = fill.GetComponent<RectTransform>();
    }

    private void UpdateHealthUI(float currentValue, float maxValue)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxValue;
            healthSlider.value = currentValue;
        }

        // ?n UI khi enemy ch?t
        if (currentValue <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Cleanup subscriptions
        if (enemyCharacter != null)
        {
            enemyCharacter.health.OnValueChanged -= UpdateHealthUI;
        }
    }

    // Method ?? set health slider reference n?u c?n
    public void SetHealthSlider(Slider slider)
    {
        healthSlider = slider;
    }

    // Method ?? force refresh
    public void RefreshUI()
    {
        if (enemyCharacter != null)
        {
            UpdateHealthUI(enemyCharacter.health.currentValue, enemyCharacter.health.maxValue);
        }
    }

    // Public getters
    public Character GetCharacter() => enemyCharacter;
    public bool IsInitialized() => isInitialized;

    /// <summary>
    /// Context menu ?? test và debug
    /// </summary>
    [ContextMenu("?? Test EnemyUI")]
    public void TestEnemyUI()
    {
        Debug.Log("=== TESTING ENEMYUI ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Character: {(enemyCharacter != null ? "?" : "?")}");
        Debug.Log($"HealthSlider: {(healthSlider != null ? "?" : "?")}");
        Debug.Log($"Initialized: {(isInitialized ? "?" : "?")}");
        
        if (enemyCharacter != null)
        {
            Debug.Log($"Health: {enemyCharacter.health.currentValue}/{enemyCharacter.health.maxValue}");
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }

    [ContextMenu("?? Force Initialize")]
    public void ForceInitialize()
    {
        isInitialized = false;
        InitializeEnemyUI();
    }

    [ContextMenu("?? Test Damage")]
    public void TestDamage()
    {
        if (enemyCharacter != null)
        {
            enemyCharacter.TakeDamage(10f);
            Debug.Log($"Applied 10 damage to {gameObject.name}");
        }
    }
}
