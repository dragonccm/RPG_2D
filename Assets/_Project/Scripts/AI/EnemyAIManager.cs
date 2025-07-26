using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Quản lý tất cả các AI agent trong scene. Singleton duy nhất, hỗ trợ group, spawner, event, tìm kiếm agent.
/// </summary>
public class EnemyAIManager : MonoBehaviour
{
    // === Các thuộc tính quản lý agent, group, spawner, event ===
    [Header("AI Management")]
    [Tooltip("Danh sách tất cả các AI agent đang hoạt động")]
    public List<EnemyAIController> activeAgents = new List<EnemyAIController>();
    [Tooltip("Khoảng thời gian cập nhật cho các agent ở xa")]
    public float distantAgentUpdateInterval = 1f;
    [Tooltip("Khoảng cách để coi một agent là ở xa")]
    public float distantAgentThreshold = 50f;

    [Header("AI Spawning")]
    [Tooltip("Có cho phép sinh ra AI không")]
    public bool allowSpawning = true;
    [Tooltip("Số lượng AI tối đa trong scene")]
    public int maxAgents = 50;
    [Tooltip("Danh sách các spawner")]
    public List<EnemySpawner> spawners = new List<EnemySpawner>();

    [Header("AI Groups")]
    [Tooltip("Có sử dụng hệ thống nhóm không")]
    public bool useGrouping = true;
    [Tooltip("Danh sách các nhóm AI")]
    public List<EnemyGroupFormationManager> aiGroups = new List<EnemyGroupFormationManager>();

    [Header("AI Events")]
    [Tooltip("Sự kiện khi một agent được thêm vào")]
    public UnityEvent<EnemyAIController> OnAgentAdded;
    [Tooltip("Sự kiện khi một agent bị xóa")]
    public UnityEvent<EnemyAIController> OnAgentRemoved;
    [Tooltip("Sự kiện khi một agent chết")]
    public UnityEvent<EnemyAIController> OnAgentDeath;

    // Singleton instance
    public static EnemyAIManager Instance { get; private set; }

    // === Khởi tạo Singleton, tìm spawner, khởi động coroutine ===
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Tìm tất cả các spawner trong scene nếu chưa được gán
        if (spawners.Count == 0)
        {
            spawners.AddRange(FindObjectsOfType<EnemySpawner>());
        }

