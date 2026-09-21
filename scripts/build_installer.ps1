# =====================================================================
# DropBoard Installer Compiler Script
# Compiles installer\DropBoard_Setup.iss into dist\DropBoard-Setup-v1.0.0.exe
# =====================================================================

$projectRoot = Split-Path -Parent $PSScriptRoot
$issFile = Join-Path $projectRoot "installer\DropBoard_Setup.iss"
$distDir = Join-Path $projectRoot "dist"

# 1. Ensure dist directory exists
if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}

# 2. Locate Inno Setup Compiler (ISCC.exe)
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
