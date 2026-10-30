using UnityEngine;

/// <summary>Điểm xuất hiện có tên trong map — cổng ở map khác trỏ tới bằng spawnId.</summary>
public class MapSpawnPoint : MonoBehaviour
{
    public string spawnId = GameFlow.StartSpawn;

    public static MapSpawnPoint Find(string id)
    {
        foreach (var p in FindObjectsByType<MapSpawnPoint>())
            if (p.spawnId == id) return p;
        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 0.8f, 0f));
    }
}
