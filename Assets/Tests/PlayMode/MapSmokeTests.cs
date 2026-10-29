using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Smoke test Play Mode cho 4 map: nạp scene, chạy vài giây, không lỗi/exception, player + manager có mặt,
/// chụp màn hình (biến môi trường RPG_SNAP_DIR) để kiểm tra ánh sáng/hiển thị thật trong game.
/// </summary>
public class MapSmokeTests
{
    private readonly List<string> errors = new List<string>();

    private void OnLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) errors.Add(type + ": " + msg);
    }

    [SetUp] public void SetUp() { errors.Clear(); Application.logMessageReceived += OnLog; }
    [TearDown] public void TearDown() { Application.logMessageReceived -= OnLog; }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return null;
        yield return new WaitForSeconds(2f);
    }

    private static void Snap(string name)
    {
        string dir = System.Environment.GetEnvironmentVariable("RPG_SNAP_DIR");
        if (string.IsNullOrEmpty(dir) || Camera.main == null) return;
        Directory.CreateDirectory(dir);
        var cam = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = prev;
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        Object.Destroy(rt);
    }

    [UnityTest]
    public IEnumerator Maps_LoadAndRun_WithoutErrors([Values("M1_Village", "M2_Forest", "M3_Marsh", "M4_Tower")] string scene)
    {
        yield return Load(scene);
        var player = Object.FindAnyObjectByType<PlayerController>();
        Assert.IsNotNull(player, "có player");
        Assert.IsNotNull(QuestManager.Instance, "QuestManager");
        Assert.IsNotNull(SkillTreeManager.Instance, "SkillTreeManager");
        Assert.IsNotNull(InventoryManager.Instance, "InventoryManager");
        Assert.Greater(SpriteLoopSystem.Count, 0, "animation môi trường chạy");
        Snap(scene + "_play");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator Portal_Travel_CarriesStateAndPlacesAtSpawn()
    {
        yield return Load("M1_Village");
        var player = Object.FindAnyObjectByType<PlayerController>();
        WorldFlags.Set("gate:village_north");
        InventoryManager.Instance.AddItem(ItemDatabase.Get("potion_hp"), 2);

        GameFlow.Travel("M2_Forest", "from_village", player);
        yield return null;
        yield return new WaitForSeconds(1f);

        Assert.AreEqual("M2_Forest", SceneManager.GetActiveScene().name);
        var p2 = Object.FindAnyObjectByType<PlayerController>();
        var spawn = MapSpawnPoint.Find("from_village");
        Assert.IsNotNull(spawn);
        Assert.Less(Vector2.Distance(p2.transform.position, spawn.transform.position), 1.5f, "đứng ở điểm vào");
        Assert.IsTrue(WorldFlags.Has("gate:village_north"), "flag đi theo");
        Assert.GreaterOrEqual(InventoryManager.Instance.GetItemCount("potion_hp"), 2, "túi đồ đi theo");
        Snap("travel_M2_arrival");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }
}
