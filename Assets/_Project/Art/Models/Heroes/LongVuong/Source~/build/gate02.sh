#!/bin/sh
# Blockout gate: rebuild the calibrated master, run the blockout, clay renders, world-registered IoU,
# re-scored inside the rows each reference actually covers (build/gate_clip.py).
#   sh build/gate02.sh [review-subfolder]
A="$(cd "$(dirname "$0")/.." && pwd -W 2>/dev/null || pwd)"
SK="$HOME/.claude/skills/blender-image-to-3d/scripts"
B="${BLENDER_BIN:-D:/Blender/blender.exe}"
OUT="$A/review/${1:-02_blockout}"

# Final calibration: crown 2.34 m at canvas row 52, ground row 565 on a 580 px canvas.
"$B" --background --factory-startup --python "$SK/init_master.py" -- --out "$A/CH_LongVuong_master.blend" \
  --name CH_LongVuong --height 2.34 --ref-front "$A/ref/front.png" --ref-right "$A/ref/side.png" \
  --ref-back "$A/ref/back.png" --ref-extra "$A/ref/threequarter.png" "$A/ref/front_full.png" \
  --subject-frac 0.9103 --ground-frac 0.02586 --cell 2.0 \
  --cam-elev 35 --cam-az 0 --cam-dist 12 --cam-lens 18 > /dev/null 2>&1

"$B" --background --factory-startup --python "$A/build/02_blockout.py" -- --master "$A/CH_LongVuong_master.blend" 2>&1 \
  | grep "\[02_blockout\]"
"$B" --background --factory-startup --python "$SK/review_render.py" -- --blend "$A/CH_LongVuong_master.blend" \
  --out "$OUT" --collections LOW --views front,side,back,threequarter --mode clay --gameplay-px 180 > /dev/null 2>&1

# name  az  axis-col  cut-row  top-row  m/px  ground-row
for v in "front 0 198 461 33 0.004561 565" "side 90 150 437 0 0.004561 565" \
         "back 180 197 437 0 0.004561 565" "front_full 0 258 617 0 0.0045 602"; do
  set -- $v
  "$B" --background --factory-startup --python "$SK/world_gate.py" -- --blend "$A/CH_LongVuong_master.blend" \
    --out "$OUT/world_$1" --matte "$A/ref/$1_matte.png" --az $2 --m-per-px $6 --axis-col $3 --ground-row $7 \
    --collections LOW > /dev/null 2>&1
  printf "%-10s " "$1"; python "$A/build/gate_clip.py" "$OUT/world_$1/sil_az$(printf %03d $2).png" "$A/ref/$1_matte.png" $4 $5
done
