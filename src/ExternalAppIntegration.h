#pragma once
#include <windows.h>
#include <shellapi.h>
#include <string>
#include <vector>

struct SoftwareTarget {
    std::wstring id;
    std::wstring name;
    std::wstring exePath;
    bool isDetected;
};

class ExternalAppIntegration {
public:
    static std::vector<SoftwareTarget> DetectInstalledSoftware();
    static bool SendToAfterEffects(const std::wstring& imagePath, std::wstring& outMessage);
    static bool SendToPhotoshop(const std::wstring& imagePath, std::wstring& outMessage);
    static bool SendToCustomApp(const std::wstring& exePath, const std::wstring& imagePath, std::wstring& outMessage);
    static bool OpenWithDefaultApp(const std::wstring& imagePath, std::wstring& outMessage);
    static bool RevealInExplorer(const std::wstring& imagePath);
    static bool CopyFileToClipboard(HWND hWnd, const std::wstring& filePath);
    
private:
    static std::wstring FindExecutableInRegistry(const std::wstring& appName);
    static std::wstring FindAfterEffectsExe();
    static std::wstring FindPhotoshopExe();
    static std::wstring FindBlenderExe();
};
