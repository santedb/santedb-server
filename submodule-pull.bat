@ECHO OFF
git submodule update --remote
	ECHO WILL PULL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST ".git" (
			ECHO Pulling %%G
			git checkout master
			git pull
		)
		POPD
	)
