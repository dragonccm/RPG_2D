using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// P0-6 (GAME_MASTER_PLAN.md): test cấu trúc scene World —
/// mở scene chính (không lưu), kiểm các manager/player/spawner cốt lõi tồn tại.
/// </summary>
public class WorldSceneStructureTests
{
    const string SCENE_PATH = "Assets/_Project/Scenes/World.unity";

    [Test]
    public void WorldScene_HasCoreManagers()
    {
        Assert.IsTrue(File.Exists(SCENE_PATH), $"Scene chính phải tồn tại: {SCENE_PATH}");

        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        var names = scene.GetRootGameObjects().Select(go => go.name).ToArray();

        Assert.Contains("GameManager", names, "World.unity phải có GameManager");
        Assert.Contains("Warrior", names, "World.unity phải có nhân vật player (Warrior)");
        Assert.Contains("EnemySpawner", names, "World.unity phải có EnemySpawner");
        Assert.Contains("TileMapManager", names, "World.unity phải có TileMapManager");
    }

    [Test]
    public void WorldScene_PlayerHasController()
    {
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        var player = scene.GetRootGameObjects()
            .FirstOrDefault(go => go.GetComponent("PlayerController") != null);
        Assert.IsNotNull(player, "Phải có root object với PlayerController component");
        Assert.IsNotNull(player.GetComponent<Rigidbody2D>(),
            "Player phải có Rigidbody2D (di chuyển bằng MovePosition)");
    }
}
