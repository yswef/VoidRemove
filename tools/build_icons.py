#!/usr/bin/env python3
"""
Generates the VoidRemove brand mark and every binary derived from it.

    python tools/build_icons.py

One geometric description produces:

  assets/logo.svg              source of truth, hand-editable output
  assets/logo-mono.svg         single color variant for docs and monochrome contexts
  assets/png/icon-<n>.png      16, 24, 32, 48, 64, 128, 256, 512 and 1024
  assets/icon.ico              multi-resolution Windows icon (WPF exe + Inno Setup)

Sizes up to 32 px use a simplified drawing (full ring, no particles) because the detail
of the aperture disappears at that size.

Requires Pillow:  pip install pillow
"""

from __future__ import annotations

import io
import math
import struct
import sys
from pathlib import Path

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover - developer environment problem, not a logic one
    sys.exit("Pillow is required: python -m pip install pillow")

REPO_ROOT = Path(__file__).resolve().parent.parent
ASSETS = REPO_ROOT / "assets"
PNG_DIR = ASSETS / "png"

# ---------------------------------------------------------------- geometry

CANVAS = 512
CENTER = CANVAS / 2

TILE_RADIUS = 112
TILE_TOP = "#0C1017"
TILE_BOTTOM = "#05070A"

RING_RADIUS = 168
RING_WIDTH = 34
BLADE_SWEEP = 70.0
BLADE_GAP = 20.0
BLADE_ROTATION = -18.0

# Mint to violet, matching the accent ramp used by the Dark theme.
BLADE_COLORS = ("#2EE6A8", "#33D9C6", "#5A94F5", "#7B61FF")

ACCENT = "#2EE6A8"
VIOLET = "#7B61FF"
VOID_FILL = "#05070A"

VOID_RADIUS = 66
CORE_RING_RADIUS = 54
CORE_DOT_RADIUS = 16

PARTICLES = (
    # angle in degrees, distance from the center, radius, opacity
    (200.0, 236.0, 8.0, 0.90),
    (216.0, 212.0, 6.0, 0.65),
    (230.0, 194.0, 4.0, 0.40),
)

PNG_SIZES = (16, 24, 32, 48, 64, 128, 256, 512, 1024)
ICO_SIZES = (16, 24, 32, 48, 64, 128, 256)
SIMPLIFIED_BELOW = 48

SUPERSAMPLE = 4


# ---------------------------------------------------------------- helpers


def polar(angle_deg: float, radius: float) -> tuple[float, float]:
    """Point on a circle, 0 degrees pointing right, clockwise on screen."""
    radians = math.radians(angle_deg)
    return CENTER + radius * math.cos(radians), CENTER + radius * math.sin(radians)


def blade_angles() -> list[tuple[float, float]]:
    step = BLADE_SWEEP + BLADE_GAP
    return [(BLADE_ROTATION + index * step, BLADE_ROTATION + index * step + BLADE_SWEEP)
            for index in range(len(BLADE_COLORS))]


def arc_path(radius: float, start: float, end: float) -> str:
    x1, y1 = polar(start, radius)
    x2, y2 = polar(end, radius)
    large_arc = 1 if abs(end - start) > 180 else 0
    return f"M {x1:.2f} {y1:.2f} A {radius} {radius} 0 {large_arc} 1 {x2:.2f} {y2:.2f}"


# ---------------------------------------------------------------- svg


