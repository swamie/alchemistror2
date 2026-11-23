"""
Generates every 2D icon the mod embeds: skill icons, potion icons, crow item icon, buff icons.

Run from the repo root:  python3 art/make_icons.py
Needs Pillow. Potion and crow icons are built from Blender renders in art/renders/.
Output goes to src/AlchemistMod/Assets/Icons/ and is embedded into the dll at build time.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RENDERS = os.path.join(ROOT, "art", "renders")
OUT = os.path.join(ROOT, "src", "AlchemistMod", "Assets", "Icons")
os.makedirs(OUT, exist_ok=True)

W = 512          # working size
SIZE = 128       # skill / potion icon output size
BUFF = 64        # buff icon output size
CREAM = (236, 228, 205, 255)
DARK = (18, 16, 20, 255)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def background(top, bottom, glow=None):
    """RoR2-style square skill frame: vertical gradient, soft centre glow, dark rim."""
    img = Image.new("RGBA", (W, W))
    px = img.load()
    for y in range(W):
        c = lerp(bottom, top, 1 - y / W)
        for x in range(W):
            px[x, y] = (*c, 255)
    if glow:
        g = Image.new("L", (W, W), 0)
        ImageDraw.Draw(g).ellipse((W * 0.15, W * 0.15, W * 0.85, W * 0.85), fill=150)
        g = g.filter(ImageFilter.GaussianBlur(W * 0.12))
        img = Image.composite(Image.new("RGBA", (W, W), (*glow, 255)), img, g)
    rim = Image.new("L", (W, W), 0)
    d = ImageDraw.Draw(rim)
    d.rectangle((0, 0, W - 1, W - 1), outline=255, width=int(W * 0.035))
    rim = rim.filter(ImageFilter.GaussianBlur(6))
    img = Image.composite(Image.new("RGBA", (W, W), DARK), img, rim)
    return img


def stamp(img, mask, fill=CREAM, outline=DARK, stroke=18):
    """Paint a glyph mask with a thick dark outline, like RoR2's icon line work."""
    grown = mask.filter(ImageFilter.MaxFilter(stroke * 2 + 1))
    img.paste(Image.new("RGBA", img.size, outline), (0, 0), grown)
    if isinstance(fill, tuple):
        img.paste(Image.new("RGBA", img.size, fill), (0, 0), mask)
    else:
        img.paste(fill, (0, 0), mask)
    return img


def new_mask():
    m = Image.new("L", (W, W), 0)
    return m, ImageDraw.Draw(m)


def save(img, name, size=SIZE):
    img.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))


# ---------------------------------------------------------------- glyphs

def flask_mask(cx, cy, r, angle=0):
    m, d = new_mask()
    d.ellipse((cx - r, cy - r * 0.9, cx + r, cy + r * 1.1), fill=255)
    nw = r * 0.38
    d.rectangle((cx - nw, cy - r * 1.9, cx + nw, cy - r * 0.6), fill=255)
    d.rectangle((cx - nw * 1.35, cy - r * 2.15, cx + nw * 1.35, cy - r * 1.8), fill=255)
    return m.rotate(angle, center=(cx, cy), resample=Image.BICUBIC)


def liquid_mask(cx, cy, r, angle=0):
    m, d = new_mask()
    d.chord((cx - r * 0.78, cy - r * 0.7, cx + r * 0.78, cy + r * 0.92), 0, 180, fill=255)
    return m.rotate(angle, center=(cx, cy), resample=Image.BICUBIC)


def icon_throw():
    img = background((60, 110, 55), (14, 30, 18), glow=(120, 200, 90))
    m, d = new_mask()
    for i in range(7):  # dashed throw arc
        a0 = 200 + i * 14
        d.arc((60, 90, 470, 520), a0, a0 + 8, fill=255, width=22)
    stamp(img, m, fill=(220, 240, 200, 255), stroke=10)
    stamp(img, flask_mask(330, 290, 85, angle=-30))
    img.paste(Image.new("RGBA", img.size, (120, 210, 80, 255)), (0, 0), liquid_mask(330, 290, 85, angle=-30))
    return img


def icon_knife():
    img = background((120, 30, 35), (25, 8, 10), glow=(200, 60, 60))
    m, d = new_mask()
    blade = [(140, 380), (380, 110), (410, 100), (400, 130), (170, 400)]
    d.polygon(blade, fill=255)
    stamp(img, m, fill=(225, 230, 235, 255))
    m2, d2 = new_mask()
    d2.polygon([(95, 430), (150, 365), (185, 400), (120, 460)], fill=255)  # handle
    d2.rectangle((120, 350, 200, 372), fill=255)
    stamp(img, m2.rotate(0), fill=(110, 70, 40, 255))
    m3, d3 = new_mask()
    for (x, y, r) in ((330, 300, 26), (370, 360, 20), (300, 380, 16)):
        d3.ellipse((x - r, y - r * 1.2, x + r, y + r), fill=255)
        d3.polygon([(x - r * 0.7, y - r * 0.6), (x, y - r * 2.4), (x + r * 0.7, y - r * 0.6)], fill=255)
    stamp(img, m3, fill=(200, 25, 35, 255), stroke=8)
    return img


