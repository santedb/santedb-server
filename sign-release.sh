#!/bin/bash

sign_all_asms() {
	for S in ./bin/Release/{SanteDB*.exe,SanteMPI*.exe,SanteIMS*.exe,SanteGuard*.exe,Sante*.exe,SanteDB*.dll,SanteMPI*.dll,SanteIMS*.dll,SanteGuard*.dll,Sante*.dll}; do
        	if [ -f ${S} ]; then
			id `whoami` | sed -E "s/(.{60}).*$/\1/" | signcode -spc ~/.secret/authenticode.spc -v ~/.secret/authenticode.pvk  -a sha1 -$ commercial -n SanteDB\ Server -i http://santesuite.com/ -t http://timestamp.digicert.com/scripts/timstamp.dll -tr 10 ${S}
                fi
        done
}

sign_all_recursive() {
	sign_all_asms
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

