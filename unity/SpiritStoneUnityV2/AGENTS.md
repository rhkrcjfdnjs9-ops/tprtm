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
