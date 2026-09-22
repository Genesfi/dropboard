#include "WebViewHost.h"
#include "ExternalAppIntegration.h"
#include "LocalHttpServer.h"
#include <wrl/event.h>
#include <shlobj.h>
#include <urlmon.h>
#include <commdlg.h>
#include <sstream>
#include <fstream>
#include <vector>
#include <set>
#include <iomanip>
#include <chrono>

#include <winhttp.h>
#include <wincrypt.h>
#include <regex>
#include <gdiplus.h>

#pragma comment(lib, "urlmon.lib")
#pragma comment(lib, "winhttp.lib")
#pragma comment(lib, "crypt32.lib")
#pragma comment(lib, "gdiplus.lib")

using namespace Microsoft::WRL;

// Helper function to get GDI+ encoder CLSID for PNG/JPEG
static int GetEncoderClsid(const WCHAR* format, CLSID* pClsid) {
    UINT num = 0, size = 0;
    Gdiplus::GetImageEncodersSize(&num, &size);
    if (size == 0) return -1;
    auto* pImageCodecInfo = (Gdiplus::ImageCodecInfo*)(malloc(size));
    if (!pImageCodecInfo) return -1;
    Gdiplus::GetImageEncoders(num, size, pImageCodecInfo);
    for (UINT j = 0; j < num; ++j) {
        if (wcscmp(pImageCodecInfo[j].MimeType, format) == 0) {
            *pClsid = pImageCodecInfo[j].Clsid;
            free(pImageCodecInfo);
            return j;
        }
    }
    free(pImageCodecInfo);
    return -1;
}

// Win32 GDI direct frame capture from screen DC
static bool CaptureScreenRectToPng(int screenX, int screenY, int width, int height, const std::wstring& outPngPath) {
    if (width <= 0 || height <= 0) return false;
    HDC hdcScreen = GetDC(NULL);
    if (!hdcScreen) return false;
    HDC hdcMem = CreateCompatibleDC(hdcScreen);
    if (!hdcMem) { ReleaseDC(NULL, hdcScreen); return false; }
    HBITMAP hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
    if (!hBitmap) { DeleteDC(hdcMem); ReleaseDC(NULL, hdcScreen); return false; }
    HGDIOBJ hOld = SelectObject(hdcMem, hBitmap);
    BitBlt(hdcMem, 0, 0, width, height, hdcScreen, screenX, screenY, SRCCOPY);
    SelectObject(hdcMem, hOld);
    DeleteDC(hdcMem);
    ReleaseDC(NULL, hdcScreen);

    Gdiplus::GdiplusStartupInput gdiplusStartupInput;
    ULONG_PTR gdiplusToken = 0;
    Gdiplus::GdiplusStartup(&gdiplusToken, &gdiplusStartupInput, NULL);

    bool saved = false;
    {
        Gdiplus::Bitmap bmp(hBitmap, NULL);
        CLSID pngClsid;
        if (GetEncoderClsid(L"image/png", &pngClsid) >= 0) {
            if (bmp.Save(outPngPath.c_str(), &pngClsid, NULL) == Gdiplus::Ok) {
                saved = true;
            }
        }
    }
    Gdiplus::GdiplusShutdown(gdiplusToken);
    DeleteObject(hBitmap);
    return saved;
}

// Simple JSON string parser helper for safe extraction
namespace JsonUtil {
    std::wstring EscapeString(const std::wstring& str) {
        std::wstring res;
        res.reserve(str.length() + str.length() / 8);
        for (wchar_t c : str) {
            switch (c) {
                case L'\"': res += L"\\\""; break;
                case L'\\': res += L"\\\\"; break;
                case L'\b': res += L"\\b"; break;
                case L'\f': res += L"\\f"; break;
                case L'\n': res += L"\\n"; break;
                case L'\r': res += L"\\r"; break;
                case L'\t': res += L"\\t"; break;
                default: res += c; break;
            }
        }
        return res;
    }

    std::string WideToUtf8(const std::wstring& wstr) {
        if (wstr.empty()) return std::string();
        int sizeNeeded = WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), (int)wstr.length(), nullptr, 0, nullptr, nullptr);
        if (sizeNeeded <= 0) return std::string();
        std::string strTo(sizeNeeded, 0);
        WideCharToMultiByte(CP_UTF8, 0, wstr.c_str(), (int)wstr.length(), &strTo[0], sizeNeeded, nullptr, nullptr);
        return strTo;
    }

    std::wstring Utf8ToWide(const std::string& str) {
        if (str.empty()) return std::wstring();
        int sizeNeeded = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), (int)str.length(), nullptr, 0);
        if (sizeNeeded <= 0) return std::wstring();
        std::wstring wstrTo(sizeNeeded, 0);
        MultiByteToWideChar(CP_UTF8, 0, str.c_str(), (int)str.length(), &wstrTo[0], sizeNeeded);
        return wstrTo;
    }

    std::wstring ExtractString(const std::wstring& json, const std::wstring& key) {
        std::wstring searchKey = L"\"" + key + L"\"";
        size_t keyPos = json.find(searchKey);
        if (keyPos == std::wstring::npos) return L"";

        size_t colonPos = json.find(L':', keyPos + searchKey.length());
        if (colonPos == std::wstring::npos) return L"";

        size_t quoteStart = json.find(L'\"', colonPos + 1);
        if (quoteStart == std::wstring::npos) return L"";

        std::wstring result;
        if (json.length() > quoteStart + 1) {
            result.reserve(json.length() - quoteStart);
        }
        bool inEscape = false;
        for (size_t i = quoteStart + 1; i < json.length(); ++i) {
            wchar_t c = json[i];
            if (inEscape) {
                if (c == L'\"') result += L'\"';
                else if (c == L'\\') result += L'\\';
                else if (c == L'n') result += L'\n';
                else if (c == L'r') result += L'\r';
                else if (c == L't') result += L'\t';
                else result += c;
                inEscape = false;
            } else if (c == L'\\') {
                inEscape = true;
            } else if (c == L'\"') {
                break;
            } else {
                result += c;
            }
        }
        return result;
    }

    double ExtractNumber(const std::wstring& json, const std::wstring& key, double defaultVal = 0.0) {
        std::wstring searchKey = L"\"" + key + L"\"";
        size_t keyPos = json.find(searchKey);
        if (keyPos == std::wstring::npos) return defaultVal;

        size_t colonPos = json.find(L':', keyPos + searchKey.length());
        if (colonPos == std::wstring::npos) return defaultVal;

        size_t valStart = json.find_first_not_of(L" \t\r\n", colonPos + 1);
        if (valStart == std::wstring::npos) return defaultVal;

        size_t valEnd = json.find_first_of(L",}\" \t\r\n", valStart);
        std::wstring numStr = (valEnd == std::wstring::npos) ? json.substr(valStart) : json.substr(valStart, valEnd - valStart);
        try {
            return std::stod(numStr);
        } catch (...) {
            return defaultVal;
        }
    }

    bool ExtractBool(const std::wstring& json, const std::wstring& key, bool defaultVal = false) {
        std::wstring searchKey = L"\"" + key + L"\"";
        size_t keyPos = json.find(searchKey);
        if (keyPos == std::wstring::npos) return defaultVal;

        size_t colonPos = json.find(L':', keyPos + searchKey.length());
        if (colonPos == std::wstring::npos) return defaultVal;

        size_t truePos = json.find(L"true", colonPos);
        size_t falsePos = json.find(L"false", colonPos);
        size_t nextComma = json.find(L',', colonPos);
        size_t nextBrace = json.find(L'}', colonPos);
        size_t limit = min(nextComma != std::wstring::npos ? nextComma : (size_t)-1,
                           nextBrace != std::wstring::npos ? nextBrace : (size_t)-1);

        if (truePos < limit) return true;
        if (falsePos < limit) return false;
        return defaultVal;
    }
}

WebViewHost::WebViewHost()
    : m_hWnd(nullptr)
    , m_isAlwaysOnTop(false)
    , m_currentOpacity(1.0f)
    , m_messageToken() {
}

WebViewHost::~WebViewHost() {
    Close();
}

