#!/bin/bash
declare build_dir=`pwd`

build_nuget_cwd() {
	if [ -f *.nuspec ]; then
        	mono ${build_dir}/.nuget/nuget.exe pack -OutputDirectory bin/publish -prop Configuration=Release -prop VersionNumber=$1
        elif [ -f *.csproj ]; then
        	dotnet pack --no-build --configuration Release --output bin/publish /p:VersionNumber=$1
        fi
        if [ -d ./bin/publish ]; then
        	for N in ./bin/publish/*.nupkg; do
                	dotnet nuget push -s http://oss-baget.fyfesoftware.ca:8080/v3/index.json -k $3 ${N}
                done
                rm -rfv ./bin/publish
        fi
}

if (( $# < 2 ))
then
	echo "Use: build-on-linux.sh VERSION_ID BRANCH_NAME"
	exit -1;
fi;

if (( $# > 2 ))
then
for S in *; do
	if [ -d "${S}" ]; then
		echo "Entering ${S}"
		cd "${S}"
		build_nuget_cwd $1 $2 $3
		for D in *; do
			if [ -d "${D}" ]; then
				cd "${D}"
				build_nuget_cwd $1 $2 $3
				cd ..
			fi
		done
		cd ..
	fi
done
fi

