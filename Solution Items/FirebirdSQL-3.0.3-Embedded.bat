@echo off

if exist "%2fbclient.dll" (
	echo Skip Extract of FIREBIRD
) else (
	echo "C:\Program Files\7-Zip\7z.exe" x -o%2 -y %1\FirebirdSQL-3.0.3-Embedded.zip
	"C:\Program Files\7-Zip\7z.exe" x -o%2 -y %1\FirebirdSQL-3.0.3-Embedded.zip
)