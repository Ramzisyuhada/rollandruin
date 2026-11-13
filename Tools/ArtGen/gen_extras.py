"""Roll & Ruin - portrait boss, Varga, ikon aplikasi, dan animasi.
Pakai: python gen_extras.py <out_dir>   (memanggil gen_sprites.py juga)
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
sys.argv = [sys.argv[0], OUT]
import gen_sprites as base  # noqa: E402  (ikut regenerate sprite 16x16)

rgba = base.rgba
LOOKUP = {name: g for items in base.SPRITES.values() for name, g in items}

DARK = {"G": "g", "L": "G", "Y": "y", "R": "r", "W": "e", "w": "e", "U": "u", "P": "p",
        "B": "b", "n": "B", "s": "d", "l": "s", "h": "l", "O": "o"}
LIGHT = {"g": "G", "G": "L", "y": "Y", "r": "R", "B": "n", "s": "l", "l": "h", "u": "U",
         "p": "P", "b": "B"}


# ------------------------------------------------------------ grid helpers
def canvas(w, h, fill="."):
    return [[fill] * w for _ in range(h)]


def put(g, x, y, c):
    if 0 <= y < len(g) and 0 <= x < len(g[0]):
        g[y][x] = c


def ell(g, cx, cy, rx, ry, c):
    for y in range(len(g)):
        for x in range(len(g[0])):
            if ((x - cx) / (rx + 0.5)) ** 2 + ((y - cy) / (ry + 0.5)) ** 2 <= 1.0:
                g[y][x] = c


def ell_shaded(g, cx, cy, rx, ry, c):
    ell(g, cx, cy, rx, ry, DARK.get(c, c))
    ell(g, cx - 1, cy - 1, max(rx - 1, 0), max(ry - 1, 0), c)


def rect(g, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(g, x, y, c)


def pts(g, points, c):
    for x, y in points:
        put(g, x, y, c)


def line(g, x0, y0, x1, y1, c):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        put(g, round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), c)


def path(g, points, r, c):
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(n + 1):
            x = x0 + (x1 - x0) * i / n
            y = y0 + (y1 - y0) * i / n
            if r == 0:
                put(g, round(x), round(y), c)
            else:
                ell(g, round(x), round(y), r, r, c)


def annulus(g, cx, cy, ro, ri, c):
    for y in range(len(g)):
        for x in range(len(g[0])):
            d = math.hypot(x - cx, y - cy)
            if ri <= d <= ro:
                g[y][x] = c


def shade(g):
    h, w = len(g), len(g[0])
    out = [row[:] for row in g]

    def empty(x, y):
        return not (0 <= x < w and 0 <= y < h) or g[y][x] == "."

    for y in range(h):
        for x in range(w):
            c = g[y][x]
            if c in ".k":
                continue
            if empty(x + 1, y) or empty(x, y + 1):
                out[y][x] = DARK.get(c, c)
            elif empty(x - 1, y) or empty(x, y - 1):
                out[y][x] = LIGHT.get(c, c)
    return out


def outline(g):
    h, w = len(g), len(g[0])
    out = [row[:] for row in g]
    for y in range(h):
        for x in range(w):
            if g[y][x] != ".":
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and g[ny][nx] not in ".k":
                    out[y][x] = "k"
                    break
    return out


def img(g):
    h, w = len(g), len(g[0])
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h):
        for x in range(w):
            if g[y][x] != ".":
                im.putpixel((x, y), rgba(g[y][x]))
    return im


def save(im, *parts):
    p = os.path.join(OUT, *parts)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    im.save(p)
    return p


# ------------------------------------------------------------ portraits 32x32
def raja_kobra(tongue=True):
    g = canvas(32, 32)
    ell_shaded(g, 16, 28, 12, 3, "G")
    rect(g, 12, 17, 20, 27, "G")
    rect(g, 14, 17, 18, 27, "L")
    for y in (19, 22, 25):
        rect(g, 14, y, 18, y, "G")
    ell_shaded(g, 16, 12, 12, 10, "G")
    ell(g, 16, 14, 8, 7, "L")
    ell(g, 7, 13, 2, 3, "g"); ell(g, 25, 13, 2, 3, "g")
    ell(g, 7, 13, 1, 1, "Y"); ell(g, 25, 13, 1, 1, "Y")
    ell(g, 16, 15, 4, 3, "k")
    ell(g, 16, 11, 6, 5, "k")
    ell(g, 16, 15, 3, 2, "G")
    ell_shaded(g, 16, 11, 5, 4, "G")
    rect(g, 11, 4, 21, 6, "Y")
    pts(g, [(11, 3), (11, 2), (16, 3), (16, 2), (16, 1), (21, 3), (21, 2), (13, 3), (19, 3)], "Y")
    g = shade(g)
    rect(g, 11, 6, 21, 6, "y")
    pts(g, [(16, 5)], "R"); pts(g, [(13, 5), (19, 5)], "U")
    rect(g, 12, 10, 13, 11, "Y"); rect(g, 19, 10, 20, 11, "Y")
    pts(g, [(13, 10), (13, 11), (19, 10), (19, 11)], "r")
    pts(g, [(11, 8), (12, 9), (13, 9), (21, 8), (20, 9), (19, 9)], "k")
    pts(g, [(15, 15), (17, 15)], "g")
    if tongue:
        pts(g, [(16, 17), (16, 18), (15, 19), (17, 19)], "R")
    return outline(g)


def naga_tangga(smoke=True):
    g = canvas(32, 32)
    for x in (6, 9, 22, 25):
        line(g, x, 1, x, 10, "B")
    for y in (3, 6, 9):
        rect(g, 7, y, 8, y, "n"); rect(g, 23, y, 24, y, "n")
    ell_shaded(g, 7, 19, 3, 4, "P"); ell_shaded(g, 25, 19, 3, 4, "P")
    ell_shaded(g, 16, 15, 9, 8, "R")
    ell_shaded(g, 16, 22, 6, 5, "R")
    rect(g, 12, 25, 20, 26, "n")
    g = shade(g)
    rect(g, 10, 11, 13, 11, "r"); rect(g, 19, 11, 22, 11, "r")
    rect(g, 11, 13, 12, 14, "Y"); rect(g, 20, 13, 21, 14, "Y")
    pts(g, [(12, 13), (12, 14), (20, 13), (20, 14)], "k")
    pts(g, [(16, 9), (14, 10), (18, 10), (16, 11)], "r")
    pts(g, [(14, 21), (18, 21)], "k")
    rect(g, 11, 24, 21, 24, "k")
    pts(g, [(12, 25), (14, 25), (18, 25), (20, 25), (13, 26), (19, 26)], "W")
    g = outline(g)
    if smoke:
        pts(g, [(9, 21), (8, 20), (7, 19)], "h")
        pts(g, [(23, 21), (24, 20), (25, 19)], "h")
    return g


def ouroboros():
    g = canvas(32, 32)
    annulus(g, 16, 17, 13, 8, "P")
    annulus(g, 16, 17, 8.9, 8, "Y")
    for a in range(0, 360, 30):
        r = math.radians(a)
        put(g, round(16 + 10.5 * math.cos(r)), round(17 + 10.5 * math.sin(r)), "p")
    for a in (20, 110, 290):
        r = math.radians(a)
        put(g, round(16 + 12 * math.cos(r)), round(17 + 12 * math.sin(r)), "Y")
    ell_shaded(g, 8, 7, 5, 4, "P")
    g = shade(g)
    pts(g, [(5, 3), (6, 2), (8, 2), (9, 1), (11, 3)], "Y")
    pts(g, [(7, 6), (8, 6)], "Y"); put(g, 8, 6, "r")
    rect(g, 9, 8, 13, 9, "k")
    pts(g, [(10, 8), (12, 8)], "W")
    path(g, [(13, 9), (11, 10)], 0, "p")
    for (x, y) in ((16, 14), (15, 15), (16, 15), (17, 15), (14, 16), (15, 16), (16, 16), (17, 16), (18, 16),
                   (15, 17), (16, 17), (17, 17), (16, 18)):
        put(g, x, y, "R")
    pts(g, [(16, 14), (15, 15), (14, 16)], "W")
    pts(g, [(17, 17), (16, 18)], "r")
    return outline(g)


def varga(blink=False):
    g = canvas(32, 32)
    ell_shaded(g, 16, 31, 15, 7, "r")
    ell_shaded(g, 16, 13, 10, 11, "R")
    ell_shaded(g, 16, 16, 6, 6, "n")
    g = shade(g)
    for x in range(9, 24):
        for y in (10, 11):
            if g[y][x] in "nB":
                g[y][x] = "r" if y == 10 else "b"
    pts(g, [(12, 12), (14, 12), (18, 12), (20, 12)], "b")
    if blink:
        pts(g, [(12, 16), (13, 16), (19, 16), (20, 16)], "b")
    else:
        pts(g, [(13, 15), (13, 16), (19, 15), (19, 16)], "k")
        pts(g, [(12, 15), (18, 15)], "W")
    pts(g, [(12, 13), (13, 13), (19, 12), (20, 13)], "b")
    pts(g, [(15, 19), (16, 19), (17, 18)], "b")
    pts(g, [(21, 17), (22, 18)], "r")
    rect(g, 10, 22, 22, 23, "Y"); rect(g, 10, 24, 22, 24, "y")
    line(g, 5, 24, 24, 31, "B")
    pts(g, [(16, 26), (15, 27), (16, 27), (17, 27), (16, 28)], "Y")
    put(g, 16, 28, "y")
    return outline(g)


PORTRAITS = {
    "boss_raja_kobra": raja_kobra(),
    "boss_naga_tangga": naga_tangga(),
    "boss_ouroboros": ouroboros(),
    "char_varga": varga(),
}


def framed(g):
    f = canvas(40, 40, "d")
    for i in range(40):
        for t, c in ((0, "k"), (1, "l"), (2, "s")):
            f[t][i] = f[39 - t][i] = f[i][t] = f[i][39 - t] = c
    for y in range(32):
        for x in range(32):
            if g[y][x] != ".":
                f[y + 4][x + 4] = g[y][x]
    return f


for name, g in PORTRAITS.items():
    save(img(g), "portraits", name + ".png")
    save(img(framed(g)), "portraits", name + "_framed.png")


# ------------------------------------------------------------ app icon
def icon_layers():
    bg = canvas(32, 32)
    for y in range(32):
        for x in range(32):
            d = math.hypot(x - 15.5, y - 15.5)
            bg[y][x] = "s" if d < 8 else ("d" if d < 13 else ("d" if d < 16 and (x + y) % 2 else "k"))
    pts(bg, [(4, 5), (28, 2), (3, 27), (27, 21)], "Y")

    obj = canvas(32, 32)
    for x in (4, 8):
        line(obj, x, 0, x, 31, "n")
    for y in range(2, 32, 5):
        rect(obj, 5, y, 7, y, "B")
    rect(obj, 9, 11, 24, 26, "W")
    for (x, y) in ((9, 11), (24, 11), (9, 26), (24, 26)):
        put(obj, x, y, ".")
    rect(obj, 24, 12, 24, 25, "e"); rect(obj, 10, 26, 23, 26, "e")
    path(obj, [(5, 31), (14, 29), (22, 29), (27, 24), (28, 16), (26, 9)], 2, "G")
    ell(obj, 22, 6, 5, 4, "G")
    obj = shade(obj)
    for (x, y) in ((11, 13), (21, 13), (11, 23), (21, 23)):
        rect(obj, x, y, x + 1, y + 1, "k")
    rect(obj, 16, 18, 17, 19, "R")
    path(obj, [(5, 31), (14, 29), (22, 29), (27, 24), (28, 16), (26, 9)], 0, "L")
    rect(obj, 20, 4, 21, 5, "Y"); pts(obj, [(20, 4), (20, 5)], "k")
    rect(obj, 21, 1, 25, 1, "Y"); pts(obj, [(21, 0), (23, 0), (25, 0)], "Y"); put(obj, 23, 1, "R")
    pts(obj, [(16, 7), (15, 7), (14, 6), (14, 8)], "R")
    return bg, outline(obj)


bg, obj = icon_layers()
full = img(bg)
full.alpha_composite(img(obj))
save(full.resize((512, 512), Image.NEAREST), "app_icon", "app_icon_512.png")
save(full.resize((192, 192), Image.NEAREST), "app_icon", "app_icon_192.png")
fg = Image.new("RGBA", (432, 432), (0, 0, 0, 0))
fg.alpha_composite(img(obj).resize((288, 288), Image.NEAREST), (72, 72))
save(fg, "app_icon", "adaptive_foreground_432.png")
save(img(bg).resize((432, 432), Image.NEAREST), "app_icon", "adaptive_background_432.png")

# ------------------------------------------------------------ animations
ANIMS = {}  # name -> list of PIL frames


def base_img(name):
    return base.to_img(LOOKUP[name])


def on_canvas(sprite, w, h, x=0, y=0):
    c = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    c.alpha_composite(sprite, (x, y))
    return c


def resized_bottom(sprite, w, h, cw, ch):
    s = sprite.resize((w, h), Image.NEAREST)
    return on_canvas(s, cw, ch, (cw - w) // 2, ch - h)


# dice tumble (16x24, pivot bawah); tampilkan sisi hasil asli setelah frame terakhir
for color in ("white", "red", "blue", "gold", "purple"):
    faces = [3, 5, 2, 6, 4, 1]
    offs = [8, 3, 0, 0, 3, 8]
    frames = []
    for i, (f, oy) in enumerate(zip(faces, offs)):
        s = base.to_img(LOOKUP[f"die_{color}_{f}"])
        if i % 2:
            s = s.resize((12, 16), Image.NEAREST)
            frames.append(on_canvas(s, 16, 24, 2, oy))
        else:
            frames.append(on_canvas(s, 16, 24, 0, oy))
    land = base.to_img(LOOKUP[f"die_{color}_1"]).resize((16, 13), Image.NEAREST)
    frames.append(on_canvas(land, 16, 24, 0, 11))
    ANIMS[f"dice_roll_{color}"] = frames

# pawn hop & idle (16x24, pivot bawah)
for who in ("player", "rival"):
    s = base_img(f"pawn_{who}")
    ANIMS[f"pawn_{who}_hop"] = [
        resized_bottom(s, 16, 13, 16, 24),
        on_canvas(s.resize((14, 18), Image.NEAREST), 16, 24, 1, 4),
        on_canvas(s, 16, 24, 0, 3),
        on_canvas(s, 16, 24, 0, 1),
        on_canvas(s, 16, 24, 0, 4),
        resized_bottom(s, 16, 13, 16, 24),
    ]
    ANIMS[f"pawn_{who}_idle"] = [on_canvas(s, 16, 24, 0, 8), resized_bottom(s, 16, 15, 16, 24)]

# snake tongue flick
for v in ("green", "gold", "poison"):
    g = LOOKUP[f"snake_{v}_head"]
    f0 = [row[:] for row in g]; f0[0] = ["."] * 16; f0[1] = ["."] * 16
    f1 = [row[:] for row in g]; f1[0] = ["."] * 16
    ANIMS[f"snake_{v}_tongue"] = [base.to_img(x) for x in (f0, f1, g, f1)]

# slime bounce (16x20)
sl = base_img("enemy_slime")
ANIMS["enemy_slime_bounce"] = [
    on_canvas(sl, 16, 20, 0, 4),
    resized_bottom(sl, 16, 13, 16, 20),
    on_canvas(sl, 16, 20, 0, 4),
    on_canvas(sl.resize((14, 19), Image.NEAREST), 16, 20, 1, 0),
]

# chest open
cc, co = base_img("chest_closed"), base_img("chest_open")
spark = base.to_img(base.from_rows(["..Y.....W.", "..........", "W.........", ".......Y..", "....W....."], center=False))
co_spark = on_canvas(co, 16, 16); co_spark.alpha_composite(spark, (3, 0))
ANIMS["chest_open"] = [cc, on_canvas(cc, 16, 16, -1, 0), on_canvas(cc, 16, 16, 1, 0), co, co_spark]

# trap spikes naik
tg = LOOKUP["trap_spikes"]
plate = next(y for y, row in enumerate(tg) if row.count("k") >= 12)
trap_frames = []
for keep in (0, 2, 5):
    f = [row[:] for row in tg]
    for y in range(0, plate - keep):
        f[y] = ["."] * 16
    trap_frames.append(base.to_img(f))
ANIMS["trap_spikes_activate"] = trap_frames

# campfire flicker
cf = LOOKUP["campfire"]
STEP = {"W": "YW", "Y": "OWY", "O": "oYO", "o": "Oo"}
fire = []
for seed in range(4):
    rnd = random.Random(seed * 11 + 3)
    f = [row[:] for row in cf]
    for y in range(16):
        for x in range(16):
            if f[y][x] in STEP:
                f[y][x] = rnd.choice(STEP[f[y][x]])
    for _ in range(2):
        x, y = rnd.randint(5, 11), rnd.randint(0, 2)
        if f[y][x] == ".":
            f[y][x] = rnd.choice("oO")
    fire.append(base.to_img(f))
ANIMS["campfire_flicker"] = fire

# torch (sudah ada 2 frame) jadi strip
ANIMS["torch_flicker"] = [base_img("torch_0"), base_img("torch_1")]

# coin spin
coin = base_img("icon_coin")
bbox = coin.getbbox()
cropped = coin.crop(bbox)
spin = []
for f in (1.0, 0.7, 0.35, 0.15, 0.35, 0.7):
    w = max(2, round(cropped.width * f))
    s = cropped.resize((w, cropped.height), Image.NEAREST)
    if f in (0.35,) and len(spin) > 3:
        s = s.transpose(Image.FLIP_LEFT_RIGHT)
    spin.append(on_canvas(s, 16, 16, (16 - w) // 2, bbox[1]))
ANIMS["coin_spin"] = spin


# hit flash: siluet putih
def flash(g):
    return [[("W" if c not in ".k" else c) for c in row] for row in g]


for name in ("enemy_goblin", "enemy_skeleton", "enemy_slime", "enemy_mimic"):
    ANIMS[f"{name}_hit"] = [base_img(name), base.to_img(flash(LOOKUP[name])), base_img(name)]
for name, g in PORTRAITS.items():
    if name.startswith("boss"):
        ANIMS[f"{name}_hit"] = [img(g), img(flash(g)), img(g)]

# portrait idle
ANIMS["boss_raja_kobra_idle"] = [img(raja_kobra(True)), img(raja_kobra(False))]
ANIMS["boss_naga_tangga_idle"] = [img(naga_tangga(True)), img(naga_tangga(False))]
ANIMS["char_varga_blink"] = [img(varga(False)), img(varga(False)), img(varga(False)), img(varga(True))]
ou = img(ouroboros())
ANIMS["boss_ouroboros_idle"] = [on_canvas(ou, 32, 34, 0, 1), on_canvas(ou, 32, 34, 0, 0),
                                on_canvas(ou, 32, 34, 0, 1), on_canvas(ou, 32, 34, 0, 2)]

BG = (36, 28, 46, 255)
FPS_MS = {"dice": 70, "pawn_player_hop": 70, "pawn_rival_hop": 70, "coin": 90, "char_varga_blink": 500}
for name, frames in ANIMS.items():
    w, h = frames[0].size
    strip = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, fr in enumerate(frames):
        strip.alpha_composite(fr, (i * w, 0))
        save(fr, "animations", name, f"{name}_{i}.png")
    save(strip, "animations", f"{name}_strip.png")
    sc = 8 if w <= 16 else 5
    gif_frames = []
    for fr in frames:
        b = Image.new("RGBA", (w * sc, h * sc), BG)
        b.alpha_composite(fr.resize((w * sc, h * sc), Image.NEAREST))
        gif_frames.append(b.convert("P", palette=Image.ADAPTIVE))
    dur = next((v for k, v in FPS_MS.items() if name.startswith(k)), 160)
    gp = os.path.join(OUT, "previews", f"{name}.gif")
    os.makedirs(os.path.dirname(gp), exist_ok=True)
    gif_frames[0].save(gp, save_all=True, append_images=gif_frames[1:], duration=dur, loop=0, disposal=2)

# ------------------------------------------------------------ preview sheet
font = ImageFont.load_default()
sheet = Image.new("RGBA", (1000, 1600), BG)
dr = ImageDraw.Draw(sheet)
dr.text((16, 12), "PORTRAITS (32x32, framed 40x40)", fill=(242, 193, 78, 255), font=font)
x = 16
for name, g in PORTRAITS.items():
    sheet.alpha_composite(img(framed(g)).resize((200, 200), Image.NEAREST), (x, 34))
    dr.text((x, 240), name, fill=(220, 214, 230, 255), font=font)
    x += 240
dr.text((16, 272), "APP ICON (512 / adaptive)", fill=(242, 193, 78, 255), font=font)
sheet.alpha_composite(full.resize((256, 256), Image.NEAREST), (16, 294))
sheet.alpha_composite(img(bg).resize((128, 128), Image.NEAREST), (300, 294))
sheet.alpha_composite(img(obj).resize((128, 128), Image.NEAREST), (300, 294))
dr.text((300, 430), "adaptive fg+bg", fill=(220, 214, 230, 255), font=font)
dr.text((16, 572), "ANIMATIONS (semua frame)", fill=(242, 193, 78, 255), font=font)
y = 594
x = 16
for name, frames in ANIMS.items():
    w, h = frames[0].size
    sc = 3 if w <= 16 else 2
    block_w = len(frames) * (w * sc + 2) + 8
    if x + block_w > 990:
        x = 16
        y += 24 * 3 + 22
    for i, fr in enumerate(frames):
        sheet.alpha_composite(fr.resize((w * sc, h * sc), Image.NEAREST), (x + i * (w * sc + 2), y))
    dr.text((x, y + h * sc + 3), name[:26], fill=(200, 194, 210, 255), font=font)
    x += max(block_w, 150)
sheet = sheet.crop((0, 0, 1000, min(1600, y + 34 * 2 + 30)))
sheet.save(os.path.join(OUT, "preview_extras.png"))
print(f"OK: {len(PORTRAITS)} portrait, icon, {len(ANIMS)} animasi")
