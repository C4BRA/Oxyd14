#!/usr/bin/env python3
"""Generate maps/smoothing.json: walls, windows and tables (IconSmooth corner sprites).

Walls and windows take their corner pieces from the CEV-Eris walls and windows
already baked for CardinalFlags smoothing under Textures/Oxyd/erisported. Wall
and window materials Eris does not have (gold, brass, uranium, ...) get the
Eris shape tinted to the SS14 material colour.

Tables use icons/obj/tables.dmi directly: Eris tables are a frame plus a
material layer per corner with the same corner encoding SS14 uses, tinted by
the material colour as Eris does (code/modules/materials/materials.dm).

Usage: python3 Tools/eris_reskin/gen_smoothing.py
"""

from __future__ import annotations

import json
import re
from pathlib import Path

import match

HERE = Path(__file__).resolve().parent
WALLS = "Oxyd/erisported/walls.rsi"
WINDOWS = "Oxyd/erisported/windows/"
TABLES = "icons/obj/tables.dmi"
STEEL = "#8e9799"  # PLASTEEL_COLOUR, used by Eris steel

# SS14 wall RSIs that already are Eris steel; everything else is tinted to its own colour.
UNTINTED_WALLS = {"solid", "reinforced", "solid-rust"}
WINDOW_SOURCES = {
    "window-base": ("glass", None), "window-base-reinf": ("reinf", None), "window-plasma": ("plasma", None),
    "window-plasma-reinf": ("plasma_reinf", None), "window-tinted": ("tinted", None),
    "window-uranium": ("plasma", "#62e36c"), "window-uranium-reinf": ("plasma_reinf", "#62e36c"),
    "window-plastitanium": ("reinf", "#6f7a80"), "window-shuttle": ("reinf", "#9fb4c8"),
    "window-regal": ("glass", "#edd12f"), "window-malign": ("glass", "#9c56c4"),
    "exo-resin": ("glass", "#b5a07a"), "exo-resin-borg": ("glass", "#b5a07a"),
}
# SS14 table RSI -> (Eris material layer, tint). Colours from Eris materials.dm.
TABLE_SOURCES = {
    "generic": ("metal", STEEL), "countermetal": ("metal", STEEL), "frame": (None, None),
    "reinforced": ("reinf_metal", STEEL), "wood": ("wood", None), "counterwood": ("wood", None),
    "glass": ("glass", "#00e1ff"), "r_glass": ("rglass", "#00e1ff"), "plasma": ("glass", "#fc2bc5"),
    "stone": ("stone", "#d9c179"), "brass": ("metal", "#edd12f"), "card": ("solid", "#aaaaaa"),
    "carpet": ("carpet", "#da020a"), "web": ("solid", "#e8e7c8"), "xeno": ("solid", "#6c7364"),
    "debug": ("solid", "#ff00ff"),
}


def avg_tint(rsi: str, state: str) -> str:
    im = match.rsi_first_frame(rsi, state)
    if im is None:
        return STEEL
    px = [p for p in im.getdata() if p[3] > 128]
    if not px:
        return STEEL
    r, g, b = (sum(p[i] for p in px) / len(px) for i in range(3))
    m = max(r, g, b, 1)
    return "#%02x%02x%02x" % tuple(int(min(255, c * 255 / m)) for c in (r, g, b))


def corner_bases(states: list[dict]) -> dict[str, list[int]]:
    bases: dict[str, list[int]] = {}
    for s in states:
        m = re.fullmatch(r"(.*?)([0-7])", s["name"])
        if m and s.get("directions", 1) == 4:
            bases.setdefault(m.group(1), []).append(int(m.group(2)))
    return bases


def cardinal_base(rsi: str) -> str:
    meta, states = match.rsi_states(rsi)
    for s in states:
        m = re.fullmatch(r"(.*?)15", s["name"])
        if m:
            return m.group(1)
    raise ValueError(f"no cardinal states in {rsi}")


def main() -> int:
    entries = []

    for rsi in match.list_rsis("Structures/Walls"):
        stem = Path(rsi).stem
        if "diagonal" in stem or stem in ("railing", "grille", "fence", "wooden_fence"):
            continue
        meta, states = match.rsi_states(rsi)
        bases = corner_bases(states)
        if not bases:
            continue
        tint = None if stem in UNTINTED_WALLS else avg_tint(rsi, "full")
        _, eris_states = match.rsi_states(WALLS)
        eris_names = {s["name"] for s in eris_states}
        out = {}
        for base, values in bases.items():
            eris_base = "reinf_over" if "reinf" in base else "solid"
            for v in values:
                src = {"corner_from_cardinal": {"rsi": WALLS, "base": eris_base, "value": v}}
                if tint:
                    src["tint"] = tint
                out[f"{base}{v}"] = src
        for s in states:
            if s["name"] in out:
                continue
            if s["name"] in eris_names:
                out[s["name"]] = {"rsi": WALLS, "state": s["name"], **({"tint": tint} if tint else {})}
        entries.append({"target": rsi, "standin": bool(tint), "states": out})

    for rsi in match.list_rsis("Structures/Windows"):
        stem = Path(rsi).stem
        if stem == "directional":
            meta, states = match.rsi_states(rsi)
            _, es = match.rsi_states(WINDOWS + "directional.rsi")
            names = {s["name"] for s in es}
            out = {s["name"]: {"rsi": WINDOWS + "directional.rsi", "state": s["name"]} for s in states if s["name"] in names}
            entries.append({"target": rsi, "standin": False, "states": out})
            continue
        if stem not in WINDOW_SOURCES:
            continue
        src_name, tint = WINDOW_SOURCES[stem]
        src_rsi = f"{WINDOWS}{src_name}.rsi"
        eris_base = cardinal_base(src_rsi)
        meta, states = match.rsi_states(rsi)
        out = {}
        for base, values in corner_bases(states).items():
            for v in values:
                src = {"corner_from_cardinal": {"rsi": src_rsi, "base": eris_base, "value": v}}
                if tint:
                    src["tint"] = tint
                out[f"{base}{v}"] = src
        if any(s["name"] == "full" for s in states):
            out["full"] = {"rsi": src_rsi, "state": "full", **({"tint": tint} if tint else {})}
        entries.append({"target": rsi, "standin": bool(tint), "states": out})

    for rsi in match.list_rsis("Structures/Furniture/Tables"):
        stem = Path(rsi).stem
        if stem not in TABLE_SOURCES:
            continue
        material, tint = TABLE_SOURCES[stem]
        meta, states = match.rsi_states(rsi)
        out = {}

        def layers(v):
            parts = [{"dmi": TABLES, "state": str(v)}]
            if material:
                part = {"dmi": TABLES, "state": f"{material}_{v}"}
                if tint:
                    part["tint"] = tint
                parts.append(part)
            return {"layers": parts}

        for base, values in corner_bases(states).items():
            for v in values:
                out[f"{base}{v}"] = layers(v)
        if any(s["name"] == "full" for s in states):
            out["full"] = {"merge_dirs": layers(0)}
        entries.append({"target": rsi, "standin": stem in ("brass", "card", "web", "xeno", "debug", "plasma"),
                        "states": out})

    (HERE / "maps" / "smoothing.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
    print(f"wrote {len(entries)} smoothing entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
