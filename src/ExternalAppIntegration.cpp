#include "ExternalAppIntegration.h"
#include <shlobj.h>
#include <fstream>
#include <sstream>

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
    std::wstring regPath = FindExecutableInRegistry(L"AfterFX.exe");
    if (!regPath.empty() && GetFileAttributesW(regPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return regPath;
    }

    // Common Adobe install locations (check newest first)
    const wchar_t* commonPaths[] = {
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2025\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2024\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2023\\Support Files\\AfterFX.exe",
        L"C:\\Program Files\\Adobe\\Adobe After Effects 2022\\Support Files\\AfterFX.exe"
    };

    for (const wchar_t* p : commonPaths) {
        if (GetFileAttributesW(p) != INVALID_FILE_ATTRIBUTES) {
            return p;
        }
    }
    return L"";
}

std::wstring ExternalAppIntegration::FindPhotoshopExe() {
    std::wstring regPath = FindExecutableInRegistry(L"Photoshop.exe");
    if (!regPath.empty() && GetFileAttributesW(regPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return regPath;
    }

    const wchar_t* commonPaths[] = {
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2025\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2024\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop 2023\\Photoshop.exe",
        L"C:\\Program Files\\Adobe\\Adobe Photoshop (Beta)\\Photoshop.exe"
    };

    for (const wchar_t* p : commonPaths) {
        if (GetFileAttributesW(p) != INVALID_FILE_ATTRIBUTES) {
            return p;
        }
    }
    return L"";
}

std::wstring ExternalAppIntegration::FindBlenderExe() {
    std::wstring regPath = FindExecutableInRegistry(L"blender.exe");
    if (!regPath.empty() && GetFileAttributesW(regPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return regPath;
    }

    const wchar_t* commonPaths[] = {
        L"C:\\Program Files\\Blender Foundation\\Blender 4.3\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.2\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.1\\blender.exe",
        L"C:\\Program Files\\Blender Foundation\\Blender 4.0\\blender.exe"
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

bool ExternalAppIntegration::SendToAfterEffects(const std::wstring& imagePath, std::wstring& outMessage) {
    std::wstring aeExe = FindAfterEffectsExe();
    if (aeExe.empty()) {
        outMessage = L"Adobe After Effects executable not detected on this system.";
        return false;
    }

    // Convert Windows backslashes to forward slashes for ExtendScript File path
    std::wstring jsPath = imagePath;
    for (auto& ch : jsPath) {
        if (ch == L'\\') ch = L'/';
    }

    // Prepare temp jsx script
    wchar_t tempPath[MAX_PATH] = {0};
    GetTempPathW(MAX_PATH, tempPath);
    std::wstring scriptFile = std::wstring(tempPath) + L"dropboard_ae_import.jsx";

    std::wstringstream jsx;
    jsx << L"// DropBoard Auto Import Script for Adobe After Effects\n";
    jsx << L"(function() {\n";
    jsx << L"    try {\n";
    jsx << L"        var targetFile = new File(\"" << jsPath << L"\");\n";
    jsx << L"        if (!targetFile.exists) {\n";
    jsx << L"            alert(\"DropBoard: Reference file not found: \" + targetFile.fsName);\n";
    jsx << L"            return;\n";
    jsx << L"        }\n";
    jsx << L"        if (!app.project) {\n";
    jsx << L"            app.newProject();\n";
    jsx << L"        }\n";
    jsx << L"        var importOptions = new ImportOptions(targetFile);\n";
    jsx << L"        var footageItem = app.project.importFile(importOptions);\n";
    jsx << L"        if (app.project.activeItem && (app.project.activeItem instanceof CompItem)) {\n";
    jsx << L"            var comp = app.project.activeItem;\n";
    jsx << L"            var layer = comp.layers.add(footageItem);\n";
    jsx << L"            layer.selected = true;\n";
    jsx << L"        }\n";
    jsx << L"    } catch(err) {\n";
    jsx << L"        alert(\"DropBoard AE Import Error: \" + err.toString());\n";
    jsx << L"    }\n";
    jsx << L"})();\n";

    // Write file in UTF-8
    std::wofstream file(scriptFile, std::ios::trunc);
    if (!file.is_open()) {
        outMessage = L"Failed to create temporary ExtendScript file.";
        return false;
    }
    file << jsx.str();
    file.close();

    // Execute via afterfx.exe -r <scriptFile>
    std::wstring cmd = L"\"" + aeExe + L"\" -r \"" + scriptFile + L"\"";
    
    STARTUPINFOW si = { sizeof(si) };
    PROCESS_INFORMATION pi = {0};
    std::vector<wchar_t> cmdBuffer(cmd.begin(), cmd.end());
    cmdBuffer.push_back(L'\0');

    if (CreateProcessW(nullptr, cmdBuffer.data(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, &si, &pi)) {
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        outMessage = L"Sent reference to Adobe After Effects successfully!";
        return true;
    } else {
        outMessage = L"Failed to launch After Effects process.";
        return false;
    }
}

bool ExternalAppIntegration::SendToPhotoshop(const std::wstring& imagePath, std::wstring& outMessage) {
    std::wstring psExe = FindPhotoshopExe();
    if (psExe.empty()) {
        // Fallback to default app
        return OpenWithDefaultApp(imagePath, outMessage);
    }

    HINSTANCE hInst = ShellExecuteW(nullptr, L"open", psExe.c_str(), (L"\"" + imagePath + L"\"").c_str(), nullptr, SW_SHOWNORMAL);
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

    HINSTANCE hInst = ShellExecuteW(nullptr, L"open", exePath.c_str(), (L"\"" + imagePath + L"\"").c_str(), nullptr, SW_SHOWNORMAL);
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
