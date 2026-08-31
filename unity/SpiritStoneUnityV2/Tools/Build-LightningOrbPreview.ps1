param(
    [string]$Source = "Assets/Characters/Arca/Pixel64/Drafts/Effects/LightningOrbV1/Arca_LightningOrb_ImageGen_Concept_v3_EffectOnly.png"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class LightningOrbFrameConverter
{
    private static bool IsBackground(byte r, byte g, byte b)
    {
        int maximum = Math.Max(r, Math.Max(g, b));
        int minimum = Math.Min(r, Math.Min(g, b));
        return minimum >= 235 && maximum - minimum <= 10 && maximum < 255;
    }

    private static bool IsBackgroundCandidate(byte r, byte g, byte b)
    {
        int maximum = Math.Max(r, Math.Max(g, b));
        int minimum = Math.Min(r, Math.Min(g, b));
        return minimum >= 225 && maximum - minimum <= 22;
    }

    private static bool[] BuildBackgroundMask(byte[] pixels, int stride, int left, int width, int height)
    {
        bool[] mask = new bool[width * height];
        Queue<int> queue = new Queue<int>();
        Action<int, int> enqueue = (x, y) =>
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int index = y * width + x;
            if (mask[index]) return;
            int offset = y * stride + (left + x) * 3;
            byte b = pixels[offset], g = pixels[offset + 1], r = pixels[offset + 2];
            if (!IsBackgroundCandidate(r, g, b)) return;
            mask[index] = true;
            queue.Enqueue(index);
        };

        for (int x = 0; x < width; x++) { enqueue(x, 0); enqueue(x, height - 1); }
        for (int y = 0; y < height; y++) { enqueue(0, y); enqueue(width - 1, y); }
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width, y = index / width;
            enqueue(x - 1, y); enqueue(x + 1, y); enqueue(x, y - 1); enqueue(x, y + 1);
        }
        return mask;
    }

    private static Color Palette(byte r, byte g, byte b)
    {
        double luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
        if (luminance >= 245) return Color.FromArgb(255, 255, 247, 255);
        if (luminance >= 205) return Color.FromArgb(255, 239, 190, 255);
        if (luminance >= 160) return Color.FromArgb(255, 201, 103, 255);
        if (luminance >= 110) return Color.FromArgb(255, 145, 52, 224);
        if (luminance >= 65) return Color.FromArgb(255, 86, 19, 154);
        return Color.FromArgb(255, 39, 6, 70);
    }

    public static void Convert(Bitmap source, int frameIndex, int frameWidth, int canvasSize, string destination)
    {
        Rectangle sourceRectangle = new Rectangle(0, 0, source.Width, source.Height);
        BitmapData sourceData = source.LockBits(sourceRectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        byte[] sourcePixels = new byte[Math.Abs(sourceData.Stride) * source.Height];
        Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);
        source.UnlockBits(sourceData);

        int left = frameIndex * frameWidth;
        int groupFirstFrame = (frameIndex / 4) * 4;
        int minX = frameWidth, minY = source.Height, maxX = -1, maxY = -1;
        for (int groupFrame = groupFirstFrame; groupFrame < groupFirstFrame + 4; groupFrame++)
        {
            int groupLeft = groupFrame * frameWidth;
            bool[] groupBackground = BuildBackgroundMask(sourcePixels, sourceData.Stride, groupLeft, frameWidth, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < frameWidth; x++)
                {
                    if (groupBackground[y * frameWidth + x]) continue;
                    int offset = y * sourceData.Stride + (groupLeft + x) * 3;
                    byte b = sourcePixels[offset], g = sourcePixels[offset + 1], r = sourcePixels[offset + 2];
                    int maximum = Math.Max(r, Math.Max(g, b));
                    int minimum = Math.Min(r, Math.Min(g, b));
                    if (!IsBackground(r, g, b) && (maximum - minimum > 12 || maximum == 255))
                    {
                        minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                    }
                }
            }
        }

        if (maxX < minX || maxY < minY) throw new InvalidOperationException("No visible pixels in frame " + frameIndex);
        int contentWidth = maxX - minX + 1, contentHeight = maxY - minY + 1;
        int available = canvasSize - 8;
        double scale = Math.Min((double)available / contentWidth, (double)available / contentHeight);
        int targetWidth = Math.Max(1, (int)Math.Floor(contentWidth * scale));
        int targetHeight = Math.Max(1, (int)Math.Floor(contentHeight * scale));
        int offsetX = (canvasSize - targetWidth) / 2, offsetY = (canvasSize - targetHeight) / 2;

        using (Bitmap result = new Bitmap(canvasSize, canvasSize, PixelFormat.Format32bppArgb))
        {
            bool[] currentBackground = BuildBackgroundMask(sourcePixels, sourceData.Stride, left, frameWidth, source.Height);
            Rectangle targetRectangle = new Rectangle(0, 0, canvasSize, canvasSize);
            BitmapData targetData = result.LockBits(targetRectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            byte[] targetPixels = new byte[Math.Abs(targetData.Stride) * canvasSize];
            for (int targetY = 0; targetY < targetHeight; targetY++)
            {
                int sourceY = minY + Math.Min(contentHeight - 1, (int)Math.Floor(targetY / scale));
                for (int targetX = 0; targetX < targetWidth; targetX++)
                {
                    int sourceX = minX + Math.Min(contentWidth - 1, (int)Math.Floor(targetX / scale));
                    if (currentBackground[sourceY * frameWidth + sourceX]) continue;
                    int sourceOffset = sourceY * sourceData.Stride + (left + sourceX) * 3;
                    byte b = sourcePixels[sourceOffset], g = sourcePixels[sourceOffset + 1], r = sourcePixels[sourceOffset + 2];
                    int maximum = Math.Max(r, Math.Max(g, b));
                    int minimum = Math.Min(r, Math.Min(g, b));
                    if (IsBackground(r, g, b)) continue;
                    Color color = Palette(r, g, b);
                    int targetOffset = (offsetY + targetY) * targetData.Stride + (offsetX + targetX) * 4;
                    targetPixels[targetOffset] = color.B;
                    targetPixels[targetOffset + 1] = color.G;
                    targetPixels[targetOffset + 2] = color.R;
                    targetPixels[targetOffset + 3] = 255;
                }
            }
            Marshal.Copy(targetPixels, 0, targetData.Scan0, targetPixels.Length);
            result.UnlockBits(targetData);
            result.Save(destination, ImageFormat.Png);
        }
    }
}
'@

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $projectRoot $Source
$outputRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Drafts/Effects/LightningOrbV1/NormalizedV1"
$gatherRoot = Join-Path $outputRoot "Gather_128"
$orbRoot = Join-Path $outputRoot "Orb_64"
$impactRoot = Join-Path $outputRoot "Impact_128"
$previewRoot = Join-Path $outputRoot "Preview_128"

