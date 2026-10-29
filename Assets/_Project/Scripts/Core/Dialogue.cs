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
    public string giveItemOnEnd;          // itemId trao cho người chơi (ItemDatabase)
    public string setFlagOnEnd;           // world flag bật khi xong (mở cổng, đổi NPC...)

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

    // M1 — Mai trao Cuộn Lướt Gió sau q1, mở cổng bắc (LEVEL_DESIGN 3.1, 2.2)
    public static readonly DialogueBlock MaiReward = new DialogueBlock(
        "mai_reward",
        new DialogueLine("Mai", "Ông Tễ bảo tay kiếm con đã vững. Cầm lấy — cuộn này là của cha con để lại."),
        new DialogueLine("Mai", "Lướt Gió: chạy như gió qua khe nước, qua hố đất. Không có nó, rừng sẽ nuốt con."),
        new DialogueLine("Kiền", "Cổng bắc...?"),
        new DialogueLine("Mai", "Ta đã cho mở. Đi đi — trước khi trăng lên tới đỉnh."))
    { giveItemOnEnd = "scroll_dash", setFlagOnEnd = "gate:village_north" };

    public static readonly DialogueBlock MaiAfter = new DialogueBlock(
        "mai_after",
        new DialogueLine("Mai", "Nhớ: chỗ nào có lửa ấm thì trăng đỏ yếu đi. Nghỉ ở đống lửa khi cần."));

    // M1 — bảng gỗ ở cổng bắc
    public static readonly DialogueBlock SignNorthGate = new DialogueBlock(
        "sign_north",
        new DialogueLine("Bảng gỗ", "↑ Rừng Thảm Vàng. Cổng đóng từ khi trăng đỏ mọc — theo lệnh trưởng làng Mai."));

    // M2 — Bé Na trốn sau tảng đá (lore về Lam)
    public static readonly DialogueBlock NaFirst = new DialogueBlock(
        "na_first",
        new DialogueLine("Bé Na", "Anh Kiền! Em... em chạy theo con cừu rồi lạc mất đường về."),
        new DialogueLine("Bé Na", "Chị Lam ngày xưa hay dẫn em ra đây hát bài cúng trăng. Chị bảo trăng trắng là trăng hiền."),
        new DialogueLine("Bé Na", "Cầm cái này đi — mẹ dặn em đưa cho người canh làng."))
    { giveItemOnEnd = "potion_hp", setFlagOnEnd = "npc:na_met" };

    public static readonly DialogueBlock NaRepeat = new DialogueBlock(
        "na_repeat",
        new DialogueLine("Bé Na", "Em sẽ chờ ở đây tới khi anh dọn đường. Đừng để bọn mắt đỏ lại gần nhé..."));

    // M2 — bàn thờ rừng
    public static readonly DialogueBlock AltarStone = new DialogueBlock(
        "altar_forest",
        new DialogueLine("Bia đá", "\"Ba ngọn lửa canh ba phía. Thắp cùng lúc, đá sẽ nhường đường.\""));

    // M3 — Lão Câu chèo đò
    public static readonly DialogueBlock LaoCauFirst = new DialogueBlock(
        "laocau_first",
        new DialogueLine("Lão Câu", "Hừm, người làng Phong Nguyệt hả? Đầm này ba năm nay không ai dám qua."),
        new DialogueLine("Lão Câu", "Bọn mặc áo cúng đóng trại trên đảo giữa đầm. Chúng giữ Chìa Khóa Tháp."),
        new DialogueLine("Lão Câu", "Hạ được kẻ cầm đầu thì quay lại đây — lão chở ngươi về làng bằng đò."))
    { setFlagOnEnd = "npc:laocau_met" };

    public static readonly DialogueBlock LaoCauRepeat = new DialogueBlock(
        "laocau_repeat",
        new DialogueLine("Lão Câu", "Bùn ở phía bắc sâu lắm. Đi chậm thì chết, đi nhanh thì... lướt qua."));

    // M5 — bích họa trong hang (lore Lễ Cúng Trăng 3 năm trước; móc DLC)
    public static readonly DialogueBlock CaveMuralLeft = new DialogueBlock(
        "cave_mural_left",
        new DialogueLine("Bích họa", "Một vòng người áo trắng quỳ quanh tháp. Trăng trắng treo giữa trời. Một cô bé cầm mặt nạ đứng giữa."),
        new DialogueLine("Kiền", "Lam... đây là lễ cúng trăng ba năm trước."));

    public static readonly DialogueBlock CaveMuralRight = new DialogueBlock(
        "cave_mural_right",
        new DialogueLine("Bích họa", "Phía sau cô bé có một bóng đen không mặt, tay đặt lên vai. Agis đứng cách đó rất xa, quay lưng."),
        new DialogueLine("Kiền", "Không phải Agis... vậy ai đã dẫn Lam đi?"))
    { setFlagOnEnd = "lore:cave_shadow" };

    // M4 — bia trước cầu thang tầng 2
    public static readonly DialogueBlock TowerInscription = new DialogueBlock(
        "tower_inscription",
        new DialogueLine("Bia khắc", "\"Kẻ đổi trăng lấy người — trăng sẽ đòi lại gấp đôi.\""),
        new DialogueLine("Kiền", "Agis... ông biết điều này mà vẫn làm."));

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
