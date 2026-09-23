using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DropBoard.Native.Services
{
    public static class PaletteCardRenderer
    {
        private static readonly Geometry CopyIconGeometry = Geometry.Parse("M19,21H8V7h11m0-2H8a2,2 0 0,0-2,2v14a2,2 0 0,0 2,2h11a2,2 0 0,0 2-2V7a2,2 0 0,0-2-2m-3-4H4a2,2 0 0,0-2,2v14h2V3h12V1Z");

        public static BitmapSource RenderPaletteCard(List<PalettePin> pins, string title = "Color Palette", int rows = 1, double targetWidth = 0, double targetHeight = 0)
            => RenderPaletteBitmap(pins, rows, targetWidth, targetHeight);

        public static BitmapSource RenderPaletteBitmap(List<PalettePin> pins, string title = "Color Palette")
            => RenderPaletteBitmap(pins, 1, 0, 0);

        public static BitmapSource RenderPaletteBitmap(List<PalettePin> pins, int rows = 1, double targetWidth = 0, double targetHeight = 0)
        {
            if (pins == null || pins.Count == 0)
            {
                pins = new List<PalettePin>
                {
                    new PalettePin { Color = Color.FromRgb(239, 68, 68) },
                    new PalettePin { Color = Color.FromRgb(249, 115, 22) },
                    new PalettePin { Color = Color.FromRgb(16, 185, 129) },
                    new PalettePin { Color = Color.FromRgb(59, 130, 246) },
                    new PalettePin { Color = Color.FromRgb(139, 92, 246) }
                };
            }

            int count = pins.Count;
            bool isTwoRows = rows == 2 && count > 1;

            double renderW;
            double renderH;

            if (targetWidth > 0 && targetHeight > 0)
            {
                double scale = Math.Clamp(1400.0 / targetWidth, 1.0, 3.0);
                renderW = Math.Round(targetWidth * scale);
                renderH = Math.Round(targetHeight * scale);
            }
            else
            {
                renderW = 1200;
                renderH = isTwoRows ? 620 : 340;
            }

            const double dpi = 96.0;
            double pixelsPerDip = 1.0;
            try
            {
                if (Application.Current?.MainWindow != null)
                {
                    pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
                }
            }
            catch { }

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Rounded corner radius matching the canvas card
                double cornerRadius = Math.Clamp(Math.Min(renderW, renderH) * 0.065, 14.0, 26.0);

                // Clip to rounded rectangle so swatches naturally curve at borders with 100% TRANSPARENT outside
                var clipGeo = new RectangleGeometry(new Rect(0, 0, renderW, renderH), cornerRadius, cornerRadius);
                dc.PushClip(clipGeo);

                var monoFont = new Typeface(new FontFamily("Consolas, Segoe UI, monospace"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

                if (isTwoRows)
                {
                    int topCount = (int)Math.Ceiling(count / 2.0);
                    int bottomCount = count - topCount;
                    double rowH = renderH / 2.0;

                    // Top row
                    double topW = renderW / (double)topCount;
                    for (int i = 0; i < topCount; i++)
                    {
                        var pin = pins[i];
                        Rect swatchRect = new Rect(i * topW, 0, topW + 0.5, rowH + 0.5);
                        dc.DrawRectangle(new SolidColorBrush(pin.Color), null, swatchRect);

                        DrawSwatchLabel(dc, pin, swatchRect, monoFont, pixelsPerDip, isTwoRows: true);
                    }

                    // Bottom row
                    double botW = renderW / (double)bottomCount;
                    for (int j = 0; j < bottomCount; j++)
                    {
                        var pin = pins[topCount + j];
                        Rect swatchRect = new Rect(j * botW, rowH, botW + 0.5, rowH + 0.5);
                        dc.DrawRectangle(new SolidColorBrush(pin.Color), null, swatchRect);

                        DrawSwatchLabel(dc, pin, swatchRect, monoFont, pixelsPerDip, isTwoRows: true);
                    }
                }
                else
                {
                    double swatchW = renderW / (double)count;
                    for (int i = 0; i < count; i++)
                    {
                        var pin = pins[i];
                        Rect swatchRect = new Rect(i * swatchW, 0, swatchW + 0.5, renderH + 0.5);
                        dc.DrawRectangle(new SolidColorBrush(pin.Color), null, swatchRect);

                        DrawSwatchLabel(dc, pin, swatchRect, monoFont, pixelsPerDip, isTwoRows: false);
                    }
                }

                // Pop clipping
                dc.Pop();

                // Subtle transparent-card outline
                Pen borderPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), 1.5);
                dc.DrawRoundedRectangle(null, borderPen, new Rect(0.75, 0.75, renderW - 1.5, renderH - 1.5), cornerRadius, cornerRadius);
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)renderW, (int)renderH, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }

        private static void DrawSwatchLabel(DrawingContext dc, PalettePin pin, Rect rect, Typeface typeface, double pixelsPerDip, bool isTwoRows)
        {
            // Perceived luminance for high-contrast legible text
            double lum = (0.299 * pin.Color.R + 0.587 * pin.Color.G + 0.114 * pin.Color.B) / 255.0;
            bool isLight = lum > 0.58;

            Color textCol = isLight ? Color.FromArgb(235, 20, 24, 33) : Color.FromArgb(245, 255, 255, 255);
            Color iconCol = isLight ? Color.FromArgb(170, 50, 55, 65) : Color.FromArgb(190, 255, 255, 255);

            Brush textBrush = new SolidColorBrush(textCol);
            Brush iconBrush = new SolidColorBrush(iconCol);

            // Responsive font size
            double fontSize = Math.Clamp(rect.Width * 0.11, 10.0, isTwoRows ? 15.0 : 18.0);

            FormattedText hexText = new FormattedText(
                pin.Hex,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                textBrush,
                pixelsPerDip);

            double padX = Math.Clamp(rect.Width * 0.08, 6.0, 16.0);
            double padY = Math.Clamp(rect.Height * 0.06, 6.0, 16.0);

            double textX = rect.Left + padX;
            double textY = rect.Bottom - hexText.Height - padY;

            dc.DrawText(hexText, new Point(textX, textY));

            // Draw copy icon if there is enough room in the swatch cell
            double availIconSpace = rect.Width - padX - hexText.Width;
            if (availIconSpace >= 20.0)
            {
                double iconSize = Math.Clamp(fontSize * 0.9, 10.0, 16.0);
                double iconX = textX + hexText.Width + 6.0;
                double iconY = textY + (hexText.Height - iconSize) / 2.0;

                // Scale geometry from 24x24 to iconSize
                double iconScale = iconSize / 24.0;
                var transformed = CopyIconGeometry.Clone();
                transformed.Transform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform(iconScale, iconScale),
                        new TranslateTransform(iconX, iconY)
                    }
                };
                dc.DrawGeometry(iconBrush, null, transformed);
            }
        }

        public static byte[] RenderPalettePngBytes(List<PalettePin> pins, string title = "Color Palette", int rows = 1, double targetWidth = 0, double targetHeight = 0)
        {
            var bmp = RenderPaletteBitmap(pins, rows, targetWidth, targetHeight);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }

        public static void CopyPaletteImageToClipboard(List<PalettePin> pins, string title = "Color Palette", int rows = 1, double targetWidth = 0, double targetHeight = 0)
        {
            var bmp = RenderPaletteBitmap(pins, rows, targetWidth, targetHeight);
            Clipboard.SetImage(bmp);
        }

        public static string SavePaletteImageToFile(List<PalettePin> pins, string targetDir, string title = "Color Palette", int rows = 1, double targetWidth = 0, double targetHeight = 0)
        {
            Directory.CreateDirectory(targetDir);
            string safeTitle = string.Concat(title.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "palette";
            string filename = $"{safeTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(targetDir, filename);

            byte[] bytes = RenderPalettePngBytes(pins, title, rows, targetWidth, targetHeight);
            File.WriteAllBytes(fullPath, bytes);
            return fullPath;
        }
    }
}
