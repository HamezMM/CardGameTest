#!/usr/bin/env python3
"""
Generates flat-icon style card-face art for every card in /Data, plus a card back.

Output goes to src/Gamma931.App/Assets/Cards/<category>/<id>.png (300x420, 5:7 card ratio).
Art only -- no baked-in text -- so CardFaceView.xaml lays the (data-driven, CSV-editable)
name/effect text on top in WPF. Re-run any time a card is added/renamed in /Data; this script
reads the CSVs directly so it never drifts from the game data.

Usage: python3 tools/generate_card_art.py
"""
from __future__ import annotations

import colorsys
import csv
import hashlib
import math
import os
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "Data"
OUT = ROOT / "src" / "Gamma931.App" / "Assets" / "Cards"

W, H = 300, 420


def seeded_rng(card_id: str) -> random.Random:
    seed = int(hashlib.md5(card_id.encode("utf-8")).hexdigest(), 16)
    return random.Random(seed)


def hex_to_rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return tuple(int(h[i : i + 2], 16) for i in (0, 2, 4))


def lighten(rgb, amount: float):
    r0, g0, b0 = rgb[:3]
    h, l, s = colorsys.rgb_to_hls(r0 / 255, g0 / 255, b0 / 255)
    l = max(0.0, min(1.0, l + amount))
    r, g, b = colorsys.hls_to_rgb(h, l, s)
    result = (int(r * 255), int(g * 255), int(b * 255))
    return result + tuple(rgb[3:]) if len(rgb) > 3 else result


def vertical_gradient(size, top_rgb, bottom_rgb):
    w, h = size
    base = Image.new("RGB", (1, h))
    for y in range(h):
        t = y / max(1, h - 1)
        px = tuple(int(top_rgb[i] + (bottom_rgb[i] - top_rgb[i]) * t) for i in range(3))
        base.putpixel((0, y), px)
    return base.resize((w, h))


def vignette(size, strength=110):
    w, h = size
    mask = Image.new("L", size, 0)
    d = ImageDraw.Draw(mask)
    cx, cy = w / 2, h / 2
    maxr = math.hypot(cx, cy)
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            r = math.hypot(x - cx, y - cy) / maxr
            v = int(max(0, min(255, (r - 0.55) * strength * 3)))
            d.rectangle([x, y, x + 1, y + 1], fill=v)
    return mask.filter(ImageFilter.GaussianBlur(20))


def card_base(top_hex: str, bottom_hex: str) -> Image.Image:
    top = hex_to_rgb(top_hex)
    bottom = hex_to_rgb(bottom_hex)
    img = vertical_gradient((W, H), top, bottom).convert("RGBA")
    dark = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dark.putalpha(vignette((W, H)))
    img = Image.alpha_composite(img, dark)
    return img


def frame(img: Image.Image, accent):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([6, 6, W - 7, H - 7], radius=18, outline=accent, width=6)
    d.rounded_rectangle([16, 16, W - 17, H - 17], radius=12, outline=(235, 235, 245, 255), width=2)
    return img


# ---------------------------------------------------------------------------
# icon primitives (drawn centered around cx,cy at roughly `scale` px across)
# ---------------------------------------------------------------------------

def poly(d, pts, fill=None, outline=None, width=3):
    d.polygon(pts, fill=fill, outline=outline)
    if outline and width > 1:
        d.line(pts + [pts[0]], fill=outline, width=width, joint="curve")


