"""Sinh sprite pixel-art 64 px/tile cho các vật thể thiếu trong Tiny Swords (rương, cần gạt, bẫy...).
Bảng màu + viền tối khớp Tiny Swords. Chạy: python3 tools/gen_pixel_props.py <thư mục đích>"""
import sys, os
from PIL import Image, ImageDraw

OUT = sys.argv[1] if len(sys.argv) > 1 else "Assets/_Project/Art/Generated"
os.makedirs(OUT, exist_ok=True)

OUTLINE = (27, 25, 38, 255)
WOOD_D, WOOD, WOOD_L = (92, 56, 40, 255), (139, 89, 58, 255), (184, 128, 82, 255)
STONE_D, STONE, STONE_L = (74, 84, 96, 255), (112, 126, 138, 255), (160, 174, 180, 255)
GOLD_D, GOLD, GOLD_L = (168, 112, 32, 255), (230, 174, 52, 255), (255, 226, 120, 255)
RUNE, RUNE_L = (132, 72, 220, 255), (196, 150, 255, 255)
RED, RED_L = (176, 44, 52, 255), (232, 96, 88, 255)
MUD_D, MUD, MUD_L = (70, 50, 38, 255), (98, 72, 52, 255), (128, 98, 70, 255)
SHADOW = (20, 20, 30, 90)

def canvas(w=64, h=64):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))

def box(d, x0, y0, x1, y1, fill, outline=OUTLINE):
    d.rectangle([x0, y0, x1, y1], fill=outline)
    d.rectangle([x0 + 2, y0 + 2, x1 - 2, y1 - 2], fill=fill)

def ellipse_shadow(d, cx, cy, rx, ry):
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=SHADOW)

def save(im, name):
    im.save(os.path.join(OUT, name + ".png"))

# ── Rương đóng / mở ──
def chest(open_):
    im = canvas(); d = ImageDraw.Draw(im)
    ellipse_shadow(d, 32, 54, 22, 5)
    box(d, 10, 30, 54, 54, WOOD)                        # thân
    d.rectangle([12, 32, 52, 35], fill=WOOD_L)
    for x in (12, 50):                                   # nẹp kim loại
        d.rectangle([x, 32, x + 2, 52], fill=GOLD_D)
    if open_:
        box(d, 10, 14, 54, 30, WOOD_D)                  # nắp mở ra sau
        d.rectangle([14, 26, 50, 30], fill=GOLD_L)       # vàng bên trong
        d.rectangle([20, 24, 26, 27], fill=GOLD)
        d.rectangle([36, 25, 44, 28], fill=GOLD)
    else:
        box(d, 8, 20, 56, 34, WOOD)                     # nắp
        d.rectangle([10, 22, 54, 24], fill=WOOD_L)
        box(d, 27, 28, 37, 40, GOLD)                    # ổ khoá
        d.rectangle([31, 33, 33, 36], fill=OUTLINE)
    return im
save(chest(False), "chest_closed"); save(chest(True), "chest_open")

# ── Cần gạt (công tắc) tắt / bật ──
def lever(on):
    im = canvas(); d = ImageDraw.Draw(im)
    ellipse_shadow(d, 32, 54, 18, 5)
    box(d, 16, 40, 48, 54, STONE)
    d.rectangle([18, 42, 46, 44], fill=STONE_L)
    # cần
    if on:
        pts = [(30, 42), (34, 42), (48, 18), (44, 16)]
        knob = (42, 10, 52, 20)
    else:
        pts = [(30, 42), (34, 42), (20, 18), (16, 20)]
        knob = (12, 12, 22, 22)
    d.polygon(pts, fill=OUTLINE)
    d.polygon([(p[0] + (1 if i < 2 else 1), p[1]) for i, p in enumerate(pts)], fill=WOOD)
    d.ellipse(knob, fill=OUTLINE)
    d.ellipse((knob[0] + 2, knob[1] + 2, knob[2] - 2, knob[3] - 2), fill=(90, 200, 110, 255) if on else RED)
    return im
save(lever(False), "lever_off"); save(lever(True), "lever_on")

# ── Bẫy gai: tấm đá có lỗ + đầu gai ──
im = canvas(); d = ImageDraw.Draw(im)
box(d, 4, 10, 60, 58, STONE_D)
d.rectangle([6, 12, 58, 15], fill=STONE)
for gx in (14, 30, 46):
    for gy in (22, 40):
        d.polygon([(gx - 5, gy + 8), (gx + 5, gy + 8), (gx, gy - 4)], fill=OUTLINE)
        d.polygon([(gx - 3, gy + 7), (gx + 3, gy + 7), (gx, gy - 1)], fill=STONE_L)
save(im, "spike_plate")

# ── Cột đuốc câu đố ──
im = canvas(64, 96); d = ImageDraw.Draw(im)
ellipse_shadow(d, 32, 88, 14, 4)
box(d, 26, 40, 38, 90, WOOD)
d.rectangle([28, 42, 30, 88], fill=WOOD_L)
box(d, 18, 28, 46, 42, STONE)                           # bát lửa
d.rectangle([20, 30, 44, 32], fill=STONE_L)
d.rectangle([22, 24, 42, 30], fill=OUTLINE)
d.rectangle([24, 25, 40, 29], fill=(60, 40, 34, 255))   # than
save(im, "torch_post")

