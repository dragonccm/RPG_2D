using UnityEngine;
using System.Collections.Generic;

public class NearbyHealthDisplay : MonoBehaviour
{
    [SerializeField] private float displayRange = 10f;
    [SerializeField] private int maxDisplayCount = 5;
    [SerializeField] private bool showWorldSpaceHealthBars = true;
    [SerializeField] private string enemyHealthBarPrefabPath = "EnemyHealthBar";
    
    private List<GameObject> healthBars = new List<GameObject>();
    private Canvas uiCanvas;

    void Start()
    {
        // Tìm UICanvas, n?u không có thì tìm Canvas ??u tiên
        uiCanvas = GameObject.Find("UICanvas")?.GetComponent<Canvas>();
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
        
        if (uiCanvas == null)
        {
            Debug.LogError("Không tìm th?y Canvas nào trong scene!");
        }
    }

    void Update()
    {
        if (showWorldSpaceHealthBars && uiCanvas != null)
        {
            UpdateNearbyHealthBars();
        }
    }

    void UpdateNearbyHealthBars()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<GameObject> enemiesInRange = new List<GameObject>();

        foreach (GameObject enemy in enemies)
        {
            Character enemyChar = enemy.GetComponent<Character>();
            if (enemyChar == null || enemyChar.health.currentValue <= 0) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= displayRange)
            {
                enemiesInRange.Add(enemy);
            }
        }

        // S?p x?p theo kho?ng cách
        enemiesInRange.Sort((a, b) => Vector2.Distance(transform.position, a.transform.position)
            .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        if (enemiesInRange.Count > maxDisplayCount)
        {
            enemiesInRange = enemiesInRange.GetRange(0, maxDisplayCount);
        }

        // C?p nh?t health bars
        for (int i = 0; i < enemiesInRange.Count; i++)
        {
            GameObject enemy = enemiesInRange[i];
            if (i < healthBars.Count)
            {
                if (healthBars[i] != null)
                {
                    healthBars[i].SetActive(true);
                    UpdateHealthBarPosition(healthBars[i], enemy);
                    HealthBar healthBar = healthBars[i].GetComponent<HealthBar>();
                    if (healthBar != null && (healthBar.target == null || healthBar.target.gameObject != enemy))
                    {
                        healthBar.Initialize(enemy.GetComponent<Character>());
                        // Setup cho enemy: ch? slider, không text/mana/name
                        healthBar.SetupForEnemy();
                    }
                }
            }
            else
            {
                CreateNewHealthBar(enemy);
            }
        }

        // ?n các health bar th?a
        for (int i = enemiesInRange.Count; i < healthBars.Count; i++)
        {
            if (healthBars[i] != null)
            {
                healthBars[i].SetActive(false);
            }
        }
    }

    void CreateNewHealthBar(GameObject enemy)
    {
        GameObject healthBarPrefab = Resources.Load<GameObject>(enemyHealthBarPrefabPath);
        if (healthBarPrefab != null)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, uiCanvas.transform);
            healthBars.Add(healthBarInstance);
            
            HealthBar healthBar = healthBarInstance.GetComponent<HealthBar>();
            if (healthBar != null)
            {
                healthBar.Initialize(enemy.GetComponent<Character>());
                // Setup cho enemy: ch? slider, không text/mana/name
                healthBar.SetupForEnemy();
            }
            else
            {
                Debug.LogWarning($"HealthBar component không tìm th?y trong prefab {enemyHealthBarPrefabPath}");
            }
            
            UpdateHealthBarPosition(healthBarInstance, enemy);
        }
        else
        {
            Debug.LogError($"Không tìm th?y prefab {enemyHealthBarPrefabPath} trong Resources folder!");
        }
    }

    void UpdateHealthBarPosition(GameObject healthBar, GameObject target)
    {
        if (healthBar != null && target != null && Camera.main != null)
        {
            Vector3 worldPos = target.transform.position + Vector3.up * 1.5f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            
            // Ki?m tra xem target có trong t?m nhìn camera không
            if (screenPos.z > 0)
            {
                healthBar.transform.position = screenPos;
                healthBar.SetActive(true);
            }
            else
            {
                healthBar.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        // Cleanup t?t c? health bars
        foreach (GameObject healthBar in healthBars)
        {
            if (healthBar != null)
                Destroy(healthBar);
        }
        healthBars.Clear();
    }

    // Public methods ?? configure
    public void SetDisplayRange(float range)
    {
        displayRange = range;
    }

    public void SetMaxDisplayCount(int count)
    {
        maxDisplayCount = count;
    }

    public void SetShowWorldSpaceHealthBars(bool show)
    {
        showWorldSpaceHealthBars = show;
        
        // ?n/hi?n t?t c? health bars hi?n t?i
        foreach (GameObject healthBar in healthBars)
        {
            if (healthBar != null)
                healthBar.SetActive(show);
        }
    }

    // Debug method
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, displayRange);
    }
}