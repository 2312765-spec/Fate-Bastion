#!/bin/sh
# Topology gate: validate LOW, wire + clay renders of LOD0, world-registered IoU of LOD0 (LOD1/2 hidden
# in a scratch copy so they do not add to the silhouette).
A="$(cd "$(dirname "$0")/.." && pwd -W 2>/dev/null || pwd)"
SK="$HOME/.claude/skills/blender-image-to-3d/scripts"
B="${BLENDER_BIN:-D:/Blender/blender.exe}"
OUT="$A/review/04_topology"
M="$A/CH_LongVuong_master.blend"
G="$OUT/_gate_lod0.blend"
mkdir -p "$OUT"
"$B" --background --factory-startup --python "$SK/validate.py" -- --blend "$M" --collections LOW \
  --out "$A/review/04_validate.json" --budget-tris 8500 --require-uv 2>&1 | grep -E "^TRIS|^WARN|^FAIL|^   "
cat > "$OUT/_hide_lods.py" <<'PY'
import bpy, sys
for o in bpy.data.objects:
    if o.name.endswith(("_LOD1", "_LOD2")):
        o.hide_render = True
bpy.ops.wm.save_as_mainfile(filepath=sys.argv[sys.argv.index("--") + 1])
PY
"$B" --background "$M" --python "$OUT/_hide_lods.py" -- "$G" > /dev/null 2>&1
LOD0="CH_LongVuong_Body_LOD0"
"$B" --background --factory-startup --python "$SK/review_render.py" -- --blend "$G" --out "$OUT" \
  --only "$LOD0" --views front,side,threequarter --mode wire > /dev/null 2>&1
"$B" --background --factory-startup --python "$SK/review_render.py" -- --blend "$G" --out "$OUT" \
  --only "$LOD0" --views front,side,back,threequarter --mode clay --gameplay-px 180 > /dev/null 2>&1
for v in "front 0 198 461 33 0.004561 565" "side 90 150 437 0 0.004561 565" \
         "back 180 197 437 0 0.004561 565" "front_full 0 258 617 0 0.0045 602"; do
  set -- $v
  "$B" --background --factory-startup --python "$SK/world_gate.py" -- --blend "$G" \
    --out "$OUT/world_$1" --matte "$A/ref/$1_matte.png" --az $2 --m-per-px $6 --axis-col $3 --ground-row $7 \
    --collections LOW > /dev/null 2>&1
  printf "%-10s " "$1"; python "$A/build/gate_clip.py" "$OUT/world_$1/sil_az$(printf %03d $2).png" "$A/ref/$1_matte.png" $4 $5
done
