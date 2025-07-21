using UnityEngine;
using System.Collections;

/// <summary>
/// X? lý kh? n?ng phòng th? v?i lá ch?n c?a k? thù
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
    
    // Các component
    private Enemy enemy;
    private Animator animator;
    private AudioSource audioSource;
    
    // Tr?ng thái lá ch?n
    private bool isShieldActive = false;
    private float lastShieldActivateTime = -999f;
    private float lastShieldHitTime = -999f;
    private float shieldDeactivateTime = 0f;
    private int activationCount = 0;
    private float damageTaken = 0f;
    
    // ??i t??ng lá ch?n
    private GameObject shieldVisual;
    private SpriteRenderer shieldRenderer;
    
    // Coroutine
    private Coroutine pulseCoroutine;
    private Coroutine regenCoroutine;
    
    private void Awake()
    {
        // L?y các component c?n thi?t
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // T?o AudioSource n?u ch?a có
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = soundVolume;
        }
        
        // ??ng ký s? ki?n nh?n sát th??ng
        if (enemy != null)
        {
            enemy.OnDamageTaken += HandleDamageTaken;
            enemy.OnHealthChanged += HandleHealthChanged;
        }
    }
    
    private void OnDestroy()
    {
        // H?y ??ng ký s? ki?n
        if (enemy != null)
        {
            enemy.OnDamageTaken -= HandleDamageTaken;
            enemy.OnHealthChanged -= HandleHealthChanged;
        }
    }
    
    /// <summary>
    /// X? lý s? ki?n thay ??i máu
    /// </summary>
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // N?u ?ã kích ho?t t? ??ng và máu gi?m xu?ng d??i ng??ng
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
    /// X? lý s? ki?n nh?n sát th??ng
    /// </summary>
    private void HandleDamageTaken(Enemy enemy, float damage, float newHealth)
    {
        // N?u lá ch?n không ho?t ??ng, không làm gì c?
        if (!isShieldActive)
        {
            return;
        }
        
        // C?p nh?t th?i gian b? ?ánh cu?i
        lastShieldHitTime = Time.time;
        
        // N?u ch?n t?t c? sát th??ng
        if (blockAllDamageWhenActive)
        {
            // Gi?m l??ng lá ch?n
            shieldHealth -= damage;
            
            // ??t sát th??ng nh?n ???c v? 0
            damageTaken = 0f;
        }
        else
        {
            // Tính toán sát th??ng gi?m
            float reducedDamage = damage * (1f - damageReduction);
            
            // Gi?m l??ng lá ch?n
            shieldHealth -= damage * damageReduction;
            
            // ??t sát th??ng nh?n ???c
            damageTaken = reducedDamage;
        }
        
        // Hi?u ?ng khi b? ?ánh
        if (flashOnHit)
        {
            StartCoroutine(FlashShield());
        }
        
        // Phát âm thanh b? ?ánh
        if (audioSource != null && shieldHitSound != null)
        {
            audioSource.PlayOneShot(shieldHitSound);
        }
        
        // Hi?n th? hi?u ?ng b? ?ánh
        if (shieldHitEffectPrefab != null)
        {
            Instantiate(shieldHitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // C?p nh?t hi?u ?ng lá ch?n
        UpdateShieldVisual();
        
        // N?u lá ch?n ?ã h?t
        if (shieldHealth <= 0f)
        {
            // V? lá ch?n
            BreakShield();
        }
    }
    
    /// <summary>
    /// Ki?m tra xem có th? kích ho?t lá ch?n không
    /// </summary>
    public bool CanActivateShield()
    {
        // N?u lá ch?n ?ang ho?t ??ng, không th? kích ho?t
        if (isShieldActive)
        {
            return false;
        }
        
        // N?u ch?a h?t cooldown, không th? kích ho?t
        if (Time.time < lastShieldActivateTime + shieldCooldown)
        {
            return false;
        }
        
        // N?u không th? kích ho?t nhi?u l?n và ?ã kích ho?t tr??c ?ó
        if (!canActivateMultipleTimes && activationCount > 0)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Kích ho?t lá ch?n
    /// </summary>
    public void ActivateShield()
    {
        if (!CanActivateShield())
        {
            return;
        }
        
        // C?p nh?t tr?ng thái
        isShieldActive = true;
        lastShieldActivateTime = Time.time;
        shieldDeactivateTime = Time.time + shieldDuration;
        activationCount++;
        
        // ??t l?i l??ng lá ch?n
        shieldHealth = maxShieldHealth;
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("ShieldActive", true);
        }
        
        // T?o hi?u ?ng lá ch?n
        CreateShieldVisual();
        
        // Phát âm thanh kích ho?t
        if (audioSource != null && shieldActivateSound != null)
        {
            audioSource.PlayOneShot(shieldActivateSound);
        }
        
        // Hi?n th? hi?u ?ng kích ho?t
        if (shieldActivateEffectPrefab != null)
        {
            Instantiate(shieldActivateEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // B?t ??u tái t?o lá ch?n
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        regenCoroutine = StartCoroutine(RegenerateShield());
        
        // T? ??ng t?t lá ch?n sau m?t kho?ng th?i gian
        StartCoroutine(DeactivateShieldAfterDuration());
    }
    
    /// <summary>
    /// T?t lá ch?n
    /// </summary>
    public void DeactivateShield()
    {
        // N?u lá ch?n không ho?t ??ng, không làm gì c?
        if (!isShieldActive)
        {
            return;
        }
        
        // C?p nh?t tr?ng thái
        isShieldActive = false;
        
        // Kích ho?t animation n?u có
        if (animator != null)
        {
            animator.SetBool("ShieldActive", false);
        }
        
        // H?y hi?u ?ng lá ch?n
        DestroyShieldVisual();
        
        // D?ng tái t?o lá ch?n
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
    }
    
    /// <summary>
    /// V? lá ch?n
    /// </summary>
    private void BreakShield()
    {
        // Phát âm thanh v? lá ch?n
        if (audioSource != null && shieldBreakSound != null)
        {
            audioSource.PlayOneShot(shieldBreakSound);
        }
        
        // Hi?n th? hi?u ?ng v? lá ch?n
        if (shieldBreakEffectPrefab != null)
        {
            Instantiate(shieldBreakEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // T?t lá ch?n
        DeactivateShield();
    }
    
    /// <summary>
    /// T?o hi?u ?ng lá ch?n
    /// </summary>
    private void CreateShieldVisual()
    {
        // N?u ?ã có hi?u ?ng, h?y
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
    /// H?y hi?u ?ng lá ch?n
    /// </summary>
    private void DestroyShieldVisual()
    {
        // D?ng hi?u ?ng pulse
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // H?y ??i t??ng lá ch?n
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
            shieldVisual = null;
            shieldRenderer = null;
        }
    }
    
    /// <summary>
    /// C?p nh?t hi?u ?ng lá ch?n
    /// </summary>
    private void UpdateShieldVisual()
    {
        // N?u không có renderer, không làm gì c?
        if (shieldRenderer == null)
        {
            return;
        }
        
        // Tính toán ?? trong su?t d?a trên l??ng lá ch?n còn l?i
        float alpha = 0.2f + 0.8f * (shieldHealth / maxShieldHealth);
        
        // C?p nh?t màu lá ch?n
        Color color = shieldColor;
        color.a = alpha;
        shieldRenderer.color = color;
    }
    
    /// <summary>
    /// Hi?u ?ng pulse cho lá ch?n
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
            // Tính toán kích th??c pulse
            float pulse = 1f + Mathf.Sin(time * pulseRate) * pulseAmount;
            
            // Áp d?ng kích th??c
            shieldVisual.transform.localScale = originalScale * pulse;
            
            // T?ng th?i gian
            time += Time.deltaTime;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Hi?u ?ng nh?p nháy khi b? ?ánh
    /// </summary>
    private IEnumerator FlashShield()
    {
        // N?u không có renderer, không làm gì c?
        if (shieldRenderer == null)
        {
            yield break;
        }
        
        // L?u màu g?c
        Color originalColor = shieldRenderer.color;
        
        // Thay ??i màu
        shieldRenderer.color = hitColor;
        
        // ??i m?t kho?ng th?i gian
        yield return new WaitForSeconds(flashDuration);
        
        // Tr? v? màu g?c
        shieldRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Tái t?o lá ch?n
    /// </summary>
    private IEnumerator RegenerateShield()
    {
        while (isShieldActive)
        {
            // N?u v?a b? ?ánh, ??i m?t kho?ng th?i gian
            if (Time.time < lastShieldHitTime + shieldRegenDelay)
            {
                yield return null;
                continue;
            }
            
            // N?u lá ch?n ?ã ??y, không làm gì c?
            if (shieldHealth >= maxShieldHealth)
            {
                yield return null;
                continue;
            }
            
            // T?ng l??ng lá ch?n
            shieldHealth = Mathf.Min(shieldHealth + shieldRegenRate * Time.deltaTime, maxShieldHealth);
            
            // C?p nh?t hi?u ?ng lá ch?n
            UpdateShieldVisual();
            
            yield return null;
        }
    }
    
    /// <summary>
    /// T? ??ng t?t lá ch?n sau m?t kho?ng th?i gian
    /// </summary>
    private IEnumerator DeactivateShieldAfterDuration()
    {
        // ??i ??n th?i ?i?m t?t
        yield return new WaitForSeconds(shieldDuration);
        
        // N?u lá ch?n v?n còn ho?t ??ng, t?t
        if (isShieldActive)
        {
            DeactivateShield();
        }
    }
    
    /// <summary>
    /// L?y tr?ng thái lá ch?n
    /// </summary>
    public bool IsShieldActive()
    {
        return isShieldActive;
    }
    
    /// <summary>
    /// L?y l??ng lá ch?n hi?n t?i
    /// </summary>
    public float GetShieldHealth()
    {
        return shieldHealth;
    }
    
    /// <summary>
    /// L?y ph?n tr?m lá ch?n
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
        // V? ph?m vi lá ch?n
        Gizmos.color = shieldColor;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}