# BỘ NGUYÊN TẮC CHUNG & BẢN THIẾT KẾ CẢI TỔ DỰ ÁN (PROJECT BIBLE)
**Dự án:** RPG "Trăng Đỏ" (RPG_Game_Current)  
**Nền tảng:** Unity 6000.6.7f1 LTS — URP 2D  
**Mục tiêu:** Xây dựng game 2D Top-down Action RPG chuẩn mực, xóa bỏ chắp vá, tối ưu hiệu năng và cảm giác chơi (game feel).

---

## 1. NGUYÊN TẮC COMBAT & HOẠT ẢNH (COMBAT & ANIMATION)

### 1.1 Giải quyết triệt để lỗi "Lệch trọng tâm chân" khi đánh
* **Nguyên nhân gốc rễ:** Cắt sprite tự động (Automatic Slice) khiến mỗi frame có kích thước bounding box khác nhau (ví dụ: Idle 81×91px, vung kiếm 130×92px). Khi pivot đặt ở Center (0.5, 0.5), tâm của khung hình vung kiếm bị lệch sang phía lưỡi kiếm, kéo theo toàn bộ cơ thể và chân nhân vật bị trượt/giật.
* **Quy tắc bắt buộc:**
  1. **Fixed Grid Slicing:** Toàn bộ sprite nhân vật (Tiny Swords) phải được cắt theo lưới cố định của asset gốc (**192×192 px** mỗi ô frame).
  2. **Anchor chân cố định (Foot Pivot):** Pivot của mọi frame trong mọi animation (Idle, Run, Attack1, Attack2, Guard, Die) phải đặt chính xác tại **điểm tiếp đất giữa 2 chân** (`Bottom Center` hoặc Custom Pivot đồng nhất, ví dụ `{x: 0.5, y: 0.38}`).
  3. **Không dịch chuyển Transform khi đánh:** Di chuyển của đòn đánh phải do code điều khiển (bằng `Rigidbody2D.MovePosition` hoặc lực lướt nhẹ ngắn `0.05s`), tuyệt đối không dịch chuyển root trong animation clip để tránh trượt chân.

### 1.2 Chu kỳ đòn đánh 3 nhịp chuẩn chỉnh (Attack Lifecycle)
Mọi đòn đánh của Player và Enemy phải tuân thủ nghiêm ngặt 3 pha:
1. **Telegraph / Wind-up (Chuẩn bị):** 
   - Nhân vật lùi nhẹ hoặc giơ kiếm tích lực (0.15s – 0.4s).
   - Với Enemy: Bắt buộc có tín hiệu hình ảnh (mắt đỏ chớp sáng, vòng vạch định hướng pixel trên mặt đất).
2. **Active / Impact (Gây sát thương):** 
   - Lưỡi kiếm vung qua trong 0.05s – 0.1s. Kích hoạt hitbox thông qua `Physics2D.OverlapCircleNonAlloc`.
3. **Recovery / Cancel Window (Hồi chiêu & Thoát đòn):**
   - Đòn đánh khựng nhẹ trước khi về Idle (0.1s – 0.3s).
   - Cho phép người chơi "Combo Cancel" (bấm đòn tiếp theo đúng nhịp) hoặc "Dodge Cancel" (dùng Dash để thoát trạng thái).

### 1.3 Cảm giác va chạm (Hit Feedback & Juice) — Tinh tế, không lạm dụng
- **Hit-Stop (Micro-freeze):** Khi chém trúng quái, dừng frame toàn bộ hoạt ảnh của cả Player và quái trong **40ms – 60ms** (khoảng 2-3 frame). Tạo cảm giác lưỡi kiếm cắn sâu vào da thịt.
- **Screen Shake tinh tế:**
  - Đòn thường: Biên độ `0.05`, thời gian `0.08s` (chỉ rung nhẹ góc nhìn).
  - Đòn kết liễu (Finisher nhịp 3) hoặc chí mạng: Biên độ `0.12`, thời gian `0.15s`.
  - Giảm dần theo hàm lũy thừa, không rung giật liên tục gây chóng mặt.
- **Hit Flash:** Nhấp nháy màu trắng sáng (`Color.white`) trong đúng 1 frame trên SpriteRenderer của đối phương.

