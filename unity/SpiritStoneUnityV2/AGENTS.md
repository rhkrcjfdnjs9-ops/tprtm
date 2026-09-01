# Spirit Stone Unity Project Rules

These instructions are the project-level source of truth for Codex work in this repository.

## Response and decision contract

- Treat the current project source and committed project design documents as the Single Source of Truth.
- Before answering implementation questions, inspect the relevant existing classes, methods, data structures, scenes, and packages. Do not guess.
- Never invent a class, method, field, data structure, package, Unity API, or project convention that has not been verified in the current project or official Unity API for the installed version.
- Preserve the most recently agreed design structure recorded in the conversation or project documents. Extend that structure instead of silently replacing it.
- If prior context and current source conflict, report the concrete conflict and use the current source until the user explicitly approves a migration.
- Give one best solution optimized for this Unity 2D RPG. Do not present multiple alternatives unless the user explicitly asks for alternatives.
- For questions and implementation reports, use exactly these three concise sections:
  1. `1. 판단한 문제점`
  2. `2. 수정할 구체적 코드`
  3. `3. 선택한 이유`
- Do not add unrelated introductions, repeated explanations, or speculative advice outside those sections.

## Scope and inspection

- Inspect source with `rg --files Assets -g '*.cs' -g '*.asmdef' -g '*.asmref'` by default.
- Do not read or edit `.meta`, `.png`, `.psb`, `.prefab`, `.unity`, audio, video, or other binary/generated assets unless the task specifically requires that asset.
- Preserve existing systems and extend them with the smallest compatible change.
- Do not introduce a second manager, state system, input system, or character framework when one already exists.

## Unity 2D runtime rules

- Use only 2D physics components for 2D gameplay: `Rigidbody2D`, `Collider2D`, `Physics2D`, and their 2D APIs.
- Perform physics forces, velocity changes, and Rigidbody2D movement in `FixedUpdate`.
- Keep input collection and non-physics visual updates in `Update` or `LateUpdate` as appropriate.
- Cache frequently used component references in `Awake`, `OnEnable`, or `Start`. Do not call `GetComponent`, `Find`, or `Camera.main` repeatedly per frame.
- Prefer `[SerializeField] private` references for scene dependencies. Validate required references and fail with a clear component-prefixed error.
- Do not change runtime Transform scale to animate impact or movement unless the design explicitly requires it. Preserve character scale at `(1,1,1)`.

## Pixel art and rendering source of truth

- Apply the production-document priority in this exact order:
  1. `../../Docs/Unity_2D_Pixel_Art_Game_Project_Art_Rendering_Standard_v1.0.md`
  2. `../../Docs/Unity_64x64_Pixel_Character_Production_Specification.md`
  3. `../../Docs/AI_Character_Animation_Pipeline.md`
  4. `../../Docs/AI_Pixel_Character_Animation_Pipeline.md`
  5. Individual character and animation settings
- Treat `../../Docs/Unity_2D_Pixel_Art_Game_Project_Art_Rendering_Standard_v1.0.md` as the project-wide art and rendering source of truth.
- Treat `../../Docs/AI_Character_Animation_Pipeline.md` as the character-identity, Blender rigging, animation-render, ComfyUI conversion, and Unity-delivery pipeline source of truth.
- Treat `../../Docs/AI_Pixel_Character_Animation_Pipeline.md` as the AI motion-generation and sprite-sheet pipeline source of truth. AI output is motion reference material only until it is normalized to the 64x64 master grid and passes the document's QC gates.
- Maintain one immutable Character Master and one reusable 3D character/rig per character. Do not regenerate the character with ImageGen for each animation; ImageGen motion variants are reference drafts only and cannot become production assets directly.
- Build production motion through the verified sequence `Character Master -> reusable 3D character/rig -> Blender animation -> PNG sequence -> deterministic ComfyUI pixel conversion -> 64x64 cleanup -> sprite sheet -> Unity`.
- Validate each pipeline stage before connecting the next stage. If the new pipeline conflicts with current project assets or runtime systems, report the concrete conflict before changing them.
- Use a 16 px base grid, 32x32 main tiles, 16x16 detail tiles, and 64x64 standard character canvases.
- Use PPU 32 for every runtime pixel-art sprite. Do not assign a different PPU to an individual runtime asset.
- Use Point filtering, mipmaps off, compression none, clamp wrapping, integer/pixel-aligned placement, and Bottom Center pivots for character frames.
- Use the Pixel Perfect Camera with Assets PPU 32 and one fixed reference resolution.
- Do not treat ImageGen output or a downscaled illustration as production pixel art until it passes exact pixel-grid, alpha, palette, baseline, pivot, and import-setting validation.
- Keep every character animation frame on a 64x64 canvas with a common foot baseline and pivot. Use Unity Animator for Idle, Walk, Attack, Hit, and Death states.
- Keep runtime sprites separate from source, concept, and enlarged preview images; only runtime sprites belong in gameplay atlases.

