using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Qu?n lý nhóm enemy tu?n tra chung m?t qu? ??o: anchor, waypoint, mode, radius.
/// Kéo th? enemy vào danh sách này ?? t? ??ng gán các giá tr? tu?n tra cho t?ng enemy.
/// </summary>
public class EnemyGroupManager : MonoBehaviour
{
    public enum PatrolGroupType { WaypointRoute, RandomAroundAnchor }

    [Header("Thiết Lập Nhóm Tuần Tra")]
    public PatrolGroupType patrolGroupType = PatrolGroupType.WaypointRoute;
    public Transform anchor;
    public List<Transform> patrolPoints = new List<Transform>();
    public Enemy.PatrolMode patrolMode = Enemy.PatrolMode.Loop;
    public float randomPatrolRadius = 5f;
    [Tooltip("Danh sách các enemy sẽ tuần tra theo thiết lập này")]
    public List<Enemy> enemies = new List<Enemy>();

    void Start()
    {
        Debug.Log($"[EnemyGroupManager] Setup patrol cho {enemies.Count} enemies - Type: {patrolGroupType}, Mode: {patrolMode}");
        
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            
            // Gán anchor cho tất cả enemies
            enemy.anchor = anchor;
            Debug.Log($"[EnemyGroupManager] Setup {enemy.name} - anchor: {anchor?.name}");
            
            if (patrolGroupType == PatrolGroupType.WaypointRoute)
            {
                // Patrol theo waypoints
                enemy.patrolMode = patrolMode;
                enemy.patrolPoints = new List<Transform>(patrolPoints); // Copy list để tránh reference issues
                enemy.randomPatrolRadius = 0f; // Không dùng random radius
                
                Debug.Log($"[EnemyGroupManager] {enemy.name} setup WAYPOINT patrol với {patrolPoints.Count} waypoints");
                
                // Debug check
                if (patrolPoints == null || patrolPoints.Count == 0)
                {
                    Debug.LogWarning($"[EnemyGroupManager] Enemy {enemy.name} được gán waypoint patrol nhưng không có waypoints!");
                }
            }
            else // RandomAroundAnchor
            {
                // Patrol ngẫu nhiên quanh anchor
                enemy.patrolMode = Enemy.PatrolMode.RandomAroundAnchor;
                enemy.patrolPoints = new List<Transform>(); // Clear waypoints
                enemy.randomPatrolRadius = randomPatrolRadius;
                
                Debug.Log($"[EnemyGroupManager] {enemy.name} setup RANDOM patrol với radius {randomPatrolRadius}");
            }
        }
    }
}