foreach ($path in @($gatherRoot, $orbRoot, $impactRoot, $previewRoot)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

function Test-BackgroundPixel([System.Drawing.Color]$color) {
    $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
    $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
    return $minimum -ge 235 -and ($maximum - $minimum) -le 10 -and $maximum -lt 255
}

function Get-PaletteColor([System.Drawing.Color]$color) {
    $luminance = 0.2126 * $color.R + 0.7152 * $color.G + 0.0722 * $color.B
    if ($luminance -ge 245) { return [System.Drawing.Color]::FromArgb(255, 255, 247, 255) }
    if ($luminance -ge 205) { return [System.Drawing.Color]::FromArgb(255, 239, 190, 255) }
    if ($luminance -ge 160) { return [System.Drawing.Color]::FromArgb(255, 201, 103, 255) }
    if ($luminance -ge 110) { return [System.Drawing.Color]::FromArgb(255, 145, 52, 224) }
    if ($luminance -ge 65) { return [System.Drawing.Color]::FromArgb(255, 86, 19, 154) }
    return [System.Drawing.Color]::FromArgb(255, 39, 6, 70)
}

function Convert-Frame(
    [System.Drawing.Bitmap]$sheet,
    [int]$frameIndex,
    [int]$frameWidth,
    [int]$canvasSize,
    [string]$destination
) {
    [LightningOrbFrameConverter]::Convert($sheet, $frameIndex, $frameWidth, $canvasSize, $destination)
    return
    $left = $frameIndex * $frameWidth
    $minX = $frameWidth
    $minY = $sheet.Height
    $maxX = -1
    $maxY = -1

    for ($y = 0; $y -lt $sheet.Height; $y++) {
        for ($x = 0; $x -lt $frameWidth; $x++) {
            $color = $sheet.GetPixel($left + $x, $y)
            if (-not (Test-BackgroundPixel $color)) {
                $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
                $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
                if (($maximum - $minimum) -gt 12 -or $maximum -eq 255) {
                    $minX = [Math]::Min($minX, $x)
                    $minY = [Math]::Min($minY, $y)
                    $maxX = [Math]::Max($maxX, $x)
                    $maxY = [Math]::Max($maxY, $y)
                }
            }
        }
    }

    if ($maxX -lt $minX -or $maxY -lt $minY) {
        throw "Frame $frameIndex contains no visible effect pixels."
    }

    $contentWidth = $maxX - $minX + 1
    $contentHeight = $maxY - $minY + 1
    $available = $canvasSize - 8
    $scale = [Math]::Min($available / $contentWidth, $available / $contentHeight)
    $targetWidth = [Math]::Max(1, [Math]::Floor($contentWidth * $scale))
    $targetHeight = [Math]::Max(1, [Math]::Floor($contentHeight * $scale))
    $offsetX = [Math]::Floor(($canvasSize - $targetWidth) / 2)
    $offsetY = [Math]::Floor(($canvasSize - $targetHeight) / 2)

    $result = New-Object System.Drawing.Bitmap($canvasSize, $canvasSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($targetY = 0; $targetY -lt $targetHeight; $targetY++) {
        $sourceY = $minY + [Math]::Min($contentHeight - 1, [Math]::Floor($targetY / $scale))
        for ($targetX = 0; $targetX -lt $targetWidth; $targetX++) {
            $sourceX = $minX + [Math]::Min($contentWidth - 1, [Math]::Floor($targetX / $scale))
            $color = $sheet.GetPixel($left + $sourceX, $sourceY)
            if (Test-BackgroundPixel $color) { continue }

            $maximum = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
            $minimum = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
            if (($maximum - $minimum) -le 12 -and $maximum -lt 255) { continue }
            $result.SetPixel($offsetX + $targetX, $offsetY + $targetY, (Get-PaletteColor $color))
        }
    }

    $result.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
    $result.Dispose()
}

$sheet = [System.Drawing.Bitmap]::FromFile($sourcePath)
try {
    if ($sheet.Width % 12 -ne 0) { throw "Source width must contain exactly 12 equal frames." }
    $frameWidth = [int]($sheet.Width / 12)

    for ($index = 0; $index -lt 12; $index++) {
        $phaseIndex = $index % 4
        $previewPath = Join-Path $previewRoot ("fx_arca_lightning_orb_{0:D2}.png" -f $index)
        Convert-Frame $sheet $index $frameWidth 128 $previewPath

        if ($index -lt 4) {
            Convert-Frame $sheet $index $frameWidth 128 (Join-Path $gatherRoot ("fx_arca_lightning_orb_gather_{0:D2}.png" -f $phaseIndex))
        }
        elseif ($index -lt 8) {
            Convert-Frame $sheet $index $frameWidth 64 (Join-Path $orbRoot ("fx_arca_lightning_orb_projectile_{0:D2}.png" -f $phaseIndex))
        }
        else {
            Convert-Frame $sheet $index $frameWidth 128 (Join-Path $impactRoot ("fx_arca_lightning_orb_impact_{0:D2}.png" -f $phaseIndex))
        }
    }
}
finally {
    $sheet.Dispose()
}

$ffmpeg = "D:\Counter-Strike Online\Bin\FFmpeg.exe"
if (Test-Path $ffmpeg) {
    $previewPattern = Join-Path $previewRoot "fx_arca_lightning_orb_%02d.png"
    $gifPath = Join-Path $outputRoot "Arca_LightningOrb_NormalizedV1_Preview.gif"
    & $ffmpeg -y -v error -framerate 10 -i $previewPattern -filter_complex "[0:v]split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse" -loop 0 $gifPath
}

Write-Output $outputRoot
