; MatchKit Inno Setup Script

#define MyAppConfiguration "Release"
; Assuming idp.iss is in a known location or Inno Setup's include paths.
; If not, you might need to provide a full path:
; #include <C:\Program Files (x86)\Inno Download Plugin\idp.iss>
; For now, we'll assume it's in the default search path.
#include <idp.iss>

#define MyAppName "MatchKit"
#define MyAppVersion GetVersionNumbersString('..\MatchKit.Tray\bin\' + MyAppConfiguration + '\MatchKit.Tray.exe')
#if "" == MyAppVersion
  #error 'Could not get binary version from ..\MatchKit.Tray\bin\' + MyAppConfiguration + '\MatchKit.Tray.exe'
#endif
#define MyAppPublisher "Flux Inc"
#define MyAppURL "https://fluxinc.co"
#define MyAppExeName "MatchKit.Tray.exe"

[Setup]
AppId={{4B6BC687-2813-4A0A-A660-2B271727FA17}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/support
DefaultDirName={autopf}\{#MyAppPublisher}\{#MyAppName}
OutputDir=output
OutputBaseFilename=MatchKitSetup-{#MyAppVersion}
Compression=lzma
PrivilegesRequired=admin
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\MatchKit\bin\{#MyAppConfiguration}\MatchKit.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\MatchKit.Tray\bin\{#MyAppConfiguration}\MatchKit.Tray.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\MatchKit.Tray\bin\{#MyAppConfiguration}\MatchKit.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\MatchKit.Tray\bin\{#MyAppConfiguration}\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\MatchKit.Tray\bin\{#MyAppConfiguration}\*.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\MatchKit.Tray\bin\{#MyAppConfiguration}\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "input\README.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme;


[Icons]
Name: "{autoprograms}\{#MyAppName}\MatchKit Tray"; Filename: "{app}\{#MyAppExeName}";
Name: "{autoprograms}\{#MyAppName}\Configure MatchKit"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--config";

; Add Desktop shortcut for All Users if the task is selected
Name: "{commondesktop}\{#MyAppName} Tray"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut for all users"; GroupDescription: "Additional shortcuts:"

[Registry]
Root: HKCU; SubKey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}Tray"; ValueData: "\""{app}\{#MyAppExeName}\"""; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "--config"; Description: "Launch application configuration"; Flags: postinstall waituntilterminated skipifsilent

[Code]
function Framework48IsNotInstalled(): Boolean;
var
  bSuccess: Boolean;
  regVersion: Cardinal;
begin
  Result := True;
  bSuccess := RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', regVersion);
  if (True = bSuccess) and (regVersion >= 528040) then begin
    Result := False;
  end;
end;

procedure InitializeWizard;
begin
  if Framework48IsNotInstalled() then
  begin
    idpAddFile('https://go.microsoft.com/fwlink/?linkid=2088631', ExpandConstant('{tmp}\\NetFrameworkInstaller.exe'));
    idpDownloadAfter(wpReady);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  // Validation for removed argument pages is no longer needed.
  // if CurPageID = ArgumentsPage1.ID then
  // begin
  //   if Trim(ArgumentsPage1.Values[0]) = '' then // Window Name
  //   begin
  //     MsgBox('Target Window Name cannot be empty.', mbError, MB_OK);
  //     Result := False;
  //     Exit;
  //   end;
  //   if Trim(ArgumentsPage1.Values[1]) = '' then // Regex
  //   begin
  //     MsgBox('Regular Expression Pattern cannot be empty.', mbError, MB_OK);
  //     Result := False;
  //     Exit;
  //   end;
  //   if Trim(ArgumentsPage1.Values[2]) = '' then // Hotkey
  //   begin
  //     MsgBox('Hotkey cannot be empty.', mbError, MB_OK);
  //     Result := False;
  //     Exit;
  //   end;
  // end
  // else if CurPageID = ArgumentsPage2.ID then
  // begin
  //   // Optional: Add validation for URL/JSON Key if needed.
  //   // For example, if URL is provided, JSON Key might be encouraged or validated.
  //   // For now, we assume they are optional and don't need strict validation here.
  //   // If URL is not empty and JSON Key is empty, you might want to warn the user.
  //   if (Trim(ArgumentsPage2.Values[0]) <> '') and (Trim(ArgumentsPage2.Values[1]) = '') then
  //   begin
  //     // Example: Simple warning, not blocking
  //     // MsgBox('You have provided a URL without a JSON Key. The full API response will be used if no key is specified.', mbInformation, MB_OK);
  //   end;
  // end;

  // If it's the last custom page, or any page where parameters are finalized, store them.
  // It's generally safest to call this after each custom page if subsequent pages don't depend on these values for their setup,
  // or ensure GetTrayAppParametersForRegistry calls it, which it does.
  // No explicit call to StoreUserInputsAndBuildParameters() needed here if GetTrayAppParametersForRegistry handles it.
end;

procedure InstallFramework;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := 'Installing .NET Framework 4.8. This might take a few minutes...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if not Exec(ExpandConstant('{tmp}\NetFrameworkInstaller.exe'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('.NET Framework 4.8 installation failed. Result Code: ' + IntToStr(ResultCode) + '.' + #13#10 +
             'You may need to install it manually and then re-run this installer.', mbError, MB_OK);
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
    DeleteFile(ExpandConstant('{tmp}\NetFrameworkInstaller.exe'));
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if Framework48IsNotInstalled() then
    begin
      if MsgBox('Microsoft .NET Framework 4.8 is required but not installed. Do you want to download and install it now?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        InstallFramework();
      end
      else
      begin
        MsgBox('Installation of .NET Framework 4.8 was skipped. {#MyAppName} may not function correctly. Please install it manually.', mbError, MB_OK);
      end;
    end;
  end;
end;
