using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mô tả 1 map cho MapBuilder (LEVEL_DESIGN 3). Bản đồ là lưới ASCII, hàng đầu = phía bắc.
///
/// Địa hình: '.' cỏ · ',' cát/đường đất · '~' nước (tháp: vực) · '#' khối đá nhô (vách, không đi được)
///           'v' khe vực (FallHazard — Lướt Gió bay qua) · 'm' bùn (SlowZone)
/// Vật thể:  'T' cây lớn · 't' cây nhỏ · 'b' bụi · 'r' đá · 'H' nhà · 'W' tháp canh · 's' bù nhìn
///           'p' bí ngô · 'g' bảng gỗ (đọc được) · 'k' bảng đầu lâu · 'L' cột đèn
/// Hang:     'u' nấm phát sáng (đèn xanh lục)
/// Gameplay: 'S' điểm bắt đầu · 'F' đống lửa · 'C' rương · '^' bẫy gai · 'P' cần gạt · 'O' đuốc câu đố
///           'D' tượng bắn tên · '%' đá nứt · 'R' rào rune · 'G' rào gỗ theo flag · 'A' rào khoá arena
///           'Z' sàn sụp · 'N' NPC · 'X' cổng sang map khác · 'Y' cầu thang trong map · '1'..'9' nhóm địch
///           'B' boss Agis · 'M' tháp trăng đỏ (mốc)
/// Danh sách theo loại (Chests, Npcs, Portals...) được gán theo THỨ TỰ ĐỌC (trái→phải, trên→dưới).
/// </summary>
public class MapSpec
{
    public enum Theme { Village, Forest, Marsh, Tower, Cave }

    public string sceneName;
    public string displayName;
    public Theme theme;
    public string[] rows;
    public int seed = 1;
    public float clutterDensity = 0.1f;

    public Color background = new Color(0.28f, 0.66f, 0.68f);
    public Color waterTint = Color.white;
    public Color globalLight = new Color(0.16f, 0.18f, 0.29f);
    public float globalIntensity = 0.45f;
    public List<MoteDef> motes = new List<MoteDef>();
    public string startSpawnId = GameFlow.StartSpawn;

    public List<ChestDef> chests = new List<ChestDef>();
    public List<NpcDef> npcs = new List<NpcDef>();
    public List<PortalDef> portals = new List<PortalDef>();
    public List<StairDef> stairs = new List<StairDef>();
    public List<SignDef> signs = new List<SignDef>();
    public List<string> flagGates = new List<string>();        // 'G' theo thứ tự: flag mở rào
    public Dictionary<char, EncounterDef> encounters = new Dictionary<char, EncounterDef>();
    public List<SwitchDef> switches = new List<SwitchDef>();
    public List<Vector2> dartDirections = new List<Vector2>(); // 'D' theo thứ tự
    public TorchDef torches;                                   // mọi 'O' thuộc 1 câu đố
    public string runeDissolveFlag = "";
    public string arenaLockEncounter = "";                     // 'A' khoá theo encounter này
    public string agisEncounterFlag = "";
    public List<AmbienceShift> ambienceShifts = new List<AmbienceShift>();

    public int Width => rows[0].Length;
    public int Height => rows.Length;
}

public class MoteDef
{
    public AmbientMotes.Kind kind;
    public int count;
    public Color color;
    public MoteDef(AmbientMotes.Kind k, int n, Color c) { kind = k; count = n; color = c; }
}

public class ChestDef
{
    public string id, itemId, questId = "", objectiveId = "";
    public int count = 1;
    public string requireFlag = ""; // rương chỉ hiện khi flag bật (vd giải câu đố)
}

public class NpcDef
{
    public string name, pawnColor = "Blue";
    public DialogueBlock first, quest, repeat, reward;
    public string rewardAfterQuest = "";
    public string showWhenFlag = "";
    public string hideWhenFlag = "";   // NPC rời đi khi flag bật (dân làng đóng cửa)
}

public class PortalDef
{
    public string targetScene, targetSpawnId, arrivalSpawnId;
    public Vector2 arrivalOffset = Vector2.down;
    public string requiredItemId = "", requiredFlag = "", lockedMessage = "Lối này chưa mở.";
    public string startQuest = "";
}

public class StairDef
{
    public string toSpawnId, arrivalSpawnId;
    public Vector2 arrivalOffset = Vector2.down;
}

public class SignDef
{
    public DialogueBlock text;
}

public class EncounterDef
{
    public string id;
    public float radius = 9f;
    public List<EnemyDef> enemies = new List<EnemyDef>();
    public string reportClear = "";
}

public class EnemyDef
{
    public string prefab;          // "Goblin" | "Archer" | "Lancer" | "Sheep"
    public Vector2 offset;
    public bool elite;
    public string eliteName = "", rewardItemId = "";
    public Color tint = Color.white;   // biến thể: nhuộm màu để phân biệt (vd Kỵ Sĩ Bóng Đêm)
    public float scale = 1f;
    public EnemyDef(string p, float x, float y) { prefab = p; offset = new Vector2(x, y); }
}

public class SwitchDef
{
    public string id, questId = "", objectiveId = "";
}

public class TorchDef
{
    public string id = "torches";
    public float window = 6f;
    public string questId = "", objectiveId = "";
    public string revealChestId = ""; // rương hiện ra khi giải xong
}

public class AmbienceShift
{
    public string flag;
    public Color color;
    public float intensity;
}
