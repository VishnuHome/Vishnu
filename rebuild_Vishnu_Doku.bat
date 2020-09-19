REM File ...\rebuild_Doku.bat
REM Batch zur Umwandlug aller relevanten Projekte von Framework
REM Die Reihenfolge bitte nicht ohne Nachdenken ändern
REM 
REM 14.01.2018 Erik Nagel
REM 03.07.2020 Auf neue Projektstruktur angepasst.

REM SET devenv=...\Common7\IDE\devenv.exe
SET devenv=%DEVENV%

SET ROOTPATH=%VISHNUROOT%
SET BUILDALLROOTPATH=%ROOTPATH%\VishnuHome\Documentation\Vishnu.doc
SET ERRORLOGPATH=%BUILDALLROOTPATH%
DEL "%ERRORLOGPATH%\devenvErrorLog.txt"

rem SET buildType=build
SET buildType=rebuild

:FirstRun

SET target=Release
goto Start

:SecondRun

SET target=Debug

:Start

:Documentation

SET target=Debug

%devenv% "%BUILDALLROOTPATH%\Vishnu_doc\Vishnu_doc.sln" /%buildType% %target% /project "%BUILDALLROOTPATH%\Vishnu_doc\Vishnu_doc\Vishnu_doc.shfbproj" /projectconfig %target% /out "%ERRORLOGPATH%\devenvErrorLog.txt"
copy "%BUILDALLROOTPATH%\Vishnu_doc\Vishnu_doc\Help\Vishnu_doc.de.chm" ..
:Finish

echo Fertig!
