@ECHO OFF
IF [%1] == [] (
	echo Must specify branch
	goto end
)
	ECHO WILL PULL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (*) DO (
		git submodule set-branch --branch version/3.0 %%G
		PUSHD %%G
		IF EXIST ".git" (
			ECHO Pulling %1 on %%G
			git checkout %1
			git pull
		)
		POPD
	)
:end