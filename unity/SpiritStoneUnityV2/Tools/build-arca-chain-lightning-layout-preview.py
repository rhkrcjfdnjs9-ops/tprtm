from pathlib import Path
from random import Random

from PIL import Image, ImageDraw


project_root = Path(__file__).resolve().parents[1]
draft_root = project_root / "Assets/Characters/Arca/Pixel64/Drafts/Effects/ChainLightningLayoutV1"
draft_root.mkdir(parents=True, exist_ok=True)
core_path = project_root / (
    "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/ThunderCore/"
    "IdleRotateV2/Arca_ThunderCore_IdleRotate_V2_00.png"
)
preview_path = draft_root / "Arca_ChainLightning_Layout_Draft_v1_4x.gif"

canvas_size = (256, 128)
core_positions = [(38, 20), (14, 88), (62, 88)]
center = (38, 61)
targets = [(130, 34), (178, 64), (226, 92)]
palette = {
    "outline": (48, 10, 82, 255),
    "purple": (164, 42, 255, 255),
    "light": (224, 148, 255, 255),
    "white": (255, 255, 255, 255),
    "enemy": (116, 38, 46, 255),
}


def jagged_points(start, end, seed, segments=8, spread=4):
    rng = Random(seed)
    points = [start]
    dx = end[0] - start[0]
    dy = end[1] - start[1]
    length = max(1.0, (dx * dx + dy * dy) ** 0.5)
    normal = (-dy / length, dx / length)
    for index in range(1, segments):
        t = index / segments
        offset = rng.randint(-spread, spread)
        points.append((round(start[0] + dx * t + normal[0] * offset),
                       round(start[1] + dy * t + normal[1] * offset)))
    points.append(end)
    return points


def lightning(draw, start, end, seed, width=2, spread=4):
    points = jagged_points(start, end, seed, spread=spread)
    draw.line(points, fill=palette["outline"], width=width + 4, joint="curve")
    draw.line(points, fill=palette["purple"], width=width + 2, joint="curve")
    draw.line(points, fill=palette["white"], width=width, joint="curve")


def enemy(draw, position, shocked=False):
    x, y = position
    draw.rectangle((x - 3, y - 12, x + 3, y - 6), fill=palette["enemy"])
    draw.rectangle((x - 4, y - 5, x + 4, y + 6), fill=palette["enemy"])
    draw.line((x - 3, y + 6, x - 5, y + 13), fill=palette["enemy"], width=2)
    draw.line((x + 3, y + 6, x + 5, y + 13), fill=palette["enemy"], width=2)
    if shocked:
        for branch, delta in enumerate(((-9, -12), (10, -5), (-10, 7), (9, 12))):
            lightning(draw, (x, y), (x + delta[0], y + delta[1]), 500 + branch, width=1, spread=2)


with Image.open(core_path) as image:
    core = image.convert("RGBA").resize((24, 24), Image.Resampling.NEAREST)

frames = []
for frame_index in range(10):
    frame = Image.new("RGBA", canvas_size, (5, 4, 15, 255))
    draw = ImageDraw.Draw(frame)
    for position in targets:
        enemy(draw, position, shocked=False)
    for x, y in core_positions:
        frame.alpha_composite(core, (x - 12, y - 12))

    if frame_index >= 1:
        for core_index, position in enumerate(core_positions):
            lightning(draw, position, center, 100 + core_index + frame_index * 10, width=1, spread=3)
    if frame_index >= 2:
        pulse = 1 + min(2, frame_index - 2)
        x, y = center
        draw.polygon(
            ((x, y - 5 - pulse), (x + 2, y - 2), (x + 5 + pulse, y), (x + 2, y + 2),
             (x, y + 5 + pulse), (x - 2, y + 2), (x - 5 - pulse, y), (x - 2, y - 2)),
            fill=palette["purple"],
        )
        draw.line((x, y - 4, x, y + 4), fill=palette["white"], width=1)
        draw.line((x - 4, y, x + 4, y), fill=palette["white"], width=1)
        draw.point((x, y), fill=palette["white"])
    if frame_index >= 3:
        lightning(draw, center, targets[0], 200 + frame_index, width=2, spread=5)
    if frame_index >= 4:
        enemy(draw, targets[0], shocked=True)
    if frame_index >= 5:
        lightning(draw, targets[0], targets[1], 300 + frame_index, width=2, spread=5)
    if frame_index >= 6:
        enemy(draw, targets[1], shocked=True)
    if frame_index >= 7:
        lightning(draw, targets[1], targets[2], 400 + frame_index, width=2, spread=5)
    if frame_index >= 8:
        enemy(draw, targets[2], shocked=True)
    if frame_index == 9:
        x, y = targets[2]
        for ray in ((-17, 0), (17, 0), (0, -17), (0, 17), (-12, -12), (12, -12), (-12, 12), (12, 12)):
            lightning(draw, (x, y), (x + ray[0], y + ray[1]), 700 + ray[0] + ray[1], width=1, spread=2)

    frame.save(draft_root / f"Arca_ChainLightning_Layout_Draft_v1_{frame_index:02}.png")
    frames.append(frame.resize((1024, 512), Image.Resampling.NEAREST))

frames[0].save(
    preview_path,
    save_all=True,
    append_images=frames[1:],
    duration=[220, 150, 130, 110, 110, 110, 110, 110, 140, 220],
    loop=0,
    disposal=2,
)
print(preview_path)
