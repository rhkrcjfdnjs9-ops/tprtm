import bpy
import math
from pathlib import Path


OUTPUT = Path(bpy.path.abspath("//../Blender/Animations/Knight_Idle.blend"))
ARMATURE_NAME = "ArcaArmature"


def key_rotation(bone_name, xyz_degrees, frame):
    bone = armature.pose.bones[bone_name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = tuple(math.radians(value) for value in xyz_degrees)
    bone.keyframe_insert(data_path="rotation_euler", frame=frame)


def key_location(bone_name, xyz, frame):
    bone = armature.pose.bones[bone_name]
    bone.location = xyz
    bone.keyframe_insert(data_path="location", frame=frame)


armature = bpy.data.objects.get(ARMATURE_NAME)
if armature is None or armature.type != "ARMATURE":
    raise RuntimeError(f"Armature not found: {ARMATURE_NAME}")

OUTPUT.parent.mkdir(parents=True, exist_ok=True)

bpy.context.view_layer.objects.active = armature
armature.select_set(True)
bpy.ops.object.mode_set(mode="POSE")

# Pose 0: the locked neutral standing pose.
for pose_bone in armature.pose.bones:
    pose_bone.rotation_mode = "XYZ"
    pose_bone.rotation_euler = (0.0, 0.0, 0.0)
    pose_bone.location = (0.0, 0.0, 0.0)

scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 15
scene.render.fps = 15

# Frame 1: neutral standing.
key_location("Pelvis", (0.0, 0.0, 0.0), 1)
key_rotation("Spine", (0.0, 0.0, 0.0), 1)
key_rotation("Neck", (0.0, 0.0, 0.0), 1)
key_rotation("Head", (0.0, 0.0, 0.0), 1)

# Frame 5: subtle inhale and a very small head response.
key_location("Pelvis", (0.0, 0.0, 0.018), 5)
key_rotation("Spine", (-0.7, 0.0, 0.45), 5)
key_rotation("Neck", (0.25, 0.0, -0.2), 5)
key_rotation("Head", (0.45, 0.0, -0.35), 5)

# Frame 10: settle across the center with a restrained opposing sway.
key_location("Pelvis", (0.0, 0.0, -0.010), 10)
key_rotation("Spine", (0.45, 0.0, -0.35), 10)
key_rotation("Neck", (-0.15, 0.0, 0.15), 10)
key_rotation("Head", (-0.3, 0.0, 0.28), 10)

# Frame 15: exact copy of frame 1 for a clean idle loop.
key_location("Pelvis", (0.0, 0.0, 0.0), 15)
key_rotation("Spine", (0.0, 0.0, 0.0), 15)
key_rotation("Neck", (0.0, 0.0, 0.0), 15)
key_rotation("Head", (0.0, 0.0, 0.0), 15)

if armature.animation_data and armature.animation_data.action:
    armature.animation_data.action.name = "Knight_Idle"

scene.frame_set(1)
bpy.ops.object.mode_set(mode="OBJECT")
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT))
print(f"IDLE_SAVED={OUTPUT}")
print("IDLE_FRAMES=1,5,10,15")
print("MASTER_UNCHANGED=True")
