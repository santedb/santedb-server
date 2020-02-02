  ; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SanteDB Server"
#define MyAppPublisher "Mohawk College mHealth & eHealth Development and Innovation Centre"
#define MyAppURL "http://santesuite.org"

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
DefaultDirName={pf64}\SanteSuite\SanteDB\Server
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\License.rtf
OutputDir=..\bin\release\dist\
OutputBaseFilename = santedb-server-{#MyAppVersion}
SolidCompression=yes
Uninstallable=true
WizardSmallImageFile=.\install-small.bmp
WizardImageFile=.\install.bmp
#ifdef DEBUG
Compression = none
#else
Compression = lzma
#endif
AppCopyright = Copyright (C) 2015-2019 SanteSuite Community Partners
ArchitecturesInstallIn64BitMode = x64
ArchitecturesAllowed =  x64
; SignTool=default sign $f
; SignedUninstaller=yes

[Types]
Name: full; Description: Complete Installation
Name: imsi; Description: HDSI Only (Cluster Install)
Name: ami; Description: AMI Only (Cluster Install)
Name: auth; Description: ACS Only (Cluster Install)
Name: bis; Description: BIS Only (Cluster Install)
Name: demo; Description: Demo Installation
Name: tools; Description: Tooling Only              
Name: custom; Description: Custom Installation; Flags: iscustom

[Components]
Name: core; Description: SanteDB Core; Types: full imsi ami auth bis demo
Name: core\bre; Description: JInt Business Rules Engine; Types: full demo
Name: core\protocol; Description: XML Clinical Support Decision Engine; Types: full demo
Name: server; Description: SanteDB Service Host; Types: full demo
Name: msg; Description: Core Messaging Interfaces; Types: full demo
Name: msg\hdsi; Description: Health Administration Interface; Types: full imsi demo
Name: msg\ami; Description: Administration Management Interface; Types: full ami demo
Name: msg\auth; Description: OAuth2.0 Authentication Server; Types: full auth demo
Name: interop; Description: Integration Interfaces; Types: full demo
Name: interop\fhir; Description: HL7 Fast Health Integration Resources; Types: full demo
Name: interop\hl7; Description: HL7v2 Messaging; Types: full demo
Name: interop\gs1; Description: GS1 BMS Messaging; Types: full demo
Name: interop\jira; Description: JIRA Integration; Types: full
Name: interop\atna; Description: ATNA & DICOM Auditing; Types: full
Name: interop\openapi; Description: OpenAPI; Types: full demo
Name: reporting; Description: Reporting Services; Types: full
Name: reporting\bis; Description: Business Intelligence Services; Types: full bis
Name: reporting\risi; Description: Report Integration Service (Legacy); Types: full 
Name: reporting\jasper; Description: Jasper Reports Server Integration (Legacy); Types: full
Name: tfa; Description: Two Factor Authentication; Types: full
Name: tfa\twilio; Description: Twilio SMS TFA Adapter; Types: full
Name: tfa\email; Description: Email TFA Adapter; Types: full
Name: mdm; Description: Master Data Management (MDM); Types: full
Name: match; Description: Record Matcher (SanteMatch); Types: full
Name: db; Description: Data Persistence; Types: full demo
Name: db\fbsql; Description: FirebirdSQL Persistence Services; Types: full demo
Name: db\psql; Description: PostgreSQL Persistence Services; Types: full
Name: cache; Description: Memory Caching Services; Types: full demo
Name: cache\redis; Description: REDIS Shared Memory Caching; Types: full
Name: tools; Description: Management Tooling; Types: full demo
Name: demo; Description: Elbonia Quickstart; Types: demo

[Files]

; Microsoft .NET Framework 4.5 Installation
Source: .\dotNetFx45_Full_setup.exe; DestDir: {tmp} ; Flags: dontcopy

; VC Redist for FBSQL
Source: .\vc2010.exe; DestDir: {tmp} ; Flags: dontcopy

; ADO.NET 
Source: ..\bin\release\Npgsql.dll; DestDir: {app}; Components: db\psql
Source: ..\bin\release\fbclient.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\fbembed.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\ib_util.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\icudt52.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\icuin52.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\icuuc52.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\firebird.conf; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\firebird.msg; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\icudt52l.dat; DestDir: {app}; Components: db\fbsql demo
Source: ..\bin\release\IDPLicense.txt; DestDir: {app}\NOTICES; Components: db\fbsql demo
Source: ..\bin\release\IPLicense.txt; DestDir: {app}\NOTICES; Components: db\fbsql demo
Source: ..\bin\release\plugins\engine12.dll; DestDir: {app}\plugins; Components: db\fbsql demo
Source: ..\bin\release\FirebirdSQL.Data.FirebirdClient.dll; DestDir: {app}; Components: db\fbsql demo
Source: ..\SanteDB\Data\SDB_BASE.FDB; DestDir: {app}; Components: demo
Source: ..\SanteDB\Data\SDB_AUDIT.FDB; DestDir: {app}; Components: demo

; XClinical Protocol 
Source: ..\bin\release\Antlr3.Runtime.dll; DestDir: {app}; Components: core\protocol
Source: ..\bin\release\ExpressionEvaluator.dll; DestDir: {app}; Components: core\protocol
Source: ..\bin\Release\SanteDB.Cdss.Xml.dll; DestDir: {app}; Components: core\protocol

; JINT BRE
Source: ..\bin\release\SanteDB.BusinessRules.JavaScript.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\jint.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\esprima.dll; DestDir: {app}; Components: core\bre

; Caching
Source: ..\bin\release\SanteDB.Caching.Memory.dll; DestDir: {app}; Components: cache

; REDIS
Source: ..\bin\release\SanteDB.Caching.Redis.dll; DestDir: {app}; Components: cache\redis
Source: ..\bin\release\StackExchange.Redis.dll; DestDir: {app}; Components: cache\redis

; Demo Data
Source: ..\SanteDB\Data\Demo\*.dataset; DestDir: {app}\data; Components: demo
Source: ..\SanteDB\App.Config.Demo; DestDir: {app}; DestName: SanteDB.exe.config; Components: demo
Source: ..\SanteDB\santedb.config.xml; DestDir: {app}; Components: demo

; Core DLLS
Source: ..\bin\release\MARC.Everest.dll; DestDir: {app}; Components: core
Source: ..\bin\release\Microsoft.Diagnostics.Runtime.dll; DestDir: {app}; Components: core
Source: ..\bin\release\Microsoft.Threading.Tasks.dll; DestDir: {app}; Components: core
Source: ..\bin\release\Microsoft.Threading.Tasks.Extensions.Desktop.dll; DestDir: {app}; Components: core
Source: ..\bin\release\Microsoft.Threading.Tasks.Extensions.dll; DestDir: {app}; Components: core
Source: ..\bin\release\MohawkCollege.Util.Console.Parameters.dll; DestDir: {app}; Components: core tools
Source: ..\bin\release\Newtonsoft.Json.dll; DestDir: {app}; Components: core
Source: ..\bin\release\RestSrvr.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Api.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Applets.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Model.AMI.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Model.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Model.RISI.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Model.ViewModelSerializers.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SharpCompress.dll; DestDir: {app}; Components: core
Source: ..\bin\release\System.Data.DataSetExtensions.dll; DestDir: {app}; Components: core
Source: ..\bin\release\System.Runtime.InteropServices.RuntimeInformation.dll; DestDir: {app}; Components: core
Source: ..\bin\release\System.Threading.Tasks.Extensions.dll; DestDir: {app}; Components: core
Source: ..\bin\release\System.ValueTuple.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.exe; DestDir: {app}; Components: server

; OAUTH
Source: ..\bin\release\SanteDB.Authentication.OAuth2.dll; DestDir: {app}; Components: msg\auth
Source: ..\bin\release\System.IdentityModel.Tokens.Jwt.dll; DestDir: {app}; Components: msg\auth tools

; AMI
Source: ..\bin\release\MARC.Util.CertificateTools.dll; DestDir: {app}; Components: msg\ami
Source: ..\solution items\CERTADMINLIB.dll; DestDir: {app}; Components: msg\ami
Source: ..\bin\release\SanteDB.Messaging.AMI.dll; DestDir: {app}; Components: msg\ami
Source: ..\bin\release\SanteDB.Rest.AMI.dll; DestDir: {app}; Components: msg\ami

; Common Messaging
Source: ..\bin\release\SanteDB.Rest.Common.dll; DestDir: {app}; Components: msg

; HL7
Source: ..\bin\release\SanteDB.Messaging.HL7.dll; DestDir: {app}; Components: interop\hl7
Source: ..\Solution Items\NHapi.Base.dll; DestDir: {app}; Components: interop\hl7
Source: ..\Solution Items\NHapi.Model.V25.dll; DestDir: {app}; Components: interop\hl7

; ATNA 
Source: ..\bin\release\SanteDB.Messaging.ATNA.dll; DestDir: {app}; Components: interop\atna
Source: ..\bin\release\AtnaApi.dll; DestDir: {app}; Components: interop\atna

; FHIR
Source: ..\bin\release\SanteDB.Messaging.FHIR.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\release\Hl7.Fhir.R4.Core.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\release\Hl7.Fhir.Support.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\release\Hl7.FhirPath.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\release\Hl7.Fhir.Support.Poco.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\release\Hl7.Fhir.Serialization.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\release\Hl7.Fhir.ElementModel.dll; DestDir: {app}; Components: interop\fhir

; GS1
Source: ..\bin\release\SanteDB.Messaging.GS1.dll; DestDir: {app}; Components: interop\gs1

; HDSI
Source: ..\bin\release\SanteDB.Messaging.HDSI.dll; DestDir: {app}; Components: msg\hdsi
Source: ..\bin\release\SanteDB.Rest.HDSI.dll; DestDir: {app}; Components: msg\hdsi

; RISI
Source: ..\bin\release\SanteDB.Messaging.RISI.dll; DestDir: {app}; Components: reporting\risi
Source: ..\bin\release\SanteDB.Persistence.Reporting.ADO.dll; DestDir: {app}; Components: reporting
Source: ..\bin\release\SanteDB.Reporting.Core.dll; DestDir: {app}; Components: reporting
Source: ..\bin\release\SanteDB.Reporting.Jasper.dll; DestDir: {app}; Components: reporting\jasper

; Twilio
Source: ..\bin\release\JWT.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\release\RestSharp.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\release\Twilio.Api.dll; DestDir: {app}; Components: core
Source: ..\bin\release\SanteDB.Core.Security.Tfa.Twilio.dll; DestDir: {app}; Components: tfa\twilio

; EMail TFA
Source: ..\bin\release\SanteDB.Core.Security.Tfa.Email.dll; DestDir: {app}; Components: tfa\email

; Data Stuff
Source: ..\bin\release\data\*.dataset; DestDir: {app}\data; Components: server
Source: ..\bin\release\applets\*.pak; DestDir: {app}\applets; Components: server

; ADO Stuff
Source: ..\SanteDB.Persistence.Data.ADO\Data\SQL\FBSQL\*.sql; DestDir: {app}\sql\fbsql; Components: db\fbsql
Source: ..\SanteDB.Persistence.Data.ADO\Data\SQL\PSQL\*.sql; DestDir: {app}\sql\psql; Components: db\psql
Source: ..\SanteDB.Persistence.Data.ADO\Data\SQL\Updates\*-PSQL.sql; DestDir: {app}\sql\updates; Components: db\psql
Source: ..\SanteDB.Warehouse.ADO\Data\SQL\PSQL\*.sql; DestDir: {app}\sql; Components: db\psql
Source: ..\bin\release\SanteDB.OrmLite.dll; DestDir: {app}; Components: db
Source: ..\bin\release\SanteDB.Persistence.Auditing.Ado.dll; DestDir: {app}; Components: db
Source: ..\bin\release\SanteDB.Persistence.Data.ADO.dll; DestDir: {app}; Components: db
Source: ..\bin\release\SanteDB.Warehouse.ADO.dll; DestDir: {app}; Components: db

; JIRA
Source: ..\bin\release\SanteDB.Persistence.Diagnostics.Jira.dll; DestDir: {app}; Components: interop\jira

; Email Diagnostics
Source: ..\bin\release\SanteDB.Persistence.Diagnostics.Email.dll; DestDir: {app}; Components: server

; Tools
Source: ..\bin\release\sdbac.exe; DestDir: {app}; Components: tools
Source: ..\bin\release\SanteDB.Messaging.AMI.Client.dll; DestDir: {app}; Components: tools
Source: ..\bin\release\SanteDB.Tools.DataSandbox.dll; DestDir: {app}; Components: tools

; MDM
Source: ..\bin\release\SanteDB.Persistence.MDM.dll; DestDir: {app}; Components: mdm

; Matcher
Source: ..\bin\release\SanteDB.Matcher.dll; DestDir: {app}; Components: match
Source: ..\bin\release\SanteDB.Matcher.Configuration.File.dll; DestDir: {app}; Components: match
Source: ..\bin\release\Phonix.dll; DestDir: {app}; Components: match

; Configuration
Source: ..\bin\release\ConfigTool.exe; DestDir: {app}; 
Source: ..\bin\release\SanteDB.Configuration.dll; DestDir: {app};

; Demo
Source: ..\SanteDB\Data\Demo\*.dataset; DestDir: {app}\data; Components: demo
Source: ..\SanteDB\santedb.config.dev.xml; DestDir: {app}; DestName: santedb.config.xml; Components: demo

; BIS
Source: ..\bin\release\SanteDB.BI.dll; DestDir: {app}; Components: reporting\bis demo
Source: ..\bin\release\SanteDB.Rest.BIS.dll; DestDir: {app}; Components: reporting\bis demo

; Metadata
Source: ..\bin\release\SanteDB.Messaging.Metadata.dll; DestDir: {app}; Components: interop\openapi demo
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Run]
;Filename: "{app}\ConfigTool.exe";  Description: "Configure SanteDB Server"; Flags: postinstall ; 
Filename: "{app}\SanteDB.exe"; Parameters:"--install"; Flags: runhidden runascurrentuser; StatusMsg: "Registering SanteDB Service"
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB REST Ports"" dir=in protocol=TCP localport=8080 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; Components: demo
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB HL7 Ports"" dir=in protocol=TCP localport=2100 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; Components: demo
Filename: "net.exe";StatusMsg: "Starting Services..."; Parameters: "start santedb"; Flags: runhidden; Components: demo


