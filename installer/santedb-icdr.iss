; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SanteDB Server"
#define MyAppPublisher "SanteDB Community"
#define MyAppURL "http://santesuite.org"

#ifndef MyAppVersion
#define MyAppVersion "3.0"
#endif 

#ifndef SignKey
#define SignKey "8185304d2f840a371d72a21d8780541bf9f0b5d2"
#endif 

#ifndef SignOpts
#define SignOpts ""
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
AppCopyright = Copyright (C) 2015-2021 SanteSuite Contributors
ArchitecturesInstallIn64BitMode = x64
ArchitecturesAllowed =  x64
WizardStyle=modern

#ifndef UNSIGNED
SignedUninstaller=yes
SignTool=default /sha1 {#SignKey} {#SignOpts} /d $qSanteDB iCDR Server$q $f
#endif
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
Name: msg\www; Description: Web Hosting Services; Types: full auth demo 
Name: msg\app; Description: Client Application Services; Types: full auth demo 
Name: dcdr; Description: dCDR Client Services; Types: full demo
Name: bi; Description: Business Intelligence Services; Types: full auth demo 
Name: interop; Description: Integration Interfaces; Types: full demo 
Name: interop\fhir; Description: HL7 Fast Health Integration Resources; Types: full demo 
Name: interop\hl7; Description: HL7v2 Messaging; Types: full demo
Name: interop\gs1; Description: GS1 BMS Messaging; Types: full demo
Name: interop\jira; Description: JIRA Integration; Types: full
Name: interop\msmq; Description: Microsoft Message Queue (MSMQ); Types: full
Name: interop\rabbitmq; Description: RabbitMQ Integration; Types: full
Name: interop\atna; Description: ATNA & DICOM Auditing; Types: full
Name: interop\openapi; Description: OpenAPI; Types: full demo
Name: reporting; Description: Reporting Services; Types: full
Name: reporting\bis; Description: Business Intelligence Services; Types: full bis 
Name: tfa; Description: Two Factor Authentication; Types: full
Name: tfa\twilio; Description: Twilio SMS TFA Adapter; Types: full
Name: tfa\email; Description: Email TFA Adapter; Types: full
Name: mdm; Description: Master Data Management (MDM); Types: full 
Name: match; Description: Record Matcher (SanteMatch); Types: full 
Name: db; Description: Data Persistence; Types: full demo 
Name: db\fbsql; Description: FirebirdSQL Persistence Services; Types: full demo 
Name: db\sqlite; Description: SQLite Persistence Services; Types: full demo 
Name: db\psql; Description: PostgreSQL Persistence Services; Types: full 
Name: cache; Description: Memory Caching Services; Types: full demo 
Name: cache\redis; Description: REDIS Shared Memory Caching; Types: full 
Name: tools; Description: Management Tooling; Types: full demo
Name: dev; Description: Development Tooling; Types: full demo
Name: demo; Description: Elbonia Quickstart; Types: demo

[Files]

; Microsoft .NET Framework 4.5 Installation
Source: .\netfx.exe; DestDir: {tmp} ; Flags: dontcopy

; VC Redist for FBSQL
Source: .\vc_redist.x64.exe; DestDir: {tmp} ; Flags: dontcopy

; LIcenses
Source: ..\bin\Release\IDPLicense.txt; DestDir: {app}\licenses; Components: db\fbsql
Source: ..\bin\Release\IPLicense.txt; DestDir: {app}\licenses; Components: db\fbsql
Source: ..\bin\Release\License.rtf; DestDir: {app}\licenses

; Firebird SQL 
Source: ..\bin\Release\fbclient.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\firebird.conf; DestDir: {app}; Components: db\fbsql
; Source: ..\bin\Release\fbembed.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\FirebirdSql.Data.FirebirdClient.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\ib_util.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\icudt52.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\icuin52.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\icuuc52.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\plugins\engine12.dll; DestDir: {app}\plugins; Components: db\fbsql
Source: ..\bin\Release\icudt52l.dat; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\firebird.msg; DestDir: {app}; Components: db\fbsql
;Source: ..\bin\Release\fbembed.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\fbclient.dll; DestDir: {app}; Components: db\fbsql

; RabbitMQ 
Source: ..\bin\Release\RabbitMQ.Client.dll; DestDir: {app}; Components: interop\rabbitmq
Source: ..\bin\Release\SanteDB.Queue.RabbitMq.dll; DestDir: {app}; Components: interop\rabbitmq

; MSMQ Support
Source: ..\bin\Release\SanteDB.Queue.Msmq.dll; DestDir: {app}; Components: interop\msmq

; Demo Data
Source: ..\SanteDB\Data\Demo\*.dataset; DestDir: {app}\data; Components: demo

; Config Samples
Source: ..\SanteDB\santedb.config.fbsql.xml; DestDir: {app}; DestName: santedb.config.fbsql.xml; Components: db\fbsql
Source: ..\SanteDB\Data\SDB_BASE.FDB; DestDir: {app}; Components: db\fbsql demo; Flags: confirmoverwrite
Source: ..\SanteDB\Data\SDB_AUDIT.FDB; DestDir: {app}; Components: db\fbsql demo; Flags: confirmoverwrite

Source: ..\SanteDB\santedb.config.psql.xml; DestDir: {app}; DestName: santedb.config.psql.xml; Components: db\psql; 
; Security AMI stuff
Source: ..\bin\Release\SanteDB.Core.Model.AMI.dll; DestDir: {app}; Components: msg\ami
Source: ..\bin\Release\SanteDB.Rest.AMI.dll; DestDir: {app}; Components: msg\ami

; Config Parts 
; TODO: Individual files here
Source: ..\bin\release\config\*.*; DestDir: {app}\config; Components: server; 
Source: "..\bin\release\config\template\Standard SanteDB.xml"; DestDir: {app}\config; Components: server
; Data Stuff
Source: ..\bin\release\Data\*.dataset; DestDir: {app}\data; Components: server
Source: ..\bin\release\applets\*.pak; DestDir: {app}\applets; Components: server

; Tools
Source: ..\santedb-tools\bin\Release\sdbac.exe; DestDir: {app}; Components: tools
Source: ..\santedb-tools\bin\Release\sdbac.exe.config; DestDir: {app}; Components: tools
Source: ..\santedb-tools\bin\Release\SanteDB.AdminConsole.Api.dll; DestDir: {app}; Components: tools
Source: ..\bin\Release\SanteDB.exe.config; DestDir: {app}; DestName: sdbac.exe.config; Components: tools
Source: ..\bin\Release\Mono.Posix.dll; DestDir: {app}; Components: core

Source: ..\bin\release\SanteDB.Messaging.AMI.Client.dll; DestDir: {app}; Components: tools

;Documentation For OpenAPI
Source: ..\bin\Release\RestSrvr.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.OAuth.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.BI.xml; DestDir: {app}; Components: interop\openapi
; Source: ..\bin\Release\SanteDB.Core.Applets.xml; DestDir: {app}; Components: interop\openapi
;Source: ..\bin\Release\SanteDB.Core.Model.AMI.xml; DestDir: {app}; Components: interop\openapi
;Source: ..\bin\Release\SanteDB.Core.Model.ViewModelSerializers.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.FHIR.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.GS1.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.Metadata.xml; DestDir: {app}; Components: interop\openapi
;Source: ..\bin\Release\SanteDB.Rest.AMI.xml; DestDir: {app}; Components: interop\openapi
;Source: ..\bin\Release\SanteDB.Rest.BIS.xml; DestDir: {app}; Components: interop\openapi
;Source: ..\bin\Release\SanteDB.Rest.Common.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.HDSI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.AMI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.Metadata.dll; DestDir: {app}; Components: interop\openapi
; Core Services
Source: ..\bin\Release\ConfigTool.exe; DestDir: {app}; Components: server
Source: ..\bin\Release\ConfigTool.exe.config; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.exe; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.exe.config; DestDir: {app}; Components: server
Source: ..\bin\Release\MohawkCollege.Util.Console.Parameters.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\Newtonsoft.Json.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\RestSrvr.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\SanteDB.Configuration.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Core.Api.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\SanteDB.Core.Applets.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\SanteDB.Docker.Core.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.Core.Model.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\SharpCompress.dll; DestDir: {app}; Components: core
Source: ..\bin\release\System.Runtime.CompilerServices.Unsafe.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.Messaging.AMI.Client.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.Messaging.HDSI.Client.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.OrmLite.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.Auditing.ADO.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.Data.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.PubSub.ADO.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.Diagnostics.Email.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\BouncyCastle.Crypto.dll; DestDir: {app}; Components: server core
Source: ..\bin\Release\SanteDB.Security.Certs.BouncyCastle.dll; DestDir: {app}; Components: server core
Source: ..\bin\Release\SanteDB.Core.i18n.dll; DestDir: {app}; Components: server core
Source: ..\bin\Release\zxing.dll; DestDir: {app}; Components: server core
Source: ..\bin\Release\zxing.presentation.dll; DestDir: {app}; Components: server core
Source: ..\bin\Release\ZXing.Windows.Compatibility.dll; DestDir: {app}; Components: server core
;Source: ..\bin\Release\fr\*; DestDir: {app}\fr; Components: server core

; Client Services
Source: ..\bin\Release\SanteDB.Client.dll; DestDir: {app}; Components: msg\app dcdr;
Source: ..\bin\Release\SanteDB.Rest.AppService.dll; DestDir: {app}; Components: msg\app dcdr;
Source: ..\bin\Release\SanteDB.Client.Disconnected.dll; DestDir: {app}; Components: dcdr;
Source: ..\bin\Release\SanteDB.Persistence.Synchronization.ADO.dll; DestDir: {app}; Components: dcdr;

; WWW Services
Source: ..\bin\Release\SanteDB.Rest.Www.dll; DestDir: {app}; Components: msg\www dcdr;

; Dev tooling
Source: ..\bin\Release\SanteDB.DevTools.dll; DestDir: {app}; Components: dev
Source: ..\bin\Release\SanteDB.PakMan.Common.dll; DestDir: {app}; Components: dev


; Common BRE
Source: ..\bin\Release\Antlr*.dll; DestDir: {app}; Components: core\bre core\protocol core
Source: ..\bin\Release\DynamicExpresso.Core.dll; DestDir: {app}; Components: core\bre core\protocol                              
Source: ..\bin\Release\Jint.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\Acornima.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\SanteDB.BusinessRules.JavaScript.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\SanteDB.Cdss.Xml.dll; DestDir: {app}; Components: core\protocol

; ATNA
Source: ..\bin\Release\AtnaApi.dll; DestDir: {app}; Components: interop\atna

; FHIR R4 Support
Source: ..\bin\Release\Hl7.Fhir.ElementModel.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.R4B.Core.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.Serialization.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Microsoft.IdentityModel.Tokens.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Microsoft.IdentityModel.Logging.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.Support.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.Support.Poco.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.FhirPath.dll; DestDir: {app}; Components: interop\fhir
Source: ..\santedb-fhir\SanteDB.Messaging.FHIR\Data\*.dataset; DestDir: {app}\data; Components: interop\fhir
Source: ..\bin\Release\SanteDB.Messaging.FHIR.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\FhirParameterMap.xml; DestDir: {app}; components: interop\fhir

; Twilio Integration
Source: ..\bin\Release\RestSharp.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\Release\Twilio.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\Release\SanteDB.Core.Security.Tfa.Twilio.dll; DestDir: {app}; Components: tfa\twilio

; HL7v2
Source: ..\bin\Release\NHapi.Base.dll; DestDir: {app}; Components: interop\hl7
Source: ..\bin\Release\NHapi.Model.V231.dll; DestDir: {app}; Components: interop\hl7
Source: ..\bin\Release\NHapi.Model.V25.dll; DestDir: {app}; Components: interop\hl7
Source: ..\santedb-hl7\SanteDB.Messaging.HL7\Data\*.dataset; DestDir: {app}\data; Components: interop\hl7
Source: ..\bin\Release\SanteDB.Messaging.HL7.dll; DestDir: {app}; Components: interop\hl7

; NPSQL
Source: ..\bin\Release\Npgsql.dll; DestDir: {app}; Components: db\psql
Source: ..\santedb-data\bin\Release\netstandard2.0\SQL\PSQL\*.sql; Flags: recursesubdirs; DestDir: {app}\data\sql\psql; Components: db\psql; 
Source: ..\santedb-data\bin\Release\netstandard2.0\SQL\FBSQL\*.sql; Flags: recursesubdirs; DestDir: {app}\data\sql\fbsql; Components: db\fbsql;
Source: ..\santedb-data\bin\Release\netstandard2.0\SQL\SQLite\*.sql; Flags: recursesubdirs; DestDir: {app}\data\sql\sqlite; Components: db\sqlite;
Source: ..\santedb-data\SanteDB.Persistence.Auditing.ADO\bin\Release\netstandard2.0\Data\SQL\AuditDB\PSQL\*.sql; DestDir: {app}\data\sql\psql\auditdb; Components: db\psql;
Source: ..\santedb-data\SanteDB.Persistence.Auditing.ADO\bin\Release\netstandard2.0\Data\SQL\AuditDB\FBSQL\*.sql; DestDir: {app}\data\sql\fbsql\auditdb; Components: db\fbsql;
Source: ..\santedb-data\SanteDB.Persistence.Auditing.ADO\bin\Release\netstandard2.0\Data\SQL\AuditDB\SQLITE\*.sql; DestDir: {app}\data\sql\sqlite\auditdb; Components: db\sqlite;

; Matching Infrastructure
Source: ..\bin\Release\Phonix.dll; DestDir: {app}; Components: match
Source: ..\bin\Release\SanteDB.Matcher.dll; DestDir: {app}; Components: match

; OAUTH
Source: ..\bin\Release\SanteDB.Rest.OAuth.dll; DestDir: {app}; Components: msg\auth

; BI REPORTING
Source: ..\bin\Release\SanteDB.BI.dll; DestDir: {app}; Components: reporting\bis
Source: ..\bin\Release\SanteDB.Rest.BIS.dll; DestDir: {app}; Components: reporting\bis

; Caching 
Source: ..\bin\Release\SanteDB.Caching.Memory.dll; DestDir: {app}; Components: cache
Source: ..\bin\Release\SanteDB.Caching.Redis.dll; DestDir: {app}; Components: cache\redis

#ifdef MONO_BUILD
#else
Source: ..\bin\Release\Pipelines.Sockets.Unofficial.dll; DestDir: {app}; Components: cache\redis
#endif

Source: ..\bin\Release\StackExchange.Redis.dll; DestDir: {app}; Components: cache\redis


; Core Messaging
Source: ..\bin\Release\SanteDB.Core.Model.ViewModelSerializers.dll; DestDir: {app}; Components: msg\hdsi msg\ami

; Atna Interop
Source: ..\bin\Release\SanteDB.Messaging.Atna.dll; DestDir: {app}; Components: interop\atna

; GS1
Source: ..\bin\Release\SanteDB.Messaging.GS1.dll; DestDir: {app}; Components: interop\gs1
Source: ..\santedb-gs1\SanteDB.Messaging.GS1\Data\*.dataset; DestDir: {app}\data; Components: interop\gs1

; JDSO
Source: ..\bin\Release\SanteDB.Rest.HDSI.dll; DestDir: {app}; Components: msg\hdsi

; JIRA Integration
Source: ..\bin\Release\SanteDB.Persistence.Diagnostics.Jira.dll; DestDir: {app}; Components: interop\jira

; MDM INfrastructure
Source: ..\bin\Release\SanteDB.Persistence.MDM.dll; DestDir: {app}; Components: mdm
Source: ..\santedb-mdm\SanteDB.Persistence.MDM\Data\*.dataset; DestDir: {app}\data; Components: mdm
Source: "..\bin\Release\config\template\SanteDB MDM.xml"; DestDir: {app}\config\template; Components: mdm

; SQLIte
Source: ..\bin\Release\Microsoft.Data.Sqlite.dll; DestDir: {app}; Components: db\sqlite
Source: ..\bin\Release\runtimes\*; DestDir: {app}\runtimes; Flags: recursesubdirs; Components: db\sqlite
Source: ..\Solution Items\spellfix.dll; DestDir: {app}; Components: db\sqlite
;Source: ..\bin\Release\SQLitePCLRaw.batteries_v2.dll; DestDir: {app}; Components: db\sqlite
Source: ..\bin\Release\SQLitePCLRaw.core.dll; DestDir: {app}; Components: db\sqlite
Source: ..\bin\Release\SQLitePCLRaw.provider.dynamic_cdecl.dll; DestDir: {app}; Components: db\sqlite


Source: ..\bin\Release\SanteDB.Rest.Common.dll; DestDir: {app}; Components: msg reporting
Source: ..\bin\Release\SanteDB.Rest.HDSI.dll; DestDir: {app}; Components: msg\hdsi

; Common .NET Standard
Source: ..\bin\Release\MimeMapping.dll; DestDir: {app};
Source: ..\bin\Release\MimeTypesMap.dll; DestDir: {app};
Source: ..\bin\Release\Polly.dll; DestDir: {app};
;Source: ..\bin\Release\net*.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\System.*.dll; DestDir: {app}; 
Source: ..\bin\Release\Microsoft.*.dll; Excludes: Microsoft.Data.Sqlite.dll; DestDir: {app}; 

[UninstallDelete]
Type: filesandordirs; Name: "{app}\data\*.completed"
Type: files; Name: "{app}\santedb.xml"
Type: files; Name: "{app}\santedb.config.xml"
Type: files; Name: "{app}\ctxkey.enc"
Type: filesandordirs; Name: "{app}\datastream\*"
Type: files; Name: "{app}\xcron.xml"
Type: files; Name: "{app}\xstate.xml"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Run]
Filename: "{app}\ConfigTool.exe";  Description: "Configure SanteDB Server"; Flags: postinstall ; 
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB REST Ports"" dir=in protocol=TCP localport=8080 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; 
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB HL7 Ports"" dir=in protocol=TCP localport=2100 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; 


