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
    [Header("Auto Targeting")]
    [Tooltip("Phạm vi phát hiện ban đầu của kẻ địch. Kẻ địch sẽ tìm kiếm mục tiêu mới trong phạm vi này.")]
    public float detectionRange = 10f;
    [Tooltip("Phạm vi truy đuổi của kẻ địch. Kẻ địch sẽ tiếp tục đuổi theo mục tiêu trong phạm vi này ngay cả khi mục tiêu đã ra khỏi detectionRange.")]
    public float chaseRange = 20f;
    [Tooltip("Sát thương cơ bản của kẻ địch.")]
    public float baseDamage = 10f;
    [Tooltip("Layer của Player để kẻ địch có thể phát hiện.")]
    public LayerMask playerLayerMask = 1 << 7; // Layer của Player

    [SerializeField] protected Transform target; // Mục tiêu hiện tại của kẻ địch
    [SerializeField] protected NavMeshAgent agent; // NavMeshAgent để điều khiển di chuyển

    // Cache để tối ưu performance
    protected static readonly List<Transform> tempPlayerList = new List<Transform>();
    protected static readonly List<GameObject> tempPlayerObjects = new List<GameObject>();
    private float nextTargetUpdateTime = 0f;
    private const float TARGET_UPDATE_INTERVAL = 0.2f; // Cập nhật mục tiêu mỗi 0.2s

    // Cache player reference để tối ưu hiệu suất
    private static Transform[] cachedPlayers;
    private static float lastPlayerCacheTime = 0f;
    private const float PLAYER_CACHE_DURATION = 1f; // Cache player trong 1 giây

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

    /// <summary>
    /// Khởi tạo agent, đăng ký với AIManager, chuẩn bị NavMeshAgent.
    /// </summary>
    protected virtual void Start()
    {
        Debug.Log("--------------------------------------------------------------------------------------");
        agent = GetComponent<NavMeshAgent>();
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
        Debug.Log($"[{gameObject.name}] Enemy Start. Initial target: {target?.name ?? "None"}");

        // Debug thông tin layer và mask
        Debug.Log($"[{gameObject.name}] Player Layer Mask: {playerLayerMask.value} (binary: {System.Convert.ToString(playerLayerMask.value, 2)})");
    }

    /// <summary>
    /// Update: Cập nhật target định kỳ và xử lý di chuyển.
    /// </summary>
    protected virtual void Update()
    {
        // Chỉ cập nhật mục tiêu theo khoảng thời gian để tối ưu hiệu suất
        if (Time.time >= nextTargetUpdateTime)
        {
            UpdateTarget();
            nextTargetUpdateTime = Time.time + TARGET_UPDATE_INTERVAL;
        }

        HandleMovement();
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
                Debug.Log($"[{gameObject.name}] Found player by tag: {player.name}, Layer: {player.layer} ({LayerMask.LayerToName(player.layer)})");
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
                            Debug.Log($"[{gameObject.name}] Found player by layer: {obj.name}, Layer: {layer} ({LayerMask.LayerToName(layer)})");
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
                    Debug.Log($"[{gameObject.name}] Found player by component: {player.name}");
                }
            }
        }

        // Convert to Transform array và cache
        cachedPlayers = tempPlayerObjects.Where(p => p != null).Select(p => p.transform).ToArray();
        lastPlayerCacheTime = Time.time;

        Debug.Log($"[{gameObject.name}] Total players found: {cachedPlayers.Length}");

        return cachedPlayers;
    }

    /// <summary>
    /// Cập nhật target tốt nhất cho kẻ địch dựa trên khoảng cách, nhóm, v.v.
    /// </summary>
    protected virtual void UpdateTarget()
    {
        Debug.Log("--------------------------------------UpdateTarget------------------------------------------------");

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
                Debug.Log($"[{gameObject.name}] Current target {target.name} still valid at {distanceToCurrentTarget:F2}m");
            }
        }

        // BƯỚC 2: Tìm players bằng multiple methods
        tempPlayerList.Clear();

        // Method A: Physics.OverlapSphere
        var colliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);
        Debug.Log($"[{gameObject.name}] Physics.OverlapSphere found {colliders.Length} colliders");

        foreach (var col in colliders)
        {
            if (col != null && IsValidTarget(col.transform))
            {
                tempPlayerList.Add(col.transform);
                Debug.Log($"[{gameObject.name}] Valid target from OverlapSphere: {col.name}");
            }
        }

        // Method B: Fallback - tìm tất cả players trong scene và filter theo distance
        if (tempPlayerList.Count == 0)
        {
            Debug.Log($"[{gameObject.name}] No targets found via OverlapSphere, using fallback method");
            var allPlayers = FindAllPlayers();

            foreach (var player in allPlayers)
            {
                if (player != null && IsValidTarget(player))
                {
                    float distance = Vector3.Distance(transform.position, player.position);
                    if (distance <= detectionRange)
                    {
                        tempPlayerList.Add(player);
                        Debug.Log($"[{gameObject.name}] Valid target from fallback: {player.name} at {distance:F2}m");
                    }
                }
            }
        }

        Debug.Log($"[{gameObject.name}] Found {tempPlayerList.Count} potential targets in detection range");

        // BƯỚC 3: Đánh giá ứng cử viên player tốt nhất
        Transform bestPlayerCandidate = EvaluatePlayerTargetCandidates(tempPlayerList);
        if (bestPlayerCandidate != null)
        {
            float distToBestCandidate = Vector3.Distance(transform.position, bestPlayerCandidate.position);

            // So sánh với mục tiêu hiện tại
            if (currentBestTarget == null || distToBestCandidate < currentBestDistance)
            {
                currentBestTarget = bestPlayerCandidate;
                currentBestDistance = distToBestCandidate;
                Debug.Log($"[{gameObject.name}] New best target: {currentBestTarget.name} at {currentBestDistance:F2}m");
            }
        }

        // BƯỚC 4: Xử lý group target
        var aiController = GetComponent<EnemyAIController>();
        if (aiController?.group?.groupTarget != null)
        {
            float distToGroupTarget = Vector3.Distance(transform.position, aiController.group.groupTarget.position);
            if (distToGroupTarget <= detectionRange && IsValidTarget(aiController.group.groupTarget))
            {
                if (currentBestTarget == null || distToGroupTarget < currentBestDistance)
                {
                    currentBestTarget = aiController.group.groupTarget;
                    currentBestDistance = distToGroupTarget;
                    Debug.Log($"[{gameObject.name}] Prioritizing group target: {currentBestTarget.name}");
                }
            }
        }

        // BƯỚC 5: Gán target cuối cùng
        bool targetChanged = (target != currentBestTarget);
        target = currentBestTarget;

        if (targetChanged)
        {
            Debug.Log($"[{gameObject.name}] Target changed to: {target?.name ?? "None"}");
        }

        // BƯỚC 6: Kiểm tra chase range
        if (target != null && Vector3.Distance(transform.position, target.position) > chaseRange)
        {
            Debug.Log($"[{gameObject.name}] Target {target.name} out of chase range, setting to null");
            target = null;
        }

        // BƯỚC 7: Cập nhật AI Controller
        UpdateAIController(aiController);
    }

    /// <summary>
    /// Kiểm tra một transform có phải là target hợp lệ không (tag, layer, active, máu...)
    /// </summary>
    private bool IsValidTarget(Transform t)
    {
        if (t == null || t == transform) return false;

        // Kiểm tra tag
        if (!t.CompareTag("Player")) return false;

        // Kiểm tra layer (nếu được chỉ định)
        if (playerLayerMask != 0 && (playerLayerMask & (1 << t.gameObject.layer)) == 0)
        {
            Debug.Log($"[{gameObject.name}] Target {t.name} has wrong layer {t.gameObject.layer}");
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
    /// Cập nhật trạng thái AI Controller dựa trên target hiện tại.
    /// </summary>
    private void UpdateAIController(EnemyAIController aiController)
    {
        if (aiController == null) return;

        aiController.playerTarget = target;

        if (target != null)
        {
            var attackController = GetComponent<EnemyAttackController>();
            float currentDistance = Vector3.Distance(transform.position, target.position);

            if (attackController != null && currentDistance <= attackController.AttackRange)
            {
                Debug.Log($"[{gameObject.name}] Switching to AttackState");
                aiController.ChangeState(aiController.attackState);
            }
            else if (currentDistance <= chaseRange)
            {
                Debug.Log($"[{gameObject.name}] Switching to ChaseState");
                aiController.ChangeState(aiController.chaseState);
            }
        }
        else
        {
            Debug.Log($"[{gameObject.name}] No target, switching to IdleState");
            aiController.ChangeState(aiController.idleState);
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
    /// Xử lý di chuyển của kẻ địch (theo target hoặc dừng lại).
    /// </summary>
    protected virtual void HandleMovement()
    {
        if (agent == null) return;

        if (target != null)
        {
            if (agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] NavMeshAgent not ready for pathfinding");
            }
        }
        else
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
            agent.isStopped = true;
        }
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
    /// Debug method để kiểm tra setup
    /// </summary>
    [ContextMenu("Debug Player Detection")]
    public void DebugPlayerDetection()
    {
        Debug.Log($"=== Debug Player Detection for {gameObject.name} ===");
        Debug.Log($"Detection Range: {detectionRange}");
        Debug.Log($"Chase Range: {chaseRange}");
        Debug.Log($"Player Layer Mask: {playerLayerMask.value}");

        var allPlayers = FindAllPlayers();
        Debug.Log($"Found {allPlayers.Length} players in scene");

        foreach (var player in allPlayers)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            Debug.Log($"Player: {player.name}, Distance: {distance:F2}, Valid: {IsValidTarget(player)}");
        }
    }
}

// Interface và classes khác giữ nguyên
public interface IDamageable
{
    void TakeDamage(float damage);
}

public class PlayerThreatManager { }