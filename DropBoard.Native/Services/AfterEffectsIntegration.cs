using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DropBoard.Native
{
    public class AeExportItem
    {
        public string FilePath { get; set; } = "";
        public double RelX { get; set; } = 0.0;
        public double RelY { get; set; } = 0.0;
        public double Width { get; set; } = 0.0;
        public double Height { get; set; } = 0.0;
    }

    public class AeExportCompPayload
    {
        public string Mode { get; set; } = "loose_photos"; // "group_comp" or "loose_photos"
        public string CompName { get; set; } = "References";
        public double CompWidth { get; set; } = 1920.0;
        public double CompHeight { get; set; } = 1080.0;
        public string NotesText { get; set; } = "";
        public string FontText { get; set; } = "";
        public string FontFamily { get; set; } = "";
        public string SampleText { get; set; } = "";
        public string VfxText { get; set; } = "";
        public List<AeExportItem> Items { get; set; } = new();
    }

    public static class AfterEffectsIntegration
    {
        #region Win32 P/Invoke

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_MAXIMIZE = 3;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion

        #region Process & App Discovery

        public static IntPtr FindAfterEffectsWindow()
        {
            // 1. Direct check for standard After Effects main window class
            IntPtr hWnd = FindWindow("AE_Console_Win", null);
            if (hWnd != IntPtr.Zero && IsWindowVisible(hWnd)) return hWnd;

            // 2. Comprehensive enumeration across all visible top-level windows
            IntPtr found = IntPtr.Zero;
            EnumWindows((h, lParam) =>
            {
                if (!IsWindowVisible(h)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(h, sb, 256);
                if (sb.ToString().Contains("Adobe After Effects", StringComparison.OrdinalIgnoreCase))
                {
                    found = h;
                    return false; // Stop enumeration
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        public static string FindRunningProcessPath(string exeName)
        {
            string cleanName = Path.GetFileNameWithoutExtension(exeName);
            Process[] procs = Process.GetProcessesByName(cleanName);
            foreach (var p in procs)
            {
                try
                {
                    string? path = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        return path;
                    }
                }
                catch { }
            }
            return "";
        }

        public static string FindExecutableInRegistry(string appName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{appName}");
                string? path = key?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }
            catch { }
            return "";
        }

        public static string FindAfterEffectsExe()
        {
            // 1. If After Effects is already actively running, target that exact instance!
            string running = FindRunningProcessPath("AfterFX.exe");
            if (!string.IsNullOrEmpty(running) && File.Exists(running))
            {
                return running;
            }

            // 2. Check Windows Registry default App Paths
            string regPath = FindExecutableInRegistry("AfterFX.exe");
            if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
            {
                return regPath;
            }

            // 3. Comprehensive list of install locations (2026 down to CC 2018)
            string[] commonPaths = new[]
            {
                @"C:\Program Files\Adobe\Adobe After Effects 2026\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2025\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2024\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2023\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2022\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2021\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2020\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects CC 2019\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects 2019\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects CC 2018\Support Files\AfterFX.exe",
                @"C:\Program Files\Adobe\Adobe After Effects (Beta)\Support Files\AfterFX.exe"
            };

            foreach (var p in commonPaths)
            {
                if (File.Exists(p)) return p;
            }

            // 4. Wildcard scan in Adobe folder
            try
            {
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string adobeDir = Path.Combine(progFiles, "Adobe");
                if (Directory.Exists(adobeDir))
                {
                    foreach (var dir in Directory.GetDirectories(adobeDir, "Adobe After Effects*"))
                    {
                        string candidate = Path.Combine(dir, "Support Files", "AfterFX.exe");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            catch { }

            return "";
        }

        public static string FindAfterEffectsCmd()
        {
            string aeExe = FindAfterEffectsExe();
            if (string.IsNullOrEmpty(aeExe)) return "";

            // If AfterFX.com exists in the same Support Files directory, prefer it for CLI execution
            string aeCom = Path.ChangeExtension(aeExe, ".com");
            if (File.Exists(aeCom))
            {
                return aeCom;
            }
            return aeExe;
        }

        public static string FindPhotoshopExe()
        {
            // 1. Running process
            string running = FindRunningProcessPath("Photoshop.exe");
            if (!string.IsNullOrEmpty(running) && File.Exists(running))
            {
                return running;
            }

            // 2. Registry
            string regPath = FindExecutableInRegistry("Photoshop.exe");
            if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
            {
                return regPath;
            }

            // 3. Common paths
            string[] commonPaths = new[]
            {
                @"C:\Program Files\Adobe\Adobe Photoshop 2026\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2025\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2024\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2023\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2022\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2021\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2020\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop CC 2019\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop 2019\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop CC 2018\Photoshop.exe",
                @"C:\Program Files\Adobe\Adobe Photoshop (Beta)\Photoshop.exe"
            };

            foreach (var p in commonPaths)
            {
                if (File.Exists(p)) return p;
            }

            // 4. Wildcard scan
            try
            {
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string adobeDir = Path.Combine(progFiles, "Adobe");
                if (Directory.Exists(adobeDir))
                {
                    foreach (var dir in Directory.GetDirectories(adobeDir, "Adobe Photoshop*"))
                    {
                        string candidate = Path.Combine(dir, "Photoshop.exe");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            catch { }

            return "";
        }

        #endregion

        #region JSX Generator

        public static string EscapeForJsxString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '\\') sb.Append("\\\\");
                else if (c == '\"') sb.Append("\\\"");
                else if (c == '\'') sb.Append("\\\'");
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < 32 || c > 126)
                {
                    sb.AppendFormat("\\u{0:x4}", (int)c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string EscapeJsxFilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string p = path.Replace('\\', '/');
            return EscapeForJsxString(p);
        }

        public static string BuildExportJsx(AeExportCompPayload payload)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("// DropBoard Smart Export for Adobe After Effects");
            sb.AppendLine("(function() {");
            sb.AppendLine("    try {");
            sb.AppendLine("        if (!app.project) {");
            sb.AppendLine("            app.newProject();");
            sb.AppendLine("        }");
            sb.AppendLine("        app.beginUndoGroup(\"DropBoard Reference Import\");\n");

            sb.AppendLine("        function getOrCreateFolder(name) {");
            sb.AppendLine("            for (var i = 1; i <= app.project.items.length; i++) {");
            sb.AppendLine("                var item = app.project.items[i];");
            sb.AppendLine("                if ((item instanceof FolderItem) && item.name === name) {");
            sb.AppendLine("                    return item;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            return app.project.items.addFolder(name);");
            sb.AppendLine("        }\n");

            sb.AppendLine("        var refFolder = getOrCreateFolder(\"_References\");\n");

            if (payload.Mode == "group_comp")
            {
                string rawName = string.IsNullOrWhiteSpace(payload.CompName) ? "References" : payload.CompName;
                string compName = "REF_" + EscapeForJsxString(rawName);
                double compW = payload.CompWidth > 0 ? payload.CompWidth : 1920.0;
                double compH = payload.CompHeight > 0 ? payload.CompHeight : 1080.0;

                sb.AppendLine($"        var compW = {compW.ToString(inv)};");
                sb.AppendLine($"        var compH = {compH.ToString(inv)};");
                sb.AppendLine($"        var comp = app.project.items.addComp(\"{compName}\", compW, compH, 1.0, 10.0, 30.0);");
                sb.AppendLine("        comp.parentFolder = refFolder;\n");

                // 1. Dedicated Typography Text Layer with actual font applied
                bool hasTypo = !string.IsNullOrEmpty(payload.FontFamily) || !string.IsNullOrEmpty(payload.SampleText);
                if (hasTypo)
                {
                    string fontName = string.IsNullOrEmpty(payload.FontFamily) ? "Sans-Serif" : payload.FontFamily;
                    string sampleText = string.IsNullOrEmpty(payload.SampleText) ? fontName : payload.SampleText;

                    sb.AppendLine($"        var typoLayer = comp.layers.addText(\"{EscapeForJsxString(sampleText)}\");");
                    sb.AppendLine($"        typoLayer.name = \"TYPOGRAPHY: {EscapeForJsxString(fontName)} [Guide Layer]\";");
                    sb.AppendLine("        typoLayer.guideLayer = true;");
                    sb.AppendLine("        typoLayer.property(\"Position\").setValue([80, 130]);");
                    sb.AppendLine("        var typoProp = typoLayer.property(\"Source Text\");");
                    sb.AppendLine("        var typoDoc = typoProp.value;");
                    sb.AppendLine("        typoDoc.justification = ParagraphJustification.LEFT_JUSTIFY;");
                    sb.AppendLine("        typoDoc.fontSize = 38;");
                    sb.AppendLine("        typoDoc.fillColor = [0.98, 0.90, 0.42];");
                    sb.AppendLine($"        try {{ typoDoc.font = \"{EscapeForJsxString(fontName)}\"; }} catch(e) {{");
                    sb.AppendLine("            try {");
                    sb.AppendLine("                if (app.fonts && app.fonts.allFonts) {");
                    sb.AppendLine("                    for (var fi = 0; fi < app.fonts.allFonts.length; fi++) {");
                    sb.AppendLine("                        var fo = app.fonts.allFonts[fi];");
                    sb.AppendLine($"                        if (fo.familyName.toLowerCase() === \"{EscapeForJsxString(fontName)}\".toLowerCase() || fo.postScriptName.toLowerCase() === \"{EscapeForJsxString(fontName)}\".toLowerCase()) {{");
                    sb.AppendLine("                            typoDoc.font = fo.postScriptName;");
                    sb.AppendLine("                            break;");
                    sb.AppendLine("                        }");
                    sb.AppendLine("                    }");
                    sb.AppendLine("                }");
                    sb.AppendLine("            } catch(e2) {}");
                    sb.AppendLine("        }");
                    sb.AppendLine("        typoProp.setValue(typoDoc);\n");

                    sb.AppendLine($"        var specLayer = comp.layers.addText(\"FONT: {EscapeForJsxString(fontName)}\");");
                    sb.AppendLine("        specLayer.name = \"FONT SPEC [Guide Layer]\";");
                    sb.AppendLine("        specLayer.guideLayer = true;");
                    sb.AppendLine("        specLayer.property(\"Position\").setValue([80, 180]);");
                    sb.AppendLine("        var specProp = specLayer.property(\"Source Text\");");
                    sb.AppendLine("        var specDoc = specProp.value;");
                    sb.AppendLine("        specDoc.justification = ParagraphJustification.LEFT_JUSTIFY;");
                    sb.AppendLine("        specDoc.fontSize = 15;");
                    sb.AppendLine("        specDoc.fillColor = [0.60, 0.66, 0.76];");
                    sb.AppendLine("        specProp.setValue(specDoc);\n");
                }

                // 2. Dedicated Notes & Checklist Guide Layer
                bool hasNotes = !string.IsNullOrEmpty(payload.NotesText) || !string.IsNullOrEmpty(payload.VfxText);
                if (hasNotes)
                {
                    string fullNotes = "";
                    if (!string.IsNullOrEmpty(payload.NotesText)) fullNotes += payload.NotesText + "\n\n";
                    if (!string.IsNullOrEmpty(payload.VfxText)) fullNotes += "VFX SPECS:\n" + payload.VfxText;

                    double notesPosY = hasTypo ? 240.0 : 90.0;
                    sb.AppendLine($"        var notesLayer = comp.layers.addText(\"{EscapeForJsxString(fullNotes)}\");");
                    sb.AppendLine("        notesLayer.name = \"SCENE NOTES & CHECKLIST [Guide Layer]\";");
                    sb.AppendLine("        notesLayer.guideLayer = true;");
                    sb.AppendLine($"        notesLayer.property(\"Position\").setValue([80, {notesPosY.ToString(inv)}]);");
                    sb.AppendLine("        var nProp = notesLayer.property(\"Source Text\");");
                    sb.AppendLine("        var nDoc = nProp.value;");
                    sb.AppendLine("        nDoc.justification = ParagraphJustification.LEFT_JUSTIFY;");
                    sb.AppendLine("        nDoc.fontSize = 18;");
                    sb.AppendLine("        nDoc.fillColor = [0.90, 0.93, 0.98];");
                    sb.AppendLine("        nProp.setValue(nDoc);\n");
                }

                bool hasInfo = hasTypo || hasNotes;

                // Layout items
                if (payload.Items != null && payload.Items.Count > 0)
                {
                    double minX = 1e9, minY = 1e9, maxX = -1e9, maxY = -1e9;
                    foreach (var it in payload.Items)
                    {
                        if (it.RelX < minX) minX = it.RelX;
                        if (it.RelY < minY) minY = it.RelY;
                        if (it.RelX + it.Width > maxX) maxX = it.RelX + it.Width;
                        if (it.RelY + it.Height > maxY) maxY = it.RelY + it.Height;
                    }

                    double layoutW = (maxX > minX) ? (maxX - minX) : 100.0;
                    double layoutH = (maxY > minY) ? (maxY - minY) : 100.0;

                    double leftMargin = hasInfo ? 520.0 : 80.0;
                    double rightMargin = 60.0;
                    double topMargin = 70.0;
                    double bottomMargin = 70.0;

                    double availW = compW - leftMargin - rightMargin;
                    double availH = compH - topMargin - bottomMargin;
                    if (availW < 100.0) availW = 100.0;
                    if (availH < 100.0) availH = 100.0;

                    double fitScale = Math.Min(availW / layoutW, availH / layoutH);
                    if (fitScale <= 0.0) fitScale = 1.0;

                    double targetAreaCenterX = leftMargin + availW / 2.0;
                    double targetAreaCenterY = topMargin + availH / 2.0;
                    double layoutCenterX = minX + layoutW / 2.0;
                    double layoutCenterY = minY + layoutH / 2.0;

                    sb.AppendLine("        var itemsData = [");
                    for (int i = 0; i < payload.Items.Count; ++i)
                    {
                        var it = payload.Items[i];
                        double posX = targetAreaCenterX + (it.RelX + it.Width / 2.0 - layoutCenterX) * fitScale;
                        double posY = targetAreaCenterY + (it.RelY + it.Height / 2.0 - layoutCenterY) * fitScale;
                        double desiredW = it.Width * fitScale;

                        sb.AppendLine($"            {{ path: \"{EscapeJsxFilePath(it.FilePath)}\", x: {posX.ToString(inv)}, y: {posY.ToString(inv)}, targetW: {desiredW.ToString(inv)} }}{(i + 1 < payload.Items.Count ? "," : "")}");
                    }
                    sb.AppendLine("        ];\n");

                    sb.AppendLine("        for (var j = 0; j < itemsData.length; j++) {");
                    sb.AppendLine("            var it = itemsData[j];");
                    sb.AppendLine("            try {");
                    sb.AppendLine("                var f = new File(it.path);");
                    sb.AppendLine("                if (f.exists) {");
                    sb.AppendLine("                    var footage = null;");
                    sb.AppendLine("                    try {");
                    sb.AppendLine("                        var io = new ImportOptions();");
                    sb.AppendLine("                        io.file = f;");
                    sb.AppendLine("                        if (io.canImportAs(ImportAsType.FOOTAGE)) {");
                    sb.AppendLine("                            io.importAs = ImportAsType.FOOTAGE;");
                    sb.AppendLine("                        }");
                    sb.AppendLine("                        io.sequence = false;");
                    sb.AppendLine("                        io.forceAlphabetical = false;");
                    sb.AppendLine("                        footage = app.project.importFile(io);");
                    sb.AppendLine("                    } catch(eIo) {");
                    sb.AppendLine("                        try {");
                    sb.AppendLine("                            footage = app.project.importFile(new ImportOptions(f));");
                    sb.AppendLine("                        } catch(eDirect) {}");
                    sb.AppendLine("                    }");
                    sb.AppendLine("                    if (footage) {");
                    sb.AppendLine("                        footage.parentFolder = refFolder;");
                    sb.AppendLine("                        var layer = comp.layers.add(footage);");
                    sb.AppendLine("                        layer.guideLayer = true;");
                    sb.AppendLine("                        layer.property(\"Position\").setValue([it.x, it.y]);");
                    sb.AppendLine("                        if (footage.width > 0) {");
                    sb.AppendLine("                            var sc = (it.targetW / footage.width) * 100;");
                    sb.AppendLine("                            layer.property(\"Scale\").setValue([sc, sc]);");
                    sb.AppendLine("                        }");
                    sb.AppendLine("                    }");
                    sb.AppendLine("                }");
                    sb.AppendLine("            } catch(eItem) {}");
                    sb.AppendLine("        }");
                }

                sb.AppendLine("\n        comp.openInViewer();");
            }
            else
            {
                // Mode: loose_photos (Selected photos sent directly to active comp or auto-created comp)
                sb.AppendLine("        var activeComp = (app.project.activeItem && (app.project.activeItem instanceof CompItem)) ? app.project.activeItem : null;");
                sb.AppendLine("        if (!activeComp) {");
                sb.AppendLine("            for (var ci = 1; ci <= app.project.items.length; ci++) {");
                sb.AppendLine("                if (app.project.items[ci] instanceof CompItem) {");
                sb.AppendLine("                    activeComp = app.project.items[ci];");
                sb.AppendLine("                    break;");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("            if (!activeComp) {");
                sb.AppendLine("                activeComp = app.project.items.addComp(\"Reference Footage\", 1920, 1080, 1.0, 10.0, 30.0);");
                sb.AppendLine("                activeComp.parentFolder = refFolder;");
                sb.AppendLine("            }");
                sb.AppendLine("            activeComp.openInViewer();");
                sb.AppendLine("        }\n");

                sb.AppendLine("        var itemsData = [");
                for (int i = 0; i < payload.Items.Count; ++i)
                {
                    var it = payload.Items[i];
                    sb.AppendLine($"            {{ path: \"{EscapeJsxFilePath(it.FilePath)}\", relX: {it.RelX.ToString(inv)}, relY: {it.RelY.ToString(inv)}, width: {it.Width.ToString(inv)}, height: {it.Height.ToString(inv)} }}{(i + 1 < payload.Items.Count ? "," : "")}");
                }
                sb.AppendLine("        ];\n");

                sb.AppendLine("        var minX = 1e9, minY = 1e9, maxX = -1e9, maxY = -1e9;");
                sb.AppendLine("        for (var k = 0; k < itemsData.length; k++) {");
                sb.AppendLine("            var it = itemsData[k];");
                sb.AppendLine("            if (it.relX < minX) minX = it.relX;");
                sb.AppendLine("            if (it.relY < minY) minY = it.relY;");
                sb.AppendLine("            if (it.relX + it.width > maxX) maxX = it.relX + it.width;");
                sb.AppendLine("            if (it.relY + it.height > maxY) maxY = it.relY + it.height;");
                sb.AppendLine("        }");
                sb.AppendLine("        var layoutW = (maxX > minX) ? (maxX - minX) : 100;");
                sb.AppendLine("        var layoutH = (maxY > minY) ? (maxY - minY) : 100;");
                sb.AppendLine("        var layoutCenterX = minX + layoutW / 2;");
                sb.AppendLine("        var layoutCenterY = minY + layoutH / 2;\n");

                sb.AppendLine("        for (var k = 0; k < itemsData.length; k++) {");
                sb.AppendLine("            var it = itemsData[k];");
                sb.AppendLine("            try {");
                sb.AppendLine("                var f = new File(it.path);");
                sb.AppendLine("                if (f.exists) {");
                sb.AppendLine("                    var footage = null;");
                sb.AppendLine("                    try {");
                sb.AppendLine("                        var io = new ImportOptions();");
                sb.AppendLine("                        io.file = f;");
                sb.AppendLine("                        if (io.canImportAs(ImportAsType.FOOTAGE)) {");
                sb.AppendLine("                            io.importAs = ImportAsType.FOOTAGE;");
                sb.AppendLine("                        }");
                sb.AppendLine("                        io.sequence = false;");
                sb.AppendLine("                        io.forceAlphabetical = false;");
                sb.AppendLine("                        footage = app.project.importFile(io);");
                sb.AppendLine("                    } catch(eIo) {");
                sb.AppendLine("                        try {");
                sb.AppendLine("                            footage = app.project.importFile(new ImportOptions(f));");
                sb.AppendLine("                        } catch(eDirect) {}");
                sb.AppendLine("                    }");
                sb.AppendLine("                    if (footage) {");
                sb.AppendLine("                        footage.parentFolder = refFolder;");
                sb.AppendLine("                        if (activeComp) {");
                sb.AppendLine("                            var layer = activeComp.layers.add(footage);");
                sb.AppendLine("                            layer.guideLayer = true;");
                sb.AppendLine("                            layer.selected = true;");
                sb.AppendLine("                            if (itemsData.length === 1) {");
                sb.AppendLine("                                layer.property(\"Position\").setValue([activeComp.width / 2, activeComp.height / 2]);");
                sb.AppendLine("                                if (footage.width > 0 && footage.height > 0) {");
                sb.AppendLine("                                    var targetW = activeComp.width * 0.75;");
                sb.AppendLine("                                    var targetH = activeComp.height * 0.75;");
                sb.AppendLine("                                    var sc = Math.min(targetW / footage.width, targetH / footage.height) * 100;");
                sb.AppendLine("                                    layer.property(\"Scale\").setValue([sc, sc]);");
                sb.AppendLine("                                }");
                sb.AppendLine("                            } else {");
                sb.AppendLine("                                var maxAllowedW = activeComp.width * 0.82;");
                sb.AppendLine("                                var maxAllowedH = activeComp.height * 0.82;");
                sb.AppendLine("                                var fitScale = Math.min(maxAllowedW / layoutW, maxAllowedH / layoutH);");
                sb.AppendLine("                                if (fitScale <= 0) fitScale = 1.0;");
                sb.AppendLine("                                var posX = activeComp.width / 2 + (it.relX + it.width / 2 - layoutCenterX) * fitScale;");
                sb.AppendLine("                                var posY = activeComp.height / 2 + (it.relY + it.height / 2 - layoutCenterY) * fitScale;");
                sb.AppendLine("                                layer.property(\"Position\").setValue([posX, posY]);");
                sb.AppendLine("                                if (footage.width > 0) {");
                sb.AppendLine("                                    var targetW = it.width * fitScale;");
                sb.AppendLine("                                    var sc = (targetW / footage.width) * 100;");
                sb.AppendLine("                                    layer.property(\"Scale\").setValue([sc, sc]);");
                sb.AppendLine("                                }");
                sb.AppendLine("                            }");
                sb.AppendLine("                        }");
                sb.AppendLine("                    }");
                sb.AppendLine("                }");
                sb.AppendLine("            } catch(eItem) {}");
                sb.AppendLine("        }");
            }

            sb.AppendLine("\n        app.endUndoGroup();");
            sb.AppendLine("        \"SUCCESS\";");
            sb.AppendLine("    } catch(err) {");
            sb.AppendLine("        try { app.endUndoGroup(); } catch(e) {}");
            sb.AppendLine("        \"ERROR: \" + err.toString();");
            sb.AppendLine("    }");
            sb.AppendLine("})();");

            return sb.ToString();
        }

        #endregion

        #region Execution

        private static readonly object _exportLock = new();
        private static DateTime _lastExportTime = DateTime.MinValue;

        public static bool ExportToAfterEffects(AeExportCompPayload payload, out string outMessage)
        {
            lock (_exportLock)
            {
                if ((DateTime.UtcNow - _lastExportTime).TotalMilliseconds < 800)
                {
                    outMessage = "Export is processing...";
                    return true;
                }
                _lastExportTime = DateTime.UtcNow;

                string aeCmd = FindAfterEffectsCmd();
                if (string.IsNullOrEmpty(aeCmd))
                {
                    aeCmd = FindAfterEffectsExe();
                }

                if (string.IsNullOrEmpty(aeCmd))
                {
                    outMessage = "Adobe After Effects executable not detected on this system.";
                    return false;
                }

                string scriptContent = BuildExportJsx(payload);
                string tempScript = Path.Combine(Path.GetTempPath(), "dropboard_ae_advanced_export.jsx");

                try
                {
                    // Write file in UTF-8 with BOM (0xEF, 0xBB, 0xBF)
                    File.WriteAllText(tempScript, scriptContent, new UTF8Encoding(true));
                }
                catch (Exception ex)
                {
                    outMessage = $"Failed to create temporary ExtendScript file: {ex.Message}";
                    return false;
                }

                // Check if After Effects is already open and whether its window is currently maximized
                IntPtr hAE = FindAfterEffectsWindow();
                bool wasMaximized = (hAE != IntPtr.Zero && IsZoomed(hAE));

                bool launched = false;
                try
                {
                    // Use -ro (run once override) so AE never rejects second script execution with modal warning!
                    var psi = new ProcessStartInfo
                    {
                        FileName = aeCmd,
                        Arguments = $"-ro \"{tempScript}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using var proc = Process.Start(psi);
                    launched = proc != null;
                }
                catch
                {
                    // Fallback via cmd.exe /d /s /c
                    try
                    {
                        string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        string cmdExe = Path.Combine(sysDir, "cmd.exe");
                        string fullArgs = $"/d /s /c \"\"{aeCmd}\" -ro \"{tempScript}\"\"";

                        var psiFallback = new ProcessStartInfo
                        {
                            FileName = cmdExe,
                            Arguments = fullArgs,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        using var procFallback = Process.Start(psiFallback);
                        launched = procFallback != null;
                    }
                    catch
                    {
                        launched = false;
                    }
                }

            // Active Watchdog Task:
            // If AE was maximized, ensure it stays maximized if anything tries to restore it down
            if (hAE != IntPtr.Zero && wasMaximized)
            {
                Task.Run(() =>
                {
                    for (int i = 0; i < 50; ++i) // Check every 50ms for 2.5 seconds
                    {
                        Thread.Sleep(50);
                        if (!IsZoomed(hAE))
                        {
                            ShowWindow(hAE, SW_MAXIMIZE);
                            SetForegroundWindow(hAE);
                            Thread.Sleep(150);
                            if (!IsZoomed(hAE))
                            {
                                ShowWindow(hAE, SW_MAXIMIZE);
                            }
                            break;
                        }
                    }
                });
            }
            else if (hAE != IntPtr.Zero)
            {
                SetForegroundWindow(hAE);
            }

                if (launched)
                {
                    outMessage = (payload.Mode == "group_comp")
                        ? "Created Reference Comp in Adobe After Effects!"
                        : (payload.Items.Count > 1
                            ? $"Imported {payload.Items.Count} references into Adobe After Effects!"
                            : "Imported reference footage into Adobe After Effects!");
                    return true;
                }
                else
                {
                    outMessage = "Failed to launch After Effects process.";
                    return false;
                }
            }
        }

        #endregion
    }
}
