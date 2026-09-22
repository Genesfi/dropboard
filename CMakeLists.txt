cmake_minimum_required(VERSION 3.20)
project(DropBoard VERSION 1.0.0 LANGUAGES CXX RC)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_compile_definitions(WIN32_LEAN_AND_MEAN)

# Source files
set(SOURCES
    src/main.cpp
    src/WebViewHost.h
    src/WebViewHost.cpp
    src/ExternalAppIntegration.h
    src/ExternalAppIntegration.cpp
    src/LocalHttpServer.h
    src/LocalHttpServer.cpp
    src/resource.h
    src/app.rc
    src/app.manifest
)

# Target executable (Win32 GUI application)
add_executable(DropBoard WIN32 ${SOURCES})

# Include directories
target_include_directories(DropBoard PRIVATE
    src
    ${CMAKE_CURRENT_SOURCE_DIR}/packages/Microsoft.Web.WebView2/build/native/include
)

# WebView2 static loader library
set(WEBVIEW2_LIB_DIR "${CMAKE_CURRENT_SOURCE_DIR}/packages/Microsoft.Web.WebView2/build/native/x64")
target_link_directories(DropBoard PRIVATE ${WEBVIEW2_LIB_DIR})

target_link_libraries(DropBoard PRIVATE
    WebView2LoaderStatic
    dwmapi
    shlwapi
    urlmon
    winhttp
    ws2_32
    ole32
    shell32
    user32
    gdi32
    gdiplus
    comdlg32
    advapi32
)

# Copy assets folder to output directory after build
add_custom_command(TARGET DropBoard POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory
        "${CMAKE_CURRENT_SOURCE_DIR}/assets"
        "$<TARGET_FILE_DIR:DropBoard>/assets"
    COMMENT "Copying DropBoard assets to binary directory..."
)
