#!/bin/bash

if (( $# < 2 ))
then
	echo "Use: build-on-linux.sh VERSION_ID BRANCH_NAME"
	exit -1;
fi;

# Create output directories if not exists
mkdir -p {./bin/Release/data,./santedb-fhir/bin/Release/data,./santedb-hl7/bin/Release/data,./santedb-gs1/bin/Release/data,./santedb-mdm/bin/Release/data}

# Restore, build and compile 
./submodule-pull.sh $2
msbuild /t:clean /t:restore santedb-server-linux-ext.sln /p:VersionNumber=$1 /m
msbuild /t:build /p:Configuration=Release santedb-server-linux-ext.sln /p:VersionNumber=$1 /p:NoFirebird=1 /m

# Build the tarball structure
if [ -d santedb-server-$1 ]; then
	rmdir -r santedb-server-$1;
fi;

# MSBUILD on linux doesn't copy over documentation files for dependent projects so we're going to copy them manually
cp ./santedb-model/bin/Release/*.XML ./bin/Release/ 
cp -v ./santedb-fhir/SanteDB.Messaging.FHIR/Data/* ./bin/Release/data/
cp -v ./santedb-hl7/SanteDB.Messaging.HL7/Data/* ./bin/Release/data/
cp -v ./santedb-gs1/SanteDB.Messaging.GS1/Data/* ./bin/Release/data/
mkdir -p ./bin/Release/data/SQL
cp -rv ./SanteDB.Persistence.Data.ADO/Data/SQL/* ./bin/Release/data/SQL/

mkdir santedb-server-$1
cd santedb-server-$1
cp ../bin/Release/*.dll ./
cp ../bin/Release/*.config ./
cp ../bin/Release/*.xml ./
cp ../bin/Release/*.exe ./
cp ../bin/Release/*.exe.config ./
mkdir -p {elbonia/data,applets,data/sql,config,schema}
cp ../bin/Release/data/*.dataset ./data
cp ../bin/Release/data/SQL/* ./data/sql -r
cp ../bin/Release/config/* ./config
cp ../bin/Release/applets/*.pak ./applets
cp ../SanteDB/data/demo/* elbonia/data
cp ../SanteDB/data/*.FDB elbonia

cd ..
rmdir ./bin/dist -r
mkdir ./bin/dist -p
tar cjvf ./bin/dist/santedb-server-$1.tar.bz2 santedb-server-$1
tar czvf ./bin/dist/santedb-server-$1.tar.gz santedb-server-$1
rm -r ./santedb-server-$1

# Test if ISCC exist and if wine is present (required for building the installers)
if [ ! -f /usr/bin/wine ]; then
	echo "Skipping packaging of Windows Installer (requires Wine)"
	exit 0;
fi;
if [ ! -f /opt/inno/ISCC.exe ]; then
	echo "Skipping packaging of Windows Installer (requires Inno Setup Compiler code at /opt/inno/ISCC.EXE)"
	exit 0;
fi;

# Download the VC++ and NETFX Redist and expand the FireBird reference libraries
unzip -o ./Solution\ Items/FirebirdSQL-3.0.3-Embedded.zip -d ./bin/Release/
wget -q -O - https://download.microsoft.com/download/3/2/2/3224B87F-CFA0-4E70-BDA3-3DE650EFEBA5/vcredist_x64.exe > ./installer/vc2010.exe
wget -q -O - https://download.visualstudio.microsoft.com/download/pr/014120d7-d689-4305-befd-3cb711108212/1f81f3962f75eff5d83a60abd3a3ec7b/ndp48-web.exe > ./installer/netfx.exe
/usr/bin/wine /opt/inno/ISCC.exe /o./bin/dist ./installer/santedb-server.iss /d"MyAppVersion=$1" /d"UNSIGNED=true"
