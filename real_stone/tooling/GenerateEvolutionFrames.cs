using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class GenerateEvolutionFrames
{
    private static readonly string[] Names = { "idle", "walk", "attack", "hit", "death" };
    private static readonly int[] Counts = { 6, 8, 8, 5, 6 };

    private static bool IsBackdrop(Color c)
    {
        int max = Math.Max(c.R, Math.Max(c.G, c.B));
        int min = Math.Min(c.R, Math.Min(c.G, c.B));
        return min >= 220 && max - min <= 18;
    }

    private static Bitmap ExtractStage(Bitmap sheet, int stage)
    {
        int cellWidth = sheet.Width / 6;
        int left = (stage - 1) * cellWidth;
        int right = stage == 6 ? sheet.Width : left + cellWidth;
        var cell = new Bitmap(right - left, sheet.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(cell))
            g.DrawImage(sheet, new Rectangle(0, 0, cell.Width, cell.Height),
                new Rectangle(left, 0, cell.Width, cell.Height), GraphicsUnit.Pixel);

        var seen = new bool[cell.Width, cell.Height];
        var queue = new Queue<Point>();
        for (int x = 0; x < cell.Width; x++) { queue.Enqueue(new Point(x, 0)); queue.Enqueue(new Point(x, cell.Height - 1)); }
        for (int y = 0; y < cell.Height; y++) { queue.Enqueue(new Point(0, y)); queue.Enqueue(new Point(cell.Width - 1, y)); }
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            if (p.X < 0 || p.Y < 0 || p.X >= cell.Width || p.Y >= cell.Height || seen[p.X, p.Y]) continue;
            seen[p.X, p.Y] = true;
            if (!IsBackdrop(cell.GetPixel(p.X, p.Y))) continue;
            cell.SetPixel(p.X, p.Y, Color.Transparent);
            queue.Enqueue(new Point(p.X + 1, p.Y)); queue.Enqueue(new Point(p.X - 1, p.Y));
            queue.Enqueue(new Point(p.X, p.Y + 1)); queue.Enqueue(new Point(p.X, p.Y - 1));
        }
        KeepLargestConnectedShape(cell);
        return cell;
    }

    private static void KeepLargestConnectedShape(Bitmap image)
    {
        var seen = new bool[image.Width, image.Height];
        var groups = new List<List<Point>>();
        for (int y = 0; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
        {
            if (seen[x, y] || image.GetPixel(x, y).A <= 20) continue;
            var group = new List<Point>();
            var queue = new Queue<Point>();
            queue.Enqueue(new Point(x, y));
            seen[x, y] = true;
            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                group.Add(p);
                Point[] around = { new Point(p.X + 1, p.Y), new Point(p.X - 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X, p.Y - 1) };
                foreach (Point n in around)
                {
                    if (n.X < 0 || n.Y < 0 || n.X >= image.Width || n.Y >= image.Height || seen[n.X, n.Y]) continue;
                    if (image.GetPixel(n.X, n.Y).A <= 20) continue;
                    seen[n.X, n.Y] = true;
                    queue.Enqueue(n);
                }
            }
            groups.Add(group);
        }
        groups.Sort(delegate(List<Point> a, List<Point> b) { return b.Count.CompareTo(a.Count); });
        for (int i = 1; i < groups.Count; i++)
            foreach (Point p in groups[i]) image.SetPixel(p.X, p.Y, Color.Transparent);
    }

    private static Rectangle Bounds(Bitmap image)
    {
        int minX = image.Width, minY = image.Height, maxX = 0, maxY = 0;
        for (int y = 0; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).A > 20)
            { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
        return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    private static void Pose(int action, int frame, out float angle, out int dx, out int dy, out float scaleX, out float scaleY)
    {
        angle = 0; dx = 0; dy = 0; scaleX = 1; scaleY = 1;
        if (action == 0) { int[] bob = { 0, -1, -2, -1, 0, 1 }; dy = bob[frame]; scaleY = frame == 2 ? 1.012f : 1; }
        if (action == 1) { int[] bob = { 0, -3, -1, 1, 0, -3, -1, 1 }; int[] sway = { -2, 0, 2, 1, -2, 0, 2, 1 }; dy = bob[frame]; dx = sway[frame]; angle = sway[frame] * .7f; }
        if (action == 2) { float[] lean = { 0, -3, -7, -12, 10, 6, 2, 0 }; int[] rush = { 0, 1, 3, 8, 13, 8, 3, 0 }; angle = lean[frame]; dx = rush[frame]; dy = frame == 4 ? -2 : 0; scaleX = frame == 4 ? 1.05f : 1; }
        if (action == 3) { int[] shake = { -5, 5, -4, 2, 0 }; dx = shake[frame]; angle = -shake[frame] * .8f; scaleY = frame == 1 ? .94f : 1; }
        if (action == 4) { float[] fall = { 0, 8, 20, 38, 58, 76 }; int[] drop = { 0, 3, 10, 21, 35, 47 }; angle = fall[frame]; dy = drop[frame]; scaleY = frame >= 4 ? .92f : 1; }
    }

    private static void DrawSealedFrame(Bitmap source, Rectangle crop, int stage, int action, int frame, string path)
    {
        using (var output = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(output))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            float angle, sx, sy;
            int dx, dy;
            Pose(action, frame, out angle, out dx, out dy, out sx, out sy);
            float targetHeight = stage <= 2 ? 190 : stage == 3 ? 198 : 205;
            float ratio = targetHeight / crop.Height;
            float w = crop.Width * ratio * sx;
            float h = crop.Height * ratio * sy;
            g.TranslateTransform(128 + dx, 230 + dy);
            g.RotateTransform(angle);
            var dest = new RectangleF(-w / 2, -h, w, h);
            if (action == 3 && frame < 3)
            {
                var matrix = new ColorMatrix(new float[][] {
                    new float[] {1,0,0,0,0}, new float[] {0,.65f,0,0,0}, new float[] {0,0,.65f,0,0},
                    new float[] {0,0,0,1,0}, new float[] {.18f,0,0,0,1} });
                using (var attributes = new ImageAttributes())
                { attributes.SetColorMatrix(matrix); g.DrawImage(source, Rectangle.Round(dest), crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes); }
            }
            else g.DrawImage(source, dest, crop, GraphicsUnit.Pixel);
            output.Save(path, ImageFormat.Png);
        }
    }

    private static void MakeAwakened(string sourcePath, string outputPath, int action, int frame)
    {
        using (var source = new Bitmap(sourcePath))
        using (var output = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(output))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            var matrix = new ColorMatrix(new float[][] {
                new float[] {1.08f,0,0,0,0}, new float[] {0,1.05f,0,0,0}, new float[] {0,0,1.22f,0,0},
                new float[] {0,0,0,1,0}, new float[] {.02f,.03f,.07f,0,1} });
            using (var attributes = new ImageAttributes())
            { attributes.SetColorMatrix(matrix); g.DrawImage(source, new Rectangle(0,0,256,256), 0,0,256,256, GraphicsUnit.Pixel, attributes); }
            if (action != 4 || frame < 3)
            {
                using (var cyan = new Pen(Color.FromArgb(220, 80, 224, 255), 3))
                using (var gold = new Pen(Color.FromArgb(220, 255, 210, 92), 2))
                {
                    g.DrawPolygon(cyan, new[] { new Point(110,55), new Point(116,39), new Point(122,55), new Point(128,34), new Point(134,55), new Point(141,41), new Point(146,57) });
                    g.DrawArc(gold, 106, 47, 45, 18, 185, 170);
                }
            }
            output.Save(outputPath, ImageFormat.Png);
        }
    }

    public static void Run(string lineupPath, string originalFrames, string outputRoot)
    {
        using (var lineup = new Bitmap(lineupPath))
        {
            for (int stage = 1; stage <= 4; stage++)
            using (var source = ExtractStage(lineup, stage))
            {
                Rectangle crop = Bounds(source);
                string folder = Path.Combine(outputRoot, "stage_" + stage);
                Directory.CreateDirectory(folder);
                for (int action = 0; action < Names.Length; action++)
                for (int frame = 0; frame < Counts[action]; frame++)
                    DrawSealedFrame(source, crop, stage, action, frame, Path.Combine(folder, "grania_" + Names[action] + "_" + frame + ".png"));
            }
        }
        for (int stage = 5; stage <= 6; stage++)
        {
            string folder = Path.Combine(outputRoot, "stage_" + stage);
            Directory.CreateDirectory(folder);
            for (int action = 0; action < Names.Length; action++)
            for (int frame = 0; frame < Counts[action]; frame++)
            {
                string name = "grania_" + Names[action] + "_" + frame + ".png";
                string source = Path.Combine(originalFrames, name);
                string destination = Path.Combine(folder, name);
                if (stage == 5) File.Copy(source, destination, true);
                else MakeAwakened(source, destination, action, frame);
            }
        }
    }
}
