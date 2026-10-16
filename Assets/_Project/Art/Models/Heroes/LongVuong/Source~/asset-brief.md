# Asset brief – CH_LongVuong

```text
ASSET      CH_LongVuong  (Long Vương, Legendary hero, element Fire, role Mage – Fate Bastion)
CATEGORY   character, biped (humanoid, heavy layered costume: scale armour, robe, fur trim, very long hair)

VIEWS      Sheet panels share one canvas (580 px tall), one scale and ground row 565 – see build/00_ref_panels.py.
           ref/front_full.png    front, full body incl. boots and floor (game-model render, dark studio,
                                 mild perspective), own scale: crown row 82, ground row 602, axis col 258,
                                 0.0045 m/px. The only view showing the ground; authority for legs/boots.
           ref/front.png         front, az 0, el 0, lens 0 (near-orthographic studio render). Subject x 6-385,
                                 antler tips cut by the sheet top, boots cut at row 461. Axis col 198.
           ref/side.png          left side (camera on +X), az 90, el 0, lens 0. Face toward image left. Axis col 150.
           ref/back.png          back, az 180, el 0, lens 0. Antler tips visible (row 13). Axis col 197.
           ref/threequarter.png  concept, az ~30, el ~0, lens ~85 (mild perspective) – likeness and detail only.
           Captions and the film title were erased from the mattes (ref/*_matte.png); the caption crossing the
           torso/arm (rows 229-261) is rebuilt from the rows above and below, so that band is approximate.

SCALE      Crown-to-ground 513 px = 2.34 m  ->  0.004561 m/px (sheets); 520 px -> 0.0045 m/px (front_full).
           2.34 m = 1.8 m adult x 1.3 (Legendary size rule, GDD 12). Antler tips ~2.53 m.
           Ground row 565 on the sheets is DERIVED from front_full (robe-bottom-to-sole gap = 0.316 x
           crown-to-robe-bottom). First pass had inferred 495 (7.4 heads); corrected to 8.5 heads.

PROPORTIONS (final metres; H = head height ≈ 0.28 m; z from ground) – the build keeps the sheet
           measurements in first-pass units and maps them with p' = 0.8635 p + (0, 0, 0.319)
           1  total crown height 8.5 H (2.34 m)
           2  chin z 2.06 m; neck base z 1.98 m
           3  pauldron fur span 1.34 m (sheets) / 1.40 m (front_full) at z 1.85 m – widest point
           4  pauldron crest top z 2.30 m, crests at x ±0.22 m
           5  torso width under pauldrons ≈ 0.68 m incl. armour at z 1.66 m
           6  belt z 1.44-1.51 m
           7  robe widest 1.09 m at z 0.89 m (sheets) / 1.30 m (front_full)
           8  robe fur hem bottom z 0.68 m; lower robe to z 0.545 m (midpoint, see CONFLICTS)
           9  boots to z 0.55 (shaft 0.14-0.16 m wide), feet centres ±0.25 m, soles splay to ±0.31 m
           10 arms in A-pose ≈ 52° below horizontal; shoulder (±0.29, 1.91), elbow (±0.54, 1.56),
              wrist (±0.73, 1.33), claw tips (±0.87, 1.15)
           11 side: chest front 0.19 m ahead of the axis, robe front 0.26 m ahead, hair back 0.60 m behind
           12 hair: crown to z 0.85 m down the back, widest ±0.43 m at z 1.44 m
           13 antlers: two beams from the crown rising ~0.19 m above it, each with one outward tine

CONFLICTS  front_full (game render) vs the design sheets: its robe is ~0.1 m wider each side at z 0.8-1.1
           and its fur hem sits ~0.09 m higher; its lower robe ends at z 0.53 vs 0.56 on the sheets.
           The sheets win for the robe (orthographic design sheet); front_full wins for legs/boots.
           Per-view ceilings recorded: front_full IoU 0.84 (robe bands -0.045..-0.07), sheet front
           bottom band +0.054 (lower-robe hem midpoint).

PARTS      (construction and overlap order; rigid = follows one bone)
           body (complete mannequin, kept in master)             deforming
           head + face, ears (pointed)                            deforming (Head)
           eyes                                                   separate, rigid (Head)
           hair cap + long back mass + 2 front strands            separate, rigid on Head / chain later
           antlers (2)                                            separate, rigid (Head)
           undersuit / sleeves (puffed fur-lined upper arms)      deforming
           chest armour (scale lamellar) + chest emblem           rigid (Chest / UpperChest)
           belt + buckle                                          rigid (Hips)
           robe: inner skirt, front/back scale tabards, side panels, fur hem   rigid/deforming (Hips, legs)
           pauldrons (dragon-head crest + 3 layered scale plates + fur roll)   rigid (UpperArm)
           gauntlets with long claws                              rigid (LowerArm / Hand)
           braided cords with tassels (2)                         rigid (Hips)
           boots (dark, armoured)                                 rigid (Foot)

SILHOUETTE 1 very wide pauldrons with upturned dragon-head crests and fur flare (inverted triangle top)
           2 long robe flaring to a fur hem (bell bottom)
           3 floor-long silver hair mass behind (back/side read)
           4 antlers above the head
           5 long clawed hands hanging out in A-pose

MATERIALS  silver armour (painted/polished metal, roughness 0.3-0.45)  ·  dark steel gauntlets/boots (0.4-0.55)
           white fur (0.85-0.95)  ·  silver hair (0.4-0.6)  ·  skin pale (0.5)  ·  robe cloth ivory (0.7)
           FIRE DELTA (see below): ember-red to gold scale tint, emissive ember veins, flame tips

DESIGN DELTA (keep the read of the reference, make the character our own; reference is a film design)
           - Palette: keep silver/white base; replace the blue-grey scale tint and pink motifs with an
             ember red -> gold gradient and add emissive ember veins (Fire element).
           - Antlers: our own branching with flame-shaped glowing tips.
           - Face: original face, no likeness; ember eyes and a flame mark on the forehead.
           - Chest emblem: a flame-wreathed dragon pearl instead of the reference's dragon-face plaque.
           - Pauldron crests: dragon heads redesigned with open jaws and a flame tongue.
           - Tassels end in flame shapes; hair gets ember-red tips.

ARTICULATION Unity Humanoid deformation skeleton (Hips … Head, arms, legs, toes) authored in A-pose;
           rigid armour to one bone, cloth/robe skinned to Hips + upper legs; optional hair chain inferred.
           Sockets: SOCKET_Muzzle (left palm, cast point), SOCKET_Overhead (above antlers, ≈2.75 m),
           SOCKET_AuraAnchor (ground). Animation: none here – the game retargets shared Humanoid clips.

INFERRED   antler tips in the sheet front view (taken from the back view); the far (right) side
           (mirrored from the left); the face is redesigned (delta), not measured; inside of the robe;
           hair underside; claws' exact count (4 + thumb assumed).

TARGET     Unity 6 URP, FBX. LOW-POLY REVISION (user choice, matches GDD "low-poly / stylized" and the
           Quaternius heroes): LOD0 ≈ 8k tris (built 8,086), LOD1 3.5k, LOD2 1.5k; ONE mesh
           (CH_LongVuong_Body_LOD*), ONE material (M_LV_Palette), flat shading, three 64 x 64 palette
           PNGs (basecolor sRGB; metallic R + smoothness A; emission) sampled with point filtering.
           Fire delta = faceted flame tongues on the tabards and pauldron tiers (palette swatch 3).
           Earlier pass (21.5k tris, 2k baked set) superseded. Game camera: third person, distance 12 m,
           pitch 35°, vertical FOV 60° -> subject ≈ 180 px tall at 1080p. Gate gameplay size 180 px.
```

PHASE 6 RESULT (rig)
           CH_LongVuong_Rig: 22 bones named after Unity Humanoid (Hips … Head, Left/Right Shoulder, UpperArm,
           LowerArm, Hand, UpperLeg, LowerLeg, Foot, Toes); rest pose = modelled A-pose (arms ~52° down).
           Rigid skin: every vertex 1 bone, weight 1.0 (LOD0 5,109 / LOD1 2,816 verts). Sockets:
           SOCKET_Muzzle (LeftHand, left palm), SOCKET_Overhead (root, z 2.70), SOCKET_AuraAnchor (root, z 0.02).
           Pose sheet review/06_rig/pose_sheet.png: arms up, cast forward, stride, crouch.
           Known limits: robe, tabards and hem fur ride Hips rigidly, so legs pass through the robe in
           stride and crouch; heroes are placed and never walk (S4), clips are Idle / Attack / Spawn, so
           this is accepted. Pauldrons ride the Shoulder bones: an arm raised past ~90° slides under them.
