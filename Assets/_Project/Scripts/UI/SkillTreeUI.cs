using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// S4 (GAME_MASTER_PLAN 7): UI cây kỹ năng — Tab mở/đóng, danh sách nút 4 nhánh,
/// click để mở nút (cần điểm + đúng tier/level). Panel dựng bằng code trên UI CANVAS.
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI Instance { get; private set; }

    private GameObject panel;
    private Text pointsLabel;
    private Transform rowsRoot;
    private readonly System.Collections.Generic.List<GameObject> rowCache =
        new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildPanel();
        if (panel != null) panel.SetActive(false);
        else Debug.LogWarning("SkillTreeUI: không tìm thấy Canvas — panel chưa dựng.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) TogglePanel();
    }
    public void TogglePanel()
    {
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen) return; // hội thoại ưu tiên
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) Refresh();
    }

    private void BuildPanel()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        panel = new GameObject("SkillTreePanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.one * 0.5f;
        rt.anchorMax = Vector2.one * 0.5f;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(520f, 420f);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pointsLabel = MakeText(panel.transform, "Points", font, new Vector2(0f, 190f), new Vector2(480f, 26f),
            TextAnchor.UpperCenter, 18, new Color(1f, 0.85f, 0.4f));
        rowsRoot = new GameObject("Rows", typeof(RectTransform)).transform;
        rowsRoot.SetParent(panel.transform, false);
        var rowsRt = rowsRoot.GetComponent<RectTransform>();
        rowsRt.anchoredPosition = new Vector2(0f, 170f);
        rowsRt.sizeDelta = new Vector2(500f, 360f);
    }

    private Text MakeText(Transform parent, string name, Font font, Vector2 pos, Vector2 size,
        TextAnchor anchor, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = color;
        return t;
    }

    /// <summary>Render lại toàn bộ danh sách nút theo trạng thái hiện tại.</summary>
    public void Refresh()
    {
        if (panel == null || !panel.activeSelf) return;
        var mgr = SkillTreeManager.Instance;
        if (mgr == null) return;

        foreach (var row in rowCache) Destroy(row);
        rowCache.Clear();

        pointsLabel.text = $"CÂY KỸ NĂNG — điểm còn: {mgr.SkillPoints}  (Tab để đóng)";
        float y = -14f;
        foreach (var node in SkillTreeManager.ALL_NODES)
        {
            bool unlocked = mgr.IsUnlocked(node.id);
            var can = mgr.CanUnlock(node.id, out var reasonRaw);
            string state = unlocked ? "✔" : (can == SkillTreeManager.UnlockResult.Ok ? "[Mở]" : $"🔒 {reasonRaw}");

            var row = new GameObject("Row_" + node.id, typeof(RectTransform));
            row.transform.SetParent(rowsRoot, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta = new Vector2(500f, 24f);
            var rowText = row.AddComponent<Text>();
            rowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rowText.fontSize = 14;
            rowText.alignment = TextAnchor.UpperLeft;
            rowText.color = unlocked ? new Color(0.5f, 1f, 0.55f)
                : (can == SkillTreeManager.UnlockResult.Ok ? Color.white : new Color(1f, 1f, 1f, 0.45f));
            rowText.text = $"{state}  T{node.tier} {node.name} — {node.desc}";
            if (can == SkillTreeManager.UnlockResult.Ok && !unlocked)
            {
                var btn = row.AddComponent<Button>();
                var captured = node.id;
                btn.onClick.AddListener(() =>
                {
                    if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.Unlock(captured))
                        Refresh();
                });
            }
            rowCache.Add(row);
            y -= 22f;
        }
    }
}
