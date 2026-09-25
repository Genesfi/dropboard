# DropBoard Native Studio 🎨
> **Next-Generation GPU-Accelerated Reference Board & Creative Production Canvas for Windows**

DropBoard is a high-performance, GPU-accelerated reference board and creative production tool designed for digital artists, animators, video editors, and storyboard creators. Built with **100% Native C# .NET 9.0 (WPF & DirectX GPU acceleration)**, DropBoard combines the featherweight speed and zero-disk bloat of PureRef with advanced storyboarding tools, real-time color extraction, freehand vector inking, and direct creative pipeline bridges for Adobe Photoshop and After Effects.

---

## ✨ Key Features

### 🖼️ Pure Memory Architecture (PureRef-Style Portability)
- **Zero Disk Cache Bloat**: `.dropboard` project files embed images directly in portable Base64 format. Projects open and render **100% in RAM** without extracting temporary cache files to disk.
- **Ultra-Fast Batch Import**: Optimized for importing 100+ high-resolution reference photos simultaneously with memory-capped decoding (900px decode cache) to keep RAM footprint minimal (~200–500MB).
- **Single-File Portability**: Share your `.dropboard` file between workstations seamlessly without broken file paths or missing link errors.

### 🎨 Freehand Canvas Brush Tool (Panel-less Vector Inking)
- **Infinite Canvas Inking**: Draw directly anywhere across the canvas, in between cards, or directly on top of reference images with zero box panels or dark backgrounds.
- **Stylus Pressure & Vector Curves**: Fully supports graphic pen tablets (Wacom, Huion, etc.) with native Windows Ink pressure sensitivity and smooth antialiased Bézier curves.
- **Transformable Sketch Objects**: Once finished (<kbd>V</kbd> / <kbd>Esc</kbd>), strokes automatically convert into a borderless, transparent foreground object that can be moved anywhere and scaled proportionally with 4-corner handles.
- **Point Eraser & Color Studio**: Erase by point (Photoshop / Clip Studio Paint style), switch brush sizes (`2px`, `4px`, `8px`, `16px`), pick palette colors, and copy transparent PNGs directly to the clipboard.

### 🎬 Scene & Storyboard Grouping
- **Smart Scene Frames**: Group reference photos and notes into dedicated scenes (e.g., *Scene 01*, *Scene 02*) with auto-fitting bounds and custom color-coded frames.
- **Batch Move & Transform**: Drag or resize the group frame to move and transform all contained cards collectively.
- **Auto-Arrange Engines**: Automatically sequence scene groups into widescreen landscape grids or horizontal storyboard stage pipelines.

### 🎭 Real-Time Color Palette Studio
- **Automatic Palette Extraction**: Instantly analyzes reference images to extract color palettes based on mood (Colorful, Vibrant, Muted, Pastel, Moody, Monochrome).
- **Interactive Pin Sampler**: Drag color pins freely over the image to preview and sample colors live in real time.
- **Standalone Live Palette Cards**: Pin color swatches to the canvas, customize grid layouts, and copy hex codes or export palette graphics with one click.

### 📝 Sticky Note & Typography Cards
- **Rich Typography**: Add notes with customizable system fonts, font sizes, text alignments, and contrast shadows.
- **Transparent & Color Presets**: Keep notes transparent or apply sleek glassmorphism background colors.
- **Rapid Navigation**: Use keyboard shortcut <kbd>N</kbd> to spawn sticky notes instantly at the viewport center.

### 🎥 YouTube & Video Reference Integration
- **Embedded Web Player**: Paste YouTube links (<kbd>Ctrl</kbd> + <kbd>V</kbd>) or drag video URLs directly onto the canvas to watch reference clips and animations side-by-side with your artwork.
- **Lockstep HWND Tracking**: Hardware-accelerated video rendering synchronized seamlessly with canvas pan and zoom.

### ✂️ Non-Destructive Crop & Masking
- **Interactive 8-Point Crop Handles**: Crop and isolate specific details or composition frames without altering the original source image data.
- **Full Reversibility**: Reset or readjust crop boundaries at any time.

### 🚀 Creative Software Integration
- **Adobe After Effects**: Export selected scene groups or reference cards directly into your active After Effects project as organized composition layers via automated JSX scripting.
- **Adobe Photoshop**: Instant 1-click hand-off to open reference images directly in Photoshop.
- **System Explorer & Clipboard**: Copy image files directly to the Windows clipboard or reveal them in File Explorer.

### 🌐 Browser Companion Extension
- **1-Click Web Capture**: Integrated local HTTP bridge (port 28888) captures reference images from **Pinterest**, ArtStation, YouTube, and web browsers straight onto the canvas.

