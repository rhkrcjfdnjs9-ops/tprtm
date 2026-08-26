# Grania Unity 2D Rig Plan

Master: `grania_rig_master_v3_no_wings.png` (1230 x 1278, transparent)

Project production standard: `Assets/Characters/Grania/PRODUCTION_STANDARD.md`

Locked coordinates:

- Canvas: `1230 x 1278`
- Center X: `615`
- Ground Y: `73` pixels from bottom
- Every exported part retains the full master canvas and original pixel coordinates.
- Default part scale is always `(1, 1, 1)`.
- Wings are excluded.

## Layer order (back to front)

1. FX/HaloBack
2. FX/FloatingCrystalsBack
3. Hair/Back/Center
4. Hair/Back/Left01, Left02, Left03
5. Hair/Back/Right01, Right02, Right03
6. Costume/BackSkirt
7. Body/Pelvis
8. Leg/Left/Thigh, Calf, Foot
9. Leg/Right/Thigh, Calf, Foot
10. Body/Torso, Neck
11. Arm/Right/Upper, Lower, Hand
12. Weapon/SwordBase, SwordGlow, SwordCrystals
13. Arm/Left/Upper, Lower, Hand
14. Costume/FrontSkirtCenter
15. Costume/FrontSkirtLeft, FrontSkirtRight
16. Costume/WaistArmor, ChestArmor
17. Head/FaceBase, NoseShade
18. Face/EyeLeft/White, Iris, Pupil, UpperLid, LowerLid
19. Face/EyeRight/White, Iris, Pupil, UpperLid, LowerLid
20. Face/BrowLeft, BrowRight
21. Face/MouthNeutral, MouthSmile, MouthOpen
22. Hair/Side/Left, Right
23. Hair/Front/BangsCenter, BangsLeft, BangsRight
24. Accessory/HairFlower
25. FX/HaloFront, FloatingCrystalsFront
26. Accessory/CrystalCrown

## Bone hierarchy

Root -> Pelvis -> Torso -> Neck -> Head

- Torso -> Shoulder.R -> UpperArm.R -> LowerArm.R -> Hand.R -> Sword
- Torso -> Shoulder.L -> UpperArm.L -> LowerArm.L -> Hand.L
- Pelvis -> Thigh.R -> Calf.R -> Foot.R
- Pelvis -> Thigh.L -> Calf.L -> Foot.L
- Head -> Hair chains (front, side, back)
- Pelvis -> Skirt chains (front center, left, right, back)
- Root -> Halo -> Crown / floating crystals

## Non-negotiable constraints

- Sword remains in Grania's own right hand.
- Face, costume palette, crown, halo, and proportions match the approved master.
- Hidden shoulder, elbow, wrist, hip, and knee areas must be reconstructed before deformation.
- Face and torso use minimal mesh deformation; hair, skirt, halo, and effects take most secondary motion.
- Every exported layer keeps the same 1230 x 1278 canvas and origin.
