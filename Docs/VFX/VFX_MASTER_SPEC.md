# VFX Master Specification

## 1. 문서 지위

이 문서는 `SpiritStoneUnityV2`의 2D RPG VFX 제작에 대한 최상위 기준이다. 모든 VFX Skill Request, Design Plan, 원화, 프레임, Material, Prefab, Particle System 및 런타임 구현은 이 문서와 같은 폴더의 나머지 VFX 문서를 먼저 참조한다.

우선순위는 다음과 같다.

1. `VFX_MASTER_SPEC.md`
2. `VFX_STYLE_GUIDE.md`
3. `VFX_PALETTE.md`
4. `VFX_NAMING_RULES.md`
5. `VFX_SKILL_TEMPLATE.md`
6. 개별 VFX Design Plan
7. 개별 에셋

임의로 규격을 변경하지 않는다. 규격 변경은 문서 버전 변경과 사용자 승인을 필요로 한다.

## 2. 확인된 프로젝트 기준

- Unity: `6000.3.22f1`
- 화면: Android Portrait `1080x1920`
- 캐릭터: 64x64 픽셀 아트, PPU 32, Point, Mipmap Off, Compression None
- 기존 VFX: SpriteRenderer 프레임 애니메이션과 `PrototypeCombatVfxSystem`의 오브젝트 풀 사용
- 기존 풀: 초기 24개, `Acquire`/`Release` 방식
- 기존 보조 연출: Camera Shake, Transform Animation, Sprite Flash
- 현재 Render Pipeline: Universal Render Pipeline 17.3.0 + 2D Renderer
- 현재 Shader Graph 상태: 17.3.0 설치 및 사용 가능
- URP Pipeline Asset: `Assets/Settings/VFX/SpiritStone_URP2D.asset`
- 2D Renderer Data: `Assets/Settings/VFX/SpiritStone_Renderer2D.asset`
- URP Global Settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`
- 복구 메뉴: `Tools/SpiritStone/VFX/Configure URP 2D`

VFX는 URP 2D를 기본 렌더링 환경으로 사용한다. Shader Graph와 2D Light는 캐릭터 가독성과 모바일 성능 예산을 통과할 때만 사용하며, 기존 SpriteRenderer 기반 VFX와 풀링 구조를 유지한 상태에서 확장한다.

## 3. 목표 스타일

픽셀 캐릭터와 고해상도 2D VFX가 공존하는 하이브리드 스타일을 사용한다.

- 캐릭터와 상호작용 지점은 픽셀 그리드와 PPU 32를 따른다.
- 핵심 타격 실루엣은 작은 화면에서도 한 프레임 안에 읽혀야 한다.
- 고해상도 VFX는 부드러운 플라스마, 연무, 광원, 왜곡처럼 픽셀로 표현하기 어려운 보조층에 사용한다.
- 고해상도 보조층이 캐릭터 얼굴, 무기, 발 기준선 및 적의 피격 포즈를 장시간 가리지 않게 한다.
- 픽셀층과 고해상도층은 별도의 SpriteRenderer, Particle System 또는 Material로 분리한다.

## 4. 기술 선택 규칙

| 목적 | 기본 기술 |
|---|---|
| 명확한 키포즈와 타격 실루엣 | Sprite Animation / Sprite Sheet |
| 반복되는 작은 파편과 불꽃 | Particle System |
| 플라스마, 용해, 마스크, 왜곡 | Shader Graph |
| 빠른 검기, 번개 잔상 | TrailRenderer 또는 프레임 Trail Sprite |
| 이동, 압축, 반동 | Transform Animation |
| 강한 타격 보조 | Camera Shake + Hit Stop |
| 반복 생성되는 전투 VFX | Object Pooling |

하나의 효과에 모든 기술을 넣지 않는다. 핵심 실루엣을 만드는 최소 조합만 사용한다.

## 5. 표준 레이어 구조

```text
VfxRoot
├── Telegraph       # 선택 사항, 공격 예고
├── Core            # 가장 밝은 핵심 실루엣
├── Energy          # 중간 크기 에너지 몸체
├── Accent          # 번개, 검기, 충격선
├── Particles       # 작은 파편과 잔상
├── Trail           # 이동 잔상
└── Light           # URP 2D 선택 사항
```

레이어는 큰 실루엣, 중간 에너지, 작은 파편 순으로 설계한다. 작은 파편만으로 VFX를 구성하지 않는다.

## 6. 폴더 구조

```text
Assets/VFX/
├── Source/<Element>/<SkillId>/
├── Sprites/<Element>/<SkillId>/
├── SpriteSheets/<Element>/<SkillId>/
├── Materials/<Element>/
├── Shaders/
├── Prefabs/<Element>/<SkillId>/
├── Animations/<Element>/<SkillId>/
├── Data/
└── Tests/
```

AI 원본, ComfyUI 출력 및 Aseprite 작업 파일은 `Source`에 보관하고 런타임 `Resources` 또는 Atlas에 직접 넣지 않는다.

## 7. 제작 파이프라인

```text
VFX Skill Request
→ 기존 문서 확인
→ VFX Design Plan
→ 실루엣 키프레임
→ 에셋 생성/수정
→ Aseprite 정리 및 Sprite Sheet
→ Unity Import
→ Material/Animation/Prefab
→ 풀링 연결
→ 테스트 씬 검증
→ 인게임 검증
```

### AI/ComfyUI

- AI 출력은 초안이며 바로 Production 에셋이 아니다.
- 시작 프레임, 최대 타격 프레임, 종료 프레임을 고정한다.
- 프레임마다 중심, 크기, 색상, 방향이 흔들리면 정규화한다.
- 투명 배경, 공통 캔버스, 동일 Pivot을 유지한다.

### Aseprite

- 잘린 픽셀, 잔여 배경, 반투명 픽셀을 정리한다.
- 프레임 타이밍, 공통 중심, 팔레트, Sprite Sheet를 확정한다.
- 픽셀 VFX는 최종 확대/축소 보간을 사용하지 않는다.

## 8. 성능 예산

- 목표: Android 세로 화면에서 60 FPS
- 일반 공격 VFX 수명: 권장 0.10~0.45초
- 일반 스킬 VFX 수명: 권장 0.25~1.20초
- 화면에 동시에 존재하는 주요 VFX: 권장 8개 이하
- 한 효과의 Particle 최대 활성 수: 일반 32, 궁극기 96을 초기 상한으로 사용
- 전투 중 반복 Instantiate/Destroy 금지, 풀링 사용
- 매 프레임 `GetComponent`, `Find`, `Camera.main` 반복 호출 금지
- 모바일에서 불필요한 투명 오버드로우와 대형 Full Screen Particle을 피한다.

수치는 프로파일링 결과와 사용자 승인 없이 상향하지 않는다.

## 9. 완료 검증 게이트

다음 항목을 모두 통과해야 완료로 간주한다.

1. 캐릭터 가독성: 핵심 포즈와 피격 대상이 보인다.
2. VFX 실루엣: 정지 화면에서도 공격 종류를 구분할 수 있다.
3. 애니메이션 타이밍: 예고, 발사, 타격, 잔상이 논리적으로 연결된다.
4. 파티클 과밀: 작은 파편이 핵심 실루엣을 덮지 않는다.
5. 색상 규칙: `VFX_PALETTE.md`를 따른다.
6. 성능: Android 목표 환경에서 60 FPS 검증을 수행한다.
7. 구조: Sprite/Material/Prefab이 명명 및 폴더 규칙을 따른다.
8. 부드러움: 60 FPS 재생에서 프레임 누락과 위치 흔들림이 없다.

추가로 Unity Console 컴파일 오류 0건, 누락 Sprite/Material 0건, 비활성 풀 반환 여부를 확인한다.
