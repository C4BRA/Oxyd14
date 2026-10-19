#!/usr/bin/env python3
"""Generate maps/<group>.json for non-clothing sprites (objects, structures, mobs, effects).

Each SS14 RSI is matched to a CEV-Eris candidate from a pool chosen by the
RSI's folder. Candidates are Eris item types (with their worn and in-hand
sprites), loose states from specific DMIs (cocktail glasses, plants, ...) and,
for guns, the Eris guns already converted to the SS14 gun layout under
Textures/Oxyd/erisported.

RSIs whose entities show different states (one RSI holding many different
objects) are split so each of those states gets its own match.

States are mapped like this:
  the state the entity shows       -> Eris icon
  inhand-*, wielded-*, equipped-*  -> Eris in-hand / worn sprites when Eris has them
  other full-size states           -> the matching Eris variant (open, on, empty, ...) or the Eris icon
  small overlays (fills, lights)   -> kept, or blanked for guns and structures where Eris art
                                      already includes them

Usage: python3 Tools/eris_reskin/gen_objects.py --eris ../CEV-Eris --group objects
"""

from __future__ import annotations

import argparse
import json
import re
from collections import defaultdict
from pathlib import Path

import catalog
import dmi
import match

HERE = Path(__file__).resolve().parent
SLOT = {"BELT": "belt", "BACKPACK": "back", "SUITSTORAGE": "s_store", "HELMET": "head", "MASK": "mask",
        "EYES": "glasses", "EARS": "ears", "HAND": "gloves", "FEET": "shoes", "NECK": "accessory",
        "OUTERCLOTHING": "suit", "INNERCLOTHING": "uniform"}

I = "/obj/item/"
# (folder prefix, Eris type prefixes, loose-state DMIs, overlay policy). First match wins.
GROUPS: dict[str, list[tuple[str, list[str], list[str], str]]] = {
    "objects": [
        ("Objects/Weapons/Guns/Ammunition", [I + "ammo_magazine", I + "ammo_casing", I + "storage/box"],
         ["icons/obj/ammo.dmi", "icons/obj/ammo_speed.dmi"], "keep"),
        ("Objects/Weapons/Guns/Projectiles", [I + "projectile"], ["icons/obj/projectiles.dmi"], "keep"),
        ("Objects/Weapons/Guns", ["GUNS"], [], "blank"),
        ("Objects/Weapons/Melee", [I + "tool", I + "melee", I + "material", I + "shield", I + "twohanded"], [], "keep"),
        ("Objects/Weapons/Grenades", [I + "grenade", I + "explosive", I + "plastique"], [], "keep"),
        ("Objects/Weapons/Bombs", [I + "grenade", I + "plastique", I + "device/assembly"], [], "keep"),
        ("Objects/Weapons", [I + "tool", I + "melee", I + "grenade", I], [], "keep"),
        ("Objects/Consumable/Drinks", [I + "reagent_containers/food/drinks", I + "reagent_containers/glass"],
         ["icons/obj/drinks.dmi"], "keep"),
        ("Objects/Consumable/Food", [I + "reagent_containers/food", I + "trash"], ["icons/obj/food.dmi", "icons/obj/food_ingredients.dmi"], "keep"),
        ("Objects/Consumable/Smokeables", [I + "clothing/mask/smokable", I + "storage/fancy", I + "flame"], ["icons/obj/cigarettes.dmi"], "keep"),
        ("Objects/Consumable/Trash", [I + "trash"], ["icons/obj/trash.dmi"], "keep"),
        ("Objects/Consumable", [I + "reagent_containers", I + "trash"], [], "keep"),
        ("Objects/Specific/Hydroponics", [I + "seeds", I + "reagent_containers/food/snacks/grown", I + "tool/minihoe",
                                          I + "plantspray", I + "weedkiller"], ["icons/obj/hydroponics_products.dmi", "icons/obj/seeds.dmi"], "keep"),
        ("Objects/Specific/Medical", [I + "stack/medical", I + "reagent_containers", I + "device/scanner", I + "storage/firstaid",
                                      I + "bodybag", I + "tool/scalpel", I + "tool/surgicaldrill", I + "implanter"], ["icons/obj/surgery.dmi"], "keep"),
        ("Objects/Specific/Chemistry", [I + "reagent_containers"], ["icons/obj/chemical.dmi"], "keep"),
        ("Objects/Specific/Janitorial", [I + "mop", I + "soap", I + "reagent_containers", I + "storage/bag", I + "lightreplacer"],
         ["icons/obj/janitor.dmi"], "keep"),
        ("Objects/Specific", [I], [], "keep"),
        ("Objects/Fun", [I + "toy", I + "dice", I + "deck", I + "card", I + "bikehorn", I + "storage/pill_bottle/dice",
                         I + "instrument", I + "device/synthesized_instrument"], ["icons/obj/toy.dmi", "icons/obj/musician.dmi"], "keep"),
        ("Objects/Tools", [I + "tool", I + "device", I + "storage/toolbox", I + "flame", I + "lighter", I + "rcd"], ["icons/obj/tools.dmi"], "keep"),
        ("Objects/Devices", [I + "device", I + "electronics", I + "card", I + "modular_computer", I + "computer_hardware",
                             I + "implanter", I + "radio"], ["icons/obj/device.dmi"], "keep"),
        ("Objects/Materials", [I + "stack", I + "ore", I + "material", I + "shard"], ["icons/obj/stack"], "keep"),
        ("Objects/Tanks", [I + "tank"], ["icons/obj/tank.dmi"], "keep"),
        ("Objects/Storage", [I + "storage"], ["icons/obj/storage.dmi"], "keep"),
        ("Objects/Power", [I + "cell", I + "stock_parts"], ["icons/obj/power_cells.dmi"], "keep"),
        ("Objects/Tiles", [I + "stack/tile"], [], "keep"),
        ("Objects/Vehicles", ["/obj/vehicle", "/obj/structure/bed/chair/wheelchair"], ["icons/obj/vehicles.dmi"], "blank"),
        ("Objects/Decoration", [I, "/obj/structure/flora"], ["icons/obj/plants.dmi"], "keep"),
        ("Objects/Misc", [I], ["icons/obj/bureaucracy.dmi", "icons/obj/items.dmi"], "keep"),
        ("Objects", [I], [], "keep"),
    ],
}

