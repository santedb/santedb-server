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
                	dotnet nuget push -s http://oss-baget.fyfesoftware.ca:8080/v3/index.json -k $2 ${N}
                done

		for N in ./bin/publish/*.snupkg; do
			dotnet nuget push -s http://oss-baget.fyfesoftware.ca:8080/v3/index.json -k $2 ${N}
		done
                rm -rfv ./bin/publish
        fi
}

if (( $# < 2 ))
then
	echo "Use: pack-publish-nuget.sh VERSION_ID YOUR_API_KEY"
	exit -1;
fi;

for S in *; do
	if [ -d "${S}" ]; then
		echo "Entering ${S}"
		cd "${S}"
		build_nuget_cwd $1 $2
		for D in *; do
			if [ -d "${D}" ]; then
				cd "${D}"
				build_nuget_cwd $1 $2
				cd ..
			fi
		done
		cd ..
	fi
done