"""Catalogue CEV-Eris atom types and resolve where their sprites live.

Parses the DM type tree under code/ (full paths and indented nesting), resolves
inherited vars, then works out, per item, the DMI + state used for:
  icon      - the item on the floor / in the inventory
  worn      - per equipment slot, the sprite drawn on the mob
  inhand    - left / right hand sprites

This mirrors code/modules/mob/living/carbon/human/update_icons.dm closely
enough for sprite lookup; it is not a DM interpreter.
"""

from __future__ import annotations

import json
import re
from functools import lru_cache
from pathlib import Path

import dmi

VARS = ("name", "desc", "icon", "icon_state", "item_state", "icon_override", "contained_sprite",
        "item_icons", "item_state_slots", "slot_flags", "color", "wielded_icon", "w_class")
TYPE_RE = re.compile(r"^/?((?:obj|mob|turf|area|atom|datum)(?:/\w+)*)$")
CHILD_RE = re.compile(r"^/?(\w+(?:/\w+)*)$")
VAR_RE = re.compile(r"^(?:var/(?:\w+/)*)?(\w+)\s*=\s*(.+)$")

# update_icons.dm defaults per slot (male variants; female ones are not supported by SS14).
SLOT_DMI = {
    "uniform": "icons/inventory/uniform/mob.dmi",
    "suit": "icons/inventory/suit/mob.dmi",
    "gloves": "icons/inventory/hands/mob.dmi",
    "glasses": "icons/inventory/eyes/mob.dmi",
    "ears": "icons/inventory/ears/mob.dmi",
    "mask": "icons/inventory/face/mob.dmi",
    "head": "icons/inventory/head/mob.dmi",
    "shoes": "icons/inventory/feet/mob.dmi",
    "belt": "icons/inventory/belt/mob.dmi",
    "s_store": "icons/inventory/on_suit/mob.dmi",
    "back": "icons/inventory/back/mob.dmi",
    "accessory": "icons/inventory/accessory/mob.dmi",
}
SLOT_STR = {"uniform": "slot_w_uniform_str", "suit": "slot_wear_suit_str", "gloves": "slot_gloves_str",
            "glasses": "slot_glasses_str", "ears": "slot_l_ear_str", "mask": "slot_wear_mask_str",
            "head": "slot_head_str", "shoes": "slot_shoes_str", "belt": "slot_belt_str",
            "s_store": "slot_s_store_str", "back": "slot_back_str", "l_hand": "slot_l_hand_str",
            "r_hand": "slot_r_hand_str"}
# WORN_* defines in update_icons.dm: contained sprites keep every slot in the item's own DMI.
CONTAINED_SUFFIX = {"uniform": "_un", "suit": "_su", "head": "_he", "l_hand": "_lh", "r_hand": "_rh",
                    "s_store": "_ss", "ears": "_le", "gloves": "_gl", "shoes": "_sh", "glasses": "_ey",
                    "belt": "_be", "back": "_ba", "mask": "_ma"}
INHAND_DMIS = {"l_hand": ["icons/mob/items/lefthand.dmi", "icons/mob/items/lefthand_guns.dmi"],
               "r_hand": ["icons/mob/items/righthand.dmi", "icons/mob/items/righthand_guns.dmi"]}


def _strip_comment(line: str) -> str:
    out, q = [], None
    i = 0
    while i < len(line):
        ch = line[i]
        if q:
            out.append(ch)
            if ch == "\\" and i + 1 < len(line):
                out.append(line[i + 1])
                i += 1
            elif ch == q:
                q = None
        elif ch in "\"'":
            q = ch
            out.append(ch)
        elif line.startswith("//", i):
            break
        else:
            out.append(ch)
        i += 1
    return "".join(out).rstrip()


def parse_types(code: Path) -> dict[str, dict]:
    types: dict[str, dict] = {}
    for f in sorted(code.rglob("*.dm")):
        stack: list[tuple[int, str | None]] = []
        in_block_comment = False
        for raw in f.read_text(encoding="utf-8", errors="replace").splitlines():
            if in_block_comment:
                if "*/" in raw:
                    in_block_comment = False
                continue
            if raw.lstrip().startswith("/*"):
                in_block_comment = "*/" not in raw
                continue
            line = _strip_comment(raw)
            if not line.strip():
                continue
            expanded = line.replace("    ", "\t")
            indent = len(expanded) - len(expanded.lstrip("\t"))
            text = line.strip()
            while stack and stack[-1][0] >= indent:
                stack.pop()
            parent = stack[-1][1] if stack else None
            if indent == 0:
                m = TYPE_RE.match(text)
                path = "/" + m.group(1) if m else None
                if path:
                    types.setdefault(path, {})
                stack.append((0, path))
                continue
            if parent is None:
                stack.append((indent, None))
                continue
            v = VAR_RE.match(text)
            if v and "(" not in text.split("=")[0]:
                key = v.group(1)
                if key in VARS and key not in types[parent]:
                    types[parent][key] = v.group(2).strip()
                stack.append((indent, None))
                continue
            m = CHILD_RE.match(text)
            if m and not text.startswith(("proc", "verb", "var")):
                path = f"{parent}/{m.group(1)}"
                types.setdefault(path, {})
                stack.append((indent, path))
            else:
                stack.append((indent, None))
    return types


