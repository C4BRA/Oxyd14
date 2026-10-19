#!/usr/bin/env python3
"""Generate maps/bodies.json: human body parts, hair and facial hair.

Body parts: the CEV-Eris human (icons/mob/human_races/r_human.dmi) is cut to
each SS14 part's silhouette and colour-matched to it, so SS14 skin tones and
markings still apply. Hair and facial hair: same-named Eris styles first,
otherwise the Eris style whose silhouette overlaps most.

Usage: python3 Tools/eris_reskin/gen_mobs.py --eris ../CEV-Eris
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import dmi
import match

HERE = Path(__file__).resolve().parent
BODY = "icons/mob/human_races/r_human.dmi"
HAIR = "icons/mob/human_races/species/human/hair.dmi"
FACIAL = "icons/mob/human_races/species/human/facial.dmi"
PARTS = "Mobs/Species/Human/parts.rsi"


def body_layers(sex: str) -> dict:
    parts = ["chest", "groin", "head", "l_arm", "r_arm", "l_leg", "r_leg"]
    return {"layers": [{"dmi": BODY, "state": f"{p}_{sex}"} for p in parts]}


def mask(im):
    return {(x, y) for y in range(im.height) for x in range(im.width) if im.getpixel((x, y))[3] > 32}


def iou(a: set, b: set) -> float:
    return len(a & b) / max(1, len(a | b))


def styles(eris: Path, rsi: str, dmi_path: str) -> dict:
    meta, states = match.rsi_states(rsi)
    eris_states = dmi.load(eris / dmi_path)
    eris_masks = {n: mask(s.images[0][0]) for n, s in eris_states.items() if n and s.dirs >= 4}
    norm = {n.replace("_", "").replace("-", "").lower(): n for n in eris_masks}
    out, used = {}, {}
    for s in states:
        name = s["name"]
        key = name.replace("_", "").replace("-", "").lower()
        recolour = lambda src: {"mask_transfer": {"src": src, "ref": {"rsi": rsi, "state": name, "base": True},  # noqa: E731
                                                  "keep_shape": True}}
        if key in norm:
            out[name] = recolour({"dmi": dmi_path, "state": norm[key]})
            continue
        im = match.rsi_first_frame(rsi, name)
        if im is None:
            continue
        m = mask(im)
        best = max(eris_masks, key=lambda n: iou(m, eris_masks[n]) - 0.02 * used.get(n, 0))
        used[best] = used.get(best, 0) + 1
        out[name] = recolour({"dmi": dmi_path, "state": best})
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--eris", type=Path, required=True)
    eris = ap.parse_args().eris.resolve()

    meta, states = match.rsi_states(PARTS)
    body = {}
    for s in states:
        n = s["name"]
        sex = "f" if n.endswith("_f") else "m"
        body[n] = {"mask_transfer": {"src": body_layers(sex), "ref": {"rsi": PARTS, "state": n, "base": True}}}
    entries = [
        {"target": PARTS, "standin": False, "states": body},
        {"target": "Mobs/Customization/human_hair.rsi", "standin": False,
         "states": styles(eris, "Mobs/Customization/human_hair.rsi", HAIR)},
        {"target": "Mobs/Customization/human_facial_hair.rsi", "standin": False,
         "states": styles(eris, "Mobs/Customization/human_facial_hair.rsi", FACIAL)},
    ]
    (HERE / "maps" / "bodies.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
    print(f"wrote body map: {sum(len(e['states']) for e in entries)} states")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
