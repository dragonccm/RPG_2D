using UnityEngine;
using System.Collections;

/// <summary>
/// Animator for damage indicators with smooth transitions and effects
/// </summary>
public class DamageIndicatorAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Text textComponent;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Color originalColor;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        textComponent = GetComponent<UnityEngine.UI.Text>();
        
        originalScale = transform.localScale;
        originalPosition = transform.position;
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else if (textComponent != null)
            originalColor = textComponent.color;
    }
    
    #region Public Animation Methods
    
    /// <summary>
    /// Start pulsing animation for previews
    /// </summary>
    public void StartPulseAnimation(float intensity, float maxScale, float duration)
    {
        StartCoroutine(PulseCoroutine(intensity, maxScale, duration));
    }
    
    /// <summary>
    /// Start impact animation with strong impact feel
    /// </summary>
    public void StartImpactAnimation(Color impactColor, float duration, bool isCritical)
    {
        StartCoroutine(ImpactCoroutine(impactColor, duration, isCritical));
    }
    
    /// <summary>
    /// Start floating text animation
    /// </summary>
    public void StartFloatingTextAnimation(float height, float duration)
    {
        StartCoroutine(FloatingTextCoroutine(height, duration));
    }
    
    /// <summary>
    /// Start combo animation with flashy effects
    /// </summary>
    public void StartComboAnimation(float duration)
    {
        StartCoroutine(ComboCoroutine(duration));
    }
    
    /// <summary>
    /// Start fade in animation
    /// </summary>
    public void StartFadeIn(float duration, float targetAlpha = 1f)
    {
        StartCoroutine(FadeCoroutine(0f, targetAlpha, duration));
    }
    
    /// <summary>
    /// Start fade out animation
    /// </summary>
    public void StartFadeOut(float duration)
    {
        StartCoroutine(FadeCoroutine(GetCurrentAlpha(), 0f, duration));
    }
    
    /// <summary>
    /// Start scale animation
    /// </summary>
    public void StartScaleAnimation(Vector3 targetScale, float duration, AnimationCurve curve = null)
    {
        StartCoroutine(ScaleCoroutine(transform.localScale, targetScale, duration, curve ?? scaleCurve));
    }
    
    #endregion
    
    #region Animation Coroutines
    
    private IEnumerator PulseCoroutine(float intensity, float maxScale, float duration)
    {
        float elapsed = 0f;
        Vector3 baseScale = originalScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Create pulsing effect
            float pulsePhase = (elapsed / duration) * Mathf.PI * 6f; // 6 pulses over duration
            float pulse = Mathf.Sin(pulsePhase) * intensity;
            float scale = 1f + pulse;
            
            // Apply max scale limit
            scale = Mathf.Min(scale, maxScale);
            
            transform.localScale = baseScale * scale;
            
            // Optional alpha pulsing
            if (spriteRenderer != null)
            {
                Color color = originalColor;
                color.a = originalColor.a * (0.7f + pulse * 0.3f);
                spriteRenderer.color = color;
            }
            
            yield return null;
        }
        
        // Reset to original
        transform.localScale = baseScale;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
    
    private IEnumerator ImpactCoroutine(Color impactColor, float duration, bool isCritical)
    {
        float elapsed = 0f;
        Vector3 baseScale = originalScale;
        Vector3 startScale = baseScale * (isCritical ? 1.5f : 1.2f);
        Vector3 endScale = baseScale * 0.8f;
        
        // Impact flash
        SetColor(impactColor);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Scale animation with punch effect
            float scaleProgress = scaleCurve.Evaluate(progress);
            Vector3 currentScale = Vector3.Lerp(startScale, endScale, scaleProgress);
            transform.localScale = currentScale;
            
            // Alpha fade out
            float alpha = alphaCurve.Evaluate(progress);
            Color color = impactColor;
            color.a *= alpha;
            SetColor(color);
            
            // Critical hit intense impact effect (local movement)
            if (isCritical && progress < 0.3f)
            {
                Vector3 impactOffset = Random.insideUnitSphere * 0.05f;
                impactOffset.z = 0f;
                transform.position = originalPosition + impactOffset;
            }
            
            yield return null;
        }
        
        // Reset position
        transform.position = originalPosition;
    }
    
    private IEnumerator FloatingTextCoroutine(float height, float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = originalPosition;
        Vector3 endPos = startPos + Vector3.up * height;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Movement with easing
            float moveProgress = movementCurve.Evaluate(progress);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, moveProgress);
            transform.position = currentPos;
            
            // Scale animation (grow then shrink)
            float scaleMultiplier;
            if (progress < 0.3f)
            {
                scaleMultiplier = Mathf.Lerp(0.5f, 1.2f, progress / 0.3f);
            }
            else
            {
                scaleMultiplier = Mathf.Lerp(1.2f, 0.8f, (progress - 0.3f) / 0.7f);
            }
            transform.localScale = originalScale * scaleMultiplier;
            
            // Alpha fade out in last 30%
            if (progress > 0.7f)
            {
                float fadeProgress = (progress - 0.7f) / 0.3f;
                float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
                Color color = originalColor;
                color.a *= alpha;
                SetColor(color);
            }
            
            yield return null;
        }
    }
    
    private IEnumerator ComboCoroutine(float duration)
    {
        float elapsed = 0f;
        Vector3 baseScale = originalScale;
        
        // Start with big impact
        transform.localScale = baseScale * 1.8f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Scale down with bounce
            float bounceScale;
            if (progress < 0.2f)
            {
                bounceScale = Mathf.Lerp(1.8f, 1.1f, progress / 0.2f);
            }
            else if (progress < 0.4f)
            {
                bounceScale = Mathf.Lerp(1.1f, 1.3f, (progress - 0.2f) / 0.2f);
            }
            else
            {
                bounceScale = Mathf.Lerp(1.3f, 0.8f, (progress - 0.4f) / 0.6f);
            }
            
            transform.localScale = baseScale * bounceScale;
            
            // Color shifting for combo effect
            if (textComponent != null)
            {
                float hueShift = Mathf.Sin(progress * Mathf.PI * 3f) * 0.1f;
                Color.RGBToHSV(originalColor, out float h, out float s, out float v);
                h = (h + hueShift) % 1f;
                Color shiftedColor = Color.HSVToRGB(h, s, v);
                shiftedColor.a = originalColor.a;
                
                // Fade out in last 40%
                if (progress > 0.6f)
                {
                    float fadeProgress = (progress - 0.6f) / 0.4f;
                    shiftedColor.a *= Mathf.Lerp(1f, 0f, fadeProgress);
                }
                
                textComponent.color = shiftedColor;
            }
            
            yield return null;
        }
    }
    
    private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            Color color = originalColor;
            color.a = alpha;
            SetColor(color);
            
            yield return null;
        }
        
        // Ensure final alpha
        Color finalColor = originalColor;
        finalColor.a = toAlpha;
        SetColor(finalColor);
    }
    
    private IEnumerator ScaleCoroutine(Vector3 fromScale, Vector3 toScale, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            float curveProgress = curve.Evaluate(progress);
            Vector3 currentScale = Vector3.Lerp(fromScale, toScale, curveProgress);
            transform.localScale = currentScale;
            
            yield return null;
        }
        
        transform.localScale = toScale;
    }
    
    #endregion
    
    #region Utility Methods
    
    private void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else if (textComponent != null)
        {
            textComponent.color = color;
        }
    }
    
    private float GetCurrentAlpha()
    {
        if (spriteRenderer != null)
            return spriteRenderer.color.a;
        else if (textComponent != null)
            return textComponent.color.a;
        return 1f;
    }
    
    /// <summary>
    /// Stop all animations and reset to original state
    /// </summary>
    public void ResetToOriginal()
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
        transform.position = originalPosition;
        SetColor(originalColor);
    }
    
    /// <summary>
    /// Quick impact effect for immediate feedback
    /// </summary>
    public void QuickImpact(float intensity = 1.2f, float duration = 0.2f)
    {
        StartCoroutine(QuickImpactCoroutine(intensity, duration));
    }
    
    private IEnumerator QuickImpactCoroutine(float intensity, float duration)
    {
        Vector3 targetScale = originalScale * intensity;
        transform.localScale = targetScale;
        
        yield return StartCoroutine(ScaleCoroutine(targetScale, originalScale, duration, scaleCurve));
    }
    
    #endregion
}

