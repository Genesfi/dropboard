#include "ExternalAppIntegration.h"
#include <shlobj.h>
#include <tlhelp32.h>
#include <fstream>
#include <sstream>
#include <thread>
#include <chrono>

// Helper to check if a specific process is already running and retrieve its full executable path
static std::wstring FindRunningProcessPath(const std::wstring& exeName) {
    HANDLE hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnap == INVALID_HANDLE_VALUE) return L"";

    PROCESSENTRY32W pe = { sizeof(pe) };
    if (Process32FirstW(hSnap, &pe)) {
        do {
            if (_wcsicmp(pe.szExeFile, exeName.c_str()) == 0) {
                HANDLE hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pe.th32ProcessID);
                if (hProc) {
                    wchar_t fullPath[MAX_PATH] = { 0 };
                    DWORD size = MAX_PATH;
                    if (QueryFullProcessImageNameW(hProc, 0, fullPath, &size)) {
                        CloseHandle(hProc);
                        CloseHandle(hSnap);
                        return std::wstring(fullPath);
                    }
                    CloseHandle(hProc);
                }
            }
        } while (Process32NextW(hSnap, &pe));
    }
    CloseHandle(hSnap);
    return L"";
}

// Helper to find the main window handle for After Effects
static HWND FindAfterEffectsWindow() {
    // 1. Direct check for standard After Effects main window class
    HWND hWnd = FindWindowW(L"AE_Console_Win", nullptr);
    if (hWnd && IsWindow(hWnd) && IsWindowVisible(hWnd)) return hWnd;

    // 2. Comprehensive enumeration across all visible top-level windows
    HWND hFound = nullptr;
    EnumWindows([](HWND h, LPARAM lParam) -> BOOL {
        if (!IsWindowVisible(h)) return TRUE;
        wchar_t title[256] = { 0 };
        GetWindowTextW(h, title, 256);
        if (wcsstr(title, L"Adobe After Effects") != nullptr) {
            *reinterpret_cast<HWND*>(lParam) = h;
            return FALSE; // Stop enumeration
        }
        return TRUE;
    }, reinterpret_cast<LPARAM>(&hFound));
    return hFound;
}

// Helper to scan Adobe folder for any version matching a prefix
static std::wstring ScanAdobeDirectoryForExe(const std::wstring& folderPrefix, const std::wstring& relativeExePath) {
    std::wstring searchPattern = L"C:\\Program Files\\Adobe\\" + folderPrefix + L"*";
    WIN32_FIND_DATAW findData;
    HANDLE hFind = FindFirstFileW(searchPattern.c_str(), &findData);
    if (hFind != INVALID_HANDLE_VALUE) {
        do {
            if (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
                std::wstring candidate = L"C:\\Program Files\\Adobe\\" + std::wstring(findData.cFileName) + L"\\" + relativeExePath;
                if (GetFileAttributesW(candidate.c_str()) != INVALID_FILE_ATTRIBUTES) {
                    FindClose(hFind);
                    return candidate;
                }
            }
        } while (FindNextFileW(hFind, &findData));
        FindClose(hFind);
    }
    return L"";
}

std::wstring ExternalAppIntegration::FindExecutableInRegistry(const std::wstring& appName) {
    HKEY hKey = nullptr;
    std::wstring subKey = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + appName;
    std::wstring result = L"";

    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, subKey.c_str(), 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        wchar_t path[MAX_PATH] = {0};
        DWORD pathSize = sizeof(path);
        if (RegQueryValueExW(hKey, nullptr, nullptr, nullptr, (LPBYTE)path, &pathSize) == ERROR_SUCCESS) {
            result = path;
        }
        RegCloseKey(hKey);
    }
    return result;
}

