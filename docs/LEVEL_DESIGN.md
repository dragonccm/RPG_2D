# LEVEL DESIGN & WORLD BUILDING — RPG "TRĂNG ĐỎ"

> Tài liệu thiết kế màn chơi và thế giới. Bổ sung cho `GAME_MASTER_PLAN.md` (Phần 4, 8, 9, 10) và
> `CORE_PROJECT_GUIDELINES.md` (Mục 3, 6.8). Khi mâu thuẫn, tài liệu này thắng về **bố cục map, nhịp chơi, lore**;
> GUIDELINES thắng về **quy chuẩn kỹ thuật**.

**Phiên bản:** 0.1 — 29/10/2026
**Trạng thái hiện tại của world:** 1 scene `World.unity` chứa chung Làng + Rừng (tilemap lớn nhất 84×66 tile, 7 tilemap, 1 Light2D toàn cục). NPC, đống lửa, cổng đều là **hình tròn mềm placeholder** do `WorldBuilder.CreateCircleSprite` vẽ — vi phạm GUIDELINES 2.1, sẽ thay trong đợt dựng lại.

---

## 0. THƯỚC ĐO CHUẨN (METRICS)

Mọi con số đo bằng **tile**. 1 tile = 1 đơn vị Unity = 64 px art (Tiny Swords, PPU 64).

| Thước đo | Giá trị | Nguồn / lý do |
|---|---|---|
| Khung camera | **17.8 × 10 tile** (ortho size 5, 16:9) | `World.unity` |
| Tốc độ đi | 3.5 tile/s | `PlayerController.moveSpeed` |
| Hành lang chính | 3–4 tile rộng | đủ né sang bên, không thấy trống |
| Lối phụ / lối bí mật | 2 tile | người chơi "lách" qua, cảm giác khám phá |
| Arena giao tranh | 10×7 → 14×9 tile (vừa 1 màn hình) | địch không đánh từ ngoài khung hình |
| Arena boss | 16×11 tile, có viền vách | camera thấy toàn bộ khi lùi 1 chút |
| Khoảng cách spawn → nhóm địch đầu | ≥ 1 màn hình (≥ 18 tile) | cho người chơi định thần |
| Khoảng cách giữa 2 đống lửa | ≤ 90 giây đi + đánh | chết không mất quá nhiều tiến độ |
| Kích thước map | 60×40 → 80×60 tile | ≈ 14–27 màn hình |
| Chiều dài lộ trình chính | 150–220 tile (đường uốn) | ≈ 45–60 giây đi không giao tranh |

**Quy tắc tầm nhìn:** từ bất kỳ điểm nào trên đường chính, người chơi phải thấy (hoặc nghe/thấy ánh sáng của) **mục tiêu kế tiếp** trong vòng 1 màn hình: ánh đèn đống lửa, cột khói, NPC có dấu `!`, cổng phát sáng.

---

## 1. WORLD BIBLE

### 1.1 Thế giới
- **Vùng Nguyệt Châu** — thung lũng nhỏ bao quanh bởi rừng thông và đầm lầy, nằm dưới bóng **Tháp Trăng Đỏ** cổ. Người dân sống bằng nông nghiệp, chăn cừu, đốn gỗ.
- **Lễ Cúng Trăng** — mỗi 12 năm, dân làng dâng lễ lên tháp để "giữ trăng trắng". 3 năm trước lễ bị bỏ dở: con gái Agis là **Lam** biến mất trên đỉnh tháp.
- **Trăng đỏ** — Agis, pháp sư giữ tháp, đảo ngược nghi lễ để đổi vầng trăng lấy Lam. Ánh trăng đỏ **làm hóa dại sinh vật sống** (cừu, chuột, yêu tinh rừng) và đánh thức các tượng/bẫy cổ trong tháp.
- **Luật của thế giới (người chơi đọc được qua môi trường):**
  1. Nơi có lửa ấm (đống lửa, đuốc, đèn lồng) thì trăng đỏ yếu đi → **đống lửa = vùng an toàn + checkpoint**.
  2. Càng gần tháp, bầu trời càng đỏ, sương càng dày, cây càng khô.
  3. Sinh vật hóa dại có **mắt đỏ phát sáng** — dấu hiệu nhận biết kẻ thù từ xa.

