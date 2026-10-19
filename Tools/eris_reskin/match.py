"""Shared helpers for pairing SS14 RSIs with CEV-Eris sprites."""

from __future__ import annotations

import colorsys
import json
import math
import re
from collections import defaultdict
from functools import lru_cache
from pathlib import Path

import yaml
from PIL import Image

import dmi

REPO = Path(__file__).resolve().parents[2]
TEXTURES = REPO / "Resources" / "Textures"
PROTOTYPES = REPO / "Resources" / "Prototypes"

STOP = {"the", "a", "an", "of", "and", "with", "for", "in", "on", "rsi", "png", "icon", "clothing", "item",
        "object", "s", "de", "alt", "new", "old", "base", "x", "1", "2", "3", "obj", "storage", "rank", "textures"}

# SS14 / station words -> the CEV-Eris faction or naming used for the same thing.
SYNONYMS = {
    "security": ["ironhammer", "ih", "sec", "warden", "officer"], "sec": ["security", "ironhammer"],
    "hos": ["commander", "ironhammer", "security"], "warden": ["gunnery", "sergeant", "ironhammer"],
    "detective": ["inspector", "det"], "engineering": ["technomancer", "engineer", "eng"],
    "engineer": ["technomancer", "engineering"], "ce": ["exultant", "technomancer", "chief"],
    "atmos": ["atmospheric", "technomancer"], "atmospheric": ["atmos", "technomancer"],
    "medical": ["moebius", "medic", "doctor", "med"], "doctor": ["medical", "moebius", "medic"],
    "cmo": ["biolab", "moebius", "chief", "medical"], "paramedic": ["moebius", "emt", "paramedic"],
    "science": ["moebius", "research", "scientist", "sci"], "scientist": ["moebius", "research", "science"],
    "rd": ["expedition", "overseer", "moebius", "research"], "roboticist": ["moebius", "robotics"],
    "chemist": ["moebius", "chemistry", "chem"], "geneticist": ["moebius", "genetics"],
    "virologist": ["moebius", "virology"], "cargo": ["guild", "aster", "technician", "supply"],
    "qm": ["guild", "merchant", "quartermaster"], "quartermaster": ["guild", "merchant"],
    "salvage": ["guild", "miner", "mining"], "miner": ["guild", "mining"], "mining": ["guild", "miner"],
    "chaplain": ["neotheology", "preacher", "church", "acolyte"], "botanist": ["agrolyte", "neotheology", "hydro"],
    "janitor": ["custodian", "neotheology", "janitor"], "captain": ["captain", "caphat"],
    "hop": ["first", "officer", "fo"], "bartender": ["club", "bar"], "chef": ["club", "cook", "chef"],
    "syndicate": ["syndi", "mercenary", "merc", "serbian"], "nukie": ["syndicate", "mercenary"],
    "hardsuit": ["rig", "voidsuit", "space", "suit", "helmet"], "eva": ["space", "void", "rig"],
    "jumpsuit": ["uniform", "under", "jumpsuit"], "jumpskirt": ["skirt", "uniform", "under"],
    "beret": ["beret"], "cap": ["cap", "soft"], "boots": ["boots", "jackboots", "shoes"],
    "gloves": ["gloves"], "glasses": ["glasses", "goggles"], "goggles": ["goggles", "glasses"],
    "hud": ["hud", "glasses"], "gasmask": ["gas", "mask"], "breath": ["breath", "mask"],
    "coat": ["coat", "jacket", "overcoat"], "jacket": ["jacket", "coat"], "vest": ["vest", "armor"],
    "armor": ["armor", "vest", "plate"], "helmet": ["helmet", "helm"], "hood": ["hood"],
    "labcoat": ["labcoat", "lab", "coat"], "apron": ["apron"], "winter": ["winter", "coat", "warm"],
    "bio": ["bio", "biohazard"], "fire": ["fire", "firefighter"], "rad": ["radiation", "rad"],
    "clown": ["clown"], "mime": ["mime"], "wizard": ["wizard"], "ninja": ["ninja"],
    "headset": ["headset", "radio"], "satchel": ["satchel"], "duffel": ["duffel", "duffelbag"],
    "backpack": ["backpack", "bag"], "belt": ["belt", "utility"], "holster": ["holster"],
    "cloak": ["cloak", "cape", "poncho", "mantle"], "scarf": ["scarf"], "tie": ["tie"],
    "medal": ["medal"], "bedsheet": ["bedsheet", "sheet"], "magboots": ["magboots"],
    "generic": ["crate", "closet", "generic"], "secure": ["secure", "locked", "weapon"],
}


