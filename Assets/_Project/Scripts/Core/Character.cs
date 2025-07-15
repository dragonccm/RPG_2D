using System.Collections;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Health & Mana")]
    public Resource health;
    public Resource mana;
    
    [Header("Combat Effects")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private bool enableHitStop = true;
    
    public bool isStunned { get; private set; }
    public bool isBeingKnockedBack { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine currentKnockbackCoroutine;
    private Coroutine currentFlashCoroutine;

    public System.Action<float> OnDamageTaken;
    public System.Action OnDeath;

    protected virtual void Awake()
    {
        health = gameObject.AddComponent<Resource>();
        mana = gameObject.AddComponent<Resource>();
        health.Initialize(100f, 0f);
        mana.Initialize(50f, 5f);
        
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isStunned) return;
        
        health.Decrease(damage);
        TriggerDamageFlash();
        
        if (showDamageNumbers && CombatEffectsManager.Instance != null)
        {
            Vector3 damagePosition = transform.position + Vector3.up * 1.5f;
            CombatEffectsManager.Instance.ShowDamageNumber(damage, damagePosition, isCritical);
        }
        
        if (enableScreenShake && damage > 10f && CombatEffectsManager.Instance != null)
        {
            float shakeIntensity = Mathf.Clamp(damage * 0.01f, 0.05f, 0.3f);
            CombatEffectsManager.Instance.ScreenShake(shakeIntensity, 0.1f);
        }
        
        if (enableHitStop && isCritical && CombatEffectsManager.Instance != null)
        {
            CombatEffectsManager.Instance.HitStop(0.1f);
        }
        
        if (CombatEffectsManager.Instance != null)
        {
            CombatEffectsManager.Instance.CreateImpactEffect(
                transform.position, 
                isCritical ? Color.yellow : Color.red, 
                isCritical ? 1.5f : 1f
            );
        }
        
        OnDamageTaken?.Invoke(damage);
        
        if (health.currentValue <= 0)
        {
            Die();
        }
    }

    public void TakeDamageWithKnockback(float damage, float knockbackForce, Vector2 knockbackDirection, bool isCritical = false)
    {
        TakeDamage(damage, isCritical);
        
        if (health.currentValue > 0)
        {
            ApplyKnockback(knockbackForce, knockbackDirection);
        }
    }

    public void ApplyKnockback(float force, Vector2 direction)
    {
        if (rb == null || isStunned) return;
        
        float actualForce = force / knockbackResistance;
        
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        
        currentKnockbackCoroutine = StartCoroutine(KnockbackCoroutine(actualForce, direction.normalized));
    }

    private IEnumerator KnockbackCoroutine(float force, Vector2 direction)
    {
        isBeingKnockedBack = true;
        
        Vector2 knockbackVelocity = direction * force;
        rb.linearVelocity = knockbackVelocity;
        
        float knockbackDuration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < knockbackDuration)
        {
            elapsedTime += Time.fixedDeltaTime;
            float progress = elapsedTime / knockbackDuration;
            
            float currentForce = force * Mathf.Exp(-progress * 5f);
            rb.linearVelocity = direction * currentForce;
            
            yield return new WaitForFixedUpdate();
        }
        
        rb.linearVelocity = Vector2.zero;
        isBeingKnockedBack = false;
        currentKnockbackCoroutine = null;
    }

    private void TriggerDamageFlash()
    {
        if (spriteRenderer == null) return;
        
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        currentFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = originalColor;
        currentFlashCoroutine = null;
    }

    public void ApplyStun(float duration)
    {
        if (!isStunned)
        {
            StartCoroutine(StunCoroutine(duration));
        }
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    public void SetKnockbackResistance(float resistance)
    {
        knockbackResistance = Mathf.Max(0.1f, resistance);
    }

    public void SetDamageFlashSettings(float duration, Color flashColor)
    {
        damageFlashDuration = duration;
        damageFlashColor = flashColor;
    }

    public void SetCombatEffectsEnabled(bool damageNumbers, bool screenShake, bool hitStop)
    {
        showDamageNumbers = damageNumbers;
        enableScreenShake = screenShake;
        enableHitStop = hitStop;
    }

    protected virtual void Die()
    {
        Debug.Log($"{name} đã chết!");
        
        OnDeath?.Invoke();
        
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        if (CombatEffectsManager.Instance != null)
        {
            CombatEffectsManager.Instance.CreateImpactEffect(
                transform.position, 
                Color.black, 
                2f
            );
        }
        
        Destroy(gameObject);
    }

    public bool CanMove()
    {
        return !isStunned && !isBeingKnockedBack;
    }

    public void Heal(float amount)
    {
        health.Increase(amount);
        
        if (showDamageNumbers && CombatEffectsManager.Instance != null)
        {
            Vector3 healPosition = transform.position + Vector3.up * 1.5f;
            CombatEffectsManager.Instance.ShowDamageNumber(amount, healPosition, false);
        }
        
        if (CombatEffectsManager.Instance != null)
        {
            CombatEffectsManager.Instance.CreateImpactEffect(
                transform.position, 
                Color.green, 
                1f
            );
        }
    }

    public void RestoreMana(float amount)
    {
        if (mana != null)
        {
            mana.Increase(amount);
        }
    }

    private void OnDestroy()
    {
        if (currentKnockbackCoroutine != null)
        {
            StopCoroutine(currentKnockbackCoroutine);
        }
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
    }
}