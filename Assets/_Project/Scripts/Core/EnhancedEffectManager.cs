using UnityEngine;
using System.Collections;

/// <summary>
/// Enhanced Effect Manager - Qu?n lý vi?c t?o, ??nh v? và h?y effect prefabs
/// ??m b?o effect xu?t hi?n ?úng v? trí va ch?m và ???c h?y sau khi animation hoàn t?t
/// </summary>
public class EnhancedEffectManager : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float defaultEffectLifetime = 3f;
    [SerializeField] private bool autoDetectAnimationLength = true;
    [SerializeField] private bool enableDebugLogging = true;
    
    [Header("Position Adjustment")]
    [SerializeField] private Vector3 defaultOffset = Vector3.zero;
    [SerializeField] private bool adjustForCollisionPoint = true;
    
    /// <summary>
    /// T?o effect t?i v? trí va ch?m chính xác v?i auto-destroy
    /// </summary>
    public static GameObject CreateEffectAtPosition(GameObject effectPrefab, Vector3 position, 
        Quaternion rotation = default, Transform parent = null, float? customLifetime = null)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("?? EffectPrefab is null - cannot create effect");
            return null;
        }
        
        // Instantiate effect t?i v? trí chính xác
        GameObject effectInstance = Instantiate(effectPrefab, position, rotation, parent);
        effectInstance.name = $"{effectPrefab.name}_Effect_{Time.time:F2}";
        
        // Add auto-destroy component
        var autoDestroy = effectInstance.AddComponent<EffectAutoDestroy>();
        
        // Determine lifetime
        float lifetime = customLifetime ?? GetEffectLifetime(effectInstance);
        autoDestroy.Initialize(lifetime, true); // Enable debug logging
        
        Debug.Log($"? Created effect '{effectPrefab.name}' at position {position} with lifetime {lifetime}s");
        
        return effectInstance;
    }
    
    /// <summary>
    /// T?o effect t?i v? trí va ch?m v?i m?c tiêu c? th?
    /// </summary>
    public static GameObject CreateImpactEffect(GameObject effectPrefab, Vector3 impactPosition, 
        Vector3 impactDirection, GameObject target = null, float? customLifetime = null)
    {
        if (effectPrefab == null) return null;
        
        // Adjust position for better visual impact
        Vector3 adjustedPosition = impactPosition;
        if (target != null)
        {
            // Position effect slightly in front of target for visibility
            adjustedPosition += impactDirection.normalized * 0.1f;
        }
        
        // Calculate rotation based on impact direction
        Quaternion rotation = Quaternion.identity;
        if (impactDirection != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(impactDirection);
        }
        
        GameObject effectInstance = CreateEffectAtPosition(effectPrefab, adjustedPosition, rotation, null, customLifetime);
        
        if (effectInstance != null)
        {
            // Add impact-specific enhancements
            var impactEnhancer = effectInstance.AddComponent<ImpactEffectEnhancer>();
            impactEnhancer.Initialize(target, impactDirection);
        }
        
        return effectInstance;
    }
    
    /// <summary>
    /// T?o effect v?i follow target (cho projectile hits)
    /// </summary>
    public static GameObject CreateFollowEffect(GameObject effectPrefab, Transform target, 
        Vector3 offset = default, float? customLifetime = null)
    {
        if (effectPrefab == null || target == null) return null;
        
        Vector3 spawnPosition = target.position + offset;
        GameObject effectInstance = CreateEffectAtPosition(effectPrefab, spawnPosition, Quaternion.identity, null, customLifetime);
        
        if (effectInstance != null)
        {
            // Add follow behavior
            var follower = effectInstance.AddComponent<EffectFollowTarget>();
            follower.Initialize(target, offset);
        }
        
        return effectInstance;
    }
    
    /// <summary>
    /// Determine effect lifetime t? animation clips ho?c particle systems
    /// </summary>
    private static float GetEffectLifetime(GameObject effectInstance)
    {
        float maxLifetime = 1f; // Default fallback
        
        // Check Animator components
        var animators = effectInstance.GetComponentsInChildren<Animator>();
        foreach (var animator in animators)
        {
            if (animator.runtimeAnimatorController != null)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    maxLifetime = Mathf.Max(maxLifetime, clip.length);
                }
            }
        }
        
        // Check ParticleSystem components
        var particleSystems = effectInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            if (ps.main.loop)
            {
                // For looping systems, use a reasonable time
                maxLifetime = Mathf.Max(maxLifetime, 3f);
            }
            else
            {
                // Calculate total lifetime including start lifetime
                float psLifetime = ps.main.startLifetime.constantMax + ps.main.duration;
                maxLifetime = Mathf.Max(maxLifetime, psLifetime);
            }
        }
        
        // Check Animation components (legacy)
        var animations = effectInstance.GetComponentsInChildren<Animation>();
        foreach (var anim in animations)
        {
            foreach (AnimationState state in anim)
            {
                maxLifetime = Mathf.Max(maxLifetime, state.length);
            }
        }
        
        // Add buffer time for cleanup
        return maxLifetime + 0.5f;
    }
}

