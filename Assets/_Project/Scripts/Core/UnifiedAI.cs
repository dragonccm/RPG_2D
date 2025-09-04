using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unified AI system to replace multiple enemy controllers
/// Consolidates EnemyAI, EnemyController, and behavior trees
/// </summary>
public class UnifiedAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private AIState currentState = AIState.Idle;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private bool enableDebugLogging = false;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool loopPatrol = true;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attackDamage = 10;

    private Transform target;
    private UnifiedMovement movement;
    private UnifiedCombat combat;
    private UnifiedAnimator animator;
    private int currentPatrolIndex = 0;
    private float lastAttackTime;
    private float patrolWaitTimer;
    private int currentHealth;
    private bool isAlive = true;

    private void Awake()
    {
        InitializeAI();
    }

    private void InitializeAI()
    {
        movement = GetComponent<UnifiedMovement>();
        combat = GetComponent<UnifiedCombat>();
        animator = GetComponent<UnifiedAnimator>();

        currentHealth = maxHealth;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // Create default patrol points
            CreateDefaultPatrolPoints();
        }
    }

    private void CreateDefaultPatrolPoints()
    {
        patrolPoints = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject point = new GameObject(PerformanceUtils.FormatString("PatrolPoint_{0}", i));
            point.transform.position = transform.position + new Vector3(
                Mathf.Cos(i * 90f * Mathf.Deg2Rad) * 3f,
                0f,
                Mathf.Sin(i * 90f * Mathf.Deg2Rad) * 3f
            );
            point.transform.parent = transform.parent;
            patrolPoints[i] = point.transform;
        }
    }

    private void Update()
    {
        if (!isAlive) return;

        UpdateAI();
    }

    private void UpdateAI()
    {
        // Find target if none
        if (target == null)
        {
            FindTarget();
        }

        // Update AI state
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;
            case AIState.Patrol:
                UpdatePatrolState();
                break;
            case AIState.Chase:
                UpdateChaseState();
                break;
            case AIState.Attack:
                UpdateAttackState();
                break;
            case AIState.Flee:
                UpdateFleeState();
                break;
            case AIState.Dead:
                UpdateDeadState();
                break;
        }

        // Update animator
        if (animator != null)
        {
            bool isMoving = movement != null && movement.IsMoving();
            animator.SetMovement(isMoving, isMoving ? movement.GetVelocity().magnitude : 0f);
        }
    }

    private void UpdateIdleState()
    {
        // Look for targets
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= detectionRange)
            {
                ChangeState(AIState.Chase);
            }
        }
        else
        {
            // Start patrolling after a delay
            if (Random.value < 0.01f) // 1% chance per frame
            {
                ChangeState(AIState.Patrol);
            }
        }
    }

    private void UpdatePatrolState()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            ChangeState(AIState.Idle);
            return;
        }

        // Move to current patrol point
        Vector3 targetPos = patrolPoints[currentPatrolIndex].position;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance > 0.5f)
        {
            if (movement != null)
            {
                movement.MoveToPosition(targetPos);
            }
        }
        else
        {
            // Reached patrol point
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                // Move to next patrol point
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                if (!loopPatrol && currentPatrolIndex == 0)
                {
                    ChangeState(AIState.Idle);
                    return;
                }
                patrolWaitTimer = 0f;
            }
        }

        // Check for targets while patrolling
        if (target != null)
        {
            float targetDistance = Vector3.Distance(transform.position, target.position);
            if (targetDistance <= detectionRange)
            {
                ChangeState(AIState.Chase);
            }
        }
    }

    private void UpdateChaseState()
    {
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > detectionRange * 1.5f)
        {
            // Target out of range, return to patrol
            target = null;
            ChangeState(AIState.Patrol);
            return;
        }

        if (distance <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }

        // Chase target
        if (movement != null)
        {
            movement.FollowTarget(target);
        }
    }

    private void UpdateAttackState()
    {
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > attackRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }

        // Attack if cooldown is ready
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }

        // Face target
        FaceTarget();
    }

    private void UpdateFleeState()
    {
        if (target == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        // Move away from target
        Vector3 fleeDirection = (transform.position - target.position).normalized;
        if (movement != null)
        {
            movement.Move(fleeDirection);
        }

        // Stop fleeing if far enough
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > detectionRange * 2f)
        {
            target = null;
            ChangeState(AIState.Idle);
        }
    }

    private void UpdateDeadState()
    {
        // Handle death animation and cleanup
        if (animator != null)
        {
            animator.TriggerDeath();
        }
    }

    private void PerformAttack()
    {
        if (combat != null)
        {
            // Determine attack direction based on target position
            Vector3 direction = (target.position - transform.position).normalized;
            AttackDirection attackDir = GetAttackDirection(direction);

            combat.PerformAttack(attackDir);
        }
        else
        {
            // Fallback attack
            if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("👹 Enemy attacked for {0} damage", attackDamage));
        }
    }

    private AttackDirection GetAttackDirection(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (angle >= -45f && angle < 45f)
            return AttackDirection.Right;
        else if (angle >= 45f && angle < 135f)
            return AttackDirection.Up;
        else if (angle >= 135f || angle < -135f)
            return AttackDirection.Left;
        else
            return AttackDirection.Down;
    }

    private void FaceTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        if (animator != null)
        {
            animator.SetFacingDirection(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }

    private void FindTarget()
    {
        // Find player or other targets
        var player = ServiceLocator.GetService<PlayerController>();
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= detectionRange)
            {
                target = player.transform;
            }
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        AIState oldState = currentState;
        currentState = newState;

        // Exit old state
        switch (oldState)
        {
            case AIState.Patrol:
                patrolWaitTimer = 0f;
                if (movement != null)
                {
                    movement.StopFollowing();
                }
                break;
            case AIState.Chase:
                if (movement != null)
                {
                    movement.StopFollowing();
                }
                break;
        }

        // Enter new state
        switch (newState)
        {
            case AIState.Patrol:
                currentPatrolIndex = 0;
                patrolWaitTimer = 0f;
                break;
            case AIState.Dead:
                isAlive = false;
                if (movement != null)
                {
                    movement.StopFollowing();
                }
                break;
        }

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🤖 AI state changed: {0} → {1}", oldState, newState));
        }
    }

    /// <summary>
    /// Take damage and handle AI response
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (!isAlive) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("👹 Enemy took {0} damage, health: {1}/{2}", damage, currentHealth, maxHealth));
        }

        // Trigger hurt animation
        if (animator != null)
        {
            animator.TriggerHurt();
        }

        // Set target to attacker if not already set
        if (target == null)
        {
            FindTarget();
        }

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Chance to flee if low health
            if (currentHealth < maxHealth * 0.3f && Random.value < 0.5f)
            {
                ChangeState(AIState.Flee);
            }
        }
    }

    private void Die()
    {
        ChangeState(AIState.Dead);

        // Drop loot
        DropLoot();

        // Trigger death event
        GameEvents.OnEnemyDeath?.Invoke(gameObject);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("💀 Enemy died");
        }
    }

    private void DropLoot()
    {
        // Chance to drop items
        if (Random.value < 0.3f) // 30% chance
        {
            var inventory = ServiceLocator.GetService<UnifiedInventory>();
            if (inventory != null)
            {
                // Drop random item
                string[] possibleDrops = { "potion_health", "herb_healing", "gem_ruby" };
                string dropItem = possibleDrops[Random.Range(0, possibleDrops.Length)];
                inventory.AddItem(dropItem, 1);

                if (enableDebugLogging)
                {
                    PerformanceUtils.Log(PerformanceUtils.FormatString("💰 Enemy dropped: {0}", dropItem));
                }
            }
        }

        // Always drop gold
        var inventoryService = ServiceLocator.GetService<UnifiedInventory>();
        if (inventoryService != null)
        {
            int goldAmount = Random.Range(5, 15);
            inventoryService.AddGold(goldAmount);

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("💰 Enemy dropped {0} gold", goldAmount));
            }
        }
    }

    /// <summary>
    /// Set patrol points
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
    }

    /// <summary>
    /// Set detection range
    /// </summary>
    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }

    /// <summary>
    /// Set attack range
    /// </summary>
    public void SetAttackRange(float range)
    {
        attackRange = range;
    }

    /// <summary>
    /// Get current health
    /// </summary>
    public int GetHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Get max health
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Check if alive
    /// </summary>
    public bool IsAlive()
    {
        return isAlive;
    }

    /// <summary>
    /// Get current AI state
    /// </summary>
    public AIState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Force state change
    /// </summary>
    public void ForceStateChange(AIState newState)
    {
        ChangeState(newState);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Visualize patrol points
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (loopPatrol && patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}

/// <summary>
/// AI state enumeration
/// </summary>
public enum AIState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee,
    Dead
}
