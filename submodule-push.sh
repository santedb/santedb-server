#!/bin/bash
if [ -f .gitmodules ]; then
	git submodule update --remote
	for S in *; do
		if [ -d "${S}" ]; then
			cd "${S}"
			if [ -f .git ]; then
				git checkout $1
				git pull --ff-only
				git add *
				git commit -am $2
				git push
			fi
			cd ..
		fi
	done 
fi
