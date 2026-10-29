using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public IEnumerator Maps_LoadAndRun_WithoutErrors([Values("M1_Village", "M2_Forest", "M3_Marsh", "M4_Tower", "M5_Cave")] string scene)
    {
        yield return Load(scene);
        var player = Object.FindAnyObjectByType<PlayerController>();
        Assert.IsNotNull(player, "có player");
        Assert.IsNotNull(QuestManager.Instance, "QuestManager");
        Assert.IsNotNull(SkillTreeManager.Instance, "SkillTreeManager");
        Assert.IsNotNull(InventoryManager.Instance, "InventoryManager");
        Assert.Greater(SpriteLoopSystem.Count, 0, "animation môi trường chạy");
        Assert.IsFalse(player.GetComponent<Character>().GodModeEnabled, "không bật god mode trong map thật");
        Snap(scene + "_play");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator VillageGate_OpensWhenFlagSet()
    {
        WorldFlags.Import(null);
        yield return Load("M1_Village");
        var gates = Object.FindObjectsByType<FlagGate>().Where(g => g.flag == "gate:village_north").ToList();
        Assert.AreEqual(5, gates.Count, "5 đoạn rào cổng bắc");
        Assert.IsTrue(gates.All(g => g.GetComponent<Collider2D>().enabled), "đóng trước q1");
        var portal = Object.FindObjectsByType<MapPortal>().First(p => p.targetScene == "M2_Forest");
        Assert.IsFalse(portal.CanPass(out _));

        WorldFlags.Set("gate:village_north");
        yield return null;
        Assert.IsTrue(gates.All(g => !g.GetComponent<Collider2D>().enabled), "mở sau khi Mai trao cuộn");
        Assert.IsTrue(portal.CanPass(out _));
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator Encounter_SpawnsSquad_AndClearsFlagWhenAllDie()
    {
        WorldFlags.Import(null);
        yield return Load("M2_Forest");
        var enc = Object.FindObjectsByType<EncounterGroup>().First(e => e.name == "Encounter_forest_e2");
        enc.Activate();
        yield return null;
        Assert.Greater(enc.Alive, 0, "sinh quái");
        var spawned = Object.FindObjectsByType<Character>()
            .Where(c => c.GetComponent<PlayerController>() == null
                        && Vector2.Distance(c.transform.position, enc.transform.position) < 4f).ToList();
        Assert.AreEqual(enc.Alive, spawned.Select(c => c.gameObject).Distinct().Count(), "đúng đội hình");
        Assert.IsTrue(spawned.All(c => c.GetComponent<YSort>() != null), "quái được y-sort");
        foreach (var c in spawned) if (c != null) c.TakeDamage(99999f);
        yield return null;
        foreach (var c in spawned) if (c != null) c.TakeDamage(99999f); // prefab có 2 Character (Archer)
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(enc.IsCleared,
            $"alive={enc.Alive} spawned={spawned.Count} left={spawned.Count(c => c != null)} errors={string.Join(" | ", errors)}");
        Assert.IsTrue(WorldFlags.Has(enc.Flag), "lưu trạng thái đã dọn");
    }

    [UnityTest]
    public IEnumerator Gap_WalkingIn_FallsBackToEdge()
    {
        yield return Load("M1_Village");
        var gap = Object.FindObjectsByType<FallHazard>().First();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var character = player.GetComponent<Character>();
        float hp = character.health.currentValue;
        var bounds = gap.GetComponent<Collider2D>().bounds;

        // Đi bộ vào từ mép trái khe
        var rb = player.GetComponent<Rigidbody2D>();
        Vector2 edge = new Vector2(bounds.min.x - 0.6f, bounds.center.y);
        player.transform.position = edge; rb.position = edge;
        yield return new WaitForFixedUpdate();
        rb.position = new Vector2(bounds.center.x, bounds.center.y);
        player.transform.position = rb.position;
        yield return new WaitForSeconds(0.3f);

        Assert.IsFalse(bounds.Contains(new Vector3(player.transform.position.x, player.transform.position.y, bounds.center.z)),
            "không còn đứng trong khe");
        Assert.Less(character.health.currentValue, hp, "rơi mất máu");
    }

    [UnityTest]
    public IEnumerator RatSquad_UsesRatPrefab_AndSmallSprite()
    {
        WorldFlags.Import(null);
        yield return Load("M5_Cave");
        var enc = Object.FindObjectsByType<EncounterGroup>().First(e => e.name == "Encounter_cave_rats_w");
        enc.Activate();
        yield return new WaitForSeconds(0.3f);
        var rats = Object.FindObjectsByType<Character>().Where(c => c.name.StartsWith("Enemy_Rat")).ToList();
        Assert.AreEqual(4, rats.Count, "4 chuột");
        var sr = rats[0].GetComponentInChildren<SpriteRenderer>();
        Assert.Less(sr.bounds.size.x, 1f, "chuột nhỏ hơn 1 tile (đúng mật độ pixel 64 PPU)");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator FerryAndCave_PortalsGateOnFlags()
    {
        WorldFlags.Import(null);
        yield return Load("M1_Village");
        var dock = Object.FindObjectsByType<MapPortal>().First(p => p.targetScene == "M3_Marsh");
        Assert.IsFalse(dock.CanPass(out _), "bến đò đóng khi chưa hạ thủ lĩnh đầm");
        WorldFlags.Set("clear:marsh_boss");
        Assert.IsTrue(dock.CanPass(out _));
        Assert.IsNotNull(Object.FindObjectsByType<MapPortal>().FirstOrDefault(p => p.targetScene == "M5_Cave"), "có cổng hang");
    }

    [UnityTest]
    public IEnumerator VillageAtmosphere_ShiftsWhenQ3Starts_AndOngTeLeaves()
    {
        WorldFlags.Import(null);
        yield return Load("M1_Village");
        var ongTe = GameObject.Find("NPC_Ong_Te");
        Assert.IsNotNull(ongTe);
        var light = Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>()
            .First(l => l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global);
        float before = light.color.r;
        QuestManager.Instance.StartQuest("q3");
        yield return new WaitForSeconds(5f);
        Assert.IsTrue(WorldFlags.Has("quest:q3:started"));
        Assert.Less(light.color.b, 0.4f, "ánh sáng ngả đỏ");
        Assert.IsFalse(ongTe.GetComponentsInChildren<Renderer>().Any(r => r.enabled), "Ông Tễ rời đi");
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
