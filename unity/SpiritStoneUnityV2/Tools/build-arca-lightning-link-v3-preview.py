from pathlib import Path

from PIL import Image


project_root = Path(__file__).resolve().parents[1]
draft_root = project_root / "Assets/Characters/Arca/Pixel64/Drafts/Effects/LightningLinkV3"
source_path = draft_root / "Arca_LightningLink_ImageGen_Draft_v3.png"
preview_path = draft_root / "Arca_LightningLink_Draft_v3_8x.gif"

with Image.open(source_path) as source_image:
    source = source_image.convert("RGBA")

frames = []
for frame_index in range(6):
    left = round(frame_index * source.width / 6)
    right = round((frame_index + 1) * source.width / 6)
    cell = source.crop((left, 0, right, source.height))
    alpha_box = cell.getchannel("A").getbbox()
    if alpha_box is None:
        raise RuntimeError(f"Frame {frame_index} contains no visible pixels")
    bolt = cell.crop(alpha_box)
    scale = min(58 / bolt.width, 50 / bolt.height)
    size = (max(1, round(bolt.width * scale)), max(1, round(bolt.height * scale)))
    bolt = bolt.resize(size, Image.Resampling.NEAREST)
    frame = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    frame.alpha_composite(bolt, ((64 - size[0]) // 2, (64 - size[1]) // 2))
    frame.save(draft_root / f"Arca_LightningLink_Draft_v3_{frame_index:02}.png")
    frames.append(frame.resize((512, 512), Image.Resampling.NEAREST))

frames[0].save(
    preview_path,
    save_all=True,
    append_images=frames[1:],
    duration=[65, 65, 65, 75, 90, 130],
    loop=0,
    disposal=2,
    transparency=0,
)
print(preview_path)
