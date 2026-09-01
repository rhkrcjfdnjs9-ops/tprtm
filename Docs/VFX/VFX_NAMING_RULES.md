# VFX Naming Rules

## 1. 공통 형식

영문 소문자 폴더와 PascalCase Unity 에셋 이름을 사용한다. Skill ID는 데이터에 정의된 안정적인 영문 식별자를 사용한다.

```text
Vfx_<Owner>_<SkillId>_<Phase>_<Variant>
```

Phase 목록:

- `Telegraph`
- `Gather`
- `Cast`
- `Muzzle`
- `Projectile`
- `Trail`
- `Impact`
- `Area`
- `Status`
- `Decay`

## 2. 파일 이름

| 에셋 | 형식 | 예시 |
|---|---|---|
| Sprite Frame | `spr_vfx_<owner>_<skill>_<phase>_<nn>.png` | `spr_vfx_arca_lightningorb_impact_03.png` |
| Sprite Sheet | `ss_vfx_<owner>_<skill>_<phase>.png` | `ss_vfx_arca_lightningorb_impact.png` |
| Material | `mat_vfx_<element>_<purpose>.mat` | `mat_vfx_lightning_additive.mat` |
| Shader Graph | `sg_vfx_<purpose>.shadergraph` | `sg_vfx_plasma_dissolve.shadergraph` |
| Animation Clip | `anim_vfx_<owner>_<skill>_<phase>.anim` | `anim_vfx_arca_lightningorb_impact.anim` |
| Prefab | `pf_vfx_<owner>_<skill>_<phase>.prefab` | `pf_vfx_arca_lightningorb_projectile.prefab` |
| Data | `data_vfx_<owner>_<skill>.asset` | `data_vfx_arca_lightningorb.asset` |
| Design Plan | `VFX_PLAN_<Owner>_<SkillId>.md` | `VFX_PLAN_Arca_LightningOrb.md` |

프레임 번호는 0부터 시작하고 두 자리 숫자를 사용한다. 100프레임 이상은 세 자리를 사용한다.

## 3. GameObject 계층 이름

```text
Vfx_Arca_LightningOrb
├── Core
├── Energy
├── Accent
├── Particles
├── Trail
└── Light
```

런타임 풀 키와 Prefab 이름은 동일한 Skill ID와 Phase를 공유한다. 임의 문자열 풀 키를 추가하지 않는다.

## 4. 버전 관리

- Production 파일명에는 `final`, `new`, `latest`를 사용하지 않는다.
- Draft만 `_v01`, `_v02` 버전을 사용할 수 있다.
- 승인된 에셋은 안정적인 파일명으로 승격하고 `.meta` GUID를 유지한다.
- 폐기 초안은 Runtime/Resources 폴더에 남기지 않는다.
