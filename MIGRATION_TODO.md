# 📋 Checklist & Rincian Fitur Belum Di-Migrasi (Web JS ➔ Native C# WPF)

Dokumen ini diperbarui berdasarkan audit mendalam terhadap kode sumber asli **Web JS (`assets/app.js`, `index.html`)** dibandingkan dengan implementasi **Native C# (.NET 9 / WPF - `DropBoard.Native`)**.

---

## 🔥 KATEGORI 1: FITUR UTAMA & WORKFLOW HARIAN (PROGRESS MIGRASI)

### 1. 🌐 Import URL (Pinterest, YouTube, Direct Web Image) & Clipboard Text
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - **Clipboard Text (<kbd>Ctrl</kbd>+<kbd>V</kbd>)**: Otomatis mendeteksi string URL `http://`, `https://`, dan `data:image/` dari clipboard lalu mengunduhnya langsung ke RAM.
  - **Tombol URL di Dock**: Memunculkan dialog modern gelap (*Add Web Reference*) yang secara otomatis memuat URL dari clipboard bila tersedia.
  - **Pinterest High-Res Resolver**: Otomatis mentransformasi thumbnail resolusi rendah (`/236x/`, `/474x/`, `/564x/`) menjadi resolusi tinggi (`/736x/`).
  - **YouTube Link Parser & Fallback**: Ekstraksi ID video dari link reguler, `youtu.be`, dan `shorts`. Mengunduh thumbnail `maxresdefault.jpg` dengan fallback otomatis ke `hqdefault.jpg`.
  - **Pure Memory Architecture**: Menyimpan gambar dalam bentuk Base64 di memory (`base64Data`) serta cache lokal.

---

### 2. ↔️ Pengaturan Arrange Gap Termasuk 0px ("No Gap")
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - Rotasi preset gap kini mendukung: `0px (No Gap) ➔ 8px ➔ 16px ➔ 24px ➔ 32px ➔ 48px ➔ 0px`.
  - Teks tombol menampilkan `↔️ Gap: No Gap` ketika bernilai 0px, dan `↔️ Gap: {X}px` untuk nilai lainnya.
  - Gambar dapat dirapatkan 100% tanpa celah sama sekali (PureRef standard).

---

### 3. 📌 Perbaikan Toolbar Pin / Float & Bug Double Icon
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - **Double Icon Teratasi**: Karakter emoji `📌` dihapus dari kode C#, hanya menampilkan ikon pushpin vektor SVG tunggal dari XAML dengan label teks `Pin` atau `Float`.
  - **Pemisahan Logika Window vs Toolbar**:
    - Tombol `BtnPin` di Titlebar khusus mengatur **Window Always-on-Top** (`Topmost`).
    - Tombol `BtnDockPin` di Floating Dock khusus mengatur **Toolbar Auto-Hide (Float) vs Toolbar Pinned**.
  - **Animasi Auto-Hide 100% Bersih**: Ketika dalam mode `Float`, toolbar meluncur keluar (slide-out 75px) dan menghilang total (**Opacity 0.0**, zero leftover opacity). Mendekatkan kursor ke area tepi atas/kiri atau toolbar langsung memunculkannya kembali secara halus (slide-in & fade-in 1.0).
  - **Mode 1x Left Sidebar (Single Column Icon-Only)**: Ditambahkan tombol **`Sidebar`** (`BtnDockPosToggle`). Mengubah orientasi dock menjadi vertikal di sebelah kiri (`1x mode`), menyembunyikan teks label, dan menampilkan ikon/badge secara presisi dalam ukuran ringkas 34x34 px. Mengklik kembali tombol mengubahnya menjadi **`Top Bar`** horizontal.

---

