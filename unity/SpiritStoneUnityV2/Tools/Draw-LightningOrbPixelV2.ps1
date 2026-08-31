param()

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class LightningOrbPixelArtist
{
    private static readonly Color Deep = Color.FromArgb(255, 55, 8, 94);
    private static readonly Color Dark = Color.FromArgb(255, 91, 18, 153);
    private static readonly Color Mid = Color.FromArgb(255, 137, 42, 213);
    private static readonly Color Bright = Color.FromArgb(255, 201, 103, 255);
    private static readonly Color Light = Color.FromArgb(255, 241, 196, 255);
    private static readonly Color White = Color.FromArgb(255, 255, 255, 255);

    private static void Pixel(Bitmap image, int x, int y, Color color, int size = 1)
    {
        int radius = Math.Max(0, size - 1);
        for (int py = y - radius; py <= y + radius; py++)
            for (int px = x - radius; px <= x + radius; px++)
                if (px >= 0 && px < image.Width && py >= 0 && py < image.Height)
                    image.SetPixel(px, py, color);
    }

    private static void Line(Bitmap image, Point start, Point end, Color color, int size = 1)
    {
        int dx = Math.Abs(end.X - start.X), sx = start.X < end.X ? 1 : -1;
        int dy = -Math.Abs(end.Y - start.Y), sy = start.Y < end.Y ? 1 : -1;
        int error = dx + dy;
        int x = start.X, y = start.Y;
        while (true)
        {
            Pixel(image, x, y, color, size);
            if (x == end.X && y == end.Y) break;
            int doubled = error * 2;
            if (doubled >= dy) { error += dy; x += sx; }
            if (doubled <= dx) { error += dx; y += sy; }
        }
    }

    private static void Bolt(Bitmap image, Point[] points, bool strong)
    {
        int outerSize = strong ? 2 : 1;
        for (int i = 0; i < points.Length - 1; i++) Line(image, points[i], points[i + 1], Deep, outerSize);
        for (int i = 0; i < points.Length - 1; i++) Line(image, points[i], points[i + 1], Bright, 1);
        if (strong)
            for (int i = 1; i < points.Length - 1; i += 2) Pixel(image, points[i].X, points[i].Y, White, 1);
    }

    private static Point Lerp(Point a, Point b, double t)
    {
        return new Point((int)Math.Round(a.X + (b.X - a.X) * t), (int)Math.Round(a.Y + (b.Y - a.Y) * t));
    }

    private static Point[] JaggedPath(Point start, Point end, int seed, double amount)
    {
        Point[] path = new Point[6];
        path[0] = start; path[5] = end;
        double dx = end.X - start.X, dy = end.Y - start.Y;
        double length = Math.Max(1.0, Math.Sqrt(dx * dx + dy * dy));
        double px = -dy / length, py = dx / length;
        int[] signs = { 1, -1, 1, -1 };
        for (int i = 1; i < 5; i++)
        {
            double t = i / 5.0;
            double offset = signs[(i + seed) % signs.Length] * amount * (i % 2 == 0 ? 0.65 : 1.0);
            path[i] = new Point((int)Math.Round(start.X + dx * t + px * offset),
                (int)Math.Round(start.Y + dy * t + py * offset));
        }
        return path;
    }

    private static void Ring(Bitmap image, Point center, int radius, Color outer, Color inner)
    {
        for (int degree = 0; degree < 360; degree += 2)
        {
            double angle = degree * Math.PI / 180.0;
            int x = center.X + (int)Math.Round(Math.Cos(angle) * radius);
            int y = center.Y + (int)Math.Round(Math.Sin(angle) * radius);
            Pixel(image, x, y, outer, 2);
        }
        for (int degree = 0; degree < 360; degree += 2)
        {
            double angle = degree * Math.PI / 180.0;
            int x = center.X + (int)Math.Round(Math.Cos(angle) * radius);
            int y = center.Y + (int)Math.Round(Math.Sin(angle) * radius);
            Pixel(image, x, y, inner, 1);
        }
    }

    private static void FillDisk(Bitmap image, Point center, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= radius * radius) Pixel(image, center.X + x, center.Y + y, color);
    }

    private static void Spark(Bitmap image, Point center, int length, Color color)
    {
        Line(image, new Point(center.X - length, center.Y), new Point(center.X + length, center.Y), color);
        Line(image, new Point(center.X, center.Y - length), new Point(center.X, center.Y + length), color);
        Pixel(image, center.X, center.Y, White);
    }

    public static void DrawGather(string directory)
    {
        Point top = new Point(64, 15), left = new Point(23, 101), right = new Point(105, 101), center = new Point(64, 63);
        for (int frame = 0; frame < 4; frame++)
        using (Bitmap image = new Bitmap(128, 128, PixelFormat.Format32bppArgb))
        {
            // Key poses: ignition -> triangle lock -> convergence -> compressed charge.
            double progress = new[] { 0.08, 0.35, 0.78, 1.0 }[frame];
            Point[] sources = { top, left, right };
            for (int index = 0; index < sources.Length; index++)
            {
                Spark(image, sources[index], frame < 2 ? 2 : 4, frame == 3 ? White : Bright);
                Point destination = Lerp(sources[index], center, progress);
                Bolt(image, JaggedPath(sources[index], destination, index + frame, 4.0), frame >= 2);
            }
            if (frame >= 1) Spark(image, center, frame == 3 ? 8 : frame + 1, frame == 3 ? White : Light);
            if (frame == 3)
            {
                FillDisk(image, center, 7, Deep);
                Ring(image, center, 9, Deep, Bright);
                FillDisk(image, center, 4, White);
            }
            image.Save(System.IO.Path.Combine(directory, string.Format("fx_arca_lightning_orb_gather_v2_{0:D2}.png", frame)), ImageFormat.Png);
        }
    }

    public static void DrawProjectile(string directory)
    {
        Point center = new Point(34, 32);
        for (int frame = 0; frame < 4; frame++)
        using (Bitmap image = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
        {
            // Key poses: release -> stable flight -> acceleration -> contact compression.
            int orbRadius = frame == 3 ? 8 : 11;
            FillDisk(image, center, orbRadius, Deep);
            FillDisk(image, center, Math.Max(5, orbRadius - 2), Dark);
            Ring(image, center, orbRadius + 1, Deep, frame == 3 ? White : Bright);
            Ring(image, center, Math.Max(5, orbRadius - 2), Mid, Light);
            FillDisk(image, center, frame == 3 ? 4 : 3, White);

            int shift = frame % 2 == 0 ? -1 : 1;
            Bolt(image, new[] { new Point(23, 29 + shift), new Point(28, 26 - shift), center, new Point(39, 36 + shift), new Point(44, 33 - shift) }, true);
            Bolt(image, new[] { new Point(31 + shift, 21), new Point(36 - shift, 27), center, new Point(29 + shift, 39), new Point(34, 43) }, false);
            Spark(image, new Point(48, 22 + frame), 1, Bright);
            Spark(image, new Point(49, 41 - frame), 1, Mid);

            int tailLength = frame == 0 ? 6 : frame == 1 ? 11 : frame == 2 ? 17 : 4;
            Bolt(image, new[] { new Point(21, 31), new Point(17, 28 + shift), new Point(12, 33 - shift), new Point(34 - tailLength, 31 + shift) }, true);
            if (frame == 0) Spark(image, new Point(18, 32), 5, White);
            if (frame == 3)
            {
                Line(image, new Point(45, 22), new Point(45, 42), White, 2);
                Spark(image, new Point(45, 32), 5, Light);
            }
            image.Save(System.IO.Path.Combine(directory, string.Format("fx_arca_lightning_orb_projectile_v2_{0:D2}.png", frame)), ImageFormat.Png);
        }
    }

    public static void DrawImpact(string directory)
    {
        Point center = new Point(64, 64);
        int[] radii = { 9, 19, 47, 52 };
        for (int frame = 0; frame < 4; frame++)
        using (Bitmap image = new Bitmap(128, 128, PixelFormat.Format32bppArgb))
        {
            int radius = radii[frame];
            if (frame < 3)
            {
                // Impact 01 is the exact gameplay hit frame. 02 is visual follow-through.
                Ring(image, center, radius, Deep, frame <= 1 ? White : Bright);
                int rays = frame == 0 ? 4 : frame == 1 ? 8 : 12;
                for (int ray = 0; ray < rays; ray++)
                {
                    double angle = (ray * 360.0 / rays + frame * 9) * Math.PI / 180.0;
                    Point start = new Point(center.X + (int)Math.Round(Math.Cos(angle) * Math.Max(5, radius - 6)),
                        center.Y + (int)Math.Round(Math.Sin(angle) * Math.Max(5, radius - 6)));
                    Point end = new Point(center.X + (int)Math.Round(Math.Cos(angle) * Math.Min(57, radius + 12)),
                        center.Y + (int)Math.Round(Math.Sin(angle) * Math.Min(57, radius + 12)));
                    Bolt(image, JaggedPath(start, end, ray + frame, frame == 1 ? 5.0 : 3.0), frame >= 1);
                }
                FillDisk(image, center, frame == 1 ? 12 : frame == 0 ? 5 : 4, White);
                Spark(image, center, frame == 1 ? 20 : frame == 0 ? 8 : 10, Light);
                if (frame == 1)
                {
                    Line(image, new Point(24, 64), new Point(104, 64), White, 2);
                    Line(image, new Point(64, 24), new Point(64, 104), White, 2);
                }
            }
            else
            {
                for (int arc = 0; arc < 8; arc++)
                {
                    double startAngle = (arc * 45 + 8) * Math.PI / 180.0;
                    double endAngle = (arc * 45 + 28) * Math.PI / 180.0;
                    Point start = new Point(center.X + (int)Math.Round(Math.Cos(startAngle) * radius), center.Y + (int)Math.Round(Math.Sin(startAngle) * radius));
                    Point end = new Point(center.X + (int)Math.Round(Math.Cos(endAngle) * radius), center.Y + (int)Math.Round(Math.Sin(endAngle) * radius));
                    Bolt(image, JaggedPath(start, end, arc, 2.0), false);
                    Spark(image, end, 1, Mid);
                }
                Spark(image, center, 3, Bright);
            }
            image.Save(System.IO.Path.Combine(directory, string.Format("fx_arca_lightning_orb_impact_v2_{0:D2}.png", frame)), ImageFormat.Png);
        }
    }
}
'@

$projectRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Drafts/Effects/LightningOrbV3_StrongKeyframes"
$gatherRoot = Join-Path $outputRoot "Gather_128"
$projectileRoot = Join-Path $outputRoot "Projectile_64"
$impactRoot = Join-Path $outputRoot "Impact_128"
$previewRoot = Join-Path $outputRoot "Preview_128"
foreach ($directory in @($outputRoot, $gatherRoot, $projectileRoot, $impactRoot, $previewRoot)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

[LightningOrbPixelArtist]::DrawGather($gatherRoot)
[LightningOrbPixelArtist]::DrawProjectile($projectileRoot)
[LightningOrbPixelArtist]::DrawImpact($impactRoot)

for ($frame = 0; $frame -lt 12; $frame++) {
    $phaseFrame = $frame % 4
    if ($frame -lt 4) {
        $source = Join-Path $gatherRoot ("fx_arca_lightning_orb_gather_v2_{0:D2}.png" -f $phaseFrame)
    }
    elseif ($frame -lt 8) {
        $source = Join-Path $projectileRoot ("fx_arca_lightning_orb_projectile_v2_{0:D2}.png" -f $phaseFrame)
    }
    else {
        $source = Join-Path $impactRoot ("fx_arca_lightning_orb_impact_v2_{0:D2}.png" -f $phaseFrame)
    }

    $sourceImage = [System.Drawing.Bitmap]::FromFile($source)
    try {
        $preview = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($preview)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            if ($sourceImage.Width -eq 64) {
                $graphics.DrawImage($sourceImage, 32, 32, 64, 64)
            }
            else {
                $graphics.DrawImageUnscaled($sourceImage, 0, 0)
            }
        }
        finally { $graphics.Dispose() }
        $preview.Save((Join-Path $previewRoot ("fx_arca_lightning_orb_v2_{0:D2}.png" -f $frame)), [System.Drawing.Imaging.ImageFormat]::Png)
        $preview.Dispose()
    }
    finally { $sourceImage.Dispose() }
}

$ffmpeg = "D:\Counter-Strike Online\Bin\FFmpeg.exe"
if (Test-Path $ffmpeg) {
    $pattern = Join-Path $previewRoot "fx_arca_lightning_orb_v2_%02d.png"
    $gif = Join-Path $outputRoot "Arca_LightningOrb_StrongKeyframesV3_Preview_8x.gif"
    & $ffmpeg -y -v error -framerate 10 -i $pattern -filter_complex "[0:v]scale=1024:1024:flags=neighbor,split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse" -loop 0 $gif
    $sheet = Join-Path $outputRoot "Arca_LightningOrb_StrongKeyframesV3_ContactSheet.png"
    & $ffmpeg -y -v error -framerate 1 -i $pattern -frames:v 12 -vf "scale=512:512:flags=neighbor,tile=4x3" $sheet
}

Write-Output $outputRoot