### 1.2 Nhân vật
| Nhân vật | Vai trò | Art có sẵn | Nơi xuất hiện |
|---|---|---|---|
| **Kiền** | Người chơi — lính canh làng, chỉ có kiếm | Tiny Swords Warrior (Knights) | khắp nơi |
| **Mai** | Trưởng làng, giao quest đầu, trao Cuộn Lướt Gió | (cần sprite NPC) | M1 |
| **Ông Tễ** | Thợ rèn già, rèn lại gươm, kể về lễ cúng trăng | (cần sprite NPC) | M1 |
| **Bé Na** | Đứa trẻ trốn trong rừng — quest phụ, lore về Lam | (cần sprite NPC) | M2 |
| **Lão Câu** | Người chèo đò ở đầm, mở đường tắt về làng | (cần sprite NPC) | M3 |
| **Agis** | Boss cuối — pháp sư cha | `Art/Agis` | M4 |
| **Bóng Lam** | Hồn ảnh mờ — chỉ xuất hiện ở tháp và ending | (tái màu Agis/NightBorne) | M4 |

### 1.3 Phe địch (ánh xạ vào art + AI đã có)
| Phe | Kẻ địch | Art | AI | Map |
|---|---|---|---|---|
| Thú hóa dại | Cừu điên (lao húc) | `Resources/Sheep`, `animation/sheep` | Melee | M1 rìa, M2 |
| Thú hóa dại | Chuột đầm (bầy, nhanh) | `Art/Rat` | Melee nhóm | M3 |
| Yêu tinh rừng | Goblin đuốc, Goblin ném thuốc nổ | `Factions/Goblins` | Melee / Ranged | M2, M3 |
| Yêu tinh rừng | **Thợ Săn Bóng** (mini-boss) | `Art/Hobgoblin` | Elite Melee + 2 Support | M2 |
| Đội Cúng Trăng | Pháp sư cúng (hồi máu đồng bọn) | Goblin tái màu | Support | M3, M4 |
| Đội Cúng Trăng | **Nhà Chiến Thuật Kích** (mini-boss) | `Necromancer` sheet | Elite Ranged | M3 |
| Hộ vệ tháp | Kỵ sĩ bóng đêm | `Art/NightBorne` | Shielder / Elite | M4 |
| — | **Agis** | `Art/Agis` | Boss 3 phase | M4 đỉnh |

### 1.4 Bảng màu & không khí theo vùng
| Vùng | Global Light 2D | Đèn điểm | Cảm xúc |
|---|---|---|---|
| M1 Làng | `#2a2f4a` · 0.45 | Đèn lồng vàng cam, đống lửa giữa làng | ấm, an toàn nhưng lo âu |
| M2 Rừng | `#1f2a2a` · 0.35 | Đom đóm xanh, đuốc yêu tinh cam đỏ | bí ẩn, bị theo dõi |
| M3 Đầm | `#25223a` · 0.30 + sương | Ma trơi tím, đèn đò | ngột ngạt, mất phương hướng |
| M4 Tháp | `#3a1418` · 0.30 (đỏ) | Nến nghi lễ đỏ, rune tím | áp lực, trang nghiêm |
| M5 Hang | `#101418` · 0.15 | Nấm phát sáng xanh lục, quặng | yên tĩnh, phần thưởng |

---

## 2. BẢN ĐỒ THẾ GIỚI & TIẾN TRÌNH MỞ KHÓA

### 2.1 Kết nối các vùng
```
                         [M4 THÁP TRĂNG ĐỎ]
                          tầng 2 (Agis)
                               ▲
                          tầng 1 (gauntlet)
                               ▲ cổng tháp (cần Chìa Khóa Tháp — q3)
                               │
   [M5 HANG]◄─ đá nứt ─┐  [M3 ĐẦM LAN SƯƠNG] ◄── cầu gãy (Lướt Gió) ──┐
   (optional, Hỏa Kích)│        │  └─ đò Lão Câu: lối tắt về M1 (sau mini-boss M3)
                       │        │
               [M1 LÀNG PHONG NGUYỆT] ──── cổng bắc (q1 xong) ───► [M2 RỪNG THẢM VÀNG]
                    (hub, an toàn)                                   mini-boss Thợ Săn Bóng
```

### 2.2 Kỹ năng = chìa khóa thế giới (metroidvania nhẹ)
Mỗi cuộn kỹ năng **mở đường mới** chứ không chỉ thêm sát thương — người chơi quay lại map cũ thấy chỗ trước đây không qua được.

