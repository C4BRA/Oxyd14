"""Colour helpers for pieces that need to be pulled onto the CEV-Eris palette."""

from __future__ import annotations

from PIL import Image

# Sampled from icons/mob/screen/ErisStyle.dmi (HUD panels), darkest to lightest.
RAMPS = {
    "eris_ui": ["#07090e", "#141824", "#182133", "#1f3850", "#254966", "#284f71", "#4299d3", "#a9d8f5"],
    # Eris NanoUI chrome (nano/css/shared.css).
    "eris_nano": ["#101010", "#202020", "#272727", "#2c2c2c", "#40628a", "#507aac", "#98b0c3", "#ffffff"],
    # Eris NanoUI title bar blue, bright enough that SS14's dark header art lands on #40628a.
    "eris_header": ["#1f3850", "#40628a", "#40628a", "#40628a", "#507aac", "#98b0c3"],
    # Eris ship hull greys (walls.dmi, tables.dmi).
    "eris_metal": ["#141414", "#2c2f2f", "#4d535e", "#767e8f", "#9399a7", "#a5abb8", "#d0d4dc"],
}


def _hex(c: str) -> tuple[int, int, int]:
    c = c.lstrip("#")
    return int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16)


def _lut(stops: list[str]) -> list[tuple[int, int, int]]:
    cols = [_hex(s) for s in stops]
    lut = []
    for i in range(256):
        t = i / 255 * (len(cols) - 1)
        a = int(t)
        b = min(a + 1, len(cols) - 1)
        f = t - a
        lut.append(tuple(round(cols[a][k] + (cols[b][k] - cols[a][k]) * f) for k in range(3)))
    return lut


_LUTS: dict[str, list[tuple[int, int, int]]] = {}


def recolor(im: Image.Image, ramp: str) -> Image.Image:
    """Map each pixel's luminance onto a palette ramp, keeping alpha."""
    lut = _LUTS.setdefault(ramp, _lut(RAMPS[ramp]))
    im = im.convert("RGBA")
    lum = im.convert("L")
    out = Image.new("RGBA", im.size)
    px_l = lum.load()
    px_a = im.getchannel("A").load()
    px_o = out.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b = lut[px_l[x, y]]
            px_o[x, y] = (r, g, b, px_a[x, y])
    return out


def tint(im: Image.Image, colour: str) -> Image.Image:
    """Multiply by a colour, the way BYOND applies obj.color."""
    r, g, b = _hex(colour)
    im = im.convert("RGBA")
    ch = im.split()
    return Image.merge(
        "RGBA",
        (
            ch[0].point(lambda v: v * r // 255),
            ch[1].point(lambda v: v * g // 255),
            ch[2].point(lambda v: v * b // 255),
            ch[3],
        ),
    )


def transfer(src: Image.Image, ref: Image.Image, keep_shape: bool = False) -> Image.Image:
    """src pixels inside ref's silhouette (ref pixels where src is empty), with each colour
    channel's mean and spread moved to ref's. Used to fit Eris body art to SS14 skin tinting."""
    src, ref = src.convert("RGBA"), ref.convert("RGBA")
    sp, rp = src.load(), ref.load()
    w, h = ref.size
    if keep_shape:
        # Keep the source's shape; only borrow the reference's colour statistics.
        ref_px = [rp[x, y] for y in range(h) for x in range(w) if rp[x, y][3] > 0]
        src_px = [sp[x, y] for y in range(h) for x in range(w) if sp[x, y][3] > 0]
        if not ref_px or not src_px:
            return src.copy()
        out = src.copy()
        op = out.load()
        for c in range(3):
            sv, rv = [p[c] for p in src_px], [p[c] for p in ref_px]
            sm, rm = sum(sv) / len(sv), sum(rv) / len(rv)
            ssd = (sum((v - sm) ** 2 for v in sv) / len(sv)) ** 0.5 or 1.0
            rsd = (sum((v - rm) ** 2 for v in rv) / len(rv)) ** 0.5 or 1.0
            for y in range(h):
                for x in range(w):
                    p = list(op[x, y])
                    if p[3] > 0:
                        p[c] = int(min(255, max(0, (p[c] - sm) * rsd / ssd + rm)))
                        op[x, y] = tuple(p)
        return out
    pts = [(x, y) for y in range(h) for x in range(w) if rp[x, y][3] > 0]
    if not pts:
        return Image.new("RGBA", ref.size)
    src_px = [sp[x, y] for x, y in pts if sp[x, y][3] > 0]
    ref_px = [rp[x, y] for x, y in pts]
    if not src_px:
        return ref.copy()

    def stats(px, c):
        vals = [p[c] for p in px]
        m = sum(vals) / len(vals)
        sd = (sum((v - m) ** 2 for v in vals) / len(vals)) ** 0.5 or 1.0
        return m, sd

    st = [(stats(src_px, c), stats(ref_px, c)) for c in range(3)]
    out = Image.new("RGBA", ref.size)
    op = out.load()
    for x, y in pts:
        if sp[x, y][3] > 0:
            p = sp[x, y]
            col = [int(min(255, max(0, (p[c] - st[c][0][0]) * st[c][1][1] / st[c][0][1] + st[c][1][0])))
                   for c in range(3)]
        else:
            col = list(rp[x, y][:3])
        op[x, y] = (*col, rp[x, y][3])
    return out
