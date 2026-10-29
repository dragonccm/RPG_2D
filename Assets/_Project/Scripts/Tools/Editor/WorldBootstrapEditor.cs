#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// S4: editor bootstrap — gắn DialogueInteractable + quest portal bằng types THẬT của project
/// (component thêm qua eval CLI bị lưu fileID tạm → bị strip lúc play, nên phải qua menu này).
/// Managers/UI không lưu scene — GameSystemsBootstrap tự sinh lúc play.
/// Tools ▸ RPG ▸ S4 Bootstrap Scene — idempotent.
/// </summary>
public static class WorldBootstrapEditor
{
    [MenuItem("Tools/RPG/S4 Bootstrap Scene")]
    public static void Bootstrap()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path) || !scene.path.EndsWith("World.unity"))
        {
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/World.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        // 1. Xoá GO manager/UI rải từ eval — runtime bootstrap sẽ tự sinh khi play
        foreach (var name in new[] { "QuestManager", "SkillTreeManager", "DialogueUI", "SkillTreeUI", "GameUICanvas" })
        {
            var go = UnityEngine.GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        // 2. NPC: bỏ DialogueInteractable rác (nếu có) rồi gắn bằng type thật
        var mai = UnityEngine.GameObject.Find("NPC_Mai");
        if (mai != null) FixNpc(mai, DialogueDatabase.MaiFirstMeet, null, DialogueDatabase.MaiRepeat);

        var te = UnityEngine.GameObject.Find("NPC_OngTe");
        if (te == null)
        {
            te = WorldBuilder.CreateCircleSprite("NPC_OngTe", new Vector3(6f, -19.5f, 0f),
                new Color(0.85f, 0.65f, 0.45f, 0.95f), 0.45f);
            var label = new GameObject("Label");
            label.transform.SetParent(te.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = "Ông Tễ";
            tm.characterSize = 0.22f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.color = Color.white;
        }
        FixNpc(te, null, DialogueDatabase.OngTeQuest, DialogueDatabase.OngTeRepeat);

        // 3. Portal làng→rừng: tự bắt đầu Q2
        var portal = UnityEngine.GameObject.Find("Portal_village_to_forest");
        if (portal != null)
        {
            var sp = portal.GetComponent<ScenePortal>();
            if (sp != null) sp.startQuestOnEnter = "q2";
        }

        // 4. Rương Làng: Cuộn Bí Kíp Lướt Gió (Scroll_WindDash)
        var dashItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Resources/Items/Scroll_WindDash.asset");
        CreateOrUpdateChest("Chest_Village_Dash", new Vector3(-4.5f, -20.5f, 0f), dashItem, 1);

        // 5. Câu đố Rừng & Rào cản (Q3)
        var barrierGo = GameObject.Find("Puzzle_Barrier");
        if (barrierGo == null)
        {
            barrierGo = WorldBuilder.CreateCircleSprite("Puzzle_Barrier", new Vector3(15f, 16.5f, 0f), new Color(0.6f, 0.2f, 0.2f, 0.9f), 0.7f);
            var col = barrierGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 2.5f);
            barrierGo.layer = 0; // Default collision
        }

        var switchGo = GameObject.Find("Puzzle_Switch");
        if (switchGo == null)
        {
            switchGo = WorldBuilder.CreateCircleSprite("Puzzle_Switch", new Vector3(12f, 18.5f, 0f), new Color(0.7f, 0.7f, 0.7f, 1f), 0.5f);
            var col = switchGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
        }
        var ps = switchGo.GetComponent<PuzzleSwitch>() ?? switchGo.AddComponent<PuzzleSwitch>();
        SetPrivateField(ps, "targetBarrier", barrierGo);
        SetPrivateField(ps, "questId", "q3");
        SetPrivateField(ps, "objectiveId", "q3_switch");

        // 6. Rương Tháp Cổ: Chìa Khóa Tháp (Key_Tower) sau rào chắn
        var keyItem = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Resources/Items/Key_Tower.asset");
        var chestKey = CreateOrUpdateChest("Chest_Ruins_Key", new Vector3(17f, 16.5f, 0f), keyItem, 1);
        if (chestKey != null)
        {
            SetPrivateField(chestKey, "questId", "q3");
            SetPrivateField(chestKey, "objectiveId", "q3_key");
        }

        // 7. Bẫy gai (SpikeTraps) canh gác lối vào tháp
        CreateOrUpdateSpikeTrap("SpikeTrap_1", new Vector3(13.5f, 16.5f, 0f));
        CreateOrUpdateSpikeTrap("SpikeTrap_2", new Vector3(14.5f, 16.5f, 0f));

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("✅ S4 Bootstrap Scene xong (NPC + portal + Traps + Puzzles + Chests). Managers sinh runtime.");
    }

    private static TreasureChest CreateOrUpdateChest(string name, Vector3 pos, ItemData reward, int count)
    {
        var chestGo = GameObject.Find(name);
        if (chestGo == null)
        {
            chestGo = WorldBuilder.CreateCircleSprite(name, pos, new Color(0.85f, 0.65f, 0.2f, 1f), 0.6f);
            var col = chestGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;
        }
        var chest = chestGo.GetComponent<TreasureChest>() ?? chestGo.AddComponent<TreasureChest>();
        SetPrivateField(chest, "rewardItem", reward);
        SetPrivateField(chest, "rewardCount", count);
        return chest;
    }

    private static SpikeTrap CreateOrUpdateSpikeTrap(string name, Vector3 pos)
    {
        var trapGo = GameObject.Find(name);
        if (trapGo == null)
        {
            trapGo = WorldBuilder.CreateCircleSprite(name, pos, new Color(0.3f, 0.3f, 0.3f, 0.8f), 0.5f);
            var col = trapGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);
        }
        return trapGo.GetComponent<SpikeTrap>() ?? trapGo.AddComponent<SpikeTrap>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        field?.SetValue(target, value);
    }

    private static void FixNpc(GameObject npc, DialogueBlock first, DialogueBlock quest, DialogueBlock repeat)
    {
        foreach (var c in npc.GetComponents<Component>())
        {
            // comp từ eval (assembly tạm) hoặc DialogueInteractable cũ → bỏ, gắn lại sạch
            var asmName = c != null ? c.GetType().Assembly.GetName().Name : null;
            if (c is DialogueInteractable || (asmName != null && asmName.StartsWith("PipelineEval")))
            {
                Object.DestroyImmediate(c);
            }
        }
        npc.AddComponent<DialogueInteractable>().SetBlocks(first, quest, repeat);
    }
}
#endif