def icon_crab(d, cx, cy, scale, body_color, spiky=False, eyes=True, rng=None):
    rng = rng or random.Random(0)
    bw, bh = scale * 0.62, scale * 0.42
    d.ellipse([cx - bw / 2, cy - bh / 2, cx + bw / 2, cy + bh / 2], fill=body_color)
    leg_color = lighten(body_color, -0.12)
    for side in (-1, 1):
        for i in range(4):
            t = i / 3
            lx = cx + side * bw * 0.3
            ly = cy - bh * 0.15 + t * bh * 0.9
            ex = lx + side * scale * (0.28 + 0.05 * t)
            ey = ly + scale * 0.16
            d.line([lx, ly, ex, ey], fill=leg_color, width=max(3, int(scale * 0.035)))
            d.line([ex, ey, ex + side * scale * 0.06, ey + scale * 0.1], fill=leg_color, width=max(3, int(scale * 0.03)))
    for side in (-1, 1):
        claw_cx = cx + side * bw * 0.62
        claw_cy = cy - bh * 0.05
        pts = [
            (cx + side * bw * 0.22, cy - bh * 0.1),
            (claw_cx, claw_cy - scale * 0.09),
            (claw_cx + side * scale * 0.14, claw_cy),
            (claw_cx, claw_cy + scale * 0.09),
        ]
        poly(d, pts, fill=leg_color)
        if spiky:
            d.ellipse([claw_cx - 4, claw_cy - 4, claw_cx + 4, claw_cy + 4], fill=lighten(body_color, 0.25))
    if spiky:
        for i in range(5):
            ang = math.pi * (0.15 + 0.7 * i / 4)
            sx = cx + math.cos(ang) * bw * 0.42
            sy = cy - math.sin(ang) * bh * 0.9
            poly(d, [(sx - 6, cy - bh * 0.1), (sx, sy), (sx + 6, cy - bh * 0.1)], fill=leg_color)
    if eyes:
        for side in (-1, 1):
            ex = cx + side * bw * 0.14
            ey = cy - bh * 0.55
            d.ellipse([ex - 7, ey - 10, ex + 7, ey + 6], fill=lighten(body_color, 0.3))
            d.ellipse([ex - 3, ey - 6, ex + 3, ey - 1], fill=(20, 20, 20, 255))


def icon_cross(d, cx, cy, scale, color):
    a = scale * 0.16
    b = scale * 0.5
    d.rounded_rectangle([cx - a, cy - b, cx + a, cy + b], radius=int(a * 0.6), fill=color)
    d.rounded_rectangle([cx - b, cy - a, cx + b, cy + a], radius=int(a * 0.6), fill=color)


def icon_leaf(d, cx, cy, scale, color, rng):
    ang = -math.pi / 4
    length = scale * 0.85
    width = scale * 0.42
    dx, dy = math.cos(ang), math.sin(ang)
    px, py = -dy, dx
    base = (cx - dx * length * 0.42, cy - dy * length * 0.42)
    tip = (cx + dx * length * 0.58, cy + dy * length * 0.58)

    def edge(sign, steps=10):
        pts = []
        for s in range(steps + 1):
            t = s / steps
            bow = math.sin(t * math.pi) * width * 0.5 * sign
            x = base[0] + (tip[0] - base[0]) * t + px * bow
            y = base[1] + (tip[1] - base[1]) * t + py * bow
            pts.append((x, y))
        return pts

    outline = edge(1) + edge(-1)[::-1]
    d.polygon(outline, fill=color)
    d.line([base, tip], fill=lighten(color, -0.28), width=max(2, int(scale * 0.02)))
    for t in (0.35, 0.6, 0.82):
        mx = base[0] + (tip[0] - base[0]) * t
        my = base[1] + (tip[1] - base[1]) * t
        vw = width * 0.42 * math.sin(t * math.pi)
        for sign in (1, -1):
            d.line([(mx, my), (mx + px * vw * sign, my + py * vw * sign)], fill=lighten(color, -0.22), width=max(1, int(scale * 0.012)))
    stem_end = (base[0] - dx * scale * 0.14, base[1] - dy * scale * 0.14)
    d.line([base, stem_end], fill=lighten(color, -0.3), width=max(3, int(scale * 0.035)))


