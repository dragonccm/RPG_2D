using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pooling system for performance optimization
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new Queue<T>();
    private readonly T prefab;
    private readonly Transform parent;
    private readonly int initialSize;
    private readonly int maxSize;

    public ObjectPool(T prefab, Transform parent = null, int initialSize = 10, int maxSize = 50)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.initialSize = initialSize;
        this.maxSize = maxSize;

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private T CreateNewObject()
    {
        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// Get an object from the pool
    /// </summary>
    public T Get()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
        }

        // Pool is empty or object was destroyed, create new one
        if (pool.Count < maxSize)
        {
            return CreateNewObject();
        }

        // Pool is at max size, return null
        PerformanceUtils.LogWarning("Object pool reached maximum size!");
        return null;
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null || !obj.gameObject.activeSelf)
            return;

        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    /// <summary>
    /// Clear all objects from the pool
    /// </summary>
    public void Clear()
    {
        while (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
    }

    /// <summary>
    /// Get current pool size
    /// </summary>
    public int Count => pool.Count;
}

/// <summary>
/// MonoBehaviour-based object pool for Unity components
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private bool enableDebugLogging = false;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private bool isInitialized = false;

    /// <summary>
    /// Initialize the pool with a prefab
    /// </summary>
    public void Initialize(GameObject prefab, int initialSize = 10)
    {
        if (isInitialized)
        {
            PerformanceUtils.LogWarning("ObjectPool is already initialized!");
            return;
        }

        this.prefab = prefab;
        this.initialPoolSize = initialSize;
        InitializePool();

        isInitialized = true;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔄 ObjectPool initialized with {0} objects", initialPoolSize));
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// Spawn an object from the pool at the specified position and rotation
    /// </summary>
    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        if (!isInitialized)
        {
            PerformanceUtils.LogWarning("ObjectPool must be initialized before spawning!");
            return null;
        }

        GameObject obj = GetFromPool();
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("🎯 Spawned object at {0}", position));
            }
        }

        return obj;
    }

    /// <summary>
    /// Spawn an object from the pool with a specific prefab (for dynamic pooling)
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!isInitialized || this.prefab != prefab)
        {
            // Reinitialize with new prefab if different
            Clear();
            Initialize(prefab, initialPoolSize);
        }

        return Spawn(position, rotation);
    }

    private GameObject GetFromPool()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                return obj;
            }
        }

        // Pool is empty or object was destroyed, create new one
        if (pool.Count < maxPoolSize)
        {
            return CreateNewObject();
        }

        // Pool is at max size, return null
        PerformanceUtils.LogWarning("Object pool reached maximum size!");
        return null;
    }

    /// <summary>
    /// Return an object to the pool
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null || !obj.activeSelf)
            return;

        obj.SetActive(false);
        obj.transform.parent = transform;
        pool.Enqueue(obj);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("♻️ Object returned to pool");
        }
    }

    /// <summary>
    /// Return all active objects to the pool
    /// </summary>
    public void ReturnAll()
    {
        GameObject[] activeObjects = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            activeObjects[i] = transform.GetChild(i).gameObject;
        }

        foreach (GameObject obj in activeObjects)
        {
            if (obj.activeSelf)
            {
                Return(obj);
            }
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("♻️ All objects returned to pool");
        }
    }

    /// <summary>
    /// Prewarm the pool by creating all initial objects
    /// </summary>
    public void Prewarm()
    {
        if (!isInitialized)
        {
            PerformanceUtils.LogWarning("ObjectPool must be initialized before prewarming!");
            return;
        }

        // Pool is already initialized in InitializePool()
        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔥 Pool prewarmed with {0} objects", pool.Count));
        }
    }

    /// <summary>
    /// Clear all objects from the pool
    /// </summary>
    public void Clear()
    {
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // Also destroy any remaining children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        isInitialized = false;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🗑️ Object pool cleared");
        }
    }

    /// <summary>
    /// Get current pool size
    /// </summary>
    public int Count => pool.Count;
}