std::wstring ExternalAppIntegration::FindAfterEffectsExe() {
    // 1. If After Effects is already actively running, target that exact instance!
    std::wstring runningPath = FindRunningProcessPath(L"AfterFX.exe");
    if (!runningPath.empty() && GetFileAttributesW(runningPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return runningPath;
    }

    // 2. Check Windows Registry default App Paths
    std::wstring regPath = FindExecutableInRegistry(L"AfterFX.exe");
    if (!regPath.empty() && GetFileAttributesW(regPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return regPath;
    }

    // 3. Comprehensive list of install locations (2026 down to CC 2018)
    const wchar_t* commonPaths[] = {
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2026\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2025\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2024\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2023\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2022\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2021\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2020\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects CC 2019\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2019\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects CC 2018\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects (Beta)\\Support Files\\AfterFX.exe"
    };

    for (const wchar_t* p : commonPaths) {
        if (GetFileAttributesW(p) != INVALID_FILE_ATTRIBUTES) {
            return p;
        }
    }

    // 4. Wildcard scan in Adobe folder
    std::wstring scanned = ScanAdobeDirectoryForExe(L"Adobe After Effects", L"Support Files\\AfterFX.exe");
    if (!scanned.empty()) {
        return scanned;
    }

    return L"";
}

std::wstring ExternalAppIntegration::FindAfterEffectsCmd() {
    std::wstring aeExe = FindAfterEffectsExe();
    if (aeExe.empty()) return L"";

    // If AfterFX.com exists in the same Support Files directory, prefer it for CLI execution
    std::wstring aeCom = aeExe;
    size_t extPos = aeCom.rfind(L".exe");
    if (extPos != std::wstring::npos) {
        aeCom.replace(extPos, 4, L".com");
        if (GetFileAttributesW(aeCom.c_str()) != INVALID_FILE_ATTRIBUTES) {
            return aeCom;
        }
    }
    return aeExe;
}

std::wstring ExternalAppIntegration::FindPhotoshopExe() {
    // 1. If Photoshop is already actively running, target that exact instance!
    std::wstring runningPath = FindRunningProcessPath(L"Photoshop.exe");
    if (!runningPath.empty() && GetFileAttributesW(runningPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return runningPath;
    }

    // 2. Check Windows Registry default App Paths
    std::wstring regPath = FindExecutableInRegistry(L"Photoshop.exe");
    if (!regPath.empty() && GetFileAttributesW(regPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return regPath;
    }

    // 3. Comprehensive list of install locations (2026 down to CC 2018)
    const wchar_t* commonPaths[] = {
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2026\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2025\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2024\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2023\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2022\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2021\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2020\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop CC 2019\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2019\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop CC 2018\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop (Beta)\\Photoshop.exe"
    };

    for (const wchar_t* p : commonPaths) {
        if (GetFileAttributesW(p) != INVALID_FILE_ATTRIBUTES) {
            return p;
        }
    }

    // 4. Wildcard scan in Adobe folder
    std::wstring scanned = ScanAdobeDirectoryForExe(L"Adobe Photoshop", L"Photoshop.exe");
    if (!scanned.empty()) {
        return scanned;
    }

    return L"";
}

std::wstring ExternalAppIntegration::FindBlenderExe() {
    // 1. If Blender is already running, target that exact instance!
    std::wstring runningPath = FindRunningProcessPath(L"blender.exe");
    if (!runningPath.empty() && GetFileAttributesW(runningPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return runningPath;
    }

    // 2. Check Windows Registry default App Paths
    std::wstring regPath = FindExecutableInRegistry(L"blender.exe");
    if (!regPath.empty() && GetFileAttributesW(regPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return regPath;
    }

    // 3. Common Blender Foundation & Steam install locations
    const wchar_t* commonPaths[] = {
        L"C:\\Program Files\\Blender Foundation\\Blender 4.4\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.3\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.2\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.1\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.0\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 3.6\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 3.5\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 3.4\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 3.3\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 3.0\\blender.exe",
        L"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Blender\\blender.exe"
    };

    for (const wchar_t* p : commonPaths) {
        if (GetFileAttributesW(p) != INVALID_FILE_ATTRIBUTES) {
            return p;
        }
    }
    return L"";
}

std::vector<SoftwareTarget> ExternalAppIntegration::DetectInstalledSoftware() {
    std::vector<SoftwareTarget> targets;

    std::wstring aePath = FindAfterEffectsExe();
    targets.push_back({ L"after_effects", L"Adobe After Effects", aePath, !aePath.empty() });

    std::wstring psPath = FindPhotoshopExe();
    targets.push_back({ L"photoshop", L"Adobe Photoshop", psPath, !psPath.empty() });

    std::wstring blenderPath = FindBlenderExe();
    targets.push_back({ L"blender", L"Blender", blenderPath, !blenderPath.empty() });

    return targets;
}

static std::wstring EscapeForJsxString(const std::wstring& s) {
    std::wstring res;
    for (wchar_t c : s) {
        if (c == L'\\') res += L"\\\\";
        else if (c == L'\"') res += L"\\\"";
        else if (c == L'\'') res += L"\\\'";
        else if (c == L'\n') res += L"\\n";
        else if (c == L'\r') res += L"\\r";
        else if (c == L'\t') res += L"\\t";
        else if (c < 32 || c > 126) {
            wchar_t hexBuf[10];
            swprintf_s(hexBuf, L"\\u%04x", (unsigned int)c);
            res += hexBuf;
        } else {
            res += c;
        }
    }
    return res;
}

static std::wstring EscapeJsxFilePath(const std::wstring& path) {
    std::wstring p = path;
    for (auto& c : p) {
        if (c == L'\\') c = L'/';
    }
    return EscapeForJsxString(p);
}

bool ExternalAppIntegration::ExportToAfterEffectsAdvanced(const AeExportCompPayload& payload, std::wstring& outMessage) {
    std::wstringstream jsx;
    jsx << L"// DropBoard Smart Export for Adobe After Effects\n";
    jsx << L"(function() {\n";
    jsx << L"    try {\n";
    jsx << L"        if (!app.project) {\n";
    jsx << L"            app.newProject();\n";
    jsx << L"        }\n";
    jsx << L"        app.beginUndoGroup(\"DropBoard Reference Import\");\n\n";

    jsx << L"        function getOrCreateFolder(name) {\n";
    jsx << L"            for (var i = 1; i <= app.project.items.length; i++) {\n";
    jsx << L"                var item = app.project.items[i];\n";
    jsx << L"                if ((item instanceof FolderItem) && item.name === name) {\n";
    jsx << L"                    return item;\n";
    jsx << L"                }\n";
    jsx << L"            }\n";
    jsx << L"            return app.project.items.addFolder(name);\n";
    jsx << L"        }\n\n";

    jsx << L"        var refFolder = getOrCreateFolder(\"_References\");\n";

    if (payload.mode == L"group_comp") {
        std::wstring rawName = payload.compName.empty() ? L"References" : payload.compName;
        std::wstring compName = L"REF_" + EscapeForJsxString(rawName);
        double compW = payload.compWidth > 0 ? payload.compWidth : 1920.0;
        double compH = payload.compHeight > 0 ? payload.compHeight : 1080.0;

        jsx << L"        var compW = " << compW << L";\n";
        jsx << L"        var compH = " << compH << L";\n";
        jsx << L"        var comp = app.project.items.addComp(\"" << compName << L"\", compW, compH, 1.0, 10.0, 30.0);\n";
        jsx << L"        comp.parentFolder = refFolder;\n\n";

        // 1. Dedicated Typography Text Layer with actual font applied
        bool hasTypo = !payload.fontFamily.empty() || !payload.sampleText.empty();
        if (hasTypo) {
            std::wstring fontName = payload.fontFamily.empty() ? L"Sans-Serif" : payload.fontFamily;
            std::wstring sampleText = payload.sampleText.empty() ? fontName : payload.sampleText;

            jsx << L"        var typoLayer = comp.layers.addText(\"" << EscapeForJsxString(sampleText) << L"\");\n";
            jsx << L"        typoLayer.name = \"TYPOGRAPHY: " << EscapeForJsxString(fontName) << L" [Guide Layer]\";\n";
            jsx << L"        typoLayer.guideLayer = true;\n";
            jsx << L"        typoLayer.property(\"Position\").setValue([80, 130]);\n";
            jsx << L"        var typoProp = typoLayer.property(\"Source Text\");\n";
            jsx << L"        var typoDoc = typoProp.value;\n";
            jsx << L"        typoDoc.justification = ParagraphJustification.LEFT_JUSTIFY;\n";
            jsx << L"        typoDoc.fontSize = 38;\n";
            jsx << L"        typoDoc.fillColor = [0.98, 0.90, 0.42];\n";
            jsx << L"        try { typoDoc.font = \"" << EscapeForJsxString(fontName) << L"\"; } catch(e) {\n";
            jsx << L"            try {\n";
            jsx << L"                if (app.fonts && app.fonts.allFonts) {\n";
            jsx << L"                    for (var fi = 0; fi < app.fonts.allFonts.length; fi++) {\n";
            jsx << L"                        var fo = app.fonts.allFonts[fi];\n";
            jsx << L"                        if (fo.familyName.toLowerCase() === \"" << EscapeForJsxString(fontName) << L"\".toLowerCase() || fo.postScriptName.toLowerCase() === \"" << EscapeForJsxString(fontName) << L"\".toLowerCase()) {\n";
            jsx << L"                            typoDoc.font = fo.postScriptName;\n";
            jsx << L"                            break;\n";
            jsx << L"                        }\n";
            jsx << L"                    }\n";
            jsx << L"                }\n";
            jsx << L"            } catch(e2) {}\n";
            jsx << L"        }\n";
            jsx << L"        typoProp.setValue(typoDoc);\n\n";

            jsx << L"        var specLayer = comp.layers.addText(\"FONT: " << EscapeForJsxString(fontName) << L"\");\n";
            jsx << L"        specLayer.name = \"FONT SPEC [Guide Layer]\";\n";
            jsx << L"        specLayer.guideLayer = true;\n";
            jsx << L"        specLayer.property(\"Position\").setValue([80, 180]);\n";
            jsx << L"        var specProp = specLayer.property(\"Source Text\");\n";
            jsx << L"        var specDoc = specProp.value;\n";
            jsx << L"        specDoc.justification = ParagraphJustification.LEFT_JUSTIFY;\n";
            jsx << L"        specDoc.fontSize = 15;\n";
            jsx << L"        specDoc.fillColor = [0.60, 0.66, 0.76];\n";
            jsx << L"        specProp.setValue(specDoc);\n\n";
        }

        // 2. Dedicated Notes & Checklist Guide Layer
        bool hasNotes = !payload.notesText.empty() || !payload.vfxText.empty();
        if (hasNotes) {
            std::wstring fullNotes = L"";
            if (!payload.notesText.empty()) fullNotes += payload.notesText + L"\\n\\n";
            if (!payload.vfxText.empty()) fullNotes += L"VFX SPECS:\\n" + payload.vfxText;

            double notesPosY = hasTypo ? 240.0 : 90.0;
            jsx << L"        var notesLayer = comp.layers.addText(\"" << EscapeForJsxString(fullNotes) << L"\");\n";
            jsx << L"        notesLayer.name = \"SCENE NOTES & CHECKLIST [Guide Layer]\";\n";
            jsx << L"        notesLayer.guideLayer = true;\n";
            jsx << L"        notesLayer.property(\"Position\").setValue([80, " << notesPosY << L"]);\n";
            jsx << L"        var nProp = notesLayer.property(\"Source Text\");\n";
            jsx << L"        var nDoc = nProp.value;\n";
            jsx << L"        nDoc.justification = ParagraphJustification.LEFT_JUSTIFY;\n";
            jsx << L"        nDoc.fontSize = 18;\n";
            jsx << L"        nDoc.fillColor = [0.90, 0.93, 0.98];\n";
            jsx << L"        nProp.setValue(nDoc);\n\n";
        }

        bool hasInfo = hasTypo || hasNotes;

        // Layout items
        if (!payload.items.empty()) {
            // Find layout bounding box
            double minX = 1e9, minY = 1e9, maxX = -1e9, maxY = -1e9;
            for (const auto& it : payload.items) {
                if (it.relX < minX) minX = it.relX;
                if (it.relY < minY) minY = it.relY;
                if (it.relX + it.width > maxX) maxX = it.relX + it.width;
                if (it.relY + it.height > maxY) maxY = it.relY + it.height;
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

            double fitScale = min(availW / layoutW, availH / layoutH);
            if (fitScale <= 0.0) fitScale = 1.0;

            double targetAreaCenterX = leftMargin + availW / 2.0;
            double targetAreaCenterY = topMargin + availH / 2.0;
            double layoutCenterX = minX + layoutW / 2.0;
            double layoutCenterY = minY + layoutH / 2.0;

            jsx << L"        var itemsData = [\n";
            for (size_t i = 0; i < payload.items.size(); ++i) {
                const auto& it = payload.items[i];
                double posX = targetAreaCenterX + (it.relX + it.width / 2.0 - layoutCenterX) * fitScale;
                double posY = targetAreaCenterY + (it.relY + it.height / 2.0 - layoutCenterY) * fitScale;
                double desiredW = it.width * fitScale;

                jsx << L"            { path: \"" << EscapeJsxFilePath(it.filePath) 
                    << L"\", x: " << posX 
                    << L", y: " << posY 
                    << L", targetW: " << desiredW << L" }"
                    << (i + 1 < payload.items.size() ? L"," : L"") << L"\n";
            }
            jsx << L"        ];\n\n";

            jsx << L"        for (var j = 0; j < itemsData.length; j++) {\n";
            jsx << L"            var it = itemsData[j];\n";
            jsx << L"            try {\n";
            jsx << L"                var f = new File(it.path);\n";
            jsx << L"                if (f.exists) {\n";
            jsx << L"                    var footage = null;\n";
            jsx << L"                    try {\n";
            jsx << L"                        var io = new ImportOptions();\n";
            jsx << L"                        io.file = f;\n";
            jsx << L"                        if (io.canImportAs(ImportAsType.FOOTAGE)) {\n";
            jsx << L"                            io.importAs = ImportAsType.FOOTAGE;\n";
            jsx << L"                        }\n";
            jsx << L"                        io.sequence = false;\n";
            jsx << L"                        io.forceAlphabetical = false;\n";
            jsx << L"                        footage = app.project.importFile(io);\n";
            jsx << L"                    } catch(eIo) {\n";
            jsx << L"                        try {\n";
            jsx << L"                            footage = app.project.importFile(new ImportOptions(f));\n";
            jsx << L"                        } catch(eDirect) {}\n";
            jsx << L"                    }\n";
            jsx << L"                    if (footage) {\n";
            jsx << L"                        footage.parentFolder = refFolder;\n";
            jsx << L"                        var layer = comp.layers.add(footage);\n";
            jsx << L"                        layer.guideLayer = true;\n";
            jsx << L"                        layer.property(\"Position\").setValue([it.x, it.y]);\n";
            jsx << L"                        if (footage.width > 0) {\n";
            jsx << L"                            var sc = (it.targetW / footage.width) * 100;\n";
            jsx << L"                            layer.property(\"Scale\").setValue([sc, sc]);\n";
            jsx << L"                        }\n";
            jsx << L"                    }\n";
            jsx << L"                }\n";
            jsx << L"            } catch(eItem) {}\n";
            jsx << L"        }\n";
        }

        jsx << L"\n        comp.openInViewer();\n";
    }
    else {
        // Mode: loose_photos (User selected only photos - send directly to active comp or auto-create comp)
        jsx << L"        var activeComp = (app.project.activeItem && (app.project.activeItem instanceof CompItem)) ? app.project.activeItem : null;\n";
        jsx << L"        if (!activeComp) {\n";
        jsx << L"            for (var ci = 1; ci <= app.project.items.length; ci++) {\n";
        jsx << L"                if (app.project.items[ci] instanceof CompItem) {\n";
        jsx << L"                    activeComp = app.project.items[ci];\n";
        jsx << L"                    break;\n";
        jsx << L"                }\n";
        jsx << L"            }\n";
        jsx << L"            if (!activeComp) {\n";
        jsx << L"                activeComp = app.project.items.addComp(\"Reference Footage\", 1920, 1080, 1.0, 10.0, 30.0);\n";
        jsx << L"                activeComp.parentFolder = refFolder;\n";
        jsx << L"            }\n";
        jsx << L"            activeComp.openInViewer();\n";
        jsx << L"        }\n\n";
        jsx << L"        var itemsData = [\n";
        for (size_t i = 0; i < payload.items.size(); ++i) {
            const auto& it = payload.items[i];
            jsx << L"            { path: \"" << EscapeJsxFilePath(it.filePath) 
                << L"\", relX: " << it.relX 
                << L", relY: " << it.relY 
                << L", width: " << it.width 
                << L", height: " << it.height << L" }"
                << (i + 1 < payload.items.size() ? L"," : L"") << L"\n";
        }
        jsx << L"        ];\n\n";

        jsx << L"        var minX = 1e9, minY = 1e9, maxX = -1e9, maxY = -1e9;\n";
        jsx << L"        for (var k = 0; k < itemsData.length; k++) {\n";
        jsx << L"            var it = itemsData[k];\n";
        jsx << L"            if (it.relX < minX) minX = it.relX;\n";
        jsx << L"            if (it.relY < minY) minY = it.relY;\n";
        jsx << L"            if (it.relX + it.width > maxX) maxX = it.relX + it.width;\n";
        jsx << L"            if (it.relY + it.height > maxY) maxY = it.relY + it.height;\n";
        jsx << L"        }\n";
        jsx << L"        var layoutW = (maxX > minX) ? (maxX - minX) : 100;\n";
        jsx << L"        var layoutH = (maxY > minY) ? (maxY - minY) : 100;\n";
        jsx << L"        var layoutCenterX = minX + layoutW / 2;\n";
        jsx << L"        var layoutCenterY = minY + layoutH / 2;\n\n";

        jsx << L"        for (var k = 0; k < itemsData.length; k++) {\n";
        jsx << L"            var it = itemsData[k];\n";
        jsx << L"            try {\n";
        jsx << L"                var f = new File(it.path);\n";
        jsx << L"                if (f.exists) {\n";
        jsx << L"                    var footage = null;\n";
        jsx << L"                    try {\n";
        jsx << L"                        var io = new ImportOptions();\n";
        jsx << L"                        io.file = f;\n";
        jsx << L"                        if (io.canImportAs(ImportAsType.FOOTAGE)) {\n";
        jsx << L"                            io.importAs = ImportAsType.FOOTAGE;\n";
        jsx << L"                        }\n";
        jsx << L"                        io.sequence = false;\n";
        jsx << L"                        io.forceAlphabetical = false;\n";
        jsx << L"                        footage = app.project.importFile(io);\n";
        jsx << L"                    } catch(eIo) {\n";
        jsx << L"                        try {\n";
        jsx << L"                            footage = app.project.importFile(new ImportOptions(f));\n";
        jsx << L"                        } catch(eDirect) {}\n";
        jsx << L"                    }\n";
        jsx << L"                    if (footage) {\n";
        jsx << L"                        footage.parentFolder = refFolder;\n";
        jsx << L"                        if (activeComp) {\n";
        jsx << L"                            var layer = activeComp.layers.add(footage);\n";
        jsx << L"                            layer.guideLayer = true;\n";
        jsx << L"                            layer.selected = true;\n";
        jsx << L"                            if (itemsData.length === 1) {\n";
        jsx << L"                                layer.property(\"Position\").setValue([activeComp.width / 2, activeComp.height / 2]);\n";
        jsx << L"                                if (footage.width > 0 && footage.height > 0) {\n";
        jsx << L"                                    var targetW = activeComp.width * 0.75;\n";
        jsx << L"                                    var targetH = activeComp.height * 0.75;\n";
        jsx << L"                                    var sc = Math.min(targetW / footage.width, targetH / footage.height) * 100;\n";
        jsx << L"                                    layer.property(\"Scale\").setValue([sc, sc]);\n";
        jsx << L"                                }\n";
        jsx << L"                            } else {\n";
        jsx << L"                                var maxAllowedW = activeComp.width * 0.82;\n";
        jsx << L"                                var maxAllowedH = activeComp.height * 0.82;\n";
        jsx << L"                                var fitScale = Math.min(maxAllowedW / layoutW, maxAllowedH / layoutH);\n";
        jsx << L"                                if (fitScale <= 0) fitScale = 1.0;\n";
        jsx << L"                                var posX = activeComp.width / 2 + (it.relX + it.width / 2 - layoutCenterX) * fitScale;\n";
        jsx << L"                                var posY = activeComp.height / 2 + (it.relY + it.height / 2 - layoutCenterY) * fitScale;\n";
        jsx << L"                                layer.property(\"Position\").setValue([posX, posY]);\n";
        jsx << L"                                if (footage.width > 0) {\n";
        jsx << L"                                    var targetW = it.width * fitScale;\n";
        jsx << L"                                    var sc = (targetW / footage.width) * 100;\n";
        jsx << L"                                    layer.property(\"Scale\").setValue([sc, sc]);\n";
        jsx << L"                                }\n";
        jsx << L"                            }\n";
        jsx << L"                        }\n";
        jsx << L"                    }\n";
        jsx << L"                }\n";
        jsx << L"            } catch(eItem) {}\n";
        jsx << L"        }\n";
    }

    jsx << L"\n        app.endUndoGroup();\n";
    jsx << L"        \"SUCCESS\";\n";
    jsx << L"    } catch(err) {\n";
    jsx << L"        try { app.endUndoGroup(); } catch(e) {}\n";
    jsx << L"        \"ERROR: \" + err.toString();\n";
    jsx << L"    }\n";
    jsx << L"})();\n";

    // Direct execution via After Effects command CLI (AfterFX.com / AfterFX.exe)
    std::wstring aeCmd = FindAfterEffectsCmd();
    if (aeCmd.empty()) {
        aeCmd = FindAfterEffectsExe();
    }
    if (aeCmd.empty()) {
        outMessage = L"Adobe After Effects executable not detected on this system.";
        return false;
    }

    wchar_t tempPath[MAX_PATH] = {0};
    GetTempPathW(MAX_PATH, tempPath);
    std::wstring scriptFile = std::wstring(tempPath) + L"dropboard_ae_advanced_export.jsx";

    // Write file in UTF-8 with BOM
    std::string utf8Script;
    int needed = WideCharToMultiByte(CP_UTF8, 0, jsx.str().c_str(), (int)jsx.str().length(), nullptr, 0, nullptr, nullptr);
    if (needed > 0) {
        utf8Script.resize(needed);
        WideCharToMultiByte(CP_UTF8, 0, jsx.str().c_str(), (int)jsx.str().length(), &utf8Script[0], needed, nullptr, nullptr);
    }

    std::ofstream file(scriptFile, std::ios::binary | std::ios::trunc);
    if (!file.is_open()) {
        outMessage = L"Failed to create temporary ExtendScript file.";
        return false;
    }
    const unsigned char bom[3] = { 0xEF, 0xBB, 0xBF };
    file.write(reinterpret_cast<const char*>(bom), 3);
    file.write(utf8Script.data(), utf8Script.size());
    file.close();

    // Check if After Effects is already open and whether its window is currently maximized
    HWND hAE = FindAfterEffectsWindow();
    bool wasMaximized = (hAE != nullptr && IsZoomed(hAE));

    // Execute via ShellExecuteW using SW_HIDE so the command CLI runner runs silently
    // and NEVER sends SW_SHOWNORMAL to the GUI window!
    HINSTANCE hInst = ShellExecuteW(
        nullptr,
        L"open",
        aeCmd.c_str(),
        (L"-r \"" + scriptFile + L"\"").c_str(),
        nullptr,
        SW_HIDE
    );

    bool launched = ((INT_PTR)hInst > 32);

    if (!launched) {
        // Fallback: cmd.exe /d /s /c
        wchar_t sysDir[MAX_PATH] = {0};
        GetSystemDirectoryW(sysDir, MAX_PATH);
        std::wstring cmdExe = std::wstring(sysDir) + L"\\cmd.exe";
        std::wstring fullCmd = L"\"" + cmdExe + L"\" /d /s /c \"\"" + aeCmd + L"\" -r \"" + scriptFile + L"\"\"";

        STARTUPINFOW si = { sizeof(si) };
        si.cb = sizeof(si);
        si.dwFlags = STARTF_USESHOWWINDOW;
        si.wShowWindow = SW_HIDE;
        PROCESS_INFORMATION pi = {0};
        std::vector<wchar_t> cmdBuffer(fullCmd.begin(), fullCmd.end());
        cmdBuffer.push_back(L'\0');

        if (CreateProcessW(nullptr, cmdBuffer.data(), nullptr, nullptr, FALSE, CREATE_NO_WINDOW, nullptr, nullptr, &si, &pi)) {
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            launched = true;
        }
    }

    // Active Watchdog Thread:
    // If AE was maximized, ensure it stays maximized if anything tries to restore it down
    if (hAE && wasMaximized) {
        std::thread([hAE]() {
            for (int i = 0; i < 50; ++i) { // Check every 50ms for 2.5 seconds
                std::this_thread::sleep_for(std::chrono::milliseconds(50));
                if (!IsZoomed(hAE)) {
                    ShowWindow(hAE, SW_MAXIMIZE);
                    SetForegroundWindow(hAE);
                    std::this_thread::sleep_for(std::chrono::milliseconds(150));
                    if (!IsZoomed(hAE)) {
                        ShowWindow(hAE, SW_MAXIMIZE);
                    }
                    break;
                }
            }
        }).detach();
    } else if (hAE) {
        SetForegroundWindow(hAE);
    }

    if (launched) {
        outMessage = (payload.mode == L"group_comp") 
            ? L"Created Reference Comp in Adobe After Effects!"
            : L"Imported reference footage into Adobe After Effects!";
        return true;
    } else {
        outMessage = L"Failed to launch After Effects process.";
        return false;
    }
}

bool ExternalAppIntegration::SendToAfterEffects(const std::wstring& imagePath, std::wstring& outMessage) {
    AeExportCompPayload p;
    p.mode = L"loose_photos";
    AeExportItem item;
    item.filePath = imagePath;
    p.items.push_back(item);
    return ExportToAfterEffectsAdvanced(p, outMessage);
}

bool ExternalAppIntegration::SendToPhotoshop(const std::wstring& imagePath, std::wstring& outMessage) {
    std::wstring psExe = FindPhotoshopExe();
    if (psExe.empty()) {
        // Fallback to default app
        return OpenWithDefaultApp(imagePath, outMessage);
    }

    HINSTANCE hInst = ShellExecuteW(nullptr, L"open", psExe.c_str(), (L"\"" + imagePath + L"\"").c_str(), nullptr, SW_SHOW);
    if ((INT_PTR)hInst > 32) {
        outMessage = L"Opened in Adobe Photoshop.";
        return true;
    } else {
        outMessage = L"Failed to open Photoshop.";
        return false;
    }
}

bool ExternalAppIntegration::SendToCustomApp(const std::wstring& exePath, const std::wstring& imagePath, std::wstring& outMessage) {
    if (exePath.empty()) {
        outMessage = L"No custom application path specified.";
        return false;
    }

    HINSTANCE hInst = ShellExecuteW(nullptr, L"open", exePath.c_str(), (L"\"" + imagePath + L"\"").c_str(), nullptr, SW_SHOW);
    if ((INT_PTR)hInst > 32) {
        outMessage = L"Sent reference to application successfully.";
        return true;
    } else {
        outMessage = L"Failed to execute target application.";
        return false;
    }
}

bool ExternalAppIntegration::OpenWithDefaultApp(const std::wstring& imagePath, std::wstring& outMessage) {
    HINSTANCE hInst = ShellExecuteW(nullptr, L"open", imagePath.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
    if ((INT_PTR)hInst > 32) {
        outMessage = L"Opened in default system viewer.";
        return true;
    } else {
        outMessage = L"Failed to open file with default application.";
        return false;
    }
}

bool ExternalAppIntegration::RevealInExplorer(const std::wstring& imagePath) {
    if (imagePath.empty()) return false;
    DWORD attr = GetFileAttributesW(imagePath.c_str());
    if (attr != INVALID_FILE_ATTRIBUTES) {
        std::wstring param = L"/select,\"" + imagePath + L"\"";
        HINSTANCE hInst = ShellExecuteW(nullptr, L"open", L"explorer.exe", param.c_str(), nullptr, SW_SHOWNORMAL);
        return ((INT_PTR)hInst > 32);
    } else {
        size_t slash = imagePath.rfind(L'\\');
        if (slash != std::wstring::npos) {
            std::wstring dir = imagePath.substr(0, slash);
            HINSTANCE hInst = ShellExecuteW(nullptr, L"open", dir.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
            return ((INT_PTR)hInst > 32);
        }
    }
    return false;
}

bool ExternalAppIntegration::CopyFileToClipboard(HWND hWnd, const std::wstring& filePath) {
    if (!OpenClipboard(hWnd)) {
        return false;
    }
    EmptyClipboard();

    // Allocate memory for DROPFILES structure + double-null terminated file path
    size_t pathLenWithNulls = filePath.length() + 2; // + null terminator + list null terminator
    size_t totalSize = sizeof(DROPFILES) + (pathLenWithNulls * sizeof(wchar_t));

    HGLOBAL hMem = GlobalAlloc(GMEM_MOVEABLE, totalSize);
    if (!hMem) {
        CloseClipboard();
        return false;
    }

    DROPFILES* pDropFiles = (DROPFILES*)GlobalLock(hMem);
    if (!pDropFiles) {
        GlobalFree(hMem);
        CloseClipboard();
        return false;
    }

    pDropFiles->pFiles = sizeof(DROPFILES);
    pDropFiles->pt.x = 0;
    pDropFiles->pt.y = 0;
    pDropFiles->fNC = FALSE;
    pDropFiles->fWide = TRUE;

    wchar_t* pDest = (wchar_t*)((BYTE*)pDropFiles + sizeof(DROPFILES));
    wcsncpy_s(pDest, pathLenWithNulls, filePath.c_str(), filePath.length());
    pDest[filePath.length()] = L'\0';
    pDest[filePath.length() + 1] = L'\0'; // Double null

    GlobalUnlock(hMem);

    SetClipboardData(CF_HDROP, hMem);
    CloseClipboard();
    return true;
}
