@ECHO OFF

IF %2=="" (
	ECHO USE SUBMODULE-PUSH BRANCH MESSAGE
) ELSE (
	ECHO WILL UPDATE SUBMODULES ON "%1"
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD "%%G"
		IF EXIST ".git" (
			ECHO Pushing %%G
			git checkout %1
			git add *
			git commit -am %2
			git pull
			git push
			git push --tags
		)
		POPD
	)
)