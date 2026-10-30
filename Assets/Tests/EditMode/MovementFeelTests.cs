using NUnit.Framework;
using UnityEngine;

/// <summary>Chỉ số di chuyển ban đầu hợp lý: player không quá nhanh, quái thường đuổi chậm hơn player (trừ chuột bầy).</summary>
public class MovementFeelTests
{
    private static float Speed(string path)
    {
        var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return go.GetComponent<EnemyMovementController>().moveSpeed;
    }

    [Test]
    public void PlayerSpeed_IsComfortable()
    {
        var player = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Core/Warrior.prefab");
        float v = player.GetComponent<PlayerController>().moveSpeed;
        Assert.That(v, Is.InRange(2.4f, 3.0f), "tile/giây — màn hình rộng 17.8 tile, băng qua ≈ 6s");
        var rb = player.GetComponent<Rigidbody2D>();
        Assert.AreEqual(RigidbodyInterpolation2D.Interpolate, rb.interpolation, "nội suy vật lý để chuyển động mượt");
    }

    [Test]
    public void CommonEnemies_ChaseSlowerThanPlayer_SoPlayerCanDisengage()
    {
        var player = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Core/Warrior.prefab");
        float p = player.GetComponent<PlayerController>().moveSpeed;
        foreach (var path in new[] { "Assets/_Project/Art/animation/Goblin/Goblin.prefab", "Assets/_Project/Art/animation/sheep/Enemy_Sheep.prefab",
                                     "Assets/_Project/Art/animation/Lancer/Lancer.prefab", "Assets/_Project/Art/animation/Archer/Archer.prefab" })
            Assert.Less(Speed(path), p, path);
    }

    [Test]
    public void Camera_FollowIsSmoothAndFrameRateIndependent()
    {
        var cam = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Core/Main Camera.prefab");
        Assert.GreaterOrEqual(cam.GetComponent<CameraFollow>().followSpeed, 8f);
    }
}
