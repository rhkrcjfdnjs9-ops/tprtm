# VFX_PLAN_Arca_LightningOrb

## Gameplay Contract

- Owner: Arca
- Display Name: 라이트닝 오브
- Element: Lightning
- Type: Ranged AOE
- Damage event: 투사체가 적 중심에 도달하고 백색 플래시가 발생하는 순간
- Status event: 적 전체 감전 1.5초
- Target count: 현재 살아 있는 적 전체
- Runtime duration: 0.80초

## Silhouette

- Large silhouette: 압축된 구체에서 360도로 뻗는 굵고 비대칭인 방사형 번개
- Medium energy: 보라색 구체 몸체와 충전 중 코어에서 중심으로 모이는 전류
- Small fragments: 충돌 후 바깥으로 튀는 6개 이하의 전기 파편
- Direction of force: 세 코어 → 중앙 구체 → 적 중심 → 방사형 외부

## Timeline

| Phase | Time | Keyframe | Gameplay event |
|---|---:|---|---|
| Gather | 0.00~0.24초 | 세 코어 충전, 중앙 구체 형성 | 없음 |
| Release/Travel | 0.24~0.48초 | 작은 아크를 동반한 구체 이동 | 없음 |
| Compression | 0.48~0.52초 | 적 중심에서 구체 급격히 축소 | 없음 |
| Flash/Impact | 0.52~0.60초 | 백색 핵과 굵은 방사형 번개 최대 확장 | 광역 피해 및 감전 판독 지점 |
| Decay | 0.60~0.80초 | 작은 전기 파편과 잔류 전기 소멸 | 없음 |

## Technology

- Sprite Animation: Gather 6프레임, Projectile 6프레임, Impact 8프레임
- Particle System: 사용하지 않음. 기존 풀링 Sprite 파편 6개로 동일 목적을 더 낮은 비용에 구현
- Material/Shader: URP 2D Sprite Unlit 공용 번개 Material
- Trail: Projectile Sprite 내부의 짧은 불규칙 아크로 표현
- Transform Animation: 투사체 이동과 충돌 직전 압축
- Camera Shake: 0.06초, 강도 0.09, 감쇠
- Hit Stop: 최대 타격 키프레임에서 0.045초
- Pool Key: `Vfx_Arca_LightningOrb_<Phase>`

## Asset List

- Sprite: `Assets/VFX/Resources/VFX/Lightning/LightningOrb/<Phase>/spr_vfx_arca_lightningorb_<phase>_<nn>.png`
- Material: `Assets/VFX/Resources/VFX/Materials/mat_vfx_lightning_sprite_unlit.mat`
- Runtime: 기존 `PrototypeCombatVfxSystem` 확장
- Generator: `Assets/Editor/VFX/ArcaLightningOrbVfxGenerator.cs`

## Performance Budget

- Max sprite fragments: 6
- Max active instances during impact: 8 이하
- Max active major VFX: 1
- Expected overdraw: 캐릭터 한 명 크기보다 작은 국부 영역
- Runtime Instantiate/Destroy: 금지, 기존 풀 사용

## Palette

- Outline: `#180326`, `#2B0644`
- Body: `#7D1AC4`, `#9830DF`, `#B84DF2`
- Bright: `#D26CFF`, `#E798FF`, `#F2C2FF`
- Peak: `#FFFFFF` — 발사 핵과 최대 충돌 프레임에서만 사용

## Acceptance Gates

1. 정지 프레임에서도 구체와 방사형 번개를 구분할 수 있다.
2. 백색 최고광이 캐릭터와 적 실루엣을 장시간 덮지 않는다.
3. Gather → Travel → Compression → Impact → Decay 순서가 0.8초 안에 읽힌다.
4. 작은 파편이 주 번개보다 강하지 않다.
5. Lightning/Arca 팔레트 이외 색을 사용하지 않는다.
6. Android 세로 화면에서 주요 동시 인스턴스 8개 이하를 유지한다.
7. Sprite/Material/Runtime 이름이 VFX 명명 규칙을 따른다.
8. 60 FPS에서 위치 흔들림이나 프레임 누락이 없다.
