@echo off

set version=%1

		if exist "c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\15.0\Bin\MSBuild.exe" (
	        	echo will use VS 2019 Community build tools
        		set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\15.0\Bin"
		) else ( 
			if exist "c:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        			set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
	        		echo will use VS 2019 Pro build tools
			) else (
				echo Unable to locate VS 2019 build tools, will use default build tools on path
			)
		)

if exist "c:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
	set inno="c:\Program Files (x86)\Inno Setup 6\ISCC.exe"
) else (
	if exist "c:\Program Files (x86)\Inno Setup 5\ISCC.exe" (
		set inno="c:\Program Files (x86)\Inno Setup 5\ISCC.exe"
	) else (
		echo Can't Find INNO Setup Tools
		goto :eof
	)
)

set cwd=%cd%
set nuget="%cwd%\.nuget\nuget.exe"
echo Will build version %version%
echo Will use NUGET in %nuget%
echo Will use MSBUILD in %msbuild%

if exist "%nuget%" (
	%msbuild%\msbuild santedb-server-ext.sln /t:restore
	%msbuild%\msbuild santedb-server-ext.sln /t:clean /t:rebuild /p:configuration=Release /m:1

	FOR /R "%cwd%" %%G IN (*.nuspec) DO (
		echo Packing %%~pG
		pushd "%%~pG"
		if [%2] == [] (
			%nuget% pack -OutputDirectory "%localappdata%\NugetStaging" -prop Configuration=Release  -msbuildpath %msbuild%
		) else (
			echo Publishing NUPKG
			%nuget% pack -prop Configuration=Release -msbuildpath %msbuild%
			FOR /R %%F IN (*.nupkg) do (
				%nuget% push "%%F" -Source https://api.nuget.org/v3/index.json -ApiKey %2 
			)
		) 
		popd
	)

	FOR /R "%cwd%\bin\Release" %%G IN (*.exe) DO (
		echo Signing %%G
		"C:\Program Files (x86)\Windows Kits\8.1\bin\x86\signtool.exe" sign /d "SanteDB iCDR"  "%%G"
	)
	
	%inno% "/o.\bin\dist" ".\installer\SanteDB-Server.iss" /d"MyAppVersion=%version%" /d"x64" /d"BUNDLED"
	%inno% "/o.\bin\dist" ".\installer\SanteDB-Server.iss" /d"MyAppVersion=%version%" /d"BUNDLED"
	%inno% "/o.\bin\dist" ".\installer\SanteDB-Server.iss" /d"MyAppVersion=%version%" /d"x64" 


) else (	
	echo Cannot find NUGET 
)

:eof