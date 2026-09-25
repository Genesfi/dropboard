using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DropBoard.Native.Services
{
    public enum ColorMood
    {
        Colorful,
        Bright,
        Muted,
        Deep,
        Dark,
        Dominant
    }

    public class PalettePin
    {
        public double RelX { get; set; } = 0.5; // 0.0 to 1.0 relative to image
        public double RelY { get; set; } = 0.5;
        public Color Color { get; set; } = Colors.White;
        public string Hex => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";
        public bool IsLocked { get; set; } = false;
    }

    public static class ColorPaletteExtractor
    {
        private class SamplePoint
        {
            public double RelX;
            public double RelY;
            public Color Color;
            public double Hue;
            public double Sat;
            public double Val;
            public double Weight = 1.0;
        }

        public static List<PalettePin> ExtractPalette(BitmapSource sourceBmp, int count, ColorMood mood, int seed = 0, List<PalettePin>? existingPins = null)
        {
            count = Math.Clamp(count, 3, 10);
            var pins = new List<PalettePin>();

            // Preserve locked pins if provided
            if (existingPins != null)
            {
                foreach (var p in existingPins)
                {
                    if (p.IsLocked && pins.Count < count)
                    {
                        pins.Add(new PalettePin
                        {
                            RelX = p.RelX,
                            RelY = p.RelY,
                            Color = p.Color,
                            IsLocked = true
                        });
                    }
                }
            }

            int needed = count - pins.Count;
            if (needed <= 0) return pins.Take(count).ToList();

            if (sourceBmp == null)
            {
                return GenerateFallbackPins(count, pins);
            }

            try
            {
                // Safe format conversion
                FormatConvertedBitmap formatted = new FormatConvertedBitmap(sourceBmp, PixelFormats.Bgra32, null, 0);
                int w = formatted.PixelWidth;
                int h = formatted.PixelHeight;
                if (w <= 0 || h <= 0) return GenerateFallbackPins(count, pins);

                // Downsample using RenderTargetBitmap into a clean fixed grid
                int targetW = 140;
                int targetH = Math.Max(10, (int)Math.Round(140.0 / Math.Max(0.1, (double)w / h)));
                if (targetH > 140)
                {
                    targetH = 140;
                    targetW = Math.Max(10, (int)Math.Round(140.0 * Math.Max(0.1, (double)w / h)));
                }

                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    dc.DrawImage(formatted, new Rect(0, 0, targetW, targetH));
                }
                RenderTargetBitmap rtb = new RenderTargetBitmap(targetW, targetH, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);
                rtb.Freeze();

                int sw = rtb.PixelWidth;
                int sh = rtb.PixelHeight;
                int stride = sw * 4;
                byte[] pixels = new byte[sh * stride];
                rtb.CopyPixels(pixels, stride, 0);

                // Collect valid samples
                var samples = new List<SamplePoint>();

                for (int y = 0; y < sh; y += 2)
                {
                    for (int x = 0; x < sw; x += 2)
                    {
                        int idx = y * stride + x * 4;
                        if (idx + 3 >= pixels.Length) continue;

                        byte b = pixels[idx];
                        byte g = pixels[idx + 1];
                        byte r = pixels[idx + 2];
                        byte a = pixels[idx + 3];
                        if (a < 64) continue; // Skip transparent

                        Color c = Color.FromRgb(r, g, b);
                        var (hue, sat, val) = RgbToHsv(c);

                        double relX = (double)x / Math.Max(1, sw - 1);
                        double relY = (double)y / Math.Max(1, sh - 1);

                        double moodScore = CalculateMoodScore(sat, val, mood);

                        samples.Add(new SamplePoint
                        {
                            RelX = relX,
                            RelY = relY,
                            Color = c,
                            Hue = hue,
                            Sat = sat,
                            Val = val,
                            Weight = moodScore
                        });
                    }
                }

                if (samples.Count == 0) return GenerateFallbackPins(count, pins);

                List<SamplePoint> candidates;
                var selected = new List<SamplePoint>();

                if (seed != 0)
                {
                    // Randomizer based on seed (e.g. from 🎲 shuffle/randomize button)
                    Random rng = new Random(seed);
                    candidates = samples.OrderByDescending(s => s.Weight + (rng.NextDouble() * 0.4)).Take(Math.Min(400, samples.Count)).ToList();
                    selected.Add(candidates[rng.Next(0, Math.Min(5, candidates.Count))]);
                }
                else
                {
                    // Deterministic mode: strictly by weight with zero random jitter, picking top representative sample
                    candidates = samples.OrderByDescending(s => s.Weight).Take(Math.Min(400, samples.Count)).ToList();
                    selected.Add(candidates[0]);
                }

                while (selected.Count < needed && candidates.Count > 0)
                {
                    SamplePoint? bestCandidate = null;
                    double maxMinDist = -1.0;

                    for (int i = 0; i < Math.Min(80, candidates.Count); i++)
                    {
                        var cand = candidates[i];
                        double minDist = double.MaxValue;

                        foreach (var sel in selected)
                        {
                            double d = ColorDistanceHsv(cand, sel);
                            if (d < minDist) minDist = d;
                        }

                        foreach (var lk in pins)
                        {
                            var (lh, ls, lv) = RgbToHsv(lk.Color);
                            double d = ColorDistanceHsv(cand, new SamplePoint { Hue = lh, Sat = ls, Val = lv });
                            if (d < minDist) minDist = d;
                        }

                        double score = minDist * cand.Weight;
                        if (score > maxMinDist)
                        {
                            maxMinDist = score;
                            bestCandidate = cand;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        selected.Add(bestCandidate);
                        candidates.Remove(bestCandidate);
                    }
                    else
                    {
                        break;
                    }
                }

                var sortedSelected = selected.OrderBy(s => s.Hue).ToList();

                foreach (var s in sortedSelected)
                {
                    pins.Add(new PalettePin
                    {
                        RelX = Math.Clamp(s.RelX, 0.05, 0.95),
                        RelY = Math.Clamp(s.RelY, 0.05, 0.95),
                        Color = s.Color
                    });
                }
            }
            catch
            {
                return GenerateFallbackPins(count, pins);
            }

            return pins;
        }

        private static List<PalettePin> GenerateFallbackPins(int count, List<PalettePin> existing)
        {
            string[] fallbacks = { "#38BDF8", "#0284C7", "#F59E0B", "#EF4444", "#10B981", "#8B5CF6", "#EC4899", "#14B8A6" };
            for (int i = existing.Count; i < count; i++)
            {
                Color c = (Color)ColorConverter.ConvertFromString(fallbacks[i % fallbacks.Length]);
                existing.Add(new PalettePin
                {
                    RelX = 0.15 + (i * 0.15),
                    RelY = 0.5,
                    Color = c
                });
            }
            return existing;
        }

        public static Color SampleColorAt(BitmapSource bmp, double relX, double relY)
        {
            if (bmp == null) return Colors.White;
            try
            {
                int w = bmp.PixelWidth;
                int h = bmp.PixelHeight;
                if (w <= 0 || h <= 0) return Colors.White;

                int px = Math.Clamp((int)Math.Round(relX * (w - 1)), 0, w - 1);
                int py = Math.Clamp((int)Math.Round(relY * (h - 1)), 0, h - 1);

                int rectW = Math.Min(3, w - px);
                int rectH = Math.Min(3, h - py);
                if (rectW <= 0 || rectH <= 0) return Colors.White;

                var cb = new CroppedBitmap(bmp, new Int32Rect(px, py, rectW, rectH));
                var fc = new FormatConvertedBitmap(cb, PixelFormats.Bgra32, null, 0);
                int stride = rectW * 4;
                byte[] pixels = new byte[rectH * stride];
                fc.CopyPixels(pixels, stride, 0);

                int rSum = 0, gSum = 0, bSum = 0, count = 0;
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte b = pixels[i];
                    byte g = pixels[i + 1];
                    byte r = pixels[i + 2];
                    byte a = pixels[i + 3];
                    if (a > 30)
                    {
                        rSum += r;
                        gSum += g;
                        bSum += b;
                        count++;
                    }
                }

                if (count > 0)
                {
                    return Color.FromRgb((byte)(rSum / count), (byte)(gSum / count), (byte)(bSum / count));
                }
            }
            catch { }

            return Colors.White;
        }

        private static double CalculateMoodScore(double sat, double val, ColorMood mood)
        {
            return mood switch
            {
                ColorMood.Colorful => Math.Pow(sat, 1.2) * (val > 0.25 ? 1.0 : 0.4),
                ColorMood.Bright => (val > 0.6 ? 1.2 : 0.3) * (sat > 0.3 ? 1.0 : 0.6),
                ColorMood.Muted => (sat is >= 0.12 and <= 0.55 ? 1.4 : 0.4) * (val is >= 0.3 and <= 0.85 ? 1.2 : 0.5),
                ColorMood.Deep => Math.Pow(sat, 1.3) * (val is >= 0.2 and <= 0.7 ? 1.4 : 0.4),
                ColorMood.Dark => (val is <= 0.45 ? 1.5 : 0.3) * (sat > 0.15 ? 1.2 : 0.8),
                ColorMood.Dominant => 1.0,
                _ => 1.0
            };
        }

        private static double ColorDistanceHsv(SamplePoint a, SamplePoint b)
        {
            double dHue = Math.Abs(a.Hue - b.Hue);
            if (dHue > 180) dHue = 360 - dHue;
            double normHue = dHue / 180.0;

            double dSat = Math.Abs(a.Sat - b.Sat);
            double dVal = Math.Abs(a.Val - b.Val);

            return (normHue * 0.55) + (dSat * 0.25) + (dVal * 0.20);
        }

        public static (double h, double s, double v) RgbToHsv(Color c)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0.00001)
            {
                if (Math.Abs(max - r) < 0.00001) h = 60 * (((g - b) / delta) % 6);
                else if (Math.Abs(max - g) < 0.00001) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
                if (h < 0) h += 360;
            }

            double s = max < 0.00001 ? 0 : delta / max;
            double v = max;
            return (h, s, v);
        }
    }
}