### 4. ✂️ Non-Destructive Image Crop & Masking (8-Point Handles)
* **Status**: ✅ **SELESAI DI-MIGRASI (1:1 Web JS & README)**
  - **8-Point Interactive Handles**:
    - **4 Draggable Corner Handles** (`NW`, `NE`, `SW`, `SE`): Kotak amber `#FBBF24` 10x10px dengan border `#0F1117` dan kursor diagonal (`SizeNWSE`/`SizeNESW`) yang interaktif mengubah lebar dan tinggi crop secara bebas.
    - **4 Draggable Edge Bars** (`Top`, `Bottom`, `Left`, `Right`): Bar border hit-test dengan kursor kardinal (`SizeNS`/`SizeWE`) dan highlight hover halus untuk mengatur sisi atas/bawah/kiri/kanan.
  - **Center Panning / Move Box**: Bagian tengah kotak putus-putus kuning (`StrokeDashArray 4 2`, `Cursor = SizeAll`) dapat di-drag untuk menggeser jendela crop ke area lain gambar referensi secara mulus.
  - **Live Visual Dim Masks**: 4 masker gelap (`rgba(8, 10, 15, 0.72)`) di luar area crop yang ter-update secara realtime sewaktu handle atau kotak digeser.
  - **Modern Floating Controls Pill**:
    - `✓ Done (Enter)`: Menerapkan crop presisi dengan `CroppedBitmap` native WPF dan menyesuaikan ukuran kartu (fit to crop).
    - `↺ Reset (Esc)`: Mengembalikan kartu ke gambar asli penuh secara non-destruktif (*100% loss-free*).
    - `✕ Cancel`: Menutup mode crop dan mengembalikan kondisi tampilan sebelumnya tanpa perubahan.
  - **Keyboard Shortcuts**: Mendukung <kbd>Enter</kbd> untuk konfirmasi crop dan <kbd>Escape</kbd> untuk reset/batal.
  - **Non-Destructive Core**: Setiap kartu menyimpan `OriginalBitmap`, `BaseWidth`, `BaseHeight`, `BaseX`, `BaseY`, serta persentase crop. Saat disimpan ke sesi/proyek `.dropboard`, data gambar asli utuh tetap dipertahankan non-destruktif bersama metadata `crop`.

---

### 5. 🖱️ Context Menu Modern: Ikon, Subtitle, & Crop (1:1 Web JS)
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - **Header Kategori**: Menampilkan header redup uppercase `"REFERENCE"` di bagian atas menu.
  - **Badge Aplikasi Asli**: Badge ungu `[Ae]` untuk *Send to After Effects* dan badge biru `[Ps]` untuk *Open in Photoshop*.
  - **Ikon Vektor Lengkap**: Seluruh item memiliki ikon vektor SVG presisi (Copy, Reveal in Explorer, Crop, Bring to Front, Reset Scale, Zoom, Delete).
  - **Dua Baris (Title + Subtitle)**: Setiap aksi dilengkapi teks deskripsi kecil di bawah judul (*"Auto-import footage to comp"*, *"Show cached source file"*, dll).
  - **Menu Aksi Crop**: Opsi *"Crop / Mask Reference"* telah disematkan di posisi yang tepat.
  - **Styling Dark Modern**: ContextMenu menggunakan background `#161922`, border `#2E384D`, rounded corner radius 10, drop shadow elegan, dan item highlight `#252F42`.

---

