# Vishnu
Vishnu ist eine Monitoring- und Prozesssteuerungssoftware mit integrierter Verarbeitung erweiterter logischer Ausdrücke.

![Vishnu-Hilfe Startseite](./FirstView.png?raw=true "Vishnu-Hilfe")
*So sieht Vishnu nach dem Start mit dem ersten Demo-Job ungefähr aus*.

## Voraussetzungen

  - Läuft auf Windows-Systemen ab Version 7.
  - Entwicklung und Umwandlung mit Visual Studio 2019 oder höher.

## Schnellstart

Für die erste Einrichtung führe bitte nachfolgende Schritte aus:
  - ### Vorbereitung:
	* Ein lokales Basisverzeichnis für alle weiteren Vishnu- und Hilfs-Verzeichnisse anlegen, zum Beispiel c:\Users\<user>\Documents\MyVishnu
	* #### Wichtig: Eine Umgebungsvariable "Vishnu_Root" auf den Pfad zu diesem Verzeichnis setzen, z.B.: Vishnu_Root=c:\Users\<user>\Documents\MyVishnu.

  - ### Installation:
	* [init.zip](https://github.com/VishnuHome/Setup/raw/master/Vishnu.bin/init.zip) herunterladen und in das Basisverzeichnis entpacken.

	Es entsteht dann folgende Struktur:
      
	![Verzeichnisse nach Installation](./struct.png?raw=true "Verzeichnisstruktur")

	### Vishnu-Demo:

	- Im Verzeichnis ReadyBin/Vishnu.bin das Script **Vishnu_Demo.bat** starten.

## Demos
Im Unterverzeichnis **DemoJobs** findest du die Job-Definitionen einer Reihe von interessanten Demonstrations-Jobs.

## Dokumentation
Die ausführliche Dokumentation findest du im mit entpackten **Vishnu_doc.de.chm** oder
online unter [Vishnu online Dokumentation](https://neteti.de/Vishnu.Doc/)
<br/><span>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</span><span style="font-size:14px;">(Hinweis: Wenn die Hilfedatei (*.chm) nicht korrekt angezeigt werden sollte, sieh bitte im [CHM-HowTo](CHM_HowTo.md) nach.)</span>

## Quellcode und Entwicklung

Es gibt grundsätzlich zwei unabhängige Vorgehensweisen:
  - Wenn du kein Team-Mitglied der Organisation **VishnuHome** bist (das ist der Normalfall),
    dann lies einfach hier weiter.
  - Team-Mitglieder lesen bitte den Abschnitt [Quellcode und Entwickung für Team-Mitglieder](#Quellcode-und-Entwicklung-fuer-Team-Mitglieder)

---

1. Forken des Repositories **Vishnu** über den Button Fork
<br/>(Repository https://github.com/VishnuHome/Vishnu)

   ![Fork](Fork_Button.png)
2. Clonen des geforkten Repositories **Vishnu** in das existierende Unterverzeichnis
	.../MyVishnu/**VishnuHome**
	
	- a) in der git-bash über git clone:

		  cd VishnuHome
		  git clone git@github.com:VishnuHome/Vishnu.git

	- b) über "Open with GitHub Desktop", wenn du die Desktop-Anwendung bevorzugst
	
	- c) über "Download ZIP" kommst du zwar auch an den Quellcode von Vishnu, 
	     hast dann aber keine Anbindung an dein geforktes Repository auf github.
	
   ![clone](Git_Clone_small.png)
	

## Quellcode und Entwicklung fuer Team-Mitglieder

Wechsle bitte in das Repository [Setup](https://github.com/VishnuHome/Setup) und folge den Anweisungen im dortigen README.md.

## Die Vishnu-Plugins

Vishnu ist nur die Logik-Maschine, die eigentliche Arbeit machen die Vishnu-Plugins.
Vishnu-Plugins sind kleine Programmteile (DLLs), die von Vishnu zur Laufzeit geladen werden.
Zu den Vishnu-Plugins gehören neben anderen die Checker. 
Checker sind die wesentlichen Vishnu-Akteure. Sie machen die Prüf-Arbeit und liefern
Prüfergebnisse zurück (siehe auch in der Hilfe [Vishnu Akteure](https://neteti.de/Vishnu.Doc/html/bc0ffa08-c936-4fad-8fdb-dbd2279fc360.htm)
und [eigene Checker](https://neteti.de/Vishnu.Doc/html/a3f9771a-ac24-46c0-97df-d2bde6a990e8.htm)).
Vishnu liefert schon eine Reihe von Checkern mit. Diese findest du unter [InPlug](https://github.com/InPlug).

Vishnu-Plugins kannst du genauso forken und clonen wie unter [Quellcode und Entwickung](#Quellcode-und-Entwicklung) schon beschrieben,
nur dass dein lokales Root-Verzeichnis **InPlug** sein sollte.
<br/>(Quellen: https://github.com/InPlug)

## Das Basis-Framework
Vishnu arbeitet mit einigen allgemeingültigen DLLs, dem Basis-Framework.
Für den Fall, dass man sich die Quellen davon anschauen will oder dorthinein debuggen muss,
kann man sich die zugehörigen Quellen in den dafür vorgesehenen Ordner **WorkFrame** clonen.
<br/>(Quellen: https://github.com/WorkFrame)

## Mitmachen (Contributing)
Wenn du Fehler entdeckst oder Verbesserungsvorschläge einbringen willst, eröffne bitte zuerst ein neues Diskusionsthema ("New issue").<br/>
Bitte prüfe aber vorher, ob ein passendes Thema nicht vielleicht schon existiert.

#### Änderungen und Debugging am Plugin- oder Vishnu-Quellcode

**Wichtig:** Mach möglichst keine Änderungen, Korrekturen oder Erweiterungen
am master-branch des Quellcodes, sondern lege zuerst einen neuen **feature-branch** an. 
Nur so kannst du später deine Verbesserungen auch dem Vishnu-Team zurückliefern.

Da der Vishnu-Kernel besonders hohen Anforderungen an Korrektheit, Stabilität und Performance unterliegt,
solltest du vielleicht mit Korrekturen, Erweiterungen an - oder Neuerstellung von - Vishnu-Plugins beginnen.
Der Vishnu-Kernel ist darüber hinaus sehr testaufwendig, weshalb Reaktionen des noch sehr kleinen Teams länger
auf sich warten lassen können.

#### Änderungen am Quellcode in das Original-Repository zurückspielen

Wenn du Änderungen am Plugin- oder Vishnu-Quellcode vorgenommen und ausführlich getestet hast, kannst du deinen **feature-branch** an das Vishnu-Team über einen **"pull request"** zurückliefern. Das Vishnu-Team begutachtet und testet dann diesen Branch und übernimmt diesen bei Erfolg in Vishnu-master.
Bitte versuche nicht, einen direkt geänderten master-branch zurückzumelden - wir würden das zurückweisen.

**Ein Hinweis in eigener Sache: das Vishnu-Team ist aktuell (Juli 2022) noch sehr klein, weshalb die Bearbeitung deiner Anfragen und Änderungen länger dauern kann!**

## Fremde Software, fremde Ideen

[Sandcastle Help File Builder (SHFB)](https://github.com/EWSoftware/SHFB)<br/>
Vielen Dank an Eric Woodruff, EWSoftware.
Ohne den Sandcastle Help File Builder wäre die Vishnu-Dokumentation nicht denkbar.

[Visual Commander](https://marketplace.visualstudio.com/items?itemName=SergeyVlasov.VisualCommander)<br/>
Danke auch an Sergey Vlasov für seine hilfreiche Visual Studio Automatisierung.

[Newtonsoft.Json](https://www.newtonsoft.com/json)<br/>
Json ohne die Software von James Newton-King? Undenkbar!

Demo-Logik für SplashWindow<br/>
[Dank an Amr Azab](http://www.codeproject.com/Articles/116875/WPF-Loading-Splash-Screen)
und [Nate Lowry](http://blog.dontpaniclabs.com/post/2013/11/14/Dynamic-Splash-Screens-in-WPF).

[Zwei Properties vergleichen](https://stackoverflow.com/questions/37302270/comparing-two-dynamic-values-in-datatrigger)<br/>
Dank an Jason Tyler.

[Mindest-Informationen über einen Screen](http://stackoverflow.com/questions/1927540/how-to-get-the-size-of-the-current-screen-in-wpf)<br/>
Danke an Nils Andresen auf StackOverflow

[ZIP-Routinen für ZIPs > 4GB mit Passwörtern](http://www.eggheadcafe.com/tutorials/aspnet/9ce6c242-c14c-4969-9251-af95e4cf320f/zip--unzip-folders-and-f.aspx)<br/>
Danke Peter Bromberg.

[Basisklasse für ViewModels, kapselt INotifyPropertyChanged](https://github.com/poma/SshConnect/blob/master/SshConnect/MvvmFoundation/ObservableObject.cs)<br/>
Vielen Dank an Roman Semenov (poma) für diese Starthilfe.

[Monphasen Berechnung](https://www.codeproject.com/script/Membership/View.aspx?mid=1961229)<br/>
Danke Mostafa Kaisoun für seine Berechnungslogik.

[Wettervorhersagen und Wetter-Icons](http://www.7timer.info)<br/>
Vielen Dank an Chenzhou Cui und seine Freunde, die diese wunderbare, freie Seite für Wettervorhersagen betreiben.

[WPF-Tortendiagrammme](https://www.codeproject.com/Articles/442506/Simple-and-Easy-to-Use-Pie-Chart-Controls-in-WPF)</br>
Vielen Dank an Kashif Imran auf Code Project.

[Equality Converter](https://stackoverflow.com/questions/37302270/comparing-two-dynamic-values-in-datatrigger)</br>
Dank an Jason Tyler auf stackoverflow.

Dank auch an die vielen weiteren Software-Entwickler/innen, die ihr Wissen mit uns allen geteilt haben.<br/>
Beispielhaft für viele:<br/>
[Thomas Claudius Huber](https://www.thomasclaudiushuber.com/)<br/>
und [Scott Hanselman](https://www.hanselman.com/)

Last but not least geht mein Dank an Microsoft für ihre "Community Edition"s.

### Viel Spass mit Vishnu!
