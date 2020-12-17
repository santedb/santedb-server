#!/bin/bash
if [ -f .gitmodules ]; then
	git submodule update --remote
	for S in *; do
		if [ -d "${S}" ]; then
			cd "${S}"
			if [ -f .git ]; then
				git checkout $1
				git pull --ff-only
			fi
			cd ..
		fi
	done 
fi
