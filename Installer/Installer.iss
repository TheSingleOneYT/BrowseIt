; Inno Setup
; https://jrsoftware.org/isinfo.php

#define MyAppName "BrowseIt"
#define MyAppVersion "1.3.2"
#define MyAppPublisher "TS1"
#define MyAppURL "https://github.com/TheSingleOneYT/BrowseIt"
#define MyAppExeName "BrowseIt.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{A148904B-4506-4E56-95B0-AA086B41B712}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=Assets\BrowseIt License.txt
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputBaseFilename=BrowseIt-setup
SetupIconFile=Assets\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardSmallImageFile=Assets\bmp\icon.bmp
WizardImageFile=Assets\bmp\Wizard_large_bmp.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "bulgarian"; MessagesFile: "compiler:Languages\Bulgarian.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "Program\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "Program\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Other\windowsdesktop-runtime-3.1.22-win-x64.exe"; DestDir: {tmp}; Flags: deleteafterinstall
Source: "JSONDBSetup\*"; DestDir: {tmp}; Flags: deleteafterinstall
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall
Filename: "{tmp}\windowsdesktop-runtime-3.1.22-win-x64.exe"; Parameters: /install /quiet /norestart; Check: DependencyInstall; Flags: runhidden; StatusMsg: Installing Windows .NET Runtime 3.1.22...
Filename: "{tmp}\JSONDB_Setup.exe"; Check: DependencyInstall; Flags: runhidden; StatusMsg: Installing Latest JSONDatabase...
; The majority of the below + part of the 2 lines above is ripped from:
; https://github.com/rocksdanister/lively/blob/core-separation/src/installer/Script.iss
; which ripped part of the code from:
; https://github.com/domgho/InnoDependencyInstaller 
[Code]    
function NetCoreNeedsInstall(version: String): Boolean;
var
	netcoreRuntime: String;
	resultCode: Integer;
begin
  // Example: 'Microsoft.NETCore.App', 'Microsoft.AspNetCore.App', 'Microsoft.WindowsDesktop.App'
  netcoreRuntime := 'Microsoft.WindowsDesktop.App'
	Result := not(Exec(ExpandConstant('{tmp}{\}') + 'netcorecheck.exe', netcoreRuntime + ' ' + version, '', SW_HIDE, ewWaitUntilTerminated, resultCode) and (resultCode = 0));
end;

function CmdLineParamNotExists(const Value: string): Boolean;
var
  I: Integer;  
begin
  Result := True;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), Value) = 0 then
    begin
      Result := False;
      Exit;
    end;
end;

function DependencyInstall(): Boolean;
begin
  Result := CmdLineParamNotExists('/NODEPENDENCIES');
end;