### 6. 🟣 Deep Adobe After Effects Pipeline Export (1:1 C++ Legacy & ExtendScript JSX)
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - **Auto-Detection Berlapis Cepat & Presisi**:
    - Mendeteksi proses aktif (`AfterFX.exe` via `Process.GetProcessesByName`).
    - Registry `App Paths` (`SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\AfterFX.exe`).
    - Jalur standar versi Adobe CC (2026, 2025, 2024, 2023, 2022, 2021, 2020, CC 2019, 2019, CC 2018, Beta).
    - Wildcard directory lookup tanpa rekursi lambat.
    - Mengutamakan eksekutor CLI `AfterFX.com` jika tersedia di samping `AfterFX.exe`.
  - **Engine ExtendScript JSX Standar DropBoard**:
    - Mode `loose_photos`: Mengimpor file ke folder khusus `_References`, memasukkan footage ke komposisi aktif (atau membuat comp `Reference Footage` 1080p 30fps bila belum ada).
    - Menghitung rasio & posisi layout otomatis: jika 1 gambar, diskalakan 75% di tengah; jika banyak gambar, menjaga posisi kanvas relatif dan menyesuaikan skala (fit 82%).
    - Mode `group_comp`: Membuat komposisi `REF_<CompName>`, guide layer Tipografi & Font Spec, guide layer Scene Notes/Checklist, serta membuka komposisi langsung di viewer.
    - Script disimpan dengan format UTF-8 BOM (`0xEF, 0xBB, 0xBF`) di folder temporary dan dieksekusi melalui argumen `-r "<scriptPath>"`.
  - **Window Maximization & Foreground Watchdog**:
    - Mendeteksi handle window utama After Effects (`AE_Console_Win` atau enumerasi title `Adobe After Effects`).
    - Memeriksa apakah jendela AE sedang maximized (`IsZoomed`). Jika ya, watchdog task memastikan status maximize tetap dipertahankan pasca eksekusi CLI.
    - Membawa AE ke layar utama (*Foreground*).
  - **Cropped Reference Handling**:
    - Bila kartu dalam kondisi dicrop, sistem otomatis menghasilkan cache PNG gambar hasil crop dan mengirimkan gambar crop tersebut ke After Effects.
  - **Multi-Selection & Single Card Export**:
    - Mendukung ekspor dari tombol dock `[Ae] Export`, tombol hover bar `Ae Import`, dan Context Menu `Send to After Effects`.
    - Jika beberapa kartu dipilih (multi-selection), seluruh kartu terpilih diekspor bersamaan dalam satu grup undo After Effects.


---

### 7. 📺 YouTube Interactive Card & "Snap Video Frame" (1:1 Web JS & After Effects Integration)
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - **Auto-Detection URL & Thumbnail**:
    - Mendeteksi link `youtube.com/watch?v=...`, `youtu.be/...`, dan `youtube.com/shorts/...` secara otomatis saat di-paste (<kbd>Ctrl</kbd>+<kbd>V</kbd>), di-drag & drop dari browser, atau dimasukkan lewat dialog *Add Web Reference*.
    - Mengunduh thumbnail resolusi tinggi (`maxresdefault.jpg` dengan fallback ke `hqdefault.jpg`) sebagai poster awal.
  - **Badge YouTube Merah**:
    - Kartu referensi YouTube otomatis dilengkapi badge merah elegan `[▶ YouTube]` di sudut kiri bawah kartu.
  - **In-Card Video Playback (WebView2 Integration)**:
    - Dilengkapi kontrol hover toolbar: tombol **`▶ Play`** / **`⏹ Stop`** merah/amber.
    - **Double Click**: Mengklik ganda kartu YouTube langsung mengaktifkan player interaktif di dalam kartu (in-line player).
    - Menghubungkan engine WebView2 secara lazily ke embed YouTube resmi (`youtube-nocookie.com/embed/{id}?autoplay=1&enablejsapi=1`).
    - Mendukung tombol **`↗ Browser`** untuk membuka video langsung di browser default.
  - **Fitur "Snap to Ae" (Capture Video Frame ke After Effects)**:
    - Tombol khusus **`📸 Snap to Ae`** pada hover toolbar kartu dan Context Menu.
    - Mengambil frame video secara presisi langsung dari engine Chromium compositor via `CapturePreviewAsync` (atau fallback visual image).
    - Menyimpan frame sebagai PNG di direktori Cache.
    - **Menaruh kartu gambar hasil snapshot baru** di kanvas tepat di sebelah kanan kartu video YouTube, lengkap dengan riwayat Undo/Redo.
    - **Langsung mengekspor frame snapshot tersebut ke Adobe After Effects** (masuk ke folder `_References` dan ditempatkan di tengah komposisi aktif).
  - **Persistensi Sesi & Undo/Redo**:
    - Metadata `isYouTube`, `youtubeId`, dan `youtubeUrl` tersimpan secara utuh di file `.dropboard` maupun snapshot riwayat Undo/Redo.

