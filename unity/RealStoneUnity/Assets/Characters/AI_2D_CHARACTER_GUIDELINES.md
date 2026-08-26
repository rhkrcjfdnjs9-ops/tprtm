# Unity 2D Character Asset Production Template

## 0. 목적

이 프로젝트의 2D 캐릭터는 AI 생성 이미지를 기반으로 제작하며, 최종 결과물은 Unity에서 **정렬, 조립, 애니메이션, 장비 교체, 파츠 교체가 가능한 구조**로 사용한다.

가장 중요한 원칙:

> **AI가 생성한 이미지의 개별 파츠 좌표를 신뢰하지 않는다.  
> 모든 캐릭터는 하나의 고정된 좌표계와 제작 규격을 공유한다.**

---

## 1. Master Canvas 규격

모든 캐릭터의 기준 캔버스는 동일하게 유지한다.

```text
Canvas
Width  : 512 px
Height : 512 px

Center X : 256
Ground Y : 64
```

캐릭터는 항상 다음 조건을 만족해야 한다.

- 캐릭터 전체가 512x512 내부에 존재
- 머리 위 여백 확보
- 발이 Ground Y 기준선에 맞음
- 캐릭터 중심선은 X=256
- 좌우 캐릭터 위치가 임의로 이동하지 않음
- 캐릭터 크기를 캐릭터마다 임의로 변경하지 않음

### 금지

```text
캐릭터 A → 화면 중앙
캐릭터 B → 약간 오른쪽
캐릭터 C → 크게 확대
```

개별 캐릭터마다 임의의 위치/크기 보정을 하지 않는다.

---

## 2. 기본 포즈

모든 캐릭터는 기본적으로 **정면 Neutral A-Pose**를 사용한다.

```text
        HEAD
          │
       ┌──┴──┐
      ARM   ARM
       \     /
        BODY
       /    \
     LEG    LEG
```

기본 포즈의 목적은 그림의 아름다움보다 **파츠 교체와 애니메이션 호환성**이다.

캐릭터마다 기본 포즈를 다르게 만들지 않는다.

---

## 3. Body Proportion Lock

다음 신체 비율은 프로젝트 전체에서 통일한다.

- Head
- Body
- Arm
- Hand
- Leg
- Foot

캐릭터의 성별, 직업, 의상, 헤어스타일이 달라져도 기본적인 관절 위치와 신체 비율은 유지한다.

특히 다음 위치는 변경하지 않는다.

```text
Neck
Shoulder_L
Shoulder_R

Elbow_L
Elbow_R

Wrist_L
Wrist_R

Hip_L
Hip_R

Knee_L
Knee_R

Ankle_L
Ankle_R
```

---

## 4. Pivot 규칙

Unity에서 회전하는 모든 파츠는 **실제 관절 위치를 Pivot으로 사용한다.**

| 파츠 | Pivot |
|---|---|
| Head | Neck |
| Arm_L | Shoulder_L |
| Arm_R | Shoulder_R |
| Forearm_L | Elbow_L |
| Forearm_R | Elbow_R |
| Hand_L | Wrist_L |
| Hand_R | Wrist_R |
| Leg_L | Hip_L |
| Leg_R | Hip_R |
| Foot_L | Ankle_L |
| Foot_R | Ankle_R |

### 금지

이미지의 중앙을 자동으로 Pivot으로 사용하지 않는다.

```text
X WRONG

┌────────────┐
│            │
│     ●      │ ← 이미지 중앙
│            │
└────────────┘
```

### 올바른 방식

```text
O CORRECT

      ● ← 실제 관절
      │
      │
      │
      │
```

---

## 5. Sprite Canvas 규칙

파츠를 분리하더라도 **원본 좌표계를 유지한다.**

예:

```text
head.png
body.png
arm_l.png
arm_r.png
leg_l.png
leg_r.png
```

모든 파일은 동일한 Master Canvas 기준으로 제작한다.

```text
512x512
```

따라서 파츠가 실제로 작은 영역만 차지하더라도 이미지 크기를 임의로 잘라내지 않는다.

### 잘못된 방식

```text
head.png  = 120x130
arm.png   = 80x200
body.png  = 180x220
```

### 올바른 방식

```text
head.png  = 512x512
arm.png   = 512x512
body.png  = 512x512
```

각 이미지 안에서 필요 없는 영역은 Transparent 처리한다.

이 규칙을 사용하면 모든 파츠가 동일한 좌표계를 공유하므로 Unity 조립 시 위치 보정이 최소화된다.

---

## 6. Character Hierarchy

Unity Hierarchy는 기본적으로 다음 구조를 따른다.

```text
CharacterRoot
│
├── Body
│
├── Head
│
├── Arm_L
│   ├── Forearm_L
│   └── Hand_L
│
├── Arm_R
│   ├── Forearm_R
│   └── Hand_R
│
├── Leg_L
│   └── Foot_L
│
├── Leg_R
│   └── Foot_R
│
├── Equipment
│   ├── Weapon
│   ├── Hair
│   ├── Hat
│   └── Accessory
│
└── Effects
```

---

## 7. Transform 규칙

모든 파츠는 `CharacterRoot` 기준의 Local Transform을 사용한다.

```text
CharacterRoot
Position = 0,0,0
Rotation = 0,0,0
Scale    = 1,1,1
```

각 파츠 역시 기본적으로 다음을 유지한다.

```text
Scale = 1,1,1
```

캐릭터 위치 이동은 반드시 `CharacterRoot`에서 처리한다.

파츠마다 World Position을 직접 수정하지 않는다.

---

## 8. Sorting Order 규칙

기본적인 앞/뒤 관계는 다음 순서를 따른다.

