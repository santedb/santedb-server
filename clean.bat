@ECHO OFF
	ECHO WILL PULL SUBMODULES
	SET cwd = %cd%
	FOR /D %%G IN (.\*) DO (
		ECHO Cleaning %%G
		PUSHD %%G
		IF EXIST "bin" (
			DEL bin\*.* /s /q
		)
		IF EXIST "obj" (		
			DEL obj\*.* /s /q
		)
		IF EXIST "deploy" (
			DEL deploy\*.* /s /q
		)
		IF EXIST "TestResults" (		
			DEL TestResults\*.* /s /q
		)
				IF EXIST "dist" (		
			DEL dist\*.* /s /q
		)
		CALL ..\CLEAN.BAT
		POPD
	)