def _unquote(v: str | None) -> str | None:
    if v is None:
        return None
    v = v.strip()
    if v in ("null", '""', "''"):
        return None
    if len(v) >= 2 and v[0] == v[-1] and v[0] in "\"'":
        return v[1:-1]
    return v


class Catalog:
    def __init__(self, eris: Path):
        self.eris = eris
        self.raw = parse_types(eris / "code")

    def var(self, path: str, key: str) -> str | None:
        p = path
        while p:
            d = self.raw.get(p)
            if d and key in d:
                return d[key]
            p = p.rsplit("/", 1)[0] if p.count("/") > 1 else None
        return None

    def has(self, dmi_path: str | None, state: str | None) -> bool:
        if not dmi_path or state is None:
            return False
        p = self.eris / dmi_path
        return p.exists() and state in dmi.load(p)

    def item_icons(self, path: str) -> dict[str, str]:
        raw = self.var(path, "item_icons") or ""
        return {k: v for k, v in re.findall(r"(slot_\w+_str)\s*=\s*'([^']+)'", raw)}

    def item_state_slots(self, path: str) -> dict[str, str]:
        raw = self.var(path, "item_state_slots") or ""
        return {k: v for k, v in re.findall(r"(slot_\w+_str)\s*=\s*\"([^\"]+)\"", raw)}

    def resolve(self, path: str) -> dict | None:
        icon = _unquote(self.var(path, "icon"))
        state = _unquote(self.var(path, "icon_state"))
        if not icon or state is None or not self.has(icon, state):
            return None
        item_state = _unquote(self.var(path, "item_state")) or state
        contained = (self.var(path, "contained_sprite") or "").strip() in ("1", "TRUE")
        override = _unquote(self.var(path, "icon_override"))
        overrides = self.item_icons(path)
        slot_states = self.item_state_slots(path)
        out = {"type": path, "name": _unquote(self.var(path, "name")) or path.rsplit("/", 1)[1],
               "desc": _unquote(self.var(path, "desc")) or "", "color": _unquote(self.var(path, "color")),
               "icon": [icon, state], "worn": {}, "inhand": {}}

        def find(slot: str, states: list[str]) -> list[str] | None:
            dmis = []
            if contained:
                suffix = CONTAINED_SUFFIX.get(slot)
                if suffix:
                    for s in states:
                        if self.has(override or icon, s + suffix):
                            return [override or icon, s + suffix]
            if SLOT_STR.get(slot) in overrides:
                dmis.append(overrides[SLOT_STR[slot]])
            if override:
                dmis.append(override)
            dmis += INHAND_DMIS.get(slot) or ([SLOT_DMI[slot]] if slot in SLOT_DMI else [])
            for d in dmis:
                for s in states:
                    for cand in (s, s + ("_l" if slot == "l_hand" else "_r" if slot == "r_hand" else "")):
                        if self.has(d, cand):
                            return [d, cand]
            return None

        for slot in SLOT_DMI:
            first = slot_states.get(SLOT_STR.get(slot, ""))
            order = [first] if first else []
            order += [state, item_state] if slot in ("uniform", "suit", "head") else [item_state, state]
            hit = find(slot, [s for s in order if s])
            if hit:
                out["worn"][slot] = hit
        for hand in ("l_hand", "r_hand"):
            first = slot_states.get(SLOT_STR[hand])
            hit = find(hand, [s for s in ([first] if first else []) + [item_state, state] if s])
            if hit:
                out["inhand"][hand] = hit
        return out


@lru_cache(maxsize=1)
def load(eris: Path, cache: Path) -> list[dict]:
    if cache.exists():
        return json.loads(cache.read_text())
    cat = Catalog(eris)
    items = []
    for path in sorted(cat.raw):
        if not path.startswith(("/obj", "/mob", "/turf")):
            continue
        r = cat.resolve(path)
        if r:
            items.append(r)
    cache.parent.mkdir(exist_ok=True)
    cache.write_text(json.dumps(items))
    return items
