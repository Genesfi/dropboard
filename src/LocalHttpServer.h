#pragma once
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <string>
#include <thread>
#include <atomic>
#include <functional>

#pragma comment(lib, "ws2_32.lib")

class LocalHttpServer {
public:
    using AddImageCallback = std::function<void(const std::wstring& url, const std::wstring& title)>;

    LocalHttpServer();
    ~LocalHttpServer();

    bool Start(int port, AddImageCallback onAddImage);
    void Stop();

private:
    void ServerThread();
    void HandleClient(SOCKET clientSock);

    int m_port;
    SOCKET m_listenSock;
    std::thread m_thread;
    std::atomic<bool> m_running;
    AddImageCallback m_onAddImage;
};