---

## 2. NGUYÊN TẮC HIỆU ỨNG HÌNH ẢNH (VFX STANDARD)

### 2.1 Tuyệt đối cấm "Hiệu ứng vector / hình học trơn" trong Pixel Art
- **Lệnh cấm:** Cấm dùng `LineRenderer` vẽ đường tròn toán học 64 đỉnh, cấm dùng `Texture2D` runtime vẽ gradient mềm làm vòng kỹ năng (như trong `DamageAreaIndicator.cs` cũ).
- **Quy chuẩn mới:**
  - 100% VFX phải là **Pixel Art VFX** (vẽ tay từng frame) hoặc **Particle System dạng pixelated** (Pixel Art Particles).
  - Kích thước pixel (Pixel Density / PPU) của VFX phải đồng bộ với nhân vật: **PPU = 64** (art Tiny Swords: 1 tile = 64 px = 1 đơn vị). Không scale sprite pixel theo số lẻ — sinh/vẽ đúng cỡ.
  - Hiệu ứng chém: Lưỡi chém hình lưỡi liềm pixel (Slash Arc) khớp với góc vung kiếm.
  - Hiệu ứng va chạm (Sparks/Blood): 3-5 hạt pixel văng theo hướng vung đòn và rơi xuống sàn trong 0.2s.

---

## 3. NGUYÊN TẮC MÔI TRƯỜNG, ÁNH SÁNG & KHÔNG GIAN (LEVEL DESIGN & DEPTH)

### 3.1 Quy chuẩn cấu trúc Map (Map Size & Density)
- **Kích thước vừa vặn:** Mỗi map có diện tích khoảng **60×40 tile đến 80×60 tile** (khoảng 2-3 màn hình camera). Người chơi đi từ đầu đến cuối map mất 45–60 giây đi bộ nếu không gặp quái.
- **Không gian có mục đích (No Dead Empty Space):** 
  - Mỗi góc map đều phải có lý do tồn tại: một bụi rậm giấu rương nhỏ, một đống đổ nát gợi ý cốt truyện, một câu đố bật công tắc, hoặc một cạm bẫy gai.
- **Yếu tố tương tác:**
  - **Cạm bẫy (Traps):** Gai bật từ dưới đất (Spike Trap), bẫy tên bắn từ tượng đá (Dart Trap), vùng sình lầy làm chậm 50% tốc độ.
  - **Câu đố (Puzzles):** Kéo cần gạt theo thứ tự ký hiệu, đẩy đá chặn dòng nước, thắp sáng 3 ngọn đuốc cùng lúc.

### 3.2 Tạo chiều sâu cho cảnh quan phẳng (Depth & Life)
1. **Phân lớp Tilemap (4 tầng bắt buộc):**
   - `Layer 0: Ground_Base` (Đất nền, cỏ phẳng)
   - `Layer 1: Ground_Clutter` (Cỏ nổi, hoa dại, vệt sỏi, rêu phong)
   - `Layer 2: Environment_Props` (Cây cối, đá tảng, hàng rào, tượng đá — có Collider2D và Y-Sorting)
   - `Layer 3: Canopy_Overhead` (Tán cây che khuất đầu, mái hiên — người chơi đi dưới sẽ mờ đi nhẹ)
2. **Sự sống trong môi trường (Ambient Animation):**
   - Bụi cỏ và hoa có animation đung đưa nhẹ theo gió (2-3 frame loop).
   - Đuốc, đống lửa có ngọn lửa bập bùng + tàn lửa bay.
   - Thêm hạt môi trường (Ambient Dust Particles, lá rơi lác đác, sương mù nổi sát mặt đất).
3. **Ánh sáng URP 2D (Lighting Depth):**
   - **Global Light 2D:** Đặt mức sáng môi trường ban đêm màu xanh sẫm / tím than (`#1a1c2b`, intensity 0.35) tạo không khí bí ẩn của "Trăng Đỏ".
   - **Point Light 2D:** Ánh sáng vàng cam ấm áp cho đống lửa, đuốc làng, đèn lồng NPC (intensity 1.2, bán kính 4-6m).
   - **Blob Shadow:** Mọi nhân vật, quái, và vật thể nổi phải có bóng elip bán trong suốt (`Alpha: 0.35`) nằm ở chân.

