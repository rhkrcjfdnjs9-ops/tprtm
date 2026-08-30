import bpy


CAMERA_NAME = "ArcaRenderCamera"
ROOT_NAME = "Arca_ModelRoot"
ARMATURE_NAME = "ArcaArmature"

CAMERA_LOCATION = (0.0, -12.0, 3.0)
CAMERA_ROTATION = (1.545801, 0.0, 0.0)
CAMERA_ORTHO_SCALE = 6.5
ROOT_LOCATION = (0.0, 0.0, 0.0)
ROOT_ROTATION = (0.0, 0.0, 0.0)
ROOT_SCALE = (1.0, 1.0, 1.0)


scene = bpy.context.scene
camera = bpy.data.objects.get(CAMERA_NAME)
root = bpy.data.objects.get(ROOT_NAME)
armature = bpy.data.objects.get(ARMATURE_NAME)

if camera is None or camera.type != "CAMERA":
    raise RuntimeError(f"Fixed camera not found: {CAMERA_NAME}")
if root is None:
    raise RuntimeError(f"Character root not found: {ROOT_NAME}")
if armature is None or armature.type != "ARMATURE":
    raise RuntimeError(f"Character armature not found: {ARMATURE_NAME}")

scene.camera = camera
camera.data.type = "ORTHO"
camera.location = CAMERA_LOCATION
camera.rotation_mode = "XYZ"
camera.rotation_euler = CAMERA_ROTATION
camera.data.ortho_scale = CAMERA_ORTHO_SCALE
camera.lock_location = (True, True, True)
camera.lock_rotation = (True, True, True)
camera.lock_scale = (True, True, True)
camera.data["capture_fixed"] = True

root.location = ROOT_LOCATION
root.rotation_mode = "XYZ"
root.rotation_euler = ROOT_ROTATION
root.scale = ROOT_SCALE
root.lock_location = (True, True, True)
root.lock_rotation = (True, True, True)
root.lock_scale = (True, True, True)

armature.location = ROOT_LOCATION
armature.rotation_mode = "XYZ"
armature.rotation_euler = ROOT_ROTATION
armature.scale = ROOT_SCALE
armature.lock_location = (True, True, True)
armature.lock_rotation = (True, True, True)
armature.lock_scale = (True, True, True)
armature["capture_direction"] = "Front"
armature["capture_scale"] = "1,1,1"
armature["capture_position"] = "0,0,0"

bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print("CAPTURE_CAMERA_FIXED=True")
print("CAPTURE_CHARACTER_POSITION_FIXED=True")
print("CAPTURE_CHARACTER_SCALE_FIXED=True")
print("CAPTURE_CHARACTER_DIRECTION=Front")
