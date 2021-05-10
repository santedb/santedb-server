@echo off
mkdir sdbac-%1
pushd sdbac-%1
copy "..\bin\Release\Antlr3.Runtime.dll"
copy "..\bin\Release\Esprima.dll"
copy "..\bin\Release\ExpressionEvaluator.dll"
copy "..\bin\Release\Jint.dll"
copy "..\bin\Release\MohawkCollege.Util.Console.Parameters.dll"
copy "..\bin\Release\Newtonsoft.Json.dll"
copy "..\bin\Release\RazorTemplates.Core.dll"
copy "..\bin\Release\RestSrvr.dll"
copy "..\bin\Release\SanteDB.BI.dll"
copy "..\bin\Release\SanteDB.BusinessRules.JavaScript.dll"
copy "..\bin\Release\SanteDB.Configuration.dll"
copy "..\bin\Release\SanteDB.Core.Api.dll"
copy "..\bin\Release\SanteDB.Core.Applets.dll"
copy "..\bin\Release\SanteDB.Core.Model.AMI.dll"
copy "..\bin\Release\SanteDB.Core.Model.dll"
copy "..\bin\Release\SanteDB.Core.Model.RISI.dll"
copy "..\bin\Release\SanteDB.Core.Model.ViewModelSerializers.dll"
copy "..\bin\Release\SanteDB.Docker.Core.dll"
copy "..\bin\Release\SanteDB.Messaging.AMI.Client.dll"
copy "..\bin\Release\SanteDB.Messaging.HDSI.Client.dll"
copy "..\bin\Release\SanteDB.OrmLite.dll"
copy "..\bin\Release\SanteDB.Rest.Common.dll"
copy "..\bin\Release\SanteDB.Server.AdminConsole.Api.dll"
copy "..\bin\Release\SanteDB.Server.Core.dll"
copy "..\bin\Release\sdbac.exe"
copy "..\bin\Release\sdbac.exe.config"
copy "..\bin\Release\SharpCompress.dll"
copy "..\bin\Release\System.Buffers.dll"
copy "..\bin\Release\System.IdentityModel.Tokens.Jwt.dll"
copy "..\bin\Release\System.Memory.dll"
copy "..\bin\Release\System.Numerics.Vectors.dll"
copy "..\bin\Release\System.Runtime.CompilerServices.Unsafe.dll"
popd
"C:\program files\7-zip\7z" a -r -ttar .\bin\dist\sdbac-%1.tar .\sdbac-%1
"C:\program files\7-zip\7z" a -r -tzip .\bin\dist\sdbac-%1.zip .\sdbac-%1
"C:\program files\7-zip\7z" a -tbzip2 .\bin\dist\sdbac-%1.tar.bz2 .\bin\dist\sdbac-%1.tar
"C:\program files\7-zip\7z" a -tgzip .\bin\dist\sdbac-%1.tar.gz .\bin\dist\sdbac-%1.tar
rmdir /s /q sdbac-%1