def icon_wrench_gear(d, cx, cy, scale, color):
    r = scale * 0.28
    teeth = 8
    pts = []
    for i in range(teeth * 2):
        ang = math.pi * 2 * i / (teeth * 2)
        rr = r * (1.15 if i % 2 == 0 else 0.85)
        pts.append((cx + math.cos(ang) * rr, cy + math.sin(ang) * rr))
    d.polygon(pts, fill=color)
    d.ellipse([cx - r * 0.45, cy - r * 0.45, cx + r * 0.45, cy + r * 0.45], fill=lighten(color, -0.25))
    wx, wy = cx + scale * 0.32, cy + scale * 0.32
    d.line([cx + r * 0.5, cy + r * 0.5, wx, wy], fill=lighten(color, 0.15), width=int(scale * 0.09))
    d.ellipse([wx - scale * 0.09, wy - scale * 0.09, wx + scale * 0.16, wy + scale * 0.16], outline=lighten(color, 0.15), width=int(scale * 0.05))


def icon_compass(d, cx, cy, scale, color, rng):
    r = scale * 0.42
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=color, width=max(3, int(scale * 0.035)))
    ang = rng.uniform(0, math.pi * 2)
    for offset, col, ln in ((0, lighten(color, 0.25), r * 0.85), (math.pi, lighten(color, -0.2), r * 0.5)):
        a = ang + offset
        ex, ey = cx + math.cos(a) * ln, cy + math.sin(a) * ln
        perp = a + math.pi / 2
        p1 = (cx + math.cos(perp) * r * 0.1, cy + math.sin(perp) * r * 0.1)
        p2 = (cx - math.cos(perp) * r * 0.1, cy - math.sin(perp) * r * 0.1)
        poly(d, [p1, (ex, ey), p2], fill=col)
    d.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=lighten(color, 0.3))


def icon_crosshair(d, cx, cy, scale, color):
    r = scale * 0.4
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=color, width=max(3, int(scale * 0.035)))
    d.ellipse([cx - r * 0.5, cy - r * 0.5, cx + r * 0.5, cy + r * 0.5], outline=color, width=max(2, int(scale * 0.02)))
    for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        d.line([cx + dx * r * 0.55, cy + dy * r * 0.55, cx + dx * r * 1.15, cy + dy * r * 1.15], fill=color, width=max(3, int(scale * 0.03)))
    d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=color)


def icon_star(d, cx, cy, scale, color, points=5):
    outer = scale * 0.46
    inner = outer * 0.42
    pts = []
    for i in range(points * 2):
        ang = -math.pi / 2 + math.pi * i / points
        r = outer if i % 2 == 0 else inner
        pts.append((cx + math.cos(ang) * r, cy + math.sin(ang) * r))
    d.polygon(pts, fill=color)


def icon_sword(d, cx, cy, scale, color):
    bl = scale * 0.62
    bw = scale * 0.08
    ang = -math.pi / 4
    dx, dy = math.cos(ang), math.sin(ang)
    px, py = -dy, dx
    tip = (cx + dx * bl * 0.55, cy + dy * bl * 0.55)
    base = (cx - dx * bl * 0.35, cy - dy * bl * 0.35)
    poly(d, [
        (tip[0] + px * bw * 0.1, tip[1] + py * bw * 0.1),
        (base[0] + px * bw, base[1] + py * bw),
        (base[0] - px * bw, base[1] - py * bw),
        (tip[0] - px * bw * 0.1, tip[1] - py * bw * 0.1),
    ], fill=lighten(color, 0.2))
    guard_c = (base[0] - dx * bl * 0.06, base[1] - dy * bl * 0.06)
    d.line([guard_c[0] + px * bw * 2.2, guard_c[1] + py * bw * 2.2, guard_c[0] - px * bw * 2.2, guard_c[1] - py * bw * 2.2], fill=color, width=max(3, int(scale * 0.045)))
    handle_end = (base[0] - dx * bl * 0.22, base[1] - dy * bl * 0.22)
    d.line([guard_c, handle_end], fill=lighten(color, -0.2), width=max(4, int(scale * 0.06)))


