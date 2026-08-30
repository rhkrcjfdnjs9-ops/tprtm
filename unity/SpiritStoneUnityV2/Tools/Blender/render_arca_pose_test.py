import bpy
from pathlib import Path


output_dir = Path(bpy.path.abspath("//PoseTestRenders"))
output_dir.mkdir(parents=True, exist_ok=True)

scene = bpy.context.scene
scene.render.image_settings.file_format = "PNG"
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100

for frame, label in ((1, "Neutral"), (24, "Arm"), (48, "Hand"), (72, "Leg"), (96, "Body")):
    scene.frame_set(frame)
    scene.render.filepath = str(output_dir / f"Arca_Pose_{label}.png")
    bpy.ops.render.render(write_still=True)
    print(f"POSE_RENDER={label}:{scene.render.filepath}")
