# 2D Character Production Tool

Single source of truth: `Assets/Characters/AI_2D_CHARACTER_GUIDELINES.md`

Unity menu:

- `Tools/2D Character/Character Creator`
- `Tools/2D Character/Character Validator`
- `Tools/2D Character/Project Settings`

## Creator

Creates the standard folder tree, copies a conforming 512x512 master, writes character YAML data,
creates `CharacterRoot`, builds the standard hierarchy, assigns standard-named part sprites when present,
applies sorting orders, and saves a prefab. It refuses to resize a nonconforming master automatically.

Standard part names include `Body.png`, `Head.png`, `Arm_L.png`, `Forearm_L.png`, `Hand_L.png`,
`Arm_R.png`, `Forearm_R.png`, `Hand_R.png`, `Leg_L.png`, `Foot_L.png`, `Leg_R.png`, `Foot_R.png`,
`BackHair.png`, and `FrontHair.png` in the character's `Parts` folder.

## Validator

Checks required folders, canvas size, master horizontal center, ground baseline, sprite importer settings,
prefab root/hierarchy, local transforms, and standard sorting orders. It reports existing legacy assets but
does not rewrite, resize, or delete them.

## Project Settings

Shows the locked project standard and global joint layout. It can apply safe importer settings to character
textures. Locked coordinate values are deliberately read-only and are not stored per character.