# ── Rào rune 1 tile ──
for i in range(2):
    im = canvas(); d = ImageDraw.Draw(im)
    for x in range(0, 64, 4):
        a = 120 + ((x // 4 + i) % 3) * 40
        d.rectangle([x, 4, x + 1, 60], fill=(RUNE[0], RUNE[1], RUNE[2], a))
    d.rectangle([0, 0, 63, 3], fill=OUTLINE); d.rectangle([0, 60, 63, 63], fill=OUTLINE)
    for gx, gy in ((12, 18), (32, 36), (50, 20), (22, 48), (44, 50)):
        o = (i * 2)
        d.rectangle([gx - 3, gy - 3 + o, gx + 3, gy + 3 + o], fill=RUNE_L)
        d.rectangle([gx - 1, gy - 1 + o, gx + 1, gy + 1 + o], fill=(255, 255, 255, 255))
    save(im, f"rune_barrier_{i}")

# ── Tượng bắn tên (nhìn xuống) ──
im = canvas(64, 96); d = ImageDraw.Draw(im)
ellipse_shadow(d, 32, 88, 22, 5)
box(d, 12, 56, 52, 90, STONE_D)                         # bệ
d.rectangle([14, 58, 50, 61], fill=STONE)
box(d, 16, 14, 48, 60, STONE)                           # thân tượng
d.rectangle([18, 16, 46, 19], fill=STONE_L)
box(d, 20, 22, 44, 44, STONE_D)                         # mặt
for ex in (25, 35):
    d.rectangle([ex, 28, ex + 4, 32], fill=OUTLINE)     # hốc mắt (sáng đỏ khi telegraph — tint sprite)
d.rectangle([28, 38, 36, 42], fill=OUTLINE)             # miệng bắn
save(im, "dart_statue")

# ── Sàn đá nứt (sàn sụp) ──
im = canvas(); d = ImageDraw.Draw(im)
box(d, 0, 0, 63, 63, STONE)
for x0, y0 in ((2, 2), (32, 2), (2, 32), (32, 32)):
    d.rectangle([x0, y0, x0 + 29, y0 + 29], fill=STONE)
    d.rectangle([x0, y0, x0 + 29, y0 + 2], fill=STONE_L)
d.line([(8, 10), (20, 24), (18, 36), (30, 44), (44, 40), (54, 54)], fill=OUTLINE, width=2)
d.line([(20, 24), (34, 20), (40, 8)], fill=OUTLINE, width=2)
save(im, "floor_cracked")

# ── Cổng đá dịch chuyển (2 cột + rune) ──
im = canvas(128, 128); d = ImageDraw.Draw(im)
ellipse_shadow(d, 64, 118, 52, 8)
for x in (14, 94):
    box(d, x, 30, x + 20, 120, STONE)
    d.rectangle([x + 2, 32, x + 5, 118], fill=STONE_L)
box(d, 8, 18, 120, 36, STONE_D)                         # đà ngang
d.rectangle([10, 20, 118, 23], fill=STONE)
for gx in (40, 64, 88):
    d.rectangle([gx - 3, 25, gx + 3, 31], fill=RUNE_L)
save(im, "portal_stones")

# ── Rào gỗ (cổng làng / khoá arena) ──
im = canvas(); d = ImageDraw.Draw(im)
ellipse_shadow(d, 32, 58, 30, 4)
for x in (4, 22, 40):
    d.polygon([(x, 18), (x + 9, 6), (x + 18, 18), (x + 18, 58), (x, 58)], fill=OUTLINE)
    d.polygon([(x + 2, 18), (x + 9, 9), (x + 16, 18), (x + 16, 56), (x + 2, 56)], fill=WOOD)
    d.rectangle([x + 3, 20, x + 5, 54], fill=WOOD_L)
d.rectangle([0, 28, 63, 33], fill=OUTLINE); d.rectangle([0, 29, 63, 31], fill=WOOD_D)
d.rectangle([0, 44, 63, 49], fill=OUTLINE); d.rectangle([0, 45, 63, 47], fill=WOOD_D)
save(im, "palisade")

# ── Cột đèn lồng ──
im = canvas(64, 128); d = ImageDraw.Draw(im)
ellipse_shadow(d, 32, 120, 12, 4)
box(d, 28, 30, 36, 122, WOOD_D)
box(d, 28, 22, 52, 30, WOOD)                            # cần treo
box(d, 40, 30, 54, 50, (60, 40, 34, 255))               # lồng đèn
d.rectangle([43, 34, 51, 46], fill=GOLD_L)
save(im, "lantern_post")

# ── Bùn (2 frame bọt) ──
for i in range(2):
    im = canvas(); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 63, 63], fill=MUD)
    for (x, y) in ((6, 10), (30, 6), (48, 22), (14, 40), (40, 46), (54, 56)):
        d.rectangle([x, y, x + 6, y + 2], fill=MUD_D)
    bubbles = [(20, 22), (44, 34)] if i == 0 else [(26, 30), (12, 52)]
    for (x, y) in bubbles:
        d.rectangle([x - 2, y - 2, x + 2, y + 2], fill=MUD_L)
        d.rectangle([x - 1, y - 3, x + 1, y - 3], fill=MUD_L)
    save(im, f"mud_{i}")

print("ok", sorted(os.listdir(OUT)))