VARIANT_SYNONYMS = {
    "open": ["open", "opened", "_open", "-open"], "closed": ["closed", ""], "on": ["on", "1", "_on", "-on", "active", "lit"],
    "off": ["off", "0", "", "_off"], "empty": ["empty", "0", "_empty", "-empty"], "full": ["full", "", "1"],
    "lit": ["lit", "on", "_on", "1"], "unlit": ["", "off", "0"], "wielded": ["_wielded", "-wielded", "wielded"],
    "broken": ["broken", "-broken", "_broken"], "folded": ["folded", "_folded"], "active": ["active", "on", "1"],
}


def variant_state(eris: Path, src: list[str], target: str, primary: str) -> list[str] | None:
    """Find an Eris state in the same DMI that is the target's variant of src."""
    states = dmi.load(eris / src[0])
    extra = [t for t in re.split(r"[-_ ]+", target.lower()) if t and t not in re.split(r"[-_ ]+", primary.lower())]
    for tok in extra:
        for suf in VARIANT_SYNONYMS.get(tok, [tok]):
            for cand in (src[1] + suf, f"{src[1]}_{suf}", f"{src[1]}-{suf}", f"{suf}_{src[1]}", suf):
                if cand and cand != src[1] and cand in states:
                    return [src[0], cand]
    return None


def load_candidates(eris: Path, items: list[dict], prefixes: list[str], loose: list[str], sig_cache: dict) -> list[dict]:
    out, seen = [], set()

    def sig(src):
        key = f"{src[0]}:{src[1]}"
        if key not in sig_cache:
            im = match.eris_first_frame(eris, src)
            sig_cache[key] = match.signature(im) if im else None
        return sig_cache[key]

    for it in items:
        if not any(it["type"].startswith(p) for p in prefixes):
            continue
        key = f"{it['icon'][0]}:{it['icon'][1]}"
        if key in seen:
            continue
        seen.add(key)
        out.append({"key": key, "name": it["name"], "icon": it["icon"], "inhand": it["inhand"], "worn": it["worn"],
                    "tok": match.tokens(it["name"], it["icon"][1], it["type"].rsplit("/", 2)[-2:][0], it["type"].rsplit("/", 1)[1]),
                    "sig": sig(it["icon"]), "type": it["type"]})
    for pattern in loose:
        paths = sorted((eris / pattern).glob("*.dmi")) if (eris / pattern).is_dir() else [eris / pattern]
        for p in paths:
            if not p.exists():
                continue
            rel = str(p.relative_to(eris))
            for st, data in dmi.load(p).items():
                key = f"{rel}:{st}"
                if key in seen or not st:
                    continue
                # Skip overlay pieces (fills, masks, lights): too little art to stand in for an object.
                if match.coverage(data.images[0][0]) < 40 or re.search(r"(mask|overlay|fill|_lights?$|-lights?$)", st):
                    continue
                seen.add(key)
                out.append({"key": key, "name": st, "icon": [rel, st], "inhand": {}, "worn": {},
                            "tok": match.tokens(st.replace("_", " ")), "sig": sig([rel, st]), "type": rel})
    return out