/// <summary>
/// Component t? ??ng h?y effect sau khi animation hoàn t?t
/// </summary>
public class EffectAutoDestroy : MonoBehaviour
{
    private float lifetime;
    private bool enableDebug;
    private float startTime;
    
    public void Initialize(float effectLifetime, bool debug = false)
    {
        lifetime = effectLifetime;
        enableDebug = debug;
        startTime = Time.time;
        
        if (enableDebug)
        {
            Debug.Log($"?? Effect '{gameObject.name}' will be destroyed in {lifetime}s");
        }
        
        // Start destruction countdown
        StartCoroutine(DestroyAfterTime());
    }
    
    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(lifetime);
        
        if (enableDebug)
        {
            float actualLifetime = Time.time - startTime;
            Debug.Log($"?? Destroying effect '{gameObject.name}' after {actualLifetime:F2}s");
        }
        
        // Fade out effect before destroying if possible
        yield return StartCoroutine(FadeOutEffect());
        
        Destroy(gameObject);
    }
    
    private IEnumerator FadeOutEffect()
    {
        float fadeTime = 0.3f;
        float elapsedTime = 0f;
        
        // Get all renderers for fading
        var renderers = GetComponentsInChildren<Renderer>();
        var originalColors = new Color[renderers.Length];
        
        // Store original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
        
        // Fade out
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    Color newColor = originalColors[i];
                    newColor.a *= alpha;
                    renderers[i].material.color = newColor;
                }
            }
            
            yield return null;
        }
    }
}

/// <summary>
/// Component c?i thi?n effect va ch?m v?i target tracking
/// </summary>
public class ImpactEffectEnhancer : MonoBehaviour
{
    private GameObject target;
    private Vector3 impactDirection;
    
    public void Initialize(GameObject impactTarget, Vector3 direction)
    {
        target = impactTarget;
        impactDirection = direction.normalized;
        
        // Apply impact-specific enhancements
        ApplyImpactEnhancements();
    }
    
    private void ApplyImpactEnhancements()
    {
        // Scale effect based on target size if available
        if (target != null)
        {
            var targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                Vector3 targetSize = targetRenderer.bounds.size;
                float scale = Mathf.Max(targetSize.x, targetSize.y, targetSize.z);
                transform.localScale *= Mathf.Clamp(scale, 0.5f, 2f);
            }
        }
        
        // Apply screen shake for strong impacts
        if (impactDirection.magnitude > 0.8f)
        {
            var effectsManager = FindFirstObjectByType<CombatEffectsManager>();
            if (effectsManager != null)
            {
                effectsManager.ScreenShake(0.1f, 0.2f);
            }
        }
    }
}

/// <summary>
/// Component làm effect follow target (cho continuous effects)
/// </summary>
public class EffectFollowTarget : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    
    public void Initialize(Transform followTarget, Vector3 followOffset)
    {
        target = followTarget;
        offset = followOffset;
    }
    
    private void Update()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
        else
        {
            // Target destroyed, destroy effect
            Destroy(gameObject);
        }
    }
}