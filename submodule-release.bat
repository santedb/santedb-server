@ECHO OFF

IF %2=="" (
	ECHO USE SUBMODULE-RELEASE FROM_BRANCH VERSION_ID
) ELSE (
	ECHO WILL RELEASE SUBMODULES ON "%1"
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD "%%G"
		IF EXIST ".git" (
			ECHO Pushing %%G
			git checkout %1
			git add *
			git commit -am "Release of %2"
			git pull
			git push
			git checkout master
			git pull
			git merge %1
			git tag -a v%2 -m "Release of %2"
			git push 
			git push --tags
			git checkout %1
		)
		POPD
	)
)