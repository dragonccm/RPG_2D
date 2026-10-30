using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kiểm tra MapSpec theo chuẩn LEVEL_DESIGN (mục 0, 5, 6): kích thước, 1 điểm bắt đầu, đống lửa, mọi mục tiêu
/// tới được từ điểm bắt đầu (giả định đủ kỹ năng/flag), nhóm địch không kích hoạt ngay chỗ xuất hiện,
/// mật độ chi tiết ≥ 12 / màn hình, cổng sang map khác trỏ đúng điểm xuất hiện, danh sách khớp ký hiệu.
/// Trả về danh sách vi phạm (rỗng = đạt). Chạy trong test EditMode và khi MapBuilder.BuildAll.
/// </summary>
public static class MapDesignCheck
{
    private const string Solid = "~#TtbrHWspgkLuDOP";     // chặn đi bộ hoàn toàn
    private const string Props = "TtbrHWspgkLu";           // vật thể trang trí đếm vào mật độ
    private const float ScreenTiles = 17.8f * 10f;          // 1 màn hình camera (ortho 5, 16:9)

    public static List<string> Check(MapSpec s, IReadOnlyDictionary<string, MapSpec> all)
    {
        var errors = new List<string>();
        void Err(string m) => errors.Add($"{s.sceneName}: {m}");

        int W = s.rows.Max(r => r.Length), H = s.rows.Length;
        if (W < 36 || W > 80 || H < 28 || H > 66) Err($"kích thước {W}×{H} ngoài khoảng 36–80 × 28–66 tile");
        foreach (var r in s.rows) if (r.Length != s.rows[0].Length) { Err("các hàng không cùng độ dài"); break; }

        char At(int x, int y) => x < 0 || y < 0 || x >= W || y >= H || x >= s.rows[y].Length ? '~' : s.rows[y][x];
        var cells = new Dictionary<char, List<Vector2Int>>();
        int ground = 0, props = 0;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                char c = At(x, y);
                if (!cells.TryGetValue(c, out var l)) cells[c] = l = new List<Vector2Int>();
                l.Add(new Vector2Int(x, y));
                if (c != '~' && c != 'v') ground++;
                if (Props.IndexOf(c) >= 0) props++;
            }
        List<Vector2Int> Get(char c) => cells.TryGetValue(c, out var l) ? l : new List<Vector2Int>();

        // 1 điểm bắt đầu, có đống lửa
        if (Get('S').Count != 1) Err($"cần đúng 1 điểm bắt đầu 'S' (có {Get('S').Count})");
        if (Get('F').Count < 1) Err("thiếu đống lửa 'F' (checkpoint)");

        // danh sách khớp ký hiệu
        if (Get('X').Count > s.portals.Count) Err($"{Get('X').Count} cổng 'X' nhưng chỉ có {s.portals.Count} PortalDef");
        if (Get('N').Count > s.npcs.Count) Err($"{Get('N').Count} NPC 'N' nhưng chỉ có {s.npcs.Count} NpcDef");
        if (Get('C').Count > s.chests.Count) Err($"{Get('C').Count} rương 'C' nhưng chỉ có {s.chests.Count} ChestDef");
        if (Get('g').Count > s.signs.Count) Err($"{Get('g').Count} bảng 'g' nhưng chỉ có {s.signs.Count} SignDef");
        if (Get('Y').Count > s.stairs.Count) Err($"{Get('Y').Count} cầu thang 'Y' nhưng chỉ có {s.stairs.Count} StairDef");
        if (Get('G').Count > s.flagGates.Count) Err($"{Get('G').Count} rào 'G' nhưng chỉ có {s.flagGates.Count} flag");
        if (Get('D').Count > s.dartDirections.Count) Err($"{Get('D').Count} tượng 'D' nhưng chỉ có {s.dartDirections.Count} hướng");
        if (Get('O').Count > 0 && s.torches == null) Err("có đuốc 'O' nhưng thiếu TorchDef");
        for (char d = '1'; d <= '9'; d++)
            if (Get(d).Count > 0 && !s.encounters.ContainsKey(d)) Err($"nhóm địch '{d}' chưa có EncounterDef");
        foreach (var kv in s.encounters)
            if (Get(kv.Key).Count != 1) Err($"EncounterDef '{kv.Key}' phải có đúng 1 ô trên bản đồ (có {Get(kv.Key).Count})");
        var chestIds = s.chests.Select(c => c.id).ToList();
        if (chestIds.Count != chestIds.Distinct().Count()) Err("ChestDef trùng id");