---

## 4. HỆ THỐNG TIẾN TRÌNH: KỸ NĂNG, INVENTORY & NHIỆM VỤ

### 4.1 Triết lý kỹ năng mới: "Tome / Scroll Based"
- **Nhân vật khởi đầu:**
  - CHỈ CÓ đòn đánh chém cơ bản (Combo 3 nhịp: Chém ngang -> Chém xéo -> Bổ mạnh).
  - Không có Dash, không có Teleport, không có Charge Attack sẵn.
- **Cuộn kỹ năng (Skill Scrolls):**
  - **Cuộn Lướt Gió (Wind Dash):** Nhận được từ Trưởng làng Mai sau khi làm xong nhiệm vụ đầu tiên cứu dân làng.
  - **Cuộn Hoả Kích (Flame Charge):** Nhận được sau khi giải câu đố trong Rừng Thảm Vàng.
  - **Cuộn Hư Không (Shadow Teleport):** Nhận được khi đánh bại Mini-boss Thợ Săn Bóng.
- **Vai trò mới của Cây kỹ năng (Skill Tree):**
  - Cây kỹ năng **KHÔNG PHẢI** là nơi bán chiêu mới.
  - Cây kỹ năng là nơi **cường hóa, đột biến (evolve)** các kỹ năng người chơi ĐÃ HỌC qua cuộn (ví dụ: Lướt thêm hiệu ứng né bất tử i-frame, Gồng kiếm tạo sóng lửa lan, Đòn chém thường tăng 15% chí mạng).

### 4.2 Hệ thống Túi đồ (Inventory System)
- **Cấu trúc:** Hệ thống ô lưới (Grid 16 ô cho MVP).
- **Phân loại vật phẩm:**
  1. `Consumables` (Bình máu, bình mana, thuốc giải độc).
  2. `Skill Tomes` (Cuộn bí kíp kỹ năng — click chuột phải để học).
  3. `Quest & Keys` (Chìa khóa mở cửa tháp, mảnh chuông cổ, thư tay của Mai).
  4. `Materials` (Quặng sắt rèn kiếm, tiền vàng).
- **Hotbar (Phím 1, 2, 3, 4):** Cho phép kéo bình máu hoặc vật phẩm nhanh vào các ô số để bấm dùng ngay trong trận chiến.

### 4.3 Hệ thống Nhiệm vụ & Cốt truyện (Quest & Narrative)
- **Tích hợp cốt truyện vào tiến trình:**
  - **Chương 1 (Làng Phong Nguyệt):** Người dân hoảng loạn vì trăng đỏ biến dị muông thú. Nhiệm vụ: Gặp thợ rèn sửa gươm cũ -> Cứu dân làng -> Mở khóa Cuộn Lướt Gió -> Cổng rừng mở ra.
  - **Chương 2 (Rừng & Đầm):** Giải câu đố bẫy đá cổ -> Thu thập mảnh chìa khóa Tháp -> Hạ mini-boss -> Mở khóa các cuộn kỹ năng nâng cao.
  - **Chương 3 (Đỉnh Tháp Trăng Đỏ):** Đối mặt với Agis với đầy đủ bộ kỹ năng đã tôi luyện.

---

## 5. NGUYÊN TẮC KỸ THUẬT & CODE (TECHNICAL STANDARDS)

### 5.1 Physics 2D & Truy vấn va chạm
- **CẤM:** Tuyệt đối không dùng các hàm `Physics.*` 3D (`OverlapSphere`, `Raycast` 3D) và không dùng `FindObjectsByType<T>()` trong vòng lặp gameplay.
- **CHUẨN:** 
  - Sử dụng `Physics2D.OverlapCircleNonAlloc` với mảng đệm tái sử dụng (pre-allocated buffer) để kiểm tra trúng đòn.
  - Sử dụng `ContactFilter2D` với đúng LayerMask (Layer 6: Player, Layer 3: Enemy).