def build_svg(simplified: bool = False) -> str:
    parts: list[str] = []
    parts.append(
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" '
        'width="512" height="512" role="img" aria-label="VoidRemove">'
    )
    parts.append("  <title>VoidRemove</title>")
    parts.append("  <defs>")
    parts.append('    <linearGradient id="voidTile" x1="0" y1="0" x2="0" y2="1">')
    parts.append(f'      <stop offset="0" stop-color="{TILE_TOP}"/>')
    parts.append(f'      <stop offset="1" stop-color="{TILE_BOTTOM}"/>')
    parts.append("    </linearGradient>")
    parts.append("  </defs>")
    parts.append(f'  <rect width="512" height="512" rx="{TILE_RADIUS}" fill="url(#voidTile)"/>')

    if simplified:
        parts.append(
            f'  <circle cx="256" cy="256" r="{RING_RADIUS}" fill="none" '
            f'stroke="{ACCENT}" stroke-width="{RING_WIDTH + 10}"/>'
        )
    else:
        for (start, end), color in zip(blade_angles(), BLADE_COLORS):
            parts.append(
                f'  <path d="{arc_path(RING_RADIUS, start, end)}" fill="none" '
                f'stroke="{color}" stroke-width="{RING_WIDTH}" stroke-linecap="round"/>'
            )
        for angle, distance, radius, opacity in PARTICLES:
            x, y = polar(angle, distance)
            parts.append(
                f'  <circle cx="{x:.2f}" cy="{y:.2f}" r="{radius}" '
                f'fill="{ACCENT}" opacity="{opacity:.2f}"/>'
            )

    parts.append(f'  <circle cx="256" cy="256" r="{VOID_RADIUS}" fill="{VOID_FILL}"/>')
    parts.append(
        f'  <circle cx="256" cy="256" r="{CORE_RING_RADIUS}" fill="none" '
        f'stroke="{VIOLET}" stroke-width="6" opacity="0.85"/>'
    )
    parts.append(f'  <circle cx="256" cy="256" r="{CORE_DOT_RADIUS}" fill="{ACCENT}"/>')
    parts.append("</svg>")
    return "\n".join(parts) + "\n"


def build_mono_svg() -> str:
    """Single color mark for documentation, favicons and print."""
    parts: list[str] = []
    parts.append(
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" '
        'width="512" height="512" role="img" aria-label="VoidRemove">'
    )
    parts.append("  <title>VoidRemove</title>")
    for (start, end), _ in zip(blade_angles(), BLADE_COLORS):
        parts.append(
            f'  <path d="{arc_path(RING_RADIUS, start, end)}" fill="none" '
            f'stroke="currentColor" stroke-width="{RING_WIDTH}" stroke-linecap="round"/>'
        )
    parts.append(f'  <circle cx="256" cy="256" r="{CORE_DOT_RADIUS}" fill="currentColor"/>')
    parts.append("</svg>")
    return "\n".join(parts) + "\n"


# ---------------------------------------------------------------- raster


def hex_to_rgba(value: str) -> tuple[int, int, int, int]:
    text = value.lstrip("#")
    return int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16), 255


