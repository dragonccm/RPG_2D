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

    // S2 (GAME_MASTER_PLAN 12.2-1): pooling cho damage numbers + impact effects
    private ObjectPool damageNumberPool;
    private ObjectPool impactEffectPool;
    private static Sprite circleSprite;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }

        InitializePoolsIfNeeded();
    }

    private void InitializePoolsIfNeeded()
    {
        if (damageNumberPool == null)
        {
            damageNumberPool = new ObjectPool(CreateDamageNumberObject, transform);
        }
        if (impactEffectPool == null)
        {
            impactEffectPool = new ObjectPool(CreateImpactObject, transform);
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int S = 48;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x - (S - 1) * 0.5f) / ((S - 1) * 0.5f);
                float dy = (y - (S - 1) * 0.5f) / ((S - 1) * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(1f - d) * 255f);
                px[y * S + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    private GameObject CreateDamageNumberObject()
    {
        var go = new GameObject("DamageNumber");
        var tm = go.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.4f;
        return go;
    }

    private GameObject CreateImpactObject()
    {
        var go = new GameObject("ImpactEffect");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.sortingLayerName = "Effects";
        sr.sortingOrder = 5000;
        return go;
    }

    public void ShowDamageNumber(float damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (!Application.isPlaying) return;
        InitializePoolsIfNeeded();
        if (damageNumberPrefab != null)
        {
            GameObject damageNumberObj = Instantiate(damageNumberPrefab, worldPosition, Quaternion.identity);
            StartCoroutine(AnimateDamageNumber(damageNumberObj, damage, isCritical));
        }
        else
        {
            // S2: dùng pool thay cho create/destroy mỗi lần (0 GC trong gameplay ổn định)
            GameObject textObj = damageNumberPool.Get(worldPosition);
            StartCoroutine(AnimatePooledDamageNumber(textObj, damage, worldPosition, isCritical));
        }
    }

    private IEnumerator AnimatePooledDamageNumber(GameObject textObj, float damage, Vector3 position, bool isCritical)
    {
        TextMesh textMesh = textObj.GetComponent<TextMesh>();
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

        damageNumberPool.Release(textObj);
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
        if (!Application.isPlaying || mainCamera == null) return;
        
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
        if (!Application.isPlaying) return;
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
        if (!Application.isPlaying) return;
        InitializePoolsIfNeeded();
        // S2: pooled sprite effect (2D, URP-safe) thay primitive 3D Standard shader
        StartCoroutine(CreateImpactEffectCoroutine(position, color, scale));
    }

    private IEnumerator CreateImpactEffectCoroutine(Vector3 position, Color color, float scale)
    {
        GameObject effectObj = impactEffectPool.Get(position);
        var renderer = effectObj.GetComponent<SpriteRenderer>();
        renderer.color = color;

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
                Color currentColor = renderer.color;
                currentColor.a = 1f - progress;
                renderer.color = currentColor;
            }

            yield return null;
        }

        impactEffectPool.Release(effectObj);
    }
}