| Cuộn | Nhận ở | Mở được gì trong thế giới |
|---|---|---|
| **Lướt Gió** (Dash) | Mai, xong q1 (M1) | Nhảy qua **khe vực/cầu gãy** 2–3 tile (M2→M3, rương bí mật M1) |
| **Hỏa Kích** (Charge) | Giải câu đố bàn thờ rừng (M2) | Phá **đá nứt / tường nứt** (lối vào M5, đường tắt M3) |
| **Hư Không** (Teleport) | Hạ Thợ Săn Bóng (M2) | Xuyên **hàng rào rune** mỏng 1 tile (tầng 1 tháp, rương đầm) |

> ⚠️ **Mâu thuẫn cần sửa trong code:** `agis_wizzar.cs` hiện trao Cuộn Hư Không khi Agis gục — vô nghĩa vì đó là boss cuối. Chuyển phần thưởng sang mini-boss Thợ Săn Bóng; Agis rơi vật phẩm ending (Mặt Nạ Lam).
> Scene hiện có `Chest_Village_Dash` — giữ, nhưng rương chỉ mở sau khi Mai giao (đặt flag `q1_done`).

### 2.3 Đường cong độ khó
| Map | Thời lượng | Số nhóm địch | Loại địch mới được dạy | Cơ chế mới |
|---|---|---|---|---|
| M1 | 5–7 phút | 1 (rìa làng, tutorial) | Cừu điên | di chuyển, đánh 3 nhịp, tương tác F, đống lửa |
| M2 | 8–10 phút | 5 + mini-boss | Goblin đuốc → Goblin ném bom | Dash, bẫy gai, câu đố công tắc |
| M3 | 8–10 phút | 6 + mini-boss | Chuột bầy, pháp sư hồi máu, Shielder | sương che tầm nhìn, bùn làm chậm, Charge phá đá |
| M4 | 10–12 phút | gauntlet 3 phòng + boss | Kỵ sĩ bóng đêm | Teleport qua rune, elevation, boss 3 phase |
| M5 | 4–6 phút | 2 + elite | — | phần thưởng, lore |

**Luật dạy địch:** địch mới **luôn xuất hiện lần đầu một mình** ở arena rộng → lần 2 đi cùng 1 loại quen → lần 3 trộn 3 loại.

---

## 3. THIẾT KẾ TỪNG MAP

Ký hiệu trong sơ đồ: `S` điểm vào · `F` đống lửa · `N` NPC · `E` nhóm địch · `B` boss/mini-boss · `C` rương · `T` bẫy · `P` câu đố · `X` lối ra · `?` bí mật · `≈` nước/đầm · `#` vách/cây dày · `~` khe vực · `%` đá nứt.

### 3.1 M1 — LÀNG PHONG NGUYỆT (hub) · 64×44 tile
**Mục đích:** dạy điều khiển trong an toàn, giới thiệu thế giới, nhận quest đầu. Người chơi sẽ quay lại đây nhiều lần → phải thay đổi theo tiến trình (story flags).

```
 ################################ X(cổng bắc → M2, khóa tới khi xong q1) ####
 #  ruộng   ruộng      ║  E (rìa: 2 cừu điên — tutorial combat)          #
 #  ░░░░    ░░░░       ║                                                  #
 #        nhà Mai N    ║        giếng   lò rèn N(Ông Tễ)                  #
 #   C?(sau nhà,       ║                     ┌──┐                         #
 #   cần Dash qua ~)   ╚════ quảng trường F ═╡  ├══ chợ (đèn lồng) ════    #
 #                          (đống lửa chính)                              #
 #   chuồng cừu (rào gãy, dấu máu)        nhà Na (cửa khóa, đồ chơi rơi)  #
 #                                                                        #
 #  bến sông ≈≈≈≈≈≈  đò (khóa tới sau M3)          %  ← lối hang M5 (Hỏa Kích) #
 ######################################## S (cổng nam — điểm bắt đầu) #######
```
**Nhịp (beats):**
1. **S → quảng trường (0:00–0:40):** đường thẳng 4 tile, 2 bên nhà có đèn lồng — dạy đi + nhìn ánh sáng = an toàn. Dân làng đứng co cụm quanh đống lửa (bark ngắn khi đi ngang).
2. **Gặp Mai (0:40–1:30):** hội thoại q1 → "tìm Ông Tễ rèn lại gươm".
3. **Lò rèn (1:30–2:30):** Ông Tễ giao gươm, dạy combo 3 nhịp trên **hình nộm rơm** (không phải quái) → `q1_combo`.
4. **Rìa bắc (2:30–4:00):** 2 cừu điên phá rào — trận đầu, arena 12×8, không đường lùi bẩy. Đống lửa quảng trường ở 1 màn hình phía sau.
5. **Trả quest (4:00–5:00):** Mai trao **Cuộn Lướt Gió**, mở cổng bắc. Dạy Dash ngay: khe nước 2 tile sau nhà Mai dẫn tới rương `C?` (bình máu ×2).

