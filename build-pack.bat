@echo off

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" (
        echo will use VS 2017 build tools
        set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
) else (
	if exist "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
        echo will use VS 2017 build tools
        set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
	) else ( echo Unable to locate VS 2017 build tools, will use default build tools )
)

set cwd = %cd%
echo Will use NUGET in %cd%
echo Will use MSBUILD in %msbuild%

FOR /R %cwd% %%G IN (*.sln) DO (
	echo Building %%~pG 
	pushd %%~pG
	%msbuild% %%G /t:rebuild /p:configuration=release /m
	popd
)

FOR /R %cwd% %%G IN (*.nuspec) DO (
	echo Packing %%~pG
	pushd %%~pG
	%cd%\nuget.exe pack -OutputDirectory %localappdata%\NugetStaging -prop Configuration=Release
	popd
)