#!/usr/bin/env python3
"""Generate maps/effects.json: hand-picked effect, fluid, decal and speech bubble sprites.

These sheets mix many unrelated sprites and are placed by mappers, so only
pairs that clearly mean the same thing are reskinned.

Usage: python3 Tools/eris_reskin/gen_effects.py
"""

from __future__ import annotations

import json
from pathlib import Path

import match

HERE = Path(__file__).resolve().parent
E = "icons/effects/"
FD = "icons/turf/flooring/decals.dmi"
DIRS = {"s": 0, "n": 1, "e": 2, "w": 3}


def e(dmi, state, **kw):
    return {"dmi": dmi, "state": state, **kw}


def speech() -> dict:
    meta, states = match.rsi_states("Effects/speech.rsi")
    out = {}
    for s in states:
        name = s["name"]
        kind, n = name.rstrip("0123456789"), name[len(name.rstrip("0123456789")):]
        robot = kind in ("robot", "machine", "syndibot", "xenoborg", "swarmer", "holo")
        idx = min(int(n or 0), 2)
        out[name] = e("icons/mob/talk.dmi", f"{'hR' if robot else 'h'}{idx}")
    return out


def markings() -> dict:
    out = {"delivery": e(FD, "delivery"), "loading_area": e(FD, "loadingarea"), "warn_full": e(FD, "warningfull"),
           "warningline": e(FD, "warning"), "warninglinecorner": e(FD, "warningcorner"),
           "warninglinecorner_flipped": e(FD, "warningcorner", flip="x"), "warn_corner": e(FD, "warningcorner"),
           "warn_corner_flipped": e(FD, "warningcorner", flip="x"), "caution": e(FD, "dangerfull"),
           "warn_box": e(FD, "warningcee"), "warn_end": e(FD, "warningcee")}
    for d, i in DIRS.items():
        out[f"warn_line_{d}"] = e(FD, "warning", dir=i)
        out[f"warn_end_{d}"] = e(FD, "warningcee", dir=i)
    for c, i in (("se", 0), ("nw", 1), ("ne", 2), ("sw", 3)):
        out[f"warn_corner_{c}"] = e(FD, "warningcorner", dir=i)
        out[f"warn_corner_small_{c}"] = e(FD, "warningcorner", dir=i)
    return out


def main() -> int:
    maps = {
        "Effects/sparks.rsi": {"sparks": e(E + "effects.dmi", "sparks")},
        "Effects/fire.rsi": {"1": e(E + "fire.dmi", "1"), "2": e(E + "fire.dmi", "2"), "3": e(E + "fire.dmi", "3"),
                             "fire": e(E + "fire.dmi", "fire")},
        "Effects/fire_greyscale.rsi": {"1": e(E + "fire.dmi", "1"), "2": e(E + "fire.dmi", "2"), "3": e(E + "fire.dmi", "3")},
        "Effects/foam.rsi": {"foam": e(E + "effects.dmi", "foam"), "metal_foam": e(E + "effects.dmi", "metalfoam"),
                             "iron_foam": e(E + "effects.dmi", "ironfoam")},
        "Effects/emp.rsi": {"emp_disable": e(E + "effects.dmi", "empdisable"), "emp_pulse": e(E + "effects.dmi", "emppulse")},
        "Effects/extinguisherSpray.rsi": {"extinguish": e(E + "effects.dmi", "extinguish")},
        "Effects/electricity.rsi": {"electrified": e(E + "effects.dmi", "electricity")},
        "Effects/chempuff.rsi": {"chempuff": e("icons/obj/chempuff.dmi", "")},
        "Effects/explosion.rsi": {"explosion": e(E + "96x96.dmi", "explosion"), "explosionfast": e(E + "96x96.dmi", "explosion")},
        "Effects/speech.rsi": speech(),
        "Fluids/vomit.rsi": {f"vomit-{i}": e(E + "blood.dmi", f"vomit_{i + 1}") for i in range(4)},
        "Fluids/vomit_toxin.rsi": {f"vomit_toxin-{i}": e(E + "blood.dmi", f"vomittox_{i + 1}") for i in range(4)},
        "Fluids/gibblet.rsi": {f"gibblet-{i}": e(E + "blood.dmi", s) for i, s in enumerate(("gib1", "gib2", "gib3", "gib5", "gib6"))},
        "Fluids/splatter.rsi": {f"splatter-{i}": e(E + "blood.dmi", f"splatter{i + 1}") for i in range(6)},
        "Fluids/writing.rsi": {f"writing-{i}": e(E + "blood.dmi", f"writing{i + 1}") for i in range(5)},
        "Decals/markings.rsi": markings(),
        "Decals/burnt.rsi": {"burnt1": e(E + "effects.dmi", "scorch"), "burnt2": e(E + "effects.dmi", "light_scorch"),
                             "burnt3": e(E + "effects.dmi", "scorch"), "burnt4": e(E + "effects.dmi", "light_scorch")},
        "Decals/dirty.rsi": {"dirt": e(E + "effects.dmi", "dirt"), "dirtlight": e(E + "effects.dmi", "dirt"),
                             "dirtmedium": e(E + "effects.dmi", "dirt"), "dirtheavy": e(E + "effects.dmi", "dirt"),
                             "rust": e(FD, "rust"), "damaged": e("icons/turf/flooring/damage.dmi", "broken0"),
                             "remains": e(E + "blood.dmi", "remains")},
        "Decals/derelictsign.rsi": {f"derelict{i}": e(FD, f"derelict{i}") for i in range(1, 17)},
    }
    entries = []
    for rsi, states in maps.items():
        if not (match.TEXTURES / rsi / "meta.json").exists():
            continue
        _, have = match.rsi_states(rsi)
        names = {s["name"] for s in have}
        entries.append({"target": rsi, "standin": False, "states": {k: v for k, v in states.items() if k in names}})
    (HERE / "maps" / "effects.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
    print(f"wrote {len(entries)} effect entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
