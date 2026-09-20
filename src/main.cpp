#include <windows.h>
#include <dwmapi.h>
#include <shlwapi.h>
#include <string>
#include <fstream>
#include <memory>
#include "WebViewHost.h"
#include "resource.h"

#pragma comment(lib, "dwmapi.lib")
#pragma comment(lib, "shlwapi.lib")

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

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE, PWSTR, int nCmdShow) {
    // Enable Per-Monitor DPI Awareness v2
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // Initialize COM
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
    if (FAILED(hr)) return 1;

    const wchar_t CLASS_NAME[] = L"DropBoardMainWindowClass";

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
    HBRUSH darkBrush = CreateSolidBrush(RGB(13, 15, 20));
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

    HWND hWnd = CreateWindowExW(
        WS_EX_APPWINDOW,
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

    // Enable Windows 10/11 DWM shadow for frameless window
    MARGINS margins = { 1, 1, 1, 1 };
    DwmExtendFrameIntoClientArea(hWnd, &margins);

    // Set DWM Immersive Dark Mode
    BOOL darkMode = TRUE;
    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(darkMode));

    // Initialize WebView2
    g_webViewHost = std::make_unique<WebViewHost>();
    std::wstring assetsDir = LocateAssetsDir();
    if (!g_webViewHost->Initialize(hWnd, assetsDir)) {
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
