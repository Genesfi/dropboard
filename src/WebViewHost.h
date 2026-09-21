#pragma once
#include <windows.h>
#include <wrl.h>
#include "WebView2.h"
#include <string>
#include <memory>
#include <functional>

class LocalHttpServer;

using Microsoft::WRL::ComPtr;

#define WM_DROPBOARD_ADD_URL (WM_USER + 201)

struct AddImageRequest {
    std::wstring url;
    std::wstring title;
};

class WebViewHost {
public:
    WebViewHost();
    ~WebViewHost();

    bool Initialize(HWND hWnd, const std::wstring& assetsPath, const std::wstring& initialFilePath = L"");
    void Resize(int width, int height);
    void Close();

    void LoadBoardFromFile(const std::wstring& filePath);
    std::wstring GetInitialFilePath() const { return m_initialFilePath; }

    void PostMessageToWeb(const std::wstring& jsonMessage);
    void QueueAddImage(const std::wstring& url, const std::wstring& title);
    void ProcessAddImage(const std::wstring& url, const std::wstring& title);

    HWND GetHWND() const { return m_hWnd; }
    bool IsAlwaysOnTop() const { return m_isAlwaysOnTop; }
    void SetAlwaysOnTop(bool top);
    void SetOpacity(float opacity);

private:
    void SetupWebMessageHandling();
    void OnWebMessageReceived(const std::wstring& message);
    void HandleJsonCommand(const std::wstring& json);

    // Helpers
    std::wstring GetCacheDirectory();
    std::wstring GetSessionFilePath();
    std::wstring DownloadImageToLocalCache(const std::wstring& url);
    std::wstring ResolveWebImageUrl(const std::wstring& inputUrl);
    std::wstring SaveBase64ToLocalCache(const std::string& base64Str, const std::wstring& ext);

    HWND m_hWnd;
    std::wstring m_assetsPath;
    std::wstring m_initialFilePath;
    bool m_isAlwaysOnTop;
    float m_currentOpacity;

    ComPtr<ICoreWebView2Environment> m_environment;
    ComPtr<ICoreWebView2Controller> m_controller;
    ComPtr<ICoreWebView2> m_webview;
    EventRegistrationToken m_messageToken;
    std::unique_ptr<LocalHttpServer> m_httpServer;
};
