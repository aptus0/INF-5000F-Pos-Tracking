#define MyAppName "SAMER Hub"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SAMER"
#define MyAppExeName "SamerHub.Desktop.Avalonia.exe"
#define MyAppIcon "..\Resources\logo.ico"

[Setup]
AppId={{9D55C291-E2A5-4E34-BDE3-6C484D4E1B99}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://samer.com
AppSupportURL=https://samer.com/support
AppUpdatesURL=https://samer.com/updates
AppCopyright=© 2024 SAMER. Tüm hakları saklıdır.
DefaultDirName={autopf}\SAMER Hub
DefaultGroupName=SAMER Hub
AllowNoIcons=yes
PrivilegesRequired=admin
OutputBaseFilename=SAMERHubSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
WizardResizable=yes
WizardImageBackColor=clBlue
WizardImageFile=compiler:WizModernImage.bmp
WizardSmallImageFile=compiler:WizModernSmallImage.bmp
SetupIconFile={#MyAppIcon}
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\desktop\logo.ico
SetupLogging=yes
LogFile={commonappdata}\SAMER Hub\Logs\Setup_{#MyAppVersion}_{code:GetDateTime}.log
DisableStartupPrompt=yes
UsePreviousSetupType=yes
UsePreviousLanguage=yes
UsePreviousTasks=yes
AlwaysShowDirOnReadyPage=yes
AlwaysShowGroupOnReadyPage=yes
ArchitecturesAllowed=x64
VersionInfoVersion={#MyAppVersion}

[Dirs]
Name: "{commonappdata}\SAMER Hub"
Name: "{commonappdata}\SAMER Hub\Logs"
Name: "{commonappdata}\SAMER Hub\Backups"
Name: "{commonappdata}\SAMER Hub\Receipts"
Name: "{commonappdata}\SAMER Hub\Database"

[Files]
; Publish dosyaları
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: recursesubdirs ignoreversion; Excludes: "*.pdb"
Source: "..\publish\service\*"; DestDir: "{app}\service"; Flags: recursesubdirs ignoreversion; Excludes: "*.pdb"

; Logo dosyası
Source: "..\Resources\logo.ico"; DestDir: "{app}\desktop"; Flags: ignoreversion

; PowerShell scriptleri
Source: "scripts\install-service.ps1"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "scripts\uninstall-service.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\firewall.ps1"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autodesktop}\SAMER Hub"; Filename: "{app}\desktop\{#MyAppExeName}"; IconFilename: "{app}\desktop\logo.ico"; IconIndex: 0; Comment: "SAMER Hub - Restoran Yönetim Sistemi"
Name: "{group}\SAMER Hub"; Filename: "{app}\desktop\{#MyAppExeName}"; IconFilename: "{app}\desktop\logo.ico"; IconIndex: 0; Comment: "SAMER Hub - Restoran Yönetim Sistemi"
Name: "{group}\Kaldır"; Filename: "{uninstallexe}"; Comment: "SAMER Hub'ı Kaldır"
Name: "{commondesktop}\SAMER Hub"; Filename: "{app}\desktop\{#MyAppExeName}"; IconFilename: "{app}\desktop\logo.ico"; IconIndex: 0; Comment: "SAMER Hub - Restoran Yönetim Sistemi"

[Run]
; PowerShell scriptlerini çalıştır
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{tmp}\install-service.ps1"" -InstallRoot ""{app}"""; Flags: runhidden waituntilterminated; StatusMsg: "Servis kuruluyor..."
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{tmp}\firewall.ps1"""; Flags: runhidden waituntilterminated; StatusMsg: "Güvenlik duvarı kuralları yapılandırılıyor..."

; Uygulamayı başlat
Filename: "{app}\desktop\{#MyAppExeName}"; Description: "{#MyAppName} uygulamasını başlat"; Flags: postinstall skipifsilent nowait; OnlyBelowVersion: 0

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\scripts\uninstall-service.ps1"""; Flags: runhidden waituntilterminated; StatusMsg: "Servis kaldiriliyor..."

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.BeveledLabel=Kurulum Sihirbazı
english.BeveledLabel=Setup Wizard

[Code]
function GetDateTime(Param: String): String;
begin
  Result := GetDateTimeString('yyyy-mm-dd_hh-mm-ss', '-', ':');
end;
