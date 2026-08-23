using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class PrepareSpriteSheet
{
    private static bool IsBackground(Color c)
    {
        int max = Math.Max(c.R, Math.Max(c.G, c.B));
        int min = Math.Min(c.R, Math.Min(c.G, c.B));
        return c.A == 0 || (min >= 220 && max - min <= 12);
    }

    public static void Run(string source, string destination)
    {
        RunSized(source, destination, 1024, 1024);
    }

    public static void RunSized(string source, string destination, int outputWidth, int outputHeight)
    {
        using (var original = new Bitmap(source))
        using (var transparent = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb))
        {
            using (var graphics = Graphics.FromImage(transparent)) graphics.DrawImageUnscaled(original, 0, 0);
            var seen = new bool[transparent.Width, transparent.Height];
            var queue = new Queue<Point>();
            for (int x = 0; x < transparent.Width; x++) { queue.Enqueue(new Point(x, 0)); queue.Enqueue(new Point(x, transparent.Height - 1)); }
            for (int y = 0; y < transparent.Height; y++) { queue.Enqueue(new Point(0, y)); queue.Enqueue(new Point(transparent.Width - 1, y)); }
            while (queue.Count > 0)
            {
                Point point = queue.Dequeue();
                if (point.X < 0 || point.Y < 0 || point.X >= transparent.Width || point.Y >= transparent.Height || seen[point.X, point.Y]) continue;
                seen[point.X, point.Y] = true;
                Color color = transparent.GetPixel(point.X, point.Y);
                if (!IsBackground(color)) continue;
                transparent.SetPixel(point.X, point.Y, Color.Transparent);
                queue.Enqueue(new Point(point.X + 1, point.Y)); queue.Enqueue(new Point(point.X - 1, point.Y));
                queue.Enqueue(new Point(point.X, point.Y + 1)); queue.Enqueue(new Point(point.X, point.Y - 1));
            }
            using (var output = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(output))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(transparent, new Rectangle(0, 0, outputWidth, outputHeight), 0, 0, transparent.Width, transparent.Height, GraphicsUnit.Pixel);
                output.Save(destination, ImageFormat.Png);
            }
        }
    }
}
