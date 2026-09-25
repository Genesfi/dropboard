; =====================================================================
; DropBoard - Professional Windows Setup Script (Inno Setup 6)
; High-performance infinite reference canvas for motion designers & 3D artists
; =====================================================================

#define MyAppName "DropBoard"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Genesfi"
#define MyAppURL "https://github.com/Genesfi/dropboard"
#define MyAppExeName "DropBoard.Native.exe"
#define MyAppFileExt ".dropboard"
#define MyAppProgID "DropBoard.Project"

[Setup]
AppId={{D8145B1C-E7A9-4B52-9A4B-3D6A123C4B89}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=DropBoard-Setup-v1.0.0
SetupIconFile=..\DropBoard.Native\app_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
ChangesAssociations=yes
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=auto
InfoAfterFile=INFO_EXTENSION.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "fileassoc"; Description: "Associate .dropboard project files with DropBoard"; GroupDescription: "File Associations:"; Flags: checkedonce

[Files]
; DropBoard Native C# .NET 9 WPF Self-Contained Release
Source: "..\DropBoard.Native\bin\Release\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Browser Companion Extension (Chrome, Edge, Brave)
Source: "..\extension\*"; DestDir: "{app}\extension"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Browser Extension Folder"; Filename: "{app}\extension"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; 1. File Extension Association (.dropboard)
Root: HKA; Subkey: "Software\Classes\{#MyAppFileExt}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppProgID}"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppFileExt}"; ValueType: string; ValueName: "Content Type"; ValueData: "application/x-dropboard"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppFileExt}"; ValueType: string; ValueName: "PerceivedType"; ValueData: "Document"; Flags: uninsdeletevalue; Tasks: fileassoc

; 2. ProgID Registration
Root: HKA; Subkey: "Software\Classes\{#MyAppProgID}"; ValueType: string; ValueName: ""; ValueData: "DropBoard Project File"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppProgID}"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "DropBoard Project File"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppProgID}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppProgID}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: fileassoc

; 3. Applications List
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey; Tasks: fileassoc

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\extension"; Description: "Open Browser Extension folder (for Chrome, Edge, Brave)"; Flags: postinstall shellexec skipifsilent
Filename: "{app}\extension\HOW_TO_INSTALL.txt"; Description: "View Extension Installation Guide (Notepad)"; Flags: postinstall shellexec skipifsilent