def tokens(*texts: str) -> set[str]:
    out: set[str] = set()
    for text in texts:
        if not text:
            continue
        text = re.sub(r"([a-z])([A-Z])", r"\1 \2", text)
        for t in re.split(r"[^a-z0-9]+", text.lower()):
            if t and t not in STOP and not t.isdigit():
                # Crude singular form so "satchels" meets "satchel".
                out.add(t[:-1] if len(t) > 4 and t.endswith("s") and not t.endswith("ss") else t)
    return out


def expand(toks: set[str]) -> set[str]:
    out = set(toks)
    for t in toks:
        out.update(SYNONYMS.get(t, ()))
    return out


def name_score(a: set[str], b: set[str], idf: dict[str, float] | None = None) -> float:
    """Similarity of two token sets, a = SS14 side (expanded), b = Eris side.
    With idf, rare words (job and faction names) count for more than common ones."""
    if not a or not b:
        return 0.0
    if idf is None:
        return len(a & b) / math.sqrt(len(a) * len(b))
    w = lambda ts: sum(idf.get(t, 1.0) for t in ts)  # noqa: E731
    hit = w(a & b)
    # Eris often runs words together ("medicalcrate", "protolathe_work"): partial credit for
    # an SS14 word found inside an Eris word, or the other way round.
    for t in a - b:
        if len(t) >= 4 and any(len(x) >= 4 and (t in x or x in t) for x in b):
            hit += 0.75 * idf.get(t, 1.0)
    return min(1.0, hit / math.sqrt(w(a) * w(b)))


def idf_weights(docs: list[set[str]]) -> dict[str, float]:
    df: dict[str, int] = defaultdict(int)
    for d in docs:
        for t in d:
            df[t] += 1
    n = len(docs) or 1
    return {t: math.log(1 + n / c) for t, c in df.items()}


def assign(targets: list, candidates_for, score, reuse_penalty: float = 0.06):
    """Greedy global assignment: most confident targets pick first, reuse is penalised.
    candidates_for(t) -> list of candidates; score(t, c) -> (total, name_score).
    Returns {target index: (candidate, total, name_score)}."""
    ranked = []
    for i, t in enumerate(targets):
        sc = sorted(((score(t, c), c) for c in candidates_for(t)), key=lambda x: -x[0][0])[:12]
        if sc:
            ranked.append((sc[0][0][0], i, sc))
    ranked.sort(key=lambda x: -x[0])
    used: dict[int, int] = defaultdict(int)
    out = {}
    for _, i, sc in ranked:
        # Reuse is only traded against candidates whose names match about as well.
        top_ns = max(x[0][1] for x in sc)
        pool = [x for x in sc if x[0][1] >= top_ns - 0.15]
        best = max(pool, key=lambda x: x[0][0] - reuse_penalty * used[id(x[1])])
        used[id(best[1])] += 1
        out[i] = (best[1], best[0][0], best[0][1])
    return out


# --- colour signatures -----------------------------------------------------

