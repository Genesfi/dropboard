#include <windows.h>
#include <dwmapi.h>
#include <shlwapi.h>
#include <shlobj.h>
#include <shellapi.h>
#include <string>
#include <fstream>
#include <memory>
#include "WebViewHost.h"
#include "resource.h"

#pragma comment(lib, "dwmapi.lib")
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")

static std::unique_ptr<WebViewHost> g_webViewHost;

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) {
    switch (message) {
    case WM_SIZE:
        if (g_webViewHost) {
            g_webViewHost->Resize(LOWORD(lParam), HIWORD(lParam));
        }
        return 0;

    case WM_NCCALCSIZE:
        // Returning 0 removes the standard title bar entirely while preserving resize borders and Aero snap!
        return 0;

    case WM_ERASEBKGND:
        return 1;

    case WM_COPYDATA: {
        COPYDATASTRUCT* cds = reinterpret_cast<COPYDATASTRUCT*>(lParam);
        if (cds && cds->dwData == 1001 && cds->lpData && g_webViewHost) {
            const wchar_t* pPath = reinterpret_cast<const wchar_t*>(cds->lpData);
            g_webViewHost->LoadBoardFromFile(pPath);
            if (IsIconic(hWnd)) {
                ShowWindow(hWnd, SW_RESTORE);
            }
            SetForegroundWindow(hWnd);
            return TRUE;
        }
        return FALSE;
    }

    case WM_DROPBOARD_ADD_URL: {
        AddImageRequest* req = reinterpret_cast<AddImageRequest*>(lParam);
        if (req && g_webViewHost) {
            g_webViewHost->ProcessAddImage(req->url, req->title);
            delete req;
        }
        if (IsIconic(hWnd)) {
            ShowWindow(hWnd, SW_RESTORE);
        }
        SetForegroundWindow(hWnd);
        return 0;
    }

    case WM_NCHITTEST: {
        WINDOWPLACEMENT wp = { sizeof(wp) };
        GetWindowPlacement(hWnd, &wp);
        if (wp.showCmd == SW_SHOWMAXIMIZED) {
            return HTCLIENT;
        }

        POINT pt = { (short)LOWORD(lParam), (short)HIWORD(lParam) };
        RECT rc;
        GetWindowRect(hWnd, &rc);
        const int border = 8;

        bool left = pt.x >= rc.left && pt.x < rc.left + border;
        bool right = pt.x <= rc.right && pt.x > rc.right - border;
        bool top = pt.y >= rc.top && pt.y < rc.top + border;
        bool bottom = pt.y <= rc.bottom && pt.y > rc.bottom - border;

        if (top && left) return HTTOPLEFT;
        if (top && right) return HTTOPRIGHT;
        if (bottom && left) return HTBOTTOMLEFT;
        if (bottom && right) return HTBOTTOMRIGHT;
        if (left) return HTLEFT;
        if (right) return HTRIGHT;
        if (top) return HTTOP;
        if (bottom) return HTBOTTOM;
        return HTCLIENT;
    }

    case WM_DESTROY:
        if (g_webViewHost) {
            g_webViewHost->Close();
        }
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(hWnd, message, wParam, lParam);
}

std::wstring GetExecutableDir() {
    wchar_t path[MAX_PATH] = {0};
    GetModuleFileNameW(nullptr, path, MAX_PATH);
    PathRemoveFileSpecW(path);
    return std::wstring(path);
}

std::wstring LocateAssetsDir() {
    std::wstring exeDir = GetExecutableDir();

    // Check workspace root first if running from build/Release or build/Debug
    std::wstring candidate3 = exeDir + L"\\..\\..\\assets";
    wchar_t resolved3[MAX_PATH] = {0};
    if (PathCanonicalizeW(resolved3, candidate3.c_str()) &&
        GetFileAttributesW(resolved3) != INVALID_FILE_ATTRIBUTES) {
        return std::wstring(resolved3);
    }

    // Check parent directory (for build/Release output)
    std::wstring candidate2 = exeDir + L"\\..\\assets";
    wchar_t resolved2[MAX_PATH] = {0};
    if (PathCanonicalizeW(resolved2, candidate2.c_str()) &&
        GetFileAttributesW(resolved2) != INVALID_FILE_ATTRIBUTES) {
        return std::wstring(resolved2);
    }

    // Check if assets folder exists alongside exe
    std::wstring candidate1 = exeDir + L"\\assets";
    if (GetFileAttributesW(candidate1.c_str()) != INVALID_FILE_ATTRIBUTES) {
        return candidate1;
    }

    return candidate1;
}

void EnsureFileAssociationRegistered() {
    wchar_t exePath[MAX_PATH] = { 0 };
    if (!GetModuleFileNameW(nullptr, exePath, MAX_PATH)) return;

    std::wstring exeStr = exePath;
    std::wstring iconStr = exeStr + L",0";
    std::wstring cmdStr = L"\"" + exeStr + L"\" \"%1\"";

    auto setRegString = [](HKEY root, const std::wstring& subkey, const wchar_t* valName, const std::wstring& valData) {
        HKEY hKey = nullptr;
        if (RegCreateKeyExW(root, subkey.c_str(), 0, nullptr, REG_OPTION_NON_VOLATILE, KEY_WRITE, nullptr, &hKey, nullptr) == ERROR_SUCCESS) {
            RegSetValueExW(hKey, valName, 0, REG_SZ,
                reinterpret_cast<const BYTE*>(valData.c_str()),
                static_cast<DWORD>((valData.length() + 1) * sizeof(wchar_t)));
            RegCloseKey(hKey);
        }
    };

    // 1. HKCU\Software\Classes\.dropboard
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\.dropboard", nullptr, L"DropBoard.Project");
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\.dropboard", L"Content Type", L"application/x-dropboard");
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\.dropboard", L"PerceivedType", L"Document");

    // 2. HKCU\Software\Classes\DropBoard.Project
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\DropBoard.Project", nullptr, L"DropBoard Project File");
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\DropBoard.Project", L"FriendlyTypeName", L"DropBoard Project File");

    // 3. HKCU\Software\Classes\DropBoard.Project\DefaultIcon
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\DropBoard.Project\\DefaultIcon", nullptr, iconStr);

    // 4. HKCU\Software\Classes\DropBoard.Project\shell\open\command
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\DropBoard.Project\\shell\\open\\command", nullptr, cmdStr);

    // 5. HKCU\Software\Classes\Applications\DropBoard.exe
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\Applications\\DropBoard.exe", L"FriendlyAppName", L"DropBoard Studio");
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\Applications\\DropBoard.exe\\DefaultIcon", nullptr, iconStr);
    setRegString(HKEY_CURRENT_USER, L"Software\\Classes\\Applications\\DropBoard.exe\\shell\\open\\command", nullptr, cmdStr);

    // 6. HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.dropboard
    setRegString(HKEY_CURRENT_USER, L"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.dropboard\\OpenWithList", L"a", L"DropBoard.exe");
    setRegString(HKEY_CURRENT_USER, L"Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.dropboard\\OpenWithList", L"MRUList", L"a");

    // 7. Refresh Windows Shell
    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, nullptr, nullptr);
}