def icon_gun(d, cx, cy, scale, color):
    bx0, by0 = cx - scale * 0.42, cy - scale * 0.05
    bx1, by1 = cx + scale * 0.1, cy + scale * 0.05
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=int(scale * 0.03), fill=color)
    d.rounded_rectangle([bx1 - 6, by0 - 6, bx1 + scale * 0.3, by1 + 6], radius=int(scale * 0.02), fill=lighten(color, -0.1))
    grip = [(cx - scale * 0.28, by1), (cx - scale * 0.34, by1), (cx - scale * 0.4, cy + scale * 0.32), (cx - scale * 0.2, cy + scale * 0.32), (cx - scale * 0.14, by1)]
    poly(d, grip, fill=lighten(color, -0.2))
    d.ellipse([cx - scale * 0.02, by0 - 10, cx + scale * 0.02 + 6, by0 + 6], fill=lighten(color, 0.35))


def icon_shield(d, cx, cy, scale, color):
    w, h = scale * 0.55, scale * 0.65
    pts = [
        (cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2),
        (cx + w / 2, cy + h * 0.05), (cx, cy + h / 2),
        (cx - w / 2, cy + h * 0.05),
    ]
    d.polygon(pts, fill=color, outline=lighten(color, -0.25))
    inset = 10
    pts2 = [
        (cx - w / 2 + inset, cy - h / 2 + inset), (cx + w / 2 - inset, cy - h / 2 + inset),
        (cx + w / 2 - inset, cy + h * 0.02), (cx, cy + h / 2 - inset),
        (cx - w / 2 + inset, cy + h * 0.02),
    ]
    d.polygon(pts2, outline=lighten(color, 0.3), width=2)


def icon_medkit(d, cx, cy, scale, color):
    w, h = scale * 0.6, scale * 0.42
    d.rounded_rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], radius=10, fill=color, outline=lighten(color, -0.25), width=3)
    handle_w = w * 0.35
    d.rounded_rectangle([cx - handle_w / 2, cy - h / 2 - 14, cx + handle_w / 2, cy - h / 2 + 6], radius=6, outline=lighten(color, -0.3), width=4)
    icon_cross(d, cx, cy, scale * 0.42, lighten(color, 0.35))


def icon_flame(d, cx, cy, scale, color, rng):
    tip = (cx, cy - scale * 0.5)
    pts = [tip]
    for t in (0.15, 0.32, 0.5, 0.68, 0.85, 1.0):
        wob = rng.uniform(-0.08, 0.08)
        x = cx + (scale * 0.32 + wob * scale) * (0.4 + t * 0.6)
        y = cy - scale * 0.5 + t * scale * 0.95
        pts.append((x, y))
    pts.append((cx, cy + scale * 0.5))
    for t in (1.0, 0.85, 0.68, 0.5, 0.32, 0.15):
        wob = rng.uniform(-0.08, 0.08)
        x = cx - (scale * 0.32 + wob * scale) * (0.4 + t * 0.6)
        y = cy - scale * 0.5 + t * scale * 0.95
        pts.append((x, y))
    d.polygon(pts, fill=color)
    inner = [(cx, cy - scale * 0.3)]
    for t in (0.3, 0.6, 0.9):
        inner.append((cx + scale * 0.14 * (1 - t), cy - scale * 0.3 + t * scale * 0.65))
    inner.append((cx, cy + scale * 0.35))
    for t in (0.9, 0.6, 0.3):
        inner.append((cx - scale * 0.14 * (1 - t), cy - scale * 0.3 + t * scale * 0.65))
    d.polygon(inner, fill=lighten(color, 0.35))


def icon_snowflake(d, cx, cy, scale, color):
    r = scale * 0.45
    for i in range(6):
        ang = math.pi * i / 3
        ex, ey = cx + math.cos(ang) * r, cy + math.sin(ang) * r
        d.line([cx, cy, ex, ey], fill=color, width=max(3, int(scale * 0.03)))
        for t in (0.45, 0.75):
            bx, by = cx + math.cos(ang) * r * t, cy + math.sin(ang) * r * t
            for pm in (-1, 1):
                bang = ang + pm * math.pi / 4
                d.line([bx, by, bx + math.cos(bang) * r * 0.22, by + math.sin(bang) * r * 0.22], fill=color, width=max(2, int(scale * 0.02)))


