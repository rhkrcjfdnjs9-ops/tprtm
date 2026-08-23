using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class SplitSpriteFrames
{
    private static readonly int[][] BaseSequences = {
        new[] { 0, 1, 2, 3, 2, 1 },
        new[] { 0, 1, 2, 3, 0, 1, 2, 3 },
        new[] { 0, 1, 2, 3, 3 },
    };

    private static void SaveFrame(Bitmap source, int columns, int rows, int column, int row, string path)
    {
        int cellWidth = source.Width / columns;
        int cellHeight = source.Height / rows;
        using (var frame = new Bitmap(cellWidth, cellHeight, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(frame))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(source, new Rectangle(0, 0, cellWidth, cellHeight), new Rectangle(column * cellWidth, row * cellHeight, cellWidth, cellHeight), GraphicsUnit.Pixel);
            frame.Save(path, ImageFormat.Png);
        }
    }

    public static void Run(string baseSheet, string actionSheet, string outputFolder, string prefix)
    {
        Directory.CreateDirectory(outputFolder);
        using (var baseImage = new Bitmap(baseSheet))
        using (var actionImage = new Bitmap(actionSheet))
        {
            string[] names = { "idle", "walk", "hit" };
            int[] rows = { 0, 1, 3 };
            for (int animation = 0; animation < names.Length; animation++)
            {
                for (int frame = 0; frame < BaseSequences[animation].Length; frame++)
                {
                    SaveFrame(baseImage, 4, 4, BaseSequences[animation][frame], rows[animation], Path.Combine(outputFolder, prefix + "_" + names[animation] + "_" + frame + ".png"));
                }
            }
            for (int frame = 0; frame < 8; frame++)
                SaveFrame(actionImage, 8, 2, frame, 0, Path.Combine(outputFolder, prefix + "_attack_" + frame + ".png"));
            for (int frame = 0; frame < 6; frame++)
                SaveFrame(actionImage, 8, 2, frame, 1, Path.Combine(outputFolder, prefix + "_death_" + frame + ".png"));
        }
    }
}