void WebViewHost::Close() {
    if (m_httpServer) {
        m_httpServer->Stop();
        m_httpServer.reset();
    }
    if (m_webview && m_messageToken.value != 0) {
        m_webview->remove_WebMessageReceived(m_messageToken);
        m_messageToken.value = 0;
    }
    if (m_controller) {
        m_controller->Close();
        m_controller = nullptr;
    }
    m_webview = nullptr;
    m_environment = nullptr;
}

std::wstring WebViewHost::GetCacheDirectory() {
    wchar_t localAppData[MAX_PATH] = {0};
    SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, SHGFP_TYPE_CURRENT, localAppData);
    std::wstring cacheDir = std::wstring(localAppData) + L"\\DropBoard\\Cache";
    CreateDirectoryW((std::wstring(localAppData) + L"\\DropBoard").c_str(), nullptr);
    CreateDirectoryW(cacheDir.c_str(), nullptr);
    return cacheDir;
}

std::wstring WebViewHost::GetSessionFilePath() {
    wchar_t localAppData[MAX_PATH] = {0};
    SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, SHGFP_TYPE_CURRENT, localAppData);
    std::wstring appDir = std::wstring(localAppData) + L"\\DropBoard";
    CreateDirectoryW(appDir.c_str(), nullptr);
    return appDir + L"\\session.dropboard";
}

extern void EnsureFileAssociationRegistered();

bool WebViewHost::Initialize(HWND hWnd, const std::wstring& assetsPath, const std::wstring& initialFilePath) {
    m_hWnd = hWnd;
    m_assetsPath = assetsPath;
    m_initialFilePath = initialFilePath;

    // Start background HTTP server for Browser Extension bridge on port 28888
    m_httpServer = std::make_unique<LocalHttpServer>();
    m_httpServer->Start(28888, [this](const std::wstring& url, const std::wstring& title) {
        QueueAddImage(url, title);
    });

    // User data folder in LocalAppData
    wchar_t localAppData[MAX_PATH] = {0};
    SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, SHGFP_TYPE_CURRENT, localAppData);
    std::wstring userDataFolder = std::wstring(localAppData) + L"\\DropBoard\\WebView2Data";
    CreateDirectoryW(userDataFolder.c_str(), nullptr);

    HRESULT hr = CreateCoreWebView2EnvironmentWithOptions(
        nullptr,
        userDataFolder.c_str(),
        nullptr,
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [this](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
                if (FAILED(result) || !env) {
                    MessageBoxW(m_hWnd, L"Failed to create WebView2 environment. Ensure Microsoft Edge WebView2 Runtime is installed.", L"DropBoard Error", MB_ICONERROR);
                    return result;
                }
                m_environment = env;

                m_environment->CreateCoreWebView2Controller(
                    m_hWnd,
                    Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>(
                        [this](HRESULT ctrlResult, ICoreWebView2Controller* controller) -> HRESULT {
                            if (FAILED(ctrlResult) || !controller) {
                                MessageBoxW(m_hWnd, L"Failed to create WebView2 controller.", L"DropBoard Error", MB_ICONERROR);
                                return ctrlResult;
                            }
                            m_controller = controller;

                            RECT bounds;
                            GetClientRect(m_hWnd, &bounds);
                            RECT initialBounds = { 5, 5, bounds.right - 5, bounds.bottom - 5 };
                            m_controller->put_Bounds(initialBounds);
                            m_controller->put_IsVisible(TRUE);

                            // Dark background for seamless loading
                            ComPtr<ICoreWebView2Controller2> controller2;
                            if (SUCCEEDED(m_controller.As(&controller2))) {
                                COREWEBVIEW2_COLOR darkColor = { 255, 18, 20, 26 };
                                controller2->put_DefaultBackgroundColor(darkColor);
                            }

                            m_controller->get_CoreWebView2(&m_webview);
                            if (!m_webview) return E_FAIL;

                            ComPtr<ICoreWebView2Settings> settings;
                            m_webview->get_Settings(&settings);
                            if (settings) {
                                settings->put_IsScriptEnabled(TRUE);
                                settings->put_AreDefaultScriptDialogsEnabled(TRUE);
                                settings->put_IsWebMessageEnabled(TRUE);
                                settings->put_AreDefaultContextMenusEnabled(FALSE);
                                settings->put_AreDevToolsEnabled(TRUE);
                            }

                            // Intercept external popup/blank links to open in system default browser
                            m_webview->add_NewWindowRequested(
                                Callback<ICoreWebView2NewWindowRequestedEventHandler>(
                                    [](ICoreWebView2*, ICoreWebView2NewWindowRequestedEventArgs* args) -> HRESULT {
                                        args->put_Handled(TRUE);
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            ShellExecuteW(nullptr, L"open", uri, nullptr, nullptr, SW_SHOWNORMAL);
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr
                            );

                            SetupWebMessageHandling();

                            // Inject clean YouTube frame capture hook & UI cleanup into all frames
                            m_webview->AddScriptToExecuteOnDocumentCreated(
                                L"(function() {\n"
                                L"  if (window.self === window.top) return;\n"
                                L"  if (location.hostname.indexOf('youtube') === -1) return;\n"
                                L"  function cleanYoutube() {\n"
                                L"    try {\n"
                                L"      if (!document.getElementById('dropboard-clean-yt')) {\n"
                                L"        var style = document.createElement('style');\n"
                                L"        style.id = 'dropboard-clean-yt';\n"
                                L"        style.textContent = '.ytp-pause-overlay, .ytp-bezel, .ytp-chrome-top, .ytp-gradient-top, .ytp-gradient-bottom { display: none !important; }';\n"
                                L"        (document.head || document.documentElement).appendChild(style);\n"
                                L"      }\n"
                                L"    } catch(e) {}\n"
                                L"  }\n"
                                L"  cleanYoutube();\n"
                                L"  if (document.readyState === 'loading') {\n"
                                L"    document.addEventListener('DOMContentLoaded', cleanYoutube);\n"
                                L"  } else {\n"
                                L"    cleanYoutube();\n"
                                L"  }\n"
                                L"  window.addEventListener('mousedown', function(e) {\n"
                                L"    if (e.button === 0) {\n"
                                L"      var cardId = '';\n"
                                L"      try {\n"
                                L"        if (location.hash && location.hash.indexOf('cardId=') !== -1) {\n"
                                L"          cardId = location.hash.split('cardId=')[1].split('&')[0];\n"
                                L"        }\n"
                                L"      } catch(err) {}\n"
                                L"      window.parent.postMessage({\n"
                                L"        type: 'DROPBOARD_IFRAME_CLICK',\n"
                                L"        cardId: cardId,\n"
                                L"        clientX: e.screenX,\n"
                                L"        clientY: e.screenY\n"
                                L"      }, '*');\n"
                                L"    } else if (e.button === 1 || e.button === 2) {\n"
                                L"      e.preventDefault();\n"
                                L"      window.parent.postMessage({\n"
                                L"        type: 'DROPBOARD_IFRAME_PAN_START',\n"
                                L"        button: e.button,\n"
                                L"        clientX: e.screenX,\n"
                                L"        clientY: e.screenY\n"
                                L"      }, '*');\n"
                                L"    }\n"
                                L"  }, true);\n"
                                L"  window.addEventListener('wheel', function(e) {\n"
                                L"    e.preventDefault();\n"
                                L"    window.parent.postMessage({\n"
                                L"      type: 'DROPBOARD_IFRAME_WHEEL',\n"
                                L"      deltaX: e.deltaX,\n"
                                L"      deltaY: e.deltaY,\n"
                                L"      deltaMode: e.deltaMode || 0,\n"
                                L"      clientX: e.screenX,\n"
                                L"      clientY: e.screenY,\n"
                                L"      ctrlKey: e.ctrlKey,\n"
                                L"      shiftKey: e.shiftKey,\n"
                                L"      altKey: e.altKey\n"
                                L"    }, '*');\n"
                                L"  }, { passive: false });\n"
                                L"  window.addEventListener('message', function(ev) {\n"
                                L"    if (!ev.data) return;\n"
                                L"    if (ev.data.type === 'DROPBOARD_CAPTURE_YT_FRAME') {\n"
                                L"      try {\n"
                                L"        var v = document.querySelector('video');\n"
                                L"        if (v) {\n"
                                L"          var w = v.videoWidth || v.clientWidth || 1280;\n"
                                L"          var h = v.videoHeight || v.clientHeight || 720;\n"
                                L"          var c = document.createElement('canvas');\n"
                                L"          c.width = w;\n"
                                L"          c.height = h;\n"
                                L"          var ctx = c.getContext('2d');\n"
                                L"          ctx.drawImage(v, 0, 0, w, h);\n"
                                L"          var dataUrl = c.toDataURL('image/png');\n"
                                L"          window.parent.postMessage({\n"
                                L"            type: 'DROPBOARD_YT_FRAME_RESULT',\n"
                                L"            cardId: ev.data.cardId,\n"
                                L"            dataUrl: dataUrl,\n"
                                L"            sendToAe: !!ev.data.sendToAe\n"
                                L"          }, '*');\n"
                                L"        }\n"
                                L"      } catch(err) {\n"
                                L"        console.warn('Frame capture error:', err);\n"
                                L"      }\n"
                                L"    }\n"
                                L"  });\n"
                                L"})();\n",
                                nullptr
                            );

                            // Map folder to virtual host name for secure origin
                            ComPtr<ICoreWebView2_3> webview3;
                            if (SUCCEEDED(m_webview.As(&webview3))) {
                                webview3->SetVirtualHostNameToFolderMapping(
                                    L"dropboard.local",
                                    m_assetsPath.c_str(),
                                    COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND_ALLOW
                                );
                                std::wstring cacheDir = GetCacheDirectory();
                                webview3->SetVirtualHostNameToFolderMapping(
                                    L"dropboard-cache.local",
                                    cacheDir.c_str(),
                                    COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND_ALLOW
                                );
                                m_webview->Navigate(L"https://dropboard.local/index.html");
                            } else {
                                std::wstring fileUrl = L"file:///" + m_assetsPath + L"/index.html";
                                for (auto& ch : fileUrl) if (ch == L'\\') ch = L'/';
                                m_webview->Navigate(fileUrl.c_str());
                            }

                            return S_OK;
                        }).Get());
                return S_OK;
            }).Get());

    return SUCCEEDED(hr);
}

