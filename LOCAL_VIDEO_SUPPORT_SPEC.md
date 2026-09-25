# 🎬 Rencana & Spesifikasi Dukungan Video Lokal (Native C# WPF)

Dokumen ini berisi spesifikasi teknis, arsitektur, dan rencana implementasi pemutar video lokal di **DropBoard Native**. Sesuai prinsip DropBoard, implementasi ini **100% Native C# / WPF / DirectX** tanpa menggunakan engine browser (Chromium / WebView2).

---

## 🎯 Filosofi & Tujuan
1. **0% Chromium / Browser Bloat**: Tidak menggunakan WebView2 atau engine browser untuk video lokal. Menghilangkan overhead proses background `msedgewebview2.exe` dan konsumsi RAM yang tinggi.
2. **GPU Hardware Acceleration**: Memanfaatkan DirectX Media Engine dan Windows Media Foundation (WMF) bawaan Windows untuk decoding video secara instan dan efisien di GPU.
3. **Zero Airspace Issue**: Video menyatu penuh ke dalam Visual Tree WPF, sehingga dapat di-zoom, pan, rotasi, crop, dan ditumpuk antar-layer secara mulus tanpa *floating window glitch*.
4. **RAM Ramah & Ramping (Streaming dari Disk)**: Video tidak di-load seluruhnya ke RAM, melainkan di-stream langsung dari disk saat diputar.
5. **Aman & Nyaman**: Default **Muted (Suara Senyap)** saat pertama kali di-import agar tidak membuat kaget ketika membuka banyak referensi sekaligus.

---

## 📁 Format File yang Didukung
File picker dan drag-and-drop akan menerima ekstensi video berikut:
* **Standar Modern:** `.mp4`, `.mov`, `.webm`
* **Container Matroska:** `.mkv` (video H.264/AV1/VP9 dengan audio AAC/MP3/Opus)
* **Legacy & Windows Media:** `.avi`, `.wmv`
* **Format HP Lawas:** `.3gp` (menggunakan decoder Windows Media Foundation)

> **Catatan Kompatibilitas Codec:**
> Format `.3gp` dan file `.mkv` dengan audio khusus (seperti DTS/AC3) bergantung pada ketersediaan decoder DirectShow / WMF di sistem operasi Windows pengguna. Jika sistem memiliki decoder tersebut, video akan langsung berputar lancar.

---

## 🏗️ Arsitektur Pemutar (Native WPF)

### 1. Komponen Pemutar
* Menggunakan **`System.Windows.Controls.MediaElement`** yang dikonfigurasi dengan:
  * `LoadedBehavior = MediaState.Manual` (kontrol penuh melalui kode: Play, Pause, Stop, Seek).
  * `UnloadedBehavior = MediaState.Close` (otomatis melepas lock file di disk saat kartu dihapus).
  * `ScrubbingEnabled = true` (memungkinkan frame ter-update secara visual saat timeline digeser meski dalam keadaan paused).

### 2. State & Data Model (`CardItem`)
Penambahan field pendukung pada struct/class `CardItem`:
```csharp
// Local Video Support
public bool IsLocalVideo { get; set; } = false;
public string VideoFilePath { get; set; } = "";
public bool IsVideoPlaying { get; set; } = false;
public bool IsVideoLooping { get; set; } = true;    // Default ON (standar art/anim ref)
public bool IsVideoMuted { get; set; } = true;      // Default Muted agar nyaman
public double VideoDurationSeconds { get; set; } = 0;
public double VideoCurrentSeconds { get; set; } = 0;
public Border? VideoTagBadge { get; set; } = null;
public Slider? VideoTimelineSlider { get; set; } = null;
```

---

## 🎛️ Fitur & Kontrol Pengguna (UI/UX)

### 1. Tampilan Kartu di Kanvas
* **Badge Format:** Badge modern transparan di pojok kiri atas kartu bertuliskan format (`MP4`, `MKV`, `3GP`, dsb.) dengan warna aksen cyan/ungu.
* **Tombol Play Tengah:** Tombol Play bulat semi-transparan yang muncul di tengah kartu saat video dalam keadaan pause / hover.
* **Thumbnail / Poster Frame:** Saat kartu pertama kali di-drop, player mengambil poster frame awal sehingga kartu tetap tampil rapi meskipun belum di-play.

