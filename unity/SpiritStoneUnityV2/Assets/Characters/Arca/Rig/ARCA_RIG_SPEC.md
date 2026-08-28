# Arca Rig Specification

## Production document

- Primary layered asset: `Arca_Rig_v1.psb`
- Unity importer: `com.unity.2d.psdimporter@9.1.1`
- Unity rigging package: `com.unity.2d.animation@10.2.2`
- The PSB is generated as a genuine PSB v2 (`8BPS`, version 2), not by renaming a PSD or PNG.
- Standalone PNG files under `Parts/` are layer sources and review artifacts. The PSB is the Unity-facing layered character asset.

## Locked source

- Canvas: 1254 x 1254 pixels
- Source: `Source/Arca_RigMaster_Locked.png`
- SHA-256: `2E77D8F72C8CB59EC69C1081C3CAE6D3DA09C43A46CDE81EC73604106C571DBC`
- Background: transparent PNG
- Character direction: front view
- Character center and original canvas coordinates must remain unchanged
- AI redraw and whole-image regeneration are prohibited for rig extraction

## Layer order

1. Back cape
2. Back hair
3. Rear arm and leg sections
4. Torso and pelvis
5. Front leg sections
6. Front arm sections and hands
7. Face
8. Front hair and ornaments
9. Facial expression layers

## Required visible parts

- `Cape_Back`
- `Hair_Back`
- `Torso`
- `Pelvis`
- `Head_Base`
- `Hair_Front`
- `Hair_Ornament`
- `Arm_L_Upper`, `Arm_L_Lower`, `Hand_L`
- `Arm_R_Upper`, `Arm_R_Lower`, `Hand_R`
- `Leg_L_Upper`, `Leg_L_Lower`, `Foot_L`
- `Leg_R_Upper`, `Leg_R_Lower`, `Foot_R`
- `Eyes`, `Brows`, `Mouth`

## Joint pivots

- Neck: `(605, 515)`
- Left shoulder: `(493, 594)`
- Left elbow: `(421, 665)`
- Left wrist: `(385, 711)`
- Right shoulder: `(714, 590)`
- Right elbow: `(776, 665)`
- Right wrist: `(810, 713)`
- Left hip: `(548, 781)`
- Left knee: `(575, 910)`
- Right hip: `(667, 781)`
- Right knee: `(684, 910)`

Coordinates use top-left image space. They are initial manual anchors and must be visually verified before final mesh binding.

## Validation rule

The neutral reassembled render must be compared pixel-for-pixel against the locked master. A part is not accepted when it changes silhouette, color, position, or visible proportions outside its intended seam.

## Current PSB contents

- `Arca/Back/Cape_Back_L_Visible_v1`
- `Arca/Back/Cape_Back_R_Visible_v1`
- `Arca/__REFERENCE_MASTER_LOCKED` (hidden)

Only actually extracted layers are added. Empty placeholder layers are not treated as completed parts.
