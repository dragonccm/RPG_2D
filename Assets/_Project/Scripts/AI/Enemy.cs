using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using System;

// Explicitly use UnityEngine.Random for Unity-specific random operations
using Random = UnityEngine.Random;

/// <summary>
/// Lớp cơ sở cho tất cả kẻ địch trong game. Quản lý target, di chuyển, máu, sự kiện, và tích hợp với AI Controller.
/// Đảm bảo mọi kẻ địch đều chỉ cần gắn script này + AIController phù hợp để hoạt động.
/// </summary>
public class Enemy : MonoBehaviour
{
    public enum PatrolMode { Once, Loop, RandomAroundAnchor, PingPong }

    [Header("Cài đặt vùng phát hiện")]
    [Tooltip("Khoảng cách để địch phát hiện người chơi")]
    public float detectionRange = 10f;
    [Tooltip("Khoảng cách tối đa để tiếp tục đuổi người chơi")]
    public float chaseRange = 20f;
    [Header("Cài đặt tuần tra (chỉ thiết lập qua EnemyGroupManager)")]
    [HideInInspector]
    public PatrolMode patrolMode = PatrolMode.Loop;
    [HideInInspector]
    public List<Transform> patrolPoints = new List<Transform>();
    [HideInInspector]
    public Transform anchor;
    [HideInInspector]
    public float randomPatrolRadius = 5f;
    [Tooltip("Ngưỡng khoảng cách để coi là đã đến điểm đích (anchor hoặc patrol point)")]
    public float arriveThreshold = 1.2f;
    [Header("Cài đặt khác")]
    [Tooltip("Sát thương cơ bản của kẻ địch.")]
    public float baseDamage = 10f;
    [Tooltip("Layer của Player để kẻ địch có thể phát hiện.")]
    public LayerMask playerLayerMask = 1 << 7; // Layer của Player

    protected Transform target; // Mục tiêu hiện tại của kẻ địch (không SerializeField)
    [SerializeField] protected NavMeshAgent agent; // NavMeshAgent để điều khiển di chuyển

    // Cache để tối ưu performance
    protected static readonly List<Transform> tempPlayerList = new List<Transform>();
    protected static readonly List<GameObject> tempPlayerObjects = new List<GameObject>();
    private float nextTargetUpdateTime = 0f;
    private const float TARGET_UPDATE_INTERVAL = 0.2f; // Cập nhật mục tiêu mỗi 0.2s

    // Thêm throttling cho patrol/anchor logic
    private float nextPatrolUpdateTime = 0f;
    private const float PATROL_UPDATE_INTERVAL = 0.3f; // Cập nhật patrol mỗi 0.3s
    
    // Cache distance calculations
    private float cachedAnchorDistance = 0f;
    private float lastAnchorDistanceTime = 0f;
    private const float ANCHOR_DISTANCE_CACHE_DURATION = 0.2f;

    // Cache player reference để tối ưu hiệu suất
    private static Transform[] cachedPlayers;
    private static float lastPlayerCacheTime = 0f;
    private const float PLAYER_CACHE_DURATION = 1f; // Cache player trong 1 giây

    // Movement controller reference
    protected EnemyMovementController movementController;

    // --- Event & Property cho hệ thống combat, buff, elite, v.v. ---
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action<Enemy, float, float> OnDamageTaken;
    public event Action<GameObject, float> OnDealDamage;
    public event Action<int> OnPhaseChanged;

    private float _currentHealth = 100f;
    private float _maxHealth = 100f;
    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;
    private float _defenseMultiplier = 1f;
    private float _damageReduction = 0f;
    private bool _invulnerable = false;
    public int phaseCount { get; set; } = 1;

    private float bossRange = 15f;
    private float minDistance = 5f;

    public float CurrentHealth { get => _currentHealth; protected set => _currentHealth = value; }
    public float MaxHealth { get => _maxHealth; protected set => _maxHealth = value; }
    public float DamageMultiplier { get => _damageMultiplier; set => _damageMultiplier = value; }
    public float SpeedMultiplier { get => _speedMultiplier; set => _speedMultiplier = value; }
    public float AttackSpeedMultiplier { get => _attackSpeedMultiplier; set => _attackSpeedMultiplier = value; }
    public float DefenseMultiplier { get => _defenseMultiplier; set => _defenseMultiplier = value; }
    public bool Invulnerable { get => _invulnerable; set => _invulnerable = value; }
    public bool IsPlayer { get; set; }
    public bool IsDead { get; set; }

