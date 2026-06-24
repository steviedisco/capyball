#!/usr/bin/env python3
"""Generate Sega-arcade candy textures for Capyball.

These are intentionally geometric (checkerboards, grids, stripes, rails) — the
kind of procedural-looking surface detail that reads as intentional and "arcade"
rather than as missing art. Output goes to assets/textures/ as small, tileable PNGs.

Run: python3 tools/gen_textures.py
"""
import os
import math
import random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "assets", "textures")
os.makedirs(OUT, exist_ok=True)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def soften(im, r=1):
    return im.filter(ImageFilter.GaussianBlur(r))


def save(im, name):
    path = os.path.join(OUT, name)
    im.save(path)
    print(f"  wrote {path}  ({im.size[0]}x{im.size[1]})")


def checker(size, cells, light, dark, bevel=True):
    """A tileable checkerboard with optional soft bevelled edges (2-tone arcade floor)."""
    im = Image.new("RGB", (size, size), dark)
    px = im.load()
    cell = size / cells
    for y in range(size):
        for x in range(size):
            cx = int(x / cell)
            cy = int(y / cell)
            base = light if (cx + cy) % 2 == 0 else dark
            if bevel:
                # subtle inner bevel: brighter near top-left of each cell, darker bottom-right
                fx = (x % cell) / cell
                fy = (y % cell) / cell
                edge = min(fx, 1 - fx, fy, 1 - fy)
                if edge < 0.12:
                    shade = 1.0 + (0.12 - edge) * 1.2  # brighten rim slightly
                    shade = min(1.25, shade)
                else:
                    shade = 1.0 - (1 - min(fx, 1 - fx, fy, 1 - fy)) * 0.05
                base = tuple(min(255, max(0, int(c * shade))) for c in base)
            px[x, y] = base
    return im


def grid(size, lines, base, line_color):
    """A tech-grid: solid base with brighter grid lines."""
    im = Image.new("RGB", (size, size), base)
    d = ImageDraw.Draw(im)
    step = size // lines
    for i in range(0, size + 1, step):
        d.line([(i, 0), (i, size)], fill=line_color, width=2)
        d.line([(0, i), (size, i)], fill=line_color, width=2)
    # tiny corner dots for sparkle
    for i in range(0, size, step):
        for j in range(0, size, step):
            d.ellipse([i - 1, j - 1, i + 2, j + 2], fill=line_color)
    return im


def hazard(size, stripes, base, stripe_color, angle=45):
    """Diagonal hazard stripes (boost pads, ramps)."""
    im = Image.new("RGB", (size, size), base)
    px = im.load()
    period = size / stripes / 2
    for y in range(size):
        for x in range(size):
            # diagonal coordinate
            d = (x + y) / math.sqrt(2)
            if (d % (period * 2)) < period:
                px[x, y] = stripe_color
    return im


def rail(size, base, top):
    """A wall-rail: dark body with a bright band across the top third (reads as a guardrail)."""
    im = Image.new("RGB", (size, size), base)
    px = im.load()
    band = size // 3
    for y in range(size):
        for x in range(size):
            if y < band:
                # gradient from top to base
                t = y / band
                px[x, y] = lerp(top, base, t)
    # vertical seams every quarter for a panel feel
    d = ImageDraw.Draw(im)
    for qx in range(0, size, size // 4):
        d.line([(qx, 0), (qx, size)], fill=tuple(int(c * 0.7) for c in base), width=1)
    return im


def noise(size, amount=18, seed=1):
    """Soft greyscale noise for surface variation (used as a subtle overlay / detail map)."""
    rnd = random.Random(seed)
    im = Image.new("L", (size, size), 128)
    px = im.load()
    for y in range(size):
        for x in range(size):
            px[x, y] = 128 + rnd.randint(-amount, amount)
    return soften(im, 1)


def dots(size, cells, base, dot_color, radius=0.18):
    """A polka-dot pattern — candy candy candy."""
    im = Image.new("RGB", (size, size), base)
    d = ImageDraw.Draw(im)
    cell = size / cells
    r = cell * radius
    for cy in range(cells):
        for cx in range(cells):
            x = cx * cell + cell / 2
            y = cy * cell + cell / 2
            d.ellipse([x - r, y - r, x + r, y + r], fill=dot_color)
    return im


def main():
    print("Generating Capyball textures...")
    # Palette pairs (light, dark) in the candy range.
    mint = ((150, 250, 190), (60, 200, 120))
    sunny = ((255, 240, 150), (230, 180, 40))
    tangerine = ((255, 200, 130), (220, 110, 30))
    ocean = ((150, 210, 255), (40, 110, 200))
    grape = ((200, 170, 255), (120, 80, 200))

    # Checker floors — the signature arcade racetrack read.
    save(checker(256, 8, *mint), "checker_mint.png")
    save(checker(256, 8, *sunny), "checker_sunny.png")
    save(checker(256, 8, *ocean), "checker_ocean.png")

    # Grid platform tops.
    save(grid(256, 8, lerp(tangerine[1], tangerine[0], 0.3), tangerine[0]), "grid_tangerine.png")
    save(grid(256, 8, lerp(grape[1], grape[0], 0.3), grape[0]), "grid_grape.png")

    # Hazard stripes for boost pads / ramps.
    save(hazard(256, 6, (235, 150, 40), (255, 245, 200)), "hazard_orange.png")
    save(hazard(256, 6, (180, 60, 130), (255, 245, 200)), "hazard_pink.png")

    # Wall rails.
    save(rail(256, (60, 50, 90), (180, 170, 255)), "rail_grape.png")
    save(rail(256, (40, 60, 110), (150, 210, 255)), "rail_ocean.png")

    # Candy dots for variety.
    save(dots(256, 6, lerp(mint[1], mint[0], 0.3), (255, 255, 255)), "dots_mint.png")

    # Soft noise (greyscale) — universal surface detail overlay.
    save(noise(256), "noise_soft.png")

    print("Done.")


if __name__ == "__main__":
    main()
