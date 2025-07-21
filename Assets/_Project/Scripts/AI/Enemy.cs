using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using System;

// Explicitly use UnityEngine.Random for Unity-specific random operations
using Random = UnityEngine.Random;

// Base Enemy class với auto-targeting được tối ưu
public class Enemy : MonoBehaviour
{
    [Header("Auto Targeting")]
    [Tooltip("Phạm vi phát hiện ban đầu của kẻ địch.")]
    public float detectionRange = 10f;
    [Tooltip("Phạm vi truy đuổi của kẻ địch. Kẻ địch sẽ tiếp tục đuổi theo mục tiêu trong phạm vi này ngay cả khi mục tiêu đã ra khỏi detectionRange.")]
    public float chaseRange = 20f;
    [Tooltip("Layer của Player để kẻ địch có thể phát hiện.")]
    public LayerMask playerLayerMask = 1 << 6; // Layer của Player

    [SerializeField] protected Transform target; // Mục tiêu hiện tại của kẻ địch
    [SerializeField] protected NavMeshAgent agent; // NavMeshAgent để điều khiển di chuyển

    // Cache để tối ưu performance
    protected static readonly List<Transform> tempPlayerList = new List<Transform>();
    private float nextTargetUpdateTime = 0f;
    private const float TARGET_UPDATE_INTERVAL = 0.2f; // Cập nhật mục tiêu mỗi 0.2s thay vì mỗi frame

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

    private float bossRange = 15f; // Giá trị ví dụ, điều chỉnh theo nhu cầu
    private float minDistance = 5f; // Giá trị ví dụ, điều chỉnh theo nhu cầu

    public float CurrentHealth { get => _currentHealth; protected set => _currentHealth = value; }
    public float MaxHealth { get => _maxHealth; protected set => _maxHealth = value; }
    public float DamageMultiplier { get => _damageMultiplier; set => _damageMultiplier = value; }
    public float SpeedMultiplier { get => _speedMultiplier; set => _speedMultiplier = value; }
    public float AttackSpeedMultiplier { get => _attackSpeedMultiplier; set => _attackSpeedMultiplier = value; }
    public float DefenseMultiplier { get => _defenseMultiplier; set => _defenseMultiplier = value; }
    public float DamageReduction { get => _damageReduction; set => _damageReduction = value; }
    public bool Invulnerable { get => _invulnerable; set => _invulnerable = value; }
    public bool IsPlayer { get; set; }
    public bool IsDead { get; set; }

    public void SetInvulnerable(bool value) { _invulnerable = value; }
    public void SetDamageMultiplier(float value) { _damageMultiplier = value; }
    public void SetSpeedMultiplier(float value) { _speedMultiplier = value; }
    public void SetAttackSpeedMultiplier(float value) { _attackSpeedMultiplier = value; }
    public void SetDefenseMultiplier(float value) { _defenseMultiplier = value; }
    public void SetDamageReduction(float value) { _damageReduction = value; }

