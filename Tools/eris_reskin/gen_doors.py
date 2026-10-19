#!/usr/bin/env python3
"""Generate maps/doors.json: airlocks, firelocks, blast doors, shutters, windoors, material doors.

Department airlocks reuse the CEV-Eris airlocks already baked into SS14's airlock
layout (Textures/Oxyd/erisported/airlocks). Other doors read icons/obj/doors/*.dmi
and rename Eris states (door_closed, pdoorc0, leftopening, ...) to SS14's.

Usage: python3 Tools/eris_reskin/gen_doors.py
"""

from __future__ import annotations

import json
from pathlib import Path

import match

HERE = Path(__file__).resolve().parent
BAKED = "Oxyd/erisported/airlocks/"
D = "icons/obj/doors/"

# SS14 airlock RSI stem -> baked Eris airlock (same state layout).
AIRLOCKS = {
    "airlock-base": "int", "airlock-maints": "maint", "airlock-external": "external", "cargo-base": "mining",
    "cargo-mining": "mining", "cargo-salvage": "mining", "centcomm-base": "centcomm", "command-base": "command",
    "engineering-atmos": "atmos", "engineering-base": "engineering", "medical-base": "medical",
    "medical-chem": "medical", "medical-viro": "medical", "science-base": "science", "security-base": "security",
    "service-base": "int", "service-botany": "int", "service-freezer": "freezer",
}
# SS14 door RSI stem -> (Eris DMI, {SS14 state: Eris state}); standin=True where Eris has no matching door.
AIRLOCK_STATES = {"closed": "door_closed", "open": "door_open", "opening": "door_opening",
                  "closing": "door_closing", "assembly": "door_closed", "panel_open": "panel_open",
                  "welded": "welded", "sparks": "door_spark"}
FIRELOCK_STATES = {**AIRLOCK_STATES, "locked": "door_locked", "deny": "door_deny", "welded": "welded",
                   "welded_open": "welded_open", "palert": "palert", "alert_cold": "alert_cold",
                   "alert_hot": "alert_hot"}
BLAST = {"closed": "pdoor1", "open": "pdoor0", "opening": "pdoorc0", "closing": "pdoorc1", "assembly": "pdoor_unplated"}
SHUTTER = {"closed": "shutter1", "open": "shutter0", "opening": "shutterc0", "closing": "shutterc1",
           "assembly": "shutter_unplated"}
WINDOOR = {"closed": "left", "open": "leftopen", "opening": "leftopening", "closing": "leftclosing",
           "assembly": "l_windoor_assembly0", "sparks": "leftspark"}


def material(prefix: str) -> dict:
    return {"closed": prefix, "open": f"{prefix}open", "opening": f"{prefix}opening", "closing": f"{prefix}closing"}


