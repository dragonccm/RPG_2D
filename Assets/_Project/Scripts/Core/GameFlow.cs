using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Luồng chơi giữa các map (LEVEL_DESIGN 6.1): mỗi map 1 scene. Đi qua cổng → chụp trạng thái
/// (SaveData) + autosave → nạp scene đích → khôi phục trạng thái và đặt player ở MapSpawnPoint.
/// Cũng là điểm vào New Game / Continue cho menu chính.
/// </summary>
public static class GameFlow
{
    public const string FirstMap = "M1_Village";
    public const string StartSpawn = "start";

    private static SaveSystem.SaveData carry;
    private static string pendingSpawn;

    public static bool HasPendingTransition => carry != null;

    /// <summary>Sang map khác qua cổng.</summary>
    public static void Travel(string sceneName, string spawnId, PlayerController player)
    {
        carry = SaveSystem.CaptureFrom(player, player != null ? player.GetComponent<ModularSkillManager>() : null,
            "portal:" + spawnId);
        carry.scene = sceneName;
        pendingSpawn = spawnId;
        SaveSystem.Save(carry);
        SceneManager.LoadScene(sceneName);
    }

    public static void NewGame()
    {
        WorldFlags.Import(null);
        if (InventoryManager.Instance != null) InventoryManager.Instance.ClearAll();
        LevelSystem.Import(1, 0);
        carry = null;
        pendingSpawn = StartSpawn;
        SceneManager.LoadScene(FirstMap);
    }

    public static void Continue()
    {
        var data = SaveSystem.Load();
        if (data == null || string.IsNullOrEmpty(data.scene) || !Application.CanStreamedLevelBeLoaded(data.scene))
        {
            NewGame();
            return;
        }
        carry = data;
        pendingSpawn = null; // đứng đúng chỗ đã lưu (đống lửa)
        SceneManager.LoadScene(data.scene);
    }

    /// <summary>Gọi bởi GameSystemsBootstrap sau khi scene nạp xong và manager đã sẵn sàng.</summary>
    public static void OnSceneReady()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null) return;
        LightUtil.EnsurePlayerLight(player.transform);

        if (carry != null)
        {
            SaveSystem.ApplyTo(carry, player, player.GetComponent<ModularSkillManager>());
            carry = null;
        }

        if (!string.IsNullOrEmpty(pendingSpawn))
        {
            var spawn = MapSpawnPoint.Find(pendingSpawn);
            if (spawn != null) PlaceAt(player, spawn.transform.position);
            pendingSpawn = null;
        }

        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null)
        {
            if (cam.target == null) cam.target = player.transform;
            cam.SnapTo(player.transform.position);
        }
    }

    private static void PlaceAt(PlayerController player, Vector3 pos)
    {
        player.transform.position = pos;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = pos;
    }
}
