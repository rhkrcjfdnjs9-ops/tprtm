using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class NormalizeActionSheet
{
    private static bool IsBackground(Color color)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));
        return color.A == 0 || (min >= 218 && max - min <= 14);
    }

    private static Bitmap ExtractCell(Bitmap source, Rectangle area)
    {
        var cell = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(cell)) graphics.DrawImage(source, new Rectangle(0, 0, area.Width, area.Height), area, GraphicsUnit.Pixel);
        var seen = new bool[cell.Width, cell.Height];
        var queue = new Queue<Point>();
        for (int x = 0; x < cell.Width; x++) { queue.Enqueue(new Point(x, 0)); queue.Enqueue(new Point(x, cell.Height - 1)); }
        for (int y = 0; y < cell.Height; y++) { queue.Enqueue(new Point(0, y)); queue.Enqueue(new Point(cell.Width - 1, y)); }
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            if (p.X < 0 || p.Y < 0 || p.X >= cell.Width || p.Y >= cell.Height || seen[p.X, p.Y]) continue;
            seen[p.X, p.Y] = true;
            Color color = cell.GetPixel(p.X, p.Y);
            if (!IsBackground(color)) continue;
            cell.SetPixel(p.X, p.Y, Color.Transparent);
            queue.Enqueue(new Point(p.X + 1, p.Y)); queue.Enqueue(new Point(p.X - 1, p.Y));
            queue.Enqueue(new Point(p.X, p.Y + 1)); queue.Enqueue(new Point(p.X, p.Y - 1));
        }
        return cell;
    }

    private static Rectangle ContentBounds(Bitmap image)
    {
        int left = image.Width, top = image.Height, right = -1, bottom = -1;
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
                if (image.GetPixel(x, y).A > 12) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    public static void Run(string sourcePath, string destinationPath)
    {
        using (var source = new Bitmap(sourcePath))
        {
            int sourceWidth = source.Width / 8;
            int sourceHeight = source.Height / 2;
            var cells = new Bitmap[16];
            var bounds = new Rectangle[16];
            int maxWidth = 1, maxHeight = 1;
            for (int row = 0; row < 2; row++)
                for (int column = 0; column < 8; column++)
                {
                    int index = row * 8 + column;
                    cells[index] = ExtractCell(source, new Rectangle(column * sourceWidth, row * sourceHeight, sourceWidth, sourceHeight));
                    bounds[index] = ContentBounds(cells[index]);
                    if (!bounds[index].IsEmpty) { maxWidth = Math.Max(maxWidth, bounds[index].Width); maxHeight = Math.Max(maxHeight, bounds[index].Height); }
                }
            float scale = Math.Min(236f / maxWidth, 236f / maxHeight);
            using (var output = new Bitmap(2048, 512, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(output))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                for (int index = 0; index < 16; index++)
                {
                    Rectangle box = bounds[index];
                    if (box.IsEmpty) { cells[index].Dispose(); continue; }
                    int width = Math.Max(1, (int)Math.Round(box.Width * scale));
                    int height = Math.Max(1, (int)Math.Round(box.Height * scale));
                    int cellX = (index % 8) * 256;
                    int cellY = (index / 8) * 256;
                    var destination = new Rectangle(cellX + (256 - width) / 2, cellY + 248 - height, width, height);
                    graphics.DrawImage(cells[index], destination, box, GraphicsUnit.Pixel);
                    cells[index].Dispose();
                }
                output.Save(destinationPath, ImageFormat.Png);
            }
        }
    }
}