**Story flags làm làng thay đổi:**
| Flag | Thay đổi |
|---|---|
| `q2_done` | Bé Na về nhà — cửa mở, có quest phụ tặng bùa |
| `q3_started` | Trời đỏ hơn (Global Light đổi màu), dân làng đóng cửa, chỉ còn Mai ở quảng trường |
| `m3_ferry_open` | Đò ở bến sông hoạt động — đường tắt sang M3 |
| `ending_*` | Làng sau ending: đèn lồng trắng hoặc tro tàn |

**Deco & môi trường:** ruộng (Tiny Swords Ground), hàng rào gỗ, đống rơm, cừu thường (không hóa dại, đứng trong chuồng → tương phản với cừu điên), giếng, lò rèn có khói (particle pixel), quần áo phơi đung đưa. Mật độ ≥ 12 chi tiết/màn hình (GUIDELINES 6.8).
**Ánh sáng:** 6 đèn lồng (Point Light 2D vàng cam, bán kính 4), đống lửa quảng trường (bán kính 6, nhấp nháy), cửa sổ nhà (Freeform light nhỏ).
**Âm thanh:** gió đêm, chó sủa xa, lửa tí tách gần quảng trường.

---

### 3.2 M2 — RỪNG THẢM VÀNG · 76×56 tile
**Mục đích:** combat thật sự đầu tiên, dạy Dash trong chiến đấu, câu đố đầu tiên, mini-boss đầu tiên.

```
 ############################################################################
 # ~~~~~~~~ (vực: cầu gãy → M3, cần Lướt Gió)  X→M3                         #
 #   B  arena mini-boss 16×11 (vòng đá cổ, cổng rào đóng khi vào)          #
 #   ╚══════╗                                         ? (cây rỗng: lore Lam) #
 #          ║   F2 (đống lửa trước boss)                                    #
 #   P ── bàn thờ rừng: 3 công tắc + đuốc → mở Cuộn Hỏa Kích   E4         #
 #   │         T T (bẫy gai hành lang hẹp 3 tile)                           #
 #   E3 (goblin đuốc + 2 cừu)        ≈ suối nhỏ ≈       N (Bé Na trốn sau đá) #
 #                         E2 (goblin ném bom trên gò cao — dạy né tầm xa)   #
 #   C (rương sau bụi — cần Dash qua khe)                                   #
 #          E1 (2 goblin đuốc, arena 12×8)        F1 (đống lửa đầu rừng)    #
 ##################### S (từ cổng bắc M1) ###################################
```
**Nhịp:**
1. **F1 ngay cửa rừng** — an toàn, nhìn thấy cột khói đuốc goblin phía trước.
2. **E1:** 2 goblin đuốc — dạy dùng Dash né đòn lao.
3. **E2:** goblin ném bom đứng trên gò (elevation) — vùng nổ có **telegraph 0.8s** (`VfxService.Telegraph`). Dạy: không đứng yên.
4. **Bé Na (quest phụ):** tìm 3 đồ chơi rơi rải rác (khám phá rìa map) → thưởng bình máu + lore "Lam từng dạy em hát bài cúng trăng".
5. **Bẫy gai T:** hành lang hẹp, gai bật theo nhịp 1.2s — có **tiếng tách + nhấp nháy đỏ 0.4s** trước khi bật.
6. **E3 + câu đố P:** bàn thờ cổ với 3 công tắc; ký hiệu khắc trên 3 cây đá xung quanh cho biết thứ tự (môi trường kể chuyện, không cần chữ). Đúng thứ tự → đuốc bàn thờ sáng, rương giữa mở: **Cuộn Hỏa Kích**. Sai → 2 cừu điên xuất hiện.
7. **E4:** nhóm trộn (goblin đuốc + ném bom + cừu) — bài kiểm tra trước boss.
8. **F2 → B Thợ Săn Bóng:** arena vòng đá, cổng rào đóng sau lưng. Phase đổi ở 50% HP: gọi 2 Support. Thắng → **Cuộn Hư Không**, cổng rào mở, `q2` xong.
9. **Lối ra X:** cầu gãy qua vực — cần Lướt Gió (đã có từ M1) → sang M3.

