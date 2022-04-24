# Vishnu
Vishnu ist eine Monitoring- und Prozesssteuerungssoftware mit integrierter Verarbeitung erweiterter logischer Ausdrücke.

Für die erste Einrichtung führe bitte nachfolgende Schritte aus:
  - Vorbereitung:
    	Ein lokales Basisverzeichnis für alle weiteren Vishnu- und Hilfs-Verzeichnisse anlegen, zum Beispiel c:\Users\<user>\Documents\MyVishnu
    	Eine Umgebungsvariable "Vishnu_Root" auf den Pfad zu diesem Verzeichnis setzen, z.B.: Vishnu_Root=c:\Users\<user>\Documents\MyVishnu.
  - Installation:
    	https://github.com/VishnuHome/Setup/raw/master/Vishnu.bin/init.zip herunterladen und in das Basisverzeichnis entpacken.

Es entsteht dann folgende Struktur:
      
![Verzeichnisse nach Installation](./struct.png?raw=true "Verzeichnisstruktur")

###### Vishnu-Demo:

- Im Verzeichnis ReadyBin/Vishnu.bin das Script **Vishnu_Demo.bat** starten.

So sieht Vishnu nach dem Start ungefähr aus:
![Vishnu-Hilfe Startseite](./FirstView.png?raw=true "Vishnu-Hilfe")

Siehe auch folgende Quellen:
 - Documentation
 - DemoJobs

Das Vishnu C#-Projekt kann z.B. mit dem Debug-Parameter '-job=..\..\..\..\Documentation\DemoJobs\Simple\CheckAll'
in Visual Studio getestet werden.
