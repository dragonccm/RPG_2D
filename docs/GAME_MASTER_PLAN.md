# GAME MASTER PLAN — RPG "TRĂNG ĐỎ" (RPG_Game_Current)

> Plan tổng thể chi tiết cho game 2D RPG top-down Unity 6000.6.7f1 / URP — bám vào code hiện có (97 script), gồm: sửa lỗi, nền tảng kỹ thuật, thiết kế màn chơi/map/gameplay, nhân vật, cây kỹ năng, cốt truyện, hội thoại, hiệu năng, và các chi tiết nhỏ nhất (đổ bóng, chuẩn hoá vector đường chéo).

**Phiên bản:** 1.0 — 29/10/2026
**Scene hiện tại:** `Assets/_Project/Scenes/World.unity`
**Ngôn ngữ nội dung:** Tiếng Việt (UI + dialogue), dễ bổ sung EN sau qua hệ thống localization.

---

## MỤC LỤC

- [Phần 0 — Tầm nhìn & nguyên tắc thiết kế](#phần-0)
- [Phần 1 — Hiện trạng dự án (audit)](#phần-1)
- [Phần 2 — P0: Sửa lỗi & nền tảng kỹ thuật](#phần-2)
- [Phần 3 — P1: Polish nền gameplay](#phần-3)
- [Phần 4 — Màn chơi & Map](#phần-4)
- [Phần 5 — Gameplay loop & hệ thống chiến đấu](#phần-5)
- [Phần 6 — Nhân vật & phát triển nhân vật](#phần-6)
- [Phần 7 — Cây kỹ năng](#phần-7)
- [Phần 8 — Kẻ địch & Boss](#phần-8)
- [Phần 9 — Cốt truyện & hội thoại](#phần-9)
- [Phần 10 — Quest & tiến trình](#phần-10)
- [Phần 11 — UI/UX & âm thanh](#phần-11)
- [Phần 12 — Hiệu năng & tối ưu](#phần-12)
- [Phần 13 — Yêu cầu tối thiểu (MVP + cấu hình máy)](#phần-13)
- [Phần 14 — Các yếu tố cần chú ý (pitfalls)](#phần-14)
- [Phần 15 — Roadmap & tiêu chí nghiệm thu](#phần-15)

---

<a name="phần-0"></a>
## PHẦN 0 — TẦM NHÌN & NGUYÊN TẮC THIẾT KẾ

**Pitch 1 câu:** Một warrior đơn độc bước ra khỏi ngôi làng trong đêm vầng trăng đỏ, xuyên qua rừng và đầm, leo lên Tháp Trăng Đỏ đối mặt với pháp sư Agis — với bộ khung kỹ năng Dash/Teleport/Charge Attack làm chất chơi chính.

**3 trụ cột cảm xúc (pillars):**
1. **"Nhẹ như gió"** — di chuyển phải mượt tuyệt đối: chuẩn hoá vector chéo, accel/decel có kiểm soát, không stutter (AI đã tối ưu chống stutter — giữ nguyên chất lượng đó cho player).
2. **"Đọc được nhịp trận đấu"** — mọi đòn đánh của địch đều có telegraph (vòng sáng, rung, bóng đổ), người chơi thắng bằng phản xạ + chọn đúng skill, không phải bằng sốc số liệu.
3. **"Phát triển có trọng lượng"** — mỗi điểm skill tree phải đổi cách chơi nhìn thấy được (Dash xuyên địch, Charge giật camera...), không +5% ẩn.

**Quyết định thiết kế đã chốt:**
- **Góc nhìn:** top-down 2.5D (tilemap có elevation — đã có `World/Elevation_Entry.cs` + `Elevation_Exit.cs`).
- **Chiều dài MVP:** ~30–45 phút trải nghiệm (1 làng + 2 vùng hoang + tháp + boss).
- **Thể loại chuyển động:** real-time action, WASD + chuột nhắm hướng, hotkey skill 1–4/Q/E/Shift (khớp `UniversalHotkeyManager.cs` + `special_move.cs` hiện có: Shift=Dash, Q=Teleport, E=Charge).
- **Không multiplayer** ở mọi giai đoạn của plan này.
- **Platform đích:** PC (WebGL build để share nhanh — module Web đã cài).

---

<a name="phần-1"></a>
## PHẦN 1 — HIỆN TRẠNG DỰ ÁN (AUDIT 29/10/2026)

### 1.1 Đã có sẵn (tái sử dụng, không viết lại)
| Hệ thống | File chính | Trạng thái |
|---|---|---|
| Player di chuyển + flip + SmoothDamp | `Controllers/PlayerController.cs` | ✅ hoạt động — cần polish (mục 3.1) |
| Di chuyển đặc biệt Dash/Teleport/Charge | `Controllers/special_move.cs` | ✅ có — cần kiểm tra chuẩn hoá vector (mục 3.1) |
| Camera follow | `Controllers/CameraFollow.cs` | ✅ — thêm shake (đã có `Core/CameraShake.cs`) |
| Targeting | `Controllers/TargetingSystem.cs` | ✅ |
| AI State Machine (Idle/Patrol/Chase/Attack/Reposition/Dead/ReturnToAnchor) | `AI/States/*` | ✅ đã tối ưu performance (throttling 0.2–0.5s) |
| 4 loại AI: Melee / Ranged / Support / Boss | `AI/MeleeEnemyAI.cs`, `RangedEnemyAI.cs`, `SupportEnemyAI.cs`, `EnemyBoss.cs` | ✅ |
| Enemy group + formation + patrol route | `AI/EnemyGroupManager.cs`, `EnemyGroupFormationManager.cs`, `AIPatrolRoute.cs` | ✅ |
| Boss Agis + 4 executor (cận chiến/tầm xa/hỗ trợ/vùng ảnh hưởng) | `Boss/NewSystem/*`, `Boss/agis_wizzar.cs` | ✅ đang trong scene |
| Kỹ năng slot + hotkey + module | `Core/SkillSlot.cs`, `Core/UniversalHotkeyManager.cs`, `Core/ModularSkillManager.cs`, `Core/UnifiedSkillSystem.cs` + `Data/SkillModule.cs` | ✅ (UnifiedSkillSystem có bản `_DEPRECATED` cần dọn) |
| Hiệu ứng 2D + chí mạng | `Core/CombatEffectsManager.cs`, `Core/EnhancedEffectManager.cs`, `Core/Effect2DManager.cs`, `Core/VisualEffects.cs` | ✅ |
| UI: health bar, skill panel, damage indicator, enemy UI, cursor | `UI/*` (14 file) | ✅ |
| Elevation 2.5D | `World/Elevation_Entry.cs`, `Elevation_Exit.cs` | ✅ |
| NavMesh 2D | `NavMeshPlus-master/` | ✅ (đã patch Unity 6) |

### 1.2 Đã sửa xong (29/10/2026)
- 13 lỗi compile NavMeshPlus (`GetInstanceID()` → `GetEntityId()`, `s_SelectedID` → `EntityId`).
- Lỗi YAML `.meta`, dòng 1 `special_move.cs` hỏng, duplicate class `special_move`.
- Recompile: **0 errors, 0 warnings**. Play Mode 60s: **0 runtime exception** từ game.

### 1.3 Nợ kỹ thuật còn lại
| # | Vấn đề | Mức độ |
|---|---|---|
| T1 | `ProjectAnalysisReport.md` + `ScriptCleanupReport.md` **lỗi encoding** (mở bằng TIS-620 sai) — tài liệu không đọc được | Thấp |
| T2 | `Debug/` 4 script dính scene/tool test (`EnemyProjectileTest`, `QuickSkillSystemTest`, `SkillItemDebugTool`, `SkillSystemDiagnostic`) — phải chắc chắn không vào build | Trung bình |
| T3 | `Deprecated/UnifiedSkillSystem_DEPRECATED.cs` + README phình — code chết | Trung bình |
| T4 | Unity Editor-internal bug `Style.catalog` (GameView) — không thuộc game, chỉ note | Không fix được |
| T5 | Unity Connect 401 + Curl 35 — môi trường/dịch vụ Unity | Thấp |
| T6 | Tài liệu README.md mô tả cấu trúc **không khớp thực tế** (nói Managers/Systems/Tutorial/Utils — không tồn tại) | Thấp |
| T7 | **Chưa có hệ thống bóng (shadow)** — 0 kết quả grep `shadow` trong Scripts | Trung bình → mục 3.2 |
| T8 | Chưa có: save/load, inventory, quest, dialogue, XP/level, sound manager | Lõi MVP → phần 5–11 |

---

<a name="phần-2"></a>
## PHẦN 2 — P0: SỬA LỖI & NỀN TẢNG KỸ THUẬT (tuần 1)

> Nguyên tắc: **không dựng nội dung mới khi nền còn lỗi.** Mỗi mục có tiêu chí nghiệm thu riêng.

### 2.1 Quy trình an toàn đã thiết lập
- Build lại editor state bằng `unity recompile --project-path /home/dragonccm/RPG_Game_Current` trước mỗi lần commit code.
- Smoke test Play Mode 60s qua CLI (`EditorApplication.isPlaying`) sau mỗi thay đổi logic — đếm exception trong `Logs/Editor.log` phải = 0 (trừ bug Unity-internal đã liệt kê).
- Không sửa scene bằng tay khi đang Play Mode.

### 2.2 Danh sách việc P0 — **✅ HOÀN THÀNH 29/10/2026**
> Kết quả thực tế: recompile **0 errors / 0 warnings**; EditMode tests **8/8 xanh**; Play Mode smoke 60s **0 exception**; đã commit `8ec5653`+`8ec565e`. Ghi chú: P0-1 thực thi bằng cách xoá hẳn thư mục `Debug/` (4 script lỗi encoding, không được tham chiếu); P0-2 xoá `Deprecated/`; thêm asmdef `RPG.Game` (+Editor/Tools) & `RPG.Tests.EditMode` (P0-6 cần).
| # | Việc | Chi tiết | Nghiem thu |
|---|---|---|---|
| P0-1 | Tách code Debug ra khỏi build | Thêm assembly definition cho `Debug/` (chỉ Editor) hoặc guard `#if UNITY_EDITOR` + `#if DEVELOPMENT_BUILD` | Build WebGL không chứa 4 script Debug; scene chạy không sinh log test |
| P0-2 | Dọn `Deprecated/` + `UnifiedSkillSystem` trùng | Xác nhận scene chỉ tham chiếu bản mới (grep GUID), xoá bản cũ + README chết | Grep class deprecated = 0; recompile sạch |
| P0-3 | Regenerate tài liệu encoding hỏng | Viết lại `ProjectAnalysisReport.md`/`ScriptCleanupReport.md` bằng UTF-8 (nội dung rút gọn còn có giá trị) hoặc xoá | File mở đọc được, đúng UTF-8 |
| P0-4 | Cập nhật README.md khớp cấu trúc thật | Sửa cây thư mục, phiên bản Unity 6000.6.7f1 (không phải 2021.3), bỏ .NET 4.7.1 badge sai | README khớp 100% thực tế |
| P0-5 | Khóa guideline .meta | Cộng tác viên/script không được sửa `.meta` thủ công; bổ sung script CI check `git diff` cảnh báo `.meta` GUID đổi | Không còn YAML warning khi mở editor |
| P0-6 | PlayMode test framework | 3 test `UnityTest` qua `com.unity.test-framework` (đã cài): (1) player di chuyển đúng tốc độ mọi hướng, (2) player không tự nhận damage từ skill của chính mình, (3) enemy spawner spawn đủ số | `unity test` chạy xanh |
| P0-7 | Version control | `git init` (nếu chưa) + `.gitignore` chuẩn Unity (Library/, Temp/, Logs/, UserSettings/), commit baseline sạch | `git status` sạch sau baseline |

---

<a name="phần-3"></a>
## PHẦN 3 — P1: POLISH NỀN GAMEPLAY (tuần 1–2)

> Đây là phần "chi tiết nhỏ nhất" user yêu cầu — làm trước nội dung, vì ảnh hưởng cảm giác chơi toàn cục.

### 3.1 ⚙️ Chuẩn hoá vector đường chéo (diagonal speed) — **ưu tiên cao nhất**
**Hiện trạng đã kiểm chứng:** `PlayerController.cs:219` có `movement.normalized * moveSpeed` — hướng đúng; **nhưng** bọc trong `Vector2.SmoothDamp(rb.position, targetPosition, ...)` — SmoothDamp nội suy **từng trục riêng biệt**, khiến gia tốc theo chéo lệch pha trục X/Y → tốc độ chéo thực tế ≠ tốc độ trục. Ngoài ra cần rà 3 chỗ khác:
1. `special_move.cs` — `PerformDash`, `PerformTeleport`, `PerformChargeAttack` có dùng `transform.up/right` hay vector thô chưa normalize.
2. `AI/EnemyMovementController.cs` + `States/ChaseState.cs` — enemy đi chéo phải cùng chuẩn (địch nhanh hơn player 41% khi chéo = gameOver cảm giác).
3. Animation blend — nếu có blend tree theo `movement.x/y` thô, phải dùng bản normalized làm tham số.

**Kế hoạch sửa:**
- Tạo `Core/MovementMath.cs` — 1 nguồn duy nhất:
  ```csharp
  public static class MovementMath {
      // Diagonal-safe: vector luôn có magnitude <= 1
      public static Vector2 ClampDiagonal(Vector2 raw) {
          if (raw.sqrMagnitude > 1f) return raw.normalized; // giữ analog stick nhẹ
          return raw;
      }
  }
  ```
- Player: `movement = MovementMath.ClampDiagonal(new Vector2(moveX, moveY));` — thay SmoothDamp position per-axis bằng tăng tốc mượt theo **vector chuẩn hoá** (`moveSpeed * accelCurve`), hoặc giữ SmoothDamp trên `currentVelocity` vector (không phải position).
- Enemy/AI: cùng helper; test bằng PlayMode test đo `rb.velocity.magnitude` khi chéo = magnitude trục (sai số < 1%).
- **Nghiệm thu:** test tự động 8 hướng (0°, 45°, 90°...315°) — tốc độ sai số ≤ 1%; chơi tay không cảm giác "trượt xiên nhanh".

### 3.2 🌑 Hệ thống đổ bóng (shadow)
**Hiện trạng:** không có bóng — nhân vật "trôi" trên nền.
**Thiết kế (đủ đẹp, rẻ nhất):** **blob shadow** — sprite ellip áp-gradient, không dùng realtime shadow của URP (tiết kiệm GPU, chuẩn thể loại top-down).
- 1 prefab `ShadowBlob`: `SpriteRenderer` (đen, alpha ~0.35, sort layer `Shadows` ngay trên nền tilemap, dưới mọi thực thể).
- Script `Core/BlobShadow.cs` (`[RequireComponent(typeof(SpriteRenderer))]`, `[ExecuteAlways]`):
  - Attach vào Player + mọi Enemy/Boss/prop lớn (cây, tháp).
  - `LateUpdate()`: raycast/cast xuống floor layer, đặt bóng tại điểm đất; **scale theo độ cao** (khi nhảy/dash lên elevation, bóng nhỏ + mờ dần → cảm giác độ cao thật).
  - Không xoay theo flip — bóng luôn ellipse nằm ngang.
- Elevation: kết hợp `Elevation_Entry/Exit` — bóng đổi scale theo `characterHeight`.
- **Nghiệm thu:** mọi thực thể có bóng ổn định, bóng nhỏ/mờ khi lên cao, **0 raycast mỗi frame trên static props** (chỉ cần 1 lần đặt khi spawn).

### 3.3 Depth-sorting & layer chuẩn
- Thiết lập Sorting Layer cố định: `Background < Ground < Shadows < Props < Characters < Effects < UI-WORLD`.
- Trong `Characters`: **y-sort** (Transform Sorting Fudge hoặc script gán `sortingOrder = - Mathf.RoundToInt(pos.y * 100)`) — player đi sau/en trên cây, đứng trước/en dưới.
- Tilemap: dùng Tilemap Renderer mode `Individual` cho tile có phần nổi.

### 3.4 Camera & cảm giác đánh đòn
- `CameraFollow` + dead-zone nhẹ; camera lerp theo hướng input trước 10% (look-ahead).
- Hit-stop 40–60ms khi chí mạng (mở rộng `CameraShake.cs` thành `HitFeedback`: shake + hit-stop + flash trắng enemy 1 frame).
- Screen-space vignette đỏ khi HP < 25%.

### 3.5 Input chuẩn hoá
- Chuyển dần sang `com.unity.inputsystem` (đã cài) — Input Actions asset: Move (WASD/arrow), Skill1–4, Dash/Teleport/Charge, Interact (F), Pause (Esc).
- Rebind + lưu `PlayerPrefs` (đã có `SimpleHotkeyChanger.cs` — hợp nhất vào 1 hệ thống).

### 3.6 Danh sách P1 đầy đủ
| # | Việc | Ước lượng |
|---|---|---|
| P1-1 | MovementMath + sửa Player/Enemy/special_move (3.1) | 1 ngày |
| P1-2 | BlobShadow + wiring mọi thực thể (3.2) | 1 ngày |
| P1-3 | Sorting layer + y-sort toàn cảnh (3.3) | 0.5 ngày |
| P1-4 | Hit-stop + shake + flash (3.4) | 0.5 ngày |
| P1-5 | Input Actions + rebind (3.5) | 1 ngày |
| P1-6 | Damage numbers (đã có `NearbyHealthDisplay`) — polish kiểu jump+fade, crit to màu vàng | 0.5 ngày |
| P1-7 | Footstep/impact particle cơ bản (dust khi dash, vị khi trúng) | 1 ngày |

---

<a name="phần-4"></a>
## PHẦN 4 — MÀN CHƠI & MAP

### 4.1 Cấu trúc màn chơi (MVP: 5 màn + 2 phụ)
| # | Màn | Loại | Mục đích chơi | Boss/địch chốt |
|---|---|---|---|---|
| M1 | **Làng Phong Nguyệt** (hub) | An toàn, hội thoại, shop cơ bản | Học di chuyển/tương tác, nhận quest đầu | — |
| M2 | **Rừng Thảm Vàng** | Chặn đường: nhóm địch patrol, cheap trap | Dạy combat cơ bản + Dash (tutorial hóa qua gameplay) | Mini-boss: **Thợ Săn Bóng** (1 MeleeElite + 2 Support) |
| M3 | **Đầm Lan Sương** | Khu vực nguy hiểm: enemy tầm xa + shielder | Dạy đúng kỹ năng tầm xa + Reposition | Mini-boss: **Nhà Chiến Thuật Kích** (RangedElite, telegraph toàn map) |
| M4 | **Đỉnh Tháp Trăng Đỏ (2 tầng)** | Đứng yên không được: phòng nhảy tầng, gauntlet | Tổng hợp mọi kỹ năng + elevation | **Boss cuối: Agis** (3 phase) |
| M5 | **Hang Dưới Làng** (phụ, optional) | Loot + lore + 1 route tắt về tháp | Thưởng người khám phá | Elite Đá Gộp |
| M6 | **Giấc Mộng Trăng Đỏ** (phụ, unclock sau khi ăn thua boss 1 lần) | Modifier run (giống roguelite nhẹ) | Replay value | Ngẫu nhiên |

**Mỗi map = 1 scene additive** (nạp/đỡ qua Addressables — đã cài), mở rộng sau không phá scene cũ.

### 4.2 Quy chuẩn map (áp dụng cho từng map)
- **Tilemap layers:** `Floor`, `Decoration`, `Collision`, `Elevation`. Tile 16px (khớp asset hiện có: FarmLand/Grass/Path/Fences), vẽ 1 scene ~60×40 tile.
- **Mật độ địch:** trung bình 1 nhóm/35–50 giây đi; mỗi nhóm 2–4 con; **mở khóa cửa/chướng ngại khi nhóm chết** → clear có trọng lượng.
- **Điểm hứng (beats) mỗi map:** 1 chướng ngại dạy cơ chế → 1 kho báu cảm ơn → 1 mini-boss/boss.
- **Cửa nghỉ (checkpoint):** đống lửa — hồi máu full + save (mục 5.5).
- **Readability:** enemy tầm xa phải hiện vòng telegraph trước khi bắn 0.6s (dùng sẵn `UI/DamageAreaIndicator.cs`).

### 4.3 Bản đồ tổng thể (vị trí các vùng nối nhau)
```
        [M5 Hang Dưới Làng]
               |
[Làng Phong Nguyệt M1] --[cổng bắc]--> [Rừng Thảm Vàng M2]
               |                              |
        [nhà chữa cháy]                [đầm lan sương M3]
                                              |
                                    [Tháp Trăng Đỏ M4: tầng 1 → tầng 2]
```

---

<a name="phần-5"></a>
## PHẦN 5 — GAMEPLAY LOOP & HỆ THỐNG CHIẾN ĐẤU

### 5.1 Vòng lặp lõi (30 giây)
`Quan sát (telegraph địch) → Di chuyển/Dodge (Dash/Teleport) → Phản đòn (Combo + Charge) → Ăn reward (vật liệu/level) → Đặt mục tiêu tiếp theo`

### 5.2 Điều khiển (khớp code hiện có)
| Input | Hành động | Ghi chú |
|---|---|---|
| WASD / Arrow | Di chuyển | Chuẩn hoá chéo (3.1) |
| Chuột trái / J | Đánh thường (combo 3 nhịp) | Hướng theo chuột |
| **Left Shift** | **Dash** (i-frame nếu bật) | `special_move.cs` |
| **Q** | **Teleport** tới điểm nhắm (giới hạn tầm, không xuyên tường) | `special_move.cs` |
| **E** | **Charge Attack** (giữ để tích, thả) | `special_move.cs` |
| 1–4 | Skill slot (cây kỹ năng mở thêm) | `SkillSlot.cs` + `UniversalHotkeyManager` |
| F | Tương tác (hội thoại/lửa/chuông) | |
| Tab | Skill tree / bảng chỉ số | |
| Esc | Pause | |

### 5.3 Combat numbers khung (điều chỉnh qua Playtest)
- Player: HP 100, moveSpeed 3.5 (khớp code), dash: 15 speed / 0.3s / cooldown 1s / i-frame có — (khớp `special_move.cs` mặc định).
- Charge: 12 speed / 4m / 50 dmg / knockback 10 / cooldown 1.5s.
- Enemy cơ bản: HP 30–60, damage 8–15; Elite: HP 150, damage 20; Boss: HP 1200, 3 phase.
- Crit: 10% ×2.0 (mở được lên 25% ×2.5 qua cây).
- Đòn địch luôn có wind-up 0.4–0.7s + telegraph.

### 5.4 Trạng thái nhân vật (state player)
`Normal → Dash → Charge → Stunned → Dead`; một script `PlayerStateMachine` nhỏ thay cờ bool rải rác (`isBusy`, `canMove` hiện ở `PlayerController`) — giảm bug tương tranh giữa skill.

### 5.5 Save/Load (bắt buộc cho MVP)
- JSON save tại `Application.persistentDataPath`: vị trí, HP/MP, level/XP, skill tree đã mở, vật phẩm, quest flag, checkpoint gần nhất.
- Save chỉ tại đống lửa + auto-save lúc qua màn; **1 slot + 1 backup** (ghi 2 file chống hỏng).

### 5.6 Inventory MVP (tối giản)
- 12 slot tiêu hảo: hồi máu (3 loại), buff dmg/tốc, mảnh key mở cửa tháp.
- Không gear/drop rác trong MVP — gear để v2.

---

<a name="phần-6"></a>
## PHẦN 6 — NHÂN VẬT & PHÁT TRIỂN NHÂN VẬT

### 6.1 Diễn bảng nhân vật chính
| Nhân vật | Vai | Tính cách | Arc (phát triển) |
|---|---|---|---|
| **Kiền — Warrior** (player) | Người chơi | Trầm lặng, ít nói nhưng quyết liệt; cựu lính canh làng | Tin vào gươm → hiểu "mạnh thật là bảo vệ được người khác" (mỗi màn 1 mốc) |
| **Mai — Trưởng làng** | Quest giver, lưu điểm cảm xúc | Dịu dàng, lo lắng, giấu bí mật về Agis | Từ "con về đi" → tự mình cầm vũ khí hộ làng ở M4 |
| **Ông Tễ — Thợ rèn** | Shop + upgrade vũ khí | Cộc cằn, dí dỏm | Khóa: chỉ nâng vũ khí sau mỗi mini-boss |
| **Agis — Boss cuối** | Pháp sư hoá vầng trăng đỏ | Không ác quỷ: tuyệt vọng vì mất con gái | Đối thoại 2 trước trận: người chơi có thể chọn **tha** hoặc **hủy diệt** (ảnh hưởng ending) |
| **Cô Bé Bóng (Lam)** | NPC du hành cùng Agis (lore) | Im lặng, chỉ hiện trong vài frame | Bật mí lối vào hang M5 |

### 6.2 Chỉ số phát triển (level 1→20)
| Chỉ số | Base | Mỗi cấp | Nguồn tăng thêm |
|---|---|---|---|
| HP | 100 | +8 | Cây: +25/nút (3 nút) |
| Damage | 10 | +1.5 | Cây + Ông Tễ (nâng vũ khí) |
| Move speed | 3.5 | — | Cây: 1 nút +0.25 |
| Crit chance/dmg | 10% / 2.0 | — | Cây: tới 25% / 2.5 |
| Dash cooldown | 1.0s | — | Cây: −0.15s/nút (2 nút) |
| Skill point | — | 1 điểm/cấp (max 19) + 3 điểm thưởng từ quest | |

XP curve: cần `60 × level^1.35` XP (L1→2 = 60 XP; L19→20 ≈ 3.050 XP). Số liệu config trong `ScriptableObject` `CharacterStatsSO` — không hard-code trong class.

### 6.3 Trang phục/biến thể (cosmetic, không ảnh hưởng số)
3 skin Warrior: lính làng (mặc định) → giáp Thợ rèn (nhiệm vụ M3) → dạ phục Mặt Trăng (hoàn thành M6).

---

<a name="phần-7"></a>
## PHẦN 7 — CÂY KỸ NĂNG

> 3 nhánh gắn với 3 kỹ năng đặc biệt hiện có (Dash/Teleport/Charge) + 1 nhánh thống quát. Render: UI cây scroll (đã có `UI/SkillPanelUI.cs` + `SkillItemPrefabHandler.cs`).

### 7.1 Cấu trúc cây (tổng 19 điểm tối đa; mỗi nhánh 6 nút)
```
                      [Gốc: Kiền Thức Tỉnh — mở Tab tree]
      ┌────────────────────┬───────────────────┬──────────────────┐
   NHÁNH GIÓ (Dash)   NHÁNH KHÔNG (Teleport)  NHÁNH LỬA (Charge)   NHÁNH THÂN (Passive)
   1 Gió Lạnh         1 Bước Ngắn             1 Sốc Nhiệt          1 Thể Lực (+25 HP)
   2 Lướt Xuyên       2 Đổi Vị                2 Cháy Lan           2 Cường Kích (+1.5 dmg)
     (dash xuyên địch)  (teleport qua tường mỏng)(charge gây burn 3s)  2 Mũi Nhọn (+5% crit)
   3 Kích Ứng         3 Phân Thân             3 Trấn Áp            3 Bóng Nhẹ (dash cd −0.15s)
     (dash gây 20 dmg)  (tạo bản sao dụ địch 2s) (charge stun 1s)    3 Dự Bị (i-frame khi hồi máu)
   4 Đại Kỹ:          4 Đại Kỹ:               4 Đại Kỹ:            4 Đại Kỹ:
     CUỒNG GIÓ          HOÁ BÓNG ẤM             THIÊN PHỤC           BẤT DIỆT THÂN
     (đọc: 5s dash      (teleport cluster       (charge nổ diện      (5s không chết,
      không cd)          3 điểm liên tiếp)        rộng)                hồi 30% HP)
```
- Mỗi nút: 1 điểm; nút tier 3–4 yêu cầu level tối thiểu (6 / 12).
- **Nguyên tắc thiết kế:** nút nào cũng đổi hành vi nhìn thấy (list "hiệu ứng trong game" ở cột phải mỗi nút trong UI), không +5% ẩn.

### 7.2 Skill dùng chung hệ thống hiện có
- Mỗi nút = 1 `SkillModule` (ScriptableObject, `Data/SkillModule.cs`) + 1 `ISkillExecutor` — khớp `ModularSkillManager` hiện có → không cần viết hệ thống mới, chỉ thêm dữ liệu.

---

<a name="phần-8"></a>
## PHẦN 8 — KẺ ĐỊCH & BOSS

### 8.1 Bảng địch (khớp hệ thống AI hiện có: Melee/Ranged/Support/Boss + Elite/Shielder)
| Tên | Kiểu | HP | DMG | Mechanic chính | Xuất hiện |
|---|---|---|---|---|---|
| Sói Rừng | Melee | 30 | 8 | Tụ 2–3 con, gánh (flank) | M2 |
| Bọ Gai | Melee | 45 | 12 | Chụm thành quả cầu, hất tung khi dash trúng | M2 |
| Thợ Săn Bóng | Ranged | 40 | 10 | Bắn 3 mũi hình quạt, telegraph 0.6s | M2 (mini-boss variant Elite) |
| Đầm Chóp | Support | 55 | 6 | Hồi máu đồng đội 10 HP/2s (ưu tiên hạ trước) | M3 |
| Nhện Sương | Melee | 35 | 10 | Tơ chậm 30% 2s (gây slow) | M3 |
| Kẻ Đá Gộp | Melee Elite | 150 | 20 | Gánh trang bị khiên — phải Teleport ra sau | M5 (mini-boss) |
| Bóng Tầng Tháp | Melee/Ranged | 50 | 14 | Xe buýt tăng tốc theo tầng | M4 |
| **Agis** | **Boss** | 1200 | — | 3 phase (dưới) | M4 (boss cuối) |

### 8.2 Agis — 3 phase (đúng executor có sẵn)
| Phase | HP | Pattern | Executor code |
|---|---|---|---|
| P1 — Trăng non | 100%→70% | Gọi 2 kẻ địch mỗi 10s; bắn cầu lửa telegraph tròn (`BossTamXaExecutor`) | `BossTamXaExecutor.cs` |
| P2 — Trăng khuyết | 70%→35% | Thêm 2 khu ảnh hưởng lan (`BossVungAnhHuongExecutor`), dash lùi + hồi máu ít | `BossVungAnhHuongExecutor.cs` |
| P3 — **Trăng Đỏ** | 35%→0% | Combo cận chiến gấp đôi tốc độ + gọi hỗ trợ (healer) — phải hạ healer trước | `BossCanChienExecutor.cs` + `BossHoTroExecutor.cs` |
- Mỗi phase đổi: màu của trần + nhạc đệm nhanh hơn + camera shake nhẹ vào phase.

### 8.3 Telegraph chuẩn (yếu tố "đọc được")
- Mọi đòn AOE: vòng tròn/thanh hiện trước 0.5–0.8s, alpha nhấp nháy 2 lần, rồi nổ.
- Địch tạo telegraph bằng `UI/DamageAreaIndicator.cs` — quy ước mã màu: trắng=đòn thường, đỏ=chí mạng AOE, xanh=hỗ trợ đồng đội.

---

<a name="phần-9"></a>
## PHẦN 9 — CỐT TRUYỆN & HỘI THOẠI

### 9.1 Tóm tắt cốt truyện (3 hồi)
- **Hồi 1 (M1–M2):** Đêm trăng đỏ bất thường — làng Phong Nguyệt bị sói hoá dại tấn công. Kiền được Mai giao cho nhiệm vụ tìm Ông Tễ rèn lại gươm, rồi dõi theo dấu vết vào Rừng Thảm Vàng. Hé lộ: con gái Agis, Lam, biến mất 3 năm trước trong lễ cúng trăng.
- **Hồi 2 (M3–M4 tầng 1):** Sương đầm cho thấy nhóm "Chiến Thuật Kích" — tàn dư đội cúng trăng của Agis — đang tiến hành nghi thức thức tỉnh Tháp. Kiền hiểu Agis không hủy diệt làng vì ác, mà để "đổi" vầng trăng lấy con gái.
- **Hồi 3 (M4 tầng 2 + ending):** Trên đỉnh tháp, Agis (cùng bóng Lam) chuẩn bị hoàn tất nghi thức. Kiền đánh bại anh. **2 ending theo lựa chọn:**
  - **Tha + trao lại mặt nạ Lam:** trăng đỏ tắt, Lam tỉnh, làng thương Agis như một người bệnh. (ending chính)
  - **Hủy diệt:** trăng đỏ vỡ thành rắc lấp lánh, nhưng Kiền mang theo ánh mắt cuối của Agis. (ending "đắt giá")
- Ending mở cho DLC: "Ai đã lấy Lam 3 năm trước?" — bóng đen thoáng qua trong credits.

### 9.2 Hệ thống hội thoại
- **Định dạng:** ScriptableObject `DialogueSO` (node graph đơn giản): mỗi node = {speaker, text VN, choices[], next, onEndEvent}.
- **UI:** hộp thoại dưới màn, typing effect 30 ký tự/s, chuột/space để qua, lựa chọn bằng phím 1/2.
- **Quy tắc:** không quá 4 dòng chữ/hộp; NPC có 3 loại hội thoại: `FirstMeet`, `Repeat` (1 câu), `Quest`.
- **Camera cutscene:** khi boss xuất hiện — camera pan tới boss 1.5s + tên boss hiển thị (đã có camera follow + shake).

### 9.3 Kịch bản hội thoại mẫu (chốt trong doc, làm sau)
**M1 mở màn — Mai:**
> **Mai:** "Kiền... trăng hôm nay đỏ lắm. Ông vẫn nhớ 3 năm trước chứ?"
> **Kiền:** "Nhớ. Nghi thức bị bỏ dở — và những gì ở ngoài rừng đêm qua không phải sói thường."
> **Mai:** "Ra gặp Ông Tễ trước đi. Rồi... con quyết định. Làng này không đợi nổi một ai khác."
> *(nhiệm vụ mới: Gặp Ông Tễ)*

**M4 trước boss — Agis:**
> **Agis:** "Ngươi cũng đến lấy trăng à? Được. Nhưng trăng đêm nay là của con ta."
> **Kiền:** "Tôi đến trả lại nó cho con bé — chứ không phải cho ông."
> *(combat bắt đầu)*

**Post-boss (lựa chọn):**
> **[THA]** *(giữ F 3 giây)* — "Anh ta gục xuống, bóng dáng bé nhỏ bên cạnh dần rõ hình..."
> **[HỦY DIỆT]** *(đánh thêm 1 nhịp cuối)* — "...và trăng đỏ vỡ thành nghìn mảnh."

### 9.4 Dialogue data không code
Toàn bộ hội thoại nằm trong `Assets/_Project/Data/Dialogue/` dạng `.asset` hoặc JSON → có thể sửa story không đụng code; editor tool gõ hội thoại (Editor window đơn giản) — làm sau MVP.

---

<a name="phần-10"></a>
## PHẦN 10 — QUEST & TIẾN TRÌNH

| Quest | Màn | Trigger | Mục tiêu | Phần thưởng |
|---|---|---|---|---|
| Q1 "Gươm của người canh giữ" | M1 | Hội thoại Mai | Gặp Ông Tễ → luyện 1 đòn combo trong sân | Mở combo + 50 XP |
| Q2 "Bóng trong rừng" | M2 | Tự động sau cổng | Diệt 3 nhóm sói + mini-boss | Skill point 1 |
| Q3 "Tiếng chuông dưới sương" | M3 | Chuông đầm | Nhặt 3 mảnh chuông, diệt Nhện Sương | Mở Teleport tier 1 |
| Q4 "Tháp Trăng Đỏ" | M4 | Sau Q3 | Leo 2 tầng, hạ Agis | Ending + credit |
| Q5 (phụ) "Đèn của Bóng" | M5 | Thấy Lam trong hang | Đưa đèn về cho Mai | Skin dạ phục + lore |

Quest flag lưu trong save JSON; UI quest chỉ 1 panel gọn (đã có `UIManager`).

---

<a name="phần-11"></a>
## PHẦN 11 — UI/UX & ÂM THANH

### 11.1 UI (khung đã có: Fantasy Wooden GUI + `UI/*` 14 file)
- **HUD tối giản:** HP (trái trên), thanh charge (dưới HP), hotbar skill 4 ô (giữa dưới), minimap **KHÔNG** (map nhỏ — bỏ để giữ không khí), vàng/vật liệu (phải trên).
- **Màn hình:** Main Menu → Save/Continue → Settings (âm thanh + rebind phím) → Game → Pause → Death (Chạm lại lửa gần nhất) → Victory/Ending.
- Tất cả text qua **TextMesh Pro** (đã có) + font VN đủ dấu (Nunito/Be Vietnam Pro).
- UI Canvas hiện tại trong scene (`UI CANVAS`, `UI Canvas`) — **hợp nhất thành 1 canvas + sub-panels** tránh 2 canvas chồng.

### 11.2 Âm thanh
- **AudioMixer 3 nhóm:** Music / SFX / UI (nút chỉnh riêng trong Settings).
- BGM: 1 track/map + boss track riêng (P3 Agis chuyển tempo).
- SFX cần tối thiểu: đánh (3 biến thể), trúng đòn, dash (whoosh), teleport (bling), charge (ramp), chết địch, mở rương, checkpoint (lửa), UI click.
- Địch phát tiếng cảnh báo khi phát hiện player (0.3s) — phần của telegraph.

---

<a name="phần-12"></a>
## PHẦN 12 — HIỆU NĂNG & TỐI ƯU

### 12.1 Ngân sách khung (top-down 2D, target 60 FPS PC / 30 FPS WebGL cũ)
| Metric | Target |
|---|---|
| Frame CPU main | ≤ 8ms |
| Render | ≤ 5ms |
| Draw calls (khung cảnh điển hình) | ≤ 150 |
| Batches (SRP Batcher bật) | ≤ 50 |
| GC Alloc/frame (gameplay) | 0 B (trừ UI đổi text) |
| Enemy active tối đa / màn | 25 (PC), 15 (WebGL) |
| VFX particle cùng lúc | ≤ 300 |

### 12.2 Việc tối ưu cụ thể
1. **Object pooling bắt buộc:** projectile, damage number, VFX, teleport clone, enemy (dùng sẵn xu hướng `EnemySpawner`).
2. **AI throttling** hiện đã có (target 0.2s, patrol 0.3s) — **giữ nguyên**, chỉ thêm thông số vào `ScriptableObject` để chỉnh per-enemy.
3. **Blob shadow** thay realtime light shadow (mục 3.2) — bỏ tốn GPU Global Light 2D shadow.
4. **Texture:** đóng gói atlas theo map (Sprite Atlas); set `Max Size` 1024 cho art nhân vật, compression `High Quality` chỉ cho hero.
5. **Audio:** decode-on-demand cho music, force-to-mono cho SFX ngắn.
6. **Profiler cadence:** chạy Profiler vào cuối mỗi tuần — chụp 3 khung cảnh chuẩn (Làng 10 NPC, Rừng 20 enemy, Boss 3 phase) và lưu snapshot so sánh.
7. **WebGL:** strip engine code (`Managed Stripping Level: High`), texture ASTC, tắt crash handler.

---

<a name="phần-13"></a>
## PHẦN 13 — YÊU CẦU TỐI THIỂU

### 13.1 MVP scope (điều kiện "game chơi được từ đầu đến cuối")
- [x] Di chuyển chuẩn (không lệch chéo) + dash/teleport/charge
- [x] Combat: combo 3 nhịp + 4 skill + crit + damage number
- [ ] 5 map theo mục 4.1 + 2 mini-boss + boss Agis 3 phase
- [ ] Cây kỹ năng 19 nút (UI Tab)
- [ ] Dialogue M1 mở màn + M4 boss + ending chọn
- [ ] 5 quest (mục 10) + save/load đống lửa
- [ ] HUD + 7 SFX + 5 BGM loop
- [ ] Build WebGL chạy 30 FPS ổn định

### 13.2 Cấu hình máy tối thiểu (người chơi)
| Thành phần | Tối thiểu | Khuyến nghị |
|---|---|---|
| OS | Windows 10 64-bit / Ubuntu 22.04 / trình duyệt hiện đại | — |
| CPU | 2 nhân 2.0 GHz (Intel i3-5005U) | 4 nhân 3.0 GHz |
| RAM | 4 GB | 8 GB |
| GPU | Intel HD 4000 / WebGL2 | GTX 750 / bất kỳ Vulkan |
| Dung lượng | 500 MB (PC) / nạp WebGL ~120 MB | — |
| Input | Bàn phím + chuột | + Gamepad (sau MVP) |

### 13.3 Điều kiện môi trường dev
- Unity 6000.6.7f1 + URP 17.6.0 (khớp manifest).
- Build: `unity build /home/dragonccm/RPG_Game_Current --platform WebGL` (CLI) — script `Editor/BuildCommand.cs` đã có sẵn.
- Test: `unity test --project-path ...` mỗi PR/commit lớn.

---

<a name="phần-14"></a>
## PHẦN 14 — CÁC YẾU TỐ CẦN CHÚ Ý (PITFALLS)

1. **Vector chéo:** mọi nơi lấy input nhân tốc độ đều phải qua `MovementMath.ClampDiagonal` — kể cả dash/charge/knockback. Test 8 hướng tự động, không test tay rồi tin.
2. **SmoothDamp per-axis:** SmoothDamp từng trục làm cong quỹ đạo dash — dash phải di chuyển theo **đường thẳng chuẩn hoá**, không smooth per-axis.
3. **Bóng (shadow):** không dùng `Light2D` shadow cho nhân vật (tốn GPU + nhòe pixel art); blob sprite phải nằm sort layer `Shadows`, không flip theo hướng quay.
4. **Depth-sort:** quên y-sort cho.props có chiều cao (cây, cột) → nhân vật xuyên qua; chỉ sort trong layer `Props/Characters`, đừng sort tilemap nền.
5. **Duplicate code/system:** hiện tượng 2 bản skill system (UnifiedSkillSystem + Deprecated) — **mọi tính năng mới phải gắn vào 1 hệ thống duy nhất**; trước khi thêm file, grep GUID + tên class tránh trùng (đã bị 1 lần với `special_move`).
6. **Meta file:** không xoá `.meta` khi còn file `.cs` — GUID đổi làm mất tham chiếu scene/prefab; sửa `.meta` phải giữ GUID.
7. **Editor-internal bugs** (Style.catalog): phân biệt lỗi game vs lỗi Unity bằng stack trace (`UnityEditor.GameView...` = Unity); đừng cố "sửa" trong project.
8. **Play Mode test qua CLI:** không bấm Play tay khi editor đang bị CLI giữ; dùng `EditorApplication.isPlaying` qua `unity command script eval` — nhớ dừng sau test.
9. **AI stagger:** đã tối ưu — **không** thêm `SetDestination` mỗi frame mới trong state nào; dùng throttle của `EnemyMovementController`.
10. **ScriptableObject damage numbers:** mọi số damage/HP/XP vào `SO`, không hard-code — chỉnh cân bằng không cần build lại.
11. **Elevation:** entry/exit trigger phải đánh dấu layer `Elevation` riêng; camera không lerp qua — snap khi đổi tầng để không nhoè.
12. **Save:** JSON save phải version field (`saveVersion: 1`) để migrate tương lai; **không** lưu Transform Unity path — lưu scene + toạ độ.

---

<a name="phần-15"></a>
## PHẦN 15 — ROADMAP & TIÊU CHÍ NGHIỆM THU

### Giai đoạn tổng (10 tuần ước tính, 1 dev + AI hỗ trợ)
| Sprint | Tuần | Deliverable | Tiêu chí nghiệm thu |
|---|---|---|---|
| **S0 — Dọn nền** | 1 | P0-1…P0-7 (mục 2) | ✅ **XONG 29/10** — recompile 0/0, test 8/8, play smoke 0 exception |
| **S1 — Cảm giác chơi** | 2 | P1-1…P1-7 (mục 3) | ✅ **XONG 29/10** — test 8 hướng sai số 0% (8/8 xanh); shadow + y-sort live-verified (layer `Characters`, bóng `Shadows`); hit feedback/damage number đã có sẵn từ trước (đã verify wiring `Character.TakeDamage`); P1-5 input rebind chuyển sang S2 |
| **S2 — Lõi combat** | 3–4 | State machine player + combo + crit + damage numbers + pooling | ✅ **XONG 29/10** — `PlayerCombat` combo 3 nhịp (live-verified nối nhịp + reset window), `PlayerState` enum wire vào special_move, `ObjectPool` thay Instantiate/Destroy cho damage numbers + impact (2D URP-safe thay Standard 3D), test **15/15** xanh, Play Mode 0 exception. Còn lại cho S3: play 10 phút liên tục + đo FPS |
| **S3 — Map M1+M2** | 4–5 | 2 map + mini-boss 1 + checkpoint + save/load | Đi từ làng tới sau rừng 1 mạch không lỗi |
| **S4 — Skill tree + dialogue** | 6 | 19 nút + DialogueSO + hộp thoại + quest Q1–Q2 | Mở đủ nhánh, dialogue 0 lỗi font VN |
| **S5 — Map M3+M4+Boss** | 7–8 | 2 map + boss 3 phase + 2 ending | Trận boss 5 phút không crash; ending 2 nhánh |
| **S6 — Audio + UI polish** | 9 | Mixer 3 nhóm, SFX/BGM, HUD final, settings | Âm đúng nhóm, rebind hoạt động |
| **S7 — Hiệu năng + build** | 10 | Profiler snapshot, WebGL build | Đáp ứng ngân sách 12.1; build chạy trên máy tối thiểu |

### Định nghĩa "hoàn thành" mỗi sprint
- [ ] `unity recompile` 0 errors / 0 warnings
- [ ] Play Mode smoke 60s: 0 exception
- [ ] `unity test` (EditMode + PlayMode) xanh
- [ ] WebGL build thành công + chạy thử được 5 phút
- [ ] Update plan này: tick các mục xong + ghi số liệu thật (FPS, draw calls)

---

## PHỤ LỤC A — Lệnh CLI dùng xuyên suốt
```bash
export UNITY_PROJECT_PATH=/home/dragonccm/RPG_Game_Current
unity status --no-pager                      # kiểm editor connect (port 7801)
unity recompile --project-path $UNITY_PROJECT_PATH   # compile + báo lỗi
unity command script eval '<C# statement; return ...;'   # chạy code live
unity command editor status                  # trạng thái editor/playmode
unity test --project-path $UNITY_PROJECT_PATH        # chạy tests
unity build $UNITY_PROJECT_PATH --platform WebGl     # build
```

## PHỤ LỤC B — Code mẫu đã chốt (MovementMath)
Xem mục 3.1. Mọi module di chuyển phải import helper này; **cấm** tự nhân vector input trực tiếp.