### 5.2 Không dùng Reflection trong Gameplay Loop
- Bỏ toàn bộ việc dùng string gọi hàm reflection (`GetType().GetMethod(...)`).
- Sử dụng trực tiếp interface C# (ví dụ: `IDamageable`, `IInteractable`, `ISkillReceiver`).

### 5.3 Cấu trúc Thư mục & Đặt tên sạch
- **Casing:** Đổi toàn bộ tên file/class snake_case (`special_move.cs`, `agis_wizzar.cs`) sang PascalCase chuẩn C# (`SpecialMove.cs`, `AgisWizard.cs`).
- **Phân loại Asset:**
  - Prefabs nhân vật: `Assets/_Project/Prefabs/Characters/`
  - Prefabs môi trường & cạm bẫy: `Assets/_Project/Prefabs/Environment/`
  - Prefabs UI: `Assets/_Project/Prefabs/UI/`
  - Xóa bỏ hoàn toàn folder `__MACOSX` và các file `(1)`, `(2)` trùng lặp.

---

## 6. NGUYÊN TẮC BỔ SUNG (rà soát code 29/10/2026)

### 6.1 VFX có "chủ sở hữu" — hết cảnh gắn chắp vá
**Hiện trạng:** 4 hệ effect chồng nhau trong `Core/` (`CombatEffectsManager`, `EnhancedEffectManager`, `Effect2DManager`, `VisualEffects`) + 2 file `*Documentation.cs` nằm trong assembly game. `LineRenderer`/`SetPixel` vẫn còn ở `DamageAreaIndicator`, `agis_wizzar`, `CombatEffectsManager`.
- **1 cổng duy nhất:** gameplay chỉ gọi `VfxService.Play(VfxId, position, direction, tint)`; không script nào tự `Instantiate` effect.
- **VfxId → VfxDefinition (ScriptableObject):** prefab pixel-art, thời lượng, sorting layer, số hạt, âm thanh đi kèm. Gắn effect sai mục đích = khai báo sai trong 1 bảng, dễ rà.
- **Quy tắc ghép:** effect gắn vào *sự kiện* (HitLanded, Parried, Died, SkillCast), không gắn vào *đối tượng*. Trúng thịt → tóe máu; trúng khiên/giáp → tia lửa kim loại; trúng đá/đất → bụi.
- **Hướng & số hạt:** máu/tia lửa văng theo hướng đòn (hướng vung ± 30°), 3–5 hạt, sống ≤ 0.25s, rơi xuống sàn rồi mờ.
- Gộp 4 manager thành `VfxService`, xóa file documentation khỏi runtime (chuyển sang `docs/`).
- **Trạng thái 29/10/2026:** ✅ `Core/Vfx/VfxService.cs` + `PixelSprites.cs` đã thay `CombatEffectsManager`, `EnhancedEffectManager`, `Effect2DManager`, `VisualEffects`, `DamageAreaIndicator`, `CameraShake`. Sprite pixel sinh runtime là bản tạm — thay bằng art vẽ tay qua `VfxDefinition` sau. Đã dọn (30/10): đường ngắm kỹ năng → `PixelAimLine` (nét đứt pixel), `BlobShadow` và cầu lửa fallback của Agis → pixel dither/đặc, không gradient. Còn nợ: hạt `ParticleSystem` mượt trong `SkillExecutors` (xử lý khi refactor hệ kỹ năng), reflection `GetMethod("TriggerSkillAnimation")` ở `SkillExecutors`.

### 6.2 Chuẩn đòn đánh cho Player = Enemy = Boss (một khung duy nhất)
- Mọi đòn đều là dữ liệu `AttackDef` (SO): `windup`, `active`, `recovery`, `hitShape`, `damage`, `knockback`, `telegraph`, `cancelWindow`, `nextInChain`.
- Một `AttackRunner` dùng chung chạy 3 pha (mục 1.2); AI/Boss chỉ *chọn* `AttackDef`, không tự viết coroutine đánh riêng.
- Combo của quái/boss = danh sách `AttackDef` nối nhau + điều kiện chọn (khoảng cách, HP%, cooldown); phase boss = tập `AttackDef` khác.
- Hitbox bật bằng **Animation Event** hoặc mốc thời gian của `AttackRunner`, không đoán bằng `Invoke`.
- Telegraph bắt buộc ≥ 0.4s với đòn thường của boss, ≥ 0.8s với đòn diện rộng; không đòn nào "ra ngay" mà không báo hiệu.
- Test tự động: mỗi `AttackDef` có `windup > 0`, `hitShape` hợp lệ, chuỗi `nextInChain` không vòng lặp.