        // Bắt đầu coroutine cập nhật các agent ở xa
        StartCoroutine(UpdateDistantAgents());
    }

    // === Các phương thức quản lý agent (add, remove, death, tìm kiếm) ===
    /// <summary>
    /// Thêm một agent vào danh sách quản lý (tự động đăng ký event chết).
    /// </summary>
    public void AddAgent(EnemyAIController agent)
    {
        // Nếu agent chưa có trong danh sách
        if (!activeAgents.Contains(agent))
        {
            // Thêm vào danh sách
            activeAgents.Add(agent);

            // Đăng ký sự kiện khi agent chết
            Character character = agent.GetComponent<Character>();
            if (character != null)
            {
                // Correctly subscribe to the OnDeath event
                character.OnDeath += () => HandleAgentDeath(agent);
            }

            // Gọi sự kiện
            OnAgentAdded.Invoke(agent);
        }
    }

    /// <summary>
    /// Xóa một agent khỏi danh sách quản lý (tự động hủy đăng ký event chết).
    /// </summary>
    public void RemoveAgent(EnemyAIController agent)
    {
        // Nếu agent có trong danh sách
        if (activeAgents.Contains(agent))
        {
            // Xóa khỏi danh sách
            activeAgents.Remove(agent);

            // Hủy đăng ký sự kiện
            Character character = agent.GetComponent<Character>();
            if (character != null)
            {
                character.OnDeath -= () => HandleAgentDeath(agent);
            }

            // Gọi sự kiện
            OnAgentRemoved.Invoke(agent);
        }
    }

    /// <summary>
    /// Xử lý sự kiện khi một agent chết (gọi event, xóa khỏi danh sách).
    /// </summary>
    private void HandleAgentDeath(EnemyAIController agent)
    {
        // Gọi sự kiện
        OnAgentDeath.Invoke(agent);

        // Xóa agent khỏi danh sách
        RemoveAgent(agent);
    }

    /// <summary>
    /// Tìm agent gần nhất với một vị trí.
    /// </summary>
    public EnemyAIController FindNearestAgent(Vector3 position)
    {
        EnemyAIController nearestAgent = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAIController agent in activeAgents)
        {
            float distance = Vector3.Distance(position, agent.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestAgent = agent;
            }
        }

        return nearestAgent;
    }

    /// <summary>
    /// Tìm tất cả agent trong một bán kính.
    /// </summary>
    public List<EnemyAIController> FindAgentsInRadius(Vector3 position, float radius)
    {
        List<EnemyAIController> agentsInRadius = new List<EnemyAIController>();

        foreach (EnemyAIController agent in activeAgents)
        {
            if (Vector3.Distance(position, agent.transform.position) <= radius)
            {
                agentsInRadius.Add(agent);
            }
        }

        return agentsInRadius;
    }

    /// <summary>
    /// Coroutine cập nhật các agent ở xa (có thể tối ưu performance).
    /// </summary>
    private IEnumerator UpdateDistantAgents()
    {
        while (true)
        {
            // Tìm người chơi
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 playerPosition = player.transform.position;

                // Cập nhật các agent ở xa
                foreach (EnemyAIController agent in activeAgents)
                {
                    // Loại bỏ logic vô hiệu hóa component để tránh phá vỡ State Machine
                    // Thay vào đó, nếu cần tối ưu, có thể giảm tần suất tính toán bên trong AI
                    // hoặc sử dụng một hệ thống "cập nhật thấp tần số" riêng biệt.
                    // Hiện tại, chúng ta sẽ để AI luôn hoạt động.
                    // if (Vector3.Distance(playerPosition, agent.transform.position) > distantAgentThreshold)
                    // {
                    //     agent.enabled = false; 
                    // }
                    // else
                    // {
                    //     agent.enabled = true;
                    // }
                }
            }

            // Chờ một khoảng thời gian
            yield return new WaitForSeconds(distantAgentUpdateInterval);
        }
    }

    // === Quản lý group AI ===
    /// <summary>
    /// Tạo một nhóm AI mới.
    /// </summary>
    public EnemyGroupFormationManager CreateGroup()
    {
        if (!useGrouping)
        {
            return null;
        }

        GameObject groupObject = new GameObject("EnemyGroup");
        EnemyGroupFormationManager newGroup = groupObject.AddComponent<EnemyGroupFormationManager>();
        aiGroups.Add(newGroup);
        return newGroup;
    }

    /// <summary>
    /// Thêm một agent vào một nhóm.
    /// </summary>
    public void AddAgentToGroup(EnemyAIController agent, EnemyGroupFormationManager group)
    {
        if (useGrouping && group != null && !group.members.Contains(agent))
        {
            group.members.Add(agent);
            agent.group = group;
        }
    }

    /// <summary>
    /// Xóa một agent khỏi một nhóm.
    /// </summary>
    public void RemoveAgentFromGroup(EnemyAIController agent, EnemyGroupFormationManager group)
    {
        if (useGrouping && group != null && group.members.Contains(agent))
        {
            group.members.Remove(agent);
            agent.group = null;
        }
    }

    /// <summary>
    /// Xóa tất cả các agent và group khỏi scene.
    /// </summary>
    public void ClearAllAgents()
    {
        // Xóa tất cả các agent
        for (int i = activeAgents.Count - 1; i >= 0; i--)
        {
            if (activeAgents[i] != null)
            {
                Destroy(activeAgents[i].gameObject);
            }
        }

        // Xóa danh sách
        activeAgents.Clear();

        // Xóa các nhóm
        aiGroups.Clear();
    }
}
