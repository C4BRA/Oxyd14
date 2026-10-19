#!/usr/bin/env python3
"""Generate maps/clothing.json: every SS14 clothing RSI gets the closest CEV-Eris garment.

Candidates are Eris clothing of the same equipment slot. They are scored on
name/role tokens (with SS14 -> Eris job synonyms) and on colour, and a reuse
penalty spreads picks across the Eris wardrobe. Picks whose names do not
match are flagged "standin" so they can be reviewed.

Usage: python3 Tools/eris_reskin/gen_clothing.py --eris ../CEV-Eris
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

import catalog
import match

HERE = Path(__file__).resolve().parent

# SS14 equipped-<SLOT> -> (Eris worn slot, Eris type prefixes allowed as candidates)
SLOTS = {
    "HELMET": ("head", ["/obj/item/clothing/head"]),
    "INNERCLOTHING": ("uniform", ["/obj/item/clothing/under"]),
    "OUTERCLOTHING": ("suit", ["/obj/item/clothing/suit"]),
    "FEET": ("shoes", ["/obj/item/clothing/shoes"]),
    "HAND": ("gloves", ["/obj/item/clothing/gloves"]),
    "MASK": ("mask", ["/obj/item/clothing/mask"]),
    "EYES": ("glasses", ["/obj/item/clothing/glasses"]),
    "EARS": ("ears", ["/obj/item/device/radio/headset", "/obj/item/clothing/ears"]),
    "BELT": ("belt", ["/obj/item/storage/belt", "/obj/item/clothing/belt"]),
    "BACKPACK": ("back", ["/obj/item/storage/backpack"]),
    "NECK": ("accessory", ["/obj/item/clothing/accessory"]),
    "SUITSTORAGE": ("s_store", ["/obj/item"]),
}
# Folder -> slot, for RSIs that only have an icon (no worn state).
FOLDER_SLOT = {"Head": "HELMET", "Uniforms": "INNERCLOTHING", "OuterClothing": "OUTERCLOTHING", "Shoes": "FEET",
               "Hands": "HAND", "Mask": "MASK", "Eyes": "EYES", "Ears": "EARS", "Belt": "BELT", "Back": "BACKPACK",
               "Neck": "NECK", "Under": "INNERCLOTHING", "Multiple": "EARS"}
# Eris has inhand sprites for few garments; these generic ones are tinted to the garment colour.
GENERIC_INHAND = {"head": "tdhelm", "uniform": "grey_hoodie", "suit": "leather_jacket", "shoes": "w_shoes",
                  "gloves": "lgloves", "mask": "gas_mask", "glasses": "sunglasses", "ears": "headset",
                  "belt": "briefcase", "back": "trashbag", "accessory": "bedsheet", "s_store": "briefcase"}
# State suffixes that are species/body variants; Eris only draws humans, so those keep SS14 art.
SPECIES = ("vox", "reptilian", "dog", "hamster", "monkey", "kobold", "arachnid", "moth", "diona", "slime",
           "dwarf", "vulpkanin", "felinid", "skeleton", "rodentia", "harpy", "resomi", "unathi", "tajaran")
NAME_THRESHOLD = 0.34


def avg_colour(im) -> str:
    px = [p for p in im.convert("RGBA").getdata() if p[3] > 128]
    if not px:
        return "#ffffff"
    r, g, b = (sum(p[i] for p in px) // len(px) for i in range(3))
    # Normalise brightness so the tint keeps the generic sprite readable.
    m = max(r, g, b, 1)
    return "#%02x%02x%02x" % tuple(min(255, int(c * 230 / m)) for c in (r, g, b))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--eris", type=Path, required=True)
    args = ap.parse_args()
    eris = args.eris.resolve()
    items = catalog.load(eris, HERE / "cache" / "eris_items.json")
    names = match.entity_names_by_rsi()

    pools: dict[str, list[dict]] = {}
    for key, (slot, prefixes) in SLOTS.items():
        pool = []
        for it in items:
            if slot not in it["worn"] or not any(it["type"].startswith(p) for p in prefixes):
                continue
            im = match.eris_first_frame(eris, it["icon"])
            pool.append({**it, "_tok": match.tokens(it["name"], it["icon"][1], it["type"].replace("/", " ")),
                         "_sig": match.signature(im) if im else None})
        pools[key] = pool
    # Cloaks, mantles and ponchos live under suits in Eris.
    pools["NECK"] += [p for p in pools["OUTERCLOTHING"] + pools["MASK"] if p["_tok"] & {"cloak", "cape", "poncho", "mantle", "scarf"}]

    targets = []
    for rsi in match.list_rsis("Clothing"):
        meta, states = match.rsi_states(rsi)
        snames = [s["name"] for s in states]
        equipped = [n.split("-", 1)[1].split("-")[0] for n in snames if n.startswith("equipped-")]
        primary = next((e for e in equipped if e in SLOTS), None) or FOLDER_SLOT.get(rsi.split("/")[1])
        if primary is None or not pools.get(primary):
            continue
        icon_state = "icon" if "icon" in snames else snames[0]
        ss_im = match.rsi_first_frame(rsi, icon_state)
        targets.append({"rsi": rsi, "snames": snames, "primary": primary, "icon_state": icon_state,
                        "tok": match.expand(match.tokens(rsi.replace("/", " "), *names.get(rsi, []))),
                        "sig": match.signature(ss_im) if ss_im else None})
    idf = match.idf_weights([c["_tok"] for pool in pools.values() for c in pool])
    overrides = json.loads((HERE / "clothing_overrides.json").read_text()) if (HERE / "clothing_overrides.json").exists() else {}
    by_type = {it["type"]: it for it in items}

    def score(t, c):
        ns = match.name_score(t["tok"], c["_tok"], idf)
        return 0.7 * ns + 0.3 * match.colour_score(t["sig"], c["_sig"]), ns

    picks = match.assign(targets, lambda t: pools[t["primary"]], score)
    entries, report = [], []
    for i, t in enumerate(targets):
        rsi, snames, primary, icon_state = t["rsi"], t["snames"], t["primary"], t["icon_state"]
        if rsi in overrides and overrides[rsi] in by_type:
            cand, ns = by_type[overrides[rsi]], 1.0
        elif i in picks:
            cand, _, ns = picks[i]
        else:
            continue
        cand = {**cand, "_ns": ns}
        eslot = SLOTS[primary][0]
        tint = avg_colour(match.eris_first_frame(eris, cand["icon"]))
        state_map = {}
        for n in snames:
            parts = n.split("-")
            if any(p in SPECIES for p in parts):
                continue
            if n.startswith("equipped-"):
                slot = SLOTS.get(parts[1], (eslot, None))[0]
                worn = cand["worn"].get(slot) or cand["worn"].get(eslot) or next(iter(cand["worn"].values()), None)
                if not worn:
                    continue
                variant = parts[2:]  # e.g. open, toggled, on
                src = list(worn)
                if variant:
                    for suf in ("_" + "_".join(variant), "-" + "-".join(variant), "_open", "_up", "_on", "1"):
                        if catalog_has(eris, worn[0], worn[1] + suf):
                            src = [worn[0], worn[1] + suf]
                            break
                state_map[n] = src
            elif n in ("inhand-left", "inhand-right"):
                hand = "l_hand" if n.endswith("left") else "r_hand"
                if hand in cand["inhand"]:
                    state_map[n] = cand["inhand"][hand]
                else:
                    dmi_path = "icons/mob/items/lefthand.dmi" if hand == "l_hand" else "icons/mob/items/righthand.dmi"
                    state_map[n] = {"dmi": dmi_path, "state": GENERIC_INHAND[eslot], "tint": tint}
            elif n == icon_state or n.startswith("icon"):
                src = list(cand["icon"])
                suffix = n[len("icon"):].lstrip("-_")
                if suffix:
                    for suf in ("_" + suffix, "-" + suffix, suffix):
                        if catalog_has(eris, src[0], src[1] + suf):
                            src = [src[0], src[1] + suf]
                            break
                state_map[n] = src
        if not state_map:
            continue
        standin = cand["_ns"] < NAME_THRESHOLD
        entries.append({"target": rsi, "eris_type": cand["type"], "standin": standin, "states": state_map})
        report.append(f"{'~' if standin else '='} {rsi} <- {cand['type']} ({cand['name']}) n={cand['_ns']:.2f}")

    (HERE / "maps").mkdir(exist_ok=True)
    (HERE / "maps" / "clothing.json").write_text(json.dumps(entries, indent=1) + "\n", encoding="utf-8")
    (HERE / "cache" / "clothing_report.txt").write_text("\n".join(report) + "\n", encoding="utf-8")
    print(f"wrote {len(entries)} clothing entries ({sum(e['standin'] for e in entries)} stand-ins)")
    return 0


def catalog_has(eris: Path, dmi_path: str, state: str) -> bool:
    import dmi

    p = eris / dmi_path
    return p.exists() and state in dmi.load(p)


if __name__ == "__main__":
    raise SystemExit(main())
