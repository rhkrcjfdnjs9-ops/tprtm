# VFX Palette

## 1. 사용 원칙

- 색은 `외곽/그림자 → 몸체 → 핵 → 최고광` 순으로 명도 위계를 만든다.
- 최고광은 화면 면적의 15% 이하를 기본값으로 한다.
- 순백색은 발사와 최대 타격 키프레임에 집중한다.
- 고해상도 보조층도 아래 속성 Hue에서 벗어나지 않는다.
- HDR Color와 Bloom은 URP 2D Material에서만 사용하며, 핵심 발광부에 제한한다.

## 2. 공통 중립색

| 역할 | Hex |
|---|---|
| Deep Outline | `#160C24` |
| Neutral Shadow | `#30263D` |
| Soft Smoke | `#70657E` |
| Pale Energy | `#F2ECFF` |
| Peak White | `#FFFFFF` |

## 3. 속성 팔레트

### Water

`#082A52` → `#105E9C` → `#27A5E8` → `#83DFFF` → `#E5FAFF`

### Fire

`#3B0710` → `#8F1618` → `#E43B1F` → `#FF8A22` → `#FFF0A6`

### Wind

`#073B36` → `#0D7568` → `#28C99E` → `#8AF2D2` → `#E9FFF8`

### Lightning / Arca

| 역할 | Hex |
|---|---|
| Void | `#180326` |
| Deep | `#2B0644` |
| Shadow | `#43086B` |
| Dark | `#60109A` |
| Purple | `#7D1AC4` |
| Mid | `#9830DF` |
| Vivid | `#B84DF2` |
| Bright | `#D26CFF` |
| Light | `#E798FF` |
| Pale | `#F2C2FF` |
| Ice | `#FAE8FF` |
| Peak | `#FFFFFF` |

### Light

`#49360A` → `#A87918` → `#E6B93F` → `#FFE68A` → `#FFFBE0`

### Dark

`#12091D` → `#2C153F` → `#54236F` → `#8746A8` → `#D1A4E5`

## 4. 상태 효과

| 상태 | 기준색 |
|---|---|
| Stun | `#B84DF2` |
| Burn | `#FF4A20` |
| Heal | `#5AF0A2` |
| Shield | `#55BFFF` |
| Attack Buff | `#FF5A42` |
| Speed Buff | `#4DFFD0` |

## 5. 금지 사항

- 한 스킬 안에서 속성 팔레트 두 개를 동일 비중으로 혼합하지 않는다.
- 순백색 외곽을 전체 수명 동안 유지하지 않는다.
- 캐릭터 대표색과 동일 명도/채도의 큰 면적을 캐릭터 바로 위에 겹치지 않는다.
- AI 출력의 임의 색상을 정규화 없이 사용하지 않는다.
