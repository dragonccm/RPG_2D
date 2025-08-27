using UnityEngine;
using System.Collections;
using RPGGame.AI;

/// <summary>
/// Script thi?t l?p lo?i hình k? ??ch và hành vi AI t??ng ?ng
/// G?n vào k? ??ch ?? xác ??nh lo?i và behavior patterns
/// </summary>
[RequireComponent(typeof(CoreEnemy))]
[DisallowMultipleComponent]
public class EnemyTypeController : MonoBehaviour
{
    #region === ENEMY TYPE CONFIGURATION ===
    
    [Header("?? Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Melee;
    [SerializeField] private EnemyDifficulty difficulty = EnemyDifficulty.Normal;
    [SerializeField] private EnemyBehavior behavior = EnemyBehavior.Normal;
    
    [Header("?? Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private bool canCriticalHit = false;
    [SerializeField] private float criticalChance = 0.1f;
    [SerializeField] private float criticalMultiplier = 2f;
    
    [Header("?? AI Behavior")]
    [SerializeField] private AIPersonality personality = AIPersonality.Balanced;
    [SerializeField] private float aggressionLevel = 0.5f;
    [SerializeField] private float fearThreshold = 0.2f; // Health percentage to start retreating
    [SerializeField] private bool canCallForHelp = false;
    [SerializeField] private float helpCallRange = 10f;
    
    [Header("?? Movement Behavior")]
    [SerializeField] private MovementPattern movementPattern = MovementPattern.Direct;
    [SerializeField] private float retreatDistance = 5f;
    [SerializeField] private float optimalRange = 3f; // Preferred combat distance
    [SerializeField] private bool canJump = false;
    [SerializeField] private bool canFly = false;
    
    [Header("? Special Abilities")]
    [SerializeField] private bool hasSpecialAttack = false;
    [SerializeField] private float specialAttackCooldown = 10f;
    [SerializeField] private bool canSummon = false;
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private int maxSummons = 3;
    
    [Header("?? Difficulty Modifiers")]
    [SerializeField] private float healthMultiplier = 1f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float experienceReward = 100f;
    
    #endregion
    
    #region === COMPONENTS & STATE ===
    
    private CoreEnemy coreEnemy;
    private EnemyAIState currentState = EnemyAIState.Idle;
    private float lastAttackTime = 0f;
    private float lastSpecialAttackTime = 0f;
    private int currentSummons = 0;
    private bool isRetreating = false;
    private bool hasCalledForHelp = false;
    
    // AI State Machine
    private float stateTimer = 0f;
    private float nextStateEvaluationTime = 0f;
    private const float STATE_EVALUATION_INTERVAL = 0.1f;
    
    #endregion
    
    #region === PROPERTIES ===
    
    public EnemyType EnemyType => enemyType;
    public EnemyDifficulty Difficulty => difficulty;
    public EnemyBehavior Behavior => behavior;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public float BaseDamage => baseDamage;
    public EnemyAIState CurrentState => currentState;
    public bool IsRetreating => isRetreating;
    public float AggressionLevel => aggressionLevel;
    public float DamageMultiplier => damageMultiplier;
    
    #endregion
    
    #region === INITIALIZATION ===
    
    private void Awake()
    {
        coreEnemy = GetComponent<CoreEnemy>();
        ApplyTypeModifiers();
        ApplyDifficultyModifiers();
    }
    
    private void Start()
    {
        InitializeTypeSpecificBehavior();
    }
    
    private void ApplyTypeModifiers()
    {
        switch (enemyType)
        {
            case EnemyType.Melee:
                attackRange = 2f;
                attackCooldown = 1.2f;
                optimalRange = 1.5f;
                aggressionLevel = 0.8f;
                break;
            case EnemyType.Ranged:
                attackRange = 8f;
                attackCooldown = 2f;
                optimalRange = 6f;
                aggressionLevel = 0.6f;
                break;
            case EnemyType.Support:
                attackRange = 3f;
                attackCooldown = 3f;
                optimalRange = 5f;
                aggressionLevel = 0.3f;
                canCallForHelp = true;
                fearThreshold = 0.5f;
                break;
            case EnemyType.Boss:
                attackRange = 4f;
                attackCooldown = 1f;
                hasSpecialAttack = true;
                specialAttackCooldown = 8f;
                canCriticalHit = true;
                criticalChance = 0.2f;
                aggressionLevel = 0.9f;
                canCallForHelp = true;
                break;
            case EnemyType.Flying:
                canFly = true;
                attackRange = 5f;
                aggressionLevel = 0.7f;
                break;
            case EnemyType.Summoner:
                canSummon = true;
                maxSummons = 3;
                attackRange = 6f;
                aggressionLevel = 0.4f;
                break;
        }
    }
    
    private void ApplyDifficultyModifiers()
    {
        switch (difficulty)
        {
            case EnemyDifficulty.Easy:
                healthMultiplier = 0.7f;
                damageMultiplier = 0.8f;
                speedMultiplier = 0.9f;
                experienceReward = 50f;
                break;
            case EnemyDifficulty.Normal:
                healthMultiplier = 1f;
                damageMultiplier = 1f;
                speedMultiplier = 1f;
                experienceReward = 100f;
                break;
            case EnemyDifficulty.Hard:
                healthMultiplier = 1.5f;
                damageMultiplier = 1.3f;
                speedMultiplier = 1.1f;
                experienceReward = 200f;
                break;
            case EnemyDifficulty.Extreme:
                healthMultiplier = 2f;
                damageMultiplier = 1.6f;
                speedMultiplier = 1.2f;
                experienceReward = 400f;
                break;
            case EnemyDifficulty.Elite:
                healthMultiplier = 2.5f;
                damageMultiplier = 2f;
                speedMultiplier = 1.3f;
                experienceReward = 600f;
                hasSpecialAttack = true;
                canCriticalHit = true;
                break;
            case EnemyDifficulty.Champion:
                healthMultiplier = 3f;
                damageMultiplier = 2.5f;
                speedMultiplier = 1.4f;
                experienceReward = 1000f;
                hasSpecialAttack = true;
                canCriticalHit = true;
                canSummon = true;
                break;
        }
        
        // Apply modifiers to CoreEnemy
        if (coreEnemy != null)
        {
            coreEnemy.SetMaxHealth(coreEnemy.MaxHealth * healthMultiplier);
            coreEnemy.SetMoveSpeed(coreEnemy.MoveSpeed * speedMultiplier);
        }
    }
    
    private void InitializeTypeSpecificBehavior()
    {
        // Initialize based on personality
        switch (personality)
        {
            case AIPersonality.Aggressive:
                aggressionLevel = Mathf.Min(aggressionLevel + 0.3f, 1f);
                fearThreshold = 0.1f;
                break;
            case AIPersonality.Defensive:
                aggressionLevel = Mathf.Max(aggressionLevel - 0.3f, 0f);
                fearThreshold = 0.4f;
                break;
            case AIPersonality.Cunning:
                canCallForHelp = true;
                break;
            case AIPersonality.Berserker:
                aggressionLevel = 1f;
                fearThreshold = 0f;
                attackCooldown *= 0.7f;
                break;
        }
    }
    
    #endregion
    
    #region === COMBAT SYSTEM ===
    
    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }
    
    public void PerformAttack()
    {
        if (!CanAttack() || coreEnemy.CurrentTarget == null) return;
        
        lastAttackTime = Time.time;
        float damage = baseDamage * damageMultiplier;
        
        coreEnemy.TriggerAnimation("Attack");
        
        IDamageable damageableTarget = coreEnemy.CurrentTarget.GetComponent<IDamageable>();
        if (damageableTarget != null)
        {
            bool isCritical = canCriticalHit && UnityEngine.Random.value <= criticalChance;
            if (isCritical)
            {
                damage *= criticalMultiplier;
            }
            
            damageableTarget.TakeDamage(damage);
        }
        
        if (hasSpecialAttack && CanUseSpecialAttack())
        {
            StartCoroutine(PerformSpecialAttackCoroutine());
        }
    }
    
    public bool CanUseSpecialAttack()
    {
        return hasSpecialAttack && Time.time >= lastSpecialAttackTime + specialAttackCooldown;
    }
    
    private IEnumerator PerformSpecialAttackCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        lastSpecialAttackTime = Time.time;
        
        switch (enemyType)
        {
            case EnemyType.Boss:
                PerformBossSpecialAttack();
                break;
            case EnemyType.Summoner:
                PerformSummon();
                break;
            default:
                PerformGenericSpecialAttack();
                break;
        }
    }
    
    private void PerformBossSpecialAttack()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRange * 2f);
        
        foreach (var target in targets)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null && target.CompareTag("Player"))
            {
                damageable.TakeDamage(baseDamage * 1.5f * damageMultiplier);
            }
        }
        
        coreEnemy.TriggerAnimation("SpecialAttack");
    }
    
    private void PerformSummon()
    {
        if (!canSummon || currentSummons >= maxSummons || summonPrefab == null) return;
        
        Vector3 summonPosition = transform.position + UnityEngine.Random.insideUnitSphere * 3f;
        summonPosition.y = transform.position.y;
        
        GameObject summon = Instantiate(summonPrefab, summonPosition, Quaternion.identity);
        currentSummons++;
    }
    
    private void PerformGenericSpecialAttack()
    {
        if (coreEnemy.CurrentTarget != null)
        {
            IDamageable damageableTarget = coreEnemy.CurrentTarget.GetComponent<IDamageable>();
            if (damageableTarget != null)
            {
                damageableTarget.TakeDamage(baseDamage * 2f * damageMultiplier);
            }
        }
    }
    
    #endregion
    
    #region === AI STATE SYSTEM ===
    
    private void Update()
    {
        if (coreEnemy == null || coreEnemy.IsDead) return;
        
        UpdateStateTimer();
        EvaluateStateTransitions();
        ExecuteCurrentState();
    }
    
    private void UpdateStateTimer()
    {
        stateTimer += Time.deltaTime;
    }
    
    private void EvaluateStateTransitions()
    {
        if (Time.time < nextStateEvaluationTime) return;
        
        EnemyAIState newState = DetermineOptimalState();
        if (newState != currentState)
        {
            ChangeState(newState);
        }
        
        nextStateEvaluationTime = Time.time + STATE_EVALUATION_INTERVAL;
    }
    
    private EnemyAIState DetermineOptimalState()
    {
        if (coreEnemy.IsDead) return EnemyAIState.Dead;
        if (coreEnemy.IsStunned) return EnemyAIState.Stunned;
        
        Transform target = coreEnemy.CurrentTarget;
        if (target == null) return EnemyAIState.Idle;
        
        float distanceToTarget = coreEnemy.GetDistanceToTarget();
        float healthPercentage = coreEnemy.CurrentHealth / coreEnemy.MaxHealth;
        
        if (healthPercentage <= fearThreshold && behavior != EnemyBehavior.Aggressive)
        {
            return EnemyAIState.Retreat;
        }
        
        if (distanceToTarget <= attackRange && CanAttack())
        {
            return EnemyAIState.Attack;
        }
        
        if (enemyType == EnemyType.Ranged && distanceToTarget < optimalRange)
        {
            return EnemyAIState.Reposition;
        }
        
        if (distanceToTarget <= coreEnemy.ChaseRange)
        {
            return EnemyAIState.Chase;
        }
        
        return EnemyAIState.Idle;
    }
    
    private void ChangeState(EnemyAIState newState)
    {
        ExitCurrentState();
        currentState = newState;
        stateTimer = 0f;
        EnterNewState();
    }
    
    private void ExitCurrentState()
    {
        switch (currentState)
        {
            case EnemyAIState.Attack:
                coreEnemy.SetAnimationBool("IsAttacking", false);
                break;
            case EnemyAIState.Retreat:
                isRetreating = false;
                break;
        }
    }
    
    private void EnterNewState()
    {
        switch (currentState)
        {
            case EnemyAIState.Attack:
                coreEnemy.StopMovement();
                coreEnemy.SetAnimationBool("IsAttacking", true);
                break;
            case EnemyAIState.Retreat:
                isRetreating = true;
                if (canCallForHelp && !hasCalledForHelp)
                {
                    CallForHelp();
                }
                break;
            case EnemyAIState.Dead:
                coreEnemy.StopMovement();
                break;
        }
    }
    
    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case EnemyAIState.Idle:
                ExecuteIdleState();
                break;
            case EnemyAIState.Chase:
                ExecuteChaseState();
                break;
            case EnemyAIState.Attack:
                ExecuteAttackState();
                break;
            case EnemyAIState.Retreat:
                ExecuteRetreatState();
                break;
            case EnemyAIState.Reposition:
                ExecuteRepositionState();
                break;
        }
    }
    
    private void ExecuteIdleState()
    {
        // Idle behavior
    }
    
    private void ExecuteChaseState()
    {
        if (coreEnemy.CurrentTarget != null)
        {
            Vector3 targetPosition = CalculateOptimalPosition();
            coreEnemy.MoveTo(targetPosition);
        }
    }
    
    private void ExecuteAttackState()
    {
        if (CanAttack() && coreEnemy.CurrentTarget != null)
        {
            PerformAttack();
        }
    }
    
    private void ExecuteRetreatState()
    {
        Vector3 retreatPosition = CalculateRetreatPosition();
        coreEnemy.MoveTo(retreatPosition);
    }
    
    private void ExecuteRepositionState()
    {
        Vector3 repositionTarget = CalculateRepositionPosition();
        coreEnemy.MoveTo(repositionTarget);
    }
    
    #endregion
    
    #region === MOVEMENT & POSITIONING ===
    
    private Vector3 CalculateOptimalPosition()
    {
        if (coreEnemy.CurrentTarget == null) return transform.position;
        
        Vector3 targetPos = coreEnemy.CurrentTarget.position;
        Vector3 directionToTarget = (targetPos - transform.position).normalized;
        
        switch (enemyType)
        {
            case EnemyType.Melee:
                return targetPos - directionToTarget * (attackRange * 0.8f);
            case EnemyType.Ranged:
                return targetPos - directionToTarget * optimalRange;
            default:
                return targetPos - directionToTarget * optimalRange;
        }
    }
    
    private Vector3 CalculateRetreatPosition()
    {
        if (coreEnemy.CurrentTarget == null) return transform.position;
        
        Vector3 directionAwayFromTarget = (transform.position - coreEnemy.CurrentTarget.position).normalized;
        return transform.position + directionAwayFromTarget * retreatDistance;
    }
    
    private Vector3 CalculateRepositionPosition()
    {
        if (coreEnemy.CurrentTarget == null) return transform.position;
        
        Vector3 targetPos = coreEnemy.CurrentTarget.position;
        Vector3 directionToTarget = (targetPos - transform.position).normalized;
        
        return targetPos - directionToTarget * optimalRange;
    }
    
    private void CallForHelp()
    {
        hasCalledForHelp = true;
        
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, helpCallRange);
        
        foreach (var collider in nearbyEnemies)
        {
            CoreEnemy enemy = collider.GetComponent<CoreEnemy>();
            if (enemy != null && enemy != coreEnemy && enemy.CurrentTarget == null)
            {
                enemy.SetTarget(coreEnemy.CurrentTarget);
            }
        }
    }
    
    #endregion
    
    #region === GIZMOS ===
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, optimalRange);
        
        if (canCallForHelp)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, helpCallRange);
        }
    }
    
    #endregion
}