/// <summary>
/// File: TargetingSystem.cs
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Clean targeting system with proper API usage and removed deprecated code
/// </summary>

using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
    [Header("Targeting Settings")]
    public GameObject currentTarget;
    [SerializeField] private float targetingRange = 10f;
    
    private GameObject healthBarInstance;
    private Canvas uiCanvas;

    void Start()
    {
        uiCanvas = GameObject.Find("UICanvas")?.GetComponent<Canvas>();
        if (uiCanvas == null)
            uiCanvas = FindFirstObjectByType<Canvas>();
        
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
            Character targetCharacter = currentTarget.GetComponent<Character>();
            if (targetCharacter == null || targetCharacter.health.currentValue <= 0)
            {
                DestroyHealthBar();
                currentTarget = null;
                return;
            }
            
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
        
        if (closestEnemy != currentTarget)
        {
            currentTarget = closestEnemy;
        }
    }

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

    public void SetTarget(GameObject target)
    {
        if (target != currentTarget)
        {
            DestroyHealthBar();
            currentTarget = target;
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