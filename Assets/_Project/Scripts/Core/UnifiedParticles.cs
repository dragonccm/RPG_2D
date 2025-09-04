using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unified particle system to replace multiple effect managers
/// Consolidates ParticleManager, EffectController, and visual effects
/// </summary>
public class UnifiedParticles : MonoBehaviour
{
    [Header("Particle Prefabs")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private GameObject dustEffectPrefab;
    [SerializeField] private GameObject magicEffectPrefab;
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

    [Header("Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private bool enableDebugLogging = false;

    private Dictionary<string, ObjectPool> effectPools = new Dictionary<string, ObjectPool>();
    private Dictionary<string, GameObject> effectPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        InitializeEffectPools();
        ServiceLocator.RegisterService(this);
    }

    private void InitializeEffectPools()
    {
        // Register effect prefabs
        RegisterEffect("Hit", hitEffectPrefab);
        RegisterEffect("Blood", bloodEffectPrefab);
        RegisterEffect("Dust", dustEffectPrefab);
        RegisterEffect("Magic", magicEffectPrefab);
        RegisterEffect("LevelUp", levelUpEffectPrefab);
        RegisterEffect("Death", deathEffectPrefab);

        // Create pools for each effect
        foreach (var kvp in effectPrefabs)
        {
            if (kvp.Value != null)
            {
                CreateEffectPool(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Register a new effect prefab
    /// </summary>
    public void RegisterEffect(string effectName, GameObject prefab)
    {
        if (prefab != null && !effectPrefabs.ContainsKey(effectName))
        {
            effectPrefabs.Add(effectName, prefab);

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("✨ Registered effect: {0}", effectName));
            }
        }
    }

    /// <summary>
    /// Create object pool for effect
    /// </summary>
    private void CreateEffectPool(string effectName, GameObject prefab)
    {
        if (effectPools.ContainsKey(effectName)) return;

        var pool = new GameObject(PerformanceUtils.FormatString("{0}Pool", effectName)).AddComponent<ObjectPool>();
        pool.transform.parent = transform;
        pool.Initialize(prefab, initialPoolSize);

        effectPools.Add(effectName, pool);
    }

    /// <summary>
    /// Play effect at position
    /// </summary>
    public void PlayEffect(string effectName, Vector3 position, Quaternion rotation = default)
    {
        if (!effectPools.TryGetValue(effectName, out ObjectPool pool))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Effect pool not found: {0}", effectName));
            return;
        }

        GameObject effect = pool.Spawn(position, rotation);
        if (effect != null)
        {
            // Auto-return to pool when effect finishes
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                StartCoroutine(ReturnEffectToPool(effect, pool, duration));
            }
            else
            {
                // Fallback: return after 2 seconds
                StartCoroutine(ReturnEffectToPool(effect, pool, 2f));
            }

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("✨ Played effect: {0} at {1}", effectName, position));
            }
        }
    }

    /// <summary>
    /// Play effect at position with custom duration
    /// </summary>
    public void PlayEffect(string effectName, Vector3 position, float duration)
    {
        if (!effectPools.TryGetValue(effectName, out ObjectPool pool))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Effect pool not found: {0}", effectName));
            return;
        }

