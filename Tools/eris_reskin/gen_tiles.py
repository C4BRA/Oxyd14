#!/usr/bin/env python3
"""Generate maps/tiles.json: floor tile textures under Textures/Tiles.

Every tile PNG (a strip of 32x32 variants) is filled with the closest CEV-Eris
floor: the floor bases already extracted to Textures/Oxyd/erisported, or states
from icons/turf/flooring. Edge overlays for planet tiles are left alone.

Usage: python3 Tools/eris_reskin/gen_tiles.py --eris ../CEV-Eris
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

import dmi
import match

HERE = Path(__file__).resolve().parent
ERISPORTED = "Oxyd/erisported"
F = "icons/turf/flooring/"

# SS14 tile name word -> Eris floor words, tried in order.
WORDS = {
    "white": ["white"], "gray": ["steel"], "steel": ["steel"], "dark": ["dark"], "mono": ["monofloor"],
    "mini": ["tiles"], "pavement": ["panels"], "herringbone": ["techfloor_grid"], "slats": ["techfloor"],
    "plastic": ["gray_platform"], "maint": ["techmaint"], "damaged": ["tiles"], "diagonal": ["tiles"],
    "offset": ["tiles"], "checker": ["cafe"], "kitchen": ["cafe"], "cafe": ["cafe"], "bar": ["bar_flat"],
    "wood": ["wood"], "carpet": ["carpet"], "grass": ["grass"], "dirt": ["dirt"], "asteroid": ["asteroidfloor"],
    "plating": ["plating"], "lattice": ["lattices"], "hull": ["hullcenter"], "reinforced": ["reinforced"],
    "freezer": ["freezerfloor"], "concrete": ["concrete"], "glass": ["reinforced"], "exo": ["techmaint"],
    "circuit": ["bcircuit"], "green": ["gcircuit", "grass"], "blue": ["blucarpet", "bluecorner"],
    "arcade": ["purcarpet"], "red": ["carpet"], "orange": ["oracarpet", "orangecorner"],
    "purple": ["purcarpet"], "cyan": ["turcarpet", "cyancorner"], "black": ["bcarpet"], "showroom": ["showroomfloor"],
    "hydro": ["hydrofloor"], "cult": ["cult"], "gold": ["golden"], "elevator": ["elevatorshaft"],
    "shuttle": ["techfloor"], "mining": ["techmaint_cargo"], "cargo": ["cargo"], "danger": ["danger"],
    "silver": ["steel_monofloor"], "bronze": ["golden"], "brass": ["golden"], "web": ["dirt"],
    "snow": ["snow"], "desert": ["asteroidfloor"], "basalt": ["asteroidfloor"], "sand": ["asteroidfloor"],
    "rock": ["asteroidfloor"], "ice": ["reinforced"], "lino": ["tiles_white"], "techmaint": ["techmaint"],
    "flesh": ["cult"], "mowed": ["grass"], "jungle": ["grass"], "astro": ["grass"],
}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--eris", type=Path, required=True)
    eris = ap.parse_args().eris.resolve()

    cands: dict[str, dict] = {}
    for p in sorted((match.TEXTURES / ERISPORTED).glob("*_base.png")):
        stem = p.name.removesuffix("_base.png")
        cands[stem] = {"png": f"{ERISPORTED}/{p.name}"}
    for name, dmi_file in (("freezerfloor", "misc.dmi"), ("concrete", "misc.dmi"), ("showroomfloor", "misc.dmi"),
                           ("hydrofloor", "misc.dmi"), ("elevatorshaft", "misc.dmi"), ("hexacrete", "misc.dmi")):
        cands[name] = {"dmi": F + dmi_file, "state": name}
    snow = dmi.load(eris / "icons/turf/snow.dmi") if (eris / "icons/turf/snow.dmi").exists() else {}
    if snow:
        cands["snow"] = {"dmi": "icons/turf/snow.dmi", "state": next(iter(snow))}
    grass = [{"dmi": F + "grass.dmi", "state": f"grass{i}"} for i in range(4)]
    plating = [{"dmi": F + "plating.dmi", "state": "plating"}]

    entries, report = [], []
    for p in sorted((match.TEXTURES / "Tiles").rglob("*.png")):
        rel = str(p.relative_to(match.TEXTURES))
        if ".rsi/" in rel or re.search(r"(edge|corner|[-_][NSEW]{1,2}$)", p.stem):
            continue
        words = re.split(r"[-_ /]+", rel.lower().removeprefix("tiles/").removesuffix(".png"))
        pick = None
        # Colour + pattern first ("white-mono" -> white_monofloor), then single words.
        colour = next((WORDS[w][0] for w in words if w in ("white", "gray", "steel", "dark")), None)
        pattern = next((WORDS[w][0] for w in words if w in ("mono", "mini", "pavement", "herringbone", "slats",
                                                           "plastic", "cargo", "danger", "diagonal", "offset",
                                                           "damaged")), None)
        if colour and pattern:
            key = f"tiles_{colour}" if pattern == "tiles" else f"{colour}_{pattern}"
            pick = key if key in cands else None
        if pick is None and colour and f"tiles_{colour}" in cands:
            pick = f"tiles_{colour}"
        if pick is None:
            for w in words:
                for cand in WORDS.get(w, []):
                    hit = next((k for k in cands if k == cand), None) or next((k for k in cands if cand in k), None)
                    if hit:
                        pick = hit
                        break
                if pick:
                    break
        standin = pick is None
        if "grass" in words or pick == "grass":
            srcs = grass
        elif pick is None:
            srcs = plating if "plating" in words else [cands["tiles_steel"]]
        else:
            srcs = [cands[pick]]
        entries.append({"file": rel, "src": srcs, "strip": True, "standin": standin})
        report.append(f"{'~' if standin else '='} {rel} <- {pick}")
    (HERE / "maps" / "tiles.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
    (HERE / "cache" / "tiles_report.txt").write_text("\n".join(report) + "\n")
    print(f"wrote {len(entries)} tile entries ({sum(e['standin'] for e in entries)} stand-ins)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
