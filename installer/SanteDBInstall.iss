  ; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "SanteDB Server"
#define MyAppPublisher "SanteDB Community"
#define MyAppURL "http://santesuite.org"
#define MyAppVersion "2.0.35"
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
AppCopyright = Copyright (C) 2015-2020 SanteSuite Contributors
ArchitecturesInstallIn64BitMode = x64
ArchitecturesAllowed =  x64
WizardStyle=modern
SignedUninstaller=yes
SignTool=default sign /a /n $qFyfe Software$q /d $qSanteDB iCDR Server$q $f

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
Name: mpi; Description: SanteMPI Only
Name: guard; Description: SanteGuard Only
Name: custom; Description: Custom Installation; Flags: iscustom

[Components]
Name: core; Description: SanteDB Core; Types: full imsi ami auth bis demo guard mpi
Name: core\bre; Description: JInt Business Rules Engine; Types: full demo guard mpi
Name: core\protocol; Description: XML Clinical Support Decision Engine; Types: full demo
Name: server; Description: SanteDB Service Host; Types: full demo guard mpi
Name: msg; Description: Core Messaging Interfaces; Types: full demo guard mpi
Name: msg\hdsi; Description: Health Administration Interface; Types: full imsi demo guard mpi
Name: msg\ami; Description: Administration Management Interface; Types: full ami demo guard mpi
Name: msg\auth; Description: OAuth2.0 Authentication Server; Types: full auth demo guard mpi
Name: bi; Description: Business Intelligence Services; Types: full auth demo guard mpi
Name: interop; Description: Integration Interfaces; Types: full demo guard mpi
Name: interop\fhir; Description: HL7 Fast Health Integration Resources; Types: full demo guard mpi
Name: interop\hl7; Description: HL7v2 Messaging; Types: full demo mpi
Name: interop\gs1; Description: GS1 BMS Messaging; Types: full demo
Name: interop\jira; Description: JIRA Integration; Types: full
Name: interop\atna; Description: ATNA & DICOM Auditing; Types: full
Name: interop\openapi; Description: OpenAPI; Types: full demo
Name: reporting; Description: Reporting Services; Types: full
Name: reporting\bis; Description: Business Intelligence Services; Types: full bis guard mpi
Name: reporting\risi; Description: Report Integration Service (Legacy); Types: full 
Name: reporting\jasper; Description: Jasper Reports Server Integration (Legacy); Types: full
Name: tfa; Description: Two Factor Authentication; Types: full
Name: tfa\twilio; Description: Twilio SMS TFA Adapter; Types: full
Name: tfa\email; Description: Email TFA Adapter; Types: full
Name: mdm; Description: Master Data Management (MDM); Types: full mpi
Name: match; Description: Record Matcher (SanteMatch); Types: full mpi
Name: db; Description: Data Persistence; Types: full demo guard mpi
Name: db\fbsql; Description: FirebirdSQL Persistence Services; Types: full demo guard mpi
Name: db\psql; Description: PostgreSQL Persistence Services; Types: full guard mpi
Name: cache; Description: Memory Caching Services; Types: full demo guard mpi
Name: cache\redis; Description: REDIS Shared Memory Caching; Types: full guard mpi
Name: tools; Description: Management Tooling; Types: full demo
Name: mpi; Description: SanteMPI Plugins; Types: full demo mpi
Name: guard; Description: SanteGuard Plugins; Types: full demo guard
Name: demo; Description: Elbonia Quickstart; Types: demo

[Files]

; Microsoft .NET Framework 4.5 Installation
Source: .\netfx.exe; DestDir: {tmp} ; Flags: dontcopy

; VC Redist for FBSQL
Source: .\vc2010.exe; DestDir: {tmp} ; Flags: dontcopy

; Firebird SQL 
Source: ..\SanteDB\Data\SDB_BASE.FDB; DestDir: {app}; Components: demo 
Source: ..\SanteDB\Data\SDB_AUDIT.FDB; DestDir: {app}; Components: demo
Source: ..\bin\Release\fbclient.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\fbembed.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\FirebirdSql.Data.FirebirdClient.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\ib_util.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\icudt52.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\icuin52.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\icuuc52.dll; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\plugins\engine12.dll; DestDir: {app}\plugins; Components: db\fbsql
Source: ..\bin\Release\icudt52l.dat; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\firebird.conf; DestDir: {app}; Components: db\fbsql
Source: ..\bin\Release\firebird.msg; DestDir: {app}; Components: db\fbsql

; Demo Data
Source: ..\SanteDB\Data\Demo\*.dataset; DestDir: {app}\data; Components: demo
Source: ..\SanteDB\santedb.config.dev.xml; DestDir: {app}; DestName: santedb.config.xml; Components: demo