def draw_tile(image: Image.Image, size: int) -> None:
    """Rounded square with a vertical gradient, masked to the tile shape."""
    radius = int(TILE_RADIUS * size / CANVAS)
    top = hex_to_rgba(TILE_TOP)
    bottom = hex_to_rgba(TILE_BOTTOM)

    gradient = Image.new("RGBA", (size, size))
    gradient_draw = ImageDraw.Draw(gradient)
    for y in range(size):
        t = y / max(1, size - 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        gradient_draw.line([(0, y), (size, y)], fill=color)

    mask = Image.new("L", (size * SUPERSAMPLE, size * SUPERSAMPLE), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [0, 0, size * SUPERSAMPLE - 1, size * SUPERSAMPLE - 1],
        radius=radius * SUPERSAMPLE,
        fill=255,
    )
    mask = mask.resize((size, size), Image.LANCZOS)

    image.paste(gradient, (0, 0), mask)


def draw_logo(size: int) -> Image.Image:
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    scale = size / CANVAS

    draw_tile(image, size)
    draw = ImageDraw.Draw(image)

    if size <= SIMPLIFIED_BELOW:
        radius = RING_RADIUS * scale
        width = max(1, int((RING_WIDTH + 10) * scale))
        draw.ellipse(
            [CENTER * scale - radius, CENTER * scale - radius,
             CENTER * scale + radius, CENTER * scale + radius],
            outline=hex_to_rgba(ACCENT),
            width=width,
        )
    else:
        radius = RING_RADIUS * scale
        width = max(1, int(RING_WIDTH * scale))
        for (start, end), color in zip(blade_angles(), BLADE_COLORS):
            x1, y1 = polar(start, RING_RADIUS)
            x2, y2 = polar(end, RING_RADIUS)
            box = [CENTER * scale - radius, CENTER * scale - radius,
                   CENTER * scale + radius, CENTER * scale + radius]
            draw.arc(box, start=start, end=end, fill=hex_to_rgba(color), width=width)
            cap = width / 2
            for x, y in ((x1 * scale, y1 * scale), (x2 * scale, y2 * scale)):
                draw.ellipse([x - cap, y - cap, x + cap, y + cap], fill=hex_to_rgba(color))

        for angle, distance, particle_radius, opacity in PARTICLES:
            x, y = polar(angle, distance * scale)
            r = max(1.0, particle_radius * scale)
            color = hex_to_rgba(ACCENT)
            draw.ellipse([x - r, y - r, x + r, y + r], fill=(color[0], color[1], color[2], int(255 * opacity)))

    void_radius = VOID_RADIUS * scale
    if size <= SIMPLIFIED_BELOW:
        void_radius -= width * 0.35
    draw.ellipse(
        [CENTER * scale - void_radius, CENTER * scale - void_radius,
         CENTER * scale + void_radius, CENTER * scale + void_radius],
        fill=hex_to_rgba(VOID_FILL),
    )

    if size > SIMPLIFIED_BELOW:
        ring_radius = CORE_RING_RADIUS * scale
        draw.ellipse(
            [CENTER * scale - ring_radius, CENTER * scale - ring_radius,
             CENTER * scale + ring_radius, CENTER * scale + ring_radius],
            outline=hex_to_rgba(VIOLET),
            width=max(1, int(6 * scale)),
        )

    dot_radius = CORE_DOT_RADIUS * scale
    if size <= SIMPLIFIED_BELOW:
        dot_radius *= 1.35
    draw.ellipse(
        [CENTER * scale - dot_radius, CENTER * scale - dot_radius,
         CENTER * scale + dot_radius, CENTER * scale + dot_radius],
        fill=hex_to_rgba(ACCENT),
    )

    return image


def render(size: int) -> Image.Image:
    """Supersampled render, so the small sizes stay crisp."""
    large = draw_logo(size * SUPERSAMPLE)
    return large.resize((size, size), Image.LANCZOS)


# ---------------------------------------------------------------- ico container


def write_ico(path: Path, images: dict[int, Image.Image]) -> None:
    """Packs PNG-compressed frames into a Windows .ico (Vista and newer)."""
    entries: list[bytes] = []
    payloads: list[bytes] = []
    offset = 6 + 16 * len(images)

    for size in sorted(images):
        buffer = io.BytesIO()
        images[size].save(buffer, format="PNG")
        data = buffer.getvalue()

        dimension = 0 if size >= 256 else size
        entries.append(struct.pack(
            "<BBBBHHII",
            dimension, dimension, 0, 0, 1, 32, len(data), offset,
        ))
        payloads.append(data)
        offset += len(data)

    header = struct.pack("<HHH", 0, 1, len(images))
    path.write_bytes(header + b"".join(entries) + b"".join(payloads))


# ---------------------------------------------------------------- main


def main() -> int:
    ASSETS.mkdir(parents=True, exist_ok=True)
    PNG_DIR.mkdir(parents=True, exist_ok=True)

    (ASSETS / "logo.svg").write_text(build_svg(), encoding="utf-8")
    (ASSETS / "logo-mono.svg").write_text(build_mono_svg(), encoding="utf-8")

    rendered: dict[int, Image.Image] = {}
    for size in PNG_SIZES:
        image = render(size)
        image.save(PNG_DIR / f"icon-{size}.png", format="PNG")
        rendered[size] = image

    write_ico(ASSETS / "icon.ico", {size: rendered[size] for size in ICO_SIZES})

    print(f"wrote {ASSETS / 'logo.svg'}")
    print(f"wrote {ASSETS / 'logo-mono.svg'}")
    print(f"wrote {len(PNG_SIZES)} PNG sizes into {PNG_DIR}")
    print(f"wrote {ASSETS / 'icon.ico'} with frames: {', '.join(str(s) for s in ICO_SIZES)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
