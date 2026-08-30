import bpy
import math
from pathlib import Path
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ARCA_ROOT = PROJECT_ROOT / "Assets" / "Characters" / "Arca"
BLENDER_ROOT = ARCA_ROOT / "Blender"
RIG_TEST_ROOT = BLENDER_ROOT / "RigTests"
RIGGED_BLEND = BLENDER_ROOT / "Arca_Rigged.blend"


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_bone(data, name, head, tail, parent=None, deform=True):
    bone = data.edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    bone.use_deform = deform
    if parent:
        bone.parent = data.edit_bones[parent]
    return bone


def create_armature():
    data = bpy.data.armatures.new("ArcaArmatureData")
    armature = bpy.data.objects.new("ArcaArmature", data)
    bpy.context.collection.objects.link(armature)
    root = bpy.data.objects.get("Arca_ModelRoot")
    if root:
        armature.parent = root
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    add_bone(data, "Root", (0, 0, 0.15), (0, 0, 0.85), deform=False)
    add_bone(data, "Pelvis", (0, 0, 0.85), (0, 0, 2.15), "Root")
    add_bone(data, "Spine", (0, 0, 2.15), (0, 0, 3.15), "Pelvis")
    add_bone(data, "Neck", (0, 0, 3.15), (0, 0, 3.48), "Spine")
    add_bone(data, "Head", (0, 0, 3.48), (0, 0, 4.55), "Neck")
    add_bone(data, "Forelock", (0, 0, 4.70), (0, 0, 5.55), "Head")
    add_bone(data, "HairSide.L", (-0.56, 0.05, 4.55), (-0.82, 0.12, 3.70), "Head")
    add_bone(data, "HairSide.R", (0.56, 0.05, 4.55), (0.82, 0.12, 3.70), "Head")
    add_bone(data, "UpperArm.L", (-0.55, 0, 2.95), (-0.91, 0, 2.28), "Spine")
    add_bone(data, "Forearm.L", (-0.91, 0, 2.28), (-1.03, 0, 1.82), "UpperArm.L")
    add_bone(data, "Hand.L", (-1.03, 0, 1.82), (-1.05, 0, 1.55), "Forearm.L")
    add_bone(data, "UpperArm.R", (0.55, 0, 2.95), (0.91, 0, 2.28), "Spine")
    add_bone(data, "Forearm.R", (0.91, 0, 2.28), (1.03, 0, 1.82), "UpperArm.R")
    add_bone(data, "Hand.R", (1.03, 0, 1.82), (1.05, 0, 1.55), "Forearm.R")
    add_bone(data, "Thigh.L", (-0.28, 0, 1.72), (-0.31, 0, 1.02), "Pelvis")
    add_bone(data, "Shin.L", (-0.31, 0, 1.02), (-0.34, 0, 0.28), "Thigh.L")
    add_bone(data, "Thigh.R", (0.28, 0, 1.72), (0.31, 0, 1.02), "Pelvis")
    add_bone(data, "Shin.R", (0.31, 0, 1.02), (0.34, 0, 0.28), "Thigh.R")
    add_bone(data, "Cape.L", (-0.28, 0.38, 3.10), (-0.78, 0.48, 1.86), "Spine")
    add_bone(data, "CapeTip.L", (-0.78, 0.48, 1.86), (-1.24, 0.55, 0.72), "Cape.L")
    add_bone(data, "Cape.R", (0.28, 0.38, 3.10), (0.78, 0.48, 1.86), "Spine")
    add_bone(data, "CapeTip.R", (0.78, 0.48, 1.86), (1.24, 0.55, 0.72), "Cape.R")
    add_bone(data, "IK.Foot.L", (-0.34, -0.18, 0.28), (-0.34, -0.18, 0.70), "Root", deform=False)
    add_bone(data, "IK.Foot.R", (0.34, -0.18, 0.28), (0.34, -0.18, 0.70), "Root", deform=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    armature.show_in_front = True
    for side in ("L", "R"):
        constraint = armature.pose.bones[f"Shin.{side}"].constraints.new("IK")
        constraint.name = f"FootIK.{side}"
        constraint.target = armature
        constraint.subtarget = f"IK.Foot.{side}"
        constraint.chain_count = 2
    return armature


def clear_rig_data(obj):
    obj.vertex_groups.clear()
    for modifier in list(obj.modifiers):
        if modifier.type == "ARMATURE":
            obj.modifiers.remove(modifier)


def add_rigid_weight(obj, armature, bone_name):
    clear_rig_data(obj)
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("ArcaArmature", "ARMATURE")
    modifier.object = armature


def add_cape_weights(obj, armature, upper_bone, tip_bone):
    clear_rig_data(obj)
    upper = obj.vertex_groups.new(name=upper_bone)
    tip = obj.vertex_groups.new(name=tip_bone)
    z_min = min(vertex.co.z for vertex in obj.data.vertices)
    z_max = max(vertex.co.z for vertex in obj.data.vertices)
    span = max(0.001, z_max - z_min)
    for vertex in obj.data.vertices:
        upper_weight = max(0.0, min(1.0, (vertex.co.z - z_min) / span))
        tip_weight = 1.0 - upper_weight
        upper.add([vertex.index], upper_weight, "REPLACE")
        tip.add([vertex.index], tip_weight, "REPLACE")
    modifier = obj.modifiers.new("ArcaArmature", "ARMATURE")
    modifier.object = armature


def select_bone(obj_name):
    exact = {
        "CroppedTop": "Spine", "Midriff": "Spine", "Pelvis": "Pelvis",
        "HandLMesh": "Hand.L", "HandRMesh": "Hand.R",
        "ThighL": "Thigh.L", "ThighR": "Thigh.R",
    }
    if obj_name in exact:
        return exact[obj_name]
    rules = [
        (("LightningForelock",), "Forelock"),
        (("HairSideL", "HairLockL", "HairTipL", "HairNapeL"), "HairSide.L"),
        (("HairSideR", "HairLockR", "HairTipR", "HairNapeR"), "HairSide.R"),
        (("Head", "HairBack", "HairTop", "HairHighlight", "Bang", "Eye", "Cheek", "Mouth", "HairOrnament"), "Head"),
        (("UpperArmL",), "UpperArm.L"), (("UpperArmR",), "UpperArm.R"),
        (("ForearmGuardL", "GuardTrimL", "GuardPlateL", "WristGemL"), "Forearm.L"),
        (("ForearmGuardR", "GuardTrimR", "GuardPlateR", "WristGemR"), "Forearm.R"),
        (("BootL", "BootFootL", "BootTrimL", "BootPlateL", "BootGoldL", "BootGemL", "BootGemFrontL", "BootToeTrimL"), "Shin.L"),
        (("BootR", "BootFootR", "BootTrimR", "BootPlateR", "BootGoldR", "BootGemR", "BootGemFrontR", "BootToeTrimR"), "Shin.R"),
        (("CapeGlowL", "CapeInnerL", "CapeGoldL"), "Cape.L"),
        (("CapeGlowR", "CapeInnerR", "CapeGoldR"), "Cape.R"),
        (("Belt", "DiagonalBelt", "Skirt", "BackEmblem"), "Pelvis"),
        (("Chest", "Top", "Neck", "Collar", "Shoulder"), "Spine"),
    ]
    for prefixes, bone in rules:
        if obj_name.startswith(prefixes):
            return bone
    return None


def bind_meshes(armature):
    bound = 0
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        if obj.name == "CapeTailL":
            add_cape_weights(obj, armature, "Cape.L", "CapeTip.L")
            bound += 1
            continue
        if obj.name == "CapeTailR":
            add_cape_weights(obj, armature, "Cape.R", "CapeTip.R")
            bound += 1
            continue
        bone = select_bone(obj.name)
        if bone:
            add_rigid_weight(obj, armature, bone)
            bound += 1
    return bound


def reset_pose(armature):
    for bone in armature.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = (0.0, 0.0, 0.0)
        bone.location = (0.0, 0.0, 0.0)


def render_test(armature, name, pose_actions):
    reset_pose(armature)
    for bone_name, action, values in pose_actions:
        bone = armature.pose.bones[bone_name]
        if action == "rotate":
            bone.rotation_euler = tuple(math.radians(value) for value in values)
        else:
            bone.location = values
    bpy.context.view_layer.update()
    bpy.context.scene.render.filepath = str(RIG_TEST_ROOT / f"Arca_Rig_{name}.png")
    bpy.ops.render.render(write_still=True)


def main():
    RIG_TEST_ROOT.mkdir(parents=True, exist_ok=True)
    camera = bpy.data.objects["ArcaRenderCamera"]
    camera.location = (0.0, -12.0, 3.0)
    look_at(camera, (0.0, 0.0, 2.7))
    bpy.context.scene.camera = camera
    armature = create_armature()
    bound_count = bind_meshes(armature)
    render_test(armature, "Neutral", [])
    render_test(armature, "FK", [
        ("Forearm.L", "rotate", (0, 45, 0)),
        ("Forearm.R", "rotate", (0, -45, 0)),
        ("Head", "rotate", (0, 0, 12)),
    ])
    render_test(armature, "IK", [
        ("IK.Foot.L", "move", (-0.18, 0.0, 0.22)),
        ("IK.Foot.R", "move", (0.18, 0.0, 0.12)),
    ])
    render_test(armature, "Secondary", [
        ("Forelock", "rotate", (0, 10, -8)),
        ("HairSide.L", "rotate", (0, -8, -6)),
        ("HairSide.R", "rotate", (0, 8, 6)),
        ("Cape.L", "rotate", (0, -10, -5)),
        ("CapeTip.L", "rotate", (0, -14, -8)),
        ("Cape.R", "rotate", (0, 10, 5)),
        ("CapeTip.R", "rotate", (0, 14, 8)),
    ])
    reset_pose(armature)
    bpy.ops.wm.save_as_mainfile(filepath=str(RIGGED_BLEND))
    print(f"BOUND_MESHES={bound_count}")
    print(f"ARCA_RIGGED_BLEND={RIGGED_BLEND}")


if __name__ == "__main__":
    main()
