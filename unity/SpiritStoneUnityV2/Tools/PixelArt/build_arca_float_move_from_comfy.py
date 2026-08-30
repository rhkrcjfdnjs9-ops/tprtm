from pathlib import Path

from PIL import Image


MASTER = Path(
    r"C:\Users\rhkrc\orca\projects\tprtm\unity\SpiritStoneUnityV2"
    r"\Assets\Characters\Arca\Pixel64\character_arca_idle_01_v3.png"
)
OUTPUT = Path(r"D:\ComfyUI-Data\output\Arca\FloatMove\NormalizedV1")
SOURCE_VIDEO_FRAMES = (0, 9, 18, 27, 36, 44)
HORIZONTAL_OFFSETS = (0, 1, 1, 2, 1, 0)
VERTICAL_OFFSETS = (0, -1, -1, 0, 1, 0)
TRAIL_STRENGTH = (0, 1, 1, 1, 1, 0)


def extend_left_edge(
    image: Image.Image,
    box: tuple[int, int, int, int],
) -> None:
    source = image.copy()
    left, top, right, bottom = box
    for y in range(top, bottom):
        for x in range(left, right):
            pixel = source.getpixel((x, y))
            if pixel[3] == 0 or x == 0:
                continue
            if source.getpixel((x - 1, y))[3] == 0:
                image.putpixel((x - 1, y), pixel)
                break


def create_frame(master: Image.Image, x_offset: int, y_offset: int, trail: int) -> Image.Image:
    posed = master.copy()
    if trail:
        extend_left_edge(posed, (15, 20, 24, 35))
        extend_left_edge(posed, (16, 45, 25, 54))

    frame = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    frame.alpha_composite(posed, (x_offset, y_offset))
    return frame


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    master = Image.open(MASTER).convert("RGBA")
    if master.size != (64, 64):
        raise ValueError(f"Master must be 64x64, got {master.size}")

    frames = []
    for index, values in enumerate(zip(HORIZONTAL_OFFSETS, VERTICAL_OFFSETS, TRAIL_STRENGTH)):
        frame = create_frame(master, *values)
        frame.save(OUTPUT / f"character_arca_float_move_comfy_{index + 1:02}.png")
        frames.append(frame)

    sheet = Image.new("RGBA", (64 * len(frames), 64), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.alpha_composite(frame, (64 * index, 0))
    sheet.save(OUTPUT / "character_arca_float_move_comfy_sheet_v1.png")

    preview_frames = [frame.resize((512, 512), Image.Resampling.NEAREST) for frame in frames]
    preview_frames[0].save(
        OUTPUT / "character_arca_float_move_comfy_preview_v1.gif",
        save_all=True,
        append_images=preview_frames[1:],
        duration=110,
        loop=0,
        disposal=2,
    )

    (OUTPUT / "source_manifest.txt").write_text(
        "master=character_arca_idle_01_v3.png\n"
        "motion_reference=arca_float_move_ai_motion_v1_00001_.mp4\n"
        f"selected_video_frames={','.join(map(str, SOURCE_VIDEO_FRAMES))}\n"
        f"horizontal_offsets={','.join(map(str, HORIZONTAL_OFFSETS))}\n"
        f"vertical_offsets={','.join(map(str, VERTICAL_OFFSETS))}\n"
        "canvas=64x64\nfilter=nearest\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
