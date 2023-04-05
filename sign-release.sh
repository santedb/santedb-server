#!/bin/bash

sign_all_asms() {
	if [ -d $1 ]; then
	echo "Signing Assemblies in `pwd`/$1"
	for S in $1/{SanteDB*.exe,SanteMPI*.exe,SanteIMS*.exe,SanteGuard*.exe,Sante*.exe,SanteDB*.dll,SanteMPI*.dll,SanteIMS*.dll,SanteGuard*.dll,Sante*.dll}; do
        	if [ -f "${S}" ]; then
			echo "Signing `pwd`/${S}"
			id `whoami` | sed -E "s/(.{60}).*$/\1/" | signcode -spc ~/.secret/authenticode.spc -v ~/.secret/authenticode.pvk  -a sha1 -$ commercial -n SanteDB\ Server -i http://santesuite.com/ -t http://timestamp.digicert.com/scripts/timstamp.dll -tr 10 ${S}
                fi
        done
	fi
}

sign_all_recursive() {
	if [ -d "./bin/Release/netstandard2.0" ]; then
		sign_all_asms "bin/Release/netstandard2.0"
	elif [ -d "./bin/Release/net4.8" ]; then
		sign_all_asms "bin/Release/net4.8"
	elif [ -d "./bin/Release" ]; then
		sign_all_asms "bin/Release"
	fi
	for D in *; do
		if [ -d "${D}" ]; then
			cd "${D}"
			sign_all_recursive
			cd ..
		fi
	done
}

if [ -d ~/.secret ]; then
	sign_all_recursive
fi

