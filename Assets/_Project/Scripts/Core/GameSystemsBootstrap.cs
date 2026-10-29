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
    private static void EnsureSystems()
    {
        if (QuestManager.Instance == null)
            new GameObject("QuestManager").AddComponent<QuestManager>();
        if (SkillTreeManager.Instance == null)
            new GameObject("SkillTreeManager").AddComponent<SkillTreeManager>();

        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
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
        // (Debug log tắt để không spam mỗi scene load)
    }
}
