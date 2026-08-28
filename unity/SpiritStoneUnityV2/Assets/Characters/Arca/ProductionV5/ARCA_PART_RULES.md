# Arca Production V5 — Locked Part Rules

- Design reference: `Arca_AnimationMaster_V3_Transparent.png`
- Canvas: 1254 x 1254 pixels, transparent RGBA
- Character center: X = 627
- Coordinate system: original V3 canvas coordinates; no per-part cropping
- Transform on import: position `(0,0,0)`, rotation `(0,0,0)`, scale `(1,1,1)`
- Style lock: V3 face, proportions, palette, lighting direction, line weight, costume geometry
- Ownership: each visible feature belongs to exactly one named layer
- Hidden coverage: every part must include the area normally hidden beneath adjacent parts
- Forbidden: extracting/cutting the V3 raster as the deliverable, arbitrary redesign, mirrored substitute, duplicated limbs, baked background

## Part 01 — Torso

- Anchor center: X = 627
- Vertical placement: neck base through waist, matching V3
- Includes: complete neck base, chest garment, central purple gem, complete abdomen/waist body underneath neighboring parts
- Excludes: head, hair, both arms, both hands, shoulder armor, cape, belt, skirt, legs
- Joint coverage: both shoulder sockets and waist connection must extend beneath adjacent parts
- Output: full-canvas transparent PNG named `Torso.png`
