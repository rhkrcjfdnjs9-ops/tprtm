from pathlib import Path

from PIL import Image


MASTER = Path(
    r"C:\Users\rhkrc\orca\projects\tprtm\unity\SpiritStoneUnityV2"
    r"\Assets\Characters\Arca\Pixel64\character_arca_idle_01_v3.png"
)
OUTPUT = Path(r"D:\ComfyUI-Data\output\Arca\Idle\NormalizedV1")

# Six key timings selected from the approved ComfyUI floating-idle motion.
SOURCE_VIDEO_FRAMES = (0, 9, 18, 27, 36, 44)
# Preserve every master pixel; only the one-pixel floating displacement changes.
VERTICAL_OFFSETS = (0, -1, -1, 0, 1, 0)
# Secondary sway direction per frame. Positive values extend only the right-side
# hair/cape silhouette; negative values extend only the left-side silhouette.
SECONDARY_SWAY = (0, 1, 1, 0, -1, 0)


def extend_transparent_edge(
    image: Image.Image,
    box: tuple[int, int, int, int],
    direction: int,
) -> None:
    """Add one pixel only outside the selected silhouette; never rewrite its core."""
    if direction == 0:
        return
    source = image.copy()
    left, top, right, bottom = box
    x_range = range(right - 1, left - 1, -1) if direction > 0 else range(left, right)
    for y in range(top, bottom):
        for x in x_range:
            pixel = source.getpixel((x, y))
            target_x = x + direction
            if pixel[3] == 0 or not 0 <= target_x < 64:
                continue
            if source.getpixel((target_x, y))[3] == 0:
                image.putpixel((target_x, y), pixel)
                break


def translated_master(master: Image.Image, y_offset: int, sway: int) -> Image.Image:
    posed = master.copy()
    if sway > 0:
        extend_transparent_edge(posed, (40, 20, 49, 35), 1)
        extend_transparent_edge(posed, (42, 45, 51, 54), 1)
    elif sway < 0:
        extend_transparent_edge(posed, (15, 20, 24, 35), -1)
        extend_transparent_edge(posed, (16, 45, 25, 54), -1)

    frame = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    frame.alpha_composite(posed, (0, y_offset))
    return frame


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    master = Image.open(MASTER).convert("RGBA")
    if master.size != (64, 64):
        raise ValueError(f"Master must be 64x64, got {master.size}")

    frames = []
    for index, (y_offset, sway) in enumerate(zip(VERTICAL_OFFSETS, SECONDARY_SWAY)):
        frame = translated_master(master, y_offset, sway)
        frame.save(OUTPUT / f"character_arca_idle_comfy_{index + 1:02}.png")
        frames.append(frame)

    sheet = Image.new("RGBA", (64 * len(frames), 64), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (64 * index, 0))
    sheet.save(OUTPUT / "character_arca_idle_comfy_sheet_v1.png")

    preview_frames = [
        frame.resize((512, 512), Image.Resampling.NEAREST) for frame in frames
    ]
    preview_frames[0].save(
        OUTPUT / "character_arca_idle_comfy_preview_v1.gif",
        save_all=True,
        append_images=preview_frames[1:],
        duration=145,
        loop=0,
        disposal=2,
    )

    (OUTPUT / "source_manifest.txt").write_text(
        "master=character_arca_idle_01_v3.png\n"
        "motion_reference=arca_idle_ai_motion_v1_00001_.mp4\n"
        f"selected_video_frames={','.join(map(str, SOURCE_VIDEO_FRAMES))}\n"
        f"vertical_offsets={','.join(map(str, VERTICAL_OFFSETS))}\n"
        f"secondary_sway={','.join(map(str, SECONDARY_SWAY))}\n"
        "canvas=64x64\nfilter=nearest\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
