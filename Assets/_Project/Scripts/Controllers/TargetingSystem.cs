using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [Header("Targeting Settings")]
    public GameObject currentTarget;
    [SerializeField] private float targetingRange = 10f;
    //[SerializeField] private string targetHealthBarPrefabPath = "HealthBarText";
    
    private GameObject healthBarInstance;
    private Canvas uiCanvas;

    void Start()
    {
        // Tìm UICanvas, nếu không có thì tìm Canvas đầu tiên
        uiCanvas = GameObject.Find("UICanvas")?.GetComponent<Canvas>();
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
        
        if (uiCanvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas nào trong scene!");
        }
    }

    void Update()
    {
        if (currentTarget == null)
        {
            AutoSelectTarget();
        }
        else
        {
            // Kiểm tra target còn sống và trong tầm
            Character targetCharacter = currentTarget.GetComponent<Character>();
            if (targetCharacter == null || targetCharacter.health.currentValue <= 0)
            {
                DestroyHealthBar();
                currentTarget = null;
                return;
            }

            //UpdateHealthBarPosition();
            
            // Kiểm tra khoảng cách
            if (Vector2.Distance(transform.position, currentTarget.transform.position) > targetingRange)
            {
                DestroyHealthBar();
                currentTarget = null;
            }
        }
    }

    void AutoSelectTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            Character enemyChar = enemy.GetComponent<Character>();
            if (enemyChar == null || enemyChar.health.currentValue <= 0) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance && distance <= targetingRange)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        
        //if (closestEnemy != currentTarget)
        //{
        //    currentTarget = closestEnemy;
        //    if (currentTarget != null)
        //    {
        //        CreateHealthBar();
        //    }
        //}
    }

    //void CreateHealthBar()
    //{
    //    if (healthBarInstance == null && uiCanvas != null && currentTarget != null)
    //    {
    //        GameObject healthBarPrefab = Resources.Load<GameObject>(targetHealthBarPrefabPath);
    //        if (healthBarPrefab != null)
    //        {
    //            healthBarInstance = Instantiate(healthBarPrefab, uiCanvas.transform);
    //            HealthBar healthBar = healthBarInstance.GetComponent<HealthBar>();
    //            if (healthBar != null)
    //            {
    //                Character targetCharacter = currentTarget.GetComponent<Character>();
    //                healthBar.Initialize(targetCharacter);
                    
    //                // Configure cho target health bar
    //                healthBar.SetShowMana(false); // Target thường không hiển thị mana
    //                healthBar.SetShowName(true);  // Hiển thị tên target
    //                healthBar.SetShowText(true);  // Hiển thị số liệu
    //            }
    //            else
    //            {
    //                Debug.LogWarning($"HealthBar component không tìm thấy trong prefab {targetHealthBarPrefabPath}");
    //            }
    //        }
    //        else
    //        {
    //            Debug.LogError($"Không tìm thấy prefab {targetHealthBarPrefabPath} trong Resources folder!");
    //        }
    //    }
    //}

    //void UpdateHealthBarPosition()
    //{
    //    if (healthBarInstance != null && currentTarget != null && Camera.main != null)
    //    {
    //        Vector3 targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.transform.position + Vector3.up * 2f);
            
    //        // Kiểm tra xem target có trong tầm nhìn camera không
    //        if (targetScreenPos.z > 0)
    //        {
    //            healthBarInstance.transform.position = targetScreenPos;
    //            healthBarInstance.SetActive(true);
    //        }
    //        else
    //        {
    //            healthBarInstance.SetActive(false);
    //        }
    //    }
    //}

    void DestroyHealthBar()
    {
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
            healthBarInstance = null;
        }
    }

    void OnDestroy()
    {
        DestroyHealthBar();
    }

    // Public methods để control từ bên ngoài
    public void SetTarget(GameObject target)
    {
        if (target != currentTarget)
        {
            DestroyHealthBar();
            currentTarget = target;
            //if (currentTarget != null)
            //{
            //    CreateHealthBar();
            //}
        }
    }

    public void ClearTarget()
    {
        DestroyHealthBar();
        currentTarget = null;
    }

    public void SetTargetingRange(float range)
    {
        targetingRange = range;
    }

    // Debug method
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetingRange);
        
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
}