### 2. Hover Action Toolbar
Saat kursor diarahkan ke kartu video, muncul pill toolbar dengan opsi:
1. `▶ Play` / `⏸ Pause`: Memutar atau menjeda video.
2. `🔁 Loop [On/Off]`: Mengatur pengulangan video otomatis saat durasi habis (sangat cocok untuk referensi walk-cycle / animasi).
3. `🔇 / 🔊 Mute`: Toggle mute suara video.
4. `📸 AE`: Snapshot clean frame pada detik aktif langsung dikirim ke komposisi aktif Adobe After Effects.
5. `📋 Board`: Snapshot clean frame menjadi kartu gambar baru di kanvas DropBoard.
6. `⏱ Scrubber Bar`: Bar timeline di bagian bawah kartu untuk menggeser ke detik/frame tertentu secara presisi.

---

## 🛡️ Checklist Keamanan & Stabilitas Sistem

| Aspek | Strategi Keamanan |
| :--- | :--- |
| **Pembersihan Memori (No Memory Leak)** | Ketika kartu dihapus (<kbd>Del</kbd>) atau board direset, panggil `Stop()`, `Source = null`, dan lepaskan event handler. |
| **File Lock Prevention** | File video di disk tidak boleh terkunci (*file in use*) saat aplikasi tidak memutar video. |
| **Anti-Lag / Culling** | Jika kartu video berada di luar batas pandang layar (off-screen viewport saat zoom-out jauh), pemutaran dapat di-pause otomatis untuk menghemat siklus GPU. |
| **Handling Corrupt Video** | Dilengkapi blok `try-catch` dan event `MediaFailed`. Jika file video korup atau codec tidak ditemukan, kartu akan menampilkan fallback badge *"Unsupported Codec"* tanpa menyebabkan aplikasi crash. |

---

## ✅ Status Implementasi: SELESAI DITERAPKAN (100% Native)

Seluruh fitur telah berhasil diimplementasikan dan dikompilasi secara sukses ke dalam **[DropBoard.Native](file:///f:/Native%20Win/DropBoard/DropBoard.Native/MainWindow.xaml.cs)**:
1. **Filter Input:** Drag-and-drop, clipboard paste (<kbd>Ctrl</kbd>+<kbd>V</kbd>), dan open file dialog kini mengenali format `.mp4`, `.mov`, `.mkv`, `.webm`, `.avi`, `.wmv`, `.3gp`, dan `.m4v`.
2. **High-Res Poster Extraction:** Menggunakan Windows Shell COM (`IShellItemImageFactory`) untuk mengambil thumbnail resolusi tinggi instan dari shell Windows Explorer, dengan fallback poster visual dinamis.
3. **Player Architecture:** Menggunakan WPF `MediaElement` native (`Manual` loaded behavior + `Close` unloaded behavior) yang dirender langsung via DirectX tanpa memicu airspace glitch.
4. **Interactive Controls:**
   - Badge format video (`▶ MP4`, `▶ MKV`, `▶ 3GP`, dll.) dengan tombol interaktif Play/Pause.
   - Tombol Play tengah kartu dengan drop shadow.
   - Pill Toolbar hover lengkap: `Play/Pause`, `🔁 Loop Toggle`, `🔇 Mute/Unmute`, `📸 AE`, `📋 Board`, `Copy`, dan `✕ Delete`.
   - **Glassmorphism Scrubber Bar**: Timeline slider halus dengan label durasi (`0:00 / 1:25`) yang otomatis muncul saat hover atau diputar.
5. **Snapshot Frame:** Mengambil frame aktif langsung menjadi kartu gambar baru di kanvas atau diekspor ke Adobe After Effects active composition via JSX bridge.
6. **Project Persistence:** Status video lokal, looping, dan mute tersimpan ke file `.dropboard` dan sesi canvas.

