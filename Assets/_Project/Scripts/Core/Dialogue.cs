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

    // M4 — Agis trước trận boss (plan 9.3)
    public static readonly DialogueBlock AgisPreBoss = new DialogueBlock(
        "agis_pre_boss",
        new DialogueLine("Agis", "Ngươi cũng đến cướp vầng trăng à? Được... Nhưng trăng đêm nay là của con gái ta!"),
        new DialogueLine("Kiền", "Tôi đến để đưa ông và con bé trở về làng — chứ không phải để cướp nó."),
        new DialogueLine("Agis", "Đã quá muộn rồi... Sức mạnh của Trăng Đỏ không thể đảo ngược!"))
    { startQuestOnEnd = "q4" };

    // M4 — Agis sau trận boss (kết cục tha thứ)
    public static readonly DialogueBlock AgisEndingForgive = new DialogueBlock(
        "agis_ending_forgive",
        new DialogueLine("Agis", "Ta... ta đã lạc lối sao? Lam... con vẫn ở đây ư?"),
        new DialogueLine("Kiền", "Hãy nắm lấy tay con bé. Làng Phong Nguyệt vẫn luôn mở rộng cửa đón ông trở về."),
        new DialogueLine("Agis", "Cảm ơn ngươi... Vầng trăng đỏ... cuối cùng cũng đã tắt."))
    { claimQuestOnEnd = "q4" };

    // M4 — Agis sau trận boss (kết cục hủy diệt)
    public static readonly DialogueBlock AgisEndingDestroy = new DialogueBlock(
        "agis_ending_destroy",
        new DialogueLine("Kiền", "Ác mộng của ông... chấm dứt tại đây."),
        new DialogueLine("Agis", "Vầng trăng vỡ tan rồi... Nhưng ký ức này... ngươi sẽ phải mang theo suốt đời..."))
    { claimQuestOnEnd = "q4" };
}
