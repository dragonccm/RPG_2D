using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// S4 (GAME_MASTER_PLAN 9.2): hộp thoại dưới màn — typing 30 ký tự/s, Space/click để qua.
/// Panel dựng bằng code trên UI CANVAS (không cần prefab). Chọn số 1/2 cho choice — MVP linear.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    private const float CHARS_PER_SECOND = 30f;

    private GameObject panel;
    private Text speakerText;
    private Text bodyText;
    private Text hint;

    private DialogueBlock current;
    private int lineIndex;
    private Coroutine typing;
    private bool typingDone;
    private float nextAdvanceTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildPanel();
        if (panel != null) panel.SetActive(false);
        else Debug.LogWarning("DialogueUI: không tìm thấy Canvas — panel chưa dựng.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildPanel()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        panel = new GameObject("DialoguePanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 20f);
        rt.sizeDelta = new Vector2(640f, 150f);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.85f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        speakerText = CreateText("Speaker", font, new Vector2(-310f, 45f), new Vector2(600f, 28f),
            TextAnchor.UpperLeft, 20, new Color(1f, 0.85f, 0.4f));
        bodyText = CreateText("Body", font, new Vector2(-310f, 10f), new Vector2(600f, 80f),
            TextAnchor.UpperLeft, 17, Color.white);
        hint = CreateText("Hint", font, new Vector2(180f, -40f), new Vector2(280f, 24f),
            TextAnchor.UpperRight, 13, new Color(1f, 1f, 1f, 0.6f));
        hint.text = "[Space] để tiếp tục";
    }

    private Text CreateText(string name, Font font, Vector2 pos, Vector2 size, TextAnchor anchor,
        int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = anchor;
        t.color = color;
        t.supportRichText = false;
        return t;
    }

    public void StartDialogue(DialogueBlock block)
    {
        if (block == null || block.lines == null || block.lines.Length == 0) return;
        if (current != null) return; // đang hội thoại — không chồng
        current = block;
        lineIndex = 0;
        panel.SetActive(true);
        ShowLine();
    }

    public bool IsOpen => current != null;

    private void ShowLine()
    {
        var line = current.lines[lineIndex];
        speakerText.text = line.speaker;
        bodyText.text = "";
        typingDone = false;
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string text)
    {
        var wait = new WaitForSeconds(1f / CHARS_PER_SECOND);
        for (int i = 1; i <= text.Length; i++)
        {
            bodyText.text = text.Substring(0, i);
            yield return wait;
        }
        typingDone = true;
    }

    private void Update()
    {
        if (current == null) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (Time.unscaledTime < nextAdvanceTime) return;
            nextAdvanceTime = Time.unscaledTime + 0.25f; // chống double-advance
            if (!typingDone)
            {
                // skip typing: hiện nguyên dòng
                if (typing != null) StopCoroutine(typing);
                bodyText.text = current.lines[lineIndex].text;
                typingDone = true;
            }
            else Advance();
        }
    }

    private void Advance()
    {
        lineIndex++;
        if (lineIndex < current.lines.Length) { ShowLine(); return; }
        EndDialogue();
    }

    private void EndDialogue()
    {
        var block = current;
        current = null;
        panel.SetActive(false);

        if (block == null) return;
        if (!string.IsNullOrEmpty(block.startQuestOnEnd) && QuestManager.Instance != null)
            QuestManager.Instance.StartQuest(block.startQuestOnEnd);
        if (!string.IsNullOrEmpty(block.reportObjectiveOnEnd) && QuestManager.Instance != null)
        {
            var kv = block.reportObjectiveOnEnd.Split(':');
            if (kv.Length == 2) QuestManager.Instance.ReportProgress(kv[0], kv[1]);
        }
        if (!string.IsNullOrEmpty(block.claimQuestOnEnd) && QuestManager.Instance != null)
            QuestManager.Instance.ClaimReward(block.claimQuestOnEnd);
        if (!string.IsNullOrEmpty(block.giveItemOnEnd) && InventoryManager.Instance != null)
        {
            var item = ItemDatabase.Get(block.giveItemOnEnd);
            if (item != null && InventoryManager.Instance.AddItem(item, 1))
                InventoryManager.Instance.Notify(Loc.T("inv.received", item.itemName));
        }
        if (!string.IsNullOrEmpty(block.setFlagOnEnd))
            WorldFlags.Set(block.setFlagOnEnd);
    }
}
