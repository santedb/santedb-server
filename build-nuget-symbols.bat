@echo off

	if [%msbuild%] == [] (
	if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MsBuild.exe" (
	        	echo will use VS 2022 Pro build tools
        		set msbuild="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin"
	) else (
		if exist "c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\15.0\Bin\MSBuild.exe" (
				set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\15.0\Bin"
		) else ( 
			if exist "c:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
					set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
			) else (
				echo Unable to locate VS 2019 build tools, set msbuild environment variable manually
				set shouldexit=1
			)
		)
	)
)

	

set cwd=%cd%
set nuget="%cwd%\.nuget\nuget.exe"
echo Will use NUGET in %nuget%
echo Will use MSBUILD in %msbuild%

IF [%1] == []  (
	%msbuild%\msbuild santedb-server-ext.sln /t:clean /t:restore 
	%msbuild%\msbuild santedb-server-ext.sln /t:build /p:configuration=debug  /m
) ELSE (
	%msbuild%\msbuild santedb-server-ext.sln /t:clean /t:restore /p:VersionNumber=%1
	%msbuild%\msbuild santedb-server-ext.sln /t:build /p:configuration=debug /p:VersionNumber=%1 /m
	%msbuild%\msbuild santedb-server-ext.sln /t:pack /p:configuration=debug /p:VersionNumber=%1 /m
)

FOR /R "%cwd%" %%G IN (*.nuspec) DO (
	echo Packing %%~pG
	pushd "%%~pG"
	IF [%1] == []  (
		%nuget% pack -OutputDirectory "%localappdata%\NugetStaging" -prop VersionNumber=2.1.0-debug -prop Configuration=Debug -symbols -msbuildpath %msbuild%
	) ELSE (
		%nuget% pack -OutputDirectory "%localappdata%\NugetStaging" -prop VersionNumber=%1 -prop Configuration=Debug -symbols -msbuildpath %msbuild%
	)
	popd
)