def gun_candidates(eris: Path, items: list[dict]) -> list[dict]:
    """Eris guns already converted to SS14's gun RSI layout (Textures/Oxyd/erisported)."""
    names = match.entity_names_by_rsi()
    by_dmi = defaultdict(list)
    for it in items:
        if it["type"].startswith("/obj/item/gun"):
            by_dmi[Path(it["icon"][0]).stem].append(it)
    out = []
    for rsi in match.list_rsis("Oxyd/erisported"):
        meta, states = match.rsi_states(rsi)
        sn = {s["name"] for s in states}
        if "base" not in sn or "inhand-left" not in sn:
            continue
        stem = Path(rsi).stem
        eris_types = by_dmi.get(stem, [])
        toks = match.tokens(stem, *names.get(rsi, []), *(t["name"] for t in eris_types), *(t["type"] for t in eris_types))
        im = match.rsi_first_frame(rsi, "base")
        out.append({"key": rsi, "name": stem, "rsi": rsi, "states": sn, "tok": toks,
                    "sig": match.signature(im) if im else None, "type": " ".join(t["type"] for t in eris_types)})
    return out


GUN_CLASS = {"Pistols": ["pistol", "revolver"], "Revolvers": ["revolver"], "Rifles": ["automatic", "rifle", "boltgun"],
             "SMGs": ["smg", "automatic"], "Shotguns": ["shotgun"], "Snipers": ["sniper", "boltgun", "heavysniper"],
             "LMGs": ["lmg", "automatic"], "HMGs": ["lmg", "automatic"], "Launchers": ["launcher", "grenade", "rpg"],
             "Battery": ["energy", "laser"], "Bow": ["bow", "crossbow"]}


def frame(rsi, state):
    return match.rsi_first_frame(rsi, state)


