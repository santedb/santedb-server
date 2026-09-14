@echo off

SET cwd=%cd%

dotnet restore santedb-server.slnx
dotnet build -c Release santedb-server.slnx

echo Build Completed Successfully