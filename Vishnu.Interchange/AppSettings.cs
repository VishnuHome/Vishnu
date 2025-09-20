﻿using NetEti.ApplicationEnvironment;
using System.Collections.Generic;
using System.IO;
using System;
using NetEti.Globals;
using System.Security;
using System.Windows;
using NetEti.FileTools;
using NetEti.FileTools.Zip;
using NetEti.ObjectSerializer;
using System.Reflection.Metadata;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Erbt allgemeingültige Einstellungen von BasicAppSettings oder davon abgeleiteten
    /// Klassen und fügt anwendungsspezifische Properties hinzu.
    /// Holt Applikationseinstellungen aus verschiedenen Quellen:
    /// Kommandozeile, Vishnu.exe.config (app.config), Vishnu.exe.config.user, Environment, Registry.
    /// <seealso cref="BasicAppSettings"/>
    /// </summary>
    /// <remarks>
    /// File: AppSettings.cs
    /// Autor: Erik Nagel, NetEti
    ///
    /// 11.10.2013 Erik Nagel: erstellt.
    /// 28.01.2024 Erik Nagel: Property VishnuRoot hinzugefügt.
    /// </remarks>
    public sealed class AppSettings : BasicAppSettings
    {
        #region public members

        /// <summary>
        /// Event, das ausgelöst wird, wenn User-Parameter neu geladen wurden.
        /// </summary>
        public event EventHandler? UserParametersReloaded;

        /// <summary>
        /// Routine, die den ParameterReader entsprechend der vorher in
        /// UserParameterReaderPath konfigurierten Parameter initialisiert.
        /// Je nach UserParameterReader kann dieser Vorgang länger dauern und
        /// wird deshalb hier nicht automatisch ausgeführt, sondern muss
        /// extern und möglichst direkt nach Instanziierung aufgerufen werden.
        /// </summary>
        public void InitUserParameterReader()
        {
            try
            {
                this._userParameterReader?.Init(this._userParameterReaderParameters);
            }
            catch (Exception ex)
            {
                this.FatalInitializationException = new ApplicationException(
                    "Ausnahme bei InitUserParameterReader, Parameter: "
                    + this._userParameterReaderParameters ?? "???", ex);
                // this.FatalInitializationException = ex;
            }
        }

        #region derived disposable

        private bool _disposed = false;

        /// <summary>
        /// Abschlussarbeiten.
        /// </summary>
        /// <param name="disposing">False, wenn vom eigenen Destruktor aufgerufen.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Verwaltete Ressourcen der abgeleiteten Klasse freigeben
                if (File.Exists(this._jsonJobPathesLog))
                {
                    try
                    {
                        File.Delete(this._jsonJobPathesLog);
                    }
                    catch { }
                }
            }

            // Nicht verwaltete Ressourcen der abgeleiteten Klasse freigeben

            _disposed = true;

            // Basisklassen-Dispose aufrufen
            base.Dispose(disposing);
        }

        #endregion derived disposable

        #region Properties (alphabetic)

        /// <summary>
        /// Bei True werden Knoten, die nicht auf kooperativen Cancel reagieren (können)
        /// durch "Abort()" hart abgebrochen.
        /// Immer: false; künftig wegfallend.
        /// </summary>
        public bool AbortingAllowed { get; set; }

        /// <summary>
        /// Bei True werden Null-Ergebnisse von Checkern wie True oder False-Ergebnisse behandelt.
        /// Sie setzen dann z.B. Exceptions zurück, was bei erneutem Auftreten dieser Exceptions
        /// zu erneutem Auslösen von Workern führt.
        /// Default: false.
        /// </summary>
        public bool AcceptNullResults { get; set; }

        /// <summary>
        /// Liste von Verzeichnissen, in denen nach zu ladenden Assemblies
        /// gesucht werden soll.
        /// </summary>
        public List<string> AssemblyDirectories { get; set; }

        /// <summary>
        /// Bei true wird der Tree automatisch gestartet.
        /// Default: false.
        /// </summary>
        public bool Autostart { get; set; }

        /// <summary>
        /// Bei true wird vor jedem run der betroffene
        /// Teilbaum gestoppt (Break).
        /// Default: false.
        /// </summary>
        public bool BreakTreeBeforeStart { get; set; }

        /// <summary>
        /// Steuert das Verhalten beim UserRun eines Knoten in einem Controlled-Tree,
        /// der über ein TreeEvent getriggert wird und dessen Startvoraussetzungen
        /// noch nicht erfüllt sind:
        /// None: kein Start möglich,
        /// Info: kein Start möglich - es erfolgt eine Meldung,
        /// Question: es erfolgt eine mit "Nein" vorbelegte JaNein-Abfrage.
        /// Defult: Question.
        /// </summary>
        public DialogSettings ControlledNodeUserRunDialog
        {
            get
            {
                return this._controlledNodeUserRunDialog;
            }
            set
            {
                this._controlledNodeUserRunDialog = value;
            }
        }

        /// <summary>
        /// Bei true befindet sich Vishnu im Demo-Modus.
        /// Dies ist direkt nach Erstinstallation der Fall.
        /// Default: false.
        /// </summary>
        public bool DemoModus { get; set; }

        /// <summary>
        /// Bei true wird wird der Main-Job und rekursiv alle Properties
        /// und Unterklassen inklusive SubJobs in eine Json-Datei geschrieben.
        /// Default: false
        /// </summary>
        public bool DumpLoadedJobs { get; set; }

        ///// <summary>
        ///// Bei true gibt Vishnu über den InfoController am Ende der
        ///// Verarbeitung alle AppSetting-Properties mit Wert und Quelle aus.
        ///// Default: false.
        ///// </summary>
        //public bool DumpAppSettings { get; set; }

        /// <summary>
        /// Wenn ungleich null, dann sollte die Anwendung die Exception melden und sich beenden.
        /// </summary>
        public Exception? FatalInitializationException { get; set; }

        /// <summary>
        /// Kombinierbare Liste von Typen von Knoten des Trees zur Filterung
        /// der Knoten in einer flachen Liste von Knoten (FlatNodeViewModelList).
        /// </summary>
        public NodeTypes FlatNodeListFilter { get; set; }

        /// <summary>
        /// Die bevorzugte Vishnu-Hilfe (wird über F1 oder das Kontext-Menü geladen):
        ///     "online": die Vishnu-Hilfe wird aus dem Internet geladen (default);
        ///     "local": die Vishnu-Hilfe wird als .chm lokal geladen.
        /// </summary>
        public string HelpPreference { get; set; }

        /// <summary>
        /// Bei True werden alle durch EventTrigger getriggerten Knoten
        /// neu initialisiert, wenn der Anwender irgendeinen Knoten startet.
        /// Experimentell, Default: false.
        /// </summary>
        public bool InitAtUserRun { get; set; }

        /// <summary>
        /// Returns true, wenn gerade eine vom User definierte Ruhezeit
        /// für Vishnu-Akteure (Checker) läuft.
        /// </summary>
        /// <returns>True, wenn gerade eine vom User definierte Ruhezeit
        /// für Vishnu-Akteure (Checker) läuft.</returns>
        public bool IsInSleepTime
        {
            get
            {
                if (!this._sleepTimeSet)
                {
                    return false;
                }
                DateTime jetzt = DateTime.Now;
                TimeSpan bisJetzt = new TimeSpan(jetzt.Hour, jetzt.Minute, jetzt.Second);
                return
                    (this.SleepTimeFrom > this.SleepTimeTo &&
                     (bisJetzt >= this.SleepTimeFrom || bisJetzt < this.SleepTimeTo))
                    ||
                    (this.SleepTimeFrom < this.SleepTimeTo &&
                     (bisJetzt >= this.SleepTimeFrom && bisJetzt < this.SleepTimeTo));
            }
        }

        /// <summary>
        /// Liste von Verzeichnissen, in denen nach zu ladenden Jobs
        /// gesucht werden soll. Wird durch die Vishnu-Logik gefüllt.
        /// </summary>
        public Stack<string> JobDirPathes;

        // <summary>
        // Bei True wird, wenn beim Start kein Job gesetzt ist,
        // Ein LoadFileDialog zum Laden eines JobDescription.xml gestartet.
        // Bei False erfolgt eine Fehlermeldung.
        // Default: false.
        // </summary>
        private bool LoadJobDialog { get; set; }

        /// <summary>
        /// Das bevorzugte Format für JobDescription-Dateien:
        /// "xml" für "JobDescription.xml" oder "json" für "JobDescription.json".
        /// Default: "xml".
        /// </summary>
        public string? PreferredJobDescriptionFormat { get; set; }

        /// <summary>
        /// Der Dateipfad zum Verzeichnis der lokalen Konfiguration.
        /// Default: Pfad zum AppConfigUser-Verzeichnis.
        /// </summary>
        public string? LocalConfigurationDirectory { get; set; }

        /// <summary>
        /// Verzögerung in Millisekunden, bevor ein LogicalCanged-Event weitergegeben wird.
        /// Default: 0
        /// </summary>
        public int LogicalChangedDelay { get; set; }

        /// <summary>
        /// Basis zur Potenz der Hierarchie-Tiefe der aktuellen JobList
        /// bezogen auf die Hierarchie der untergeordneten JobLists.
        /// Wird mit LogicalChangedDelay verrechnet:
        /// int calcDepth = jobPackage.Job.MaxSubJobDepth;
        /// jobPackage.Job.LogicalChangedDelay = (int)(this._appSettings.LogicalChangedDelay
        ///  * Math.Pow(this._appSettings.LogicalChangedDelayPower, calcDepth));  
        /// Default: 0
        /// </summary>
        public double LogicalChangedDelayPower { get; set; }

        /// <summary>
        /// Name des Haupt-Jobs.
        /// </summary>
        public string MainJobName
        {
            get
            {
                return this._mainJobName;
            }
            set
            {
                this._mainJobName = value;
                this.AppEnvAccessor.RegisterKeyValue("MainJobName", this._mainJobName);
            }
        }

        /// <summary>
        /// Wird programmintern als Merkfeld für die aktuellen Bildschirmgrenzen genutzt.
        /// Wird von MainWindow.xaml.cs gesetzt.
        /// </summary>
        public static Rect ActScreenBounds { get; set; }

        /// <summary>
        /// Bei True werden definierte Worker nicht aufgerufen.
        /// Default: false.
        /// </summary>
        public bool NoWorkers { get; set; }

        /// <summary>
        /// Dateipfad zum obersten Job.
        /// </summary>
        public string? RootJobPackagePath { get; set; }

        /// <summary>
        /// Bei true werden mehrere Bildschirme als ein einziger
        /// großer Bildschirm behandelt, ansonsten zählt für
        /// Größen- und Positionsänderungen der Bildschirm, auf dem
        /// sich das MainWindow hauptsächlich befindet (ActualScreen).
        /// Default: false.
        /// </summary>
        public bool SizeOnVirtualScreen { get; set; }

        /// <summary>
        /// Beginn einer vom User definierten Ruhezeit für Vishnu-Akteure (Checker).
        /// </summary>
        public TimeSpan SleepTimeFrom { get; set; }

        /// <summary>
        /// Ende einer vom User definierten Ruhezeit für Vishnu-Akteure (Checker).
        /// </summary>
        public TimeSpan SleepTimeTo { get; set; }

        /// <summary>
        /// Überlebensdauer eines Snapshots.
        /// </summary>
        public TimeSpan SnapshotSustain { get; set; }

        /// <summary>
        /// Vom User definiertes Verzeichnis, in dem Snapshots von Knoten abgespeichert werden.
        /// Ist der SnapshotDirectory-Pfad relativ oder leer (i.d.R. nicht sinnvoll), dann ist der
        /// SnapshotDirectory-Pfad immer relativ zum Verzeichnis, in dem die JobDescription.xml
        /// des MainJob liegt.
        /// </summary>
        public string SnapshotDirectory { get; set; }

        /// <summary>
        /// Ausrichtung des Trees beim Start der Anwendung.
        ///   AlternatingHorizontal: Alternierender Aufbau, waagerecht beginnend (Default).
        ///   Vertical: Senkrechter Aufbau.
        ///   Horizontal: Waagerechter Aufbau.
        ///   AlternatingVertical: Alternierender Aufbau, senkrecht beginnend.
        /// </summary>
        public TreeOrientation StartTreeOrientation { get; set; }

        /// <summary>
        /// Bei True wird mit der Job-Ansicht gestartet, ansonsten mit der Tree-Ansicht (default: false).
        /// </summary>
        public bool StartWithJobs { get; set; }

        /// <summary>
        /// Parameter zur Steuerung des Startverhaltens von getriggerten Knoten
        /// beim Start durch den Anwender (UserRun):
        ///   None = kein direkter Start,
        ///   All = alle getriggerten Knoten innerhalb
        ///          eines durch UserRun gestarteten (Teil-)Trees starten direkt
        ///          (wie nicht getriggerte Knoten),
        ///   Direct = alle getriggerten Knoten starten direkt, wenn sie selbst
        ///          durch UserRun gestartet wurden.
        ///   AllNoTreeEvents = wie All aber nicht durch TreeEvents getriggerte Knoten,
        ///   DirectNoTreeEvents = wie Direct aber nicht durch TreeEvents getriggerte Knoten.
        /// Default: Direct.
        /// </summary>
        public TriggeredNodeStartConstraint StartTriggeredNodesOnUserRun
        {
            get
            {
                return this._startTriggeredNodesOnUserRun;
            }
            set
            {
                this._startTriggeredNodesOnUserRun = value;
            }
        }

        /// <summary>
        /// Die Zeit in Millisekunden, die gewartet wird, bevor ein
        /// neuer Versuch gestartet wird, einen Knoten zu starten.
        /// Default: 100.
        /// </summary>
        public int TryRunAsyncSleepTime { get; set; }

        /// <summary>
        /// Liste von INodeChecker-Dll-Namen, die für jeden Run neu geladen werden sollen.
        /// Dient zum Debuggen von Memory Leaks, die mutmaßlich durch User-Checker verursacht werden.
        /// </summary>
        public List<string> UncachedCheckers { get; set; }

        /// <summary>
        /// Liste von Extensions von unterstützten JobDescriptions;
        /// Default: ".xml".
        /// </summary>
        public List<string> ValidJobDescriptionExtensions { get; set; }

        /// <summary>
        /// Verzeichnis, in dem Job-übergreifende Assemblies des Users abgelegt sind.
        /// </summary>
        public string UserAssemblyDirectory { get; set; }

        /// <summary>
        /// Dateipfad einer optionalen Dll, die IParameterReader implementiert.
        /// Wenn vorhanden, wird die Dll dynamisch geladen und erweitert Vishnus
        /// Fähigkeiten zur Parameter-Ersetzung in ReplaceWildcards.
        /// </summary>
        public string? UserParameterReaderPath { get; set; }

        /// <summary>
        /// Das Vishnu-Rootverzeichnis zur Laufzeit.
        /// Im Gegensatz zum optionalen Environment-Setting "Vishnu_Root" wird
        /// diese Property, wenn sie nicht von außen als Parameter gesetzt wird,
        /// durch die Vishnu-Programmlogik gefüllt, so dass sie immer so gut, wie
        /// möglich gesetzt ist und somit auch von außen über "%VishnuRoot%"
        /// genutzt werden kann.
        /// Während "Vishnu_Root", sofern es gesetzt wurde, immer das Root-Verzeichnis
        /// der Vishnu-Entwicklung bezeichnet, zeigt "VishnuRoot" bevorzugt auf
        /// das Verzeichnis, in dem sich die gestartete Vishnu.exe befindet
        /// ("ApplicationRootPath").
        /// Nur, wenn sich auf gleicher Hierarchiestufe, wie "ApplicationRootPath"
        /// kein Verzeichnis "UserAssemblies" befindet, wird "VishnuRoot", wenn nicht
        /// von außen mitgegeben, so gut es geht, durch die Vishnu-Programmlogik auf
        /// das das Root-Verzeichnis der Vishnu-Entwicklung gesetzt.
        /// </summary>
        public string VishnuRoot { get; private set; }

        /// <summary>
        /// VishnuSourceRoot bezeichnet immer das Root-Verzeichnis der Vishnu-Entwicklung.
        /// Wenn im Programm-Environment kein Verzeichnis "Vishnu_Root" gesetzt wurde,
        /// wird VishnuSourceRoot so gut es geht, durch die Vishnu-Programmlogik auf
        /// das das Root-Verzeichnis der Vishnu-Entwicklung gesetzt.
        /// </summary>
        public string VishnuSourceRoot { get; private set; }

        /// <summary>
        /// Datenklasse mit wesentlichen Darstellungsmerkmalen des Vishnu-MainWindow.
        /// </summary>
        public WindowAspects? VishnuWindowAspects { get; set; }

        /// <summary>
        /// Der Herausgeber der ClickOnce Installation.
        /// </summary>
        public string? VishnuProvider { get; private set; }

        /// <summary>
        /// Nur für internen Gebrauch.
        /// </summary>
        public SecureString? XUnlock { get; set; }

        /// <summary>
        /// Wenn es sich beim MainJob um ein Zip handelt, wird dieses von Vishnu
        /// in das WorkingDirectory entpackt. Hierbei ändert sich allerdings das
        /// Bezugsverzeichnis bei relativen Pfadangaben von SubJobs.
        /// Um - zumindest in einfachen Szenarien - SubJobs noch finden zu können,
        /// wird im gleichen Verzeichnis, in dem sich das Zip-File befindet, ein
        /// Unterverzeichnis angelegt und dies während der laufenden Vishnu-Session
        /// zusätzlich als Ausgangs-Verzeichnis für die Suche nach SubJobs mit
        /// relativen Pfadangaben genutzt.
        /// Vishnu löscht dieses Verzeichnis dann wieder, bevor es sich beendet.
        /// </summary>
        public string? ZipRelativeDummyDirectory { get; set; }

        #endregion Properties (alphabetic)

        /// <summary>
        /// Lädt die Systemeinstellungen bei der Initialisierung oder lädt sie auf Anforderung erneut.
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();
            this.RegistryBasePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Vishnu\";
            this.AppEnvAccessor.UnregisterKey("MainJobName");
            this.AppEnvAccessor.UnregisterKey("WorkingDirectory");
            this.AppEnvAccessor.UnregisterKey("DebugFile");
            this.AppEnvAccessor.UnregisterKey("SnapshotDirectory");
            this.AppEnvAccessor.UnregisterKey("DebugFile");
            this.AppEnvAccessor.UnregisterKey("StatisticsFile");

            // Ermittlung der Properties VishnuRoot und UserAssemblyDirectory zur Laufzeit.
            string userAssemblyDirectoryCandidate = Path.Combine(this.ApplicationRootPath, @"../UserAssemblies");
            string relativeVishnuRoot = String.Empty;
            string? vishnuRoot = this.GetStringValue("VishnuRoot", null);
            if (vishnuRoot != null)
            {
                this.VishnuRoot = vishnuRoot;
                this.VishnuSourceRoot = this.VishnuRoot;
            }
            else
            {
                if (Directory.Exists(userAssemblyDirectoryCandidate))
                {
                    this.VishnuRoot = this.ApplicationRootPath;
                    relativeVishnuRoot = this.ApplicationRootPath;
                    this.VishnuRoot = this.GetStringValue("Vishnu_Root", relativeVishnuRoot)
                        ?? throw new ArgumentNullException("VishnuRoot darf hier nicht null sein.");
                    this.VishnuSourceRoot = Path.Combine(this.ApplicationRootPath, @"../..");
                }
                else
                {
                    if (!this.IsFrameworkAssembly)
                    {
                        relativeVishnuRoot = Path.Combine(this.ApplicationRootPath, @"../../../../../..");
                    }
                    else
                    {
                        relativeVishnuRoot = Path.Combine(this.ApplicationRootPath, @"../../../../..");
                    }
                    this.VishnuRoot = this.GetStringValue("Vishnu_Root", relativeVishnuRoot)
                        ?? throw new ArgumentNullException("VishnuRoot darf hier nicht null sein.");
                    this.VishnuSourceRoot = VishnuRoot;
                    userAssemblyDirectoryCandidate =
                        Path.Combine(this.VishnuRoot, "ReadyBin", "UserAssemblies");
                }
            }
            if (!Directory.Exists(userAssemblyDirectoryCandidate))
            {
                userAssemblyDirectoryCandidate =
                    Path.Combine(this.VishnuRoot, "ReadyBin", "UserAssemblies");
            }
            this.AppEnvAccessor.RegisterKeyValue("VishnuRoot", Path.GetFullPath(this.VishnuRoot));
            AppSettingsRegistry.RememberParameterSource("VishnuRoot", "Logic", Path.GetFullPath(this.VishnuRoot));
            this.AppEnvAccessor.RegisterKeyValue("VishnuSourceRoot", Path.GetFullPath(this.VishnuSourceRoot));
            AppSettingsRegistry.RememberParameterSource("VishnuSourceRoot", "Logic", Path.GetFullPath(this.VishnuSourceRoot));
            this.UserAssemblyDirectory = this.GetStringValue("UserAssemblyDirectory",
                this.GetStringValue("Vishnu_UserAssemblies", userAssemblyDirectoryCandidate)) ?? userAssemblyDirectoryCandidate;
            this.AssemblyDirectories = new List<string> {
                "",
                AppDomain.CurrentDomain.BaseDirectory
            };
            if (!this.AssemblyDirectories.Contains(this.UserAssemblyDirectory))
            {
                this.AssemblyDirectories.Insert(0, this.UserAssemblyDirectory);
                this.AppEnvAccessor.RegisterKeyValue("UserAssemblyDirectory", this.UserAssemblyDirectory);
            }

            string? preferredJobDescriptionFormat = this.GetStringValue("PreferredJobDescriptionFormat", "xml");
            preferredJobDescriptionFormat = preferredJobDescriptionFormat?.ToLower().Trim().TrimStart('.');
            if (preferredJobDescriptionFormat == "json")
            {
                this.PreferredJobDescriptionFormat = "json";
                this.ValidJobDescriptionExtensions =
                    new List<string>(".json;.xml".Split(';'));
            }
            else
            {
                this.PreferredJobDescriptionFormat = "xml";
                this.ValidJobDescriptionExtensions =
                    new List<string>(".xml;.json".Split(';'));
            }

            // --------------------------------------------------------------------------------------------------------
            // Ab hier wird der Job-Pfad übernommen oder auf Demo-Job-Pfad gesetzt, ein Job-Name gesetzt und
            // RootJobPhysicalName von RootJobPackagePath separiert.
            // --------------------------------------------------------------------------------------------------------
            this.JobDirPathes = new Stack<string>();
            this.JobDirPathes.Push(Path.Combine(Path.GetDirectoryName(this.UserAssemblyDirectory) ?? "", "Vishnu.bin"));
            this.JobDirPathes.Push(Path.Combine(this.VishnuSourceRoot, "VishnuHome/Tests"));
            this.JobDirPathes.Push(Path.Combine(this.VishnuSourceRoot, "VishnuHome/Documentation"));
            this.JobDirPathes.Push(this.ApplicationRootPath);
            this.JobDirPathes.Push("");
            string? rootJobPackagePath = this.GetStringValue("Job", this.GetStringValue("1", null));
            // mögliche Belegungen (bei allen sind Wildcards '%xyz%' möglich):
            // 1. Pfad zum Jobverzeichnis
            // 2. Pfad inklusive JobDescription.{xml|json}
            // 3. Pfad mit Endung .zip oder ohne Endung, aber gezipptes File
            // 4. null
            bool defaultDemo = false;
            if (rootJobPackagePath == null)
            {
                rootJobPackagePath = @"DemoJobs\Simple\CheckAll";
                defaultDemo = true;
            }
            this.DemoModus = this.GetValue<bool>("DemoModus", defaultDemo);
            this.RootJobPackagePath = this.PrepareAndLocatePath(rootJobPackagePath, this.JobDirPathes.ToArray());
            this.MainJobName = Path.GetFileNameWithoutExtension(this.RootJobPackagePath);
            if (this.MainJobName.ToLower() == "jobdescription")
            {
                this.MainJobName = Path.GetFileName(Path.GetDirectoryName(
                    this.RootJobPackagePath)) ?? "";
            }
            this.AppEnvAccessor.RegisterKeyValue("JobDirectory", this.RootJobPackagePath);
            // --------------------------------------------------------------------------------------------------------
            string? tmpDirectory = null;
            if (this.AppEnvAccessor.IsDefault("WorkingDirectory"))
            {
                string jobnameExtension = "";
                if (!String.IsNullOrEmpty(this._mainJobName))
                {
                    jobnameExtension = "." + this.MainJobName;
                }
                tmpDirectory = this.GetStringValue("__REPLACE_WILDCARDS__", Path.Combine(Path.Combine(this.TempDirectory, this.ApplicationName + jobnameExtension),
                  this.ProcessId.ToString()).TrimEnd(Path.DirectorySeparatorChar));
            }
            else
            {
                tmpDirectory = this.GetStringValue("__REPLACE_WILDCARDS__", this.WorkingDirectory);
            }
            if (tmpDirectory != null)
            {
                this.WorkingDirectory = tmpDirectory;
            }
            this.AppEnvAccessor.RegisterKeyValue("WorkingDirectory", this.WorkingDirectory);
            this.LocalConfigurationDirectory = this.GetStringValue("LocalConfigurationDirectory", Path.GetDirectoryName(this.AppConfigUser));

            this.SnapshotDirectory = this.GetStringValue("SnapshotDirectory", "..\\Snapshots") ?? String.Empty;
            if (!(this.SnapshotDirectory.Contains(":")
                  || this.SnapshotDirectory.StartsWith(@"/")
                  || this.SnapshotDirectory.StartsWith(@"\")))
            {
                if (!Directory.Exists(this.RootJobPackagePath))
                {
                    this.SnapshotDirectory = Path.Combine(
                        Path.GetDirectoryName(this.RootJobPackagePath) ?? "", this.SnapshotDirectory);
                }
                else
                {
                    this.SnapshotDirectory = Path.Combine(this.RootJobPackagePath, this.SnapshotDirectory);
                }
            }
            this.AppEnvAccessor.RegisterKeyValue("SnapshotDirectory", this.SnapshotDirectory);

            string defaultDebugFile = this.WorkingDirectory + Path.DirectorySeparatorChar + this.ApplicationName + @".log";
            this.DebugFile = this.GetStringValue("DebugFile", defaultDebugFile) ?? defaultDebugFile;
            this.AppEnvAccessor.RegisterKeyValue("DebugFile", this.DebugFile);
            this.StatisticsFile = this.GetStringValue("StatisticsFile", this.WorkingDirectory + Path.DirectorySeparatorChar + this.ApplicationName + @".stat");

            this.AbortingAllowed = false; // Immer: false; künftig wegfallend. // this.GetValue<bool>("AbortingAllowed", true);
            this.AcceptNullResults = this.GetValue<bool>("AcceptNullResults", false);

            this.UserParameterReaderPath = this.GetStringValue("UserParameterReaderPath", null);
            this.StartTreeOrientation = (TreeOrientation)Enum.Parse(typeof(TreeOrientation),
                this.GetStringValue("StartTreeOrientation", "AlternatingHorizontal") ?? "AlternatingHorizontal");
            this.StartWithJobs = this.GetValue<bool>("StartWithJobs", false);
            string? userCheckerArrayString = this.GetStringValue("UncachedCheckers", null);
            if (userCheckerArrayString != null)
            {
                this.UncachedCheckers = new List<string>(userCheckerArrayString.Split(';'));
            }
            else
            {
                this.UncachedCheckers = new List<string>();
            }
            this.FlatNodeListFilter = NodeTypes.Constant | NodeTypes.JobConnector | NodeTypes.NodeConnector | NodeTypes.NodeList;
            string? filters = this.GetStringValue("FlatNodeListFilter", null);
            if (filters != null)
            {
                this.FlatNodeListFilter = NodeTypes.None;
                foreach (string filter in filters.Split('|'))
                {
                    string cleanFilter = filter.ToLower().Trim();
                    switch (cleanFilter)
                    {
                        case "nodeconnector":
                            this.FlatNodeListFilter |= NodeTypes.NodeConnector;
                            break;
                        case "valuemodifier":
                            this.FlatNodeListFilter |= NodeTypes.ValueModifier;
                            break;
                        case "constant":
                            this.FlatNodeListFilter |= NodeTypes.Constant;
                            break;
                        case "checker":
                            this.FlatNodeListFilter |= NodeTypes.Checker;
                            break;
                        case "nodelist":
                            this.FlatNodeListFilter |= NodeTypes.NodeList;
                            break;
                        case "joblist":
                            this.FlatNodeListFilter |= NodeTypes.JobList;
                            break;
                        case "snapshot":
                            this.FlatNodeListFilter |= NodeTypes.Snapshot;
                            break;
                        default:
                            this.FlatNodeListFilter |= NodeTypes.None;
                            break;
                    }
                }
            }
            if (!Enum.TryParse(this.GetStringValue("StartTriggeredNodesOnUserRun", "Direct"),
              true, out this._startTriggeredNodesOnUserRun))
            {
                this._startTriggeredNodesOnUserRun = TriggeredNodeStartConstraint.Direct;
            }
            if (this._startTriggeredNodesOnUserRun == TriggeredNodeStartConstraint.DirectNoTreeEvents)
            {
                this._startTriggeredNodesOnUserRun = TriggeredNodeStartConstraint.Direct | TriggeredNodeStartConstraint.NoTreeEvents;
            }
            if (this._startTriggeredNodesOnUserRun == TriggeredNodeStartConstraint.AllNoTreeEvents)
            {
                this._startTriggeredNodesOnUserRun = TriggeredNodeStartConstraint.All | TriggeredNodeStartConstraint.NoTreeEvents;
            }
            if (!Enum.TryParse(this.GetStringValue("ControlledNodeUserRunDialog", "Question"),
              true, out this._controlledNodeUserRunDialog))
            {
                this._controlledNodeUserRunDialog = DialogSettings.Question;
            }
            if ((this.GetStringValue("r", "")?.ToUpper() == "R") || (this.GetStringValue("run", "")?.ToUpper() == "RUN"))
            {
                this.Autostart = true;
            }
            else
            {
                this.Autostart = this.GetValue<bool>("Autostart", defaultDemo);
            }
            this.SizeOnVirtualScreen = this.GetValue<bool>("SizeOnVirtualScreen", false);
            this.LoadJobDialog = this.GetValue<bool>("LoadJobDialog", false);
            this.LogicalChangedDelay = this.GetValue<int>("LogicalChangedDelay", 0);
            this.LogicalChangedDelayPower = this.GetValue<double>("LogicalChangedDelayPower", 0);
            this.BreakTreeBeforeStart = this.GetValue<bool>("BreakTreeBeforeStart", false);
            this.InitAtUserRun = this.GetValue<bool>("InitAtUserRun", false);
            this.TryRunAsyncSleepTime = this.GetValue<int>("TryRunAsyncSleepTime", 100);
            this.HelpPreference = (this.GetStringValue("HelpPreference", "online") ?? "online").Trim().ToLower();
            string? tmpUnlock = this.GetStringValue("XUnlock", "");
            this.XUnlock = new SecureString();
            if (tmpUnlock != null)
            {
                for (int i = 0; i < tmpUnlock.Length; i++)
                {
                    char c = tmpUnlock[i];
                    this.XUnlock.AppendChar(c);
                }
            }
            this.XUnlock.MakeReadOnly();
            this.NoWorkers = this.GetValue<bool>("NoWorkers", false);
            this.DumpLoadedJobs = this.GetValue<bool>("DumpLoadedJobs", false);
            if (!String.IsNullOrEmpty(this.UserParameterReaderPath))
            {
                string resolvedUserParameterReaderPath = "???";
                try
                {
                    resolvedUserParameterReaderPath = this.ReplaceWildcards(this.UserParameterReaderPath);
                    this.LoadUserParameterReader(resolvedUserParameterReaderPath);
                }
                catch (Exception ex)
                {
                    this.FatalInitializationException
                        = new ApplicationException("Ausnahme bei " + resolvedUserParameterReaderPath, ex);
                }
            }
            this.VishnuProvider = this.GetVishnuProvider();
            string sleepTimeString = this.GetStringValue("SleepTime", "00:00:00-00:00:00") ?? "00:00:00-00:00:00";
            this.SetSleepTime(sleepTimeString);
            string snapshotSustainString = this.GetStringValue("SnapshotSustain", "00:00:02") ?? "00:00:02";
            this.SetSnapshotSustain(snapshotSustainString);
        }

        /// <summary>
        /// Übernimmt einen Pfad-Kandidaten zu einer Datei oder einem Verzeichnis.
        /// Ersetzt Wildcards der Form "%Name%" innerhalb des Pfad-Kandidaten,
        /// ergänzt diesen anhand der zusätzlich übergebenen verschiedenen pathCandidates,
        /// versucht verschiedene Endungen (z.B. ".zip") und/oder ".xml" oder ".json"
        /// und prüft die Existenz bzw. Erreichbarkeit.
        /// </summary>
        /// <param name="rawPath">Absoluter oder relativer Pfad, der zu ergänzen und zu prüfen ist.</param>
        /// <param name="pathCandidates">Mögliche Pfad-Teile zum Kombinieren; Default: this.JobDirPathes.ToArray().</param>
        /// <param name="throwIfFail">Wenn der Path nicht existiert und throwIfFail true ist, wird eine Exception geworfen; defaule: true.</param>
        /// <returns>Modifizierter und verifizierter Pfad zu einer Datei oder leerer String.</returns>
        /// <exception cref="ArgumentException">Exception, wenn kein Pfad übergeben wurde.</exception>
        /// <exception cref="FileNotFoundException">Exception, wenn kein möglicher Pfad existiert.</exception>
        public string PrepareAndLocatePath(string? rawPath, string[]? pathCandidates = null, bool throwIfFail = true)
        {
            if (rawPath == null)
            {
                throw new ArgumentException("Es wurde kein gültiger Job-Pfad gefunden.");
            }
            if (pathCandidates == null)
            {
                pathCandidates = this.JobDirPathes.ToArray();
            }
            string tmpPath = ReplaceWildcardsAndPathes(rawPath, pathCandidates);
            tmpPath = UnpackIfPacked(tmpPath);
            if (!Directory.Exists(tmpPath) && !File.Exists(tmpPath))
            {
                foreach (string extension in this.ValidJobDescriptionExtensions)
                {
                    string tmpPath2 = tmpPath + extension;
                    if (File.Exists(tmpPath2))
                    {
                        tmpPath = tmpPath2;
                        break;
                    }
                }
            }
            if (Directory.Exists(tmpPath))
            {
                tmpPath = Path.Combine(tmpPath, "JobDescription");
                foreach (string extension in this.ValidJobDescriptionExtensions)
                {
                    string tmpPath2 = tmpPath + extension;
                    if (File.Exists(tmpPath2))
                    {
                        tmpPath = tmpPath2;
                        break;
                    }
                }
            }
            if (throwIfFail && !File.Exists(tmpPath))
            {
                throw new FileNotFoundException(String.Format(
                    $"Der Pfad wurde nicht gefunden: {rawPath}."));
            }
            return tmpPath;
        }

        /// <summary>
        /// Ersetzt Wildcards und Pfade in einem Parameter-String.
        /// </summary>
        /// <param name="para">String mit Parametern, in denen Wildcards ersetzt werden sollen.</param>
        /// <param name="searchDirectories">Array von Pfaden, die ggf. durchsucht/eingesetzt werden.</param>
        /// <returns>Der "para"-String mit ersetzten Wildcards.</returns>
        public string ReplaceWildcardsAndPathes(string para, string[] searchDirectories)
        {
            para = this.ReplaceWildcards(para);
            string paraString = "";
            string delimiter = "";
            foreach (string paraPart in para.Split('|'))
            {
                string modifiedParaPart = paraPart;
                if (modifiedParaPart.Trim() != String.Empty)
                {
                    string path = NetworkMappingsRefresher.GetNextReachablePath(
                        modifiedParaPart, searchDirectories) ?? modifiedParaPart;
                    modifiedParaPart = path.Replace(@"Plugin\Plugin", "Plugin");
                }
                paraString += delimiter + modifiedParaPart;
                delimiter = "|";
            }
            return paraString;
        }

        // Alter Kommentar: Achtung: diese String-Ersetzung verursacht Memory-Leaks! Deshalb nicht in Loops verwenden.
        // 14.01.2018 Nagel: Neue Tests konnten keine Memory-Leaks aufzeigen (siehe BasicAppSettingsDemo).
        /// <summary>
        /// Ersetzt hier definierte Wildcards durch ihre Laufzeit-Werte:
        /// '%HOME%': '...bin\Debug'.
        /// </summary>
        /// <param name="inString">Wildcard</param>
        /// <returns>Laufzeit-Ersetzung</returns>
        public override string ReplaceWildcards(string inString)
        {
            try
            {
                ThreadLocker.LockNameGlobal("ReplaceWildcards");
                string replaced = base.ReplaceWildcards(inString);
                // Regex.Replace(inString, @"%HOME%", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), RegexOptions.IgnoreCase);
                if (inString.ToUpper().Contains("%JOBDIRECTORIES%"))
                {
                    replaced = String.Join(",", this.JobDirPathes.ToArray());
                }
                return replaced;
            }
            finally
            {
                ThreadLocker.UnlockNameGlobal("ReplaceWildcards");
            }
        }

        /// <summary>
        /// Löst den übergebenen Pfad unter Berücksichtigung der Suchreihenfolge
        /// in einen gesicherten Pfad auf, wenn möglich.
        /// </summary>
        /// <param name="path">Pfad zur Dll/Exe.</param>
        /// <returns>Gesicherter Pfad zur Dll/Exe</returns>
        /// <exception cref="IOException" />
        public static string GetResolvedPath(string path)
        {
            AppSettings appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            foreach (string tmpDirectory in appSettings.AssemblyDirectories)
            {
                if (tmpDirectory == "" || Directory.Exists(tmpDirectory))
                {
                    string tmpPath = Path.Combine(tmpDirectory, path).Replace(@"Plugin\Plugin", "Plugin");
                    if (File.Exists(tmpPath) || Directory.Exists(tmpPath))
                    {
                        return tmpPath;
                    }
                }
            }
            throw new IOException(String.Format("Der Pfad {0} wurde nicht gefunden.", path));
        }

        /// <summary>
        /// Registriert die übergebene IGetStringValue-Instanz (UserParameterReader)
        /// beim enthaltenen AppEnvReader. Der neue Getter wird in der Auswertungskette
        /// direkt hinter CommandLineAccess eingehängt, was ihm (nach der Kommandozeile)
        /// höchste Priorität gibt.
        /// </summary>
        /// <param name="stringValueGetter">Zu registrierende IGetStringValue-Instanz.</param>
        public void RegisterUserStringValueGetter(IGetStringValue stringValueGetter)
        {
            this.AppEnvAccessor.RegisterStringValueGetterAt(stringValueGetter, 1);
        }

        #endregion public members

        #region private members

        private string _mainJobName;
        private TriggeredNodeStartConstraint _startTriggeredNodesOnUserRun;
        private DialogSettings _controlledNodeUserRunDialog;
        private IParameterReader? _userParameterReader;
        private string? _userParameterReaderParameters;
        private bool _sleepTimeSet;
        private string _jsonJobPathesLog;

        /// <summary>
        /// Private Konstruktor, wird ggf. über Reflection vom externen statischen
        /// GenericSingletonProvider über GetInstance() aufgerufen.
        /// Holt alle Infos und stellt sie als Properties zur Verfügung.
        /// </summary>
#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
        private AppSettings()
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
          : base()
        {
            // Die nachfolgenden Properties werden in BasicAppSettings auf jeden Fall gefüllt,
            // weswegen oben die warning CS8618 disabled wird.

            //this.FatalInitializationException = null;
            //this.AssemblyDirectories = new List<string>();
            //this.JobDirPathes = new Stack<string>();
            //this.UserAssemblyDirectory = ".";
            //this.RootJobXmlName = "jobdescription.xml";
            //this.UncachedCheckers = new List<string>();
            //this._mainJobName = string.Empty;
        }

        private void ParameterReader_ParametersReloaded(object? sender, EventArgs e)
        {
            this.OnUserParametersReloaded();
        }

        /// <summary>
        /// Löst das UserParametersReloaded-Ereignis aus.
        /// </summary>
        private void OnUserParametersReloaded()
        {
            UserParametersReloaded?.Invoke(this, new EventArgs());
        }

        // Wenn der Job ein ZIP-Archiv ist, wird der Job ins WorkingDirectory
        // entpackt und ein angepasster Pfad zum Job zurückgeliefert.
        private string UnpackIfPacked(string physicalJobPath)
        {
            string tmpPath = physicalJobPath;
            if (!File.Exists(tmpPath))
            {
                tmpPath = tmpPath.TrimEnd('\\').TrimEnd('/') + ".zip";
            }
            if (File.Exists(tmpPath))
            {
                ZipAccess zipAccess = new ZipAccess();
                try
                {
                    if (zipAccess.IsZip(tmpPath))
                    {
                        if (String.IsNullOrEmpty(this.ZipRelativeDummyDirectory)
                            || !Directory.Exists(this.ZipRelativeDummyDirectory))
                        {
                            this.CreateAndRememberZipRelativeDummyDirectory(tmpPath);
                        }
                        physicalJobPath = this.Unpack(tmpPath, zipAccess);
                    }
                }
                finally
                {
                    zipAccess.Dispose();
                }
            }
            return physicalJobPath;
        }

        // Entpackt einen Job aus einem ZIP-Archiv.
        private string Unpack(string archive, ZipAccess zipAccess)
        {
            string rtn = Directory.GetCurrentDirectory();
            zipAccess.UnZipArchive(archive, this.WorkingDirectory, null, false);
            string archivePath = Path.Combine(Path.GetDirectoryName(archive) ?? "", Path.GetFileNameWithoutExtension(archive));
            if (!this.AssemblyDirectories.Contains(archivePath))
            {
                this.AssemblyDirectories.Add(archivePath);
            }
            if (!this.JobDirPathes.Contains(archivePath))
            {
                this.JobDirPathes.Push(archivePath);
            }
            rtn = Path.Combine(this.WorkingDirectory, Path.GetFileNameWithoutExtension(archive));
            return rtn;
        }

        private void CreateAndRememberZipRelativeDummyDirectory(string zipPath)
        {
            string fallbackDirectory = Directory.GetCurrentDirectory();
            string randomDirectoryName = Path.Combine(Path.GetDirectoryName(zipPath) ?? fallbackDirectory,
                Path.GetRandomFileName() + ".v");
            Directory.CreateDirectory(randomDirectoryName);
            this.ZipRelativeDummyDirectory = randomDirectoryName;
            if (!this.JobDirPathes.Contains(randomDirectoryName))
            {
                this.JobDirPathes.Push(randomDirectoryName);
            }
        }

        private void SetSnapshotSustain(string snapshotSustainString)
        {
            TimeSpan snapshotSustain;
            if (TimeSpan.TryParse(snapshotSustainString, out snapshotSustain))
            {
                this.SnapshotSustain = snapshotSustain;
            }
            else
            {
                this.SnapshotSustain = new TimeSpan(0, 0, 1);
            }
        }

        private void SetSleepTime(string sleepTimeString)
        {
            this._sleepTimeSet = false;
            TimeSpan sleepTimeFrom;
            TimeSpan sleepTimeTo;
            string? sleeptimeFromString = sleepTimeString.Split('-')[0]?.Trim();
            string? sleeptimeToString = sleepTimeString.Split('-')[1]?.Trim();
            if (TimeSpan.TryParse(sleeptimeFromString, out sleepTimeFrom))
            {
                this.SleepTimeFrom = sleepTimeFrom;
                if (sleepTimeFrom != new TimeSpan(0, 0, 0))
                {
                    this._sleepTimeSet = true;
                }
            }
            else
            {
                this.SleepTimeFrom = new TimeSpan(0, 0, 0);
            }
            if (TimeSpan.TryParse(sleeptimeToString, out sleepTimeTo))
            {
                this.SleepTimeTo = sleepTimeTo;
                if (sleepTimeTo != new TimeSpan(0, 0, 0))
                {
                    this._sleepTimeSet = true;
                }
            }
            else
            {
                this.SleepTimeTo = new TimeSpan(0, 0, 0);
            }
            if (this.SleepTimeFrom == this.SleepTimeTo)
            {
                this._sleepTimeSet = false;
            }
        }

        // Lädt dynamisch eine optionale zusätzliche Dll, die eine
        // IParameterReader-Instanz zur Verfügung stellt.
        // Der zusätzliche ParameterReader wird an der zweiten Stelle
        // der Auswertungskette direkt hinter CommandLineAccess eingefügt,
        // so dass er (nach der Kommandozeile) höchste Priorität bekommt.
        private void LoadUserParameterReader(string userParameterReaderPathAndParameters)
        {
            string[] pathPara = (userParameterReaderPathAndParameters + "|").Split('|', 2);
            string userParameterReaderPath = pathPara[0];
            string userParameterReaderParameters = pathPara[1].TrimEnd('|');
            IParameterReader? parameterReader = null;
            try
            {
                VishnuAssemblyLoader loader = VishnuAssemblyLoader.GetAssemblyLoader(this.AssemblyDirectories);
                if (loader != null)
                {
                    object? candidate = loader.DynamicLoadObjectOfTypeFromAssembly(userParameterReaderPath, typeof(IParameterReader));
                    if (candidate != null)
                    {
                        parameterReader = (IParameterReader)candidate;
                    }
                }

            }
            catch { }
            if (parameterReader != null)
            {
                this.RegisterUserStringValueGetter(
                    new ParameterReaderToIGetStringValue(parameterReader, userParameterReaderPath));
                this._userParameterReader = parameterReader;
                this._userParameterReaderParameters = userParameterReaderParameters;
                parameterReader.ParametersReloaded += ParameterReader_ParametersReloaded;
            }
            else
            {
                throw new ApplicationException(String.Format("Die User-Assembly {0} konnte nicht geladen werden.",
                    userParameterReaderPath));
            }
        }

        private string GetVishnuProvider()
        {
            string vishnuProvider = this.GetStringValue("VishnuProvider", "NetEti") ?? "NetEti";
            return vishnuProvider;
        }

        // Kapselt eine IParameterReader-Klasse in eine IGetStringValue-Klasse
        // damit diese über die AppSettings registriert werden kann.
        private class ParameterReaderToIGetStringValue : IGetStringValue
        {
            /// <summary>
            /// Liefert genau einen Wert zu einem Key. Wenn es keinen Wert zu dem
            /// Key gibt, wird defaultValue zurückgegeben.
            /// </summary>
            /// <param name="key">Der Zugriffsschlüssel (string)</param>
            /// <param name="defaultValue">Das default-Ergebnis (string)</param>
            /// <returns>Der Ergebnis-String</returns>
            public string? GetStringValue(string key, string? defaultValue)
            {
                string? result = this._parameterReader.ReadParameter(key);
                return result ?? defaultValue;
            }

            /// <summary>
            /// Liefert ein string-Array zu einem Key. Wenn es keinen Wert zu dem
            /// Key gibt, wird defaultValue zurückgegeben.
            /// </summary>
            /// <param name="key">Der Zugriffsschlüssel (string)</param>
            /// <param name="defaultValues">Das default-Ergebnis (string[])</param>
            /// <returns>Das Ergebnis-String-Array</returns>
            public string?[]? GetStringValues(string key, string?[]? defaultValues)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Konstruktor - übernimmt eine IParameterReader-Instanz und den Pfad dorthin.
            /// </summary>
            /// <param name="parameterReader">Zu kapselnde IParameterReader-Instanz.</param>
            /// <param name="pathToReader">Pfad zum ParameterReader.</param>
            public ParameterReaderToIGetStringValue(IParameterReader parameterReader, string pathToReader)
            {
                this._parameterReader = parameterReader;
                this._pathToReader = pathToReader;
            }

            /// <summary>
            /// Liefert einen beschreibenden Namen dieses StringValueGetters,
            /// z.B. Name plus ggf. Quellpfad.
            /// </summary>
            public string Description
            {
                get
                {
                    return "UserParameterReader(" + this._pathToReader + ")";
                }
                set
                {
                }
            }

            private IParameterReader _parameterReader;
            private string _pathToReader;
        }

        /// <summary>
        /// Speichert die aktuellen Suchpfade in eine externe Json-Datei
        /// zur weiteren Verwendung in anderen Prozessen über GetResolvedVishnuPath.
        /// </summary>
        public void SaveJobPathes()
        {
            string jsonJobLog = VishnuAssemblyLoader.GetJobPathesLogPath(this.ProcessId.ToString());
            List<string> jobPathes =
                new List<string>(this.AssemblyDirectories);
            SerializationUtility.SaveToJsonFile<List<string>>(jobPathes, jsonJobLog, includeFields: true);
            this._jsonJobPathesLog = jsonJobLog;
            string fallbackJsonJobLog = VishnuAssemblyLoader.GetJobPathesLogPath("0");
            SerializationUtility.SaveToJsonFile<List<string>>(jobPathes, fallbackJsonJobLog, includeFields: true);
        }

        #endregion private members

    } // public sealed class AppSettings: BasicAppSettings
}