def icon_tree(d, cx, cy, scale, color, rng):
    trunk_w = scale * 0.08
    d.rectangle([cx - trunk_w / 2, cy, cx + trunk_w / 2, cy + scale * 0.35], fill=lighten(color, -0.35))
    for i, (dx, r, dy) in enumerate([(0, 0.4, -0.1), (-0.22, 0.28, 0.05), (0.22, 0.28, 0.05)]):
        rr = scale * r
        ccx, ccy = cx + scale * dx, cy - scale * 0.15 + scale * dy
        d.ellipse([ccx - rr, ccy - rr, ccx + rr, ccy + rr], fill=lighten(color, 0.06 * i))


def icon_wave(d, cx, cy, scale, color, rows=3):
    for i in range(rows):
        y = cy - scale * 0.25 + i * scale * 0.22
        pts = [(cx - scale * 0.5, y + scale * 0.08)]
        steps = 5
        for s in range(steps + 1):
            t = s / steps
            x = cx - scale * 0.5 + t * scale
            yy = y + math.sin(t * math.pi * 2 + i) * scale * 0.06
            pts.append((x, yy))
        d.line(pts, fill=lighten(color, i * 0.12), width=max(4, int(scale * 0.05)), joint="curve")


def icon_mountain_ruins(d, cx, cy, scale, color, rng, broken=False):
    base_y = cy + scale * 0.35
    pts = [(cx - scale * 0.5, base_y), (cx - scale * 0.15, cy - scale * 0.45), (cx + scale * 0.1, cy - scale * 0.1), (cx + scale * 0.5, base_y)]
    d.polygon(pts, fill=color)
    if broken:
        for i in range(3):
            bx = cx - scale * 0.3 + i * scale * 0.28
            bh = rng.uniform(0.12, 0.3) * scale
            d.rectangle([bx, base_y - bh, bx + scale * 0.12, base_y], fill=lighten(color, -0.1 + 0.05 * i))


def icon_dunes(d, cx, cy, scale, color, rng):
    d.ellipse([cx + scale * 0.1, cy - scale * 0.45, cx + scale * 0.34, cy - scale * 0.21], fill=lighten(color, 0.4))
    for i in range(3):
        y = cy + scale * (0.02 + i * 0.16)
        pts = []
        for s in range(9):
            t = s / 8
            x = cx - scale * 0.5 + t * scale
            yy = y - math.sin(t * math.pi * 1.5 + i * 1.3) * scale * 0.08
            pts.append((x, yy))
        pts += [(cx + scale * 0.5, cy + scale * 0.5), (cx - scale * 0.5, cy + scale * 0.5)]
        d.polygon(pts, fill=lighten(color, -0.05 * i))


def icon_rocket(d, cx, cy, scale, color):
    d.polygon([(cx, cy - scale * 0.5), (cx + scale * 0.18, cy - scale * 0.1), (cx - scale * 0.18, cy - scale * 0.1)], fill=lighten(color, 0.25))
    d.rounded_rectangle([cx - scale * 0.18, cy - scale * 0.1, cx + scale * 0.18, cy + scale * 0.3], radius=8, fill=color)
    d.polygon([(cx - scale * 0.18, cy + scale * 0.05), (cx - scale * 0.34, cy + scale * 0.35), (cx - scale * 0.18, cy + scale * 0.25)], fill=lighten(color, -0.15))
    d.polygon([(cx + scale * 0.18, cy + scale * 0.05), (cx + scale * 0.34, cy + scale * 0.35), (cx + scale * 0.18, cy + scale * 0.25)], fill=lighten(color, -0.15))
    d.ellipse([cx - 8, cy, cx + 8, cy + 16], fill=lighten(color, -0.3))
    flame_pts = [(cx - scale * 0.1, cy + scale * 0.3), (cx + scale * 0.1, cy + scale * 0.3), (cx, cy + scale * 0.5)]
    d.polygon(flame_pts, fill=lighten(color, 0.5))


