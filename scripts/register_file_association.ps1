$exePath = "f:\Native Win\DropBoard\build\Release\DropBoard.exe"
$iconPath = "f:\Native Win\DropBoard\build\Release\DropBoard.exe,0"

# 1. Register .dropboard extension
$classesPath = "HKCU:\Software\Classes"
if (-not (Test-Path "$classesPath\.dropboard")) {
    New-Item -Path "$classesPath\.dropboard" -Force | Out-Null
}
Set-ItemProperty -Path "$classesPath\.dropboard" -Name "(Default)" -Value "DropBoard.Project"
Set-ItemProperty -Path "$classesPath\.dropboard" -Name "Content Type" -Value "application/x-dropboard"
Set-ItemProperty -Path "$classesPath\.dropboard" -Name "PerceivedType" -Value "Document"

# 2. Register DropBoard.Project ProgID
if (-not (Test-Path "$classesPath\DropBoard.Project")) {
    New-Item -Path "$classesPath\DropBoard.Project" -Force | Out-Null
}
Set-ItemProperty -Path "$classesPath\DropBoard.Project" -Name "(Default)" -Value "DropBoard Project File"
Set-ItemProperty -Path "$classesPath\DropBoard.Project" -Name "FriendlyTypeName" -Value "DropBoard Project File"

# 3. DefaultIcon
if (-not (Test-Path "$classesPath\DropBoard.Project\DefaultIcon")) {
    New-Item -Path "$classesPath\DropBoard.Project\DefaultIcon" -Force | Out-Null
}
Set-ItemProperty -Path "$classesPath\DropBoard.Project\DefaultIcon" -Name "(Default)" -Value $iconPath

# 4. shell\open\command
if (-not (Test-Path "$classesPath\DropBoard.Project\shell\open\command")) {
    New-Item -Path "$classesPath\DropBoard.Project\shell\open\command" -Force | Out-Null
}
$cmdValue = '"' + $exePath + '" "%1"'
Set-ItemProperty -Path "$classesPath\DropBoard.Project\shell\open\command" -Name "(Default)" -Value $cmdValue

# 5. Applications\DropBoard.exe
if (-not (Test-Path "$classesPath\Applications\DropBoard.exe\shell\open\command")) {
    New-Item -Path "$classesPath\Applications\DropBoard.exe\shell\open\command" -Force | Out-Null
}
Set-ItemProperty -Path "$classesPath\Applications\DropBoard.exe\shell\open\command" -Name "(Default)" -Value $cmdValue

if (-not (Test-Path "$classesPath\Applications\DropBoard.exe\DefaultIcon")) {
    New-Item -Path "$classesPath\Applications\DropBoard.exe\DefaultIcon" -Force | Out-Null
}
Set-ItemProperty -Path "$classesPath\Applications\DropBoard.exe\DefaultIcon" -Name "(Default)" -Value $iconPath

# 6. Explorer UserChoice & FileExts
$fileExtsPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.dropboard"
if (-not (Test-Path "$fileExtsPath\OpenWithList")) {
    New-Item -Path "$fileExtsPath\OpenWithList" -Force | Out-Null
}
Set-ItemProperty -Path "$fileExtsPath\OpenWithList" -Name "a" -Value "DropBoard.exe"
Set-ItemProperty -Path "$fileExtsPath\OpenWithList" -Name "MRUList" -Value "a"

if (-not (Test-Path "$fileExtsPath\OpenWithProgids")) {
    New-Item -Path "$fileExtsPath\OpenWithProgids" -Force | Out-Null
}
Set-ItemProperty -Path "$fileExtsPath\OpenWithProgids" -Name "DropBoard.Project" -Value ([byte[]]@())

# 7. Notify Explorer Shell
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class ShellChangeNotifier {
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
"@
[ShellChangeNotifier]::SHChangeNotify(0x08000000, 0x0000, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Host "DropBoard file association successfully registered with icon: $iconPath"
