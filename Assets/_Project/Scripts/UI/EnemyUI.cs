using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyUI - Qu?n l� health bar cho enemy v?i error handling t?t h?n
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
        // Th? kh?i t?o l?i n?u ch?a th�nh c�ng
        if (!isInitialized)
        {
            InitializeEnemyUI();
        }
    }

    private void InitializeEnemyUI()
    {
        // T�m Character component trong nhi?u c�ch kh�c nhau
        enemyCharacter = GetComponentInParent<Character>();
        if (enemyCharacter == null)
            enemyCharacter = GetComponent<Character>();
        if (enemyCharacter == null)
            enemyCharacter = GetComponentInChildren<Character>();

        // N?u v?n kh�ng t�m th?y, th? t�m trong parent objects
        if (enemyCharacter == null)
        {
            Transform parent = transform.parent;
            while (parent != null && enemyCharacter == null)
            {
                enemyCharacter = parent.GetComponent<Character>();
                parent = parent.parent;
            }
        }

        // N?u v?n kh�ng c� Character, t?o m?t component Character c? b?n
        if (enemyCharacter == null)
        {
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
        // T? ??ng t�m health slider n?u ch?a c�
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
        
        // T?o fill area v� fill
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

    [ContextMenu("?? Force Initialize")]
    public void ForceInitialize()
    {
        isInitialized = false;
        InitializeEnemyUI();
    }
}