def icon_elbow():
    img = background((130, 95, 40), (30, 20, 8), glow=(230, 180, 80))
    m, d = new_mask()
    d.line([(120, 160), (300, 300)], fill=255, width=95)       # upper arm
    d.line([(300, 300), (150, 420)], fill=255, width=85)       # forearm
    d.ellipse((240, 240, 360, 360), fill=255)                 # elbow
    stamp(img, m, fill=(70, 72, 80, 255))
    m2, d2 = new_mask()
    star = []
    for i in range(16):
        r = 120 if i % 2 == 0 else 55
        a = math.pi * 2 * i / 16
        star.append((380 + math.cos(a) * r, 250 + math.sin(a) * r))
    d2.polygon(star, fill=255)
    stamp(img, m2, fill=(255, 225, 120, 255), stroke=10)
    return img


def cloud_mask(cx, cy, s):
    m, d = new_mask()
    for (dx, dy, r) in ((-0.9, 0.2, 0.55), (-0.3, -0.25, 0.7), (0.45, -0.05, 0.62), (0.95, 0.3, 0.45), (0.0, 0.35, 0.6)):
        d.ellipse((cx + (dx - r) * s, cy + (dy - r) * s, cx + (dx + r) * s, cy + (dy + r) * s), fill=255)
    return m


def icon_vault():
    img = background((90, 95, 100), (20, 22, 26), glow=(170, 175, 180))
    stamp(img, cloud_mask(300, 340, 120), fill=(190, 195, 190, 255))
    m, d = new_mask()
    d.arc((90, 70, 380, 360), 160, 300, fill=255, width=40)
    d.polygon([(85, 210), (150, 260), (60, 290)], fill=255)  # arrow head pointing back/down-left
    stamp(img, m)
    return img


def icon_transmute():
    img = background((60, 40, 110), (12, 8, 28), glow=(150, 110, 230))
    m, d = new_mask()
    d.ellipse((96, 96, 416, 416), outline=255, width=26)
    tri = [(256, 130), (370, 330), (142, 330)]
    d.polygon(tri, outline=255, width=26)
    d.line([(150, 280), (362, 280)], fill=255, width=22)
    d.ellipse((226, 226, 286, 286), fill=255)
    stamp(img, m, fill=(225, 210, 255, 255), stroke=12)
    m2, d2 = new_mask()
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        x, y = 256 + math.cos(a) * 215, 256 + math.sin(a) * 215
        d2.ellipse((x - 18, y - 18, x + 18, y + 18), fill=255)
    stamp(img, m2, fill=(140, 255, 120, 255), stroke=8)
    return img


def icon_passive():
    img = background((70, 80, 50), (16, 18, 10), glow=(160, 170, 100))
    m, d = new_mask()
    d.rectangle((40, 330, 472, 380), fill=255)
    stamp(img, m, fill=(110, 70, 40, 255))
    for i, col in enumerate(((120, 210, 80), (255, 130, 40), (220, 190, 40))):
        cx = 140 + i * 116
        stamp(img, flask_mask(cx, 280, 52))
        img.paste(Image.new("RGBA", img.size, (*col, 255)), (0, 0), liquid_mask(cx, 280, 52))
    return img


