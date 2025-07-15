/// <summary>
/// File: CombatEffectsManager.cs  
/// Author: Unity 2D RPG Refactoring Agent
/// Description: Manages combat visual effects like damage numbers, screen shake, hit stop
/// </summary>

using UnityEngine;
using System.Collections;

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
                instance = FindFirstObjectByType<CombatEffectsManager>();
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

    public void ShowDamageNumber(float damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (damageNumberPrefab != null)
        {
            GameObject damageNumberObj = Instantiate(damageNumberPrefab, worldPosition, Quaternion.identity);
            StartCoroutine(AnimateDamageNumber(damageNumberObj, damage, isCritical));
        }
        else
        {
            StartCoroutine(CreateSimpleDamageNumber(damage, worldPosition, isCritical));
        }
    }

    private IEnumerator CreateSimpleDamageNumber(float damage, Vector3 position, bool isCritical)
    {
        GameObject textObj = new GameObject("DamageNumber");
        textObj.transform.position = position;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = Mathf.Ceil(damage).ToString();
        textMesh.fontSize = isCritical ? 20 : 14;
        textMesh.color = isCritical ? Color.yellow : Color.red;
        textMesh.anchor = TextAnchor.MiddleCenter;
        
        float elapsedTime = 0f;
        Vector3 startPos = position;
        Vector3 endPos = position + Vector3.up * 2f;
        
        while (elapsedTime < damageNumberLifetime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / damageNumberLifetime;
            
            textObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            Color color = textMesh.color;
            color.a = 1f - progress;
            textMesh.color = color;
            
            yield return null;
        }
        
        Destroy(textObj);
    }

    private IEnumerator AnimateDamageNumber(GameObject damageNumberObj, float damage, bool isCritical)
    {
        var textComponent = damageNumberObj.GetComponent<TextMesh>();
        if (textComponent != null)
        {
            textComponent.text = Mathf.Ceil(damage).ToString();
            textComponent.color = isCritical ? Color.yellow : Color.red;
        }
        
        Vector3 startPos = damageNumberObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * damageNumberSpeed;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < damageNumberLifetime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / damageNumberLifetime;
            
            damageNumberObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            if (textComponent != null)
            {
                Color color = textComponent.color;
                color.a = 1f - progress;
                textComponent.color = color;
            }
            
            yield return null;
        }
        
        Destroy(damageNumberObj);
    }

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

    private IEnumerator ScreenShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        originalCameraPosition = mainCamera.transform.position;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            float offsetX = Random.Range(-intensity, intensity);
            float offsetY = Random.Range(-intensity, intensity);
            
            mainCamera.transform.position = originalCameraPosition + new Vector3(offsetX, offsetY, 0);
            
            yield return null;
        }
        
        mainCamera.transform.position = originalCameraPosition;
        isShaking = false;
    }

    public void HitStop(float duration = -1f)
    {
        float stopDuration = duration > 0 ? duration : hitStopDuration;
        StartCoroutine(HitStopCoroutine(stopDuration));
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = originalTimeScale;
    }

    public void CreateImpactEffect(Vector3 position, Color color, float scale = 1f)
    {
        StartCoroutine(CreateImpactEffectCoroutine(position, color, scale));
    }

    private IEnumerator CreateImpactEffectCoroutine(Vector3 position, Color color, float scale)
    {
        GameObject effectObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effectObj.name = "ImpactEffect";
        effectObj.transform.position = position;
        effectObj.transform.localScale = Vector3.zero;
        
        var collider = effectObj.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        
        var renderer = effectObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            renderer.material = material;
        }
        
        float duration = 0.3f;
        float elapsedTime = 0f;
        Vector3 targetScale = Vector3.one * scale;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            effectObj.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);
            
            if (renderer != null)
            {
                Color currentColor = renderer.material.color;
                currentColor.a = 1f - progress;
                renderer.material.color = currentColor;
            }
            
            yield return null;
        }
        
        Destroy(effectObj);
    }
}