[UninstallRun]
Filename: "{app}\SanteDB.exe"; Parameters: "--uninstall"; StatusMsg: "Un-registering SanteDB"; Flags:runhidden runascurrentuser;


[Icons]
Name: "{commonprograms}\SanteDB\SanteDB Server Console"; Filename: "{app}\sdbac.exe"
Name: "{commonprograms}\SanteDB\SanteDB Server Configuration"; Filename: "{app}\configtool.exe"
Filename: "http://help.santesuite.org"; Name: "{group}\SanteDB\SanteDB Help"; IconFilename: "{app}\santedb.exe"

; Components
[Code]
var
  dotNetNeeded: boolean;
  memoDependenciesNeeded: string;
  psqlPageId : integer;
  chkInstallPSQL : TCheckBox;
  txtPostgresSU, txtPostgresSUPass : TEdit;

const
  dotnetRedistURL = '{tmp}\dotNetFx45_Full_setup.exe';
  // local system for testing...
  // dotnetRedistURL = 'http://192.168.1.1/dotnetfx.exe';


function Framework45IsNotInstalled(): Boolean;
var
  bSuccess: Boolean;
  regVersion: Cardinal;
begin
  Result := True;
  bSuccess := RegQueryDWordValue(HKLM, 'Software\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', regVersion);
  if (True = bSuccess) and (regVersion >= 378675) then begin
    Result := False;
  end;
