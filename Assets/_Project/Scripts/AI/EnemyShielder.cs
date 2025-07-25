using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// X? l� kh? n?ng t?o l� ch?n b?o v? cho ??ng minh c?a k? th� h? tr?
/// </summary>
public class EnemyShielder : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float shieldAmount = 20f;
    [SerializeField] private float shieldDuration = 10f;
    [SerializeField] private float shieldCooldown = 15f;
    [SerializeField] private float shieldRadius = 5f;
    [SerializeField] private int maxTargetsPerShield = 2;
    [SerializeField] private bool canShieldSelf = true;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Shield Effects")]
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.5f, 1f, 0.5f);
    [SerializeField] private float shieldPulseRate = 1f;
    [SerializeField] private float shieldPulseAmount = 0.2f;
    [SerializeField] private AudioClip shieldCreateSound;
    [SerializeField] private AudioClip shieldBreakSound;
    [SerializeField] private float shieldVolume = 0.5f;
    
    [Header("Advanced Settings")]
    [SerializeField] private bool regenerateShield = false;
    [SerializeField] private float regenerateAmount = 5f;
    [SerializeField] private float regenerateInterval = 3f;
    [SerializeField] private bool shieldAbsorbsAllDamage = false;
    [SerializeField] private bool prioritizeLowHealthAllies = true;
    [SerializeField] private bool shieldStacksWithExisting = false;
    
    // C�c component
    // private Animator animator; // Removed direct Animator reference
    private AudioSource audioSource;
    
    // Th?i gian t? l?n t?o l� ch?n cu?i
    private float lastShieldTime = -999f;
    
    // Danh s�ch k? th� ?ang ???c b?o v?
    private Dictionary<Enemy, ShieldInfo> activeShields = new Dictionary<Enemy, ShieldInfo>();
    
    // Th�ng tin v? l� ch?n
    private class ShieldInfo
    {
        public float amount;
        public float maxAmount;
        public float endTime;
        public GameObject shieldObject;
        public Coroutine regenerateCoroutine;
        
        public ShieldInfo(float amount, float duration, GameObject shieldObject)
        {
            this.amount = amount;
            this.maxAmount = amount;
            this.endTime = Time.time + duration;
            this.shieldObject = shieldObject;
            this.regenerateCoroutine = null;
        }
    }
    
    private float damageTaken = 0f;

    private void Awake()
    {
        // L?y c�c component c?n thi?t
        // animator = GetComponent<Animator>(); // Removed direct Animator reference
        audioSource = GetComponent<AudioSource>();
        
        // T?o AudioSource n?u ch?a c�
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = shieldVolume;
        }
        
        // B?t ??u ki?m tra v� x�a l� ch?n h?t h?n
        StartCoroutine(CheckExpiredShields());
    }
    
    /// <summary>
    /// Ki?m tra xem c� th? t?o l� ch?n kh�ng
    /// </summary>
    public bool CanShield()
    {
        return Time.time >= lastShieldTime + shieldCooldown;
    }
    
    /// <summary>
    /// T�m ??ng minh ?? t?o l� ch?n
    /// </summary>
    public List<Enemy> FindAlliesToShield()
    {
        List<Enemy> allies = new List<Enemy>();
        
        // T�m t?t c? k? th� trong ph?m vi
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, shieldRadius, enemyLayer);
        
        foreach (Collider2D collider in colliders)
        {
            // L?y component Enemy
            Enemy enemy = collider.GetComponent<Enemy>();
            
            // Ki?m tra xem c� ph?i l� Enemy kh�ng
            if (enemy != null)
            {
                // N?u l� b?n th�n v� kh�ng th? t? t?o l� ch?n, b? qua
                if (enemy.gameObject == gameObject && !canShieldSelf)
                {
                    continue;
                }
                
                // N?u ?� c� l� ch?n v� kh�ng cho ph�p stack, b? qua
                if (activeShields.ContainsKey(enemy) && !shieldStacksWithExisting)
                {
                    continue;
                }
                
                allies.Add(enemy);
            }
        }
        
        // S?p x?p theo m?c m�u n?u c?n ?u ti�n k? th� c� m�u th?p
        if (prioritizeLowHealthAllies)
        {
            allies.Sort((a, b) => a.GetHealthPercent().CompareTo(b.GetHealthPercent()));
        }
        
        return allies;
    }
    
    /// <summary>
    /// T?o l� ch?n cho ??ng minh
    /// </summary>
    public void ShieldAllies()
    {
        if (!CanShield())
        {
            return;
        }
        
        // C?p nh?t th?i gian t?o l� ch?n cu?i
        lastShieldTime = Time.time;
        
        // T�m ??ng minh ?? t?o l� ch?n
        List<Enemy> alliesToShield = FindAlliesToShield();
        
        // N?u kh�ng c� ??ng minh n�o, kh�ng l�m g� c?
        if (alliesToShield.Count == 0)
        {
            return;
        }
        
        // K�ch ho?t animation t?o l� ch?n n?u c�
        // if (animator != null)
        // {
        //     animator.SetTrigger("Shield");
        // } // Animation handled by EnemyAnimatorController
        
        // Ph�t �m thanh t?o l� ch?n
        if (audioSource != null && shieldCreateSound != null)
        {
            audioSource.PlayOneShot(shieldCreateSound);
        }
        
        // Gi?i h?n s? l??ng m?c ti�u
        int targetsToShield = Mathf.Min(alliesToShield.Count, maxTargetsPerShield);
        
        // T?o l� ch?n cho t?ng ??ng minh
        for (int i = 0; i < targetsToShield; i++)
        {
            Enemy ally = alliesToShield[i];
            
            // T?o l� ch?n
            CreateShield(ally);
        }
    }
    
    /// <summary>
    /// T?o l� ch?n cho ??ng minh
    /// </summary>
    private void CreateShield(Enemy target)
    {
        // T?o ??i t??ng l� ch?n
        GameObject shieldObject = null;
        if (shieldPrefab != null)
        {
            shieldObject = Instantiate(shieldPrefab, target.transform.position, Quaternion.identity);
            shieldObject.transform.SetParent(target.transform);
            
            // ?i?u ch?nh k�ch th??c l� ch?n
            shieldObject.transform.localScale = Vector3.one * 1.2f;
            
            // ?i?u ch?nh m�u l� ch?n
            SpriteRenderer renderer = shieldObject.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = shieldColor;
                
                // B?t ??u hi?u ?ng pulse
                StartCoroutine(PulseShield(renderer));
            }
        }
        
        // N?u ?� c� l� ch?n v� cho ph�p stack
        if (activeShields.ContainsKey(target) && shieldStacksWithExisting)
        {
            // T?ng l??ng l� ch?n
            activeShields[target].amount += shieldAmount;
            activeShields[target].maxAmount += shieldAmount;
            
            // L�m m?i th?i gian
            activeShields[target].endTime = Time.time + shieldDuration;
            
            // H?y ??i t??ng l� ch?n m?i n?u ?� t?o
            if (shieldObject != null)
            {
                Destroy(shieldObject);
            }
        }
        else
        {
            // T?o th�ng tin l� ch?n m?i
            ShieldInfo shieldInfo = new ShieldInfo(shieldAmount, shieldDuration, shieldObject);
            
            // N?u ?� c� l� ch?n c?, h?y
            if (activeShields.ContainsKey(target))
            {
                RemoveShield(target);
            }
            
            // Th�m l� ch?n m?i
            activeShields.Add(target, shieldInfo);
            
            // ??ng k� s? ki?n nh?n s�t th??ng
            target.OnDamageTaken += HandleDamageTaken;
            
            // B?t ??u t�i t?o l� ch?n n?u ???c b?t
            if (regenerateShield)
            {
                shieldInfo.regenerateCoroutine = StartCoroutine(RegenerateShield(target));
            }
        }
    }
    
    /// <summary>
    /// X? l� s? ki?n nh?n s�t th??ng
    /// </summary>
    private void HandleDamageTaken(Enemy enemy, float damage, float newHealth)
    {
        // N?u kh�ng c� l� ch?n, kh�ng l�m g� c?
        if (!activeShields.ContainsKey(enemy))
        {
            return;
        }
        
        ShieldInfo shieldInfo = activeShields[enemy];
        
        // N?u l� ch?n h?p th? t?t c? s�t th??ng
        if (shieldAbsorbsAllDamage)
        {
            // Gi?m l??ng l� ch?n
            shieldInfo.amount -= damage;
            
            // ??t s�t th??ng nh?n ???c v? 0
            damageTaken = 0f;
        }
        else
        {
            // N?u s�t th??ng nh? h?n ho?c b?ng l??ng l� ch?n
            if (damage <= shieldInfo.amount)
            {
                // Gi?m l??ng l� ch?n
                shieldInfo.amount -= damage;
                
                // ??t s�t th??ng nh?n ???c v? 0
                damageTaken = 0f;
            }
            else
            {
                // N?u s�t th??ng l?n h?n l??ng l� ch?n
                // T�nh to�n s�t th??ng c�n l?i
                damageTaken = damage - shieldInfo.amount;
                
                // ??t l??ng l� ch?n v? 0
                shieldInfo.amount = 0f;
            }
        }
        
        // C?p nh?t hi?u ?ng l� ch?n
        UpdateShieldVisual(enemy);
        
        // N?u l� ch?n ?� h?t
        if (shieldInfo.amount <= 0f)
        {
            // Ph�t �m thanh v? l� ch?n
            if (audioSource != null && shieldBreakSound != null)
            {
                audioSource.PlayOneShot(shieldBreakSound);
            }
            
            // X�a l� ch?n
            RemoveShield(enemy);
        }
    }
    
    /// <summary>
    /// C?p nh?t hi?u ?ng l� ch?n
    /// </summary>
    private void UpdateShieldVisual(Enemy target)
    {
        // N?u kh�ng c� l� ch?n, kh�ng l�m g� c?
        if (!activeShields.ContainsKey(target))
        {
            return;
        }
        
        ShieldInfo shieldInfo = activeShields[target];
        
        // N?u kh�ng c� ??i t??ng l� ch?n, kh�ng l�m g� c?
        if (shieldInfo.shieldObject == null)
        {
            return;
        }
        
        // L?y renderer c?a l� ch?n
        SpriteRenderer renderer = shieldInfo.shieldObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // T�nh to�n ?? trong su?t d?a tr�n l??ng l� ch?n c�n l?i
            float alpha = 0.2f + 0.8f * (shieldInfo.amount / shieldInfo.maxAmount);
            
            // C?p nh?t m�u l� ch?n
            Color color = shieldColor;
            color.a = alpha;
            renderer.color = color;
        }
    }
    
    /// <summary>
    /// T�i t?o l� ch?n
    /// </summary>
    private IEnumerator RegenerateShield(Enemy enemy)
    {
        while (activeShields.ContainsKey(enemy))
        {
            // ??i m?t kho?ng th?i gian
            yield return new WaitForSeconds(regenerateInterval);
            
            // N?u kh�ng c�n l� ch?n ho?c target kh�ng c�n t?n t?i, d?ng l?i
            if (!activeShields.ContainsKey(enemy) || enemy == null)
            {
                yield break;
            }
            
            ShieldInfo shieldInfo = activeShields[enemy];
            
            // N?u l� ch?n ?� ??y, kh�ng l�m g� c?
            if (shieldInfo.amount >= shieldInfo.maxAmount)
            {
                continue;
            }
            
            // T?ng l??ng l� ch?n
            shieldInfo.amount = Mathf.Min(shieldInfo.amount + regenerateAmount, shieldInfo.maxAmount);
            
            // C?p nh?t hi?u ?ng l� ch?n
            UpdateShieldVisual(enemy);
        }
    }
    
    /// <summary>
    /// Hi?u ?ng pulse cho l� ch?n
    /// </summary>
    private IEnumerator PulseShield(SpriteRenderer renderer)
    {
        float time = 0f;
        
        while (renderer != null)
        {
            // T�nh to�n k�ch th??c pulse
            float pulse = 1f + Mathf.Sin(time * shieldPulseRate) * shieldPulseAmount;
            
            // �p d?ng k�ch th??c
            if (renderer.transform != null)
            {
                renderer.transform.localScale = Vector3.one * pulse;
            }
            
            // T?ng th?i gian
            time += Time.deltaTime;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// X�a l� ch?n
    /// </summary>
    private void RemoveShield(Enemy target)
    {
        // N?u kh�ng c� l� ch?n, kh�ng l�m g� c?
        if (!activeShields.ContainsKey(target))
        {
            return;
        }
        
        ShieldInfo shieldInfo = activeShields[target];
        
        // H?y ??i t??ng l� ch?n
        if (shieldInfo.shieldObject != null)
        {
            Destroy(shieldInfo.shieldObject);
        }
        
        // H?y coroutine t�i t?o l� ch?n
        if (shieldInfo.regenerateCoroutine != null)
        {
            StopCoroutine(shieldInfo.regenerateCoroutine);
        }
        
        // H?y ??ng k� s? ki?n nh?n s�t th??ng
        target.OnDamageTaken -= HandleDamageTaken;
        
        // X�a kh?i danh s�ch
        activeShields.Remove(target);
    }
    
    /// <summary>
    /// Ki?m tra v� x�a l� ch?n h?t h?n
    /// </summary>
    private IEnumerator CheckExpiredShields()
    {
        while (true)
        {
            // Danh s�ch k? th� c?n x�a
            List<Enemy> enemiesToRemove = new List<Enemy>();
            
            // Ki?m tra t?ng k? th�
            foreach (KeyValuePair<Enemy, ShieldInfo> pair in activeShields)
            {
                Enemy enemy = pair.Key;
                ShieldInfo shieldInfo = pair.Value;
                
                // N?u k? th� kh�ng c�n t?n t?i ho?c l� ch?n h?t h?n
                if (enemy == null || Time.time >= shieldInfo.endTime)
                {
                    enemiesToRemove.Add(enemy);
                }
            }
            
            // X�a c�c l� ch?n h?t h?n
            foreach (Enemy enemy in enemiesToRemove)
            {
                RemoveShield(enemy);
            }
            
            // ??i m?t kho?ng th?i gian tr??c khi ki?m tra l?i
            yield return new WaitForSeconds(1f);
        }
    }
    
    /// <summary>
    /// L?y ph?m vi t?o l� ch?n
    /// </summary>
    public float GetShieldRadius()
    {
        return shieldRadius;
    }
    
    /// <summary>
    /// V? Gizmos ?? debug
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // V? ph?m vi t?o l� ch?n
        Gizmos.color = shieldColor;
        Gizmos.DrawWireSphere(transform.position, shieldRadius);
    }
}