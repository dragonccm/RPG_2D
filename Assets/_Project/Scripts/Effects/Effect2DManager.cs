using UnityEngine;
using System.Collections;

/// <summary>
/// Simple 2D Effect Manager for boss and other 2D effects
/// </summary>
public static class Effect2DManager
{
    /// <summary>
    /// Create 2D effect with automatic destruction
    /// </summary>
    public static GameObject CreateEffect2D(GameObject effectPrefab, Vector3 position, Quaternion rotation, float lifetime, bool autoDestroy = true)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("Effect2DManager: effectPrefab is null");
            return CreateFallbackEffect2D(position, Color.white, 1f, lifetime);
        }

        GameObject effect = Object.Instantiate(effectPrefab, position, rotation);
        effect.name = $"{effectPrefab.name}_2D_{Time.time:F2}";

        if (autoDestroy)
        {
            // Auto destroy after lifetime
            Object.Destroy(effect, lifetime);
        }

        return effect;
    }

    /// <summary>
    /// Create simple fallback effect when no prefab is available
    /// </summary>
    public static GameObject CreateFallbackEffect2D(Vector3 position, Color color, float size, float lifetime)
    {
        GameObject fallbackEffect = new GameObject("FallbackEffect2D");
        fallbackEffect.transform.position = position;

        // Create simple circle sprite
        var spriteRenderer = fallbackEffect.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite(32, color);
        spriteRenderer.sortingOrder = 100;

        // Set initial scale
        fallbackEffect.transform.localScale = Vector3.one * size;

        // Add pulsing animation
        var pulser = fallbackEffect.AddComponent<SimpleEffectPulser>();
        pulser.Initialize(lifetime, size);

        // Auto destroy
        Object.Destroy(fallbackEffect, lifetime);

        return fallbackEffect;
    }

    /// <summary>
    /// Create effect that follows a transform
    /// </summary>
    public static GameObject CreateFollowEffect2D(GameObject effectPrefab, Transform target, Vector3 offset, float scale, bool autoDestroy = false)
    {
        if (effectPrefab == null || target == null)
        {
            Debug.LogWarning("Effect2DManager: effectPrefab or target is null");
            return null;
        }

        GameObject effect = Object.Instantiate(effectPrefab, target.position + offset, target.rotation);
        effect.name = $"{effectPrefab.name}_Follow_{Time.time:F2}";
        effect.transform.localScale = Vector3.one * scale;

        // Add follow component
        var follower = effect.AddComponent<EffectFollower2D>();
        follower.Initialize(target, offset);

        return effect;
    }

    /// <summary>
    /// Create warning indicator for boss attacks
    /// </summary>
    public static GameObject CreateWarningIndicator2D(Vector3 position, float radius, float warningTime, Color warningColor)
    {
        GameObject warning = new GameObject("WarningIndicator2D");
        warning.transform.position = position;

        // Create warning sprite
        var spriteRenderer = warning.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite(64, warningColor);
        spriteRenderer.sortingOrder = 50;

        // Set scale to radius
        warning.transform.localScale = Vector3.one * radius * 2f;

        // Add warning animation
        var animator = warning.AddComponent<WarningIndicatorAnimator>();
        animator.Initialize(warningTime, warningColor);

        // Auto destroy after warning time
        Object.Destroy(warning, warningTime);

        return warning;
    }

    /// <summary>
    /// Create simple circle sprite
    /// </summary>
    private static Sprite CreateCircleSprite(int resolution, Color color)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        Color[] colors = new Color[resolution * resolution];
        Vector2 center = Vector2.one * (resolution * 0.5f);
        float radius = resolution * 0.4f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    float alpha = 1f - (distance / radius);
                    colors[y * resolution + x] = new Color(color.r, color.g, color.b, alpha * color.a);
                }
                else
                {
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f);
    }
}

/// <summary>
/// Simple effect pulser for fallback effects
/// </summary>
public class SimpleEffectPulser : MonoBehaviour
{
    private float duration;
    private float maxScale;
    private Vector3 originalScale;
    private SpriteRenderer spriteRenderer;

    public void Initialize(float effectDuration, float scale)
    {
        duration = effectDuration;
        maxScale = scale;
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();

        StartCoroutine(PulseEffect());
    }

    private IEnumerator PulseEffect()
    {
        float elapsed = 0f;
        Color originalColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Pulse scale
            float pulse = 1f + Mathf.Sin(elapsed * 10f) * 0.1f;
            transform.localScale = originalScale * pulse;

            // Fade out
            Color color = originalColor;
            color.a = 1f - progress;
            spriteRenderer.color = color;

            yield return null;
        }
    }
}

/// <summary>
/// Warning indicator animator for boss attacks
/// </summary>
public class WarningIndicatorAnimator : MonoBehaviour
{
    private float duration;
    private Color warningColor;
    private SpriteRenderer spriteRenderer;

    public void Initialize(float warningDuration, Color color)
    {
        duration = warningDuration;
        warningColor = color;
        spriteRenderer = GetComponent<SpriteRenderer>();

        StartCoroutine(AnimateWarning());
    }

    private IEnumerator AnimateWarning()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Pulsing effect - faster as time progresses
            float pulseSpeed = 2f + progress * 8f;
            float pulse = Mathf.Sin(elapsed * pulseSpeed);

            // Color intensity based on pulse and progress
            Color color = warningColor;
            color.a = 0.3f + (pulse * 0.3f) + (progress * 0.4f);
            spriteRenderer.color = color;

            // Scale pulsing
            float scalePulse = 1f + pulse * 0.1f;
            transform.localScale = Vector3.one * transform.localScale.x * scalePulse;

            yield return null;
        }

        // Final flash
        spriteRenderer.color = new Color(warningColor.r, warningColor.g, warningColor.b, 1f);
        yield return new WaitForSeconds(0.1f);
    }
}

/// <summary>
/// Effect follower component for effects that need to follow a target
/// </summary>
public class EffectFollower2D : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;

    public void Initialize(Transform followTarget, Vector3 followOffset)
    {
        target = followTarget;
        offset = followOffset;
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
        else
        {
            // Target was destroyed, destroy this effect too
            Destroy(gameObject);
        }
    }
}