def map_states(eris, rsi, snames, primary, related, cand, policy):
    """Return {state: source} for the states of one target (primary + its related states)."""
    out = {}
    p_cov = max(1, match.coverage(frame(rsi, primary)))
    gun = "rsi" in cand
    for n in [primary] + related:
        if gun:
            if n in cand["states"] and n != "base":
                out[n] = {"rsi": cand["rsi"], "state": n}
            elif n in (primary, "icon", "base"):
                out[n] = {"rsi": cand["rsi"], "state": "base"}
            elif n.startswith(("inhand-", "wielded-")):
                alt = n.replace("wielded-", "") if n.replace("wielded-", "") in cand["states"] else None
                if alt:
                    out[n] = {"rsi": cand["rsi"], "state": alt}
            elif n.startswith("equipped-"):
                alt = next((s for s in ("equipped-SUITSTORAGE", "equipped-BACKPACK") if s in cand["states"]), None)
                if alt:
                    out[n] = {"rsi": cand["rsi"], "state": alt}
            else:
                out[n] = {"blank": True}
            continue
        if n == primary:
            out[n] = list(cand["icon"])
        elif n.startswith(("inhand-", "wielded-inhand-")):
            hand = "l_hand" if "left" in n else "r_hand"
            if hand in cand["inhand"]:
                src = cand["inhand"][hand]
                if n.startswith("wielded"):
                    wv = variant_state(eris, src, "wielded", "")
                    src = wv or src
                out[n] = list(src)
        elif n.startswith("equipped-"):
            slot = SLOT.get(n.split("-")[1])
            if slot and slot in cand["worn"]:
                out[n] = list(cand["worn"][slot])
        else:
            cov = match.coverage(frame(rsi, n))
            if cov >= 0.6 * p_cov:
                out[n] = variant_state(eris, cand["icon"], n, primary) or list(cand["icon"])
            elif policy == "blank":
                out[n] = {"blank": True}
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--eris", type=Path, required=True)
    ap.add_argument("--group", required=True, choices=sorted(GROUPS))
    args = ap.parse_args()
    eris = args.eris.resolve()
    items = catalog.load(eris, HERE / "cache" / "eris_items.json")
    names = match.entity_names_by_rsi()
    sig_cache_path = HERE / "cache" / "signatures.json"
    sig_cache = json.loads(sig_cache_path.read_text()) if sig_cache_path.exists() else {}
    overrides_path = HERE / f"{args.group}_overrides.json"
    overrides = json.loads(overrides_path.read_text()) if overrides_path.exists() else {}
    done = {e["target"] for mp in (HERE / "maps").glob("*.json") if mp.stem != args.group
            for e in json.loads(mp.read_text()) if "target" in e}

    pools: dict[str, list[dict]] = {}
    targets = []
    config = GROUPS[args.group]
    roots = sorted({c[0].split("/")[0] for c in config})
    for root in roots:
        for rsi in match.list_rsis(root):
            if rsi in done:
                continue
            rule = next((c for c in config if rsi.startswith(c[0])), None)
            if rule is None:
                continue
            prefix, type_prefixes, loose, policy = rule
            if prefix not in pools:
                pools[prefix] = gun_candidates(eris, items) if type_prefixes == ["GUNS"] else \
                    load_candidates(eris, items, type_prefixes, loose, sig_cache)
            meta, states = match.rsi_states(rsi)
            snames = [s["name"] for s in states]
            used = [s for s in match.USED_STATES.get(rsi, {}) if s in snames]
            if len(used) > 1:
                groups = {u: [] for u in used}
                for n in snames:
                    if n in groups:
                        continue
                    owner = max((u for u in used if n.startswith(u)), key=len, default=None)
                    if owner:
                        groups[owner].append(n)
                    elif n.startswith(("inhand-", "wielded-", "equipped-")):
                        groups[used[0]].append(n)
            else:
                primary = match.primary_state(rsi, snames)
                groups = {primary: [n for n in snames if n != primary]}
            gun_class = GUN_CLASS.get(rsi.split("/")[3] if rsi.startswith("Objects/Weapons/Guns/") else "", [])
            for primary, related in groups.items():
                ent_names = match.USED_NAMES.get(rsi, {}).get(primary) if len(groups) > 1 else names.get(rsi, [])
                im = frame(rsi, primary)
                tok = match.tokens(Path(rsi).stem, rsi.split("/")[-2], primary if len(groups) > 1 else "", *(ent_names or []))
                targets.append({"rsi": rsi, "snames": snames, "primary": primary, "related": related, "prefix": prefix,
                                "policy": policy, "tok": match.expand(tok), "sig": match.signature(im) if im else None,
                                "gun_class": gun_class, "split": len(groups) > 1})
    sig_cache_path.write_text(json.dumps(sig_cache))

    idf = match.idf_weights([c["tok"] for pool in pools.values() for c in pool])

    def score(t, c):
        ns = match.name_score(t["tok"], c["tok"], idf)
        bonus = 0.15 if t["gun_class"] and any(k in c["type"] for k in t["gun_class"]) else 0.0
        return 0.65 * ns + 0.35 * match.colour_score(t["sig"], c["sig"]) + bonus, ns

    by_key = {c["key"]: c for pool in pools.values() for c in pool}
    picks = match.assign(targets, lambda t: pools[t["prefix"]], score, reuse_penalty=0.04)
    merged: dict[str, dict] = {}
    report = []
    for i, t in enumerate(targets):
        okey = f"{t['rsi']}:{t['primary']}" if t["split"] else t["rsi"]
        if okey in overrides and overrides[okey] in by_key:
            cand, ns = by_key[overrides[okey]], 1.0
        elif i in picks:
            cand, _, ns = picks[i]
        else:
            continue
        states = map_states(eris, t["rsi"], t["snames"], t["primary"], t["related"], cand, t["policy"])
        if not states:
            continue
        e = merged.setdefault(t["rsi"], {"target": t["rsi"], "standin": False, "states": {}, "eris": []})
        e["states"].update(states)
        e["eris"].append(cand["key"])
        standin = ns < 0.34
        e["standin"] = e["standin"] or standin
        report.append(f"{'~' if standin else '='} {okey} <- {cand['key']} ({cand['name']}) n={ns:.2f}")

    (HERE / "maps" / f"{args.group}.json").write_text(json.dumps(list(merged.values()), indent=1) + "\n", encoding="utf-8")
    (HERE / "cache" / f"{args.group}_report.txt").write_text("\n".join(sorted(report)) + "\n", encoding="utf-8")
    print(f"wrote {len(merged)} {args.group} entries from {len(targets)} targets "
          f"({sum(1 for r in report if r.startswith('~'))} stand-in picks)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
