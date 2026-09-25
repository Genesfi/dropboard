using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using DropBoard.Native.Services;
using Microsoft.Win32;
using XamlAnimatedGif;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DropBoard.Native
{
    public enum ResizeCorner
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Right,
        Left,
        Top,
        Bottom
    }

    public enum ToastType
    {
        Success,
        Info,
        Error
    }

    public class CanvasSnapshot
    {
        public string ActionName { get; set; } = "";
        public List<CardSnapshot> Cards { get; set; } = new();
        public List<GroupSnapshot> Groups { get; set; } = new();
        public double MatrixM11 { get; set; } = 1.0;
        public double MatrixOffsetX { get; set; }
        public double MatrixOffsetY { get; set; }
    }

    public class GroupSnapshot
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Color { get; set; } = "";
        public string Notes { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class GroupItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "Scene 01";
        public string Color { get; set; } = "#3B82F6";
        public string Notes { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 460;
        public double Height { get; set; } = 380;
        public Grid Container { get; set; } = null!;
        public Border? HeaderBorder { get; set; }
        public Border FrameBorder { get; set; } = null!;
        public TextBox? NotesBox { get; set; }
        public TextBlock TitleText { get; set; } = null!;
        public TextBlock CountBadge { get; set; } = null!;
        public Border ColorDot { get; set; } = null!;
        public Border? ResizeHandle { get; set; }
        public Border HandleTL { get; set; } = null!;
        public Border HandleTR { get; set; } = null!;
        public Border HandleBL { get; set; } = null!;
        public Border HandleBR { get; set; } = null!;
        public Border? HoverToolbar { get; set; }
        public bool IsSelected { get; set; } = false;
    }

    public class NoteChecklistItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Text { get; set; } = "";
        public bool IsChecked { get; set; } = false;
    }

    public class CardSnapshot
    {
        public string Id { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string LocalPath { get; set; } = "";
        public string Base64Data { get; set; } = "";
        public BitmapSource Bitmap { get; set; } = null!;
        public BitmapSource? OriginalBitmap { get; set; }
        public double BaseWidth { get; set; }
        public double BaseHeight { get; set; }
        public double BaseX { get; set; }
        public double BaseY { get; set; }
        public double CropTop { get; set; }
        public double CropRight { get; set; }
        public double CropBottom { get; set; }
        public double CropLeft { get; set; }
        public bool IsYouTube { get; set; } = false;
        public string YouTubeId { get; set; } = "";
        public string YouTubeUrl { get; set; } = "";
        public double LastPlaybackSeconds { get; set; } = 0;
        public bool IsLocalVideo { get; set; } = false;
        public string VideoFilePath { get; set; } = "";
        public bool IsVideoLooping { get; set; } = true;
        public bool IsVideoMuted { get; set; } = true;
        public double VideoDurationSeconds { get; set; } = 0;
        public bool IsNote { get; set; } = false;
        public string NoteText { get; set; } = "";
        public string NoteFontFamily { get; set; } = "Segoe UI";
        public double NoteFontSize { get; set; } = 16.0;
        public string NoteTextColor { get; set; } = "#FFFFFF";
        public string NoteBgColor { get; set; } = "Transparent";
        public TextAlignment NoteAlignment { get; set; } = TextAlignment.Left;
        public bool NoteHasShadow { get; set; } = false;
        public bool HasDeadline { get; set; } = false;
        public string? DeadlineIso { get; set; } = null;
        public string DeadlineLabel { get; set; } = "Deadline";
        public bool IsChecklist { get; set; } = false;
        public string ChecklistJson { get; set; } = "";
        public string NoteDoodleInkBase64 { get; set; } = "";
        public string? NoteBgGifPath { get; set; } = null;
        public string? NoteBgGifBase64 { get; set; } = null;
        public bool IsPaletteCard { get; set; } = false;
        public string PalettePinsData { get; set; } = "";
        public int PaletteColorCount { get; set; } = 5;
        public ColorMood PaletteMood { get; set; } = ColorMood.Colorful;
        public int PaletteRows { get; set; } = 1;
        public string? LinkedSourceCardId { get; set; } = null;
        public string? GroupId { get; set; } = null;
        public bool IsDrawCard { get; set; } = false;
        public string DrawInkBase64 { get; set; } = "";
        public string DrawPenColor { get; set; } = "#38BDF8";
        public double DrawPenSize { get; set; } = 3.0;
        public bool DrawIsEraser { get; set; } = false;
    }

    public class CardItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string? GroupId { get; set; } = null;
        public string LocalPath { get; set; } = "";
        public string Base64Data { get; set; } = "";
        public Grid Container { get; set; } = null!;
        public Border ContentBorder { get; set; } = null!;
        public Image ImageControl { get; set; } = null!;
        public BitmapSource Bitmap { get; set; } = null!;
        public BitmapSource OriginalBitmap { get; set; } = null!;
        public double BaseWidth { get; set; } = 0;
        public double BaseHeight { get; set; } = 0;
        public double BaseX { get; set; } = 0;
        public double BaseY { get; set; } = 0;
        public double CropTop { get; set; } = 0;
        public double CropRight { get; set; } = 0;
        public double CropBottom { get; set; } = 0;
        public double CropLeft { get; set; } = 0;
        public bool IsCropped => (CropTop > 0 || CropRight > 0 || CropBottom > 0 || CropLeft > 0);
        public double AspectRatio { get; set; } = 1.0;
        public bool IsSelected { get; set; } = false;
        public Border HoverToolbar { get; set; } = null!;

        // YouTube Video Player Integration
        public bool IsYouTube { get; set; } = false;
        public string YouTubeId { get; set; } = "";
        public string YouTubeUrl { get; set; } = "";
        public double LastPlaybackSeconds { get; set; } = 0;
        public bool IsPlayingYouTube { get; set; } = false;
        public MediaElement? NativePlayer { get; set; } = null;
        public Microsoft.Web.WebView2.Wpf.WebView2? PlayerControl { get; set; } = null;
        public Border? YouTubeTagBadge { get; set; } = null;
        public Border? BtnPlayOverlay { get; set; } = null;
        public Border? CenterPlayBtn { get; set; } = null;

        // Local Video Integration (100% Native WPF MediaElement & DirectX)
        public bool IsLocalVideo { get; set; } = false;
        public string VideoFilePath { get; set; } = "";
        public bool IsVideoPlaying { get; set; } = false;
        public bool IsVideoLooping { get; set; } = true;
        public bool IsVideoMuted { get; set; } = true;
        public double VideoDurationSeconds { get; set; } = 0;
        public double VideoCurrentSeconds { get; set; } = 0;
        public bool IsUserScrubbingTimeline { get; set; } = false;
        public DateTime LastUserSeekTime { get; set; } = DateTime.MinValue;
        public Border? VideoTagBadge { get; set; } = null;
        public Border? BtnVideoPlayOverlay { get; set; } = null;
        public Border? BtnVideoLoopOverlay { get; set; } = null;
        public Border? BtnVideoMuteOverlay { get; set; } = null;
        public Border? CenterVideoPlayBtn { get; set; } = null;
        public Border? VideoScrubberContainer { get; set; } = null;
        public Slider? VideoTimelineSlider { get; set; } = null;
        public TextBlock? VideoTimeText { get; set; } = null;
        public DispatcherTimer? VideoPlaybackTimer { get; set; } = null;

        // Note / Text Card Integration
        public bool IsNote { get; set; } = false;
        public string NoteText { get; set; } = "";
        public string NoteFontFamily { get; set; } = "Segoe UI";
        public double NoteFontSize { get; set; } = 16.0;
        public string NoteTextColor { get; set; } = "#FFFFFF";
        public string NoteBgColor { get; set; } = "Transparent";
        public TextAlignment NoteAlignment { get; set; } = TextAlignment.Left;
        public bool NoteHasShadow { get; set; } = false;
        public TextBox? NoteEditor { get; set; } = null;
        public Border? BtnNoteShadow { get; set; } = null;
        public TextBlock? NoteFontNameText { get; set; } = null;
        public TextBlock? NoteFontSizeText { get; set; } = null;

        // Note Deadline Integration
        public bool HasDeadline { get; set; } = false;
        public DateTime? DeadlineDateTime { get; set; } = null;
        public string DeadlineLabel { get; set; } = "Deadline";
        public Border? DeadlineBadge { get; set; } = null;
        public TextBlock? DeadlineText { get; set; } = null;
        public Border? BtnNoteDeadline { get; set; } = null;

        // Note Checklist Integration
        public bool IsChecklist { get; set; } = false;
        public List<NoteChecklistItem>? ChecklistItems { get; set; } = new();
        public Grid? NoteBodyContainer { get; set; } = null;
        public ScrollViewer? ChecklistScrollViewer { get; set; } = null;
        public StackPanel? ChecklistPanel { get; set; } = null;
        public Border? BtnNoteChecklist { get; set; } = null;

        // Note Freehand Doodle / Coret Layer
        public bool IsNoteDoodleActive { get; set; } = false;
        public System.Windows.Controls.InkCanvas? NoteDoodleCanvas { get; set; } = null;
        public string NoteDoodleInkBase64 { get; set; } = "";
        public Border? NoteDoodleIndicator { get; set; } = null;
        public Border? BtnNoteDoodle { get; set; } = null;
        public Border? BtnNoteCornerPen { get; set; } = null;
        public System.Windows.Shapes.Path? NoteCornerPenIcon { get; set; } = null;

        // Note Background GIF Integration
        public string? NoteBgGifPath { get; set; } = null;
        public string? NoteBgGifBase64 { get; set; } = null;
        public Image? NoteBgGifImage { get; set; } = null;
        public Border? NoteBgGifOverlay { get; set; } = null;

        // Hand-Drawn Sketch / Draw Card Integration
        public bool IsDrawCard { get; set; } = false;
        public System.Windows.Controls.InkCanvas? DrawCanvas { get; set; } = null;
        public string DrawInkBase64 { get; set; } = "";
        public string DrawPenColor { get; set; } = "#38BDF8";
        public double DrawPenSize { get; set; } = 3.0;
        public bool DrawIsEraser { get; set; } = false;
        public Border? BtnDrawPen { get; set; } = null;
        public Border? BtnDrawEraser { get; set; } = null;

        // HWND lockstep position and size cache
        public int LastPixelX { get; set; } = int.MinValue;
        public int LastPixelY { get; set; } = int.MinValue;
        public int LastPixelW { get; set; } = int.MinValue;
        public int LastPixelH { get; set; } = int.MinValue;

        // Real-time Canvas Color Palette Mode
        public bool IsPaletteMode { get; set; } = false;
        public Canvas? PalettePinsCanvas { get; set; } = null;
        public List<PalettePin> ActivePalettePins { get; set; } = new();
        public int PaletteColorCount { get; set; } = 5;
        public ColorMood PaletteMood { get; set; } = ColorMood.Colorful;
        public Dictionary<ColorMood, List<PalettePin>> PaletteMoodCache { get; set; } = new();

        // Standalone Live Color Palette Card
        public bool IsPaletteCard { get; set; } = false;
        public int PaletteRows { get; set; } = 1;
        public CardItem? LinkedSourceImageCard { get; set; } = null;
        public CardItem? LinkedPaletteCard { get; set; } = null;
        public string? PendingLinkedSourceCardId { get; set; } = null;
        public Grid? PaletteGridContent { get; set; } = null;
        public TextBlock? PaletteTitleText { get; set; } = null;

        // Resize Handles
        public Border HandleTL { get; set; } = null!;
        public Border HandleTR { get; set; } = null!;
        public Border HandleBL { get; set; } = null!;
        public Border HandleBR { get; set; } = null!;
        public Border? HandleR { get; set; } = null;
        public Border? HandleL { get; set; } = null;
        public Border? HandleT { get; set; } = null;
        public Border? HandleB { get; set; } = null;

        private double _fallbackX = 0;
        private double _fallbackY = 0;
        private double _fallbackWidth = 320;
        private double _fallbackHeight = 180;

        public double X
        {
            get
            {
                if (Container == null) return _fallbackX;
                double val = Canvas.GetLeft(Container);
                return double.IsNaN(val) ? _fallbackX : val;
            }
            set
            {
                double safeVal = double.IsNaN(value) ? 0 : value;
                _fallbackX = safeVal;
                if (Container != null)
                {
                    Canvas.SetLeft(Container, safeVal);
                    if (!IsCropped || BaseWidth <= 0) BaseX = safeVal;
                    else BaseX = safeVal - BaseWidth * (CropLeft / 100.0);
                }
            }
        }

        public double Y
        {
            get
            {
                if (Container == null) return _fallbackY;
                double val = Canvas.GetTop(Container);
                return double.IsNaN(val) ? _fallbackY : val;
            }
            set
            {
                double safeVal = double.IsNaN(value) ? 0 : value;
                _fallbackY = safeVal;
                if (Container != null)
                {
                    Canvas.SetTop(Container, safeVal);
                    if (!IsCropped || BaseHeight <= 0) BaseY = safeVal;
                    else BaseY = safeVal - BaseHeight * (CropTop / 100.0);
                }
            }
        }

        public double Width
        {
            get
            {
                if (Container == null) return _fallbackWidth;
                double val = Container.Width;
                if (double.IsNaN(val) || val <= 0) val = Container.ActualWidth;
                return (double.IsNaN(val) || val <= 0) ? _fallbackWidth : val;
            }
            set
            {
                if (double.IsNaN(value) || value <= 0) return;
                _fallbackWidth = value;
                if (Container != null) Container.Width = value;
                if (ContentBorder != null) ContentBorder.Width = value;
                if (ImageControl != null) ImageControl.Width = value;
                if (NativePlayer != null) NativePlayer.Width = value;
            }
        }

        public double Height
        {
            get
            {
                if (Container == null) return _fallbackHeight;
                double val = Container.Height;
                if (double.IsNaN(val) || val <= 0) val = Container.ActualHeight;
                return (double.IsNaN(val) || val <= 0) ? _fallbackHeight : val;
            }
            set
            {
                if (double.IsNaN(value) || value <= 0) return;
                _fallbackHeight = value;
                if (Container != null) Container.Height = value;
                if (ContentBorder != null) ContentBorder.Height = value;
                if (ImageControl != null) ImageControl.Height = value;
                if (NativePlayer != null) NativePlayer.Height = value;
            }
        }
    }

    public partial class MainWindow : Window
    {
        private readonly List<CardItem> _cards = new();
        private readonly HashSet<CardItem> _selectedCards = new();
        private readonly List<GroupItem> _groups = new();
        private readonly HashSet<GroupItem> _selectedGroups = new();

        // Group Dragging & Resizing State
        private bool _isDraggingGroup = false;
        private GroupItem? _draggingGroup = null;
        private Point _groupDragStartMousePoint;
        private Point _groupDragStartPos;
        private readonly Dictionary<CardItem, Point> _groupCardsInitialPositions = new();
        private bool _isResizingGroup = false;
        private GroupItem? _resizingGroup = null;
        private ResizeCorner _activeGroupCorner = ResizeCorner.None;
        private Point _groupResizeStartMousePoint;
        private Rect _groupResizeInitialBounds;

        private class GroupResizeMemberState
        {
            public CardItem Card { get; set; } = null!;
            public double InitWidth { get; set; }
            public double InitHeight { get; set; }
            public double RelX { get; set; }
            public double RelY { get; set; }
        }
        private readonly List<GroupResizeMemberState> _groupResizeMemberCards = new();

        // System Fonts Cache for Notes
        private static readonly List<string> _installedFontNames =
            Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n).ToList();
        private bool _isCardSubMenuOpen = false;

        // Persistent Settings & HTTP Server
        private readonly AppSettings _settings = AppSettings.Load();
        private LocalHttpServer? _httpServer;

        // Undo & Redo History
        private readonly Stack<CanvasSnapshot> _undoStack = new();
        private readonly Stack<CanvasSnapshot> _redoStack = new();
        private bool _isApplyingSnapshot = false;
        private int _highestZ = 10;
        private int _lowestZ = 5;

        // Paths & Session Management
        private static string AppDataDir => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DropBoard");
        private static string SessionFilePath => System.IO.Path.Combine(AppDataDir, "session.dropboard");
        private static string RecentConfigPath => System.IO.Path.Combine(AppDataDir, "recent.json");
        private static string CacheDir => System.IO.Path.Combine(AppDataDir, "Cache");
        public static string PalettesDir => System.IO.Path.Combine(AppDataDir, "Palettes");

        private readonly DispatcherTimer _autoSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(1000) };
        private bool _isRestoringSession = false;
        private bool _isImportingBatch = false;

        // Canvas Pan & Zoom
        private Point _lastPanPoint;
        private Point _panStartMousePoint;
        private bool _isPanning = false;
        private double _panDistanceAccumulator = 0;

        // Marquee Selection Box
        private bool _isMarqueeSelecting = false;
        private Point _marqueeStartWorldPoint;

        // Card Dragging (Single & Multi)
        private bool _isDraggingCards = false;
        private Point _cardDragStartMousePoint;
        private readonly Dictionary<CardItem, Point> _cardsInitialPositions = new();

        // Palette Swatch Click & Copy Tracking
        private (CardItem card, Action copyAction)? _pendingSwatchClick = null;
        private Point _pendingSwatchStartPoint;

        // Video Card Click Tracking for Pause/Play on Tap
        private CardItem? _pendingVideoCardClick = null;
        private Point _pendingVideoCardStartPoint;

        // Card Resizing
        private bool _isResizingCard = false;
        private CardItem? _resizingCard = null;
        private ResizeCorner _activeCorner = ResizeCorner.None;
        private Point _resizeStartMousePoint;
        private Rect _resizeInitialBounds;

        // Crop Mode
        private bool _isCropping = false;
        private CardItem? _activeCroppingCard = null;
        private Grid? _activeCropOverlay = null;

        // Layout Spacing Gap & Project
        private double _currentGap = 24.0;
        private string _currentFilePath = "";
        private string _projectName = "untitled";
        private bool _isDockAutoHide = false;
        private bool _isDockRevealed = true;
        private string _dockPosition = "top";
        private EventHandler? _activeZoomAnimation = null;
        private DispatcherTimer? _dockTrackingTimer = null;
        private DispatcherTimer? _deadlineRealtimeTimer = null;
        private int _gridArrangeCount = 0;
        private int _pipelineArrangeCount = 0;
        private readonly Dictionary<string, TaskCompletionSource<string>> _pendingCleanFrameRequests = new();

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        public static void TrimWorkingSet()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, false, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, false, true);
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);
            }
            catch { }
        }

        private DispatcherTimer? _idleTrimTimer = null;
        private void ScheduleWorkingSetTrim(int delayMs = 3500)
        {
            try
            {
                if (_idleTrimTimer == null)
                {
                    _idleTrimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delayMs) };
                    _idleTrimTimer.Tick += (s, e) =>
                    {
                        _idleTrimTimer?.Stop();
                        Task.Run(() => TrimWorkingSet());
                    };
                }
                else
                {
                    _idleTrimTimer.Stop();
                    _idleTrimTimer.Interval = TimeSpan.FromMilliseconds(delayMs);
                }
                _idleTrimTimer.Start();
            }
            catch { }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOCOPYBITS = 0x0100;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOSENDCHANGING = 0x0400;

        private void SyncActiveHwndPositions(bool updateSize = false)
        {
            // Fast exit if no WebView2 video player is currently active
            bool hasActivePlayer = false;
            for (int i = 0; i < _cards.Count; i++)
            {
                var pc = _cards[i].PlayerControl;
                if (pc != null && pc.IsVisible)
                {
                    hasActivePlayer = true;
                    break;
                }
            }
            if (!hasActivePlayer) return;

            PresentationSource? source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null) return;

            Matrix dpiMatrix = source.CompositionTarget.TransformToDevice;
            Matrix canvasMatrix = CanvasMatrixTransform.Matrix;

            // Direct analytical computation: 0 visual-tree delay, 0-frame lag!
            Point containerOrigin = CanvasContainer.TranslatePoint(new Point(0, 0), this);

            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.PlayerControl != null && card.PlayerControl.IsVisible && card.Container.IsDescendantOf(this))
                {
                    try
                    {
                        IntPtr handle = card.PlayerControl.Handle;
                        if (handle != IntPtr.Zero)
                        {
                            // Transform world card bounds directly through canvas matrix in zero time
                            Point topLeftCanvas = canvasMatrix.Transform(new Point(card.X, card.Y));
                            Point bottomRightCanvas = canvasMatrix.Transform(new Point(card.X + card.Width, card.Y + card.Height));

                            double topLeftDipX = topLeftCanvas.X + containerOrigin.X;
                            double topLeftDipY = topLeftCanvas.Y + containerOrigin.Y;
                            double bottomRightDipX = bottomRightCanvas.X + containerOrigin.X;
                            double bottomRightDipY = bottomRightCanvas.Y + containerOrigin.Y;

                            int pixelX = (int)Math.Round(topLeftDipX * dpiMatrix.M11);
                            int pixelY = (int)Math.Round(topLeftDipY * dpiMatrix.M22);
                            int pixelW = Math.Max(1, (int)Math.Round((bottomRightDipX - topLeftDipX) * dpiMatrix.M11));
                            int pixelH = Math.Max(1, (int)Math.Round((bottomRightDipY - topLeftDipY) * dpiMatrix.M22));

                            bool posChanged = (pixelX != card.LastPixelX || pixelY != card.LastPixelY);
                            bool sizeChanged = updateSize || (pixelW != card.LastPixelW || pixelH != card.LastPixelH);

                            if (!posChanged && !sizeChanged) continue;

                            const uint baseFlags = SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING | SWP_NOCOPYBITS;

                            if (sizeChanged)
                            {
                                SetWindowPos(handle, IntPtr.Zero, pixelX, pixelY, pixelW, pixelH, baseFlags);
                                card.LastPixelW = pixelW;
                                card.LastPixelH = pixelH;
                            }
                            else
                            {
                                SetWindowPos(handle, IntPtr.Zero, pixelX, pixelY, 0, 0, baseFlags | SWP_NOSIZE);
                            }

                            card.LastPixelX = pixelX;
                            card.LastPixelY = pixelY;
                        }
                    }
                    catch { }
                }
            }
        }

        private void SetWebViewHitTesting(bool enable)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.PlayerControl != null)
                {
                    card.PlayerControl.IsHitTestVisible = enable;
                }
            }
        }

        private bool IsTextEditorFocused(KeyEventArgs? e = null)
        {
            if (e != null)
            {
                if (e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase || e.OriginalSource is PasswordBox) return true;
                if (e.OriginalSource is DependencyObject depO && (FindVisualParent<System.Windows.Controls.Primitives.TextBoxBase>(depO) != null || FindVisualParent<PasswordBox>(depO) != null)) return true;
                if (e.Source is System.Windows.Controls.Primitives.TextBoxBase || e.Source is PasswordBox) return true;
                if (e.Source is DependencyObject depS && (FindVisualParent<System.Windows.Controls.Primitives.TextBoxBase>(depS) != null || FindVisualParent<PasswordBox>(depS) != null)) return true;
            }

            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused != null)
            {
                if (focused is System.Windows.Controls.Primitives.TextBoxBase || focused is PasswordBox) return true;
                if (FindVisualParent<System.Windows.Controls.Primitives.TextBoxBase>(focused) != null || FindVisualParent<PasswordBox>(focused) != null) return true;
            }

            var winFocused = FocusManager.GetFocusedElement(this) as DependencyObject;
            if (winFocused != null)
            {
                if (winFocused is System.Windows.Controls.Primitives.TextBoxBase || winFocused is PasswordBox) return true;
                if (FindVisualParent<System.Windows.Controls.Primitives.TextBoxBase>(winFocused) != null || FindVisualParent<PasswordBox>(winFocused) != null) return true;
            }

            return false;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isTyping = IsTextEditorFocused(e);

            if (e.Key == Key.Space && !_isPanning)
            {
                if (!isTyping)
                {
                    SetWebViewHitTesting(false);
                }
            }
            else if ((e.Key == Key.B || e.Key == Key.P) && Keyboard.Modifiers == ModifierKeys.None && !_isPanning && !_isDraggingCards && !_isResizingCard)
            {
                if (!isTyping)
                {
                    ToggleBrushMode();
                    e.Handled = true;
                }
            }
            else if ((e.Key == Key.V || e.Key == Key.Escape) && _isBrushMode)
            {
                if (!isTyping)
                {
                    ToggleBrushMode(false);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.None && !_isPanning && !_isDraggingCards && !_isResizingCard)
            {
                if (!isTyping)
                {
                    BtnAddNote_Click(sender, e);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.None && !_isPanning && !_isDraggingCards && !_isResizingCard)
            {
                if (!isTyping)
                {
                    BtnAddChecklist_Click(sender, e);
                    e.Handled = true;
                }
            }
            else if ((e.Key == Key.Delete || e.Key == Key.Back) && Keyboard.Modifiers == ModifierKeys.None && !_isPanning && !_isDraggingCards && !_isResizingCard && !_isCropping)
            {
                if (!isTyping)
                {
                    if (_selectedCards.Count > 0 || _selectedGroups.Count > 0)
                    {
                        DeleteSelectedCards();
                        e.Handled = true;
                    }
                }
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && !_isPanning)
            {
                SetWebViewHitTesting(true);
            }
        }

        #region Performance Optimization & Caching (300+ Images Support)

        private static readonly DropShadowEffect CardActiveShadow = CreateFrozenCardShadow();

        private static DropShadowEffect CreateFrozenCardShadow()
        {
            var ds = new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 6,
                Direction = 270,
                Opacity = 0.65,
                Color = Colors.Black
            };
            ds.Freeze();
            return ds;
        }

        private static BitmapImage LoadOptimizedBitmap(string filePath, int maxDecodeWidth = 900)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(filePath, UriKind.Absolute);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            if (maxDecodeWidth > 0)
            {
                bi.DecodePixelWidth = maxDecodeWidth;
            }
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private static BitmapImage LoadOptimizedBitmapFromStream(Stream stream, int maxDecodeWidth = 900)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = stream;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            if (maxDecodeWidth > 0)
            {
                bi.DecodePixelWidth = maxDecodeWidth;
            }
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private void UpdateViewportCulling()
        {
            if (_cards.Count <= 10 || CanvasContainer.ActualWidth <= 0 || CanvasContainer.ActualHeight <= 0)
            {
                for (int i = 0; i < _cards.Count; i++)
                {
                    if (_cards[i].Container.Visibility != Visibility.Visible)
                        _cards[i].Container.Visibility = Visibility.Visible;
                }
                return;
            }

            try
            {
                Matrix inv = CanvasMatrixTransform.Matrix;
                if (!inv.HasInverse) return;
                inv.Invert();

                Point p0 = inv.Transform(new Point(0, 0));
                Point p1 = inv.Transform(new Point(CanvasContainer.ActualWidth, CanvasContainer.ActualHeight));

                double minX = Math.Min(p0.X, p1.X);
                double maxX = Math.Max(p0.X, p1.X);
                double minY = Math.Min(p0.Y, p1.Y);
                double maxY = Math.Max(p0.Y, p1.Y);

                // Viewport buffer margin 450px world-space di sekeliling layar
                double buffer = 450.0;
                Rect visibleRect = new Rect(minX - buffer, minY - buffer, (maxX - minX) + (buffer * 2), (maxY - minY) + (buffer * 2));

                for (int i = 0; i < _cards.Count; i++)
                {
                    var card = _cards[i];
                    if (card.IsSelected || card.IsPlayingYouTube)
                    {
                        if (card.Container.Visibility != Visibility.Visible)
                            card.Container.Visibility = Visibility.Visible;
                        continue;
                    }

                    Rect cardRect = new Rect(card.X, card.Y, Math.Max(1, card.Width), Math.Max(1, card.Height));
                    bool isVisible = visibleRect.IntersectsWith(cardRect);
                    Visibility targetVis = isVisible ? Visibility.Visible : Visibility.Collapsed;

                    if (card.Container.Visibility != targetVis)
                    {
                        card.Container.Visibility = targetVis;
                    }
                }
            }
            catch { }
        }

        #endregion

        private static string FormatGapText(double gap) => gap == 0 ? "Gap: No Gap" : $"Gap: {(int)gap}px";

        public MainWindow(string? initialFilePath = null)
        {
            InitializeComponent();

            // Hook CompositionTarget.Rendering so HwndHost child windows stay 100% lockstep with DirectX frames
            CompositionTarget.Rendering += (s, e) =>
            {
                SyncActiveHwndPositions(updateSize: false);
            };

            // Restore persistent window metrics, opacity, and pin state
            ApplyCanvasTransparency(_settings.OpacityPercent);
            CanvasBgSlider.Value = _settings.OpacityPercent;
            _currentGap = _settings.ArrangeGap >= 0 ? _settings.ArrangeGap : 24.0;
            TxtGap.Text = FormatGapText(_currentGap);

            Topmost = _settings.IsPinned;
            UpdateTitlebarPinVisuals();

            _dockPosition = string.IsNullOrEmpty(_settings.DockPosition) ? "top" : _settings.DockPosition;
            _isDockAutoHide = _settings.AutoHideDock;
            ApplyDockLayout();
            UpdateDockAutoHideUI();
            ApplyTransparentTitlebar(_settings.TransparentTitlebar);

            _dockTrackingTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(45)
            };
            _dockTrackingTimer.Tick += DockTrackingTimer_Tick;
            _dockTrackingTimer.Start();

            _deadlineRealtimeTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _deadlineRealtimeTimer.Tick += (s, e) => UpdateAllDeadlineCards();
            _deadlineRealtimeTimer.Start();

            if (_settings.WindowWidth >= 400 && _settings.WindowHeight >= 300)
            {
                Width = _settings.WindowWidth;
                Height = _settings.WindowHeight;
            }

            if (_settings.WindowLeft >= 0 && _settings.WindowTop >= 0 &&
                _settings.WindowLeft < SystemParameters.VirtualScreenWidth &&
                _settings.WindowTop < SystemParameters.VirtualScreenHeight)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = _settings.WindowLeft;
                Top = _settings.WindowTop;
            }

            if (_settings.WindowState == "Maximized")
            {
                WindowState = WindowState.Maximized;
            }

            InitAutoSave();

            // Start Local HTTP server on port 28888 for Chrome/Edge extension bridge
            _httpServer = new LocalHttpServer(28888, (url, title) =>
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    await AddImageFromUrlAsync(url, title);
                });
            });
            _httpServer.Start();

            Loaded += async (s, e) =>
            {
                await InitializeSessionAsync(initialFilePath);
                UpdateResponsiveLayout(ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height);
                UpdateStorageStats();
                SetupBrushModeSwatches();
                ScheduleWorkingSetTrim(3000);
            };

            StateChanged += (s, e) =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    Task.Run(() => TrimWorkingSet());
                }
            };
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _autoSaveTimer.Stop();
            if (_settings.AutoSaveEnabled)
            {
                PerformAutoSave(isClosing: true);
            }

            // Save persistent app settings
            if (WindowState == WindowState.Normal)
            {
                _settings.WindowLeft = Left;
                _settings.WindowTop = Top;
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
            }
            _settings.WindowState = WindowState.ToString();
            _settings.OpacityPercent = (int)CanvasBgSlider.Value;
            _settings.IsPinned = Topmost;
            _settings.ArrangeGap = _currentGap;
            _settings.Save();

            _httpServer?.Stop();

            base.OnClosing(e);
        }

        private void InitAutoSave()
        {
            _autoSaveTimer.Tick += (s, e) =>
            {
                _autoSaveTimer.Stop();
                PerformAutoSave(isClosing: false);
            };
        }

        private void ScheduleAutoSave()
        {
            if (_isRestoringSession || _isImportingBatch) return;
            if (!_settings.AutoSaveEnabled) return;
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();
        }

        #region Canvas Background Transparency

        private void CanvasBgSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CanvasBgText == null) return;
            int percent = (int)e.NewValue;
            ApplyCanvasTransparency(percent);
            _settings.OpacityPercent = percent;
            _settings.Save();
        }

        private void ApplyCanvasTransparency(int percent)
        {
            byte alpha = (byte)(percent * 255 / 100);
            RootGrid.Background = new SolidColorBrush(Color.FromArgb(alpha, 13, 15, 20));

            CanvasBgText.Text = $"{percent}%";
        }

        private void BtnOpacityToggle_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            CanvasBgSlider.Value = CanvasBgSlider.Value > 0 ? 0 : 100;
        }

        private void CanvasBgSlider_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            CanvasBgSlider.Value = CanvasBgSlider.Value > 0 ? 0 : 100;
        }

        private void MenuOpacityPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string tagStr && double.TryParse(tagStr, out double val))
            {
                CanvasBgSlider.Value = val;
            }
        }

        #endregion

        #region Infinite Canvas Navigation (Pan & Zoom)

        private void CanvasContainer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Middle-click or Space+Left-click: ALWAYS triggers Pan, even over text notes or other controls!
            if (e.ChangedButton == MouseButton.Middle || 
               (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space)))
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(CanvasContainer);
                SetWebViewHitTesting(false);
                CanvasContainer.CaptureMouse();
                Cursor = Cursors.Hand;
                e.Handled = true;
                return;
            }
        }

        private void CanvasContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Right-click or Middle-click or Space+Left-click to pan
            if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Middle || 
               (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space)))
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(CanvasContainer);
                _panStartMousePoint = _lastPanPoint;
                SetWebViewHitTesting(false);
                CanvasContainer.CaptureMouse();
                Cursor = Cursors.Hand;
                e.Handled = true;
                return;
            }

            // Left-click on empty canvas: Start Marquee Selection
            if (e.ChangedButton == MouseButton.Left)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                {
                    DeselectAllCards();
                }

                _isMarqueeSelecting = true;
                Point mouseScreen = e.GetPosition(CanvasContainer);
                _marqueeStartWorldPoint = ScreenToWorld(mouseScreen);

                Canvas.SetLeft(MarqueeSelectionBox, _marqueeStartWorldPoint.X);
                Canvas.SetTop(MarqueeSelectionBox, _marqueeStartWorldPoint.Y);
                MarqueeSelectionBox.Width = 0;
                MarqueeSelectionBox.Height = 0;
                MarqueeSelectionBox.Visibility = Visibility.Visible;

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            }
        }

        private void CanvasContainer_MouseMove(object sender, MouseEventArgs e)
        {
            // 1. Panning Canvas
            if (_isPanning)
            {
                Point current = e.GetPosition(CanvasContainer);
                Vector delta = current - _lastPanPoint;
                _lastPanPoint = current;

                double panSens = Math.Clamp(_settings.PanSensitivity > 0 ? _settings.PanSensitivity : 1.0, 0.2, 3.0);
                Matrix matrix = CanvasMatrixTransform.Matrix;
                matrix.Translate(delta.X * panSens, delta.Y * panSens);
                CanvasMatrixTransform.Matrix = matrix;
                SyncActiveHwndPositions(updateSize: false);

                _panDistanceAccumulator += delta.Length;
                if (_cards.Count > 30 && _panDistanceAccumulator > 140)
                {
                    UpdateViewportCulling();
                    _panDistanceAccumulator = 0;
                }

                e.Handled = true;
                return;
            }

            // 2. Marquee Box Selection
            if (_isMarqueeSelecting)
            {
                Point mouseScreen = e.GetPosition(CanvasContainer);
                Point currentWorld = ScreenToWorld(mouseScreen);

                double x = Math.Min(_marqueeStartWorldPoint.X, currentWorld.X);
                double y = Math.Min(_marqueeStartWorldPoint.Y, currentWorld.Y);
                double w = Math.Abs(_marqueeStartWorldPoint.X - currentWorld.X);
                double h = Math.Abs(_marqueeStartWorldPoint.Y - currentWorld.Y);

                Canvas.SetLeft(MarqueeSelectionBox, x);
                Canvas.SetTop(MarqueeSelectionBox, y);
                MarqueeSelectionBox.Width = w;
                MarqueeSelectionBox.Height = h;

                // Hit test cards in marquee box with zero-lag batching
                Rect marqueeRect = new Rect(x, y, w, h);
                bool selectionChanged = false;
                foreach (CardItem card in _cards)
                {
                    Rect cardRect = new Rect(card.X, card.Y, card.Width, card.Height);
                    bool inside = marqueeRect.IntersectsWith(cardRect);
                    if (inside && !card.IsSelected)
                    {
                        SelectCard(card, addToSelection: true, updateCounts: false);
                        selectionChanged = true;
                    }
                    else if (!inside && card.IsSelected && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                    {
                        DeselectCard(card);
                        selectionChanged = true;
                    }
                }
                if (selectionChanged)
                {
                    UpdateStatusCounts();
                }
                e.Handled = true;
                return;
            }

            // 3. Resizing Card via Corner Handle
            if (_isResizingCard && _resizingCard != null)
            {
                Point currentMouse = e.GetPosition(CanvasContainer);
                Matrix matrix = CanvasMatrixTransform.Matrix;
                double zoom = matrix.M11;

                double deltaX = (currentMouse.X - _resizeStartMousePoint.X) / zoom;
                double deltaY = (currentMouse.Y - _resizeStartMousePoint.Y) / zoom;

                ApplyCardResize(_resizingCard, _activeCorner, _resizeInitialBounds, deltaX, deltaY);
                SyncActiveHwndPositions(updateSize: true);
                e.Handled = true;
                return;
            }

            // 4. Dragging Selected Cards (Single or Multi)
            if (_isDraggingCards)
            {
                Point currentMouse = e.GetPosition(CanvasContainer);
                Matrix matrix = CanvasMatrixTransform.Matrix;
                double zoom = matrix.M11;

                double deltaX = (currentMouse.X - _cardDragStartMousePoint.X) / zoom;
                double deltaY = (currentMouse.Y - _cardDragStartMousePoint.Y) / zoom;

                foreach (var kvp in _cardsInitialPositions)
                {
                    kvp.Key.X = kvp.Value.X + deltaX;
                    kvp.Key.Y = kvp.Value.Y + deltaY;
                }
                SyncActiveHwndPositions(updateSize: false);
                e.Handled = true;
                return;
            }

            // 5. Dragging Group by Header
            if (_isDraggingGroup && _draggingGroup != null)
            {
                Point currentMouse = e.GetPosition(CanvasContainer);
                Matrix matrix = CanvasMatrixTransform.Matrix;
                double zoom = matrix.M11;

                double deltaX = (currentMouse.X - _groupDragStartMousePoint.X) / zoom;
                double deltaY = (currentMouse.Y - _groupDragStartMousePoint.Y) / zoom;

                _draggingGroup.X = _groupDragStartPos.X + deltaX;
                _draggingGroup.Y = _groupDragStartPos.Y + deltaY;
                Canvas.SetLeft(_draggingGroup.Container, _draggingGroup.X);
                Canvas.SetTop(_draggingGroup.Container, _draggingGroup.Y);

                foreach (var kvp in _groupCardsInitialPositions)
                {
                    kvp.Key.X = kvp.Value.X + deltaX;
                    kvp.Key.Y = kvp.Value.Y + deltaY;
                }
                SyncActiveHwndPositions(updateSize: false);
                e.Handled = true;
                return;
            }

            // 6. Resizing Group from any of the 4 corner handles (like note cards!)
            if (_isResizingGroup && _resizingGroup != null)
            {
                Point currentMouse = e.GetPosition(CanvasContainer);
                Matrix matrix = CanvasMatrixTransform.Matrix;
                double zoom = matrix.M11;

                double deltaX = (currentMouse.X - _groupResizeStartMousePoint.X) / zoom;
                double deltaY = (currentMouse.Y - _groupResizeStartMousePoint.Y) / zoom;

                ApplyGroupResize(_resizingGroup, _activeGroupCorner, _groupResizeInitialBounds, deltaX, deltaY);

                e.Handled = true;
                return;
            }
        }

        private void CanvasContainer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bool stateChanged = false;

            if (_isPanning)
            {
                _isPanning = false;
                _panDistanceAccumulator = 0;
                SetWebViewHitTesting(true);
                CanvasContainer.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
                SyncActiveHwndPositions(updateSize: false);
                UpdateViewportCulling();
                e.Handled = true;
                stateChanged = true;

                if (e.ChangedButton == MouseButton.Right)
                {
                    Point curPos = e.GetPosition(CanvasContainer);
                    Vector diff = curPos - _panStartMousePoint;
                    if (diff.Length < 6.0)
                    {
                        Point worldPos = ScreenToWorld(curPos);
                        ShowCanvasContextMenu(curPos, worldPos);
                    }
                }
            }

            if (_isMarqueeSelecting)
            {
                _isMarqueeSelecting = false;
                MarqueeSelectionBox.Visibility = Visibility.Collapsed;
                CanvasContainer.ReleaseMouseCapture();
                e.Handled = true;
            }

            if (_isResizingCard)
            {
                _isResizingCard = false;
                SetWebViewHitTesting(true);
                if (_resizingCard != null)
                {
                    _resizingCard.Container.UpdateLayout();
                }
                _resizingCard = null;
                CanvasContainer.ReleaseMouseCapture();
                SyncActiveHwndPositions(updateSize: true);
                e.Handled = true;
                stateChanged = true;
            }

            if (_pendingSwatchClick.HasValue)
            {
                Point upPt = e.GetPosition(CanvasContainer);
                double moveDist = (upPt - _pendingSwatchStartPoint).Length;
                var pending = _pendingSwatchClick.Value;
                _pendingSwatchClick = null;

                if (moveDist < 5.0)
                {
                    pending.copyAction();
                }
            }

            if (_pendingVideoCardClick != null)
            {
                Point upPt = e.GetPosition(CanvasContainer);
                double moveDist = (upPt - _pendingVideoCardStartPoint).Length;
                var vidCard = _pendingVideoCardClick;
                _pendingVideoCardClick = null;

                if (moveDist < 8.0)
                {
                    // Restore original positions in case of micro-jitter during click
                    foreach (var kvp in _cardsInitialPositions)
                    {
                        kvp.Key.X = kvp.Value.X;
                        kvp.Key.Y = kvp.Value.Y;
                    }
                    ToggleLocalVideoPlayback(vidCard);
                }
            }

            if (_isDraggingCards)
            {
                _isDraggingCards = false;
                SetWebViewHitTesting(true);
                foreach (var card in _cardsInitialPositions.Keys)
                {
                    CheckCardGroupAffiliation(card);
                }
                _cardsInitialPositions.Clear();
                CanvasContainer.ReleaseMouseCapture();
                SyncActiveHwndPositions(updateSize: false);
                UpdateViewportCulling();
                e.Handled = true;
                stateChanged = true;
            }

            if (_isDraggingGroup)
            {
                _isDraggingGroup = false;
                _draggingGroup = null;
                _groupCardsInitialPositions.Clear();
                CanvasContainer.ReleaseMouseCapture();
                e.Handled = true;
                stateChanged = true;
            }

            if (_isResizingGroup)
            {
                _isResizingGroup = false;
                if (_resizingGroup != null)
                {
                    ReflowGroupCards(_resizingGroup, _resizingGroup.Width, _resizingGroup.Height, _resizingGroup.X, _resizingGroup.Y, animated: false);
                }
                _resizingGroup = null;
                _groupResizeMemberCards.Clear();
                CanvasContainer.ReleaseMouseCapture();
                e.Handled = true;
                stateChanged = true;
            }

            if (stateChanged)
            {
                ScheduleAutoSave();
            }
        }

        private void CanvasContainer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            string mode = string.IsNullOrEmpty(_settings.NavMode) ? "macos" : _settings.NavMode.ToLowerInvariant();
            bool isCtrlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            // In classic "mouse" mode (PureRef style): wheel ALWAYS zooms canvas
            // In "macos" mode: Ctrl+Wheel zooms canvas
            if (mode == "mouse" || isCtrlDown)
            {
                Point mousePos = e.GetPosition(CanvasContainer);
                PerformCanvasZoom(e.Delta, mousePos);
                e.Handled = true;
            }
        }

        private void CanvasContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            string mode = string.IsNullOrEmpty(_settings.NavMode) ? "macos" : _settings.NavMode.ToLowerInvariant();
            bool isCtrlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool isShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            if (mode == "mouse")
            {
                // Classic Mouse (PureRef style): plain wheel zooms canvas
                Point mousePos = e.GetPosition(CanvasContainer);
                PerformCanvasZoom(e.Delta, mousePos);
                e.Handled = true;
                return;
            }

            // For macOS Trackpad and Windows Precision:
            // Ctrl+Wheel or Pinch zooms canvas
            if (isCtrlDown)
            {
                Point mousePos = e.GetPosition(CanvasContainer);
                PerformCanvasZoom(e.Delta, mousePos);
                e.Handled = true;
                return;
            }

            // Normal 2-finger swipe / wheel pans canvas
            double panSens = Math.Clamp(_settings.PanSensitivity > 0 ? _settings.PanSensitivity : 1.0, 0.2, 3.0);
            double panDelta = (e.Delta / 3.0) * panSens;
            if (_settings.InvertPan)
            {
                panDelta = -panDelta;
            }

            Matrix matrix = CanvasMatrixTransform.Matrix;
            if (isShiftDown)
            {
                matrix.Translate(panDelta, 0);
            }
            else
            {
                matrix.Translate(0, panDelta);
            }
            CanvasMatrixTransform.Matrix = matrix;
            SyncActiveHwndPositions(updateSize: false);
            UpdateViewportCulling();
            ScheduleAutoSave();
            e.Handled = true;
        }

        private void PerformCanvasZoom(int delta, Point? centerPos = null)
        {
            Point mousePos = centerPos ?? new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
            double sens = Math.Clamp(_settings.ZoomSensitivity > 0 ? _settings.ZoomSensitivity : 1.0, 0.2, 3.0);
            double step = 0.15 * sens;
            double zoomFactor = delta > 0 ? (1.0 + step) : (1.0 / (1.0 + step));

            Matrix matrix = CanvasMatrixTransform.Matrix;

            // Clamp zoom level between 5% and 2500%
            if ((matrix.M11 * zoomFactor < 0.05 && delta < 0) || (matrix.M11 * zoomFactor > 25.0 && delta > 0))
                return;

            matrix.ScaleAt(zoomFactor, zoomFactor, mousePos.X, mousePos.Y);
            CanvasMatrixTransform.Matrix = matrix;

            int zoomPercent = (int)Math.Round(matrix.M11 * 100);
            TxtZoom.Text = $"Zoom: {zoomPercent}%";

            SyncActiveHwndPositions(updateSize: true);
            UpdateViewportCulling();

            ScheduleAutoSave();
        }

        private Point ScreenToWorld(Point screenPoint)
        {
            Matrix inv = CanvasMatrixTransform.Matrix;
            inv.Invert();
            return inv.Transform(screenPoint);
        }

        #endregion

        #region Card Creation, Selection & Resizing

        private CardItem AddImageCard(
            BitmapSource bitmap,
            Point? worldPosition = null,
            double? customWidth = null,
            double? customHeight = null,
            string localPath = "",
            string base64Data = "",
            bool autoSelect = true,
            BitmapSource? originalBitmap = null,
            double? baseWidth = null,
            double? baseHeight = null,
            double? cropLeft = null,
            double? cropTop = null,
            double? cropRight = null,
            double? cropBottom = null,
            bool isYouTube = false,
            string youTubeId = "",
            string youTubeUrl = "",
            bool isLocalVideo = false,
            string videoFilePath = "",
            bool isVideoLooping = true,
            bool isVideoMuted = true,
            bool recordUndo = true)
        {
            EmptyStateOverlay.Visibility = Visibility.Collapsed;

            BitmapSource origSource = originalBitmap ?? bitmap;
            double cL = cropLeft ?? 0;
            double cT = cropTop ?? 0;
            double cR = cropRight ?? 0;
            double cB = cropBottom ?? 0;
            bool isCropped = (cL > 0 || cT > 0 || cR > 0 || cB > 0);

            // If cropped, compute CroppedBitmap from original
            BitmapSource displayBitmap = bitmap;
            if (isCropped && origSource != null)
            {
                int imgW = origSource.PixelWidth;
                int imgH = origSource.PixelHeight;
                int pxX = (int)Math.Round((cL / 100.0) * imgW);
                int pxY = (int)Math.Round((cT / 100.0) * imgH);
                int pxW = (int)Math.Round(((100.0 - cL - cR) / 100.0) * imgW);
                int pxH = (int)Math.Round(((100.0 - cT - cB) / 100.0) * imgH);
                pxX = Math.Clamp(pxX, 0, Math.Max(0, imgW - 1));
                pxY = Math.Clamp(pxY, 0, Math.Max(0, imgH - 1));
                pxW = Math.Clamp(pxW, 1, imgW - pxX);
                pxH = Math.Clamp(pxH, 1, imgH - pxY);

                try
                {
                    displayBitmap = new CroppedBitmap(origSource, new Int32Rect(pxX, pxY, pxW, pxH));
                }
                catch
                {
                    displayBitmap = origSource;
                }
            }

            double origW = displayBitmap.PixelWidth;
            double origH = displayBitmap.PixelHeight;
            double aspect = origW / Math.Max(1.0, origH);

            if (isYouTube)
            {
                aspect = 16.0 / 9.0;
            }

            double w = customWidth ?? origW;
            double h = customHeight ?? origH;

            if (isYouTube && !customHeight.HasValue)
            {
                h = Math.Round(w / aspect);
            }

            if (!customWidth.HasValue && !customHeight.HasValue)
            {
                // Initial dimension clamped to max 380px
                double maxDim = 380.0;
                if (w > maxDim || h > maxDim)
                {
                    double scale = Math.Min(maxDim / w, maxDim / h);
                    w = Math.Round(w * scale);
                    h = Math.Round(h * scale);
                }
            }

            // Image display
            Image image = new Image
            {
                Source = displayBitmap,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = (isYouTube || isLocalVideo) ? Stretch.UniformToFill : Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Linear);

            FrameworkElement cardContentElement = image;
            MediaElement? nativePlayer = null;

            if (isLocalVideo)
            {
                Grid mediaGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                mediaGrid.Children.Add(image);

                Uri? videoUri = (!string.IsNullOrEmpty(videoFilePath) && File.Exists(videoFilePath)) ? new Uri(videoFilePath, UriKind.Absolute) : null;
                nativePlayer = new MediaElement
                {
                    Source = videoUri,
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Close,
                    Stretch = Stretch.Uniform,
                    ScrubbingEnabled = true,
                    IsMuted = isVideoMuted,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Visibility = Visibility.Visible
                };
                mediaGrid.Children.Add(nativePlayer);
                cardContentElement = mediaGrid;
            }

            // Card solid background border with shadow (sharp rectangular)
            Border contentBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 27, 36)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Effect = _cards.Count > 30 ? null : CardActiveShadow,
                Child = cardContentElement
            };

            // Container Grid hosting content + 4 resize handles
            Grid container = new Grid
            {
                Width = w,
                Height = h,
                Cursor = Cursors.SizeAll
            };
            Panel.SetZIndex(container, 10);
            container.Children.Add(contentBorder);

            // 4 Corner Handles
            Border handleTL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE);
            Border handleTR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW);
            Border handleBL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW);
            Border handleBR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE);

            container.Children.Add(handleTL);
            container.Children.Add(handleTR);
            container.Children.Add(handleBL);
            container.Children.Add(handleBR);

            CardItem item = new CardItem
            {
                Container = container,
                ContentBorder = contentBorder,
                ImageControl = image,
                NativePlayer = nativePlayer,
                Bitmap = displayBitmap,
                OriginalBitmap = origSource!,
                BaseWidth = baseWidth ?? (isCropped ? Math.Round(w / Math.Max(0.05, (100.0 - cL - cR) / 100.0)) : w),
                BaseHeight = baseHeight ?? (isCropped ? Math.Round(h / Math.Max(0.05, (100.0 - cT - cB) / 100.0)) : h),
                CropLeft = cL,
                CropTop = cT,
                CropRight = cR,
                CropBottom = cB,
                AspectRatio = aspect,
                LocalPath = localPath,
                Base64Data = base64Data,
                HandleTL = handleTL,
                HandleTR = handleTR,
                HandleBL = handleBL,
                HandleBR = handleBR,
                IsYouTube = isYouTube,
                YouTubeId = youTubeId,
                YouTubeUrl = youTubeUrl,
                IsLocalVideo = isLocalVideo,
                VideoFilePath = videoFilePath,
                IsVideoLooping = isVideoLooping,
                IsVideoMuted = isVideoMuted
            };

            // Attach Hover Quick-Action Toolbar (:: Move | [YT/Video buttons] | Ae Import | ✂ Crop | Copy | ✕)
            Border hoverToolbar = CreateCardHoverToolbar(item);
            item.HoverToolbar = hoverToolbar;
            Canvas toolbarHost = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 0,
                Height = 0,
                ClipToBounds = false
            };
            Panel.SetZIndex(toolbarHost, 9999);
            Canvas.SetTop(hoverToolbar, -38.0);
            toolbarHost.Children.Add(hoverToolbar);
            container.Children.Add(toolbarHost);

            hoverToolbar.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0)
                {
                    Canvas.SetLeft(hoverToolbar, -e.NewSize.Width / 2.0);
                    Canvas.SetTop(hoverToolbar, -38.0);
                }
            };

            container.SizeChanged += (s, e) =>
            {
                if (item.IsPaletteMode)
                {
                    UpdateCanvasPaletteLayout(item);
                }
            };

            // Red YouTube Tag Badge & Direct Play Button if this card is a YouTube reference
            if (item.IsYouTube)
            {
                Border ytBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(220, 220, 38, 38)), // Red accent
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8, 0, 0, 8),
                    Cursor = Cursors.Hand,
                    ToolTip = "Click to Play Video",
                    Child = new TextBlock
                    {
                        Text = "▶ YouTube",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    }
                };
                ytBadge.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ToggleYouTubePlayback(item);
                };
                item.YouTubeTagBadge = ytBadge;
                container.Children.Add(ytBadge);

                // Prominent YouTube Center Play Button overlay
                Border centerPlayBtn = new Border
                {
                    Width = 58,
                    Height = 40,
                    Background = new SolidColorBrush(Color.FromArgb(235, 220, 38, 38)),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    ToolTip = "Play Video",
                    Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 3, Opacity = 0.65, Color = Colors.Black },
                    Child = new TextBlock
                    {
                        Text = "▶",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(3, 0, 0, 0)
                    }
                };
                centerPlayBtn.MouseEnter += (s, e) => centerPlayBtn.Background = new SolidColorBrush(Color.FromRgb(255, 30, 30));
                centerPlayBtn.MouseLeave += (s, e) => centerPlayBtn.Background = new SolidColorBrush(Color.FromArgb(235, 220, 38, 38));
                centerPlayBtn.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ToggleYouTubePlayback(item);
                };
                item.CenterPlayBtn = centerPlayBtn;
                container.Children.Add(centerPlayBtn);
            }

            // Local Video Badges, Center Play Button, Scrubber Bar, and Events
            if (item.IsLocalVideo && item.NativePlayer != null)
            {
                MediaElement player = item.NativePlayer;

                // Wire up media lifecycle events
                player.MediaOpened += (s, e) =>
                {
                    if (player.NaturalDuration.HasTimeSpan)
                    {
                        item.VideoDurationSeconds = player.NaturalDuration.TimeSpan.TotalSeconds;
                        if (item.VideoTimelineSlider != null)
                        {
                            item.VideoTimelineSlider.Maximum = item.VideoDurationSeconds;
                        }
                        if (item.VideoTimeText != null)
                        {
                            item.VideoTimeText.Text = FormatVideoDuration(item.VideoCurrentSeconds, item.VideoDurationSeconds);
                        }
                    }

                    if (player.NaturalVideoWidth > 0 && player.NaturalVideoHeight > 0)
                    {
                        double natW = player.NaturalVideoWidth;
                        double natH = player.NaturalVideoHeight;
                        double realAspect = natW / natH;
                        item.AspectRatio = realAspect;

                        double curAspect = item.Width / Math.Max(1.0, item.Height);
                        if (Math.Abs(curAspect - realAspect) > 0.05)
                        {
                            FitVideoCardToResolution(item, notify: false);
                        }
                    }

                    // Render first frame as video thumbnail immediately upon import
                    if (!item.IsVideoPlaying)
                    {
                        try
                        {
                            player.Play();
                            player.Pause();
                            player.Position = TimeSpan.FromMilliseconds(150);
                            item.ImageControl.Visibility = Visibility.Collapsed;
                        }
                        catch { }
                    }
                };

                player.MediaEnded += (s, e) =>
                {
                    if (item.IsVideoLooping)
                    {
                        player.Position = TimeSpan.Zero;
                        player.Play();
                    }
                    else
                    {
                        StopLocalVideo(item);
                    }
                };

                player.MediaFailed += (s, e) =>
                {
                    player.Visibility = Visibility.Collapsed;
                    item.ImageControl.Visibility = Visibility.Visible;
                    ShowToast($"Video playback error: {e.ErrorException?.Message ?? "Codec not supported"}", ToastType.Error);
                    StopLocalVideo(item);
                };

                bool isUpdatingSlider = false;

                // Playback progression timer (100ms interval)
                DispatcherTimer vidTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                vidTimer.Tick += (s, e) =>
                {
                    if (item.IsVideoPlaying && item.NativePlayer != null)
                    {
                        // Skip timer update if user is actively dragging slider or recently clicked/sought within 800ms
                        if (item.IsUserScrubbingTimeline || (DateTime.UtcNow - item.LastUserSeekTime).TotalMilliseconds < 800)
                        {
                            return;
                        }

                        double pos = item.NativePlayer.Position.TotalSeconds;
                        item.VideoCurrentSeconds = pos;
                        if (item.VideoTimelineSlider != null)
                        {
                            isUpdatingSlider = true;
                            try { item.VideoTimelineSlider.Value = pos; }
                            finally { isUpdatingSlider = false; }
                        }
                        if (item.VideoTimeText != null)
                        {
                            item.VideoTimeText.Text = FormatVideoDuration(pos, item.VideoDurationSeconds);
                        }
                    }
                };
                item.VideoPlaybackTimer = vidTimer;

                // Extension format badge (Cyan / Sky Blue accent)
                string extName = System.IO.Path.GetExtension(videoFilePath).TrimStart('.').ToUpperInvariant();
                if (string.IsNullOrEmpty(extName)) extName = "VIDEO";
                Border vidBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(220, 2, 132, 199)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8, 0, 0, 8),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Click to Play/Pause {extName} Video",
                    Child = new TextBlock
                    {
                        Text = $"▶ {extName}",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    }
                };
                vidBadge.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ToggleLocalVideoPlayback(item);
                };
                item.VideoTagBadge = vidBadge;
                container.Children.Add(vidBadge);

                // Center Play Button (Sky Blue accent)
                Border centerPlayBtn = new Border
                {
                    Width = 56,
                    Height = 42,
                    Background = new SolidColorBrush(Color.FromArgb(220, 14, 165, 233)),
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    ToolTip = "Play Video",
                    Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 3, Opacity = 0.65, Color = Colors.Black },
                    Child = new TextBlock
                    {
                        Text = "▶",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(3, 0, 0, 0)
                    }
                };
                centerPlayBtn.MouseEnter += (s, e) => centerPlayBtn.Background = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                centerPlayBtn.MouseLeave += (s, e) => centerPlayBtn.Background = new SolidColorBrush(Color.FromArgb(220, 14, 165, 233));
                centerPlayBtn.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ToggleLocalVideoPlayback(item);
                };
                item.CenterVideoPlayBtn = centerPlayBtn;
                container.Children.Add(centerPlayBtn);

                // Glassmorphism Timeline Scrubber Bar
                Grid scrubberGrid = new Grid();
                scrubberGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                scrubberGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Slider slider = new Slider
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    Margin = new Thickness(8, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    IsMoveToPointEnabled = true
                };
                if (TryFindResource("VideoTimelineSliderStyle") is Style vStyle)
                {
                    slider.Style = vStyle;
                }

                Action<double> applySeek = (targetSec) =>
                {
                    if (isUpdatingSlider) return;
                    isUpdatingSlider = true;
                    try
                    {
                        double maxDuration = item.VideoDurationSeconds > 0 ? item.VideoDurationSeconds : slider.Maximum;
                        if (maxDuration > 0)
                        {
                            targetSec = Math.Clamp(targetSec, 0, maxDuration);
                        }
                        else
                        {
                            targetSec = Math.Max(0, targetSec);
                        }

                        slider.Value = targetSec;
                        item.VideoCurrentSeconds = targetSec;
                        item.LastUserSeekTime = DateTime.UtcNow;

                        if (item.NativePlayer != null)
                        {
                            try
                            {
                                item.NativePlayer.Position = TimeSpan.FromSeconds(targetSec);
                            }
                            catch { }
                        }

                        if (item.VideoTimeText != null)
                        {
                            item.VideoTimeText.Text = FormatVideoDuration(targetSec, item.VideoDurationSeconds);
                        }
                    }
                    finally
                    {
                        isUpdatingSlider = false;
                    }
                };

                Action<Point> seekSliderToMouse = (pt) =>
                {
                    double maxDuration = item.VideoDurationSeconds > 0 ? item.VideoDurationSeconds : slider.Maximum;
                    if (slider.ActualWidth > 0 && maxDuration > 0)
                    {
                        double ratio = Math.Clamp(pt.X / slider.ActualWidth, 0.0, 1.0);
                        double targetVal = ratio * maxDuration;
                        applySeek(targetVal);
                    }
                };

                slider.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    item.IsUserScrubbingTimeline = true;
                    item.LastUserSeekTime = DateTime.UtcNow;
                    slider.CaptureMouse();
                    seekSliderToMouse(e.GetPosition(slider));
                    e.Handled = true;
                };

                slider.PreviewMouseMove += (s, e) =>
                {
                    if (item.IsUserScrubbingTimeline && e.LeftButton == MouseButtonState.Pressed)
                    {
                        seekSliderToMouse(e.GetPosition(slider));
                        e.Handled = true;
                    }
                };

                slider.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    if (item.IsUserScrubbingTimeline)
                    {
                        item.IsUserScrubbingTimeline = false;
                        item.LastUserSeekTime = DateTime.UtcNow;
                        if (slider.IsMouseCaptured)
                        {
                            slider.ReleaseMouseCapture();
                        }
                        seekSliderToMouse(e.GetPosition(slider));
                        e.Handled = true;
                    }
                };

                slider.ValueChanged += (s, e) =>
                {
                    if (!isUpdatingSlider)
                    {
                        applySeek(e.NewValue);
                    }
                };

                item.VideoTimelineSlider = slider;
                Grid.SetColumn(slider, 0);
                scrubberGrid.Children.Add(slider);

                TextBlock timeText = new TextBlock
                {
                    Text = "0:00 / 0:00",
                    FontSize = 9.0,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                item.VideoTimeText = timeText;
                Grid.SetColumn(timeText, 1);
                scrubberGrid.Children.Add(timeText);

                Border scrubberContainer = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(215, 15, 23, 42)),
                    Height = 22,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 0),
                    Visibility = Visibility.Collapsed,
                    Child = scrubberGrid
                };
                // Use bubbling MouseLeftButtonDown (not Preview) so it doesn't block child slider events while preventing card drag
                scrubberContainer.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                };
                item.VideoScrubberContainer = scrubberContainer;
                container.Children.Add(scrubberContainer);
            }

            container.MouseEnter += (s, e) =>
            {
                if (_isCropping) return;
                if (Panel.GetZIndex(container) < 500 && !item.IsSelected)
                {
                    Panel.SetZIndex(container, 500);
                }
                if (!item.IsSelected && contentBorder.Effect == null)
                {
                    contentBorder.Effect = CardActiveShadow;
                }
                if (item.IsLocalVideo && item.VideoScrubberContainer != null)
                {
                    item.VideoScrubberContainer.Visibility = Visibility.Visible;
                }
                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                hoverToolbar.Opacity = 1.0;
                hoverToolbar.IsHitTestVisible = true;
            };
            container.MouseLeave += (s, e) =>
            {
                if (!item.IsSelected && !item.IsPlayingYouTube && !item.IsPaletteMode && !item.IsVideoPlaying)
                {
                    if (Panel.GetZIndex(container) == 500)
                    {
                        Panel.SetZIndex(container, 10);
                    }
                    if (_cards.Count > 30)
                    {
                        contentBorder.Effect = null;
                    }
                }
                if (item.IsLocalVideo && item.VideoScrubberContainer != null && !item.IsVideoPlaying && !item.IsSelected)
                {
                    item.VideoScrubberContainer.Visibility = Visibility.Collapsed;
                }
                if (item.IsPlayingYouTube || item.IsVideoPlaying || item.IsSelected || _isCardSubMenuOpen || item.IsPaletteMode) return;
                DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                anim.Completed += (s2, e2) =>
                {
                    if (!container.IsMouseOver && !hoverToolbar.IsMouseOver && !item.IsPlayingYouTube && !item.IsVideoPlaying && !item.IsSelected && !_isCardSubMenuOpen && !item.IsPaletteMode)
                    {
                        hoverToolbar.IsHitTestVisible = false;
                    }
                };
                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
            };

            // Attach Right-Click Context Menu
            container.ContextMenu = CreateCardContextMenu(item);
            container.MouseRightButtonDown += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    SelectCard(item, addToSelection: false);
                }
                // Prevent bubbling to CanvasContainer which would trigger canvas panning and capture mouse
                e.Handled = true;
            };
            container.MouseRightButtonUp += (s, e) =>
            {
                container.ContextMenu = CreateCardContextMenu(item);
                if (container.ContextMenu != null)
                {
                    container.ContextMenu.PlacementTarget = container;
                    container.ContextMenu.IsOpen = true;
                }
                e.Handled = true;
            };

            // Card drag interaction
            container.MouseLeftButtonDown += (s, e) =>
            {
                if (Keyboard.IsKeyDown(Key.Space)) return;

                if (e.ClickCount == 2)
                {
                    if (item.IsYouTube)
                    {
                        ToggleYouTubePlayback(item);
                        e.Handled = true;
                        return;
                    }
                    if (item.IsLocalVideo)
                    {
                        ToggleLocalVideoPlayback(item);
                        e.Handled = true;
                        return;
                    }

                    SelectCard(item, addToSelection: false);
                    ZoomToCard(item);
                    e.Handled = true;
                    return;
                }

                RecordUndo("Move Card");

                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (isShift)
                {
                    if (item.IsSelected)
                        DeselectCard(item);
                    else
                        SelectCard(item, addToSelection: true);
                }
                else
                {
                    if (!item.IsSelected)
                    {
                        SelectCard(item, addToSelection: false);
                    }
                }

                // Prepare multi-drag
                _isDraggingCards = true;
                _cardDragStartMousePoint = e.GetPosition(CanvasContainer);
                _cardsInitialPositions.Clear();
                foreach (CardItem sel in _selectedCards)
                {
                    _cardsInitialPositions[sel] = new Point(sel.X, sel.Y);
                }
                SetWebViewHitTesting(false);

                if (item.IsLocalVideo)
                {
                    _pendingVideoCardClick = item;
                    _pendingVideoCardStartPoint = e.GetPosition(CanvasContainer);
                }

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            };

            // Resize handle events
            AttachResizeHandleEvents(item, handleTL, ResizeCorner.TopLeft);
            AttachResizeHandleEvents(item, handleTR, ResizeCorner.TopRight);
            AttachResizeHandleEvents(item, handleBL, ResizeCorner.BottomLeft);
            AttachResizeHandleEvents(item, handleBR, ResizeCorner.BottomRight);

            // Determine World placement
            Point pos;
            if (worldPosition.HasValue)
            {
                pos = worldPosition.Value;
            }
            else
            {
                Matrix matrix = CanvasMatrixTransform.Matrix;
                matrix.Invert();
                Point centerScreen = new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
                pos = matrix.Transform(centerScreen);
                pos.X += (_cards.Count % 5) * 35 - (w / 2);
                pos.Y += (_cards.Count % 5) * 35 - (h / 2);
            }

            item.X = pos.X;
            item.Y = pos.Y;

            if (recordUndo && !_isRestoringSession && !_isApplyingSnapshot)
            {
                RecordUndo("Add Card");
            }

            _cards.Add(item);
            WorldCanvas.Children.Add(container);

            if (isLocalVideo && item.NativePlayer != null && item.NativePlayer.Source != null)
            {
                try
                {
                    item.NativePlayer.Play();
                    item.NativePlayer.Pause();
                }
                catch { }
            }

            EnsureLocalCache(item);
            if (!_isRestoringSession && !_isApplyingSnapshot)
            {
                CheckCardGroupAffiliation(item);
            }

            UpdateStatusCounts();
            if (autoSelect)
            {
                SelectCard(item, addToSelection: false);
            }

            if (!_isRestoringSession)
            {
                ScheduleAutoSave();
            }

            ApplyGifAnimationIfNeeded(item);

            return item;
        }

        private void ApplyGifAnimationIfNeeded(CardItem item)
        {
            if (item.IsCropped) return;

            try
            {
                if (!string.IsNullOrEmpty(item.LocalPath) && item.LocalPath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) && File.Exists(item.LocalPath))
                {
                    AnimationBehavior.SetSourceUri(item.ImageControl, new Uri(item.LocalPath, UriKind.Absolute));
                }
                else if (!string.IsNullOrEmpty(item.Base64Data) && (item.Base64Data.StartsWith("data:image/gif", StringComparison.OrdinalIgnoreCase) || IsGifData(item.Base64Data)))
                {
                    string clean = item.Base64Data.Contains(",") ? item.Base64Data.Substring(item.Base64Data.IndexOf(",") + 1) : item.Base64Data;
                    byte[] raw = Convert.FromBase64String(clean);
                    var ms = new MemoryStream(raw);
                    AnimationBehavior.SetSourceStream(item.ImageControl, ms);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to animate GIF: {ex.Message}");
            }
        }

        private static bool IsGifData(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return false;
            try
            {
                string clean = base64Data.Contains(",") ? base64Data.Substring(base64Data.IndexOf(",") + 1) : base64Data;
                if (clean.Length < 8) return false;
                byte[] bytes = Convert.FromBase64String(clean.Substring(0, Math.Min(clean.Length, 16)));
                return bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46; // "GIF"
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureLocalCache(CardItem item)
        {
            if (item.IsNote) return;
            if (!string.IsNullOrEmpty(item.LocalPath) && File.Exists(item.LocalPath))
                return;

            try
            {
                Directory.CreateDirectory(CacheDir);
                string cleanId = string.IsNullOrEmpty(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Replace(" ", "_");
                string prefix = item.IsPaletteCard ? "pal_" : (item.IsYouTube ? "snap_yt_" : "ref_");
                string filename = $"{prefix}{cleanId}.png";
                string targetPath = System.IO.Path.Combine(CacheDir, filename);

                // If already generated and valid, reuse it immediately (no duplication!)
                if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0)
                {
                    item.LocalPath = targetPath;
                    return;
                }

                if (item.Bitmap != null)
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(item.Bitmap));
                    using FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                    encoder.Save(fs);
                    item.LocalPath = targetPath;
                }
            }
            catch
            {
                // Fallback to memory and base64 if disk cache write encounters an issue
            }
        }

        private void ApplyNoteTextColor(CardItem card, string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return;
            card.NoteTextColor = hex;

            Brush brush;
            try
            {
                brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
            }
            catch
            {
                brush = Brushes.White;
            }

            Brush caretBrush = (hex == "#111827" || hex == "#000000" || hex == "#1E293B") ? Brushes.Black : Brushes.White;

            if (card.NoteEditor != null)
            {
                card.NoteEditor.Foreground = brush;
                card.NoteEditor.CaretBrush = caretBrush;
            }

            if (card.ChecklistPanel != null)
            {
                foreach (UIElement child in card.ChecklistPanel.Children)
                {
                    if (child is Grid row && row.Children.Count > 1 && row.Children[1] is Grid th)
                    {
                        foreach (UIElement gc in th.Children)
                        {
                            if (gc is TextBox tb)
                            {
                                tb.Foreground = brush;
                                tb.CaretBrush = caretBrush;
                            }
                        }
                    }
                }
            }
        }

        private void ApplyNoteFontFamily(CardItem card, string fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily)) return;
            card.NoteFontFamily = fontFamily;
            if (card.NoteFontNameText != null)
            {
                card.NoteFontNameText.Text = $"{fontFamily} ▾";
            }

            FontFamily ff = new FontFamily(fontFamily);
            if (card.NoteEditor != null)
            {
                card.NoteEditor.FontFamily = ff;
            }

            if (card.ChecklistPanel != null)
            {
                foreach (UIElement child in card.ChecklistPanel.Children)
                {
                    if (child is Grid row && row.Children.Count > 1 && row.Children[1] is Grid th)
                    {
                        TextBox? tb = null;
                        Border? strikeLine = null;
                        foreach (UIElement gc in th.Children)
                        {
                            if (gc is TextBox t) tb = t;
                            else if (gc is Border b) strikeLine = b;
                        }

                        if (tb != null)
                        {
                            tb.FontFamily = ff;
                            if (strikeLine != null && strikeLine.ActualWidth > 0)
                            {
                                double tw = MeasureTextWidth(tb);
                                strikeLine.Width = Math.Max(20, Math.Min(th.ActualWidth > 0 ? th.ActualWidth - 6 : 280, tw + 6));
                            }
                        }
                    }
                }
            }
        }

        private void ApplyNoteFontSize(CardItem card, double fontSize)
        {
            if (fontSize <= 0) return;
            card.NoteFontSize = fontSize;
            if (card.NoteFontSizeText != null)
            {
                card.NoteFontSizeText.Text = $"{(int)fontSize} ▾";
            }

            if (card.NoteEditor != null)
            {
                card.NoteEditor.FontSize = fontSize;
            }

            if (card.ChecklistPanel != null)
            {
                foreach (UIElement child in card.ChecklistPanel.Children)
                {
                    if (child is Grid row && row.Children.Count > 1 && row.Children[1] is Grid th)
                    {
                        TextBox? tb = null;
                        Border? strikeLine = null;
                        foreach (UIElement gc in th.Children)
                        {
                            if (gc is TextBox t) tb = t;
                            else if (gc is Border b) strikeLine = b;
                        }

                        if (tb != null)
                        {
                            tb.FontSize = fontSize;
                            if (strikeLine != null && strikeLine.ActualWidth > 0)
                            {
                                double tw = MeasureTextWidth(tb);
                                strikeLine.Width = Math.Max(20, Math.Min(th.ActualWidth > 0 ? th.ActualWidth - 6 : 280, tw + 6));
                            }
                        }
                    }
                }
            }
        }

        private void ApplyNoteAlignment(CardItem card, TextAlignment align)
        {
            card.NoteAlignment = align;

            if (card.NoteEditor != null)
            {
                card.NoteEditor.TextAlignment = align;
            }

            if (card.ChecklistPanel != null)
            {
                foreach (UIElement child in card.ChecklistPanel.Children)
                {
                    if (child is Grid row && row.Children.Count > 1 && row.Children[1] is Grid th)
                    {
                        foreach (UIElement gc in th.Children)
                        {
                            if (gc is TextBox tb)
                            {
                                tb.TextAlignment = align;
                            }
                        }
                    }
                }
            }
        }

        private void ApplyNoteBackground(CardItem card, string bgMode)
        {
            card.NoteBgColor = bgMode;
            card.ContentBorder.CornerRadius = new CornerRadius(6);

            bool isLightBg = bgMode == "Yellow Sticky" || bgMode == "Cyan Sticky";
            if (isLightBg && (card.NoteTextColor == "#FFFFFF" || card.NoteTextColor.Equals("White", StringComparison.OrdinalIgnoreCase)))
            {
                ApplyNoteTextColor(card, "#111827");
            }
            else if (!isLightBg && (card.NoteTextColor == "#111827" || card.NoteTextColor == "#000000"))
            {
                ApplyNoteTextColor(card, "#FFFFFF");
            }

            // If a background GIF is active, adapt the readability overlay according to the note background
            if (card.NoteBgGifOverlay != null)
            {
                if (bgMode == "Yellow Sticky" || bgMode == "Cyan Sticky" || bgMode == "Transparent")
                {
                    card.NoteBgGifOverlay.Background = Brushes.Transparent;
                }
                else
                {
                    card.NoteBgGifOverlay.Background = new SolidColorBrush(Color.FromArgb(90, 15, 23, 42));
                }
            }

            switch (bgMode)
            {
                case "Transparent":
                    // Alpha 1 makes it visually 100% transparent while remaining fully hit-testable in WPF DWM
                    card.ContentBorder.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                    card.ContentBorder.BorderBrush = card.IsSelected 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : Brushes.Transparent;
                    card.ContentBorder.BorderThickness = card.IsSelected ? new Thickness(1.5) : new Thickness(0);
                    card.ContentBorder.Effect = null;
                    break;

                case "Dark Glass":
                    card.ContentBorder.Background = new SolidColorBrush(Color.FromArgb(190, 20, 24, 33));
                    card.ContentBorder.BorderBrush = card.IsSelected 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                    card.ContentBorder.BorderThickness = card.IsSelected ? new Thickness(2) : new Thickness(1);
                    card.ContentBorder.Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 6, Opacity = 0.5, Color = Colors.Black };
                    break;

                case "Solid Dark":
                    card.ContentBorder.Background = new SolidColorBrush(Color.FromRgb(22, 27, 36));
                    card.ContentBorder.BorderBrush = card.IsSelected 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                    card.ContentBorder.BorderThickness = card.IsSelected ? new Thickness(2) : new Thickness(1);
                    card.ContentBorder.Effect = new DropShadowEffect { BlurRadius = 25, ShadowDepth = 8, Opacity = 0.65, Color = Colors.Black };
                    break;

                case "Yellow Sticky":
                    card.ContentBorder.Background = new SolidColorBrush(Color.FromArgb(250, 254, 240, 138));
                    card.ContentBorder.BorderBrush = card.IsSelected 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : new SolidColorBrush(Color.FromArgb(120, 202, 138, 4));
                    card.ContentBorder.BorderThickness = card.IsSelected ? new Thickness(2) : new Thickness(1);
                    card.ContentBorder.Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 5, Opacity = 0.35, Color = Colors.Black };
                    break;

                case "Cyan Sticky":
                    card.ContentBorder.Background = new SolidColorBrush(Color.FromArgb(250, 165, 243, 252));
                    card.ContentBorder.BorderBrush = card.IsSelected 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : new SolidColorBrush(Color.FromArgb(120, 14, 165, 233));
                    card.ContentBorder.BorderThickness = card.IsSelected ? new Thickness(2) : new Thickness(1);
                    card.ContentBorder.Effect = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 5, Opacity = 0.35, Color = Colors.Black };
                    break;

                default:
                    card.ContentBorder.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                    card.ContentBorder.BorderBrush = card.IsSelected 
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                        : new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                    card.ContentBorder.BorderThickness = card.IsSelected ? new Thickness(2) : new Thickness(1);
                    card.ContentBorder.Effect = null;
                    break;
            }
        }

        private void ApplyNoteShadow(CardItem card)
        {
            DropShadowEffect? shadow = card.NoteHasShadow
                ? new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Direction = 315,
                    Opacity = 0.95
                }
                : null;

            if (card.NoteEditor != null)
            {
                card.NoteEditor.Effect = shadow;
            }

            if (card.ChecklistPanel != null)
            {
                foreach (UIElement child in card.ChecklistPanel.Children)
                {
                    if (child is Grid row && row.Children.Count > 1 && row.Children[1] is Grid th)
                    {
                        foreach (UIElement gc in th.Children)
                        {
                            if (gc is TextBox tb)
                            {
                                tb.Effect = shadow;
                            }
                        }
                    }
                }
            }
        }

        private void ApplyNoteBgGif(CardItem card, string? gifPath, string? gifBase64 = null)
        {
            card.NoteBgGifPath = gifPath;
            card.NoteBgGifBase64 = gifBase64;

            if (card.NoteBgGifImage == null || card.NoteBgGifOverlay == null) return;

            if (string.IsNullOrEmpty(gifPath) && string.IsNullOrEmpty(gifBase64))
            {
                card.NoteBgGifImage.Visibility = Visibility.Collapsed;
                card.NoteBgGifOverlay.Visibility = Visibility.Collapsed;
                try
                {
                    AnimationBehavior.SetSourceUri(card.NoteBgGifImage, null);
                }
                catch { }
                ApplyNoteBackground(card, card.NoteBgColor);
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(gifPath) && File.Exists(gifPath))
                {
                    AnimationBehavior.SetSourceUri(card.NoteBgGifImage, new Uri(gifPath, UriKind.Absolute));
                    card.NoteBgGifImage.Visibility = Visibility.Visible;
                    card.NoteBgGifOverlay.Visibility = Visibility.Visible;
                }
                else if (!string.IsNullOrEmpty(gifBase64))
                {
                    string clean = gifBase64.Contains(",") ? gifBase64.Substring(gifBase64.IndexOf(",") + 1) : gifBase64;
                    byte[] raw = Convert.FromBase64String(clean);
                    var ms = new MemoryStream(raw);
                    AnimationBehavior.SetSourceStream(card.NoteBgGifImage, ms);
                    card.NoteBgGifImage.Visibility = Visibility.Visible;
                    card.NoteBgGifOverlay.Visibility = Visibility.Visible;
                }
                ApplyNoteBackground(card, card.NoteBgColor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set note background GIF: {ex.Message}");
            }
        }

        private void FitNoteToGifAspectRatio(CardItem card)
        {
            if (card.NoteBgGifImage == null) return;
            double gifW = 0, gifH = 0;
            if (!string.IsNullOrEmpty(card.NoteBgGifPath) && File.Exists(card.NoteBgGifPath))
            {
                try
                {
                    using var stream = File.OpenRead(card.NoteBgGifPath);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        gifW = decoder.Frames[0].PixelWidth;
                        gifH = decoder.Frames[0].PixelHeight;
                    }
                }
                catch { }
            }
            else if (!string.IsNullOrEmpty(card.NoteBgGifBase64))
            {
                try
                {
                    string clean = card.NoteBgGifBase64.Contains(",") ? card.NoteBgGifBase64.Substring(card.NoteBgGifBase64.IndexOf(",") + 1) : card.NoteBgGifBase64;
                    byte[] raw = Convert.FromBase64String(clean);
                    using var ms = new MemoryStream(raw);
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        gifW = decoder.Frames[0].PixelWidth;
                        gifH = decoder.Frames[0].PixelHeight;
                    }
                }
                catch { }
            }

            if (gifW > 0 && gifH > 0)
            {
                double targetAspect = gifW / gifH;
                card.Height = Math.Round(card.Width / targetAspect);
                card.AspectRatio = targetAspect;
                ScheduleAutoSave();
            }
        }

        private void PromptSetNoteBgGif(CardItem item)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Animated GIF (*.gif)|*.gif|All Image Files (*.gif;*.png;*.jpg;*.jpeg;*.webp)|*.gif;*.png;*.jpg;*.jpeg;*.webp|All Files (*.*)|*.*",
                Title = "Select Animated GIF Background for Note"
            };

            if (dlg.ShowDialog() == true)
            {
                ApplyNoteBgGif(item, dlg.FileName);
                ScheduleAutoSave();
                ShowToast("🎬 Applied animated GIF background to note!", ToastType.Success);
            }
        }

        private CardItem AddNoteCard(
            string text = "Type your note here...",
            Point? worldPosition = null,
            double? customWidth = null,
            double? customHeight = null,
            string fontFamily = "Segoe UI",
            double fontSize = 16.0,
            string textColor = "#FFFFFF",
            string bgColor = "Transparent",
            TextAlignment alignment = TextAlignment.Left,
            bool hasShadow = false,
            bool autoSelect = true,
            bool isChecklist = false,
            List<NoteChecklistItem>? checklistItems = null,
            bool hasDeadline = false,
            DateTime? deadlineDateTime = null,
            string deadlineLabel = "Deadline",
            string noteDoodleInkBase64 = "",
            string? noteBgGifPath = null,
            string? noteBgGifBase64 = null)
        {
            EmptyStateOverlay.Visibility = Visibility.Collapsed;

            double w = customWidth ?? (isChecklist ? 320.0 : 280.0);
            double h = customHeight ?? (isChecklist ? 240.0 : 180.0);

            // 1x1 frozen transparent BitmapSource as dummy bitmap for CardItem requirements
            BitmapSource dummyBmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
            dummyBmp.Freeze();

            Image dummyImg = new Image
            {
                Source = dummyBmp,
                Visibility = Visibility.Collapsed
            };

            TextBox editor = new TextBox
            {
                Text = text,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = alignment,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(14, 2, 14, 12),
                FontFamily = new FontFamily(string.IsNullOrWhiteSpace(fontFamily) ? "Segoe UI" : fontFamily),
                FontSize = fontSize > 0 ? fontSize : 16.0,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                CaretBrush = Brushes.White,
                Cursor = Cursors.SizeAll,
                Visibility = isChecklist ? Visibility.Collapsed : Visibility.Visible
            };

            editor.GotKeyboardFocus += (s, e) => { editor.Cursor = Cursors.IBeam; };
            editor.LostKeyboardFocus += (s, e) => { editor.Cursor = Cursors.SizeAll; };
            editor.PreviewMouseWheel += (s, e) =>
            {
                string mode = string.IsNullOrEmpty(_settings.NavMode) ? "macos" : _settings.NavMode.ToLowerInvariant();
                bool isCtrlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                if (mode == "mouse" || isCtrlDown)
                {
                    Point mousePos = e.GetPosition(CanvasContainer);
                    PerformCanvasZoom(e.Delta, mousePos);
                    e.Handled = true;
                }
            };

            try
            {
                editor.Foreground = (Brush)new BrushConverter().ConvertFromString(textColor)!;
            }
            catch
            {
                editor.Foreground = Brushes.White;
            }

            if ((bgColor == "Yellow Sticky" || bgColor == "Cyan Sticky") && textColor == "#FFFFFF")
            {
                textColor = "#111827";
                editor.Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                editor.CaretBrush = Brushes.Black;
            }

            // Top drag/pan strip above text
            Border dragHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), // hit-testable
                Cursor = Cursors.SizeAll,
                ToolTip = "Drag to move note"
            };

            Border gripPill = new Border
            {
                Width = 28,
                Height = 3.5,
                CornerRadius = new CornerRadius(1.75),
                Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.22,
                IsHitTestVisible = false
            };
            dragHeader.Child = gripPill;

            dragHeader.MouseEnter += (s, e) => { gripPill.Opacity = 0.85; };
            dragHeader.MouseLeave += (s, e) => { gripPill.Opacity = 0.22; };

            // Realtime Deadline Banner Row
            Border deadlineBanner = new Border
            {
                Margin = new Thickness(10, 0, 10, 5),
                Padding = new Thickness(8, 4, 8, 4),
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromArgb(235, 12, 38, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Visibility = hasDeadline ? Visibility.Visible : Visibility.Collapsed,
                ToolTip = "Realtime Deadline (Synchronized with computer clock - click to edit)"
            };

            Grid dlGrid = new Grid();
            dlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtDeadline = new TextBlock
            {
                Text = "⏱️ Deadline",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            Border btnClearDl = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(3),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = "Clear Deadline",
                Margin = new Thickness(4, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btnClearDl.MouseEnter += (s, e) => btnClearDl.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            btnClearDl.MouseLeave += (s, e) => btnClearDl.Background = Brushes.Transparent;

            Grid.SetColumn(txtDeadline, 0);
            Grid.SetColumn(btnClearDl, 1);
            dlGrid.Children.Add(txtDeadline);
            dlGrid.Children.Add(btnClearDl);
            deadlineBanner.Child = dlGrid;

            // Note Body: Hosts either multiline editor OR checklist
            Grid noteBodyContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Checklist ScrollViewer & Panel
            ScrollViewer checklistScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(10, 2, 10, 8),
                Visibility = isChecklist ? Visibility.Visible : Visibility.Collapsed,
                Focusable = false
            };
            checklistScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                string mode = string.IsNullOrEmpty(_settings.NavMode) ? "macos" : _settings.NavMode.ToLowerInvariant();
                bool isCtrlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                if (mode == "mouse" || isCtrlDown)
                {
                    Point mousePos = e.GetPosition(CanvasContainer);
                    PerformCanvasZoom(e.Delta, mousePos);
                    e.Handled = true;
                }
            };
            StackPanel checklistPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            checklistScrollViewer.Content = checklistPanel;

            noteBodyContainer.Children.Add(editor);
            noteBodyContainer.Children.Add(checklistScrollViewer);

            // Note Content Grid
            Grid noteContentLayer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            noteContentLayer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            noteContentLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            noteContentLayer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(dragHeader, 0);
            Grid.SetRow(deadlineBanner, 1);
            Grid.SetRow(noteBodyContainer, 2);
            noteContentLayer.Children.Add(dragHeader);
            noteContentLayer.Children.Add(deadlineBanner);
            noteContentLayer.Children.Add(noteBodyContainer);

            // Freehand Doodle / Coret InkCanvas Layer directly on top of note
            InkCanvas doodleCanvas = new InkCanvas
            {
                Background = Brushes.Transparent,
                Cursor = Cursors.Pen,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };
            doodleCanvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Color.FromRgb(244, 63, 94), // Neon coral / rose stroke
                Width = 2.5,
                Height = 2.5,
                FitToCurve = true,
                IgnorePressure = false,
                StylusTip = StylusTip.Ellipse
            };

            if (!string.IsNullOrEmpty(noteDoodleInkBase64))
            {
                try
                {
                    byte[] dBytes = Convert.FromBase64String(noteDoodleInkBase64);
                    using MemoryStream ms = new MemoryStream(dBytes);
                    doodleCanvas.Strokes = new System.Windows.Ink.StrokeCollection(ms);
                }
                catch { }
            }

            // Doodle Mode Active Floating Pill Bar (Bottom-centered for clear visibility on any note)
            Border doodleIndicator = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 8),
                Background = new SolidColorBrush(Color.FromArgb(245, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(244, 63, 94)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 3, 7, 3),
                Visibility = Visibility.Collapsed,
                Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 3, Opacity = 0.65, Color = Colors.Black }
            };
            Panel.SetZIndex(doodleIndicator, 999);

            StackPanel indSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock indTxt = new TextBlock
            {
                Text = "✏ Doodle Mode",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Border indBtnUndo = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Undo last stroke (Ctrl+Z)",
                Child = new TextBlock { Text = "↩ Undo", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)) }
            };
            TextBlock txtEraser = new TextBlock { Text = "🧹 Eraser", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)) };
            Border indBtnEraser = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Toggle Eraser Mode to erase strokes",
                Child = txtEraser
            };
            Border indBtnClear = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Clear all doodle strokes on note",
                Child = new TextBlock { Text = "Clear", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)) }
            };
            Border indBtnDone = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 244, 63, 94)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 1, 5, 1),
                Cursor = Cursors.Hand,
                ToolTip = "Finish doodling and return to normal text editing",
                Child = new TextBlock { Text = "✓ Done", FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White }
            };

            indSp.Children.Add(indTxt);
            indSp.Children.Add(indBtnUndo);
            indSp.Children.Add(indBtnEraser);
            indSp.Children.Add(indBtnClear);
            indSp.Children.Add(indBtnDone);
            doodleIndicator.Child = indSp;

            double baseW = isChecklist ? 320.0 : 280.0;
            double baseH = isChecklist ? 240.0 : 180.0;

            // Note Background Animated GIF Layer (auto-centered horizontally and vertically on resize)
            Image bgGifImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            // High contrast tint overlay to keep text/checklist legible over bright or busy animated GIFs
            Border bgGifOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(135, 15, 23, 42)), // 53% slate-900 tint
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };

            // Note Root Grid (design base coordinates)
            Grid noteRootGrid = new Grid
            {
                Width = baseW,
                Height = baseH,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            noteRootGrid.Children.Add(noteContentLayer);
            noteRootGrid.Children.Add(doodleCanvas);
            noteRootGrid.Children.Add(doodleIndicator);

            // Dynamic Content Scaling: Wrap noteRootGrid inside Viewbox with Stretch.Uniform so all text, checklist items,
            // checkboxes, strike-through lines, countdown banner, and doodles scale cleanly WITHOUT becoming gepeng!
            // Aligned to TOP so header, deadline banner, and checklist always stay pinned to the top of the card!
            Viewbox noteViewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Child = noteRootGrid
            };

            // Bottom-left quick pen button for fast doodle / strike access right on note
            Border btnCornerPen = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(160, 244, 63, 94)),
                BorderThickness = new Thickness(1.2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 0, 8),
                Cursor = Cursors.Hand,
                ToolTip = "Toggle Doodle / Strike Mode",
                Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.55, Color = Colors.Black }
            };
            Panel.SetZIndex(btnCornerPen, 995);

            System.Windows.Shapes.Path cornerPenPath = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 18,2 L 22,6 L 7,21 L 2,22 L 3,17 Z M 15,5 L 19,9"),
                Stroke = new SolidColorBrush(Color.FromRgb(244, 63, 94)),
                StrokeThickness = 1.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                SnapsToDevicePixels = true
            };
            btnCornerPen.Child = new Viewbox
            {
                Width = 12,
                Height = 12,
                Child = cornerPenPath
            };

            // Note Host Grid: Hosts the background GIF/tint directly across the FULL actual card dimensions (w x h),
            // so animated GIFs always fit and fill the entire card panel edge-to-edge without letterboxing gaps!
            Grid noteHostGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true
            };
            noteHostGrid.Children.Add(bgGifImage);
            noteHostGrid.Children.Add(bgGifOverlay);
            noteHostGrid.Children.Add(noteViewbox);
            noteHostGrid.Children.Add(btnCornerPen);

            noteHostGrid.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
                {
                    noteHostGrid.Clip = new RectangleGeometry(new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 6, 6);
                }
            };
            noteHostGrid.Clip = new RectangleGeometry(new Rect(0, 0, Math.Max(1, w), Math.Max(1, h)), 6, 6);

            Border contentBorder = new Border
            {
                Width = w,
                Height = h,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = noteHostGrid,
                ClipToBounds = true
            };

            Grid container = new Grid
            {
                Width = w,
                Height = h,
                Cursor = Cursors.SizeAll,
                AllowDrop = true
            };
            container.Children.Add(dummyImg);
            container.Children.Add(contentBorder);

            // 4 Corner Handles + 4 Edge Pill Handles (Enables resizing just right panel/edge thin or wide!)
            Border handleTL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE);
            Border handleTR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW);
            Border handleBL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW);
            Border handleBR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE);
            Border handleR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Center, Cursors.SizeWE);
            Border handleL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Center, Cursors.SizeWE);
            Border handleT = CreateResizeHandle(HorizontalAlignment.Center, VerticalAlignment.Top, Cursors.SizeNS);
            Border handleB = CreateResizeHandle(HorizontalAlignment.Center, VerticalAlignment.Bottom, Cursors.SizeNS);

            container.Children.Add(handleTL);
            container.Children.Add(handleTR);
            container.Children.Add(handleBL);
            container.Children.Add(handleBR);
            container.Children.Add(handleR);
            container.Children.Add(handleL);
            container.Children.Add(handleT);
            container.Children.Add(handleB);
            Panel.SetZIndex(container, 10);

            CardItem item = new CardItem
            {
                Container = container,
                ContentBorder = contentBorder,
                ImageControl = dummyImg,
                Bitmap = dummyBmp,
                OriginalBitmap = dummyBmp,
                BaseWidth = w,
                BaseHeight = h,
                AspectRatio = w / Math.Max(1.0, h),
                HandleTL = handleTL,
                HandleTR = handleTR,
                HandleBL = handleBL,
                HandleBR = handleBR,
                HandleR = handleR,
                HandleL = handleL,
                HandleT = handleT,
                HandleB = handleB,
                IsNote = true,
                NoteText = text,
                NoteFontFamily = fontFamily,
                NoteFontSize = fontSize,
                NoteTextColor = textColor,
                NoteBgColor = bgColor,
                NoteAlignment = alignment,
                NoteHasShadow = hasShadow,
                NoteEditor = editor,
                HasDeadline = hasDeadline,
                DeadlineDateTime = deadlineDateTime,
                DeadlineLabel = deadlineLabel,
                DeadlineBadge = deadlineBanner,
                DeadlineText = txtDeadline,
                IsChecklist = isChecklist,
                ChecklistItems = checklistItems ?? new(),
                NoteBodyContainer = noteBodyContainer,
                ChecklistScrollViewer = checklistScrollViewer,
                ChecklistPanel = checklistPanel,
                NoteDoodleCanvas = doodleCanvas,
                NoteDoodleInkBase64 = noteDoodleInkBase64,
                NoteDoodleIndicator = doodleIndicator,
                BtnNoteCornerPen = btnCornerPen,
                NoteCornerPenIcon = cornerPenPath,
                NoteBgGifPath = noteBgGifPath,
                NoteBgGifBase64 = noteBgGifBase64,
                NoteBgGifImage = bgGifImage,
                NoteBgGifOverlay = bgGifOverlay
            };

            btnCornerPen.MouseEnter += (s, e) =>
            {
                if (!item.IsNoteDoodleActive)
                {
                    btnCornerPen.Background = new SolidColorBrush(Color.FromArgb(240, 30, 41, 59));
                    btnCornerPen.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                }
            };
            btnCornerPen.MouseLeave += (s, e) =>
            {
                if (!item.IsNoteDoodleActive)
                {
                    btnCornerPen.Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
                    btnCornerPen.BorderBrush = new SolidColorBrush(Color.FromArgb(160, 244, 63, 94));
                }
            };
            btnCornerPen.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
            btnCornerPen.PreviewMouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                if (!item.IsSelected) SelectCard(item, addToSelection: false);
                ToggleNoteDoodleMode(item);
            };

            // Drag & drop .gif file directly onto note container to set background GIF
            container.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0 && files[0].EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            };
            container.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0)
                    {
                        string file = files[0];
                        string ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".gif" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp")
                        {
                            e.Handled = true;
                            ApplyNoteBgGif(item, file);
                            ScheduleAutoSave();
                            ShowToast("🎬 Applied animated GIF background to note!", ToastType.Success);
                        }
                    }
                }
            };

            ApplyNoteBackground(item, bgColor);
            ApplyNoteShadow(item);
            if (!string.IsNullOrEmpty(noteBgGifPath) || !string.IsNullOrEmpty(noteBgGifBase64))
            {
                ApplyNoteBgGif(item, noteBgGifPath, noteBgGifBase64);
            }

            editor.TextChanged += (s, e) =>
            {
                item.NoteText = editor.Text;
                ScheduleAutoSave();
            };

            // Doodle Canvas stroke event
            void UpdateNoteDoodleData()
            {
                try
                {
                    using MemoryStream ms = new MemoryStream();
                    doodleCanvas.Strokes.Save(ms);
                    item.NoteDoodleInkBase64 = Convert.ToBase64String(ms.ToArray());
                    ScheduleAutoSave();
                }
                catch { }
            }
            doodleCanvas.StrokeCollected += (s, e) =>
            {
                UpdateNoteDoodleData();

                // Gesture detection: If the user draws a horizontal strike stroke across a checklist item,
                // automatically detect it and mark that checklist item as completed / done!
                if (item.IsChecklist && item.ChecklistPanel != null && item.ChecklistItems != null)
                {
                    Rect sBounds = e.Stroke.GetBounds();
                    if (sBounds.Width >= 25)
                    {
                        for (int i = 0; i < item.ChecklistPanel.Children.Count - 1 && i < item.ChecklistItems.Count; i++)
                        {
                            if (item.ChecklistPanel.Children[i] is Grid row)
                            {
                                try
                                {
                                    GeneralTransform gt = row.TransformToVisual(doodleCanvas);
                                    Rect rowBounds = gt.TransformBounds(new Rect(0, 0, row.ActualWidth, row.ActualHeight));
                                    double strokeCenterY = sBounds.Top + sBounds.Height / 2.0;
                                    if (strokeCenterY >= rowBounds.Top && strokeCenterY <= rowBounds.Bottom)
                                    {
                                        var cItem = item.ChecklistItems[i];
                                        if (!cItem.IsChecked)
                                        {
                                            cItem.IsChecked = true;
                                            if (row.Children.Count > 0 && row.Children[0] is Border chk)
                                            {
                                                chk.Background = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                                                chk.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                                                if (chk.Child is System.Windows.Shapes.Path chkMark)
                                                {
                                                    chkMark.Opacity = 1.0;
                                                }
                                            }

                                            if (row.Children.Count > 1 && row.Children[1] is Grid th)
                                            {
                                                TextBox? tb = null;
                                                Border? strike = null;
                                                foreach (UIElement gc in th.Children)
                                                {
                                                    if (gc is TextBox t) tb = t;
                                                    else if (gc is Border b) strike = b;
                                                }

                                                if (tb != null) tb.Opacity = 0.45;
                                                if (strike != null)
                                                {
                                                    double tw = tb != null ? MeasureTextWidth(tb) : 100;
                                                    double targetW = Math.Max(20, Math.Min(th.ActualWidth > 0 ? th.ActualWidth - 6 : 280, tw + 6));
                                                    DoubleAnimation animW = new DoubleAnimation
                                                    {
                                                        From = strike.ActualWidth,
                                                        To = targetW,
                                                        Duration = TimeSpan.FromMilliseconds(260),
                                                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                                                    };
                                                    strike.BeginAnimation(FrameworkElement.WidthProperty, animW);
                                                }
                                            }

                                            ScheduleAutoSave();
                                            ShowToast($"✓ Strike detected: '{cItem.Text}' marked as Done!", ToastType.Success);
                                            break;
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            };
            doodleCanvas.StrokeErased += (s, e) => UpdateNoteDoodleData();

            indBtnUndo.MouseEnter += (s, e) => indBtnUndo.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            indBtnUndo.MouseLeave += (s, e) => indBtnUndo.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            indBtnUndo.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (doodleCanvas.Strokes.Count > 0)
                {
                    doodleCanvas.Strokes.RemoveAt(doodleCanvas.Strokes.Count - 1);
                    UpdateNoteDoodleData();
                    ShowToast("Undid last doodle stroke", ToastType.Info, 800);
                }
            };

            bool isEraserMode = false;
            indBtnEraser.MouseEnter += (s, e) =>
            {
                if (!isEraserMode) indBtnEraser.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            };
            indBtnEraser.MouseLeave += (s, e) =>
            {
                if (!isEraserMode) indBtnEraser.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            };
            indBtnEraser.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                isEraserMode = !isEraserMode;
                if (isEraserMode)
                {
                    doodleCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    doodleCanvas.Cursor = Cursors.Cross;
                    txtEraser.Text = "✏ Pen";
                    indBtnEraser.Background = new SolidColorBrush(Color.FromArgb(140, 244, 63, 94));
                    ShowToast("Eraser tool: Click/touch any stroke to erase", ToastType.Info, 900);
                }
                else
                {
                    doodleCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    doodleCanvas.Cursor = Cursors.Pen;
                    txtEraser.Text = "🧹 Eraser";
                    indBtnEraser.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                }
            };

            indBtnClear.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                doodleCanvas.Strokes.Clear();
                item.NoteDoodleInkBase64 = "";
                ScheduleAutoSave();
                ShowToast("Cleared drawing strokes on note", ToastType.Info);
            };
            indBtnDone.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (isEraserMode)
                {
                    isEraserMode = false;
                    doodleCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    doodleCanvas.Cursor = Cursors.Pen;
                    txtEraser.Text = "🧹 Eraser";
                    indBtnEraser.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                }
                ToggleNoteDoodleMode(item);
            };

            btnClearDl.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                item.HasDeadline = false;
                item.DeadlineDateTime = null;
                deadlineBanner.Visibility = Visibility.Collapsed;
                ScheduleAutoSave();
                ShowToast("Deadline removed", ToastType.Info);
            };
            deadlineBanner.MouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is DependencyObject d && FindVisualParent<Border>(d) == btnClearDl)
                {
                    return;
                }
                e.Handled = true;
                ShowDeadlinePickerPopup(deadlineBanner, item);
            };

            if (hasDeadline && deadlineDateTime.HasValue)
            {
                UpdateNoteDeadlineBadgeUI(item);
            }

            if (isChecklist)
            {
                RenderChecklistItems(item);
            }

            // Attach Hover Quick-Action Toolbar
            Border hoverToolbar = CreateCardHoverToolbar(item);
            item.HoverToolbar = hoverToolbar;
            Canvas toolbarHost = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 0,
                Height = 0,
                ClipToBounds = false
            };
            Panel.SetZIndex(toolbarHost, 9999);
            Canvas.SetTop(hoverToolbar, -38.0);
            toolbarHost.Children.Add(hoverToolbar);
            container.Children.Add(toolbarHost);

            hoverToolbar.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0)
                {
                    Canvas.SetLeft(hoverToolbar, -e.NewSize.Width / 2.0);
                    Canvas.SetTop(hoverToolbar, -38.0);
                }
            };

            container.MouseEnter += (s, e) =>
            {
                if (Panel.GetZIndex(container) < 500 && !item.IsSelected)
                {
                    Panel.SetZIndex(container, 500);
                }
                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                hoverToolbar.Opacity = 1.0;
                hoverToolbar.IsHitTestVisible = true;
            };
            container.MouseLeave += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    if (Panel.GetZIndex(container) == 500)
                    {
                        Panel.SetZIndex(container, 10);
                    }
                }
                if (item.IsSelected || editor.IsFocused || _isCardSubMenuOpen) return;
                DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                anim.Completed += (s2, e2) =>
                {
                    if (!container.IsMouseOver && !hoverToolbar.IsMouseOver && !item.IsSelected && !editor.IsFocused && !_isCardSubMenuOpen)
                    {
                        hoverToolbar.IsHitTestVisible = false;
                    }
                };
                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
            };

            // Attach Right-Click Context Menu
            container.ContextMenu = CreateCardContextMenu(item);
            container.MouseRightButtonDown += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    SelectCard(item, addToSelection: false);
                }
                e.Handled = true;
            };
            container.MouseRightButtonUp += (s, e) =>
            {
                container.ContextMenu = CreateCardContextMenu(item);
                if (container.ContextMenu != null)
                {
                    container.ContextMenu.PlacementTarget = container;
                    container.ContextMenu.IsOpen = true;
                }
                e.Handled = true;
            };

            // Mouse interaction for selection & drag
            void TriggerNoteDrag(MouseEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.Space)) return;

                if (e is MouseButtonEventArgs mbe && mbe.ClickCount == 2)
                {
                    SelectCard(item, addToSelection: false);
                    ZoomToCard(item);
                    e.Handled = true;
                    return;
                }

                RecordUndo("Move Note");

                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (isShift)
                {
                    if (item.IsSelected)
                        DeselectCard(item);
                    else
                        SelectCard(item, addToSelection: true);
                }
                else
                {
                    if (!item.IsSelected)
                    {
                        SelectCard(item, addToSelection: false);
                    }
                }

                // Prepare multi-drag
                _isDraggingCards = true;
                _cardDragStartMousePoint = e.GetPosition(CanvasContainer);
                _cardsInitialPositions.Clear();
                foreach (CardItem sel in _selectedCards)
                {
                    _cardsInitialPositions[sel] = new Point(sel.X, sel.Y);
                }
                SetWebViewHitTesting(false);

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            }

            dragHeader.MouseLeftButtonDown += (s, e) =>
            {
                TriggerNoteDrag(e);
            };

            container.MouseLeftButtonDown += (s, e) =>
            {
                // If user clicked inside editor or checklist or deadline, let them interact directly
                if (e.OriginalSource is DependencyObject dep)
                {
                    if (FindVisualParent<TextBox>(dep) != null || FindVisualParent<Button>(dep) != null)
                    {
                        return;
                    }
                    if (item.IsChecklist && FindVisualParent<ScrollViewer>(dep) != null)
                    {
                        return;
                    }
                    if (item.DeadlineBadge != null && FindVisualParent<Border>(dep) == item.DeadlineBadge)
                    {
                        return;
                    }
                }

                TriggerNoteDrag(e);
            };

            Point editorMouseDownPos = new Point(0, 0);
            bool isEditorMouseDown = false;

            editor.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (Keyboard.IsKeyDown(Key.Space)) return;

                // Alt key forces move drag directly even when editing
                if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                {
                    TriggerNoteDrag(e);
                    return;
                }

                if (!editor.IsKeyboardFocused)
                {
                    isEditorMouseDown = true;
                    editorMouseDownPos = e.GetPosition(CanvasContainer);
                    if (!item.IsSelected)
                    {
                        SelectCard(item, addToSelection: false);
                    }
                }
            };

            editor.PreviewMouseMove += (s, e) =>
            {
                if (isEditorMouseDown && e.LeftButton == MouseButtonState.Pressed && !editor.IsKeyboardFocused)
                {
                    Point currentPos = e.GetPosition(CanvasContainer);
                    Vector diff = currentPos - editorMouseDownPos;
                    if (Math.Abs(diff.X) > 4 || Math.Abs(diff.Y) > 4)
                    {
                        isEditorMouseDown = false;
                        TriggerNoteDrag(e);
                    }
                }
            };

            editor.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (isEditorMouseDown)
                {
                    isEditorMouseDown = false;
                    editor.Focus();
                    int charIndex = editor.GetCharacterIndexFromPoint(e.GetPosition(editor), true);
                    if (charIndex >= 0)
                    {
                        editor.CaretIndex = charIndex;
                    }
                }
            };

            editor.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Keyboard.ClearFocus();
                    Focus();
                    e.Handled = true;
                }
            };

            // Resize handle events
            AttachResizeHandleEvents(item, handleTL, ResizeCorner.TopLeft);
            AttachResizeHandleEvents(item, handleTR, ResizeCorner.TopRight);
            AttachResizeHandleEvents(item, handleBL, ResizeCorner.BottomLeft);
            AttachResizeHandleEvents(item, handleBR, ResizeCorner.BottomRight);
            AttachResizeHandleEvents(item, handleR, ResizeCorner.Right);
            AttachResizeHandleEvents(item, handleL, ResizeCorner.Left);
            AttachResizeHandleEvents(item, handleT, ResizeCorner.Top);
            AttachResizeHandleEvents(item, handleB, ResizeCorner.Bottom);

            // Determine World placement
            Point pos;
            if (worldPosition.HasValue)
            {
                pos = worldPosition.Value;
            }
            else
            {
                Matrix matrix = CanvasMatrixTransform.Matrix;
                matrix.Invert();
                Point centerScreen = new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
                pos = matrix.Transform(centerScreen);
                pos.X += (_cards.Count % 5) * 35 - (w / 2);
                pos.Y += (_cards.Count % 5) * 35 - (h / 2);
            }

            item.X = pos.X;
            item.Y = pos.Y;

            if (!_isRestoringSession && !_isApplyingSnapshot)
            {
                RecordUndo("Add Note");
            }

            _cards.Add(item);
            WorldCanvas.Children.Add(container);

            CheckCardGroupAffiliation(item);

            UpdateStatusCounts();
            if (autoSelect)
            {
                SelectCard(item, addToSelection: false);
            }

            if (!_isRestoringSession)
            {
                ScheduleAutoSave();
            }

            return item;
        }

        #region Note Deadline, Checklist & Doodle Engine

        private void UpdateAllDeadlineCards()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeAsync(UpdateAllDeadlineCards);
                return;
            }

            var deadlineCards = _cards.Where(c => c.IsNote && c.HasDeadline && c.DeadlineDateTime.HasValue).ToList();
            foreach (var card in deadlineCards)
            {
                UpdateNoteDeadlineBadgeUI(card);
            }
        }

        private void UpdateNoteDeadlineBadgeUI(CardItem card)
        {
            if (card.DeadlineBadge == null || card.DeadlineText == null || !card.DeadlineDateTime.HasValue) return;

            card.DeadlineBadge.Visibility = card.HasDeadline ? Visibility.Visible : Visibility.Collapsed;
            if (!card.HasDeadline) return;

            DateTime now = DateTime.Now;
            DateTime dl = card.DeadlineDateTime.Value;
            TimeSpan diff = dl - now;
            bool isOverdue = diff <= TimeSpan.Zero;
            string labelPrefix = string.IsNullOrWhiteSpace(card.DeadlineLabel) || card.DeadlineLabel == "Deadline"
                ? "" : $"{card.DeadlineLabel}: ";

            if (isOverdue)
            {
                TimeSpan past = now - dl;
                card.DeadlineText.Text = $"🚨 OVERDUE: {labelPrefix}{dl:HH:mm} ({FormatDiffShort(past)} ago)";
                card.DeadlineBadge.Background = new SolidColorBrush(Color.FromArgb(235, 69, 10, 10)); // Dark red
                card.DeadlineBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Bright red
                card.DeadlineText.Foreground = new SolidColorBrush(Color.FromRgb(254, 202, 202));
            }
            else if (diff.TotalMinutes < 60)
            {
                card.DeadlineText.Text = $"🔥 DUE SOON: {labelPrefix}{dl:HH:mm} ({(int)Math.Max(1, diff.TotalMinutes)}m left)";
                card.DeadlineBadge.Background = new SolidColorBrush(Color.FromArgb(235, 67, 20, 7)); // Dark orange
                card.DeadlineBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Bright orange
                card.DeadlineText.Foreground = new SolidColorBrush(Color.FromRgb(254, 215, 170));
            }
            else if (diff.TotalHours < 24)
            {
                card.DeadlineText.Text = $"⚠️ Due Today: {labelPrefix}{dl:HH:mm} ({(int)diff.TotalHours}h {diff.Minutes}m left)";
                card.DeadlineBadge.Background = new SolidColorBrush(Color.FromArgb(235, 60, 36, 5)); // Dark amber
                card.DeadlineBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(234, 179, 8)); // Bright amber
                card.DeadlineText.Foreground = new SolidColorBrush(Color.FromRgb(254, 240, 138));
            }
            else
            {
                int days = (int)diff.TotalDays;
                card.DeadlineText.Text = $"⏱️ {labelPrefix}{dl:ddd, d MMM HH:mm} • {days}d {diff.Hours}h left";
                card.DeadlineBadge.Background = new SolidColorBrush(Color.FromArgb(235, 12, 38, 56)); // Dark cyan
                card.DeadlineBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199)); // Bright sky blue
                card.DeadlineText.Foreground = new SolidColorBrush(Color.FromRgb(186, 230, 253));
            }

            card.DeadlineBadge.ToolTip = $"Target: {dl:dddd, dd MMMM yyyy HH:mm}\nSystem Time: {now:HH:mm}\nClick to edit or adjust realtime deadline";
        }

        private static string FormatDiffShort(TimeSpan ts)
        {
            if (ts.TotalMinutes < 60) return $"{(int)Math.Max(1, ts.TotalMinutes)}m";
            if (ts.TotalHours < 24) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{(int)ts.TotalDays}d {ts.Hours}h";
        }

        private static double MeasureTextWidth(TextBox tb)
        {
            try
            {
                string txt = string.IsNullOrEmpty(tb.Text) ? " " : tb.Text;
                FormattedText ft = new FormattedText(
                    txt,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                    tb.FontSize > 0 ? tb.FontSize : 15.0,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(tb).PixelsPerDip);
                return ft.WidthIncludingTrailingWhitespace;
            }
            catch
            {
                return Math.Max(30, tb.ActualWidth > 0 ? tb.ActualWidth * 0.8 : 80);
            }
        }

        private void RenderChecklistItems(CardItem item)
        {
            if (item.ChecklistPanel == null) return;
            item.ChecklistPanel.Children.Clear();

            if (item.ChecklistItems == null || item.ChecklistItems.Count == 0)
            {
                item.ChecklistItems = new List<NoteChecklistItem>
                {
                    new NoteChecklistItem { Text = "Storyboard Rough Sketches", IsChecked = false },
                    new NoteChecklistItem { Text = "Keyframe Animation & Timing", IsChecked = false },
                    new NoteChecklistItem { Text = "Color Grading & VFX Delivery", IsChecked = false }
                };
            }

            Brush noteFg = Brushes.White;
            try { noteFg = (Brush)new BrushConverter().ConvertFromString(item.NoteTextColor)!; } catch { }

            for (int i = 0; i < item.ChecklistItems.Count; i++)
            {
                var cItem = item.ChecklistItems[i];

                Grid row = new Grid
                {
                    Margin = new Thickness(0, 3, 0, 3),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

                // 1. Custom Checkbox button
                Border chk = new Border
                {
                    Width = 17,
                    Height = 17,
                    CornerRadius = new CornerRadius(4),
                    BorderThickness = new Thickness(1.5),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 0, 0),
                    ToolTip = "Click to complete task with animated strike-through"
                };

                System.Windows.Shapes.Path chkMark = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 3.5,8.5 L 6.8,11.8 L 13.5,4.5"),
                    Stroke = Brushes.White,
                    StrokeThickness = 2.0,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeLineJoin = PenLineJoin.Round,
                    SnapsToDevicePixels = true,
                    Opacity = cItem.IsChecked ? 1.0 : 0.0
                };
                chk.Child = chkMark;

                void UpdateChkVisual(bool isChecked)
                {
                    if (isChecked)
                    {
                        chk.Background = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                        chk.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                        chkMark.Opacity = 1.0;
                    }
                    else
                    {
                        chk.Background = new SolidColorBrush(Color.FromArgb(160, 24, 30, 42));
                        chk.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 148, 163, 184));
                        chkMark.Opacity = 0.0;
                    }
                }
                UpdateChkVisual(cItem.IsChecked);

                // 2. Text Host Grid
                Grid textHost = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBox tbTask = new TextBox
                {
                    Text = cItem.Text,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 2, 4, 2),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily(string.IsNullOrWhiteSpace(item.NoteFontFamily) ? "Segoe UI" : item.NoteFontFamily),
                    FontSize = item.NoteFontSize > 0 ? item.NoteFontSize : 15.0,
                    TextAlignment = item.NoteAlignment,
                    Foreground = noteFg,
                    Opacity = cItem.IsChecked ? 0.45 : 1.0,
                    CaretBrush = Brushes.White,
                    Cursor = Cursors.IBeam
                };

                if (item.NoteHasShadow)
                {
                    tbTask.Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 8,
                        ShadowDepth = 2,
                        Direction = 315,
                        Opacity = 0.95
                    };
                }

                Border strikeLine = new Border
                {
                    Height = 2.2,
                    CornerRadius = new CornerRadius(1.1),
                    Background = new SolidColorBrush(Color.FromRgb(244, 63, 94)), // Vibrant Rose strike line
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    Width = 0,
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 3,
                        ShadowDepth = 1,
                        Color = Colors.Black,
                        Opacity = 0.5
                    }
                };

                textHost.Children.Add(tbTask);
                textHost.Children.Add(strikeLine);

                // Initial width calculation when loading checked items
                tbTask.Loaded += (s, e) =>
                {
                    if (cItem.IsChecked)
                    {
                        double tw = MeasureTextWidth(tbTask);
                        strikeLine.Width = Math.Max(20, Math.Min(textHost.ActualWidth > 0 ? textHost.ActualWidth - 6 : 280, tw + 6));
                    }
                };

                // Checkbox Click: Smooth Strike-Through Animation!
                chk.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    cItem.IsChecked = !cItem.IsChecked;
                    UpdateChkVisual(cItem.IsChecked);

                    if (cItem.IsChecked)
                    {
                        double tw = MeasureTextWidth(tbTask);
                        double targetW = Math.Max(20, Math.Min(textHost.ActualWidth > 0 ? textHost.ActualWidth - 6 : 280, tw + 6));

                        DoubleAnimation animW = new DoubleAnimation
                        {
                            From = strikeLine.ActualWidth,
                            To = targetW,
                            Duration = TimeSpan.FromMilliseconds(260),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        strikeLine.BeginAnimation(FrameworkElement.WidthProperty, animW);

                        DoubleAnimation animOp = new DoubleAnimation
                        {
                            From = tbTask.Opacity,
                            To = 0.45,
                            Duration = TimeSpan.FromMilliseconds(200)
                        };
                        tbTask.BeginAnimation(UIElement.OpacityProperty, animOp);
                    }
                    else
                    {
                        DoubleAnimation animW = new DoubleAnimation
                        {
                            From = strikeLine.ActualWidth,
                            To = 0,
                            Duration = TimeSpan.FromMilliseconds(180),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                        };
                        strikeLine.BeginAnimation(FrameworkElement.WidthProperty, animW);

                        DoubleAnimation animOp = new DoubleAnimation
                        {
                            From = tbTask.Opacity,
                            To = 1.0,
                            Duration = TimeSpan.FromMilliseconds(200)
                        };
                        tbTask.BeginAnimation(UIElement.OpacityProperty, animOp);
                    }

                    RecordUndo("Toggle Checklist Item");
                    ScheduleAutoSave();
                };

                // In-place text edit
                tbTask.TextChanged += (s, e) =>
                {
                    cItem.Text = tbTask.Text;
                    if (cItem.IsChecked)
                    {
                        double tw = MeasureTextWidth(tbTask);
                        strikeLine.Width = Math.Max(20, Math.Min(textHost.ActualWidth > 0 ? textHost.ActualWidth - 6 : 280, tw + 6));
                    }
                    ScheduleAutoSave();
                };

                // Enter key adds new item, Backspace on empty deletes item
                tbTask.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        e.Handled = true;
                        int nextIdx = item.ChecklistItems.IndexOf(cItem) + 1;
                        var newItem = new NoteChecklistItem { Text = "", IsChecked = false };
                        item.ChecklistItems.Insert(nextIdx, newItem);
                        RenderChecklistItems(item);
                        ScheduleAutoSave();

                        // Focus new item
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                        {
                            if (item.ChecklistPanel != null && nextIdx < item.ChecklistPanel.Children.Count - 1)
                            {
                                if (item.ChecklistPanel.Children[nextIdx] is Grid nextRow &&
                                    nextRow.Children.Count > 1 &&
                                    nextRow.Children[1] is Grid th &&
                                    th.Children.Count > 0 &&
                                    th.Children[0] is TextBox tbNext)
                                {
                                    tbNext.Focus();
                                }
                            }
                        }));
                    }
                    else if (e.Key == Key.Back && string.IsNullOrEmpty(tbTask.Text) && item.ChecklistItems.Count > 1)
                    {
                        e.Handled = true;
                        int curIdx = item.ChecklistItems.IndexOf(cItem);
                        item.ChecklistItems.Remove(cItem);
                        RenderChecklistItems(item);
                        ScheduleAutoSave();

                        // Focus previous item
                        int prevIdx = Math.Max(0, curIdx - 1);
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                        {
                            if (item.ChecklistPanel != null && prevIdx < item.ChecklistPanel.Children.Count - 1)
                            {
                                if (item.ChecklistPanel.Children[prevIdx] is Grid prevRow &&
                                    prevRow.Children.Count > 1 &&
                                    prevRow.Children[1] is Grid th &&
                                    th.Children.Count > 0 &&
                                    th.Children[0] is TextBox tbPrev)
                                {
                                    tbPrev.Focus();
                                    tbPrev.CaretIndex = tbPrev.Text.Length;
                                }
                            }
                        }));
                    }
                };

                // 3. Delete Item Button
                Border btnDel = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(3),
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand,
                    Opacity = 0.2,
                    ToolTip = "Delete Item",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "✕",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                row.MouseEnter += (s, e) => btnDel.Opacity = 0.85;
                row.MouseLeave += (s, e) => btnDel.Opacity = 0.2;
                btnDel.MouseEnter += (s, e) =>
                {
                    btnDel.Background = new SolidColorBrush(Color.FromArgb(60, 239, 68, 68));
                    if (btnDel.Child is TextBlock t) t.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                };
                btnDel.MouseLeave += (s, e) =>
                {
                    btnDel.Background = Brushes.Transparent;
                    if (btnDel.Child is TextBlock t) t.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                };
                btnDel.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    item.ChecklistItems.Remove(cItem);
                    RenderChecklistItems(item);
                    ScheduleAutoSave();
                };

                Grid.SetColumn(chk, 0);
                Grid.SetColumn(textHost, 1);
                Grid.SetColumn(btnDel, 2);
                row.Children.Add(chk);
                row.Children.Add(textHost);
                row.Children.Add(btnDel);

                item.ChecklistPanel.Children.Add(row);
            }

            // Bottom "+ Add item" button
            Border btnAddItem = new Border
            {
                Margin = new Thickness(2, 6, 2, 4),
                Padding = new Thickness(8, 4, 8, 4),
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            btnAddItem.Child = new TextBlock
            {
                Text = "+ Add task item...",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            btnAddItem.MouseEnter += (s, e) =>
            {
                btnAddItem.Background = new SolidColorBrush(Color.FromArgb(35, 56, 189, 248));
                btnAddItem.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                if (btnAddItem.Child is TextBlock t) t.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            };
            btnAddItem.MouseLeave += (s, e) =>
            {
                btnAddItem.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
                btnAddItem.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                if (btnAddItem.Child is TextBlock t) t.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            };
            btnAddItem.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                var newItem = new NoteChecklistItem { Text = "", IsChecked = false };
                item.ChecklistItems.Add(newItem);
                RenderChecklistItems(item);
                ScheduleAutoSave();

                // Focus newly added item
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    int lastIdx = item.ChecklistItems.Count - 1;
                    if (item.ChecklistPanel != null && lastIdx < item.ChecklistPanel.Children.Count - 1)
                    {
                        if (item.ChecklistPanel.Children[lastIdx] is Grid lastRow &&
                            lastRow.Children.Count > 1 &&
                            lastRow.Children[1] is Grid th &&
                            th.Children.Count > 0 &&
                            th.Children[0] is TextBox tb)
                        {
                            tb.Focus();
                        }
                    }
                }));
            };

            item.ChecklistPanel.Children.Add(btnAddItem);
        }

        private void ToggleNoteChecklistMode(CardItem item)
        {
            item.IsChecklist = !item.IsChecklist;

            if (item.IsChecklist)
            {
                // Convert plain text lines into checklist items
                if (!string.IsNullOrWhiteSpace(item.NoteText))
                {
                    var lines = item.NoteText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    item.ChecklistItems = lines.Select(line =>
                    {
                        string t = line.Trim();
                        bool isDone = false;
                        if (t.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase) || t.StartsWith("[✓] ", StringComparison.OrdinalIgnoreCase))
                        {
                            isDone = true;
                            t = t.Substring(4).Trim();
                        }
                        else if (t.StartsWith("[ ] "))
                        {
                            t = t.Substring(4).Trim();
                        }
                        else if (t.StartsWith("- ") || t.StartsWith("• ") || t.StartsWith("* "))
                        {
                            t = t.Substring(2).Trim();
                        }
                        return new NoteChecklistItem { Text = t, IsChecked = isDone };
                    }).ToList();
                }
                else
                {
                    item.ChecklistItems = new List<NoteChecklistItem>
                    {
                        new NoteChecklistItem { Text = "Storyboard Rough Sketches", IsChecked = false },
                        new NoteChecklistItem { Text = "Keyframe Animation & Timing", IsChecked = false },
                        new NoteChecklistItem { Text = "Color Grading & Final Review", IsChecked = false }
                    };
                }

                if (item.NoteEditor != null) item.NoteEditor.Visibility = Visibility.Collapsed;
                if (item.ChecklistScrollViewer != null) item.ChecklistScrollViewer.Visibility = Visibility.Visible;
                RenderChecklistItems(item);
                ShowToast("Switched to Checklist Mode", ToastType.Info);
            }
            else
            {
                // Convert checklist items back to multiline text
                if (item.ChecklistItems != null && item.ChecklistItems.Count > 0)
                {
                    item.NoteText = string.Join("\n", item.ChecklistItems.Select(ci => (ci.IsChecked ? "[x] " : "[ ] ") + ci.Text));
                }
                if (item.NoteEditor != null)
                {
                    item.NoteEditor.Text = item.NoteText;
                    item.NoteEditor.Visibility = Visibility.Visible;
                }
                if (item.ChecklistScrollViewer != null) item.ChecklistScrollViewer.Visibility = Visibility.Collapsed;
                ShowToast("Switched to Text Mode", ToastType.Info);
            }

            if (item.BtnNoteChecklist != null)
            {
                item.BtnNoteChecklist.Background = item.IsChecklist
                    ? new SolidColorBrush(Color.FromArgb(55, 16, 185, 129))
                    : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                item.BtnNoteChecklist.BorderBrush = item.IsChecklist
                    ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                    : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            }

            RecordUndo("Toggle Checklist Mode");
            ScheduleAutoSave();
        }

        private void ToggleNoteDoodleMode(CardItem item)
        {
            item.IsNoteDoodleActive = !item.IsNoteDoodleActive;

            if (item.NoteDoodleCanvas != null)
            {
                item.NoteDoodleCanvas.IsHitTestVisible = item.IsNoteDoodleActive;
                if (!item.IsNoteDoodleActive)
                {
                    item.NoteDoodleCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    item.NoteDoodleCanvas.Cursor = Cursors.Pen;
                }
            }
            if (item.NoteDoodleIndicator != null)
            {
                item.NoteDoodleIndicator.Visibility = item.IsNoteDoodleActive ? Visibility.Visible : Visibility.Collapsed;
            }
            if (item.BtnNoteDoodle != null)
            {
                item.BtnNoteDoodle.Background = item.IsNoteDoodleActive
                    ? new SolidColorBrush(Color.FromArgb(55, 244, 63, 94))
                    : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                item.BtnNoteDoodle.BorderBrush = item.IsNoteDoodleActive
                    ? new SolidColorBrush(Color.FromRgb(244, 63, 94))
                    : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));

                if (item.BtnNoteDoodle.Child is StackPanel dSp)
                {
                    if (dSp.Children.Count > 0 && dSp.Children[0] is TextBlock tIcon)
                        tIcon.Foreground = item.IsNoteDoodleActive ? new SolidColorBrush(Color.FromRgb(244, 63, 94)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
                    if (dSp.Children.Count > 1 && dSp.Children[1] is TextBlock tTxt)
                    {
                        tTxt.Foreground = item.IsNoteDoodleActive ? new SolidColorBrush(Color.FromRgb(244, 63, 94)) : new SolidColorBrush(Color.FromRgb(226, 232, 240));
                        tTxt.FontWeight = item.IsNoteDoodleActive ? FontWeights.SemiBold : FontWeights.Normal;
                    }
                }
            }
            if (item.BtnNoteCornerPen != null)
            {
                item.BtnNoteCornerPen.Visibility = item.IsNoteDoodleActive ? Visibility.Collapsed : Visibility.Visible;
                if (item.IsNoteDoodleActive)
                {
                    item.BtnNoteCornerPen.Background = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                    item.BtnNoteCornerPen.BorderBrush = Brushes.White;
                    if (item.NoteCornerPenIcon != null)
                        item.NoteCornerPenIcon.Stroke = Brushes.White;
                    item.BtnNoteCornerPen.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Color = Color.FromRgb(244, 63, 94), Opacity = 0.85 };
                }
                else
                {
                    item.BtnNoteCornerPen.Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
                    item.BtnNoteCornerPen.BorderBrush = new SolidColorBrush(Color.FromArgb(160, 244, 63, 94));
                    if (item.NoteCornerPenIcon != null)
                        item.NoteCornerPenIcon.Stroke = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                    item.BtnNoteCornerPen.Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.55, Color = Colors.Black };
                }
            }

            ShowToast(item.IsNoteDoodleActive
                ? "Doodle mode ON: Draw & strike freely on this note!"
                : "Doodle mode OFF: Normal editing resumed.", ToastType.Info);
        }

        private void ShowDeadlinePickerPopup(FrameworkElement anchor, CardItem item)
        {
            _isCardSubMenuOpen = true;

            Popup popup = new Popup
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                VerticalOffset = 6
            };

            popup.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
            };

            Border container = new Border
            {
                Width = 280,
                Background = new SolidColorBrush(Color.FromRgb(22, 25, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                SnapsToDevicePixels = true,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Opacity = 0.75
                }
            };

            StackPanel sp = new StackPanel();

            // Header with Close Button
            Grid hdrGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            hdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel hdrSp = new StackPanel();
            hdrSp.Children.Add(new TextBlock
            {
                Text = "⏱ Realtime Note Deadline",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249))
            });
            hdrSp.Children.Add(new TextBlock
            {
                Text = "Synchronized live with computer clock",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 1, 0, 0)
            });
            Grid.SetColumn(hdrSp, 0);
            hdrGrid.Children.Add(hdrSp);

            Border btnClosePopup = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top,
                ToolTip = "Close (Esc)",
                Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btnClosePopup.MouseEnter += (s, e) => btnClosePopup.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            btnClosePopup.MouseLeave += (s, e) => btnClosePopup.Background = Brushes.Transparent;
            btnClosePopup.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                popup.IsOpen = false;
            };
            Grid.SetColumn(btnClosePopup, 1);
            hdrGrid.Children.Add(btnClosePopup);

            sp.Children.Add(hdrGrid);

            // Presets Header
            sp.Children.Add(new TextBlock
            {
                Text = "QUICK PRESETS (BY SYSTEM TIME)",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Presets Grid (4 rows x 2 cols)
            Grid pGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int r = 0; r < 4; r++)
                pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            (string title, Func<DateTime> calc, int row, int col)[] presets = new[]
            {
                ("+1 Hour", (Func<DateTime>)(() => DateTime.Now.AddHours(1)), 0, 0),
                ("+3 Hours", (Func<DateTime>)(() => DateTime.Now.AddHours(3)), 0, 2),
                ("Today 18:00", (Func<DateTime>)(() => DateTime.Today.AddHours(18)), 1, 0),
                ("Tonight 23:59", (Func<DateTime>)(() => DateTime.Today.AddHours(23).AddMinutes(59)), 1, 2),
                ("Tomorrow 09:00", (Func<DateTime>)(() => DateTime.Today.AddDays(1).AddHours(9)), 2, 0),
                ("Tomorrow 18:00", (Func<DateTime>)(() => DateTime.Today.AddDays(1).AddHours(18)), 2, 2),
                ("In 3 Days", (Func<DateTime>)(() => DateTime.Today.AddDays(3).AddHours(18)), 3, 0),
                ("1 Week", (Func<DateTime>)(() => DateTime.Today.AddDays(7).AddHours(18)), 3, 2)
            };

            foreach (var p in presets)
            {
                Border btnP = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 2, 0, 2),
                    Cursor = Cursors.Hand
                };
                TextBlock tbP = new TextBlock
                {
                    Text = p.title,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                btnP.Child = tbP;

                btnP.MouseEnter += (s, e) =>
                {
                    btnP.Background = new SolidColorBrush(Color.FromArgb(50, 56, 189, 248));
                    btnP.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    tbP.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                };
                btnP.MouseLeave += (s, e) =>
                {
                    btnP.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                    btnP.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                    tbP.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                };

                btnP.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    DateTime target = p.calc();
                    item.HasDeadline = true;
                    item.DeadlineDateTime = target;
                    UpdateNoteDeadlineBadgeUI(item);
                    RecordUndo("Set Deadline Preset");
                    ScheduleAutoSave();
                    popup.IsOpen = false;
                    ShowToast($"Deadline set: {target:ddd, d MMM HH:mm}", ToastType.Success);
                };

                Grid.SetRow(btnP, p.row);
                Grid.SetColumn(btnP, p.col);
                pGrid.Children.Add(btnP);
            }
            sp.Children.Add(pGrid);

            // Custom Date & Time Header
            sp.Children.Add(new TextBlock
            {
                Text = "CUSTOM DEADLINE",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            // Date Picker Row
            DateTime initialDate = item.DeadlineDateTime?.Date ?? DateTime.Today.AddDays(1);
            DatePicker dp = new DatePicker
            {
                SelectedDate = initialDate,
                SelectedDateFormat = DatePickerFormat.Short,
                Margin = new Thickness(0, 0, 0, 8),
                Height = 26
            };
            sp.Children.Add(dp);

            // Time Row (Hours & Minutes with direct typing support)
            Grid timeGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            timeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            ComboBox cbHour = new ComboBox { Height = 26, IsEditable = true };
            for (int h = 0; h < 24; h++) cbHour.Items.Add($"{h:D2}");
            cbHour.SelectedItem = $"{(item.DeadlineDateTime?.Hour ?? 18):D2}";

            ComboBox cbMin = new ComboBox { Height = 26, IsEditable = true };
            for (int m = 0; m < 60; m += 5) cbMin.Items.Add($"{m:D2}");
            cbMin.SelectedItem = $"{((item.DeadlineDateTime?.Minute ?? 0) / 5 * 5):D2}";

            Grid.SetColumn(cbHour, 0);
            Grid.SetColumn(cbMin, 2);
            timeGrid.Children.Add(cbHour);
            timeGrid.Children.Add(cbMin);
            sp.Children.Add(timeGrid);

            // Optional Label Row
            TextBox tbLabel = new TextBox
            {
                Text = string.IsNullOrEmpty(item.DeadlineLabel) ? "Deadline" : item.DeadlineLabel,
                Height = 24,
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(tbLabel);

            void ApplyCustomDeadline()
            {
                DateTime d;
                if (!string.IsNullOrWhiteSpace(dp.Text) && DateTime.TryParse(dp.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var p1))
                    d = p1;
                else if (!string.IsNullOrWhiteSpace(dp.Text) && DateTime.TryParse(dp.Text, out var p2))
                    d = p2;
                else
                    d = dp.SelectedDate ?? DateTime.Today.AddDays(1);

                int hr = 18;
                if (int.TryParse(cbHour.Text, out int hVal) || int.TryParse(cbHour.SelectedItem as string, out hVal))
                    hr = Math.Clamp(hVal, 0, 23);

                int mn = 0;
                if (int.TryParse(cbMin.Text, out int mVal) || int.TryParse(cbMin.SelectedItem as string, out mVal))
                    mn = Math.Clamp(mVal, 0, 59);

                DateTime customTarget = new DateTime(d.Year, d.Month, d.Day, hr, mn, 0);

                item.HasDeadline = true;
                item.DeadlineDateTime = customTarget;
                item.DeadlineLabel = string.IsNullOrWhiteSpace(tbLabel.Text) ? "Deadline" : tbLabel.Text.Trim();
                UpdateNoteDeadlineBadgeUI(item);
                RecordUndo("Set Custom Deadline");
                ScheduleAutoSave();
                popup.IsOpen = false;
                ShowToast($"Deadline set: {customTarget.ToString("ddd, d MMM HH:mm", CultureInfo.InvariantCulture)}", ToastType.Success);
            }

            KeyEventHandler onEnterApply = (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    ApplyCustomDeadline();
                }
            };
            dp.KeyDown += onEnterApply;
            cbHour.KeyDown += onEnterApply;
            cbMin.KeyDown += onEnterApply;
            tbLabel.KeyDown += onEnterApply;

            // Action Buttons
            Grid actGrid = new Grid();
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (item.HasDeadline)
            {
                actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            Border btnApply = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10, 6, 10, 6),
                Cursor = Cursors.Hand
            };
            btnApply.Child = new TextBlock
            {
                Text = "Apply Deadline",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnApply.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                ApplyCustomDeadline();
            };
            Grid.SetColumn(btnApply, 0);
            actGrid.Children.Add(btnApply);

            if (item.HasDeadline)
            {
                Border btnRemove = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 6, 8, 6),
                    Cursor = Cursors.Hand
                };
                btnRemove.Child = new TextBlock
                {
                    Text = "✕ Clear",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                btnRemove.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    item.HasDeadline = false;
                    item.DeadlineDateTime = null;
                    if (item.DeadlineBadge != null) item.DeadlineBadge.Visibility = Visibility.Collapsed;
                    RecordUndo("Clear Deadline");
                    ScheduleAutoSave();
                    popup.IsOpen = false;
                    ShowToast("Deadline removed", ToastType.Info);
                };
                Grid.SetColumn(btnRemove, 2);
                actGrid.Children.Add(btnRemove);
            }

            sp.Children.Add(actGrid);
            container.Child = sp;
            popup.Child = container;

            void ActivatePopupHwnd()
            {
                if (PresentationSource.FromVisual(container) is HwndSource source && source.Handle != IntPtr.Zero)
                {
                    SetActiveWindow(source.Handle);
                    SetFocus(source.Handle);
                }
            }

            popup.Opened += (s, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    ActivatePopupHwnd();
                    dp.Focus();
                }));
            };

            container.PreviewMouseDown += (s, e) =>
            {
                ActivatePopupHwnd();
            };

            MouseButtonEventHandler? outsideClickHandler = null;
            outsideClickHandler = (s, e) =>
            {
                if (!popup.IsOpen) return;
                // If DatePicker's calendar dropdown is currently open, don't close popup
                if (dp.IsDropDownOpen) return;

                popup.IsOpen = false;
            };
            this.PreviewMouseDown += outsideClickHandler;

            KeyEventHandler? escKeyHandler = null;
            escKeyHandler = (s, e) =>
            {
                if (e.Key == Key.Escape && popup.IsOpen)
                {
                    e.Handled = true;
                    popup.IsOpen = false;
                }
            };
            this.PreviewKeyDown += escKeyHandler;

            popup.Closed += (s, e) =>
            {
                this.PreviewMouseDown -= outsideClickHandler;
                this.PreviewKeyDown -= escKeyHandler;
                _isCardSubMenuOpen = false;
            };

            popup.IsOpen = true;
        }

        #endregion

        private CardItem AddDrawCard(
            Point? worldPosition = null,
            double? customWidth = null,
            double? customHeight = null,
            string initialInkBase64 = "",
            string penColor = "#38BDF8",
            double penSize = 3.0,
            bool autoSelect = true)
        {
            EmptyStateOverlay.Visibility = Visibility.Collapsed;

            double w = customWidth ?? 380;
            double h = customHeight ?? 280;

            // InkCanvas element
            InkCanvas inkCanvas = new InkCanvas
            {
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                Cursor = Cursors.Pen,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Setup high-quality drawing attributes
            Color drawingColor = Colors.DeepSkyBlue;
            try
            {
                drawingColor = (Color)ColorConverter.ConvertFromString(penColor);
            }
            catch { }

            inkCanvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = drawingColor,
                Width = penSize,
                Height = penSize,
                FitToCurve = true,
                IgnorePressure = false,
                StylusTip = StylusTip.Ellipse
            };

            // Restore strokes if provided
            if (!string.IsNullOrEmpty(initialInkBase64))
            {
                try
                {
                    byte[] strokeBytes = Convert.FromBase64String(initialInkBase64);
                    using MemoryStream ms = new MemoryStream(strokeBytes);
                    inkCanvas.Strokes = new StrokeCollection(ms);
                }
                catch { }
            }

            // Dummy background placeholder for uniform CardItem contract
            BitmapSource dummyBmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 0 }, 4);
            Image dummyImg = new Image
            {
                Source = dummyBmp,
                Visibility = Visibility.Collapsed
            };

            // Wrap InkCanvas inside Viewbox so all strokes scale smoothly with corner handles
            Viewbox vb = new Viewbox
            {
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            vb.Child = inkCanvas;

            Border contentBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), // Nearly 100% transparent hit test surface
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)),
                BorderThickness = new Thickness(0), // 100% borderless when idle!
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = vb
            };

            Grid container = new Grid
            {
                Width = w,
                Height = h,
                Cursor = Cursors.SizeAll
            };
            container.Children.Add(dummyImg);
            container.Children.Add(contentBorder);

            // 4 Corner Handles
            Border handleTL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE);
            Border handleTR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW);
            Border handleBL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW);
            Border handleBR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE);

            container.Children.Add(handleTL);
            container.Children.Add(handleTR);
            container.Children.Add(handleBL);
            container.Children.Add(handleBR);
            _highestZ++;
            Panel.SetZIndex(container, _highestZ + 1000);

            CardItem item = new CardItem
            {
                Container = container,
                ContentBorder = contentBorder,
                ImageControl = dummyImg,
                Bitmap = dummyBmp,
                OriginalBitmap = dummyBmp,
                BaseWidth = w,
                BaseHeight = h,
                AspectRatio = w / Math.Max(1.0, h),
                HandleTL = handleTL,
                HandleTR = handleTR,
                HandleBL = handleBL,
                HandleBR = handleBR,
                IsDrawCard = true,
                DrawCanvas = inkCanvas,
                DrawInkBase64 = initialInkBase64,
                DrawPenColor = penColor,
                DrawPenSize = penSize
            };

            // Event to update serialized ink data
            void UpdateInkData()
            {
                try
                {
                    using MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    item.DrawInkBase64 = Convert.ToBase64String(ms.ToArray());
                    ScheduleAutoSave();
                }
                catch { }
            }

            inkCanvas.StrokeCollected += (s, e) => UpdateInkData();
            inkCanvas.StrokeErased += (s, e) => UpdateInkData();

            // Attach Hover Quick-Action Toolbar
            Border hoverToolbar = CreateCardHoverToolbar(item);
            item.HoverToolbar = hoverToolbar;
            Canvas toolbarHost = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 0,
                Height = 0,
                ClipToBounds = false
            };
            Panel.SetZIndex(toolbarHost, 9999);
            Canvas.SetTop(hoverToolbar, -38.0);
            toolbarHost.Children.Add(hoverToolbar);
            container.Children.Add(toolbarHost);

            hoverToolbar.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0)
                {
                    Canvas.SetLeft(hoverToolbar, -e.NewSize.Width / 2.0);
                    Canvas.SetTop(hoverToolbar, -38.0);
                }
            };

            // Context Menu
            container.ContextMenu = CreateCardContextMenu(item);
            container.MouseRightButtonDown += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    SelectCard(item, addToSelection: false);
                }
                e.Handled = true;
            };
            container.MouseRightButtonUp += (s, e) =>
            {
                container.ContextMenu = CreateCardContextMenu(item);
                if (container.ContextMenu != null)
                {
                    container.ContextMenu.PlacementTarget = container;
                    container.ContextMenu.IsOpen = true;
                }
                e.Handled = true;
            };

            // Drag card logic
            void TriggerDrawCardDrag(MouseEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.Space)) return;

                if (e is MouseButtonEventArgs mbe && mbe.ClickCount == 2)
                {
                    SelectCard(item, addToSelection: false);
                    ZoomToCard(item);
                    e.Handled = true;
                    return;
                }

                RecordUndo("Move Sketch Card");

                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (isShift)
                {
                    if (item.IsSelected)
                        DeselectCard(item);
                    else
                        SelectCard(item, addToSelection: true);
                }
                else
                {
                    if (!item.IsSelected)
                    {
                        SelectCard(item, addToSelection: false);
                    }
                }

                _isDraggingCards = true;
                _cardDragStartMousePoint = e.GetPosition(CanvasContainer);
                _cardsInitialPositions.Clear();
                foreach (CardItem sel in _selectedCards)
                {
                    _cardsInitialPositions[sel] = new Point(sel.X, sel.Y);
                }
                SetWebViewHitTesting(false);

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            }

            contentBorder.MouseLeftButtonDown += (s, e) => TriggerDrawCardDrag(e);
            inkCanvas.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (Keyboard.IsKeyDown(Key.Space)) return;
                TriggerDrawCardDrag(e);
            };

            // Resize handle events
            AttachResizeHandleEvents(item, handleTL, ResizeCorner.TopLeft);
            AttachResizeHandleEvents(item, handleTR, ResizeCorner.TopRight);
            AttachResizeHandleEvents(item, handleBL, ResizeCorner.BottomLeft);
            AttachResizeHandleEvents(item, handleBR, ResizeCorner.BottomRight);

            // Determine World placement
            Point pos;
            if (worldPosition.HasValue)
            {
                pos = worldPosition.Value;
            }
            else
            {
                Matrix matrix = CanvasMatrixTransform.Matrix;
                matrix.Invert();
                Point centerScreen = new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
                pos = matrix.Transform(centerScreen);
                pos.X += (_cards.Count % 5) * 35 - (w / 2);
                pos.Y += (_cards.Count % 5) * 35 - (h / 2);
            }

            item.X = pos.X;
            item.Y = pos.Y;

            if (!_isRestoringSession && !_isApplyingSnapshot)
            {
                RecordUndo("Add Sketch Card");
            }

            _cards.Add(item);
            WorldCanvas.Children.Add(container);

            CheckCardGroupAffiliation(item);
            UpdateStatusCounts();

            if (autoSelect)
            {
                SelectCard(item, addToSelection: false);
            }

            if (!_isRestoringSession)
            {
                ScheduleAutoSave();
            }

            return item;
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private Border CreateResizeHandle(HorizontalAlignment hAlign, VerticalAlignment vAlign, Cursor cursor)
        {
            bool isEdge = (hAlign == HorizontalAlignment.Center || vAlign == VerticalAlignment.Center);
            double w = (vAlign == VerticalAlignment.Center) ? 8 : (isEdge ? 24 : 18);
            double h = (hAlign == HorizontalAlignment.Center) ? 8 : (isEdge ? 24 : 18);
            double radius = isEdge ? 4 : 9;

            Border handle = new Border
            {
                Width = w,
                Height = h,
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(radius),
                HorizontalAlignment = hAlign,
                VerticalAlignment = vAlign,
                Margin = new Thickness(
                    hAlign == HorizontalAlignment.Left ? -(w / 2) : 0,
                    vAlign == VerticalAlignment.Top ? -(h / 2) : 0,
                    hAlign == HorizontalAlignment.Right ? -(w / 2) : 0,
                    vAlign == VerticalAlignment.Bottom ? -(h / 2) : 0),
                Cursor = cursor,
                Visibility = Visibility.Collapsed
            };
            Panel.SetZIndex(handle, 10000);
            return handle;
        }

        private void AttachResizeHandleEvents(CardItem card, Border handle, ResizeCorner corner)
        {
            void StartResize(MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2 && card.IsNote)
                {
                    double targetRatio = card.IsChecklist ? (320.0 / 240.0) : (280.0 / 180.0);
                    card.Height = Math.Round(card.Width / targetRatio);
                    card.AspectRatio = targetRatio;
                    ScheduleAutoSave();
                    ShowToast("✨ Reset note aspect ratio (Fixed gepeng)", ToastType.Success);
                    e.Handled = true;
                    return;
                }

                RecordUndo("Resize Card");

                _isResizingCard = true;
                _resizingCard = card;
                _activeCorner = corner;
                _resizeStartMousePoint = e.GetPosition(CanvasContainer);
                _resizeInitialBounds = new Rect(card.X, card.Y, card.Width, card.Height);
                SetWebViewHitTesting(false);

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            }

            handle.PreviewMouseLeftButtonDown += (s, e) => StartResize(e);
            handle.MouseLeftButtonDown += (s, e) => StartResize(e);
        }

        private void ApplyCardResize(CardItem card, ResizeCorner corner, Rect initial, double deltaX, double deltaY)
        {
            if (card.IsYouTube)
            {
                card.AspectRatio = 16.0 / 9.0;
            }

            double newW = initial.Width;
            double newH = initial.Height;
            double newX = initial.X;
            double newY = initial.Y;

            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (card.IsPaletteCard || card.IsNote || isShift)
            {
                // Free-form 2D resize for notes, palettes, or when Shift is held (Allows making notes thin/wide freely!)
                switch (corner)
                {
                    case ResizeCorner.BottomRight:
                        newW = Math.Max(80, initial.Width + deltaX);
                        newH = Math.Max(50, initial.Height + deltaY);
                        break;

                    case ResizeCorner.BottomLeft:
                        newW = Math.Max(80, initial.Width - deltaX);
                        newH = Math.Max(50, initial.Height + deltaY);
                        newX = initial.Right - newW;
                        break;

                    case ResizeCorner.TopRight:
                        newW = Math.Max(80, initial.Width + deltaX);
                        newH = Math.Max(50, initial.Height - deltaY);
                        newY = initial.Bottom - newH;
                        break;

                    case ResizeCorner.TopLeft:
                        newW = Math.Max(80, initial.Width - deltaX);
                        newH = Math.Max(50, initial.Height - deltaY);
                        newX = initial.Right - newW;
                        newY = initial.Bottom - newH;
                        break;

                    case ResizeCorner.Right:
                        newW = Math.Max(80, initial.Width + deltaX);
                        break;

                    case ResizeCorner.Left:
                        newW = Math.Max(80, initial.Width - deltaX);
                        newX = initial.Right - newW;
                        break;

                    case ResizeCorner.Bottom:
                        newH = Math.Max(50, initial.Height + deltaY);
                        break;

                    case ResizeCorner.Top:
                        newH = Math.Max(50, initial.Height - deltaY);
                        newY = initial.Bottom - newH;
                        break;
                }
                card.AspectRatio = newW / Math.Max(1.0, newH);
            }
            else
            {
                // Proportional aspect-ratio locked resize for Images and Sketch Cards (Prevents Gepeng!)
                switch (corner)
                {
                    case ResizeCorner.BottomRight:
                        newW = Math.Max(60, initial.Width + deltaX);
                        newH = newW / card.AspectRatio;
                        break;

                    case ResizeCorner.BottomLeft:
                        newW = Math.Max(60, initial.Width - deltaX);
                        newH = newW / card.AspectRatio;
                        newX = initial.Right - newW;
                        break;

                    case ResizeCorner.TopRight:
                        newW = Math.Max(60, initial.Width + deltaX);
                        newH = newW / card.AspectRatio;
                        newY = initial.Bottom - newH;
                        break;

                    case ResizeCorner.TopLeft:
                        newW = Math.Max(60, initial.Width - deltaX);
                        newH = newW / card.AspectRatio;
                        newX = initial.Right - newW;
                        newY = initial.Bottom - newH;
                        break;

                    case ResizeCorner.Right:
                        newW = Math.Max(60, initial.Width + deltaX);
                        newH = newW / card.AspectRatio;
                        break;

                    case ResizeCorner.Left:
                        newW = Math.Max(60, initial.Width - deltaX);
                        newH = newW / card.AspectRatio;
                        newX = initial.Right - newW;
                        break;

                    case ResizeCorner.Bottom:
                        newH = Math.Max(60, initial.Height + deltaY);
                        newW = newH * card.AspectRatio;
                        break;

                    case ResizeCorner.Top:
                        newH = Math.Max(60, initial.Height - deltaY);
                        newW = newH * card.AspectRatio;
                        newY = initial.Bottom - newH;
                        break;
                }
            }

            card.Width = newW;
            card.Height = newH;
            card.X = newX;
            card.Y = newY;

            if (card.IsCropped)
            {
                double visW_pct = Math.Max(0.05, (100.0 - card.CropLeft - card.CropRight) / 100.0);
                double visH_pct = Math.Max(0.05, (100.0 - card.CropTop - card.CropBottom) / 100.0);
                card.BaseWidth = Math.Round(newW / visW_pct);
                card.BaseHeight = Math.Round(newH / visH_pct);
                card.BaseX = Math.Round(newX - card.BaseWidth * (card.CropLeft / 100.0));
                card.BaseY = Math.Round(newY - card.BaseHeight * (card.CropTop / 100.0));
            }
            else
            {
                card.BaseWidth = newW;
                card.BaseHeight = newH;
                card.BaseX = newX;
                card.BaseY = newY;
            }

            if (!string.IsNullOrEmpty(card.GroupId))
            {
                var g = _groups.FirstOrDefault(grp => grp.Id == card.GroupId);
                if (g != null)
                {
                    FitGroupToCards(g, recordUndo: false);
                }
            }
        }

        private void SelectCard(CardItem card, bool addToSelection, bool updateCounts = true)
        {
            if (card.IsSelected && addToSelection) return;

            if (!addToSelection)
            {
                DeselectAllCards();
            }

            card.IsSelected = true;
            if (!_selectedCards.Contains(card))
            {
                _selectedCards.Add(card);
            }

            if (card.IsNote)
            {
                ApplyNoteBackground(card, card.NoteBgColor);
            }
            else if (card.IsPaletteCard)
            {
                card.ContentBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Blue border
                card.ContentBorder.BorderThickness = new Thickness(1.5);
                card.ContentBorder.CornerRadius = new CornerRadius(12);
            }
            else if (card.IsDrawCard)
            {
                card.ContentBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)); // Thin selection outline
                card.ContentBorder.BorderThickness = new Thickness(1);
                card.ContentBorder.CornerRadius = new CornerRadius(0);
                card.ContentBorder.Effect = null;
            }
            else
            {
                card.ContentBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Blue border
                card.ContentBorder.BorderThickness = new Thickness(2);
                card.ContentBorder.CornerRadius = new CornerRadius(0);
                // Pasang GPU DropShadow hanya jika seleksi sedikit (<= 5) agar GPU DirectX tidak lag
                card.ContentBorder.Effect = _selectedCards.Count <= 5 ? CardActiveShadow : null;
            }

            // Tampilkan corner & edge handles jika seleksi tidak terlalu banyak (<= 15)
            Visibility handleVis = _selectedCards.Count <= 15 ? Visibility.Visible : Visibility.Collapsed;
            card.HandleTL.Visibility = handleVis;
            card.HandleTR.Visibility = handleVis;
            card.HandleBL.Visibility = handleVis;
            card.HandleBR.Visibility = handleVis;
            if (card.HandleR != null) card.HandleR.Visibility = handleVis;
            if (card.HandleL != null) card.HandleL.Visibility = handleVis;
            if (card.HandleT != null) card.HandleT.Visibility = handleVis;
            if (card.HandleB != null) card.HandleB.Visibility = handleVis;

            _highestZ++;
            Panel.SetZIndex(card.Container, _highestZ + 1000);

            if (card.HoverToolbar != null)
            {
                // Pada multi-selection masal, jangan tampilkan puluhan hover toolbar serentak
                if (_selectedCards.Count <= 1)
                {
                    card.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                    card.HoverToolbar.Opacity = 1.0;
                    card.HoverToolbar.IsHitTestVisible = true;
                }
                else
                {
                    card.HoverToolbar.Opacity = 0.0;
                    card.HoverToolbar.IsHitTestVisible = false;
                }
            }

            if (updateCounts)
            {
                UpdateStatusCounts();
            }
        }

        private void DeselectCard(CardItem card)
        {
            card.IsSelected = false;
            _selectedCards.Remove(card);

            if (card.IsNote)
            {
                ApplyNoteBackground(card, card.NoteBgColor);
            }
            else if (card.IsPaletteCard)
            {
                card.ContentBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                card.ContentBorder.BorderThickness = new Thickness(1);
                card.ContentBorder.CornerRadius = new CornerRadius(12);
            }
            else if (card.IsDrawCard)
            {
                card.ContentBorder.BorderThickness = new Thickness(0); // 100% borderless when idle!
                card.ContentBorder.Effect = null;
            }
            else
            {
                card.ContentBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                card.ContentBorder.BorderThickness = new Thickness(1);
                card.ContentBorder.CornerRadius = new CornerRadius(0);
                if (_cards.Count > 30)
                {
                    card.ContentBorder.Effect = null;
                }
            }

            card.HandleTL.Visibility = Visibility.Collapsed;
            card.HandleTR.Visibility = Visibility.Collapsed;
            card.HandleBL.Visibility = Visibility.Collapsed;
            card.HandleBR.Visibility = Visibility.Collapsed;
            if (card.HandleR != null) card.HandleR.Visibility = Visibility.Collapsed;
            if (card.HandleL != null) card.HandleL.Visibility = Visibility.Collapsed;
            if (card.HandleT != null) card.HandleT.Visibility = Visibility.Collapsed;
            if (card.HandleB != null) card.HandleB.Visibility = Visibility.Collapsed;

            if (card.HoverToolbar != null && !card.IsPlayingYouTube && !card.Container.IsMouseOver)
            {
                card.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                card.HoverToolbar.Opacity = 0.0;
                card.HoverToolbar.IsHitTestVisible = false;
            }

            Panel.SetZIndex(card.Container, 10);
            UpdateStatusCounts();
        }

        private void DeselectAllCards()
        {
            foreach (CardItem card in _selectedCards.ToList())
            {
                DeselectCard(card);
            }
            DeselectAllGroups();
        }

        private void UpdateStatusCounts()
        {
            TxtRefCount.Text = $"{_cards.Count} References";
            TxtRefCountTop.Text = $"{_cards.Count} References";
            TxtSelectionCount.Text = $"{_selectedCards.Count} Selected";
        }

        #endregion

        #region Auto-Arrange Grid Engine (Grid & Pipeline)

        private void BtnGridArrange_Click(object sender, RoutedEventArgs e)
        {
            RecordUndo("Arrange Grid");
            _gridArrangeCount++;

            // Handle Grouped Cards: If entire board is being arranged, auto-layout groups internally so members stay inside!
            if (_selectedCards.Count == 0)
            {
                foreach (var group in _groups)
                {
                    AutoLayoutGroup(group, animated: true, recordUndo: false);
                    FitGroupToCards(group, recordUndo: false);
                }
            }
            else
            {
                // If selection consists only of cards in a specific group, layout only within that group!
                var groupIds = _selectedCards.Where(c => !string.IsNullOrEmpty(c.GroupId)).Select(c => c.GroupId!).Distinct().ToList();
                if (groupIds.Count == 1 && _selectedCards.All(c => !string.IsNullOrEmpty(c.GroupId)))
                {
                    var grp = _groups.FirstOrDefault(g => g.Id == groupIds[0]);
                    if (grp != null)
                    {
                        AutoLayoutGroup(grp, animated: true, recordUndo: false);
                        FitGroupToCards(grp, recordUndo: false);
                        ScheduleAutoSave();
                        ShowToast($"Arranged cards in {grp.Title}", ToastType.Success);
                        return;
                    }
                }
            }

            // Target cards for canvas grid: only cards that do NOT belong to any group!
            var targetCards = _selectedCards.Count > 0 
                ? _selectedCards.Where(c => string.IsNullOrEmpty(c.GroupId)).ToList()
                : _cards.Where(c => string.IsNullOrEmpty(c.GroupId)).ToList();

            if (targetCards.Count == 0)
            {
                if (_groups.Count > 0)
                {
                    double curX = _groups.Min(g => g.X);
                    double curY = _groups.Min(g => g.Y);
                    foreach (var g in _groups)
                    {
                        Canvas.SetLeft(g.Container, curX);
                        Canvas.SetTop(g.Container, curY);
                        g.X = curX;
                        g.Y = curY;
                        curX += g.Width + _currentGap;
                    }
                    ScheduleAutoSave();
                    ShowToast($"Arranged {_groups.Count} scene groups", ToastType.Success);
                    ZoomToFitAllCards(animated: true, showToast: false);
                }
                return;
            }

            int total = targetCards.Count;
            int baseCols = total <= 3 ? total : (total <= 6 ? 3 : (total <= 12 ? 4 : 5));

            int colVariation = (_gridArrangeCount - 1) % 3;
            int cols = colVariation == 0 ? baseCols : (colVariation == 1 ? Math.Max(2, baseCols - 1) : Math.Min(6, baseCols + 1));

            if (_gridArrangeCount > 1)
            {
                var rng = new Random();
                targetCards = targetCards.OrderBy(_ => rng.Next()).ToList();
            }

            double gap = _currentGap;
            double avgW = targetCards.Average(c => c.Width);
            double colWidth = Math.Clamp(avgW, 240.0, 420.0);
            double totalGridWidth = (cols * colWidth) + ((cols - 1) * gap);

            double startX, startY;
            if (_selectedCards.Count > 0)
            {
                startX = targetCards.Min(c => c.X);
                startY = targetCards.Min(c => c.Y);
            }
            else
            {
                if (_groups.Count > 0)
                {
                    startX = _groups.Max(g => g.X + g.Width) + (gap * 2);
                    startY = _groups.Min(g => g.Y);
                }
                else
                {
                    Matrix matrix = CanvasMatrixTransform.Matrix;
                    matrix.Invert();
                    Point centerScreen = new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
                    Point centerWorld = matrix.Transform(centerScreen);

                    startX = centerWorld.X - (totalGridWidth / 2);
                    startY = centerWorld.Y - 200;
                }
            }

            // PureRef Masonry Column Tracking: pack into the shortest column to eliminate gaps
            double[] colHeights = new double[cols];
            for (int c = 0; c < cols; c++) colHeights[c] = startY;

            foreach (var card in targetCards)
            {
                double newW = colWidth;
                double newH = Math.Round(colWidth / Math.Max(0.1, card.AspectRatio));
                card.Width = newW;
                card.Height = newH;

                int bestCol = 0;
                for (int c = 1; c < cols; c++)
                {
                    if (colHeights[c] < colHeights[bestCol])
                    {
                        bestCol = c;
                    }
                }

                double cardX = startX + bestCol * (colWidth + gap);
                double cardY = colHeights[bestCol];

                AnimateCardPosition(card, cardX, cardY);
                colHeights[bestCol] += newH + gap;
            }

            ScheduleAutoSave();
            string msg = _selectedCards.Count > 0 
                ? $"Grid Layout #{_gridArrangeCount}: {targetCards.Count} cards ({cols} Columns)" 
                : $"Grid Layout #{_gridArrangeCount}: {cols} Columns (Gap: {(int)_currentGap}px)";
            ShowToast(msg, ToastType.Success);

            if (_selectedCards.Count == 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ZoomToFitAllCards(animated: true, showToast: false);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void BtnPipelineArrange_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCards.Count == 0)
            {
                // Auto-layout any groups internally first
                foreach (var g in _groups)
                {
                    AutoLayoutGroup(g, animated: true, recordUndo: false);
                    FitGroupToCards(g, recordUndo: false);
                }
            }
            else
            {
                // If selection consists only of cards in a specific group, layout only within that group!
                var groupIds = _selectedCards.Where(c => !string.IsNullOrEmpty(c.GroupId)).Select(c => c.GroupId!).Distinct().ToList();
                if (groupIds.Count == 1 && _selectedCards.All(c => !string.IsNullOrEmpty(c.GroupId)))
                {
                    var grp = _groups.FirstOrDefault(g => g.Id == groupIds[0]);
                    if (grp != null)
                    {
                        AutoLayoutGroup(grp, animated: true, recordUndo: false);
                        FitGroupToCards(grp, recordUndo: false);
                        ScheduleAutoSave();
                        ShowToast($"Arranged cards in {grp.Title}", ToastType.Success);
                        return;
                    }
                }
            }

            // Target cards for pipeline: only cards that do NOT belong to any group!
            var targetCards = _selectedCards.Count > 0 
                ? _selectedCards.Where(c => string.IsNullOrEmpty(c.GroupId)).ToList()
                : _cards.Where(c => string.IsNullOrEmpty(c.GroupId)).ToList();

            if (targetCards.Count == 0)
            {
                if (_groups.Count > 0)
                {
                    double curX = _groups.Min(g => g.X);
                    double curY = _groups.Min(g => g.Y);
                    foreach (var g in _groups)
                    {
                        Canvas.SetLeft(g.Container, curX);
                        Canvas.SetTop(g.Container, curY);
                        g.X = curX;
                        g.Y = curY;
                        curX += g.Width + _currentGap;
                    }
                    ScheduleAutoSave();
                    ShowToast($"Arranged {_groups.Count} scene groups horizontally", ToastType.Success);
                    ZoomToFitAllCards(animated: true, showToast: false);
                }
                return;
            }

            RecordUndo("Arrange Pipeline");
            _pipelineArrangeCount++;

            int mode = (_pipelineArrangeCount - 1) % 4;
            // Mode 0: Horizontal Storyboard (Linear order)
            // Mode 1: Horizontal Storyboard (Shuffled sequence)
            // Mode 2: 2-Row Storyboard (Top & Bottom tracks)
            // Mode 3: Vertical Reel / Feed

            if (mode == 1 || mode == 2)
            {
                var rng = new Random();
                targetCards = targetCards.OrderBy(_ => rng.Next()).ToList();
            }

            double gap = _currentGap;
            double startX, startY;

            if (_selectedCards.Count > 0)
            {
                startX = targetCards.Min(c => c.X);
                startY = targetCards.Min(c => c.Y);
            }
            else
            {
                Matrix matrix = CanvasMatrixTransform.Matrix;
                if (matrix.HasInverse)
                {
                    matrix.Invert();
                    double viewW = CanvasContainer.ActualWidth > 0 ? CanvasContainer.ActualWidth : ActualWidth;
                    double viewH = CanvasContainer.ActualHeight > 0 ? CanvasContainer.ActualHeight : ActualHeight;
                    Point centerScreen = new Point(viewW / 2.0, (viewH + 40.0) / 2.0);
                    Point centerWorld = matrix.Transform(centerScreen);

                    startX = centerWorld.X - (targetCards.Count * 220.0 / 2.0);
                    startY = centerWorld.Y - 130.0;
                }
                else
                {
                    startX = 100;
                    startY = 100;
                }
            }

            string modeDesc;

            if (mode == 3) // Vertical Reel / Feed
            {
                double targetW = 320.0;
                double curY = startY;
                foreach (CardItem card in targetCards)
                {
                    double newW = targetW;
                    double newH = Math.Round(targetW / Math.Max(0.1, card.AspectRatio));
                    card.Width = newW;
                    card.Height = newH;
                    AnimateCardPosition(card, startX, curY);
                    curY += newH + gap;
                }
                modeDesc = "Vertical Reel";
            }
            else if (mode == 2) // 2-Row Storyboard
            {
                double targetH = 220.0;
                double curX1 = startX;
                double curX2 = startX;
                double row1Y = startY;
                double row2Y = startY + targetH + gap;

                for (int i = 0; i < targetCards.Count; i++)
                {
                    CardItem card = targetCards[i];
                    double newH = targetH;
                    double newW = Math.Round(targetH * card.AspectRatio);
                    card.Width = newW;
                    card.Height = newH;

                    if (i % 2 == 0)
                    {
                        AnimateCardPosition(card, curX1, row1Y);
                        curX1 += newW + gap;
                    }
                    else
                    {
                        AnimateCardPosition(card, curX2, row2Y);
                        curX2 += newW + gap;
                    }
                }
                modeDesc = "2-Row Storyboard";
            }
            else // Mode 0 or 1: Horizontal Storyboard (Linear or Shuffled)
            {
                double targetH = 260.0;
                double curX = startX;
                foreach (CardItem card in targetCards)
                {
                    double newH = targetH;
                    double newW = Math.Round(targetH * card.AspectRatio);
                    card.Width = newW;
                    card.Height = newH;

                    AnimateCardPosition(card, curX, startY);
                    curX += newW + gap;
                }
                modeDesc = mode == 1 ? "Shuffled Timeline" : "Linear Timeline";
            }

            ScheduleAutoSave();
            string msg = _selectedCards.Count > 0
                ? $"Pipeline #{_pipelineArrangeCount}: {modeDesc} ({targetCards.Count} selected)"
                : $"Pipeline #{_pipelineArrangeCount}: {modeDesc}";
            ShowToast(msg, ToastType.Success);

            if (_selectedCards.Count == 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ZoomToFitAllCards(animated: true, showToast: false);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void BtnGapToggle_Click(object sender, RoutedEventArgs e)
        {
            _currentGap = _currentGap switch
            {
                0.0 => 8.0,
                8.0 => 16.0,
                16.0 => 24.0,
                24.0 => 32.0,
                32.0 => 48.0,
                _ => 0.0
            };
            TxtGap.Text = FormatGapText(_currentGap);
            _settings.ArrangeGap = _currentGap;
            _settings.Save();
            ScheduleAutoSave();
            ShowToast(_currentGap == 0 ? "Arrange gap set to No Gap (0px)" : $"Arrange gap set to {(int)_currentGap}px", ToastType.Info);
        }

        private void AnimateCardPosition(CardItem card, double targetX, double targetY)
        {
            DoubleAnimation animX = new DoubleAnimation(card.X, targetX, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation animY = new DoubleAnimation(card.Y, targetY, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // CRITICAL FIX: Release animation hold on completed so card can be moved freely with mouse!
            animX.Completed += (s, e) =>
            {
                card.Container.BeginAnimation(Canvas.LeftProperty, null);
                card.X = targetX;
            };
            animY.Completed += (s, e) =>
            {
                card.Container.BeginAnimation(Canvas.TopProperty, null);
                card.Y = targetY;
            };

            card.Container.BeginAnimation(Canvas.LeftProperty, animX);
            card.Container.BeginAnimation(Canvas.TopProperty, animY);
        }

        #endregion

        #region Duplication, Clipboard & Shortcuts

        private void BtnDuplicate_Click(object sender, RoutedEventArgs e) => DuplicateSelectedCards();

        private void DuplicateSelectedCards()
        {
            if (_selectedCards.Count == 0) return;

            RecordUndo("Duplicate Cards");

            var cardsToDuplicate = _selectedCards.ToList();
            DeselectAllCards();

            foreach (CardItem card in cardsToDuplicate)
            {
                Point newPos = new Point(card.X + 30, card.Y + 30);
                AddImageCard(
                    card.Bitmap,
                    newPos,
                    customWidth: card.Width,
                    customHeight: card.Height,
                    localPath: card.LocalPath,
                    base64Data: card.Base64Data,
                    originalBitmap: card.OriginalBitmap,
                    baseWidth: card.BaseWidth,
                    baseHeight: card.BaseHeight,
                    cropLeft: card.CropLeft,
                    cropTop: card.CropTop,
                    cropRight: card.CropRight,
                    cropBottom: card.CropBottom);
            }

            ScheduleAutoSave();
            ShowToast($"Duplicated {cardsToDuplicate.Count} item(s)", ToastType.Success);
        }

        private void DeleteSelectedCards()
        {
            if (_selectedCards.Count == 0 && _selectedGroups.Count == 0) return;

            RecordUndo("Delete Selected");

            int cardCount = _selectedCards.Count;
            if (cardCount > 0)
            {
                foreach (CardItem card in _selectedCards.ToList())
                {
                    CleanupCard(card);
                    WorldCanvas.Children.Remove(card.Container);
                    _cards.Remove(card);
                }
                _selectedCards.Clear();
            }

            int groupCount = _selectedGroups.Count;
            if (groupCount > 0)
            {
                foreach (GroupItem group in _selectedGroups.ToList())
                {
                    // Dissolve group affiliation for member cards without deleting the cards themselves
                    foreach (var card in _cards.Where(c => c.GroupId == group.Id))
                    {
                        card.GroupId = null;
                    }
                    WorldCanvas.Children.Remove(group.Container);
                    _groups.Remove(group);
                }
                _selectedGroups.Clear();
                UpdateGroupCounts();
            }

            UpdateStatusCounts();
            UpdateStorageStats();
            if (_cards.Count == 0)
            {
                EmptyStateOverlay.Visibility = Visibility.Visible;
            }

            ScheduleAutoSave();
            if (groupCount > 0 && cardCount > 0)
                ShowToast($"Deleted {cardCount} reference(s) and {groupCount} group(s)", ToastType.Info);
            else if (groupCount > 0)
                ShowToast($"Deleted {groupCount} group(s)", ToastType.Info);
            else
                ShowToast($"Deleted {cardCount} reference(s)", ToastType.Info);
        }

        private void BringSelectedToFront()
        {
            if (_selectedCards.Count == 0) return;
            RecordUndo("Bring to Front");
            foreach (var card in _selectedCards)
            {
                Panel.SetZIndex(card.Container, ++_highestZ);
            }
            ScheduleAutoSave();
            ShowToast("Brought to front (Ctrl + ])", ToastType.Info, 1200);
        }

        private void SendSelectedToBack()
        {
            if (_selectedCards.Count == 0) return;
            RecordUndo("Send to Back");
            foreach (var card in _selectedCards)
            {
                Panel.SetZIndex(card.Container, --_lowestZ);
            }
            ScheduleAutoSave();
            ShowToast("Sent to back (Ctrl + [)", ToastType.Info, 1200);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool isTyping = IsTextEditorFocused(e);

            // Undo (Ctrl+Z)
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                if (isTyping) return; // Allow native textbox undo
                var activeDoodleCard = _cards.FirstOrDefault(c => c.IsNote && c.IsNoteDoodleActive && c.NoteDoodleCanvas != null && c.NoteDoodleCanvas.Strokes.Count > 0);
                if (activeDoodleCard != null && activeDoodleCard.NoteDoodleCanvas != null && activeDoodleCard.NoteDoodleCanvas.Strokes.Count > 0)
                {
                    activeDoodleCard.NoteDoodleCanvas.Strokes.RemoveAt(activeDoodleCard.NoteDoodleCanvas.Strokes.Count - 1);
                    try
                    {
                        using MemoryStream ms = new MemoryStream();
                        activeDoodleCard.NoteDoodleCanvas.Strokes.Save(ms);
                        activeDoodleCard.NoteDoodleInkBase64 = Convert.ToBase64String(ms.ToArray());
                        ScheduleAutoSave();
                    }
                    catch { }
                    ShowToast("Undid last doodle stroke (Ctrl+Z)", ToastType.Info, 800);
                    e.Handled = true;
                    return;
                }

                Undo();
                e.Handled = true;
            }
            // Redo (Ctrl+Y or Ctrl+Shift+Z)
            else if ((e.Key == Key.Y && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) ||
                     (e.Key == Key.Z && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift)))
            {
                if (isTyping) return; // Allow native textbox redo
                Redo();
                e.Handled = true;
            }
            // Bring to Front (Ctrl + ])
            else if (e.Key == Key.OemCloseBrackets && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BringSelectedToFront();
                e.Handled = true;
            }
            // Send to Back (Ctrl + [)
            else if (e.Key == Key.OemOpenBrackets && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SendSelectedToBack();
                e.Handled = true;
            }
            // Group Selected (Ctrl + G)
            else if (e.Key == Key.G && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (isTyping) return;
                BtnAddGroup_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Copy (Ctrl+C)
            else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (isTyping) return; // Allow native textbox text copying
                BtnCopy_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Select All (Ctrl+A)
            else if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (isTyping) return; // Allow native textbox select-all text
                foreach (CardItem card in _cards)
                {
                    SelectCard(card, addToSelection: true);
                }
                e.Handled = true;
            }
            // Duplicate (Ctrl+D)
            else if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (isTyping) return;
                DuplicateSelectedCards();
                e.Handled = true;
            }
            // Paste (Ctrl+V)
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (isTyping) return; // Allow native textbox text pasting
                PasteFromClipboard();
                e.Handled = true;
            }
            // Save (Ctrl+S)
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    SaveProjectAs();
                else
                    SaveProject();
                e.Handled = true;
            }
            // Open (Ctrl+O)
            else if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BtnOpenDropboard_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // New (Ctrl+N)
            else if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BtnNew_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // Delete / Backspace
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (!isTyping)
                {
                    DeleteSelectedCards();
                    e.Handled = true;
                }
            }
            // Fit All in View (Home / F key)
            else if (e.Key == Key.Home || (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.None))
            {
                if (!isTyping)
                {
                    ZoomToFitAllCards(animated: true);
                    e.Handled = true;
                }
            }
            // Escape to Deselect All
            else if (e.Key == Key.Escape)
            {
                if (!isTyping)
                {
                    DeselectAllCards();
                    e.Handled = true;
                }
            }
        }

        private void PasteFromClipboard()
        {
            // 1. Check if clipboard has HTML with image source (e.g. copying GIF or image from browser)
            if (Clipboard.ContainsData(DataFormats.Html))
            {
                try
                {
                    string html = Clipboard.GetData(DataFormats.Html) as string ?? "";
                    var match = System.Text.RegularExpressions.Regex.Match(html, @"<img\s+[^>]*src=[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string src = match.Groups[1].Value;
                        if (!string.IsNullOrEmpty(src))
                        {
                            src = System.Net.WebUtility.HtmlDecode(src);
                            if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                src.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                (src.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && src.Contains(";base64,")))
                            {
                                _ = AddImageFromUrlAsync(src, "Pasted Reference");
                                return;
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Check if clipboard has file drop list (e.g. copied from File Explorer)
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files != null && files.Count > 0)
                {
                    var fileList = files.Cast<string>().ToList();
                    Point pastePos = ScreenToWorld(new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2));
                    var imageFiles = new List<string>();
                    var videoFiles = new List<string>();
                    foreach (string file in fileList)
                    {
                        string ext = System.IO.Path.GetExtension(file);
                        if (IsSupportedVideoExtension(ext)) videoFiles.Add(file);
                        else imageFiles.Add(file);
                    }
                    if (videoFiles.Count > 0) ImportVideoFilesBatch(videoFiles, pastePos);
                    if (imageFiles.Count > 0) ImportImageFilesBatch(imageFiles, pastePos);
                    return;
                }
            }

            // 3. Check if clipboard has plain text (URL, YouTube link, or data:image)
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText()?.Trim() ?? "";
                if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    (text.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && text.Contains(";base64,")))
                {
                    _ = AddImageFromUrlAsync(text, "Pasted Reference");
                    return;
                }
            }

            // 4. Fallback to direct raw clipboard bitmap (e.g. Snipping Tool, screenshot, PrintScreen)
            if (Clipboard.ContainsImage())
            {
                BitmapSource image = Clipboard.GetImage();
                if (image != null)
                {
                    AddImageCard(image);
                    ShowToast("Pasted image from clipboard", ToastType.Success);
                }
            }
        }

        #endregion

        #region Drag & Drop and File Loading / Saving

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.UnicodeText) ||
                e.Data.GetDataPresent(DataFormats.Text))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private async void ImportImageFilesBatch(IEnumerable<string> filePaths, Point startWorldPos)
        {
            var filesList = filePaths
                .Where(f => !string.IsNullOrEmpty(f) && File.Exists(f))
                .Where(f =>
                {
                    string ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
                    return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif";
                })
                .ToList();

            int total = filesList.Count;
            if (total == 0) return;

            _isImportingBatch = true;
            try
            {
                bool showModal = total >= 6;
                if (showModal)
                {
                    TxtImportProgressTitle.Text = $"Importing {total} Images...";
                    TxtImportProgressStatus.Text = "Optimizing and preparing references...";
                    TxtImportProgressPercent.Text = "0%";
                    TxtImportProgressCount.Text = $"0 of {total} processed";
                    ImportProgressBarFill.Width = 0;
                    ImportProgressModalOverlay.Opacity = 1.0;
                    ImportProgressModalOverlay.Visibility = Visibility.Visible;
                }

                Point curPos = startWorldPos;
                int processed = 0;
                int batchSize = 6;

                for (int i = 0; i < total; i += batchSize)
                {
                    int currentBatchCount = Math.Min(batchSize, total - i);
                    var batchSlice = filesList.GetRange(i, currentBatchCount);

                    // Decode images completely off-thread to keep UI 100% fluid at 60 FPS
                    var decodedList = await Task.Run(() =>
                    {
                        var list = new List<(BitmapImage Bmp, string Path)>();
                        foreach (string file in batchSlice)
                        {
                            try
                            {
                                BitmapImage bi = new BitmapImage();
                                bi.BeginInit();
                                bi.UriSource = new Uri(file, UriKind.Absolute);
                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                bi.DecodePixelWidth = 1000;
                                bi.EndInit();
                                bi.Freeze();
                                list.Add((bi, file));
                            }
                            catch { }
                        }
                        return list;
                    });

                    // Attach to Canvas in UI thread without synchronous file I/O or multi-card undo cloning
                    foreach (var item in decodedList)
                    {
                        AddImageCard(item.Bmp, curPos, localPath: item.Path, autoSelect: false, recordUndo: false);
                        curPos.X += 30;
                        curPos.Y += 30;
                        processed++;
                    }

                    if (showModal)
                    {
                        double pct = (double)processed / total;
                        ImportProgressBarFill.Width = Math.Clamp(332.0 * pct, 0, 332.0);
                        TxtImportProgressPercent.Text = $"{(int)(pct * 100)}%";
                        TxtImportProgressCount.Text = $"{processed} of {total} processed";
                        if (decodedList.Count > 0)
                        {
                            TxtImportProgressStatus.Text = $"Loaded {System.IO.Path.GetFileName(decodedList[^1].Path)}";
                        }
                    }

                    // Yield briefly to UI thread for smooth rendering and progress animation
                    await Task.Delay(8);
                }

                RecordUndo($"Import {total} Images");
                UpdateViewportCulling();
                GC.Collect(2, GCCollectionMode.Forced, false);
                ScheduleWorkingSetTrim(2000);

                if (showModal)
                {
                    DoubleAnimation fadeAnim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(220));
                    fadeAnim.Completed += (s, e) =>
                    {
                        ImportProgressModalOverlay.Visibility = Visibility.Collapsed;
                    };
                    ImportProgressModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
                }

                ShowToast($"Successfully imported {total} image{(total > 1 ? "s" : "")}!", ToastType.Success);
            }
            finally
            {
                _isImportingBatch = false;
                ScheduleAutoSave();
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            Point dropScreenPos = e.GetPosition(CanvasContainer);
            Point worldPos = ScreenToWorld(dropScreenPos);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var imageFiles = new List<string>();
                var videoFiles = new List<string>();
                foreach (string file in files)
                {
                    string ext = System.IO.Path.GetExtension(file);
                    if (ext.Equals(".dropboard", StringComparison.OrdinalIgnoreCase))
                    {
                        LoadDropboardFile(file);
                    }
                    else if (IsSupportedVideoExtension(ext))
                    {
                        videoFiles.Add(file);
                    }
                    else if (ext.ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif")
                    {
                        imageFiles.Add(file);
                    }
                }

                if (videoFiles.Count > 0)
                {
                    ImportVideoFilesBatch(videoFiles, worldPos);
                }
                if (imageFiles.Count > 0)
                {
                    ImportImageFilesBatch(imageFiles, worldPos);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.UnicodeText) || e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = ((string)(e.Data.GetData(DataFormats.UnicodeText) ?? e.Data.GetData(DataFormats.Text) ?? "")).Trim();
                if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _ = AddImageFromUrlAsync(text, "Dropped Reference");
                }
                else if (!string.IsNullOrWhiteSpace(text))
                {
                    AddNoteCard(text: text, worldPosition: worldPos);
                    ShowToast("Created note from dropped text", ToastType.Success);
                }
            }
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "All Media (*.png;*.jpg;*.mp4;*.mkv;*.3gp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.gif;*.mp4;*.mkv;*.webm;*.mov;*.avi;*.wmv;*.3gp;*.m4v|Images (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Videos (*.mp4;*.mkv;*.webm;*.mov;*.3gp)|*.mp4;*.mkv;*.webm;*.mov;*.avi;*.wmv;*.3gp;*.m4v|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true && dlg.FileNames.Length > 0)
            {
                Point startPos = ScreenToWorld(new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2));
                var imageFiles = new List<string>();
                var videoFiles = new List<string>();
                foreach (string file in dlg.FileNames)
                {
                    string ext = System.IO.Path.GetExtension(file);
                    if (IsSupportedVideoExtension(ext)) videoFiles.Add(file);
                    else imageFiles.Add(file);
                }
                if (videoFiles.Count > 0) ImportVideoFilesBatch(videoFiles, startPos);
                if (imageFiles.Count > 0) ImportImageFilesBatch(imageFiles, startPos);
            }
        }

        private void BtnAddUrl_Click(object sender, RoutedEventArgs e)
        {
            string clipText = "";
            try
            {
                if (Clipboard.ContainsText())
                    clipText = Clipboard.GetText()?.Trim() ?? "";
            }
            catch { }

            string initialUrl = (clipText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || clipText.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                ? clipText : "";

            string? input = ShowUrlInputDialog(initialUrl);
            if (!string.IsNullOrWhiteSpace(input))
            {
                _ = AddImageFromUrlAsync(input.Trim(), "Web Reference");
            }
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            var note = AddNoteCard();
            ShowToast("Added sticky note (Press N)", ToastType.Success);

            // Focus editor
            Dispatcher.BeginInvoke(new Action(() =>
            {
                note.NoteEditor?.Focus();
                note.NoteEditor?.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void BtnAddChecklist_Click(object sender, RoutedEventArgs e)
        {
            var note = AddNoteCard(
                text: "",
                isChecklist: true,
                hasDeadline: true,
                deadlineDateTime: DateTime.Today.AddDays(1).AddHours(18),
                deadlineLabel: "Deadline"
            );
            ShowToast("Added Checklist & Deadline task note (Press T)", ToastType.Success);
        }

        #region Freehand Canvas Brush Tool Engine

        private bool _isBrushMode = false;
        private string _activeBrushColor = "#38BDF8";
        private double _activeBrushSize = 4.0;
        private bool _activeBrushIsEraser = false;

        private void SetupBrushModeSwatches()
        {
            if (BrushColorSwatches == null) return;
            BrushColorSwatches.Children.Clear();

            string[] colors = new string[] { "#38BDF8", "#FFFFFF", "#FBBF24", "#EF4444", "#10B981", "#A78BFA", "#0F172A" };
            foreach (var hex in colors)
            {
                Border swatch = new Border
                {
                    Width = 14,
                    Height = 14,
                    CornerRadius = new CornerRadius(7),
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(hex == _activeBrushColor && !_activeBrushIsEraser ? 2 : 1),
                    BorderBrush = (hex == _activeBrushColor && !_activeBrushIsEraser) ? Brushes.White : new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    ToolTip = $"Brush Color: {hex}"
                };
                try { swatch.Background = (Brush)new BrushConverter().ConvertFromString(hex)!; } catch { }

                swatch.MouseLeftButtonDown += (s, e) =>
                {
                    _activeBrushColor = hex;
                    _activeBrushIsEraser = false;
                    UpdateBrushModeOverlayAttributes();
                    SetupBrushModeSwatches();
                    e.Handled = true;
                };

                BrushColorSwatches.Children.Add(swatch);
            }
        }

        private void UpdateBrushModeOverlayAttributes()
        {
            if (CanvasBrushOverlay == null) return;

            Color c = Colors.DeepSkyBlue;
            try { c = (Color)ColorConverter.ConvertFromString(_activeBrushColor); } catch { }

            CanvasBrushOverlay.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = c,
                Width = _activeBrushSize,
                Height = _activeBrushSize,
                FitToCurve = true,
                IgnorePressure = false,
                StylusTip = StylusTip.Ellipse
            };

            CanvasBrushOverlay.EditingMode = _activeBrushIsEraser
                ? InkCanvasEditingMode.EraseByPoint
                : InkCanvasEditingMode.Ink;

            double eraserRadius = Math.Max(14.0, _activeBrushSize * 3.5);
            CanvasBrushOverlay.EraserShape = new EllipseStylusShape(eraserRadius, eraserRadius);

            if (TxtBrushSize != null) TxtBrushSize.Text = $"{(int)_activeBrushSize}px ▾";
            if (TxtBrushEraser != null)
            {
                TxtBrushEraser.Foreground = _activeBrushIsEraser
                    ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
                    : new SolidColorBrush(Color.FromRgb(156, 163, 175));
            }
        }

        private void ToggleBrushMode(bool? forceState = null)
        {
            bool nextState = forceState ?? !_isBrushMode;
            if (_isBrushMode == nextState) return;
            _isBrushMode = nextState;

            if (_isBrushMode)
            {
                DeselectAllCards();
                CanvasBrushOverlay.Visibility = Visibility.Visible;
                CanvasBrushOverlay.IsHitTestVisible = true;
                CanvasBrushOverlay.Cursor = Cursors.Pen;
                CanvasBrushOverlay.Strokes.Clear();

                UpdateBrushModeOverlayAttributes();
                SetupBrushModeSwatches();

                BrushModeBar.Visibility = Visibility.Visible;

                // Visual highlight on Dock Brush button
                BtnAddDraw.Background = new SolidColorBrush(Color.FromArgb(60, 56, 189, 248));
                BtnAddDraw.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                IconBrushPath.Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                IconBrushLine.Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                TxtBrushBtn.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));

                ShowToast("Brush Mode: Draw freely anywhere! (Press V or 'Finish' to transform)", ToastType.Info, 2500);
            }
            else
            {
                // Commit any drawn strokes into a transformable freehand card!
                if (CanvasBrushOverlay.Strokes.Count > 0)
                {
                    Rect bounds = CanvasBrushOverlay.Strokes.GetBounds();
                    if (bounds.Width > 0 && bounds.Height > 0)
                    {
                        double overlayLeft = Canvas.GetLeft(CanvasBrushOverlay);
                        if (double.IsNaN(overlayLeft)) overlayLeft = 0;
                        double overlayTop = Canvas.GetTop(CanvasBrushOverlay);
                        if (double.IsNaN(overlayTop)) overlayTop = 0;

                        double pad = 8;
                        double cardX = Math.Round(bounds.Left + overlayLeft - pad);
                        double cardY = Math.Round(bounds.Top + overlayTop - pad);
                        double cardW = Math.Max(40, Math.Round(bounds.Width + pad * 2));
                        double cardH = Math.Max(30, Math.Round(bounds.Height + pad * 2));

                        // Shift strokes relative to card local origin
                        Matrix shift = Matrix.Identity;
                        shift.Translate(-bounds.Left + pad, -bounds.Top + pad);
                        CanvasBrushOverlay.Strokes.Transform(shift, false);

                        string base64 = "";
                        try
                        {
                            using MemoryStream ms = new MemoryStream();
                            CanvasBrushOverlay.Strokes.Save(ms);
                            base64 = Convert.ToBase64String(ms.ToArray());
                        }
                        catch { }

                        var newCard = AddDrawCard(
                            worldPosition: new Point(cardX, cardY),
                            customWidth: cardW,
                            customHeight: cardH,
                            initialInkBase64: base64,
                            penColor: _activeBrushColor,
                            penSize: _activeBrushSize,
                            autoSelect: true);

                        ShowToast("Created freehand sketch! Ready to transform.", ToastType.Success);
                    }
                    CanvasBrushOverlay.Strokes.Clear();
                }

                CanvasBrushOverlay.Visibility = Visibility.Collapsed;
                CanvasBrushOverlay.IsHitTestVisible = false;
                BrushModeBar.Visibility = Visibility.Collapsed;

                // Reset Dock button visual
                BtnAddDraw.Background = Brushes.Transparent;
                BtnAddDraw.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                IconBrushPath.Stroke = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                IconBrushLine.Stroke = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                TxtBrushBtn.Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219));
            }
        }

        private void BtnAddDraw_Click(object sender, RoutedEventArgs e)
        {
            ToggleBrushMode();
        }

        private void BtnBrushSizeToggle_Click(object sender, RoutedEventArgs e)
        {
            double[] sizes = new double[] { 2.0, 4.0, 8.0, 16.0 };
            int curIdx = Array.IndexOf(sizes, _activeBrushSize);
            _activeBrushSize = sizes[(curIdx + 1) % sizes.Length];
            UpdateBrushModeOverlayAttributes();
        }

        private void BtnBrushEraserToggle_Click(object sender, RoutedEventArgs e)
        {
            _activeBrushIsEraser = !_activeBrushIsEraser;
            UpdateBrushModeOverlayAttributes();
            SetupBrushModeSwatches();
        }

        private void BtnBrushDone_Click(object sender, RoutedEventArgs e)
        {
            ToggleBrushMode(false);
        }

        #endregion

        private string? ShowUrlInputDialog(string initialUrl = "")
        {
            Window dialog = new Window
            {
                Title = "Add Web Reference",
                Width = 460,
                Height = 185,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            Border root = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(242, 18, 21, 29)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 18, 20, 18),
                Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 8, Opacity = 0.7, Color = Colors.Black }
            };

            StackPanel sp = new StackPanel();

            TextBlock title = new TextBlock
            {
                Text = "Add Web Reference",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            sp.Children.Add(title);

            TextBlock desc = new TextBlock
            {
                Text = "Supports Pinterest, YouTube (video/shorts), and direct image links:",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(desc);

            TextBox input = new TextBox
            {
                Text = initialUrl,
                Background = new SolidColorBrush(Color.FromRgb(26, 32, 44)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 14)
            };
            sp.Children.Add(input);

            StackPanel btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button btnCancel = new Button
            {
                Content = "Cancel",
                Style = (Style)FindResource("TbButton"),
                Margin = new Thickness(0, 0, 8, 0),
                Width = 75,
                Height = 28
            };
            btnCancel.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            btnPanel.Children.Add(btnCancel);

            Button btnAdd = new Button
            {
                Content = "Add Reference",
                Style = (Style)FindResource("TbButton"),
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                FontWeight = FontWeights.Bold,
                Width = 110,
                Height = 28
            };
            btnAdd.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            btnPanel.Children.Add(btnAdd);

            sp.Children.Add(btnPanel);
            root.Child = sp;
            dialog.Content = root;

            input.SelectAll();
            dialog.Loaded += (s, e) => input.Focus();
            input.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
                else if (e.Key == Key.Escape)
                {
                    dialog.DialogResult = false;
                    dialog.Close();
                }
            };

            bool? result = dialog.ShowDialog();
            return result == true ? input.Text.Trim() : null;
        }

        private void ApplyDockLayout()
        {
            double curW = ActualWidth > 0 ? ActualWidth : Width;
            double curH = ActualHeight > 0 ? ActualHeight : Height;
            UpdateResponsiveLayout(curW, curH);
        }

        private void RevealDock()
        {
            if (!_isDockAutoHide || _isDockRevealed) return;
            _isDockRevealed = true;

            FloatingDock.IsHitTestVisible = true;
            DoubleAnimation animOpacity = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            FloatingDock.BeginAnimation(UIElement.OpacityProperty, animOpacity);

            if (_dockPosition == "left")
            {
                DoubleAnimation animX = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                DockTranslateTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            }
            else
            {
                DoubleAnimation animY = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                DockTranslateTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            }
        }

        private void HideDock()
        {
            if (!_isDockAutoHide || !_isDockRevealed) return;
            _isDockRevealed = false;

            DoubleAnimation animOpacity = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            animOpacity.Completed += (s, e) =>
            {
                if (_isDockAutoHide && !_isDockRevealed)
                {
                    FloatingDock.IsHitTestVisible = false;
                    FloatingDock.Opacity = 0.0;
                }
            };
            FloatingDock.BeginAnimation(UIElement.OpacityProperty, animOpacity);

            if (_dockPosition == "left")
            {
                double hideDist = FloatingDock.ActualWidth > 55 ? -(FloatingDock.ActualWidth + 30) : -85;
                DoubleAnimation animX = new DoubleAnimation(hideDist, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                DockTranslateTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            }
            else
            {
                // Slide up behind the 40px TitleBar (Top: 52 - 55 = -3px, Bottom: ~37px < 40px)
                DoubleAnimation animY = new DoubleAnimation(-55, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                DockTranslateTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            }
        }

        private void DockHoverZone_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isDockAutoHide) RevealDock();
        }

        private void DockHoverZone_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDockAutoHide && !_isDockRevealed) RevealDock();
        }

        private void FloatingDock_MouseEnter(object sender, MouseEventArgs e) => RevealDock();

        private void FloatingDock_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDockAutoHide)
            {
                Point pt = e.GetPosition(this);
                if (_dockPosition == "top" && pt.Y <= 105)
                {
                    // Still in top hover zone
                    return;
                }
                HideDock();
            }
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDockAutoHide) return;
            if (_isPanning || _isMarqueeSelecting || _isDraggingCards || _isResizingCard || _isCropping) return;

            Point pt = e.GetPosition(this);
            if (_dockPosition == "left")
            {
                double dockW = FloatingDock.ActualWidth > 0 ? FloatingDock.ActualWidth : 44;
                if ((pt.X <= (dockW + 15) && pt.Y > 40) || FloatingDock.IsMouseOver)
                {
                    RevealDock();
                }
                else if ((pt.X > (dockW + 70) || pt.Y <= 40) && !FloatingDock.IsMouseOver)
                {
                    HideDock();
                }
            }
            else // top
            {
                // Hover zone is anywhere in top area (Y <= 105) or over the dock itself
                if (FloatingDock.IsMouseOver || pt.Y <= 105)
                {
                    RevealDock();
                }
                else if (pt.Y > 125 && !FloatingDock.IsMouseOver)
                {
                    HideDock();
                }
            }
        }

        private void DockTrackingTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isDockAutoHide) return;
            if (!IsVisible || WindowState == WindowState.Minimized) return;
            if (_isPanning || _isMarqueeSelecting || _isDraggingCards || _isResizingCard || _isCropping) return;

            if (!GetCursorPos(out POINT screenPt)) return;

            try
            {
                Point clientPt = PointFromScreen(new Point(screenPt.X, screenPt.Y));

                // Check if cursor has exited the window bounds
                if (clientPt.X < -15 || clientPt.X > ActualWidth + 15 || clientPt.Y < -15 || clientPt.Y > ActualHeight + 15)
                {
                    if (_isDockRevealed && !FloatingDock.IsMouseOver)
                    {
                        HideDock();
                    }
                    return;
                }

                if (_dockPosition == "left")
                {
                    double dockW = FloatingDock.ActualWidth > 0 ? FloatingDock.ActualWidth : 44;
                    if ((clientPt.X <= (dockW + 15) && clientPt.Y > 40) || FloatingDock.IsMouseOver)
                    {
                        RevealDock();
                    }
                    else if ((clientPt.X > (dockW + 70) || clientPt.Y <= 40) && !FloatingDock.IsMouseOver)
                    {
                        HideDock();
                    }
                }
                else // top
                {
                    // Hover zone is anywhere in top area (clientPt.Y <= 105) or over the dock itself
                    if (FloatingDock.IsMouseOver || clientPt.Y <= 105)
                    {
                        RevealDock();
                    }
                    else if (clientPt.Y > 125 && !FloatingDock.IsMouseOver)
                    {
                        HideDock();
                    }
                }
            }
            catch { }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResponsiveLayout(e.NewSize.Width, e.NewSize.Height);
            SyncActiveHwndPositions(updateSize: false);
        }

        private void UpdateResponsiveLayout(double width, double height = 0)
        {
            if (width <= 0) return;
            if (height <= 0) height = ActualHeight > 0 ? ActualHeight : Height;

            bool isLeft = _dockPosition == "left";

            // 1. Dock Toolbar Responsive Layout (Top Horizontal vs Left Sidebar)
            if (isLeft)
            {
                FloatingDock.HorizontalAlignment = HorizontalAlignment.Left;
                FloatingDock.VerticalAlignment = VerticalAlignment.Center;
                FloatingDock.Margin = new Thickness(10, 0, 0, 0);

                // Auto 2-Kolom (Photoshop Style) jika tinggi window sempit (< 620px)
                bool isTwoColumn = height < 620;
                bool isUltraCompact = height < 460;
                double btnSize = isUltraCompact ? 28.0 : (isTwoColumn ? 32.0 : 34.0);

                // Batasi tinggi dock agar tidak pernah meluap menabrak titlebar atau status bar
                FloatingDock.MaxHeight = Math.Max(160, height - 90);

                if (isTwoColumn)
                {
                    // 2 Kolom: lebar pas untuk 2 tombol + padding
                    FloatingDock.Width = (btnSize * 2) + 16;
                    FloatingDock.Padding = new Thickness(4, 4, 4, 4);
                    DockWrapPanel.Orientation = Orientation.Horizontal;

                    foreach (UIElement child in DockWrapPanel.Children)
                    {
                        if (child is Button btn)
                        {
                            btn.Width = btnSize;
                            btn.Height = btnSize;
                            btn.Padding = new Thickness(0);
                            btn.Margin = new Thickness(1);
                            if (btn.Content is StackPanel sp)
                            {
                                foreach (UIElement item in sp.Children)
                                {
                                    if (item is TextBlock tb)
                                        tb.Visibility = Visibility.Collapsed;
                                    else if (item is FrameworkElement fe)
                                        fe.Margin = new Thickness(0);
                                }
                            }
                        }
                        else if (child is System.Windows.Shapes.Rectangle rect)
                        {
                            // Sembunyikan divider pada mode 2 kolom agar sangat rapi dan hemat ruang
                            rect.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    // 1 Kolom memanjang ke bawah (Normal tinggi)
                    FloatingDock.Width = 44;
                    FloatingDock.Padding = new Thickness(4, 3, 4, 3);
                    DockWrapPanel.Orientation = Orientation.Vertical;

                    foreach (UIElement child in DockWrapPanel.Children)
                    {
                        if (child is Button btn)
                        {
                            btn.Width = 34;
                            btn.Height = 34;
                            btn.Padding = new Thickness(0);
                            btn.Margin = new Thickness(0, 0, 0, 2);
                            if (btn.Content is StackPanel sp)
                            {
                                foreach (UIElement item in sp.Children)
                                {
                                    if (item is TextBlock tb)
                                        tb.Visibility = Visibility.Collapsed;
                                    else if (item is FrameworkElement fe)
                                        fe.Margin = new Thickness(0);
                                }
                            }
                        }
                        else if (child is System.Windows.Shapes.Rectangle rect)
                        {
                            rect.Visibility = Visibility.Visible;
                            rect.Width = 20;
                            rect.Height = 1;
                            rect.Margin = new Thickness(0, 4, 0, 4);
                        }
                    }
                }

                TxtDockPos.Text = "Top Bar";
                BtnDockPosToggle.ToolTip = "Switch Toolbar Position (Top Bar Horizontal)";
                if (IconDockPosSidebar != null) IconDockPosSidebar.Visibility = Visibility.Collapsed;
                if (IconDockPosTopbar != null) IconDockPosTopbar.Visibility = Visibility.Visible;
            }
            else // top horizontal mode
            {
                FloatingDock.HorizontalAlignment = HorizontalAlignment.Center;
                FloatingDock.VerticalAlignment = VerticalAlignment.Top;
                FloatingDock.Margin = new Thickness(0, 52, 0, 0);
                FloatingDock.Width = double.NaN;
                FloatingDock.MaxHeight = double.PositiveInfinity;
                FloatingDock.Padding = new Thickness(6, 3, 6, 3);
                DockWrapPanel.Orientation = Orientation.Horizontal;

                bool compactDock = width <= 1080;
                foreach (UIElement child in DockWrapPanel.Children)
                {
                    if (child is Button btn)
                    {
                        btn.Width = double.NaN;
                        btn.Height = double.NaN;
                        btn.Margin = new Thickness(0);
                        btn.Padding = compactDock ? new Thickness(6, 4, 6, 4) : new Thickness(7, 4, 7, 4);
                        if (btn.Content is StackPanel sp)
                        {
                            foreach (UIElement item in sp.Children)
                            {
                                if (item is TextBlock tb)
                                {
                                    tb.Visibility = compactDock ? Visibility.Collapsed : Visibility.Visible;
                                }
                                else if (item is FrameworkElement fe)
                                {
                                    fe.Margin = compactDock ? new Thickness(0) : new Thickness(0, 0, 5, 0);
                                }
                            }
                        }
                    }
                    else if (child is System.Windows.Shapes.Rectangle rect)
                    {
                        rect.Visibility = Visibility.Visible;
                        rect.Width = 1;
                        rect.Height = 16;
                        rect.Margin = new Thickness(5, 0, 5, 0);
                    }
                }

                TxtDockPos.Text = "Sidebar";
                BtnDockPosToggle.ToolTip = "Switch Toolbar Position (Left Sidebar 1x Mode)";
                if (IconDockPosSidebar != null) IconDockPosSidebar.Visibility = Visibility.Visible;
                if (IconDockPosTopbar != null) IconDockPosTopbar.Visibility = Visibility.Collapsed;
            }

            // 2. Titlebar Responsive Anti-Overlap
            // Threshold 1120px: Collapse New, Open, Save, Save As to pure SVG icons (saves ~160px)
            bool compactTitleBtns = width <= 1120;
            if (TxtBtnNew != null) TxtBtnNew.Visibility = compactTitleBtns ? Visibility.Collapsed : Visibility.Visible;
            if (TxtBtnOpen != null) TxtBtnOpen.Visibility = compactTitleBtns ? Visibility.Collapsed : Visibility.Visible;
            if (TxtBtnSave != null) TxtBtnSave.Visibility = compactTitleBtns ? Visibility.Collapsed : Visibility.Visible;
            if (TxtBtnSaveAs != null) TxtBtnSaveAs.Visibility = compactTitleBtns ? Visibility.Collapsed : Visibility.Visible;
            if (TxtBtnPin != null) TxtBtnPin.Visibility = compactTitleBtns ? Visibility.Collapsed : Visibility.Visible;
            if (TxtBtnAbout != null) TxtBtnAbout.Visibility = compactTitleBtns ? Visibility.Collapsed : Visibility.Visible;

            Thickness iconMargin = compactTitleBtns ? new Thickness(0) : new Thickness(0, 0, 5, 0);
            Thickness iconMarginPinAbout = compactTitleBtns ? new Thickness(0) : new Thickness(0, 0, 4, 0);
            Thickness btnPadding = compactTitleBtns ? new Thickness(6, 4, 6, 4) : new Thickness(8, 4, 8, 4);

            if (IconBtnNew != null) IconBtnNew.Margin = iconMargin;
            if (IconBtnOpen != null) IconBtnOpen.Margin = iconMargin;
            if (IconBtnSave != null) IconBtnSave.Margin = iconMargin;
            if (IconBtnSaveAs != null) IconBtnSaveAs.Margin = iconMargin;
            if (IconBtnPin != null) IconBtnPin.Margin = iconMarginPinAbout;
            if (IconBtnAbout != null) IconBtnAbout.Margin = iconMarginPinAbout;

            if (BtnNew != null) BtnNew.Padding = btnPadding;
            if (BtnOpenDropboard != null) BtnOpenDropboard.Padding = btnPadding;
            if (BtnSave != null) BtnSave.Padding = btnPadding;
            if (BtnSaveAs != null) BtnSaveAs.Padding = btnPadding;
            if (BtnPin != null) BtnPin.Padding = btnPadding;
            if (BtnAbout != null) BtnAbout.Padding = btnPadding;

            // Threshold 900px: Hide Save As, ref counter, meta divider
            bool hide900 = width <= 900;
            if (TxtRefCountTop != null) TxtRefCountTop.Visibility = hide900 ? Visibility.Collapsed : Visibility.Visible;
            if (TxtMetaDivider != null) TxtMetaDivider.Visibility = hide900 ? Visibility.Collapsed : Visibility.Visible;
            if (BtnSaveAs != null) BtnSaveAs.Visibility = hide900 ? Visibility.Collapsed : Visibility.Visible;
            if (TxtProjectTitle != null) TxtProjectTitle.MaxWidth = hide900 ? 90 : 160;

            // Threshold 760px: Hide Studio badge, folder icon, about button
            bool hide760 = width <= 760;
            if (BrandBadgeStudio != null) BrandBadgeStudio.Visibility = hide760 ? Visibility.Collapsed : Visibility.Visible;
            if (IconFolder != null) IconFolder.Visibility = hide760 ? Visibility.Collapsed : Visibility.Visible;
            if (BtnAbout != null) BtnAbout.Visibility = hide760 ? Visibility.Collapsed : Visibility.Visible;
            if (TxtProjectTitle != null) TxtProjectTitle.MaxWidth = hide760 ? 60 : 90;

            // Threshold 620px: Hide project title, hide New button
            bool hide620 = width <= 620;
            if (TxtProjectTitle != null) TxtProjectTitle.Visibility = hide620 ? Visibility.Collapsed : Visibility.Visible;
            if (BtnNew != null) BtnNew.Visibility = hide620 ? Visibility.Collapsed : Visibility.Visible;

            // Threshold 520px: Hide Settings button
            bool hide520 = width <= 520;
            if (BtnSettings != null) BtnSettings.Visibility = hide520 ? Visibility.Collapsed : Visibility.Visible;

            // Threshold 450px: Hide Pin button, brand title
            bool hide450 = width <= 450;
            if (BtnPin != null) BtnPin.Visibility = hide450 ? Visibility.Collapsed : Visibility.Visible;
            if (TxtBrandTitle != null) TxtBrandTitle.Visibility = hide450 ? Visibility.Collapsed : Visibility.Visible;

            // Opacity slider container: ALWAYS VISIBLE across all window sizes
            if (BorderOpacityContainer != null) BorderOpacityContainer.Visibility = Visibility.Visible;

            bool compactOpacity = width <= 410;
            if (CanvasBgText != null) CanvasBgText.Visibility = compactOpacity ? Visibility.Collapsed : Visibility.Visible;
            if (CanvasBgSlider != null) CanvasBgSlider.Width = compactOpacity ? 42 : 60;
        }

        private void UpdateDockAutoHideUI()
        {
            TxtFloat.Text = _isDockAutoHide ? "Float" : "Pin";
            BtnDockPin.ToolTip = _isDockAutoHide 
                ? "Toolbar is in Auto-Hide (Float) mode. Click to Pin permanently." 
                : "Toolbar is Pinned. Click to enable Auto-Hide (Float).";

            if (DockPinLine != null && DockPinPath != null)
            {
                if (_isDockAutoHide)
                {
                    var slate = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                    DockPinLine.Stroke = slate;
                    DockPinPath.Stroke = slate;
                    DockPinPath.Fill = Brushes.Transparent;
                    BtnDockPin.Background = Brushes.Transparent;
                }
                else
                {
                    var cyan = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    DockPinLine.Stroke = cyan;
                    DockPinPath.Stroke = cyan;
                    DockPinPath.Fill = new SolidColorBrush(Color.FromArgb(45, 56, 189, 248));
                    BtnDockPin.Background = new SolidColorBrush(Color.FromArgb(25, 56, 189, 248));
                }
            }

            if (_isDockAutoHide)
            {
                if (!FloatingDock.IsMouseOver)
                {
                    _isDockRevealed = true; // force HideDock to execute transition
                    HideDock();
                }
                else
                {
                    _isDockRevealed = true;
                }
            }
            else
            {
                _isDockRevealed = true;
                FloatingDock.BeginAnimation(UIElement.OpacityProperty, null);
                DockTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
                DockTranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
                DockTranslateTransform.X = 0;
                DockTranslateTransform.Y = 0;
                FloatingDock.Opacity = 1.0;
                FloatingDock.IsHitTestVisible = true;
            }
        }

        private void BtnDockPosToggle_Click(object sender, RoutedEventArgs e)
        {
            _dockPosition = _dockPosition == "left" ? "top" : "left";
            _settings.DockPosition = _dockPosition;
            _settings.Save();
            ApplyDockLayout();
            UpdateDockAutoHideUI();
            ShowToast(_dockPosition == "left" ? "Toolbar switched to Left Sidebar (1x Mode)" : "Toolbar switched to Top Bar (Horizontal)", ToastType.Info);
        }

        private void BtnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCards.Count > 0)
            {
                RecordUndo("Group Selected Cards");
                double minX = _selectedCards.Min(c => c.X);
                double minY = _selectedCards.Min(c => c.Y);
                double maxX = _selectedCards.Max(c => c.X + c.Width);
                double maxY = _selectedCards.Max(c => c.Y + c.Height);

                double pad = 16.0;
                double headerH = 34.0;
                double gx = minX - pad;
                double gy = minY - headerH - pad;
                double gw = Math.Max(260, (maxX - minX) + (pad * 2));
                double gh = Math.Max(180, (maxY - minY) + headerH + (pad * 2));

                var grp = AddSceneGroup(
                    title: $"Scene {(_groups.Count + 1):D2}",
                    customX: gx,
                    customY: gy,
                    width: gw,
                    height: gh,
                    recordUndo: false);

                foreach (var c in _selectedCards)
                {
                    c.GroupId = grp.Id;
                }
                AutoLayoutGroup(grp, animated: true, recordUndo: false);
                UpdateGroupCounts();
                ShowToast($"Grouped {_selectedCards.Count} references into {grp.Title}", ToastType.Success);
            }
            else
            {
                Point centerWorld = ScreenToWorld(new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2));
                AddSceneGroup(
                    title: $"Scene {(_groups.Count + 1):D2}",
                    customX: centerWorld.X - 230,
                    customY: centerWorld.Y - 190);
                ShowToast("Added Scene Group Frame", ToastType.Success);
            }
        }

        #region Scene Groups System

        public static UIElement CreateSvgIcon(string pathData, string strokeColor = "#94A3B8", double size = 12.0, double strokeThickness = 1.8)
        {
            Viewbox vb = new Viewbox { Width = size, Height = size };
            Canvas cv = new Canvas { Width = 24, Height = 24 };
            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = (Brush)new BrushConverter().ConvertFromString(strokeColor)!,
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = Brushes.Transparent
            };
            cv.Children.Add(p);
            vb.Child = cv;
            return vb;
        }

        public GroupItem AddSceneGroup(
            string title = "Scene 01",
            string notes = "",
            double? customX = null,
            double? customY = null,
            double width = 460,
            double height = 380,
            string color = "#3B82F6",
            string? customId = null,
            bool recordUndo = true)
        {
            if (recordUndo && !_isApplyingSnapshot && !_isRestoringSession)
            {
                RecordUndo("Add Scene Group");
            }

            double x = customX ?? 100;
            double y = customY ?? 100;
            width = Math.Max(200, width);
            height = Math.Max(140, height);
            string id = customId ?? ("grp_" + Guid.NewGuid().ToString("N")[..8]);

            Color grpCol;
            try { grpCol = (Color)ColorConverter.ConvertFromString(color); }
            catch { grpCol = Color.FromRgb(59, 130, 246); }

            // 1. Ultra-clean Frame Border (identical feel to a Note Card)
            Border frameBorder = new Border
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Color.FromArgb(8, grpCol.R, grpCol.G, grpCol.B)),
                BorderBrush = new SolidColorBrush(grpCol),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                SnapsToDevicePixels = true,
                Cursor = Cursors.SizeAll
            };

            // 2. Minimalist In-Frame Label at top-left (like text on a note card!)
            StackPanel labelSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 10, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            Border colorDot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(grpCol),
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            labelSp.Children.Add(colorDot);

            TextBlock titleText = new TextBlock
            {
                Text = title,
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = "Double-click to rename group"
            };
            labelSp.Children.Add(titleText);

            TextBox titleBox = new TextBox
            {
                Text = title,
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 1, 4, 1),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Visibility = Visibility.Collapsed
            };
            labelSp.Children.Add(titleBox);

            TextBlock countBadge = new TextBlock
            {
                Text = "(0 refs)",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            labelSp.Children.Add(countBadge);

            frameBorder.Child = labelSp;

            Grid container = new Grid
            {
                Width = width,
                Height = height,
                Cursor = Cursors.SizeAll,
                ClipToBounds = false
            };
            Canvas.SetLeft(container, x);
            Canvas.SetTop(container, y);
            Panel.SetZIndex(container, 0);

            container.Children.Add(frameBorder);

            // 3. Four Corner Resize Handles (exactly like Note Cards!)
            Border handleTL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE);
            Border handleTR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW);
            Border handleBL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW);
            Border handleBR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE);

            handleTL.BorderBrush = new SolidColorBrush(grpCol);
            handleTR.BorderBrush = new SolidColorBrush(grpCol);
            handleBL.BorderBrush = new SolidColorBrush(grpCol);
            handleBR.BorderBrush = new SolidColorBrush(grpCol);

            container.Children.Add(handleTL);
            container.Children.Add(handleTR);
            container.Children.Add(handleBL);
            container.Children.Add(handleBR);

            TextBox notesBox = new TextBox { Text = notes, Visibility = Visibility.Collapsed };

            GroupItem item = new GroupItem
            {
                Id = id,
                Title = title,
                Color = color,
                Notes = notes,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Container = container,
                FrameBorder = frameBorder,
                NotesBox = notesBox,
                TitleText = titleText,
                CountBadge = countBadge,
                ColorDot = colorDot,
                HandleTL = handleTL,
                HandleTR = handleTR,
                HandleBL = handleBL,
                HandleBR = handleBR
            };

            AttachGroupResizeHandleEvents(item, handleTL, ResizeCorner.TopLeft);
            AttachGroupResizeHandleEvents(item, handleTR, ResizeCorner.TopRight);
            AttachGroupResizeHandleEvents(item, handleBL, ResizeCorner.BottomLeft);
            AttachGroupResizeHandleEvents(item, handleBR, ResizeCorner.BottomRight);

            // 4. Floating Hover Toolbar on top of group (zero-size Canvas host avoids clipping when group is narrow/resized)
            Border hoverToolbar = CreateGroupHoverToolbar(item);
            item.HoverToolbar = hoverToolbar;

            Canvas toolbarHost = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 0,
                Height = 0,
                ClipToBounds = false
            };
            Panel.SetZIndex(toolbarHost, 10001);
            Canvas.SetTop(hoverToolbar, -36.0);
            toolbarHost.Children.Add(hoverToolbar);
            container.Children.Add(toolbarHost);

            hoverToolbar.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0)
                {
                    Canvas.SetLeft(hoverToolbar, -e.NewSize.Width / 2.0);
                    Canvas.SetTop(hoverToolbar, -36.0);
                }
            };

            // Hover effects for revealing corner handles and floating toolbar
            container.MouseEnter += (s, e) =>
            {
                handleTL.Visibility = Visibility.Visible;
                handleTR.Visibility = Visibility.Visible;
                handleBL.Visibility = Visibility.Visible;
                handleBR.Visibility = Visibility.Visible;

                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                hoverToolbar.Opacity = 1.0;
                hoverToolbar.IsHitTestVisible = true;
            };

            container.MouseLeave += (s, e) =>
            {
                if (!item.IsSelected && !container.IsMouseOver && !hoverToolbar.IsMouseOver)
                {
                    handleTL.Visibility = Visibility.Collapsed;
                    handleTR.Visibility = Visibility.Collapsed;
                    handleBL.Visibility = Visibility.Collapsed;
                    handleBR.Visibility = Visibility.Collapsed;

                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!item.IsSelected && !container.IsMouseOver && !hoverToolbar.IsMouseOver)
                        {
                            hoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    hoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            // In-frame double click to rename
            titleText.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    titleBox.Text = item.Title;
                    titleText.Visibility = Visibility.Collapsed;
                    titleBox.Visibility = Visibility.Visible;
                    titleBox.Focus();
                    titleBox.SelectAll();
                    e.Handled = true;
                }
            };

            void FinishRename()
            {
                if (titleBox.Visibility == Visibility.Visible)
                {
                    string newTitle = string.IsNullOrWhiteSpace(titleBox.Text) ? "Scene 01" : titleBox.Text.Trim();
                    item.Title = newTitle;
                    titleText.Text = newTitle;
                    titleBox.Visibility = Visibility.Collapsed;
                    titleText.Visibility = Visibility.Visible;
                    ScheduleAutoSave();
                }
            }

            titleBox.LostFocus += (s, e) => FinishRename();
            titleBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) FinishRename();
                else if (e.Key == Key.Escape)
                {
                    titleBox.Text = item.Title;
                    titleBox.Visibility = Visibility.Collapsed;
                    titleText.Visibility = Visibility.Visible;
                }
            };

            // Drag group + member cards by clicking on frameBorder or label
            MouseButtonEventHandler startGroupDrag = (s, e) =>
            {
                if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed)
                {
                    bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                    SelectGroup(item, addToSelection: isShift);

                    RecordUndo("Move Group");
                    _isDraggingGroup = true;
                    _draggingGroup = item;
                    _groupDragStartMousePoint = e.GetPosition(CanvasContainer);
                    _groupDragStartPos = new Point(item.X, item.Y);

                    _groupCardsInitialPositions.Clear();
                    foreach (var card in _cards.Where(c => c.GroupId == item.Id))
                    {
                        _groupCardsInitialPositions[card] = new Point(card.X, card.Y);
                    }

                    CanvasContainer.CaptureMouse();
                    e.Handled = true;
                }
            };

            frameBorder.MouseLeftButtonDown += startGroupDrag;
            labelSp.MouseLeftButtonDown += startGroupDrag;

            _groups.Add(item);
            WorldCanvas.Children.Add(container);

            UpdateGroupCounts();
            UpdateStorageStats();
            ScheduleAutoSave();

            return item;
        }

        private void SelectGroup(GroupItem group, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                DeselectAllCards();
                DeselectAllGroups();
            }

            group.IsSelected = true;
            _selectedGroups.Add(group);

            group.HandleTL.Visibility = Visibility.Visible;
            group.HandleTR.Visibility = Visibility.Visible;
            group.HandleBL.Visibility = Visibility.Visible;
            group.HandleBR.Visibility = Visibility.Visible;

            if (group.HoverToolbar != null)
            {
                group.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                group.HoverToolbar.Opacity = 1.0;
                group.HoverToolbar.IsHitTestVisible = true;
            }

            // Visual highlight for selected group frame
            group.FrameBorder.BorderThickness = new Thickness(2.5);
            try
            {
                Color c = (Color)ColorConverter.ConvertFromString(group.Color);
                group.FrameBorder.Background = new SolidColorBrush(Color.FromArgb(22, c.R, c.G, c.B));
            }
            catch { }
        }

        private void DeselectGroup(GroupItem group)
        {
            group.IsSelected = false;
            _selectedGroups.Remove(group);

            group.FrameBorder.BorderThickness = new Thickness(1.5);
            try
            {
                Color c = (Color)ColorConverter.ConvertFromString(group.Color);
                group.FrameBorder.Background = new SolidColorBrush(Color.FromArgb(8, c.R, c.G, c.B));
            }
            catch { }

            if (!group.Container.IsMouseOver)
            {
                group.HandleTL.Visibility = Visibility.Collapsed;
                group.HandleTR.Visibility = Visibility.Collapsed;
                group.HandleBL.Visibility = Visibility.Collapsed;
                group.HandleBR.Visibility = Visibility.Collapsed;

                if (group.HoverToolbar != null)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s, e) =>
                    {
                        if (!group.Container.IsMouseOver && !group.IsSelected)
                        {
                            group.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    group.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            }
        }

        private void DeselectAllGroups()
        {
            foreach (var g in _selectedGroups.ToList())
            {
                DeselectGroup(g);
            }
        }

        private Border CreateGroupHoverToolbar(GroupItem item)
        {
            Color grpCol;
            try { grpCol = (Color)ColorConverter.ConvertFromString(item.Color); }
            catch { grpCol = Color.FromRgb(59, 130, 246); }

            Border pill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 20, 24, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(6, 2, 6, 2),
                Opacity = 0.0,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 3,
                    Direction = 270,
                    Opacity = 0.6,
                    Color = Colors.Black
                }
            };
            Panel.SetZIndex(pill, 10001);

            pill.MouseEnter += (s, e) =>
            {
                pill.BeginAnimation(UIElement.OpacityProperty, null);
                pill.Opacity = 1.0;
                pill.IsHitTestVisible = true;
            };
            pill.MouseLeftButtonDown += (s, e) =>
            {
                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                SelectGroup(item, addToSelection: isShift);
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // 1. Move grip
            StackPanel moveSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            moveSp.Children.Add(new TextBlock
            {
                Text = "::",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            moveSp.Children.Add(new TextBlock
            {
                Text = "Move",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                VerticalAlignment = VerticalAlignment.Center
            });

            Border btnMove = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(6, 3, 6, 3),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.SizeAll,
                ToolTip = "Drag to Move Group & References",
                Child = moveSp
            };
            btnMove.MouseLeftButtonDown += (s, e) =>
            {
                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                SelectGroup(item, addToSelection: isShift);

                RecordUndo("Move Group");
                _isDraggingGroup = true;
                _draggingGroup = item;
                _groupDragStartMousePoint = e.GetPosition(CanvasContainer);
                _groupDragStartPos = new Point(item.X, item.Y);

                _groupCardsInitialPositions.Clear();
                foreach (var card in _cards.Where(c => c.GroupId == item.Id))
                {
                    _groupCardsInitialPositions[card] = new Point(card.X, card.Y);
                }

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            };
            sp.Children.Add(btnMove);

            Border CreateDiv() => new Border { Width = 1, Height = 12, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Margin = new Thickness(3, 0, 3, 0), VerticalAlignment = VerticalAlignment.Center };

            sp.Children.Add(CreateDiv());

            // 2. Title Label
            TextBlock titleLabel = new TextBlock
            {
                Text = item.Title,
                FontSize = 11.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            };
            sp.Children.Add(titleLabel);

            sp.Children.Add(CreateDiv());

            // 3. Auto-Fit Button (SVG)
            Border btnFit = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(5, 3, 5, 3),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                ToolTip = "Auto-Fit Group Frame to Member References",
                Child = CreateSvgIcon("M 15,3 h 6 v 6 M 9,21 H 3 v -6 M 21,3 L 14,10 M 3,21 L 10,14", "#38BDF8", 12)
            };
            btnFit.MouseEnter += (s, e) => btnFit.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            btnFit.MouseLeave += (s, e) => btnFit.Background = Brushes.Transparent;
            btnFit.MouseLeftButtonDown += (s, e) => { e.Handled = true; FitGroupToCards(item); };
            sp.Children.Add(btnFit);

            // 4. Tidy Grid Button (SVG)
            Border btnTidy = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(5, 3, 5, 3),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                ToolTip = "Tidy & Auto-arrange references in clean grid",
                Child = CreateSvgIcon("M 4,4 H 20 V 20 H 4 Z M 9,9 H 15 V 15 H 9 Z", "#A7F3D0", 12)
            };
            btnTidy.MouseEnter += (s, e) => btnTidy.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            btnTidy.MouseLeave += (s, e) => btnTidy.Background = Brushes.Transparent;
            btnTidy.MouseLeftButtonDown += (s, e) => { e.Handled = true; TidyGroup(item); };
            sp.Children.Add(btnTidy);

            sp.Children.Add(CreateDiv());

            // 5. Color Picker Dots
            string[] paletteColors = new[] { "#3b82f6", "#8b5cf6", "#ec4899", "#10b981", "#f59e0b" };
            StackPanel colorSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 4, 0) };
            foreach (string colHex in paletteColors)
            {
                Color c = (Color)ColorConverter.ConvertFromString(colHex);
                Border dot = new Border
                {
                    Width = 9,
                    Height = 9,
                    CornerRadius = new CornerRadius(4.5),
                    Background = new SolidColorBrush(c),
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Set group color to {colHex}"
                };
                dot.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    item.Color = colHex;
                    Color newC = (Color)ColorConverter.ConvertFromString(colHex);
                    item.FrameBorder.BorderBrush = new SolidColorBrush(newC);
                    item.FrameBorder.Background = new SolidColorBrush(Color.FromArgb(8, newC.R, newC.G, newC.B));
                    if (item.ColorDot != null) item.ColorDot.Background = new SolidColorBrush(newC);
                    if (item.HandleTL != null) item.HandleTL.BorderBrush = new SolidColorBrush(newC);
                    if (item.HandleTR != null) item.HandleTR.BorderBrush = new SolidColorBrush(newC);
                    if (item.HandleBL != null) item.HandleBL.BorderBrush = new SolidColorBrush(newC);
                    if (item.HandleBR != null) item.HandleBR.BorderBrush = new SolidColorBrush(newC);
                    ScheduleAutoSave();
                };
                colorSp.Children.Add(dot);
            }
            sp.Children.Add(colorSp);

            sp.Children.Add(CreateDiv());

            // 6. Delete Button (SVG)
            Border btnDel = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(5, 3, 5, 3),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                ToolTip = "Delete Group Frame (keeps reference cards on canvas)",
                Child = CreateSvgIcon("M 18,6 L 6,18 M 6,6 L 18,18", "#EF4444", 11.5, 2.0)
            };
            btnDel.MouseEnter += (s, e) => btnDel.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            btnDel.MouseLeave += (s, e) => btnDel.Background = Brushes.Transparent;
            btnDel.MouseLeftButtonDown += (s, e) => { e.Handled = true; RemoveGroup(item); };
            sp.Children.Add(btnDel);

            pill.Child = sp;
            return pill;
        }

        private void AttachGroupResizeHandleEvents(GroupItem group, Border handle, ResizeCorner corner)
        {
            handle.MouseLeftButtonDown += (s, e) =>
            {
                RecordUndo("Resize Group");

                _isResizingGroup = true;
                _resizingGroup = group;
                _activeGroupCorner = corner;
                _groupResizeStartMousePoint = e.GetPosition(CanvasContainer);
                _groupResizeInitialBounds = new Rect(group.X, group.Y, group.Width, group.Height);

                _groupResizeMemberCards.Clear();
                foreach (var card in _cards.Where(c => c.GroupId == group.Id))
                {
                    _groupResizeMemberCards.Add(new GroupResizeMemberState
                    {
                        Card = card,
                        InitWidth = card.Width,
                        InitHeight = card.Height,
                        RelX = card.X - group.X,
                        RelY = card.Y - group.Y
                    });
                }

                CanvasContainer.CaptureMouse();
                e.Handled = true;
            };
        }

        private void ApplyGroupResize(GroupItem group, ResizeCorner corner, Rect initial, double deltaX, double deltaY)
        {
            double newW = initial.Width;
            double newH = initial.Height;
            double newX = initial.X;
            double newY = initial.Y;

            switch (corner)
            {
                case ResizeCorner.BottomRight:
                    newW = Math.Max(140, initial.Width + deltaX);
                    newH = Math.Max(100, initial.Height + deltaY);
                    break;

                case ResizeCorner.BottomLeft:
                    newW = Math.Max(140, initial.Width - deltaX);
                    newH = Math.Max(100, initial.Height + deltaY);
                    newX = initial.Right - newW;
                    break;

                case ResizeCorner.TopRight:
                    newW = Math.Max(140, initial.Width + deltaX);
                    newH = Math.Max(100, initial.Height - deltaY);
                    newY = initial.Bottom - newH;
                    break;

                case ResizeCorner.TopLeft:
                    newW = Math.Max(140, initial.Width - deltaX);
                    newH = Math.Max(100, initial.Height - deltaY);
                    newX = initial.Right - newW;
                    newY = initial.Bottom - newH;
                    break;
            }

            group.X = newX;
            group.Y = newY;
            group.Width = newW;
            group.Height = newH;

            Canvas.SetLeft(group.Container, newX);
            Canvas.SetTop(group.Container, newY);
            group.Container.Width = newW;
            group.Container.Height = newH;
            group.FrameBorder.Width = newW;
            group.FrameBorder.Height = newH;

            // Dynamically reflow and fit all member cards into the new container dimensions!
            ReflowGroupCards(group, newW, newH, newX, newY, animated: false);
        }

        public void ReflowGroupCards(GroupItem group, double groupW, double groupH, double groupX, double groupY, bool animated = false)
        {
            var memberCards = _cards.Where(c => c.GroupId == group.Id).ToList();
            if (memberCards.Count == 0) return;

            double pad = 16.0;
            double topPad = 34.0;
            double gap = 12.0;

            double availW = Math.Max(80.0, groupW - (pad * 2));
            double availH = Math.Max(60.0, groupH - topPad - pad);

            var visualCards = memberCards.Where(c => !c.IsPaletteCard).ToList();
            var paletteCards = memberCards.Where(c => c.IsPaletteCard).ToList();

            double paletteRowH = 0;
            if (paletteCards.Count > 0)
            {
                paletteRowH = (paletteCards.Count * 76.0) + (paletteCards.Count * gap);
            }

            double visualAvailH = Math.Max(40.0, availH - paletteRowH);

            if (visualCards.Count > 0)
            {
                int bestCols = 1;
                double bestFitScore = double.MaxValue;
                int maxPossibleCols = Math.Min(visualCards.Count, 6);

                // Find the optimal column count that fits visual cards with minimal wasted vertical/horizontal ratio
                for (int c = 1; c <= maxPossibleCols; c++)
                {
                    int r = (int)Math.Ceiling((double)visualCards.Count / c);
                    double cellW = (availW - ((c - 1) * gap)) / c;
                    double cellH = (visualAvailH - ((r - 1) * gap)) / r;

                    if (cellW <= 20 || cellH <= 20) continue;

                    double cellAspect = cellW / Math.Max(1.0, cellH);
                    // Preference around 1.15 aspect ratio
                    double score = Math.Abs(Math.Log(cellAspect / 1.15));
                    if (score < bestFitScore)
                    {
                        bestFitScore = score;
                        bestCols = c;
                    }
                }

                int cols = bestCols;
                int rows = (int)Math.Ceiling((double)visualCards.Count / cols);

                double maxCellW = Math.Max(30.0, (availW - ((cols - 1) * gap)) / cols);
                double maxCellH = Math.Max(30.0, (visualAvailH - ((rows - 1) * gap)) / rows);

                for (int i = 0; i < visualCards.Count; i++)
                {
                    var card = visualCards[i];
                    int col = i % cols;
                    int row = i / cols;

                    double cellX = groupX + pad + (col * (maxCellW + gap));
                    double cellY = groupY + topPad + (row * (maxCellH + gap));

                    double aspect = card.AspectRatio > 0.05 ? card.AspectRatio : (card.Width / Math.Max(1.0, card.Height));
                    double cardW = maxCellW;
                    double cardH = Math.Round(cardW / aspect);

                    if (cardH > maxCellH)
                    {
                        cardH = maxCellH;
                        cardW = Math.Round(cardH * aspect);
                    }

                    double finalX = cellX + ((maxCellW - cardW) / 2.0);
                    double finalY = cellY + ((maxCellH - cardH) / 2.0);

                    card.Width = Math.Max(30, cardW);
                    card.Height = Math.Max(30, cardH);
                    if (card.IsCropped)
                    {
                        double visW_pct = Math.Max(0.05, (100.0 - card.CropLeft - card.CropRight) / 100.0);
                        double visH_pct = Math.Max(0.05, (100.0 - card.CropTop - card.CropBottom) / 100.0);
                        card.BaseWidth = Math.Round(card.Width / visW_pct);
                        card.BaseHeight = Math.Round(card.Height / visH_pct);
                    }
                    else
                    {
                        card.BaseWidth = card.Width;
                        card.BaseHeight = card.Height;
                    }

                    if (animated)
                    {
                        AnimateCardPosition(card, finalX, finalY);
                    }
                    else
                    {
                        card.X = finalX;
                        card.Y = finalY;
                    }
                }
            }

            // Position palette cards neatly below the visual cards in the group!
            if (paletteCards.Count > 0)
            {
                double curPalY = visualCards.Count > 0 
                    ? visualCards.Max(c => c.Y + c.Height) + gap 
                    : groupY + topPad;

                foreach (var pal in paletteCards)
                {
                    double palW = Math.Min(availW, Math.Max(220.0, availW * 0.95));
                    double palH = Math.Clamp(pal.Height > 0 ? pal.Height : 74.0, 50.0, 95.0);
                    pal.Width = palW;
                    pal.Height = palH;
                    pal.X = groupX + pad + ((availW - palW) / 2.0);
                    pal.Y = curPalY;
                    Panel.SetZIndex(pal.Container, 25);
                    curPalY += palH + gap;
                }
            }

            SyncActiveHwndPositions(updateSize: true);
        }

        public void FitGroupToCards(GroupItem group, bool recordUndo = true)
        {
            var memberCards = _cards.Where(c => c.GroupId == group.Id).ToList();
            if (memberCards.Count == 0)
            {
                if (recordUndo) ShowToast("Group is empty", ToastType.Info, 1500);
                return;
            }

            if (recordUndo) RecordUndo("Fit Group");

            double minX = memberCards.Min(c => c.X);
            double minY = memberCards.Min(c => c.Y);
            double maxX = memberCards.Max(c => c.X + c.Width);
            double maxY = memberCards.Max(c => c.Y + c.Height);

            double pad = 16.0;
            double topPad = 32.0;

            double newX = minX - pad;
            double newY = minY - topPad;
            double newW = Math.Max(200, (maxX - minX) + (pad * 2));
            double newH = Math.Max(140, (maxY - minY) + topPad + pad);

            group.X = newX;
            group.Y = newY;
            group.Width = newW;
            group.Height = newH;

            Canvas.SetLeft(group.Container, newX);
            Canvas.SetTop(group.Container, newY);
            group.Container.Width = newW;
            group.Container.Height = newH;
            group.FrameBorder.Width = newW;
            group.FrameBorder.Height = newH;

            UpdateGroupCounts();
            ScheduleAutoSave();
            if (recordUndo)
            {
                ShowToast($"Fitted {group.Title} to {memberCards.Count} reference(s)", ToastType.Success, 2000);
            }
        }

        public void AutoLayoutGroup(GroupItem group, bool animated = true, bool recordUndo = false)
        {
            var memberCards = _cards.Where(c => c.GroupId == group.Id && !c.IsPaletteCard).ToList();
            if (memberCards.Count == 0)
            {
                FitGroupToCards(group, recordUndo: false);
                return;
            }

            if (recordUndo) RecordUndo("Auto-Arrange Group");

            int count = memberCards.Count;
            double pad = 16.0;
            double topPad = 34.0;
            double gap = 14.0;

            if (count == 1)
            {
                FitGroupToCards(group, recordUndo: false);
                return;
            }

            // Target height for harmonious row / grid alignment
            double targetH = count switch
            {
                2 => 300.0,
                3 => 260.0,
                4 => 240.0,
                _ => 220.0
            };

            // Scale member cards to uniform target height while preserving natural aspect ratio
            foreach (var card in memberCards)
            {
                double aspect = card.AspectRatio > 0.05 ? card.AspectRatio : (card.Width / Math.Max(1.0, card.Height));
                double newH = targetH;
                double newW = Math.Round(newH * aspect);
                card.Width = newW;
                card.Height = newH;
                if (card.IsCropped)
                {
                    double visW_pct = Math.Max(0.05, (100.0 - card.CropLeft - card.CropRight) / 100.0);
                    double visH_pct = Math.Max(0.05, (100.0 - card.CropTop - card.CropBottom) / 100.0);
                    card.BaseWidth = Math.Round(newW / visW_pct);
                    card.BaseHeight = Math.Round(newH / visH_pct);
                }
                else
                {
                    card.BaseWidth = newW;
                    card.BaseHeight = newH;
                }
            }

            // Decide columns: 2 cards = 2 cols, 3 cards = 3 cols (if total width <= 960) else 2 cols, 4 cards = 2 cols
            int cols = count switch
            {
                2 => 2,
                3 => 3,
                4 => 2,
                5 or 6 => 3,
                _ => Math.Min(4, (int)Math.Ceiling(Math.Sqrt(count)))
            };

            if (cols == 3 && memberCards.Sum(c => c.Width) + (2 * gap) > 960)
            {
                cols = 2;
            }

            double startX = group.X + pad;
            double startY = group.Y + topPad;

            // Track column X positions and running Y heights
            double[] colWidths = new double[cols];
            for (int i = 0; i < count; i++)
            {
                int c = i % cols;
                colWidths[c] = Math.Max(colWidths[c], memberCards[i].Width);
            }

            double[] colX = new double[cols];
            double[] colY = new double[cols];

            colX[0] = startX;
            for (int c = 1; c < cols; c++)
            {
                colX[c] = colX[c - 1] + colWidths[c - 1] + gap;
            }
            for (int c = 0; c < cols; c++)
            {
                colY[c] = startY;
            }

            for (int i = 0; i < count; i++)
            {
                var card = memberCards[i];
                int c = i % cols;

                double targetX = colX[c];
                double targetY = colY[c];

                if (animated)
                {
                    AnimateCardPosition(card, targetX, targetY);
                }
                else
                {
                    card.X = targetX;
                    card.Y = targetY;
                }

                colY[c] += card.Height + gap;
            }

            double totalW = (colX[cols - 1] + colWidths[cols - 1] + pad) - group.X;
            double finalBottomY = colY.Max() - gap;

            var paletteCards = _cards.Where(c => c.GroupId == group.Id && c.IsPaletteCard).ToList();
            if (paletteCards.Count > 0)
            {
                double curPalY = finalBottomY + gap;
                double palWidth = Math.Max(220.0, totalW - (pad * 2));
                foreach (var pal in paletteCards)
                {
                    double palH = Math.Clamp(pal.Height > 0 ? pal.Height : 74.0, 50.0, 95.0);
                    pal.Width = palWidth;
                    pal.Height = palH;
                    if (animated)
                    {
                        AnimateCardPosition(pal, startX, curPalY);
                    }
                    else
                    {
                        pal.X = startX;
                        pal.Y = curPalY;
                    }
                    Panel.SetZIndex(pal.Container, 25);
                    curPalY += palH + gap;
                }
                finalBottomY = curPalY - gap;
            }

            double maxColH = finalBottomY + pad - group.Y;

            group.Width = Math.Max(200, totalW);
            group.Height = Math.Max(140, maxColH);

            group.Container.Width = group.Width;
            group.Container.Height = group.Height;
            if (group.FrameBorder != null)
            {
                group.FrameBorder.Width = group.Width;
                group.FrameBorder.Height = group.Height;
            }

            UpdateGroupCounts();
            ScheduleAutoSave();
            if (recordUndo)
            {
                ShowToast($"Auto-arranged {count} references in {group.Title}", ToastType.Success, 2000);
            }
        }

        public void TidyGroup(GroupItem group)
        {
            AutoLayoutGroup(group, animated: true, recordUndo: true);
        }

        public void RemoveGroup(GroupItem group)
        {
            RecordUndo("Delete Group");
            foreach (var card in _cards.Where(c => c.GroupId == group.Id))
            {
                card.GroupId = null;
            }
            WorldCanvas.Children.Remove(group.Container);
            _groups.Remove(group);
            _selectedGroups.Remove(group);
            UpdateGroupCounts();
            UpdateStorageStats();
            ScheduleAutoSave();
            ShowToast($"Deleted group: {group.Title}", ToastType.Info);
        }

        public void UpdateGroupCounts()
        {
            foreach (var group in _groups)
            {
                int count = _cards.Count(c => c.GroupId == group.Id);
                if (group.CountBadge != null)
                {
                    group.CountBadge.Text = $"({count} {(count == 1 ? "ref" : "refs")})";
                }
            }
        }

        private void CheckCardGroupAffiliation(CardItem card)
        {
            Rect cardRect = new Rect(card.X, card.Y, card.Width, card.Height);
            Point center = new Point(card.X + card.Width / 2, card.Y + card.Height / 2);
            GroupItem? targetGroup = null;

            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                var g = _groups[i];
                Rect groupRect = new Rect(g.X, g.Y, g.Width, g.Height);
                if (groupRect.Contains(center) || groupRect.IntersectsWith(cardRect))
                {
                    targetGroup = g;
                    break;
                }
            }

            if (targetGroup != null)
            {
                string? prevGroupId = card.GroupId;
                card.GroupId = targetGroup.Id;

                if (card.IsPaletteCard || card.IsNote)
                {
                    Panel.SetZIndex(card.Container, 25);
                    var otherCards = _cards.Where(c => c.GroupId == targetGroup.Id && c != card).ToList();
                    if (otherCards.Count > 0)
                    {
                        Rect cardRectExact = new Rect(card.X, card.Y, card.Width, card.Height);
                        bool overlaps = otherCards.Any(other =>
                        {
                            Rect otherRect = new Rect(other.X, other.Y, other.Width, other.Height);
                            return otherRect.IntersectsWith(cardRectExact);
                        });

                        if (overlaps || prevGroupId != targetGroup.Id)
                        {
                            double bottomY = otherCards.Max(o => o.Y + o.Height);
                            card.Y = bottomY + 14.0;
                            card.X = otherCards.Min(o => o.X);
                        }
                    }
                    FitGroupToCards(targetGroup, recordUndo: false);
                }
                else
                {
                    var memberCards = _cards.Where(c => c.GroupId == targetGroup.Id && !c.IsPaletteCard).ToList();
                    if (memberCards.Count >= 2)
                    {
                        // Check if newly added/moved card overlaps with existing cards
                        Rect cardRectExact = new Rect(card.X, card.Y, card.Width, card.Height);
                        bool overlaps = memberCards.Where(c => c != card).Any(other =>
                        {
                            Rect otherRect = new Rect(other.X, other.Y, other.Width, other.Height);
                            return otherRect.IntersectsWith(cardRectExact);
                        });

                        if (overlaps || prevGroupId != targetGroup.Id)
                        {
                            AutoLayoutGroup(targetGroup, animated: true, recordUndo: false);
                        }
                        else
                        {
                            FitGroupToCards(targetGroup, recordUndo: false);
                        }
                    }
                    else
                    {
                        FitGroupToCards(targetGroup, recordUndo: false);
                    }
                }

                if (!string.IsNullOrEmpty(prevGroupId) && prevGroupId != targetGroup.Id)
                {
                    var prevG = _groups.FirstOrDefault(g => g.Id == prevGroupId);
                    if (prevG != null) FitGroupToCards(prevG, recordUndo: false);
                }

                UpdateGroupCounts();
            }
            else
            {
                if (card.GroupId != null)
                {
                    string oldGroupId = card.GroupId;
                    card.GroupId = null;
                    var oldGroup = _groups.FirstOrDefault(g => g.Id == oldGroupId);
                    if (oldGroup != null)
                    {
                        FitGroupToCards(oldGroup, recordUndo: false);
                    }
                    UpdateGroupCounts();
                }
            }
        }

        #endregion

        #region About & Settings Modals

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseAboutModal_Click(object sender, RoutedEventArgs e)
        {
            AboutModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ChkAutoSave.IsChecked = _settings.AutoSaveEnabled;
            TxtAutoSaveStatus.Text = _settings.AutoSaveEnabled ? "Status: Auto-Save Active" : "Status: Auto-Save Paused";
            TxtAutoSaveStatus.Foreground = new SolidColorBrush(_settings.AutoSaveEnabled ? Color.FromRgb(52, 211, 153) : Color.FromRgb(248, 113, 113));

            SliderPanSens.Value = _settings.PanSensitivity;
            TxtPanSens.Text = $"{_settings.PanSensitivity:0.0}x";

            SliderZoomSens.Value = _settings.ZoomSensitivity;
            TxtZoomSens.Text = $"{_settings.ZoomSensitivity:0.0}x";

            ChkInvertPan.IsChecked = _settings.InvertPan;

            ChkAutoHideDock.IsChecked = _settings.AutoHideDock;
            if (ChkTransparentTitlebar != null)
                ChkTransparentTitlebar.IsChecked = _settings.TransparentTitlebar;

            UpdateNavModeVisuals(_settings.NavMode);
            UpdateDockPosVisuals(_settings.DockPosition);
            UpdateStorageStats();

            SettingsModalOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseSettingsModal_Click(object sender, RoutedEventArgs e)
        {
            SettingsModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void ModalBackdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                AboutModalOverlay.Visibility = Visibility.Collapsed;
                SettingsModalOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void SettingsTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tabName)
            {
                PaneGeneral.Visibility = tabName == "General" ? Visibility.Visible : Visibility.Collapsed;
                PaneNavigation.Visibility = tabName == "Navigation" ? Visibility.Visible : Visibility.Collapsed;
                PaneToolbar.Visibility = tabName == "Toolbar" ? Visibility.Visible : Visibility.Collapsed;
                PaneShortcuts.Visibility = tabName == "Shortcuts" ? Visibility.Visible : Visibility.Collapsed;
                PaneStorage.Visibility = tabName == "Storage" ? Visibility.Visible : Visibility.Collapsed;
                PaneAbout.Visibility = tabName == "About" ? Visibility.Visible : Visibility.Collapsed;

                Button[] allTabs = new[] { TabBtnGeneral, TabBtnNav, TabBtnToolbar, TabBtnShortcuts, TabBtnStorage, TabBtnAbout };
                foreach (var t in allTabs)
                {
                    if (t == null) continue;
                    bool isActive = (t.Tag as string) == tabName;
                    t.Background = isActive ? new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)) : Brushes.Transparent;
                    t.Foreground = isActive ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                }
            }
        }

        private void SettingAutoSave_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkAutoSave == null || TxtAutoSaveStatus == null) return;
            _settings.AutoSaveEnabled = ChkAutoSave.IsChecked == true;
            _settings.Save();
            TxtAutoSaveStatus.Text = _settings.AutoSaveEnabled ? "Status: Auto-Save Active" : "Status: Auto-Save Paused";
            TxtAutoSaveStatus.Foreground = new SolidColorBrush(_settings.AutoSaveEnabled ? Color.FromRgb(52, 211, 153) : Color.FromRgb(248, 113, 113));
            ShowToast(_settings.AutoSaveEnabled ? "Auto-Save Enabled" : "Auto-Save Paused (Manual Save Only)", ToastType.Info);
        }

        private void BtnSaveProject_Click(object sender, RoutedEventArgs e)
        {
            SaveProject();
        }

        private void SelectNavMode_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string mode)
            {
                _settings.NavMode = mode;
                _settings.Save();
                UpdateNavModeVisuals(mode);
                ShowToast($"Navigation Mode: {mode.ToUpper()}", ToastType.Info);
            }
        }

        private void UpdateNavModeVisuals(string mode)
        {
            if (CardNavMac == null || CardNavWin == null || CardNavMouse == null) return;
            var activeBorder = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            var inactiveBorder = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            var activeText = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            var inactiveText = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            var inactiveIcon = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            bool isMac = mode == "macos";
            CardNavMac.BorderBrush = isMac ? activeBorder : inactiveBorder;
            CardNavMac.BorderThickness = new Thickness(isMac ? 1.5 : 1.0);
            if (TxtNavMacTitle != null) TxtNavMacTitle.Foreground = isMac ? activeText : inactiveText;
            if (IconNavMacScreen != null) IconNavMacScreen.Stroke = isMac ? activeText : inactiveIcon;
            if (IconNavMacBase != null) IconNavMacBase.Stroke = isMac ? activeText : inactiveIcon;
            if (IconNavMacPad != null) IconNavMacPad.Stroke = isMac ? activeText : inactiveIcon;

            bool isWin = mode == "windows";
            CardNavWin.BorderBrush = isWin ? activeBorder : inactiveBorder;
            CardNavWin.BorderThickness = new Thickness(isWin ? 1.5 : 1.0);
            if (TxtNavWinTitle != null) TxtNavWinTitle.Foreground = isWin ? activeText : inactiveText;
            if (IconNavWinPath != null) IconNavWinPath.Fill = isWin ? activeText : inactiveIcon;

            bool isMouse = mode == "mouse";
            CardNavMouse.BorderBrush = isMouse ? activeBorder : inactiveBorder;
            CardNavMouse.BorderThickness = new Thickness(isMouse ? 1.5 : 1.0);
            if (TxtNavMouseTitle != null) TxtNavMouseTitle.Foreground = isMouse ? activeText : inactiveText;
            if (IconNavMouseBody != null) IconNavMouseBody.Stroke = isMouse ? activeText : inactiveIcon;
            if (IconNavMouseWheel != null) IconNavMouseWheel.Stroke = isMouse ? activeText : inactiveIcon;
            if (IconNavMouseDiv != null) IconNavMouseDiv.Stroke = isMouse ? activeText : inactiveIcon;
        }

        private void SliderPanSens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtPanSens == null) return;
            _settings.PanSensitivity = e.NewValue;
            TxtPanSens.Text = $"{e.NewValue:0.0}x";
            _settings.Save();
        }

        private void SliderZoomSens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtZoomSens == null) return;
            _settings.ZoomSensitivity = e.NewValue;
            TxtZoomSens.Text = $"{e.NewValue:0.0}x";
            _settings.Save();
        }

        private void ChkInvertPan_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkInvertPan == null) return;
            _settings.InvertPan = ChkInvertPan.IsChecked == true;
            _settings.Save();
            ShowToast(_settings.InvertPan ? "Invert Pan: On" : "Invert Pan: Off", ToastType.Info);
        }

        private void SelectDockPos_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string pos)
            {
                _dockPosition = pos;
                _settings.DockPosition = pos;
                _settings.Save();
                ApplyDockLayout();
                UpdateDockAutoHideUI();
                UpdateDockPosVisuals(pos);
                ShowToast(pos == "left" ? "Toolbar: Left Sidebar" : "Toolbar: Top Bar", ToastType.Info);
            }
        }

        private void UpdateDockPosVisuals(string pos)
        {
            if (CardDockTop == null || CardDockLeft == null) return;
            var activeBorder = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            var inactiveBorder = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            var activeText = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            var inactiveText = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            var inactiveIcon = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            bool isTop = pos == "top";
            CardDockTop.BorderBrush = isTop ? activeBorder : inactiveBorder;
            CardDockTop.BorderThickness = new Thickness(isTop ? 1.5 : 1.0);
            if (TxtDockTopTitle != null) TxtDockTopTitle.Foreground = isTop ? activeText : inactiveText;
            if (IconDockTopWin != null) IconDockTopWin.Stroke = isTop ? activeText : inactiveIcon;
            if (IconDockTopBar != null) IconDockTopBar.Fill = isTop ? activeText : inactiveIcon;

            bool isLeft = pos == "left";
            CardDockLeft.BorderBrush = isLeft ? activeBorder : inactiveBorder;
            CardDockLeft.BorderThickness = new Thickness(isLeft ? 1.5 : 1.0);
            if (TxtDockLeftTitle != null) TxtDockLeftTitle.Foreground = isLeft ? activeText : inactiveText;
            if (IconDockLeftWin != null) IconDockLeftWin.Stroke = isLeft ? activeText : inactiveIcon;
            if (IconDockLeftBar != null) IconDockLeftBar.Fill = isLeft ? activeText : inactiveIcon;
        }

        private void ChkAutoHideDock_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkAutoHideDock == null) return;
            _isDockAutoHide = ChkAutoHideDock.IsChecked == true;
            _settings.AutoHideDock = _isDockAutoHide;
            _settings.Save();
            UpdateDockAutoHideUI();
            ShowToast(_isDockAutoHide ? "Toolbar: Auto-Hide (Float)" : "Toolbar: Pinned", ToastType.Info);
        }

        private void BtnTitlebarGlass_Click(object sender, RoutedEventArgs e)
        {
            bool newState = !_settings.TransparentTitlebar;
            _settings.TransparentTitlebar = newState;
            _settings.Save();
            ApplyTransparentTitlebar(newState);
            ShowToast(newState ? "Top Bar: Transparent Glass Active" : "Top Bar: Solid Studio Dark", ToastType.Info);
        }

        private void MenuTransparentTopBar_Click(object sender, RoutedEventArgs e)
        {
            if (MenuTransparentTopBar == null) return;
            bool newState = MenuTransparentTopBar.IsChecked;
            _settings.TransparentTitlebar = newState;
            _settings.Save();
            ApplyTransparentTitlebar(newState);
            ShowToast(newState ? "Top Bar: Transparent Glass Active" : "Top Bar: Solid Studio Dark", ToastType.Info);
        }

        private void ChkTransparentTitlebar_Changed(object sender, RoutedEventArgs e)
        {
            if (ChkTransparentTitlebar == null) return;
            bool isTrans = ChkTransparentTitlebar.IsChecked == true;
            if (_settings.TransparentTitlebar == isTrans) return;
            _settings.TransparentTitlebar = isTrans;
            _settings.Save();
            ApplyTransparentTitlebar(isTrans);
            ShowToast(isTrans ? "Top Bar: Transparent Glass Active" : "Top Bar: Solid Studio Dark", ToastType.Info);
        }

        private void ApplyTransparentTitlebar(bool isTransparent)
        {
            if (TopTitleBar == null) return;
            if (isTransparent)
            {
                TopTitleBar.Background = Brushes.Transparent;
                TopTitleBar.BorderThickness = new Thickness(0);
            }
            else
            {
                TopTitleBar.Background = new SolidColorBrush(Color.FromRgb(13, 15, 20)); // #FF0D0F14
                TopTitleBar.BorderThickness = new Thickness(0, 0, 0, 1);
            }

            // Update top bar quick toggle button visuals
            if (BtnTitlebarGlass != null)
            {
                var cyanBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                var slateBrush = new SolidColorBrush(Color.FromRgb(156, 163, 175));

                if (isTransparent)
                {
                    BtnTitlebarGlass.Background = new SolidColorBrush(Color.FromArgb(38, 56, 189, 248)); // #2638BDF8
                    BtnTitlebarGlass.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248));
                    BtnTitlebarGlass.ToolTip = "Top Bar: Transparent Glass (Click for Solid Dark)";
                    if (GlassWinRect != null) GlassWinRect.Stroke = cyanBrush;
                    if (GlassWinLine != null) GlassWinLine.Stroke = cyanBrush;
                    if (GlassWinHeader != null) GlassWinHeader.Fill = new SolidColorBrush(Color.FromArgb(60, 56, 189, 248));
                    if (GlassDot1 != null) GlassDot1.Fill = cyanBrush;
                    if (GlassDot2 != null) GlassDot2.Fill = cyanBrush;
                }
                else
                {
                    BtnTitlebarGlass.Background = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)); // #0AFFFFFF
                    BtnTitlebarGlass.BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
                    BtnTitlebarGlass.ToolTip = "Top Bar: Solid Studio Dark (Click for Transparent Glass)";
                    if (GlassWinRect != null) GlassWinRect.Stroke = slateBrush;
                    if (GlassWinLine != null) GlassWinLine.Stroke = slateBrush;
                    if (GlassWinHeader != null) GlassWinHeader.Fill = Brushes.Transparent;
                    if (GlassDot1 != null) GlassDot1.Fill = slateBrush;
                    if (GlassDot2 != null) GlassDot2.Fill = slateBrush;
                }
            }

            if (MenuTransparentTopBar != null)
                MenuTransparentTopBar.IsChecked = isTransparent;

            if (ChkTransparentTitlebar != null && ChkTransparentTitlebar.IsChecked != isTransparent)
                ChkTransparentTitlebar.IsChecked = isTransparent;
        }

        public void OpenPalettesFolder()
        {
            try
            {
                Directory.CreateDirectory(PalettesDir);

                // Auto-migrate any existing palettes from Windows Temp so user never loses their files
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DropBoard_Palettes");
                if (Directory.Exists(tempDir))
                {
                    foreach (var file in Directory.GetFiles(tempDir, "*.png"))
                    {
                        string dest = System.IO.Path.Combine(PalettesDir, System.IO.Path.GetFileName(file));
                        if (!File.Exists(dest))
                        {
                            try { File.Copy(file, dest); } catch { }
                        }
                    }
                }

                Process.Start(new ProcessStartInfo("explorer.exe", PalettesDir) { UseShellExecute = true });
                ShowToast("Opened Palettes folder in Explorer", ToastType.Info);
            }
            catch (Exception ex)
            {
                ShowToast("Could not open folder: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnOpenPalettesFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenPalettesFolder();
        }

        private void BtnOpenCacheFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(CacheDir);
                Process.Start(new ProcessStartInfo("explorer.exe", CacheDir) { UseShellExecute = true });
                ShowToast("Opened cache folder in Explorer", ToastType.Info);
            }
            catch (Exception ex)
            {
                ShowToast("Could not open cache: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnClearCacheStorage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(CacheDir))
                {
                    var files = Directory.GetFiles(CacheDir);
                    foreach (var f in files)
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
                UpdateStorageStats();
                ShowToast("Cache storage cleared", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Error clearing cache: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnCanvasQuickCleanCache_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            try
            {
                if (Directory.Exists(CacheDir))
                {
                    var di = new DirectoryInfo(CacheDir);
                    long totalBytes = 0;
                    int count = 0;
                    foreach (var file in di.GetFiles())
                    {
                        try
                        {
                            totalBytes += file.Length;
                            file.Delete();
                            count++;
                        }
                        catch { }
                    }
                    double freedMb = totalBytes / (1024.0 * 1024.0);
                    foreach (var c in _cards)
                    {
                        if (!string.IsNullOrEmpty(c.LocalPath) && c.LocalPath.StartsWith(CacheDir, StringComparison.OrdinalIgnoreCase))
                        {
                            c.LocalPath = "";
                        }
                    }
                    UpdateStorageStats();
                    ShowToast($"Cache cleaned ({count} files, {freedMb:F1} MB freed)", ToastType.Success, 2800);
                }
                else
                {
                    ShowToast("Cache is already empty", ToastType.Info, 2000);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to clean cache: {ex.Message}", ToastType.Error);
            }
        }

        private void UpdateStorageStats()
        {
            if (TxtStatCards != null) TxtStatCards.Text = _cards.Count.ToString();
            if (TxtStatGroups != null) TxtStatGroups.Text = _groups.Count.ToString();
            if (TxtStatNotes != null) TxtStatNotes.Text = _cards.Count(c => c.IsNote || c.IsPaletteCard).ToString();

            try
            {
                if (Directory.Exists(CacheDir))
                {
                    var di = new DirectoryInfo(CacheDir);
                    long totalBytes = di.EnumerateFiles().Sum(f => f.Length);
                    double mb = totalBytes / (1024.0 * 1024.0);
                    if (TxtCanvasCacheSize != null)
                    {
                        TxtCanvasCacheSize.Text = mb < 0.1 && totalBytes > 0 ? "Cache: <0.1 MB" : $"Cache: {mb:F1} MB";
                    }
                }
                else if (TxtCanvasCacheSize != null)
                {
                    TxtCanvasCacheSize.Text = "Cache: 0 MB";
                }
            }
            catch { }
        }

        private void BtnOpenX_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://x.com/migi_gn") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowToast("Could not open browser: " + ex.Message, ToastType.Error);
            }
        }

        private void BtnCopyEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText("gnmigi@gmail.com");
                ShowToast("Email copied: gnmigi@gmail.com", ToastType.Success);
            }
            catch
            {
                ShowToast("gnmigi@gmail.com", ToastType.Info);
            }
        }

        private void BtnRefreshAssoc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.dropboard"))
                    {
                        key.SetValue("", "DropBoard.Project");
                    }
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\DropBoard.Project\DefaultIcon"))
                    {
                        key.SetValue("", $"{exePath},0");
                    }
                    using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\DropBoard.Project\shell\open\command"))
                    {
                        key.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
                ShowToast("File association (.dropboard) refreshed!", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Assoc error: " + ex.Message, ToastType.Error);
            }
        }

        #endregion

        private void BtnAddNode_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Production Nodes (Note, Typography, VFX) will be available in Fase 2!", "DropBoard Nodes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void BtnAeExport_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCards.Count > 0)
            {
                ExportCardsToAe(_selectedCards);
            }
            else if (_cards.Count > 0)
            {
                ExportCardsToAe(_cards);
            }
            else
            {
                ShowToast("Please add or select reference images to export to After Effects!", ToastType.Info);
            }
        }

        private void BtnPsOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCards.Count > 0)
            {
                SendCardToPs(_selectedCards.First());
            }
            else if (_cards.Count > 0)
            {
                SendCardToPs(_cards.First());
            }
            else
            {
                ShowToast("Please add or select an image to open in Photoshop!", ToastType.Info);
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCards.Count > 0)
            {
                CardItem first = _selectedCards.First();
                Clipboard.SetImage(first.Bitmap);
                ShowToast("Copied image to clipboard!", ToastType.Success);
            }
        }

        private void BtnFloatToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDockAutoHide = !_isDockAutoHide;
            _settings.AutoHideDock = _isDockAutoHide;
            _settings.Save();
            UpdateDockAutoHideUI();
            ShowToast(_isDockAutoHide ? "Toolbar set to Auto-Hide (Float)" : "Toolbar Pinned (Always Visible)", ToastType.Info);
        }

        private void ClearCanvasItems()
        {
            foreach (CardItem card in _cards)
            {
                if (card.PlayerControl != null)
                {
                    try { card.PlayerControl.Dispose(); } catch { }
                    card.PlayerControl = null;
                }
                WorldCanvas.Children.Remove(card.Container);
            }
            _cards.Clear();
            _selectedCards.Clear();

            foreach (GroupItem group in _groups)
            {
                WorldCanvas.Children.Remove(group.Container);
            }
            _groups.Clear();
            _selectedGroups.Clear();

            UpdateStatusCounts();
            UpdateGroupCounts();
            UpdateStorageStats();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            if (_cards.Count == 0) return;
            if (MessageBox.Show("Clear all reference cards from board?", "DropBoard Clear", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RecordUndo("Clear Board");
                ClearCanvasItems();
                EmptyStateOverlay.Visibility = Visibility.Visible;
                ScheduleAutoSave();
                ShowToast("Canvas cleared", ToastType.Info);
            }
        }

        #region Undo / Redo & ContextMenu Engine

        private CanvasSnapshot CreateCurrentSnapshot(string actionName)
        {
            return new CanvasSnapshot
            {
                ActionName = actionName,
                MatrixM11 = CanvasMatrixTransform.Matrix.M11,
                MatrixOffsetX = CanvasMatrixTransform.Matrix.OffsetX,
                MatrixOffsetY = CanvasMatrixTransform.Matrix.OffsetY,
                Groups = _groups.Select(g => new GroupSnapshot
                {
                    Id = g.Id,
                    Title = g.Title,
                    Color = g.Color,
                    Notes = g.Notes,
                    X = g.X,
                    Y = g.Y,
                    Width = g.Width,
                    Height = g.Height
                }).ToList(),
                Cards = _cards.Select(c => new CardSnapshot
                {
                    Id = c.Id,
                    GroupId = c.GroupId,
                    X = c.X,
                    Y = c.Y,
                    Width = c.Width,
                    Height = c.Height,
                    LocalPath = c.LocalPath,
                    Base64Data = c.Base64Data,
                    Bitmap = c.Bitmap,
                    OriginalBitmap = c.OriginalBitmap ?? c.Bitmap,
                    BaseWidth = c.BaseWidth,
                    BaseHeight = c.BaseHeight,
                    BaseX = c.BaseX,
                    BaseY = c.BaseY,
                    CropTop = c.CropTop,
                    CropRight = c.CropRight,
                    CropBottom = c.CropBottom,
                    CropLeft = c.CropLeft,
                    IsYouTube = c.IsYouTube,
                    YouTubeId = c.YouTubeId,
                    YouTubeUrl = c.YouTubeUrl,
                    IsLocalVideo = c.IsLocalVideo,
                    VideoFilePath = c.VideoFilePath,
                    IsVideoLooping = c.IsVideoLooping,
                    IsVideoMuted = c.IsVideoMuted,
                    VideoDurationSeconds = c.VideoDurationSeconds,
                    IsNote = c.IsNote,
                    NoteText = c.NoteText,
                    NoteFontFamily = c.NoteFontFamily,
                    NoteFontSize = c.NoteFontSize,
                    NoteTextColor = c.NoteTextColor,
                    NoteBgColor = c.NoteBgColor,
                    NoteAlignment = c.NoteAlignment,
                    NoteHasShadow = c.NoteHasShadow,
                    HasDeadline = c.HasDeadline,
                    DeadlineIso = c.DeadlineDateTime?.ToString("o"),
                    DeadlineLabel = c.DeadlineLabel,
                    IsChecklist = c.IsChecklist,
                    ChecklistJson = c.IsChecklist && c.ChecklistItems != null ? JsonSerializer.Serialize(c.ChecklistItems) : "",
                    NoteDoodleInkBase64 = c.NoteDoodleInkBase64,
                    NoteBgGifPath = c.NoteBgGifPath,
                    NoteBgGifBase64 = c.NoteBgGifBase64,
                    IsPaletteCard = c.IsPaletteCard,
                    PaletteColorCount = c.PaletteColorCount,
                    PaletteMood = c.PaletteMood,
                    PaletteRows = c.PaletteRows,
                    LinkedSourceCardId = c.IsPaletteCard ? c.LinkedSourceImageCard?.Id : null,
                    PalettePinsData = c.IsPaletteCard && c.ActivePalettePins != null && c.ActivePalettePins.Count > 0
                        ? JsonSerializer.Serialize(c.ActivePalettePins) : "",
                    IsDrawCard = c.IsDrawCard,
                    DrawInkBase64 = c.DrawInkBase64,
                    DrawPenColor = c.DrawPenColor,
                    DrawPenSize = c.DrawPenSize,
                    DrawIsEraser = c.DrawIsEraser
                }).ToList()
            };
        }

        private void RecordUndo(string actionName)
        {
            if (_isApplyingSnapshot || _isRestoringSession) return;

            var snap = CreateCurrentSnapshot(actionName);
            _undoStack.Push(snap);
            if (_undoStack.Count > 30)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                for (int i = list.Count - 1; i >= 0; i--) _undoStack.Push(list[i]);
            }
            _redoStack.Clear();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;

            var currentSnap = CreateCurrentSnapshot("Current");
            _redoStack.Push(currentSnap);

            var snap = _undoStack.Pop();
            ApplySnapshot(snap);
            ShowToast($"Undo: {snap.ActionName}", ToastType.Info);
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            var currentSnap = CreateCurrentSnapshot("Current");
            _undoStack.Push(currentSnap);

            var snap = _redoStack.Pop();
            ApplySnapshot(snap);
            ShowToast($"Redo: {snap.ActionName}", ToastType.Info);
        }

        private void ApplySnapshot(CanvasSnapshot snap)
        {
            _isApplyingSnapshot = true;
            try
            {
                var snapCardIds = snap.Cards.Select(c => c.Id).ToHashSet();

                // 1. Remove only cards that no longer exist in this snapshot
                for (int i = _cards.Count - 1; i >= 0; i--)
                {
                    var card = _cards[i];
                    if (!snapCardIds.Contains(card.Id))
                    {
                        if (card.PlayerControl != null)
                        {
                            try { card.PlayerControl.Dispose(); } catch { }
                            card.PlayerControl = null;
                        }
                        WorldCanvas.Children.Remove(card.Container);
                        _cards.RemoveAt(i);
                        _selectedCards.Remove(card);
                    }
                }

                var existingMap = _cards.ToDictionary(c => c.Id);

                // 2. Reconcile existing cards (smoothly restore pos/size) or create new ones
                foreach (var cs in snap.Cards)
                {
                    if (existingMap.TryGetValue(cs.Id, out var existingCard))
                    {
                        // Existing card: restore position and dimensions WITHOUT rebuilding or pausing YouTube!
                        existingCard.X = cs.X;
                        existingCard.Y = cs.Y;
                        existingCard.Width = cs.Width;
                        existingCard.Height = cs.Height;
                        existingCard.BaseX = cs.BaseX;
                        existingCard.BaseY = cs.BaseY;
                        existingCard.BaseWidth = cs.BaseWidth;
                        existingCard.BaseHeight = cs.BaseHeight;
                        existingCard.GroupId = cs.GroupId;

                        if (existingCard.IsPaletteCard)
                        {
                            existingCard.PaletteMood = cs.PaletteMood;
                            existingCard.PaletteColorCount = cs.PaletteColorCount;
                            existingCard.PaletteRows = cs.PaletteRows;
                            if (!string.IsNullOrEmpty(cs.LinkedSourceCardId) && existingMap.TryGetValue(cs.LinkedSourceCardId, out var linkedSrc))
                            {
                                existingCard.LinkedSourceImageCard = linkedSrc;
                                linkedSrc.LinkedPaletteCard = existingCard;
                            }
                            if (!string.IsNullOrEmpty(cs.PalettePinsData))
                            {
                                try
                                {
                                    existingCard.ActivePalettePins = JsonSerializer.Deserialize<List<PalettePin>>(cs.PalettePinsData) ?? new();
                                    UpdatePaletteCardContent(existingCard);
                                }
                                catch { }
                            }
                        }
                        else if (existingCard.IsNote)
                        {
                            existingCard.NoteText = cs.NoteText;
                            if (existingCard.NoteEditor != null)
                            {
                                existingCard.NoteEditor.Text = cs.NoteText;
                            }
                            existingCard.NoteHasShadow = cs.NoteHasShadow;
                            ApplyNoteFontFamily(existingCard, cs.NoteFontFamily);
                            ApplyNoteFontSize(existingCard, cs.NoteFontSize);
                            ApplyNoteAlignment(existingCard, cs.NoteAlignment);
                            ApplyNoteTextColor(existingCard, cs.NoteTextColor);
                            ApplyNoteBackground(existingCard, cs.NoteBgColor);
                            ApplyNoteShadow(existingCard);

                            // Restore Deadline
                            existingCard.HasDeadline = cs.HasDeadline;
                            existingCard.DeadlineLabel = cs.DeadlineLabel;
                            if (cs.HasDeadline && !string.IsNullOrEmpty(cs.DeadlineIso) && DateTime.TryParse(cs.DeadlineIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDl))
                            {
                                existingCard.DeadlineDateTime = parsedDl;
                            }
                            else
                            {
                                existingCard.DeadlineDateTime = null;
                            }
                            UpdateNoteDeadlineBadgeUI(existingCard);

                            // Restore Checklist
                            existingCard.IsChecklist = cs.IsChecklist;
                            if (cs.IsChecklist && !string.IsNullOrEmpty(cs.ChecklistJson))
                            {
                                try
                                {
                                    existingCard.ChecklistItems = JsonSerializer.Deserialize<List<NoteChecklistItem>>(cs.ChecklistJson) ?? new List<NoteChecklistItem>();
                                }
                                catch
                                {
                                    existingCard.ChecklistItems = new List<NoteChecklistItem>();
                                }
                            }
                            else if (!cs.IsChecklist)
                            {
                                existingCard.ChecklistItems = null;
                            }
                            if (existingCard.ChecklistScrollViewer != null && existingCard.NoteEditor != null)
                            {
                                existingCard.ChecklistScrollViewer.Visibility = existingCard.IsChecklist ? Visibility.Visible : Visibility.Collapsed;
                                existingCard.NoteEditor.Visibility = existingCard.IsChecklist ? Visibility.Collapsed : Visibility.Visible;
                            }
                            if (existingCard.IsChecklist)
                            {
                                RenderChecklistItems(existingCard);
                            }

                            // Restore Doodle
                            existingCard.NoteDoodleInkBase64 = cs.NoteDoodleInkBase64;
                            if (existingCard.NoteDoodleCanvas != null)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(cs.NoteDoodleInkBase64))
                                    {
                                        existingCard.NoteDoodleCanvas.Strokes.Clear();
                                    }
                                    else
                                    {
                                        byte[] raw = Convert.FromBase64String(cs.NoteDoodleInkBase64);
                                        using var ms = new System.IO.MemoryStream(raw);
                                        existingCard.NoteDoodleCanvas.Strokes = new System.Windows.Ink.StrokeCollection(ms);
                                    }
                                }
                                catch { }
                            }

                            // Restore Background GIF
                            if (existingCard.NoteBgGifPath != cs.NoteBgGifPath || existingCard.NoteBgGifBase64 != cs.NoteBgGifBase64)
                            {
                                ApplyNoteBgGif(existingCard, cs.NoteBgGifPath, cs.NoteBgGifBase64);
                            }
                        }
                        else if (existingCard.IsDrawCard)
                        {
                            existingCard.DrawPenColor = cs.DrawPenColor;
                            existingCard.DrawPenSize = cs.DrawPenSize;
                            existingCard.DrawIsEraser = cs.DrawIsEraser;
                            if (existingCard.DrawInkBase64 != cs.DrawInkBase64 && existingCard.DrawCanvas != null)
                            {
                                existingCard.DrawInkBase64 = cs.DrawInkBase64;
                                try
                                {
                                    if (string.IsNullOrEmpty(cs.DrawInkBase64))
                                    {
                                        existingCard.DrawCanvas.Strokes.Clear();
                                    }
                                    else
                                    {
                                        byte[] raw = Convert.FromBase64String(cs.DrawInkBase64);
                                        using var ms = new System.IO.MemoryStream(raw);
                                        existingCard.DrawCanvas.Strokes = new System.Windows.Ink.StrokeCollection(ms);
                                    }
                                }
                                catch { }
                            }
                        }
                        else if (cs.Bitmap != null && existingCard.Bitmap != cs.Bitmap && !existingCard.IsPlayingYouTube)
                        {
                            existingCard.Bitmap = cs.Bitmap;
                            existingCard.ImageControl.Source = cs.Bitmap;
                        }
                    }
                    else
                    {
                        if (cs.IsPaletteCard)
                        {
                            List<PalettePin>? pins = null;
                            if (!string.IsNullOrEmpty(cs.PalettePinsData))
                            {
                                try { pins = JsonSerializer.Deserialize<List<PalettePin>>(cs.PalettePinsData); } catch { }
                            }
                            var newPaletteCard = AddPaletteCard(
                                sourceCard: null,
                                initialPins: pins,
                                worldPosition: new Point(cs.X, cs.Y),
                                customWidth: cs.Width,
                                customHeight: cs.Height,
                                mood: cs.PaletteMood,
                                colorCount: cs.PaletteColorCount,
                                rows: cs.PaletteRows,
                                autoSelect: false);
                            newPaletteCard.Id = cs.Id;
                            newPaletteCard.GroupId = cs.GroupId;
                            newPaletteCard.PendingLinkedSourceCardId = cs.LinkedSourceCardId;
                        }
                        else if (cs.IsNote)
                        {
                            DateTime? noteDl = null;
                            if (cs.HasDeadline && !string.IsNullOrEmpty(cs.DeadlineIso) && DateTime.TryParse(cs.DeadlineIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDl))
                            {
                                noteDl = parsedDl;
                            }
                            List<NoteChecklistItem>? parsedChecklist = null;
                            if (cs.IsChecklist && !string.IsNullOrEmpty(cs.ChecklistJson))
                            {
                                try { parsedChecklist = JsonSerializer.Deserialize<List<NoteChecklistItem>>(cs.ChecklistJson); } catch { }
                            }

                            var newNote = AddNoteCard(
                                text: cs.NoteText,
                                worldPosition: new Point(cs.X, cs.Y),
                                customWidth: cs.Width,
                                customHeight: cs.Height,
                                fontFamily: cs.NoteFontFamily,
                                fontSize: cs.NoteFontSize,
                                textColor: cs.NoteTextColor,
                                bgColor: cs.NoteBgColor,
                                alignment: cs.NoteAlignment,
                                hasShadow: cs.NoteHasShadow,
                                isChecklist: cs.IsChecklist,
                                checklistItems: parsedChecklist,
                                hasDeadline: cs.HasDeadline,
                                deadlineDateTime: noteDl,
                                deadlineLabel: cs.DeadlineLabel,
                                noteDoodleInkBase64: cs.NoteDoodleInkBase64,
                                noteBgGifPath: cs.NoteBgGifPath,
                                noteBgGifBase64: cs.NoteBgGifBase64,
                                autoSelect: false);
                            newNote.Id = cs.Id;
                            newNote.GroupId = cs.GroupId;
                        }
                        else if (cs.IsDrawCard)
                        {
                            var newDraw = AddDrawCard(
                                worldPosition: new Point(cs.X, cs.Y),
                                customWidth: cs.Width,
                                customHeight: cs.Height,
                                initialInkBase64: cs.DrawInkBase64,
                                penColor: cs.DrawPenColor,
                                penSize: cs.DrawPenSize,
                                autoSelect: false);
                            newDraw.Id = cs.Id;
                            newDraw.GroupId = cs.GroupId;
                        }
                        else
                        {
                            var newCard = AddImageCard(
                                cs.Bitmap ?? cs.OriginalBitmap!,
                                new Point(cs.X, cs.Y),
                                customWidth: cs.Width,
                                customHeight: cs.Height,
                                localPath: cs.LocalPath,
                                base64Data: cs.Base64Data,
                                autoSelect: false,
                                originalBitmap: cs.OriginalBitmap,
                                baseWidth: cs.BaseWidth,
                                baseHeight: cs.BaseHeight,
                                cropLeft: cs.CropLeft,
                                cropTop: cs.CropTop,
                                cropRight: cs.CropRight,
                                cropBottom: cs.CropBottom,
                                isYouTube: cs.IsYouTube,
                                youTubeId: cs.YouTubeId,
                                youTubeUrl: cs.YouTubeUrl,
                                isLocalVideo: cs.IsLocalVideo,
                                videoFilePath: cs.VideoFilePath,
                                isVideoLooping: cs.IsVideoLooping,
                                isVideoMuted: cs.IsVideoMuted);
                            newCard.Id = cs.Id;
                            newCard.GroupId = cs.GroupId;
                        }
                    }
                }

                // Re-link newly created palette cards to their source image cards
                foreach (var card in _cards)
                {
                    if (card.IsPaletteCard && !string.IsNullOrEmpty(card.PendingLinkedSourceCardId))
                    {
                        var src = _cards.FirstOrDefault(c => c.Id == card.PendingLinkedSourceCardId);
                        if (src != null)
                        {
                            card.LinkedSourceImageCard = src;
                            src.LinkedPaletteCard = card;
                        }
                    }
                }

                // Reconcile Groups
                var snapGroupIds = (snap.Groups ?? new List<GroupSnapshot>()).Select(g => g.Id).ToHashSet();
                for (int i = _groups.Count - 1; i >= 0; i--)
                {
                    var g = _groups[i];
                    if (!snapGroupIds.Contains(g.Id))
                    {
                        WorldCanvas.Children.Remove(g.Container);
                        _groups.RemoveAt(i);
                        _selectedGroups.Remove(g);
                    }
                }

                var existingGroupsMap = _groups.ToDictionary(g => g.Id);
                if (snap.Groups != null)
                {
                    foreach (var gs in snap.Groups)
                    {
                        if (existingGroupsMap.TryGetValue(gs.Id, out var existingG))
                        {
                            existingG.X = gs.X;
                            existingG.Y = gs.Y;
                            existingG.Width = gs.Width;
                            existingG.Height = gs.Height;
                            existingG.Title = gs.Title;
                            existingG.Color = gs.Color;
                            existingG.Notes = gs.Notes;
                            Canvas.SetLeft(existingG.Container, gs.X);
                            Canvas.SetTop(existingG.Container, gs.Y);
                            existingG.Container.Width = Math.Max(200, gs.Width);
                            existingG.Container.Height = Math.Max(150, gs.Height);
                            if (existingG.TitleText != null) existingG.TitleText.Text = gs.Title;
                            if (existingG.NotesBox != null) existingG.NotesBox.Text = gs.Notes;
                            if (existingG.FrameBorder != null)
                            {
                                try
                                {
                                    var bc = (Color)ColorConverter.ConvertFromString(gs.Color);
                                    existingG.FrameBorder.BorderBrush = new SolidColorBrush(bc);
                                }
                                catch { }
                            }
                            if (existingG.ColorDot != null)
                            {
                                try
                                {
                                    var bc = (Color)ColorConverter.ConvertFromString(gs.Color);
                                    existingG.ColorDot.Background = new SolidColorBrush(bc);
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            AddSceneGroup(
                                title: gs.Title,
                                notes: gs.Notes,
                                customX: gs.X,
                                customY: gs.Y,
                                width: gs.Width,
                                height: gs.Height,
                                color: gs.Color,
                                customId: gs.Id,
                                recordUndo: false);
                        }
                    }
                }
                UpdateGroupCounts();

                UpdateStatusCounts();
                EmptyStateOverlay.Visibility = _cards.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                SyncActiveHwndPositions(updateSize: true);
                ScheduleAutoSave();
            }
            finally
            {
                _isApplyingSnapshot = false;
            }
        }

        #region Toast Notification System

        public void ShowToast(string message, ToastType type = ToastType.Info, int durationMs = 2800)
        {
            Dispatcher.Invoke(() =>
            {
                // Prevent duplicate spam of identical toast message
                if (ToastContainer.Children.Count > 0)
                {
                    var lastToast = ToastContainer.Children[^1] as FrameworkElement;
                    if (lastToast?.Tag as string == message)
                    {
                        return;
                    }
                }

                // Cap maximum visible toasts to 3 so it never stacks up to the top!
                while (ToastContainer.Children.Count >= 3)
                {
                    ToastContainer.Children.RemoveAt(0);
                }

                Border toast = new Border
                {
                    Tag = message,
                    Background = new SolidColorBrush(Color.FromArgb(242, 20, 24, 33)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 7, 0, 0),
                    Opacity = 0,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    SnapsToDevicePixels = true,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 14,
                        ShadowDepth = 3,
                        Opacity = 0.5
                    }
                };

                Color accentColor = type switch
                {
                    ToastType.Success => Color.FromRgb(16, 185, 129), // #10B981 emerald
                    ToastType.Error => Color.FromRgb(239, 68, 68),     // #EF4444 red
                    _ => Color.FromRgb(59, 130, 246)                  // #3B82F6 blue
                };

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Accent indicator bar on left
                Border accentBar = new Border
                {
                    Width = 4,
                    Background = new SolidColorBrush(accentColor),
                    CornerRadius = new CornerRadius(8, 0, 0, 8)
                };
                Grid.SetColumn(accentBar, 0);
                grid.Children.Add(accentBar);

                // Toast message text
                TextBlock txt = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 12.5,
                    FontFamily = new FontFamily("Segoe UI, Inter, Sans-serif"),
                    FontWeight = FontWeights.Normal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 8, 16, 8)
                };
                Grid.SetColumn(txt, 1);
                grid.Children.Add(txt);

                toast.Child = grid;

                TranslateTransform translate = new TranslateTransform(0, 10);
                toast.RenderTransform = translate;

                ToastContainer.Children.Add(toast);

                // Smooth slide-in & fade-in
                DoubleAnimation animFadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                DoubleAnimation animSlideIn = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                toast.BeginAnimation(UIElement.OpacityProperty, animFadeIn);
                translate.BeginAnimation(TranslateTransform.YProperty, animSlideIn);

                // Auto dismiss timer
                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    DoubleAnimation animFadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
                    DoubleAnimation animSlideOut = new DoubleAnimation(0, 20, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    animFadeOut.Completed += (s2, e2) =>
                    {
                        ToastContainer.Children.Remove(toast);
                    };
                    toast.BeginAnimation(UIElement.OpacityProperty, animFadeOut);
                    translate.BeginAnimation(TranslateTransform.XProperty, animSlideOut);
                };
                timer.Start();
            });
        }

        #endregion

        private static UIElement CreateMenuIcon(string pathData, string strokeColor = "#94A3B8")
        {
            Viewbox vb = new Viewbox { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Canvas c = new Canvas { Width = 24, Height = 24 };
            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(strokeColor)),
                StrokeThickness = 2.0,
                Fill = Brushes.Transparent,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            c.Children.Add(p);
            vb.Child = c;
            return vb;
        }

        private static UIElement CreateBadgeIcon(string text, string textColor, string bgColor, string borderColor)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textColor))
            };
            b.Child = tb;
            return b;
        }

        private static MenuItem CreateRichMenuItem(UIElement icon, string title, string subtitle, RoutedEventHandler onClick, string? titleColor = null)
        {
            MenuItem mi = new MenuItem();
            Grid grid = new Grid { Margin = new Thickness(2, 2, 10, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn((FrameworkElement)icon, 0);
            grid.Children.Add(icon);

            StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            TextBlock tbTitle = new TextBlock
            {
                Text = title,
                FontSize = 12.0,
                FontWeight = FontWeights.SemiBold,
                Foreground = titleColor != null 
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(titleColor)) 
                    : new SolidColorBrush(Color.FromRgb(241, 245, 249))
            };
            sp.Children.Add(tbTitle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock tbSub = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 10.0,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 1, 0, 0)
                };
                sp.Children.Add(tbSub);
            }

            Grid.SetColumn(sp, 1);
            grid.Children.Add(sp);

            mi.Header = grid;
            mi.Click += onClick;
            return mi;
        }

        private ContextMenu CreateCardContextMenu(CardItem item)
        {
            ContextMenu cm = new ContextMenu();

            if (item.IsNote)
            {
                // Category Header: "STICKY NOTE"
                MenuItem miHeaderNote = new MenuItem
                {
                    Header = new TextBlock
                    {
                        Text = "STICKY NOTE",
                        FontSize = 9.0,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                        Margin = new Thickness(8, 4, 8, 4)
                    },
                    IsEnabled = false,
                    Focusable = false
                };
                cm.Items.Add(miHeaderNote);

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 16,4 h 2 a 2,2 0 0 1 2,2 v 14 a 2,2 0 0 1 -2,2 H 6 a 2,2 0 0 1 -2,-2 V 6 a 2,2 0 0 1 2,-2 h 2 M 9,2 h 6 a 1,1 0 0 1 1,1 v 2 a 1,1 0 0 1 -1,1 H 9 a 1,1 0 0 1 -1,-1 V 3 a 1,1 0 0 1 1,-1 z"),
                    "Copy Text",
                    "Copy note contents to clipboard",
                    (s, e) =>
                    {
                        try
                        {
                            Clipboard.SetText(item.NoteText);
                            ShowToast("Copied note text to clipboard", ToastType.Success);
                        }
                        catch (Exception ex)
                        {
                            ShowToast("Copy failed: " + ex.Message, ToastType.Error);
                        }
                    }
                ));

                MenuItem miBg = new MenuItem
                {
                    Header = "Background Style",
                    Icon = CreateMenuIcon("M 12,2 L 2,7 L 12,12 L 22,7 Z M 2,17 L 12,22 L 22,17 M 2,12 L 12,17 L 22,12")
                };
                string[] styles = new[] { "Transparent", "Dark Glass", "Solid Dark", "Yellow Sticky", "Cyan Sticky" };
                foreach (string st in styles)
                {
                    MenuItem sub = new MenuItem { Header = st };
                    sub.Click += (s, e) =>
                    {
                        ApplyNoteBackground(item, st);
                        ScheduleAutoSave();
                    };
                    miBg.Items.Add(sub);
                }
                cm.Items.Add(miBg);

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 4,4 L 20,4 L 20,20 L 4,20 Z M 8,12 L 12,16 L 16,12", "#F472B6"),
                    "Set Animated GIF Background...",
                    "Pick a looping GIF background that fits this note",
                    (s, e) => PromptSetNoteBgGif(item)
                ));

                if (!string.IsNullOrEmpty(item.NoteBgGifPath) || !string.IsNullOrEmpty(item.NoteBgGifBase64))
                {
                    cm.Items.Add(CreateRichMenuItem(
                        CreateMenuIcon("M 4,4 L 20,4 L 20,20 L 4,20 Z M 8,8 L 16,16 M 16,8 L 8,16", "#38BDF8"),
                        "Fit Note to GIF Ratio",
                        "Snap note proportions to match GIF aspect ratio exactly",
                        (s, e) => FitNoteToGifAspectRatio(item)
                    ));

                    cm.Items.Add(CreateRichMenuItem(
                        CreateMenuIcon("M 4,8 L 4,4 L 8,4 M 20,8 L 20,4 L 16,4 M 4,16 L 4,20 L 8,20 M 20,16 L 20,20 L 16,20", "#38BDF8"),
                        item.NoteBgGifImage?.Stretch == Stretch.Uniform ? "Set GIF to Fill (Cover)" : "Set GIF to Fit (Contain)",
                        "Toggle between edge-to-edge fill and uncropped contain",
                        (s, e) =>
                        {
                            if (item.NoteBgGifImage != null)
                            {
                                item.NoteBgGifImage.Stretch = item.NoteBgGifImage.Stretch == Stretch.Uniform ? Stretch.UniformToFill : Stretch.Uniform;
                                ShowToast(item.NoteBgGifImage.Stretch == Stretch.Uniform ? "GIF mode: Contain (Entire GIF visible)" : "GIF mode: Cover (Fills entire card)", ToastType.Info);
                            }
                        }
                    ));

                    cm.Items.Add(CreateRichMenuItem(
                        CreateMenuIcon("M 3,6 h 18 M 19,6 v 14 a 2,2 0 0 1 -2,2 H 7 a 2,2 0 0 1 -2,-2 V 6", "#EF4444"),
                        "Remove GIF Background",
                        "Clear animated GIF background from this note",
                        (s, e) =>
                        {
                            ApplyNoteBgGif(item, null, null);
                            ScheduleAutoSave();
                            ShowToast("Removed note background GIF", ToastType.Info);
                        },
                        titleColor: "#EF4444"
                    ));
                }

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 4,8 L 4,4 L 8,4 M 20,8 L 20,4 L 16,4 M 4,16 L 4,20 L 8,20 M 20,16 L 20,20 L 16,20", "#38BDF8"),
                    "Reset Aspect Ratio",
                    "Restore note to natural proportions (removes distortion)",
                    (s, e) =>
                    {
                        double targetRatio = item.IsChecklist ? (320.0 / 240.0) : (280.0 / 180.0);
                        item.Height = Math.Round(item.Width / targetRatio);
                        item.AspectRatio = targetRatio;
                        ScheduleAutoSave();
                        ShowToast("✨ Reset note aspect ratio to default", ToastType.Success);
                    }
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 12,2 A 10,10 0 1 0 22,12 A 10,10 0 0 0 12,2 Z M 12,6 L 12,12 L 16,14"),
                    "Set / Edit Deadline",
                    item.HasDeadline && item.DeadlineDateTime.HasValue ? $"Current: {item.DeadlineDateTime.Value.ToString("ddd, d MMM HH:mm", CultureInfo.InvariantCulture)}" : "Set realtime countdown deadline",
                    (s, e) => ShowDeadlinePickerPopup(item.Container, item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 4,4 L 10,4 L 10,10 L 4,10 Z M 14,7 L 20,7 M 4,14 L 10,14 L 10,20 L 4,20 Z M 14,17 L 20,17"),
                    item.IsChecklist ? "Switch to Plain Text" : "Switch to Checklist Mode",
                    "Interactive tasks with strike-through animations",
                    (s, e) => ToggleNoteChecklistMode(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 18,2 L 22,6 L 7,21 L 2,22 L 3,17 Z"),
                    item.IsNoteDoodleActive ? "Turn Off Doodle Mode" : "Turn On Doodle Layer",
                    "Draw and strike through tasks directly over note",
                    (s, e) => ToggleNoteDoodleMode(item)
                ));

                cm.Items.Add(new Separator());

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 12,4 L 12,20 M 6,10 L 12,4 L 18,10"),
                    "Bring to Front",
                    "Stack above other items",
                    (s, e) =>
                    {
                        _highestZ++;
                        Panel.SetZIndex(item.Container, _highestZ);
                    }
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 12,20 L 12,4 M 6,14 L 12,20 L 18,14"),
                    "Send to Back",
                    "Stack below other items (Ctrl + [)",
                    (s, e) =>
                    {
                        Panel.SetZIndex(item.Container, --_lowestZ);
                        ScheduleAutoSave();
                    }
                ));

                cm.Items.Add(new Separator());

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 3,6 h 18 M 19,6 v 14 a 2,2 0 0 1 -2,2 H 7 a 2,2 0 0 1 -2,-2 V 6 M 8,6 V 4 a 2,2 0 0 1 2,-2 h 4 a 2,2 0 0 1 2,2 v 2", "#EF4444"),
                    "Delete Note",
                    "Remove from canvas",
                    (s, e) => RemoveCard(item),
                    titleColor: "#EF4444"
                ));

                return cm;
            }
            else if (item.IsPaletteCard)
            {
                // Category Header: "LIVE COLOR PALETTE"
                MenuItem miHeaderPalette = new MenuItem
                {
                    Header = new TextBlock
                    {
                        Text = "LIVE COLOR PALETTE",
                        FontSize = 9.0,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                        Margin = new Thickness(8, 4, 8, 4)
                    },
                    IsEnabled = false,
                    Focusable = false
                };
                cm.Items.Add(miHeaderPalette);

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 4,4 L 10,4 M 4,10 L 10,10 M 14,14 L 20,14 M 14,20 L 20,20", "#FBBF24"),
                    "Randomize Colors",
                    "Re-extract random palette tones",
                    (s, e) => RefreshPaletteCardFromSource(item, isRandom: true)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 12,2 A 7,7 0 0 0 5,9 c 0,5.25 7,13 7,13 s 7,-7.75 7,-13 a 7,7 0 0 0 -7,-7 z", "#38BDF8"),
                    "Toggle Sampling Pins",
                    "Show or hide live pins on image",
                    (s, e) =>
                    {
                        if (item.LinkedSourceImageCard == null || !_cards.Contains(item.LinkedSourceImageCard))
                        {
                            var candidate = _selectedCards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null)
                                         ?? _cards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null);
                            if (candidate != null)
                            {
                                item.LinkedSourceImageCard = candidate;
                                candidate.LinkedPaletteCard = item;
                                if (candidate.ActivePalettePins == null || candidate.ActivePalettePins.Count == 0)
                                    candidate.ActivePalettePins = item.ActivePalettePins;
                                ScheduleAutoSave();
                            }
                        }

                        if (item.LinkedSourceImageCard != null)
                        {
                            var src = item.LinkedSourceImageCard;
                            src.LinkedPaletteCard = item;
                            if (src.ActivePalettePins == null || src.ActivePalettePins.Count == 0)
                                src.ActivePalettePins = item.ActivePalettePins;

                            if (src.IsPaletteMode)
                                CloseCanvasPaletteMode(src);
                            else
                                StartCanvasPaletteMode(src);
                        }
                        else
                        {
                            ShowToast("Click or select an image on the board first", ToastType.Info);
                        }
                    }
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 10,14 a 5,5 0 0 0 7.07,0 l 3.54,-3.54 a 5,5 0 0 0 -7.07,-7.07 l -1.49,1.49 M 14,10 a 5,5 0 0 0 -7.07,0 l -3.54,3.54 a 5,5 0 0 0 7.07,7.07 l 1.49,-1.49", "#34D399"),
                    "Link to Selected Image",
                    "Connect palette to currently selected reference image",
                    (s, e) =>
                    {
                        var target = _selectedCards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null)
                                  ?? _cards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null);
                        if (target != null)
                        {
                            item.LinkedSourceImageCard = target;
                            target.LinkedPaletteCard = item;
                            target.ActivePalettePins = item.ActivePalettePins;
                            ScheduleAutoSave();
                            ShowToast("Palette connected to reference image!", ToastType.Success);
                            StartCanvasPaletteMode(target);
                        }
                        else
                        {
                            ShowToast("Please select an image on the canvas first", ToastType.Info);
                        }
                    }
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateBadgeIcon("Ae", "#9999FF", "#2E284A", "#4A3F75"),
                    "Export to After Effects",
                    "Send palette graphic to AE composition",
                    (s, e) => ExportCanvasPaletteToAe(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 16,4 h 2 a 2,2 0 0 1 2,2 v 14 a 2,2 0 0 1 -2,2 H 6 a 2,2 0 0 1 -2,-2 V 6 a 2,2 0 0 1 2,-2 h 2 M 9,2 h 6 a 1,1 0 0 1 1,1 v 2 a 1,1 0 0 1 -1,1 H 9 a 1,1 0 0 1 -1,-1 V 3 a 1,1 0 0 1 1,-1 z"),
                    "Copy Palette Image",
                    "Copy palette graphic to clipboard",
                    (s, e) => CopyCardToClipboard(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 3,5 h 18 v 6 h -18 z M 3,13 h 18 v 6 h -18 z", "#93C5FD"),
                    item.PaletteRows == 1 ? "Switch to 2 Rows" : "Switch to 1 Row",
                    "Toggle 1 or 2 rows layout",
                    (s, e) =>
                    {
                        item.PaletteRows = item.PaletteRows == 1 ? 2 : 1;
                        UpdatePaletteCardContent(item);
                        RebuildCardHoverToolbar(item);
                        ScheduleAutoSave();
                    }
                ));

                cm.Items.Add(new Separator());

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 12,4 L 12,20 M 6,10 L 12,4 L 18,10"),
                    "Bring to Front",
                    "Stack above other items",
                    (s, e) =>
                    {
                        _highestZ++;
                        Panel.SetZIndex(item.Container, _highestZ);
                    }
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 12,20 L 12,4 M 6,14 L 12,20 L 18,14"),
                    "Send to Back",
                    "Stack below other items (Ctrl + [)",
                    (s, e) =>
                    {
                        Panel.SetZIndex(item.Container, --_lowestZ);
                        ScheduleAutoSave();
                    }
                ));

                cm.Items.Add(new Separator());

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 3,6 h 18 M 19,6 v 14 a 2,2 0 0 1 -2,2 H 7 a 2,2 0 0 1 -2,-2 V 6 M 8,6 V 4 a 2,2 0 0 1 2,-2 h 4 a 2,2 0 0 1 2,2 v 2", "#EF4444"),
                    "Delete Palette Card",
                    "Remove palette from canvas",
                    (s, e) => RemoveCard(item),
                    titleColor: "#EF4444"
                ));

                return cm;
            }

            bool isMulti = _selectedCards.Contains(item) && _selectedCards.Count > 1;
            int selCount = isMulti ? _selectedCards.Count : 1;

            // Category Header: "REFERENCE" or "SELECTION (X REFERENCES SELECTED)"
            MenuItem miHeader = new MenuItem
            {
                Header = new TextBlock
                {
                    Text = isMulti ? $"SELECTION ({selCount} REFERENCES SELECTED)" : "REFERENCE",
                    FontSize = 9.0,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(isMulti ? Color.FromRgb(56, 189, 248) : Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(8, 4, 8, 4)
                },
                IsEnabled = false,
                Focusable = false
            };
            cm.Items.Add(miHeader);

            if (isMulti)
            {
                // Multi-select Arrange & Group Tools
                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 3,3 h 7 v 7 H 3 Z M 14,3 h 7 v 7 H 14 Z M 14,14 h 7 v 7 H 14 Z M 3,14 h 7 v 7 H 3 Z", "#38BDF8"),
                    "Grid Arrange Selection",
                    $"Pack {selCount} selected references into a clean masonry grid",
                    (s, e) => BtnGridArrange_Click(s, e)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 3,6 h 5 M 3,12 h 5 M 3,18 h 5 M 12,4 h 9 v 4 H 12 Z M 12,10 h 9 v 4 H 12 Z M 12,16 h 9 v 4 H 12 Z", "#818CF8"),
                    "Pipeline Arrange Selection",
                    $"Sequence {selCount} selected items into storyboard flow",
                    (s, e) => BtnPipelineArrange_Click(s, e)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 3,3 h 18 v 18 H 3 Z M 8,8 h 8 v 3 H 8 Z", "#3B82F6"),
                    "Group into Scene Group",
                    $"Wrap {selCount} selected references into a Scene container (Ctrl+G)",
                    (s, e) => BtnAddGroup_Click(s, e)
                ));

                cm.Items.Add(new Separator());
            }

            // YouTube specific actions
            if (item.IsYouTube)
            {
                cm.Items.Add(CreateRichMenuItem(
                    CreateBadgeIcon("YT", "#FF0000", "#3E1010", "#802020"),
                    item.IsPlayingYouTube ? "Stop Video" : "Play Video",
                    "Toggle in-card YouTube player",
                    (s, e) => ToggleYouTubePlayback(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateBadgeIcon("Ae", "#9999FF", "#2E284A", "#4A3F75"),
                    "Snap Frame to After Effects",
                    "Capture video frame directly to AE comp",
                    (s, e) => SnapYouTubeFrameToAE(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 18,13 v 6 a 2,2 0 0 1 -2,2 H 5 a 2,2 0 0 1 -2,-2 V 8 a 2,2 0 0 1 2,-2 h 6 M 15,3 h 6 v 6 M 10,14 L 21,3"),
                    "Open in Browser",
                    "View video on youtube.com",
                    (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(item.YouTubeUrl))
                            Process.Start(new ProcessStartInfo { FileName = item.YouTubeUrl, UseShellExecute = true });
                    }
                ));

                cm.Items.Add(new Separator());
            }

            // Local Video specific actions
            if (item.IsLocalVideo)
            {
                cm.Items.Add(CreateRichMenuItem(
                    CreateBadgeIcon("VID", "#38BDF8", "#0C2A4A", "#1E40AF"),
                    item.IsVideoPlaying ? "Pause Video" : "Play Video",
                    "Toggle local video playback",
                    (s, e) => ToggleLocalVideoPlayback(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateBadgeIcon("Ae", "#9999FF", "#2E284A", "#4A3F75"),
                    "Snap Frame to After Effects",
                    "Capture video frame directly to AE comp",
                    (s, e) => SnapLocalVideoFrameToAE(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateBadgeIcon("📋", "#34D399", "#064E3B", "#059669"),
                    "Snap Frame to Canvas",
                    "Capture video frame as reference card",
                    (s, e) => SnapLocalVideoFrameToBoard(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 15,3 h 6 v 6 M 9,21 H 3 v -6 M 21,3 l -7,7 M 3,21 l 7,-7"),
                    "Fit to Video Resolution",
                    "Fit card aspect ratio to original video dimensions",
                    (s, e) => FitVideoCardToResolution(item)
                ));

                cm.Items.Add(CreateRichMenuItem(
                    CreateMenuIcon("M 4,4 h 16 v 16 H 4 Z"),
                    "Original 1:1 Video Size",
                    "Set card to exact 1:1 pixel resolution of video",
                    (s, e) => ResetVideoToOriginal1to1(item)
                ));

                cm.Items.Add(new Separator());
            }

            // 1. Send to After Effects
            cm.Items.Add(CreateRichMenuItem(
                CreateBadgeIcon("Ae", "#9999FF", "#2E284A", "#4A3F75"),
                isMulti ? $"Send to After Effects ({selCount} Items)" : "Send to After Effects",
                isMulti ? $"Auto-import {selCount} references to AE comp as Guide Layers (#)" : "Auto-import footage to comp as Guide Layer (#)",
                (s, e) =>
                {
                    if (_selectedCards.Contains(item) && _selectedCards.Count > 1)
                        ExportCardsToAe(_selectedCards);
                    else
                        ExportCardsToAe(new[] { item });
                }
            ));

            // 2. Open in Photoshop
            cm.Items.Add(CreateRichMenuItem(
                CreateBadgeIcon("Ps", "#31A8FF", "#16314A", "#1D4C75"),
                "Open in Photoshop",
                "Open high-res file",
                (s, e) => SendCardToPs(item)
            ));

            // 3. Generate Color Palette (Adobe Style)
            cm.Items.Add(CreateRichMenuItem(
                CreateBadgeIcon("🎨", "#38BDF8", "#1E293B", "#38BDF8"),
                "Extract Color Palette",
                "Real-time canvas palette generator & interactive sample pins",
                (s, e) => StartCanvasPaletteMode(item)
            ));

            cm.Items.Add(new Separator());

            // 3. Copy Image File
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 9,9 L 22,9 L 22,22 L 9,22 Z M 5,15 L 4,15 A 2,2 0 0 1 2,13 L 2,4 A 2,2 0 0 1 4,2 L 13,2 A 2,2 0 0 1 15,4 L 15,5"),
                "Copy Image File",
                "Paste directly to any software",
                (s, e) =>
                {
                    EnsureLocalCache(item);
                    if (!string.IsNullOrEmpty(item.LocalPath) && File.Exists(item.LocalPath))
                    {
                        var col = new System.Collections.Specialized.StringCollection { item.LocalPath };
                        Clipboard.SetFileDropList(col);
                        ShowToast("Copied image file to clipboard!", ToastType.Success);
                    }
                    else
                    {
                        Clipboard.SetImage(item.Bitmap);
                        ShowToast("Copied image to clipboard!", ToastType.Success);
                    }
                }
            ));

            // 4. Reveal in Explorer
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 22,19 A 2,2 0 0 1 20,21 L 4,21 A 2,2 0 0 1 2,19 L 2,5 A 2,2 0 0 1 4,3 L 9,3 L 11,6 L 20,6 A 2,2 0 0 1 22,8 Z"),
                "Reveal in Explorer",
                "Show cached source file",
                (s, e) =>
                {
                    EnsureLocalCache(item);
                    if (!string.IsNullOrEmpty(item.LocalPath) && File.Exists(item.LocalPath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{item.LocalPath}\"");
                    }
                }
            ));

            cm.Items.Add(new Separator());

            // 5. Crop / Mask Reference
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 6,2 L 6,16 A 2,2 0 0 0 8,18 L 22,18 M 18,22 L 18,8 A 2,2 0 0 0 16,6 L 2,6", "#38BDF8"),
                "Crop / Mask Reference",
                "Focus on specific area",
                (s, e) => StartCropCard(item)
            ));

            // 6. Bring to Front
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 12,2 L 2,7 L 12,12 L 22,7 Z M 2,17 L 12,22 L 22,17 M 2,12 L 12,17 L 22,12"),
                isMulti ? $"Bring Selection to Front ({selCount})" : "Bring to Front",
                "Stack above other items (Ctrl + ])",
                (s, e) =>
                {
                    var targets = isMulti ? _selectedCards.ToList() : new List<CardItem> { item };
                    foreach (var c in targets)
                    {
                        Panel.SetZIndex(c.Container, ++_highestZ);
                    }
                    ScheduleAutoSave();
                }
            ));

            // 6b. Send to Back
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 12,22 L 2,17 L 12,12 L 22,17 Z M 2,7 L 12,2 L 2,7 M 2,12 L 12,7 L 22,12"),
                isMulti ? $"Send Selection to Back ({selCount})" : "Send to Back",
                "Stack below other items (Ctrl + [)",
                (s, e) =>
                {
                    var targets = isMulti ? _selectedCards.ToList() : new List<CardItem> { item };
                    foreach (var c in targets)
                    {
                        Panel.SetZIndex(c.Container, --_lowestZ);
                    }
                    ScheduleAutoSave();
                }
            ));

            // 7. Reset 1:1 Scale
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 15,3 L 21,3 L 21,9 M 9,21 L 3,21 L 3,15 M 21,3 L 14,10 M 3,21 L 10,14", "#38BDF8"),
                isMulti ? $"Reset 1:1 Scale ({selCount} Selected)" : "Reset 1:1 Scale",
                isMulti ? $"Reset all {selCount} items to natural pixel dimensions" : "",
                (s, e) =>
                {
                    RecordUndo("Reset 1:1 Scale");
                    var targets = isMulti ? _selectedCards.ToList() : new List<CardItem> { item };
                    foreach (var c in targets)
                    {
                        if (c.Bitmap != null)
                        {
                            c.Width = c.Bitmap.PixelWidth;
                            c.Height = c.Bitmap.PixelHeight;
                        }
                    }
                    ScheduleAutoSave();
                    ShowToast(isMulti ? $"Reset 1:1 Scale for {targets.Count} items" : "Reset 1:1 Scale", ToastType.Info);
                }
            ));

            cm.Items.Add(new Separator());

            // 8. Zoom to Reference
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 11,19 A 8,8 0 1 0 11,3 A 8,8 0 0 0 11,19 Z M 21,21 L 16.65,16.65"),
                isMulti ? $"Zoom to Selection ({selCount})" : "Zoom to Reference",
                isMulti ? "Focus canvas view on selected items" : "Focus canvas view on this image",
                (s, e) =>
                {
                    if (isMulti)
                        ZoomToSelectedCards();
                    else
                        ZoomToCard(item);
                }
            ));

            // 9. Delete Reference
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 3,6 L 5,6 L 21,6 M 19,6 L 19,20 A 2,2 0 0 1 17,22 L 7,22 A 2,2 0 0 1 5,20 L 5,6 M 8,6 L 8,4 A 2,2 0 0 1 10,2 L 14,2 A 2,2 0 0 1 16,4 L 16,6", "#EF4444"),
                isMulti ? $"Delete {selCount} Selected References" : "Delete Reference",
                isMulti ? "Remove selected references from canvas (Del)" : "",
                (s, e) =>
                {
                    DeleteSelectedCards();
                    ShowToast(isMulti ? $"Deleted {selCount} references" : "Deleted reference", ToastType.Info);
                },
                titleColor: "#EF4444"
            ));

            return cm;
        }

        private void ShowCanvasContextMenu(Point screenPoint, Point worldPoint)
        {
            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = CanvasContainer,
                Placement = PlacementMode.MousePoint
            };

            // Category Header: "CANVAS TOOLS"
            MenuItem miHeader = new MenuItem
            {
                Header = new TextBlock
                {
                    Text = "CANVAS TOOLS",
                    FontSize = 9.0,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(8, 4, 8, 4)
                },
                IsEnabled = false,
                Focusable = false
            };
            cm.Items.Add(miHeader);

            // 1. Add Scene Group Frame
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 3,3 h 18 v 18 H 3 Z M 8,8 h 8 v 3 H 8 Z", "#3B82F6"),
                "Add Scene Group",
                "Storyboard container frame with notes & info",
                (s, e) =>
                {
                    AddSceneGroup(
                        title: $"Scene {(_groups.Count + 1):D2}",
                        customX: worldPoint.X - 230,
                        customY: worldPoint.Y - 190);
                    ShowToast("Added Scene Group Frame", ToastType.Success);
                }
            ));

            // 2. Add Sticky Note / Text Card
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 4,4 L 16,4 L 20,8 L 20,20 L 4,20 Z M 16,4 L 16,8 L 20,8 M 8,10 h 5 M 8,14 h 8", "#38BDF8"),
                "Add Text / Sticky Note",
                "Lyrics, direction, or notes (N)",
                (s, e) =>
                {
                    AddNoteCard(worldPosition: worldPoint);
                    ShowToast("Added Note Card", ToastType.Success);
                }
            ));

            // 2.5 Add Interactive Checklist Note
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 9,11 L 12,14 L 22,4 M 21,12 v 7 a 2,2 0 0 1 -2,2 H 5 a 2,2 0 0 1 -2,-2 V 5 a 2,2 0 0 1 2,-2 h 11", "#10B981"),
                "Add Checklist Note",
                "Interactive task checklist with checkboxes & strike-through",
                (s, e) =>
                {
                    AddNoteCard(worldPosition: worldPoint, isChecklist: true);
                    ShowToast("Added Checklist Note", ToastType.Success);
                }
            ));

            // 3. Freehand Canvas Brush Tool
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 17 3 a 2.85 2.83 0 1 1 4 4 L 7.5 20.5 L 2 22 L 3.5 16.5 Z M 15 5 L 19 9", "#A855F7"),
                "Canvas Freehand Brush",
                "Draw and annotate directly on canvas (B/P)",
                (s, e) => BtnAddDraw_Click(s, e)
            ));

            cm.Items.Add(new Separator());

            // 4. Auto-Arrange Grid
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 3,3 h 7 v 7 H 3 Z M 14,3 h 7 v 7 H 14 Z M 14,14 h 7 v 7 H 14 Z M 3,14 h 7 v 7 H 3 Z", "#38BDF8"),
                "Auto-Arrange Grid",
                "Pack all loose references into neat masonry grid",
                (s, e) => BtnGridArrange_Click(s, e)
            ));

            // 5. Pipeline Storyboard Arrange
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 3,6 h 5 M 3,12 h 5 M 3,18 h 5 M 12,4 h 9 v 4 H 12 Z M 12,10 h 9 v 4 H 12 Z M 12,16 h 9 v 4 H 12 Z", "#818CF8"),
                "Pipeline Storyboard",
                "Arrange cards in storyboard sequence row",
                (s, e) => BtnPipelineArrange_Click(s, e)
            ));

            cm.Items.Add(new Separator());

            // 6. Add Image File...
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 14,2 L 6,2 A 2,2 0 0 0 4,4 L 4,20 A 2,2 0 0 0 6,22 L 18,22 A 2,2 0 0 0 20,20 L 20,8 Z M 14,2 L 14,8 L 20,8 M 12,18 L 12,12 M 9,15 L 15,15", "#10B981"),
                "Import Image File...",
                "Open file dialog to add image references",
                (s, e) => BtnAddImage_Click(s, e)
            ));

            // 7. Add URL Reference...
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 12,2 A 10,10 0 1 0 22,12 A 10,10 0 0 0 12,2 Z M 2,12 h 20 M 12,2 A 15.3,15.3 0 0 1 16,12 A 15.3,15.3 0 0 1 12,22 A 15.3,15.3 0 0 1 8,12 A 15.3,15.3 0 0 1 12,2 Z", "#F59E0B"),
                "Add Web Reference (URL)...",
                "Paste YouTube, Pinterest, or web image URL",
                (s, e) => BtnAddUrl_Click(s, e)
            ));

            // 8. Paste Image / URL
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 9,9 L 22,9 L 22,22 L 9,22 Z M 5,15 L 4,15 A 2,2 0 0 1 2,13 L 2,4 A 2,2 0 0 1 4,2 L 13,2 A 2,2 0 0 1 15,4 L 15,5", "#38BDF8"),
                "Paste Image / URL",
                "Paste from clipboard directly to canvas (Ctrl+V)",
                (s, e) => PasteFromClipboard()
            ));

            cm.Items.Add(new Separator());

            // 9. Fit Canvas in View
            cm.Items.Add(CreateRichMenuItem(
                CreateMenuIcon("M 15,3 h 6 v 6 M 9,21 H 3 v -6 M 21,3 l -7,7 M 3,21 l 7,-7", "#94A3B8"),
                "Fit Canvas in View",
                "Frame all cards neatly into viewport (F / Home)",
                (s, e) => ZoomToFitAllCards(animated: true)
            ));

            cm.IsOpen = true;
        }

        #region Card Hover Quick-Action Toolbar & Interactive Crop

        private Border CreateCardHoverToolbar(CardItem item)
        {
            Border pill = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0),
                Background = new SolidColorBrush(Color.FromArgb(235, 24, 28, 38)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(5, 2, 5, 2),
                Opacity = 0.0,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 3,
                    Direction = 270,
                    Opacity = 0.6,
                    Color = Colors.Black
                }
            };

            pill.MouseEnter += (s, e) =>
            {
                pill.BeginAnimation(UIElement.OpacityProperty, null);
                pill.Opacity = 1.0;
                pill.IsHitTestVisible = true;
            };
            pill.MouseLeave += (s, e) =>
            {
                if (item.IsPlayingYouTube || item.IsSelected || _isCardSubMenuOpen || item.IsPaletteMode) return;
                DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                anim.Completed += (s2, e2) =>
                {
                    if (!item.Container.IsMouseOver && !pill.IsMouseOver && !item.IsPlayingYouTube && !item.IsSelected && !_isCardSubMenuOpen && !item.IsPaletteMode)
                    {
                        pill.IsHitTestVisible = false;
                    }
                };
                pill.BeginAnimation(UIElement.OpacityProperty, anim);
            };

            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 1. Move grip button
            StackPanel moveSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock moveGrip = new TextBlock
            {
                Text = item.IsNote ? "⋮⋮" : "::",
                FontFamily = new FontFamily(item.IsNote ? "Segoe UI" : "Consolas, Segoe UI"),
                FontSize = item.IsNote ? 12 : 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Margin = item.IsNote ? new Thickness(0) : new Thickness(0, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock moveTxt = new TextBlock
            {
                Text = "Move",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = (item.IsNote || item.IsPaletteCard) ? Visibility.Collapsed : Visibility.Visible
            };
            moveSp.Children.Add(moveGrip);
            moveSp.Children.Add(moveTxt);

            Border btnMove = new Border
            {
                Background = Brushes.Transparent,
                Padding = (item.IsNote || item.IsPaletteCard) ? new Thickness(4, 3, 4, 3) : new Thickness(6, 3, 6, 3),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.SizeAll,
                ToolTip = item.IsNote ? "Drag to Move Note" : (item.IsPaletteCard ? "Drag to Move Palette Card" : "Drag to Move Reference Card"),
                Child = moveSp
            };
            btnMove.MouseLeftButtonDown += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    SelectCard(item, addToSelection: false);
                }
                _isDraggingCards = true;
                _cardDragStartMousePoint = e.GetPosition(CanvasContainer);
                _cardsInitialPositions.Clear();
                foreach (CardItem c in _selectedCards)
                {
                    _cardsInitialPositions[c] = new Point(c.X, c.Y);
                }
                SetWebViewHitTesting(false);
                btnMove.CaptureMouse();
                e.Handled = true;
            };
            btnMove.MouseMove += (s, e) =>
            {
                if (_isDraggingCards && btnMove.IsMouseCaptured)
                {
                    Point curPos = e.GetPosition(CanvasContainer);
                    double dx = (curPos.X - _cardDragStartMousePoint.X) / CanvasMatrixTransform.Matrix.M11;
                    double dy = (curPos.Y - _cardDragStartMousePoint.Y) / CanvasMatrixTransform.Matrix.M22;

                    foreach (CardItem c in _selectedCards)
                    {
                        if (_cardsInitialPositions.TryGetValue(c, out Point initPos))
                        {
                            c.X = initPos.X + dx;
                            c.Y = initPos.Y + dy;
                        }
                    }
                    SyncActiveHwndPositions(updateSize: false);
                    e.Handled = true;
                }
            };
            btnMove.MouseLeftButtonUp += (s, e) =>
            {
                if (_isDraggingCards && btnMove.IsMouseCaptured)
                {
                    _isDraggingCards = false;
                    SetWebViewHitTesting(true);
                    btnMove.ReleaseMouseCapture();
                    SyncActiveHwndPositions(updateSize: false);

                    foreach (CardItem c in _selectedCards.ToList())
                    {
                        CheckCardGroupAffiliation(c);
                    }

                    RecordUndo("Move Reference");
                    ScheduleAutoSave();
                    e.Handled = true;
                }
            };

            Border CreatePillButton(string label, Color color, string tooltip, Action onClick, bool isBold = false)
            {
                TextBlock tb = new TextBlock
                {
                    Text = label,
                    FontSize = 10.5,
                    FontWeight = isBold ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Border b = new Border
                {
                    Background = Brushes.Transparent,
                    Padding = new Thickness(6, 3, 6, 3),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    ToolTip = tooltip,
                    Child = tb
                };
                b.MouseEnter += (s, e) => b.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
                b.MouseLeave += (s, e) => b.Background = Brushes.Transparent;
                b.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected)
                    {
                        SelectCard(item, addToSelection: false);
                    }
                    onClick();
                };
                return b;
            }

            sp.Children.Add(btnMove);

            if (item.IsYouTube)
            {
                // Dedicated, clean YouTube card toolbar (no redundant Ae Import or static Crop)
                Border btnPlay = CreatePillButton(item.IsPlayingYouTube ? "⏹ Stop" : "▶ Play", Color.FromRgb(239, 68, 68), "Play / Stop Video in DropBoard", () =>
                {
                    ToggleYouTubePlayback(item);
                }, isBold: true);
                item.BtnPlayOverlay = btnPlay;

                Border btnSnapAe = CreatePillButton("📸 AE", Color.FromRgb(165, 180, 252), "Snapshot clean video frame directly to Adobe After Effects", () =>
                {
                    SnapYouTubeFrameToAE(item);
                }, isBold: true);

                Border btnSnapBoard = CreatePillButton("📋 Board", Color.FromRgb(52, 211, 153), "Snapshot clean video frame to DropBoard canvas", () =>
                {
                    SnapYouTubeFrameToBoard(item);
                }, isBold: true);

                Border btnBrowser = CreatePillButton("↗", Color.FromRgb(147, 197, 253), "Open in External Browser", () =>
                {
                    if (!string.IsNullOrEmpty(item.YouTubeUrl))
                    {
                        Process.Start(new ProcessStartInfo { FileName = item.YouTubeUrl, UseShellExecute = true });
                    }
                });

                Border btnCopy = CreatePillButton("Copy", Color.FromRgb(209, 213, 219), "Copy Image to Clipboard", () =>
                {
                    CopyCardToClipboard(item);
                });

                Border btnDel = CreatePillButton("✕", Color.FromRgb(239, 68, 68), "Delete Reference (Del)", () =>
                {
                    RemoveCard(item);
                }, isBold: true);

                sp.Children.Add(btnPlay);
                sp.Children.Add(btnSnapAe);
                sp.Children.Add(btnSnapBoard);
                sp.Children.Add(btnBrowser);
                sp.Children.Add(btnCopy);
                sp.Children.Add(btnDel);
            }
            else if (item.IsLocalVideo)
            {
                Border btnPlay = CreatePillButton(item.IsVideoPlaying ? "⏸ Pause" : "▶ Play", Color.FromRgb(56, 189, 248), "Play / Pause Video", () =>
                {
                    ToggleLocalVideoPlayback(item);
                }, isBold: true);
                item.BtnVideoPlayOverlay = btnPlay;

                Border btnLoop = CreatePillButton(item.IsVideoLooping ? "🔁 Loop ON" : "🔁 Loop OFF", item.IsVideoLooping ? Color.FromRgb(52, 211, 153) : Color.FromRgb(156, 163, 175), "Toggle Video Looping", () =>
                {
                    item.IsVideoLooping = !item.IsVideoLooping;
                    if (item.BtnVideoLoopOverlay?.Child is TextBlock tb)
                    {
                        tb.Text = item.IsVideoLooping ? "🔁 Loop ON" : "🔁 Loop OFF";
                        tb.Foreground = new SolidColorBrush(item.IsVideoLooping ? Color.FromRgb(52, 211, 153) : Color.FromRgb(156, 163, 175));
                    }
                    ShowToast(item.IsVideoLooping ? "🔁 Video Loop Enabled" : "Video Loop Disabled", ToastType.Info, 1500);
                    ScheduleAutoSave();
                });
                item.BtnVideoLoopOverlay = btnLoop;

                Border btnMute = CreatePillButton(item.IsVideoMuted ? "🔇 Muted" : "🔊 Sound", item.IsVideoMuted ? Color.FromRgb(251, 146, 60) : Color.FromRgb(96, 165, 250), "Toggle Audio Mute", () =>
                {
                    item.IsVideoMuted = !item.IsVideoMuted;
                    if (item.NativePlayer != null) item.NativePlayer.IsMuted = item.IsVideoMuted;
                    if (item.BtnVideoMuteOverlay?.Child is TextBlock tb)
                    {
                        tb.Text = item.IsVideoMuted ? "🔇 Muted" : "🔊 Sound";
                        tb.Foreground = new SolidColorBrush(item.IsVideoMuted ? Color.FromRgb(251, 146, 60) : Color.FromRgb(96, 165, 250));
                    }
                    ShowToast(item.IsVideoMuted ? "🔇 Audio Muted" : "🔊 Audio Unmuted", ToastType.Info, 1500);
                });
                item.BtnVideoMuteOverlay = btnMute;

                Border btnSnapAe = CreatePillButton("📸 AE", Color.FromRgb(165, 180, 252), "Snapshot clean video frame directly to Adobe After Effects", () =>
                {
                    SnapLocalVideoFrameToAE(item);
                }, isBold: true);

                Border btnSnapBoard = CreatePillButton("📋 Board", Color.FromRgb(52, 211, 153), "Snapshot clean video frame to DropBoard canvas", () =>
                {
                    SnapLocalVideoFrameToBoard(item);
                }, isBold: true);

                Border btnCopy = CreatePillButton("Copy", Color.FromRgb(209, 213, 219), "Copy Video File to Clipboard", () =>
                {
                    CopyCardToClipboard(item);
                });

                Border btnDel = CreatePillButton("✕", Color.FromRgb(239, 68, 68), "Delete Video (Del)", () =>
                {
                    RemoveCard(item);
                }, isBold: true);

                Border btnFit = CreatePillButton("⛶ Fit Reso", Color.FromRgb(56, 189, 248), "Fit card dimensions to exact video resolution & aspect ratio", () =>
                {
                    FitVideoCardToResolution(item);
                }, isBold: true);

                sp.Children.Add(btnPlay);
                sp.Children.Add(btnFit);
                sp.Children.Add(btnLoop);
                sp.Children.Add(btnMute);
                sp.Children.Add(btnSnapAe);
                sp.Children.Add(btnSnapBoard);
                sp.Children.Add(btnCopy);
                sp.Children.Add(btnDel);
            }
            else if (item.IsNote)
            {
                Border CreateDivider() => new Border
                {
                    Width = 1,
                    Height = 12,
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    Margin = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 1. Searchable Dark Font Family Picker Button
                TextBlock txtFontName = new TextBlock
                {
                    Text = string.IsNullOrEmpty(item.NoteFontFamily) ? "Segoe UI ▾" : $"{item.NoteFontFamily} ▾",
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxWidth = 72,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                item.NoteFontNameText = txtFontName;

                Border btnFontPicker = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Search and Change Font",
                    Child = txtFontName
                };
                btnFontPicker.MouseEnter += (s, e) => btnFontPicker.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                btnFontPicker.MouseLeave += (s, e) => btnFontPicker.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                btnFontPicker.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnFontPicker.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected)
                    {
                        SelectCard(item, addToSelection: false);
                    }
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        ShowFontPickerPopup(btnFontPicker, item, chosenFont =>
                        {
                            ApplyNoteFontFamily(item, chosenFont);
                            ScheduleAutoSave();
                        });
                    }));
                };

                // 2. Compact Font Size Button (Scroll Wheel or Click for Presets)
                TextBlock txtSize = new TextBlock
                {
                    Text = $"{(int)item.NoteFontSize} ▾",
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                item.NoteFontSizeText = txtSize;

                void StepFontSize(double delta)
                {
                    double newSize = Math.Clamp(item.NoteFontSize + delta, 8, 96);
                    ApplyNoteFontSize(item, newSize);
                    ScheduleAutoSave();
                }

                Border btnFontSize = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Font Size (Scroll Wheel or Click for Presets)",
                    Child = txtSize
                };
                btnFontSize.MouseEnter += (s, e) => btnFontSize.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                btnFontSize.MouseLeave += (s, e) => btnFontSize.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                btnFontSize.MouseWheel += (s, e) =>
                {
                    e.Handled = true;
                    StepFontSize(e.Delta > 0 ? 2 : -2);
                };
                btnFontSize.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnFontSize.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ShowFontSizeMenu(btnFontSize, item, newSize =>
                    {
                        ApplyNoteFontSize(item, newSize);
                        ScheduleAutoSave();
                    });
                };

                // 3. Compact Paragraph Alignment Buttons (Left | Center | Right)
                Border CreateAlignBtn(string type, TextAlignment align, string tooltip)
                {
                    string pathData = type switch
                    {
                        "Left" => "M 2.5,3.5 H 13.5 M 2.5,7.5 H 9 M 2.5,11.5 H 13.5 M 2.5,15.5 H 7",
                        "Center" => "M 2.5,3.5 H 13.5 M 4.5,7.5 H 11.5 M 2.5,11.5 H 13.5 M 5.5,15.5 H 10.5",
                        "Right" => "M 2.5,3.5 H 13.5 M 7,7.5 H 13.5 M 2.5,11.5 H 13.5 M 9,15.5 H 13.5",
                        _ => "M 2.5,3.5 H 13.5 M 2.5,7.5 H 9 M 2.5,11.5 H 13.5 M 2.5,15.5 H 7"
                    };

                    System.Windows.Shapes.Path p = new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse(pathData),
                        Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                        StrokeThickness = 1.7,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        SnapsToDevicePixels = true
                    };

                    Viewbox vb = new Viewbox
                    {
                        Width = 12,
                        Height = 12,
                        Child = p,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    Border b = new Border
                    {
                        Width = 20,
                        Height = 20,
                        Background = Brushes.Transparent,
                        CornerRadius = new CornerRadius(3),
                        Cursor = Cursors.Hand,
                        ToolTip = tooltip,
                        Margin = new Thickness(0.5, 0, 0.5, 0),
                        Child = vb,
                        Tag = p
                    };
                    return b;
                }

                Border btnAlignL = CreateAlignBtn("Left", TextAlignment.Left, "Align Left");
                Border btnAlignC = CreateAlignBtn("Center", TextAlignment.Center, "Align Center");
                Border btnAlignR = CreateAlignBtn("Right", TextAlignment.Right, "Align Right");

                void RefreshAlignState()
                {
                    void UpdateBtn(Border btn, TextAlignment align)
                    {
                        bool isActive = item.NoteAlignment == align;
                        if (btn.Tag is System.Windows.Shapes.Path p)
                        {
                            p.Stroke = isActive
                                ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                                : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                        }
                        btn.Background = isActive
                            ? new SolidColorBrush(Color.FromArgb(50, 56, 189, 248))
                            : Brushes.Transparent;
                    }

                    UpdateBtn(btnAlignL, TextAlignment.Left);
                    UpdateBtn(btnAlignC, TextAlignment.Center);
                    UpdateBtn(btnAlignR, TextAlignment.Right);
                }
                RefreshAlignState();

                void WireAlign(Border btn, TextAlignment align)
                {
                    btn.MouseEnter += (s, e) =>
                    {
                        if (item.NoteAlignment != align)
                            btn.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
                    };
                    btn.MouseLeave += (s, e) =>
                    {
                        if (item.NoteAlignment != align)
                            btn.Background = Brushes.Transparent;
                        else
                            btn.Background = new SolidColorBrush(Color.FromArgb(50, 56, 189, 248));
                    };
                    btn.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        if (!item.IsSelected) SelectCard(item, addToSelection: false);
                        ApplyNoteAlignment(item, align);
                        RefreshAlignState();
                        ScheduleAutoSave();
                    };
                }

                WireAlign(btnAlignL, TextAlignment.Left);
                WireAlign(btnAlignC, TextAlignment.Center);
                WireAlign(btnAlignR, TextAlignment.Right);

                StackPanel alignGroup = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(1, 0, 1, 0)
                };
                alignGroup.Children.Add(btnAlignL);
                alignGroup.Children.Add(btnAlignC);
                alignGroup.Children.Add(btnAlignR);

                // 4. Background Style Compact Button
                Border bgMiniDot = new Border
                {
                    Width = 10,
                    Height = 10,
                    CornerRadius = new CornerRadius(2),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                    Margin = new Thickness(0, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                void UpdateBgDot()
                {
                    bgMiniDot.Background = item.NoteBgColor switch
                    {
                        "Transparent" => Brushes.Transparent,
                        "Dark Glass" => new SolidColorBrush(Color.FromArgb(140, 56, 189, 248)),
                        "Solid Dark" => new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                        "Yellow Sticky" => new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                        "Cyan Sticky" => new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                        _ => Brushes.Transparent
                    };
                }
                UpdateBgDot();

                TextBlock txtBgChevron = new TextBlock
                {
                    Text = "▾",
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                StackPanel bgSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                bgSp.Children.Add(bgMiniDot);
                bgSp.Children.Add(txtBgChevron);

                // 5. Custom Color Picker Button
                Border colorDot = new Border
                {
                    Width = 10,
                    Height = 10,
                    CornerRadius = new CornerRadius(5),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                    Margin = new Thickness(0, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                try
                {
                    colorDot.Background = (Brush)new BrushConverter().ConvertFromString(item.NoteTextColor)!;
                }
                catch
                {
                    colorDot.Background = Brushes.White;
                }

                Border btnBgPicker = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 3, 5, 3),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Note Background Style (Transparent, Dark Glass, Sticky)",
                    Child = bgSp
                };
                btnBgPicker.MouseEnter += (s, e) => btnBgPicker.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                btnBgPicker.MouseLeave += (s, e) => btnBgPicker.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                btnBgPicker.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnBgPicker.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ShowNoteBgMenu(btnBgPicker, item, chosenBg =>
                    {
                        ApplyNoteBackground(item, chosenBg);
                        UpdateBgDot();
                        try
                        {
                            colorDot.Background = (Brush)new BrushConverter().ConvertFromString(item.NoteTextColor)!;
                        }
                        catch { }
                        ScheduleAutoSave();
                    });
                };

                TextBlock txtColorChevron = new TextBlock
                {
                    Text = "▾",
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                StackPanel colorBtnSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                colorBtnSp.Children.Add(colorDot);
                colorBtnSp.Children.Add(txtColorChevron);

                Border btnColor = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 3, 5, 3),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Custom Text Color (Palette & Hex Input)",
                    Child = colorBtnSp
                };
                btnColor.MouseEnter += (s, e) => btnColor.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                btnColor.MouseLeave += (s, e) => btnColor.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                btnColor.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnColor.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ShowColorPickerPopup(btnColor, item, chosenHex =>
                    {
                        ApplyNoteTextColor(item, chosenHex);
                        try
                        {
                            colorDot.Background = (Brush)new BrushConverter().ConvertFromString(chosenHex)!;
                        }
                        catch { }
                        ScheduleAutoSave();
                    });
                };

                // 5.5 Text Drop Shadow Toggle Button (SVG Icon)
                var pShadow = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M2 3h8v2H7v7H5V5H2V3zm5.5 5h6.5v1.8h-2.2v5.2H9.8v-5.2H7.5V8z"),
                    SnapsToDevicePixels = true
                };
                Viewbox vbShadow = new Viewbox
                {
                    Width = 12,
                    Height = 12,
                    Child = pShadow,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Border btnShadow = new Border
                {
                    Width = 22,
                    Height = 20,
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    ToolTip = "Toggle Text Drop Shadow (Contrast & Visibility on Any Canvas)",
                    Margin = new Thickness(1, 0, 1, 0),
                    Child = vbShadow
                };
                item.BtnNoteShadow = btnShadow;

                void RefreshShadowBtn()
                {
                    if (item.NoteHasShadow)
                    {
                        btnShadow.Background = new SolidColorBrush(Color.FromArgb(55, 56, 189, 248));
                        btnShadow.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                        pShadow.Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    }
                    else
                    {
                        btnShadow.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                        btnShadow.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                        pShadow.Fill = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                    }
                }
                RefreshShadowBtn();

                btnShadow.MouseEnter += (s, e) =>
                {
                    if (!item.NoteHasShadow)
                        btnShadow.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                };
                btnShadow.MouseLeave += (s, e) =>
                {
                    RefreshShadowBtn();
                };
                btnShadow.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnShadow.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    item.NoteHasShadow = !item.NoteHasShadow;
                    ApplyNoteShadow(item);
                    RefreshShadowBtn();
                    ScheduleAutoSave();
                    ShowToast(item.NoteHasShadow ? "Text drop shadow enabled" : "Text drop shadow disabled", ToastType.Info);
                };

                // 6. Overflow More Actions Button (•••)
                Border btnMore = new Border
                {
                    Background = Brushes.Transparent,
                    Padding = new Thickness(5, 2, 5, 2),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    ToolTip = "More Actions (Copy, Duplicate, Delete)",
                    Child = new TextBlock
                    {
                        Text = "•••",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                btnMore.MouseEnter += (s, e) => btnMore.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                btnMore.MouseLeave += (s, e) => btnMore.Background = Brushes.Transparent;
                btnMore.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnMore.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    ShowNoteMoreMenu(btnMore, item);
                };

                // 5.6 Realtime Deadline Button
                Border btnDeadline = new Border
                {
                    Background = item.HasDeadline
                        ? new SolidColorBrush(Color.FromArgb(55, 56, 189, 248))
                        : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = item.HasDeadline
                        ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                        : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Set / Edit Realtime Deadline (Synchronized with System Clock)"
                };
                StackPanel dlSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                dlSp.Children.Add(new TextBlock
                {
                    Text = "⏱",
                    FontSize = 10.5,
                    Foreground = item.HasDeadline ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    Margin = new Thickness(0, 0, 2, 0)
                });
                dlSp.Children.Add(new TextBlock
                {
                    Text = "Deadline",
                    FontSize = 10,
                    Foreground = item.HasDeadline ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    FontWeight = item.HasDeadline ? FontWeights.SemiBold : FontWeights.Normal
                });
                btnDeadline.Child = dlSp;
                item.BtnNoteDeadline = btnDeadline;

                btnDeadline.MouseEnter += (s, e) =>
                {
                    if (!item.HasDeadline) btnDeadline.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                };
                btnDeadline.MouseLeave += (s, e) =>
                {
                    if (!item.HasDeadline) btnDeadline.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                };
                btnDeadline.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnDeadline.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ShowDeadlinePickerPopup(btnDeadline, item);
                };

                // 5.7 Checklist Toggle Button
                Border btnChecklist = new Border
                {
                    Background = item.IsChecklist
                        ? new SolidColorBrush(Color.FromArgb(55, 16, 185, 129))
                        : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = item.IsChecklist
                        ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                        : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Toggle Checklist Mode (Check/uncheck with smooth strike-through animation)"
                };
                StackPanel chkSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                chkSp.Children.Add(new TextBlock
                {
                    Text = "☑",
                    FontSize = 10.5,
                    Foreground = item.IsChecklist ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    Margin = new Thickness(0, 0, 2, 0)
                });
                chkSp.Children.Add(new TextBlock
                {
                    Text = "Checklist",
                    FontSize = 10,
                    Foreground = item.IsChecklist ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    FontWeight = item.IsChecklist ? FontWeights.SemiBold : FontWeights.Normal
                });
                btnChecklist.Child = chkSp;
                item.BtnNoteChecklist = btnChecklist;

                btnChecklist.MouseEnter += (s, e) =>
                {
                    if (!item.IsChecklist) btnChecklist.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                };
                btnChecklist.MouseLeave += (s, e) =>
                {
                    if (!item.IsChecklist) btnChecklist.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                };
                btnChecklist.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnChecklist.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ToggleNoteChecklistMode(item);
                };

                // 5.8 Freehand Doodle / Strike Layer Toggle Button
                Border btnDoodle = new Border
                {
                    Background = item.IsNoteDoodleActive
                        ? new SolidColorBrush(Color.FromArgb(55, 244, 63, 94))
                        : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = item.IsNoteDoodleActive
                        ? new SolidColorBrush(Color.FromRgb(244, 63, 94))
                        : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Toggle Doodle Layer (Draw, strike through, and annotate directly on note)"
                };
                StackPanel doodleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                doodleSp.Children.Add(new TextBlock
                {
                    Text = "✏",
                    FontSize = 10.5,
                    Foreground = item.IsNoteDoodleActive ? new SolidColorBrush(Color.FromRgb(244, 63, 94)) : new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    Margin = new Thickness(0, 0, 2, 0)
                });
                doodleSp.Children.Add(new TextBlock
                {
                    Text = "Doodle",
                    FontSize = 10,
                    Foreground = item.IsNoteDoodleActive ? new SolidColorBrush(Color.FromRgb(244, 63, 94)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    FontWeight = item.IsNoteDoodleActive ? FontWeights.SemiBold : FontWeights.Normal
                });
                btnDoodle.Child = doodleSp;
                item.BtnNoteDoodle = btnDoodle;

                btnDoodle.MouseEnter += (s, e) =>
                {
                    if (!item.IsNoteDoodleActive) btnDoodle.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                };
                btnDoodle.MouseLeave += (s, e) =>
                {
                    if (!item.IsNoteDoodleActive) btnDoodle.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                };
                btnDoodle.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnDoodle.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ToggleNoteDoodleMode(item);
                };

                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnFontPicker);
                sp.Children.Add(btnFontSize);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(alignGroup);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnBgPicker);
                sp.Children.Add(btnColor);
                sp.Children.Add(btnShadow);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnDoodle);
                sp.Children.Add(btnDeadline);
                sp.Children.Add(btnChecklist);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnMore);
            }
            else if (item.IsDrawCard)
            {
                Border CreateDivider() => new Border
                {
                    Width = 1,
                    Height = 12,
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    Margin = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 1. Pen Tool Button
                Border btnPen = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 56, 189, 248)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Pen Tool (Draw with mouse/stylus)",
                    Child = new TextBlock
                    {
                        Text = "✏ Pen",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                item.BtnDrawPen = btnPen;

                // 2. Eraser Tool Button
                Border btnEraser = new Border
                {
                    Background = Brushes.Transparent,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Eraser Tool (Erase stroke)",
                    Child = new TextBlock
                    {
                        Text = "🧹 Erase",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                item.BtnDrawEraser = btnEraser;

                void RefreshToolVisuals()
                {
                    if (item.DrawIsEraser)
                    {
                        btnPen.Background = Brushes.Transparent;
                        btnPen.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                        if (btnPen.Child is TextBlock tbP) tbP.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));

                        btnEraser.Background = new SolidColorBrush(Color.FromArgb(50, 245, 158, 11));
                        btnEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                        if (btnEraser.Child is TextBlock tbE) tbE.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    }
                    else
                    {
                        btnPen.Background = new SolidColorBrush(Color.FromArgb(50, 56, 189, 248));
                        btnPen.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                        if (btnPen.Child is TextBlock tbP) tbP.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));

                        btnEraser.Background = Brushes.Transparent;
                        btnEraser.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                        if (btnEraser.Child is TextBlock tbE) tbE.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                    }
                }

                btnPen.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnPen.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    item.DrawIsEraser = false;
                    if (item.DrawCanvas != null)
                    {
                        item.DrawCanvas.EditingMode = InkCanvasEditingMode.Ink;
                        try { item.DrawCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString(item.DrawPenColor); } catch { }
                    }
                    RefreshToolVisuals();
                };

                btnEraser.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnEraser.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    item.DrawIsEraser = true;
                    if (item.DrawCanvas != null)
                    {
                        item.DrawCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    }
                    RefreshToolVisuals();
                };

                // 3. Brush Size Button (2px, 4px, 8px, 16px)
                TextBlock txtSize = new TextBlock
                {
                    Text = $"{(int)item.DrawPenSize}px ▾",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Border btnBrushSize = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Toggle Brush Size (2px, 4px, 8px, 16px)",
                    Child = txtSize
                };
                btnBrushSize.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnBrushSize.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    double[] sizes = new double[] { 2.0, 4.0, 8.0, 16.0 };
                    int curIdx = Array.IndexOf(sizes, item.DrawPenSize);
                    double nextSize = sizes[(curIdx + 1) % sizes.Length];
                    item.DrawPenSize = nextSize;
                    txtSize.Text = $"{(int)nextSize}px ▾";
                    if (item.DrawCanvas != null)
                    {
                        item.DrawCanvas.DefaultDrawingAttributes.Width = nextSize;
                        item.DrawCanvas.DefaultDrawingAttributes.Height = nextSize;
                    }
                    ScheduleAutoSave();
                };

                // 4. Color Swatch Picker Button
                Border colorDot = new Border
                {
                    Width = 11,
                    Height = 11,
                    CornerRadius = new CornerRadius(5.5),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    Margin = new Thickness(0, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                void UpdateColorDot()
                {
                    try { colorDot.Background = (Brush)new BrushConverter().ConvertFromString(item.DrawPenColor)!; }
                    catch { colorDot.Background = new SolidColorBrush(Color.FromRgb(56, 189, 248)); }
                }
                UpdateColorDot();

                StackPanel colorSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                colorSp.Children.Add(colorDot);
                colorSp.Children.Add(new TextBlock
                {
                    Text = "▾",
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                Border btnColor = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Pick Pen Color",
                    Child = colorSp
                };
                btnColor.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnColor.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                    ShowDrawColorMenu(btnColor, item, chosenHex =>
                    {
                        item.DrawPenColor = chosenHex;
                        if (item.DrawCanvas != null)
                        {
                            try
                            {
                                item.DrawCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString(chosenHex);
                            }
                            catch { }
                        }
                        item.DrawIsEraser = false;
                        if (item.DrawCanvas != null) item.DrawCanvas.EditingMode = InkCanvasEditingMode.Ink;
                        RefreshToolVisuals();
                        UpdateColorDot();
                        ScheduleAutoSave();
                    });
                };

                // 5. Clear All Strokes
                Border btnClear = CreatePillButton("Clear", Color.FromRgb(248, 113, 113), "Clear all sketch strokes", () =>
                {
                    if (item.DrawCanvas != null)
                    {
                        item.DrawCanvas.Strokes.Clear();
                        item.DrawInkBase64 = "";
                        ScheduleAutoSave();
                        ShowToast("Cleared sketch strokes", ToastType.Info);
                    }
                });

                // 6. Copy as Transparent PNG
                Border btnCopyPng = CreatePillButton("Copy PNG", Color.FromRgb(52, 211, 153), "Copy sketch to clipboard as transparent PNG", () =>
                {
                    CopyDrawCardToClipboard(item);
                }, isBold: true);

                // 7. Delete Card
                Border btnDel = CreatePillButton("✕", Color.FromRgb(239, 68, 68), "Delete Sketch Card (Del)", () =>
                {
                    RemoveCard(item);
                }, isBold: true);

                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnPen);
                sp.Children.Add(btnEraser);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnBrushSize);
                sp.Children.Add(btnColor);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnClear);
                sp.Children.Add(btnCopyPng);
                sp.Children.Add(btnDel);
            }
            else if (item.IsPaletteCard)
            {
                Border CreateDivider() => new Border
                {
                    Width = 1,
                    Height = 14,
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    Margin = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 1. Mood Dropdown Pill
                StackPanel moodSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                TextBlock txtMoodLabel = new TextBlock
                {
                    Text = $"{item.PaletteMood} ▾",
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                moodSp.Children.Add(txtMoodLabel);

                Border btnMood = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Color Harmony Mood (Colorful, Bright, Muted, Deep, Dark, Dominant)",
                    Child = moodSp
                };
                btnMood.MouseEnter += (s, e) => btnMood.Background = new SolidColorBrush(Color.FromArgb(70, 56, 189, 248));
                btnMood.MouseLeave += (s, e) => btnMood.Background = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248));
                btnMood.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    ShowPaletteCardMoodMenu(btnMood, item);
                };

                // 2. Swatch count Stepper: [ - ] 5 [ + ]
                StackPanel countSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                Border CreateStepBtn(string text, string tip, Action onStep)
                {
                    Border sb = new Border
                    {
                        Width = 17,
                        Height = 17,
                        Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                        CornerRadius = new CornerRadius(3),
                        Cursor = Cursors.Hand,
                        ToolTip = tip,
                        Child = new TextBlock
                        {
                            Text = text,
                            FontSize = 10.5,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    sb.MouseEnter += (s, e) => sb.Background = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255));
                    sb.MouseLeave += (s, e) => sb.Background = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
                    sb.MouseLeftButtonDown += (s, e) => { e.Handled = true; onStep(); };
                    return sb;
                }

                Border btnMinus = CreateStepBtn("−", "Decrease color count (min 3)", () =>
                {
                    if (item.PaletteColorCount > 3)
                    {
                        item.PaletteColorCount--;
                        RefreshPaletteCardFromSource(item, isRandom: false);
                        RebuildCardHoverToolbar(item);
                    }
                });

                TextBlock txtCountVal = new TextBlock
                {
                    Text = item.PaletteColorCount.ToString(),
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0)
                };

                Border btnPlus = CreateStepBtn("+", "Increase color count (max 10)", () =>
                {
                    if (item.PaletteColorCount < 10)
                    {
                        item.PaletteColorCount++;
                        RefreshPaletteCardFromSource(item, isRandom: false);
                        RebuildCardHoverToolbar(item);
                    }
                });

                countSp.Children.Add(btnMinus);
                countSp.Children.Add(txtCountVal);
                countSp.Children.Add(btnPlus);

                // 3. Randomize Button
                Border btnRandom = CreatePillButton("🎲", Color.FromRgb(251, 191, 36), "Re-sample random palette colors", () =>
                {
                    RefreshPaletteCardFromSource(item, isRandom: true);
                }, isBold: true);

                // 4. Rows Toggle Button: 1 Row <-> 2 Rows
                Border btnRows = CreatePillButton(item.PaletteRows == 2 ? "2R" : "1R", Color.FromRgb(147, 197, 253), "Toggle 1 or 2 rows layout", () =>
                {
                    item.PaletteRows = item.PaletteRows == 1 ? 2 : 1;
                    UpdatePaletteCardContent(item);
                    RebuildCardHoverToolbar(item);
                    ScheduleAutoSave();
                    ShowToast($"Layout: {item.PaletteRows} row(s)", ToastType.Info);
                }, isBold: true);

                // 5. More Actions Button (Pins, AE Export, Copy, Zoom, Delete)
                Border btnMore = new Border
                {
                    Background = Brushes.Transparent,
                    Padding = new Thickness(5, 2, 5, 2),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    ToolTip = "More Actions (Pins, AE Export, Copy, Delete)",
                    Child = new TextBlock
                    {
                        Text = "•••",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                btnMore.MouseEnter += (s, e) => btnMore.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                btnMore.MouseLeave += (s, e) => btnMore.Background = Brushes.Transparent;
                btnMore.PreviewMouseLeftButtonDown += (s, e) => e.Handled = true;
                btnMore.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    ShowPaletteCardMoreMenu(btnMore, item);
                };

                // 6. Delete Button
                Border btnDel = CreatePillButton("✕", Color.FromRgb(239, 68, 68), "Delete Palette Card", () =>
                {
                    RemoveCard(item);
                }, isBold: true);

                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnMood);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(countSp);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnRandom);
                sp.Children.Add(btnRows);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnMore);
                sp.Children.Add(btnDel);
            }
            else if (item.IsPaletteMode)
            {
                Border CreateDivider() => new Border
                {
                    Width = 1,
                    Height = 14,
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    Margin = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                // In image card during palette mode: quick Randomize and Close Pins
                Border btnRandom = CreatePillButton("🎲 Random", Color.FromRgb(251, 191, 36), "Re-sample random palette pins on image", () =>
                {
                    if (item.LinkedPaletteCard != null)
                        RefreshPaletteCardFromSource(item.LinkedPaletteCard, isRandom: true);
                    else
                        RefreshCanvasPalette(item, isRandom: true);
                }, isBold: true);

                Border btnClosePins = CreatePillButton("✕ Close Pins", Color.FromRgb(239, 68, 68), "Hide sampling pins from image", () =>
                {
                    CloseCanvasPaletteMode(item);
                }, isBold: true);

                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnRandom);
                sp.Children.Add(CreateDivider());
                sp.Children.Add(btnClosePins);
            }
            else
            {
                // Regular Image Card buttons
                Border btnPalette = CreatePillButton("🎨 Palette", Color.FromRgb(56, 189, 248), "Extract real-time interactive color palette on canvas", () =>
                {
                    StartCanvasPaletteMode(item);
                }, isBold: true);

                Border btnAe = CreatePillButton("Ae Import", Color.FromRgb(165, 180, 252), "Export this reference to Adobe After Effects", () =>
                {
                    if (_selectedCards.Contains(item) && _selectedCards.Count > 1)
                        ExportCardsToAe(_selectedCards);
                    else
                        ExportCardsToAe(new[] { item });
                }, isBold: true);

                Border btnCrop = CreatePillButton("✂ Crop", Color.FromRgb(251, 191, 36), "Crop / Mask Reference", () =>
                {
                    StartCropCard(item);
                }, isBold: true);

                Border btnCopy = CreatePillButton("Copy", Color.FromRgb(209, 213, 219), "Copy Image to Clipboard", () =>
                {
                    CopyCardToClipboard(item);
                });

                Border btnDel = CreatePillButton("✕", Color.FromRgb(239, 68, 68), "Delete Reference (Del)", () =>
                {
                    RemoveCard(item);
                }, isBold: true);

                sp.Children.Add(btnPalette);
                sp.Children.Add(btnAe);
                sp.Children.Add(btnCrop);
                sp.Children.Add(btnCopy);
                sp.Children.Add(btnDel);
            }

            pill.Child = sp;
            return pill;
        }

        private void ShowFontPickerPopup(FrameworkElement anchor, CardItem item, Action<string> onFontSelected)
        {
            _isCardSubMenuOpen = true;

            Popup popup = new Popup
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                VerticalOffset = 4
            };

            popup.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
                if (item.HoverToolbar != null && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!_isCardSubMenuOpen && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                        {
                            item.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            Border container = new Border
            {
                Width = 230,
                MaxHeight = 320,
                Background = new SolidColorBrush(Color.FromRgb(22, 25, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                SnapsToDevicePixels = true,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 22,
                    ShadowDepth = 6,
                    Opacity = 0.7
                }
            };

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Search input box with pure vector SVG search icon
            Grid searchGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            TextBox searchBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 36, 48)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(27, 5, 8, 5),
                FontSize = 11.5,
                FontFamily = new FontFamily("Segoe UI, Inter")
            };

            System.Windows.Shapes.Path searchSvg = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 9.5,9.5 L 13.5,13.5 M 11,6 A 5,5 0 1 1 1,6 A 5,5 0 0 1 11,6 Z"),
                Stroke = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                StrokeThickness = 1.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                SnapsToDevicePixels = true
            };
            Viewbox searchIconVb = new Viewbox
            {
                Width = 12,
                Height = 12,
                Child = searchSvg,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(9, 0, 0, 0),
                IsHitTestVisible = false
            };

            TextBlock placeholder = new TextBlock
            {
                Text = "Search fonts...",
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                FontSize = 11.5,
                Margin = new Thickness(27, 6, 0, 0),
                IsHitTestVisible = false
            };

            searchGrid.Children.Add(searchBox);
            searchGrid.Children.Add(searchIconVb);
            searchGrid.Children.Add(placeholder);

            Grid.SetRow(searchGrid, 0);
            grid.Children.Add(searchGrid);

            // List of fonts
            ListBox listBox = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                MaxHeight = 240
            };
            ScrollViewer.SetVerticalScrollBarVisibility(listBox, ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Disabled);

            // Custom ItemTemplate for typeface preview
            DataTemplate itemTemplate = new DataTemplate();
            FrameworkElementFactory tbFactory = new FrameworkElementFactory(typeof(TextBlock));
            tbFactory.SetBinding(TextBlock.TextProperty, new Binding("."));
            tbFactory.SetBinding(TextBlock.FontFamilyProperty, new Binding("."));
            tbFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249)));
            tbFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            tbFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemTemplate.VisualTree = tbFactory;
            listBox.ItemTemplate = itemTemplate;

            // Custom ItemContainerStyle for dark hover / selection
            Style itemStyle = new Style(typeof(ListBoxItem));
            ControlTemplate ct = new ControlTemplate(typeof(ListBoxItem));
            FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "Bd";
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            bd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            bd.SetValue(Border.PaddingProperty, new Thickness(8, 5, 8, 5));
            bd.SetValue(Border.MarginProperty, new Thickness(0, 1, 0, 1));
            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            ct.VisualTree = bd;

            Trigger mouseOver = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            mouseOver.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(37, 47, 66)), "Bd"));
            ct.Triggers.Add(mouseOver);

            Trigger selected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selected.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 58, 138)), "Bd"));
            ct.Triggers.Add(selected);

            itemStyle.Setters.Add(new Setter(ListBoxItem.TemplateProperty, ct));
            listBox.ItemContainerStyle = itemStyle;

            void UpdateList(string query)
            {
                var filtered = string.IsNullOrWhiteSpace(query)
                    ? _installedFontNames
                    : _installedFontNames.Where(f => f.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                listBox.ItemsSource = filtered;
            }

            UpdateList("");
            if (_installedFontNames.Contains(item.NoteFontFamily))
            {
                listBox.SelectedItem = item.NoteFontFamily;
                listBox.ScrollIntoView(item.NoteFontFamily);
            }

            searchBox.TextChanged += (s, e) =>
            {
                placeholder.Visibility = string.IsNullOrEmpty(searchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                UpdateList(searchBox.Text);
            };

            // Selection via mouse click on item (guards against clicking scrollbar)
            listBox.PreviewMouseLeftButtonUp += (s, e) =>
            {
                DependencyObject dep = (DependencyObject)e.OriginalSource;
                while (dep != null && !(dep is ListBoxItem))
                {
                    if (dep is ScrollBar || dep is Thumb || dep is RepeatButton)
                    {
                        return;
                    }
                    dep = VisualTreeHelper.GetParent(dep);
                }
                if (dep is ListBoxItem lbi)
                {
                    string? chosen = (lbi.DataContext as string) ?? (lbi.Content as string);
                    if (!string.IsNullOrEmpty(chosen))
                    {
                        e.Handled = true;
                        onFontSelected(chosen);
                        popup.IsOpen = false;
                    }
                }
            };

            // Keyboard navigation in searchBox and listBox
            searchBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    string? chosen = listBox.SelectedItem as string ?? (listBox.Items.Count > 0 ? listBox.Items[0] as string : null);
                    if (!string.IsNullOrEmpty(chosen))
                    {
                        e.Handled = true;
                        onFontSelected(chosen);
                        popup.IsOpen = false;
                    }
                }
                else if (e.Key == Key.Down)
                {
                    if (listBox.Items.Count > 0)
                    {
                        listBox.Focus();
                        if (listBox.SelectedIndex < 0) listBox.SelectedIndex = 0;
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    e.Handled = true;
                }
            };

            listBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (listBox.SelectedItem is string chosen && !string.IsNullOrEmpty(chosen))
                    {
                        e.Handled = true;
                        onFontSelected(chosen);
                        popup.IsOpen = false;
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    e.Handled = true;
                }
            };

            Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);

            container.Child = grid;
            popup.Child = container;

            popup.Opened += (s, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    searchBox.Focus();
                    searchBox.SelectAll();
                }));
            };

            popup.IsOpen = true;
        }

        private static string GetNoteBgShortLabel(string bgMode)
        {
            return bgMode switch
            {
                "Transparent" => "Trans",
                "Dark Glass" => "Glass",
                "Solid Dark" => "Dark",
                "Yellow Sticky" => "Yellow",
                "Cyan Sticky" => "Cyan",
                _ => "Trans"
            };
        }

        private void ShowNoteBgMenu(FrameworkElement anchor, CardItem item, Action<string> onChosen)
        {
            _isCardSubMenuOpen = true;

            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 3
            };

            cm.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
                if (item.HoverToolbar != null && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!_isCardSubMenuOpen && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                        {
                            item.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            var modes = new (string mode, string label, string iconColor)[]
            {
                ("Transparent", "Transparent (Float on Canvas)", "#94A3B8"),
                ("Dark Glass", "Dark Glass (Frosted Glass)", "#38BDF8"),
                ("Solid Dark", "Solid Dark (Classic Card)", "#64748B"),
                ("Yellow Sticky", "Yellow Sticky (Classic Post-It)", "#FACC15"),
                ("Cyan Sticky", "Cyan Sticky (Note Blue)", "#38BDF8")
            };

            foreach (var (mode, label, iconColor) in modes)
            {
                Border dot = new Border
                {
                    Width = 10,
                    Height = 10,
                    CornerRadius = new CornerRadius(5),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconColor)),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                MenuItem mi = new MenuItem
                {
                    Header = label,
                    Icon = dot,
                    FontWeight = item.NoteBgColor == mode ? FontWeights.Bold : FontWeights.Normal
                };

                string chosenMode = mode;
                mi.Click += (s, e) => onChosen(chosenMode);
                cm.Items.Add(mi);
            }

            cm.Items.Add(new Separator());

            MenuItem miGif = new MenuItem
            {
                Header = "🎬 Set Animated GIF Background...",
                FontWeight = FontWeights.Normal
            };
            miGif.Click += (s, e) => PromptSetNoteBgGif(item);
            cm.Items.Add(miGif);

            if (!string.IsNullOrEmpty(item.NoteBgGifPath) || !string.IsNullOrEmpty(item.NoteBgGifBase64))
            {
                MenuItem miRemGif = new MenuItem
                {
                    Header = "🚫 Remove GIF Background",
                    Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68))
                };
                miRemGif.Click += (s, e) =>
                {
                    ApplyNoteBgGif(item, null, null);
                    ScheduleAutoSave();
                    ShowToast("Removed note background GIF", ToastType.Info);
                };
                cm.Items.Add(miRemGif);
            }

            cm.IsOpen = true;
        }

        private void ShowFontSizeMenu(FrameworkElement anchor, CardItem item, Action<double> onSizeChosen)
        {
            _isCardSubMenuOpen = true;
            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 3
            };
            cm.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
                if (item.HoverToolbar != null && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!_isCardSubMenuOpen && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                        {
                            item.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            double[] sizes = new[] { 10.0, 12.0, 14.0, 16.0, 18.0, 20.0, 24.0, 28.0, 32.0, 36.0, 48.0, 64.0, 72.0 };
            foreach (double sz in sizes)
            {
                MenuItem mi = new MenuItem
                {
                    Header = $"{(int)sz} pt",
                    FontWeight = (int)item.NoteFontSize == (int)sz ? FontWeights.Bold : FontWeights.Normal
                };
                double chosenSz = sz;
                mi.Click += (s, e) => onSizeChosen(chosenSz);
                cm.Items.Add(mi);
            }

            cm.IsOpen = true;
        }

        private void ShowColorPickerPopup(FrameworkElement anchor, CardItem item, Action<string> onColorChosen)
        {
            _isCardSubMenuOpen = true;

            Popup popup = new Popup
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                VerticalOffset = 4
            };

            popup.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
                if (item.HoverToolbar != null && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!_isCardSubMenuOpen && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                        {
                            item.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            Border container = new Border
            {
                Width = 224,
                Background = new SolidColorBrush(Color.FromRgb(22, 25, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                SnapsToDevicePixels = true,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Opacity = 0.75
                }
            };

            StackPanel sp = new StackPanel();

            // Initial color parsing & HSV state
            string initialColor = string.IsNullOrWhiteSpace(item.NoteTextColor) ? "#FFFFFF" : item.NoteTextColor;
            Color initC = Colors.White;
            try
            {
                initC = (Color)ColorConverter.ConvertFromString(initialColor);
            }
            catch { }

            var (initH, initS, initV) = ColorToHsv(initC);
            double currentHue = initH;
            double currentSat = initS;
            double currentVal = initV;

            // 1. Top Bar: Live Preview + Hex Input + Eyedropper Button
            Grid topRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

            Border livePreview = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                Background = new SolidColorBrush(initC)
            };

            TextBox hexBox = new TextBox
            {
                Text = initialColor,
                Background = new SolidColorBrush(Color.FromRgb(30, 36, 48)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                VerticalContentAlignment = VerticalAlignment.Center,
                MaxLength = 9
            };

            // Pipet (Eyedropper) Button
            Border btnEyedropper = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(30, 36, 48)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = "Pick color from screen / canvas (Eyedropper)"
            };

            System.Windows.Shapes.Path pipetIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 14,2 C 14.5,1.5 15.5,1.5 16,2 L 18,4 C 18.5,4.5 18.5,5.5 18,6 L 15.5,8.5 L 11.5,4.5 L 14,2 Z M 10.5,5.5 L 14.5,9.5 L 7,17 L 3,17 L 3,13 L 10.5,5.5 Z M 3,17 L 1,19"),
                Stroke = Brushes.White,
                StrokeThickness = 1.3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                SnapsToDevicePixels = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Viewbox pipetVb = new Viewbox
            {
                Width = 14,
                Height = 14,
                Child = pipetIcon
            };
            btnEyedropper.Child = pipetVb;

            btnEyedropper.MouseEnter += (s, e) =>
            {
                btnEyedropper.Background = new SolidColorBrush(Color.FromArgb(70, 56, 189, 248));
                btnEyedropper.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
            };
            btnEyedropper.MouseLeave += (s, e) =>
            {
                btnEyedropper.Background = new SolidColorBrush(Color.FromRgb(30, 36, 48));
                btnEyedropper.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            };

            btnEyedropper.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                popup.IsOpen = false;
                StartScreenColorPicker(pickedColor =>
                {
                    string hex = $"#{pickedColor.R:X2}{pickedColor.G:X2}{pickedColor.B:X2}";
                    onColorChosen(hex);
                    ShowColorPickerPopup(anchor, item, onColorChosen);
                }, () =>
                {
                    ShowColorPickerPopup(anchor, item, onColorChosen);
                });
            };

            Grid.SetColumn(livePreview, 0);
            Grid.SetColumn(hexBox, 2);
            Grid.SetColumn(btnEyedropper, 4);
            topRow.Children.Add(livePreview);
            topRow.Children.Add(hexBox);
            topRow.Children.Add(btnEyedropper);
            sp.Children.Add(topRow);

            // 2. 2D Saturation / Value Gradient Canvas
            const double svWidth = 200;
            const double svHeight = 100;

            Border svContainer = new Border
            {
                Width = svWidth,
                Height = svHeight,
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Cursor = Cursors.Cross,
                Margin = new Thickness(0, 0, 0, 8)
            };

            Canvas svCanvas = new Canvas { Width = svWidth, Height = svHeight };
            System.Windows.Shapes.Rectangle hueLayer = new System.Windows.Shapes.Rectangle
            {
                Width = svWidth,
                Height = svHeight,
                Fill = new SolidColorBrush(HsvToColor(currentHue, 1.0, 1.0))
            };
            System.Windows.Shapes.Rectangle satLayer = new System.Windows.Shapes.Rectangle
            {
                Width = svWidth,
                Height = svHeight,
                Fill = new LinearGradientBrush(Colors.White, Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(1, 0))
            };
            System.Windows.Shapes.Rectangle valLayer = new System.Windows.Shapes.Rectangle
            {
                Width = svWidth,
                Height = svHeight,
                Fill = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Colors.Black, new Point(0, 0), new Point(0, 1))
            };

            Border svThumb = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 4, ShadowDepth = 1, Opacity = 0.8 }
            };

            Canvas.SetLeft(svThumb, Math.Clamp(currentSat * svWidth - 6, -6, svWidth - 6));
            Canvas.SetTop(svThumb, Math.Clamp((1.0 - currentVal) * svHeight - 6, -6, svHeight - 6));

            svCanvas.Children.Add(hueLayer);
            svCanvas.Children.Add(satLayer);
            svCanvas.Children.Add(valLayer);
            svCanvas.Children.Add(svThumb);
            svContainer.Child = svCanvas;
            sp.Children.Add(svContainer);

            // 3. Rainbow Hue Bar
            const double hueWidth = 200;
            const double hueHeight = 12;

            Border hueContainer = new Border
            {
                Width = hueWidth,
                Height = hueHeight,
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 10)
            };

            Canvas hueCanvas = new Canvas { Width = hueWidth, Height = hueHeight };

            LinearGradientBrush rainbowBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(255, 0, 0), 0.0),
                    new GradientStop(Color.FromRgb(255, 255, 0), 0.17),
                    new GradientStop(Color.FromRgb(0, 255, 0), 0.33),
                    new GradientStop(Color.FromRgb(0, 255, 255), 0.50),
                    new GradientStop(Color.FromRgb(0, 0, 255), 0.67),
                    new GradientStop(Color.FromRgb(255, 0, 255), 0.83),
                    new GradientStop(Color.FromRgb(255, 0, 0), 1.0)
                }
            };

            System.Windows.Shapes.Rectangle hueTrack = new System.Windows.Shapes.Rectangle { Width = hueWidth, Height = hueHeight, Fill = rainbowBrush };

            Border hueThumb = new Border
            {
                Width = 8,
                Height = 14,
                CornerRadius = new CornerRadius(2),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 4, ShadowDepth = 1, Opacity = 0.8 }
            };

            Canvas.SetLeft(hueThumb, Math.Clamp((currentHue / 360.0) * hueWidth - 4, -4, hueWidth - 4));
            Canvas.SetTop(hueThumb, -1);

            hueCanvas.Children.Add(hueTrack);
            hueCanvas.Children.Add(hueThumb);
            hueContainer.Child = hueCanvas;
            sp.Children.Add(hueContainer);

            // Synchronizing handlers
            bool suppressUpdate = false;

            void UpdateColorOutput()
            {
                Color c = HsvToColor(currentHue, currentSat, currentVal);
                string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                suppressUpdate = true;
                hexBox.Text = hex;
                suppressUpdate = false;
                livePreview.Background = new SolidColorBrush(c);
                onColorChosen(hex);
            }

            void SyncFromColor(Color c)
            {
                var (h, s, v) = ColorToHsv(c);
                currentHue = h;
                currentSat = s;
                currentVal = v;

                hueLayer.Fill = new SolidColorBrush(HsvToColor(currentHue, 1.0, 1.0));
                Canvas.SetLeft(svThumb, Math.Clamp(currentSat * svWidth - 6, -6, svWidth - 6));
                Canvas.SetTop(svThumb, Math.Clamp((1.0 - currentVal) * svHeight - 6, -6, svHeight - 6));
                Canvas.SetLeft(hueThumb, Math.Clamp((currentHue / 360.0) * hueWidth - 4, -4, hueWidth - 4));
                livePreview.Background = new SolidColorBrush(c);
            }

            // SV Drag logic
            bool isDraggingSv = false;
            void HandleSvMove(Point pt)
            {
                currentSat = Math.Clamp(pt.X / svWidth, 0.0, 1.0);
                currentVal = Math.Clamp(1.0 - (pt.Y / svHeight), 0.0, 1.0);

                Canvas.SetLeft(svThumb, Math.Clamp(currentSat * svWidth - 6, -6, svWidth - 6));
                Canvas.SetTop(svThumb, Math.Clamp((1.0 - currentVal) * svHeight - 6, -6, svHeight - 6));

                UpdateColorOutput();
            }

            svContainer.MouseLeftButtonDown += (s, e) =>
            {
                isDraggingSv = true;
                svContainer.CaptureMouse();
                HandleSvMove(e.GetPosition(svContainer));
                e.Handled = true;
            };
            svContainer.MouseMove += (s, e) =>
            {
                if (isDraggingSv)
                {
                    HandleSvMove(e.GetPosition(svContainer));
                    e.Handled = true;
                }
            };
            svContainer.MouseLeftButtonUp += (s, e) =>
            {
                if (isDraggingSv)
                {
                    isDraggingSv = false;
                    svContainer.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };

            // Hue Drag logic
            bool isDraggingHue = false;
            void HandleHueMove(Point pt)
            {
                currentHue = Math.Clamp((pt.X / hueWidth) * 360.0, 0.0, 360.0);
                if (currentHue >= 360.0) currentHue = 0.0;

                Canvas.SetLeft(hueThumb, Math.Clamp((currentHue / 360.0) * hueWidth - 4, -4, hueWidth - 4));
                hueLayer.Fill = new SolidColorBrush(HsvToColor(currentHue, 1.0, 1.0));

                UpdateColorOutput();
            }

            hueContainer.MouseLeftButtonDown += (s, e) =>
            {
                isDraggingHue = true;
                hueContainer.CaptureMouse();
                HandleHueMove(e.GetPosition(hueContainer));
                e.Handled = true;
            };
            hueContainer.MouseMove += (s, e) =>
            {
                if (isDraggingHue)
                {
                    HandleHueMove(e.GetPosition(hueContainer));
                    e.Handled = true;
                }
            };
            hueContainer.MouseLeftButtonUp += (s, e) =>
            {
                if (isDraggingHue)
                {
                    isDraggingHue = false;
                    hueContainer.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };

            void ApplyHex(string hex)
            {
                if (suppressUpdate) return;
                if (string.IsNullOrWhiteSpace(hex)) return;
                hex = hex.Trim();
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (hex.Length == 4 || hex.Length == 7 || hex.Length == 9)
                {
                    try
                    {
                        Color c = (Color)ColorConverter.ConvertFromString(hex);
                        SyncFromColor(c);
                        onColorChosen(hex);
                    }
                    catch { }
                }
            }

            hexBox.TextChanged += (s, e) =>
            {
                ApplyHex(hexBox.Text);
            };

            // 4. Presets Header
            TextBlock presetsLabel = new TextBlock
            {
                Text = "PRESETS",
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 2, 0, 6)
            };
            sp.Children.Add(presetsLabel);

            // 5. Preset Colors Grid (18 curated colors in 6x3)
            string[][] palette = new string[][]
            {
                new[] { "#FFFFFF", "#D1D5DB", "#9CA3AF", "#4B5563", "#1F2937", "#000000" },
                new[] { "#EF4444", "#F97316", "#FBBF24", "#10B981", "#06B6D4", "#3B82F6" },
                new[] { "#6366F1", "#8B5CF6", "#EC4899", "#F43F5E", "#84CC16", "#14B8A6" }
            };

            foreach (var row in palette)
            {
                StackPanel rowSp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                foreach (string colorHex in row)
                {
                    Border swatch = new Border
                    {
                        Width = 24,
                        Height = 24,
                        CornerRadius = new CornerRadius(12),
                        Margin = new Thickness(2.5),
                        Cursor = Cursors.Hand,
                        ToolTip = colorHex,
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
                    };

                    try
                    {
                        swatch.Background = (Brush)new BrushConverter().ConvertFromString(colorHex)!;
                    }
                    catch { }

                    swatch.MouseEnter += (s, e) =>
                    {
                        swatch.BorderBrush = Brushes.White;
                        swatch.BorderThickness = new Thickness(2);
                    };
                    swatch.MouseLeave += (s, e) =>
                    {
                        swatch.BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                        swatch.BorderThickness = new Thickness(1);
                    };
                    string hexToApply = colorHex;
                    swatch.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        hexBox.Text = hexToApply;
                        ApplyHex(hexToApply);
                        popup.IsOpen = false;
                    };

                    rowSp.Children.Add(swatch);
                }

                sp.Children.Add(rowSp);
            }

            container.Child = sp;
            popup.Child = container;

            popup.Opened += (s, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    hexBox.Focus();
                    hexBox.SelectAll();
                }));
            };

            popup.IsOpen = true;
        }

        private void StartScreenColorPicker(Action<Color> onColorPicked, Action onCancelled)
        {
            Window overlay = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
                Topmost = true,
                ShowInTaskbar = false,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight,
                Cursor = Cursors.Cross
            };

            Canvas canvas = new Canvas { IsHitTestVisible = false };
            overlay.Content = canvas;

            Border loupeCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 24, 33)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 10, 6),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.85
                }
            };

            StackPanel loupeSp = new StackPanel { Orientation = Orientation.Horizontal };

            Border swatchCircle = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.White,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.White
            };

            StackPanel textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock hexText = new TextBlock
            {
                Text = "#FFFFFF",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White
            };
            TextBlock hintText = new TextBlock
            {
                Text = "Click to pick • Esc to cancel",
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                Margin = new Thickness(0, 1, 0, 0)
            };
            textCol.Children.Add(hexText);
            textCol.Children.Add(hintText);

            loupeSp.Children.Add(swatchCircle);
            loupeSp.Children.Add(textCol);
            loupeCard.Child = loupeSp;

            canvas.Children.Add(loupeCard);

            Color lastColor = Colors.White;

            void SampleAtCursor(MouseEventArgs? e = null)
            {
                GetCursorPos(out POINT screenPt);
                IntPtr hdc = GetDC(IntPtr.Zero);
                uint pixel = GetPixel(hdc, screenPt.X, screenPt.Y);
                ReleaseDC(IntPtr.Zero, hdc);

                if (pixel != 0xFFFFFFFF)
                {
                    byte r = (byte)(pixel & 0xFF);
                    byte g = (byte)((pixel >> 8) & 0xFF);
                    byte b = (byte)((pixel >> 16) & 0xFF);
                    lastColor = Color.FromRgb(r, g, b);

                    swatchCircle.Background = new SolidColorBrush(lastColor);
                    hexText.Text = $"#{r:X2}{g:X2}{b:X2}";
                }

                Point mousePos = e != null ? e.GetPosition(overlay) : new Point(screenPt.X - overlay.Left, screenPt.Y - overlay.Top);
                double cardX = mousePos.X + 18;
                double cardY = mousePos.Y + 18;

                if (cardX + 180 > overlay.ActualWidth) cardX = mousePos.X - 190;
                if (cardY + 50 > overlay.ActualHeight) cardY = mousePos.Y - 60;

                Canvas.SetLeft(loupeCard, Math.Max(8, cardX));
                Canvas.SetTop(loupeCard, Math.Max(8, cardY));
            }

            overlay.MouseMove += (s, e) => SampleAtCursor(e);

            bool finished = false;

            overlay.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (finished) return;
                finished = true;
                e.Handled = true;
                SampleAtCursor();
                overlay.Close();
                onColorPicked(lastColor);
            };

            overlay.PreviewMouseRightButtonDown += (s, e) =>
            {
                if (finished) return;
                finished = true;
                e.Handled = true;
                overlay.Close();
                onCancelled?.Invoke();
            };

            overlay.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    if (finished) return;
                    finished = true;
                    e.Handled = true;
                    overlay.Close();
                    onCancelled?.Invoke();
                }
            };

            overlay.Loaded += (s, e) =>
            {
                overlay.Activate();
                overlay.Focus();
                SampleAtCursor();
            };

            overlay.Show();
        }

        private static (double h, double s, double v) ColorToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0.00001)
            {
                if (Math.Abs(max - r) < 0.00001)
                    h = 60 * (((g - b) / delta) % 6);
                else if (Math.Abs(max - g) < 0.00001)
                    h = 60 * (((b - r) / delta) + 2);
                else
                    h = 60 * (((r - g) / delta) + 4);

                if (h < 0) h += 360;
            }

            double s = max < 0.00001 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
        }

        private static Color HsvToColor(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = v - c;

            double rPrime = 0, gPrime = 0, bPrime = 0;
            if (h >= 0 && h < 60) { rPrime = c; gPrime = x; bPrime = 0; }
            else if (h >= 60 && h < 120) { rPrime = x; gPrime = c; bPrime = 0; }
            else if (h >= 120 && h < 180) { rPrime = 0; gPrime = c; bPrime = x; }
            else if (h >= 180 && h < 240) { rPrime = 0; gPrime = x; bPrime = c; }
            else if (h >= 240 && h < 300) { rPrime = x; gPrime = 0; bPrime = c; }
            else { rPrime = c; gPrime = 0; bPrime = x; }

            byte r = (byte)Math.Clamp((int)Math.Round((rPrime + m) * 255), 0, 255);
            byte g = (byte)Math.Clamp((int)Math.Round((gPrime + m) * 255), 0, 255);
            byte b = (byte)Math.Clamp((int)Math.Round((bPrime + m) * 255), 0, 255);

            return Color.FromRgb(r, g, b);
        }

        private void ShowNoteMoreMenu(FrameworkElement anchor, CardItem item)
        {
            _isCardSubMenuOpen = true;
            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 3
            };
            cm.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
                if (item.HoverToolbar != null && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!_isCardSubMenuOpen && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                        {
                            item.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            MenuItem CreateIconMenuItem(string header, string svgPathData, Action onClick, Color? textColor = null, Color? iconColor = null)
            {
                var mi = new MenuItem
                {
                    Header = header,
                    Foreground = new SolidColorBrush(textColor ?? Color.FromRgb(241, 245, 249)),
                    FontSize = 12
                };

                var p = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse(svgPathData),
                    Stroke = new SolidColorBrush(iconColor ?? textColor ?? Color.FromRgb(148, 163, 184)),
                    StrokeThickness = 1.6,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    SnapsToDevicePixels = true
                };

                var vb = new Viewbox
                {
                    Width = 14,
                    Height = 14,
                    Child = p,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                mi.Icon = vb;
                mi.Click += (s, e) => onClick();
                return mi;
            }

            MenuItem miCopy = CreateIconMenuItem(
                "Copy Note Text",
                "M8 7v10a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2H10a2 2 0 0 0-2 2z M4 17V5a2 2 0 0 1 2-2h12",
                () =>
                {
                    try
                    {
                        Clipboard.SetText(item.NoteText);
                        ShowToast("Copied note text to clipboard", ToastType.Success);
                    }
                    catch (Exception ex)
                    {
                        ShowToast("Copy failed: " + ex.Message, ToastType.Error);
                    }
                });

            MenuItem miDup = CreateIconMenuItem(
                "Duplicate Note",
                "M15 2H6a2 2 0 0 0-2 2v12 M9 6h11a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H9a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2z",
                () => DuplicateNoteCard(item));

            MenuItem miZoom = CreateIconMenuItem(
                "Zoom to Note",
                "M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16z M21 21l-4.35-4.35",
                () => ZoomToCard(item));

            MenuItem miDel = CreateIconMenuItem(
                "Delete Note",
                "M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2 M10 11v6 M14 11v6",
                () => RemoveCard(item),
                textColor: Color.FromRgb(239, 68, 68),
                iconColor: Color.FromRgb(239, 68, 68));
            miDel.FontWeight = FontWeights.SemiBold;

            MenuItem miGif = CreateIconMenuItem(
                "Set GIF Background...",
                "M4 4h16v16H4z M8 12l4 4 4-4",
                () => PromptSetNoteBgGif(item),
                iconColor: Color.FromRgb(244, 114, 182));

            cm.Items.Add(miCopy);
            cm.Items.Add(miDup);
            cm.Items.Add(miGif);
            cm.Items.Add(miZoom);
            cm.Items.Add(new Separator());
            cm.Items.Add(miDel);

            cm.IsOpen = true;
        }

        private void ShowDrawColorMenu(FrameworkElement anchor, CardItem item, Action<string> onColorChosen)
        {
            _isCardSubMenuOpen = true;
            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 3,
                Background = new SolidColorBrush(Color.FromRgb(22, 27, 36)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Padding = new Thickness(4)
            };
            cm.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
            };

            var colors = new (string Name, string Hex)[]
            {
                ("Cyan", "#38BDF8"),
                ("White", "#FFFFFF"),
                ("Yellow", "#FBBF24"),
                ("Red", "#EF4444"),
                ("Green", "#22C55E"),
                ("Orange", "#F97316"),
                ("Dark Slate", "#1E293B")
            };

            foreach (var (name, hex) in colors)
            {
                MenuItem mi = new MenuItem
                {
                    Header = name,
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    FontSize = 11.5
                };

                Border dot = new Border
                {
                    Width = 12,
                    Height = 12,
                    CornerRadius = new CornerRadius(6),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.White,
                    Background = (Brush)new BrushConverter().ConvertFromString(hex)!
                };
                mi.Icon = dot;
                mi.Click += (s, e) => onColorChosen(hex);
                cm.Items.Add(mi);
            }

            cm.IsOpen = true;
        }

        private void CopyDrawCardToClipboard(CardItem item)
        {
            if (item.DrawCanvas == null) return;
            try
            {
                int w = Math.Max(10, (int)item.Width);
                int h = Math.Max(10, (int)item.Height);
                RenderTargetBitmap rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(item.DrawCanvas);
                Clipboard.SetImage(rtb);
                ShowToast("Copied sketch to clipboard as transparent PNG!", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to copy sketch: {ex.Message}", ToastType.Error);
            }
        }

        private void ShowPaletteCardMoreMenu(FrameworkElement anchor, CardItem item)
        {
            _isCardSubMenuOpen = true;
            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 3
            };
            cm.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
                if (item.HoverToolbar != null && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                {
                    DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                    anim.Completed += (s2, e2) =>
                    {
                        if (!_isCardSubMenuOpen && !item.Container.IsMouseOver && !item.HoverToolbar.IsMouseOver && !item.IsSelected)
                        {
                            item.HoverToolbar.IsHitTestVisible = false;
                        }
                    };
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
                }
            };

            MenuItem miPins = new MenuItem { Header = "📍 Toggle Sampling Pins" };
            miPins.Click += (s, e) =>
            {
                if (item.LinkedSourceImageCard == null || !_cards.Contains(item.LinkedSourceImageCard))
                {
                    var candidate = _selectedCards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null)
                                 ?? _cards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null);
                    if (candidate != null)
                    {
                        item.LinkedSourceImageCard = candidate;
                        candidate.LinkedPaletteCard = item;
                        if (candidate.ActivePalettePins == null || candidate.ActivePalettePins.Count == 0)
                        {
                            candidate.ActivePalettePins = item.ActivePalettePins;
                        }
                        ScheduleAutoSave();
                        ShowToast("Connected palette to reference image!", ToastType.Success);
                    }
                }

                if (item.LinkedSourceImageCard != null)
                {
                    var src = item.LinkedSourceImageCard;
                    src.LinkedPaletteCard = item;
                    if (src.ActivePalettePins == null || src.ActivePalettePins.Count == 0)
                    {
                        src.ActivePalettePins = item.ActivePalettePins;
                    }

                    if (src.IsPaletteMode)
                        CloseCanvasPaletteMode(src);
                    else
                        StartCanvasPaletteMode(src);
                }
                else
                {
                    ShowToast("Select an image to link this palette to", ToastType.Info);
                }
            };

            MenuItem miAe = new MenuItem { Header = "🎬 Export to After Effects" };
            miAe.Click += (s, e) => ExportCanvasPaletteToAe(item);

            MenuItem miSave = new MenuItem { Header = "💾 Save Palette Image (PNG)" };
            miSave.Click += (s, e) => SavePaletteCardImage(item);

            MenuItem miFolder = new MenuItem { Header = "📁 Open Palettes Folder" };
            miFolder.Click += (s, e) => OpenPalettesFolder();

            MenuItem miCopy = new MenuItem { Header = "📋 Copy Palette Image" };
            miCopy.Click += (s, e) => CopyCardToClipboard(item);

            MenuItem miZoom = new MenuItem { Header = "🔍 Zoom to Palette" };
            miZoom.Click += (s, e) => ZoomToCard(item);

            MenuItem miDel = new MenuItem
            {
                Header = "🗑 Delete Palette Card",
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontWeight = FontWeights.SemiBold
            };
            miDel.Click += (s, e) => RemoveCard(item);

            cm.Items.Add(miPins);
            cm.Items.Add(miAe);
            cm.Items.Add(miSave);
            cm.Items.Add(miFolder);
            cm.Items.Add(miCopy);
            cm.Items.Add(miZoom);
            cm.Items.Add(new Separator());
            cm.Items.Add(miDel);

            cm.IsOpen = true;
        }

        #region Standalone Live Color Palette Card System

        private CardItem AddPaletteCard(
            CardItem? sourceCard = null,
            List<PalettePin>? initialPins = null,
            Point? worldPosition = null,
            double? customWidth = null,
            double? customHeight = null,
            ColorMood mood = ColorMood.Colorful,
            int colorCount = 5,
            int rows = 1,
            bool autoSelect = true)
        {
            EmptyStateOverlay.Visibility = Visibility.Collapsed;

            double w = customWidth ?? 460.0;
            double h = customHeight ?? (rows == 2 ? 260.0 : 200.0);

            // 1x1 frozen transparent BitmapSource as dummy bitmap for CardItem requirements
            BitmapSource dummyBmp = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
            dummyBmp.Freeze();

            Image dummyImg = new Image
            {
                Source = dummyBmp,
                Visibility = Visibility.Collapsed
            };

            // Swatches Grid
            Grid swatchesGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Rounded clip container so swatches cleanly follow card's 14px border radius
            Border swatchesClipBorder = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Child = swatchesGrid
            };
            swatchesClipBorder.SizeChanged += (s, e) =>
            {
                if (swatchesClipBorder.ActualWidth > 0 && swatchesClipBorder.ActualHeight > 0)
                {
                    swatchesClipBorder.Clip = new RectangleGeometry
                    {
                        Rect = new Rect(0, 0, swatchesClipBorder.ActualWidth, swatchesClipBorder.ActualHeight),
                        RadiusX = 14,
                        RadiusY = 14
                    };
                }
            };

            // Outer container border: 100% transparent background, subtle sleek border & soft drop shadow
            Border contentBorder = new Border
            {
                Width = w,
                Height = h,
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                SnapsToDevicePixels = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Opacity = 0.55,
                    Color = Colors.Black
                },
                Child = swatchesClipBorder
            };

            Grid container = new Grid
            {
                Width = w,
                Height = h,
                Cursor = Cursors.SizeAll
            };
            container.Children.Add(dummyImg);
            container.Children.Add(contentBorder);

            // 4 Corner Handles
            Border handleTL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Top, Cursors.SizeNWSE);
            Border handleTR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Top, Cursors.SizeNESW);
            Border handleBL = CreateResizeHandle(HorizontalAlignment.Left, VerticalAlignment.Bottom, Cursors.SizeNESW);
            Border handleBR = CreateResizeHandle(HorizontalAlignment.Right, VerticalAlignment.Bottom, Cursors.SizeNWSE);

            container.Children.Add(handleTL);
            container.Children.Add(handleTR);
            container.Children.Add(handleBL);
            container.Children.Add(handleBR);

            CardItem item = new CardItem
            {
                Container = container,
                ContentBorder = contentBorder,
                ImageControl = dummyImg,
                Bitmap = dummyBmp,
                OriginalBitmap = dummyBmp,
                BaseWidth = w,
                BaseHeight = h,
                AspectRatio = w / Math.Max(1.0, h),
                HandleTL = handleTL,
                HandleTR = handleTR,
                HandleBL = handleBL,
                HandleBR = handleBR,
                IsPaletteCard = true,
                LinkedSourceImageCard = sourceCard,
                PaletteColorCount = colorCount,
                PaletteMood = mood,
                PaletteRows = rows,
                PaletteGridContent = swatchesGrid,
                PaletteTitleText = null
            };

            if (initialPins != null && initialPins.Count > 0)
            {
                item.ActivePalettePins = new List<PalettePin>(initialPins);
            }
            else if (sourceCard != null)
            {
                BitmapSource? bmp = sourceCard.Bitmap ?? sourceCard.OriginalBitmap;
                if (bmp != null)
                {
                    item.ActivePalettePins = ColorPaletteExtractor.ExtractPalette(bmp, colorCount, mood, 0);
                }
            }

            UpdatePaletteCardContent(item);

            // Attach Hover Quick-Action Toolbar for Palette Card
            Border hoverToolbar = CreateCardHoverToolbar(item);
            item.HoverToolbar = hoverToolbar;
            Canvas toolbarHost = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 0,
                Height = 0,
                ClipToBounds = false
            };
            Panel.SetZIndex(toolbarHost, 9999);
            Canvas.SetTop(hoverToolbar, -38.0);
            toolbarHost.Children.Add(hoverToolbar);
            container.Children.Add(toolbarHost);

            hoverToolbar.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0)
                {
                    Canvas.SetLeft(hoverToolbar, -e.NewSize.Width / 2.0);
                    Canvas.SetTop(hoverToolbar, -38.0);
                }
            };

            container.MouseEnter += (s, e) =>
            {
                if (_isCropping) return;
                if (Panel.GetZIndex(container) < 500 && !item.IsSelected)
                {
                    Panel.SetZIndex(container, 500);
                }
                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                hoverToolbar.Opacity = 1.0;
                hoverToolbar.IsHitTestVisible = true;
            };
            container.MouseLeave += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    if (Panel.GetZIndex(container) == 500)
                    {
                        Panel.SetZIndex(container, 25);
                    }
                }
                if (item.IsSelected || _isCardSubMenuOpen) return;
                DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(180));
                anim.Completed += (s2, e2) =>
                {
                    if (!container.IsMouseOver && !hoverToolbar.IsMouseOver && !item.IsSelected && !_isCardSubMenuOpen)
                    {
                        hoverToolbar.IsHitTestVisible = false;
                    }
                };
                hoverToolbar.BeginAnimation(UIElement.OpacityProperty, anim);
            };

            // Attach Right-Click Context Menu for Palette Card
            container.ContextMenu = CreateCardContextMenu(item);
            container.MouseRightButtonDown += (s, e) =>
            {
                if (!item.IsSelected)
                {
                    SelectCard(item, addToSelection: false);
                }
                e.Handled = true;
            };
            container.MouseRightButtonUp += (s, e) =>
            {
                container.ContextMenu = CreateCardContextMenu(item);
                if (container.ContextMenu != null)
                {
                    container.ContextMenu.PlacementTarget = container;
                    container.ContextMenu.IsOpen = true;
                }
                e.Handled = true;
            };

            // Solo Drag interaction (movable anywhere on canvas independently)
            container.MouseLeftButtonDown += (s, e) =>
            {
                if (Keyboard.IsKeyDown(Key.Space)) return;

                if (e.ClickCount == 2)
                {
                    SelectCard(item, addToSelection: false);
                    ZoomToCard(item);
                    e.Handled = true;
                    return;
                }

                RecordUndo("Move Palette Card");

                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (isShift)
                {
                    if (item.IsSelected) DeselectCard(item);
                    else SelectCard(item, addToSelection: true);
                }
                else
                {
                    if (!item.IsSelected) SelectCard(item, addToSelection: false);
                }

                _isDraggingCards = true;
                _cardDragStartMousePoint = e.GetPosition(CanvasContainer);
                _cardsInitialPositions.Clear();
                foreach (CardItem sel in _selectedCards)
                {
                    _cardsInitialPositions[sel] = new Point(sel.X, sel.Y);
                }
                SetWebViewHitTesting(false);
                CanvasContainer.CaptureMouse();
                e.Handled = true;
            };

            // Resize handle events
            AttachResizeHandleEvents(item, handleTL, ResizeCorner.TopLeft);
            AttachResizeHandleEvents(item, handleTR, ResizeCorner.TopRight);
            AttachResizeHandleEvents(item, handleBL, ResizeCorner.BottomLeft);
            AttachResizeHandleEvents(item, handleBR, ResizeCorner.BottomRight);

            // Placement
            Point pos;
            if (worldPosition.HasValue)
            {
                pos = worldPosition.Value;
            }
            else if (sourceCard != null)
            {
                pos = new Point(sourceCard.X + sourceCard.Width + 24, sourceCard.Y);
            }
            else
            {
                Matrix matrix = CanvasMatrixTransform.Matrix;
                matrix.Invert();
                Point centerScreen = new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
                pos = matrix.Transform(centerScreen);
            }

            item.X = pos.X;
            item.Y = pos.Y;

            if (!_isRestoringSession && !_isApplyingSnapshot)
            {
                RecordUndo("Add Palette Card");
            }

            _cards.Add(item);
            WorldCanvas.Children.Add(container);
            Panel.SetZIndex(container, 25);

            if (sourceCard != null)
            {
                sourceCard.LinkedPaletteCard = item;
                item.LinkedSourceImageCard = sourceCard;
                if (!string.IsNullOrEmpty(sourceCard.GroupId))
                {
                    item.GroupId = sourceCard.GroupId;
                    var grp = _groups.FirstOrDefault(g => g.Id == sourceCard.GroupId);
                    if (grp != null)
                    {
                        FitGroupToCards(grp, recordUndo: false);
                    }
                }
            }
            CheckCardGroupAffiliation(item);

            UpdateStatusCounts();
            if (autoSelect)
            {
                SelectCard(item, addToSelection: false);
            }

            if (!_isRestoringSession)
            {
                ScheduleAutoSave();
            }

            return item;
        }

        private void UpdatePaletteCardContent(CardItem paletteCard)
        {
            if (paletteCard.PaletteGridContent == null) return;
            paletteCard.PaletteGridContent.Children.Clear();
            paletteCard.PaletteGridContent.ColumnDefinitions.Clear();
            paletteCard.PaletteGridContent.RowDefinitions.Clear();

            var pins = paletteCard.ActivePalettePins;
            if (pins == null || pins.Count == 0) return;

            int count = pins.Count;
            bool isTwoRows = paletteCard.PaletteRows == 2 && count > 1;

            if (isTwoRows)
            {
                paletteCard.PaletteGridContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                paletteCard.PaletteGridContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                int topCount = (int)Math.Ceiling(count / 2.0);
                int bottomCount = count - topCount;

                Grid topGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                for (int i = 0; i < topCount; i++)
                {
                    topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                Grid bottomGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                for (int i = 0; i < bottomCount; i++)
                {
                    bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                for (int i = 0; i < topCount; i++)
                {
                    var cell = CreateSwatchCell(paletteCard, pins[i], isTwoRows: true);
                    Grid.SetColumn(cell, i);
                    topGrid.Children.Add(cell);
                }

                for (int i = 0; i < bottomCount; i++)
                {
                    var cell = CreateSwatchCell(paletteCard, pins[topCount + i], isTwoRows: true);
                    Grid.SetColumn(cell, i);
                    bottomGrid.Children.Add(cell);
                }

                Grid.SetRow(topGrid, 0);
                Grid.SetRow(bottomGrid, 1);
                paletteCard.PaletteGridContent.Children.Add(topGrid);
                paletteCard.PaletteGridContent.Children.Add(bottomGrid);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    paletteCard.PaletteGridContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                for (int i = 0; i < count; i++)
                {
                    var cell = CreateSwatchCell(paletteCard, pins[i], isTwoRows: false);
                    Grid.SetColumn(cell, i);
                    paletteCard.PaletteGridContent.Children.Add(cell);
                }
            }
        }

        private FrameworkElement CreateSwatchCell(CardItem paletteCard, PalettePin pin, bool isTwoRows)
        {
            // Perceived luminance calculation for high-contrast legible text
            double lum = (0.299 * pin.Color.R + 0.587 * pin.Color.G + 0.114 * pin.Color.B) / 255.0;
            bool isLight = lum > 0.58;

            Color textCol = isLight ? Color.FromArgb(220, 20, 24, 33) : Color.FromArgb(245, 255, 255, 255);
            Color subCol = isLight ? Color.FromArgb(160, 50, 55, 65) : Color.FromArgb(180, 255, 255, 255);
            Brush textBrush = new SolidColorBrush(textCol);
            Brush subBrush = new SolidColorBrush(subCol);

            Grid cellGrid = new Grid
            {
                Background = new SolidColorBrush(pin.Color),
                Cursor = Cursors.Hand,
                ToolTip = $"Click to copy {pin.Hex}\nRight-click to change color (Color Picker)\nRGB: {pin.Color.R}, {pin.Color.G}, {pin.Color.B}"
            };

            // Subtle hover overlay
            Border hoverOverlay = new Border
            {
                Background = Brushes.White,
                Opacity = 0,
                IsHitTestVisible = false
            };
            cellGrid.MouseEnter += (s, e) => hoverOverlay.Opacity = 0.12;
            cellGrid.MouseLeave += (s, e) => hoverOverlay.Opacity = 0;

            // Bottom bar: Hex text + copy icon button (Seamless Coolors / Adobe style)
            Border copyPill = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(isTwoRows ? 4 : 6, 0, isTwoRows ? 4 : 6, isTwoRows ? 5 : 8),
                Padding = new Thickness(isTwoRows ? 3 : 5, 2, isTwoRows ? 3 : 5, 2),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = $"Click to copy {pin.Hex}",
                IsHitTestVisible = true
            };

            Grid pillGrid = new Grid();
            pillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pillGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock hexTb = new TextBlock
            {
                Text = pin.Hex,
                FontSize = isTwoRows ? 9.5 : 11.0,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas, Segoe UI, monospace"),
                Foreground = textBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(hexTb, 0);
            pillGrid.Children.Add(hexTb);

            System.Windows.Shapes.Path copyIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M19,21H8V7h11m0-2H8a2,2 0 0,0-2,2v14a2,2 0 0,0 2,2h11a2,2 0 0,0 2-2V7a2,2 0 0,0-2-2m-3-4H4a2,2 0 0,0-2,2v14h2V3h12V1Z"),
                Fill = subBrush,
                Width = isTwoRows ? 11 : 13,
                Height = isTwoRows ? 11 : 13,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                Opacity = 0.85
            };
            Grid.SetColumn(copyIcon, 1);
            pillGrid.Children.Add(copyIcon);
            copyPill.Child = pillGrid;

            Color pillHoverCol = isLight ? Color.FromArgb(40, 0, 0, 0) : Color.FromArgb(45, 255, 255, 255);
            copyPill.MouseEnter += (s, e) =>
            {
                copyPill.Background = new SolidColorBrush(pillHoverCol);
                copyIcon.Opacity = 1.0;
            };
            copyPill.MouseLeave += (s, e) =>
            {
                copyPill.Background = Brushes.Transparent;
                copyIcon.Opacity = 0.85;
            };

            string curHex = pin.Hex;
            Action performCopy = () =>
            {
                try
                {
                    Clipboard.SetText(curHex);
                    ShowToast($"Copied {curHex} to clipboard!", ToastType.Success);

                    // Morph icon into checkmark for 800ms
                    copyIcon.Data = Geometry.Parse("M9,20.42L2.79,14.21L5.62,11.38L9,14.77L18.88,4.88L21.71,7.71L9,20.42Z");
                    copyIcon.Fill = isLight ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(52, 211, 153));
                    DispatcherTimer dt = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
                    dt.Tick += (s, e) =>
                    {
                        dt.Stop();
                        copyIcon.Data = Geometry.Parse("M19,21H8V7h11m0-2H8a2,2 0 0,0-2,2v14a2,2 0 0,0 2,2h11a2,2 0 0,0 2-2V7a2,2 0 0,0-2-2m-3-4H4a2,2 0 0,0-2,2v14h2V3h12V1Z");
                        copyIcon.Fill = subBrush;
                    };
                    dt.Start();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Clipboard] Copy error: {ex.Message}");
                }
            };

            copyPill.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true; // Stop bubbling so card drag is NOT initiated on copy click!
                performCopy();
            };

            cellGrid.Children.Add(hoverOverlay);
            cellGrid.Children.Add(copyPill);

            // Right-click context menu: Pick custom color or copy hex
            ContextMenu swatchMenu = new ContextMenu();
            MenuItem miPick = new MenuItem
            {
                Header = "🎨 Change Color (Color Picker)...",
                FontWeight = FontWeights.Bold
            };
            miPick.Click += (s, e) =>
            {
                ShowColorPickerPopup(cellGrid, paletteCard, chosenHex =>
                {
                    try
                    {
                        Color c = (Color)ColorConverter.ConvertFromString(chosenHex);
                        pin.Color = c;
                        paletteCard.PaletteMoodCache[paletteCard.PaletteMood] = paletteCard.ActivePalettePins.Select(p => new PalettePin
                        {
                            RelX = p.RelX,
                            RelY = p.RelY,
                            Color = p.Color,
                            IsLocked = p.IsLocked
                        }).ToList();

                        UpdatePaletteCardContent(paletteCard);
                        if (paletteCard.LinkedSourceImageCard != null && paletteCard.LinkedSourceImageCard.IsPaletteMode)
                        {
                            RenderCanvasPalettePins(paletteCard.LinkedSourceImageCard);
                        }
                        ScheduleAutoSave();
                    }
                    catch { }
                });
            };

            MenuItem miCopy = new MenuItem { Header = $"📋 Copy Hex ({pin.Hex})" };
            miCopy.Click += (s, e) => performCopy();

            swatchMenu.Items.Add(miPick);
            swatchMenu.Items.Add(miCopy);

            cellGrid.ContextMenu = swatchMenu;
            cellGrid.MouseRightButtonDown += (s, e) =>
            {
                swatchMenu.PlacementTarget = cellGrid;
                swatchMenu.IsOpen = true;
                e.Handled = true;
            };

            // Swatch body click: clicking anywhere on the color swatch (without dragging > 5px) also copies hex!
            cellGrid.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _pendingSwatchClick = (paletteCard, performCopy);
                _pendingSwatchStartPoint = e.GetPosition(CanvasContainer);
            };

            return cellGrid;
        }

        private void StartCanvasPaletteMode(CardItem item)
        {
            if (item == null || item.IsNote || item.IsPaletteCard) return;
            BitmapSource? bmp = item.Bitmap ?? item.OriginalBitmap;
            if (bmp == null)
            {
                ShowToast("Cannot extract palette from this card", ToastType.Error);
                return;
            }

            item.IsPaletteMode = true;

            // Check if we already have a linked palette card on board
            CardItem? paletteCard = item.LinkedPaletteCard;
            if (paletteCard == null || !_cards.Contains(paletteCard))
            {
                paletteCard = _cards.FirstOrDefault(c => c.IsPaletteCard && (c.LinkedSourceImageCard == item || c.LinkedSourceImageCard == null));
                if (paletteCard != null)
                {
                    item.LinkedPaletteCard = paletteCard;
                    paletteCard.LinkedSourceImageCard = item;
                }
            }

            if (paletteCard == null || !_cards.Contains(paletteCard))
            {
                if (item.ActivePalettePins == null || item.ActivePalettePins.Count == 0)
                {
                    item.ActivePalettePins = ColorPaletteExtractor.ExtractPalette(bmp, item.PaletteColorCount, item.PaletteMood, 0);
                }

                // Spawn independent transparent palette card directly beside reference image
                Point sidePos = new Point(item.X + item.Width + 24, item.Y);
                paletteCard = AddPaletteCard(
                    sourceCard: item,
                    initialPins: item.ActivePalettePins,
                    worldPosition: sidePos,
                    mood: item.PaletteMood,
                    colorCount: item.PaletteColorCount,
                    autoSelect: false);
                item.LinkedPaletteCard = paletteCard;
            }
            else
            {
                item.LinkedPaletteCard = paletteCard;
                paletteCard.LinkedSourceImageCard = item;

                // If image card has no pins (e.g. after reload), take from palette card
                if (item.ActivePalettePins == null || item.ActivePalettePins.Count == 0)
                {
                    if (paletteCard.ActivePalettePins != null && paletteCard.ActivePalettePins.Count > 0)
                    {
                        item.ActivePalettePins = new List<PalettePin>(paletteCard.ActivePalettePins);
                    }
                    else
                    {
                        item.ActivePalettePins = ColorPaletteExtractor.ExtractPalette(bmp, paletteCard.PaletteColorCount, paletteCard.PaletteMood, 0);
                    }
                }

                // Ensure pins have valid normalized positions (RelX, RelY) on image
                for (int i = 0; i < item.ActivePalettePins.Count; i++)
                {
                    var p = item.ActivePalettePins[i];
                    if (p.RelX <= 0 && p.RelY <= 0)
                    {
                        p.RelX = (i + 1.0) / (item.ActivePalettePins.Count + 1.0);
                        p.RelY = 0.5;
                    }
                }

                paletteCard.ActivePalettePins = item.ActivePalettePins;
                UpdatePaletteCardContent(paletteCard);
            }

            // Set up PalettePinsCanvas overlay directly over image card
            if (item.PalettePinsCanvas == null)
            {
                item.PalettePinsCanvas = new Canvas
                {
                    Width = item.Width,
                    Height = item.Height,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    ClipToBounds = false,
                    Background = Brushes.Transparent // enables click detection on card
                };
                Panel.SetZIndex(item.PalettePinsCanvas, 850);

                // Clicking anywhere on image canvas moves nearest pin to clicked spot
                item.PalettePinsCanvas.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.OriginalSource == item.PalettePinsCanvas && item.ActivePalettePins.Count > 0)
                    {
                        Point pt = e.GetPosition(item.PalettePinsCanvas);
                        double relX = Math.Clamp(pt.X / Math.Max(1.0, item.Width), 0.02, 0.98);
                        double relY = Math.Clamp(pt.Y / Math.Max(1.0, item.Height), 0.02, 0.98);

                        PalettePin? nearest = null;
                        double minDist = double.MaxValue;
                        foreach (var p in item.ActivePalettePins)
                        {
                            double dx = p.RelX - relX;
                            double dy = p.RelY - relY;
                            double dist = dx * dx + dy * dy;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearest = p;
                            }
                        }

                        if (nearest != null)
                        {
                            nearest.RelX = relX;
                            nearest.RelY = relY;
                            BitmapSource? b = item.Bitmap ?? item.OriginalBitmap;
                            if (b != null)
                            {
                                nearest.Color = ColorPaletteExtractor.SampleColorAt(b, relX, relY);
                            }
                            RenderCanvasPalettePins(item);
                            if (item.LinkedPaletteCard != null)
                            {
                                item.LinkedPaletteCard.ActivePalettePins = item.ActivePalettePins;
                                UpdatePaletteCardContent(item.LinkedPaletteCard);
                            }
                        }
                        e.Handled = true;
                    }
                };

                item.Container.Children.Add(item.PalettePinsCanvas);
            }

            RenderCanvasPalettePins(item);
            RebuildCardHoverToolbar(item);

            ShowToast("Live Palette Card placed at side. Drag pins or move palette solo!", ToastType.Info);
        }

        private void CloseCanvasPaletteMode(CardItem item)
        {
            if (!item.IsPaletteMode) return;
            item.IsPaletteMode = false;

            if (item.PalettePinsCanvas != null)
            {
                item.Container.Children.Remove(item.PalettePinsCanvas);
                item.PalettePinsCanvas = null;
            }

            RebuildCardHoverToolbar(item);
            ShowToast("Closed sampling pins on reference", ToastType.Info);
        }

        private void RefreshPaletteCardFromSource(CardItem paletteCard, bool isRandom)
        {
            CardItem? source = paletteCard.LinkedSourceImageCard;
            if (source == null)
            {
                // Standalone palette adjustment without active source image (prevents freezing on - / + buttons)
                if (paletteCard.ActivePalettePins != null && paletteCard.ActivePalettePins.Count > 0)
                {
                    int targetCount = paletteCard.PaletteColorCount;
                    if (targetCount < paletteCard.ActivePalettePins.Count)
                    {
                        paletteCard.ActivePalettePins = paletteCard.ActivePalettePins.Take(targetCount).ToList();
                    }
                    else if (targetCount > paletteCard.ActivePalettePins.Count)
                    {
                        while (paletteCard.ActivePalettePins.Count < targetCount)
                        {
                            var last = paletteCard.ActivePalettePins.Last();
                            byte r = (byte)Math.Clamp((int)(last.Color.R * 0.85 + 25), 0, 255);
                            byte g = (byte)Math.Clamp((int)(last.Color.G * 0.85 + 25), 0, 255);
                            byte b = (byte)Math.Clamp((int)(last.Color.B * 0.85 + 25), 0, 255);
                            paletteCard.ActivePalettePins.Add(new PalettePin { Color = Color.FromRgb(r, g, b) });
                        }
                    }
                    UpdatePaletteCardContent(paletteCard);
                    ScheduleAutoSave();
                }
                return;
            }

            BitmapSource? bmp = source.Bitmap ?? source.OriginalBitmap;
            if (bmp == null) return;

            List<PalettePin> pins;
            if (isRandom)
            {
                int seed = new Random().Next(1, 999999);
                pins = ColorPaletteExtractor.ExtractPalette(bmp, paletteCard.PaletteColorCount, paletteCard.PaletteMood, seed);
                paletteCard.PaletteMoodCache[paletteCard.PaletteMood] = pins.Select(p => new PalettePin
                {
                    RelX = p.RelX,
                    RelY = p.RelY,
                    Color = p.Color,
                    IsLocked = p.IsLocked
                }).ToList();
            }
            else
            {
                if (paletteCard.PaletteMoodCache.TryGetValue(paletteCard.PaletteMood, out var cached) && cached.Count == paletteCard.PaletteColorCount)
                {
                    pins = cached.Select(p => new PalettePin
                    {
                        RelX = p.RelX,
                        RelY = p.RelY,
                        Color = p.Color,
                        IsLocked = p.IsLocked
                    }).ToList();
                }
                else
                {
                    pins = ColorPaletteExtractor.ExtractPalette(bmp, paletteCard.PaletteColorCount, paletteCard.PaletteMood, 0);
                    paletteCard.PaletteMoodCache[paletteCard.PaletteMood] = pins.Select(p => new PalettePin
                    {
                        RelX = p.RelX,
                        RelY = p.RelY,
                        Color = p.Color,
                        IsLocked = p.IsLocked
                    }).ToList();
                }
            }

            paletteCard.ActivePalettePins = pins;
            source.ActivePalettePins = pins;
            source.PaletteColorCount = paletteCard.PaletteColorCount;
            source.PaletteMood = paletteCard.PaletteMood;

            UpdatePaletteCardContent(paletteCard);

            if (source.IsPaletteMode && source.PalettePinsCanvas != null)
            {
                RenderCanvasPalettePins(source);
            }
            ScheduleAutoSave();
        }

        private void RefreshCanvasPalette(CardItem item, bool isRandom)
        {
            BitmapSource? bmp = item.Bitmap ?? item.OriginalBitmap;
            if (bmp == null) return;

            int seed = isRandom ? new Random().Next(1, 999999) : 0;
            item.ActivePalettePins = ColorPaletteExtractor.ExtractPalette(bmp, item.PaletteColorCount, item.PaletteMood, seed);
            RenderCanvasPalettePins(item);

            if (item.LinkedPaletteCard != null)
            {
                item.LinkedPaletteCard.ActivePalettePins = item.ActivePalettePins;
                UpdatePaletteCardContent(item.LinkedPaletteCard);
            }
        }

        private void RenderCanvasPalettePins(CardItem item)
        {
            if (item.PalettePinsCanvas == null) return;
            item.PalettePinsCanvas.Children.Clear();

            double cardW = item.Width;
            double cardH = item.Height;
            item.PalettePinsCanvas.Width = cardW;
            item.PalettePinsCanvas.Height = cardH;

            for (int i = 0; i < item.ActivePalettePins.Count; i++)
            {
                PalettePin pin = item.ActivePalettePins[i];

                Border pip = new Border
                {
                    Width = 6,
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };

                Border pinElement = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(13),
                    BorderThickness = new Thickness(2.5),
                    BorderBrush = Brushes.White,
                    Background = new SolidColorBrush(pin.Color),
                    Cursor = Cursors.Hand,
                    ToolTip = $"Sample Pin #{i + 1}: {pin.Hex}\nDrag to adjust sample position",
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 10,
                        ShadowDepth = 2,
                        Opacity = 0.75,
                        Color = Colors.Black
                    },
                    Child = pip
                };

                Canvas.SetLeft(pinElement, (pin.RelX * cardW) - 13);
                Canvas.SetTop(pinElement, (pin.RelY * cardH) - 13);

                bool isDraggingPin = false;
                Point dragStartOffset = new Point();

                pinElement.MouseLeftButtonDown += (s, e) =>
                {
                    isDraggingPin = true;
                    dragStartOffset = e.GetPosition(pinElement);
                    pinElement.CaptureMouse();
                    pinElement.Cursor = Cursors.SizeAll;
                    e.Handled = true;
                };

                pinElement.MouseMove += (s, e) =>
                {
                    if (isDraggingPin && pinElement.IsMouseCaptured)
                    {
                        Point mousePos = e.GetPosition(item.PalettePinsCanvas);
                        double pinCenterX = mousePos.X - dragStartOffset.X + 13;
                        double pinCenterY = mousePos.Y - dragStartOffset.Y + 13;

                        double relX = Math.Clamp(pinCenterX / Math.Max(1.0, cardW), 0.01, 0.99);
                        double relY = Math.Clamp(pinCenterY / Math.Max(1.0, cardH), 0.01, 0.99);

                        pin.RelX = relX;
                        pin.RelY = relY;

                        Canvas.SetLeft(pinElement, (relX * cardW) - 13);
                        Canvas.SetTop(pinElement, (relY * cardH) - 13);

                        BitmapSource? bmp = item.Bitmap ?? item.OriginalBitmap;
                        if (bmp != null)
                        {
                            Color sampled = ColorPaletteExtractor.SampleColorAt(bmp, relX, relY);
                            pin.Color = sampled;
                            pinElement.Background = new SolidColorBrush(sampled);
                            pinElement.ToolTip = $"Sample Pin: {pin.Hex}\nDrag to adjust sample position";

                            // Live update the linked standalone Palette Card!
                            if (item.LinkedPaletteCard != null)
                            {
                                item.LinkedPaletteCard.ActivePalettePins = item.ActivePalettePins;
                                UpdatePaletteCardContent(item.LinkedPaletteCard);
                            }
                        }

                        e.Handled = true;
                    }
                };

                pinElement.MouseLeftButtonUp += (s, e) =>
                {
                    if (isDraggingPin)
                    {
                        isDraggingPin = false;
                        pinElement.ReleaseMouseCapture();
                        pinElement.Cursor = Cursors.Hand;
                        e.Handled = true;
                    }
                };

                item.PalettePinsCanvas.Children.Add(pinElement);
            }
        }

        private void ShowPaletteCardMoodMenu(FrameworkElement anchor, CardItem item)
        {
            _isCardSubMenuOpen = true;
            ContextMenu cm = new ContextMenu
            {
                PlacementTarget = anchor,
                Placement = PlacementMode.Bottom,
                VerticalOffset = 3
            };

            cm.Closed += (s, e) =>
            {
                _isCardSubMenuOpen = false;
            };

            var moods = new[]
            {
                (ColorMood.Colorful, "Colorful", "Vibrant, high-saturation color harmony"),
                (ColorMood.Bright, "Bright", "High luminance and light tints"),
                (ColorMood.Muted, "Muted", "Soft, low-saturation pastel tones"),
                (ColorMood.Deep, "Deep", "Rich, saturated medium tones"),
                (ColorMood.Dark, "Dark", "Moody, deep shadows and dark tones"),
                (ColorMood.Dominant, "Dominant", "Most frequent reference image colors")
            };

            foreach (var (m, title, desc) in moods)
            {
                ColorMood targetMood = m;
                MenuItem mi = new MenuItem
                {
                    Header = title,
                    FontWeight = item.PaletteMood == targetMood ? FontWeights.Bold : FontWeights.Normal
                };
                mi.Click += (s, e) =>
                {
                    if (item.ActivePalettePins != null && item.ActivePalettePins.Count > 0)
                    {
                        item.PaletteMoodCache[item.PaletteMood] = item.ActivePalettePins.Select(p => new PalettePin
                        {
                            RelX = p.RelX,
                            RelY = p.RelY,
                            Color = p.Color,
                            IsLocked = p.IsLocked
                        }).ToList();
                    }

                    item.PaletteMood = targetMood;
                    RefreshPaletteCardFromSource(item, isRandom: false);
                    RebuildCardHoverToolbar(item);
                    ShowToast($"Mood set to {title}", ToastType.Info);
                };
                cm.Items.Add(mi);
            }

            cm.IsOpen = true;
        }

        private void UpdateCanvasPaletteLayout(CardItem item)
        {
            if (item.PalettePinsCanvas != null)
            {
                RenderCanvasPalettePins(item);
            }
        }

        private void ExportCanvasPaletteToAe(CardItem item)
        {
            if (item.ActivePalettePins == null || item.ActivePalettePins.Count == 0) return;
            try
            {
                string title = item.LinkedSourceImageCard != null && !string.IsNullOrEmpty(item.LinkedSourceImageCard.LocalPath)
                    ? System.IO.Path.GetFileNameWithoutExtension(item.LinkedSourceImageCard.LocalPath)
                    : "Theme";
                string savedFile = PaletteCardRenderer.SavePaletteImageToFile(
                    item.ActivePalettePins,
                    PalettesDir,
                    "Palette_" + title,
                    rows: item.PaletteRows,
                    targetWidth: item.Width,
                    targetHeight: item.Height);

                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(savedFile, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                double cardW = item.Width > 0 ? item.Width : 480.0;
                double cardH = item.Height > 0 ? item.Height : 240.0;

                CardItem tempCard = new CardItem
                {
                    Bitmap = bi,
                    LocalPath = savedFile,
                    Width = cardW,
                    Height = cardH,
                    AspectRatio = cardW / Math.Max(1.0, cardH),
                    X = item.X,
                    Y = item.Y
                };
                ExportCardsToAe(new[] { tempCard });
            }
            catch (Exception ex)
            {
                ShowToast("AE Export failed: " + ex.Message, ToastType.Error);
            }
        }

        private void SavePaletteCardImage(CardItem item)
        {
            if (item.ActivePalettePins == null || item.ActivePalettePins.Count == 0) return;
            try
            {
                string title = item.LinkedSourceImageCard != null && !string.IsNullOrEmpty(item.LinkedSourceImageCard.LocalPath)
                    ? System.IO.Path.GetFileNameWithoutExtension(item.LinkedSourceImageCard.LocalPath)
                    : "Theme";
                string savedFile = PaletteCardRenderer.SavePaletteImageToFile(
                    item.ActivePalettePins,
                    PalettesDir,
                    "Palette_" + title,
                    rows: item.PaletteRows,
                    targetWidth: item.Width,
                    targetHeight: item.Height);

                ShowToast("Saved palette image to Palettes folder!", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Save failed: " + ex.Message, ToastType.Error);
            }
        }

        private void RebuildCardHoverToolbar(CardItem item)
        {
            if (item.HoverToolbar == null) return;
            if (item.HoverToolbar.Parent is Canvas host)
            {
                host.Children.Remove(item.HoverToolbar);
                Border newToolbar = CreateCardHoverToolbar(item);
                item.HoverToolbar = newToolbar;
                Canvas.SetTop(newToolbar, -38.0);
                host.Children.Add(newToolbar);

                newToolbar.SizeChanged += (s, e) =>
                {
                    if (e.NewSize.Width > 0)
                    {
                        Canvas.SetLeft(newToolbar, -e.NewSize.Width / 2.0);
                        Canvas.SetTop(newToolbar, -38.0);
                    }
                };

                newToolbar.Opacity = 1.0;
                newToolbar.IsHitTestVisible = true;
            }
        }

        private void OpenColorPaletteStudio(CardItem item)
        {
            StartCanvasPaletteMode(item);
        }

        #endregion

        private void DuplicateNoteCard(CardItem item)
        {
            RecordUndo("Duplicate Note");
            Point newPos = new Point(item.X + 24, item.Y + 24);

            List<NoteChecklistItem>? dupChecklist = null;
            if (item.IsChecklist && item.ChecklistItems != null)
            {
                dupChecklist = item.ChecklistItems.Select(ci => new NoteChecklistItem { Id = Guid.NewGuid().ToString("N"), Text = ci.Text, IsChecked = ci.IsChecked }).ToList();
            }

            AddNoteCard(
                text: item.NoteText,
                worldPosition: newPos,
                customWidth: item.Width,
                customHeight: item.Height,
                fontFamily: item.NoteFontFamily,
                fontSize: item.NoteFontSize,
                textColor: item.NoteTextColor,
                bgColor: item.NoteBgColor,
                alignment: item.NoteAlignment,
                hasShadow: item.NoteHasShadow,
                isChecklist: item.IsChecklist,
                checklistItems: dupChecklist,
                hasDeadline: item.HasDeadline,
                deadlineDateTime: item.DeadlineDateTime,
                deadlineLabel: item.DeadlineLabel,
                noteDoodleInkBase64: item.NoteDoodleInkBase64,
                noteBgGifPath: item.NoteBgGifPath,
                noteBgGifBase64: item.NoteBgGifBase64,
                autoSelect: true);
            ScheduleAutoSave();
            ShowToast("Duplicated note", ToastType.Success);
        }

        private void DuplicateDrawCard(CardItem item)
        {
            RecordUndo("Duplicate Sketch");
            Point newPos = new Point(item.X + 24, item.Y + 24);
            AddDrawCard(
                worldPosition: newPos,
                customWidth: item.Width,
                customHeight: item.Height,
                initialInkBase64: item.DrawInkBase64,
                penColor: item.DrawPenColor,
                penSize: item.DrawPenSize,
                autoSelect: true);
            ScheduleAutoSave();
            ShowToast("Duplicated sketch", ToastType.Success);
        }

        private void CleanupCard(CardItem card)
        {
            if (card.IsPaletteMode)
            {
                CloseCanvasPaletteMode(card);
            }
            if (card.IsPaletteCard)
            {
                if (card.LinkedSourceImageCard != null)
                {
                    CloseCanvasPaletteMode(card.LinkedSourceImageCard);
                    card.LinkedSourceImageCard.LinkedPaletteCard = null;
                }
            }
            if (card.LinkedPaletteCard != null)
            {
                card.LinkedPaletteCard.LinkedSourceImageCard = null;
            }
            if (card.PlayerControl != null)
            {
                try { card.PlayerControl.Dispose(); } catch { }
                card.PlayerControl = null;
            }
            if (card.IsLocalVideo)
            {
                CleanupLocalVideoCard(card);
            }

            if (card.ImageControl != null)
            {
                card.ImageControl.Source = null;
            }
            card.Bitmap = null!;
            card.OriginalBitmap = null!;
            card.Base64Data = "";
            card.NoteDoodleCanvas = null;
            card.NoteDoodleIndicator = null;
            card.NoteEditor = null;
            ScheduleWorkingSetTrim(2000);
        }

        private void RemoveCard(CardItem card)
        {
            RecordUndo("Delete Card");
            CleanupCard(card);
            WorldCanvas.Children.Remove(card.Container);
            _cards.Remove(card);
            _selectedCards.Remove(card);
            UpdateStatusCounts();
            if (_cards.Count == 0)
            {
                EmptyStateOverlay.Visibility = Visibility.Visible;
            }
            ScheduleAutoSave();
            ShowToast(card.IsPaletteCard ? "Deleted palette card" : "Deleted reference", ToastType.Info);
        }

        private void CopyCardToClipboard(CardItem item)
        {
            try
            {
                if (item.IsLocalVideo)
                {
                    if (!string.IsNullOrEmpty(item.VideoFilePath) && File.Exists(item.VideoFilePath))
                    {
                        var sc = new System.Collections.Specialized.StringCollection { item.VideoFilePath };
                        Clipboard.SetFileDropList(sc);
                        ShowToast("Copied video file to clipboard", ToastType.Success);
                        return;
                    }
                }
                if (item.IsDrawCard)
                {
                    CopyDrawCardToClipboard(item);
                    return;
                }
                if (item.IsNote)
                {
                    Clipboard.SetText(item.NoteText);
                    ShowToast("Copied note text to clipboard", ToastType.Success);
                    return;
                }
                if (item.IsPaletteCard && item.ActivePalettePins != null && item.ActivePalettePins.Count > 0)
                {
                    string title = item.LinkedSourceImageCard != null && !string.IsNullOrEmpty(item.LinkedSourceImageCard.LocalPath)
                        ? System.IO.Path.GetFileNameWithoutExtension(item.LinkedSourceImageCard.LocalPath)
                        : "Color Palette";
                    PaletteCardRenderer.CopyPaletteImageToClipboard(
                        item.ActivePalettePins,
                        title,
                        rows: item.PaletteRows,
                        targetWidth: item.Width,
                        targetHeight: item.Height);
                    ShowToast("Copied palette graphic to clipboard!", ToastType.Success);
                    return;
                }
                if (!string.IsNullOrEmpty(item.LocalPath) && File.Exists(item.LocalPath))
                {
                    var fileList = new System.Collections.Specialized.StringCollection { item.LocalPath };
                    Clipboard.SetFileDropList(fileList);
                }
                else if (item.Bitmap != null)
                {
                    Clipboard.SetImage(item.Bitmap);
                }
                ShowToast("Copied reference image to clipboard", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast("Copy failed: " + ex.Message, ToastType.Error);
            }
        }

        private void StartCropCard(CardItem item)
        {
            if (_isCropping) return;
            _isCropping = true;
            _activeCroppingCard = item;

            // Hide card hover quick-action toolbar
            if (item.HoverToolbar != null)
            {
                item.HoverToolbar.Opacity = 0.0;
                item.HoverToolbar.IsHitTestVisible = false;
            }

            // Hide normal corner resize handles while cropping
            if (item.HandleTL != null) item.HandleTL.Visibility = Visibility.Collapsed;
            if (item.HandleTR != null) item.HandleTR.Visibility = Visibility.Collapsed;
            if (item.HandleBL != null) item.HandleBL.Visibility = Visibility.Collapsed;
            if (item.HandleBR != null) item.HandleBR.Visibility = Visibility.Collapsed;

            // Ensure OriginalBitmap and base dimensions exist
            if (item.OriginalBitmap == null)
            {
                item.OriginalBitmap = item.Bitmap;
            }
            if (item.BaseWidth <= 0 || item.BaseHeight <= 0)
            {
                item.BaseWidth = item.Width;
                item.BaseHeight = item.Height;
                item.BaseX = item.X;
                item.BaseY = item.Y;
            }

            // Store previous state for Cancel option
            BitmapSource prevBitmap = item.Bitmap;
            double prevW = item.Width;
            double prevH = item.Height;
            double prevX = item.X;
            double prevY = item.Y;
            double prevCropL = item.CropLeft;
            double prevCropT = item.CropTop;
            double prevCropR = item.CropRight;
            double prevCropB = item.CropBottom;

            // Expand card back to full original uncropped image so user can see full reference and edit crop clearly
            item.ImageControl.Source = item.OriginalBitmap;
            item.Width = item.BaseWidth;
            item.Height = item.BaseHeight;
            item.X = item.BaseX;
            item.Y = item.BaseY;

            // Initialize crop percentages (use existing crop if card is already cropped, else default 10%)
            double cropL = item.IsCropped ? item.CropLeft : 10.0;
            double cropT = item.IsCropped ? item.CropTop : 10.0;
            double cropR = item.IsCropped ? item.CropRight : 10.0;
            double cropB = item.IsCropped ? item.CropBottom : 10.0;

            Canvas cropCanvas = new Canvas
            {
                Width = item.BaseWidth,
                Height = item.BaseHeight,
                ClipToBounds = false
            };

            Grid cropOverlay = new Grid
            {
                Width = item.BaseWidth,
                Height = item.BaseHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            cropOverlay.Children.Add(cropCanvas);
            _activeCropOverlay = cropOverlay;

            // 4 Dark Dim Masks around the active box (0.72 dark opacity matching Web JS)
            Brush maskBrush = new SolidColorBrush(Color.FromArgb(185, 8, 10, 15));
            Border maskTop = new Border { Background = maskBrush, IsHitTestVisible = false };
            Border maskBottom = new Border { Background = maskBrush, IsHitTestVisible = false };
            Border maskLeft = new Border { Background = maskBrush, IsHitTestVisible = false };
            Border maskRight = new Border { Background = maskBrush, IsHitTestVisible = false };

            // Active Yellow Dashed Box (Center Draggable to Pan Crop Area)
            System.Windows.Shapes.Rectangle activeBox = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(28, 251, 191, 36)),
                Cursor = Cursors.SizeAll
            };

            // 4 Draggable Edge Bars (Top, Bottom, Left, Right)
            Border edgeTop = new Border
            {
                Height = 8,
                Background = new SolidColorBrush(Color.FromArgb(1, 251, 191, 36)),
                Cursor = Cursors.SizeNS
            };
            Border edgeBottom = new Border
            {
                Height = 8,
                Background = new SolidColorBrush(Color.FromArgb(1, 251, 191, 36)),
                Cursor = Cursors.SizeNS
            };
            Border edgeLeft = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromArgb(1, 251, 191, 36)),
                Cursor = Cursors.SizeWE
            };
            Border edgeRight = new Border
            {
                Width = 8,
                Background = new SolidColorBrush(Color.FromArgb(1, 251, 191, 36)),
                Cursor = Cursors.SizeWE
            };

            // Subtle hover highlight on edge bars
            void AddEdgeHover(Border edge)
            {
                edge.MouseEnter += (s, e) => edge.Background = new SolidColorBrush(Color.FromArgb(120, 251, 191, 36));
                edge.MouseLeave += (s, e) => edge.Background = new SolidColorBrush(Color.FromArgb(1, 251, 191, 36));
            }
            AddEdgeHover(edgeTop);
            AddEdgeHover(edgeBottom);
            AddEdgeHover(edgeLeft);
            AddEdgeHover(edgeRight);

            // 4 Corner Handles (NW, NE, SW, SE)
            Border CreateCornerHandle(Cursor cursor)
            {
                Border h = new Border
                {
                    Width = 10,
                    Height = 10,
                    Background = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(15, 17, 23)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(2),
                    Cursor = cursor
                };
                h.MouseEnter += (s, e) => h.Background = Brushes.White;
                h.MouseLeave += (s, e) => h.Background = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                return h;
            }

            Border cornerNW = CreateCornerHandle(Cursors.SizeNWSE);
            Border cornerNE = CreateCornerHandle(Cursors.SizeNESW);
            Border cornerSW = CreateCornerHandle(Cursors.SizeNESW);
            Border cornerSE = CreateCornerHandle(Cursors.SizeNWSE);

            // Floating Controls Pill: Done, Reset, Cancel (Authentic 1:1 Web JS .crop-controls-pill)
            Border pill = new Border
            {
                Height = 36,
                Background = new SolidColorBrush(Color.FromArgb(246, 20, 24, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(130, 251, 191, 36)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18), // Pure capsule geometry (Height/2)
                Padding = new Thickness(6, 3, 6, 3),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 5,
                    Direction = 270,
                    Opacity = 0.7,
                    Color = Colors.Black
                }
            };
            StackPanel pillSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // 1. Done Button (Solid Amber Capsule with Dark Bold Text like .crop-btn-done)
            Border btnDone = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(251, 191, 36)),
                CornerRadius = new CornerRadius(13),
                Padding = new Thickness(12, 5, 12, 5),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0),
                Child = new TextBlock
                {
                    Text = "✓ Done (Enter)",
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 17, 23)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btnDone.MouseEnter += (s, e) => btnDone.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            btnDone.MouseLeave += (s, e) => btnDone.Background = new SolidColorBrush(Color.FromRgb(251, 191, 36));

            // 2. Reset Button (Translucent Pill with White Text like .crop-btn-reset)
            Border btnReset = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(13),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0),
                Child = new TextBlock
                {
                    Text = "↺ Reset (Esc)",
                    Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btnReset.MouseEnter += (s, e) => btnReset.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            btnReset.MouseLeave += (s, e) => btnReset.Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));

            // 3. Cancel Button (Ghost Pill)
            Border btnCancel = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(13),
                Padding = new Thickness(8, 5, 8, 5),
                Cursor = Cursors.Hand,
                Child = new TextBlock
                {
                    Text = "✕ Cancel",
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontWeight = FontWeights.Normal,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btnCancel.MouseEnter += (s, e) =>
            {
                btnCancel.Background = new SolidColorBrush(Color.FromArgb(30, 239, 68, 68));
                if (btnCancel.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            };
            btnCancel.MouseLeave += (s, e) =>
            {
                btnCancel.Background = Brushes.Transparent;
                if (btnCancel.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            };

            pillSp.Children.Add(btnDone);
            pillSp.Children.Add(btnReset);
            pillSp.Children.Add(btnCancel);
            pill.Child = pillSp;

            // Z-Order: Masks -> Active Box -> Edge Bars -> Corner Handles -> Pill
            cropCanvas.Children.Add(maskTop);
            cropCanvas.Children.Add(maskBottom);
            cropCanvas.Children.Add(maskLeft);
            cropCanvas.Children.Add(maskRight);
            cropCanvas.Children.Add(activeBox);
            cropCanvas.Children.Add(edgeTop);
            cropCanvas.Children.Add(edgeBottom);
            cropCanvas.Children.Add(edgeLeft);
            cropCanvas.Children.Add(edgeRight);
            cropCanvas.Children.Add(cornerNW);
            cropCanvas.Children.Add(cornerNE);
            cropCanvas.Children.Add(cornerSW);
            cropCanvas.Children.Add(cornerSE);
            cropCanvas.Children.Add(pill);

            void UpdateCropLayout()
            {
                double cardW = item.BaseWidth;
                double cardH = item.BaseHeight;

                double lPx = (cropL / 100.0) * cardW;
                double tPx = (cropT / 100.0) * cardH;
                double rPx = (cropR / 100.0) * cardW;
                double bPx = (cropB / 100.0) * cardH;

                double boxW = Math.Max(10, cardW - lPx - rPx);
                double boxH = Math.Max(10, cardH - tPx - bPx);

                // Update 4 dim masks
                Canvas.SetLeft(maskTop, 0);
                Canvas.SetTop(maskTop, 0);
                maskTop.Width = cardW;
                maskTop.Height = Math.Max(0, tPx);

                Canvas.SetLeft(maskBottom, 0);
                Canvas.SetTop(maskBottom, cardH - bPx);
                maskBottom.Width = cardW;
                maskBottom.Height = Math.Max(0, bPx);

                Canvas.SetLeft(maskLeft, 0);
                Canvas.SetTop(maskLeft, tPx);
                maskLeft.Width = Math.Max(0, lPx);
                maskLeft.Height = Math.Max(0, boxH);

                Canvas.SetLeft(maskRight, cardW - rPx);
                Canvas.SetTop(maskRight, tPx);
                maskRight.Width = Math.Max(0, rPx);
                maskRight.Height = Math.Max(0, boxH);

                // Update active dashed box
                Canvas.SetLeft(activeBox, lPx);
                Canvas.SetTop(activeBox, tPx);
                activeBox.Width = boxW;
                activeBox.Height = boxH;

                // Update 4 edge bars (centered along the 4 borders of activeBox)
                Canvas.SetLeft(edgeTop, lPx);
                Canvas.SetTop(edgeTop, tPx - 4);
                edgeTop.Width = boxW;

                Canvas.SetLeft(edgeBottom, lPx);
                Canvas.SetTop(edgeBottom, tPx + boxH - 4);
                edgeBottom.Width = boxW;

                Canvas.SetLeft(edgeLeft, lPx - 4);
                Canvas.SetTop(edgeLeft, tPx);
                edgeLeft.Height = boxH;

                Canvas.SetLeft(edgeRight, lPx + boxW - 4);
                Canvas.SetTop(edgeRight, tPx);
                edgeRight.Height = boxH;

                // Update 4 corner handles (10x10 centered at corners)
                Canvas.SetLeft(cornerNW, lPx - 5);
                Canvas.SetTop(cornerNW, tPx - 5);

                Canvas.SetLeft(cornerNE, lPx + boxW - 5);
                Canvas.SetTop(cornerNE, tPx - 5);

                Canvas.SetLeft(cornerSW, lPx - 5);
                Canvas.SetTop(cornerSW, tPx + boxH - 5);

                Canvas.SetLeft(cornerSE, lPx + boxW - 5);
                Canvas.SetTop(cornerSE, tPx + boxH - 5);

                // Update controls pill position
                double pillW = 290;
                double pillH = 36;
                double pillLeft = lPx + (boxW - pillW) / 2.0;
                pillLeft = Math.Clamp(pillLeft, 6, Math.Max(6, cardW - pillW - 6));
                double pillTop = tPx + boxH + 10;
                if (pillTop + pillH > cardH)
                {
                    pillTop = Math.Max(6, tPx + boxH - pillH - 8);
                }
                Canvas.SetLeft(pill, pillLeft);
                Canvas.SetTop(pill, pillTop);
            }

            UpdateCropLayout();

            // Universal drag listener for all 8 handles + active box
            void AttachHandle(UIElement handle, Action<double, double, double, double, double, double> onDrag)
            {
                bool isDragging = false;
                Point startMouse = new Point();
                double initL = 0, initT = 0, initR = 0, initB = 0;

                handle.MouseLeftButtonDown += (s, e) =>
                {
                    isDragging = true;
                    startMouse = e.GetPosition(cropCanvas);
                    initL = cropL; initT = cropT; initR = cropR; initB = cropB;
                    handle.CaptureMouse();
                    e.Handled = true;
                };

                handle.MouseMove += (s, e) =>
                {
                    if (isDragging)
                    {
                        Point cur = e.GetPosition(cropCanvas);
                        double dx = cur.X - startMouse.X;
                        double dy = cur.Y - startMouse.Y;
                        double dxPct = (dx / item.BaseWidth) * 100.0;
                        double dyPct = (dy / item.BaseHeight) * 100.0;

                        onDrag(dxPct, dyPct, initL, initT, initR, initB);
                        UpdateCropLayout();
                        e.Handled = true;
                    }
                };

                handle.MouseLeftButtonUp += (s, e) =>
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        handle.ReleaseMouseCapture();
                        e.Handled = true;
                    }
                };
            }

            // 1. Top Edge
            AttachHandle(edgeTop, (dx, dy, initL, initT, initR, initB) =>
            {
                double maxAllowed = 85.0 - initB;
                cropT = Math.Round(Math.Clamp(initT + dy, 0, maxAllowed), 1);
            });

            // 2. Bottom Edge
            AttachHandle(edgeBottom, (dx, dy, initL, initT, initR, initB) =>
            {
                double maxAllowed = 85.0 - initT;
                cropB = Math.Round(Math.Clamp(initB - dy, 0, maxAllowed), 1);
            });

            // 3. Left Edge
            AttachHandle(edgeLeft, (dx, dy, initL, initT, initR, initB) =>
            {
                double maxAllowed = 85.0 - initR;
                cropL = Math.Round(Math.Clamp(initL + dx, 0, maxAllowed), 1);
            });

            // 4. Right Edge
            AttachHandle(edgeRight, (dx, dy, initL, initT, initR, initB) =>
            {
                double maxAllowed = 85.0 - initL;
                cropR = Math.Round(Math.Clamp(initR - dx, 0, maxAllowed), 1);
            });

            // 5. Corner NW
            AttachHandle(cornerNW, (dx, dy, initL, initT, initR, initB) =>
            {
                cropT = Math.Round(Math.Clamp(initT + dy, 0, 85.0 - initB), 1);
                cropL = Math.Round(Math.Clamp(initL + dx, 0, 85.0 - initR), 1);
            });

            // 6. Corner NE
            AttachHandle(cornerNE, (dx, dy, initL, initT, initR, initB) =>
            {
                cropT = Math.Round(Math.Clamp(initT + dy, 0, 85.0 - initB), 1);
                cropR = Math.Round(Math.Clamp(initR - dx, 0, 85.0 - initL), 1);
            });

            // 7. Corner SW
            AttachHandle(cornerSW, (dx, dy, initL, initT, initR, initB) =>
            {
                cropB = Math.Round(Math.Clamp(initB - dy, 0, 85.0 - initT), 1);
                cropL = Math.Round(Math.Clamp(initL + dx, 0, 85.0 - initR), 1);
            });

            // 8. Corner SE
            AttachHandle(cornerSE, (dx, dy, initL, initT, initR, initB) =>
            {
                cropB = Math.Round(Math.Clamp(initB - dy, 0, 85.0 - initT), 1);
                cropR = Math.Round(Math.Clamp(initR - dx, 0, 85.0 - initL), 1);
            });

            // 9. Active Box (Center Pan)
            AttachHandle(activeBox, (dx, dy, initL, initT, initR, initB) =>
            {
                double boxWPct = 100.0 - initL - initR;
                double boxHPct = 100.0 - initT - initB;

                double newL = initL + dx;
                if (newL < 0) newL = 0;
                if (newL + boxWPct > 100.0) newL = 100.0 - boxWPct;
                double newR = 100.0 - (newL + boxWPct);

                double newT = initT + dy;
                if (newT < 0) newT = 0;
                if (newT + boxHPct > 100.0) newT = 100.0 - boxHPct;
                double newB = 100.0 - (newT + boxHPct);

                cropL = Math.Round(newL, 1);
                cropR = Math.Round(newR, 1);
                cropT = Math.Round(newT, 1);
                cropB = Math.Round(newB, 1);
            });

            KeyEventHandler? onCropKey = null;

            void CloseOverlay()
            {
                if (onCropKey != null)
                {
                    this.PreviewKeyDown -= onCropKey;
                    onCropKey = null;
                }

                item.Container.Children.Remove(cropOverlay);
                _isCropping = false;
                _activeCroppingCard = null;
                _activeCropOverlay = null;

                // Restore normal corner resize handles
                if (item.HandleTL != null) item.HandleTL.Visibility = Visibility.Visible;
                if (item.HandleTR != null) item.HandleTR.Visibility = Visibility.Visible;
                if (item.HandleBL != null) item.HandleBL.Visibility = Visibility.Visible;
                if (item.HandleBR != null) item.HandleBR.Visibility = Visibility.Visible;
            }

            void FinishCrop()
            {
                RecordUndo("Crop Reference");

                int imgW = item.OriginalBitmap.PixelWidth;
                int imgH = item.OriginalBitmap.PixelHeight;

                int pxX = (int)Math.Round((cropL / 100.0) * imgW);
                int pxY = (int)Math.Round((cropT / 100.0) * imgH);
                int pxW = (int)Math.Round(((100.0 - cropL - cropR) / 100.0) * imgW);
                int pxH = (int)Math.Round(((100.0 - cropT - cropB) / 100.0) * imgH);

                pxX = Math.Clamp(pxX, 0, Math.Max(0, imgW - 1));
                pxY = Math.Clamp(pxY, 0, Math.Max(0, imgH - 1));
                pxW = Math.Clamp(pxW, 1, imgW - pxX);
                pxH = Math.Clamp(pxH, 1, imgH - pxY);

                CroppedBitmap cb = new CroppedBitmap(item.OriginalBitmap, new Int32Rect(pxX, pxY, pxW, pxH));
                item.Bitmap = cb;
                item.ImageControl.Source = cb;
                item.AspectRatio = (double)pxW / Math.Max(1.0, pxH);

                double visW = Math.Max(40, item.BaseWidth * ((100.0 - cropL - cropR) / 100.0));
                double visH = Math.Max(40, item.BaseHeight * ((100.0 - cropT - cropB) / 100.0));

                item.CropLeft = cropL;
                item.CropTop = cropT;
                item.CropRight = cropR;
                item.CropBottom = cropB;

                item.Width = visW;
                item.Height = visH;
                item.X = item.BaseX + item.BaseWidth * (cropL / 100.0);
                item.Y = item.BaseY + item.BaseHeight * (cropT / 100.0);

                CloseOverlay();
                ScheduleAutoSave();
                ShowToast("Crop applied (Fit to crop)", ToastType.Success);
            }

            void ResetCrop()
            {
                RecordUndo("Reset Crop");

                item.CropLeft = 0;
                item.CropTop = 0;
                item.CropRight = 0;
                item.CropBottom = 0;

                item.Bitmap = item.OriginalBitmap;
                item.ImageControl.Source = item.OriginalBitmap;
                ApplyGifAnimationIfNeeded(item);
                item.AspectRatio = (double)item.OriginalBitmap.PixelWidth / Math.Max(1.0, item.OriginalBitmap.PixelHeight);

                item.Width = item.BaseWidth;
                item.Height = item.BaseHeight;
                item.X = item.BaseX;
                item.Y = item.BaseY;

                CloseOverlay();
                ScheduleAutoSave();
                ShowToast("Crop reset to full image", ToastType.Info);
            }

            void CancelCrop()
            {
                item.Bitmap = prevBitmap;
                item.ImageControl.Source = prevBitmap;
                item.CropLeft = prevCropL;
                item.CropTop = prevCropT;
                item.CropRight = prevCropR;
                item.CropBottom = prevCropB;

                item.Width = prevW;
                item.Height = prevH;
                item.X = prevX;
                item.Y = prevY;

                CloseOverlay();
                ShowToast("Crop cancelled", ToastType.Info);
            }

            onCropKey = (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    FinishCrop();
                }
                else if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    if (item.IsCropped)
                    {
                        ResetCrop();
                    }
                    else
                    {
                        CancelCrop();
                    }
                }
            };
            this.PreviewKeyDown += onCropKey;

            btnDone.MouseLeftButtonDown += (s, e) => { e.Handled = true; FinishCrop(); };
            btnReset.MouseLeftButtonDown += (s, e) => { e.Handled = true; ResetCrop(); };
            btnCancel.MouseLeftButtonDown += (s, e) => { e.Handled = true; CancelCrop(); };

            item.Container.Children.Add(cropOverlay);
            ShowToast("Drag yellow borders/corners to crop. Press Enter when done.", ToastType.Info);
        }

        #endregion

        private void ZoomToCard(CardItem item)
        {
            if (item == null) return;

            double viewW = CanvasContainer.ActualWidth > 0 ? CanvasContainer.ActualWidth : ActualWidth;
            double viewH = CanvasContainer.ActualHeight > 0 ? CanvasContainer.ActualHeight : ActualHeight;
            if (viewW <= 0) viewW = 800;
            if (viewH <= 0) viewH = 600;

            // Comfortable margins: 120px horizontal (60px each side), 140px vertical (topbar, dock, and bottom pill clearance)
            const double marginX = 120.0;
            const double marginY = 140.0;

            double availW = Math.Max(50.0, viewW - marginX);
            double availH = Math.Max(50.0, viewH - marginY);

            double cardW = Math.Max(10.0, item.Width);
            double cardH = Math.Max(10.0, item.Height);

            // Mathematical aspect-ratio preserving fit with comfortable breathing room (~90% fill)
            double scaleX = availW / cardW;
            double scaleY = availH / cardH;
            double fitScale = Math.Min(scaleX, scaleY) * 0.90;

            // Smooth scaling: allows scaling in up to 350% or down to 15%
            double targetZoom = Math.Clamp(fitScale, 0.15, 3.5);

            double cardCenterX = item.X + (cardW / 2.0);
            double cardCenterY = item.Y + (cardH / 2.0);

            // Optical center of canvas viewport (accounting for the 40px Top TitleBar)
            double viewCenterX = viewW / 2.0;
            double viewCenterY = (viewH + 40.0) / 2.0;

            double targetOffsetX = viewCenterX - (cardCenterX * targetZoom);
            double targetOffsetY = viewCenterY - (cardCenterY * targetZoom);

            AnimateCanvasView(targetZoom, targetOffsetX, targetOffsetY);
            ShowToast($"Zoomed to reference ({(int)Math.Round(targetZoom * 100)}%)", ToastType.Info);
        }

        private void ZoomToSelectedCards()
        {
            if (_selectedCards.Count == 0) return;
            if (_selectedCards.Count == 1) { ZoomToCard(_selectedCards.First()); return; }

            double minX = _selectedCards.Min(c => c.X);
            double minY = _selectedCards.Min(c => c.Y);
            double maxX = _selectedCards.Max(c => c.X + c.Width);
            double maxY = _selectedCards.Max(c => c.Y + c.Height);

            double viewW = CanvasContainer.ActualWidth > 0 ? CanvasContainer.ActualWidth : ActualWidth;
            double viewH = CanvasContainer.ActualHeight > 0 ? CanvasContainer.ActualHeight : ActualHeight;
            if (viewW <= 0) viewW = 800;
            if (viewH <= 0) viewH = 600;

            const double marginX = 140.0;
            const double marginY = 160.0;

            double availW = Math.Max(50.0, viewW - marginX);
            double availH = Math.Max(50.0, viewH - marginY);

            double selW = Math.Max(10.0, maxX - minX);
            double selH = Math.Max(10.0, maxY - minY);

            double scaleX = availW / selW;
            double scaleY = availH / selH;
            double fitScale = Math.Min(scaleX, scaleY) * 0.90;
            double targetZoom = Math.Clamp(fitScale, 0.15, 3.5);

            double selCenterX = minX + (selW / 2.0);
            double selCenterY = minY + (selH / 2.0);

            double viewCenterX = viewW / 2.0;
            double viewCenterY = (viewH + 40.0) / 2.0;

            double targetOffsetX = viewCenterX - (selCenterX * targetZoom);
            double targetOffsetY = viewCenterY - (selCenterY * targetZoom);

            AnimateCanvasView(targetZoom, targetOffsetX, targetOffsetY);
            ShowToast($"Zoomed to {_selectedCards.Count} references ({(int)Math.Round(targetZoom * 100)}%)", ToastType.Info);
        }

        private void AnimateCanvasView(double targetZoom, double targetOffsetX, double targetOffsetY)
        {
            if (_activeZoomAnimation != null)
            {
                CompositionTarget.Rendering -= _activeZoomAnimation;
                _activeZoomAnimation = null;
            }

            Matrix current = CanvasMatrixTransform.Matrix;
            double startZoom = current.M11;
            double startX = current.OffsetX;
            double startY = current.OffsetY;

            DateTime startTime = DateTime.Now;
            TimeSpan duration = TimeSpan.FromMilliseconds(220);

            _activeZoomAnimation = (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                double t = Math.Clamp(elapsed / duration.TotalMilliseconds, 0.0, 1.0);
                // Smooth cubic ease out: 1 - (1 - t)^3
                double ease = 1.0 - Math.Pow(1.0 - t, 3);

                double curZoom = startZoom + (targetZoom - startZoom) * ease;
                double curX = startX + (targetOffsetX - startX) * ease;
                double curY = startY + (targetOffsetY - startY) * ease;

                CanvasMatrixTransform.Matrix = new Matrix(curZoom, 0, 0, curZoom, curX, curY);
                TxtZoom.Text = $"Zoom: {(int)Math.Round(curZoom * 100)}%";
                SyncActiveHwndPositions(updateSize: true);

                if (t >= 1.0)
                {
                    if (_activeZoomAnimation != null)
                    {
                        CompositionTarget.Rendering -= _activeZoomAnimation;
                        _activeZoomAnimation = null;
                    }
                    SyncActiveHwndPositions(updateSize: true);
                    ScheduleAutoSave();
                }
            };

            CompositionTarget.Rendering += _activeZoomAnimation;
        }

        public void ZoomToFitAllCards(bool animated = false, bool showToast = true)
        {
            if (_cards.Count == 0)
            {
                if (animated)
                {
                    AnimateCanvasView(1.0, 0, 0);
                }
                else
                {
                    CanvasMatrixTransform.Matrix = Matrix.Identity;
                    TxtZoom.Text = "Zoom: 100%";
                    SyncActiveHwndPositions(updateSize: true);
                }
                return;
            }

            double minX = _cards.Min(c => c.X);
            double minY = _cards.Min(c => c.Y);
            double maxX = _cards.Max(c => c.X + c.Width);
            double maxY = _cards.Max(c => c.Y + c.Height);

            double boardW = Math.Max(50.0, maxX - minX);
            double boardH = Math.Max(50.0, maxY - minY);

            double viewW = CanvasContainer.ActualWidth > 0 ? CanvasContainer.ActualWidth : ActualWidth;
            double viewH = CanvasContainer.ActualHeight > 0 ? CanvasContainer.ActualHeight : ActualHeight;
            if (viewW <= 0) viewW = 1200;
            if (viewH <= 0) viewH = 800;

            const double padding = 100.0;
            double availW = Math.Max(50.0, viewW - (padding * 2));
            double availH = Math.Max(50.0, viewH - (padding * 2) - 40.0); // 40px top titlebar clearance

            double scaleX = availW / boardW;
            double scaleY = availH / boardH;
            double targetZoom = Math.Clamp(Math.Min(scaleX, scaleY), 0.10, 1.25);

            double centerX = minX + (boardW / 2.0);
            double centerY = minY + (boardH / 2.0);

            double viewCenterX = viewW / 2.0;
            double viewCenterY = (viewH + 40.0) / 2.0;

            double targetOffsetX = viewCenterX - (centerX * targetZoom);
            double targetOffsetY = viewCenterY - (centerY * targetZoom);

            if (animated)
            {
                AnimateCanvasView(targetZoom, targetOffsetX, targetOffsetY);
                if (showToast)
                {
                    ShowToast($"Framed {_cards.Count} references ({(int)Math.Round(targetZoom * 100)}%)", ToastType.Info);
                }
            }
            else
            {
                CanvasMatrixTransform.Matrix = new Matrix(targetZoom, 0, 0, targetZoom, targetOffsetX, targetOffsetY);
                TxtZoom.Text = $"Zoom: {(int)Math.Round(targetZoom * 100)}%";
                SyncActiveHwndPositions(updateSize: true);
            }
        }

        private void BtnFitAll_Click(object sender, MouseButtonEventArgs e)
        {
            ZoomToFitAllCards(animated: true);
            e.Handled = true;
        }

        private string GetExportPathForCard(CardItem item)
        {
            EnsureLocalCache(item);
            if (!item.IsCropped || item.Bitmap == null)
            {
                return item.LocalPath;
            }

            try
            {
                Directory.CreateDirectory(CacheDir);
                string filename = $"crop_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
                string targetPath = System.IO.Path.Combine(CacheDir, filename);

                int origW = item.Bitmap.PixelWidth;
                int origH = item.Bitmap.PixelHeight;
                int srcX = (int)Math.Round(origW * (item.CropLeft / 100.0));
                int srcY = (int)Math.Round(origH * (item.CropTop / 100.0));
                int srcW = (int)Math.Round(origW * (1.0 - (item.CropLeft + item.CropRight) / 100.0));
                int srcH = (int)Math.Round(origH * (1.0 - (item.CropTop + item.CropBottom) / 100.0));

                srcX = Math.Clamp(srcX, 0, origW - 1);
                srcY = Math.Clamp(srcY, 0, origH - 1);
                srcW = Math.Clamp(srcW, 1, origW - srcX);
                srcH = Math.Clamp(srcH, 1, origH - srcY);

                CroppedBitmap croppedBmp = new CroppedBitmap(item.Bitmap, new Int32Rect(srcX, srcY, srcW, srcH));
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBmp));
                using FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                encoder.Save(fs);
                return targetPath;
            }
            catch
            {
                return item.LocalPath;
            }
        }

        private void ExportCardsToAe(IEnumerable<CardItem> cards, string? compName = null, string mode = "loose_photos")
        {
            var cardList = cards.ToList();
            if (cardList.Count == 0) return;

            var payload = new AeExportCompPayload
            {
                Mode = mode,
                CompName = !string.IsNullOrWhiteSpace(compName) ? compName : (_projectName == "untitled" ? "References" : _projectName)
            };

            var clipboardPaths = new System.Collections.Specialized.StringCollection();

            foreach (var card in cardList)
            {
                string exportPath = GetExportPathForCard(card);
                if (!File.Exists(exportPath)) continue;

                clipboardPaths.Add(exportPath);
                payload.Items.Add(new AeExportItem
                {
                    FilePath = exportPath,
                    RelX = card.X,
                    RelY = card.Y,
                    Width = card.Width > 0 ? card.Width : 300,
                    Height = card.Height > 0 ? card.Height : 200
                });
            }

            if (payload.Items.Count == 0)
            {
                ShowToast("No valid image files available to export.", ToastType.Error);
                return;
            }

            // Also copy to clipboard for convenience
            try
            {
                Clipboard.SetFileDropList(clipboardPaths);
            }
            catch { }

            ShowToast(payload.Items.Count > 1
                ? $"Exporting {payload.Items.Count} references to After Effects..."
                : "Exporting reference to After Effects...", ToastType.Info, 2500);

            bool ok = AfterEffectsIntegration.ExportToAfterEffects(payload, out string outMsg);
            if (ok)
            {
                ShowToast($"📸 {outMsg}", ToastType.Success, 3500);
            }
            else
            {
                ShowToast($"After Effects: {outMsg}", ToastType.Error, 4000);
            }
        }

        private void SendCardToAe(CardItem item)
        {
            ExportCardsToAe(new[] { item });
        }

        private void SendCardToPs(CardItem item)
        {
            string exportPath = GetExportPathForCard(item);
            if (!File.Exists(exportPath)) return;

            try
            {
                string psExe = AfterEffectsIntegration.FindPhotoshopExe();
                if (!string.IsNullOrEmpty(psExe) && File.Exists(psExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = psExe,
                        Arguments = $"\"{exportPath}\"",
                        UseShellExecute = true
                    });
                    ShowToast("Opened reference in Adobe Photoshop", ToastType.Success);
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exportPath,
                        UseShellExecute = true
                    });
                    ShowToast("Opened reference in Default Viewer", ToastType.Info);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Photoshop error: {ex.Message}", ToastType.Error);
            }
        }

        #region YouTube Playback & AE Frame Capture

        public static void LogToFile(string message)
        {
            try
            {
                Directory.CreateDirectory(AppDataDir);
                string logFile = System.IO.Path.Combine(AppDataDir, "dropboard.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        private static Microsoft.Web.WebView2.Core.CoreWebView2Environment? _sharedWebViewEnv;

        private static async Task<Microsoft.Web.WebView2.Core.CoreWebView2Environment> GetSharedWebViewEnvAsync()
        {
            if (_sharedWebViewEnv != null) return _sharedWebViewEnv;

            string userDataDir = System.IO.Path.Combine(AppDataDir, "WebView2Profile");
            var envOptions = new Microsoft.Web.WebView2.Core.CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--in-process-gpu --disable-features=AudioServiceOutOfProcess,NetworkServiceInProcess,Translate,OptimizationHints --disable-gpu-shader-disk-cache --disable-crash-reporter --disable-crashpad --renderer-process-limit=1 --process-per-site --autoplay-policy=no-user-gesture-required"
            };
            _sharedWebViewEnv = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(userDataFolder: userDataDir, options: envOptions);
            return _sharedWebViewEnv;
        }

        private async void PlayYouTubeCard(CardItem item)
        {
            if (string.IsNullOrEmpty(item.YouTubeId) || item.IsPlayingYouTube) return;

            LogToFile($"PlayYouTubeCard initiated for id: {item.YouTubeId}");

            try
            {
                item.IsPlayingYouTube = true;
                item.AspectRatio = 16.0 / 9.0;
                double targetH = Math.Round(item.Width / (16.0 / 9.0));
                item.Height = targetH;
                item.BaseHeight = targetH;
                item.Container.Height = targetH;
                item.ContentBorder.Height = targetH;
                item.ImageControl.Height = targetH;

                if (item.BtnPlayOverlay?.Child is TextBlock tb)
                {
                    tb.Text = "⏹ Stop";
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)); // Amber stop
                }

                if (item.PlayerControl == null)
                {
                    var webView = new Microsoft.Web.WebView2.Wpf.WebView2
                    {
                        Width = double.NaN,
                        Height = double.NaN,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        DefaultBackgroundColor = System.Drawing.Color.FromArgb(15, 17, 23)
                    };
                    item.LastPixelX = int.MinValue;
                    item.LastPixelY = int.MinValue;
                    item.LastPixelW = int.MinValue;
                    item.LastPixelH = int.MinValue;
                    item.PlayerControl = webView;

                    // Ensure ContentBorder stays at index 0 as solid C# backing (Alpha = 255)
                    // Place webView right above ContentBorder at index 1 (behind handles & hover toolbar)
                    item.Container.Children.Insert(1, webView);

                    var env = await GetSharedWebViewEnvAsync();
                    await webView.EnsureCoreWebView2Async(env);

                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                    // Ensure local WebAssets folder exists with yt.html
                    string ytAssetsDir = System.IO.Path.Combine(AppDataDir, "WebAssets");
                    Directory.CreateDirectory(ytAssetsDir);
                    string ytHtmlPath = System.IO.Path.Combine(ytAssetsDir, "yt.html");
                    string ytHtmlContent = @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta http-equiv=""X-UA-Compatible"" content=""IE=Edge""/>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>YouTube Player</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    html, body { width: 100vw; height: 100vh; overflow: hidden; background: #000000; }
    #player-container { width: 100vw; height: 100vh; position: absolute; top: 0; left: 0; }
    iframe { border: none; width: 100% !important; height: 100% !important; display: block; }
  </style>
</head>
<body>
  <div id=""player-container""></div>
  <script>
    const params = new URLSearchParams(window.location.search);
    const ytId = params.get('id');
    const startTime = parseInt(params.get('t') || '0', 10);
    if (ytId) {
      const iframe = document.createElement('iframe');
      iframe.className = 'card-yt-iframe';
      let src = 'https://www.youtube-nocookie.com/embed/' + ytId + '?autoplay=1&playsinline=1&enablejsapi=1&rel=0';
      if (startTime > 0) {
        src += '&start=' + startTime;
      }
      iframe.src = src;
      iframe.title = 'YouTube Video Player';
      iframe.setAttribute('allow', 'accelerometer; autoplay *; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share');
      iframe.setAttribute('referrerpolicy', 'strict-origin-when-cross-origin');
      iframe.setAttribute('allowfullscreen', 'true');
      document.getElementById('player-container').appendChild(iframe);
    }

    window.requestCleanFrame = function() {
      const ifr = document.querySelector('iframe');
      if (ifr && ifr.contentWindow) {
        ifr.contentWindow.postMessage({ type: 'DROPBOARD_CAPTURE_YT_FRAME' }, '*');
      }
    };

    window.addEventListener('message', function(ev) {
      if (!ev.data) return;
      if (ev.data.type === 'DROPBOARD_YT_FRAME_RESULT') {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'YT_CLEAN_FRAME',
            dataUrl: ev.data.dataUrl
          }));
        }
      } else if (ev.data.type === 'DROPBOARD_YT_TIME_UPDATE') {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'YT_TIME_UPDATE',
            currentTime: ev.data.currentTime
          }));
        }
      } else if (typeof ev.data === 'string') {
        try {
          const d = JSON.parse(ev.data);
          if (d.event === 'infoDelivery' && d.info && typeof d.info.currentTime === 'number') {
            if (window.chrome && window.chrome.webview) {
              window.chrome.webview.postMessage(JSON.stringify({
                type: 'YT_TIME_UPDATE',
                currentTime: d.info.currentTime
              }));
            }
          }
        } catch(e) {}
      } else if (ev.data.type === 'DROPBOARD_IFRAME_CLICK') {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({ type: 'CARD_CLICK' }));
        }
      } else if (ev.data.type === 'DROPBOARD_IFRAME_CONTEXT_MENU') {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'IFRAME_CONTEXT_MENU',
            screenX: ev.data.screenX,
            screenY: ev.data.screenY
          }));
        }
      } else if (ev.data.type === 'DROPBOARD_IFRAME_WHEEL') {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'CANVAS_WHEEL',
            deltaY: ev.data.deltaY,
            screenX: ev.data.screenX,
            screenY: ev.data.screenY
          }));
        }
      } else if (ev.data.type === 'DROPBOARD_IFRAME_PAN_START') {
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'IFRAME_PAN_START',
            button: ev.data.button,
            screenX: ev.data.screenX,
            screenY: ev.data.screenY
          }));
        }
      }
    });

    window.addEventListener('mousedown', function(e) {
      if (e.button === 2) {
        e.preventDefault();
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'IFRAME_CONTEXT_MENU',
            screenX: e.screenX,
            screenY: e.screenY
          }));
        }
      } else if (e.button === 1) {
        e.preventDefault();
        if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage(JSON.stringify({
            type: 'IFRAME_PAN_START',
            button: e.button,
            screenX: e.screenX,
            screenY: e.screenY
          }));
        }
      }
    }, true);

    window.addEventListener('wheel', function(e) {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
          type: 'CANVAS_WHEEL',
          deltaY: e.deltaY,
          screenX: e.screenX,
          screenY: e.screenY
        }));
      }
    }, { passive: false });

    document.addEventListener('mousemove', function() {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ type: 'MOUSE_MOVE' }));
      }
    });
  </script>
</body>
</html>";
                    File.WriteAllText(ytHtmlPath, ytHtmlContent);

                    // Map virtual host name 'dropboard.local' to WebAssets folder
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "dropboard.local",
                        ytAssetsDir,
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
                    );

                    webView.CoreWebView2.WebMessageReceived += (s, e) =>
                    {
                        try
                        {
                            string rawMsg = e.TryGetWebMessageAsString();
                            if (string.IsNullOrEmpty(rawMsg)) return;

                            Dispatcher.InvokeAsync(() =>
                            {
                                if (rawMsg.Contains("MOUSE_MOVE"))
                                {
                                    if (Panel.GetZIndex(item.Container) < 500 && !item.IsSelected)
                                    {
                                        Panel.SetZIndex(item.Container, 500);
                                    }
                                    if (item.HoverToolbar != null)
                                    {
                                        item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                                        item.HoverToolbar.Opacity = 1.0;
                                        item.HoverToolbar.IsHitTestVisible = true;
                                    }
                                }
                                else if (rawMsg.Contains("IFRAME_PAN_START"))
                                {
                                    try
                                    {
                                        using var doc = System.Text.Json.JsonDocument.Parse(rawMsg);
                                        Point canvasPoint;
                                        if (doc.RootElement.TryGetProperty("screenX", out var sxEl) &&
                                            doc.RootElement.TryGetProperty("screenY", out var syEl))
                                        {
                                            canvasPoint = CanvasContainer.PointFromScreen(new Point(sxEl.GetDouble(), syEl.GetDouble()));
                                        }
                                        else
                                        {
                                            canvasPoint = Mouse.GetPosition(CanvasContainer);
                                        }

                                        _isPanning = true;
                                        _lastPanPoint = canvasPoint;
                                        SetWebViewHitTesting(false);
                                        CanvasContainer.CaptureMouse();
                                        Cursor = Cursors.Hand;
                                    }
                                    catch { }
                                }
                                else if (rawMsg.Contains("CARD_CLICK"))
                                {
                                    SelectCard(item, addToSelection: false);
                                }
                                else if (rawMsg.Contains("IFRAME_CONTEXT_MENU"))
                                {
                                    if (!item.IsSelected)
                                    {
                                        SelectCard(item, addToSelection: false);
                                    }
                                    if (item.Container.ContextMenu != null)
                                    {
                                        item.Container.ContextMenu.PlacementTarget = item.Container;
                                        item.Container.ContextMenu.IsOpen = true;
                                    }
                                }
                                else if (rawMsg.Contains("CANVAS_WHEEL"))
                                {
                                    try
                                    {
                                        using var doc = System.Text.Json.JsonDocument.Parse(rawMsg);
                                        if (doc.RootElement.TryGetProperty("deltaY", out var dyEl))
                                        {
                                            double dy = dyEl.GetDouble();
                                            int wpfDelta = dy > 0 ? -120 : 120;
                                            Point mousePos;
                                            if (doc.RootElement.TryGetProperty("screenX", out var sxEl) &&
                                                doc.RootElement.TryGetProperty("screenY", out var syEl))
                                            {
                                                mousePos = CanvasContainer.PointFromScreen(new Point(sxEl.GetDouble(), syEl.GetDouble()));
                                            }
                                            else
                                            {
                                                mousePos = new Point(CanvasContainer.ActualWidth / 2, CanvasContainer.ActualHeight / 2);
                                            }
                                            PerformCanvasZoom(wpfDelta, mousePos);
                                        }
                                    }
                                    catch { }
                                }
                                else if (rawMsg.Contains("YT_TIME_UPDATE"))
                                {
                                    try
                                    {
                                        using var doc = System.Text.Json.JsonDocument.Parse(rawMsg);
                                        if (doc.RootElement.TryGetProperty("currentTime", out var ctEl))
                                        {
                                            double sec = ctEl.GetDouble();
                                            if (sec > 0.5)
                                            {
                                                item.LastPlaybackSeconds = sec;
                                            }
                                        }
                                    }
                                    catch { }
                                }
                                else if (rawMsg.Contains("YT_CLEAN_FRAME"))
                                {
                                    try
                                    {
                                        using var doc = System.Text.Json.JsonDocument.Parse(rawMsg);
                                        if (doc.RootElement.TryGetProperty("dataUrl", out var urlEl))
                                        {
                                            string dataUrl = urlEl.GetString() ?? "";
                                            if (_pendingCleanFrameRequests.TryGetValue(item.Id, out var tcs))
                                            {
                                                tcs.TrySetResult(dataUrl);
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            });
                        }
                        catch { }
                    };

                    // Inject clean YouTube frame capture hook, auto-play, pan forwarding, time tracking & UI cleanup into all frames
                    await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
(function() {
  if (window.self === window.top) return;
  if (location.hostname.indexOf('youtube') === -1) return;

  function cleanYoutube() {
    try {
      if (!document.getElementById('dropboard-clean-yt')) {
        var style = document.createElement('style');
        style.id = 'dropboard-clean-yt';
        style.textContent = '.ytp-pause-overlay, .ytp-bezel, .ytp-chrome-top, .ytp-gradient-top, .ytp-gradient-bottom { display: none !important; }';
        (document.head || document.documentElement).appendChild(style);
      }
    } catch(e) {}
  }
  cleanYoutube();
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', cleanYoutube);
  } else {
    cleanYoutube();
  }

  // Auto-play immediately without waiting for extra user gesture
  function tryAutoPlay() {
    try {
      var v = document.querySelector('video');
      if (v && v.paused) {
        v.play().catch(function(){});
      }
      var btn = document.querySelector('.ytp-large-play-button');
      if (btn) btn.click();
    } catch(e) {}
  }
  setTimeout(tryAutoPlay, 300);
  setTimeout(tryAutoPlay, 700);
  setTimeout(tryAutoPlay, 1500);

  // Hook HTML5 <video> playback timestamp to remember last played position
  function hookTimeTracking() {
    try {
      var v = document.querySelector('video');
      if (v && !v._hasDropboardTracking) {
        v._hasDropboardTracking = true;
        v.addEventListener('timeupdate', function() {
          if (!v.paused && v.currentTime > 0) {
            window.parent.postMessage({
              type: 'DROPBOARD_YT_TIME_UPDATE',
              currentTime: v.currentTime
            }, '*');
          }
        });
        v.addEventListener('pause', function() {
          if (v.currentTime > 0) {
            window.parent.postMessage({
              type: 'DROPBOARD_YT_TIME_UPDATE',
              currentTime: v.currentTime
            }, '*');
          }
        });
      }
    } catch(e) {}
  }
  setInterval(hookTimeTracking, 1000);

  // Forward click inside video to WPF so card is selected; forward right click to open context menu; middle click to pan
  window.addEventListener('mousedown', function(e) {
    if (e.button === 0) {
      window.parent.postMessage({ type: 'DROPBOARD_IFRAME_CLICK' }, '*');
    } else if (e.button === 2) {
      e.preventDefault();
      window.parent.postMessage({
        type: 'DROPBOARD_IFRAME_CONTEXT_MENU',
        screenX: e.screenX,
        screenY: e.screenY
      }, '*');
    } else if (e.button === 1) {
      e.preventDefault();
      window.parent.postMessage({
        type: 'DROPBOARD_IFRAME_PAN_START',
        button: e.button,
        screenX: e.screenX,
        screenY: e.screenY
      }, '*');
    }
  }, true);

  // Forward mouse wheel so canvas zoom works when hovering over video
  window.addEventListener('wheel', function(e) {
    window.parent.postMessage({
      type: 'DROPBOARD_IFRAME_WHEEL',
      deltaY: e.deltaY,
      screenX: e.screenX,
      screenY: e.screenY
    }, '*');
  }, { passive: false });

  // Capture clean video frame from HTML5 <video> directly into offscreen <canvas>
  window.addEventListener('message', function(ev) {
    if (!ev.data) return;
    if (ev.data.type === 'DROPBOARD_CAPTURE_YT_FRAME') {
      try {
        var v = document.querySelector('video');
        if (v) {
          var w = v.videoWidth || v.clientWidth || 1280;
          var h = v.videoHeight || v.clientHeight || 720;
          var c = document.createElement('canvas');
          c.width = w;
          c.height = h;
          var ctx = c.getContext('2d');
          ctx.drawImage(v, 0, 0, w, h);
          var dataUrl = c.toDataURL('image/png');
          window.parent.postMessage({
            type: 'DROPBOARD_YT_FRAME_RESULT',
            dataUrl: dataUrl
          }, '*');
        }
      } catch(err) {
        console.warn('Frame capture error:', err);
      }
    }
  });
})();
");
                }

                item.PlayerControl.Visibility = Visibility.Visible;
                item.ContentBorder.Visibility = Visibility.Visible;
                item.ContentBorder.IsHitTestVisible = false; // Mouse events pass straight to webView
                item.ImageControl.Visibility = Visibility.Collapsed;
                if (item.NativePlayer != null) item.NativePlayer.Visibility = Visibility.Collapsed;
                if (item.YouTubeTagBadge != null) item.YouTubeTagBadge.Visibility = Visibility.Collapsed;
                if (item.CenterPlayBtn != null) item.CenterPlayBtn.Visibility = Visibility.Collapsed;

                string playerUrl = $"https://dropboard.local/yt.html?id={item.YouTubeId}";
                if (item.LastPlaybackSeconds > 1.0)
                {
                    playerUrl += $"&t={(int)Math.Floor(item.LastPlaybackSeconds)}";
                }
                item.PlayerControl.CoreWebView2.Navigate(playerUrl);

                if (item.HoverToolbar != null)
                {
                    item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                    item.HoverToolbar.Opacity = 1.0;
                    item.HoverToolbar.IsHitTestVisible = true;
                }

                if (item.LastPlaybackSeconds > 1.0)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(item.LastPlaybackSeconds);
                    string timeStr = ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
                    ShowToast($"▶ Resuming YouTube from {timeStr}", ToastType.Info, 2000);
                }
                else
                {
                    ShowToast("▶ Streaming YouTube", ToastType.Info, 1800);
                }
            }
            catch (Exception ex)
            {
                LogToFile($"PlayYouTubeCard exception: {ex.Message}");
                StopYouTubeCard(item);
                ShowToast($"Playback error: {ex.Message}", ToastType.Error);
            }
        }

        private void StopYouTubeCard(CardItem item)
        {
            item.IsPlayingYouTube = false;
            if (item.BtnPlayOverlay?.Child is TextBlock tb)
            {
                tb.Text = "▶ Play";
                tb.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red play
            }

            if (item.NativePlayer != null)
            {
                try
                {
                    item.NativePlayer.Stop();
                    item.NativePlayer.Source = null;
                }
                catch { }
                item.NativePlayer.Visibility = Visibility.Collapsed;
            }

            if (item.PlayerControl != null)
            {
                try
                {
                    item.PlayerControl.Source = new Uri("about:blank");
                    item.PlayerControl.Dispose();
                    item.Container.Children.Remove(item.PlayerControl);
                    item.PlayerControl = null;
                }
                catch { }
            }
            item.LastPixelX = int.MinValue;
            item.LastPixelY = int.MinValue;
            item.LastPixelW = int.MinValue;
            item.LastPixelH = int.MinValue;
            item.ContentBorder.Visibility = Visibility.Visible;
            item.ContentBorder.IsHitTestVisible = true;
            item.ImageControl.Visibility = Visibility.Visible;
            if (item.YouTubeTagBadge != null) item.YouTubeTagBadge.Visibility = Visibility.Visible;
            if (item.CenterPlayBtn != null) item.CenterPlayBtn.Visibility = Visibility.Visible;

            if (item.HoverToolbar != null && !item.IsSelected && !item.Container.IsMouseOver)
            {
                item.HoverToolbar.BeginAnimation(UIElement.OpacityProperty, null);
                item.HoverToolbar.Opacity = 0.0;
                item.HoverToolbar.IsHitTestVisible = false;
            }
        }

        private void ToggleYouTubePlayback(CardItem item)
        {
            if (!item.IsYouTube) return;
            if (item.IsPlayingYouTube)
            {
                StopYouTubeCard(item);
            }
            else
            {
                PlayYouTubeCard(item);
            }
        }

        private async Task<byte[]?> CaptureCleanYouTubeFrameAsync(CardItem item)
        {
            if (item == null) return null;

            byte[]? frameBytes = null;

            // 1. If NativePlayer (WPF MediaElement) is playing, capture instantly via RenderTargetBitmap
            if (item.IsPlayingYouTube && item.NativePlayer != null && item.NativePlayer.Visibility == Visibility.Visible)
            {
                try
                {
                    int w = Math.Max(10, (int)item.Width);
                    int h = Math.Max(10, (int)item.Height);
                    var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(item.NativePlayer);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var ms = new MemoryStream();
                    encoder.Save(ms);
                    frameBytes = ms.ToArray();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MediaElement capture error: {ex.Message}");
                }
            }
            // 2. If WebView2 is actively playing, capture CLEAN frame directly from HTML5 <video> element (no player UI overlays!)
            else if (item.IsPlayingYouTube && item.PlayerControl?.CoreWebView2 != null)
            {
                try
                {
                    var tcs = new TaskCompletionSource<string>();
                    _pendingCleanFrameRequests[item.Id] = tcs;

                    await item.PlayerControl.ExecuteScriptAsync("window.requestCleanFrame && window.requestCleanFrame();");

                    var completed = await Task.WhenAny(tcs.Task, Task.Delay(850));
                    if (completed == tcs.Task)
                    {
                        string dataUrl = await tcs.Task;
                        if (!string.IsNullOrEmpty(dataUrl))
                        {
                            string b64 = dataUrl.Contains(",") ? dataUrl.Substring(dataUrl.IndexOf(",") + 1) : dataUrl;
                            frameBytes = Convert.FromBase64String(b64);
                        }
                    }
                    _pendingCleanFrameRequests.Remove(item.Id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Clean canvas snapshot error: {ex.Message}");
                }

                // Fallback to viewport capture only if clean canvas extraction timed out
                if (frameBytes == null || frameBytes.Length == 0)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        await item.PlayerControl.CoreWebView2.CapturePreviewAsync(
                            Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,
                            ms);
                        frameBytes = ms.ToArray();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CapturePreviewAsync fallback error: {ex.Message}");
                    }
                }
            }

            // 3. Fallback: if not playing or preview capture returned empty, use card visual Bitmap
            if (frameBytes == null || frameBytes.Length == 0)
            {
                if (item.Bitmap != null)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(item.Bitmap));
                    using var ms = new MemoryStream();
                    encoder.Save(ms);
                    frameBytes = ms.ToArray();
                }
            }

            return frameBytes;
        }

        private async void SnapYouTubeFrameToAE(CardItem item)
        {
            if (item == null) return;

            ShowToast("📸 Capturing clean video frame to After Effects...", ToastType.Info, 2000);

            try
            {
                byte[]? frameBytes = await CaptureCleanYouTubeFrameAsync(item);
                if (frameBytes == null || frameBytes.Length == 0)
                {
                    ShowToast("Could not capture video frame", ToastType.Error);
                    return;
                }

                Directory.CreateDirectory(CacheDir);
                using var md5 = System.Security.Cryptography.MD5.Create();
                string hash = Convert.ToHexString(md5.ComputeHash(frameBytes))[..12].ToLowerInvariant();
                string filename = $"snap_yt_{hash}.png";
                string snapPath = System.IO.Path.Combine(CacheDir, filename);
                if (!File.Exists(snapPath))
                {
                    await File.WriteAllBytesAsync(snapPath, frameBytes);
                }

                // Export directly to After Effects WITHOUT adding a card to DropBoard canvas!
                var payload = new AeExportCompPayload
                {
                    Mode = "loose_photos"
                };
                payload.Items.Add(new AeExportItem
                {
                    FilePath = snapPath,
                    RelX = item.X,
                    RelY = item.Y,
                    Width = item.Width,
                    Height = item.Height
                });

                bool aeOk = AfterEffectsIntegration.ExportToAfterEffects(payload, out string aeMsg);
                if (aeOk)
                {
                    ShowToast("📸 Clean video frame exported to After Effects!", ToastType.Success, 3500);
                }
                else
                {
                    ShowToast($"Frame snapped, but AE: {aeMsg}", ToastType.Info, 3500);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Export error: {ex.Message}", ToastType.Error);
            }
        }

        private async void SnapYouTubeFrameToBoard(CardItem item)
        {
            if (item == null) return;

            ShowToast("📸 Capturing video snapshot to canvas...", ToastType.Info, 2000);

            try
            {
                byte[]? frameBytes = await CaptureCleanYouTubeFrameAsync(item);
                if (frameBytes == null || frameBytes.Length == 0)
                {
                    ShowToast("Could not capture video frame", ToastType.Error);
                    return;
                }

                Directory.CreateDirectory(CacheDir);
                using var md5 = System.Security.Cryptography.MD5.Create();
                string hash = Convert.ToHexString(md5.ComputeHash(frameBytes))[..12].ToLowerInvariant();
                string filename = $"snap_yt_{hash}.png";
                string snapPath = System.IO.Path.Combine(CacheDir, filename);
                if (!File.Exists(snapPath))
                {
                    await File.WriteAllBytesAsync(snapPath, frameBytes);
                }

                using (var imgMs = new MemoryStream(frameBytes))
                {
                    BitmapImage snappedBmp = new BitmapImage();
                    snappedBmp.BeginInit();
                    snappedBmp.StreamSource = imgMs;
                    snappedBmp.CacheOption = BitmapCacheOption.OnLoad;
                    snappedBmp.EndInit();
                    snappedBmp.Freeze();

                    Point newCardPos = new Point(item.X + item.Width + _currentGap, item.Y);
                    string b64 = $"data:image/png;base64,{Convert.ToBase64String(frameBytes)}";

                    CardItem newCard = AddImageCard(
                        snappedBmp,
                        worldPosition: newCardPos,
                        customWidth: item.Width,
                        customHeight: item.Height,
                        localPath: snapPath,
                        base64Data: b64,
                        autoSelect: true);

                    RecordUndo("Snap Video Frame to Board");
                }

                ShowToast("📋 Video snapshot card added to canvas", ToastType.Success, 2500);
            }
            catch (Exception ex)
            {
                ShowToast($"Snapshot error: {ex.Message}", ToastType.Error);
            }
        }

        #endregion

        #region Native Local Video Playback (.mp4, .mkv, .3gp, .webm, .mov, .avi, .wmv)

        private static readonly HashSet<string> SupportedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".mkv", ".webm", ".avi", ".wmv", ".3gp", ".m4v"
        };

        private static bool IsSupportedVideoExtension(string? ext)
        {
            if (string.IsNullOrEmpty(ext)) return false;
            return SupportedVideoExtensions.Contains(ext);
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a140df3cb83")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SH_SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SH_SIZE
        {
            public int cx;
            public int cy;
            public SH_SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [Flags]
        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory factory);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private static BitmapSource GetVideoPosterBitmap(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    Guid uuid = new Guid("bcc18b79-ba16-442f-80c4-8a140df3cb83");
                    int hr = SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref uuid, out IShellItemImageFactory factory);
                    if (hr == 0 && factory != null)
                    {
                        hr = factory.GetImage(new SH_SIZE(640, 640), SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_RESIZETOFIT, out IntPtr hBitmap);
                        if (hr != 0 || hBitmap == IntPtr.Zero)
                        {
                            hr = factory.GetImage(new SH_SIZE(256, 256), SIIGBF.SIIGBF_RESIZETOFIT, out hBitmap);
                        }
                        if (hr == 0 && hBitmap != IntPtr.Zero)
                        {
                            try
                            {
                                var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                bmp.Freeze();
                                return bmp;
                            }
                            finally
                            {
                                DeleteObject(hBitmap);
                            }
                        }
                    }
                }
            }
            catch { }

            return CreateFallbackVideoPoster(filePath);
        }

        private static BitmapSource CreateFallbackVideoPoster(string filePath)
        {
            string fileName = string.IsNullOrEmpty(filePath) ? "Video File" : System.IO.Path.GetFileName(filePath);
            string ext = string.IsNullOrEmpty(filePath) ? "VIDEO" : System.IO.Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.');
            int width = 640;
            int height = 360;
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var bg = new LinearGradientBrush(
                    Color.FromRgb(15, 23, 42),
                    Color.FromRgb(30, 41, 59),
                    new Point(0, 0),
                    new Point(1, 1));
                dc.DrawRectangle(bg, null, new Rect(0, 0, width, height));

                dc.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)), 2), new Rect(1, 1, width - 2, height - 2));

                var playBrush = new SolidColorBrush(Color.FromArgb(200, 56, 189, 248));
                var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                var playText = new FormattedText("▶", System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, 56, playBrush, 96);
                dc.DrawText(playText, new Point((width - playText.Width) / 2, height / 2 - 45));

                var badgeText = new FormattedText($"[{ext}] {fileName}", System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, 15, Brushes.White, 96);
                dc.DrawText(badgeText, new Point((width - badgeText.Width) / 2, height / 2 + 25));
            }
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        private static string FormatVideoDuration(double currentSec, double totalSec)
        {
            TimeSpan cur = TimeSpan.FromSeconds(Math.Max(0, currentSec));
            TimeSpan tot = TimeSpan.FromSeconds(Math.Max(0, totalSec));
            string curStr = cur.Hours > 0 ? cur.ToString(@"h\:mm\:ss") : cur.ToString(@"m\:ss");
            string totStr = tot.Hours > 0 ? tot.ToString(@"h\:mm\:ss") : tot.ToString(@"m\:ss");
            return $"{curStr} / {totStr}";
        }

        private CardItem AddLocalVideoCard(
            string filePath,
            Point? worldPosition = null,
            double? customWidth = null,
            double? customHeight = null,
            bool autoSelect = true,
            bool isLooping = true,
            bool isMuted = true,
            bool recordUndo = true)
        {
            BitmapSource poster = GetVideoPosterBitmap(filePath);
            return AddImageCard(
                poster,
                worldPosition: worldPosition,
                customWidth: customWidth,
                customHeight: customHeight,
                localPath: filePath,
                autoSelect: autoSelect,
                recordUndo: recordUndo,
                isLocalVideo: true,
                videoFilePath: filePath,
                isVideoLooping: isLooping,
                isVideoMuted: isMuted);
        }

        private void ImportVideoFilesBatch(IEnumerable<string> filePaths, Point startWorldPos)
        {
            var list = filePaths.Where(f => !string.IsNullOrEmpty(f) && File.Exists(f)).ToList();
            if (list.Count == 0) return;

            Point curPos = startWorldPos;
            foreach (string file in list)
            {
                AddLocalVideoCard(file, worldPosition: curPos, autoSelect: false, recordUndo: false);
                curPos.X += 35;
                curPos.Y += 35;
            }
            RecordUndo($"Import {list.Count} Video{(list.Count > 1 ? "s" : "")}");
            UpdateViewportCulling();
            ShowToast($"Imported {list.Count} local video{(list.Count > 1 ? "s" : "")}!", ToastType.Success);
            ScheduleAutoSave();
        }

        private void ToggleLocalVideoPlayback(CardItem item)
        {
            if (!item.IsLocalVideo) return;
            if (item.IsVideoPlaying)
            {
                PauseLocalVideo(item);
            }
            else
            {
                PlayLocalVideo(item);
            }
        }

        private void PlayLocalVideo(CardItem item)
        {
            if (!item.IsLocalVideo || item.NativePlayer == null) return;

            try
            {
                if (item.NativePlayer.Source == null && !string.IsNullOrEmpty(item.VideoFilePath) && File.Exists(item.VideoFilePath))
                {
                    item.NativePlayer.Source = new Uri(item.VideoFilePath, UriKind.Absolute);
                }

                item.NativePlayer.IsMuted = item.IsVideoMuted;
                item.NativePlayer.Visibility = Visibility.Visible;
                item.ImageControl.Visibility = Visibility.Collapsed;
                item.NativePlayer.Play();
                item.IsVideoPlaying = true;

                if (item.VideoPlaybackTimer != null && !item.VideoPlaybackTimer.IsEnabled)
                {
                    item.VideoPlaybackTimer.Start();
                }

                UpdateVideoCardUI(item);
                ShowToast($"▶ Playing video ({System.IO.Path.GetFileName(item.VideoFilePath)})", ToastType.Info, 1800);
            }
            catch (Exception ex)
            {
                LogToFile($"PlayLocalVideo exception: {ex.Message}");
                StopLocalVideo(item);
                ShowToast($"Video playback error: {ex.Message}", ToastType.Error);
            }
        }

        private void PauseLocalVideo(CardItem item)
        {
            if (!item.IsLocalVideo || item.NativePlayer == null) return;
            try
            {
                item.NativePlayer.Pause();
                item.IsVideoPlaying = false;
                if (item.VideoPlaybackTimer != null && item.VideoPlaybackTimer.IsEnabled)
                {
                    item.VideoPlaybackTimer.Stop();
                }
                UpdateVideoCardUI(item);
                ShowToast("⏸ Video Paused", ToastType.Info, 1200);
            }
            catch (Exception ex)
            {
                LogToFile($"PauseLocalVideo error: {ex.Message}");
            }
        }

        private void StopLocalVideo(CardItem item)
        {
            if (!item.IsLocalVideo || item.NativePlayer == null) return;
            try
            {
                item.NativePlayer.Pause();
                item.NativePlayer.Position = TimeSpan.FromMilliseconds(150);
                item.IsVideoPlaying = false;
                if (item.VideoPlaybackTimer != null && item.VideoPlaybackTimer.IsEnabled)
                {
                    item.VideoPlaybackTimer.Stop();
                }
                UpdateVideoCardUI(item);
            }
            catch (Exception ex)
            {
                LogToFile($"StopLocalVideo error: {ex.Message}");
            }
        }

        private void CleanupLocalVideoCard(CardItem item)
        {
            if (item.VideoPlaybackTimer != null)
            {
                try { item.VideoPlaybackTimer.Stop(); } catch { }
                item.VideoPlaybackTimer = null;
            }
            if (item.NativePlayer != null)
            {
                try
                {
                    item.NativePlayer.Stop();
                    item.NativePlayer.Source = null;
                    item.NativePlayer.Close();
                }
                catch { }
                item.NativePlayer = null;
            }
            item.IsVideoPlaying = false;
        }

        private void UpdateVideoCardUI(CardItem item)
        {
            if (item.BtnVideoPlayOverlay?.Child is TextBlock tb)
            {
                tb.Text = item.IsVideoPlaying ? "⏸ Pause" : "▶ Play";
                tb.Foreground = new SolidColorBrush(item.IsVideoPlaying ? Color.FromRgb(251, 191, 36) : Color.FromRgb(56, 189, 248));
            }
            if (item.CenterVideoPlayBtn != null)
            {
                item.CenterVideoPlayBtn.Visibility = item.IsVideoPlaying ? Visibility.Collapsed : Visibility.Visible;
            }
            if (item.VideoScrubberContainer != null)
            {
                item.VideoScrubberContainer.Visibility = (item.IsVideoPlaying || item.IsSelected || item.Container.IsMouseOver)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void FitVideoCardToResolution(CardItem item, bool notify = true)
        {
            if (!item.IsLocalVideo) return;

            double naturalW = 0;
            double naturalH = 0;

            if (item.NativePlayer != null && item.NativePlayer.NaturalVideoWidth > 0 && item.NativePlayer.NaturalVideoHeight > 0)
            {
                naturalW = item.NativePlayer.NaturalVideoWidth;
                naturalH = item.NativePlayer.NaturalVideoHeight;
            }
            else if (item.Bitmap != null && item.Bitmap.PixelWidth > 0 && item.Bitmap.PixelHeight > 0)
            {
                naturalW = item.Bitmap.PixelWidth;
                naturalH = item.Bitmap.PixelHeight;
            }

            if (naturalW <= 0 || naturalH <= 0)
            {
                if (notify) ShowToast("Video dimensions not yet loaded. Play video first.", ToastType.Info);
                return;
            }

            if (notify) RecordUndo("Fit Video Resolution");

            double aspect = naturalW / naturalH;
            item.AspectRatio = aspect;

            double currentW = item.Width;
            double currentH = item.Height;

            double targetW;
            double targetH;

            if (naturalH >= naturalW)
            {
                // Portrait / Vertical Video (e.g. 9:16)
                targetH = Math.Clamp(Math.Max(currentH, 360.0), 240.0, 620.0);
                targetW = Math.Round(targetH * aspect);
            }
            else
            {
                // Landscape / Standard Video (e.g. 16:9)
                targetW = Math.Clamp(Math.Max(currentW, 360.0), 280.0, 720.0);
                targetH = Math.Round(targetW / aspect);
            }

            item.Width = targetW;
            item.Height = targetH;
            item.BaseWidth = targetW;
            item.BaseHeight = targetH;

            if (item.ImageControl != null) item.ImageControl.Stretch = Stretch.Uniform;
            if (item.NativePlayer != null) item.NativePlayer.Stretch = Stretch.Uniform;

            if (notify)
            {
                ShowToast($"⛶ Fitted to {(int)naturalW}x{(int)naturalH} ({aspect:F2}:1)", ToastType.Success, 2000);
                ScheduleAutoSave();
            }
        }

        private void ResetVideoToOriginal1to1(CardItem item)
        {
            if (!item.IsLocalVideo) return;
            double naturalW = 0;
            double naturalH = 0;
            if (item.NativePlayer != null && item.NativePlayer.NaturalVideoWidth > 0 && item.NativePlayer.NaturalVideoHeight > 0)
            {
                naturalW = item.NativePlayer.NaturalVideoWidth;
                naturalH = item.NativePlayer.NaturalVideoHeight;
            }
            if (naturalW > 0 && naturalH > 0)
            {
                RecordUndo("Reset Video to 1:1");
                item.AspectRatio = naturalW / naturalH;
                item.Width = naturalW;
                item.Height = naturalH;
                item.BaseWidth = naturalW;
                item.BaseHeight = naturalH;
                ShowToast($"Reset to 1:1 Original Res: {(int)naturalW}x{(int)naturalH}", ToastType.Success);
                ScheduleAutoSave();
            }
        }

        private async void SnapLocalVideoFrameToAE(CardItem item)
        {
            if (item == null) return;
            ShowToast("📸 Capturing clean video frame to After Effects...", ToastType.Info, 2000);
            try
            {
                byte[]? frameBytes = CaptureCleanLocalVideoFrame(item);
                if (frameBytes == null || frameBytes.Length == 0)
                {
                    ShowToast("Could not capture video frame", ToastType.Error);
                    return;
                }

                Directory.CreateDirectory(CacheDir);
                using var md5 = System.Security.Cryptography.MD5.Create();
                string hash = Convert.ToHexString(md5.ComputeHash(frameBytes))[..12].ToLowerInvariant();
                string filename = $"snap_vid_{hash}.png";
                string snapPath = System.IO.Path.Combine(CacheDir, filename);
                if (!File.Exists(snapPath))
                {
                    await File.WriteAllBytesAsync(snapPath, frameBytes);
                }

                var payload = new AeExportCompPayload { Mode = "loose_photos" };
                payload.Items.Add(new AeExportItem
                {
                    FilePath = snapPath,
                    RelX = item.X,
                    RelY = item.Y,
                    Width = item.Width,
                    Height = item.Height
                });

                bool aeOk = AfterEffectsIntegration.ExportToAfterEffects(payload, out string aeMsg);
                if (aeOk)
                {
                    ShowToast("📸 Clean video frame exported to After Effects!", ToastType.Success, 3500);
                }
                else
                {
                    ShowToast($"Frame snapped, but AE: {aeMsg}", ToastType.Info, 3500);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"Export error: {ex.Message}", ToastType.Error);
            }
        }

        private async void SnapLocalVideoFrameToBoard(CardItem item)
        {
            if (item == null) return;
            ShowToast("📸 Capturing video snapshot to canvas...", ToastType.Info, 2000);
            try
            {
                byte[]? frameBytes = CaptureCleanLocalVideoFrame(item);
                if (frameBytes == null || frameBytes.Length == 0)
                {
                    ShowToast("Could not capture video frame", ToastType.Error);
                    return;
                }

                Directory.CreateDirectory(CacheDir);
                using var md5 = System.Security.Cryptography.MD5.Create();
                string hash = Convert.ToHexString(md5.ComputeHash(frameBytes))[..12].ToLowerInvariant();
                string filename = $"snap_vid_{hash}.png";
                string snapPath = System.IO.Path.Combine(CacheDir, filename);
                if (!File.Exists(snapPath))
                {
                    await File.WriteAllBytesAsync(snapPath, frameBytes);
                }

                using var ms = new MemoryStream(frameBytes);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                Point newPos = new Point(item.X + item.Width + 24, item.Y);
                string b64 = $"data:image/png;base64,{Convert.ToBase64String(frameBytes)}";

                RecordUndo("Snap Video Frame to Board");
                AddImageCard(
                    bi,
                    worldPosition: newPos,
                    customWidth: item.Width,
                    customHeight: item.Height,
                    localPath: snapPath,
                    base64Data: b64,
                    autoSelect: true,
                    recordUndo: false);

                ShowToast("📋 Video snapshot card added to canvas", ToastType.Success, 2500);
            }
            catch (Exception ex)
            {
                ShowToast($"Snapshot error: {ex.Message}", ToastType.Error);
            }
        }

        private byte[]? CaptureCleanLocalVideoFrame(CardItem item)
        {
            if (item == null) return null;
            try
            {
                int w = Math.Max(10, (int)item.Width);
                int h = Math.Max(10, (int)item.Height);
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                if (item.NativePlayer != null && item.NativePlayer.Visibility == Visibility.Visible)
                {
                    rtb.Render(item.NativePlayer);
                }
                else if (item.ImageControl != null)
                {
                    rtb.Render(item.ImageControl);
                }

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                LogToFile($"CaptureCleanLocalVideoFrame error: {ex.Message}");
                return null;
            }
        }

        #endregion

        private const string YOUTUBE_API_KEY = "AIzaSyCKx_Tba2ezZ2WlVtZGtf1KvA_jLFji2wQ";

        private static async Task<(string? bestThumbnailUrl, string? title)> FetchYouTubeMetadataViaApiAsync(string ytId, HttpClient client)
        {
            try
            {
                string endpoint = $"https://www.googleapis.com/youtube/v3/videos?id={ytId}&key={YOUTUBE_API_KEY}&part=snippet";
                string res = await client.GetStringAsync(endpoint);
                using var doc = JsonDocument.Parse(res);
                if (doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    var snippet = items[0].GetProperty("snippet");
                    string title = snippet.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    if (snippet.TryGetProperty("thumbnails", out var thumbs))
                    {
                        if (thumbs.TryGetProperty("maxres", out var maxres) && maxres.TryGetProperty("url", out var u1))
                            return (u1.GetString(), title);
                        if (thumbs.TryGetProperty("standard", out var std) && std.TryGetProperty("url", out var u2))
                            return (u2.GetString(), title);
                        if (thumbs.TryGetProperty("high", out var high) && high.TryGetProperty("url", out var u3))
                            return (u3.GetString(), title);
                    }
                    return (null, title);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"YouTube API error: {ex.Message}");
            }
            return (null, null);
        }

        private async Task AddImageFromUrlAsync(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                if (url.StartsWith("data:image/") && url.Contains(";base64,"))
                {
                    int comma = url.IndexOf(',');
                    string dataB64 = url.Substring(comma + 1);
                    byte[] bytes = Convert.FromBase64String(dataB64);
                    using MemoryStream ms = new MemoryStream(bytes);
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    AddImageCard(bmp, base64Data: url);
                    return;
                }

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                // Pinterest resolver: resolves pin pages (pinterest.com/pin/..., id.pinterest.com/pin/..., pin.it/...)
                // and upgrades pinimg thumbnails to 736x high-res
                if (url.Contains("pinterest.") || url.Contains("pin.it") || url.Contains("pinimg.com"))
                {
                    url = await ResolvePinterestImageUrlAsync(url, client);
                    if (string.IsNullOrEmpty(url))
                    {
                        ShowToast("Could not resolve Pinterest image", ToastType.Error);
                        return;
                    }
                }

                // YouTube thumbnail resolver (uses Google YouTube Data API v3 with user API Key)
                bool isYt = false;
                string ytId = "";
                string originalYtUrl = "";

                if (url.Contains("youtube.com") || url.Contains("youtu.be"))
                {
                    ytId = ExtractYouTubeId(url);
                    if (!string.IsNullOrEmpty(ytId))
                    {
                        isYt = true;
                        originalYtUrl = url;

                        var (apiThumb, apiTitle) = await FetchYouTubeMetadataViaApiAsync(ytId, client);
                        if (!string.IsNullOrEmpty(apiTitle))
                        {
                            title = apiTitle;
                        }
                        url = !string.IsNullOrEmpty(apiThumb) ? apiThumb : $"https://img.youtube.com/vi/{ytId}/maxresdefault.jpg";
                    }
                }

                byte[] data;
                try
                {
                    data = await client.GetByteArrayAsync(url);
                }
                catch
                {
                    if (url.Contains("maxresdefault.jpg"))
                    {
                        string fallbackUrl = url.Replace("maxresdefault.jpg", "hqdefault.jpg");
                        data = await client.GetByteArrayAsync(fallbackUrl);
                        url = fallbackUrl;
                    }
                    else
                    {
                        throw;
                    }
                }

                Directory.CreateDirectory(CacheDir);
                bool isGif = url.Contains(".gif", StringComparison.OrdinalIgnoreCase) ||
                             (data.Length > 3 && data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F');
                string ext = isGif ? ".gif" : (url.Contains(".png") ? ".png" : (url.Contains(".webp") ? ".webp" : ".jpg"));

                using var md5 = System.Security.Cryptography.MD5.Create();
                string hash = Convert.ToHexString(md5.ComputeHash(data))[..12].ToLowerInvariant();
                string filename = $"ref_{hash}{ext}";
                string cachedPath = System.IO.Path.Combine(CacheDir, filename);
                if (!File.Exists(cachedPath))
                {
                    await File.WriteAllBytesAsync(cachedPath, data);
                }

                using MemoryStream imgMs = new MemoryStream(data);
                BitmapImage fetchedBmp = new BitmapImage();
                fetchedBmp.BeginInit();
                fetchedBmp.StreamSource = imgMs;
                fetchedBmp.CacheOption = BitmapCacheOption.OnLoad;
                fetchedBmp.EndInit();
                fetchedBmp.Freeze();

                string mime = isGif ? "image/gif" : (ext == ".png" ? "image/png" : (ext == ".webp" ? "image/webp" : "image/jpeg"));
                string b64 = $"data:{mime};base64,{Convert.ToBase64String(data)}";
                AddImageCard(
                    fetchedBmp,
                    localPath: cachedPath,
                    base64Data: b64,
                    isYouTube: isYt,
                    youTubeId: ytId,
                    youTubeUrl: originalYtUrl);

                bool isPinterest = url.Contains("pinimg.com") || url.Contains("pinterest.");
                if (isYt)
                    ShowToast("Added YouTube Reference Video!", ToastType.Success);
                else if (isPinterest)
                    ShowToast("Pinterest reference loaded!", ToastType.Success);
                else
                    ShowToast("Received reference from Web URL!", ToastType.Success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add image from URL '{url}': {ex.Message}");
                ShowToast("Failed to load image from URL", ToastType.Error);
            }
        }

        private static string ExtractYouTubeId(string url)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    url,
                    @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?|shorts)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups[1].Value;
            }
            catch { }
            return "";
        }

        private static async Task<string> ResolvePinterestImageUrlAsync(string url, HttpClient client)
        {
            // 1. If it's already a direct pinimg.com image URL, upgrade to 736x high-res
            if (url.Contains("pinimg.com/"))
            {
                return System.Text.RegularExpressions.Regex.Replace(url, @"\/(236x|474x|564x|170x)\/", "/736x/");
            }

            // 2. If it is a Pinterest pin page link (pinterest.com/pin/..., id.pinterest.com/pin/..., pin.it/...)
            if (url.Contains("pinterest.com/pin/") || (url.Contains("pinterest.") && url.Contains("/pin/")) || url.Contains("pin.it/"))
            {
                try
                {
                    // Try to extract numeric Pin ID
                    var match = System.Text.RegularExpressions.Regex.Match(url, @"\/pin\/(\d+)");
                    string oembedUrl;
                    if (match.Success)
                    {
                        string pinId = match.Groups[1].Value;
                        oembedUrl = $"https://www.pinterest.com/oembed.json?url=https%3A%2F%2Fwww.pinterest.com%2Fpin%2F{pinId}%2F";
                    }
                    else
                    {
                        oembedUrl = $"https://www.pinterest.com/oembed.json?url={Uri.EscapeDataString(url)}";
                    }

                    using var req = new HttpRequestMessage(HttpMethod.Get, oembedUrl);
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    var resp = await client.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("thumbnail_url", out var thumbEl))
                        {
                            string? thumbUrl = thumbEl.GetString();
                            if (!string.IsNullOrEmpty(thumbUrl))
                            {
                                return System.Text.RegularExpressions.Regex.Replace(thumbUrl, @"\/(236x|474x|564x|170x)\/", "/736x/");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Pinterest oembed error: {ex.Message}");
                }

                // 3. Fallback: scrape og:image from HTML page
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    var resp = await client.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                    {
                        string html = await resp.Content.ReadAsStringAsync();
                        var ogMatch = System.Text.RegularExpressions.Regex.Match(html, @"<meta\s+property=[""']og:image[""']\s+content=[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (!ogMatch.Success)
                        {
                            ogMatch = System.Text.RegularExpressions.Regex.Match(html, @"content=[""']([^""']+)[""']\s+property=[""']og:image[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        }
                        if (ogMatch.Success)
                        {
                            string img = ogMatch.Groups[1].Value;
                            return System.Text.RegularExpressions.Regex.Replace(img, @"\/(236x|474x|564x|170x)\/", "/736x/");
                        }
                    }
                }
                catch { }
            }

            return url;
        }

        #endregion

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearCanvasItems();
            _currentFilePath = "";
            _projectName = "untitled";
            TxtProjectTitle.Text = "untitled";
            EmptyStateOverlay.Visibility = Visibility.Visible;

            // Reset camera to default
            CanvasMatrixTransform.Matrix = Matrix.Identity;
            TxtZoom.Text = "Zoom: 100%";

            ScheduleAutoSave();
            ShowToast("Created new board", ToastType.Info);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                SaveProjectAs();
            else
                SaveProject();
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e) => SaveProjectAs();

        private void SaveProject()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveProjectAs();
                return;
            }
            DoSave(_currentFilePath);
        }

        private void SaveProjectAs()
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "DropBoard Project (*.dropboard)|*.dropboard|All Files (*.*)|*.*",
                FileName = string.IsNullOrEmpty(_projectName) || _projectName == "untitled" ? "MyBoard.dropboard" : $"{_projectName}.dropboard"
            };

            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                _projectName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                TxtProjectTitle.Text = _projectName;
                DoSave(_currentFilePath);
            }
        }

        private void DoSave(string filePath)
        {
            try
            {
                PerformAutoSave(isClosing: false);
                ShowToast($"Project saved: \"{_projectName}\"", ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to save: {ex.Message}", ToastType.Error);
            }
        }

        private void PerformAutoSave(bool isClosing = false)
        {
            if (_isRestoringSession || _isImportingBatch) return;

            try
            {
                Directory.CreateDirectory(AppDataDir);

                var cardModels = _cards.Select(c => new
                {
                    id = c.Id,
                    groupId = c.GroupId,
                    x = c.X,
                    y = c.Y,
                    width = c.Width,
                    height = c.Height,
                    localPath = c.LocalPath,
                    imageData = c.IsNote || c.IsDrawCard || (!string.IsNullOrEmpty(c.LocalPath) && File.Exists(c.LocalPath))
                        ? ""
                        : (string.IsNullOrEmpty(c.Base64Data) ? (c.Base64Data = BitmapToBase64(c.OriginalBitmap ?? c.Bitmap)) : c.Base64Data),
                    isYouTube = c.IsYouTube,
                    youtubeId = c.YouTubeId,
                    youtubeUrl = c.YouTubeUrl,
                    lastPlaybackSeconds = c.LastPlaybackSeconds,
                    isLocalVideo = c.IsLocalVideo,
                    videoFilePath = c.VideoFilePath,
                    isVideoLooping = c.IsVideoLooping,
                    isVideoMuted = c.IsVideoMuted,
                    isNote = c.IsNote,
                    noteText = c.NoteText,
                    noteFontFamily = c.NoteFontFamily,
                    noteFontSize = c.NoteFontSize,
                    noteTextColor = c.NoteTextColor,
                    noteBgColor = c.NoteBgColor,
                    noteAlignment = c.NoteAlignment.ToString(),
                    noteHasShadow = c.NoteHasShadow,
                    hasDeadline = c.HasDeadline,
                    deadlineIso = c.DeadlineDateTime?.ToString("o"),
                    deadlineLabel = c.DeadlineLabel,
                    isChecklist = c.IsChecklist,
                    checklistJson = c.IsChecklist && c.ChecklistItems != null ? JsonSerializer.Serialize(c.ChecklistItems) : "",
                    noteDoodleInk = c.NoteDoodleInkBase64,
                    noteBgGifPath = c.NoteBgGifPath,
                    noteBgGifBase64 = c.NoteBgGifBase64,
                    isPaletteCard = c.IsPaletteCard,
                    paletteMood = c.PaletteMood.ToString(),
                    paletteColorCount = c.PaletteColorCount,
                    paletteRows = c.PaletteRows,
                    linkedSourceCardId = c.IsPaletteCard ? (c.LinkedSourceImageCard?.Id ?? "") : "",
                    palettePinsData = c.IsPaletteCard && c.ActivePalettePins != null && c.ActivePalettePins.Count > 0
                        ? JsonSerializer.Serialize(c.ActivePalettePins) : "",
                    isDrawCard = c.IsDrawCard,
                    drawInkData = c.DrawInkBase64,
                    drawPenColor = c.DrawPenColor,
                    drawPenSize = c.DrawPenSize,
                    crop = new
                    {
                        top = c.CropTop,
                        right = c.CropRight,
                        bottom = c.CropBottom,
                        left = c.CropLeft
                    }
                }).ToList();

                var matrix = CanvasMatrixTransform.Matrix;
                string projName = string.IsNullOrEmpty(_currentFilePath) ? _projectName : System.IO.Path.GetFileNameWithoutExtension(_currentFilePath);

                var project = new
                {
                    version = 3,
                    app = "DropBoard Native Studio",
                    name = projName,
                    currentFilePath = _currentFilePath,
                    arrangeGap = _currentGap,
                    panX = matrix.OffsetX,
                    panY = matrix.OffsetY,
                    zoom = matrix.M11,
                    updatedAt = DateTime.UtcNow.ToString("o"),
                    cards = cardModels,
                    groups = _groups.Select(g => new
                    {
                        id = g.Id,
                        title = g.Title,
                        color = g.Color,
                        notes = g.Notes,
                        x = g.X,
                        y = g.Y,
                        width = g.Width,
                        height = g.Height
                    }).ToList()
                };

                string json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });

                // 1. Always write to local session (even if untitled!)
                File.WriteAllText(SessionFilePath, json);

                // 2. If user saved to an explicit .dropboard file, keep it synchronized!
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    string? dir = System.IO.Path.GetDirectoryName(_currentFilePath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        File.WriteAllText(_currentFilePath, json);
                    }
                }

                // 3. Keep recent.json updated
                var recentData = new
                {
                    lastProjectFilePath = _currentFilePath,
                    projectName = projName,
                    hasSession = _cards.Count > 0,
                    lastSavedUtc = DateTime.UtcNow.ToString("o")
                };
                File.WriteAllText(RecentConfigPath, JsonSerializer.Serialize(recentData, new JsonSerializerOptions { WriteIndented = true }));

                if (!isClosing)
                {
                    string displayTitle = string.IsNullOrEmpty(_currentFilePath) ? "untitled" : System.IO.Path.GetFileNameWithoutExtension(_currentFilePath);
                    TxtProjectTitle.Text = displayTitle;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AutoSave error: " + ex.Message);
            }
        }

        private string BitmapToBase64(BitmapSource bitmap)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using MemoryStream ms = new MemoryStream();
            encoder.Save(ms);
            byte[] bytes = ms.ToArray();
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }

        private void BtnOpenDropboard_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "DropBoard Project (*.dropboard)|*.dropboard|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadDropboardFile(dlg.FileName);
            }
        }

        private void InitializeSession(string? initialFilePath)
        {
            _ = InitializeSessionAsync(initialFilePath);
        }

        private async Task InitializeSessionAsync(string? initialFilePath)
        {
            if (!string.IsNullOrEmpty(initialFilePath) && File.Exists(initialFilePath))
            {
                await LoadDropboardFileAsync(initialFilePath, isSessionRestore: false);
                return;
            }

            // Check if there was a recent project
            string? recentProj = null;
            if (File.Exists(RecentConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(RecentConfigPath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("lastProjectFilePath", out JsonElement lpfEl))
                    {
                        recentProj = lpfEl.GetString();
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(recentProj) && File.Exists(recentProj))
            {
                await LoadDropboardFileAsync(recentProj, isSessionRestore: false);
            }
            else if (File.Exists(SessionFilePath))
            {
                await LoadDropboardFileAsync(SessionFilePath, isSessionRestore: true);
            }
        }

        private class ImageCardDescriptor
        {
            public string Id { get; set; } = "";
            public string GroupId { get; set; } = "";
            public string LocalPath { get; set; } = "";
            public string Base64Data { get; set; } = "";
            public BitmapSource? Bitmap { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double? Width { get; set; }
            public double? Height { get; set; }
            public double? CropL { get; set; }
            public double? CropT { get; set; }
            public double? CropR { get; set; }
            public double? CropB { get; set; }
            public bool IsYouTube { get; set; }
            public string YouTubeId { get; set; } = "";
            public string YouTubeUrl { get; set; } = "";
            public double LastPlaybackSeconds { get; set; } = 0;
            public bool IsLocalVideo { get; set; } = false;
            public string VideoFilePath { get; set; } = "";
            public bool IsVideoLooping { get; set; } = true;
            public bool IsVideoMuted { get; set; } = true;
        }

        private void LoadDropboardFile(string filePath, bool isSessionRestore = false)
        {
            _ = LoadDropboardFileAsync(filePath, isSessionRestore);
        }

        private async Task LoadDropboardFileAsync(string filePath, bool isSessionRestore = false)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                _isRestoringSession = true;

                // Clear current canvas items
                ClearCanvasItems();

                // Restore camera zoom & pan immediately for instant response
                bool hasSavedCamera = false;
                if (root.TryGetProperty("zoom", out JsonElement zEl) &&
                    root.TryGetProperty("panX", out JsonElement pxEl) &&
                    root.TryGetProperty("panY", out JsonElement pyEl))
                {
                    double zoom = zEl.GetDouble();
                    double px = pxEl.GetDouble();
                    double py = pyEl.GetDouble();
                    if (zoom >= 0.05 && zoom <= 25.0)
                    {
                        Matrix m = new Matrix(zoom, 0, 0, zoom, px, py);
                        CanvasMatrixTransform.Matrix = m;
                        int zoomPercent = (int)Math.Round(zoom * 100);
                        TxtZoom.Text = $"Zoom: {zoomPercent}%";
                        hasSavedCamera = true;
                    }
                }

                if (root.TryGetProperty("arrangeGap", out JsonElement gapEl))
                {
                    _currentGap = gapEl.GetDouble();
                    TxtGap.Text = FormatGapText(_currentGap);
                }

                List<ImageCardDescriptor> imageCardsToLoad = new List<ImageCardDescriptor>();

                if (root.TryGetProperty("cards", out JsonElement cards) && cards.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement card in cards.EnumerateArray())
                    {
                        bool isNote = card.TryGetProperty("isNote", out JsonElement inEl) && inEl.GetBoolean();
                        if (isNote)
                        {
                            string noteText = card.TryGetProperty("noteText", out JsonElement ntEl) ? (ntEl.GetString() ?? "") : "";
                            string noteFont = card.TryGetProperty("noteFontFamily", out JsonElement nfEl) ? (nfEl.GetString() ?? "Segoe UI") : "Segoe UI";
                            double noteFontSize = card.TryGetProperty("noteFontSize", out JsonElement nfsEl) ? nfsEl.GetDouble() : 16.0;
                            string noteTextColor = card.TryGetProperty("noteTextColor", out JsonElement ntcEl) ? (ntcEl.GetString() ?? "#FFFFFF") : "#FFFFFF";
                            string noteBgColor = card.TryGetProperty("noteBgColor", out JsonElement nbcEl) ? (nbcEl.GetString() ?? "Transparent") : "Transparent";
                            bool noteHasShadow = card.TryGetProperty("noteHasShadow", out JsonElement nhsEl) && nhsEl.GetBoolean();
                            TextAlignment noteAlign = TextAlignment.Left;
                            if (card.TryGetProperty("noteAlignment", out JsonElement naEl))
                            {
                                Enum.TryParse(naEl.GetString(), out noteAlign);
                            }

                            bool hasDl = card.TryGetProperty("hasDeadline", out JsonElement hdlEl) && hdlEl.GetBoolean();
                            string dlLabel = card.TryGetProperty("deadlineLabel", out JsonElement dllEl) ? (dllEl.GetString() ?? "") : "";
                            DateTime? dlDate = null;
                            if (hasDl && card.TryGetProperty("deadlineIso", out JsonElement dliEl) && !string.IsNullOrEmpty(dliEl.GetString()))
                            {
                                if (DateTime.TryParse(dliEl.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDl))
                                    dlDate = parsedDl;
                            }

                            bool isChecklist = card.TryGetProperty("isChecklist", out JsonElement iclEl) && iclEl.GetBoolean();
                            List<NoteChecklistItem>? checklistItems = null;
                            if (isChecklist && card.TryGetProperty("checklistJson", out JsonElement cljEl) && !string.IsNullOrEmpty(cljEl.GetString()))
                            {
                                try { checklistItems = JsonSerializer.Deserialize<List<NoteChecklistItem>>(cljEl.GetString()!); } catch { }
                            }

                            string noteDoodleInk = card.TryGetProperty("noteDoodleInk", out JsonElement ndiEl) ? (ndiEl.GetString() ?? "") : "";
                            string? noteBgGifPath = card.TryGetProperty("noteBgGifPath", out JsonElement nbgpEl) ? nbgpEl.GetString() : null;
                            string? noteBgGifBase64 = card.TryGetProperty("noteBgGifBase64", out JsonElement nbgdEl) ? nbgdEl.GetString() : null;

                            double x = card.TryGetProperty("x", out JsonElement xEl) ? xEl.GetDouble() : 0;
                            double y = card.TryGetProperty("y", out JsonElement yEl) ? yEl.GetDouble() : 0;
                            double? w = card.TryGetProperty("width", out JsonElement wEl) ? wEl.GetDouble() : null;
                            double? h = card.TryGetProperty("height", out JsonElement hEl) ? hEl.GetDouble() : null;

                            var addedNote = AddNoteCard(
                                text: noteText,
                                worldPosition: new Point(x, y),
                                customWidth: w,
                                customHeight: h,
                                fontFamily: noteFont,
                                fontSize: noteFontSize,
                                textColor: noteTextColor,
                                bgColor: noteBgColor,
                                alignment: noteAlign,
                                hasShadow: noteHasShadow,
                                isChecklist: isChecklist,
                                checklistItems: checklistItems,
                                hasDeadline: hasDl,
                                deadlineDateTime: dlDate,
                                deadlineLabel: dlLabel,
                                noteDoodleInkBase64: noteDoodleInk,
                                noteBgGifPath: noteBgGifPath,
                                noteBgGifBase64: noteBgGifBase64,
                                autoSelect: false);
                            if (card.TryGetProperty("id", out JsonElement noteIdEl) && !string.IsNullOrEmpty(noteIdEl.GetString()))
                                addedNote.Id = noteIdEl.GetString()!;
                            if (card.TryGetProperty("groupId", out JsonElement noteGidEl) && !string.IsNullOrEmpty(noteGidEl.GetString()))
                                addedNote.GroupId = noteGidEl.GetString()!;
                            continue;
                        }

                        bool isPaletteCard = card.TryGetProperty("isPaletteCard", out JsonElement ipcEl) && ipcEl.GetBoolean();
                        if (isPaletteCard)
                        {
                            double x = card.TryGetProperty("x", out JsonElement xEl) ? xEl.GetDouble() : 0;
                            double y = card.TryGetProperty("y", out JsonElement yEl) ? yEl.GetDouble() : 0;
                            double? w = card.TryGetProperty("width", out JsonElement wEl) ? wEl.GetDouble() : null;
                            double? h = card.TryGetProperty("height", out JsonElement hEl) ? hEl.GetDouble() : null;
                            int count = card.TryGetProperty("paletteColorCount", out JsonElement ccEl) ? ccEl.GetInt32() : 5;
                            ColorMood mood = ColorMood.Colorful;
                            if (card.TryGetProperty("paletteMood", out JsonElement pmEl))
                            {
                                Enum.TryParse(pmEl.GetString(), out mood);
                            }
                            int rows = card.TryGetProperty("paletteRows", out JsonElement prEl) ? prEl.GetInt32() : 1;
                            List<PalettePin>? pins = null;
                            if (card.TryGetProperty("palettePinsData", out JsonElement ppdEl) && !string.IsNullOrEmpty(ppdEl.GetString()))
                            {
                                try { pins = JsonSerializer.Deserialize<List<PalettePin>>(ppdEl.GetString()!); } catch { }
                            }
                            var addedPal = AddPaletteCard(
                                sourceCard: null,
                                initialPins: pins,
                                worldPosition: new Point(x, y),
                                customWidth: w,
                                customHeight: h,
                                mood: mood,
                                colorCount: count,
                                rows: rows,
                                autoSelect: false);
                            if (card.TryGetProperty("id", out JsonElement palIdEl) && !string.IsNullOrEmpty(palIdEl.GetString()))
                                addedPal.Id = palIdEl.GetString()!;
                            if (card.TryGetProperty("groupId", out JsonElement palGidEl) && !string.IsNullOrEmpty(palGidEl.GetString()))
                                addedPal.GroupId = palGidEl.GetString()!;
                            if (card.TryGetProperty("linkedSourceCardId", out JsonElement lscEl) && !string.IsNullOrEmpty(lscEl.GetString()))
                                addedPal.PendingLinkedSourceCardId = lscEl.GetString()!;
                            continue;
                        }

                        bool isDraw = card.TryGetProperty("isDrawCard", out JsonElement idcEl) && idcEl.GetBoolean();
                        if (isDraw)
                        {
                            string inkData = card.TryGetProperty("drawInkData", out JsonElement didEl) ? (didEl.GetString() ?? "") : "";
                            string penColor = card.TryGetProperty("drawPenColor", out JsonElement dpcEl) ? (dpcEl.GetString() ?? "#38BDF8") : "#38BDF8";
                            double penSize = card.TryGetProperty("drawPenSize", out JsonElement dpsEl) ? dpsEl.GetDouble() : 3.0;

                            double x = card.TryGetProperty("x", out JsonElement xEl) ? xEl.GetDouble() : 0;
                            double y = card.TryGetProperty("y", out JsonElement yEl) ? yEl.GetDouble() : 0;
                            double? w = card.TryGetProperty("width", out JsonElement wEl) ? wEl.GetDouble() : null;
                            double? h = card.TryGetProperty("height", out JsonElement hEl) ? hEl.GetDouble() : null;

                            var addedDraw = AddDrawCard(
                                worldPosition: new Point(x, y),
                                customWidth: w,
                                customHeight: h,
                                initialInkBase64: inkData,
                                penColor: penColor,
                                penSize: penSize,
                                autoSelect: false);
                            if (card.TryGetProperty("id", out JsonElement drawIdEl) && !string.IsNullOrEmpty(drawIdEl.GetString()))
                                addedDraw.Id = drawIdEl.GetString()!;
                            if (card.TryGetProperty("groupId", out JsonElement drawGidEl) && !string.IsNullOrEmpty(drawGidEl.GetString()))
                                addedDraw.GroupId = drawGidEl.GetString()!;
                            continue;
                        }

                        string localPath = "";
                        if (card.TryGetProperty("localPath", out JsonElement lpEl))
                            localPath = lpEl.GetString() ?? "";

                        string b64 = "";
                        if (card.TryGetProperty("imageData", out JsonElement idEl))
                            b64 = idEl.GetString() ?? "";
                        else if (card.TryGetProperty("src", out JsonElement srcEl))
                            b64 = srcEl.GetString() ?? "";

                        double imgX = card.TryGetProperty("x", out JsonElement ixEl) ? ixEl.GetDouble() : 0;
                        double imgY = card.TryGetProperty("y", out JsonElement iyEl) ? iyEl.GetDouble() : 0;
                        double? imgW = card.TryGetProperty("width", out JsonElement iwEl) ? iwEl.GetDouble() : null;
                        double? imgH = card.TryGetProperty("height", out JsonElement ihEl) ? ihEl.GetDouble() : null;

                        double? cropL = null, cropT = null, cropR = null, cropB = null;
                        if (card.TryGetProperty("crop", out JsonElement cropEl))
                        {
                            if (cropEl.TryGetProperty("left", out JsonElement cl)) cropL = cl.GetDouble();
                            if (cropEl.TryGetProperty("top", out JsonElement ct)) cropT = ct.GetDouble();
                            if (cropEl.TryGetProperty("right", out JsonElement cr)) cropR = cr.GetDouble();
                            if (cropEl.TryGetProperty("bottom", out JsonElement cb)) cropB = cb.GetDouble();
                        }

                        bool isYt = false;
                        string ytId = "";
                        string ytUrl = "";
                        double lastSec = 0;
                        if (card.TryGetProperty("isYouTube", out JsonElement ytEl)) isYt = ytEl.GetBoolean();
                        if (card.TryGetProperty("youtubeId", out JsonElement ytidEl)) ytId = ytidEl.GetString() ?? "";
                        if (card.TryGetProperty("youtubeUrl", out JsonElement yturlEl)) ytUrl = yturlEl.GetString() ?? "";
                        if (card.TryGetProperty("lastPlaybackSeconds", out JsonElement lpsEl)) lastSec = lpsEl.GetDouble();

                        bool isLocalVid = false;
                        string vidPath = "";
                        bool isLooping = true;
                        bool isMuted = true;
                        if (card.TryGetProperty("isLocalVideo", out JsonElement lvEl)) isLocalVid = lvEl.GetBoolean();
                        if (card.TryGetProperty("videoFilePath", out JsonElement vpEl)) vidPath = vpEl.GetString() ?? "";
                        if (card.TryGetProperty("isVideoLooping", out JsonElement vlEl)) isLooping = vlEl.GetBoolean();
                        if (card.TryGetProperty("isVideoMuted", out JsonElement vmEl)) isMuted = vmEl.GetBoolean();

                        string cardId = card.TryGetProperty("id", out JsonElement imgIdEl) ? (imgIdEl.GetString() ?? "") : "";
                        string groupId = card.TryGetProperty("groupId", out JsonElement imgGidEl) ? (imgGidEl.GetString() ?? "") : "";

                        imageCardsToLoad.Add(new ImageCardDescriptor
                        {
                            Id = cardId,
                            GroupId = groupId,
                            LocalPath = localPath,
                            Base64Data = b64,
                            X = imgX,
                            Y = imgY,
                            Width = imgW,
                            Height = imgH,
                            CropL = cropL,
                            CropT = cropT,
                            CropR = cropR,
                            CropB = cropB,
                            IsYouTube = isYt,
                            YouTubeId = ytId,
                            YouTubeUrl = ytUrl,
                            LastPlaybackSeconds = lastSec,
                            IsLocalVideo = isLocalVid,
                            VideoFilePath = vidPath,
                            IsVideoLooping = isLooping,
                            IsVideoMuted = isMuted
                        });
                    }
                }

                // Scene groups can be restored immediately
                if (root.TryGetProperty("groups", out JsonElement groupsEl) && groupsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement gEl in groupsEl.EnumerateArray())
                    {
                        string gid = gEl.TryGetProperty("id", out var idEl) ? (idEl.GetString() ?? "") : "";
                        string gtitle = gEl.TryGetProperty("title", out var gtEl) ? (gtEl.GetString() ?? "Scene 01") : "Scene 01";
                        string gcolor = gEl.TryGetProperty("color", out var gcEl) ? (gcEl.GetString() ?? "#3B82F6") : "#3B82F6";
                        string gnotes = gEl.TryGetProperty("notes", out var gnEl) ? (gnEl.GetString() ?? "") : "";
                        double gx = gEl.TryGetProperty("x", out var gxEl) ? gxEl.GetDouble() : 0;
                        double gy = gEl.TryGetProperty("y", out var gyEl) ? gyEl.GetDouble() : 0;
                        double gw = gEl.TryGetProperty("width", out var gwEl) ? gwEl.GetDouble() : 460;
                        double gh = gEl.TryGetProperty("height", out var ghEl) ? ghEl.GetDouble() : 380;

                        AddSceneGroup(
                            title: gtitle,
                            notes: gnotes,
                            customX: gx,
                            customY: gy,
                            width: gw,
                            height: gh,
                            color: gcolor,
                            customId: !string.IsNullOrEmpty(gid) ? gid : null,
                            recordUndo: false);
                    }
                    UpdateGroupCounts();
                }

                // Multi-threaded background decode + True live pipelined streaming to Canvas
                if (imageCardsToLoad.Count > 0)
                {
                    StartupLoadingOverlay.Opacity = 1.0;
                    StartupLoadingOverlay.Visibility = Visibility.Visible;
                    StartupProgressBar.Value = 0;
                    int totalImages = imageCardsToLoad.Count;
                    TxtStartupLoading.Text = $"Loading references: 0 / {totalImages} (0%)";
                    await Task.Yield();

                    var channel = Channel.CreateUnbounded<ImageCardDescriptor>(new UnboundedChannelOptions
                    {
                        SingleWriter = false,
                        SingleReader = true
                    });

                    // Producer: Decode images across all CPU cores in parallel and stream immediately
                    var producerTask = Task.Run(() =>
                    {
                        try
                        {
                            Parallel.ForEach(imageCardsToLoad, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount) }, desc =>
                            {
                                BitmapSource? bmp = null;
                                if (desc.IsLocalVideo)
                                {
                                    try { bmp = GetVideoPosterBitmap(desc.VideoFilePath); }
                                    catch { bmp = null; }
                                }
                                else if (!string.IsNullOrEmpty(desc.LocalPath) && File.Exists(desc.LocalPath))
                                {
                                    try { bmp = LoadOptimizedBitmap(desc.LocalPath, maxDecodeWidth: 900); }
                                    catch { bmp = null; }
                                }

                                if (bmp == null && !string.IsNullOrEmpty(desc.Base64Data))
                                {
                                    try
                                    {
                                        int commaIndex = desc.Base64Data.IndexOf(",");
                                        string rawB64 = commaIndex >= 0 ? desc.Base64Data.Substring(commaIndex + 1) : desc.Base64Data;
                                        byte[] bytes = Convert.FromBase64String(rawB64);
                                        using MemoryStream ms = new MemoryStream(bytes);
                                        bmp = LoadOptimizedBitmapFromStream(ms, maxDecodeWidth: 900);
                                    }
                                    catch { bmp = null; }
                                }

                                desc.Bitmap = bmp;
                                channel.Writer.TryWrite(desc);
                            });
                        }
                        finally
                        {
                            channel.Writer.Complete();
                        }
                    });

                    // Consumer: As each image finishes decoding, immediately add to Canvas and update live progress!
                    int loadedImages = 0;
                    while (await channel.Reader.WaitToReadAsync())
                    {
                        while (channel.Reader.TryRead(out var desc))
                        {
                            if (desc.Bitmap != null)
                            {
                                var addedImg = AddImageCard(
                                    desc.Bitmap,
                                    new Point(desc.X, desc.Y),
                                    customWidth: desc.Width,
                                    customHeight: desc.Height,
                                    localPath: desc.LocalPath,
                                    base64Data: (!string.IsNullOrEmpty(desc.LocalPath) && File.Exists(desc.LocalPath)) ? "" : desc.Base64Data,
                                    autoSelect: false,
                                    cropLeft: desc.CropL,
                                    cropTop: desc.CropT,
                                    cropRight: desc.CropR,
                                    cropBottom: desc.CropB,
                                    isYouTube: desc.IsYouTube,
                                    youTubeId: desc.YouTubeId,
                                    youTubeUrl: desc.YouTubeUrl,
                                    isLocalVideo: desc.IsLocalVideo,
                                    videoFilePath: desc.VideoFilePath,
                                    isVideoLooping: desc.IsVideoLooping,
                                    isVideoMuted: desc.IsVideoMuted,
                                    recordUndo: false);

                                if (!string.IsNullOrEmpty(desc.Id))
                                    addedImg.Id = desc.Id;
                                if (!string.IsNullOrEmpty(desc.GroupId))
                                    addedImg.GroupId = desc.GroupId;
                                if (desc.LastPlaybackSeconds > 0)
                                    addedImg.LastPlaybackSeconds = desc.LastPlaybackSeconds;
                            }

                            loadedImages++;
                            double pct = (double)loadedImages / totalImages * 100.0;
                            StartupProgressBar.Value = pct;
                            TxtStartupLoading.Text = $"Loading references: {loadedImages} / {totalImages} ({(int)pct}%)";

                            // Yield every 2 images or on final item so WPF dispatcher updates the progress bar and canvas smoothly
                            if (loadedImages % 2 == 0 || loadedImages == totalImages)
                            {
                                await Task.Yield();
                            }
                        }
                    }

                    await producerTask;

                    // Brief visual finish state (180ms) so user can see it reached 100%
                    StartupProgressBar.Value = 100;
                    TxtStartupLoading.Text = $"Loaded {totalImages} references (100%)";
                    await Task.Delay(180);

                    // Smoothly fade out the startup loading pill
                    DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(250));
                    fadeOut.Completed += (s, e) =>
                    {
                        StartupLoadingOverlay.Visibility = Visibility.Collapsed;
                        StartupLoadingOverlay.Opacity = 1.0;
                    };
                    StartupLoadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }

                // Re-link palette cards to their source image cards after loading
                foreach (var card in _cards)
                {
                    if (card.IsPaletteCard && !string.IsNullOrEmpty(card.PendingLinkedSourceCardId))
                    {
                        var src = _cards.FirstOrDefault(c => c.Id == card.PendingLinkedSourceCardId);
                        if (src != null)
                        {
                            card.LinkedSourceImageCard = src;
                            src.LinkedPaletteCard = card;
                            if (src.ActivePalettePins == null || src.ActivePalettePins.Count == 0)
                            {
                                src.ActivePalettePins = card.ActivePalettePins;
                            }
                        }
                    }
                }
                var singleImage = _cards.FirstOrDefault(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null);
                if (singleImage != null && _cards.Count(c => !c.IsPaletteCard && !c.IsNote && c.Bitmap != null) == 1)
                {
                    foreach (var pal in _cards.Where(c => c.IsPaletteCard && c.LinkedSourceImageCard == null))
                    {
                        pal.LinkedSourceImageCard = singleImage;
                        singleImage.LinkedPaletteCard = pal;
                        if (singleImage.ActivePalettePins == null || singleImage.ActivePalettePins.Count == 0)
                        {
                            singleImage.ActivePalettePins = pal.ActivePalettePins;
                        }
                    }
                }

                // Verify whether cards are actually visible in the current camera viewport
                bool cardsInView = false;
                if (hasSavedCamera && _cards.Count > 0)
                {
                    Matrix currentMat = CanvasMatrixTransform.Matrix;
                    double viewW = CanvasContainer.ActualWidth > 0 ? CanvasContainer.ActualWidth : ActualWidth;
                    double viewH = CanvasContainer.ActualHeight > 0 ? CanvasContainer.ActualHeight : ActualHeight;
                    if (viewW <= 0) viewW = 1200;
                    if (viewH <= 0) viewH = 800;

                    foreach (var c in _cards)
                    {
                        Point p = currentMat.Transform(new Point(c.X, c.Y));
                        double sw = c.Width * currentMat.M11;
                        double sh = c.Height * currentMat.M22;
                        Rect screenRect = new Rect(p.X, p.Y, Math.Max(10, sw), Math.Max(10, sh));
                        Rect viewRect = new Rect(0, 0, viewW, viewH);
                        if (screenRect.IntersectsWith(viewRect))
                        {
                            cardsInView = true;
                            break;
                        }
                    }
                }

                // If no camera was stored (like legacy .dropboard files) or camera points to empty space, Auto-Fit all cards!
                if (_cards.Count > 0 && (!hasSavedCamera || !cardsInView))
                {
                    _ = Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ZoomToFitAllCards(animated: false);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }

                if (isSessionRestore)
                {
                    if (root.TryGetProperty("currentFilePath", out JsonElement cfpEl))
                    {
                        string recentPath = cfpEl.GetString() ?? "";
                        if (!string.IsNullOrEmpty(recentPath) && File.Exists(recentPath))
                        {
                            _currentFilePath = recentPath;
                            _projectName = System.IO.Path.GetFileNameWithoutExtension(recentPath);
                            TxtProjectTitle.Text = _projectName;
                        }
                        else
                        {
                            _currentFilePath = "";
                            _projectName = root.TryGetProperty("name", out JsonElement nEl) ? (nEl.GetString() ?? "untitled") : "untitled";
                            TxtProjectTitle.Text = _projectName;
                        }
                    }
                    else
                    {
                        _currentFilePath = "";
                        _projectName = "untitled";
                        TxtProjectTitle.Text = "untitled";
                    }
                }
                else
                {
                    _currentFilePath = filePath;
                    _projectName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    TxtProjectTitle.Text = _projectName;
                }

                UpdateStatusCounts();
                UpdateViewportCulling();
                EmptyStateOverlay.Visibility = _cards.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                GC.Collect(2, GCCollectionMode.Forced, false);
                ScheduleWorkingSetTrim(2500);
                if (_cards.Count > 0)
                {
                    ShowToast($"Restored: {_projectName}", ToastType.Success);
                }
            }
            catch (Exception ex)
            {
                if (!isSessionRestore)
                {
                    ShowToast($"Failed to load file: {ex.Message}", ToastType.Error);
                }
            }
            finally
            {
                _isRestoringSession = false;
                if (StartupLoadingOverlay.Visibility != Visibility.Collapsed)
                {
                    StartupLoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region Window Drag & Controls

        private void TitleDragArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Deselect cards and groups when clicking on top bar (super convenient especially in 0% transparent BG mode)
            DeselectAllCards();
            _selectedGroups.Clear();

            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                BtnMaximize_Click(sender, e);
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void FloatingDock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DeselectAllCards();
            _selectedGroups.Clear();
        }

        private void UpdateTitlebarPinVisuals()
        {
            if (PinDot == null || PinHead == null) return;
            bool isPinned = Topmost;
            var strokeBrush = isPinned ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
            PinDot.Fill = isPinned ? strokeBrush : new SolidColorBrush(Color.FromRgb(85, 85, 85));
            if (PinLine1 != null) PinLine1.Stroke = strokeBrush;
            if (PinLine4 != null) PinLine4.Stroke = strokeBrush;
            PinHead.Stroke = strokeBrush;
            PinHead.Fill = isPinned ? new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)) : Brushes.Transparent;
            if (BtnPin != null)
            {
                BtnPin.ToolTip = isPinned ? "Window Pinned (Always on Top). Click to Unpin." : "Pin Window Always on Top";
            }
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            UpdateTitlebarPinVisuals();
            _settings.IsPinned = Topmost;
            _settings.Save();
            ShowToast(Topmost ? "Window Pinned (Always on Top)" : "Window Unpinned (Normal)", ToastType.Info);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Native Win32 Free-Form Border Resizing (All 4 Edges & 4 Corners)

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(WndProc);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_COPYDATA = 0x004A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int SC_SIZE = 0xF000;

        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                {
                    if (WindowState == WindowState.Maximized)
                    {
                        break;
                    }

                    short x = unchecked((short)(long)lParam);
                    short y = unchecked((short)((long)lParam >> 16));

                    if (GetWindowRect(hwnd, out RECT rc))
                    {
                        const int border = 8;
                        bool left = x >= rc.Left && x < rc.Left + border;
                        bool right = x <= rc.Right && x > rc.Right - border;
                        bool top = y >= rc.Top && y < rc.Top + border;
                        bool bottom = y <= rc.Bottom && y > rc.Bottom - border;

                        if (top && left) { handled = true; return (IntPtr)HTTOPLEFT; }
                        if (top && right) { handled = true; return (IntPtr)HTTOPRIGHT; }
                        if (bottom && left) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
                        if (bottom && right) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
                        if (left) { handled = true; return (IntPtr)HTLEFT; }
                        if (right) { handled = true; return (IntPtr)HTRIGHT; }
                        if (top) { handled = true; return (IntPtr)HTTOP; }
                        if (bottom) { handled = true; return (IntPtr)HTBOTTOM; }
                    }
                    break;
                }

                case WM_NCLBUTTONDOWN:
                {
                    int hit = wParam.ToInt32();
                    if (hit >= HTLEFT && hit <= HTBOTTOMRIGHT)
                    {
                        int direction = 0;
                        switch (hit)
                        {
                            case HTLEFT: direction = 1; break;
                            case HTRIGHT: direction = 2; break;
                            case HTTOP: direction = 3; break;
                            case HTTOPLEFT: direction = 4; break;
                            case HTTOPRIGHT: direction = 5; break;
                            case HTBOTTOM: direction = 6; break;
                            case HTBOTTOMLEFT: direction = 7; break;
                            case HTBOTTOMRIGHT: direction = 8; break;
                        }
                        if (direction != 0)
                        {
                            SendMessage(hwnd, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + direction), lParam);
                            handled = true;
                            return IntPtr.Zero;
                        }
                    }
                    break;
                }

                case WM_COPYDATA:
                {
                    try
                    {
                        var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                        if (cds.dwData.ToInt64() == 1001 && cds.lpData != IntPtr.Zero)
                        {
                            string? filePath = Marshal.PtrToStringUni(cds.lpData);
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                Dispatcher.InvokeAsync(() =>
                                {
                                    if (filePath.EndsWith(".dropboard", StringComparison.OrdinalIgnoreCase))
                                    {
                                        LoadDropboardFile(filePath, isSessionRestore: false);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            BitmapImage bmp = LoadOptimizedBitmap(filePath, maxDecodeWidth: 1400);
                                            AddImageCard(bmp, localPath: filePath);
                                        }
                                        catch { }
                                    }
                                    if (WindowState == WindowState.Minimized)
                                    {
                                        WindowState = WindowState.Normal;
                                    }
                                    Activate();
                                });
                                handled = true;
                                return (IntPtr)1;
                            }
                        }
                    }
                    catch { }
                    break;
                }

                case WM_MOUSEHWHEEL:
                {
                    short wheelDelta = unchecked((short)((long)wParam >> 16));
                    Dispatcher.InvokeAsync(() =>
                    {
                        double panSens = Math.Clamp(_settings.PanSensitivity > 0 ? _settings.PanSensitivity : 1.0, 0.2, 3.0);
                        double panDelta = (wheelDelta / 3.0) * panSens;
                        if (_settings.InvertPan) panDelta = -panDelta;

                        Matrix matrix = CanvasMatrixTransform.Matrix;
                        matrix.Translate(-panDelta, 0);
                        CanvasMatrixTransform.Matrix = matrix;
                        SyncActiveHwndPositions(updateSize: false);
                        ScheduleAutoSave();
                    });
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }

        #endregion
    }
}