end; 

function InitializeSetup(): Boolean;

begin
 
  Result := true;
  dotNetNeeded := Framework45IsNotInstalled();
  
  if (not IsAdminLoggedOn()) then begin
    MsgBox('OpenIZ needs the Microsoft .NET Framework 4.5.1 to be installed by an Administrator', mbInformation, MB_OK);
    Result := false;
  end 
  else if(dotNetNeeded) then begin
    memoDependenciesNeeded := memoDependenciesNeeded + '      .NET Framework 4.5.2' #13;
  end;

end;

function PrepareToInstall(var needsRestart:Boolean): String;
var
  hWnd: Integer;
  ResultCode : integer;
  uninstallString : string;
begin
    
    EnableFsRedirection(true);

    ExtractTemporaryFile('vc2010.exe');
    Exec(ExpandConstant('{tmp}\vc2010.exe'), '/install /passive', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

    if (Result = '') and (dotNetNeeded = true) then begin
      ExtractTemporaryFile('dotNetFx45_Full_setup.exe');
      if Exec(ExpandConstant(dotnetRedistURL), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then begin
          // handle success if necessary; ResultCode contains the exit code
          if not (ResultCode <> 0) then begin
            Result := '.NET Framework 4.5.1 is Required';
          end;
        end else begin
          // handle failure if necessary; ResultCode contains the error code
            Result := '.NET Framework 4.5.1 is Required';
        end;
    end;

end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  s: string;

begin
  if memoDependenciesNeeded <> '' then s := s + 'Dependencies that will be automatically downloaded And installed:' + NewLine + memoDependenciesNeeded + NewLine;

  s := s + MemoDirInfo + NewLine;

  Result := s
end;

