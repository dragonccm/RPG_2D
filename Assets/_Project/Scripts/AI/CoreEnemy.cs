using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using RPG.Animation;

/// <summary>
/// Script c? s? cho t?t c? k? ??ch - ch?a thu?c t�nh v� ch?c n?ng chung
/// M?i k? ??ch ??u c?n script n�y ?? ho?t ??ng
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class CoreEnemy : MonoBehaviour, IDamageable
{
    #region === CORE PROPERTIES ===
    
    [Header("?? Enemy Identity")]
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private string enemyID = "";
    
    [Header("?? Health & Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float defense = 0f;
    [SerializeField] private float healthRegenRate = 0f;
    
    [Header("?? Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float angularSpeed = 360f;
    [SerializeField] private float rotationSpeed = 180f;
    
    [Header("??? Detection & Targeting")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float loseTargetRange = 20f;
    [SerializeField] private LayerMask playerLayerMask = 1 << 7;
    [SerializeField] private LayerMask obstacleLayerMask = 1;
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private float targetUpdateInterval = 0.2f;
    
    [Header("?? Animation & Visual")]
    [SerializeField] private bool flipOnMove = true;
    [SerializeField] private float flipSpeed = 8f;
    [SerializeField] private bool useRootMotion = false;
    
    [Header("?? Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private AudioClip[] alertSounds;
    
    [Header("?? Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLogs = false;
    
    #endregion
    
    #region === COMPONENTS & REFERENCES ===
    
    // Core Components
    private NavMeshAgent navAgent;
    private Animator animator;
    private Rigidbody2D rb2D;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider2D;
    private Collider enemyCollider3D;
    
    // Current State
    private Transform currentTarget;
    private bool isDead = false;
    private bool isStunned = false;
    private bool facingRight = true;
    
    // Timers
    private float nextTargetUpdateTime = 0f;
    private float lastHealthRegenTime = 0f;
    
    // Movement tracking
    private Vector3 lastPosition;
    private Vector2 moveDirection;
    private float currentMoveSpeed;
    
    // Cache variables cho animation optimization
    private float lastMoveSpeed = 0f;
    private bool wasMoving = false;
    private float lastMoveX = 0f;
    private float lastMoveY = 0f;
    
    #endregion
    
    #region === PROPERTIES ===
    
    public string EnemyName => enemyName;
    public int EnemyLevel => enemyLevel;
    public string EnemyID => enemyID;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float Defense => defense;
    public float MoveSpeed => moveSpeed;
    public float DetectionRange => detectionRange;
    public float ChaseRange => chaseRange;
    public bool IsDead => isDead;
    public bool IsStunned => isStunned;

    /// <summary>
    /// Transform of this damageable object
    /// </summary>
    public UnityEngine.Transform Transform => transform;
    public Transform CurrentTarget => currentTarget;
    public NavMeshAgent NavAgent => navAgent;
    public Animator EnemyAnimator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public bool FacingRight => facingRight;
    public Vector2 MoveDirection => moveDirection;
    public float CurrentMoveSpeed => currentMoveSpeed;
    public LayerMask PlayerLayerMask => playerLayerMask;
    
    #endregion
    
    #region === EVENTS ===
    
    public event System.Action<CoreEnemy, Transform> OnTargetFound;
    public event System.Action<CoreEnemy> OnTargetLost;
    public event System.Action<CoreEnemy> OnEnemyDeath;
    public event System.Action<CoreEnemy, float, float> OnHealthChanged;
    
    #endregion
    
    #region === INITIALIZATION ===
    
    private void Awake()
    {
        InitializeComponents();
        InitializeProperties();
    }
    
    private void Start()
    {
        SetupNavMeshAgent();
        SetupAnimator();
        ValidateSetup();
    }
    
    private void InitializeComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb2D = GetComponent<Rigidbody2D>();
        
        // Get colliders - check both 2D and 3D
        enemyCollider2D = GetComponent<Collider2D>();
        enemyCollider3D = GetComponent<Collider>();
    }
    
    private void InitializeProperties()
    {
        currentHealth = maxHealth;
        lastPosition = transform.position;
        
        if (string.IsNullOrEmpty(enemyID))
        {
            enemyID = System.Guid.NewGuid().ToString();
        }
        
        // CRITICAL: Ensure proper Enemy tag for skill targeting
        if (!gameObject.CompareTag("Enemy"))
        {
            Debug.LogWarning($"[{gameObject.name}] CoreEnemy should have 'Enemy' tag for proper skill targeting!");
            gameObject.tag = "Enemy";
        }
    }
    
    private void SetupNavMeshAgent()
    {
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.acceleration = acceleration;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.angularSpeed = angularSpeed;
            navAgent.updateRotation = true;
            navAgent.updatePosition = true;
        }
    }
    
    private void SetupAnimator()
    {
        if (animator != null)
        {
            animator.applyRootMotion = useRootMotion;
        }
    }
    
    private void ValidateSetup()
    {
        if (navAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent component is missing!");
        }
        
        if (animator == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Animator component is missing!");
        }
    }
    
    #endregion
    
    #region === UPDATE SYSTEM ===
    
    private void Update()
    {
        if (isDead) return;
        
        UpdateTargetDetection();
        UpdateMovementAndAnimation();
        UpdateHealthRegeneration();
        UpdateFacingDirection();
    }
    
    private void UpdateTargetDetection()
    {
        if (Time.time >= nextTargetUpdateTime)
        {
            Transform newTarget = FindBestTarget();
            
            if (newTarget != currentTarget)
            {
                Transform oldTarget = currentTarget;
                currentTarget = newTarget;
                
                if (newTarget != null)
                {
                    OnTargetFound?.Invoke(this, newTarget);
                }
                else if (oldTarget != null)
                {
                    OnTargetLost?.Invoke(this);
                }
            }
            
            nextTargetUpdateTime = Time.time + targetUpdateInterval;
        }
    }
    
    private void UpdateMovementAndAnimation()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movementDelta = currentPosition - lastPosition;
        
        moveDirection = new Vector2(movementDelta.x, movementDelta.z);
        currentMoveSpeed = movementDelta.magnitude / Time.deltaTime;
        
        // ENHANCED: Update animation through EnemyAnimatorController
        var animatorController = GetComponent<EnemyAnimatorController>();
        if (animatorController != null)
        {
            animatorController.PlayMoveAnimation(currentMoveSpeed);
        }
        
        // Tối ưu: Chỉ cập nhật animator parameters khi có thay đổi
        if (animator != null)
        {
            // Cache current values để tránh cập nhật liên tục
            if (Mathf.Abs(currentMoveSpeed - lastMoveSpeed) > 0.01f || 
                wasMoving != (currentMoveSpeed > 0.1f))
            {
                animator.SetFloat(AnimationParameters.MoveSpeed, currentMoveSpeed);
                animator.SetBool(AnimationParameters.IsMoving, currentMoveSpeed > 0.1f);
                lastMoveSpeed = currentMoveSpeed;
                wasMoving = currentMoveSpeed > 0.1f;
            }
            
            // Chỉ cập nhật MoveX/MoveY khi thay đổi đáng kể
            if (Mathf.Abs(moveDirection.x - lastMoveX) > 0.01f || 
                Mathf.Abs(moveDirection.y - lastMoveY) > 0.01f)
            {
                animator.SetFloat(AnimationParameters.MoveX, moveDirection.x);
                animator.SetFloat(AnimationParameters.MoveY, moveDirection.y);
                lastMoveX = moveDirection.x;
                lastMoveY = moveDirection.y;
            }
        }
        
        lastPosition = currentPosition;
    }
    
    private void UpdateHealthRegeneration()
    {
        if (healthRegenRate > 0f && currentHealth < maxHealth && Time.time >= lastHealthRegenTime + 1f)
        {
            Heal(healthRegenRate);
            lastHealthRegenTime = Time.time;
        }
    }
    
    private void UpdateFacingDirection()
    {
        if (!flipOnMove || moveDirection.magnitude < 0.1f) return;
        
        bool shouldFaceRight = moveDirection.x > 0;
        
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            FlipSprite();
        }
    }
    
    #endregion
    
    #region === TARGET DETECTION ===
    
    private Transform FindBestTarget()
    {
        // Find all potential targets in detection range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);
        
        Transform bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (var hit in hitColliders)
        {
            if (hit.transform == transform) continue;
            
            if (IsValidTarget(hit.transform))
            {
                float score = CalculateTargetScore(hit.transform);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = hit.transform;
                }
            }
        }
        
        return bestTarget;
    }
    
    private bool IsValidTarget(Transform target)
    {
        if (target == null) return false;
        
        // Check if target is player
        if (!target.CompareTag("Player")) return false;
        
        // Check line of sight if enabled
        if (useLineOfSight && !HasLineOfSight(target))
        {
            return false;
        }
        
        return true;
    }
    
    private bool HasLineOfSight(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget, out hit, distanceToTarget, obstacleLayerMask))
        {
            return hit.transform == target;
        }
        
        return true;
    }
    
    private float CalculateTargetScore(Transform target)
    {
        float score = 0f;
        
        // Distance factor (closer is better)
        float distance = Vector3.Distance(transform.position, target.position);
        score += 100f / (1f + distance);
        
        // Health factor (weaker targets are prioritized)
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // This would need to be implemented based on your IDamageable interface
            // score += (1f - healthPercentage) * 50f;
        }
        
        return score;
    }
    
    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }
    
    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, currentTarget.position);
    }
    
    public Vector3 GetDirectionToTarget()
    {
        if (currentTarget == null) return Vector3.zero;
        return (currentTarget.position - transform.position).normalized;
    }
    
    #endregion
    
    #region === MOVEMENT CONTROL ===
    
    public void MoveTo(Vector3 destination)
    {
        if (navAgent != null && navAgent.isActiveAndEnabled && !isDead)
        {
            navAgent.SetDestination(destination);
        }
    }
    
    public void StopMovement()
    {
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.ResetPath();
        }
    }
    
    public bool HasReachedDestination()
    {
        if (navAgent == null) return true;
        
        return !navAgent.pathPending && navAgent.remainingDistance < navAgent.stoppingDistance;
    }
    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (navAgent != null)
        {
            navAgent.speed = newSpeed;
        }
    }
    
    #endregion
    
    #region === ANIMATION CONTROL ===
    
    private void FlipSprite()
    {
        if (spriteRenderer != null)
        {
            // Flip sprite
            Vector3 scale = transform.localScale;
            scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else
        {
            // Flip transform
            Vector3 rotation = transform.eulerAngles;
            rotation.y = facingRight ? 0f : 180f;
            transform.eulerAngles = rotation;
        }
    }
    
    public void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }
    
    public void SetAnimationBool(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }
    
    public void SetAnimationFloat(string paramName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }
    
    #endregion
    
    #region === HEALTH SYSTEM ===
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        float actualDamage = Mathf.Max(0f, damage - defense);
        currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
        
        Debug.Log($"?? COREENEMY DAMAGE: {gameObject.name} took {actualDamage} damage ({oldHealth} -> {currentHealth})");
        
        OnHealthChanged?.Invoke(this, oldHealth, currentHealth);
        
        // ENHANCED: Trigger hurt animation through EnemyAnimatorController
        var animatorController = GetComponent<EnemyAnimatorController>();
        if (animatorController != null)
        {
            animatorController.PlayHurtAnimation();
        }
        
        // Play hurt sound
        PlayHurtSound();
        
        // Trigger hurt animation (fallback for legacy system)
        TriggerAnimation("Hurt");
        
        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        OnHealthChanged?.Invoke(this, oldHealth, currentHealth);
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        float healthPercentage = currentHealth / maxHealth;
        maxHealth = newMaxHealth;
        currentHealth = maxHealth * healthPercentage;
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // ENHANCED: Trigger death animation through EnemyAnimatorController
        var animatorController = GetComponent<EnemyAnimatorController>();
        if (animatorController != null)
        {
            animatorController.PlayDeathAnimation();
        }
        
        // Stop movement
        StopMovement();
        
        // Disable colliders
        if (enemyCollider2D != null) enemyCollider2D.enabled = false;
        if (enemyCollider3D != null) enemyCollider3D.enabled = false;
        
        // Play death sound
        PlayDeathSound();
        
        // Trigger death animation (fallback for legacy system)
        TriggerAnimation("Death");
        
        // Notify listeners
        OnEnemyDeath?.Invoke(this);
        
        // Destroy after delay
        StartCoroutine(DestroyAfterDelay(3f));
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    #endregion
    
    #region === STATUS EFFECTS ===
    
    public void ApplyStun(float duration)
    {
        if (!isDead)
        {
            StartCoroutine(StunCoroutine(duration));
        }
    }
    
    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        StopMovement();
        
        // Visual effects for stun could be added here
        
        yield return new WaitForSeconds(duration);
        
        isStunned = false;
    }
    
    #endregion
    
    #region === AUDIO SYSTEM ===
    
    private void PlayHurtSound()
    {
        if (audioSource != null && hurtSounds != null && hurtSounds.Length > 0)
        {
            AudioClip clip = hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void PlayDeathSound()
    {
        if (audioSource != null && deathSounds != null && deathSounds.Length > 0)
        {
            AudioClip clip = deathSounds[UnityEngine.Random.Range(0, deathSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void PlayAlertSound()
    {
        if (audioSource != null && alertSounds != null && alertSounds.Length > 0)
        {
            AudioClip clip = alertSounds[UnityEngine.Random.Range(0, alertSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
    }
    
    #endregion
    
    #region === GIZMOS ===
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Chase range
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Lose target range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
        
        // Line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
        
        // NavMesh path
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] pathCorners = navAgent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }
    }
    
    #endregion
}