std::wstring GetCommandLineTargetFile() {
    int numArgs = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &numArgs);
    std::wstring target;
    if (argv && numArgs >= 2) {
        std::wstring candidate = argv[1];
        if (!candidate.empty()) {
            if (candidate.front() == L'"' && candidate.back() == L'"' && candidate.length() >= 2) {
                candidate = candidate.substr(1, candidate.length() - 2);
            }
            target = candidate;
        }
        LocalFree(argv);
    }
    return target;
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE, PWSTR, int nCmdShow) {
    // Enable Per-Monitor DPI Awareness v2
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // Set WebView2 default background to transparent (A=0) before environment initialization
    SetEnvironmentVariableW(L"WEBVIEW2_DEFAULT_BACKGROUND_COLOR", L"0");

    // Initialize COM
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
    if (FAILED(hr)) return 1;

    const wchar_t CLASS_NAME[] = L"DropBoardMainWindowClass";

    // Parse target file from command line (e.g. user double-clicked a .dropboard file)
    std::wstring fileToOpen = GetCommandLineTargetFile();

    // Check single instance: if DropBoard is already running, hand over target file and focus it
    HWND existingHwnd = FindWindowW(CLASS_NAME, nullptr);
    if (existingHwnd) {
        if (!fileToOpen.empty()) {
            COPYDATASTRUCT cds = { 0 };
            cds.dwData = 1001;
            cds.cbData = static_cast<DWORD>((fileToOpen.length() + 1) * sizeof(wchar_t));
            cds.lpData = (PVOID)fileToOpen.c_str();
            SendMessageW(existingHwnd, WM_COPYDATA, 0, reinterpret_cast<LPARAM>(&cds));
        }
        if (IsIconic(existingHwnd)) {
            ShowWindow(existingHwnd, SW_RESTORE);
        }
        SetForegroundWindow(existingHwnd);
        CoUninitialize();
        return 0;
    }

    // Auto-ensure .dropboard file association & icon are registered in HKCU
    EnsureFileAssociationRegistered();

    HICON hAppIcon = LoadIconW(hInstance, MAKEINTRESOURCEW(IDI_APP_ICON));
    HICON hAppIconSm = (HICON)LoadImageW(hInstance, MAKEINTRESOURCEW(IDI_APP_ICON), IMAGE_ICON, GetSystemMetrics(SM_CXSMICON), GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR);

    WNDCLASSEXW wc = {0};
    wc.cbSize = sizeof(WNDCLASSEXW);
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.hIcon = hAppIcon;
    wc.hIconSm = hAppIconSm;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    HBRUSH darkBrush = CreateSolidBrush(RGB(0, 0, 0));
    wc.hbrBackground = darkBrush;
    wc.lpszClassName = CLASS_NAME;

    RegisterClassExW(&wc);

    // Create modern frameless window with native resize borders and Aero snap
    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
    int screenHeight = GetSystemMetrics(SM_CYSCREEN);
    int initialWidth = min(1360, (int)(screenWidth * 0.85));
    int initialHeight = min(880, (int)(screenHeight * 0.85));
    int posX = (screenWidth - initialWidth) / 2;
    int posY = (screenHeight - initialHeight) / 2;

    DWORD windowStyle = WS_OVERLAPPEDWINDOW | WS_CLIPCHILDREN | WS_CLIPSIBLINGS;
    DWORD windowExStyle = WS_EX_APPWINDOW;

    HWND hWnd = CreateWindowExW(
        windowExStyle,
        CLASS_NAME,
        L"DropBoard",
        windowStyle,
        posX, posY, initialWidth, initialHeight,
        nullptr, nullptr, hInstance, nullptr
    );

    if (!hWnd) return 0;

    // Apply icon to window instance as well
    if (hAppIcon) {
        SendMessageW(hWnd, WM_SETICON, ICON_BIG, (LPARAM)hAppIcon);
    }
    if (hAppIconSm) {
        SendMessageW(hWnd, WM_SETICON, ICON_SMALL, (LPARAM)hAppIconSm);
    }

    // Inform the window manager that the frame was altered
    SetWindowPos(hWnd, nullptr, 0, 0, 0, 0,
        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

    // Enable DWM alpha transparency across entire client area
    MARGINS margins = { -1, -1, -1, -1 };
    DwmExtendFrameIntoClientArea(hWnd, &margins);

    // Set DWM Immersive Dark Mode
    BOOL darkMode = TRUE;
    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(darkMode));

    // Initialize WebView2
    g_webViewHost = std::make_unique<WebViewHost>();
    std::wstring assetsDir = LocateAssetsDir();
    if (!g_webViewHost->Initialize(hWnd, assetsDir, fileToOpen)) {
        MessageBoxW(hWnd, L"Failed to initialize DropBoard UI. Please ensure Microsoft Edge WebView2 is installed.", L"DropBoard Error", MB_ICONERROR);
        return 1;
    }

    ShowWindow(hWnd, SW_SHOW);
    UpdateWindow(hWnd);

    MSG msg = {0};
    while (GetMessageW(&msg, nullptr, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessageW(&msg);
    }

    g_webViewHost.reset();
    CoUninitialize();
    return (int)msg.wParam;
}
