"""Generate the Countdown application icon.

Design mirrors the floating timer card: a warm-black rounded tile
(#161310), a cream countdown ring (#F3E9DC) notched at the top,
clock hands near 12 o'clock, and a red tip in the app's Finished red.

Outputs (next to this script):
  app.ico        multi-size icon (16/24/32/48/64/128/256)
  app-256.png    preview render

Run:  python generate_icon.py
"""

import math
from pathlib import Path

from PIL import Image, ImageDraw

SIZES = [16, 24, 32, 48, 64, 128, 256]
SUPERSAMPLE = 8  # render at 8x then downscale for edge anti-aliasing
DESIGN = 1024.0  # design canvas the geometry below is expressed in

# Palette (kept in sync with app.svg and TimerWindow.xaml)
TILE = (22, 19, 16, 255)          # #161310 running card fill
CREAM = (243, 233, 220, 255)      # #F3E9DC digit foreground
TRACK = (243, 233, 220, 38)       # cream at ~15%: unelapsed ring track
RED = (224, 80, 70, 255)          # brighter take on the #8C1D1D finish red
HAIRLINE = (255, 255, 255, 30)    # card hairline (#26FFFFFF ≈ 15%)

# Geometry in design units (0..1024)
MARGIN = 26          # transparent gap around the tile
TILE_RADIUS = 224    # tile corner radius
HAIR_INSET = 14
HAIR_WIDTH = 7
CX, CY = 512.0, 520.0     # ring center (slightly low: optical balance vs. notch)
RING_R = 300.0
TRACK_WIDTH = 56.0
ARC_WIDTH = 76.0
GAP_DEG = 52.0            # ring notch centered on 12 o'clock
DOT_R = 42.0              # red tip at the leading arc end
HANDS_MIN_SIZE = 48       # below this the hands blur to mud; ring-only reads
DOT_MIN_SIZE = 48         # red nub merges with the cream cap into mush at 16-32
# Hands in the canonical 10:10 layout: a narrow near-12 angle reads as a
# checkmark at small sizes; the open ^ shape is instantly a clock face.
MINUTE_DEG = -30          # 2 o'clock (long hand)
HOUR_DEG = -150           # 10 o'clock (short hand)
MINUTE_LEN, MINUTE_W = 190.0, 30.0
HOUR_LEN, HOUR_W = 142.0, 42.0
HUB_R = 34.0


def _pt(cx, cy, r, deg):
    a = math.radians(deg)
    return cx + r * math.cos(a), cy + r * math.sin(a)


def _snap_alpha(img: Image.Image) -> Image.Image:
    """Crush faint edge pixels and snap near-solid ones solid.

    Heavy LANCZOS downsampling of the 16-24px frames leaves a 1px gray halo;
    a steeper alpha curve keeps the tile edge clean and the ring gutsy.
    """
    r, g, b, a = img.split()
    lut = [0] * 40 + [round((v - 40) * 255 / 130) for v in range(40, 170)] + [255] * 86
    return Image.merge("RGBA", (r, g, b, a.point(lut)))


def render(size: int) -> Image.Image:
    # Small frames get a simplified, bolder treatment rather than a shrunken
    # version of the full detail (see branches below).
    small = size <= 32
    ss = 4 if small else SUPERSAMPLE
    s = size * ss
    k = s / DESIGN  # design-unit -> supersampled-pixel scale
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    def box(cx, cy, r):
        return [(cx - r) * k, (cy - r) * k, (cx + r) * k, (cy + r) * k]

    # Tile + 1px hairline, like the timer card itself.
    # Hairline is sub-pixel at small sizes; skip it so it can't gray the edge.
    d.rounded_rectangle(
        [MARGIN * k, MARGIN * k, (DESIGN - MARGIN) * k, (DESIGN - MARGIN) * k],
        radius=TILE_RADIUS * k,
        fill=TILE,
    )
    if not small:
        h = MARGIN + HAIR_INSET
        d.rounded_rectangle(
            [h * k, h * k, (DESIGN - h) * k, (DESIGN - h) * k],
            radius=(TILE_RADIUS - HAIR_INSET) * k,
            outline=HAIRLINE,
            width=max(1, round(HAIR_WIDTH * k)),
        )

    # Faint full track ring, then the elapsed arc notched at the top.
    # Track is dropped for small sizes: there it reads as a muddy halo.
    arc_width = ARC_WIDTH + 16 if small else ARC_WIDTH
    gap_deg = GAP_DEG - 8 if small else GAP_DEG
    if not small:
        d.ellipse(box(CX, CY, RING_R), outline=TRACK, width=round(TRACK_WIDTH * k))
    arc_start = -90 + gap_deg / 2          # just clockwise of 12
    arc_end = arc_start + (360 - gap_deg)  # sweep nearly all the way around
    d.arc(
        box(CX, CY, RING_R),
        start=arc_start,
        end=arc_end,
        fill=CREAM,
        width=round(arc_width * k),
    )
    # Pillow arcs have square caps; add round caps on both arc ends.
    cap_r = arc_width / 2
    for deg in (arc_start, arc_end):
        x, y = _pt(CX, CY, RING_R, deg)
        d.ellipse(box(x, y, cap_r), fill=CREAM)

    # Red nub on the leading (upper-left) end: the countdown about to close.
    # Skipped at 16-32px, where it merges with the cream cap into a pink blob.
    if size >= DOT_MIN_SIZE:
        x, y = _pt(CX, CY, RING_R, arc_end)
        d.ellipse(box(x, y, DOT_R), fill=RED)

    if size >= HANDS_MIN_SIZE:
        def hand(deg, length, width):
            x1, y1 = _pt(CX, CY, length, deg)
            d.line([CX * k, CY * k, x1 * k, y1 * k], fill=CREAM, width=round(width * k))
            d.ellipse(box(x1, y1, width / 2), fill=CREAM)  # round tip cap

        hand(MINUTE_DEG, MINUTE_LEN, MINUTE_W)   # long hand toward 2
        hand(HOUR_DEG, HOUR_LEN, HOUR_W)         # short hand toward 10
        d.ellipse(box(CX, CY, HUB_R), fill=CREAM)

    out = img.resize((size, size), Image.LANCZOS)
    return _snap_alpha(out) if small else out


def main() -> None:
    out_dir = Path(__file__).resolve().parent
    frames = {size: render(size) for size in SIZES}

    ico_path = out_dir / "app.ico"
    # Every size must appear in `sizes`; matching frames are taken verbatim
    # from append_images instead of being resized from the 256 base frame.
    frames[256].save(
        ico_path,
        format="ICO",
        sizes=[(s, s) for s in SIZES],
        append_images=[frames[s] for s in SIZES if s != 256],
    )
    frames[256].save(out_dir / "app-256.png", format="PNG")

    print(f"wrote {ico_path} ({ico_path.stat().st_size} bytes)")
    print(f"wrote {out_dir / 'app-256.png'}")


if __name__ == "__main__":
    main()
