using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using DropBoard.Native.Services;
using Microsoft.Win32;

namespace DropBoard.Native
{
    public partial class PaletteStudioWindow : Window
    {
        private readonly CardItem? _sourceCard;
        private readonly BitmapSource? _sourceBitmap;
        private readonly Action<BitmapSource>? _onAddToBoard;
        private readonly Action<List<PalettePin>>? _onExportToAe;

        private List<PalettePin> _pins = new();
        private int _colorCount = 5;
        private ColorMood _currentMood = ColorMood.Colorful;
        private int _activePinIndex = 0;
        private int _seed = 100;

        public PaletteStudioWindow(
            CardItem? sourceCard,
            BitmapSource? sourceBitmap,
            Action<BitmapSource>? onAddToBoard,
            Action<List<PalettePin>>? onExportToAe)
        {
            InitializeComponent();

            _sourceCard = sourceCard;
            _sourceBitmap = sourceBitmap ?? sourceCard?.Bitmap ?? sourceCard?.OriginalBitmap;
            _onAddToBoard = onAddToBoard;
            _onExportToAe = onExportToAe;

            if (_sourceBitmap != null)
            {
                ImgPreview.Source = _sourceBitmap;
            }

            Loaded += PaletteStudioWindow_Loaded;
        }

        private void PaletteStudioWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GeneratePalette();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void GeneratePalette(bool isRandom = false)
        {
            if (_sourceBitmap == null) return;

            if (isRandom)
            {
                _seed = new Random().Next(1, 100000);
            }

            _pins = ColorPaletteExtractor.ExtractPalette(_sourceBitmap, _colorCount, _currentMood, _seed, _pins);
            UpdateSwatchesUI();
            UpdatePinsUI();
        }

