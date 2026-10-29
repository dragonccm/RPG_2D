using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S4 (GAME_MASTER_PLAN 9.2, 9.4): dữ liệu hội thoại.
/// MVP: database khai báo trong code (VN). S5+ chuyển sang .asset/JSON để sửa không đụng code.
/// Mỗi block: chuỗi line (speaker + text); onEnd: bắt đầu/ghi tiến độ quest.
/// </summary>
[Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;

    public DialogueLine(string speaker, string text) { this.speaker = speaker; this.text = text; }
}

[Serializable]
public class DialogueBlock
{
    public string blockId;
    public DialogueLine[] lines;
    public string startQuestOnEnd;        // ví dụ "q1"
    public string reportObjectiveOnEnd;   // "questId:objectiveId"
    public string claimQuestOnEnd;        // nhận thưởng quest

    public DialogueBlock(string blockId, params DialogueLine[] lines)
    {
        this.blockId = blockId;
        this.lines = lines;
    }
}

public static class DialogueDatabase
{
    // M1 — Mai (mở màn, plan 9.3)
    public static readonly DialogueBlock MaiFirstMeet = new DialogueBlock(
        "mai_first",
        new DialogueLine("Mai", "Kiền... trăng hôm nay đỏ lắm. Ông vẫn nhớ 3 năm trước chứ?"),
        new DialogueLine("Kiền", "Nhớ. Nghi thức bị bỏ dở — và những gì ở ngoài rừng đêm qua không phải sói thường."),
        new DialogueLine("Mai", "Ra gặp Ông Tễ trước đi. Rồi... con quyết định. Làng này không đợi nổi một ai khác."))
    { startQuestOnEnd = "q1" };

    public static readonly DialogueBlock MaiRepeat = new DialogueBlock(
        "mai_repeat",
        new DialogueLine("Mai", "Ông Tễ ở đầu làng. Đừng để ông chờ lâu — ông già rồi nhưng vẫn càu nhàu lắm."));

    // Ông Tễ — hoàn thành Q1 (khi player luyện combo xong)
    public static readonly DialogueBlock OngTeQuest = new DialogueBlock(
        "ongte_quest",
        new DialogueLine("Ông Tễ", "Ồ, Kiền đấy à. Vũ khí của ngươi đã sẵn sàng — ta vừa mài lại lưỡi đêm qua."),
        new DialogueLine("Ông Tễ", "Đi thử một lượt combo trên sân cho chắc tay. Rồi theo dấu mà vào rừng."))
    { reportObjectiveOnEnd = "q1:talk_ongte", claimQuestOnEnd = "q1" };

    public static readonly DialogueBlock OngTeRepeat = new DialogueBlock(
        "ongte_repeat",
        new DialogueLine("Ông Tễ", "Rừng Thảm Vàng đêm nay đỏ rực. Cẩn thận — bóng ở đó không phải bóng người."));
}