**Deco:** cây dày 2 lớp (lớp viền map đậm, lớp trong thưa), bụi, nấm, khúc gỗ đổ, lá rơi (particle), đom đóm (particle pixel xanh), đá rêu. **Tán cây (Canopy)** che đầu ở 30% diện tích — mờ đi khi người chơi bước vào.
**Ánh sáng:** đom đóm (Point Light nhỏ bán kính 1.5, ~6 cái/màn hình chỉ 2 cái có light thật), đuốc goblin, ánh trăng lọt qua tán (Sprite Light hình đốm).

---

### 3.3 M3 — ĐẦM LAN SƯƠNG · 80×56 tile
**Mục đích:** địch tầm xa + hỗ trợ, môi trường gây áp lực (sương, bùn), dùng Charge phá đường.

```
 ############################################################################
 #  X→M4 (cổng tháp, cần Chìa Khóa Tháp)      B (đảo giữa đầm: Nhà Chiến     #
 #   ▲                                          Thuật Kích, arena 16×11)     #
 #   │  F2   P2 (3 đuốc thắp cùng lúc → nước rút, lộ Chìa Khóa)               #
 #   │      ≈≈≈≈ bùn (chậm 50%) ≈≈≈≈        E5 (shielder + pháp sư hồi máu)  #
 #   E4 (bầy chuột 5 con)             % (tường nứt: tắt về F1 — Hỏa Kích)    #
 #  ≈≈≈≈≈≈≈≈   ván gỗ hẹp 2 tile ═══════════   ≈≈≈≈≈≈      C? (rune: Hư Không) #
 #       E3 (ranged trên cọc gỗ)     N (Lão Câu + đò)                        #
 #  E2 (pháp sư + 2 goblin)                                                  #
 #       E1 (chuột ×3)    F1                                                 #
 ######### S (từ cầu gãy M2) ################################################
```
**Nhịp:** F1 → chuột (dạy bầy nhanh, đánh lan) → pháp sư hồi máu (dạy ưu tiên mục tiêu) → ranged trên cọc giữa nước (dạy Dash áp sát qua ván) → Lão Câu (lore + mở đò về M1 sau boss) → bùn + chuột (áp lực) → câu đố 3 đuốc (P2: thắp trong 6s, dùng Dash di chuyển kịp) → mini-boss trên đảo → nhặt **Chìa Khóa Tháp** (`q3_key`) → cổng tháp.
**Sương:** sprite sương pixel bán trong suốt trôi chậm ở lớp trên cùng, **dày ở xa, mỏng quanh người chơi** (vòng sáng quanh player bằng Light2D bán kính 3) → tầm nhìn ~1 màn hình.
**Deco:** lau sậy (đung đưa), cọc gỗ mục, xác thuyền, đá sông (`Terrain/Water/Rocks`), hoa súng, ma trơi tím (light + particle).

---

### 3.4 M4 — THÁP TRĂNG ĐỎ · tầng 1 (48×40) + tầng 2 (32×28)
**Tầng 1 — gauntlet 3 phòng nối hành lang, mỗi phòng 1 bài kiểm tra:**
| Phòng | Thử thách | Kỹ năng kiểm tra |
|---|---|---|
| Sảnh Tượng | 4 tượng bắn tên (Dart Trap) quét theo nhịp + 2 kỵ sĩ bóng đêm | Dash né + định vị |
| Hành Lang Rune | hàng rào rune 1 tile chặn đường, rương phía sau | Teleport (Hư Không) |
| Điện Nến | 3 làn sóng địch trộn, nền có ô gai | tổng hợp + Charge |
Đống lửa duy nhất ở giữa tầng 1 (sau phòng 2) — không hồi lại ở đỉnh.

