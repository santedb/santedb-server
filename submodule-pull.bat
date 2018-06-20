@ECHO OFF

	ECHO WILL PULL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST ".git" (
			ECHO Pulling %%G
			git pull origin master
		)
		POPD
	)
