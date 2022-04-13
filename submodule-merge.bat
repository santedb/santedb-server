@ECHO OFF
IF [%1] == [] (
	echo Must specify from branch
	goto end
)
IF [%2] == [] (
	echo Must specify to branch
	goto end
)
	ECHO WILL PULL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST ".git" (
			
			
			rem pull the FROM branch
			git checkout %1
			git pull
			rem pull the TO branch
			git checkout %2
			git pull
			rem merge FROM to TO
			git merge %1

		)
		POPD
	)
:end