#!/usr/bin/env python3
"""Write STANDINS.md: every reskinned sprite whose Eris source is a closest-match stand-in.

Usage: python3 Tools/eris_reskin/standins.py
"""

from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent


def describe(src) -> str:
    if isinstance(src, list):
        return f"{src[0]}:{src[1]}"
    if not isinstance(src, dict):
        return "?"
    for key in ("rsi", "dmi", "png", "file_png"):
        if key in src:
            return f"{src[key]}:{src.get('state', '')}".rstrip(":") + (" (recoloured)" if "recolor" in src else "")
    for key in ("layers", "mask_transfer", "corner_from_cardinal", "merge_dirs"):
        if key in src:
            inner = src[key]
            if key == "layers":
                return " + ".join(describe(s) for s in inner)
            if key == "corner_from_cardinal":
                return f"{inner['rsi']}:{inner['base']}* (tinted)" if "tint" in src else f"{inner['rsi']}:{inner['base']}*"
            return describe(inner.get("src", inner))
    return "?"


def main() -> int:
    lines = ["# Stand-ins", "",
             "Sprites below use the closest CEV-Eris art rather than an Eris version of the same thing. "
             "Fix one by adding an entry to the matching `*_overrides.json` (or the generator) and re-running "
             "`regen.sh`.", ""]
    total = 0
    for mp in sorted((HERE / "maps").glob("*.json")):
        rows = []
        for e in json.loads(mp.read_text()):
            if not e.get("standin"):
                continue
            target = e.get("target") or e.get("file")
            if "states" in e and e["states"]:
                first = next(iter(e["states"].values()))
                src = describe(first)
            elif "src" in e:
                src = describe(e["src"][0] if isinstance(e["src"], list) and e.get("strip") else e["src"])
            else:
                src = "SS14 art on an Eris panel, recoloured"
            rows.append(f"| `{target}` | `{src}` |")
        if rows:
            total += len(rows)
            lines += [f"## {mp.stem} ({len(rows)})", "", "| SS14 sprite | Eris source |", "|---|---|", *rows, ""]
    (HERE / "STANDINS.md").write_text("\n".join(lines) + "\n")
    print(f"{total} stand-ins listed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