---

### 8. 🎞️ Dukungan Penuh Animasi GIF (Animated GIF Loop)
* **Status**: ✅ **SELESAI DI-MIGRASI**
  - **Engine XamlAnimatedGif**: Merender animasi GIF berputar (*loop*) secara lancar pada kartu kanvas menggunakan `XamlAnimatedGif`.
  - **Clipboard Smart Detection (Browser Copy)**: Mendeteksi tag HTML `<img src="...">` dari browser saat user melakukan "Copy Image", mengunduh file `.gif` asli utuh alih-alih mengambil single frame statis DIB Windows.
  - **File Explorer Paste & Drag Drop**: Mendukung paste (<kbd>Ctrl</kbd>+<kbd>V</kbd>) file `.gif` langsung dari File Explorer serta drag & drop file `.gif` ke kanvas.
  - **Web URL & Data URI**: Mendeteksi URL GIF dan data URI `data:image/gif;base64,...` secara otomatis.
  - **Non-Destructive Crop Recovery**: Mengembalikan animasi GIF otomatis ketika crop di-reset (`ResetCrop`).

---

## 🎬 KATEGORI 2: PRODUCTION NODES & STORYBOARDING (FASE 2)
Komponen ini di C# saat ini masih berupa placeholder `MessageBox.Show`:

### 8. 📝 Note / Lyric Node
- Mode ganda: *Freeform Text* atau *Interactive Checklist* (daftar centang to-do).
- Badge hashtag (`#scene1`, `#ref`, `#bg`).
- Pilihan warna aksen kartu.

### 9. 🔤 Typography / Font Node
- Scan seluruh font sistem Windows secara ringan (*lazy-loading*).
- Navigasi keyboard <kbd>↑</kbd> / <kbd>↓</kbd> untuk live audition / pratinjau font langsung.
- Efek font mengalir dinamis (*cascade*) ke node teks yang tersambung kabel.

### 10. ✨ Visual Effects (VFX) Node
- Toggle kontrol efek visual: Glow, Halation, Film Grain, Chromatic Aberration, Motion Blur, Lens Flare, Glitch, dan Color Grading.

### 11. ⏱️ Schedule / Deadline Node
- Badge hitung mundur (countdown) & indikator jatuh tempo (overdue).
- Preset cepat (`+3d`, `+1w`, `+2w`) dan pemilih kalender visual.

### 11. 🔌 Dynamic Cable Wiring (Bezier Connections)
- Port koneksi input & output di sisi node/kartu.
- Kabel kurva Bezier interaktif di canvas WPF yang otomatis mengikuti posisi node saat digeser.

### 12. 🔲 Scene Groups (Container Frames)
- Frame pembungkus grup adegan bergaris putus-putus dengan judul.
- Memindahkan frame grup otomatis memindahkan semua kartu dan node di dalamnya.

---

## ⚙️ KATEGORI 3: SETTINGS, HUD & TWEAKS LAINNYA

### 13. 📐 Algoritma Auto-Arrange Tingkat Lanjut
- **Widescreen Grid**: Adaptif 3-5 kolom landscape untuk rasio layar 16:9 agar tidak memanjang ke bawah.
- **Storyboard Pipeline**: Penataan horizontal per baris adegan dengan input node rapi di sisi kiri.
- **Zero-Collision Shelf**: Rak khusus di bagian atas untuk node mandiri yang belum terhubung.

