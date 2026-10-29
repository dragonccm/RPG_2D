using UnityEngine;

/// <summary>
/// S4 (GAME_MASTER_PLAN 9.2): NPC talkable — F gần NPC để mở hội thoại.
/// Chọn block theo trạng thái quest: có quest → block quest, đã gặp → repeat, chưa → first meet.
/// </summary>
public class DialogueInteractable : MonoBehaviour
{
    [Header("Hội thoại")]
    [SerializeField] private DialogueBlock firstMeet;
    [SerializeField] private DialogueBlock questBlock;
    [SerializeField] private DialogueBlock repeat;

    [SerializeField] private float interactRange = 1.6f;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private float nextInteractTime;

    public void SetBlocks(DialogueBlock first, DialogueBlock quest, DialogueBlock rep)
    {
        firstMeet = first; questBlock = quest; repeat = rep;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;
        if (Time.unscaledTime < nextInteractTime) return;
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen) return;

        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        if (Vector2.Distance(player.transform.position, transform.position) > interactRange) return;

        var block = PickBlock();
        if (block == null) return;
        DialogueUI.Instance.StartDialogue(block);
        nextInteractTime = Time.unscaledTime + 1f;
    }

    private DialogueBlock PickBlock()
    {
        if (questBlock != null)
        {
            // block quest gắn với quest trong reportObjectiveOnEnd / startQuestOnEnd
            string qid = questBlock.reportObjectiveOnEnd != null
                ? questBlock.reportObjectiveOnEnd.Split(':')[0]
                : questBlock.startQuestOnEnd;
            if (qid != null && QuestManager.Instance != null)
            {
                var st = QuestManager.Instance.GetState(qid);
                if (st == QuestState.Active || st == QuestState.Complete) return questBlock;
            }
        }
        if (QuestManager.Instance != null && firstMeet != null)
        {
            // first-meet thường bắt đầu 1 quest — nếu quest đó đã Active/Done thì sang repeat
            string qid = firstMeet.startQuestOnEnd;
            if (qid != null)
            {
                var st = QuestManager.Instance.GetState(qid);
                if (st != QuestState.Locked) return repeat != null ? repeat : firstMeet;
            }
        }
        return firstMeet != null ? firstMeet : repeat;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
