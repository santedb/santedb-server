@ECHO OFF
IF [%1] == [] (
	echo Must specify new branch
	goto end
)

	ECHO WILL CREATE BRANCH ON ALL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST ".git" (
			
			
			
			rem Create branch FROM to TO
			git branch %1
			rem Checkout branch
			git checkout %1

		)
		POPD
	)
:end