#include "LocalHttpServer.h"
#include <iostream>
#include <sstream>
#include <vector>

LocalHttpServer::LocalHttpServer()
    : m_port(28888)
    , m_listenSock(INVALID_SOCKET)
    , m_running(false) {
}

LocalHttpServer::~LocalHttpServer() {
    Stop();
}

bool LocalHttpServer::Start(int port, AddImageCallback onAddImage) {
    m_port = port;
    m_onAddImage = onAddImage;

    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        return false;
    }

    m_listenSock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (m_listenSock == INVALID_SOCKET) {
        WSACleanup();
        return false;
    }

    // Set SO_REUSEADDR
    int opt = 1;
    setsockopt(m_listenSock, SOL_SOCKET, SO_REUSEADDR, (const char*)&opt, sizeof(opt));

    sockaddr_in addr = {0};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = inet_addr("127.0.0.1");
    addr.sin_port = htons((u_short)m_port);

    int retries = 10;
    while (bind(m_listenSock, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR && retries-- > 0) {
        Sleep(300);
    }

    if (retries < 0) {
        closesocket(m_listenSock);
        m_listenSock = INVALID_SOCKET;
        WSACleanup();
        return false;
    }

    if (listen(m_listenSock, SOMAXCONN) == SOCKET_ERROR) {
        closesocket(m_listenSock);
        m_listenSock = INVALID_SOCKET;
        WSACleanup();
        return false;
    }

    m_running = true;
    m_thread = std::thread(&LocalHttpServer::ServerThread, this);
    return true;
}

void LocalHttpServer::Stop() {
    if (!m_running) return;
    m_running = false;

    if (m_listenSock != INVALID_SOCKET) {
        closesocket(m_listenSock);
        m_listenSock = INVALID_SOCKET;
    }

    if (m_thread.joinable()) {
        m_thread.join();
    }

    WSACleanup();
}

void LocalHttpServer::ServerThread() {
    while (m_running) {
        sockaddr_in clientAddr;
        int clientLen = sizeof(clientAddr);
        SOCKET clientSock = accept(m_listenSock, (sockaddr*)&clientAddr, &clientLen);
        if (clientSock == INVALID_SOCKET) {
            if (!m_running) break;
            continue;
        }

        HandleClient(clientSock);
        closesocket(clientSock);
    }
}

static std::string ExtractJsonValue(const std::string& json, const std::string& key) {
    std::string search = "\"" + key + "\"";
    size_t pos = json.find(search);
    if (pos == std::string::npos) return "";

    size_t colon = json.find(':', pos + search.length());
    if (colon == std::string::npos) return "";

    size_t qStart = json.find('\"', colon + 1);
    if (qStart == std::string::npos) return "";

    size_t qEnd = json.find('\"', qStart + 1);
    if (qEnd == std::string::npos) return "";

    std::string val = json.substr(qStart + 1, qEnd - qStart - 1);
    // Unescape common JSON escapes
    std::string unescaped;
    for (size_t i = 0; i < val.length(); ++i) {
        if (val[i] == '\\' && i + 1 < val.length()) {
            if (val[i+1] == '/') { unescaped += '/'; i++; continue; }
            if (val[i+1] == '\\') { unescaped += '\\'; i++; continue; }
            if (val[i+1] == '\"') { unescaped += '\"'; i++; continue; }
        }
        unescaped += val[i];
    }
    return unescaped;
}

void LocalHttpServer::HandleClient(SOCKET clientSock) {
    char buffer[4096];
    std::string request;

    // Read initial request chunk
    int bytesReceived = recv(clientSock, buffer, sizeof(buffer) - 1, 0);
    if (bytesReceived <= 0) return;
    buffer[bytesReceived] = '\0';
    request.append(buffer, bytesReceived);

    // If POST, check if we need to read more for complete body
    if (request.rfind("POST", 0) == 0) {
        size_t clPos = request.find("Content-Length:");
        if (clPos == std::string::npos) clPos = request.find("content-length:");
        int expectedBodyLen = 0;
        if (clPos != std::string::npos) {
            expectedBodyLen = std::atoi(request.c_str() + clPos + 15);
        }

        size_t headerEnd = request.find("\r\n\r\n");
        while (headerEnd == std::string::npos || (int)(request.length() - (headerEnd + 4)) < expectedBodyLen) {
            bytesReceived = recv(clientSock, buffer, sizeof(buffer) - 1, 0);
            if (bytesReceived <= 0) break;
            request.append(buffer, bytesReceived);
            if (headerEnd == std::string::npos) {
                headerEnd = request.find("\r\n\r\n");
            }
        }
    }

    std::string response;

    const std::string corsHeaders =
        "Access-Control-Allow-Origin: *\r\n"
        "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n"
        "Access-Control-Allow-Headers: Content-Type, Authorization\r\n";

    // Handle OPTIONS Preflight
    if (request.rfind("OPTIONS", 0) == 0) {
        response =
            "HTTP/1.1 204 No Content\r\n"
            + corsHeaders +
            "Content-Length: 0\r\n"
            "Connection: close\r\n\r\n";
    }
    // Handle GET /ping
    else if (request.find("GET /ping") != std::string::npos) {
        std::string body = "{\"status\":\"ready\",\"app\":\"DropBoard\",\"version\":\"1.0.0\"}";
        response =
            "HTTP/1.1 200 OK\r\n"
            + corsHeaders +
            "Content-Type: application/json\r\n"
            "Content-Length: " + std::to_string(body.length()) + "\r\n"
            "Connection: close\r\n\r\n"
            + body;
    }
    // Handle POST /add
    else if (request.find("POST /add") != std::string::npos) {
        // Find end of headers
        size_t headerEnd = request.find("\r\n\r\n");
        std::string body = (headerEnd != std::string::npos) ? request.substr(headerEnd + 4) : "";

        std::string rawUrl = ExtractJsonValue(body, "url");
        std::string rawTitle = ExtractJsonValue(body, "title");

        if (!rawUrl.empty() && m_onAddImage) {
            std::wstring wUrl(rawUrl.begin(), rawUrl.end());
            std::wstring wTitle(rawTitle.begin(), rawTitle.end());
            m_onAddImage(wUrl, wTitle);

            std::string respBody = "{\"success\":true,\"message\":\"Added to DropBoard\"}";
            response =
                "HTTP/1.1 200 OK\r\n"
                + corsHeaders +
                "Content-Type: application/json\r\n"
                "Content-Length: " + std::to_string(respBody.length()) + "\r\n"
                "Connection: close\r\n\r\n"
                + respBody;
        } else {
            std::string respBody = "{\"success\":false,\"message\":\"No URL provided\"}";
            response =
                "HTTP/1.1 400 Bad Request\r\n"
                + corsHeaders +
                "Content-Type: application/json\r\n"
                "Content-Length: " + std::to_string(respBody.length()) + "\r\n"
                "Connection: close\r\n\r\n"
                + respBody;
        }
    } else {
        response =
            "HTTP/1.1 404 Not Found\r\n"
            + corsHeaders +
            "Content-Length: 0\r\n"
            "Connection: close\r\n\r\n";
    }

    send(clientSock, response.c_str(), (int)response.length(), 0);
}