void WebViewHost::Resize(int width, int height) {
    if (m_controller) {
        WINDOWPLACEMENT wp = { sizeof(wp) };
        GetWindowPlacement(m_hWnd, &wp);
        bool isMax = (wp.showCmd == SW_SHOWMAXIMIZED);
        if (isMax) {
            RECT bounds = { 0, 0, width, height };
            m_controller->put_Bounds(bounds);
        } else {
            // Keep 5px margin around for native Win32 WM_NCHITTEST sizing border
            RECT bounds = { 5, 5, width - 5, height - 5 };
            m_controller->put_Bounds(bounds);
        }
    }
}

void WebViewHost::PostMessageToWeb(const std::wstring& jsonMessage) {
    if (m_webview) {
        m_webview->PostWebMessageAsJson(jsonMessage.c_str());
    }
}

void WebViewHost::SetAlwaysOnTop(bool top) {
    m_isAlwaysOnTop = top;
    SetWindowPos(m_hWnd, top ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    
    std::wstringstream ss;
    ss << L"{\"type\":\"always_on_top_changed\",\"value\":" << (top ? L"true" : L"false") << L"}";
    PostMessageToWeb(ss.str());
}

void WebViewHost::SetOpacity(float opacity) {
    if (opacity < 0.2f) opacity = 0.2f;
    if (opacity > 1.0f) opacity = 1.0f;
    m_currentOpacity = opacity;

    LONG_PTR exStyle = GetWindowLongPtrW(m_hWnd, GWL_EXSTYLE);
    if (opacity >= 0.999f) {
        // Remove WS_EX_LAYERED for maximum performance when fully opaque
        SetWindowLongPtrW(m_hWnd, GWL_EXSTYLE, exStyle & ~WS_EX_LAYERED);
        RedrawWindow(m_hWnd, nullptr, nullptr, RDW_ERASE | RDW_INVALIDATE | RDW_FRAME | RDW_ALLCHILDREN);
    } else {
        if (!(exStyle & WS_EX_LAYERED)) {
            SetWindowLongPtrW(m_hWnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
        }
        BYTE alpha = (BYTE)(opacity * 255.0f);
        SetLayeredWindowAttributes(m_hWnd, 0, alpha, LWA_ALPHA);
    }
}

void WebViewHost::SetupWebMessageHandling() {
    if (!m_webview) return;

    m_webview->add_WebMessageReceived(
        Callback<ICoreWebView2WebMessageReceivedEventHandler>(
            [this](ICoreWebView2* sender, ICoreWebView2WebMessageReceivedEventArgs* args) -> HRESULT {
                LPWSTR rawJson = nullptr;
                if (SUCCEEDED(args->get_WebMessageAsJson(&rawJson)) && rawJson) {
                    std::wstring jsonStr = rawJson;
                    CoTaskMemFree(rawJson);
                    HandleJsonCommand(jsonStr);
                }
                return S_OK;
            }).Get(),
        &m_messageToken);
}

void WebViewHost::QueueAddImage(const std::wstring& url, const std::wstring& title) {
    auto* req = new AddImageRequest{ url, title };
    if (!PostMessageW(m_hWnd, WM_DROPBOARD_ADD_URL, 0, reinterpret_cast<LPARAM>(req))) {
        delete req;
    }
}

static std::wstring ExtractYouTubeVideoId(const std::wstring& url) {
    // Check for youtu.be/<id>
    size_t pos = url.find(L"youtu.be/");
    if (pos != std::wstring::npos) {
        size_t start = pos + 9;
        size_t end = url.find_first_of(L"?&#/ \t\r\n", start);
        if (end == std::wstring::npos) end = url.length();
        if (end > start) return url.substr(start, end - start);
    }

    // Check for /shorts/<id>
    pos = url.find(L"/shorts/");
    if (pos != std::wstring::npos) {
        size_t start = pos + 8;
        size_t end = url.find_first_of(L"?&#/ \t\r\n", start);
        if (end == std::wstring::npos) end = url.length();
        if (end > start) return url.substr(start, end - start);
    }

    // Check for /embed/<id>
    pos = url.find(L"/embed/");
    if (pos != std::wstring::npos) {
        size_t start = pos + 7;
        size_t end = url.find_first_of(L"?&#/ \t\r\n", start);
        if (end == std::wstring::npos) end = url.length();
        if (end > start) return url.substr(start, end - start);
    }

    // Check for v=<id> in youtube.com
    pos = url.find(L"v=");
    if (pos != std::wstring::npos && (url.find(L"youtube.com") != std::wstring::npos || url.find(L"youtube-nocookie.com") != std::wstring::npos)) {
        size_t start = pos + 2;
        size_t end = url.find_first_of(L"&#/ \t\r\n", start);
        if (end == std::wstring::npos) end = url.length();
        if (end > start) return url.substr(start, end - start);
    }

    return L"";
}

static std::string Base64Encode(const std::vector<BYTE>& data) {
    static const char s_chars[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    std::string ret;
    int i = 0;
    BYTE a3[3], a4[4];
    for (BYTE b : data) {
        a3[i++] = b;
        if (i == 3) {
            a4[0] = (a3[0] & 0xfc) >> 2;
            a4[1] = ((a3[0] & 0x03) << 4) + ((a3[1] & 0xf0) >> 4);
            a4[2] = ((a3[1] & 0x0f) << 2) + ((a3[2] & 0xc0) >> 6);
            a4[3] = a3[2] & 0x3f;
            for (i = 0; i < 4; i++) ret += s_chars[a4[i]];
            i = 0;
        }
    }
    if (i) {
        for (int j = i; j < 3; j++) a3[j] = '\0';
        a4[0] = (a3[0] & 0xfc) >> 2;
        a4[1] = ((a3[0] & 0x03) << 4) + ((a3[1] & 0xf0) >> 4);
        a4[2] = ((a3[1] & 0x0f) << 2) + ((a3[2] & 0xc0) >> 6);
        for (int j = 0; j < i + 1; j++) ret += s_chars[a4[j]];
        while (i++ < 3) ret += '=';
    }
    return ret;
}

static std::wstring FileToBase64DataUrl(const std::wstring& filePath) {
    if (filePath.empty()) return L"";
    HANDLE hFile = CreateFileW(filePath.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hFile == INVALID_HANDLE_VALUE) return L"";
    DWORD fileSize = GetFileSize(hFile, nullptr);
    if (fileSize == 0 || fileSize == INVALID_FILE_SIZE) {
        CloseHandle(hFile);
        return L"";
    }
    std::vector<BYTE> buffer(fileSize);
    DWORD bytesRead = 0;
    ReadFile(hFile, buffer.data(), fileSize, &bytesRead, nullptr);
    CloseHandle(hFile);
    if (bytesRead == 0) return L"";
    std::string b64 = Base64Encode(buffer);
    std::wstring mime = L"image/jpeg";
    if (filePath.length() >= 4) {
        std::wstring ext = filePath.substr(filePath.length() - 4);
        for (auto& c : ext) c = towlower(c);
        if (ext == L".png") mime = L"image/png";
        else if (ext == L".gif") mime = L"image/gif";
        else if (ext == L"webp") mime = L"image/webp";
    }
    return L"data:" + mime + L";base64," + std::wstring(b64.begin(), b64.end());
}

static std::wstring EnsureEmbeddedImagesInBoardJson(const std::wstring& json) {
    std::wstring result = json;
    size_t cardPos = 0;
    while ((cardPos = result.find(L"\"localPath\":", cardPos)) != std::wstring::npos) {
        size_t valStart = result.find(L'\"', cardPos + 12);
        if (valStart == std::wstring::npos) break;
        size_t valEnd = result.find(L'\"', valStart + 1);
        if (valEnd == std::wstring::npos) break;
        std::wstring rawPath = result.substr(valStart + 1, valEnd - valStart - 1);
        
        std::wstring unescapedPath;
        for (size_t i = 0; i < rawPath.length(); ++i) {
            if (rawPath[i] == L'\\' && i + 1 < rawPath.length() && rawPath[i+1] == L'\\') {
                unescapedPath += L'\\';
                ++i;
            } else {
                unescapedPath += rawPath[i];
            }
        }
        
        size_t objStart = result.rfind(L'{', cardPos);
        if (objStart != std::wstring::npos) {
            size_t depth = 0;
            size_t objEnd = std::wstring::npos;
            for (size_t i = objStart; i < result.length(); ++i) {
                if (result[i] == L'{') depth++;
                else if (result[i] == L'}') {
                    depth--;
                    if (depth == 0) {
                        objEnd = i + 1;
                        break;
                    }
                }
            }
            if (objEnd != std::wstring::npos) {
                std::wstring cardObj = result.substr(objStart, objEnd - objStart);
                bool hasImageData = (cardObj.find(L"data:image/") != std::wstring::npos);
                if (!hasImageData && !unescapedPath.empty()) {
                    std::wstring b64 = FileToBase64DataUrl(unescapedPath);
                    if (!b64.empty()) {
                        size_t imgDataPos = cardObj.find(L"\"imageData\":");
                        if (imgDataPos != std::wstring::npos) {
                            size_t colon = cardObj.find(L':', imgDataPos);
                            size_t valStart = cardObj.find_first_not_of(L" \t\r\n", colon + 1);
                            if (valStart != std::wstring::npos) {
                                size_t valEnd = std::wstring::npos;
                                if (cardObj[valStart] == L'\"') {
                                    for (size_t k = valStart + 1; k < cardObj.length(); ++k) {
                                        if (cardObj[k] == L'\\' && k + 1 < cardObj.length()) {
                                            ++k;
                                        } else if (cardObj[k] == L'\"') {
                                            valEnd = k + 1;
                                            break;
                                        }
                                    }
                                } else {
                                    valEnd = cardObj.find_first_of(L",}\r\n", valStart);
                                }
                                if (valEnd != std::wstring::npos) {
                                    cardObj.replace(valStart, valEnd - valStart, L"\"" + JsonUtil::EscapeString(b64) + L"\"");
                                }
                            }
                        } else {
                            cardObj.insert(1, L"\n      \"imageData\": \"" + JsonUtil::EscapeString(b64) + L"\",");
                        }
                        result.replace(objStart, objEnd - objStart, cardObj);
                        cardPos = objStart + cardObj.length();
                        continue;
                    }
                }
            }
        }
        cardPos = valEnd + 1;
    }
    return result;
}

static bool DecodeBase64ToFile(const std::string& base64Str, const std::wstring& destFilePath) {
    DWORD binarySize = 0;
    if (!CryptStringToBinaryA(base64Str.c_str(), (DWORD)base64Str.length(), CRYPT_STRING_BASE64, nullptr, &binarySize, nullptr, nullptr)) {
        return false;
    }
    if (binarySize == 0) return false;
    std::vector<BYTE> buffer(binarySize);
    if (!CryptStringToBinaryA(base64Str.c_str(), (DWORD)base64Str.length(), CRYPT_STRING_BASE64, buffer.data(), &binarySize, nullptr, nullptr)) {
        return false;
    }
    std::ofstream out(destFilePath, std::ios::binary | std::ios::trunc);
    if (!out.is_open()) return false;
    out.write(reinterpret_cast<const char*>(buffer.data()), buffer.size());
    out.close();
    return true;
}

static bool IsWebpFile(const std::wstring& filePath) {
    if (filePath.empty()) return false;
    HANDLE hFile = CreateFileW(filePath.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hFile == INVALID_HANDLE_VALUE) return false;
    BYTE header[16] = {0};
    DWORD bytesRead = 0;
    ReadFile(hFile, header, sizeof(header), &bytesRead, nullptr);
    CloseHandle(hFile);
    if (bytesRead >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F') {
        if (header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P') return true;
    }
    return false;
}

static std::wstring EnsureDiskFileForExternalApp(const std::wstring& filePath, const std::wstring& imageData, const std::wstring& cacheDir) {
    // If file exists and is NOT a WebP file, it's safe for external apps like After Effects
    if (!filePath.empty() && GetFileAttributesW(filePath.c_str()) != INVALID_FILE_ATTRIBUTES && !IsWebpFile(filePath)) {
        return filePath;
    }
    if (imageData.empty()) return filePath;
    size_t comma = imageData.find(L',');
    if (comma == std::wstring::npos) return filePath;
    std::wstring mimeHeader = imageData.substr(0, comma);
    std::wstring b64 = imageData.substr(comma + 1);
    std::string b64Utf8 = JsonUtil::WideToUtf8(b64);
    std::wstring ext = L".jpg";
    if (mimeHeader.find(L"png") != std::wstring::npos) ext = L".png";

    static uint64_t s_c = 0;
    std::wstring alphaTag = L"";
    uint64_t val = (GetTickCount64() << 16) ^ (++s_c * 0x9e3779b97f4a7c15ULL);
    for (int i = 0; i < 8; ++i) {
        alphaTag += (wchar_t)(L'a' + (val % 26));
        val /= 26;
    }
    std::wstring fname = L"ref_still_" + alphaTag + ext;
    std::wstring outPath = cacheDir + L"\\" + fname;
    DecodeBase64ToFile(b64Utf8, outPath);
    return outPath;
}

void WebViewHost::ProcessAddImage(const std::wstring& url, const std::wstring& title) {
    std::wstring ytId = ExtractYouTubeVideoId(url);
    bool isYouTube = !ytId.empty();
    std::wstring resolvedUrl = ResolveWebImageUrl(url);
    std::wstring localPath = DownloadImageToLocalCache(resolvedUrl);

    // If maxresdefault thumbnail didn't exist for YouTube, fallback to hqdefault
    if (isYouTube && localPath.empty()) {
        resolvedUrl = L"https://img.youtube.com/vi/" + ytId + L"/hqdefault.jpg";
        localPath = DownloadImageToLocalCache(resolvedUrl);
    }

    std::wstring localFileName = L"";
    if (!localPath.empty()) {
        size_t slash = localPath.rfind(L'\\');
        if (slash != std::wstring::npos) {
            localFileName = localPath.substr(slash + 1);
        }
    }
    std::wstring localWebUrl = localFileName.empty() ? L"" : (L"https://dropboard-cache.local/" + localFileName);

    std::wstring ytVideoUrl = isYouTube ? (L"https://www.youtube.com/watch?v=" + ytId) : L"";
    std::wstring b64Data = FileToBase64DataUrl(localPath);

    std::wstringstream resp;
    resp << L"{\"type\":\"external_image_added\","
         << L"\"url\":\"" << JsonUtil::EscapeString(resolvedUrl) << L"\","
         << L"\"originalUrl\":\"" << JsonUtil::EscapeString(url) << L"\","
         << L"\"title\":\"" << JsonUtil::EscapeString(title) << L"\","
         << L"\"localPath\":\"" << JsonUtil::EscapeString(localPath) << L"\","
         << L"\"localWebUrl\":\"" << JsonUtil::EscapeString(localWebUrl) << L"\","
         << L"\"imageData\":\"" << JsonUtil::EscapeString(b64Data) << L"\","
         << L"\"isYouTube\":" << (isYouTube ? L"true" : L"false") << L","
         << L"\"youtubeId\":\"" << JsonUtil::EscapeString(ytId) << L"\","
         << L"\"youtubeUrl\":\"" << JsonUtil::EscapeString(ytVideoUrl) << L"\"}";
    PostMessageToWeb(resp.str());
}

static std::wstring UrlEncode(const std::wstring& value) {
    std::wostringstream escaped;
    escaped.fill(L'0');
    escaped << std::hex;

    for (wchar_t c : value) {
        if ((c >= L'a' && c <= L'z') || (c >= L'A' && c <= L'Z') || (c >= L'0' && c <= L'9') ||
            c == L'-' || c == L'_' || c == L'.' || c == L'~') {
            escaped << c;
        } else {
            escaped << L'%' << std::uppercase << std::setw(2) << int((unsigned char)c) << std::nouppercase;
        }
    }
    return escaped.str();
}

static std::string WinHttpGetUtf8(const std::wstring& host, const std::wstring& path, bool isHttps = true) {
    std::string result;
    HINTERNET hSession = WinHttpOpen(
        L"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
        WINHTTP_NO_PROXY_NAME,
        WINHTTP_NO_PROXY_BYPASS, 0);
    if (!hSession) return "";

    HINTERNET hConnect = WinHttpConnect(hSession, host.c_str(), isHttps ? INTERNET_DEFAULT_HTTPS_PORT : INTERNET_DEFAULT_HTTP_PORT, 0);
    if (!hConnect) {
        WinHttpCloseHandle(hSession);
        return "";
    }

    DWORD flags = isHttps ? WINHTTP_FLAG_SECURE : 0;
    HINTERNET hRequest = WinHttpOpenRequest(hConnect, L"GET", path.c_str(),
                                           nullptr, WINHTTP_NO_REFERER,
                                           WINHTTP_DEFAULT_ACCEPT_TYPES, flags);
    if (!hRequest) {
        WinHttpCloseHandle(hConnect);
        WinHttpCloseHandle(hSession);
        return "";
    }

    DWORD redirectOption = WINHTTP_OPTION_REDIRECT_POLICY_ALWAYS;
    WinHttpSetOption(hRequest, WINHTTP_OPTION_REDIRECT_POLICY, &redirectOption, sizeof(redirectOption));

    if (WinHttpSendRequest(hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0, WINHTTP_NO_REQUEST_DATA, 0, 0, 0) &&
        WinHttpReceiveResponse(hRequest, nullptr)) {
        
        DWORD bytesAvailable = 0;
        while (WinHttpQueryDataAvailable(hRequest, &bytesAvailable) && bytesAvailable > 0) {
            std::vector<char> buffer(bytesAvailable);
            DWORD bytesRead = 0;
            if (WinHttpReadData(hRequest, buffer.data(), bytesAvailable, &bytesRead) && bytesRead > 0) {
                result.append(buffer.data(), bytesRead);
            }
        }
    }

    WinHttpCloseHandle(hRequest);
    WinHttpCloseHandle(hConnect);
    WinHttpCloseHandle(hSession);
    return result;
}

static bool IsValidImageFile(const std::wstring& filePath) {
    HANDLE hFile = CreateFileW(filePath.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hFile == INVALID_HANDLE_VALUE) return false;

    BYTE header[32] = {0};
    DWORD bytesRead = 0;
    ReadFile(hFile, header, sizeof(header), &bytesRead, nullptr);
    CloseHandle(hFile);

    if (bytesRead < 4) return false;

    // PNG: 89 50 4E 47
    if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
    // JPEG: FF D8 FF
    if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
    // GIF: 'G' 'I' 'F'
    if (header[0] == 'G' && header[1] == 'I' && header[2] == 'F') return true;
    // WebP: 'R' 'I' 'F' 'F' ... 'W' 'E' 'B' 'P'
    if (header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' && bytesRead >= 12) {
        if (header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P') return true;
    }
    // SVG
    if (header[0] == '<' && (header[1] == '?' || header[1] == 's' || header[1] == 'S')) {
        std::string s(reinterpret_cast<char*>(header), bytesRead);
        if (s.find("<!DOCTYPE") == std::string::npos && s.find("<html") == std::string::npos) {
            return true;
        }
    }

    return false;
}

std::wstring WebViewHost::DownloadImageToLocalCache(const std::wstring& url) {
    if (url.empty()) return L"";

    // Refuse to download HTML web page URLs directly as images
    if (url.find(L"pinterest.com/pin/") != std::wstring::npos ||
        url.find(L"pin.it/") != std::wstring::npos) {
        return L"";
    }

    std::wstring cacheDir = GetCacheDirectory();

    // Generate deterministic filename based on URL hash (Automatic Cache Deduplication)
    size_t hashVal = std::hash<std::wstring>{}(url);

    // Try to determine extension from URL
    std::wstring ext = L".jpg";
    size_t dotPos = url.rfind(L'.');
    if (dotPos != std::wstring::npos && dotPos > url.length() - 6) {
        std::wstring potentialExt = url.substr(dotPos);
        size_t queryPos = potentialExt.find(L'?');
        if (queryPos != std::wstring::npos) potentialExt = potentialExt.substr(0, queryPos);
        if (potentialExt == L".jpg" || potentialExt == L".jpeg" || potentialExt == L".png" ||
            potentialExt == L".webp" || potentialExt == L".gif" || potentialExt == L".svg") {
            ext = potentialExt;
        }
    }

    std::wstringstream ss;
    ss << cacheDir << L"\\ref_" << hashVal << ext;
    std::wstring localPath = ss.str();

    // Fast Cache Hit: If identical URL is already cached on disk, reuse immediately!
    if (GetFileAttributesW(localPath.c_str()) != INVALID_FILE_ATTRIBUTES && IsValidImageFile(localPath)) {
        return localPath;
    }

    HRESULT hr = URLDownloadToFileW(nullptr, url.c_str(), localPath.c_str(), 0, nullptr);
    if (SUCCEEDED(hr)) {
        // Validate file has valid image header magic bytes (rejects HTML error pages)
        if (IsValidImageFile(localPath)) {
            return localPath;
        }
        DeleteFileW(localPath.c_str());
    }
    return L"";
}

std::wstring WebViewHost::ResolveWebImageUrl(const std::wstring& inputUrl) {
    std::wstring url = inputUrl;
    if (url.empty()) return L"";

    // Check YouTube
    std::wstring ytId = ExtractYouTubeVideoId(url);
    if (!ytId.empty()) {
        return L"https://img.youtube.com/vi/" + ytId + L"/maxresdefault.jpg";
    }

    // Upgrade Pinterest thumbnails to 736x (safe high-res preview without 403 Forbidden)
    if (url.find(L"pinimg.com/") != std::wstring::npos) {
        const wchar_t* patterns[] = { L"/236x/", L"/474x/", L"/564x/" };
        for (const wchar_t* pat : patterns) {
            size_t pos = url.find(pat);
            if (pos != std::wstring::npos) {
                url.replace(pos, wcslen(pat), L"/736x/");
                break;
            }
        }
        return url;
    }

    // Check if it is a Pinterest Pin page link (pinterest.com/pin/... or pin.it/...)
    if (url.find(L"pinterest.com/pin/") != std::wstring::npos ||
        (url.find(L"pinterest.") != std::wstring::npos && url.find(L"/pin/") != std::wstring::npos) ||
        url.find(L"pin.it/") != std::wstring::npos) {
        
        std::wstring oembedPath;
        // Try to extract numeric Pin ID (e.g. from https://id.pinterest.com/pin/1102044971353095932/)
        std::wsmatch match;
        std::wregex pinIdRegex(L"/pin/(\\d+)");
        if (std::regex_search(url, match, pinIdRegex) && match.size() > 1) {
            std::wstring pinId = match[1].str();
            oembedPath = L"/oembed.json?url=https%3A%2F%2Fwww.pinterest.com%2Fpin%2F" + pinId + L"%2F";
        } else {
            // General Pinterest or pin.it URL: URL-encode the target URL
            oembedPath = L"/oembed.json?url=" + UrlEncode(url);
        }

        std::string jsonResp = WinHttpGetUtf8(L"www.pinterest.com", oembedPath, true);
        if (!jsonResp.empty()) {
            std::wstring wJson(jsonResp.begin(), jsonResp.end());
            std::wstring thumb = JsonUtil::ExtractString(wJson, L"thumbnail_url");
            if (!thumb.empty()) {
                return ResolveWebImageUrl(thumb);
            }
        }

        // If oEmbed failed for a web page URL, do not return the web page HTML URL!
        return L"";
    }

    return url;
}

void WebViewHost::HandleJsonCommand(const std::wstring& json) {
    std::wstring action = JsonUtil::ExtractString(json, L"action");

    if (action == L"window_minimize") {
        ShowWindow(m_hWnd, SW_MINIMIZE);
    }
    else if (action == L"window_maximize") {
        WINDOWPLACEMENT wp = { sizeof(wp) };
        GetWindowPlacement(m_hWnd, &wp);
        if (wp.showCmd == SW_SHOWMAXIMIZED) {
            ShowWindow(m_hWnd, SW_RESTORE);
        } else {
            ShowWindow(m_hWnd, SW_MAXIMIZE);
        }
    }
    else if (action == L"window_close") {
        DestroyWindow(m_hWnd);
    }
    else if (action == L"window_start_drag") {
        ReleaseCapture();
        SendMessage(m_hWnd, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }
    else if (action == L"window_start_resize") {
        std::wstring edge = JsonUtil::ExtractString(json, L"edge");
        WPARAM hit = HTBOTTOMRIGHT;
        if (edge == L"right") hit = HTRIGHT;
        else if (edge == L"left") hit = HTLEFT;
        else if (edge == L"bottom") hit = HTBOTTOM;
        else if (edge == L"top") hit = HTTOP;
        else if (edge == L"bottom_right") hit = HTBOTTOMRIGHT;
        else if (edge == L"bottom_left") hit = HTBOTTOMLEFT;
        else if (edge == L"top_right") hit = HTTOPRIGHT;
        else if (edge == L"top_left") hit = HTTOPLEFT;

        ReleaseCapture();
        SendMessage(m_hWnd, WM_NCLBUTTONDOWN, hit, 0);
    }
    else if (action == L"toggle_always_on_top") {
        SetAlwaysOnTop(!m_isAlwaysOnTop);
    }
    else if (action == L"set_always_on_top") {
        bool top = JsonUtil::ExtractBool(json, L"value", false);
        SetAlwaysOnTop(top);
    }
    else if (action == L"set_opacity") {
        double val = JsonUtil::ExtractNumber(json, L"value", 1.0);
        SetOpacity((float)val);
    }
    else if (action == L"get_system_fonts") {
        HDC hdc = GetDC(m_hWnd);
        std::set<std::wstring> fontFamilies;
        if (hdc) {
            LOGFONTW lf = { 0 };
            lf.lfCharSet = DEFAULT_CHARSET;
            EnumFontFamiliesExW(hdc, &lf, (FONTENUMPROCW)+[](const LOGFONTW* lfItem, const TEXTMETRICW*, DWORD, LPARAM lParam) -> int {
                auto* fonts = reinterpret_cast<std::set<std::wstring>*>(lParam);
                if (lfItem && lfItem->lfFaceName[0] != L'@' && lfItem->lfFaceName[0] != L'\0') {
                    fonts->insert(lfItem->lfFaceName);
                }
                return 1;
            }, reinterpret_cast<LPARAM>(&fontFamilies), 0);
            ReleaseDC(m_hWnd, hdc);
        }

        std::wstringstream resp;
        resp << L"{\"type\":\"system_fonts_list\",\"fonts\":[";
        bool first = true;
        for (const auto& f : fontFamilies) {
            if (!first) resp << L",";
            first = false;
            resp << L"\"" << JsonUtil::EscapeString(f) << L"\"";
        }
        resp << L"]}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"download_image") {
        std::wstring url = JsonUtil::ExtractString(json, L"url");
        std::wstring id = JsonUtil::ExtractString(json, L"id");

        // Resolve Pinterest / web URLs to full original masters first
        std::wstring resolvedUrl = ResolveWebImageUrl(url);
        std::wstring localPath = DownloadImageToLocalCache(resolvedUrl);
        std::wstring localFileName = L"";
        if (!localPath.empty()) {
            size_t slash = localPath.rfind(L'\\');
            if (slash != std::wstring::npos) {
                localFileName = localPath.substr(slash + 1);
            }
        }
        std::wstring localWebUrl = localFileName.empty() ? L"" : (L"https://dropboard-cache.local/" + localFileName);

        std::wstring b64Data = FileToBase64DataUrl(localPath);

        std::wstringstream resp;
        resp << L"{\"type\":\"download_completed\","
             << L"\"id\":\"" << JsonUtil::EscapeString(id) << L"\","
             << L"\"success\":" << (!localPath.empty() ? L"true" : L"false") << L","
             << L"\"resolvedUrl\":\"" << JsonUtil::EscapeString(resolvedUrl) << L"\","
             << L"\"localPath\":\"" << JsonUtil::EscapeString(localPath) << L"\","
             << L"\"localWebUrl\":\"" << JsonUtil::EscapeString(localWebUrl) << L"\","
             << L"\"imageData\":\"" << JsonUtil::EscapeString(b64Data) << L"\"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"copy_file_to_clipboard") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        std::wstring imageData = JsonUtil::ExtractString(json, L"imageData");
        filePath = EnsureDiskFileForExternalApp(filePath, imageData, GetCacheDirectory());
        bool ok = ExternalAppIntegration::CopyFileToClipboard(m_hWnd, filePath);
        std::wstringstream resp;
        resp << L"{\"type\":\"clipboard_result\",\"success\":" << (ok ? L"true" : L"false") << L"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"reveal_in_explorer") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        std::wstring imageData = JsonUtil::ExtractString(json, L"imageData");
        filePath = EnsureDiskFileForExternalApp(filePath, imageData, GetCacheDirectory());
        ExternalAppIntegration::RevealInExplorer(filePath);
    }
    else if (action == L"open_default") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        std::wstring imageData = JsonUtil::ExtractString(json, L"imageData");
        filePath = EnsureDiskFileForExternalApp(filePath, imageData, GetCacheDirectory());
        std::wstring msg;
        ExternalAppIntegration::OpenWithDefaultApp(filePath, msg);
    }
    else if (action == L"send_to_ae") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        std::wstring imageData = JsonUtil::ExtractString(json, L"imageData");
        filePath = EnsureDiskFileForExternalApp(filePath, imageData, GetCacheDirectory());
        std::wstring msg;
        bool ok = ExternalAppIntegration::SendToAfterEffects(filePath, msg);
        std::wstringstream resp;
        resp << L"{\"type\":\"software_result\","
             << L"\"app\":\"after_effects\","
             << L"\"success\":" << (ok ? L"true" : L"false") << L","
             << L"\"message\":\"" << JsonUtil::EscapeString(msg) << L"\"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"snap_video_frame") {
        double clientX = JsonUtil::ExtractNumber(json, L"clientX", 0.0);
        double clientY = JsonUtil::ExtractNumber(json, L"clientY", 0.0);
        double width = JsonUtil::ExtractNumber(json, L"width", 480.0);
        double height = JsonUtil::ExtractNumber(json, L"height", 270.0);
        bool sendToAe = (json.find(L"\"sendToAe\":true") != std::wstring::npos);
        std::wstring cardId = JsonUtil::ExtractString(json, L"cardId");
        std::wstring rawImageData = JsonUtil::ExtractString(json, L"imageData");

        std::wstring cacheDir = GetCacheDirectory();
        static int snapCounter = 1;
        std::wstring fileName = L"snap_yt_" + std::to_wstring(time(nullptr)) + L"_" + std::to_wstring(snapCounter++) + L".png";
        std::wstring fullPath = cacheDir + L"\\" + fileName;

        bool ok = false;
        if (!rawImageData.empty()) {
            size_t comma = rawImageData.find(L',');
            std::wstring b64 = (comma != std::wstring::npos) ? rawImageData.substr(comma + 1) : rawImageData;
            std::string b64Utf8 = JsonUtil::WideToUtf8(b64);
            ok = DecodeBase64ToFile(b64Utf8, fullPath);
        } else {
            // Convert client coordinates to screen coordinates
            POINT pt = { (LONG)clientX, (LONG)clientY };
            ClientToScreen(m_hWnd, &pt);
            ok = CaptureScreenRectToPng(pt.x, pt.y, (int)width, (int)height, fullPath);
        }

        std::wstring aeMsg = L"";
        bool aeOk = false;
        if (ok && sendToAe) {
            aeOk = ExternalAppIntegration::SendToAfterEffects(fullPath, aeMsg);
        }

        std::wstring localWebUrl = ok ? (L"https://dropboard-cache.local/" + fileName) : L"";
        std::wstring b64Data = ok ? (!rawImageData.empty() ? rawImageData : FileToBase64DataUrl(fullPath)) : L"";

        std::wstringstream resp;
        resp << L"{\"type\":\"frame_snapped\","
             << L"\"cardId\":\"" << JsonUtil::EscapeString(cardId) << L"\","
             << L"\"success\":" << (ok ? L"true" : L"false") << L","
             << L"\"localPath\":\"" << JsonUtil::EscapeString(fullPath) << L"\","
             << L"\"localWebUrl\":\"" << JsonUtil::EscapeString(localWebUrl) << L"\","
             << L"\"imageData\":\"" << JsonUtil::EscapeString(b64Data) << L"\","
             << L"\"sendToAe\":" << (sendToAe ? L"true" : L"false") << L","
             << L"\"aeSuccess\":" << (aeOk ? L"true" : L"false") << L"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"export_to_ae_comp") {
        AeExportCompPayload payload;
        payload.mode = JsonUtil::ExtractString(json, L"mode");
        if (payload.mode.empty()) payload.mode = L"group_comp";
        payload.compName = JsonUtil::ExtractString(json, L"compName");
        payload.compWidth = JsonUtil::ExtractNumber(json, L"compWidth", 1920.0);
        payload.compHeight = JsonUtil::ExtractNumber(json, L"compHeight", 1080.0);
        payload.notesText = JsonUtil::ExtractString(json, L"notesText");
        payload.fontText = JsonUtil::ExtractString(json, L"fontText");
        payload.fontFamily = JsonUtil::ExtractString(json, L"fontFamily");
        payload.sampleText = JsonUtil::ExtractString(json, L"sampleText");
        payload.vfxText = JsonUtil::ExtractString(json, L"vfxText");

        // Parse items array: "items": [ { ... }, { ... } ]
        size_t itemsPos = json.find(L"\"items\"");
        if (itemsPos != std::wstring::npos) {
            size_t arrStart = json.find(L'[', itemsPos);
            size_t arrEnd = (arrStart != std::wstring::npos) ? json.find(L']', arrStart) : std::wstring::npos;
            if (arrStart != std::wstring::npos && arrEnd != std::wstring::npos) {
                size_t curr = arrStart + 1;
                while (curr < arrEnd) {
                    size_t objStart = json.find(L'{', curr);
                    if (objStart == std::wstring::npos || objStart >= arrEnd) break;
                    size_t objEnd = json.find(L'}', objStart);
                    if (objEnd == std::wstring::npos || objEnd > arrEnd) break;

                    std::wstring itemJson = json.substr(objStart, objEnd - objStart + 1);
                    std::wstring itemFilePath = JsonUtil::ExtractString(itemJson, L"filePath");
                    std::wstring itemImageData = JsonUtil::ExtractString(itemJson, L"imageData");
                    itemFilePath = EnsureDiskFileForExternalApp(itemFilePath, itemImageData, GetCacheDirectory());

                    AeExportItem item;
                    item.filePath = itemFilePath;
                    item.relX = JsonUtil::ExtractNumber(itemJson, L"relX", 0.0);
                    item.relY = JsonUtil::ExtractNumber(itemJson, L"relY", 0.0);
                    item.width = JsonUtil::ExtractNumber(itemJson, L"width", 0.0);
                    item.height = JsonUtil::ExtractNumber(itemJson, L"height", 0.0);

                    payload.items.push_back(item);
                    curr = objEnd + 1;
                }
            }
        }

        std::wstring msg;
        bool ok = ExternalAppIntegration::ExportToAfterEffectsAdvanced(payload, msg);
        std::wstringstream resp;
        resp << L"{\"type\":\"software_result\","
             << L"\"app\":\"after_effects\","
             << L"\"success\":" << (ok ? L"true" : L"false") << L","
             << L"\"message\":\"" << JsonUtil::EscapeString(msg) << L"\"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"send_to_photoshop") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        std::wstring imageData = JsonUtil::ExtractString(json, L"imageData");
        filePath = EnsureDiskFileForExternalApp(filePath, imageData, GetCacheDirectory());
        std::wstring msg;
        bool ok = ExternalAppIntegration::SendToPhotoshop(filePath, msg);
        std::wstringstream resp;
        resp << L"{\"type\":\"software_result\","
             << L"\"app\":\"photoshop\","
             << L"\"success\":" << (ok ? L"true" : L"false") << L","
             << L"\"message\":\"" << JsonUtil::EscapeString(msg) << L"\"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"send_to_custom") {
        std::wstring exePath = JsonUtil::ExtractString(json, L"exePath");
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        std::wstring imageData = JsonUtil::ExtractString(json, L"imageData");
        filePath = EnsureDiskFileForExternalApp(filePath, imageData, GetCacheDirectory());
        std::wstring msg;
        bool ok = ExternalAppIntegration::SendToCustomApp(exePath, filePath, msg);
        std::wstringstream resp;
        resp << L"{\"type\":\"software_result\","
             << L"\"app\":\"custom\","
             << L"\"success\":" << (ok ? L"true" : L"false") << L","
             << L"\"message\":\"" << JsonUtil::EscapeString(msg) << L"\"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"get_software_list") {
        auto list = ExternalAppIntegration::DetectInstalledSoftware();
        std::wstringstream resp;
        resp << L"{\"type\":\"software_list\",\"items\":[";
        for (size_t i = 0; i < list.size(); ++i) {
            resp << L"{\"id\":\"" << list[i].id << L"\","
                 << L"\"name\":\"" << list[i].name << L"\","
                 << L"\"detected\":" << (list[i].isDetected ? L"true" : L"false") << L"}";
            if (i + 1 < list.size()) resp << L",";
        }
        resp << L"]}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"save_board_dialog") {
        std::wstring data = JsonUtil::ExtractString(json, L"data");
        std::wstring name = JsonUtil::ExtractString(json, L"name");
        if (name.empty()) name = L"Untitled_Board";
        // Sanitize filename characters
        for (auto& ch : name) {
            if (ch == L'<' || ch == L'>' || ch == L':' || ch == L'"' || ch == L'/' || ch == L'\\' || ch == L'|' || ch == L'?' || ch == L'*') {
                ch = L'_';
            }
        }
        std::wstring defaultFileName = name + L".dropboard";

        wchar_t szFile[MAX_PATH] = { 0 };
        wcsncpy_s(szFile, defaultFileName.c_str(), _TRUNCATE);
        OPENFILENAMEW ofn = { sizeof(ofn) };
        ofn.hwndOwner = m_hWnd;
        ofn.lpstrFile = szFile;
        ofn.nMaxFile = sizeof(szFile) / sizeof(wchar_t);
        ofn.lpstrFilter = L"DropBoard Files (*.dropboard)\0*.dropboard\0JSON Files (*.json)\0*.json\0All Files (*.*)\0*.*\0";
        ofn.nFilterIndex = 1;
        ofn.lpstrDefExt = L"dropboard";
        ofn.Flags = OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT;

        if (GetSaveFileNameW(&ofn)) {
            std::wstring embeddedData = EnsureEmbeddedImagesInBoardJson(data);
            std::string utf8Data = JsonUtil::WideToUtf8(embeddedData);
            std::ofstream out(ofn.lpstrFile, std::ios::binary | std::ios::trunc);
            if (out.is_open()) {
                out.write(utf8Data.data(), utf8Data.size());
                out.close();
                std::wstringstream resp;
                resp << L"{\"type\":\"board_saved\",\"success\":true,\"filePath\":\""
                     << JsonUtil::EscapeString(ofn.lpstrFile) << L"\"}";
                PostMessageToWeb(resp.str());
            }
        }
    }
    else if (action == L"save_board_direct") {
        std::wstring data = JsonUtil::ExtractString(json, L"data");
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        if (filePath.empty()) {
            filePath = GetSessionFilePath();
        }
        if (!filePath.empty()) {
            std::wstring embeddedData = EnsureEmbeddedImagesInBoardJson(data);
            std::string utf8Data = JsonUtil::WideToUtf8(embeddedData);
            std::ofstream out(filePath, std::ios::binary | std::ios::trunc);
            if (out.is_open()) {
                out.write(utf8Data.data(), utf8Data.size());
                out.close();
                std::wstringstream resp;
                resp << L"{\"type\":\"board_saved\",\"success\":true,\"direct\":true,\"filePath\":\""
                     << JsonUtil::EscapeString(filePath) << L"\"}";
                PostMessageToWeb(resp.str());
            } else {
                std::wstringstream resp;
                resp << L"{\"type\":\"board_saved\",\"success\":false,\"error\":\"Cannot open file for writing\"}";
                PostMessageToWeb(resp.str());
            }
        }
    }
    else if (action == L"load_board_dialog") {
        wchar_t szFile[MAX_PATH] = {0};
        OPENFILENAMEW ofn = { sizeof(ofn) };
        ofn.hwndOwner = m_hWnd;
        ofn.lpstrFile = szFile;
        ofn.nMaxFile = sizeof(szFile) / sizeof(wchar_t);
        ofn.lpstrFilter = L"DropBoard Files (*.dropboard)\0*.dropboard\0JSON Files (*.json)\0*.json\0All Files (*.*)\0*.*\0";
        ofn.nFilterIndex = 1;
        ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;

        if (GetOpenFileNameW(&ofn)) {
            std::ifstream in(ofn.lpstrFile, std::ios::binary);
            if (in.is_open()) {
                std::stringstream ss;
                ss << in.rdbuf();
                in.close();
                std::wstring wContent = JsonUtil::Utf8ToWide(ss.str());
                std::wstringstream resp;
                resp << L"{\"type\":\"board_loaded\",\"success\":true,\"filePath\":\""
                     << JsonUtil::EscapeString(ofn.lpstrFile) << L"\",\"content\":\""
                     << JsonUtil::EscapeString(wContent) << L"\"}";
                PostMessageToWeb(resp.str());
            }
        }
    }
    else if (action == L"register_file_association") {
        EnsureFileAssociationRegistered();
        std::wstringstream resp;
        resp << L"{\"type\":\"file_assoc_registered\",\"success\":true}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"load_board_direct") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        LoadBoardFromFile(filePath);
    }
    else if (action == L"load_session_board") {
        std::wstring sessionPath = GetSessionFilePath();
        if (GetFileAttributesW(sessionPath.c_str()) != INVALID_FILE_ATTRIBUTES) {
            LoadBoardFromFile(sessionPath);
        } else {
            std::wstringstream resp;
            resp << L"{\"type\":\"board_loaded\",\"success\":false,\"error\":\"No session file found\"}";
            PostMessageToWeb(resp.str());
        }
    }
    else if (action == L"app_ready") {
        if (!m_initialFilePath.empty()) {
            LoadBoardFromFile(m_initialFilePath);
            m_initialFilePath.clear();
        }
    }
    else if (action == L"open_external_url") {
        std::wstring url = JsonUtil::ExtractString(json, L"url");
        if (!url.empty()) {
            ShellExecuteW(nullptr, L"open", url.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
        }
    }
    else if (action == L"register_file_association") {
        EnsureFileAssociationRegistered();
        std::wstringstream resp;
        resp << L"{\"type\":\"file_assoc_registered\",\"success\":true}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"load_board_direct") {
        std::wstring filePath = JsonUtil::ExtractString(json, L"filePath");
        LoadBoardFromFile(filePath);
    }
    else if (action == L"clear_image_cache") {
        std::wstring cacheDir = GetCacheDirectory();
        WIN32_FIND_DATAW ffd;
        HANDLE hFind = FindFirstFileW((cacheDir + L"\\*").c_str(), &ffd);
        int deletedCount = 0;
        if (hFind != INVALID_HANDLE_VALUE) {
            do {
                if (!(ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
                    std::wstring fullPath = cacheDir + L"\\" + ffd.cFileName;
                    if (DeleteFileW(fullPath.c_str())) {
                        deletedCount++;
                    }
                }
            } while (FindNextFileW(hFind, &ffd) != 0);
            FindClose(hFind);
        }
        std::wstringstream resp;
        resp << L"{\"type\":\"cache_cleared\",\"count\":" << deletedCount << L"}";
        PostMessageToWeb(resp.str());
    }
    else if (action == L"open_cache_folder") {
        std::wstring cacheDir = GetCacheDirectory();
        ShellExecuteW(nullptr, L"open", cacheDir.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
    }
    else if (action == L"open_extension_folder") {
        wchar_t exePath[MAX_PATH];
        GetModuleFileNameW(nullptr, exePath, MAX_PATH);
        std::wstring dir = exePath;
        size_t lastSlash = dir.find_last_of(L"\\/");
        if (lastSlash != std::wstring::npos) {
            dir = dir.substr(0, lastSlash);
        }
        std::wstring extDir = dir + L"\\extension";
        DWORD attribs = GetFileAttributesW(extDir.c_str());
        if (attribs == INVALID_FILE_ATTRIBUTES || !(attribs & FILE_ATTRIBUTE_DIRECTORY)) {
            extDir = dir + L"\\..\\..\\extension";
            attribs = GetFileAttributesW(extDir.c_str());
            if (attribs == INVALID_FILE_ATTRIBUTES || !(attribs & FILE_ATTRIBUTE_DIRECTORY)) {
                extDir = L"extension";
            }
        }
        ShellExecuteW(nullptr, L"open", extDir.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
    }
}

void WebViewHost::LoadBoardFromFile(const std::wstring& filePath) {
    if (filePath.empty()) return;
    std::ifstream in(filePath, std::ios::binary);
    if (in.is_open()) {
        std::stringstream ss;
        ss << in.rdbuf();
        in.close();
        std::wstring wContent = JsonUtil::Utf8ToWide(ss.str());
        std::wstringstream resp;
        resp << L"{\"type\":\"board_loaded\",\"success\":true,\"direct\":true,\"filePath\":\""
             << JsonUtil::EscapeString(filePath) << L"\",\"content\":\""
             << JsonUtil::EscapeString(wContent) << L"\"}";
        PostMessageToWeb(resp.str());
    } else {
        std::wstringstream resp;
        resp << L"{\"type\":\"board_loaded\",\"success\":false,\"filePath\":\""
             << JsonUtil::EscapeString(filePath) << L"\",\"error\":\"Cannot open file\"}";
        PostMessageToWeb(resp.str());
    }
}


