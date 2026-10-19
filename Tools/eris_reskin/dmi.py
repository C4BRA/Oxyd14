"""Read CEV-Eris DMI files (PNG sheet + zTXt "Description" metadata).

DMI sheets are frame-major (frame0: S N E W, frame1: S N E W, ...).
States are exposed here as per-direction frame lists so callers can lay them
out in RSI order (direction-major).
"""

from __future__ import annotations

import struct
import zlib
from dataclasses import dataclass, field
from functools import lru_cache
from pathlib import Path

from PIL import Image


@dataclass
class DmiState:
    name: str
    width: int
    height: int
    dirs: int
    frames: int
    # Delays in seconds, one per frame (None for single-frame states).
    delays: list[float] | None
    # images[dir][frame]
    images: list[list[Image.Image]] = field(default_factory=list)


def _description(data: bytes) -> str | None:
    i = 8
    while i < len(data):
        ln = struct.unpack(">I", data[i : i + 4])[0]
        typ = data[i + 4 : i + 8]
        payload = data[i + 8 : i + 8 + ln]
        if typ == b"zTXt":
            _, rest = payload.split(b"\x00", 1)
            try:
                text = zlib.decompress(rest[1:]).decode("utf-8", "replace")
            except zlib.error:
                text = ""
            if "# BEGIN DMI" in text:
                return text
        if typ == b"IEND":
            break
        i += 12 + ln
    return None


@lru_cache(maxsize=None)
def load(path: Path) -> dict[str, DmiState]:
    """Return {state name: DmiState}. Duplicate state names keep the first."""
    data = path.read_bytes()
    text = _description(data)
    if text is None:
        return {}

    width = height = 32
    raw: list[dict] = []
    for line in text.splitlines():
        s = line.strip()
        if s.startswith("width ="):
            width = int(s.split("=")[1])
        elif s.startswith("height ="):
            height = int(s.split("=")[1])
        elif s.startswith("state ="):
            raw.append({"name": s.split("=", 1)[1].strip().strip('"'), "dirs": 1, "frames": 1, "delay": None})
        elif raw and s.startswith("dirs ="):
            raw[-1]["dirs"] = int(s.split("=")[1])
        elif raw and s.startswith("frames ="):
            raw[-1]["frames"] = int(s.split("=")[1])
        elif raw and s.startswith("delay ="):
            raw[-1]["delay"] = [float(x) for x in s.split("=")[1].split(",") if x.strip()]

    sheet = Image.open(path).convert("RGBA")
    cols = max(1, sheet.width // width)
    states: dict[str, DmiState] = {}
    idx = 0
    for st in raw:
        dirs, frames = st["dirs"], st["frames"]
        images: list[list[Image.Image]] = [[] for _ in range(dirs)]
        for f in range(frames):
            for d in range(dirs):
                r, c = divmod(idx, cols)
                images[d].append(sheet.crop((c * width, r * height, c * width + width, r * height + height)))
                idx += 1
        delays = None
        if frames > 1:
            ticks = (st["delay"] or [1.0] * frames)[:frames]
            ticks += [ticks[-1]] * (frames - len(ticks))
            delays = [round(max(t, 0.1) / 10.0, 3) for t in ticks]
        if st["name"] not in states:
            states[st["name"]] = DmiState(st["name"], width, height, dirs, frames, delays, images)
    return states


def get(eris_root: Path, dmi: str, state: str) -> DmiState | None:
    path = eris_root / dmi
    if not path.exists():
        return None
    return load(path).get(state)