; Security AMI stuff
Source: ..\bin\Release\SanteDB.Core.Model.AMI.dll; DestDir: {app}; Components: msg\ami
Source: ..\bin\Release\SanteDB.Messaging.AMI.dll; DestDir: {app}; Components: msg\ami
Source: ..\bin\Release\SanteDB.Rest.AMI.dll; DestDir: {app}; Components: msg\ami

; Data Stuff
Source: ..\bin\release\data\*.dataset; DestDir: {app}\data; Components: server
Source: ..\bin\release\applets\*.pak; DestDir: {app}\applets; Components: server

; ADO Stuff
Source: ..\bin\release\DATA\SQL\FBSQL\*.sql; DestDir: {app}\sql\fbsql; Components: db\fbsql
Source: ..\bin\release\DATA\SQL\Updates\*-FBSQL.sql; DestDir: {app}\sql\updates; Components: db\fbsql
Source: ..\bin\release\DATA\SQL\PSQL\*.sql; DestDir: {app}\sql\psql; Components: db\psql
Source: ..\bin\release\DATA\SQL\Updates\*-PSQL.sql; DestDir: {app}\sql\updates; Components: db\psql
Source: ..\bin\release\DATA\SQL\PSQL\*.sql; DestDir: {app}\sql; Components: db\psql

; Tools
Source: ..\bin\release\sdbac.exe; DestDir: {app}; Components: tools
Source: ..\bin\release\SanteDB.Messaging.AMI.Client.dll; DestDir: {app}; Components: tools
Source: ..\bin\release\SanteDB.Tools.DataSandbox.dll; DestDir: {app}; Components: tools

; Demo
Source: ..\SanteDB\Data\Demo\*.dataset; DestDir: {app}\data; Components: demo
Source: ..\SanteDB\santedb.config.dev.xml; DestDir: {app}; DestName: santedb.config.xml; Components: demo
; 

;Documentation For OpenAPI
Source: ..\bin\Release\RestSrvr.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Authentication.OAuth2.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.BI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Core.Applets.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Core.Model.AMI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Core.Model.RISI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Core.Model.ViewModelSerializers.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Core.Model.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.AMI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.FHIR.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.GS1.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.HDSI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.Metadata.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Messaging.RISI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.AMI.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.BIS.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.Common.xml; DestDir: {app}; Components: interop\openapi
Source: ..\bin\Release\SanteDB.Rest.HDSI.xml; DestDir: {app}; Components: interop\openapi
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
Source: ..\bin\Release\SanteDB.Core.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.Core.Model.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\SharpCompress.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.Core.Security.Tfa.Email.dll; DestDir: {app}; Components: tfa\email
Source: ..\bin\Release\SanteDB.Messaging.AMI.Client.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.Messaging.HDSI.Client.dll; DestDir: {app}; Components: core
Source: ..\bin\Release\SanteDB.OrmLite.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.Auditing.ADO.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.Data.ADO.dll; DestDir: {app}; Components: server
Source: ..\bin\Release\SanteDB.Persistence.Diagnostics.Email.dll; DestDir: {app}; Components: server

; Common BRE
Source: ..\bin\Release\Antlr3.Runtime.dll; DestDir: {app}; Components: core\bre core\protocol core
Source: ..\bin\Release\ExpressionEvaluator.dll; DestDir: {app}; Components: core\bre core\protocol                              
Source: ..\bin\Release\Jint.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\SanteDB.BusinessRules.JavaScript.dll; DestDir: {app}; Components: core\bre
Source: ..\bin\Release\SanteDB.Cdss.Xml.dll; DestDir: {app}; Components: core\protocol

; ATNA
Source: ..\bin\Release\AtnaApi.dll; DestDir: {app}; Components: interop\atna

; FHIR R4 Support
Source: ..\bin\Release\Hl7.Fhir.ElementModel.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.R4.Core.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.Serialization.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.Support.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.Fhir.Support.Poco.dll; DestDir: {app}; Components: interop\fhir
Source: ..\bin\Release\Hl7.FhirPath.dll; DestDir: {app}; Components: interop\fhir
Source: ..\SanteDB.Messaging.FHIR\Data\*.dataset; DestDir: {app}\data; Components: interop\fhir
Source: ..\bin\Release\SanteDB.Messaging.FHIR.dll; DestDir: {app}; Components: interop\fhir

; Twilio Integration
Source: ..\bin\Release\JWT.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\Release\RestSharp.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\Release\Twilio.Api.dll; DestDir: {app}; Components: tfa\twilio
Source: ..\bin\Release\SanteDB.Core.Security.Tfa.Twilio.dll; DestDir: {app}; Components: tfa\twilio