        // kết nối: BFS từ S, giả định đủ kỹ năng (khe vực, đá nứt, rào rune, rào flag/arena không chặn)
        if (Get('S').Count == 1)
        {
            var start = Get('S')[0];
            var dist = Bfs(start, W, H, At);
            // Cầu thang dịch chuyển giữa 2 tầng: nếu tới được 1 cầu thang thì mọi cầu thang (và tầng kia) đều tới được
            var stairs = Get('Y');
            if (stairs.Any(p => dist.ContainsKey(p)))
                foreach (var st in stairs.Where(p => !dist.ContainsKey(p)).ToList())
                    foreach (var kv in Bfs(st, W, H, At)) if (!dist.ContainsKey(kv.Key)) dist[kv.Key] = 0;
            foreach (char c in "XYNCPOFDgB123456789")
                foreach (var p in Get(c))
                    if (!dist.ContainsKey(p) && !AdjacentReached(p, dist))
                        Err($"'{c}' tại ({p.x},{p.y}) không tới được từ điểm bắt đầu");

            // nhóm địch không được kích hoạt ngay chỗ xuất hiện
            foreach (var kv in s.encounters)
            {
                if (Get(kv.Key).Count != 1) continue;
                float d = Vector2.Distance(start, Get(kv.Key)[0]);
                if (d < kv.Value.radius + 2f) Err($"nhóm '{kv.Key}' ({kv.Value.id}) cách điểm bắt đầu {d:0.0} < bán kính kích hoạt {kv.Value.radius}+2");
            }

            // mỗi nhóm địch/boss phải trong tầm đi bộ ≤ ~90s (≈ 300 tile) từ điểm bắt đầu hoặc 1 đống lửa
            foreach (var kv in s.encounters)
                if (Get(kv.Key).Count == 1 && dist.TryGetValue(Get(kv.Key)[0], out int steps) && steps > 300)
                    Err($"nhóm '{kv.Key}' cách điểm bắt đầu {steps} bước (> 300)");
        }

        // mật độ chi tiết: ≥ 12 / màn hình trên phần đất
        float clutter = ground * s.clutterDensity * 0.55f; // clutter thực dựng ≈ 55% mật độ danh nghĩa (loại ô bị chiếm)
        float density = (props + clutter) / Mathf.Max(1f, ground) * ScreenTiles;
        if (density < 12f) Err($"mật độ chi tiết {density:0.0} / màn hình < 12 (props {props}, clutter ≈ {clutter:0})");

        // cổng: map đích tồn tại + có điểm xuất hiện tương ứng
        foreach (var p in s.portals)
        {
            if (all == null || !all.TryGetValue(p.targetScene, out var target)) { Err($"cổng tới '{p.targetScene}' — map không tồn tại"); continue; }
            if (!SpawnIds(target).Contains(p.targetSpawnId)) Err($"cổng tới {p.targetScene} dùng spawn '{p.targetSpawnId}' mà map đó không có");
        }
        foreach (var st in s.stairs)
            if (!SpawnIds(s).Contains(st.toSpawnId)) Err($"cầu thang trỏ spawn '{st.toSpawnId}' không có trong map");
        return errors;
    }

    /// <summary>Mọi spawn id mà 1 map tạo ra: điểm bắt đầu + điểm xuất hiện cạnh mỗi cổng/cầu thang.</summary>
    public static HashSet<string> SpawnIds(MapSpec m)
    {
        var ids = new HashSet<string> { m.startSpawnId };
        foreach (var p in m.portals) ids.Add(p.arrivalSpawnId);
        foreach (var st in m.stairs) ids.Add(st.arrivalSpawnId);
        return ids;
    }

    private static bool AdjacentReached(Vector2Int p, Dictionary<Vector2Int, int> dist)
    {
        foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            if (dist.ContainsKey(p + d)) return true;
        return false;
    }

    private static Dictionary<Vector2Int, int> Bfs(Vector2Int start, int W, int H, System.Func<int, int, char> at)
    {
        var dist = new Dictionary<Vector2Int, int> { [start] = 0 };
        var q = new Queue<Vector2Int>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var n = p + d;
                if (n.x < 0 || n.y < 0 || n.x >= W || n.y >= H || dist.ContainsKey(n)) continue;
                char c = at(n.x, n.y);
                bool blocked = Solid.IndexOf(c) >= 0 && c != 'v';
                if (c == 'v' || c == '%' || c == 'R' || c == 'G' || c == 'A' || c == 'Z' || c == 'O' || c == 'P' || c == 'D') blocked = false;
                if ("NCX".IndexOf(c) >= 0) blocked = false; // đích đến: chỉ cần chạm tới ô kề
                if (blocked) continue;
                dist[n] = dist[p] + 1;
                q.Enqueue(n);
            }
        }
        return dist;
    }
}