### 6.3 Animation mượt (bổ sung cho mục 1.1)
- **Animator chỉ có 1 tham số trạng thái** (`State` hoặc trigger theo `AttackDef`), không dùng chồng `bool` chéo nhau.
- Transition Idle ↔ Run: `Has Exit Time = off`, duration 0–0.05s (pixel art không blend dài). Attack → Idle: cho exit sau cửa sổ recovery.
- Kiểm tra bằng **pivot-lock test** (Editor): xuất vị trí chân (world) của từng frame mỗi clip; lệch > 1px so với frame Idle thì báo lỗi, chặn commit.
- Mọi clip cùng nhân vật dùng cùng kích thước ô (192×192) và cùng PPU; không dùng *Automatic Slice*.
- Flip bằng `SpriteRenderer.flipX` hoặc scale gốc, không lật từng sprite con lệch pivot.

### 6.4 Rung màn hình & HitStop có ngân sách
| Sự kiện | Biên độ | Thời gian | HitStop |
|---|---|---|---|
| Đòn thường trúng | 0.05 | 0.08s | 40ms |
| Finisher / chí mạng | 0.12 | 0.15s | 60ms |
| Nhận sát thương | 0.08 | 0.10s | 0 |
| Boss slam / nổ | 0.20 | 0.25s | 0 |
- Rung không cộng dồn: lấy `max` các nguồn, không `sum`. Có hệ số `ShakeIntensity` trong Settings (0–100%, mặc định 100) cho người dễ say.
- HitStop không đóng băng UI, âm thanh nhạc nền và không dùng `Time.timeScale` toàn cục khi có thể (chỉ dừng animator/body của 2 bên) — hiện `CombatEffectsManager` đang chạm `timeScale`.

### 6.5 Skill: một nguồn sự thật (khớp mục 4.1)
**Hiện trạng:** `SkillExecutors.cs` 1795 dòng, `ModularSkillManager.cs` 1497 dòng, cộng `UnifiedSkillSystem`, `SkillSlot`, `special_move` — 3 lớp song song. `SkillExecutors.cs` còn gọi bằng reflection.
- Dữ liệu: `SkillDef` (SO) với `id`, `source` (Scroll/Quest/Boss), `executor`, `cooldown`, `cost`, `icon`.
- Trạng thái học: `SkillBook` (đã học / đang trang bị) lưu trong `SaveSystem`; **không có kỹ năng nào dùng được nếu chưa `Learn()`** (hiện đã gating qua skill tree — đổi nguồn mở khóa sang cuộn).
- Skill tree chỉ lưu *nâng cấp* (`SkillUpgrade` trỏ tới `SkillDef` cha); node cha chưa học = node khóa.
- Tách `SkillExecutors.cs` thành 1 file / 1 executor (≤ 200 dòng), đăng ký qua bảng `executorId → class`, không reflection.

### 6.6 Inventory & Item
- `ItemDef` (SO, có `id` ổn định, không đổi tên sau khi phát hành save). Lưu bằng `id` + số lượng, không lưu tham chiếu asset.
- Mọi thao tác túi qua `InventorySystem.TryAdd/TryRemove/Use` (trả `bool` + lý do); UI chỉ đọc sự kiện `InventoryChanged`.
- Nhặt đồ đầy túi → không biến mất: rơi lại sàn hoặc báo "Túi đầy".
- Vật phẩm quest không thể vứt/bán; `Use` của cuộn kỹ năng gọi `SkillBook.Learn()` và tiêu hao 1.

