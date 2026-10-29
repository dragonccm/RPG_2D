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

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("✅ S4 Bootstrap Scene xong (NPC + portal). Managers sinh runtime.");
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
