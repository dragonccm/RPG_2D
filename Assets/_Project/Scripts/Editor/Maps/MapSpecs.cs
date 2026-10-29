using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 4 map của MVP theo LEVEL_DESIGN 3.1–3.4. Ký hiệu xem MapSpec. Sửa map = sửa lưới ở đây rồi chạy
/// Tools/RPG/Maps/Build All Maps (hoặc batch MapBuilder.BuildAll).
/// </summary>
public static class MapSpecs
{
    public static IEnumerable<MapSpec> All()
    {
        yield return Village();
        yield return Forest();
        yield return Marsh();
        yield return Tower();
        yield return Cave();
    }

    // ───────────────────────── M5 — HANG DƯỚI LÀNG (tuỳ chọn) ─────────────────────────
    public static MapSpec Cave()
    {
        var s = new MapSpec
        {
            sceneName = "M5_Cave",
            displayName = "Hang Dưới Làng",
            theme = MapSpec.Theme.Cave,
            seed = 53,
            clutterDensity = 0.06f,
            background = new Color(0.04f, 0.05f, 0.07f),
            globalLight = new Color(0.2f, 0.26f, 0.3f),
            globalIntensity = 0.55f,
            startSpawnId = "from_village",
            rows = new[]
            {
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~......................~~~~~~~~~",
                "~~~~~~~~~....u......C.....u....~~~~~~~~~",
                "~~~~~~~~~...#..............#...~~~~~~~~~",
                "~~~~~~~~~..........2...........~~~~~~~~~",
                "~~~~~~~~~......................~~~~~~~~~",
                "~~~~~~~~~...#..............#...~~~~~~~~~",
                "~~~~~~~~~.u..................u.~~~~~~~~~",
                "~~~~~~~~~......................~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~....~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~u...~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~....~~~~~~~~~~~~~~~~~~",
                "~~~..................................~~~",
                "~~~.u...........u......u.............~~~",
                "~~~..g...##..................##...g..~~~",
                "~~~...........1.....F................~~~",
                "~~~......................3...........~~~",
                "~~~......##..................##......~~~",
                "~~~...u............................u.~~~",
                "~~~........u................u........~~~",
                "~~~~~~~~~~~~~~~~~~....~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~...u~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~....~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~................~~~~~~~~~~~~",
                "~~~~~~~~~~~~..u.....X....u..~~~~~~~~~~~~",
                "~~~~~~~~~~~~................~~~~~~~~~~~~",
                "~~~~~~~~~~~~...u....S...u...~~~~~~~~~~~~",
                "~~~~~~~~~~~~................~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            },
        };
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Dust, 14, new Color(0.8f, 1f, 0.9f, 0.3f)));
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Fireflies, 10, new Color(0.45f, 1f, 0.75f)));
        s.portals.Add(new PortalDef
        {
            targetScene = "M1_Village", targetSpawnId = "from_cave", arrivalSpawnId = "from_village_cave",
            arrivalOffset = Vector2.up * 1.2f,
        });
        s.signs.Add(new SignDef { text = DialogueDatabase.CaveMuralLeft });
        s.signs.Add(new SignDef { text = DialogueDatabase.CaveMuralRight });
        s.chests.Add(new ChestDef { id = "cave_hoard", itemId = "potion_hp", count = 5 });
        s.encounters['1'] = new EncounterDef { id = "cave_rats_w", radius = 6f, enemies = { new EnemyDef("Rat", -1f, 0f), new EnemyDef("Rat", 1f, 0.6f), new EnemyDef("Rat", 0f, -1f), new EnemyDef("Rat", 1.4f, -0.7f) } };
        s.encounters['3'] = new EncounterDef { id = "cave_rats_e", radius = 6f, enemies = { new EnemyDef("Rat", -1f, 0f), new EnemyDef("Rat", 1f, 0.6f), new EnemyDef("Rat", 0f, -1f) } };
        s.encounters['2'] = new EncounterDef
        {
            id = "cave_elite", radius = 7f,
            enemies =
            {
                new EnemyDef("Lancer", 0f, 0f) { elite = true, eliteName = "Đá Gộp", tint = new Color(0.62f, 0.66f, 0.62f), scale = 1.3f },
                new EnemyDef("Rat", -2.5f, 1f), new EnemyDef("Rat", 2.5f, 1f),
            },
        };
        return s;
    }

    // ───────────────────────── M1 — LÀNG PHONG NGUYỆT ─────────────────────────
    public static MapSpec Village()
    {
        const string gate = "gate:village_north";
        var s = new MapSpec
        {
            sceneName = "M1_Village",
            displayName = "Làng Phong Nguyệt",
            theme = MapSpec.Theme.Village,
            seed = 11,
            clutterDensity = 0.11f,
            globalLight = new Color(0.33f, 0.36f, 0.55f),
            globalIntensity = 0.8f,
            rows = new[]
            {
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~,,X,,~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~,,,,,~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~T..T..T.GGGGG.T..T..T~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~T..........,,,,,.........T~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~T...####.....,,,,,....1.......T~~~~~~~~~~~~~",
                "~~~~~~~~~T....####....g,,,,,...............~~~~~~~~~~~",
                "~~~~~~~~T.....####.....,,,,,.....s.....b...T~~~~~~~~~~",
                "~~~~~~~~......####.....,,,,,...............~~~~~~~~~~~",
                "~~~~~~~T...............,,,,,.........#####..T~~~~~~~~~",
                "~~~~~~~..pp.p..pp..s...,,,,,.........#####...~~~~~~~~~",
                "~~~~~~~..p..pp..p......,,,,,.........#####...~~~~~~~~~",
                "~~~~~~~T..pp..p..pp....,,,,,.....b...........T~~~~~~~~",
                "~~~~~~~................,,,,,..................~~~~~~~~",
                "~~~~~~~T...H.....H.....,,,,,.....H......H.....~~~~~~~~",
                "~~~~~~~................,,,,,..................~~~~~~~~",
                "~~~~~~~..L.....,,,,,,,,,,,,,,,,,,,,,,.....L...~~~~~~~~",
                "~~~~~~~.......,,,,,,,,,,,,,,,,,,,,,,,,,.......vv,,,~~~",
                "~~~~~~~T......,,,,,,,,,,,,,,,,,,,,,,,,,.......vv,C,~~~",
                "~~~~~~~.......,,,N,,,,,,,,F,,,,,,,N,,,,.......vv,,,~~~",
                "~~~~~~~.......,,,,,,,,,,,,,,,,,,,,,,,,,.......~~~~~~~~",
                "~~~~~~~..L....,,,,,,,,,,,,,,,,,,,,,,,,,...ss..~~~~~~~~",
                "~~~~~~~T.......,,,,,,,,,,,,,,,,,,,,,,.....s...~~~~~~~~",
                "~~~~~~~...H.....H.....,,,,,.....W.....H.......~~~~~~~~",
                "~~~~~~~...............,,,,,..................X~~~~~~~~",
                "~~~~~~~T..b...........,,,,,......L.........T..~~~~~~~~",
                "~~~~~~~~..............,,,,,..................~~~~~~~~~",
                "~~~~~~~~T..####.......,,,,,.............b....~~~~~~~~~",
                "~~~~~~~~...####.......,,,,,..............T..~~~~~~~~~~",
                "~~~~~~~~~~%~~~~T......,,S,,.........T.....~~~~~~~~~~~~",
                "~~~~~~~~,,X,~~~~~T....,,,,,......T.....~~~~~~~~~~~~~~~",
                "~~~~~~~~~,,~~~~~~~~T..,,,,,..T....~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            },
        };
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Fireflies, 14, new Color(1f, 0.9f, 0.5f)));
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Dust, 10, new Color(0.9f, 0.9f, 1f, 0.35f)));
        for (int i = 0; i < 5; i++) s.flagGates.Add(gate);
        s.signs.Add(new SignDef { text = DialogueDatabase.SignNorthGate });
        s.portals.Add(new PortalDef
        {
            targetScene = "M2_Forest", targetSpawnId = "from_village", arrivalSpawnId = "from_forest",
            arrivalOffset = Vector2.down, requiredFlag = gate, startQuest = "q2",
            lockedMessage = "Cổng bắc đóng — hãy gặp trưởng làng Mai.",
        });
        s.portals.Add(new PortalDef   // bến đò: Lão Câu chở về sau khi hạ thủ lĩnh đầm
        {
            targetScene = "M3_Marsh", targetSpawnId = "from_ferry", arrivalSpawnId = "from_marsh_ferry",
            arrivalOffset = Vector2.left, requiredFlag = "clear:marsh_boss",
            lockedMessage = "Bến đò vắng — chưa có ai chèo qua đầm.",
        });
        s.portals.Add(new PortalDef   // hang dưới làng (mở bằng Hỏa Kích phá đá nứt)
        {
            targetScene = "M5_Cave", targetSpawnId = "from_village", arrivalSpawnId = "from_cave",
            arrivalOffset = Vector2.up * 1.2f,
        });
        s.npcs.Add(new NpcDef
        {
            name = "Mai", pawnColor = "Purple",
            first = DialogueDatabase.MaiFirstMeet, repeat = DialogueDatabase.MaiRepeat,
            reward = DialogueDatabase.MaiReward, rewardAfterQuest = "q1",
        });
        s.npcs.Add(new NpcDef
        {
            name = "Ong_Te", pawnColor = "Red",
            quest = DialogueDatabase.OngTeQuest, repeat = DialogueDatabase.OngTeRepeat,
            hideWhenFlag = "quest:q3:started",   // dân làng đóng cửa khi trăng đỏ lan tới
        });
        s.ambienceShifts.Add(new AmbienceShift { flag = "quest:q3:started", color = new Color(0.52f, 0.22f, 0.3f), intensity = 0.7f });
        s.ambienceShifts.Add(new AmbienceShift { flag = "boss:agis", color = new Color(0.7f, 0.72f, 0.85f), intensity = 0.95f });
        s.chests.Add(new ChestDef { id = "village_islet", itemId = "potion_hp", count = 2 });   // qua khe bằng Lướt Gió
        s.encounters['1'] = new EncounterDef
        {
            id = "village_sheep", radius = 7f,
            enemies = { new EnemyDef("Sheep", -1f, 0f), new EnemyDef("Sheep", 1.5f, 0.5f) },
        };
        return s;
    }

    // ───────────────────────── M2 — RỪNG THẢM VÀNG ─────────────────────────
    public static MapSpec Forest()
    {
        var s = new MapSpec
        {
            sceneName = "M2_Forest",
            displayName = "Rừng Thảm Vàng",
            theme = MapSpec.Theme.Forest,
            seed = 23,
            clutterDensity = 0.14f,
            background = new Color(0.2f, 0.45f, 0.5f),
            globalLight = new Color(0.22f, 0.32f, 0.33f),
            globalIntensity = 0.7f,
            startSpawnId = "from_village",
            arenaLockEncounter = "forest_boss",
            rows = new[]
            {
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~,,X,,~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~,,,,,~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~vvvvv~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~vvvvv~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~TT.,,,,,.TTTTTTT~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~T....,,,.......TTT~~~~~~~~~~~~~~TTTTTTT~~~~~~~~~~~~~~~~",
                "~~~~T#####,,,#####...TT~~~~~~~~~~~TTT......TTT~~~~~~~~~~~~~~",
                "~~~~T#...........#....TTTTTTTTTTTT..b........TT~~~~~~~~~~~~~",
                "~~~~T#...........#.................T.....k...TT~~~~~~~~~~~~~",
                "~~~~T#.....5.....#....T...................N..TT~~~~~~~~~~~~~",
                "~~~~T#...........#....T..TT......T.......T....T~~~~~~~~~~~~~",
                "~~~~T#...........#....T..TT.....3...........TT~~~~~~~~~~~~~~",
                "~~~~T####AAAA#####....TT.............TT....TT~~~~~~~~~~~~~~~",
                "~~~~TTT..,,,,..TT......TT....T..........b.TT~~~~~~~~~~~~~~~~",
                "~~~~~TT..,F,,..T........T......TT..TT....TT~~~~~~~~~~~~~~~~~",
                "~~~~~~T..,,,,..b........TT.....TT..TT...TT~~~~~~~~~~~~~~~~~~",
                "~~~~~~TT..,,,......4......TT..............T~~~~~~~~~~~~~~~~~",
                "~~~~~~~T..,,,..............TTTT,,,TTTTTTTTT~~~~~~~~~~~~~~~~~",
                "~~~~~~~TT.,,,..TT....T.......T,,^,,T~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~T.,,,..TT..O...g...O..T,,,,T~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~T.,,,.......,,,,,,,....T,^,T~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~T.,,,.......,,C,,,.....T,,,,T~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~TT,,,.......,,,,,,..O..T,^,,T~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~T,,,..TT...........b..T,,,,T~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~T,,,,,,,,,,,,,,,,,,,,,,,,^,TT~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~TT..,,,,,,,,,,,,,,,,,,,,,,,..TT~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~T......TT.....2......TT......TT~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~T..TT..TT............TT...TT...T~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~TT.TT....1.....r.........TT....TT~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~T.........................b...T~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~TT...........,,,,,..........TT~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~TTT..b......,,F,,......TTTT~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~TTTT......,,S,,....TTT~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~TTTTTT,,X,,TTTT~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            },
        };
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Fireflies, 26, new Color(0.7f, 1f, 0.45f)));
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Leaves, 12, new Color(0.85f, 0.7f, 0.3f)));
        s.portals.Add(new PortalDef
        {
            targetScene = "M3_Marsh", targetSpawnId = "from_forest", arrivalSpawnId = "from_marsh",
            arrivalOffset = Vector2.down * 0.6f,
        });
        s.portals.Add(new PortalDef
        {
            targetScene = "M1_Village", targetSpawnId = "from_forest", arrivalSpawnId = "from_village_back",
            arrivalOffset = Vector2.up,
        });
        s.signs.Add(new SignDef { text = DialogueDatabase.AltarStone });
        s.npcs.Add(new NpcDef
        {
            name = "Be_Na", pawnColor = "Yellow",
            first = DialogueDatabase.NaFirst, repeat = DialogueDatabase.NaRepeat,
        });
        s.torches = new TorchDef { id = "forest_altar", window = 8f };
        s.chests.Add(new ChestDef { id = "forest_altar", itemId = "scroll_charge", requireFlag = "torches:forest_altar" });
        s.encounters['1'] = new EncounterDef
        {
            id = "forest_e1", radius = 6f,
            enemies = { new EnemyDef("Goblin", -1f, 0.5f), new EnemyDef("Goblin", 1.2f, -0.3f) },
        };
        s.encounters['2'] = new EncounterDef
        {
            id = "forest_e2",
            enemies = { new EnemyDef("Archer", 0f, 1.5f), new EnemyDef("Sheep", -2f, 0f) },
        };
        s.encounters['3'] = new EncounterDef
        {
            id = "forest_e3",
            enemies = { new EnemyDef("Goblin", -1f, 0f), new EnemyDef("Sheep", 1f, 1f), new EnemyDef("Sheep", 1.5f, -1f) },
        };
        s.encounters['4'] = new EncounterDef
        {
            id = "forest_e4",
            enemies = { new EnemyDef("Goblin", -1.5f, 0f), new EnemyDef("Archer", 1.5f, 1f), new EnemyDef("Goblin", 0f, -1.2f) },
        };
        s.encounters['5'] = new EncounterDef
        {
            id = "forest_boss", radius = 5.5f,
            enemies =
            {
                new EnemyDef("Goblin", 0f, 0.5f) { elite = true, eliteName = "Thợ Săn Bóng", rewardItemId = "scroll_teleport" },
                new EnemyDef("Archer", -3f, 1.5f), new EnemyDef("Archer", 3f, 1.5f),
            },
        };
        return s;
    }

    // ───────────────────────── M3 — ĐẦM LAN SƯƠNG ─────────────────────────
    public static MapSpec Marsh()
    {
        var s = new MapSpec
        {
            sceneName = "M3_Marsh",
            displayName = "Đầm Lan Sương",
            theme = MapSpec.Theme.Marsh,
            seed = 37,
            clutterDensity = 0.16f,
            background = new Color(0.16f, 0.46f, 0.43f),
            waterTint = new Color(0.58f, 0.72f, 0.64f),
            globalLight = new Color(0.27f, 0.25f, 0.42f),
            globalIntensity = 0.7f,
            startSpawnId = "from_forest",
            rows = new[]
            {
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~t...M...t~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~t..........t~~~~~~~~~~~~~~~~~~~~~~~~~~~~#####~~~~~~~~~~~~",
                "~~~~~b..,,X,,....~~~~~~~~~~~~~~~~~~~~~~~~~~~~#...#~~~~~~~~~~~~",
                "~~~~~t..,,,,,..t.~~~~~~~~~~~~~~~~~~~~~~~~~~~~#.C.#~~~~~~~~~~~~",
                "~~~~~~r..,,,..b..~~~~~~~~~~~~~~~~~~~~~~~~~~~~#...#~~~~~~~~~~~~",
                "~~~~~~~~.,,,..F.~~~~~~~~~~~~t.t~~~~~~~~~~~~~~##R##~~~~~~~~~~~~",
                "~~~~~~~t.,,,....~~~~~~~~~~~.....~~~~~~~~~~~~t.....t~~~~~~~~~~~",
                "~~~~~~~..,,,.mmmm~~~~~~~~.........~~~~~~~~...........~~~~~~~~~",
                "~~~~~~~..,,,mmmmmmm,,,,,,....6.....,,,,,,,,..O...b...~~~~~~~~~",
                "~~~~~~~t.,,,.mmmmm~~~~~~..........~~~~~~~~.........r.~~~~~~~~~",
                "~~~~~~~.b.,,...m.t.~~~~~~t.....t~~~~~~~~~~~..t.......~~~~~~~~~",
                "~~~~~~~~..,,........~~~~~~~...~~~~~~~~~~~~~..,,.O....~~~~~~~~~",
                "~~~~~~~~t.,,....r...~~~~~~~~~~~~~~~t.~~~~~~..,,......~~~~~~~~~",
                "~~~~~~~~~.,,.....P..~~~~~~~~~~~~~~...~~~~~~..,,..C.t.~~~~~~~~~",
                "~~~~~~~~~.,,..b.....~~~~~~~~~~~~~~~~~~~~~~~..,,......~~~~~~~~~",
                "~~~~~~~~~.,,...5....~~~~~~t.~~~~~~~~~~~~~~~O.,,..t...~~~~~~~~~",
                "~~~~~~~~~.,,........~~~~~~~~~~~~~~~~~~~~~~~..,,......~~~~~~~~~",
                "~~~~~~~~t.,,.....t..~~~~~~~~~~~~~~~~~~~~~~~..,,...b..~~~~~~~~~",
                "~~~~~~~~..,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,..b...~~~~~~~~~",
                "~~~~~~~~..,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,......~~~~~~~~~",
                "~~~~~~~~t..b....mmmmm.....4.......mmmm.........t.....~~~~~~~~~",
                "~~~~~~~~~......mmmmmmmm.....r....mmmmmm...N..X.......~~~~~~~~~",
                "~~~~~~~~~~..t....mmmm~~~~~~~%~~~~~~mm~~~~~~~...t....~~~~~~~~~~",
                "~~~~~~~~~~.......~~~~~~~~~~,,,~~~~~~~~~~~~~~.......~~~~~~~~~~~",
                "~~~~~~~~~~..3...~~~~~t~~~~,,,,,~~~~~~t~~~~~~..2....~~~~~~~~~~~",
                "~~~~~~~~~~......~~~~~~~~~~,,,,,~~~~~~~~~~~~~.......~~~~~~~~~~~",
                "~~~~~~~~~~.t..b.,,,,,,,,,,,,,,,,,,,,,,,,,,,,,..t...~~~~~~~~~~~",
                "~~~~~~~~~~......,,,,,,,,,,,,,F,,,,,,,,,,,,,,,......~~~~~~~~~~~",
                "~~~~~~~~~~~t......1....r.......S........X..b....~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            },
        };
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Fog, 18, new Color(0.75f, 0.8f, 0.95f, 0.22f)));
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Fireflies, 14, new Color(0.75f, 0.5f, 1f)));
        s.portals.Add(new PortalDef
        {
            targetScene = "M4_Tower", targetSpawnId = "from_marsh", arrivalSpawnId = "from_tower",
            arrivalOffset = Vector2.down, requiredItemId = "key_tower",
            lockedMessage = "Cổng tháp khoá kín — cần Chìa Khóa Tháp Trăng Đỏ.",
        });
        s.portals.Add(new PortalDef   // đò Lão Câu về làng — mở sau khi hạ thủ lĩnh đầm
        {
            targetScene = "M1_Village", targetSpawnId = "from_marsh_ferry", arrivalSpawnId = "from_ferry",
            arrivalOffset = Vector2.left, requiredFlag = "clear:marsh_boss",
            lockedMessage = "Lão Câu: \"Hạ thằng cầm đầu trên đảo rồi lão chở về.\"",
        });
        s.portals.Add(new PortalDef
        {
            targetScene = "M2_Forest", targetSpawnId = "from_marsh", arrivalSpawnId = "from_forest_back",
            arrivalOffset = Vector2.up,
        });
        s.npcs.Add(new NpcDef
        {
            name = "Lao_Cau", pawnColor = "Blue",
            first = DialogueDatabase.LaoCauFirst, repeat = DialogueDatabase.LaoCauRepeat,
        });
        s.chests.Add(new ChestDef { id = "marsh_rune", itemId = "potion_hp", count = 3 });                 // sau rào rune (Hư Không)
        s.chests.Add(new ChestDef
        {
            id = "marsh_key", itemId = "key_tower", questId = "q3", objectiveId = "q3_key",
            requireFlag = "torches:marsh_lights",
        });
        s.switches.Add(new SwitchDef { id = "marsh_old_switch", questId = "q3", objectiveId = "q3_switch" });
        s.torches = new TorchDef { id = "marsh_lights", window = 6f };
        s.encounters['1'] = new EncounterDef { id = "marsh_e1", enemies = { new EnemyDef("Rat", -1f, 0f), new EnemyDef("Rat", 1f, 0.5f), new EnemyDef("Rat", 0f, -1f), new EnemyDef("Rat", 1.2f, -0.8f), new EnemyDef("Rat", -1.2f, 0.9f) } };
        s.encounters['2'] = new EncounterDef { id = "marsh_e2", enemies = { new EnemyDef("Archer", 0f, 1f), new EnemyDef("Lancer", -1.5f, -0.5f) } };
        s.encounters['3'] = new EncounterDef { id = "marsh_e3", enemies = { new EnemyDef("Archer", 0f, 0f), new EnemyDef("Goblin", 1.5f, 1f) } };
        s.encounters['4'] = new EncounterDef { id = "marsh_e4", enemies = { new EnemyDef("Lancer", -1f, 0f), new EnemyDef("Rat", 1.5f, 0.5f), new EnemyDef("Rat", 1f, -1f), new EnemyDef("Rat", -0.5f, 1.2f) } };
        s.encounters['5'] = new EncounterDef { id = "marsh_e5", enemies = { new EnemyDef("Lancer", 0f, 0f), new EnemyDef("Archer", 2f, 1f) } };
        s.encounters['6'] = new EncounterDef
        {
            id = "marsh_boss", radius = 6f,
            enemies =
            {
                new EnemyDef("Lancer", 0f, 0f) { elite = true, eliteName = "Nhà Chiến Thuật Kích" },
                new EnemyDef("Archer", -2.5f, 1.5f), new EnemyDef("Archer", 2.5f, 1.5f),
            },
        };
        return s;
    }

    // ───────────────────────── M4 — THÁP TRĂNG ĐỎ (2 tầng) ─────────────────────────
    public static MapSpec Tower()
    {
        var s = new MapSpec
        {
            sceneName = "M4_Tower",
            displayName = "Tháp Trăng Đỏ",
            theme = MapSpec.Theme.Tower,
            seed = 41,
            clutterDensity = 0.05f,
            background = new Color(0.16f, 0.04f, 0.07f),
            globalLight = new Color(0.55f, 0.2f, 0.24f),
            globalIntensity = 0.75f,
            startSpawnId = "from_marsh",
            runeDissolveFlag = "boss:agis",
            rows = new[]
            {
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~..............~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~.....#....#.....~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~..................~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~.........B........~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~..................~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~.....#....#.....~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~..............~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~...~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~...~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~.Y.~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~#####################################~~~~~~~~~~~~",
                "~~~~~~~#...................................#~~~~~~~~~~~~",
                "~~~~~~~#.....Z.Z.Z.......#.......#.....Y...#~~~~~~~~~~~~",
                "~~~~~~~#..^...........^..#...C...#.........#~~~~~~~~~~~~",
                "~~~~~~~#.....Z.8.Z.......#.......#...g.....#~~~~~~~~~~~~",
                "~~~~~~~#.................####R####.........#~~~~~~~~~~~~",
                "~~~~~~~#..^...Z.Z....^...........F.........#~~~~~~~~~~~~",
                "~~~~~~~##########......#####.....##########~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#......#~~~#.....#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#..D...#~~~#..D..#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#......#~~~#.....#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#..7...#####..9..#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#................#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#D..............D#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#................#~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~#######...S...####~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~#.......#~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~#..,X,..#~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~#########~~~~~~~~~~~~~~~~~~~~~~~~~",
                "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            },
        };
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Embers, 22, new Color(1f, 0.35f, 0.25f)));
        s.motes.Add(new MoteDef(AmbientMotes.Kind.Dust, 12, new Color(1f, 0.7f, 0.7f, 0.3f)));
        s.portals.Add(new PortalDef
        {
            targetScene = "M3_Marsh", targetSpawnId = "from_tower", arrivalSpawnId = "from_marsh",
            arrivalOffset = Vector2.up * 2f,
        });
        // 'Y' thứ tự đọc: đỉnh tháp (xuống) trước, tầng 1 (lên) sau
        s.stairs.Add(new StairDef { toSpawnId = "tower_f1_stair", arrivalSpawnId = "tower_top", arrivalOffset = Vector2.up });
        s.stairs.Add(new StairDef { toSpawnId = "tower_top", arrivalSpawnId = "tower_f1_stair", arrivalOffset = Vector2.down });
        s.signs.Add(new SignDef { text = DialogueDatabase.TowerInscription });
        s.chests.Add(new ChestDef { id = "tower_rune", itemId = "potion_hp", count = 3 });
        s.dartDirections.AddRange(new[] { Vector2.down, Vector2.down, Vector2.right, Vector2.left });
        s.encounters['7'] = new EncounterDef { id = "tower_hall_w", radius = 5f, enemies = { new EnemyDef("Lancer", 0f, 0f) { tint = new Color(0.55f, 0.45f, 0.7f) }, new EnemyDef("Goblin", 1.5f, 0.5f) } };
        s.encounters['9'] = new EncounterDef { id = "tower_hall_e", radius = 5f, enemies = { new EnemyDef("Lancer", 0f, 0f), new EnemyDef("Archer", 0f, 1.5f) } };
        s.encounters['8'] = new EncounterDef
        {
            id = "tower_candles", radius = 7f,
            enemies = { new EnemyDef("Lancer", 0f, 0f) { elite = true, eliteName = "Kỵ Sĩ Bóng Đêm", tint = new Color(0.42f, 0.32f, 0.55f), scale = 1.15f }, new EnemyDef("Archer", -3f, 1f), new EnemyDef("Archer", 3f, 1f) },
        };
        return s;
    }
}
