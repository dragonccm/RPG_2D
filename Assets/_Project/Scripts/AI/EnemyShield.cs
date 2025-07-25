using UnityEngine;
using System.Collections;

/// <summary>
/// X? l� kh? n?ng ph�ng th? v?i l� ch?n c?a k? th�
/// </summary>
public class EnemyShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float shieldHealth = 50f;
    [SerializeField] private float maxShieldHealth = 50f;
    [SerializeField] private float shieldRegenRate = 5f;
    [SerializeField] private float shieldRegenDelay = 3f;
    [SerializeField] private float damageReduction = 0.7f;
    [SerializeField] private bool blockAllDamageWhenActive = false;
    
    [Header("Shield Activation")]
    [SerializeField] private bool autoActivateAtHealthPercent = true;
    [SerializeField] private float activateHealthPercent = 0.5f;
    [SerializeField] private float shieldCooldown = 10f;
    [SerializeField] private float shieldDuration = 5f;
    [SerializeField] private bool canActivateMultipleTimes = true;
    
    [Header("Shield Visuals")]
    [SerializeField] private GameObject shieldVisualPrefab;
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.5f, 1f, 0.5f);
    [SerializeField] private bool pulseWhenActive = true;
    [SerializeField] private float pulseRate = 1f;
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private bool flashOnHit = true;
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    
    [Header("Shield Effects")]
    [SerializeField] private GameObject shieldActivateEffectPrefab;
    [SerializeField] private GameObject shieldBreakEffectPrefab;
    [SerializeField] private GameObject shieldHitEffectPrefab;
    [SerializeField] private AudioClip shieldActivateSound;
    [SerializeField] private AudioClip shieldBreakSound;
    [SerializeField] private AudioClip shieldHitSound;
    [SerializeField] private float soundVolume = 0.5f;
    
    // C�c component
    private Enemy enemy;
    // private Animator animator; // Removed direct Animator reference
    private AudioSource audioSource;
    
    // Tr?ng th�i l� ch?n
    private bool isShieldActive = false;
    private float lastShieldActivateTime = -999f;
    private float lastShieldHitTime = -999f;
    private float shieldDeactivateTime = 0f;
    private int activationCount = 0;
    private float damageTaken = 0f;
    
    // ??i t??ng l� ch?n
    private GameObject shieldVisual;
    private SpriteRenderer shieldRenderer;
    
    // Coroutine
    private Coroutine pulseCoroutine;
    private Coroutine regenCoroutine;
    
    private void Awake()
    {
        // L?y c�c component c?n thi?t
        enemy = GetComponent<Enemy>();
        // animator = GetComponent<Animator>(); // Removed direct Animator reference
        audioSource = GetComponent<AudioSource>();
        
        // T?o AudioSource n?u ch?a c�
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = soundVolume;
        }
        
        // ??ng k� s? ki?n nh?n s�t th??ng
        if (enemy != null)
        {
            enemy.OnDamageTaken += HandleDamageTaken;
            enemy.OnHealthChanged += HandleHealthChanged;
        }
    }
    
    private void OnDestroy()
    {
        // H?y ??ng k� s? ki?n
        if (enemy != null)
        {
            enemy.OnDamageTaken -= HandleDamageTaken;
            enemy.OnHealthChanged -= HandleHealthChanged;
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n thay ??i m�u
    /// </summary>
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // N?u ?� k�ch ho?t t? ??ng v� m�u gi?m xu?ng d??i ng??ng
        if (autoActivateAtHealthPercent && !isShieldActive && CanActivateShield())
        {
            float healthPercent = currentHealth / maxHealth;
            if (healthPercent <= activateHealthPercent)
            {
                ActivateShield();
            }
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n nh?n s�t th??ng
    /// </summary>
    private void HandleDamageTaken(Enemy enemy, float damage, float newHealth)
    {
        // N?u l� ch?n kh�ng ho?t ??ng, kh�ng l�m g� c?
        if (!isShieldActive)
        {
            return;
        }
        
        // C?p nh?t th?i gian b? ?�nh cu?i
        lastShieldHitTime = Time.time;
        
        // N?u ch?n t?t c? s�t th??ng
        if (blockAllDamageWhenActive)
        {
            // Gi?m l??ng l� ch?n
            shieldHealth -= damage;
            
            // ??t s�t th??ng nh?n ???c v? 0
            damageTaken = 0f;
        }
        else
        {
            // T�nh to�n s�t th??ng gi?m
            float reducedDamage = damage * (1f - damageReduction);
            
            // Gi?m l??ng l� ch?n
            shieldHealth -= damage * damageReduction;
            
            // ??t s�t th??ng nh?n ???c
            damageTaken = reducedDamage;
        }
        
        // Hi?u ?ng khi b? ?�nh
        if (flashOnHit)
        {
            StartCoroutine(FlashShield());
        }
        
        // Ph�t �m thanh b? ?�nh
        if (audioSource != null && shieldHitSound != null)
        {
            audioSource.PlayOneShot(shieldHitSound);
        }
        
        // Hi?n th? hi?u ?ng b? ?�nh
        if (shieldHitEffectPrefab != null)
        {
            Instantiate(shieldHitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // C?p nh?t hi?u ?ng l� ch?n
        UpdateShieldVisual();
        
        // N?u l� ch?n ?� h?t
        if (shieldHealth <= 0f)
        {
            // V? l� ch?n
            BreakShield();
        }
    }
    
    /// <summary>
    /// Ki?m tra xem c� th? k�ch ho?t l� ch?n kh�ng
    /// </summary>
    public bool CanActivateShield()
    {
        // N?u l� ch?n ?ang ho?t ??ng, kh�ng th? k�ch ho?t
        if (isShieldActive)
        {
            return false;
        }
        
        // N?u ch?a h?t cooldown, kh�ng th? k�ch ho?t
        if (Time.time < lastShieldActivateTime + shieldCooldown)
        {
            return false;
        }
        
        // N?u kh�ng th? k�ch ho?t nhi?u l?n v� ?� k�ch ho?t tr??c ?�
        if (!canActivateMultipleTimes && activationCount > 0)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// K�ch ho?t l� ch?n
    /// </summary>
    public void ActivateShield()
    {
        if (!CanActivateShield())
        {
            return;
        }
        
        // C?p nh?t tr?ng th�i
        isShieldActive = true;
        lastShieldActivateTime = Time.time;
        shieldDeactivateTime = Time.time + shieldDuration;
        activationCount++;
        
        // ??t l?i l??ng l� ch?n
        shieldHealth = maxShieldHealth;
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("ShieldActive", true);
        }
        
        // T?o hi?u ?ng l� ch?n
        CreateShieldVisual();
        
        // Ph�t �m thanh k�ch ho?t
        if (audioSource != null && shieldActivateSound != null)
        {
            audioSource.PlayOneShot(shieldActivateSound);
        }
        
        // Hi?n th? hi?u ?ng k�ch ho?t
        if (shieldActivateEffectPrefab != null)
        {
            Instantiate(shieldActivateEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // B?t ??u t�i t?o l� ch?n
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        regenCoroutine = StartCoroutine(RegenerateShield());
        
        // T? ??ng t?t l� ch?n sau m?t kho?ng th?i gian
        StartCoroutine(DeactivateShieldAfterDuration());
    }
    
    /// <summary>
    /// T?t l� ch?n
    /// </summary>
    public void DeactivateShield()
    {
        // N?u l� ch?n kh�ng ho?t ??ng, kh�ng l�m g� c?
        if (!isShieldActive)
        {
            return;
        }
        
        // C?p nh?t tr?ng th�i
        isShieldActive = false;
        
        // K�ch ho?t animation n?u c�
        if (enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.SetBool("ShieldActive", false);
        }
        
        // H?y hi?u ?ng l� ch?n
        DestroyShieldVisual();
        
        // D?ng t�i t?o l� ch?n
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
    }
    
    /// <summary>
    /// V? l� ch?n
    /// </summary>
    private void BreakShield()
    {
        // Ph�t �m thanh v? l� ch?n
        if (audioSource != null && shieldBreakSound != null)
        {
            audioSource.PlayOneShot(shieldBreakSound);
        }
        
        // Hi?n th? hi?u ?ng v? l� ch?n
        if (shieldBreakEffectPrefab != null)
        {
            Instantiate(shieldBreakEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // T?t l� ch?n
        DeactivateShield();
    }
    
    /// <summary>
    /// T?o hi?u ?ng l� ch?n
    /// </summary>
    private void CreateShieldVisual()
    {
        // N?u ?� c� hi?u ?ng, h?y
        DestroyShieldVisual();
        
        // T?o hi?u ?ng m?i
        if (shieldVisualPrefab != null)
        {
            shieldVisual = Instantiate(shieldVisualPrefab, transform.position, Quaternion.identity);
            shieldVisual.transform.SetParent(transform);
            shieldVisual.transform.localPosition = Vector3.zero;
            
            // L?y renderer
            shieldRenderer = shieldVisual.GetComponent<SpriteRenderer>();
            if (shieldRenderer != null)
            {
                shieldRenderer.color = shieldColor;
            }
            
            // B?t ??u hi?u ?ng pulse n?u ???c b?t
            if (pulseWhenActive)
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = StartCoroutine(PulseShield());
            }
        }
    }
    
    /// <summary>
    /// H?y hi?u ?ng l� ch?n
    /// </summary>
    private void DestroyShieldVisual()
    {
        // D?ng hi?u ?ng pulse
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // H?y ??i t??ng l� ch?n
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
            shieldVisual = null;
            shieldRenderer = null;
        }
    }
    
    /// <summary>
    /// C?p nh?t hi?u ?ng l� ch?n
    /// </summary>
    private void UpdateShieldVisual()
    {
        // N?u kh�ng c� renderer, kh�ng l�m g� c?
        if (shieldRenderer == null)
        {
            return;
        }
        
        // T�nh to�n ?? trong su?t d?a tr�n l??ng l� ch?n c�n l?i
        float alpha = 0.2f + 0.8f * (shieldHealth / maxShieldHealth);
        
        // C?p nh?t m�u l� ch?n
        Color color = shieldColor;
        color.a = alpha;
        shieldRenderer.color = color;
    }
    
    /// <summary>
    /// Hi?u ?ng pulse cho l� ch?n
    /// </summary>
    private IEnumerator PulseShield()
    {
        if (shieldVisual == null)
        {
            yield break;
        }
        
        Vector3 originalScale = shieldVisual.transform.localScale;
        float time = 0f;
        
        while (shieldVisual != null)
        {
            // T�nh to�n k�ch th??c pulse
            float pulse = 1f + Mathf.Sin(time * pulseRate) * pulseAmount;
            
            // �p d?ng k�ch th??c
            shieldVisual.transform.localScale = originalScale * pulse;
            
            // T?ng th?i gian
            time += Time.deltaTime;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Hi?u ?ng nh?p nh�y khi b? ?�nh
    /// </summary>
    private IEnumerator FlashShield()
    {
        // N?u kh�ng c� renderer, kh�ng l�m g� c?
        if (shieldRenderer == null)
        {
            yield break;
        }
        
        // L?u m�u g?c
        Color originalColor = shieldRenderer.color;
        
        // Thay ??i m�u
        shieldRenderer.color = hitColor;
        
        // ??i m?t kho?ng th?i gian
        yield return new WaitForSeconds(flashDuration);
        
        // Tr? v? m�u g?c
        shieldRenderer.color = originalColor;
    }
    
    /// <summary>
    /// T�i t?o l� ch?n
    /// </summary>
    private IEnumerator RegenerateShield()
    {
        while (isShieldActive)
        {
            // N?u v?a b? ?�nh, ??i m?t kho?ng th?i gian
            if (Time.time < lastShieldHitTime + shieldRegenDelay)
            {
                yield return null;
                continue;
            }
            
            // N?u l� ch?n ?� ??y, kh�ng l�m g� c?
            if (shieldHealth >= maxShieldHealth)
            {
                yield return null;
                continue;
            }
            
            // T?ng l??ng l� ch?n
            shieldHealth = Mathf.Min(shieldHealth + shieldRegenRate * Time.deltaTime, maxShieldHealth);
            
            // C?p nh?t hi?u ?ng l� ch?n
            UpdateShieldVisual();
            
            yield return null;
        }
    }
    
    /// <summary>
    /// T? ??ng t?t l� ch?n sau m?t kho?ng th?i gian
    /// </summary>
    private IEnumerator DeactivateShieldAfterDuration()
    {
        // ??i ??n th?i ?i?m t?t
        yield return new WaitForSeconds(shieldDuration);
        
        // N?u l� ch?n v?n c�n ho?t ??ng, t?t
        if (isShieldActive)
        {
            DeactivateShield();
        }
    }
    
    /// <summary>
    /// L?y tr?ng th�i l� ch?n
    /// </summary>
    public bool IsShieldActive()
    {
        return isShieldActive;
    }
    
    /// <summary>
    /// L?y l??ng l� ch?n hi?n t?i
    /// </summary>
    public float GetShieldHealth()
    {
        return shieldHealth;
    }
    
    /// <summary>
    /// L?y ph?n tr?m l� ch?n
    /// </summary>
    public float GetShieldPercent()
    {
        return shieldHealth / maxShieldHealth;
    }
    
    /// <summary>
    /// V? Gizmos ?? debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // V? ph?m vi l� ch?n
        Gizmos.color = shieldColor;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}