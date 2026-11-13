"""Roll & Ruin - generator sprite pixel art 16x16 (tema dungeon fantasi)."""
import os
import random
import sys
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
N = 16

PAL = {
    "k": "#1b1424", "d": "#3a3048", "s": "#5b5070", "l": "#7e7494", "h": "#aaa1bd",
    "g": "#2e5e3a", "G": "#4d9a48", "L": "#93d56b",
    "y": "#b5761e", "Y": "#f2c14e", "W": "#f6efd9", "e": "#b8ad8f", "w": "#e9dfc6",
    "r": "#7f2533", "R": "#d9505c",
    "b": "#5a3820", "B": "#8b5a31", "n": "#c48d54",
    "u": "#2b4a7d", "U": "#4f8edb",
    "p": "#5e3592", "P": "#a873e0",
    "o": "#e2692a", "O": "#ffae45",
}


def rgba(c):
    c = PAL[c].lstrip("#")
    return tuple(int(c[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


def blank():
    return [["." for _ in range(N)] for _ in range(N)]


def from_rows(rows, center=True, name="?"):
    w = max(len(r) for r in rows)
    rows = [r.ljust(w, ".") for r in rows]
    if w > N or len(rows) > N:
        raise ValueError(f"{name}: terlalu besar {w}x{len(rows)}")
    g = blank()
    if not center:
        for y, r in enumerate(rows):
            for x, ch in enumerate(r):
                g[y][x] = ch
        return g
    pts = [(x, y) for y, r in enumerate(rows) for x, ch in enumerate(r) if ch != "."]
    x0 = min(p[0] for p in pts); x1 = max(p[0] for p in pts)
    y0 = min(p[1] for p in pts); y1 = max(p[1] for p in pts)
    ox = (N - (x1 - x0 + 1)) // 2 - x0
    oy = (N - (y1 - y0 + 1)) // 2 - y0
    for x, y in pts:
        g[y + oy][x + ox] = rows[y][x]
    return g


def recolor(g, mapping):
    return [[mapping.get(ch, ch) for ch in row] for row in g]


def to_img(g):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    for y in range(N):
        for x in range(N):
            if g[y][x] != ".":
                im.putpixel((x, y), rgba(g[y][x]))
    return im


SPRITES = {}  # category -> list of (name, grid)


def add(cat, name, g):
    SPRITES.setdefault(cat, []).append((name, g))


# ---------------------------------------------------------------- tiles
def slab_floor(seed, moss=False, crack=False):
    rnd = random.Random(seed)
    g = [["s"] * N for _ in range(N)]
    for oy in (0, 8):
        for ox in (0, 8):
            for i in range(8):
                g[oy][ox + i] = "l"
                g[oy + i][ox] = "l"
                g[oy + 7][ox + i] = "d"
                g[oy + i][ox + 7] = "d"
            g[oy][ox + 7] = "s"; g[oy + 7][ox] = "s"
    for _ in range(7):
        x, y = rnd.randint(1, 14), rnd.randint(1, 14)
        if g[y][x] == "s":
            g[y][x] = rnd.choice("dl")
    if crack:
        x, y = 3, 2
        for _ in range(9):
            g[y][x] = "k"
            y += 1; x += rnd.choice((0, 1))
            if y > 13 or x > 13:
                break
    if moss:
        for _ in range(3):
            cx, cy = rnd.randint(1, 14), rnd.randint(1, 14)
            for dx, dy in ((0, 0), (1, 0), (0, 1), (-1, 0), (1, 1)):
                x, y = cx + dx, cy + dy
                if 0 <= x < N and 0 <= y < N:
                    g[y][x] = rnd.choice("gGG")
    return g


def brick_wall():
    g = [["s"] * N for _ in range(N)]
    for y in range(N):
        row = y // 4
        off = 4 if row % 2 else 0
        for x in range(N):
            if y % 4 == 3 or (x + off) % 8 == 7:
                g[y][x] = "k"
            elif y % 4 == 0:
                g[y][x] = "l"
            elif y % 4 == 2:
                g[y][x] = "d" if (x + off) % 8 > 4 else "s"
    return g


def rim(color):
    g = blank()
    for i in range(N):
        for t in (0, 1):
            g[t][i] = color; g[N - 1 - t][i] = color
            g[i][t] = color; g[i][N - 1 - t] = color
    return g


add("tiles", "floor_a", slab_floor(1))
add("tiles", "floor_b", slab_floor(7))
add("tiles", "floor_cracked", slab_floor(3, crack=True))
add("tiles", "floor_mossy", slab_floor(5, moss=True))
add("tiles", "wall_brick", brick_wall())
add("tiles", "tile_highlight", rim("Y"))
add("tiles", "tile_danger", rim("R"))
add("tiles", "tile_rival", rim("P"))

# ---------------------------------------------------------------- dice
PIPS = {
    1: [(7, 7)],
    2: [(3, 3), (11, 11)],
    3: [(3, 3), (7, 7), (11, 11)],
    4: [(3, 3), (11, 3), (3, 11), (11, 11)],
    5: [(3, 3), (11, 3), (7, 7), (3, 11), (11, 11)],
    6: [(3, 3), (11, 3), (3, 7), (11, 7), (3, 11), (11, 11)],
}


def die(n, body="W", shade="e", pip="k"):
    g = blank()
    for y in range(1, 15):
        for x in range(1, 15):
            g[y][x] = body
    for i in range(2, 14):
        g[0][i] = g[15][i] = g[i][0] = g[i][15] = "k"
    for (x, y) in ((1, 1), (14, 1), (1, 14), (14, 14)):
        g[y][x] = "k"
    for i in range(2, 14):
        g[14][i] = shade; g[i][14] = shade
    g[13][13] = shade
    for i in range(2, 13):
        g[1][i] = "W" if body != "W" else body
    for (x, y) in PIPS.get(n, []):
        c = "R" if (n == 1 and body == "W") else pip
        for dx in (0, 1):
            for dy in (0, 1):
                g[y + dy][x + dx] = c
    return g


for n in range(1, 7):
    add("dice", f"die_white_{n}", die(n))
add("dice", "die_white_0", die(0))
DICE_COLORS = {"red": ("R", "r", "W"), "blue": ("U", "u", "W"), "gold": ("Y", "y", "k"), "purple": ("P", "p", "W")}
for cname, (body, shade, pip) in DICE_COLORS.items():
    for n in range(0, 7):
        add("dice", f"die_{cname}_{n}", die(n, body, shade, pip))

# ---------------------------------------------------------------- pawns
PAWN = [
    "................",
    "......kkkk......",
    ".....kXXXXk.....",
    "....kXhXXXXk....",
    "....kXXXXXxk....",
    "....kXXXXxxk....",
    ".....kxxxxk.....",
    "......kXXk......",
    ".....kXXXxk.....",
    ".....kXhXxk.....",
    "....kXXXXxxk....",
    "....kXhXXxxk....",
    "...kXXXXXXxxk...",
    "..kkkkkkkkkkkk..",
    "..kxxxxxxxxxxk..",
    "..kkkkkkkkkkkk..",
]
player = [r for r in PAWN]
player[0] = "......kYYk......"
rival = [r for r in PAWN]
rival[0] = ".......kk......."
rival[4] = "....kkkkkkxk...."
add("pawns", "pawn_player", recolor(from_rows(player, center=False), {"X": "U", "x": "u", "h": "W"}))
add("pawns", "pawn_rival", recolor(from_rows(rival, center=False), {"X": "R", "x": "r", "h": "W"}))

# ---------------------------------------------------------------- snake
SNAKE_HEAD = [
    "......R..R......",
    ".......RR.......",
    ".....kkkkkk.....",
    "....kGGGGGGk....",
    "...kGLGGGGLGk...",
    "...kGWkGGWkGk...",
    "...kGGGGGGGGk...",
    "...kgGGGGGGgk...",
    "....kgGLLGgk....",
    "....kgGLLGgk....",
    ".....kgLLgk.....",
    ".....kGLLGk.....",
    ".....kgLLgk.....",
    ".....kgGGgk.....",
    ".....kgLLgk.....",
    ".....kGLLGk.....",
]
BODY_A = ".....kgLLgk....."
BODY_B = ".....kGLLGk....."
BODY_D = ".....kgGGgk....."
SNAKE_BODY = [BODY_A, BODY_B, BODY_A, BODY_D] * 4
SNAKE_TAIL = [BODY_A, BODY_B, BODY_A, BODY_D, BODY_A, BODY_B,
              "......kLgk......",
              "......kLgk......",
              "......kgk.......",
              ".......kgk......",
              ".......kk.......",
              "................", "................", "................", "................", "................"]
SNAKE_VARIANTS = {
    "green": {},
    "gold": {"G": "Y", "g": "y", "L": "W"},
    "poison": {"G": "P", "g": "p", "L": "L"},
}
for vname, m in SNAKE_VARIANTS.items():
    for part, rows in (("head", SNAKE_HEAD), ("body", SNAKE_BODY), ("tail", SNAKE_TAIL)):
        add("snakes", f"snake_{vname}_{part}", recolor(from_rows(rows, center=False), m))


# ---------------------------------------------------------------- ladders
def ladder(rail, rail_dark, rung, rung_dark, broken_rung=False, knots=False):
    g = blank()
    for y in range(N):
        for x0 in (2, 10):
            g[y][x0] = "k"; g[y][x0 + 1] = rail; g[y][x0 + 2] = rail_dark; g[y][x0 + 3] = "k"
            if knots and y % 4 == 0:
                g[y][x0 + 1] = rail_dark
    for r0 in (2, 10):
        for x in range(6, 10):
            g[r0][x] = "k"; g[r0 + 1][x] = rung; g[r0 + 2][x] = rung_dark; g[r0 + 3][x] = "k"
    if broken_rung:
        for y in range(10, 14):
            g[y][7] = "."; g[y][8] = "."
        g[11][6] = g[12][9] = "k"
    return g


def chain():
    g = blank()
    link_a = [".ll.", "l..s", "l..s", ".ss."]
    link_b = [".kk.", ".hk.", ".lk.", ".kk."]
    for y in range(N):
        pattern = link_a if (y // 4) % 2 == 0 else link_b
        row = pattern[y % 4]
        for x0 in (3, 9):
            for i, ch in enumerate(row):
                g[y][x0 + i] = ch
    return g


add("ladders", "ladder_stone", ladder("h", "l", "l", "s"))
add("ladders", "ladder_rope", ladder("n", "B", "B", "b", knots=True))
add("ladders", "ladder_rope_broken", ladder("n", "B", "B", "b", broken_rung=True, knots=True))
add("ladders", "lift_chain", chain())
LIFT_CABIN = [
    ".......kk.......",
    ".......kk.......",
    "..kkkkkkkkkkkk..",
    "..kyYYYYYYYYyk..",
    "..kYbbbbbbbbYk..",
    "..kYbddddddbYk..",
    "..kYbddddddbYk..",
    "..kYbbbbbbbbYk..",
    "..kYBBBBBBBBYk..",
    "..kYBnBBBBnBYk..",
    "..kYBBBBBBBBYk..",
    "..kyyyyyyyyyyk..",
    "..kkkkkkkkkkkk..",
]
add("ladders", "lift_cabin", from_rows(LIFT_CABIN, name="lift_cabin"))

# ---------------------------------------------------------------- tile icons
ICONS = {
    "chest_closed": [
        "...kkkkkkkkkk...",
        "..kbBBBBBBBBbk..",
        "..kBnnnnnnnnBk..",
        "..kBBBBBBBBBBk..",
        "..kyyyyYYyyyyk..",
        "..kbBBBYYBBBbk..",
        "..kBBBBkkBBBBk..",
        "..kBBBBBBBBBBk..",
        "..kbBBBBBBBBbk..",
        "..kyyyyyyyyyyk..",
        "..kkkkkkkkkkkk..",
    ],
    "chest_open": [
        "...kkkkkkkkkk...",
        "..kbBBBBBBBBbk..",
        "..kBnnnnnnnnBk..",
        "..kyyyyyyyyyyk..",
        "..kkkkkkkkkkkk..",
        "..kYWYYYYWYYYk..",
        "..kyYYyYYYyYYk..",
        "..kbBBBBBBBBbk..",
        "..kBBBBBBBBBBk..",
        "..kBBBBBBBBBBk..",
        "..kbBBBBBBBBbk..",
        "..kyyyyyyyyyyk..",
        "..kkkkkkkkkkkk..",
    ],
    "trap_spikes": [
        "...k...k...k....",
        "..khk.khk.khk...",
        "..khk.khk.khk...",
        "..klk.klk.klk...",
        ".kllskllskllsk..",
        ".kkkkkkkkkkkkkk.",
        ".kssssssssssssk.",
        ".kdsdsdsdsdsdsk.",
        ".kkkkkkkkkkkkkk.",
    ],
    "campfire": [
        "........o.......",
        ".......oo.......",
        "......oOo..o....",
        "......oOOo.oo...",
        ".....oOYOo.oOo..",
        ".....oOYYOooOo..",
        "....oOYYWYOOOo..",
        "....oOYWWYYOo...",
        ".....oOYYYOo....",
        "..kbbBoOOOoBbbk.",
        ".kBnBBbkkbBBnBk.",
        "..kbbBBkkBBbbk..",
        "...kkkk..kkkk...",
    ],
    "shop_bag": [
        "......kkkk......",
        ".....kBnnBk.....",
        "......kbbk......",
        ".....kYyyYk.....",
        "....kBBnnBBk....",
        "...kBnnnnBBBk...",
        "..kBnnBBBBBBBk..",
        "..kBnBBYYBBBbk..",
        "..kBBBYyyYBBbk..",
        "..kBBBBYYBBBbk..",
        "..kbBBBBBBBbbk..",
        "...kbbbbbbbbk...",
        "....kkkkkkkk....",
    ],
    "event_scroll": [
        "...kkkkkkkkkk...",
        "..knWWWWWWWWnk..",
        "...kWWWkkWWWk...",
        "...kWWkWWkWWk...",
        "...kWWWWWkWWk...",
        "...kWWWWkWWWk...",
        "...kWWWkWWWWk...",
        "...kWWWkWWWWk...",
        "...kWWWWWWWWk...",
        "...kWWWkWWWWk...",
        "...kWWWWWWWWk...",
        "..knWWWWWWWWnk..",
        "...kkkkkkkkkk...",
    ],
    "torch_0": [
        ".......o........",
        "......oOo.......",
        "......OYO.......",
        ".....oYWYo......",
        "......OYO.......",
        ".....kkkkk......",
        ".....kBnBk......",
        "......kBk.......",
        "......kBk.......",
        "......kbk.......",
        "......kbk.......",
        ".......k........",
    ],
    "torch_1": [
        "......o.........",
        ".......oo.......",
        "......oYO.......",
        "......OWYo......",
        "......OYO.......",
        ".....kkkkk......",
        ".....kBnBk......",
        "......kBk.......",
        "......kBk.......",
        "......kbk.......",
        "......kbk.......",
        ".......k........",
    ],
}
SKULL = [
    "....kkkkkkkk....",
    "...kwwwwwwwwk...",
    "..kwwwwwwwwwwk..",
    "..kwwwwwwwwwek..",
    "..kwkEkwwkEkek..",
    "..kwkkkwwkkkek..",
    "..kwwwwkkwwwek..",
    "...kwwwwwwwek...",
    "....kwkwkwek....",
    "....kkkkkkkk....",
]
curse = SKULL + ["................", "..P..........P..", "................", ".P............P."]
ICONS["curse_skull"] = [r.replace("E", "P") for r in curse]

for name, rows in ICONS.items():
    add("board_icons", name, from_rows(rows, name=name))

# ---------------------------------------------------------------- enemies
ENEMIES = {
    "enemy_goblin": [
        ".....kkkkkk.....",
        "kk..kGGGGGGk..kk",
        "kGk.kGGGGGGk.kGk",
        ".kGkGGGGGGGGkGk.",
        "..kGGYYGGYYGGk..",
        "...kGGGGGGGGk...",
        "...kGgkkkkgGk...",
        "...kGkWkkWkGk...",
        "....kGGGGGGk....",
        ".....kkkkkk.....",
    ],
    "enemy_skeleton": [r.replace("E", "R") for r in SKULL],
    "enemy_slime": [
        "......kkkk......",
        "....kkUUUUkk....",
        "...kUWUUUUUUk...",
        "..kUWUUUUUUUUk..",
        "..kUUkUUUUkUUk..",
        ".kUUUkUUUUkUUuk.",
        ".kUUUUUUUUUUuuk.",
        ".kuuUUUUUUUuuuk.",
        "..kkkkkkkkkkkk..",
    ],
    "enemy_mimic": [
        "...kkkkkkkkkk...",
        "..kbBBBBBBBBbk..",
        "..kBRBBBBBBRBk..",
        "..kyyyyyyyyyyk..",
        "..kWkWkWWkWkWk..",
        "..kRRrrrrrrRRk..",
        "..kWkWkWWkWkWk..",
        "..kyyyyyyyyyyk..",
        "..kbBBBBBBBBbk..",
        "..kBBBBBBBBBBk..",
        "..kbBBBBBBBBbk..",
        "..kkkkkkkkkkkk..",
    ],
}
for name, rows in ENEMIES.items():
    add("enemies", name, from_rows(rows, name=name))

# ---------------------------------------------------------------- UI + relics
COIN = [
    ".....kkkkkk.....",
    "....kYYYYYYk....",
    "...kYWYYYYYyk...",
    "...kYYYyyYYyk...",
    "...kYYyYYyYyk...",
    "...kYYyYYyYyk...",
    "...kYYYyyYYyk...",
    "...kYYYYYYYyk...",
    "....kyYYYYyk....",
    ".....kkkkkk.....",
]
HEART = [
    "..kkkk..kkkk....",
    ".kRRRRkkRRRRk...",
    "kRWRRRRRRRRRrk..",
    "kRRRRRRRRRRRrk..",
    "kRRRRRRRRRRRrk..",
    ".kRRRRRRRRRrk...",
    "..kRRRRRRRrk....",
    "...kRRRRRrk.....",
    "....kRRRrk......",
    ".....kRrk.......",
    "......kk........",
]
SCALE = [
    ".......kk.......",
    "......kGGk......",
    ".....kGLGGk.....",
    "....kGLGGGgk....",
    "...kGLGGGGGgk...",
    "..kGLGGGGGGGgk..",
    "..kGGGGGGGGggk..",
    "...kGGGGGGggk...",
    "....kgGGGggk....",
    ".....kgggk......",
    "......kgk.......",
    ".......k........",
]
add("ui", "icon_coin", from_rows(COIN))
add("ui", "icon_heart", from_rows(HEART))
add("ui", "icon_sisik", from_rows(SCALE))


def panel(fill, light, dark):
    g = [[fill] * N for _ in range(N)]
    for i in range(N):
        g[0][i] = g[N - 1][i] = g[i][0] = g[i][N - 1] = "k"
    for i in range(1, N - 1):
        g[1][i] = light; g[i][1] = light
        g[N - 2][i] = dark; g[i][N - 2] = dark
    g[0][0] = g[0][N - 1] = g[N - 1][0] = g[N - 1][N - 1] = "."
    return g


add("ui", "panel_stone_9slice", panel("d", "l", "k"))
add("ui", "button_wood_9slice", panel("B", "n", "b"))
add("ui", "button_wood_pressed_9slice", panel("b", "B", "B"))
add("ui", "button_gold_9slice", panel("Y", "W", "y"))

RELICS = {
    "relic_sisik_ular": from_rows(SCALE),
    "relic_taring_patah": from_rows([
        "....kkkkkkk.....",
        "...kwwwwwwwk....",
        "...kwwwwwwek....",
        "....kwwwwek.....",
        "....kwwwwek.....",
        ".....kwwek......",
        ".....kwwek......",
        ".....kwek.......",
        "......kk........",
        "................",
        ".......kwk......",
        ".......kek......",
        "........k.......",
    ]),
    "relic_batu_berat": from_rows([
        ".....kkkkk......",
        "....klllssk.....",
        "...klhllsssk....",
        "..klhlllsssdk...",
        "..kllllssssdk...",
        ".klllsssssdddk..",
        ".klssssssdddk...",
        ".ksssssdddddk...",
        "..kkkkkkkkkkk...",
    ]),
    "relic_peta_kusam": from_rows([
        "..kkkkkkkkkkkk..",
        "..knWWWWWWWWnk..",
        "..kWRWRWWWWWWk..",
        "..kWWRWWWWWWWk..",
        "..kWRWRWWWWWWk..",
        "..kWWWWdWWWWWk..",
        "..kWWWWWWdWWWk..",
        "..kWWWWWWWWdWk..",
        "..kWWWWWWWWWdk..",
        "..knWWWWWWWWnk..",
        "..kkkkkkkkkkkk..",
    ]),
    "relic_lonceng_pengkhianat": from_rows([
        ".......kk.......",
        "......kYYk......",
        ".....kYWYYk.....",
        "....kYWYYYyk....",
        "....kYWYYYyk....",
        "....kYYYYYyk....",
        "...kYYYYYYYyk...",
        "...kYYYYYYYyk...",
        "..kYYYYYYYYYyk..",
        "..kyyyyyyyyyyk..",
        "..kkkkkkkkkkkk..",
        "......kyyk......",
        ".......kk.......",
    ]),
}
# Koin Bermuka Dua: separuh kanan koin jadi ungu
two_face = from_rows(COIN)
for y in range(N):
    for x in range(8, N):
        two_face[y][x] = {"Y": "P", "y": "p", "W": "h"}.get(two_face[y][x], two_face[y][x])
RELICS["relic_koin_bermuka_dua"] = two_face
# Tangga Lipat: tangga kayu kecil + kilau
tl = ladder("n", "B", "B", "b")
for y in range(N):
    for x in range(N):
        if y < 1 or y > 14:
            tl[y][x] = "."
for (x, y) in ((1, 1), (14, 6), (0, 12)):
    tl[y][x] = "Y"
RELICS["relic_tangga_lipat"] = tl
for name, g in RELICS.items():
    add("relics", name, g)

# ---------------------------------------------------------------- export
count = 0
for cat, items in SPRITES.items():
    d = os.path.join(OUT, "sprites", cat)
    os.makedirs(d, exist_ok=True)
    for name, g in items:
        to_img(g).save(os.path.join(d, name + ".png"))
        count += 1

# preview sheet
SCALE_F = 5
CELL = N * SCALE_F
PAD = 14
COLS = 8
font = ImageFont.load_default()
sections = list(SPRITES.items())
height = PAD
for cat, items in sections:
    rows = (len(items) + COLS - 1) // COLS
    height += 22 + rows * (CELL + 26)
width = PAD + COLS * (CELL + PAD)
sheet = Image.new("RGBA", (width, height), (36, 28, 46, 255))
dr = ImageDraw.Draw(sheet)
yy = PAD
for cat, items in sections:
    dr.text((PAD, yy), cat.upper(), fill=(242, 193, 78, 255), font=font)
    yy += 22
    for i, (name, g) in enumerate(items):
        cx = PAD + (i % COLS) * (CELL + PAD)
        cy = yy + (i // COLS) * (CELL + 26)
        dr.rectangle([cx - 1, cy - 1, cx + CELL, cy + CELL], fill=(52, 42, 64, 255))
        big = to_img(g).resize((CELL, CELL), Image.NEAREST)
        sheet.alpha_composite(big, (cx, cy))
        label = name if len(name) <= 16 else name[:15] + "~"
        dr.text((cx, cy + CELL + 4), label, fill=(220, 214, 230, 255), font=font)
    yy += ((len(items) + COLS - 1) // COLS) * (CELL + 26)
sheet.save(os.path.join(OUT, "preview_sheet.png"))


# mockup papan kecil: 6x5 petak, ular, tangga, pion
def paste(canvas, g, tx, ty, s):
    canvas.alpha_composite(to_img(g).resize((N * s, N * s), Image.NEAREST), (tx * N * s, ty * N * s))


lookup = {name: g for items in SPRITES.values() for name, g in items}
S = 4
W_, H_ = 6, 7
mock = Image.new("RGBA", (W_ * N * S, H_ * N * S), (27, 20, 36, 255))
for tx in range(W_):
    paste(mock, lookup["wall_brick"], tx, 0, S)
for ty in range(1, H_):
    for tx in range(W_):
        key = ["floor_a", "floor_b", "floor_cracked", "floor_mossy"][(tx * 3 + ty * 5) % 7 % 4]
        paste(mock, lookup[key], tx, ty, S)
paste(mock, lookup["torch_0"], 1, 0, S)
paste(mock, lookup["torch_1"], 4, 0, S)
paste(mock, lookup["ladder_stone"], 1, 2, S)
paste(mock, lookup["ladder_stone"], 1, 3, S)
paste(mock, lookup["ladder_stone"], 1, 4, S)
paste(mock, lookup["snake_green_head"], 4, 2, S)
paste(mock, lookup["snake_green_body"], 4, 3, S)
paste(mock, lookup["snake_green_tail"], 4, 4, S)
paste(mock, lookup["ladder_rope_broken"], 5, 4, S)
paste(mock, lookup["ladder_rope"], 5, 5, S)
paste(mock, lookup["tile_highlight"], 2, 5, S)
paste(mock, lookup["chest_closed"], 3, 1, S)
paste(mock, lookup["trap_spikes"], 0, 5, S)
paste(mock, lookup["campfire"], 3, 6, S)
paste(mock, lookup["enemy_slime"], 0, 1, S)
paste(mock, lookup["pawn_player"], 2, 5, S)
paste(mock, lookup["tile_rival"], 3, 4, S)
paste(mock, lookup["pawn_rival"], 3, 4, S)
paste(mock, lookup["shop_bag"], 5, 1, S)
paste(mock, lookup["curse_skull"], 0, 3, S)
mock.save(os.path.join(OUT, "mockup_board.png"))
print(f"OK: {count} sprite")