### 6.7 Quest, cốt truyện, hội thoại
- Quest = dữ liệu (`QuestDef`: bước, điều kiện, phần thưởng, cờ mở khóa) nghe `EventBus` (`EnemyKilled`, `ItemPicked`, `DialogueEnded`, `AreaEntered`), không để quest đọc trực tiếp scene.
- **Story flags** (`bool` theo tên) là nguồn duy nhất cho thay đổi thế giới: cổng mở, NPC đổi lời thoại, boss xuất hiện.
- Mỗi NPC có hội thoại theo cờ (chưa nhận / đang làm / đã xong). Mỗi quest có chỉ dẫn: dấu `!`/`?` trên đầu NPC, mục tiêu hiển thị ở HUD.
- Không có quest "chết": quest lỗi thời phải có `failIf`/`expireIf` hoặc luôn hoàn thành được.

### 6.8 Môi trường: quy tắc thêm
- **Mật độ deco:** mỗi màn hình camera có ≥ 12 chi tiết lớp `Ground_Clutter`, trộn ngẫu nhiên có seed (không lặp theo lưới, không xếp hàng thẳng). Dùng Rule Tile / Random Tile.
- **Animation môi trường rẻ:** cỏ/hoa dùng Tile animated (2–3 frame, pha lệch ngẫu nhiên) hoặc shader gió; không đặt Animator riêng cho từng bụi.
- **Chiều sâu:** viền map có tán cây/vách che đậm hơn (vignette nhẹ), nền xa tối hơn nền gần; bóng cây đổ lệch cùng 1 hướng toàn map (hướng "ánh trăng").
- Light 2D: tối đa ~8 đèn *ảnh hưởng cùng lúc* mỗi màn hình; đèn tĩnh dùng layer/target sorting giới hạn.

### 6.9 Âm thanh & cảm giác
- Mỗi sự kiện chiến đấu có SFX riêng cho chất liệu (thịt / kim loại / đá), 2–3 biến thể ngẫu nhiên, pitch ± 5%.
- `AudioService` duy nhất (hiện chưa có manager; `AudioSource` rải trong `PlayerController`, `SkillExecutors`, `agis_wizzar`). Nhạc nền phối theo vùng/boss; HitStop không làm méo âm.

### 6.10 Lưu game, localization, dữ liệu
- Save: có `version`, migrate được, ghi file tạm rồi đổi tên (đã có backup — giữ). Thêm lưu: kỹ năng đã học, túi đồ, story flags, checkpoint cuối.
- Chuỗi hiển thị (hiện ~80 chỗ tiếng Việt viết thẳng trong UI/code): đưa vào bảng chuỗi (`LocKey → text`) trước khi dựng thêm nội dung, kể cả khi chỉ có tiếng Việt.
- `Resources.Load` chỉ cho dữ liệu khởi động nhỏ; prefab nặng dùng Addressables (đã cài) hoặc tham chiếu inspector.
- Mọi hằng số cân bằng (damage, cooldown, tầm) nằm trong SO, không hard-code trong script.

### 6.11 Code & vận hành
- `Debug.Log` (hiện ~31 file) bọc bằng `Log.Info/Warn` có cờ bật theo module; build release không in log gameplay.
- Không `Instantiate/Destroy` trong vòng chiến đấu: dùng `ObjectPool` (đã có) cho đạn, hạt máu, số sát thương, quái spawn.
- `Update()` của AI/UI phải throttle hoặc event-driven; không `FindObject*` ngoài `Awake/Start`.
- File > 500 dòng = tín hiệu tách. Mỗi PR/commit: recompile 0 lỗi, EditMode test xanh, thêm test cho logic mới (damage, quest, inventory, save).

---

## 7. DANH SÁCH KIỂM TRA TRƯỚC KHI COMMIT (Definition of Done)
- [ ] Animation mới qua pivot-lock test, không trượt chân khi vào/ra đòn.
- [ ] Đòn mới là `AttackDef`, có windup/telegraph, đúng bảng rung/HitStop.
- [ ] Effect gọi qua `VfxService`, là pixel art, không `LineRenderer` hình tròn.
- [ ] Không reflection, không `Physics.*` 3D, không `FindObject*` trong gameplay.
- [ ] Item/skill/quest mới là SO có `id` ổn định và lưu được trong save.
- [ ] Chuỗi hiển thị dùng `LocKey`; số cân bằng nằm trong SO.
- [ ] Test EditMode xanh, Play Mode 60s không exception.
