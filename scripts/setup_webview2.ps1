$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$targetDir = Join-Path $rootDir "packages\Microsoft.Web.WebView2"

if (Test-Path (Join-Path $targetDir "build\native\include\WebView2.h")) {
    Write-Host "WebView2 SDK is already installed at $targetDir"
    exit 0
}

Write-Host "Downloading WebView2 SDK..."
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
}

$zipPath = Join-Path $targetDir "webview2.zip"
$url = "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.2903.40/microsoft.web.webview2.1.0.2903.40.nupkg"

Invoke-WebRequest -Uri $url -OutFile $zipPath
Write-Host "Extracting WebView2 SDK..."
Expand-Archive -Path $zipPath -DestinationPath $targetDir -Force
Remove-Item -Force $zipPath

Write-Host "WebView2 SDK installed successfully."
