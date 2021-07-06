@echo off

set signtool="C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x64\signtool.exe"
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

	%msbuild%\msbuild santedb-server-ext.sln /t:restore /p:VersionNumber=%1
	%msbuild%\msbuild santedb-server-ext.sln /t:clean /t:rebuild /p:configuration=Release /p:VersionNumber=%1 /m:1

	FOR /R "%cwd%" %%G IN (*.nuspec) DO (
		echo Packing %%~pG
		pushd "%%~pG"
		if [%2] == [] (
			%nuget% pack -OutputDirectory "%localappdata%\NugetStaging" -prop Configuration=Release  -msbuildpath %msbuild% -prop VersionNumber=%1
		) else (
			echo Publishing NUPKG
			%nuget% pack -prop Configuration=Release -msbuildpath %msbuild% -prop VersionNumber=%1
			FOR /R %%F IN (*.nupkg) do (
				%nuget% push "%%F" -Source https://api.nuget.org/v3/index.json -ApiKey %2 
			)
		) 
		popd
	)

	FOR /R "%cwd%\bin\Release" %%G IN (*.exe) DO (
		echo Signing %%G
		%signtool% sign /d "SanteDB iCDR"  "%%G"
	)
	
	%inno% "/o.\bin\dist" ".\installer\SanteDB-Server.iss" /d"MyAppVersion=%version%" 

	rem ################# TARBALLS 
	echo Building Linux Tarball

	 mkdir santedb-server-%version%
	cd santedb-server-%version%
	copy "..\bin\Release\*.dll"
	copy "..\bin\Release\*.exe"
	copy "..\bin\Release\*.exe.config"
	
	copy "..\bin\Release\*.pak"
	xcopy /I "..\bin\Release\Schema\*.*" ".\schema"
	xcopy /I /E "..\bin\Release\Data\*.*" ".\data"
	xcopy /I "..\bin\Release\Applets\*.*" ".\applets"
	xcopy /I "..\bin\Release\Config\*.*" ".\config"
	xcopy /I "..\bin\Release\Plugins\*.*" ".\plugins"
	mkdir elbonia
	mkdir elbonia\data
	copy "..\SanteDB\Data\*.fdb" elbonia
	copy "..\SanteDB\Data\demo\*.*" elbonia\data
	cd ..
	"C:\program files\7-zip\7z" a -r -ttar .\bin\dist\santedb-server-%version%.tar .\santedb-server-%version%
	"C:\program files\7-zip\7z" a -r -tzip .\bin\dist\santedb-server-%version%.zip .\santedb-server-%version%
	"C:\program files\7-zip\7z" a -tbzip2 .\bin\dist\santedb-server-%version%.tar.bz2 .\bin\dist\santedb-server-%version%.tar
	"C:\program files\7-zip\7z" a -tgzip .\bin\dist\santedb-server-%version%.tar.gz .\bin\dist\santedb-server-%version%.tar
	del /q /s .\installsupp\*.* 
	del /q /s .\santedb-server-%version%\*.*
	rmdir /q /s .\santedb-server-%version%
	rmdir /q/s .\installsupp

	call package-sdbac.bat %version%
	cd santedb-docker
	cd SanteDB.Docker.Server
	cd bin\Release
        ren Data data
	cd ..\..
	docker build --no-cache -t santesuite/santedb-icdr:%version% .
	docker build --no-cache -t santesuite/santedb-icdr .
	cd ..
	cd ..
) else (	
	echo Cannot find NUGET 
)

:eof