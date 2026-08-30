import bpy
import math
from pathlib import Path
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ARCA_ROOT = PROJECT_ROOT / "Assets" / "Characters" / "Arca"
TEST_ROOT = ARCA_ROOT / "Blender" / "DeformationTests"
TEST_BLEND = ARCA_ROOT / "Blender" / "Arca_DeformationTest.blend"


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_bone(armature, name, head, tail, parent=None):
    bone = armature.edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    if parent:
        bone.parent = armature.edit_bones[parent]
    return bone


def create_armature():
    armature_data = bpy.data.armatures.new("ArcaTestArmatureData")
    armature = bpy.data.objects.new("ArcaTestArmature", armature_data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    add_bone(armature_data, "Root", (0, 0, 0.20), (0, 0, 1.00))
    add_bone(armature_data, "Pelvis", (0, 0, 1.00), (0, 0, 2.15), "Root")
    add_bone(armature_data, "Spine", (0, 0, 2.15), (0, 0, 3.15), "Pelvis")
    add_bone(armature_data, "Neck", (0, 0, 3.15), (0, 0, 3.48), "Spine")
    add_bone(armature_data, "Head", (0, 0, 3.48), (0, 0, 4.55), "Neck")
    add_bone(armature_data, "UpperArm.L", (-0.55, 0, 2.95), (-0.91, 0, 2.28), "Spine")
    add_bone(armature_data, "Forearm.L", (-0.91, 0, 2.28), (-1.03, 0, 1.82), "UpperArm.L")
    add_bone(armature_data, "Hand.L", (-1.03, 0, 1.82), (-1.05, 0, 1.55), "Forearm.L")
    add_bone(armature_data, "UpperArm.R", (0.55, 0, 2.95), (0.91, 0, 2.28), "Spine")
    add_bone(armature_data, "Forearm.R", (0.91, 0, 2.28), (1.03, 0, 1.82), "UpperArm.R")
    add_bone(armature_data, "Hand.R", (1.03, 0, 1.82), (1.05, 0, 1.55), "Forearm.R")
    add_bone(armature_data, "Thigh.L", (-0.28, 0, 1.72), (-0.31, 0, 1.02), "Pelvis")
    add_bone(armature_data, "Shin.L", (-0.31, 0, 1.02), (-0.34, 0, 0.28), "Thigh.L")
    add_bone(armature_data, "Thigh.R", (0.28, 0, 1.72), (0.31, 0, 1.02), "Pelvis")
    add_bone(armature_data, "Shin.R", (0.31, 0, 1.02), (0.34, 0, 0.28), "Thigh.R")
    add_bone(armature_data, "Cape.L", (-0.28, 0.38, 3.10), (-1.12, 0.52, 0.82), "Spine")
    add_bone(armature_data, "Cape.R", (0.28, 0.38, 3.10), (1.12, 0.52, 0.82), "Spine")
    bpy.ops.object.mode_set(mode="OBJECT")
    return armature


def parent_to_bone(obj, armature, bone_name):
    matrix_world = obj.matrix_world.copy()
    obj.parent = armature
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_world = matrix_world


def assign_parts(armature):
    exact = {
        "HandL": "Hand.L", "HandR": "Hand.R",
        "CapeTailL": "Cape.L", "CapeTailR": "Cape.R",
        "CapeGlowL": "Cape.L", "CapeGlowR": "Cape.R",
        "CapeInnerL": "Cape.L", "CapeInnerR": "Cape.R",
        "CapeGoldL": "Cape.L", "CapeGoldR": "Cape.R",
        "ThighL": "Thigh.L", "ThighR": "Thigh.R",
    }
    prefixes = [
        (("Head", "Hair", "Bang", "Eye", "Cheek", "Mouth", "LightningForelock"), "Head"),
        (("UpperArmL",), "UpperArm.L"), (("UpperArmR",), "UpperArm.R"),
        (("ForearmGuardL", "GuardTrimL", "GuardPlateL", "WristGemL"), "Forearm.L"),
        (("ForearmGuardR", "GuardTrimR", "GuardPlateR", "WristGemR"), "Forearm.R"),
        (("BootL", "BootFootL", "BootTrimL", "BootPlateL", "BootGoldL", "BootGemL", "BootGemFrontL", "BootToeTrimL"), "Shin.L"),
        (("BootR", "BootFootR", "BootTrimR", "BootPlateR", "BootGoldR", "BootGemR", "BootGemFrontR", "BootToeTrimR"), "Shin.R"),
    ]
    for obj_name, bone_name in exact.items():
        obj = bpy.data.objects.get(obj_name)
        if obj:
            parent_to_bone(obj, armature, bone_name)
    for obj in list(bpy.data.objects):
        if obj == armature or obj.name in exact:
            continue
        for names, bone_name in prefixes:
            if obj.name.startswith(names):
                parent_to_bone(obj, armature, bone_name)
                break


def reset_pose(armature):
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0.0, 0.0, 0.0)


def render_pose(armature, name, rotations):
    reset_pose(armature)
    for bone_name, rotation in rotations.items():
        armature.pose.bones[bone_name].rotation_euler = tuple(math.radians(value) for value in rotation)
    bpy.context.view_layer.update()
    bpy.context.scene.render.filepath = str(TEST_ROOT / f"Arca_Deform_{name}.png")
    bpy.ops.render.render(write_still=True)


def main():
    TEST_ROOT.mkdir(parents=True, exist_ok=True)
    camera = bpy.data.objects["ArcaRenderCamera"]
    camera.location = (0.0, -12.0, 3.0)
    look_at(camera, (0.0, 0.0, 2.7))
    bpy.context.scene.camera = camera
    armature = create_armature()
    assign_parts(armature)
    poses = {
        "Neutral": {},
        "ArmsBent": {"Forearm.L": (0, 55, 0), "Forearm.R": (0, -55, 0)},
        "KneesBent": {"Thigh.L": (0, -12, 0), "Shin.L": (0, 42, 0), "Thigh.R": (0, 12, 0), "Shin.R": (0, -42, 0)},
        "TorsoHead": {"Spine": (0, 0, 15), "Head": (0, 0, -22)},
        "CapeSwing": {"Cape.L": (0, -18, -10), "Cape.R": (0, 18, 10)},
    }
    for pose_name, rotations in poses.items():
        render_pose(armature, pose_name, rotations)
    reset_pose(armature)
    bpy.ops.wm.save_as_mainfile(filepath=str(TEST_BLEND))
    print(f"ARCA_TEST_BLEND={TEST_BLEND}")


if __name__ == "__main__":
    main()
