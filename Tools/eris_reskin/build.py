#!/usr/bin/env python3
"""Apply CEV-Eris reskin maps to Resources/Textures.

Every map under Tools/eris_reskin/maps/*.json is a list of entries:

  {"target": "Clothing/Head/Hats/beret.rsi",       # RSI under Resources/Textures
   "states": {"icon": SRC, "equipped-HELMET": SRC},  # RSI state -> source
   "standin": false,                                # true = closest Eris analog, not a 1:1 match
   "size": [32, 32]}                                # optional: new RSI frame size

  {"file": "Interface/Default/Slots/head.png", "src": SRC, "size": [32, 32]}

SRC is one of
  ["icons/obj/tools.dmi", "wrench"]                 # Eris DMI + state
  {"dmi": ..., "state": ..., "dir": 0, "frame": 0, "flip": "x", "crop": [x0, y0, x1, y1],
   "recolor": "eris_ui", "scale": "fit"}
  {"rsi": "Oxyd/erisported/foo.rsi", "state": "icon"} # already converted RSI in this repo
  {"layers": [SRC, SRC, ...]}                       # composited bottom to top
  {"blank": true}

RSI state names, direction counts and every unmapped state are kept, so all
prototypes, visualizers and C# that reference the sprite keep working.

Usage: python3 Tools/eris_reskin/build.py --eris ../CEV-Eris [--only substring]
"""

from __future__ import annotations

import argparse
import io
import json
import math
import subprocess
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dmi  # noqa: E402
import palette  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
TEXTURES = REPO / "Resources" / "Textures"
MAPS = Path(__file__).resolve().parent / "maps"
# Upstream commit whose art is the "original" for recolours, so rebuilding is idempotent.
BASE = (Path(__file__).resolve().parent / "BASE").read_text().strip()
CREDIT = "Reskinned with CEV-Eris sprites (https://github.com/discordia-space/CEV-Eris), see Tools/eris_reskin"

# .github/rsi-schema.json license enum.
ALLOWED_LICENSES = {"CC-BY-SA-3.0", "CC-BY-SA-4.0", "CC-BY-NC-3.0", "CC-BY-NC-4.0", "CC-BY-NC-SA-3.0",
                    "CC-BY-NC-SA-4.0", "CC0-1.0"}

# BYOND/RSI 8-dir order: S N E W SE SW NE NW. Diagonals fall back to E/W.
DIAG_FALLBACK = {4: 2, 5: 3, 6: 2, 7: 3}


class Source:
    """A resolved source: per-direction frame lists plus delays."""

    def __init__(self, images: list[list[Image.Image]], delays: list[float] | None):
        self.images = images
        self.delays = delays

    @property
    def dirs(self) -> int:
        return len(self.images)

    def dir_frames(self, d: int) -> list[Image.Image]:
        if self.dirs == 1:
            return self.images[0]
        if d < self.dirs:
            return self.images[d]
        return self.images[DIAG_FALLBACK.get(d, 0) % self.dirs]


