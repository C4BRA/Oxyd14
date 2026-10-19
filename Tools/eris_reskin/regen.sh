#!/usr/bin/env bash
# Regenerate every map and rebuild all reskinned textures from upstream art.
# Usage: Tools/eris_reskin/regen.sh /path/to/CEV-Eris
set -euo pipefail
ERIS="$1"
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
BASE="$(cat "$HERE/BASE")"
cd "$HERE"
python3 gen_ui.py
python3 gen_clothing.py --eris "$ERIS"
python3 gen_smoothing.py
python3 gen_doors.py
python3 gen_tiles.py --eris "$ERIS"
python3 gen_mobs.py --eris "$ERIS"
python3 gen_effects.py
for group in objects structures mobs; do
    python3 gen_objects.py --eris "$ERIS" --group "$group"
done
cd "$REPO"
git checkout "$BASE" -- Resources/Textures/Clothing Resources/Textures/Objects Resources/Textures/Structures \
    Resources/Textures/Mobs Resources/Textures/Tiles Resources/Textures/Interface Resources/Textures/Effects \
    Resources/Textures/Fluids Resources/Textures/Decals
python3 "$HERE/build.py" --eris "$ERIS"