### 14. 🛠️ Dialog Settings Lengkap
- Pemilihan Navigation Mode (macOS Touchpad vs Windows Mouse Wheel vs Middle Drag).
- Slider sensitivitas pan & zoom, toggle invert pan.
- Konfigurasi path aplikasi eksternal (Photoshop / AE / Blender).
- Tombol "Clear Image Cache" dan registrasi asosiasi ekstensi file `.dropboard`.

### 15. ⚡ Quick Palette / Command Search
- Quick search bar (<kbd>Ctrl</kbd>+<kbd>K</kbd> / Spacebar palette) untuk mencari referensi atau membuat node baru secara instan.

---

## ✅ RIWAYAT FIX TERBARU (Native C# WPF)
- **Fix UI Freeze & Top Bar Click**: Menghapus `DockRevealZoneTop` & `DockRevealZoneLeft` invisible overlay yang sebelumnya memblokir hit testing mouse di area atas dan titlebar. Digantikan dengan `Window_PreviewMouseMove` non-blocking cursor tracking.
- **Slider Opacity Background**: Template custom dark/cyan (`CanvasOpacitySliderStyle`) menggantikan kotak putih Aero default dengan pill thumb cyan modern `#38BDF8`.
- **Styling Badge STUDIO**: Diperbarui 1:1 dengan Web JS (`#263B82F6` bg, `#4D3B82F6` border, `#60A5FA` font bold 9px).
- **Application Icon**: Menghasilkan `app_icon.ico` multi-resolusi (16x16 s/d 256x256) dari `assets/app_icon.png` dan meregistrasikannya ke `.csproj` `<ApplicationIcon>` serta `<Window Icon="...">`.
- **Responsive Toolbar & Titlebar Auto-Collapse**: Mengimplementasikan auto-collapse saat resize window/panel (mengikuti layout Web JS pada gambar 3, 4, 5):
  - Width <= 1080px: Dock toolbar horizontal otomatis masuk mode icon-only (teks label disembunyikan, padding ringkas).
  - Width <= 950px: Tombol titlebar (New, Open, Save, Save As, Pin, About) berubah jadi icon-only.
  - Width <= 720px: Menyembunyikan counter ref, meta divider, dan tombol Save As.
  - Width <= 580px: Menyembunyikan badge Studio, project title, Pin, Opacity, dan New button.
  - Width <= 420px: Menyembunyikan teks brand "DropBoard" dan menyisakan icon vector & tombol esensial.
- **Bebas Resize Kanan, Kiri, Atas, Bawah & 4 Sudut (Free-Form Border Resizing)**: Mengimplementasikan native Win32 `WM_NCHITTEST` dan `WM_NCLBUTTONDOWN` (`SC_SIZE`) via `HwndSource` hook persis seperti versi C++ Web JS (`src/main.cpp`). Cursor otomatis berubah menjadi panah resize di semua tepian window dan sudut, serta mendukung double-click titlebar untuk maximize/restore.
- **Card Quick-Action Hover Toolbar**: Mini pill toolbar yang otomatis muncul saat mouse diarahkan ke gambar referensi (`[:: Move] [Ae Import] [✂ Crop] [Copy] [✕]`) dengan animasi fade in/out halus.
- **Fitur Crop / Mask Interaktif**: Menekan tombol `✂ Crop` membuka editor crop visual dengan 4 dim mask, active amber dashed box yang bisa digeser, serta tombol `Apply Crop` (menggunakan native `CroppedBitmap`) dan `Cancel`.
- **Selection-Aware Auto-Arrange (Grid & Pipeline)**: Tombol Grid dan Pipeline kini mendeteksi apakah ada gambar yang sedang diseleksi (`_selectedCards.Count > 0`). Jika ada gambar yang dipilih, hanya gambar tersebut yang ditata ulang (in-place di sekitar posisinya); jika tidak ada yang dipilih, seluruh gambar di board ditata ulang seperti biasa.