[UninstallRun]
Filename: "{app}\SanteDB.exe"; Parameters: "--uninstall"; StatusMsg: "Un-registering SanteDB"; Flags:runhidden runascurrentuser;


[Icons]
Name: "{commonprograms}\SanteDB\SanteDB Server Console"; Filename: "{app}\sdbac.exe"
Name: "{commonprograms}\SanteDB\SanteDB Server Configuration"; Filename: "{app}\configtool.exe"
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
    WizardForm.PreparingLabel.Caption := 'Installing Visual C++ Redistributable';
    ExtractTemporaryFile('vc_redist.x64.exe');
    Exec(ExpandConstant('{tmp}\vc_redist.x64.exe'), '/install /passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    WizardForm.PreparingLabel.Caption := 'Installing Microsoft .NET Framework 4.8';
    ExtractTemporaryFile('netfx.exe');
    Exec(ExpandConstant('{tmp}\netfx.exe'), '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

end;

// Removes the 2.0 Assets which may be present in the installation directory
procedure Remove20Assets() ;
var 
  files : Array [0..7] of string;
  i : integer;
begin
  files[0] := ExpandConstant('{app}\SanteDB.Core.dll');
  files[1] := ExpandConstant('{app}\SanteGuard.Core.dll');
  files[2] := ExpandConstant('{app}\SanteGuard.Messaging.Ami.dll');
  files[3] := ExpandConstant('{app}\SanteGuard.Messaging.Syslog.dll');
  files[4] := ExpandConstant('{app}\SanteGuard.Persistence.Ado.dll');
  files[5] := ExpandConstant('{app}\SanteMPI.Messaging.PixPdqv2.dll');
  files[6] := ExpandConstant('{app}\SanteMPI.Persistence.ADO.dll');
  
  if(FileExists(files[1]) and (MsgBox('You appear to have SanteDB 2.0.x plugins which might not be compatible with this version. Would you like to remove them?', mbConfirmation, MB_YESNO) = idYes)) then begin
    for i :=  0 to 7 do begin
      if(FileExists(files[i])) then begin
        try 
          DeleteFile(files[i]);
        except 
          ShowExceptionMessage();
        end;
      end; // if
    end; // for
  end;
end;


procedure CurPageChanged(CurPageID: Integer);
begin
  if((CurPageID = wpInstalling)) then begin
    Remove20Assets();
  end;
end;
