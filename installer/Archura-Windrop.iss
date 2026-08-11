#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "Archura Windrop"
#define AppPublisher "Archura"
#define AppExeName "Archura.Windrop.exe"
#define AppUrl "https://github.com/AybarsBarut/Archura-Windrop"

[Setup]
AppId={{B89975A4-38C7-4D3B-A94D-7DA43474C625}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases/latest
DefaultDirName={autopf}\Archura Windrop
DefaultGroupName=Archura Windrop
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\artifacts\installer
OutputBaseFilename=Archura-Windrop-Setup-v{#AppVersion}-win-x64
SetupIconFile=..\src\Windrop.App\Assets\app-icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
MinVersion=10.0.19041
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Windows Installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Archura Windrop"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Archura Windrop"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=&quot;Archura Windrop IPP&quot;"; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=&quot;Archura Windrop IPP&quot; dir=in action=allow protocol=TCP localport=8631 remoteip=LocalSubnet profile=private,public program=&quot;{app}\{#AppExeName}&quot;"; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=&quot;Archura Windrop mDNS&quot;"; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=&quot;Archura Windrop mDNS&quot; dir=in action=allow protocol=UDP localport=5353 remoteip=LocalSubnet profile=private,public program=&quot;{app}\{#AppExeName}&quot;"; Flags: runhidden waituntilterminated
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=&quot;Archura Windrop IPP&quot;"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveIppFirewallRule"
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=&quot;Archura Windrop mDNS&quot;"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveMdnsFirewallRule"
