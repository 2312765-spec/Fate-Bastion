#!/bin/sh
# Forms gate: rebuild forms, clay + material renders of HIGH, world-registered IoU per view
# (re-scored inside the rows each reference covers). Forms tolerance: IoU 0.90, bands 0.03.
#   sh build/gate03.sh [review-subfolder]
A="$(cd "$(dirname "$0")/.." && pwd -W 2>/dev/null || pwd)"
SK="$HOME/.claude/skills/blender-image-to-3d/scripts"
B="${BLENDER_BIN:-D:/Blender/blender.exe}"
OUT="$A/review/${1:-03_forms}"
M="$A/CH_LongVuong_master.blend"

"$B" --background --factory-startup --python "$A/build/03_forms.py" -- --master "$M" 2>&1 | grep -E "\[03_forms\]|Error"
"$B" --background --factory-startup --python "$SK/review_render.py" -- --blend "$M" --out "$OUT" \
  --collections HIGH --views front,side,back,threequarter --mode clay --gameplay-px 180 > /dev/null 2>&1
"$B" --background --factory-startup --python "$SK/review_render.py" -- --blend "$M" --out "$OUT" \
  --collections HIGH --views front,threequarter,back --mode material > /dev/null 2>&1

for v in "front 0 198 461 33 0.004561 565" "side 90 150 437 0 0.004561 565" \
         "back 180 197 437 0 0.004561 565" "front_full 0 258 617 0 0.0045 602"; do
  set -- $v
  "$B" --background --factory-startup --python "$SK/world_gate.py" -- --blend "$M" \
    --out "$OUT/world_$1" --matte "$A/ref/$1_matte.png" --az $2 --m-per-px $6 --axis-col $3 --ground-row $7 \
    --collections HIGH > /dev/null 2>&1
  printf "%-10s " "$1"; python "$A/build/gate_clip.py" "$OUT/world_$1/sil_az$(printf %03d $2).png" "$A/ref/$1_matte.png" $4 $5
done
