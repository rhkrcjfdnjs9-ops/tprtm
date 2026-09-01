# VFX Skill Template

## 1. VFX Skill Request

```text
Owner:
Skill ID:
Display Name:
Element:
Skill Type: Basic / Skill / Ultimate / Status
Target: Single / Area / Self / Team
Gameplay Hit Time:
Damage Count:
Status Effect:
Character Motion:
Required Phases:
Reference Assets:
Special Restrictions:
```

필수 정보가 없으면 현재 게임 데이터에서 확인한다. 데이터에도 없는 값만 사용자에게 질문한다.

## 2. VFX Design Plan

```markdown
# VFX_PLAN_<Owner>_<SkillId>

## Gameplay Contract
- Damage event:
- Status event:
- Target count:
- Runtime duration:

## Silhouette
- Large silhouette:
- Medium energy:
- Small fragments:
- Direction of force:

## Timeline
| Phase | Time | Keyframe | Gameplay event |
|---|---:|---|---|
| Anticipation | | | |
| Release | | | |
| Travel | | | |
| Impact | | | |
| Decay | | | |

## Technology
- Sprite Animation:
- Particle System:
- Material/Shader:
- Trail:
- Transform Animation:
- Camera Shake:
- Hit Stop:
- Pool Key:

## Asset List
- Sprite:
- Sprite Sheet:
- Material:
- Animation:
- Prefab:

## Performance Budget
- Max particles:
- Max active instances:
- Expected overdraw:
```

Design Plan 승인 전에는 기존 Production 에셋을 덮어쓰지 않는다.

## 3. 에셋 제작 체크리스트

- [ ] `VFX_MASTER_SPEC.md` 확인
- [ ] `VFX_STYLE_GUIDE.md` 확인
- [ ] `VFX_PALETTE.md` 확인
- [ ] `VFX_NAMING_RULES.md` 확인
- [ ] 시작/최대 타격/종료 키프레임 확정
- [ ] 공통 캔버스와 Pivot 고정
- [ ] AI 출력 프레임 흔들림 정규화
- [ ] Aseprite 또는 동등한 방식으로 cleanup
- [ ] Runtime과 Source 분리

## 4. Unity 자동화 체크리스트

Unity MCP를 사용할 수 있으면 다음 순서로 자동화한다.

1. Asset Import 및 Import Setting 적용
2. 필요한 Material 생성
3. Particle System/Trail 설정
4. Animation Clip 생성
5. Prefab 조립
6. 기존 Object Pool 등록
7. VFX 테스트 씬 또는 기존 최소 전투 씬에 배치
8. Unity Console 오류 확인

Shader Graph 및 2D Light를 사용할 때는 `SpiritStone_URP2D.asset`과 `SpiritStone_Renderer2D.asset`의 연결 상태를 먼저 검증한다. 연결이 끊겼으면 에셋을 생성하지 말고 `Tools/SpiritStone/VFX/Configure URP 2D`로 복구한 뒤 진행한다.

## 5. 완료 보고서

```text
Character readability: PASS/FAIL
VFX silhouette: PASS/FAIL
Animation timing: PASS/FAIL
Particle density: PASS/FAIL
Palette: PASS/FAIL
Performance: PASS/FAIL
Sprite/Material/Prefab structure: PASS/FAIL
60 FPS smoothness: PASS/FAIL
Unity Console errors: 0
Known limitations:
```

하나라도 FAIL이면 완료로 보고하지 않는다.
