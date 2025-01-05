
![zur deutschen Version](./de.png?raw=true "de")
[zur deutschen Version](../de/)

# Vishnu
### Vishnu is a monitoring and process control toolkit with integrated processing of extended logical expressions.

![Vishnu Demo Start](./FirstView.png?raw=true "Vishnu Demo")
*This is roughly what Vishnu looks like after the start of the first demo job.*

## Requirements

  - Runs on systems with Windows 10 or higher.
  - Development and conversion with Visual Studio 2022 version 17.8 or higher.
  - .Net Runtime from 8.0.2.

## Quick start
  - ### Vishnu sources
	* You can find the sources here: [Vishnu on GitHub](https://github.com/VishnuHome/Vishnu)
	* The overview page (github page) can be found here: [Vishnu on GitHub.io](https://vishnuhome.github.io/Vishnu)

  - ### Installation:

	Please carry out the following steps for the initial setup:
	* Create a local base directory for all other Vishnu and help directories, for example c:\Users\<user>\Documents\MyVishnu.
	* Download [Vishnu.data.zip](https://github.com/VishnuHome/Setup/raw/master/Vishnu.bin/Vishnu.data.zip) and unzip it into the base directory.

	The following structure is then created:
      
	![Directories after installation](./struct.png?raw=true "Verzeichnisstruktur")

	### Vishnu demo:

	- Start **Vishnu.exe** in the ReadyBin/Vishnu.bin directory.<br />
	<span  style="font-size:14px;">Note 1: If Windows wants to install the appropriate DotNet runtime, please follow the instructions and then restart Vishnu.<br />
	<span style="font-size:14px;">Note 2: When the message appears
	 "The computer has been protected by Windows", please click once on<br /> "More information" and allow Vishnu to start.</span>

## Demos
The **DemoJobs** subdirectory contains the job definitions of a number of interesting demonstration jobs.

## Documentation
You can find the detailed documentation in **Vishnu** with **F1**; This will take you
by default to the [Vishnu online documentation](https://neteti.de/Vishnu.doc.en/)<br />
There is also a Vishnu help file available for download
at [Vishnu_doc.en.chm](https://neteti.de/Vishnu.doc.en/Vishnu_doc.en.chm)
<span style="font-size:14px;">(Note: If the help file (*.chm) is not displayed correctly, please see the [CHM-HowTo](ChmHowTo.html).)</span>
#### Keywords: Windows, wpf, c#, monitoring, job-controlling, logical tree, parallel, desktop, distributed

## Source code and development

1. forking the repository **Vishnu** via the Fork button
<br />(Repository https://github.com/VishnuHome/Vishnu)

   ![Fork](Fork_Button.png)
2. cloning of the forked repository **Vishnu** into the existing subdirectory
	.../MyVishnu/**VishnuHome**
	
	- in the git-bash via git clone:

		 cd VishnuHome<br />
		  		 git clone git@github.com:&lt;meOnGitHub&gt;/Vishnu.git

	- or via "Open with GitHub Desktop" if you prefer the desktop application
	
	- not recommended: via "Download ZIP" you can also access the source code of Vishnu,
	 but then you have no connection to your forked repository on github.
	
   ![clone](Git_Clone.png)
	

## The Vishnu plugins

Vishnu is only the logic engine, the actual work is done by the Vishnu plugins.
Vishnu plugins are small programme parts (DLLs) that are loaded by Vishnu at runtime.
The Vishnu plugins include, among others, the checkers.
Checkers are the essential Vishnu actors. They do the checking work and deliver
check results (see also in the help [Vishnu actors](https://neteti.de/Vishnu.doc.en/html/bc0ffa08-c936-4fad-8fdb-dbd2279fc360.htm)
and [own checker](https://neteti.de/Vishnu.doc.en/html/a3f9771a-ac24-46c0-97df-d2bde6a990e8.htm)).
Vishnu already supplies a range of checkers. You can find these under [InPlug](https://github.com/InPlug).

You can fork and clone Vishnu plug-ins in the same way as already described under [Source code and development](#source-code-and-development),
only that your local subdirectory should be the **InPlug** already provided.
<br />(Sources: [InPlug](https://github.com/InPlug))

## The basic framework
Vishnu works with a few universal DLLs, the basic framework.
In case you want to look at the sources or need to debug there,
you can clone the corresponding sources into the **WorkFrame** folder provided for this purpose.
<br />(Sources: https://github.com/WorkFrame)

---

## Is there support?

#### Short answer: *no*.<br />
#### long answer:
I (Erik) am currently (April 2024) still programming Vishnu alone.
Even though I would of course appreciate all your experience reports, suggestions, ideas for improvement and error messages
with interest, at the moment I simply cannot foresee how things will develop.
Where I can, I will correct errors and take suggestions into account in my personal prioritisation.
But at this point it should be said again: Vishnu is open source and free.
**So help yourselves and, above all, each other.**

## Communication and participation

**Please use the discussion topics ("Issues").**
<br />If you discover errors or want to make suggestions for improvement, please open a new discussion topic ("New issue") first.
However, please check first whether a suitable topic already exists.<br />
All kinds of suggestions for improvement are welcome, as are personal experience reports.
These don't necessarily have to be world-improving deeds; spelling mistakes also need to be corrected.
In particular, I still lack good ideas and solutions for an English presence.
In the Vishnu-Help you will find suggestions on the pages [collection of ideas](https://neteti.de/Vishnu.doc.en/html/2e84f44c-6249-45dc-bdc2-c656de87c907.htm)
and [known errors and problems](https://neteti.de/Vishnu.doc.en/html/68cd3f39-4a2c-49f3-8a90-b2442b5880a9.htm).

#### Changes and debugging of the plugin or Vishnu source code

**Important:** If possible, do not make any changes, corrections or extensions
on the master branch of the source code, but first create your own **new branch**.
This is the only way you can possibly return your improvements later (**pull-request**).

As the Vishnu kernel is subject to particularly high requirements in terms of correctness, stability and performance,
you might want to start with corrections, extensions to - or new creation of - Vishnu plugins.
The Vishnu kernel is also very time-consuming to test, which is why reactions may take longer.

#### Return source code changes to the original repository

If you have made changes to the plugin framework or Vishnu source code and tested them thoroughly,
you can return your own **branch** via a **"pull request "**.
The branch is then assessed and, if successful, transferred to Vishnu-master.
Please do not try to return a directly changed master-branch - this would not be accepted.
(see also [Is there support?](#is-there-support))

---

## Foreign software, foreign ideas

[Sandcastle Help File Builder (SHFB)](https://github.com/EWSoftware/SHFB)<br />
Many thanks to Eric Woodruff, EWSoftware.
Without the Sandcastle Help File Builder, the Vishnu documentation would be unthinkable.

[Newtonsoft.Json](https://www.newtonsoft.com/json)<br />
Thanks to James Newton-King for his indispensable software.

[Demo logic for SplashWindow](https://www.codeproject.com/Articles/116875/WPF-Loading-Splash-Screen)<br />
Thanks to Amr Azab and Nate Lowry.

[WPF pie charts](https://www.codeproject.com/Articles/442506/Simple-and-Easy-to-Use-Pie-Chart-Controls-in-WPF)<br />
Many thanks to Kashif Imran on Code Project.

[Variable Grids](https://rachel53461.wordpress.com/2011/09/17/wpf-grids-rowcolumn-count-properties/)<br />
Many thanks to Rachel Lim for her fantastic blog. Thanks also for her advice on ShutdownMode.OnMainWindowClose.

[Monphase calculation](https://www.codeproject.com/script/Membership/View.aspx?mid=1961229)<br />
Thanks to Mostafa Kaisoun for his calculation logic.

[Geolocation](https://www.geojs.io)<br />
Many thanks to the developers and sponsors of this free geolocation site.

[weather forecasts](https://open-meteo.com) and<br />
[Weather-Icons](https://www.7timer.info)<br />
Many thanks to the team at open-meteo.com and also to Chenzhou Cui and his friends who run the 7timer.info weather forecast site.

[Minimum information about a screen](https://stackoverflow.com/questions/1927540/how-to-get-the-size-of-the-current-screen-in-wpf)<br />
Thanks to Nils Andresen on StackOverflow

[Base classes for ViewModels](https://github.com/poma/SshConnect/blob/master/SshConnect/MvvmFoundation/ObservableObject.cs)<br />
Many thanks to Roman Semenov (poma) for this initial help.

[Visual Commander](https://marketplace.visualstudio.com/items?itemName=SergeyVlasov.VisualCommander)<br />
Thanks also to Sergey Vlasov for his helpful Visual Studio automation.

[SingleInstance](https://stackoverflow.com/users/51170/matt-davis)
Thanks to Matt Davis for his very good Mutex solution.

[no longer online: ZIP routines for ZIPs > 4GB with passwords]<br />
Thanks also to Peter Bromberg for his support with the zip routines.

[Equality Converter](https://stackoverflow.com/questions/37302270/comparing-two-dynamic-values-in-datatrigger)<br />
Thanks to Jason Tyler on stackoverflow.

Thanks also to the many other software developers who shared their knowledge with us all.<br />

Last but not least, my thanks go to the Microsoft teams for their free Express and Community Editions.

### Have fun with Vishnu!
Erik Nagel