**Tầng 2 — đỉnh tháp (arena Agis 16×11, hình tròn, rìa là vực trời đỏ):**
- Phase 1 (100–60%): bắn cầu lửa + teleport; telegraph vàng 0.35s (đã có).
- Phase 2 (60–30%): vùng AOE dither đỏ trên sàn (`Telegraph`), gọi 2 pháp sư cúng.
- Phase 3 (<30%): berserk, sàn rìa sụp dần (thu hẹp arena 2 tile mỗi 15s), Bóng Lam hiện ở giữa — không tấn công.
- Ending: lựa chọn Tha / Kết liễu (đã có trong `agis_wizzar.cs`).
**Ánh sáng:** trăng đỏ khổng lồ làm nền (sprite parallax), nến đỏ quanh arena, rune tím trên sàn sáng theo phase.

---

### 3.5 M5 — HANG DƯỚI LÀNG (optional) · 40×32 tile
Lối vào: đá nứt bờ sông M1 (cần Hỏa Kích). 2 nhóm chuột + **Elite Đá Gộp** (NightBorne tái màu đá). Thưởng: nâng cấp bình máu (+1 slot), bích họa kể chuyện Lễ Cúng Trăng 3 năm trước (Lam bị một **bóng đen không phải Agis** dẫn đi — hook DLC). Nấm phát sáng là nguồn sáng duy nhất.

---

## 4. CATALOG BẪY & CÂU ĐỐ

| Thành phần | Trạng thái code | Hành vi | Telegraph bắt buộc | Dùng ở |
|---|---|---|---|---|
| Bẫy gai (`SpikeTrap`) | ✅ có | bật theo nhịp hoặc khi giẫm | tiếng tách + nháy đỏ 0.4s | M2, M4 |
| Công tắc (`PuzzleSwitch`) | ✅ có | bật/tắt, báo quest | đèn đổi màu | M2, M3 |
| Rương (`TreasureChest`) | ✅ có | mở 1 lần, lưu trạng thái | — | mọi map |
| Đống lửa (`Campfire`) | ✅ có | hồi máu + lưu | — | mọi map |
| Cổng (`ScenePortal`) | ✅ có | chuyển vùng + autosave | cổng sáng | mọi map |
| Bẫy tên (Dart Trap) | ⬜ cần làm | tượng bắn tên theo hướng | mắt tượng sáng 0.6s | M4 |
| Bùn lầy (Slow Zone) | ⬜ cần làm | chậm 50% khi đứng trong | bọt bùn animation | M3 |
| Khe vực (Dash Gap) | ⬜ cần làm | rơi = mất 10% HP + về mép | mép vực có viền đá | M1, M2 |
| Đá/tường nứt (Breakable) | ⬜ cần làm | Charge phá → mở lối | vết nứt sáng cam khi có Hỏa Kích | M1, M3 |
| Rào rune (Rune Barrier) | ⬜ cần làm | chặn đi bộ, Teleport xuyên được | rune tím nhấp nháy | M3, M4 |
| Đuốc thắp (Torch Puzzle) | ⬜ cần làm | thắp N đuốc trong T giây | đuốc đã thắp tắt dần theo bậc | M3 |
| Sàn sụp (Collapsing Floor) | ⬜ cần làm | sụp theo thời gian ở phase 3 | nứt + rung nhẹ 1s | M4 |

Mọi bẫy mới implement `IInteractable` hoặc nghe `EventBus`, có trạng thái lưu được qua `SaveSystem` (bẫy đã phá, câu đố đã giải).

---

## 5. QUY TẮC THIẾT KẾ GIAO TRANH (ENCOUNTER)

1. **Arena có hình dạng:** không đánh trên bãi trống vuông. Mỗi arena có ít nhất 1 chướng ngại để vòng (đá, cây, giếng) và 1 lối thoát.
2. **Địch lộ diện trước khi đánh:** người chơi thấy nhóm địch (hoặc đuốc/mắt đỏ) trước khi vào tầm aggro ≥ 3 giây.
3. **Không spawn sau lưng** trừ bẫy phạt câu đố (có báo trước bằng âm thanh).
4. **Sau mỗi giao tranh lớn → phần thưởng hoặc nghỉ:** rương, đống lửa, NPC, hoặc cảnh đẹp.
5. **Kẻ địch tầm xa** đứng ở vị trí cao / bên kia chướng ngại → người chơi phải chọn đường áp sát.
6. **Số địch đồng thời** ≤ 5 (≤ 7 ở M4) — giữ cả hiệu năng AI và độ đọc được.

---

## 6. QUY TRÌNH DỰNG MAP (PIPELINE)