## VFX production source of truth

- Before any VFX design, asset-generation, implementation, or revision task, read all five documents below completely.
- Apply the VFX production-document priority in this exact order:
  1. `../../Docs/VFX/VFX_MASTER_SPEC.md`
  2. `../../Docs/VFX/VFX_STYLE_GUIDE.md`
  3. `../../Docs/VFX/VFX_PALETTE.md`
  4. `../../Docs/VFX/VFX_NAMING_RULES.md`
  5. `../../Docs/VFX/VFX_SKILL_TEMPLATE.md`
  6. The approved per-skill VFX Design Plan
  7. The individual VFX asset
- Treat these documents as the Single Source of Truth for the hybrid pixel-character/high-resolution-2D-VFX production pipeline. Do not silently alter their style, palette, naming, timing, hierarchy, or performance rules.
- Create and approve a VFX Design Plan before producing a new skill VFX or materially rebuilding an existing one.
- AI and ComfyUI outputs are drafts until they pass cleanup, normalization, palette, timing, alpha, and Unity import validation. Use Aseprite for pixel cleanup and sprite-sheet assembly when available.
- The verified project uses Universal Render Pipeline 17.3.0, Shader Graph 17.3.0, `Assets/Settings/VFX/SpiritStone_URP2D.asset`, and `Assets/Settings/VFX/SpiritStone_Renderer2D.asset`.
- Before creating Shader Graph or 2D Light assets, verify that the URP pipeline and 2D Renderer assets are still connected. Restore them with `Tools/SpiritStone/VFX/Configure URP 2D` when required.
- Reuse and extend the existing `PrototypeCombatVfxSystem` pooling and feedback paths instead of creating a competing runtime VFX manager.
- Every completed VFX must validate character readability, VFX silhouette, animation timing, particle density, palette compliance, performance, Sprite/Material/Prefab structure, and visual smoothness at 60 FPS.

## Naming and code style

- Namespaces: `SpiritStone.<Feature>`.
- Types, methods, properties, and events: PascalCase.
- Private fields and local variables: camelCase.
- Serialized private fields: camelCase with `[SerializeField]`; no public mutable fields.
- Interfaces: `I` prefix. Boolean names should read as predicates such as `isAlive`, `hasTarget`, or `canAttack`.
- One primary type per file; the filename must match that type.
- Prefer sealed MonoBehaviours unless inheritance is intentionally required.

## State and managers

- Use the existing `GameManager` and `SoundManager` templates under `Assets/Scripts/Core` as extension points.
- Managers expose explicit methods and read-only state; gameplay scripts must not mutate global state fields directly.
- Persistent managers must reject duplicates and use `DontDestroyOnLoad` only when cross-scene persistence is required.

## Logging

- Every runtime log must include the class name prefix: `Debug.LogFormat("[ClassName] ...")`.
- Use `Debug.LogWarningFormat` and `Debug.LogErrorFormat` for warnings and errors.
- Do not leave per-frame logs enabled.

## Verification after code changes

1. Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/verify-unity.ps1`.
2. Refresh Unity and wait for compilation to finish.
3. Check the Unity Console for zero compiler errors.
4. For gameplay changes, run the smallest relevant scene or test.
- A generated `.csproj` build is a fast C# check, not a replacement for Unity compilation.
- Do not claim a change works until the applicable checks pass.
