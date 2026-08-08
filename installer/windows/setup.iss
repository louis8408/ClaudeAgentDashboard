; Inno Setup script for Claude Agent Dashboard.
; Built by .github/workflows/release.yml against a self-contained win-x64
; `dotnet publish` output. Compile locally with:
;   ISCC.exe /DMyAppVersion=0.1.0 /DSourceDir=..\..\publish\win-x64 installer\windows\setup.iss

#define MyAppName "Claude Agent Dashboard"
#define MyAppPublisher "Louis Esterhuizen"
#define MyAppExeName "ClaudeAgentDashboard.Presentation.exe"
; Must exactly match WindowsToastNotifier's AppUserModelId constant — the Start Menu
; shortcut's AppUserModelID has to agree with the one the app raises toasts under, or
; Windows silently drops every toast (see WindowsToastShortcutRegistrar's doc comment).
#define AppUserModelId "ClaudeAgentDashboard"

#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\artifacts\windows"
#endif

[Setup]
; Fixed GUID — keep stable across releases so Windows treats future versions as upgrades
; of the same product rather than a separate install.
AppId={{6F1E6E2A-1D0B-4C0E-9C7E-6B0D8B2B9F3A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\ClaudeAgentDashboard
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; The app itself never needs elevation (its login-item and toast registrations are both
; per-user, under HKCU/%AppData%), so installing per-user avoids a UAC prompt entirely.
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename=ClaudeAgentDashboardSetup-{#MyAppVersion}
OutputDir={#OutputDir}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
; Read from the source tree, not the publish output — tray-icon.ico is an embedded
; AvaloniaResource (baked into the assembly), not a loose file dotnet publish copies out.
SetupIconFile=..\..\src\ClaudeAgentDashboard.Presentation\Assets\tray-icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Same path WindowsToastNotifier's self-registration would use ({userstartmenu} resolves to
; %AppData%\Microsoft\Windows\Start Menu\Programs) — creating it here with the correct
; AppUserModelID up front means toasts work from first launch, and the app's own
; EnsureRegistered() no-ops (it only acts when the shortcut is missing).
Name: "{userstartmenu}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; AppUserModelID: "{#AppUserModelId}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; AppUserModelID: "{#AppUserModelId}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
