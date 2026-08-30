import bpy
import math
from pathlib import Path


OUTPUT = Path(bpy.path.abspath("//Arca_PoseMode_Test.blend"))
ARMATURE_NAME = "ArcaArmature"


def set_rotation(bone_name, xyz_degrees):
    bone = armature.pose.bones[bone_name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = tuple(math.radians(value) for value in xyz_degrees)
    bone.keyframe_insert(data_path="rotation_euler")


armature = bpy.data.objects.get(ARMATURE_NAME)
if armature is None or armature.type != "ARMATURE":
    raise RuntimeError(f"Armature not found: {ARMATURE_NAME}")

bpy.context.view_layer.objects.active = armature
armature.select_set(True)
bpy.ops.object.mode_set(mode="POSE")

scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 120
scene.render.fps = 24

# 1: neutral
scene.frame_set(1)
for pose_bone in armature.pose.bones:
    pose_bone.rotation_mode = "XYZ"
    pose_bone.rotation_euler = (0.0, 0.0, 0.0)
    pose_bone.keyframe_insert(data_path="rotation_euler")

# 2: rotate arms
scene.frame_set(24)
set_rotation("UpperArm.L", (0, 0, -28))
set_rotation("UpperArm.R", (0, 0, 28))
set_rotation("Forearm.L", (0, 0, -24))
set_rotation("Forearm.R", (0, 0, 24))

# 3: rotate hands while retaining the arm pose
scene.frame_set(48)
set_rotation("UpperArm.L", (0, 0, -28))
set_rotation("UpperArm.R", (0, 0, 28))
set_rotation("Forearm.L", (0, 0, -24))
set_rotation("Forearm.R", (0, 0, 24))
set_rotation("Hand.L", (0, 18, -25))
set_rotation("Hand.R", (0, -18, 25))

# 4: rotate legs while retaining the upper-body pose
scene.frame_set(72)
set_rotation("UpperArm.L", (0, 0, -28))
set_rotation("UpperArm.R", (0, 0, 28))
set_rotation("Forearm.L", (0, 0, -24))
set_rotation("Forearm.R", (0, 0, 24))
set_rotation("Hand.L", (0, 18, -25))
set_rotation("Hand.R", (0, -18, 25))
set_rotation("Thigh.L", (0, 0, 12))
set_rotation("Thigh.R", (0, 0, -12))
set_rotation("Shin.L", (0, 0, -15))
set_rotation("Shin.R", (0, 0, 15))

# 5: rotate the torso last.
scene.frame_set(96)
set_rotation("UpperArm.L", (0, 0, -28))
set_rotation("UpperArm.R", (0, 0, 28))
set_rotation("Forearm.L", (0, 0, -24))
set_rotation("Forearm.R", (0, 0, 24))
set_rotation("Hand.L", (0, 18, -25))
set_rotation("Hand.R", (0, -18, 25))
set_rotation("Thigh.L", (0, 0, 12))
set_rotation("Thigh.R", (0, 0, -12))
set_rotation("Shin.L", (0, 0, -15))
set_rotation("Shin.R", (0, 0, 15))
set_rotation("Spine", (0, 0, 14))
set_rotation("Pelvis", (0, 0, -7))

# Return to neutral so the test loops cleanly.
scene.frame_set(120)
for pose_bone in armature.pose.bones:
    pose_bone.rotation_euler = (0.0, 0.0, 0.0)
    pose_bone.keyframe_insert(data_path="rotation_euler")

if armature.animation_data and armature.animation_data.action:
    action = armature.animation_data.action
    action.name = "Arca_PoseMode_Test"

scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT))
print(f"POSE_TEST_SAVED={OUTPUT}")
print("POSE_TEST_SEQUENCE=Arm>Hand>Leg>Body")
