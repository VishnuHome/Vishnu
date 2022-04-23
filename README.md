# Vishnu
Vishnu ist eine Monitoring- und Prozesssteuerungssoftware mit integrierter Verarbeitung erweiterter logischer Ausdrücke.

Für die erste Einrichtung führe bitte nachfolgende Schritte aus:
  - Vorbereitung:
    	Ein lokales Basisverzeichnis für alle weiteren Vishnu- und Hilfs-Verzeichnisse anlegen, zum Beispiel c:\Users\<user>\Documents\MyVishnu
    	Eine Umgebungsvariable "Vishnu_Root" auf den Pfad zu diesem Verzeichnis setzen, z.B.: Vishnu_Root=c:\Users\<user>\Documents\MyVishnu.
  - Installation:
    	https://github.com/VishnuHome/Setup/blob/master/Vishnu.bin/init.zip herunterladen und in das Basisverzeichnis entpacken.
    	Es entsteht dann folgende Struktur:
    	...\MyVishnu\
    	             ReadyBin\
    	                      Assemblies\
    	                      UserAssemblies\
    	                      Vishnu.bin\
    	             VishnuHome\

Siehe folgende Quellen:
 - Documentation
 - DemoJobs
 - Tests/TestJobs

Hier ein Screenshot:
![Vishnu-Hilfe Startseite](./FirstView.png?raw=true "Vishnu-Hilfe")

Das Vishnu C#-Projekt kann z.B. mit dem Debug-Parameter '-job=..\..\..\..\Documentation\DemoJobs\Simple\CheckAll'
in Visual Studio debugged werden.
