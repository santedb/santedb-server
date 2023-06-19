; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SanteDB Administrative Console"
#define MyAppPublisher "SanteDB Community"
#define MyAppURL "http://santesuite.org"
#ifndef MyAppVersion
#define MyAppVersion "3.0"
#endif 

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{E2A094E4-0E7E-4C21-9283-4F169DB35CF4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf64}\SanteSuite\SanteDB\AdminConsole
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\License.rtf
OutputDir=..\bin\release\dist\
OutputBaseFilename = sdbac-{#MyAppVersion}
SolidCompression=yes
Uninstallable=true
WizardSmallImageFile=.\install-small.bmp
WizardImageFile=.\install.bmp
#ifdef DEBUG
Compression = none
#else
Compression = lzma
#endif
AppCopyright = Copyright (C) 2015-2021 SanteSuite Contributors
ArchitecturesInstallIn64BitMode = x64
ArchitecturesAllowed =  x64
WizardStyle=modern

#ifndef UNSIGNED
SignedUninstaller=yes
SignTool=default /a /n $qFyfe Software$q /d $qSanteDB iCDR Admin Console$q $f
#endif
; SignTool=default sign $f
; SignedUninstaller=yes

[Files]

; Microsoft .NET Framework 4.5 Installation
Source: .\netfx.exe; DestDir: {tmp} ; Flags: dontcopy

Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.Bcl.AsyncInterfaces.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.Extensions.Primitives.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.IdentityModel.Abstractions.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.IdentityModel.JsonWebTokens.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.IdentityModel.Logging.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.IdentityModel.Tokens.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Microsoft.Net.Http.Headers.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\MohawkCollege.Util.Console.Parameters.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\Newtonsoft.Json.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\RestSrvr.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.AdminConsole.Api.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Client.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Core.Api.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Core.Applets.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Core.i18n.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Core.Model.AMI.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Core.Model.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Messaging.AMI.Client.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Messaging.HDSI.Client.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Rest.Common.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SanteDB.Rest.OAuth.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\sdbac.exe; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\sdbac.exe.config; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\SharpCompress.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Buffers.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.CodeDom.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Configuration.ConfigurationManager.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Drawing.Common.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.IO.Packaging.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Memory.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Numerics.Vectors.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Runtime.CompilerServices.Unsafe.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Security.Permissions.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Security.Principal.Windows.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Text.Encoding.CodePages.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Text.Encodings.Web.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Text.Json.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.Threading.Tasks.Extensions.dll; DestDir: {app}
Source: ..\santedb-tools\bin\Release\net4.8\System.ValueTuple.dll; DestDir: {app}


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"


[Icons]
Name: "{commonprograms}\SanteDB\SanteDB Server Console (localhost)"; Filename: "{app}\sdbac.exe"
Filename: "http://help.santesuite.org"; Name: "{group}\SanteDB\SanteDB Help"; IconFilename: "{app}\santedb.exe"

; Components
[Code]
function PrepareToInstall(var needsRestart:Boolean): String;
var
  hWnd: Integer;
  ResultCode : integer;
  uninstallString : string;
begin
    EnableFsRedirection(true);
    WizardForm.PreparingLabel.Visible := True;
    WizardForm.PreparingLabel.Caption := 'Installing Microsoft .NET Framework 4.8';
    ExtractTemporaryFile('netfx.exe');
    Exec(ExpandConstant('{tmp}\netfx.exe'), '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if((CurPageID = wpInstalling)) then begin
  end;
end;
