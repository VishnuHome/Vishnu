@ECHO OFF
FOR /D /r %%G in ("bin\*") DO Echo %%~fG
FOR /D /r %%G in ("obj\*") DO Echo %%~fG
REM FOR /D /r %%G in (".git\*") DO Echo %%~fG
dir /s /b *.chm
:Abfrage
ECHO.
ECHO Sollen die obigen Verzeichnisse alle geloescht werden? (j/n) 
SET /p wahl= 
For %%A in (J N) Do if /i '%wahl%'=='%%A' goto :Wahl1%%A 
Echo Bitte mit j oder n antworten.
Goto Abfrage
:Wahl1N 
echo Es wird nicht geloescht.
goto Ende
:Wahl1J 
echo Die Verzeichnisse werden geloescht.
FOR /D /r %%G in ("bin\*") DO rd /s /q %%~fG
FOR /D /r %%G in ("obj\*") DO rd /s /q %%~fG
REM FOR /D /r %%G in (".git\*") DO rd /s /q %%~fG
del /s /q *.chm
:Ende