def icon_lightning(d, cx, cy, scale, color):
    pts = [(cx + scale * 0.08, cy - scale * 0.48), (cx - scale * 0.18, cy + scale * 0.02), (cx, cy + scale * 0.02),
           (cx - scale * 0.08, cy + scale * 0.48), (cx + scale * 0.22, cy - scale * 0.08), (cx + scale * 0.02, cy - scale * 0.08)]
    d.polygon(pts, fill=color)


def icon_claw_swipe(d, cx, cy, scale, color, rng):
    for i in range(3):
        off = (i - 1) * scale * 0.14
        bbox = [cx - scale * 0.45 + off, cy - scale * 0.45 + off, cx + scale * 0.45 + off, cy + scale * 0.45 + off]
        d.arc(bbox, start=200, end=340, fill=lighten(color, i * 0.1), width=max(4, int(scale * 0.045)))


def icon_body_part(d, cx, cy, scale, color, part: str):
    head_r = scale * 0.14
    hx, hy = cx, cy - scale * 0.42
    torso = [cx - scale * 0.16, cy - scale * 0.28, cx + scale * 0.16, cy + scale * 0.12]
    dim = (222, 220, 232, 255)
    d.ellipse([hx - head_r, hy - head_r, hx + head_r, hy + head_r], fill=dim if part != "Head" else color)
    d.rounded_rectangle(torso, radius=10, fill=dim if part != "Torso" else color)
    for side in (-1, 1):
        ax0 = cx + side * scale * 0.16
        ay0 = cy - scale * 0.2
        ax1 = cx + side * scale * 0.34
        ay1 = cy + scale * 0.1
        d.line([ax0, ay0, ax1, ay1], fill=(color if part == "Arms" else dim), width=int(scale * 0.09))
        lx0 = cx + side * scale * 0.08
        ly0 = cy + scale * 0.12
        lx1 = cx + side * scale * 0.1
        ly1 = cy + scale * 0.48
        d.line([lx0, ly0, lx1, ly1], fill=(color if part == "Legs" else dim), width=int(scale * 0.1))


# ---------------------------------------------------------------------------
# category rendering
# ---------------------------------------------------------------------------

CHAR_ICON = {
    "CHAR-01": ("sword", (0.75, 0.28, 0.20)),   # Brawler - Melee
    "CHAR-02": ("leaf", (0.30, 0.62, 0.35)),    # Biologist - Support
    "CHAR-03": ("cross", (0.85, 0.24, 0.30)),   # Medic - Support
    "CHAR-04": ("wrench", (0.75, 0.55, 0.10)),  # Engineer - Utility
    "CHAR-05": ("compass", (0.18, 0.55, 0.60)), # Scout - Positioning
    "CHAR-06": ("crosshair", (0.55, 0.20, 0.65)), # Marksman - Ranged
    "CHAR-07": ("wrench", (0.20, 0.45, 0.70)),  # Technician - Utility
    "CHAR-08": ("star", (0.85, 0.65, 0.10)),    # Captain - Leadership
}


def render_character(cid, name, rng, top="#173a5e", bottom="#3f7ea8"):
    img = card_base(top, bottom)
    d = ImageDraw.Draw(img)
    motif, hue = CHAR_ICON.get(cid, ("star", (0.5, 0.5, 0.5)))
    color = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue[0], 0.55, 0.7)) + (255,)
    cx, cy = W / 2, H * 0.44
    scale = 190
    d.ellipse([cx - scale * 0.55, cy - scale * 0.55, cx + scale * 0.55, cy + scale * 0.55], fill=(246, 247, 250, 255))
    fn = {
        "sword": icon_sword, "cross": icon_cross, "leaf": lambda *a: icon_leaf(*a, rng),
        "wrench": icon_wrench_gear, "compass": lambda *a: icon_compass(*a, rng),
        "crosshair": icon_crosshair, "star": icon_star,
    }[motif]
    fn(d, cx, cy, scale, color)
    return frame(img, lighten(color[:3], 0.1) + (255,))