; HL7v2
Source: ..\bin\Release\NHapi.Base.dll; DestDir: {app}; Components: interop\hl7
Source: ..\bin\Release\NHapi.Model.V231.dll; DestDir: {app}; Components: interop\hl7
Source: ..\bin\Release\NHapi.Model.V25.dll; DestDir: {app}; Components: interop\hl7
Source: ..\SanteDB.Messaging.HL7\Data\*.dataset; DestDir: {app}\data; Components: interop\hl7
Source: ..\bin\Release\SanteDB.Messaging.HL7.dll; DestDir: {app}; Components: interop\hl7

; NPSQL
Source: ..\bin\Release\Npgsql.dll; DestDir: {app}; Components: db\psql

; Matching Infrastructure
Source: ..\bin\Release\Phonix.dll; DestDir: {app}; Components: match
Source: ..\bin\Release\SanteDB.Matcher.dll; DestDir: {app}; Components: match

; OAUTH
Source: ..\bin\Release\SanteDB.Authentication.OAuth2.dll; DestDir: {app}; Components: msg\auth
Source: ..\SanteDB.Authentication.OAuth\Data\*.dataset; DestDir: {app}\data; Components: msg\auth

; BI REPORTING
Source: ..\bin\Release\SanteDB.BI.dll; DestDir: {app}; Components: reporting\bis
Source: ..\bin\Release\SanteDB.Rest.BIS.dll; DestDir: {app}; Components: reporting\bis

; Caching 
Source: ..\bin\Release\SanteDB.Caching.Memory.dll; DestDir: {app}; Components: cache
Source: ..\bin\Release\SanteDB.Caching.Redis.dll; DestDir: {app}; Components: cache\redis
Source: ..\bin\Release\StackExchange.Redis.dll; DestDir: {app}; Components: cache\redis

; Core Messaging
Source: ..\bin\Release\SanteDB.Core.Model.ViewModelSerializers.dll; DestDir: {app}; Components: msg\hdsi msg\ami

; Atna Interop
Source: ..\bin\Release\SanteDB.Messaging.Atna.dll; DestDir: {app}; Components: interop\atna

; GS1
Source: ..\bin\Release\SanteDB.Messaging.GS1.dll; DestDir: {app}; Components: interop\gs1
Source: ..\SanteDB.Messaging.GS1\Data\*.dataset; DestDir: {app}\data; Components: interop\gs1

; JDSO
Source: ..\bin\Release\SanteDB.Messaging.HDSI.dll; DestDir: {app}; Components: msg\hdsi

; JIRA Integration
Source: ..\bin\Release\SanteDB.Persistence.Diagnostics.Jira.dll; DestDir: {app}; Components: interop\jira

; MDM INfrastructure
Source: ..\bin\Release\SanteDB.Persistence.MDM.dll; DestDir: {app}; Components: mdm
Source: ..\santedb-mdm\SanteDB.Persistence.MDM\Data\*.dataset; DestDir: {app}\data; Components: mdm

; Jasper Report
Source: ..\bin\Release\SanteDB.Reporting.Jasper.dll; DestDir: {app}; Components: reporting\jasper


Source: ..\bin\Release\SanteDB.Rest.Common.dll; DestDir: {app}; Components: msg reporting
Source: ..\bin\Release\SanteDB.Rest.HDSI.dll; DestDir: {app}; Components: msg\hdsi

; Legacy RISI
Source: ..\bin\Release\SanteDB.Core.Model.RISI.dll; DestDir: {app}; Components: reporting\risi
Source: ..\bin\Release\SanteDB.Messaging.RISI.dll; DestDir: {app}; Components: reporting\risi
Source: ..\bin\Release\SanteDB.Persistence.Reporting.ADO.dll; DestDir: {app}; Components: reporting\risi
Source: ..\bin\Release\SanteDB.Reporting.Core.dll; DestDir: {app}; Components: reporting\risi
Source: ..\bin\Release\SanteDB.Warehouse.ADO.dll; DestDir: {app}; Components: reporting\risi

