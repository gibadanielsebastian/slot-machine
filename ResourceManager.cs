using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D; // Added for SmoothingMode
using System.IO;
using System.Windows.Forms;

namespace LuckySpin
{
    public static class ResourceManager
    {
        public static Dictionary<SymbolType, Image> Images = new Dictionary<SymbolType, Image>();

        public static void LoadResources()
        {
            foreach (SymbolType type in Enum.GetValues(typeof(SymbolType)))
            {
                string path = Path.Combine(Application.StartupPath, "Images", $"{type.ToString().ToLower()}.png");
                
                if (File.Exists(path))
                {
                    Images[type] = Image.FromFile(path);
                }
                else
                {
                    Images[type] = CreateFallbackImage(type.ToString());
                }
            }
        }

        // Temporary method to generate winning line images
        public static void GenerateWinningLineImages()
        {
            var payLines = new List<int[]>
            {
                new[] { 1, 1, 1, 1, 1 }, // Line 1: Middle
                new[] { 0, 0, 0, 0, 0 }, // Line 2: Top
                new[] { 2, 2, 2, 2, 2 }, // Line 3: Bottom
                new[] { 0, 1, 2, 1, 0 }, // Line 4: V-shape (top-middle-bottom-middle-top)
                new[] { 2, 1, 0, 1, 2 }, // Line 5: A-shape (bottom-middle-top-middle-bottom)
                new[] { 0, 0, 1, 2, 2 }, // Line 6
                new[] { 2, 2, 1, 0, 0 }, // Line 7
                new[] { 1, 0, 0, 0, 1 }, // Line 8
                new[] { 1, 2, 2, 2, 1 }, // Line 9
                new[] { 0, 1, 0, 1, 0 }  // Line 10
            };
            var lineColors = new Color[]
            {
                Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue,
                Color.Indigo, Color.Violet, Color.White, Color.Cyan, Color.Magenta
            };

            string outputDir = Path.Combine(Application.StartupPath, "Images", "WinningLines");
            Directory.CreateDirectory(outputDir); // Ensure directory exists

            for(int lineIndex = 0; lineIndex < payLines.Count; lineIndex++)
            {
                var paylinePath = payLines[lineIndex];
                var color = lineColors[lineIndex];
                string lineName = $"line-{string.Join("", paylinePath)}.png";
                string outputPath = Path.Combine(outputDir, lineName);

                using (var bmp = new Bitmap(1000, 600, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) // Ensure transparency
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent); // Make background transparent
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (Pen pen = new Pen(color, 20))
                        {
                            PointF[] points = new PointF[5];
                            float reelWidth = (float)bmp.Width / 5;
                            float symbolHeight = (float)bmp.Height / 3;

                            for (int i = 0; i < 5; i++)
                            {
                                float x = (i * reelWidth) + (reelWidth / 2);
                                float y = (paylinePath[i] * symbolHeight) + (symbolHeight / 2); // 0=Top, 1=Middle, 2=Bottom
                                points[i] = new PointF(x, y);
                            }
                            g.DrawLines(pen, points);
                        }
                    }
                    bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private static Image CreateFallbackImage(string text)
        {
            Bitmap bmp = new Bitmap(100, 100);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.DrawRectangle(Pens.Gold, 0, 0, 99, 99);
                g.DrawString(text, new Font("Arial", 10), Brushes.White, 10, 40);
            }
            return bmp;
        }
    }
}