---

## ⌨️ Keyboard Shortcuts & Controls

| Shortcut | Action |
| :--- | :--- |
| **Touchpad 2-Finger Swipe** | Pan Canvas in 2D (Smooth 60/120fps) |
| **Touchpad 2-Finger Pinch** | Smooth Pinch Zoom In / Out at Cursor |
| <kbd>Space</kbd> + Drag / Middle Click | Pan Canvas anywhere (even over notes and text) |
| <kbd>Mouse Wheel</kbd> / <kbd>Ctrl</kbd> + Wheel | Zoom In / Out at Cursor |
| <kbd>B</kbd> / <kbd>P</kbd> | Toggle Freehand Canvas Brush Mode |
| <kbd>V</kbd> / <kbd>Esc</kbd> | Finish Brush & Transform Selection |
| <kbd>N</kbd> | Add Sticky Note / Text Card |
| <kbd>Ctrl</kbd> + <kbd>V</kbd> | Paste Image, URL, or Text from Clipboard |
| <kbd>Ctrl</kbd> + <kbd>C</kbd> | Copy Selected Image / Sketch (Transparent PNG) |
| <kbd>Ctrl</kbd> + <kbd>D</kbd> | Duplicate Selected Cards |
| <kbd>Ctrl</kbd> + <kbd>A</kbd> | Select All Items on Canvas |
| <kbd>Delete</kbd> / <kbd>Backspace</kbd> | Delete Selection |
| <kbd>Ctrl</kbd> + <kbd>Z</kbd> | Undo |
| <kbd>Ctrl</kbd> + <kbd>Y</kbd> / <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> | Redo |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save Project File (`.dropboard`) |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open Project File |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Create New Board |
| <kbd>F</kbd> / <kbd>Home</kbd> | Zoom to Fit All Items in View |
| <kbd>Double Click</kbd> Card | Focus & Zoom to Card |

---

## 🛠️ Building & Running from Source

### Prerequisites
- **Windows 10 / 11 (x64)**
- **.NET 9.0 SDK** (or newer)
- **Visual Studio 2022** (v17.12+ with *.NET Desktop Development* workload) or **Visual Studio Code** with C# Dev Kit
- **Microsoft Edge WebView2 Runtime** (Pre-installed on modern Windows 10/11)

### Quick Run
If you have compiled binaries, simply launch:
```bat
run_dropboard.bat
```

### Build Instructions (.NET CLI)
```powershell
# 1. Clone the repository
git clone https://github.com/Genesfi/dropboard.git
cd dropboard

# 2. Build the Native C# project in Release configuration
dotnet build -c Release "DropBoard.Native/DropBoard.Native.csproj"

# 3. Launch DropBoard Native Studio
start "" "DropBoard.Native/bin/Release/net9.0-windows/DropBoard.Native.exe"
```

### 📦 Building the Windows Installer
To compile the standalone setup installer (`dist/DropBoard-Setup-v1.0.0.exe`):
1. Ensure **[Inno Setup 6](https://jrsoftware.org/isdl.php)** is installed.
2. Run the 1-click installer compiler:
```bat
build_installer.bat
```
*(Or execute via PowerShell: `.\scripts\build_installer.ps1`)*.
The compiled, self-contained installer will be generated in `dist/`.

---

## 📁 Project Structure

```
DropBoard/
├── DropBoard.Native/          # Native C# .NET 9.0 Desktop Solution
│   ├── MainWindow.xaml        # Infinite Canvas UI, Floating Dock & HUD
│   ├── MainWindow.xaml.cs     # DirectX Canvas Engine, Brush System & Input Handlers
│   ├── App.xaml / App.xaml.cs # WPF Application entry & lifecycle management
│   ├── PaletteStudioWindow.*  # Real-Time Color Palette Sampler Studio
│   ├── Services/              # Native OS Services (LocalHttpServer, AppSettings, etc.)
│   └── DropBoard.Native.csproj# .NET 9.0 WPF Project Definition
├── extension/                 # Browser Companion Extension (Manifest V3)
├── installer/                 # Inno Setup 6 Script & Installer Configurations
├── scripts/                   # Automation & Installer Build Scripts
├── build_installer.bat        # 1-Click Windows Installer Compiler
├── run_dropboard.bat          # 1-Click Launch Script for DropBoard Studio
├── .gitignore                 # Git ignore rules
└── README.md                  # Project Documentation
```


---

## 📄 License
DropBoard Studio is licensed under the MIT License. See [LICENSE](LICENSE) for details.
