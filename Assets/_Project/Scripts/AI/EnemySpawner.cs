using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawn kẻ địch từ prefab tại các điểm spawn, giới hạn số lượng và tự động gán playerTarget.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Prefab kẻ địch cần spawn")]
    public GameObject enemyPrefab;

    [Tooltip("Thời gian giữa các lần spawn")]
    public float spawnInterval = 5f;

    [Tooltip("Số lượng kẻ địch tối đa được phép tồn tại từ spawner này")]
    public int maxEnemies = 5;

    [Tooltip("Các điểm spawn")]
    public Transform[] spawnPoints;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Transform playerTransform;

    void Start()
    {
        // Tìm player trong scene (giả sử có PlayerController hoặc tag "Player")
        // Ưu tiên tìm theo tag "Player" để linh hoạt hơn
        var playerByTag = GameObject.FindGameObjectWithTag("Player");
        if (playerByTag != null)
            playerTransform = playerByTag.transform;
        else
        {
            var playerObj = GameObject.FindObjectOfType<PlayerController>(); // Fallback nếu có PlayerController
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
            {
                Debug.LogWarning("EnemySpawner: Could not find Player. Ensure Player has 'Player' tag or PlayerController component.");
            }
        }
        StartCoroutine(SpawnEnemiesRoutine());
    }

    IEnumerator SpawnEnemiesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            // Xóa các enemy đã bị destroy khỏi danh sách
            spawnedEnemies.RemoveAll(e => e == null);
            if (spawnedEnemies.Count < maxEnemies)
            {
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    Debug.LogWarning("EnemySpawner: No spawn points assigned. Cannot spawn enemies.");
                    continue;
                }

                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedEnemies.Add(enemy);

                // Gán playerTransform cho các script liên quan nếu có
                // Enemy.cs sẽ tự động cập nhật EnemyAIController.playerTarget
                var enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    // Enemy component sẽ tự tìm player và quản lý target
                    // Không cần gán playerTransform trực tiếp ở đây nữa cho EnemyAIController, Movement, Attack
                    // vì Enemy.UpdateTarget() sẽ làm điều đó.
                }
                else
                {
                    Debug.LogWarning($"Spawned enemy '{enemy.name}' does not have an Enemy component. AI/Movement/Attack may not function correctly.", enemy);
                }
            }
        }
    }
}
