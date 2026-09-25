# =====================================================================
# DropBoard Installer Compiler Script
# Compiles installer\DropBoard_Setup.iss into dist\DropBoard-Setup-v1.0.0.exe
# =====================================================================

$projectRoot = Split-Path -Parent $PSScriptRoot
$nativeProj = Join-Path $projectRoot "DropBoard.Native\DropBoard.Native.csproj"
$publishDir = Join-Path $projectRoot "DropBoard.Native\bin\Release\publish"
$issFile = Join-Path $projectRoot "installer\DropBoard_Setup.iss"
$distDir = Join-Path $projectRoot "dist"

# 1. Publish DropBoard.Native (Self-Contained x64 Release)
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Publishing DropBoard.Native (.NET 9 Win-x64 Release)..." -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

dotnet publish $nativeProj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
Write-Host "Publish complete at: $publishDir`n" -ForegroundColor Green

# 2. Ensure dist directory exists
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

# 3. Locate Inno Setup Compiler (ISCC.exe)
$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $isccPaths) {
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if (-not $iscc) {
    $cmd = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
    if ($cmd) {
        $iscc = $cmd.Source
    }
}

if (-not $iscc) {
    Write-Error "Inno Setup Compiler (ISCC.exe) not found on this system. Please install Inno Setup 6."
    exit 1
}

Write-Host "Using Inno Setup Compiler: $iscc"
Write-Host "Compiling $issFile..."

# 3. Compile Installer
& $iscc $issFile

if ($LASTEXITCODE -eq 0) {
    $outputExe = Join-Path $distDir "DropBoard-Setup-v1.0.0.exe"
    if (Test-Path $outputExe) {
        $sizeMB = [math]::Round((Get-Item $outputExe).Length / 1MB, 2)
        Write-Host "`n========================================================" -ForegroundColor Green
        Write-Host "SUCCESS! Installer created successfully:" -ForegroundColor Green
        Write-Host "Path: $outputExe" -ForegroundColor Cyan
        Write-Host "Size: $sizeMB MB" -ForegroundColor Cyan
        Write-Host "========================================================`n" -ForegroundColor Green
    }
} else {
    Write-Error "Installer compilation failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
