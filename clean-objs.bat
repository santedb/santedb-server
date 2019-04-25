@ECHO OFF
	ECHO WILL PULL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		PUSHD %%G
		IF EXIST "bin" (
			DEL bin\*.* /s /q
		)
		IF EXIST "obj" (		
			DEL obj\*.* /s /q
		)
		POPD
	)