    public float GetPower() { return 100f * _damageMultiplier * _speedMultiplier; } // Ví dụ đơn giản về tính toán sức mạnh
    public bool NeedsHealing() { return _currentHealth < _maxHealth; } // Kiểm tra xem kẻ địch có cần hồi máu không
    public float GetHealthPercent() { return _maxHealth > 0 ? _currentHealth / _maxHealth : 0f; } // Lấy phần trăm máu hiện tại
    public void Heal(float amount) { _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth); OnHealthChanged?.Invoke(_currentHealth, _maxHealth); } // Hồi máu cho kẻ địch

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false; // Tắt cập nhật xoay tự động để điều khiển bằng script
            agent.updateUpAxis = false; // Tắt cập nhật trục Y tự động
        }
        // Kích hoạt MeleeEnemyAI ngay khi Start để đảm bảo nó hoạt động
        EnsureMeleeEnemyAIActive();
    }

    protected virtual void Update()
    {
        // Chỉ cập nhật mục tiêu theo khoảng thời gian để tối ưu hiệu suất
        if (Time.time >= nextTargetUpdateTime)
        {
            UpdateTarget();
            nextTargetUpdateTime = Time.time + TARGET_UPDATE_INTERVAL;
        }

        // Kiểm tra nếu mục tiêu đã ra khỏi vùng truy đuổi (chaseRange) thì bỏ mục tiêu
        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > chaseRange) // Sử dụng chaseRange để quyết định có nên bỏ mục tiêu hay không
            {
                target = null;
            }
        }

        // Logic di chuyển được gọi ở class con hoặc xử lý trực tiếp tại đây
        HandleMovement();
    }

    protected virtual void UpdateTarget()
    {
        // Nếu đã có mục tiêu và mục tiêu vẫn trong tầm chaseRange, không cần tìm mục tiêu mới
        if (target != null && Vector3.Distance(transform.position, target.position) <= chaseRange)
        {
            return;
        }

        tempPlayerList.Clear();
        // Tìm tất cả các collider trong detectionRange thuộc playerLayerMask
        var colliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);

        foreach (var col in colliders)
        {
            if (col.CompareTag("Player")) // Đảm bảo đó là Player
            {
                tempPlayerList.Add(col.transform);
            }
        }

        // Chọn mục tiêu theo độ ưu tiên (mặc định là gần nhất)
        target = GetPriorityTarget(tempPlayerList);

        // Cập nhật playerTarget cho EnemyAIController nếu có
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.playerTarget = target;
            if (target != null)
            {
                // Nếu có mục tiêu mới, chuyển trạng thái AI sang truy đuổi
                aiController.ChangeState(aiController.chaseState);
            }
            else
            {
                // Nếu không có mục tiêu, chuyển trạng thái AI sang nhàn rỗi hoặc tuần tra
                aiController.ChangeState(aiController.idleState);
            }
        }
    }

    protected virtual Transform GetPriorityTarget(List<Transform> players)
    {
        if (players.Count == 0) return null;

        // Chọn player gần nhất làm mục tiêu ưu tiên
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;

        foreach (var player in players)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }

    protected virtual void HandleMovement()
    {
        if (agent == null)
        {
            Debug.LogWarning("NavMeshAgent is not assigned to Enemy: " + gameObject.name, this);
            return;
        }

        if (target != null)
        {
            // Di chuyển đến vị trí của mục tiêu
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        else
        {
            // Nếu không có mục tiêu, dừng di chuyển
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
            agent.isStopped = true;
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Vẽ vùng detection (phát hiện mục tiêu ban đầu)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Màu xanh lá cây
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vẽ vùng chase (truy đuổi)
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Màu xanh dương
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Vẽ vùng attack (nếu có EnemyAttackController)
        var attackController = GetComponent<EnemyAttackController>();
        if (attackController != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Màu đỏ
            Gizmos.DrawWireSphere(transform.position, attackController.attackRange);
        }
    }

    // --- Bổ sung method nhận sát thương và chết cho Enemy ---
    public virtual void TakeDamage(float damage)
    {
        var character = GetComponent<Character>();
        if (character != null)
        {
            character.TakeDamage(damage);
        }
    }

    public virtual void Die()
    {
        // Trigger hiệu ứng chết, animation, v.v.
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            // Chuyển trạng thái AI sang DeadState khi kẻ địch chết
            aiController.ChangeState(new DeadState(aiController, aiController.stateMachine));
        }
        // Có thể mở rộng: phát hiệu ứng, âm thanh, v.v.
        OnDeath?.Invoke(); // Kích hoạt sự kiện OnDeath
        Destroy(gameObject); // Hủy GameObject của kẻ địch
    }

    // Các phương thức cài đặt thuộc tính
    public void SetMaxHealthMultiplier(float m) { _maxHealth *= m; }
    public void SetExperienceMultiplier(float m) { /* TODO: Triển khai logic tăng kinh nghiệm */ }
    public void SetCurrencyMultiplier(float m) { /* TODO: Triển khai logic tăng tiền tệ */ }
    public void SetItemDropChanceMultiplier(float m) { /* TODO: Triển khai logic tăng tỷ lệ rơi đồ */ }
    public void SetNamePrefix(string prefix) { gameObject.name = prefix + gameObject.name; }
    public void SetSummoned(bool value) { /* TODO: Triển khai logic cho kẻ địch được triệu hồi */ }

    public float BossRange { get => bossRange; set => bossRange = value; }
    public float MinDistance { get => minDistance; set => minDistance = value; }

    // Đảm bảo script MeleeEnemyAI luôn active khi spawn
    protected void EnsureMeleeEnemyAIActive()
    {
        var meleeAi = GetComponent<MeleeEnemyAI>();
        if (meleeAi != null)
        {
            meleeAi.enabled = true; // Luôn kích hoạt script MeleeEnemyAI
        }
    }
}

// Interface ví dụ cho các đối tượng có thể nhận sát thương
public interface IDamageable
{
    void TakeDamage(float damage);
}

// Các lớp ví dụ khác (có thể không có trong các file bạn cung cấp, nhưng được giữ lại để tránh lỗi tham chiếu)
public class PlayerThreatManager { }
public class PlayerAI { }