; Common .NET Standard
Source: ..\bin\Release\Microsoft.Bcl.AsyncInterfaces.dll; DestDir: {app}; 
Source: ..\bin\Release\Microsoft.Diagnostics.Runtime.dll; DestDir: {app}; 
Source: ..\bin\Release\Microsoft.Win32.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\netstandard.dll; DestDir: {app}; Components: core server
Source: ..\bin\Release\System.AppContext.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Buffers.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Collections.Concurrent.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Collections.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Collections.NonGeneric.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Collections.Specialized.dll; DestDir: {app}; 
Source: ..\bin\Release\System.ComponentModel.dll; DestDir: {app}; 
Source: ..\bin\Release\System.ComponentModel.EventBasedAsync.dll; DestDir: {app}; 
Source: ..\bin\Release\System.ComponentModel.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.ComponentModel.TypeConverter.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Console.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Data.Common.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Data.DataSetExtensions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.Contracts.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.Debug.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.FileVersionInfo.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.Process.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.StackTrace.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.TextWriterTraceListener.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.Tools.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.TraceSource.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Diagnostics.Tracing.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Drawing.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Dynamic.Runtime.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Globalization.Calendars.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Globalization.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Globalization.Extensions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IdentityModel.Tokens.Jwt.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.Compression.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.Compression.ZipFile.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.FileSystem.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.FileSystem.DriveInfo.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.FileSystem.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.FileSystem.Watcher.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.IsolatedStorage.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.MemoryMappedFiles.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.Pipes.dll; DestDir: {app}; 
Source: ..\bin\Release\System.IO.UnmanagedMemoryStream.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Linq.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Linq.Expressions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Linq.Parallel.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Linq.Queryable.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Memory.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.Http.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.NameResolution.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.NetworkInformation.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.Ping.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.Requests.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.Security.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.Sockets.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.WebHeaderCollection.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.WebSockets.Client.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Net.WebSockets.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Numerics.Vectors.dll; DestDir: {app}; 
Source: ..\bin\Release\System.ObjectModel.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Reflection.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Reflection.Extensions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Reflection.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Resources.Reader.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Resources.ResourceManager.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Resources.Writer.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.CompilerServices.Unsafe.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.CompilerServices.VisualC.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Extensions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Handles.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.InteropServices.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.InteropServices.RuntimeInformation.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Numerics.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Serialization.Formatters.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Serialization.Json.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Serialization.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Runtime.Serialization.Xml.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Claims.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Cryptography.Algorithms.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Cryptography.Csp.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Cryptography.Encoding.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Cryptography.Primitives.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Cryptography.X509Certificates.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.Principal.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Security.SecureString.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Text.Encoding.CodePages.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Text.Encoding.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Text.Encoding.Extensions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Text.Encodings.Web.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Text.Json.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Text.RegularExpressions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.Overlapped.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.Tasks.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.Tasks.Extensions.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.Tasks.Parallel.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.Thread.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.ThreadPool.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Threading.Timer.dll; DestDir: {app}; 
Source: ..\bin\Release\System.ValueTuple.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Xml.ReaderWriter.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Xml.XDocument.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Xml.XmlDocument.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Xml.XmlSerializer.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Xml.XPath.dll; DestDir: {app}; 
Source: ..\bin\Release\System.Xml.XPath.XDocument.dll; DestDir: {app}; 

; SanteMPI DLLS
Source: ..\..\sante-mpi\bin\Release\SanteMPI.Persistence.Ado.dll; DestDir: {app}; Components: mpi
Source: ..\..\sante-mpi\bin\Release\SanteMPI.Messaging.PixPdqV2.dll; DestDir: {app}; Components: mpi

; SanteGuard
Source: ..\..\sante-guard\bin\Release\SanteGuard.Core.dll; DestDir: {app}; Components: guard
Source: ..\..\sante-guard\bin\Release\SanteGuard.Messaging.Ami.dll; DestDir: {app}; Components: guard
Source: ..\..\sante-guard\bin\Release\SanteGuard.Messaging.Syslog.dll; DestDir: {app}; Components: guard
Source: ..\..\sante-guard\bin\Release\SanteGuard.Persistence.Ado.dll; DestDir: {app}; Components: guard

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Run]
;Filename: "{app}\ConfigTool.exe";  Description: "Configure SanteDB Server"; Flags: postinstall ; 
Filename: "{app}\SanteDB.exe"; Parameters:"--install"; Flags: runhidden runascurrentuser; StatusMsg: "Registering SanteDB Service"
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB REST Ports"" dir=in protocol=TCP localport=8080 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; 
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB HL7 Ports"" dir=in protocol=TCP localport=2100 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; 
Filename: "c:\windows\system32\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SanteDB UDP Ports"" dir=in protocol=UDP localport=514 action=allow"; StatusMsg: "Configuring Firewall"; Flags: runhidden; Components: guard
Filename: "net.exe";StatusMsg: "Starting Services..."; Parameters: "start santedb"; Flags: runhidden; Components: demo


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
    ExtractTemporaryFile('vc2010.exe');
    Exec(ExpandConstant('{tmp}\vcredist_x86.exe'), '/install /passive', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    WizardForm.PreparingLabel.Caption := 'Installing Microsoft .NET Framework 4.8';
     ExtractTemporaryFile('netfx.exe');
    Exec(ExpandConstant('{tmp}\netfx.exe'), '/q', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
end;