    public void SetInvulnerable(bool value) { _invulnerable = value; }
    public void SetDamageMultiplier(float value) { _damageMultiplier = value; }
    public void SetSpeedMultiplier(float value) { _speedMultiplier = value; }
    public void SetAttackSpeedMultiplier(float value) { _attackSpeedMultiplier = value; }
    public void SetDefenseMultiplier(float value) { _defenseMultiplier = value; }
    public void SetDamageReduction(float value) { _damageReduction = value; }

    public float GetPower() { return 100f * _damageMultiplier * _speedMultiplier; }
    public bool NeedsHealing() { return _currentHealth < _maxHealth; }
    public float GetHealthPercent() { return _maxHealth > 0 ? _currentHealth / _maxHealth : 0f; }
    public void Heal(float amount) { _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth); OnHealthChanged?.Invoke(_currentHealth, _maxHealth); }

    private int currentPatrolIndex = 0;
    private bool patrolForward = true;
    private Vector3 randomPatrolTarget;
    private bool hasRandomTarget = false;

    private enum PatrolState { None, ReturningToAnchor, Patrolling }
    private PatrolState patrolState = PatrolState.None;

    /// <summary>
    /// Khởi tạo agent, đăng ký với AIManager, chuẩn bị NavMeshAgent.
    /// </summary>
    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<EnemyMovementController>();
        
        if (agent != null)
        {
            agent.updateRotation = false; // Tắt cập nhật xoay tự động
            agent.updateUpAxis = false; // Tắt cập nhật trục Y tự động
        }

        // Đăng ký với EnemyAIManager nếu có
        if (EnemyAIManager.Instance != null)
        {
            var aiController = GetComponent<EnemyAIController>();
            if (aiController != null)
            {
                EnemyAIManager.Instance.AddAgent(aiController);
            }
        }

        EnsureMeleeEnemyAIActive();
    }

    /// <summary>
    /// Update: Cập nhật target định kỳ và xử lý di chuyển với throttling.
    /// </summary>
    protected virtual void Update()
    {
        // Cập nhật mục tiêu player theo khoảng thời gian
        if (Time.time >= nextTargetUpdateTime)
        {
            UpdateTarget();
            nextTargetUpdateTime = Time.time + TARGET_UPDATE_INTERVAL;
        }

        // Cập nhật patrol logic theo khoảng thời gian để tối ưu performance
        if (Time.time >= nextPatrolUpdateTime)
        {
            UpdatePatrolLogic();
            nextPatrolUpdateTime = Time.time + PATROL_UPDATE_INTERVAL;
        }

        // Xử lý movement cuối cùng
        HandleMovement();
    }

    /// <summary>
    /// Cập nhật logic patrol với throttling và optimization.
    /// </summary>
    private void UpdatePatrolLogic()
    {
        // Nếu không có anchor, không làm gì
        if (anchor == null) 
        {

            return;
        }

        // Kiểm tra player có trong chase range không
        bool hasValidPlayerTarget = HasValidPlayerInChaseRange();

        // Cache anchor distance để tránh tính toán lặp lại
        float distToAnchor = GetCachedAnchorDistance();



        // Logic state transitions
        if (hasValidPlayerTarget)
        {
            // Có player trong chase range → pause patrol, để AI states xử lý chase

            patrolState = PatrolState.None;
            return;
        }
        else
        {
            // Không có player hoặc player ra khỏi chase range → xử lý patrol logic
            switch (patrolState)
            {
                case PatrolState.None:
                    HandlePatrolStateNone(distToAnchor);
                    break;
                case PatrolState.ReturningToAnchor:
                    HandlePatrolStateReturning(distToAnchor);
                    break;
                case PatrolState.Patrolling:
                    HandlePatrolStatePatrolling();
                    break;
            }
        }
    }

    /// <summary>
    /// Kiểm tra có player hợp lệ trong chase range không.
    /// </summary>
    private bool HasValidPlayerInChaseRange()
    {
        if (target == null) return false;
        
        if (!IsValidTarget(target)) return false;
        
        if (!target.CompareTag("Player")) return false;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        return distanceToTarget <= chaseRange;
    }

    /// <summary>
    /// Lấy khoảng cách đến anchor với caching để tối ưu performance.
    /// </summary>
    private float GetCachedAnchorDistance()
    {
        if (Time.time - lastAnchorDistanceTime > ANCHOR_DISTANCE_CACHE_DURATION)
        {
            cachedAnchorDistance = Vector3.Distance(transform.position, anchor.position);
            lastAnchorDistanceTime = Time.time;
        }
        return cachedAnchorDistance;
    }

    /// <summary>
    /// Xử lý khi không có trạng thái patrol.
    /// Player đã ra khỏi chase range hoặc chưa có target, cần về anchor trước khi patrol.
    /// </summary>
    private void HandlePatrolStateNone(float distToAnchor)
    {
        // Luôn về anchor trước khi bắt đầu patrol
        if (distToAnchor > arriveThreshold)
        {
            target = anchor;
            hasRandomTarget = false;
            patrolState = PatrolState.ReturningToAnchor;
        }
        else
        {
            // Đã gần anchor, bắt đầu patrol cycle
            StartPatrolling();
        }
    }

    /// <summary>
    /// Xử lý khi đang trở về anchor.
    /// </summary>
    private void HandlePatrolStateReturning(float distToAnchor)
    {
        if (distToAnchor <= arriveThreshold)
        {
            StartPatrolling();
        }
        else
        {
            target = anchor;
            hasRandomTarget = false;
        }
    }

    /// <summary>
    /// Bắt đầu patrol dựa trên patrol mode.
    /// </summary>
    private void StartPatrolling()
    {
        Debug.Log($"[Enemy] {gameObject.name} StartPatrolling - mode: {patrolMode}, waypoints: {(patrolPoints?.Count ?? 0)}");
        
        if (patrolMode != PatrolMode.RandomAroundAnchor && patrolPoints != null && patrolPoints.Count > 0)
        {
            // Waypoint patrol - bắt đầu từ waypoint đầu tiên
            currentPatrolIndex = 0;
            patrolForward = true;
            target = patrolPoints[currentPatrolIndex]; // Set target ban đầu
            hasRandomTarget = false;
            patrolState = PatrolState.Patrolling;
            Debug.Log($"[Enemy] {gameObject.name} bắt đầu WAYPOINT patrol, target đầu tiên: {target.position}");
        }
        else if (patrolMode == PatrolMode.RandomAroundAnchor)
        {
            // Random patrol - sẽ generate random target trong HandleRandomPatrol()
            if (randomPatrolRadius <= 0f)
            {
                Debug.LogWarning($"[Enemy] {gameObject.name} randomPatrolRadius <= 0 ({randomPatrolRadius}), set to default 5f");
                randomPatrolRadius = 5f;
            }
            
            patrolState = PatrolState.Patrolling;
            target = null; // Clear target vì sẽ dùng randomPatrolTarget
            hasRandomTarget = false; // Reset để force generate new target
            Debug.Log($"[Enemy] {gameObject.name} bắt đầu RANDOM patrol quanh {anchor.position} với radius {randomPatrolRadius}");
        }
        else
        {
            Debug.LogWarning($"[Enemy] {gameObject.name} không thể start patrol - không có waypoints hoặc setup sai");
        }
    }

    /// <summary>
    /// Xử lý logic patrol khi đang trong trạng thái Patrolling.
    /// </summary>
    private void HandlePatrolStatePatrolling()
    {
        if (patrolMode == PatrolMode.RandomAroundAnchor)
        {
            HandleRandomPatrol();
        }
        else if (patrolPoints != null && patrolPoints.Count > 0)
        {
            HandleWaypointPatrol();
        }
        else
        {
            // Không có waypoints hợp lệ, chuyển về idle  
            Debug.LogWarning($"[Enemy] {gameObject.name} ở Patrolling state nhưng không có waypoints! PatrolPoints: {(patrolPoints == null ? "null" : patrolPoints.Count.ToString())}");
            patrolState = PatrolState.None;
            target = null;
        }
    }

    /// <summary>
    /// Xử lý patrol ngẫu nhiên quanh anchor.
    /// </summary>
    private void HandleRandomPatrol()
    {

        
        if (!hasRandomTarget || Vector3.Distance(transform.position, randomPatrolTarget) <= arriveThreshold)
        {
            Vector2 randCircle = Random.insideUnitCircle * randomPatrolRadius;
            randomPatrolTarget = anchor.position + new Vector3(randCircle.x, 0, randCircle.y);
            hasRandomTarget = true;
            Debug.Log($"[Enemy] {gameObject.name} tạo random target mới: {randomPatrolTarget} (radius: {randomPatrolRadius})");
        }

    }

    /// <summary>
    /// Xử lý patrol theo waypoints.
    /// </summary>
    private void HandleWaypointPatrol()
    {
        // Validate patrol index
        if (currentPatrolIndex < 0 || currentPatrolIndex >= patrolPoints.Count)
        {
            currentPatrolIndex = 0;
            patrolForward = true;
        }

        // Check if reached current waypoint
        if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) <= arriveThreshold)
        {
            AdvancePatrolIndex();
        }

        target = patrolPoints[currentPatrolIndex];
        hasRandomTarget = false;
    }

    /// <summary>
    /// Tiến tới waypoint tiếp theo dựa trên patrol mode.
    /// </summary>
    private void AdvancePatrolIndex()
    {
        switch (patrolMode)
        {
            case PatrolMode.Once:
                // Đi tuần qua một lần rồi dừng lại
                if (currentPatrolIndex < patrolPoints.Count - 1)
                {
                    currentPatrolIndex++;
                }
                // Khi đến waypoint cuối cùng, dừng lại (không reset về 0)
                break;
                
            case PatrolMode.PingPong:
                // Đi tuần qua rồi quay lại (ping-pong)
                if (patrolForward)
                {
                    if (currentPatrolIndex < patrolPoints.Count - 1)
                        currentPatrolIndex++;
                    else
                        patrolForward = false; // Đảo chiều khi đến cuối
                }
                else
                {
                    if (currentPatrolIndex > 0)
                        currentPatrolIndex--;
                    else
                        patrolForward = true; // Đảo chiều khi về đầu
                }
                break;
                
            case PatrolMode.Loop:
            default:
                // Đi tuần thành một vòng (loop)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                break;
        }
    }

    /// <summary>
    /// Xử lý movement cuối cùng thông qua MovementController.
    /// </summary>
    private void HandleMovement()
    {
        if (movementController == null) return;



        // Xử lý movement dựa trên patrol state và mode
        if (patrolState == PatrolState.Patrolling)
        {
            if (patrolMode == PatrolMode.RandomAroundAnchor && hasRandomTarget)
            {
                // Random patrol quanh anchor
                movementController.MoveTo(randomPatrolTarget);
            }
            else if (patrolMode != PatrolMode.RandomAroundAnchor && target != null)
            {
                // Waypoint patrol - di chuyển đến waypoint hiện tại
                movementController.MoveTo(target.position);
            }
            else
            {
                // Fallback: không có target hợp lệ trong patrol state
                patrolState = PatrolState.None;
            }
        }
        // Xử lý các trường hợp khác (return to anchor, chase player, etc.)
        else if (target != null)
        {
            movementController.MoveTo(target.position);
        }
    }

    /// <summary>
    /// Tìm tất cả player trong scene bằng nhiều phương pháp (tag, layer, component).
    /// </summary>
    private Transform[] FindAllPlayers()
    {
        // Sử dụng cache để tối ưu hiệu suất
        if (Time.time - lastPlayerCacheTime < PLAYER_CACHE_DURATION && cachedPlayers != null)
        {
            return cachedPlayers;
        }

        tempPlayerObjects.Clear();

        // Phương pháp 1: Tìm bằng tag "Player"
        var playersByTag = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in playersByTag)
        {
            if (player != null && !tempPlayerObjects.Contains(player))
            {
                tempPlayerObjects.Add(player);
            }
        }

        // Phương pháp 2: Tìm bằng layer mask (chỉ khi không tìm thấy bằng tag)
        if (tempPlayerObjects.Count == 0)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if ((playerLayerMask & (1 << layer)) != 0)
                {
                    var objectsInLayer = FindObjectsOfType<GameObject>().Where(obj => obj.layer == layer);
                    foreach (var obj in objectsInLayer)
                    {
                        if (obj != null && obj != gameObject && !tempPlayerObjects.Contains(obj))
                        {
                            tempPlayerObjects.Add(obj);
                        }
                    }
                }
            }
        }

        // Phương pháp 3: Tìm bằng component specific (ví dụ: PlayerController, CharacterController)
        if (tempPlayerObjects.Count == 0)
        {
            var playerControllers = FindObjectsOfType<MonoBehaviour>()
                .Where(mb => mb.GetType().Name.ToLower().Contains("player"))
                .Select(mb => mb.gameObject)
                .Distinct();

            foreach (var player in playerControllers)
            {
                if (player != null && !tempPlayerObjects.Contains(player))
                {
                    tempPlayerObjects.Add(player);
                }
            }
        }

        // Convert to Transform array và cache
        cachedPlayers = tempPlayerObjects.Where(p => p != null).Select(p => p.transform).ToArray();
        lastPlayerCacheTime = Time.time;

        return cachedPlayers;
    }

    /// <summary>
    /// Cập nhật target tốt nhất cho kẻ địch dựa trên khoảng cách, nhóm, v.v.
    /// </summary>
    protected virtual void UpdateTarget()
    {
        Transform currentBestTarget = null;
        float currentBestDistance = float.MaxValue;

        // BƯỚC 1: Ưu tiên target hiện tại nếu nó vẫn hợp lệ và trong tầm chaseRange
        if (target != null && IsValidTarget(target))
        {
            float distanceToCurrentTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToCurrentTarget <= chaseRange)
            {
                currentBestTarget = target;
                currentBestDistance = distanceToCurrentTarget;
            }
        }

        // BƯỚC 2: Tìm players bằng multiple methods
        tempPlayerList.Clear();
        var colliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);
        foreach (var col in colliders)
        {
            if (col != null && IsValidTarget(col.transform))
            {
                tempPlayerList.Add(col.transform);
            }
        }
        if (tempPlayerList.Count == 0)
        {
            var allPlayers = FindAllPlayers();
            foreach (var player in allPlayers)
            {
                if (player != null && IsValidTarget(player))
                {
                    float distance = Vector3.Distance(transform.position, player.position);
                    if (distance <= detectionRange)
                    {
                        tempPlayerList.Add(player);
                    }
                }
            }
        }

        Transform bestPlayerCandidate = EvaluatePlayerTargetCandidates(tempPlayerList);
        if (bestPlayerCandidate != null)
        {
            float distToBestCandidate = Vector3.Distance(transform.position, bestPlayerCandidate.position);
            if (currentBestTarget == null || distToBestCandidate < currentBestDistance)
            {
                currentBestTarget = bestPlayerCandidate;
                currentBestDistance = distToBestCandidate;
            }
        }

        // BƯỚC 4: (Bỏ qua group target, không còn group)
        // BƯỚC 5: Gán target cuối cùng (chỉ là player)
        if (currentBestTarget != null && Vector3.Distance(transform.position, currentBestTarget.position) <= chaseRange)
        {            
            target = currentBestTarget;
            Debug.Log($"[Enemy] {gameObject.name} đặt PLAYER target: {target.name}");
        }
        else if (target != null && IsValidTarget(target) && target.CompareTag("Player"))
        {
            // Chỉ reset target nếu target hiện tại là player (không phải waypoint/anchor)
            Debug.Log($"[Enemy] {gameObject.name} reset PLAYER target (player ra khỏi range)");
            target = null;
        }

        // BƯỚC 6: Cập nhật AI Controller
        var aiController = GetComponent<EnemyAIController>();
        UpdateAIController(aiController);
    }

    /// <summary>
    /// Kiểm tra một transform có phải là target hợp lệ không (tag, layer, active, máu...)
    /// </summary>
    protected bool IsValidTarget(Transform t)
    {
        if (t == null || t == transform) return false;

        // Kiểm tra tag
        if (!t.CompareTag("Player")) return false;

        // Kiểm tra layer (nếu được chỉ định)
        if (playerLayerMask != 0 && (playerLayerMask & (1 << t.gameObject.layer)) == 0)
        {
            return false;
        }

        // Kiểm tra active state
        if (!t.gameObject.activeInHierarchy) return false;

        // Kiểm tra health (nếu có component Character)
        var character = t.GetComponent<Character>();
        if (character != null && character.CurrentHealth <= 0) return false;

        return true;
    }

    /// <summary>
    /// Cập nhật trạng thái AI Controller dựa trên player target.
    /// </summary>
    private void UpdateAIController(EnemyAIController aiController)
    {
        if (aiController == null) return;

        // Chỉ set playerTarget nếu target hiện tại là player
        if (target != null && IsValidTarget(target) && target.CompareTag("Player"))
        {
            aiController.playerTarget = target;
            
            var attackController = GetComponent<EnemyAttackController>();
            float currentDistance = Vector3.Distance(transform.position, target.position);

            if (attackController != null && currentDistance <= attackController.AttackRange)
            {
                Debug.Log($"[Enemy] {gameObject.name} UpdateAIController: chuyển sang AttackState");
                aiController.ChangeState(aiController.attackState);
            }
            else if (currentDistance <= chaseRange)
            {
                Debug.Log($"[Enemy] {gameObject.name} UpdateAIController: chuyển sang ChaseState");
                aiController.ChangeState(aiController.chaseState);
            }
        }
        else
        {
            // Không có player target, CHỈ chuyển về idle nếu đang ở chase/attack state
            aiController.playerTarget = null;
            
            if (aiController.stateMachine.currentState == aiController.chaseState || 
                aiController.stateMachine.currentState == aiController.attackState)
            {
                Debug.Log($"[Enemy] {gameObject.name} UpdateAIController: không có player, chuyển về IdleState");
                aiController.ChangeState(aiController.idleState);
            }
            // Nếu đang ở IdleState hoặc PatrolState, không làm gì để tránh override
        }
    }

    /// <summary>
    /// Đánh giá và chọn target tốt nhất từ danh sách ứng cử viên (ưu tiên gần, máu thấp...)
    /// </summary>
    protected virtual Transform EvaluatePlayerTargetCandidates(List<Transform> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;

        Transform bestCandidate = null;
        float bestScore = float.MinValue;

        foreach (var candidate in candidates)
        {
            if (!IsValidTarget(candidate)) continue;

            float score = CalculateTargetScore(candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    /// <summary>
    /// Tính điểm cho một target (có thể override để thêm logic ưu tiên khác).
    /// </summary>
    protected virtual float CalculateTargetScore(Transform candidate)
    {
        float distance = Vector3.Distance(transform.position, candidate.position);
        float distanceScore = Mathf.Max(0, detectionRange - distance); // Gần hơn = điểm cao hơn

        // Có thể thêm các yếu tố khác như health, threat level, v.v.
        float healthScore = 0f;
        var character = candidate.GetComponent<Character>();
        if (character != null)
        {
            // Ưu tiên target có ít máu hơn
            healthScore = (100f - character.CurrentHealth) * 0.1f;
        }

        return distanceScore + healthScore;
    }



    /// <summary>
    /// Vẽ Gizmos debug phạm vi detection, chase, attack, line tới target.
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // Vẽ detection range
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vẽ chase range
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Vẽ attack range
        var attackController = GetComponent<EnemyAttackController>();
        if (attackController != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackController.AttackRange);
        }

        // Vẽ line đến target
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }

        // Hiển thị thông tin debug
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Target: {target?.name ?? "None"}\nPlayers in scene: {FindAllPlayers().Length}");
        }
    }

    /// <summary>
    /// Truy cập nhanh AnimatorController của enemy (nếu có).
    /// </summary>
    public EnemyAnimatorController EnemyAnimatorController => GetComponent<EnemyAnimatorController>();

    // === Các phương thức combat, chết, buff, v.v. ===

    /// <summary>
    /// Nhận sát thương (gọi tới Character nếu có).
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        var character = GetComponent<Character>();
        if (character != null)
        {
            character.TakeDamage(damage);
        }
        OnDamageTaken?.Invoke(this, damage, _currentHealth);
    }

    /// <summary>
    /// Xử lý chết: chuyển state, gọi event, unregister khỏi manager, hủy object.
    /// </summary>
    public virtual void Die()
    {
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.ChangeState(aiController.deadState);
        }
        OnDeath?.Invoke();

        // Unregister from manager
        if (EnemyAIManager.Instance != null && aiController != null)
        {
            EnemyAIManager.Instance.RemoveAgent(aiController);
        }

        Destroy(gameObject);
    }

    public void SetMaxHealthMultiplier(float m) { _maxHealth *= m; }
    public void SetExperienceMultiplier(float m) { /* TODO */ }
    public void SetCurrencyMultiplier(float m) { /* TODO */ }
    public void SetItemDropChanceMultiplier(float m) { /* TODO */ }
    public void SetNamePrefix(string prefix) { gameObject.name = prefix + gameObject.name; }
    public void SetSummoned(bool value) { /* TODO */ }

    public float BossRange { get => bossRange; set => bossRange = value; }
    public float MinDistance { get => minDistance; set => minDistance = value; }

    protected void EnsureMeleeEnemyAIActive()
    {
        var meleeAi = GetComponent<MeleeEnemyAI>();
        if (meleeAi != null)
        {
            meleeAi.enabled = true;
        }
    }

    /// <summary>
    /// Setup waypoint patrol cho enemy (có thể gọi từ code).
    /// </summary>
    public void SetupWaypointPatrol(Transform anchorPoint, List<Transform> waypoints, PatrolMode mode = PatrolMode.Loop)
    {
        anchor = anchorPoint;
        patrolPoints = new List<Transform>(waypoints);
        patrolMode = mode;
        randomPatrolRadius = 0f;
        
        Debug.Log($"[Enemy] {gameObject.name} được setup waypoint patrol với {waypoints.Count} waypoints, mode: {mode}");
    }

    /// <summary>
    /// Setup random patrol cho enemy (có thể gọi từ code).
    /// </summary>
    public void SetupRandomPatrol(Transform anchorPoint, float radius = 5f)
    {
        anchor = anchorPoint;
        patrolPoints = new List<Transform>();
        patrolMode = PatrolMode.RandomAroundAnchor;
        randomPatrolRadius = radius;
        
        Debug.Log($"[Enemy] {gameObject.name} được setup random patrol quanh {anchorPoint.name} với radius {radius}");
    }

    /// <summary>
    /// Test method để force start patrol (gọi từ inspector hoặc code để test).
    /// </summary>
    [ContextMenu("Force Start Patrol")]
    public void ForceStartPatrol()
    {
        Debug.Log($"[Enemy] {gameObject.name} ForceStartPatrol - anchor: {anchor?.name}, mode: {patrolMode}, waypoints: {patrolPoints?.Count}");
        
        if (anchor == null)
        {
            Debug.LogError($"[Enemy] {gameObject.name} không có anchor để patrol!");
            return;
        }

        // Reset states
        patrolState = PatrolState.None;
        target = null;
        hasRandomTarget = false;
        
        // Force vào PatrolState
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.ChangeState(aiController.patrolState);
        }
        
        Debug.Log($"[Enemy] {gameObject.name} đã force start patrol!");
    }

    /// <summary>
    /// Debug method để kiểm tra setup
    /// </summary>
    [ContextMenu("Debug Player Detection")]
    public void DebugPlayerDetection()
    {
        var allPlayers = FindAllPlayers();
        foreach (var player in allPlayers)
        {
            float distance = Vector3.Distance(transform.position, player.position);
        }
    }
}

// Interface và classes khác giữ nguyên
public interface IDamageable
{
    void TakeDamage(float damage);
}

public class PlayerThreatManager { }