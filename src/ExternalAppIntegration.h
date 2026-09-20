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

struct AeExportItem {
    std::wstring filePath;
    double relX = 0.0;
    double relY = 0.0;
    double width = 0.0;
    double height = 0.0;
};

struct AeExportCompPayload {
    std::wstring mode; // L"group_comp" or L"loose_photos"
    std::wstring compName;
    double compWidth = 1920.0;
    double compHeight = 1080.0;
    std::wstring notesText;
    std::wstring fontText;
    std::wstring fontFamily;
    std::wstring sampleText;
    std::wstring vfxText;
    std::vector<AeExportItem> items;
};

class ExternalAppIntegration {
public:
    static std::vector<SoftwareTarget> DetectInstalledSoftware();
    static bool SendToAfterEffects(const std::wstring& imagePath, std::wstring& outMessage);
    static bool ExportToAfterEffectsAdvanced(const AeExportCompPayload& payload, std::wstring& outMessage);
    static bool SendToPhotoshop(const std::wstring& imagePath, std::wstring& outMessage);
    static bool SendToCustomApp(const std::wstring& exePath, const std::wstring& imagePath, std::wstring& outMessage);
    static bool OpenWithDefaultApp(const std::wstring& imagePath, std::wstring& outMessage);
    static bool RevealInExplorer(const std::wstring& imagePath);
    static bool CopyFileToClipboard(HWND hWnd, const std::wstring& filePath);
    
private:
    static std::wstring FindExecutableInRegistry(const std::wstring& appName);
    static std::wstring FindAfterEffectsExe();
    static std::wstring FindAfterEffectsCmd();
    static std::wstring FindPhotoshopExe();
    static std::wstring FindBlenderExe();
};