class Builder:
    def __init__(self, eris: Path):
        self.eris = eris
        self.missing: list[str] = []

    # --- sources -------------------------------------------------------
    def resolve(self, src) -> Source | None:
        if isinstance(src, list):
            src = {"dmi": src[0], "state": src[1]}
        if src.get("blank"):
            return Source([[Image.new("RGBA", (32, 32))]], None)
        if "corner_from_cardinal" in src:
            res = self.corner_from_cardinal(**src["corner_from_cardinal"])
            if res and "tint" in src:
                res = Source([[palette.tint(im, src["tint"]) for im in fr] for fr in res.images], None)
            return res
        if "mask_transfer" in src:
            # Eris pixels cut to the reference's silhouette, then colour-matched to it, so the
            # result slots into SS14 systems built around the reference (skin tone tinting).
            spec = src["mask_transfer"]
            eris_src, ref = self.resolve(spec["src"]), self.resolve(spec["ref"])
            if eris_src is None or ref is None:
                return None
            images = []
            for d in range(ref.dirs):
                r = ref.dir_frames(d)[0]
                e = fit(eris_src.dir_frames(d)[0], r.size)
                images.append([palette.transfer(e, r, spec.get("keep_shape", False))])
            return Source(images, None)
        if "merge_dirs" in src:
            inner = self.resolve(src["merge_dirs"])
            if inner is None:
                return None
            canvas = Image.new("RGBA", inner.images[0][0].size)
            for fr in inner.images:
                canvas.alpha_composite(fr[0])
            return Source([[canvas]], None)
        if "layers" in src:
            parts = [self.resolve(s) for s in src["layers"]]
            parts = [p for p in parts if p]
            if not parts:
                return None
            base = max(parts, key=lambda p: (p.dirs, len(p.images[0])))
            images = []
            for d in range(base.dirs):
                frames = []
                for f in range(len(base.images[d])):
                    canvas = None
                    for p in parts:
                        fr = p.dir_frames(d)
                        im = fr[min(f, len(fr) - 1)]
                        if canvas is None:
                            canvas = Image.new("RGBA", im.size)
                        if im.size != canvas.size:
                            im = fit(im, canvas.size)
                        canvas.alpha_composite(im)
                    frames.append(canvas)
                images.append(frames)
            return Source(images, base.delays)
        if "file_png" in src:
            res = Source([[original_png(src["file_png"])]], None)
        elif "png" in src:
            res = Source([[Image.open(TEXTURES / src["png"]).convert("RGBA")]], None)
        elif "rsi" in src:
            res = self.from_rsi(src["rsi"], src["state"], src.get("base", False))
        else:
            st = dmi.get(self.eris, src["dmi"], src["state"])
            if st is None:
                self.missing.append(f"{src['dmi']}:{src['state']}")
                return None
            res = Source([list(fr) for fr in st.images], st.delays)
        if res is None:
            return None
        if "dir" in src:
            res = Source([res.dir_frames(src["dir"])], res.delays)
        if "frame" in src:
            res = Source([[fr[min(src["frame"], len(fr) - 1)]] for fr in res.images], None)
        if "crop" in src:
            box = tuple(src["crop"])
            res = Source([[im.crop(box) for im in fr] for fr in res.images], res.delays)
        if src.get("flip") == "x":
            res = Source([[im.transpose(Image.FLIP_LEFT_RIGHT) for im in fr] for fr in res.images], res.delays)
        if "recolor" in src:
            res = Source([[palette.recolor(im, src["recolor"]) for im in fr] for fr in res.images], res.delays)
        if "tint" in src:
            res = Source([[palette.tint(im, src["tint"]) for im in fr] for fr in res.images], res.delays)
        return res

    # IconSmooth corner layers: RSI direction S/N/E/W holds the SE/NW/NE/SW corner.
    # Corner value bits (Baystation CornerFill): 1 = counter-clockwise cardinal, 2 = diagonal, 4 = clockwise.
    # CardinalFlags bits: N=1, S=2, E=4, W=8.
    _CORNERS = [  # (quadrant box, ccw cardinal bit, cw cardinal bit) in S, N, E, W order
        ((16, 16, 32, 32), 4, 2),  # SE: ccw East, cw South
        ((0, 0, 16, 16), 8, 1),    # NW: ccw West, cw North
        ((16, 0, 32, 16), 1, 4),   # NE: ccw North, cw East
        ((0, 16, 16, 32), 2, 8),   # SW: ccw South, cw West
    ]

    def corner_from_cardinal(self, rsi: str, base: str, value: int) -> Source | None:
        """Build a corner-mode IconSmooth state from a CardinalFlags-mode RSI."""
        images = []
        for box, ccw, cw in self._CORNERS:
            mask = (ccw if value & 1 else 0) | (cw if value & 4 else 0)
            src = self.from_rsi(rsi, f"{base}{mask}")
            if src is None:
                return None
            quad = src.images[0][0].crop(box)
            canvas = Image.new("RGBA", src.images[0][0].size)
            canvas.alpha_composite(quad, box[:2])
            images.append([canvas])
        return Source(images, None)

    def from_rsi(self, rsi: str, state: str, base: bool = False) -> Source | None:
        """Read an RSI state from the working tree, or from BASE when base=True."""
        try:
            if base:
                meta = json.loads(git_bytes(f"{rsi}/meta.json").decode("utf-8-sig"))
                sheet = Image.open(io.BytesIO(git_bytes(f"{rsi}/{state}.png"))).convert("RGBA")
            else:
                meta = json.loads((TEXTURES / rsi / "meta.json").read_text(encoding="utf-8-sig"))
                sheet = Image.open(TEXTURES / rsi / f"{state}.png").convert("RGBA")
        except (FileNotFoundError, subprocess.CalledProcessError):
            self.missing.append(f"{rsi}:{state}")
            return None
        entry = next((s for s in meta["states"] if s["name"] == state), None)
        if entry is None:
            self.missing.append(f"{rsi}:{state}")
            return None
        w, h = meta["size"]["x"], meta["size"]["y"]
        cols = max(1, sheet.width // w)
        dirs = entry.get("directions", 1)
        delays = entry.get("delays")
        nframes = len(delays[0]) if delays else 1
        images = []
        for d in range(dirs):
            fr = []
            for f in range(nframes):
                r, c = divmod(d * nframes + f, cols)
                fr.append(sheet.crop((c * w, r * h, c * w + w, r * h + h)))
            images.append(fr)
        return Source(images, delays[0] if delays else None)

    # --- targets -------------------------------------------------------
    def apply_rsi(self, entry: dict) -> int:
        rdir = TEXTURES / entry["target"]
        meta_path = rdir / "meta.json"
        meta = json.loads(meta_path.read_text(encoding="utf-8-sig"))
        if entry.get("panel_all"):
            # Every state: the original SS14 art on the Eris palette, on an Eris HUD panel.
            # Single-character states are counter digits drawn over a base, so they get no panel.
            entry = dict(entry, states={})
            for st in meta["states"]:
                glyph = {"rsi": entry["target"], "state": st["name"], "base": True, "recolor": "eris_ui"}
                entry["states"][st["name"]] = glyph if len(st["name"]) == 1 else {
                    "layers": [{"dmi": "icons/mob/screen/ErisStyle.dmi", "state": "block"}, glyph]}
        size = tuple(entry.get("size") or (meta["size"]["x"], meta["size"]["y"]))
        if list(size) != [meta["size"]["x"], meta["size"]["y"]]:
            unmapped = [s["name"] for s in meta["states"] if s["name"] not in entry["states"]]
            if unmapped:
                size = (meta["size"]["x"], meta["size"]["y"])
        by_name = {s["name"]: s for s in meta["states"]}
        written = 0
        for name, src in entry["states"].items():
            st = by_name.get(name)
            if st is None:
                continue
            res = self.resolve(src)
            if res is None:
                continue
            dirs = st.get("directions", 1)
            per_dir = [[fit(im, size) for im in res.dir_frames(d)] for d in range(dirs)]
            n = len(per_dir[0])
            write_sheet(rdir / f"{name}.png", per_dir, size)
            if n > 1 and res.delays:
                st["delays"] = [list(res.delays[:n]) for _ in range(dirs)]
            else:
                st.pop("delays", None)
            written += 1
        if written:
            meta["size"] = {"x": size[0], "y": size[1]}
            if CREDIT not in (meta.get("copyright") or ""):
                meta["copyright"] = f"{meta.get('copyright') or ''}. {CREDIT}".lstrip(". ")
            # The art now includes CC-BY-SA CEV-Eris sprites; CC-BY sources can be shared under it.
            if meta.get("license") not in ALLOWED_LICENSES:
                meta["license"] = "CC-BY-SA-3.0"
            meta_path.write_text(json.dumps(meta, indent=4) + "\n", encoding="utf-8")
        return written

    def apply_file(self, entry: dict) -> int:
        path = TEXTURES / entry["file"]
        if entry.get("strip"):
            # Tile variant strips: one 32x32 variant per column, filled from the given sources in turn.
            srcs = [self.resolve(s) for s in entry["src"]]
            srcs = [s.images[0][0] for s in srcs if s]
            if not srcs:
                return 0
            with Image.open(path) as old:
                w, h = old.size
            sheet = Image.new("RGBA", (w, h))
            for i in range(max(1, w // h)):
                sheet.alpha_composite(fit(srcs[i % len(srcs)], (h, h), "stretch"), (i * h, 0))
            sheet.save(path, optimize=True)
            return 1
        res = self.resolve(entry["src"])
        if res is None:
            return 0
        im = res.images[0][0]
        if "size" in entry:
            im = fit(im, tuple(entry["size"]), entry.get("scale", "fit"))
        elif path.exists():
            with Image.open(path) as old:
                im = fit(im, old.size, entry.get("scale", "fit"))
        path.parent.mkdir(parents=True, exist_ok=True)
        im.save(path)
        return 1


def git_bytes(path: str) -> bytes:
    """Bytes of Resources/Textures/<path> at the upstream BASE commit."""
    return subprocess.run(
        ["git", "-C", str(REPO), "show", f"{BASE}:Resources/Textures/{path}"],
        check=True, capture_output=True,
    ).stdout


def original_png(path: str) -> Image.Image:
    return Image.open(io.BytesIO(git_bytes(path))).convert("RGBA")


def fit(im: Image.Image, size: tuple[int, int], mode: str = "fit") -> Image.Image:
    """Place im on a canvas of size. Larger art is cropped to its content box,
    then scaled down if it still does not fit. mode="stretch" resizes exactly."""
    if im.size == tuple(size):
        return im
    if mode == "stretch":
        return im.resize(size, Image.NEAREST)
    bbox = im.getbbox()
    if bbox is None:
        return Image.new("RGBA", size)
    if im.width > size[0] or im.height > size[1]:
        content = im.crop(bbox)
        if content.width > size[0] or content.height > size[1]:
            scale = min(size[0] / content.width, size[1] / content.height)
            content = content.resize(
                (max(1, int(content.width * scale)), max(1, int(content.height * scale))),
                Image.NEAREST if scale >= 0.5 else Image.BOX,
            )
        canvas = Image.new("RGBA", size)
        # Keep content centred on the source's centre where possible.
        cx = (bbox[0] + bbox[2]) / 2 - im.width / 2 + size[0] / 2
        cy = (bbox[1] + bbox[3]) / 2 - im.height / 2 + size[1] / 2
        x = int(min(max(cx - content.width / 2, 0), size[0] - content.width))
        y = int(min(max(cy - content.height / 2, 0), size[1] - content.height))
        canvas.alpha_composite(content, (x, y))
        return canvas
    canvas = Image.new("RGBA", size)
    canvas.alpha_composite(im, ((size[0] - im.width) // 2, (size[1] - im.height) // 2))
    return canvas


def write_sheet(path: Path, per_dir: list[list[Image.Image]], size: tuple[int, int]) -> None:
    frames = [im for fr in per_dir for im in fr]
    cols = math.ceil(math.sqrt(len(frames)))
    rows = math.ceil(len(frames) / cols)
    sheet = Image.new("RGBA", (cols * size[0], rows * size[1]))
    for i, im in enumerate(frames):
        r, c = divmod(i, cols)
        sheet.alpha_composite(im, (c * size[0], r * size[1]))
    sheet.save(path, optimize=True)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--eris", type=Path, required=True, help="path to a CEV-Eris checkout")
    ap.add_argument("--only", default="", help="only apply map files whose name contains this")
    args = ap.parse_args()

    b = Builder(args.eris.resolve())
    total_rsi = total_states = total_files = 0
    for mp in sorted(MAPS.glob("*.json")):
        if args.only not in mp.name:
            continue
        for entry in json.loads(mp.read_text(encoding="utf-8")):
            if "file" in entry:
                total_files += b.apply_file(entry)
            else:
                n = b.apply_rsi(entry)
                total_states += n
                total_rsi += bool(n)
    print(f"reskinned {total_rsi} RSIs ({total_states} states) and {total_files} loose textures")
    if b.missing:
        print(f"{len(b.missing)} missing sources, first 20:", *sorted(set(b.missing))[:20], sep="\n  ")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
