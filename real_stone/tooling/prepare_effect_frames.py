from pathlib import Path
import sys

from PIL import Image


source = Image.open(sys.argv[1]).convert("RGBA")
destination = Path(sys.argv[2])
destination.mkdir(parents=True, exist_ok=True)
cell_width = source.width / 8
common_size = source.height

for index in range(8):
    left = round(index * cell_width)
    right = round((index + 1) * cell_width)
    cell = source.crop((left, 0, right, source.height))
    square = Image.new("RGBA", (common_size, common_size), (0, 0, 0, 0))
    square.alpha_composite(cell, ((common_size - cell.width) // 2, 0))
    frame = square.resize((256, 256), Image.Resampling.NEAREST)
    frame.save(destination / f"hit_effect_{index}.png")