def potion_icon(render_name, color):
    top = tuple(int(c * 0.55 * 255) for c in color)
    bottom = tuple(int(c * 0.12 * 255) for c in color)
    glow = tuple(min(255, int(c * 0.95 * 255)) for c in color)
    img = background(top, bottom, glow=glow)
    r = Image.open(os.path.join(RENDERS, render_name + ".png")).convert("RGBA")
    bbox = r.getbbox()
    r = r.crop(bbox)
    scale = (W * 0.78) / max(r.size)
    r = r.resize((int(r.size[0] * scale), int(r.size[1] * scale)), Image.LANCZOS)
    # dark outline around the bottle so it reads on any background
    alpha = Image.new("L", (W, W), 0)
    off = ((W - r.size[0]) // 2, (W - r.size[1]) // 2)
    alpha.paste(r.split()[3], off)
    grown = alpha.point(lambda v: 255 if v > 30 else 0).filter(ImageFilter.MaxFilter(17))
    img.paste(Image.new("RGBA", img.size, DARK), (0, 0), grown)
    img.alpha_composite(r, off)
    return img


def item_icon_crow():
    r = Image.open(os.path.join(RENDERS, "ClockworkCrow.png")).convert("RGBA")
    r = r.crop(r.getbbox())
    canvas = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    scale = (W * 0.9) / max(r.size)
    r = r.resize((int(r.size[0] * scale), int(r.size[1] * scale)), Image.LANCZOS)
    off = ((W - r.size[0]) // 2, (W - r.size[1]) // 2)
    alpha = Image.new("L", (W, W), 0)
    alpha.paste(r.split()[3], off)
    grown = alpha.point(lambda v: 255 if v > 30 else 0).filter(ImageFilter.MaxFilter(15))
    canvas.paste(Image.new("RGBA", canvas.size, DARK), (0, 0), grown)
    canvas.alpha_composite(r, off)
    return canvas


# ---------------------------------------------------------------- buff glyphs (white, tinted by the game)

def buff(draw_fn):
    m, d = new_mask()
    draw_fn(d)
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    img.paste(Image.new("RGBA", img.size, (255, 255, 255, 255)), (0, 0), m)
    return img


def drop(d, cx, cy, r):
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=255)
    d.polygon([(cx - r * 0.85, cy - r * 0.5), (cx, cy - r * 2.3), (cx + r * 0.85, cy - r * 0.5)], fill=255)


BUFFS = {
    "Lacerated": lambda d: [d.line([(110 + i * 110, 420), (230 + i * 110, 90)], fill=255, width=48) for i in range(3)],
    "Infected": lambda d: (drop(d, 256, 320, 130), d.ellipse((200, 290, 240, 330), fill=0), d.ellipse((270, 340, 300, 370), fill=0), d.ellipse((250, 250, 275, 275), fill=0)),
    "Plagued": lambda d: [d.ellipse((x - r, y - r, x + r, y + r), fill=255) for (x, y, r) in ((180, 340, 110), (330, 250, 85), (300, 410, 55), (390, 380, 40), (230, 160, 45))],
    "Corroded": lambda d: (d.polygon([(256, 60), (430, 130), (400, 330), (256, 460), (112, 330), (82, 130)], fill=255),
                           d.line([(256, 60), (220, 200), (300, 260), (230, 460)], fill=0, width=30)),
    "Chilled": lambda d: [d.line([(256 + math.cos(a) * 200, 256 + math.sin(a) * 200), (256 - math.cos(a) * 200, 256 - math.sin(a) * 200)], fill=255, width=46)
                          for a in (0, math.pi / 3, 2 * math.pi / 3)],
    "Enfeebled": lambda d: (d.rectangle((200, 60, 312, 300), fill=255), d.polygon([(90, 280), (422, 280), (256, 460)], fill=255)),
    "Contagious": lambda d: [d.ellipse((x - r, y - r, x + r, y + r), fill=255) for (x, y, r) in ((256, 256, 95), (110, 130, 45), (400, 120, 40), (420, 380, 50), (100, 390, 40), (256, 450, 30))],
    "Bloodlet": lambda d: (drop(d, 256, 330, 140),),
    "Gassed": lambda d: [d.ellipse((256 + (dx - r) * 150, 280 + (dy - r) * 150, 256 + (dx + r) * 150, 280 + (dy + r) * 150), fill=255)
                         for (dx, dy, r) in ((-0.9, 0.2, 0.55), (-0.3, -0.25, 0.7), (0.45, -0.05, 0.62), (0.95, 0.3, 0.45), (0.0, 0.35, 0.6))],
    "Confused": lambda d: (d.arc((140, 60, 372, 290), 180, 90, fill=255, width=60), d.line([(256, 290), (256, 360)], fill=255, width=60),
                           d.ellipse((222, 400, 290, 468), fill=255)),
    "Festering": lambda d: (drop(d, 256, 360, 110), d.ellipse((120, 150, 190, 220), fill=255), d.ellipse((330, 100, 380, 150), fill=255), d.ellipse((370, 230, 410, 270), fill=255)),
}

POTIONS = {
    "Plague": (0.40, 0.75, 0.20), "Hellfire": (1.00, 0.45, 0.10), "Vitriol": (0.90, 0.75, 0.10),
    "Frost": (0.55, 0.85, 1.00), "Hemlock": (0.55, 0.30, 0.70), "Catalyst": (0.95, 0.95, 0.85),
    "Miasma": (0.55, 0.60, 0.25), "Sanguine": (0.65, 0.05, 0.15), "Volatile": (1.00, 0.30, 0.60),
}

if __name__ == "__main__":
    save(icon_throw(), "skill_throw")
    save(icon_knife(), "skill_knife")
    save(icon_elbow(), "skill_elbow")
    save(icon_vault(), "skill_vault")
    save(icon_transmute(), "skill_transmute")
    save(icon_passive(), "skill_passive")
    for name, col in POTIONS.items():
        save(potion_icon("Potion_" + name, col), "potion_" + name.lower())
    save(item_icon_crow(), "item_crow")
    for name, fn in BUFFS.items():
        save(buff(fn), "buff_" + name.lower(), size=BUFF)
    print("icons written to", OUT)