        GameObject effect = pool.Spawn(position, Quaternion.identity);
        if (effect != null)
        {
            StartCoroutine(ReturnEffectToPool(effect, pool, duration));

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("✨ Played effect: {0} at {1} for {2}s", effectName, position, duration));
            }
        }
    }

    /// <summary>
    /// Play effect attached to transform
    /// </summary>
    public void PlayEffectAttached(string effectName, Transform parent, Vector3 localPosition = default, Quaternion localRotation = default)
    {
        if (!effectPools.TryGetValue(effectName, out ObjectPool pool))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Effect pool not found: {0}", effectName));
            return;
        }

        GameObject effect = pool.Spawn(parent.position + localPosition, parent.rotation * localRotation);
        if (effect != null)
        {
            effect.transform.parent = parent;

            // Auto-return to pool when effect finishes
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                StartCoroutine(ReturnEffectToPool(effect, pool, duration));
            }
            else
            {
                StartCoroutine(ReturnEffectToPool(effect, pool, 2f));
            }

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("✨ Played attached effect: {0} on {1}", effectName, parent.name));
            }
        }
    }

    /// <summary>
    /// Play hit effect at position
    /// </summary>
    public void PlayHitEffect(Vector3 position)
    {
        PlayEffect("Hit", position);
    }

    /// <summary>
    /// Play blood effect at position
    /// </summary>
    public void PlayBloodEffect(Vector3 position)
    {
        PlayEffect("Blood", position);
    }

    /// <summary>
    /// Play dust effect at position
    /// </summary>
    public void PlayDustEffect(Vector3 position)
    {
        PlayEffect("Dust", position);
    }

    /// <summary>
    /// Play magic effect at position
    /// </summary>
    public void PlayMagicEffect(Vector3 position)
    {
        PlayEffect("Magic", position);
    }

    /// <summary>
    /// Play level up effect at position
    /// </summary>
    public void PlayLevelUpEffect(Vector3 position)
    {
        PlayEffect("LevelUp", position);
    }

    /// <summary>
    /// Play death effect at position
    /// </summary>
    public void PlayDeathEffect(Vector3 position)
    {
        PlayEffect("Death", position);
    }

    /// <summary>
    /// Play effect in direction
    /// </summary>
    public void PlayEffectInDirection(string effectName, Vector3 position, Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);
        PlayEffect(effectName, position, rotation);
    }

    /// <summary>
    /// Create particle trail between two points
    /// </summary>
    public void CreateParticleTrail(string effectName, Vector3 start, Vector3 end, float speed = 5f)
    {
        StartCoroutine(CreateTrailCoroutine(effectName, start, end, speed));
    }

    private System.Collections.IEnumerator CreateTrailCoroutine(string effectName, Vector3 start, Vector3 end, float speed)
    {
        Vector3 currentPos = start;
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        float traveled = 0f;

        while (traveled < distance)
        {
            PlayEffect(effectName, currentPos);
            currentPos += direction * speed * Time.deltaTime;
            traveled += speed * Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Create explosion effect with multiple particles
    /// </summary>
    public void CreateExplosion(string effectName, Vector3 center, float radius, int particleCount = 8)
    {
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (360f / particleCount) * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 position = center + direction * radius;

            PlayEffectInDirection(effectName, position, direction);
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("💥 Created explosion: {0} particles", particleCount));
        }
    }

    /// <summary>
    /// Stop all effects of specific type
    /// </summary>
    public void StopAllEffects(string effectName)
    {
        if (!effectPools.TryGetValue(effectName, out ObjectPool pool)) return;

        pool.ReturnAll();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("⏹️ Stopped all {0} effects", effectName));
        }
    }

    /// <summary>
    /// Stop all effects
    /// </summary>
    public void StopAllEffects()
    {
        foreach (var pool in effectPools.Values)
        {
            pool.ReturnAll();
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("⏹️ Stopped all effects");
        }
    }

    /// <summary>
    /// Prewarm effect pools
    /// </summary>
    public void PrewarmEffects()
    {
        foreach (var pool in effectPools.Values)
        {
            pool.Prewarm();
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🔥 Prewarmed all effect pools");
        }
    }

    /// <summary>
    /// Get effect pool for custom manipulation
    /// </summary>
    public ObjectPool GetEffectPool(string effectName)
    {
        effectPools.TryGetValue(effectName, out ObjectPool pool);
        return pool;
    }

    /// <summary>
    /// Check if effect exists
    /// </summary>
    public bool HasEffect(string effectName)
    {
        return effectPools.ContainsKey(effectName);
    }

    /// <summary>
    /// Get all registered effect names
    /// </summary>
    public string[] GetEffectNames()
    {
        string[] names = new string[effectPrefabs.Count];
        effectPrefabs.Keys.CopyTo(names, 0);
        return names;
    }

    private System.Collections.IEnumerator ReturnEffectToPool(GameObject effect, ObjectPool pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        pool.Return(effect);
    }

    /// <summary>
    /// Create a simple particle effect programmatically
    /// </summary>
    public GameObject CreateSimpleEffect(Color color, float size = 1f, float lifetime = 1f)
    {
        GameObject effect = new GameObject("SimpleEffect");
        var particleSystem = effect.AddComponent<ParticleSystem>();

        // Configure main module
        var main = particleSystem.main;
        main.startColor = color;
        main.startSize = size;
        main.startLifetime = lifetime;
        main.loop = false;
        main.playOnAwake = true;

        // Configure emission
        var emission = particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 10, 20)
        });

        // Configure shape
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        return effect;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize effect pools
        Gizmos.color = Color.magenta;
        foreach (var kvp in effectPools)
        {
            if (kvp.Value != null)
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
                break; // Just draw one sphere to indicate particle system presence
            }
        }
    }
}