BOSS_HUE = {
    "Desert": 0.09, "Swamp": 0.30, "Ice/Tundra": 0.53, "Jungle": 0.33,
    "Volcanic/Caves": 0.02, "Ruins/Wreckage": 0.58, "Coastal/Reef": 0.5,
}
BOSS_SAT = {"Ruins/Wreckage": 0.22}


def render_boss(cid, biome, rng):
    hue = BOSS_HUE.get(biome, 0.0) if biome else 0.0
    sat = BOSS_SAT.get(biome, 0.55) if biome else 0.05
    top = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue, 0.16, sat))
    bottom = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue, 0.30, sat))
    img = card_base("#%02x%02x%02x" % top, "#%02x%02x%02x" % bottom)
    d = ImageDraw.Draw(img)
    body_hue = hue if biome else 0.0
    body_l = 0.32 if biome else 0.24
    body = tuple(int(c * 255) for c in colorsys.hls_to_rgb(body_hue, body_l, 0.5)) + (255,)
    icon_crab(d, W / 2, H * 0.48, 230, body, spiky=True, eyes=True, rng=rng)
    return frame(img, lighten(body[:3], 0.15) + (255,))


def render_minion(cid, name, rng):
    img = card_base("#4a260f", "#8a4a22")
    d = ImageDraw.Draw(img)
    hue = rng.uniform(0.03, 0.08)
    body = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue, 0.42, 0.55)) + (255,)
    icon_crab(d, W / 2, H * 0.5, 150, body, spiky=False, eyes=True, rng=rng)
    return frame(img, lighten(body[:3], 0.15) + (255,))


EQ_PALETTE = {
    "Melee": ((0.02, 0.20, 0.55), icon_sword),
    "Ranged": ((0.12, 0.20, 0.55), icon_gun),
    "Protection": ((0.58, 0.20, 0.55), icon_shield),
    "Healing": ((0.38, 0.20, 0.55), icon_medkit),
    "Utility": ((0.75, 0.20, 0.55), icon_wrench_gear),
}


def render_equipment(cid, eq_type, rng):
    hue, l, s = EQ_PALETTE[eq_type][0]
    top = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue, l * 0.7, s))
    bottom = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue, l * 1.4, s))
    img = card_base("#%02x%02x%02x" % top, "#%02x%02x%02x" % bottom)
    d = ImageDraw.Draw(img)
    color = tuple(int(c * 255) for c in colorsys.hls_to_rgb(hue, 0.6, 0.65)) + (255,)
    fn = EQ_PALETTE[eq_type][1]
    d.ellipse([W / 2 - 105, H * 0.44 - 105, W / 2 + 105, H * 0.44 + 105], fill=(246, 247, 250, 255))
    fn(d, W / 2, H * 0.44, 190, color)
    return frame(img, lighten(color[:3], 0.1) + (255,))


LOC_STYLE = {
    "Desert": ("#5c4322", "#c99a4a", icon_dunes),
    "Swamp": ("#22321c", "#4c6b3a", icon_tree),
    "Ice/Tundra": ("#1c3b4a", "#7fc4dd", icon_snowflake),
    "Jungle": ("#123322", "#2f7a4a", icon_tree),
    "Volcanic/Caves": ("#3a0f0a", "#a83a1c", icon_flame),
    "Ruins/Wreckage": ("#2a2a2e", "#6c6c73", None),
    "Coastal/Reef": ("#0e3b47", "#2c9aa8", icon_wave),
}


def render_location(cid, name, biome, is_shuttle, rng):
    if is_shuttle:
        img = card_base("#0c1730", "#2c3f66")
        d = ImageDraw.Draw(img)
        icon_rocket(d, W / 2, H * 0.46, 220, (200, 210, 230, 255))
        return frame(img, (170, 190, 220, 255))
    top, bottom, fn = LOC_STYLE.get(biome, ("#333", "#666", None))
    img = card_base(top, bottom)
    d = ImageDraw.Draw(img)
    if biome == "Ruins/Wreckage":
        icon_mountain_ruins(d, W / 2, H * 0.5, 220, hex_to_rgb("#8b8b93") + (255,), rng, broken=True)
    else:
        fn(d, W / 2, H * 0.5, 220, hex_to_rgb(bottom) + (255,), rng) if fn in (icon_dunes, icon_tree, icon_flame) else fn(d, W / 2, H * 0.5, 220, hex_to_rgb(bottom) + (255,))
    return frame(img, hex_to_rgb(bottom) + (255,))