def signature(im: Image.Image) -> list[float] | None:
    """12 hue bins + 3 lightness bins + saturation, weighted by opaque pixels."""
    im = im.convert("RGBA")
    if im.width > 48:
        im = im.resize((im.width // 2, im.height // 2))
    bins = [0.0] * 16
    total = 0
    for r, g, b, a in im.getdata():
        if a < 128:
            continue
        h, l, s = colorsys.rgb_to_hls(r / 255, g / 255, b / 255)
        total += 1
        if s > 0.18 and 0.08 < l < 0.92:
            bins[int(h * 12) % 12] += 1
        else:
            bins[12 + min(2, int(l * 3))] += 1
        bins[15] += s
    if not total:
        return None
    return [v / total for v in bins]


def colour_score(a: list[float] | None, b: list[float] | None) -> float:
    if a is None or b is None:
        return 0.0
    return 1.0 - 0.5 * sum(abs(x - y) for x, y in zip(a, b))


# --- SS14 side -------------------------------------------------------------

class _Loader(yaml.SafeLoader):
    pass


def _any_tag(loader, suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)


_Loader.add_multi_constructor("!", _any_tag)


@lru_cache(maxsize=1)
def entity_names_by_rsi() -> dict[str, list[str]]:
    """RSI path -> names/ids of entities that use it (own or inherited Sprite)."""
    ents = {}
    for f in PROTOTYPES.rglob("*.yml"):
        try:
            docs = yaml.load(f.read_text(encoding="utf-8"), Loader=_Loader)
        except Exception:  # noqa: BLE001 - some files are not plain YAML
            continue
        for e in docs or []:
            if not isinstance(e, dict) or e.get("type") != "entity" or not isinstance(e.get("id"), str):
                continue
            sprites, states = set(), []
            for c in e.get("components") or []:
                if not isinstance(c, dict):
                    continue
                if isinstance(c.get("sprite"), str):
                    sprites.add(c["sprite"])
                if c.get("type") == "Sprite" and isinstance(c.get("state"), str):
                    states.append(c["state"])
                for layer in c.get("layers") or []:
                    if isinstance(layer, dict) and isinstance(layer.get("sprite"), str):
                        sprites.add(layer["sprite"])
                    if c.get("type") == "Sprite" and isinstance(layer, dict) and isinstance(layer.get("state"), str):
                        states.append(layer["state"])
            parents = e.get("parent") or []
            ents[e["id"]] = {"name": e.get("name"), "parents": parents if isinstance(parents, list) else [parents],
                             "sprites": sprites, "states": states, "desc": e.get("description") or ""}

    def sprites_of(i, seen=()):
        e = ents.get(i)
        if not e or i in seen:
            return set()
        if e["sprites"]:
            return e["sprites"]
        out = set()
        for p in e["parents"]:
            out |= sprites_of(p, seen + (i,))
        return out

    by_rsi: dict[str, list[str]] = defaultdict(list)
    for i, e in ents.items():
        for s in sprites_of(i):
            key = s.lstrip("/").removeprefix("Textures/")
            by_rsi[key].append(f"{i} {e['name'] or ''}")
            for st in e["states"][:1]:
                USED_STATES[key][st] += 1
                USED_NAMES[key][st].append(f"{i} {e['name'] or ''}")
    return by_rsi


# RSI path -> {state: number of entities whose Sprite shows it first}; filled by entity_names_by_rsi.
USED_STATES: dict[str, dict[str, int]] = defaultdict(lambda: defaultdict(int))
USED_NAMES: dict[str, dict[str, list[str]]] = defaultdict(lambda: defaultdict(list))


def primary_state(rsi: str, snames: list[str]) -> str:
    entity_names_by_rsi()
    used = USED_STATES.get(rsi)
    if used:
        best = max(used, key=used.get)
        if best in snames:
            return best
    for s in ("icon", "base", "closed", "off", "idle"):
        if s in snames:
            return s
    return snames[0]


def coverage(im) -> int:
    if im is None:
        return 0
    return sum(1 for a in im.getchannel("A").getdata() if a > 64)


def rsi_states(rsi: str) -> tuple[dict, list[dict]]:
    meta = json.loads((TEXTURES / rsi / "meta.json").read_text(encoding="utf-8-sig"))
    return meta, meta["states"]


def rsi_first_frame(rsi: str, state: str) -> Image.Image | None:
    meta = json.loads((TEXTURES / rsi / "meta.json").read_text(encoding="utf-8-sig"))
    p = TEXTURES / rsi / f"{state}.png"
    if not p.exists():
        return None
    w, h = meta["size"]["x"], meta["size"]["y"]
    return Image.open(p).convert("RGBA").crop((0, 0, w, h))


def list_rsis(prefix: str) -> list[str]:
    return sorted(str(p.relative_to(TEXTURES)) for p in (TEXTURES / prefix).rglob("*.rsi") if (p / "meta.json").exists())


def eris_first_frame(eris: Path, src: list[str]) -> Image.Image | None:
    st = dmi.get(eris, src[0], src[1])
    return st.images[0][0] if st else None


class Picker:
    """Chooses an Eris candidate per target, discouraging heavy reuse of one sprite."""

    def __init__(self, reuse_penalty: float = 0.08):
        self.used: dict[str, int] = defaultdict(int)
        self.penalty = reuse_penalty

    def pick(self, scored: list[tuple[float, str, dict]]):
        best = None
        for score, key, cand in scored:
            s = score - self.penalty * self.used[key]
            if best is None or s > best[0]:
                best = (s, key, cand, score)
        if best:
            self.used[best[1]] += 1
        return best
