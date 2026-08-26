# Grania 2D Character Production Standard

This file applies `AI_2D_CHARACTER_GUIDELINES.md` to the approved Grania master.

## Locked master coordinate system

- Canvas: `1230 x 1278`
- Horizontal center: `X = 615`
- Ground baseline: `Y = 73` from the bottom
- Master: `Master/Grania_Master.png`
- Default pose and proportions: identical to the approved master
- Wings: excluded

The template's `512 x 512` values are not used for Grania because rescaling the approved master would change its established coordinates and detail. All Grania parts use the existing `1230 x 1278` coordinate system.

## Part export rules

- Every part PNG is exactly `1230 x 1278`.
- Visible pixels remain at their original master coordinates.
- Empty regions remain transparent.
- Do not crop parts to their opaque bounds.
- Do not independently resize, translate, rotate, or redesign a visible part.
- Reconstruct only the small hidden overlap required behind a joint.
- With all joint rotations at zero, recomposition must match the master.

## Unity rules

- Root object: `CharacterRoot`
- Root transform: position `0,0,0`, rotation `0,0,0`, scale `1,1,1`
- Part local scale: `1,1,1`
- Part placement uses local transforms only.
- All full-canvas sprites share one canvas origin.
- Joint parent objects provide shoulder, elbow, wrist, hip, knee, ankle, neck, hair, skirt, sword, and halo pivots.
- Filter Mode: `Bilinear` because the approved Grania is not pixel art.
- Compression: `None`
- Mip Maps: `Off`
- Alpha Is Transparency: `On`

## Required hierarchy

```text
CharacterRoot
├── Effects
│   └── Halo
├── BackHair
├── Pelvis
│   ├── Body
│   │   ├── Neck
│   │   │   └── Head
│   │   ├── Shoulder_R
│   │   │   └── Elbow_R
│   │   │       └── Wrist_R
│   │   │           └── Weapon
│   │   └── Shoulder_L
│   │       └── Elbow_L
│   │           └── Wrist_L
│   ├── Hip_R
│   │   └── Knee_R
│   │       └── Ankle_R
│   ├── Hip_L
│   │   └── Knee_L
│   │       └── Ankle_L
│   └── Skirt
├── FrontHair
└── Accessories
```

## Acceptance gate

Before animation work:

1. Set all joint rotations to zero.
2. Set all part local positions to their authored defaults.
3. Set every scale to `1,1,1`.
4. Render the recomposed character at the master resolution.
5. Compare it with `Grania_Master.png`.
6. Reject the rig if silhouette, position, color, equipment, or proportions differ visibly.

Only a rig that passes this gate may proceed to idle, movement, attack, hit, and death animation.
