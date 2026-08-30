from pathlib import Path
from random import Random

from PIL import Image, ImageDraw, ImageEnhance


project_root = Path(__file__).resolve().parents[1]
draft_root = project_root / "Assets/Characters/Arca/Pixel64/Drafts/Effects/OverchargeV1"
source_path = draft_root / "Arca_Overcharge_Effects_ImageGen_Source_v1.png"
ring_source_path = draft_root / "Arca_Overcharge_Rings_ImageGen_Source_v2.png"
preview_path = draft_root / "Arca_Overcharge_Draft_v1_4x.gif"
arca_path = project_root / (
    "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/"
    "IdlePixelLabV2/Arca_Idle_Front_V2_00.png"
)
core_path = project_root / (
    "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/ThunderCore/"
    "IdleRotateV2/Arca_ThunderCore_IdleRotate_V2_00.png"
)


def extract_cells(path, count):
    with Image.open(path) as image:
        source = image.convert("RGBA")
    cells = []
    for index in range(count):
        left = round(index * source.width / count)
        right = round((index + 1) * source.width / count)
        cell = source.crop((left, 0, right, source.height))
        box = cell.getchannel("A").getbbox()
        if box is None:
            raise RuntimeError(f"Effect cell {index} is empty")
        cells.append(cell.crop(box))
    return cells


def fit(image, width, height):
    scale = min(width / image.width, height / image.height)
    size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    return image.resize(size, Image.Resampling.NEAREST)


def extract_complete_rings(path):
    """Extract the two dominant ring shapes and rebuild each with guaranteed transparent padding."""
    with Image.open(path) as image:
        source = image.convert("RGBA")

    rings = []
    for index in range(2):
        left = round(index * source.width / 2)
        right = round((index + 1) * source.width / 2)
        cell = source.crop((left, 0, right, source.height))
        alpha = cell.getchannel("A")
        pixels = alpha.load()
        visited = set()
        largest = []

        for y in range(cell.height):
            for x in range(cell.width):
                if pixels[x, y] < 32 or (x, y) in visited:
                    continue
                stack = [(x, y)]
                visited.add((x, y))
                component = []
                while stack:
                    px, py = stack.pop()
                    component.append((px, py))
                    for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                        if (
                            0 <= nx < cell.width
                            and 0 <= ny < cell.height
                            and (nx, ny) not in visited
                            and pixels[nx, ny] >= 32
                        ):
                            visited.add((nx, ny))
                            stack.append((nx, ny))
                if len(component) > len(largest):
                    largest = component

        if not largest:
            raise RuntimeError(f"Ring cell {index} is empty")
        xs = [point[0] for point in largest]
        ys = [point[1] for point in largest]
        cropped = cell.crop((min(xs), min(ys), max(xs) + 1, max(ys) + 1))
        side = max(cropped.width, cropped.height)
        padded_side = round(side / 0.72)
        padded = Image.new("RGBA", (padded_side, padded_side), (0, 0, 0, 0))
        padded.alpha_composite(
            cropped,
            ((padded_side - cropped.width) // 2, (padded_side - cropped.height) // 2),
        )
        rings.append(padded)
    return rings


def alpha_composite_center(canvas, image, center):
    canvas.alpha_composite(image, (round(center[0] - image.width / 2), round(center[1] - image.height / 2)))


def electric_line(draw, start, end, seed, alpha=255):
    rng = Random(seed)
    points = [start]
    dx, dy = end[0] - start[0], end[1] - start[1]
    length = max(1, (dx * dx + dy * dy) ** 0.5)
    nx, ny = -dy / length, dx / length
    for step in range(1, 6):
        t = step / 6
        offset = rng.randint(-3, 3)
        points.append((round(start[0] + dx * t + nx * offset), round(start[1] + dy * t + ny * offset)))
    points.append(end)
    draw.line(points, fill=(75, 16, 105, alpha), width=5)
    draw.line(points, fill=(182, 66, 255, alpha), width=3)
    draw.line(points, fill=(255, 255, 255, alpha), width=1)


effects = extract_cells(source_path, 6)
ring_cells = extract_complete_rings(ring_source_path)
small_ring = fit(ring_cells[0], 70, 70)
large_ring = fit(ring_cells[1], 104, 104)
small_ring.save(draft_root / "Arca_Overcharge_Ring_Small_v2.png")
large_ring.save(draft_root / "Arca_Overcharge_Ring_Large_v2.png")
vertical_arc = fit(effects[2], 28, 68)
particle_cluster = fit(effects[3], 36, 36)
upward_sparks = fit(effects[5], 42, 60)

with Image.open(arca_path) as image:
    arca = image.convert("RGBA").resize((96, 96), Image.Resampling.NEAREST)
with Image.open(core_path) as image:
    core = image.convert("RGBA").resize((28, 28), Image.Resampling.NEAREST)
bright_core = ImageEnhance.Brightness(core).enhance(1.6)

canvas_size = (320, 192)
character_center = (160, 116)
idle_positions = [(87, 62), (160, 34), (233, 62)]
charged_positions = [(105, 73), (160, 43), (215, 73)]
frames = []

for frame_index in range(14):
    canvas = Image.new("RGBA", canvas_size, (5, 4, 15, 255))
    draw = ImageDraw.Draw(canvas)
    alpha_composite_center(canvas, arca, character_center)

    if frame_index < 6:
        t = frame_index / 5
        positions = [
            (round(a[0] + (b[0] - a[0]) * t), round(a[1] + (b[1] - a[1]) * t))
            for a, b in zip(idle_positions, charged_positions)
        ]
    elif frame_index < 10:
        positions = charged_positions
    else:
        t = (frame_index - 10) / 3
        positions = [
            (round(a[0] + (b[0] - a[0]) * t), round(a[1] + (b[1] - a[1]) * t))
            for a, b in zip(charged_positions, idle_positions)
        ]

    for position in positions:
        alpha_composite_center(canvas, bright_core if 3 <= frame_index <= 10 else core, position)

    if 1 <= frame_index <= 2:
        alpha_composite_center(canvas, small_ring, character_center)
    if 3 <= frame_index <= 5:
        alpha_composite_center(canvas, large_ring, character_center)
        alpha_composite_center(canvas, vertical_arc, (160, 111))
    if 4 <= frame_index <= 10:
        for index, start in enumerate(positions):
            electric_line(draw, start, positions[(index + 1) % 3], frame_index * 17 + index)
    if 6 <= frame_index <= 9:
        offset = (-18 if frame_index % 2 == 0 else 20, 3 if frame_index % 2 == 0 else -9)
        alpha_composite_center(canvas, particle_cluster, (character_center[0] + offset[0], character_center[1] + offset[1]))
    if frame_index == 10:
        alpha_composite_center(canvas, small_ring, character_center)
    if 11 <= frame_index <= 13:
        fade = upward_sparks.copy()
        fade.putalpha(fade.getchannel("A").point(lambda value: round(value * (14 - frame_index) / 3)))
        alpha_composite_center(canvas, fade, (160, 92 - (frame_index - 11) * 5))

    frame_path = draft_root / f"Arca_Overcharge_Draft_v1_{frame_index:02}.png"
    canvas.save(frame_path)
    frames.append(canvas.resize((1280, 768), Image.Resampling.NEAREST))

frames[0].save(
    preview_path,
    save_all=True,
    append_images=frames[1:],
    duration=[150, 100, 100, 90, 90, 120, 120, 120, 120, 150, 110, 110, 110, 180],
    loop=0,
    disposal=2,
)
print(preview_path)
