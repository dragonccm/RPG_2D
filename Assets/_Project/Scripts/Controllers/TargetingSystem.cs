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

    private const float ScanInterval = 0.2f;
    private float nextScanTime;
    private Character targetCharacter; // cache — tránh GetComponent mỗi frame

    void Update()
    {
        if (currentTarget == null)
        {
            // Quét mục tiêu theo nhịp, không phải mỗi frame
            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + ScanInterval;
                AutoSelectTarget();
            }
            return;
        }

        if (targetCharacter == null || targetCharacter.gameObject != currentTarget)
            targetCharacter = currentTarget.GetComponent<Character>();

        if (targetCharacter == null || !targetCharacter.IsAlive
            || ((Vector2)(currentTarget.transform.position - transform.position)).sqrMagnitude > targetingRange * targetingRange)
        {
            ClearTarget();
        }
    }

    void AutoSelectTarget()
    {
        float rangeSqr = targetingRange * targetingRange;
        float closestSqr = float.MaxValue;
        Character closest = null;
        Vector2 self = transform.position;

        var all = Character.Active;
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (!c.CompareTag("Enemy") || !c.IsAlive) continue;
            float d = ((Vector2)c.transform.position - self).sqrMagnitude;
            if (d <= rangeSqr && d < closestSqr)
            {
                closestSqr = d;
                closest = c;
            }
        }

        targetCharacter = closest;
        currentTarget = closest != null ? closest.gameObject : null;
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
        targetCharacter = null;
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