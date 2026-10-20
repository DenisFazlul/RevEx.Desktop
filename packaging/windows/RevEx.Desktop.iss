#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\..\artifacts\publish"
#endif
#ifndef InstallerOutputDir
  #define InstallerOutputDir "..\..\artifacts\installer"
#endif

[Setup]
AppId={{B4D47750-BD67-4B63-81F1-06DD182A87B3}
AppName=RevEx
AppVersion={#AppVersion}
AppPublisher=RevEx
DefaultDirName={localappdata}\Programs\RevEx
DefaultGroupName=RevEx
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#InstallerOutputDir}
OutputBaseFilename=RevEx-Setup-{#AppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\RevEx.Desktop.exe
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\RevEx"; Filename: "{app}\RevEx.Desktop.exe"
Name: "{autodesktop}\RevEx"; Filename: "{app}\RevEx.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\RevEx.Desktop.exe"; Description: "{cm:LaunchProgram,RevEx}"; Flags: nowait postinstall skipifsilent
