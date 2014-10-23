@echo off
echo Chinook Database Version 1.4
echo.

SET MdfDelete="%UserProfile%\Chinook.mdf"
SET LdfDelete="%UserProfile%\Chinook_log.ldf"
rem set to 11.0 VS2012 (Sql 2012) ou 12.0 VS2013 (Sql 2014)
set localdb_version="11.0"

if "%1"=="" goto MENU
if not exist %1 goto ERROR

set SQLFILE=%1
goto RUNSQL

:ERROR
echo The file %1 does not exist.
echo.
goto END

:MENU
echo Options:
echo.
echo 1. Run Chinook_SqlServer.sql
echo 2. Run Chinook_SqlServer_AutoIncrementPKs.sql
echo 3. Run Chinook_SqlServer.sql (LocalDB)
echo 4. Run Chinook_SqlServer_AutoIncrementPKs.sql (LocalDB)
echo 5. Exit
echo.
choice /c 12345
if (%ERRORLEVEL%)==(1) set SQLFILE=Chinook_SqlServer.sql
if (%ERRORLEVEL%)==(2) set SQLFILE=Chinook_SqlServer_AutoIncrementPKs.sql
if (%ERRORLEVEL%)==(3) goto LOCALDB
if (%ERRORLEVEL%)==(4) goto LOCALDBPK
if (%ERRORLEVEL%)==(5) goto END

:LOCALDB
SQLFILE=Chinook_SqlServer.sql
echo.
echo Running %SQLFILE%...
IF EXIST %MdfDelete% del /F %MdfDelete%
IF EXIST %LdfDelete% del /F %LdfDelete%
sqllocaldb d TEMPDB
sqllocaldb create TEMPDB %localdb_version% -s
sqlcmd -E -S (localdb)\TEMPDB -i %SQLFILE% -b -m 1
xcopy "%UserProfile%\Chinook.mdf" ".\" /Y
xcopy "%UserProfile%\Chinook_log.ldf" ".\" /Y
goto END

:LOCALDBPK
set SQLFILE=Chinook_SqlServer_AutoIncrementPKs.sql
echo.
echo Running %SQLFILE%...
IF EXIST %MdfDelete% del /F %MdfDelete%
IF EXIST %LdfDelete% del /F %LdfDelete%
sqllocaldb d TEMPDB
sqllocaldb create TEMPDB %localdb_version% -s
sqlcmd -E -S (localdb)\TEMPDB -i %SQLFILE% -b -m 1
xcopy "%UserProfile%\Chinook.mdf" ".\" /Y
xcopy "%UserProfile%\Chinook_log.ldf" ".\" /Y
goto END

:RUNSQL
echo.
echo Running %SQLFILE%...
sqlcmd -E -S .\sqlexpress -i %SQLFILE% -b -m 1

:END
echo.
sqllocaldb stop TEMPDB
sqllocaldb d "TEMPDB"
set SQLFILE=

