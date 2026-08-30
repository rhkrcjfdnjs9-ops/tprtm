import bpy
import math
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ARCA_ROOT = PROJECT_ROOT / "Assets" / "Characters" / "Arca"
IDLE_ROOT = ARCA_ROOT / "Animations" / "Idle"
SOURCE_ROOT = IDLE_ROOT / "SourceFrames"
IDLE_BLEND = ARCA_ROOT / "Blender" / "Arca_Animation_Idle.blend"


def set_rotation(bone, degrees):
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = tuple(math.radians(value) for value in degrees)


def key_pose(armature, frame, pelvis_z, head_z, forelock_y, hair_swing, cape_swing):
    scene = bpy.context.scene
    scene.frame_set(frame)
    pelvis = armature.pose.bones["Pelvis"]
    pelvis.location = (0.0, 0.0, pelvis_z)
    pelvis.keyframe_insert("location", frame=frame)

    head = armature.pose.bones["Head"]
    set_rotation(head, (0.0, 0.0, head_z))
    head.keyframe_insert("rotation_euler", frame=frame)

    forelock = armature.pose.bones["Forelock"]
    set_rotation(forelock, (0.0, forelock_y, 0.0))
    forelock.keyframe_insert("rotation_euler", frame=frame)

    for side, sign in (("L", -1.0), ("R", 1.0)):
        hair = armature.pose.bones[f"HairSide.{side}"]
        set_rotation(hair, (0.0, sign * hair_swing, sign * hair_swing * 0.35))
        hair.keyframe_insert("rotation_euler", frame=frame)
        cape = armature.pose.bones[f"Cape.{side}"]
        set_rotation(cape, (0.0, sign * cape_swing, sign * cape_swing * 0.30))
        cape.keyframe_insert("rotation_euler", frame=frame)
        cape_tip = armature.pose.bones[f"CapeTip.{side}"]
        set_rotation(cape_tip, (0.0, sign * cape_swing * 1.35, sign * cape_swing * 0.45))
        cape_tip.keyframe_insert("rotation_euler", frame=frame)


def main():
    SOURCE_ROOT.mkdir(parents=True, exist_ok=True)
    armature = bpy.data.objects["ArcaArmature"]
    animation = bpy.data.actions.new("Arca_Idle")
    armature.animation_data_create()
    armature.animation_data.action = animation
    key_pose(armature, 1, 0.000, 0.0, 0.0, 0.0, 0.0)
    key_pose(armature, 2, 0.025, 0.7, 1.5, 1.0, 1.2)
    key_pose(armature, 3, 0.045, 0.0, 2.5, 1.8, 2.2)
    key_pose(armature, 4, 0.025, -0.7, 1.5, 1.0, 1.2)
    key_pose(armature, 5, 0.000, 0.0, 0.0, 0.0, 0.0)
    key_pose(armature, 6, -0.012, 0.4, -0.8, -0.6, -0.8)
    key_pose(armature, 7, 0.000, 0.0, 0.0, 0.0, 0.0)
    scene = bpy.context.scene
    scene.render.fps = 8
    scene.frame_start = 1
    scene.frame_end = 6
    for frame in range(1, 7):
        scene.frame_set(frame)
        scene.render.filepath = str(SOURCE_ROOT / f"Arca_Idle_{frame - 1:02d}.png")
        bpy.ops.render.render(write_still=True)
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(IDLE_BLEND))
    print(f"IDLE_ACTION={animation.name}")
    print(f"IDLE_BLEND={IDLE_BLEND}")
    print(f"IDLE_FRAMES={SOURCE_ROOT}")


if __name__ == "__main__":
    main()
