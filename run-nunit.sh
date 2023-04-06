#!/bin/bash

declare cwd=`pwd`

test_run() {
	if [ -d $1 ]; then
		echo "Discovering Test Projects in `pwd`/$1"
		for S in $1/*Test*.dll; do
        		if [ -f "${S}" ]; then
					if [ "${S}" == "NUnit3.TestAdapter.dll" ]; then 
						echo Skipping
					else
						echo "Executing Tests in `pwd`/${S}"
						mono /opt/nunit3/nunit3-console.exe "${S}"
	                fi
				fi
        	done
	fi
}

test_all_recursive() {
	if [ -d "./bin/Release/netstandard2.0" ]; then
		test_run "bin/Release/netstandard2.0"
	fi 
	if [ -d "./bin/Release/net4.8" ]; then
		test_run "bin/Release/net4.8"
	fi
	if [ -d "./bin/Release/net48" ]; then
		test_run "bin/Release/net48"
	fi 
	if [ -d "./bin/Release" ]; then
		test_run "bin/Release"
	fi 
	if [ -d "./bin/Debug/netstandard2.0" ]; then
		test_run "bin/Debug/netstandard2.0"
	fi 
	if [ -d "./bin/Debug/net4.8" ]; then
		test_run "bin/Debug/net4.8"
	fi 
	if [ -d "./bin/Debug/net48" ]; then
		test_run "bin/Debug/net48"
	fi
	if [ -d "./bin/Debug" ]; then
		test_run "bin/Debug"
	fi
	for D in *; do
		if [ -d "${D}" ]; then
			cd "${D}"
			test_all_recursive
			cd ..
		fi
	done
}

test_all_recursive