### 6.1 Cấu trúc scene
- **1 map = 1 scene**, nạp additive: `Scenes/Maps/M1_Village.unity`, `M2_Forest.unity`, `M3_Marsh.unity`, `M4_Tower_F1.unity`, `M4_Tower_F2.unity`, `M5_Cave.unity`.
- Scene `Core.unity` giữ thứ sống xuyên map: Player, Camera, `GameSystemsBootstrap`, UI, EventSystem.
- `World.unity` hiện tại được **tách**: phần Làng → M1, phần Rừng → M2. Giữ `World.unity` tới khi M1+M2 mới chạy được rồi mới xóa.

### 6.2 Cấu trúc trong mỗi scene map
```
M1_Village
├── Grid
│   ├── 00_Ground_Base        (Sorting: Ground, order 0)
│   ├── 01_Ground_Clutter     (Ground, order 10)    ← cỏ/hoa/sỏi, animated tile
│   ├── 02_Water              (Ground, order -10)   + collider
│   ├── 03_Cliffs_Elevation   (Props)               + collider, elevation
│   ├── 04_Collision          (ẩn, chỉ TilemapCollider2D + CompositeCollider2D)
│   └── 05_Canopy             (Effects, order 100)  ← mờ khi player bên dưới
├── Props            (cây, đá, nhà — prefab, Y-sort trong layer Props)
├── Interactables    (rương, công tắc, bẫy, đống lửa, cổng)
├── Encounters       (EnemySpawner/EnemyGroup theo E1..En)
├── NPCs
├── Lighting         (Global Light 2D + đèn điểm)
├── Ambience         (particle sương, lá, đom đóm; audio vùng)
└── Navigation       (NavMeshSurface 2D)
```
Tên tilemap hiện tại (`Collisin-low`, `Non-Collision-high`, `Boundiary`, `sand`, `water`, `Ground`) sẽ được ánh xạ sang chuẩn trên khi tách scene.

### 6.3 Các bước dựng mỗi map (không nhảy cóc)
1. **Greybox** — chỉ tile nền + collision + điểm S/F/E/B/X bằng gizmo. Chơi thử: lộ trình đủ 45–60s? arena đủ rộng?
2. **Encounter pass** — đặt địch, bẫy, câu đố; chơi lại đo thời gian và số lần chết.
3. **Art pass** — thay tile nền thật, props, nhà, cây (Tiny Swords). Không còn sprite tròn placeholder.
4. **Clutter pass** — cỏ/hoa/đá nhỏ (Random/Rule Tile, ≥ 12/màn hình), animated tile.
5. **Light pass** — Global Light theo bảng 1.4, đèn điểm, sương.
6. **Story pass** — NPC, hội thoại, flags, environmental storytelling.
7. **Perf pass** — Profiler: ≤ 8 light ảnh hưởng/màn hình, ≤ 5–7 địch đồng thời, 60 FPS.

### 6.4 Prefab cần có (thay `WorldBuilder` sprite tròn)
`Campfire` (sprite lửa animation + Light2D), `NPC_Base` (sprite + `DialogueInteractable` + dấu `!`/`?`), `Portal` (cổng đá + rune sáng), `Chest`, `SpikeTrap`, `PuzzleSwitch`, `DartTrap`, `Breakable`, `RuneBarrier`, `DashGap`, `SlowZone`, `TorchPuzzle`.

---

## 7. LỘ TRÌNH THỰC HIỆN

| Bước | Nội dung | Xong khi |
|---|---|---|
| **LD1** | Tách `Core.unity` + `M1_Village.unity` từ `World.unity`; greybox M1 theo 3.1; prefab Campfire/NPC/Portal thật | Đi từ S → cổng bắc, nói chuyện Mai/Ông Tễ, đánh 2 cừu, nhận Dash; 0 sprite tròn |
| **LD2** | Art + clutter + light M1; story flags làng | Chụp màn hình M1 đạt mật độ deco, ánh sáng theo bảng 1.4 |
| **LD3** | Tách + dựng lại M2 (greybox → light), bẫy Dash Gap, câu đố bàn thờ, mini-boss arena | Chơi M1→M2→mini-boss liền mạch, ~15 phút |
| **LD4** | Bẫy mới (Slow Zone, Breakable, Rune Barrier, Torch Puzzle) + M3 | M3 hoàn chỉnh, Chìa Khóa Tháp |
| **LD5** | M4 hai tầng + Dart Trap + Collapsing Floor; sửa phần thưởng Agis | Chơi hết game ~45 phút |
| **LD6** | M5 optional + đò tắt + polish | — |
