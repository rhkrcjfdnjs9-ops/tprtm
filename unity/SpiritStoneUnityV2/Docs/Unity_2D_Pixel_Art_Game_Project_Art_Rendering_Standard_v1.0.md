# Unity 2D Pixel Art Game — Project Art & Rendering Standard v1.0

This document is the project-wide source of truth for production pixel art and rendering.

## Fixed project scale

- Base grid: 16 px
- Small/detail tile: 16x16 px
- Main tile: 32x32 px = 1 Unity world unit
- Standard character canvas: 64x64 px = 2x2 world units
- Standard character visible height: 44-58 px
- Runtime sprite PPU: 32
- Character pivot and common foot anchor: Bottom Center

## Production image rules

- Use genuine integer-grid pixel art with clear 1x1 pixels.
- Do not use anti-aliasing, blur, soft edges, sub-pixel rendering, smooth gradients, or unnecessary semi-transparent pixels.
- Use a limited reusable palette, stepped outlines, and stepped shading.
- A supplied illustration is a design reference only. Reinterpret its silhouette, main colors, hair, clothing, and equipment on the target grid; do not merely downscale it.
- ImageGen output is provisional until exact canvas, alpha, palette, density, baseline, pivot, and animation-expandability checks pass.

## Unity import and rendering rules

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single; use Multiple only for sprite sheets
- Pixels Per Unit: 32
- Filter Mode: Point
- Compression: None
- Generate Mip Maps: Off
- Wrap Mode: Clamp
- Read/Write: Off unless a verified runtime requirement exists
- Camera: Unity Pixel Perfect Camera
- Pixel Perfect Camera Assets PPU: 32
- Reference resolution: 324x576 for the current Android portrait prototype
- Place runtime sprites and move the camera on the pixel grid.

## Character animation rules

- Every standard character frame uses a 64x64 canvas and the same Bottom Center pivot and foot baseline.
- Keep apparent character scale and pixel density consistent across all frames.
- Use Unity Animator states: Idle, Walk, Attack, Hit, Death. Add states only when required by verified gameplay.
- Starting ranges: Idle 4-6 frames at 6-8 FPS; Walk 6-8 at 8-12 FPS; Attack 4-8 at 10-15 FPS; Hit 2-4; Death 4-8.

## Asset organization

- Production naming: `character_<name>_<action>_<frame>`, `enemy_<name>_<action>_<frame>`, `fx_<name>_<frame>`, `tile_<category>_<name>`.
- Separate Characters, Enemies, Environment, Effects, UI, Animations, SpriteAtlases, and Materials.
- Keep source images, concepts, and enlarged previews outside runtime atlases.
- Runtime atlas groups: Characters, Enemies, Environment, Effects, UI. Assets in an atlas must share PPU, filtering, compression, and pixel density.

## Required validation

- Exact required canvas dimensions
- No anti-aliasing or blur
- No unintended semi-transparent pixels
- Consistent pixel density and palette
- Character visible height within 44-58 px unless explicitly excepted
- Common foot baseline and Bottom Center pivot
- PPU 32, Point, mipmaps off, compression none, Clamp
- Compatible with the Pixel Perfect Camera
- Animation frames preserve canvas, pivot, baseline, and apparent scale

Bosses, large monsters, buildings, backgrounds, large effects, and special UI may use larger canvases, but must retain the same PPU 32 and pixel density.
