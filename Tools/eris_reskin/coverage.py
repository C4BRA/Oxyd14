#!/usr/bin/env python3
"""Report how much of the upstream SS14 art in Resources/Textures is reskinned.

An RSI counts as reskinned when the state its entities show (or, failing that,
most of its states) comes from CEV-Eris. Oxyd's own folders (Oxyd/, _Oxyd/)
are excluded: they are already Eris or Oxyd art.

Usage: python3 Tools/eris_reskin/coverage.py [--md report.md]
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

import match

HERE = Path(__file__).resolve().parent


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--md", type=Path)
    args = ap.parse_args()

    mapped: dict[str, set] = {}
    standin: set[str] = set()
    files: set[str] = set()
    for mp in sorted((HERE / "maps").glob("*.json")):
        for e in json.loads(mp.read_text()):
            if "file" in e:
                files.add(e["file"])
                continue
            if e.get("panel_all"):
                _, all_states = match.rsi_states(e["target"])
                e["states"] = {st["name"]: {} for st in all_states}
            states = {k for k, v in e["states"].items() if not (isinstance(v, dict) and v.get("blank"))}
            mapped.setdefault(e["target"], set()).update(states | set(e["states"]))
            if e.get("standin"):
                standin.add(e["target"])

    # RSIs that entities reference directly but which already point at Eris art count as done too.
    names = match.entity_names_by_rsi()
    total, done, alt = Counter(), Counter(), Counter()
    for rsi_dir in sorted(match.TEXTURES.rglob("*.rsi")):
        rsi = str(rsi_dir.relative_to(match.TEXTURES))
        top = rsi.split("/")[0]
        if top in ("Oxyd", "_Oxyd") or not (rsi_dir / "meta.json").exists():
            continue
        cat = "/".join(rsi.split("/")[:2]) if top in ("Clothing", "Objects", "Structures", "Mobs", "Interface") else top
        total[cat] += 1
        _, states = match.rsi_states(rsi)
        snames = [s["name"] for s in states]
        m = mapped.get(rsi, set())
        primary = match.primary_state(rsi, snames)
        if primary in m or len(m) * 2 >= len(snames):
            done[cat] += 1
            if rsi in standin:
                alt[cat] += 1
    loose_total = Counter()
    for p in match.TEXTURES.rglob("*.png"):
        rel = str(p.relative_to(match.TEXTURES))
        if ".rsi/" in rel or rel.split("/")[0] not in ("Tiles", "Interface"):
            continue
        loose_total[rel.split("/")[0]] += 1
    loose_done = Counter(f.split("/")[0] for f in files)

    rows = sorted(total, key=lambda k: -total[k])
    T, D, A = sum(total.values()), sum(done.values()), sum(alt.values())
    lines = ["| Area | RSIs | Reskinned | Of which stand-ins | Coverage |", "|---|---:|---:|---:|---:|"]
    for k in rows:
        lines.append(f"| {k} | {total[k]} | {done[k]} | {alt[k]} | {100 * done[k] / total[k]:.0f}% |")
    lines.append(f"| **All upstream RSIs** | **{T}** | **{D}** | **{A}** | **{100 * D / T:.1f}%** |")
    for k in sorted(loose_total):
        lines.append(f"| {k} (loose PNGs) | {loose_total[k]} | {loose_done[k]} | | {100 * loose_done[k] / loose_total[k]:.0f}% |")
    text = "\n".join(lines)
    print(text)
    if args.md:
        args.md.write_text(text + "\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
