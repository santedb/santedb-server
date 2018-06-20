@echo off
set cwd = %cd%
echo Will use NUGET in %cd%

FOR /R %cwd% %%G IN (*.nuspec) DO (
	echo Building %%~pG
	pushd %%~pG
	%cd%\nuget.exe pack -OutputDirectory %localappdata%\NugetStaging
	popd
)