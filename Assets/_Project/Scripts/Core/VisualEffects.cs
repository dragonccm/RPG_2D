using UnityEngine;
using System.Collections;

/// <summary>
/// Component that adds pulsing effect to damage area indicators
/// </summary>
public class DamageAreaPulseEffect : MonoBehaviour
{
    private Color baseColor;
    private float duration;
    private Renderer targetRenderer;
    private Vector3 originalScale;
    private float startTime;
    
    public void Initialize(Color color, float effectDuration)
    {
        baseColor = color;
        duration = effectDuration;
        targetRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        startTime = Time.time;
        
        StartCoroutine(PulseEffect());
    }
    
    private IEnumerator PulseEffect()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float normalizedTime = elapsedTime / duration;
            
            // Pulsing scale effect
            float pulseScale = 1f + Mathf.Sin(elapsedTime * 6f) * 0.1f;
            transform.localScale = originalScale * pulseScale;
            
            // Pulsing color effect
            if (targetRenderer != null && targetRenderer.material != null)
            {
                float alpha = Mathf.Lerp(0.6f, 0.2f, normalizedTime);
                alpha *= (1f + Mathf.Sin(elapsedTime * 8f) * 0.3f);
                
                Color currentColor = baseColor;
                currentColor.a = alpha;
                targetRenderer.material.color = currentColor;
            }
            
            yield return null;
        }
    }
}

/// <summary>
/// Component that handles fade-out effect for visual indicators
/// </summary>
public class FadeOutEffect : MonoBehaviour
{
    public void StartFadeOut(float delay, float fadeTime)
    {
        StartCoroutine(FadeOutCoroutine(delay, fadeTime));
    }
    
    private IEnumerator FadeOutCoroutine(float delay, float fadeTime)
    {
        // Wait for delay
        yield return new WaitForSeconds(delay);
        
        // Get all renderers and particle systems
        var renderers = GetComponentsInChildren<Renderer>();
        var particleSystems = GetComponentsInChildren<ParticleSystem>();
        
        // Store original alpha values
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
        
        // Fade out
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            
            // Fade renderers
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    Color newColor = originalColors[i];
                    newColor.a *= alpha;
                    renderers[i].material.color = newColor;
                }
            }
            
            // Fade particle systems
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    Color currentColor = main.startColor.color;
                    currentColor.a *= alpha;
                    main.startColor = currentColor;
                }
            }
            
            yield return null;
        }
    }
}