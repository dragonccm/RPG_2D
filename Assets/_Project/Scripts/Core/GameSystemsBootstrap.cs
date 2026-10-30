using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// S4: bảo đảm các manager hệ thống luôn tồn tại ở mọi scene (runtime bootstrap).
/// Không lưu vào scene file — tự sinh lúc AfterSceneLoad nên không bao giờ bị strip.
/// Managers: QuestManager, SkillTreeManager, DialogueUI + SkillTreeUI (trên canvas).
/// </summary>
public static class GameSystemsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // Manager không DontDestroyOnLoad → dựng lại mỗi lần đổi map; trạng thái đi theo GameFlow (SaveData)
        // Đồng bộ màn hình: tránh khung hình không đều (vật lý 50Hz + render tự do gây cảm giác lag)
        QualitySettings.vSyncCount = 1;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSystems();
        GameFlow.OnSceneReady();
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (mode != UnityEngine.SceneManagement.LoadSceneMode.Single) return;
        EnsureSystems();
        GameFlow.OnSceneReady();
    }

    private static void EnsureSystems()
    {
        // Menu chính không cần manager gameplay
        if (UnityEngine.Object.FindAnyObjectByType<PlayerController>() == null) return;

        if (QuestManager.Instance == null)
            new GameObject("QuestManager").AddComponent<QuestManager>();
        if (SkillTreeManager.Instance == null)
            new GameObject("SkillTreeManager").AddComponent<SkillTreeManager>();
        if (InventoryManager.Instance == null)
            new GameObject("InventoryManager").AddComponent<InventoryManager>();

        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("GameUICanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = go.GetComponent<Canvas>();
        }
        var canvasTransform = canvas.transform;

        if (DialogueUI.Instance == null)
        {
            var d = new GameObject("DialogueUI");
            d.transform.SetParent(canvasTransform, false);
            d.AddComponent<DialogueUI>();
        }
        if (SkillTreeUI.Instance == null)
        {
            var s = new GameObject("SkillTreeUI");
            s.transform.SetParent(canvasTransform, false);
            s.AddComponent<SkillTreeUI>();
        }
        if (InventoryUI.Instance == null)
        {
            var inv = new GameObject("InventoryUI");
            inv.transform.SetParent(canvasTransform, false);
            inv.AddComponent<InventoryUI>();
        }
        // (Debug log tắt để không spam mỗi scene load)
    }
}
