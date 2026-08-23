using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class AlignFrameAnchors
{
    private static Rectangle LargestComponent(Bitmap image)
    {
        var seen = new bool[image.Width, image.Height];
        Rectangle best = Rectangle.Empty;
        int bestCount = 0;
        for (int startY = 0; startY < image.Height; startY++)
            for (int startX = 0; startX < image.Width; startX++)
            {
                if (seen[startX, startY] || image.GetPixel(startX, startY).A <= 12) continue;
                int count = 0, left = startX, right = startX, top = startY, bottom = startY;
                var queue = new Queue<Point>();
                queue.Enqueue(new Point(startX, startY));
                seen[startX, startY] = true;
                while (queue.Count > 0)
                {
                    Point p = queue.Dequeue(); count++;
                    left = Math.Min(left, p.X); right = Math.Max(right, p.X);
                    top = Math.Min(top, p.Y); bottom = Math.Max(bottom, p.Y);
                    Point[] next = { new Point(p.X + 1, p.Y), new Point(p.X - 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X, p.Y - 1) };
                    foreach (Point n in next)
                    {
                        if (n.X < 0 || n.Y < 0 || n.X >= image.Width || n.Y >= image.Height || seen[n.X, n.Y]) continue;
                        seen[n.X, n.Y] = true;
                        if (image.GetPixel(n.X, n.Y).A > 12) queue.Enqueue(n);
                    }
                }
                if (count > bestCount) { bestCount = count; best = Rectangle.FromLTRB(left, top, right + 1, bottom + 1); }
            }
        return best;
    }

    public static void RunFolder(string folder)
    {
        foreach (string file in Directory.GetFiles(folder, "*.png"))
        {
            using (var source = new Bitmap(file))
            {
                Rectangle body = LargestComponent(source);
                if (body.IsEmpty) continue;
                int offsetX = 128 - (body.Left + body.Width / 2);
                int offsetY = 246 - body.Bottom;
                using (var output = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
                using (var graphics = Graphics.FromImage(output))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.DrawImageUnscaled(source, offsetX, offsetY);
                    output.Save(file + ".aligned", ImageFormat.Png);
                }
            }
            File.Copy(file + ".aligned", file, true);
            File.Delete(file + ".aligned");
        }
    }
}