def render_crab_action(cid, name, rng):
    img = card_base("#3a2b06", "#8a641c")
    d = ImageDraw.Draw(img)
    color = (255, 205, 90, 255)
    motif = rng.choice([icon_claw_swipe, icon_lightning])
    if motif is icon_claw_swipe:
        icon_claw_swipe(d, W / 2, H * 0.46, 220, color, rng)
    else:
        icon_lightning(d, W / 2, H * 0.46, 220, color)
    return frame(img, (255, 220, 140, 255))


DAMAGE_STYLE = {
    "Arms": "#5a1414",
    "Legs": "#4a1030",
    "Torso": "#5c2a06",
    "Head": "#2a1a52",
}


def render_damage(part, rng):
    top = DAMAGE_STYLE[part]
    bottom = lighten(hex_to_rgb(top), 0.28)
    img = card_base(top, "#%02x%02x%02x" % bottom)
    d = ImageDraw.Draw(img)
    color = lighten(hex_to_rgb(top), 0.38) + (255,)
    icon_body_part(d, W / 2, H * 0.5, 230, color, part)
    return frame(img, color)


def render_back():
    img = card_base("#0b1220", "#1c2b4a")
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([28, 28, W - 29, H - 29], radius=16, outline=(120, 150, 210, 200), width=3)
    icon_star(d, W / 2, H / 2, 140, (150, 175, 225, 255), points=6)
    d.ellipse([W / 2 - 60, H / 2 - 60, W / 2 + 60, H / 2 + 60], outline=(150, 175, 225, 160), width=3)
    return frame(img, (110, 140, 200, 255))


def save(img: Image.Image, path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, "PNG", optimize=True)


def main():
    for sub in ("characters", "bosses", "minions", "equipment", "locations", "crab_actions", "damage"):
        (OUT / sub).mkdir(parents=True, exist_ok=True)

    with open(DATA / "characters.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            rng = seeded_rng(row["Id"])
            save(render_character(row["Id"], row["Name"], rng), OUT / "characters" / f"{row['Id']}.png")

    with open(DATA / "crab_bosses.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            rng = seeded_rng(row["Id"])
            save(render_boss(row["Id"], row["Biome"], rng), OUT / "bosses" / f"{row['Id']}.png")

    with open(DATA / "crab_minions.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            rng = seeded_rng(row["Id"])
            save(render_minion(row["Id"], row["Name"], rng), OUT / "minions" / f"{row['Id']}.png")

    with open(DATA / "equipment.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            rng = seeded_rng(row["Id"])
            save(render_equipment(row["Id"], row["EquipmentType"], rng), OUT / "equipment" / f"{row['Id']}.png")

    with open(DATA / "locations.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            rng = seeded_rng(row["Id"])
            is_shuttle = row["IsShuttle"].strip().lower() == "true"
            save(render_location(row["Id"], row["Name"], row["Biome"], is_shuttle, rng), OUT / "locations" / f"{row['Id']}.png")

    with open(DATA / "crab_actions.csv", newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            rng = seeded_rng(row["Id"])
            save(render_crab_action(row["Id"], row["Name"], rng), OUT / "crab_actions" / f"{row['Id']}.png")

    for part in ("Arms", "Legs", "Torso", "Head"):
        rng = seeded_rng(part)
        save(render_damage(part, rng), OUT / "damage" / f"{part}.png")

    save(render_back(), OUT / "back.png")

    count = sum(1 for _ in OUT.rglob("*.png"))
    print(f"Wrote {count} card images to {OUT}")


if __name__ == "__main__":
    main()
