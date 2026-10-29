using UnityEngine;
using System.Collections;

/// <summary>
/// 2D Effect Manager - Qu?n lý hi?u ?ng sprite animation cho môi tr??ng 2D
/// Thay th? vi?c t?o nhi?u particle systems b?ng prefab sprite animation hoàn ch?nh
/// </summary>
public class Effect2DManager : MonoBehaviour
{
    private static Effect2DManager _instance;
    public static Effect2DManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<Effect2DManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Effect2DManager");
                    _instance = go.AddComponent<Effect2DManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// T?o hi?u ?ng 2D t? prefab - PH??NG PHÁP ?ÚNG cho 2D
    /// </summary>
    /// <param name="effectPrefab">Prefab hi?u ?ng v?i sprite animation</param>
    /// <param name="position">V? trí spawn</param>
    /// <param name="rotation">Góc xoay (m?c ??nh Quaternion.identity)</param>
    /// <param name="scale">T? l? scale (m?c ??nh 1f)</param>
    /// <param name="autoDestroy">T? ??ng destroy khi animation hoàn thành (m?c ??nh true)</param>
    /// <returns>GameObject c?a hi?u ?ng ???c t?o</returns>
    public static GameObject CreateEffect2D(GameObject effectPrefab, Vector3 position, 
        Quaternion rotation = default, float scale = 1f, bool autoDestroy = true)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("[Effect2DManager] EffectPrefab is null! Cannot create effect.");
            return null;
        }

        // T?o effect t? prefab - CHÍNH XÁC nh? yêu c?u
        GameObject effect = Instantiate(effectPrefab, position, rotation == default ? Quaternion.identity : rotation);
        
        // Áp d?ng scale n?u khác 1
        if (scale != 1f)
        {
            effect.transform.localScale = Vector3.one * scale;
        }

        // T? ??ng destroy khi animation hoàn thành
        if (autoDestroy)
        {
            var autoDestroyer = effect.GetComponent<Effect2DAutoDestroy>();
            if (autoDestroyer == null)
            {
                autoDestroyer = effect.AddComponent<Effect2DAutoDestroy>();
            }
            autoDestroyer.Initialize();
        }

        Debug.Log($"[Effect2DManager] Created 2D effect: {effectPrefab.name} at {position}");
        return effect;
    }

    /// <summary>
    /// T?o hi?u ?ng 2D v?i th?i gian t?n t?i c? th?
    /// </summary>
    public static GameObject CreateEffect2DWithDuration(GameObject effectPrefab, Vector3 position, 
        float duration, Quaternion rotation = default, float scale = 1f)
    {
        GameObject effect = CreateEffect2D(effectPrefab, position, rotation, scale, false);
        if (effect != null)
        {
            Destroy(effect, duration);
        }
        return effect;
    }

    /// <summary>
    /// T?o hi?u ?ng 2D theo dõi target
    /// </summary>
    public static GameObject CreateFollowEffect2D(GameObject effectPrefab, Transform target, 
        Vector3 offset = default, float scale = 1f, bool autoDestroy = true)
    {
        if (target == null) return null;

        GameObject effect = CreateEffect2D(effectPrefab, target.position + offset, Quaternion.identity, scale, autoDestroy);
        if (effect != null)
        {
            var follower = effect.AddComponent<Effect2DFollowTarget>();
            follower.Initialize(target, offset);
        }
        return effect;
    }

    /// <summary>
    /// T?o hi?u ?ng impact 2D t?i ?i?m va ch?m
    /// </summary>
    public static GameObject CreateImpactEffect2D(GameObject effectPrefab, Vector3 impactPosition, 
        Vector3 impactDirection = default, float scale = 1f)
    {
        Quaternion rotation = Quaternion.identity;
        
        // Xoay effect theo h??ng impact n?u có
        if (impactDirection != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(Vector3.forward, impactDirection);
        }

        return CreateEffect2D(effectPrefab, impactPosition, rotation, scale, true);
    }

    /// <summary>
    /// Fallback method cho tr??ng h?p không có prefab - T?O HI?U ?NG ??N GI?N
    /// CH? S? D?NG KHI KHÔNG CÓ PREFAB
    /// </summary>
    public static GameObject CreateFallbackEffect2D(Vector3 position, Color color, float size = 1f, float duration = 1f)
    {
        GameObject fallbackEffect = new GameObject("FallbackEffect2D");
        fallbackEffect.transform.position = position;

        // T?o sprite renderer ??n gi?n
        SpriteRenderer spriteRenderer = fallbackEffect.AddComponent<SpriteRenderer>();
        
        // T?o texture ??n gi?n
        Texture2D texture = CreateSimpleCircleTexture((int)(32 * size), color);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 100; // Hi?n th? trên cùng

        // Thêm animation ??n gi?n
        fallbackEffect.AddComponent<SimpleEffect2DAnimator>().Initialize(duration, color);

        // Auto destroy
        Destroy(fallbackEffect, duration);
        
        Debug.LogWarning($"[Effect2DManager] Using fallback effect at {position} - consider using proper effect prefab!");
        return fallbackEffect;
    }

    /// <summary>
    /// T?o texture hình tròn ??n gi?n cho fallback
    /// </summary>
    private static Texture2D CreateSimpleCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        Vector2 center = Vector2.one * (size * 0.5f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius);
                    colors[y * size + x] = new Color(color.r, color.g, color.b, alpha * color.a);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
}

/// <summary>
/// Component t? ??ng destroy hi?u ?ng 2D khi animation hoàn thành
/// </summary>
public class Effect2DAutoDestroy : MonoBehaviour
{
    private Animator animator;
    private float fallbackDuration = 2f; // Th?i gian fallback n?u không có animator

    public void Initialize(float customDuration = -1f)
    {
        animator = GetComponent<Animator>();
        
        if (animator != null)
        {
            // L?y th?i gian animation t? animator
            StartCoroutine(WaitForAnimationComplete());
        }
        else
        {
            // Fallback: destroy sau th?i gian c? ??nh
            float destroyTime = customDuration > 0 ? customDuration : fallbackDuration;
            Destroy(gameObject, destroyTime);
        }
    }

    private IEnumerator WaitForAnimationComplete()
    {
        // ??i frame ??u tiên ?? animator kh?i t?o
        yield return null;
        
        // ??i animation hoàn thành
        while (animator != null && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
        
        // Animation hoàn thành, destroy object
        Destroy(gameObject);
    }
}

/// <summary>
/// Component làm hi?u ?ng 2D theo dõi target
/// </summary>
public class Effect2DFollowTarget : MonoBehaviour
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
            // Target không t?n t?i, destroy effect
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Component t?o animation ??n gi?n cho fallback effects
/// </summary>
public class SimpleEffect2DAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float duration;
    private Color originalColor;
    private Vector3 originalScale;

    public void Initialize(float animDuration, Color color)
    {
        duration = animDuration;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = color;
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            StartCoroutine(AnimateEffect());
        }
    }

    private IEnumerator AnimateEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Fade out alpha
            float alpha = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            // Scale animation
            float scale = Mathf.Lerp(0.5f, 1.2f, Mathf.Sin(progress * Mathf.PI));
            transform.localScale = originalScale * scale;
            
            yield return null;
        }
    }
}