```text
Back Layer
    ↓
Back Hair
    ↓
Body
    ↓
Leg
    ↓
Arm
    ↓
Head
    ↓
Front Hair
    ↓
Equipment
    ↓
Effect
```

구체적인 Sorting Order는 프로젝트 공통 규칙을 사용하며 캐릭터마다 임의로 변경하지 않는다.

---

## 9. AI Image Generation Rules

AI에게 캐릭터 이미지를 생성할 때 다음 조건은 항상 유지한다.

```text
2D game character
front-facing
full body
centered
neutral A-pose
consistent body proportions
consistent head size
full body visible
no cropping
transparent background
clean silhouette
game asset
fixed composition
fixed character scale
```

### 캐릭터마다 변경 가능한 요소

```text
Hair
Hair Color
Face
Skin Tone
Clothing
Armor
Weapon
Accessories
Character Class
Character Theme
Color Palette
```

### 변경하면 안 되는 요소

```text
Camera
Character Scale
Character Position
Canvas Composition
Ground Position
Body Proportion
Joint Position
Pose
```

---

## 10. Character Generation Prompt Template

AI 이미지 생성 시 다음 템플릿을 사용한다.

```text
[PROJECT STANDARD]

2D game character asset for Unity.

Canvas composition:
- 512x512 canvas
- character centered horizontally
- character center aligned to X=256
- feet aligned to Ground Y=64
- full body visible
- no cropping
- fixed character scale
- front-facing
- neutral A-pose
- consistent body proportions
- consistent head size
- clean silhouette
- transparent background

Character:
[CHARACTER DESCRIPTION]

Hair:
[HAIR DESCRIPTION]

Face:
[FACE DESCRIPTION]

Clothing:
[CLOTHING DESCRIPTION]

Equipment:
[EQUIPMENT DESCRIPTION]

Color palette:
[COLOR DESCRIPTION]

Style:
[ART STYLE DESCRIPTION]

IMPORTANT:
Do not change camera angle.
Do not change character scale.
Do not change character position.
Do not change body proportions.
Do not crop any body part.
Keep the character aligned to the project standard.
```

---

## 11. Character Data Template

각 캐릭터는 다음 정보를 가진다.

```yaml
character:
  id: character_001
  name: ExampleCharacter

  canvas:
    width: 512
    height: 512

  alignment:
    center_x: 256
    ground_y: 64

  pose:
    type: neutral_a_pose

  body:
    head_scale: standard
    body_scale: standard
    proportions: standard

  appearance:
    hair:
    face:
    clothing:
    armor:
    accessories:

  equipment:
    weapon:
    shield:
    accessory:

  parts:
    head:
    body:
    arm_l:
    arm_r:
    leg_l:
    leg_r:
```

---

## 12. 파일 구조

프로젝트 내 캐릭터 리소스는 다음 구조를 따른다.

```text
Assets/
└── Characters/
    └── Character_001/
        ├── Master/
        │   └── Character_001_Master.png
        │
        ├── Parts/
        │   ├── Head.png
        │   ├── Body.png
        │   ├── Arm_L.png
        │   ├── Arm_R.png
        │   ├── Leg_L.png
        │   └── Leg_R.png
        │
        ├── Equipment/
        │   ├── Weapon/
        │   ├── Armor/
        │   └── Accessories/
        │
        ├── Animations/
        │
        └── Data/
            └── Character_001.yaml
```

---

## 13. Codex 작업 규칙

Codex가 캐릭터 관련 파일을 생성하거나 수정할 때 반드시 다음 순서를 따른다.

```text
1. 기존 Character Template 확인
2. Master Canvas 규격 확인
3. CharacterRoot 기준 확인
4. Joint/Pivot 기준 확인
5. 기존 캐릭터와 비율 비교
6. 파일명 규칙 확인
7. 새로운 파일 생성
8. Unity Import 설정 확인
9. Transform / Pivot / Sorting 규칙 확인
10. 기존 캐릭터의 좌표계를 임의로 변경하지 않음
```

기존 캐릭터의 좌표를 수정해야 하는 경우에는 먼저 **왜 수정해야 하는지 확인**한다.

---

## 14. 절대 금지

```text
❌ 캐릭터마다 다른 캔버스 크기 사용
❌ AI 결과물을 그대로 개별 파츠로 잘라 좌표를 새로 계산
❌ World Position으로 파츠 배치
❌ 파츠마다 Scale을 임의 변경
❌ 파츠 중앙을 무조건 Pivot으로 설정
❌ 캐릭터마다 다른 기본 포즈 사용
❌ 발 위치를 캐릭터마다 다르게 설정
❌ 기존 캐릭터의 기준 좌표를 임의 변경
❌ 캐릭터마다 Unity Inspector 값을 수동으로 다르게 설정
```

---

## 15. 최종 목표

이 프로젝트의 캐릭터 제작 시스템은 다음 구조를 목표로 한다.

```text
                 AI
                  │
                  ▼
          Master Character
                  │
                  ▼
          Fixed Canvas 512²
                  │
                  ▼
             Part Split
                  │
                  ▼
       Same Coordinate System
                  │
                  ▼
              Unity
                  │
                  ▼
          CharacterRoot
                  │
        ┌─────────┼─────────┐
        ▼         ▼         ▼
      Body      Head      Limbs
        │         │         │
        └─────────┼─────────┘
                  ▼
              Animation
                  │
                  ▼
        Equipment / Skin Swap
```

### 핵심 철학

> **그림은 AI가 자유롭게 만들되, 좌표계는 자유롭게 만들게 하지 않는다.**

캐릭터가 10개, 50개, 100개로 늘어나더라도 동일한 규격을 유지하여 Unity에서 재사용 가능한 에셋 구조를 만든다.
