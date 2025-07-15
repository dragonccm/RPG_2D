using UnityEngine;
using System.Collections;

/// <summary>
/// Manager class ?? x? lý các hi?u ?ng combat nh? damage numbers, screen shake, etc.
/// </summary>
public class CombatEffectsManager : MonoBehaviour
{
    [Header("Damage Number Settings")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private float damageNumberLifetime = 1f;
    [SerializeField] private float damageNumberSpeed = 2f;
    
    [Header("Screen Shake Settings")]
    [SerializeField] private float screenShakeIntensity = 0.1f;
    [SerializeField] private float screenShakeDuration = 0.2f;
    
    [Header("Hit Stop Settings")]
    [SerializeField] private float hitStopDuration = 0.1f;
    
    private static CombatEffectsManager instance;
    public static CombatEffectsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CombatEffectsManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CombatEffectsManager");
                    instance = go.AddComponent<CombatEffectsManager>();
                }
            }
            return instance;
        }
    }

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isShaking = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }
    }

    /// <summary>
    /// Hi?n th? damage number t?i v? trí cho tr??c
    /// </summary>
    public void ShowDamageNumber(float damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (damageNumberPrefab != null)
        {
            GameObject damageNumberObj = Instantiate(damageNumberPrefab, worldPosition, Quaternion.identity);
            StartCoroutine(AnimateDamageNumber(damageNumberObj, damage, isCritical));
        }
        else
        {
            // Fallback: Create simple damage number
            StartCoroutine(CreateSimpleDamageNumber(damage, worldPosition, isCritical));
        }
    }

    /// <summary>
    /// T?o damage number ??n gi?n n?u không có prefab
    /// </summary>
    private IEnumerator CreateSimpleDamageNumber(float damage, Vector3 position, bool isCritical)
    {
        GameObject textObj = new GameObject("DamageNumber");
        textObj.transform.position = position;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = Mathf.Ceil(damage).ToString();
        textMesh.fontSize = isCritical ? 20 : 15;
        textMesh.color = isCritical ? Color.yellow : Color.red;
        textMesh.anchor = TextAnchor.MiddleCenter;
        
        // Add outline effect
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.shader = Shader.Find("GUI/Text Shader");
        }

        yield return StartCoroutine(AnimateDamageNumber(textObj, damage, isCritical));
    }

    /// <summary>
    /// Animate damage number movement and fade
    /// </summary>
    private IEnumerator AnimateDamageNumber(GameObject damageNumberObj, float damage, bool isCritical)
    {
        Vector3 startPosition = damageNumberObj.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * damageNumberSpeed;
        
        float elapsed = 0f;
        
        // Get text component (could be TextMesh or UI Text)
        TextMesh textMesh = damageNumberObj.GetComponent<TextMesh>();
        UnityEngine.UI.Text uiText = damageNumberObj.GetComponent<UnityEngine.UI.Text>();
        
        Color originalColor = Color.red;
        if (textMesh != null)
            originalColor = textMesh.color;
        else if (uiText != null)
            originalColor = uiText.color;

        while (elapsed < damageNumberLifetime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / damageNumberLifetime;
            
            // Move upward
            damageNumberObj.transform.position = Vector3.Lerp(startPosition, endPosition, progress);
            
            // Fade out
            Color currentColor = originalColor;
            currentColor.a = 1f - progress;
            
            if (textMesh != null)
                textMesh.color = currentColor;
            else if (uiText != null)
                uiText.color = currentColor;
            
            // Scale effect for critical hits
            if (isCritical)
            {
                float scale = 1f + Mathf.Sin(elapsed * 10f) * 0.1f;
                damageNumberObj.transform.localScale = Vector3.one * scale;
            }
            
            yield return null;
        }
        
        Destroy(damageNumberObj);
    }

    /// <summary>
    /// Trigger screen shake effect
    /// </summary>
    public void ScreenShake(float intensity = -1f, float duration = -1f)
    {
        if (mainCamera == null) return;
        
        float shakeIntensity = intensity > 0 ? intensity : screenShakeIntensity;
        float shakeDuration = duration > 0 ? duration : screenShakeDuration;
        
        if (!isShaking)
        {
            StartCoroutine(ScreenShakeCoroutine(shakeIntensity, shakeDuration));
        }
    }

    /// <summary>
    /// Screen shake coroutine
    /// </summary>
    private IEnumerator ScreenShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        originalCameraPosition = mainCamera.transform.position;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            Vector3 randomPoint = originalCameraPosition + Random.insideUnitSphere * intensity;
            randomPoint.z = originalCameraPosition.z; // Keep original Z position
            
            mainCamera.transform.position = randomPoint;
            
            yield return null;
        }
        
        mainCamera.transform.position = originalCameraPosition;
        isShaking = false;
    }

    /// <summary>
    /// Trigger hit stop effect (freeze frame)
    /// </summary>
    public void HitStop(float duration = -1f)
    {
        float stopDuration = duration > 0 ? duration : hitStopDuration;
        StartCoroutine(HitStopCoroutine(stopDuration));
    }

    /// <summary>
    /// Hit stop coroutine
    /// </summary>
    private IEnumerator HitStopCoroutine(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = originalTimeScale;
    }

    /// <summary>
    /// Create impact effect at position
    /// </summary>
    public void CreateImpactEffect(Vector3 position, Color color, float size = 1f)
    {
        StartCoroutine(ImpactEffectCoroutine(position, color, size));
    }

    /// <summary>
    /// Impact effect coroutine
    /// </summary>
    private IEnumerator ImpactEffectCoroutine(Vector3 position, Color color, float size)
    {
        // Create multiple particles for impact
        for (int i = 0; i < 8; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.position = position;
            particle.transform.localScale = Vector3.one * (0.1f * size);
            
            var renderer = particle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            
            // Random direction for particles
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            
            // Animate particle
            StartCoroutine(AnimateImpactParticle(particle, randomDirection, size));
        }
        
        yield return null;
    }

    /// <summary>
    /// Animate individual impact particle
    /// </summary>
    private IEnumerator AnimateImpactParticle(GameObject particle, Vector2 direction, float size)
    {
        float lifetime = 0.3f;
        float elapsed = 0f;
        Vector3 startPos = particle.transform.position;
        
        while (elapsed < lifetime && particle != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / lifetime;
            
            // Move particle
            particle.transform.position = startPos + (Vector3)(direction * progress * size);
            
            // Scale down
            float scale = (1f - progress) * 0.1f * size;
            particle.transform.localScale = Vector3.one * scale;
            
            // Fade out
            var renderer = particle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - progress;
                renderer.material.color = color;
            }
            
            yield return null;
        }
        
        if (particle != null)
        {
            Destroy(particle);
        }
    }

    /// <summary>
    /// Play combat sound effect
    /// </summary>
    public void PlayCombatSound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, mainCamera.transform.position, volume);
        }
    }

    /// <summary>
    /// Set camera reference manually if needed
    /// </summary>
    public void SetCamera(Camera camera)
    {
        mainCamera = camera;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }
    }
}