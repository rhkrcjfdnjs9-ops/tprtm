# Arca Character Master Specification

## Status

- Status: Locked production reference
- Character: Arca
- Element: Lightning
- Combat role: Ranged attacker
- Runtime art target: 64x64 pixel art
- Facing convention: screen-right during combat
- Production pipeline: `Character Master -> reusable 3D character/rig -> Blender animation -> PNG sequence -> deterministic ComfyUI conversion -> 64x64 cleanup -> Unity`

## Authoritative files

### 3D Character Master

- File: `Knight_3D_Master.blend`
- SHA-256: `553DF9C34EA372D0FB9BBD0E1CE251BB3F2C06457771B7A340672D7238CDFD17`
- Purpose: authoritative original 3D character, armature, proportions, mesh construction, materials, and neutral pose
- Rule: this file is the true 3D character source and must never be used directly for animation editing; every animation starts from a versioned working copy

### Design Master

- File: `Arca_Design_Master.png`
- Resolution: 1254x1254
- SHA-256: `E82F2BE1771E741FDD1C89A1B985F8D3CE73E11DBD2350A41B0AFC7C95D5CFB2`
- Purpose: authoritative reference for the 3D model, materials, costume construction, silhouette, and rig proportions

### Pixel Master

- File: `Arca_Pixel_Master.png`
- Resolution: 64x64
- SHA-256: `6ADF794AE4276676E3256C5873217AA12ECACDA8E72032F29A1DE9931B593A1A`
- Purpose: authoritative reference for final pixel simplification, outline density, palette, occupied area, and facial readability
- Alpha: binary transparency only; no semi-transparent pixels
- Palette: 20 opaque RGB colors

No Master may be overwritten, recolored, resized, mirrored, cropped, regenerated, or used as a destructive edit target. A deliberate design revision requires a new versioned Master and explicit user approval.

## Locked identity

- SD female lightning spirit with a large head and compact body proportions
- Purple bob haircut with a prominent upward lightning-shaped forelock
- Gold lightning-shaped hair ornament
- Purple eyes and a small readable face
- Black, purple, and gold cropped combat outfit
- Purple gemstone centered on the upper chest
- Layered dark skirt with purple highlights and gold trim
- Symmetrical dark forearm guards with purple gemstones and gold trim
- Symmetrical black boots with purple gemstones and gold trim
- Long split purple cape or coat tails descending behind both sides of the body
- Dark purple outer silhouette with bright purple highlights and restrained gold accents

## Allowed animation changes

- Arm and hand pose
- Leg and foot pose
- Head angle
- Torso lean and rotation
- Hair, forelock, skirt, and cape secondary motion
- Facial expression when required by the motion
- Temporary perspective shortening caused by a verified pose

## Forbidden animation changes

- Face identity, eye color, hairstyle, or hair ornament design
- Costume construction, gemstone placement, trim pattern, or boot design
- Body and head proportion
- Base palette or outline language
- Addition or removal of equipment
- Frame-specific redesign of hands, legs, cape, or costume
- Character scale changes between frames
- Mirroring used as a substitute for a correctly authored pose
- Per-animation ImageGen regeneration as a production source

## Pixel palette reference

The Pixel Master palette is authoritative. The most frequent colors are recorded below for automated comparison; this list does not authorize introducing new colors.

```text
#7444B0  primary purple
#17131F  deepest outline
#3B2066  dark purple
#54308F  middle purple
#FFE7D2  light skin
#2A1745  purple-black
#292435  costume dark
#D79A17  gold
#C080FF  bright purple
#1A1029  darkest purple
#9B5BDB  purple highlight
#8F5B09  dark gold
#F4AE8C  skin shadow
#FFD0B3  skin midtone
#A95145  facial accent
#D98268  skin accent
```

## Blender production requirements

- Use `Knight_3D_Master.blend` as the authoritative reusable 3D character and rig.
- Never animate directly inside `Knight_3D_Master.blend`; create a working copy for each animation.
- Keep the default model in a neutral front-facing pose with full body visible.
- Preserve bilateral limb structure so the rig can be validated clearly.
- Separate hair, forelock, cape tails, skirt layers, and rigid ornaments where secondary motion requires it.
- Create and reuse one armature for every Arca animation.
- Keep model scale, camera, orthographic framing, lighting, and render resolution fixed across animations.
- Fixed capture camera: `ArcaRenderCamera`, Orthographic, location `(0, -12, 3)`, rotation `(1.545801, 0, 0)`, Orthographic Scale `6.5`.
- Keep `Arca_ModelRoot` and `ArcaArmature` at location `(0,0,0)`, rotation `(0,0,0)`, and scale `(1,1,1)` in every animation file.
- Character screen size, screen position, and front-facing capture direction must remain identical across every motion. Animate pose bones only; never animate the camera, model root, armature object transform, or orthographic scale.
- Render animation as transparent PNG sequences at an integer-multiple working resolution, with 512x512 as the initial target.
- Do not render final production frames directly at 64x64.

## 64x64 delivery requirements

- Canvas: exactly 64x64 pixels per frame
- PPU: 32
- Pivot: Bottom Center, identical across all frames
- Filtering: Point
- Compression: None
- Mipmaps: Off
- Wrap mode: Clamp
- Resize: Nearest Neighbor or Nearest Exact only
- Transparency: binary alpha unless an approved effect specifically requires otherwise
- Character scale, ground line, horizontal reference, palette, and outline density must remain consistent
- ImageGen and generative ComfyUI outputs are reference material until deterministic normalization and quality-gate approval

## Required motion set

```text
Idle       4-6 frames, 6-8 FPS
FloatMove  6-8 frames, 8-12 FPS
Attack     6-10 frames, 10-15 FPS
Hit        2-4 frames
Death      6-10 frames with a held final pose
```

Arca uses `FloatMove` instead of a ground-based `Walk` as her normal travel animation.

## Quality gate

- Character identity matches both Masters
- Face, hair, costume, ornaments, cape, and boots remain recognizable
- No added, missing, or duplicated limbs
- No frame-specific scale distortion
- All final frames are exactly 64x64
- Common pivot and reference position are preserved
- Pixel Master palette and outline style are preserved
- No antialiasing, blur, semi-transparent edge noise, or broken outlines
- Sprite-sheet cells are exactly 64x64
- Unity import settings match this specification
- Animation loop and Animator transitions are verified in Unity
