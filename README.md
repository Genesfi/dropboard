# DropBoard STUDIO 🎨
> **Next-Generation Reference Board & Creative Production Canvas for Windows**

DropBoard is a high-performance, GPU-accelerated reference board and creative production tool designed for digital artists, animators, video editors, and storyboard creators. Built with modern C++ Win32 and Microsoft WebView2, DropBoard combines the featherweight speed of PureRef with advanced storyboarding nodes, dynamic wiring, and creative pipeline integrations.

---

## ✨ Key Features

### 🖼️ Pure Memory Architecture (PureRef-style Portability)
- **Zero Disk Cache Bloat**: `.dropboard` project files embed images directly in portable Base64 format. Projects open and render **100% in RAM** without extracting temporary files to disk.
- **On-Demand Disk Export**: Files are only written to disk when you explicitly invoke external applications (e.g., Photoshop or After Effects).
- **Single-File Portability**: Share `.dropboard` files between computers seamlessly without broken image links.

### 🎬 Production & Storyboard Nodes
- **Note / Lyric Nodes**:
  - Dual modes: Freeform Text or Interactive Checklist.
  - Custom hashtag badges and accent color palettes.
- **Typography / Font Nodes**:
  - Live typography styling that cascades directly into connected notes in real time.
  - **Progressive Lazy-Loading**: Instantly opens dropdown with 0ms lag even with 1,500+ installed system fonts.
  - **Rapid Keyboard Navigation**: Use <kbd>↑</kbd> and <kbd>↓</kbd> Arrow keys to cycle and audition fonts live.
- **Visual Effects (VFX) Nodes**:
  - Interactive toggles for Glow, Halation, Grain, Chromatic Aberration, Motion Blur, Lens Flare, Glitch, and Color Grading.
- **Schedule / Deadline Nodes**:
  - Live overdue / countdown status badge.
  - One-click deadline presets (`+3d`, `+1w`, `+2w`) and visual calendar picker.
- **Dynamic Cable Wiring**:
  - Connect nodes to reference cards, scene groups, or chain nodes together with smart curved SVG bezier cables.

### 📐 Intelligent Auto-Arrange Engines
- **Widescreen Grid**: Adaptively arranges scene groups into 3 to 5 landscape columns, filling widescreen 16:9 monitors and eliminating vertical bottleneck towers.
- **Storyboard Pipeline**: Sequences scenes sequentially in horizontal stage rows with input nodes aligned cleanly on the left.
- **Zero-Collision Shelf**: Unconnected / standalone nodes are automatically organized in a dedicated top Overview shelf.

### ✂️ Non-Destructive Crop & Masking
- Crop and isolate specific details from reference images with 8-point interactive handles without modifying the original source image.

### 🚀 Creative Software Integration
- **Adobe After Effects**: Auto-import reference footage directly into your active After Effects composition.
- **Adobe Photoshop**: Instant hand-off to Photoshop for detailed editing.
- **System Explorer & Clipboard**: Copy image files directly to the Windows clipboard or reveal them in File Explorer.

### 🌐 Browser Companion Extension
- 1-click capture from **Pinterest**, ArtStation, YouTube, and web browsers straight onto the infinite canvas.

---

## ⌨️ Keyboard Shortcuts & Controls

| Shortcut | Action |
| :--- | :--- |
| <kbd>Space</kbd> + Drag / Middle Click | Pan Canvas |
| <kbd>Mouse Wheel</kbd> | Smooth Zoom In / Out |
| <kbd>Ctrl</kbd> + <kbd>V</kbd> | Paste Image or URL from Clipboard |
| <kbd>Ctrl</kbd> + <kbd>D</kbd> | Duplicate Selected Items (Preserves Cable Connections) |
| <kbd>Ctrl</kbd> + <kbd>A</kbd> | Select All (Cards, Groups, Nodes) |
| <kbd>Delete</kbd> / <kbd>Backspace</kbd> | Delete Selection |
| <kbd>Ctrl</kbd> + <kbd>Z</kbd> | Undo |
| <kbd>Ctrl</kbd> + <kbd>Y</kbd> / <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> | Redo |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save Project File (`.dropboard`) |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open Project File |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Create New Project |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>0</kbd> | Reset Window Opacity to 100% |
| <kbd>↑</kbd> / <kbd>↓</kbd> (in Font Node) | Rapidly Cycle & Preview Fonts Live |

---

## 🛠️ Building from Source

### Prerequisites
- **Windows 10 / 11 (x64)**
- **Visual Studio 2022** (Community, Professional, or Enterprise) with *Desktop development with C++*
- **CMake** (v3.20 or newer)
- **Microsoft Edge WebView2 Runtime** (Pre-installed on Windows 10/11)

### Build Instructions
```powershell
# 1. Clone the repository
git clone https://github.com/Genesfi/dropboard.git
cd dropboard

# 2. Generate Visual Studio project files using CMake
cmake -B build -S .

# 3. Compile DropBoard in Release configuration
cmake --build build --config Release
```

The compiled binary will be available at:
```
build/Release/DropBoard.exe
```

---

## 📁 Project Structure

```
DropBoard/
├── assets/                 # Frontend UI (HTML, CSS, JS)
│   ├── app.js             # Canvas engine, node management & arrange algorithms
│   ├── index.html         # Application layout & HUD elements
│   └── styles.css         # Modern dark-mode UI design system
├── src/                    # Native C++ Win32 Host
│   ├── main.cpp           # WinMain entry point & window message loop
│   ├── WebViewHost.h/.cpp # WebView2 bridge, message dispatcher & cache management
│   ├── ExternalAppIntegration.h/.cpp # Photoshop & After Effects IPC integrations
│   └── LocalHttpServer.h/.cpp       # Extension IPC server
├── extension/              # Browser companion extension (Manifest V3)
├── packages/               # Microsoft WebView2 native SDK
├── CMakeLists.txt          # CMake build configuration
└── .gitignore              # Git ignore rules
```

---

## 📄 License
DropBoard Studio is licensed under the MIT License. See [LICENSE](LICENSE) for details.
