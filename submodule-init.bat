@ECHO OFF
git submodule init
git submodule update --remote
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST ".git" (
			ECHO Checkout master for %%G
			git checkout master
		)
		POPD
	)