        private void CmbMood_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbMood.SelectedItem is ComboBoxItem item && Enum.TryParse<ColorMood>(item.Content.ToString(), out var mood))
            {
                _currentMood = mood;
                GeneratePalette();
            }
        }

        private void BtnDecCount_Click(object sender, RoutedEventArgs e)
        {
            if (_colorCount > 3)
            {
                _colorCount--;
                TxtCount.Text = _colorCount.ToString();
                GeneratePalette();
            }
        }

        private void BtnIncCount_Click(object sender, RoutedEventArgs e)
        {
            if (_colorCount < 10)
            {
                _colorCount++;
                TxtCount.Text = _colorCount.ToString();
                GeneratePalette();
            }
        }

        private void BtnRandomize_Click(object sender, MouseButtonEventArgs e)
        {
            GeneratePalette(isRandom: true);
        }

        #region Swatches UI Rendering

        private void UpdateSwatchesUI()
        {
            SwatchesContainerGrid.Children.Clear();
            SwatchesContainerGrid.ColumnDefinitions.Clear();

            int count = _pins.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                SwatchesContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (int i = 0; i < count; i++)
            {
                int index = i;
                var pin = _pins[i];

                Grid colGrid = new Grid
                {
                    Margin = new Thickness(i == 0 ? 0 : 4, 0, i == count - 1 ? 0 : 4, 0)
                };
                colGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                colGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Big Main Swatch Bar
                Border swatchBar = new Border
                {
                    Background = new SolidColorBrush(pin.Color),
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Click to select Pin #{index + 1} ({pin.Hex})"
                };

                swatchBar.MouseLeftButtonDown += (s, e) =>
                {
                    _activePinIndex = index;
                    HighlightActivePin();
                };

                Grid.SetRow(swatchBar, 0);
                colGrid.Children.Add(swatchBar);

                // Bottom Control Pill (Hex, Copy, Lock)
                Border bottomPill = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 20, 25, 36)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 8, 0, 0),
                    Padding = new Thickness(4, 6, 4, 6)
                };

                StackPanel pillSp = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };

                // Hex text
                TextBlock hexText = new TextBlock
                {
                    Text = pin.Hex,
                    FontFamily = new FontFamily("Consolas, Segoe UI"),
                    FontWeight = FontWeights.Bold,
                    FontSize = count > 7 ? 10.5 : 12.0,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Action buttons row (Copy + Lock)
                StackPanel actionSp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                // Copy button
                Border btnCopy = new Border
                {
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 2, 4, 2),
                    Cursor = Cursors.Hand,
                    ToolTip = "Copy Hex Code"
                };
                TextBlock txtCopy = new TextBlock { Text = "📋", FontSize = 10, Foreground = Brushes.White };
                btnCopy.Child = txtCopy;
                btnCopy.MouseEnter += (s, e) => btnCopy.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                btnCopy.MouseLeave += (s, e) => btnCopy.Background = Brushes.Transparent;
                btnCopy.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    try
                    {
                        Clipboard.SetText(pin.Hex);
                        txtCopy.Text = "✓";
                        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                        timer.Tick += (s2, e2) => { txtCopy.Text = "📋"; timer.Stop(); };
                        timer.Start();
                    }
                    catch { }
                };

                // Lock button
                Border btnLock = new Border
                {
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(4, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = pin.IsLocked ? "Locked (click to unlock)" : "Lock this color"
                };
                TextBlock txtLock = new TextBlock
                {
                    Text = pin.IsLocked ? "🔒" : "🔓",
                    FontSize = 10,
                    Foreground = pin.IsLocked ? new SolidColorBrush(Color.FromRgb(251, 191, 36)) : new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
                };
                btnLock.Child = txtLock;
                btnLock.MouseEnter += (s, e) => btnLock.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                btnLock.MouseLeave += (s, e) => btnLock.Background = Brushes.Transparent;
                btnLock.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    pin.IsLocked = !pin.IsLocked;
                    txtLock.Text = pin.IsLocked ? "🔒" : "🔓";
                    txtLock.Foreground = pin.IsLocked ? new SolidColorBrush(Color.FromRgb(251, 191, 36)) : new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
                };

                actionSp.Children.Add(btnCopy);
                actionSp.Children.Add(btnLock);

                pillSp.Children.Add(hexText);
                pillSp.Children.Add(actionSp);
                bottomPill.Child = pillSp;

                Grid.SetRow(bottomPill, 1);
                colGrid.Children.Add(bottomPill);

                Grid.SetColumn(colGrid, i);
                SwatchesContainerGrid.Children.Add(colGrid);
            }
        }

        #endregion

        #region Sampling Pins UI & Dragging

        private void ImgPreview_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePinsLayout();
        }

        private void ImageHostGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePinsLayout();
        }

        private void UpdatePinsLayout()
        {
            if (ImgPreview.ActualWidth <= 0 || ImgPreview.ActualHeight <= 0) return;

            PinsCanvas.Width = ImgPreview.ActualWidth;
            PinsCanvas.Height = ImgPreview.ActualHeight;

            for (int i = 0; i < PinsCanvas.Children.Count; i++)
            {
                if (i < _pins.Count && PinsCanvas.Children[i] is FrameworkElement pinElem)
                {
                    var pin = _pins[i];
                    double px = pin.RelX * ImgPreview.ActualWidth - 14.0;
                    double py = pin.RelY * ImgPreview.ActualHeight - 14.0;
                    Canvas.SetLeft(pinElem, px);
                    Canvas.SetTop(pinElem, py);
                }
            }
        }

        private void UpdatePinsUI()
        {
            PinsCanvas.Children.Clear();
            if (ImgPreview.ActualWidth > 0 && ImgPreview.ActualHeight > 0)
            {
                PinsCanvas.Width = ImgPreview.ActualWidth;
                PinsCanvas.Height = ImgPreview.ActualHeight;
            }

            for (int i = 0; i < _pins.Count; i++)
            {
                int index = i;
                var pin = _pins[i];

                // Outer Pin Ring (Adobe Express Style Ring)
                Border pinRing = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    BorderThickness = new Thickness(2.5),
                    BorderBrush = Brushes.White,
                    Cursor = Cursors.Hand,
                    Tag = index,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 8,
                        ShadowDepth = 2,
                        Opacity = 0.8
                    }
                };

                // Center Swatch Dot inside Ring
                Border centerDot = new Border
                {
                    Width = 14,
                    Height = 14,
                    CornerRadius = new CornerRadius(7),
                    Background = new SolidColorBrush(pin.Color),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                pinRing.Child = centerDot;

                // Wire Dragging
                bool isDragging = false;
                Point dragStartMouse;

                pinRing.MouseLeftButtonDown += (s, e) =>
                {
                    isDragging = true;
                    _activePinIndex = index;
                    HighlightActivePin();
                    pinRing.CaptureMouse();
                    dragStartMouse = e.GetPosition(PinsCanvas);
                    e.Handled = true;
                };

                pinRing.MouseMove += (s, e) =>
                {
                    if (isDragging && _sourceBitmap != null && ImgPreview.ActualWidth > 0 && ImgPreview.ActualHeight > 0)
                    {
                        Point curPos = e.GetPosition(PinsCanvas);
                        double relX = Math.Clamp(curPos.X / ImgPreview.ActualWidth, 0.0, 1.0);
                        double relY = Math.Clamp(curPos.Y / ImgPreview.ActualHeight, 0.0, 1.0);

                        pin.RelX = relX;
                        pin.RelY = relY;

                        // Sample pixel color live!
                        Color sampled = ColorPaletteExtractor.SampleColorAt(_sourceBitmap, relX, relY);
                        pin.Color = sampled;
                        centerDot.Background = new SolidColorBrush(sampled);

                        // Move pin element visually
                        Canvas.SetLeft(pinRing, curPos.X - 14.0);
                        Canvas.SetTop(pinRing, curPos.Y - 14.0);

                        // Update swatch column real-time
                        UpdateSwatchesUI();
                        e.Handled = true;
                    }
                };

                pinRing.MouseLeftButtonUp += (s, e) =>
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        pinRing.ReleaseMouseCapture();
                        e.Handled = true;
                    }
                };

                PinsCanvas.Children.Add(pinRing);
            }

            UpdatePinsLayout();
            HighlightActivePin();
        }

        private void HighlightActivePin()
        {
            for (int i = 0; i < PinsCanvas.Children.Count; i++)
            {
                if (PinsCanvas.Children[i] is Border ring)
                {
                    bool isActive = (i == _activePinIndex);
                    ring.BorderBrush = isActive ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : Brushes.White;
                    ring.BorderThickness = isActive ? new Thickness(3.5) : new Thickness(2.0);
                }
            }
        }

        private void ImageHostGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_pins.Count == 0 || _sourceBitmap == null || ImgPreview.ActualWidth <= 0 || ImgPreview.ActualHeight <= 0) return;

            Point pt = e.GetPosition(ImgPreview);
            double relX = Math.Clamp(pt.X / ImgPreview.ActualWidth, 0.0, 1.0);
            double relY = Math.Clamp(pt.Y / ImgPreview.ActualHeight, 0.0, 1.0);

            // Move the active pin to this point
            if (_activePinIndex >= 0 && _activePinIndex < _pins.Count)
            {
                var pin = _pins[_activePinIndex];
                pin.RelX = relX;
                pin.RelY = relY;
                pin.Color = ColorPaletteExtractor.SampleColorAt(_sourceBitmap, relX, relY);

                UpdateSwatchesUI();
                UpdatePinsLayout();
            }
        }

        #endregion

        #region Actions & Exports

        private void BtnCopyImage_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                PaletteCardRenderer.CopyPaletteImageToClipboard(_pins, "Color Palette");
                MessageBox.Show("Palette graphic copied to clipboard!", "DropBoard Studio", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to copy image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSavePng_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Save Palette Graphic",
                    Filter = "PNG Image (*.png)|*.png",
                    FileName = "palette_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                };

                if (sfd.ShowDialog() == true)
                {
                    byte[] bytes = PaletteCardRenderer.RenderPalettePngBytes(_pins, "Color Palette");
                    File.WriteAllBytes(sfd.FileName, bytes);
                    MessageBox.Show("Saved palette graphic to: " + sfd.FileName, "DropBoard Studio", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportAe_Click(object sender, MouseButtonEventArgs e)
        {
            _onExportToAe?.Invoke(_pins);
            Close();
        }

        private void BtnAddToBoard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                BitmapSource rendered = PaletteCardRenderer.RenderPaletteBitmap(_pins, "Color Palette");
                _onAddToBoard?.Invoke(rendered);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to add palette card: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
