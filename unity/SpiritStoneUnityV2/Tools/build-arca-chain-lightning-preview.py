from pathlib import Path

from PIL import Image


project_root = Path(__file__).resolve().parents[1]
frame_root = project_root / "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Effects/ChainLightningV1"
output = project_root / "Assets/Characters/Arca/Pixel64/Previews/Arca_ChainLightning_V1_8x.gif"
output.parent.mkdir(parents=True, exist_ok=True)

frames = []
for path in sorted(frame_root.glob("Arca_ChainLightning_V1_*.png")):
    with Image.open(path) as source:
        frames.append(source.convert("RGBA").resize((512, 512), Image.Resampling.NEAREST))

if len(frames) != 8:
    raise RuntimeError(f"Expected 8 frames, found {len(frames)}")

frames[0].save(
    output,
    save_all=True,
    append_images=frames[1:],
    duration=[110, 110, 70, 70, 70, 70, 100, 130],
    loop=0,
    disposal=2,
    transparency=0,
)
print(output)
