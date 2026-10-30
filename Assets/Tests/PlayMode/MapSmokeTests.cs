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
    public IEnumerator Player_StartsWithoutSkills_ChestScrollTeachesOne()
    {
        WorldFlags.Import(null);
        SkillBook.Import(null);
        InventoryManager.Instance?.ClearAll();
        yield return Load("M5_Cave");
        var player = Object.FindAnyObjectByType<PlayerController>();
        var skills = player.GetComponent<ModularSkillManager>();
        Assert.IsEmpty(skills.GetAvailableSkills(), "nhân vật khởi đầu chưa có kỹ năng nào");

        var chest = Object.FindObjectsByType<TreasureChest>().First(c => c.Flag == "chest:cave_hoard");
        chest.OpenChest();
        Assert.AreEqual(1, InventoryManager.Instance.GetItemCount("scroll_skill_heal"), "rương cho cuộn Hồi Phục");

        PlayerPrefs.SetInt("PlayerLevel", 3);
        skills.UpdateUnlockedSlots();
        int slot = Enumerable.Range(0, InventoryManager.TOTAL_SLOTS).First(i => !InventoryManager.Instance.GetSlot(i).IsEmpty);
        Assert.IsTrue(InventoryManager.Instance.UseItem(slot));
        Assert.IsTrue(SkillBook.Has("heal"));
        Assert.IsTrue(skills.GetSlot(0).HasSkill(), "tự gắn vào ô kỹ năng đầu");
        PlayerPrefs.DeleteKey("PlayerLevel");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator Music_MapTheme_ThenBattleTheme_OnEliteEncounter()
    {
        WorldFlags.Import(null);
        yield return Load("M2_Forest");
        var bank = Resources.Load<SfxBank>("Audio/AudioBank");
        Assert.AreSame(bank.mapMusic, AudioService.CurrentMusic, "nhạc map khi vào màn");
        Object.FindObjectsByType<EncounterGroup>().First(e => e.name == "Encounter_forest_boss").Activate();
        yield return null;
        Assert.AreSame(bank.battleMusic, AudioService.CurrentMusic, "nhạc trận khi mini-boss xuất hiện");
        foreach (var c in Object.FindObjectsByType<Character>().Where(c => c.GetComponent<PlayerController>() == null).ToList())
            if (c != null) c.TakeDamage(99999f);
        yield return new WaitForSeconds(0.5f);
        Assert.AreSame(bank.mapMusic, AudioService.CurrentMusic, "hạ boss → về nhạc map");
        AudioService.Play(SfxId.SwordHit); // không lỗi khi phát
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

/// <summary>AttackRunner chạy thật trong game: windup → hit → recovery, telegraph, hủy khi choáng, đạn, nối combo.</summary>
public class AttackRunnerPlayTests
{
    private readonly List<string> errors = new List<string>();
    private void OnLog(string msg, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error) errors.Add(msg); }
    [SetUp] public void SetUp() { errors.Clear(); Application.logMessageReceived += OnLog; WorldFlags.Import(null); }
    [TearDown] public void TearDown() { Application.logMessageReceived -= OnLog; }

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

    private static IEnumerator LoadForest()
    {
        SceneManager.LoadScene("M2_Forest");
        yield return null;
        yield return new WaitForSeconds(1.5f);
    }

    private static Character Spawn(string encounterId, out PlayerController player, string prefabName = null)
    {
        player = Object.FindAnyObjectByType<PlayerController>();
        var enc = Object.FindObjectsByType<EncounterGroup>().First(e => e.name == "Encounter_" + encounterId);
        enc.Activate();
        return null;
    }

    private static Character FirstEnemy(Vector2 near, string nameStart)
    {
        return Object.FindObjectsByType<Character>()
            .Where(c => c.GetComponent<PlayerController>() == null && c.name.StartsWith(nameStart))
            .OrderBy(c => Vector2.Distance(c.transform.position, near)).First();
    }

    private static void Isolate(Character enemy)
    {
        // Tắt AI/di chuyển để kiểm tra riêng AttackRunner
        foreach (var mb in enemy.GetComponents<MonoBehaviour>())
            if (mb is EnemyAIController || mb is Enemy || mb is EnemyMovementController) mb.enabled = false;
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        var rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }
    }

    [UnityTest]
    public IEnumerator Goblin_TelegraphsThenHits_AfterWindup()
    {
        yield return LoadForest();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var pc = player.GetComponent<Character>();
        Spawn("forest_e1", out _);
        yield return null;
        var goblin = FirstEnemy(player.transform.position, "Goblin");
        Isolate(goblin);
        var def = Resources.Load<AttackDef>("Attacks/Goblin_Club");
        // đặt người chơi sát bên phải goblin
        goblin.transform.position = new Vector3(player.transform.position.x - 0.7f, player.transform.position.y, 0f);
        float hp = pc.health.currentValue;

        goblin.GetComponent<EnemyAttackController>().Attack(player.transform);
        var runner = goblin.GetComponent<AttackRunner>();
        yield return new WaitForSeconds(0.15f);
        Assert.AreEqual(AttackRunner.Phase.Windup, runner.CurrentPhase, "đang windup");
        Assert.AreEqual(hp, pc.health.currentValue, "chưa gây sát thương trong windup");
        Assert.IsNotNull(GameObject.Find("VfxTelegraph"), "có vùng báo hiệu trong windup");
        Snap("attack_goblin_windup");

        yield return new WaitForSeconds(def.windup + 0.25f);
        float expected = goblin.AttackPower * def.damageMultiplier;
        Assert.Less(pc.health.currentValue, hp, "trúng đòn sau windup");
        Assert.AreEqual(hp - Mathf.Max(1f, expected - pc.Defense), pc.health.currentValue, 0.6f, "sát thương = AttackPower × hệ số");
        Assert.IsNull(GameObject.Find("VfxTelegraph"), "telegraph biến mất khi đòn ra");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator StunDuringWindup_CancelsTheAttack()
    {
        yield return LoadForest();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var pc = player.GetComponent<Character>();
        Spawn("forest_e1", out _);
        yield return null;
        var goblin = FirstEnemy(player.transform.position, "Goblin");
        Isolate(goblin);
        goblin.transform.position = new Vector3(player.transform.position.x - 0.7f, player.transform.position.y, 0f);
        float hp = pc.health.currentValue;

        goblin.GetComponent<EnemyAttackController>().Attack(player.transform);
        yield return new WaitForSeconds(0.2f);
        goblin.ApplyStun(1.5f);
        yield return new WaitForSeconds(1f);
        Assert.AreEqual(hp, pc.health.currentValue, "choáng giữa windup → đòn bị hủy");
        Assert.AreEqual(AttackRunner.Phase.Idle, goblin.GetComponent<AttackRunner>().CurrentPhase);
    }

    [UnityTest]
    public IEnumerator Archer_FiresProjectile_AfterWindup()
    {
        yield return LoadForest();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var pc = player.GetComponent<Character>();
        Spawn("forest_e2", out _);
        yield return null;
        var archer = FirstEnemy(player.transform.position, "Archer");
        Isolate(archer);
        archer.transform.position = new Vector3(player.transform.position.x - 4f, player.transform.position.y, 0f);
        float hp = pc.health.currentValue;

        archer.GetComponent<EnemyAttackController>().Attack(player.transform);
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(0, Object.FindObjectsByType<AttackProjectile>().Length, "chưa bắn trong windup");
        Snap("attack_archer_windup");
        yield return new WaitForSeconds(0.6f);
        Assert.AreEqual(1, Object.FindObjectsByType<AttackProjectile>().Length, "đã bắn 1 mũi tên");
        Snap("attack_archer_arrow");
        yield return new WaitForSeconds(1.2f);
        Assert.Less(pc.health.currentValue, hp, "mũi tên trúng người chơi");
    }

    [UnityTest]
    public IEnumerator Sheep_AndRat_NowDealDamage()
    {
        // trước đây cừu/chuột không có kỹ năng nào → không bao giờ gây sát thương
        SceneManager.LoadScene("M5_Cave");
        yield return null;
        yield return new WaitForSeconds(1.5f);
        var player = Object.FindAnyObjectByType<PlayerController>();
        var pc = player.GetComponent<Character>();
        Object.FindObjectsByType<EncounterGroup>().First(e => e.name == "Encounter_cave_rats_w").Activate();
        yield return null;
        var rat = FirstEnemy(player.transform.position, "Enemy_Rat");
        Isolate(rat);
        rat.transform.position = new Vector3(player.transform.position.x - 0.5f, player.transform.position.y, 0f);
        float hp = pc.health.currentValue;
        rat.GetComponent<EnemyAttackController>().Attack(player.transform);
        yield return new WaitForSeconds(1.2f);
        Assert.Less(pc.health.currentValue, hp, "chuột cắn trúng");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator PlayerCombo_ChainsThreeSlashes_AndFinisherHitsHarder()
    {
        yield return LoadForest();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var runner = player.GetComponent<AttackRunner>() ?? player.gameObject.AddComponent<AttackRunner>();
        var dummy = new GameObject("Dummy");
        dummy.tag = "Untagged";
        dummy.transform.position = player.transform.position + Vector3.right * 0.9f;
        dummy.AddComponent<CircleCollider2D>().radius = 0.3f;
        var ch = dummy.AddComponent<Character>();
        ch.health.Initialize(1000f, 0f);
        ch.health.currentValue = 1000f;
        float atk = player.GetComponent<Character>().AttackPower;

        var head = Resources.Load<AttackDef>("Attacks/Player_Slash1");
        var phases = new List<int>();
        runner.Started += d => phases.Add(d.comboIndex);
        Assert.IsTrue(runner.TryStart(head, Vector2.right));
        yield return new WaitForSeconds(head.windup + head.active + head.recovery * head.cancelFrom + 0.05f);
        Assert.IsTrue(runner.TryChain(Vector2.right), "qua mốc cancelFrom → nối được nhịp 2");
        yield return new WaitForSeconds(head.next.windup + head.next.active + head.next.recovery * head.next.cancelFrom + 0.05f);
        Assert.IsTrue(runner.TryChain(Vector2.right), "nối nhịp 3");
        yield return new WaitForSeconds(2f);

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, phases, "đúng chuỗi 3 nhịp");
        float lost = 1000f - ch.health.currentValue;
        Assert.GreaterOrEqual(lost, atk * 3.5f - 0.5f, "tối thiểu 1×+1×+1.5× AttackPower");
        Assert.LessOrEqual(lost, atk * 3.5f * 2f + 0.5f, "tối đa nếu cả 3 đòn chí mạng (×2)");
        Object.Destroy(dummy);
    }
}
