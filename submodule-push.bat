@ECHO OFF

IF %1=="" (
	ECHO MISSING COMMIT MESSAGE
) ELSE (
	ECHO WILL UPDATE SUBMODULES WITH "%1"
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST ".git" (
			ECHO Pushing %%G
			git add *
			git commit -am %1
			git pull
			git push
		)
		POPD
	)
)