DOORS = {
    "Airlocks/Standard/airlock-vault": ("vault.dmi", AIRLOCK_STATES, False),
    "Airlocks/Standard/airlock-hatch": ("Doorhatchmaint2.dmi", AIRLOCK_STATES, False),
    "Airlocks/Standard/airlock-hatch-maints": ("Doorhatchmaint2.dmi", AIRLOCK_STATES, False),
    "Airlocks/Standard/airlock-hatch-syndicate": ("Doorhatchele.dmi", AIRLOCK_STATES, True),
    "Airlocks/Standard/airlock-blastdoor": ("rapid_pdoor.dmi", BLAST, False),
    "Airlocks/Standard/exo-blastdoor": ("rapid_pdoor.dmi", BLAST, True),
    "Airlocks/Standard/firelock-base": ("DoorHazard.dmi", FIRELOCK_STATES, False),
    "Airlocks/Glass/firelock-base": ("DoorHazard.dmi", FIRELOCK_STATES, False),
    "Airlocks/Standard/shuttle-base": ("Doorext.dmi", AIRLOCK_STATES, True),
    "Airlocks/Standard/shuttle-syndicate": ("hightechsecurity.dmi", AIRLOCK_STATES, True),
    "Airlocks/Standard/shuttle-xenoborg": ("Doorsand.dmi", AIRLOCK_STATES, True),
    "Airlocks/Standard/syndicate-base": ("hightechsecurity.dmi", AIRLOCK_STATES, True),
    "Airlocks/Standard/exo-base": ("Doorsand.dmi", AIRLOCK_STATES, True),
    "Airlocks/Standard/exo-xenoborg": ("Doorsand.dmi", AIRLOCK_STATES, True),
    "Airlocks/Glass/shuttle-base": ("Doorglass.dmi", AIRLOCK_STATES, True),
    "Airlocks/Glass/shuttle-syndicate": ("Doorsecglass.dmi", AIRLOCK_STATES, True),
    "Airlocks/Glass/syndicate-base": ("Doorsecglass.dmi", AIRLOCK_STATES, True),
    "Airlocks/Glass/exo-base": ("Doorglass.dmi", AIRLOCK_STATES, True),
    "Shutters/shutters": ("rapid_pdoor.dmi", SHUTTER, False),
    "Shutters/shutters-window": ("rapid_pdoor.dmi", {k: "l" + v for k, v in SHUTTER.items()}, False),
    "Shutters/shutters-radiation": ("rapid_pdoor.dmi", SHUTTER, True),
    "Windoors/windoor": ("windoor.dmi", WINDOOR, False),
    "Windoors/plasma": ("windoor.dmi", WINDOOR, True),
    "Windoors/uranium": ("windoor.dmi", WINDOOR, True),
    "Misc/edge_door_hazard": ("DoorHazard.dmi", FIRELOCK_STATES, False),
    "Misc/secret-door": ("material_doors.dmi", material("metal"), True),
    "MaterialDoors/wood-door": ("material_doors.dmi", material("wood"), False),
    "MaterialDoors/paper-door": ("material_doors.dmi", material("wood"), True),
    "MaterialDoors/card-door": ("material_doors.dmi", material("wood"), True),
    "MaterialDoors/web-door": ("material_doors.dmi", material("resin"), True),
    "MaterialDoors/gold-door": ("Doorgold.dmi", AIRLOCK_STATES, False),
    "MaterialDoors/silver-door": ("Doorsilver.dmi", AIRLOCK_STATES, False),
    "MaterialDoors/plasma-door": ("Doorplasma.dmi", AIRLOCK_STATES, False),
}
# Light overlays SS14 draws on top of a door; Eris door art has none of its own.
UNLIT_EFFECTS = {"airlock-effects": "airlock-effects", "airlock-effects-external": "airlock-effects-external",
                 "airlock-effects-freezer": "airlock-effects", "airlock-effects-vault": "airlock-effects",
                 "airlock-effects-shuttle": "airlock-effects-external", "airlock-effects-exo": "airlock-effects",
                 "airlock-effects-shuttle-xenoborg": "airlock-effects", "airlock-effects-exo-xenoborg": "airlock-effects"}


def main() -> int:
    entries = []
    for rsi in match.list_rsis("Structures/Doors"):
        meta, states = match.rsi_states(rsi)
        names = [s["name"] for s in states]
        rel = rsi.removeprefix("Structures/Doors/").removesuffix(".rsi")
        stem = Path(rel).name
        glass = "/Glass/" in rsi
        if rel.startswith("Airlocks/Effects/"):
            src = UNLIT_EFFECTS.get(stem)
            if src is None:
                continue
            src_rsi = f"{BAKED}effects/{'airlock-effects-glass' if src == 'airlock-effects' and glass else src}.rsi"
            _, ss = match.rsi_states(src_rsi)
            have = {s["name"] for s in ss}
            out = {n: {"rsi": src_rsi, "state": n} for n in names if n in have}
            entries.append({"target": rsi, "standin": stem != src, "states": out})
        elif stem in AIRLOCKS and rel.startswith("Airlocks/"):
            folder = "glass" if glass else "standard"
            src_rsi = f"{BAKED}{folder}/{AIRLOCKS[stem]}.rsi"
            if not (match.TEXTURES / src_rsi).exists():
                src_rsi = f"{BAKED}standard/{AIRLOCKS[stem]}.rsi"
            _, ss = match.rsi_states(src_rsi)
            have = {s["name"] for s in ss}
            out = {n: {"rsi": src_rsi, "state": n} for n in names if n in have}
            entries.append({"target": rsi, "standin": stem in ("service-base", "service-botany", "cargo-base"),
                            "states": out})
        elif rel in DOORS:
            dmi_name, mapping, standin = DOORS[rel]
            out = {n: [D + dmi_name, mapping[n]] for n in names if n in mapping}
            # SS14 light overlays have no Eris counterpart on these doors.
            for n in names:
                if n not in out and (n.endswith("_unlit") or n in ("secure_underlay", "panel_open", "panel_opening",
                                                                    "panel_closing")):
                    out[n] = {"blank": True}
            entries.append({"target": rsi, "standin": standin, "states": out})
    (HERE / "maps" / "doors.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
    print(f"wrote {len(entries)} door entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
