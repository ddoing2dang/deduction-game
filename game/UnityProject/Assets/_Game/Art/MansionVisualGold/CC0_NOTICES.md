# Mansion Visual Gold — CC0 provenance

Provider: Poly Haven. License: CC0 1.0.
License policy: https://polyhaven.com/license
Legal code: https://creativecommons.org/publicdomain/zero/1.0/legalcode

Release: https://github.com/ddoing2dang/deduction-game/releases/tag/mansion-cc0-fullpack-v1
Archive: Mansion_CC0_FullPack.zip (203,172,241 bytes)
SHA256: 71720193c0030977b446e520bb5c3a51e1ca644219b5117645f8b7ba971d11ba

The original archive and Pass02 backup are preserved locally under Sources/ and Backup/.
Only prepared CC0 geometry and textures are included in the game assets. No paid/generated assets.

## Models used

- Arm Chair 01: https://polyhaven.com/a/ArmChair_01
- Chandelier 01: https://polyhaven.com/a/Chandelier_01
- Chandelier 02: https://polyhaven.com/a/Chandelier_02
- Gothic Cabinet 01: https://polyhaven.com/a/GothicCabinet_01
- Gothic Commode 01: https://polyhaven.com/a/GothicCommode_01
- Gothic Bed 01: https://polyhaven.com/a/GothicBed_01
- Gothic Coffee Table: https://polyhaven.com/a/gothic_coffee_table
- Dining Table: https://polyhaven.com/a/dining_table
- Classic Console 01: https://polyhaven.com/a/ClassicConsole_01
- Fancy Picture Frame 02: https://polyhaven.com/a/fancy_picture_frame_02
- Wine Barrel 01: https://polyhaven.com/a/wine_barrel_01
- Marble Bust 01: https://polyhaven.com/a/marble_bust_01
- Antique Ceramic Vase 01: https://polyhaven.com/a/antique_ceramic_vase_01
- Tea Set 01: https://polyhaven.com/a/tea_set_01
- Classic Nightstand 01: https://polyhaven.com/a/ClassicNightstand_01
- Vintage Oil Lamp: https://polyhaven.com/a/vintage_oil_lamp
- Potted Plant 01: https://polyhaven.com/a/potted_plant_01
- Potted Plant 02: https://polyhaven.com/a/potted_plant_02
- Painted Wooden Bench: https://polyhaven.com/a/painted_wooden_bench
- Lantern Chandelier 01: https://polyhaven.com/a/lantern_chandelier_01
- Painted Wooden Sofa: https://polyhaven.com/a/painted_wooden_sofa

## Surface textures

- Marble 01: https://polyhaven.com/a/marble_01
- Diagonal Parquet: https://polyhaven.com/a/diagonal_parquet
- Dark Paneled Wood: https://polyhaven.com/a/dark_paneled_wood
- Wooden Panels: https://polyhaven.com/a/wooden_panels
- Black Walnut Veneer 03: https://polyhaven.com/a/black_walnut_veneer_03
- Floral Jacquard: https://polyhaven.com/a/floral_jacquard
- Plastered Wall 04: https://polyhaven.com/a/plastered_wall_04
- Old Stone Wall 02: https://polyhaven.com/a/old_stone_wall_02
- Monastery Stone Floor: https://polyhaven.com/a/monastery_stone_floor
- Cobblestone Floor 01: https://polyhaven.com/a/cobblestone_floor_01
- Castle Brick 02 Red: https://polyhaven.com/a/castle_brick_02_red
- Raw Wooden Floor 01: https://polyhaven.com/a/raw_wooden_floor_01

## Local preparation

UV/materials retained; measured scale normalisation; selective per-asset detail budgets;
texture resolution <=2048; roughness-to-smoothness packing; plant alpha packing;
separate Blender authoring textures and Unity-ready PNGs. See asset-processing.json.

## Supplied script review

apply_mansion_visual_gold.py was reviewed but not executed: its placement coordinates
mixed Unity Y-up and Blender Z-up, centre-aligned asset origins floated above floors,
and it did not protect actual navigation anchors or check furniture intersections.
Visual Gold uses measured source bounds and current REF_/LOCK route data instead.
