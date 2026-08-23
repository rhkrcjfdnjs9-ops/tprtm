from pathlib import Path
import sys
from collections import deque

from PIL import Image, ImageDraw


def remove_edge_fragments(frame: Image.Image) -> None:
    alpha = frame.getchannel("A")
    pixels = alpha.load()
    seen = set()
    for start_y in range(frame.height):
        for start_x in range(frame.width):
            if pixels[start_x, start_y] <= 12 or (start_x, start_y) in seen:
                continue
            queue = deque([(start_x, start_y)])
            seen.add((start_x, start_y))
            component = []
            touches_edge = False
            while queue:
                x, y = queue.popleft()
                component.append((x, y))
                touches_edge |= x <= 1 or x >= frame.width - 2
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if (0 <= nx < frame.width and 0 <= ny < frame.height and
                            (nx, ny) not in seen and pixels[nx, ny] > 12):
                        seen.add((nx, ny))
                        queue.append((nx, ny))
            if touches_edge and len(component) < 5000:
                for x, y in component:
                    frame.putpixel((x, y), (0, 0, 0, 0))


def clear_generated_background(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    # The generator occasionally renders the transparency checkerboard into
    # RGB. Flooding from every corner removes only that connected backdrop.
    for point in ((0, 0), (rgba.width - 1, 0), (0, rgba.height - 1),
                  (rgba.width - 1, rgba.height - 1)):
        ImageDraw.floodfill(rgba, point, (0, 0, 0, 0), thresh=42)
    return rgba


def split_strip(source: Path, destination: Path, prefix: str, count: int = 8) -> None:
    image = Image.open(source)
    if image.mode != "RGBA" or image.getchannel("A").getextrema()[0] == 255:
        image = clear_generated_background(image)
    destination.mkdir(parents=True, exist_ok=True)

    for index in range(count):
        left = round(index * image.width / count)
        right = round((index + 1) * image.width / count)
        cell = image.crop((left, 0, right, image.height))
        bounds = cell.getchannel("A").getbbox()
        if bounds is None:
            raise RuntimeError(f"No sprite found in frame {index}")
        sprite = cell.crop(bounds)
        scale = min(226 / sprite.width, 218 / sprite.height)
        sprite = sprite.resize(
            (max(1, round(sprite.width * scale)),
             max(1, round(sprite.height * scale))),
            Image.Resampling.NEAREST,
        )
        frame = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        x = (256 - sprite.width) // 2
        y = 244 - sprite.height
        frame.alpha_composite(sprite, (x, y))
        remove_edge_fragments(frame)
        frame.save(destination / f"{prefix}_{index}.png")


if __name__ == "__main__":
    frame_count = int(sys.argv[4]) if len(sys.argv) > 4 else 8
    split_strip(Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3], frame_count)
