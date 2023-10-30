using System.Windows;
using Vishnu.ViewModel;
using NetEti.ApplicationControl;
using NetEti.Globals;
using System;
using System.IO;
using NetEti.CustomControls;
using LogicalTaskTree.Provider;
using System.Reflection;
using System.Text;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using Vishnu.Interchange;

namespace Vishnu
{
    /// <summary>
    /// Enthält die statische "Main"-Methode.
    /// Hier startet als erstes der nicht-WPF Teil der Anwendung.
    /// </summary>
    public class EntryPoint
    {
        /// <summary>
        /// Statischer Entry-Point - hier startet die Anwendung.
        /// </summary>
        /// <param name="args">Kommandozeilen-Argumente.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            SetAddRemoveProgramsIcon();
            SingleInstanceManager manager = new SingleInstanceManager();
            manager.Run(args);
        }


        private static bool IsNetworkDeployed
        {
            get
            {
                // return System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed;
                return false;
            }
        }

        private static bool IsFirstRun
        {
            get
            {
                // return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.IsFirstRun;
                return false;
            }
        }

        /// <summary>
        /// Setzt das Icon in add/remove programs für Vishnu
        /// </summary>
        private static void SetAddRemoveProgramsIcon()
        {
            // 11.03.2023 Nagel+
            // if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed
            //    && System.Deployment.Application.ApplicationDeployment.CurrentDeployment.IsFirstRun)
            // 11.03.2023 Nagel-

            // nur, wenn deployed
            if (IsNetworkDeployed && IsFirstRun)
            {
                try
                {
                    string iconSourcePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? "", "Vishnu_multi.ico");
                    if (!File.Exists(iconSourcePath))
                    {
                        return;
                    }

                    RegistryKey? myUninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (myUninstallKey != null)
                    {
                        string[] mySubKeyNames = myUninstallKey.GetSubKeyNames();
                        for (int i = 0; i < mySubKeyNames.Length; i++)
                        {
                            RegistryKey? myKey = myUninstallKey.OpenSubKey(mySubKeyNames[i], true);
                            object? myValue = myKey?.GetValue("DisplayName");
                            if (myValue != null && myValue.ToString() == "Vishnu")
                            {
                                myKey?.SetValue("DisplayIcon", iconSourcePath);
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Erkennt eine eventuell schon laufende Instanz dieser Anwendung und
    /// entscheidet abhängig vom Schalter WindowsFormsApplicationBase.IsSingleInstance:
    ///   a) IsSingleInstance = true: informiert die schon laufende Instanz dieser
    ///      Anwendung, dass diese sich in den Vordergrund bringen soll und
    ///      beendet sich dann, um Mehrfach-Instanziierungen zu vermeiden;
    ///   b) IsSingleInstance = false: diese Instanz startet normal.
    /// Nutzt die Assembly Microsoft.VisualBasic.dll.
    /// </summary>
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private SingleInstanceApplication? _app;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        public SingleInstanceManager()
        {
            try
            {
                this.IsSingleInstance = GenericSingletonProvider.GetInstance<AppSettings>().SingleInstance;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(String.Format($"Fehler bei der Initialisierung: {ex.Message}"), "Fehlermeldung");
                throw;
            }
        }

        /// <summary>
        /// Wird beim Start der Anwendung durchlaufen - Teil der Start-Anwendung (nicht WPF).
        /// </summary>
        /// <param name="e">Enthält die Kommandozeilen-Argumente.</param>
        /// <returns>Immer false.</returns>
        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            _app = new SingleInstanceApplication();
            _app.Run();
            return false;
        }

        /// <summary>
        /// Wird bei weiteren Starts der Anwendung durchlaufen - Teil der Start-Anwendung (nicht WPF).
        /// </summary>
        /// <param name="eventArgs">Enthält die Kommandozeilen-Argumente.</param>
        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            base.OnStartupNextInstance(eventArgs);
            _app?.Activate();
        }
    }

    /// <summary>
    /// Hauptklasse für den Start der WPF-Anwendung.
    /// App.cs entspricht App.xaml.cs einer klassischen WPF-Anwendung.
    /// Hier startet als erstes der nicht-WPF Teil der Anwendung.
    /// </summary>
    /// <remarks>
    /// File: App.cs
    /// Autor: Erik Nagel
    ///
    /// 04.03.2013 Erik Nagel: erstellt
    /// 14.12.2013 Erik Nagel: Erfolgreicher Memorycheck (memory.txt). 
    /// 14.07.2016 Erik Nagel: Der Pfad "DebugFile" kann jetzt in Vishnu.exe.config.user überschrieben werden. 
    /// 28.10.2017 Erik Nagel: Implementierung von IsSingleInstance, 
    ///                        von App.xaml.cs in App.cs umbenannt und App.xaml rausgeschmissen.
    /// 23.06.2019 Erik Nagel: Aufräumen aus OnProcessExit in mainWindow_Closed und
    ///                        AppDomain.CurrentDomain.UnhandledException gezogen, da OnProcessExit
    ///                        u.U. nicht mehr ganz durchlaufen wird, sondern z.B. bei Close des
    ///                        MainWindows über "X" rechts oben abrupt abbricht.
    /// </remarks>
    public class SingleInstanceApplication : System.Windows.Application
    {
        // Das Main-Window
        private WPF_UI.MainWindow? _mainWindow;

        /// <summary>
        /// Wird von einer anderen startenden Instanz angesprungen, bevor jene sich beendet,
        /// wenn deren Schalter IsSingleInstance = true ist.
        /// </summary>
        public void Activate()
        {
            if (this._mainWindow == null)
            {
                return;
            }
            // DEBUG: MessageBox.Show("Ich wurde gerade aktiviert.");
            // Reaktivieren des Hauptfensters dieser Instanz der Anwendung.
            if (!this._mainWindow.IsVisible)
            {
                this._mainWindow.Show();
            }

            if (this._mainWindow.WindowState == WindowState.Minimized)
            {
                this._mainWindow.WindowState = WindowState.Normal;
            }

            this._mainWindow.Activate();
            //this._mainWindow.Topmost = true;  // nicht erforderlich
            //this._mainWindow.Topmost = false; // nicht erforderlich
            this._mainWindow.Focus();           // wichtig!
        }

        /// <summary>
        /// Wird beim Start der Anwendung durchlaufen.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            SingleInstanceApplication._cleanupDone = false;
            this._splashWindow = SplashWindow.StartSplashWindow();
            this._splashScreenMessageReceiver = new ViewerAsWrapper(this.handleSplashScreenMessages);
            InfoController.GetInfoSource().RegisterInfoReceiver(this._splashScreenMessageReceiver, typeof(SplashScreenMessage), new[] { InfoType.Info });
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("initializing system"));

            AppDomain.CurrentDomain.UnhandledException += (object sender, System.UnhandledExceptionEventArgs args) =>
            {
                string exceptionString;
#if DEBUG
                exceptionString = ((Exception)args.ExceptionObject).ToString(); // Exception für später retten.
#else
                exceptionString = ((Exception)args.ExceptionObject).Message; // Exception für später retten.
#endif
                if (this._splashWindow != null)
                {
                    try
                    {
                        this._splashWindow.FinishAndClose();
                    }
                    catch { }
                }
                if (!SingleInstanceApplication._cleanupDone)
                {
                    SingleInstanceApplication.doVishnuCleanup();
                }
                System.Windows.MessageBox.Show(exceptionString, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                //Application.Current.Shutdown(-1);            // die auskommentierten
                //Dispatcher.BeginInvoke((Action)delegate()    // Versuche
                //{                                            // funktionieren
                //  Application.Current.Shutdown();            // beide NICHT!
                //});
                Environment.Exit(-1);
            };
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            SingleInstanceApplication._appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            this._splashWindow.ShowVersion(SingleInstanceApplication._appSettings.ProgrammVersion);
            if (SingleInstanceApplication._appSettings.FatalInitializationException == null)
            {
                SingleInstanceApplication._appSettings.UserParametersReloaded += ParameterReader_ParametersReloaded;
                SingleInstanceApplication._appSettings.InitUserParameterReader();
            }

            this.PrepareStart();
            if (SingleInstanceApplication._appSettings.DemoModus)
            {
                this._splashWindow.ShowVersion(SingleInstanceApplication._appSettings.ProgrammVersion + " DEMO");
            }

            // Die Business-Logic
            // Demo: hart verdrahteter JobProvider:
            // App._businessLogic = new LogicalTaskTree.LogicalTaskTree(new TreeParameters("Tree 1", null), new MixedTestJobProvider());

            // Die für den gesamten Tree gültigen Parameter
            TreeParameters treeParameters = new TreeParameters(String.Format($"Tree {++LogicalTaskTree.LogicalTaskTree.TreeId}"), null);

            // Der Produktions-JobProvider mit extern über XML definierten Jobs:
            SingleInstanceApplication._businessLogic = new LogicalTaskTree.LogicalTaskTree(treeParameters, new ProductionJobProvider());

            // Das Main-Window
            this._mainWindow = new WPF_UI.MainWindow(_appSettings.StartWithJobs, _appSettings.SizeOnVirtualScreen, _appSettings.VishnuWindowAspects);
            this._mainWindow.Closed += mainWindow_Closed;
            // Das LogicalTaskTree-ViewModel
            LogicalTaskTreeViewModel logicalTaskTreeViewModel = new LogicalTaskTreeViewModel(
                SingleInstanceApplication._businessLogic, this._mainWindow, SingleInstanceApplication._appSettings.StartTreeOrientation,
                SingleInstanceApplication._appSettings.FlatNodeListFilter, treeParameters);

            // Das Main-ViewModel
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(logicalTaskTreeViewModel, this._mainWindow.ForceRecalculateWindowMeasures,
                SingleInstanceApplication._appSettings.FlatNodeListFilter, SingleInstanceApplication._appSettings.DemoModus ? "-DEMO-" : "", treeParameters);

            // Verbinden von Main-Window mit Main-ViewModel
            this._mainWindow.DataContext = mainWindowViewModel; //mainViewModel;

            // Start der UI
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("starting UI"));
            this._splashWindow.FinishAndClose();
            //this._mainWindow.Topmost = true; // nicht erforderlich
            this._mainWindow.Show();
            //this._mainWindow.Topmost = false; // nicht erforderlich
            //this._mainWindow.BringIntoView(); geht nicht
            //this._mainWindow.Focus(); geht hier nicht, muss aber nach Activate() kommen!
            this._mainWindow.Activate(); // geht!
            this._mainWindow.Focus();    // wichtig!
            if (SingleInstanceApplication._appSettings.Autostart)
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    int sleepCounter = 0;
                    while (this._mainWindow.IsRelocating && sleepCounter++ < 50)
                    {
                        Thread.Sleep(100);
                    }
                    Thread.Sleep(500); // Notwendig, das die Synchronisation selbst auch noch Zeit kostet.
                    SingleInstanceApplication._businessLogic.Tree.UserRun();
                });
            }
        }

        private void PrepareStart()
        {
            this.SetupLogging();

            // string executingAssemblyLocation = AppDomain.CurrentDomain.BaseDirectory;
            if (!this.checkCanRun())
            {
                throw new ApplicationException("Vishnu ist vorübergehend gesperrt worden."
                             + Environment.NewLine + " Bitte wenden Sie sich an die Administration.");
            }
            this.CopyJobsIfNecessary();
            if (String.IsNullOrEmpty(SingleInstanceApplication._appSettings?.MainJobName))
            {
                if (!SingleInstanceApplication._appSettings?.AppConfigUserLoaded == true)
                {
                    string userCfgDir = Path.GetDirectoryName(SingleInstanceApplication._appSettings?.AppConfigUser) ?? "";
                    if (!Directory.Exists(userCfgDir))
                    {
                        Directory.CreateDirectory(userCfgDir);
                    }
                    File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Vishnu.exe.config.user.default"),
                              Path.Combine(userCfgDir, "Vishnu.exe.config.user"), true);
                    SingleInstanceApplication._appSettings?.LoadSettings();
                }
                if (String.IsNullOrEmpty(SingleInstanceApplication._appSettings?.MainJobName))
                {
                    throw new ArgumentException("Es wurde keine Job-Definition angegeben.");
                }
            }
            this.CopyClickOnceStarter();
        }

        private void SetupLogging()
        {
            // Globales Logging installieren:
            // Beispiele - der Suchbegriff steht jeweils zwischen ":" und ")":
            //     string loggingRegexFilter = @"(?:loadChildren)";
            //     string loggingRegexFilter = @"(?:Waiting)";
            //     string loggingRegexFilter = ""; // Alles wird geloggt (ist der Default).
            //     string loggingRegexFilter = @"(?:_NOPPES_)"; // Nichts wird geloggt, bzw. nur Zeilen, die "_NOPPES_" enthalten.
            // analog: Statistics.RegexFilter.
            if (SingleInstanceApplication._appSettings == null)
            {
                throw new ApplicationException("Keine AppSettings vorhanden.");
            }
            SingleInstanceApplication._appSettings.AppEnvAccessor.UnregisterKey("DebugFile");
            string logFilePathName = SingleInstanceApplication._appSettings.ReplaceWildcards(SingleInstanceApplication._appSettings.DebugFile);
            SingleInstanceApplication._appSettings.AppEnvAccessor.RegisterKeyValue("DebugFile", logFilePathName);
            SingleInstanceApplication._logger = new Logger(logFilePathName, _appSettings.DebugFileRegexFilter, false);
            SingleInstanceApplication._logger.DebugArchivingInterval = SingleInstanceApplication._appSettings.DebugArchivingInterval;
            SingleInstanceApplication._logger.DebugArchiveMaxCount = SingleInstanceApplication._appSettings.DebugArchiveMaxCount;
            SingleInstanceApplication._logger.LoggingTriggerCounter = 15000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            InfoController.GetInfoSource().RegisterInfoReceiver(SingleInstanceApplication._logger, InfoTypes.Collection2InfoTypeArray(InfoTypes.All));
            Statistics.IsTimerTriggered = true; // LoggingTriggerCounter gibt die Anzahl Zählvorgänge vor, nach der die Ausgabe erfolgt.
            Statistics.LoggingTriggerCounter = 10000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            string statisticsFilePathName = SingleInstanceApplication._appSettings.ReplaceWildcards(SingleInstanceApplication._appSettings.StatisticsFile ?? "");

            Statistics.RegexFilter = SingleInstanceApplication._appSettings.StatisticsFileRegexFilter;
            SingleInstanceApplication._statisticsLogger = new Logger(statisticsFilePathName);
            SingleInstanceApplication._statisticsLogger.MaxBufferLineCount = 1; // Statistics puffert selber
            InfoController.GetInfoSource().RegisterInfoReceiver(SingleInstanceApplication._statisticsLogger, new InfoType[] { InfoType.Statistics });

            if (SingleInstanceApplication._appSettings.FatalInitializationException != null)
            {
                throw SingleInstanceApplication._appSettings.FatalInitializationException;
            }
        }

        private void CopyJobsIfNecessary()
        {
            try
            {
                //InfoController.GetInfoPublisher().Publish(this,
                //    String.Format($"IsClickOnce: {SingleInstanceApplication._appSettings.IsClickOnce}"), InfoType.NoRegex);
                string clickOnceDataDirectoryString = SingleInstanceApplication._appSettings?.ClickOnceDataDirectory == null ?
                    "null" : SingleInstanceApplication._appSettings.ClickOnceDataDirectory;
                //InfoController.GetInfoPublisher().Publish(this,
                //    String.Format($"ClickOnceDataDirectory: {clickOnceDataDirectoryString}"), InfoType.NoRegex);
                //InfoController.GetInfoPublisher().Publish(this,
                //    String.Format($"NewDeployment.xml: {File.Exists(Path.Combine(SingleInstanceApplication._appSettings.ApplicationRootPath, "NewDeployment.xml"))}")
                //    , InfoType.NoRegex);
                // MessageBox.Show(String.Format($"Vor ClickOnceAktionen auf {clickOnceDataDirectoryString}"));
                if (SingleInstanceApplication._appSettings?.IsClickOnce == true
                    && Directory.Exists(clickOnceDataDirectoryString)
                    && File.Exists(Path.Combine(SingleInstanceApplication._appSettings.ApplicationRootPath, "NewDeployment.xml")))
                {
                    this.DirectoryCopy(clickOnceDataDirectoryString,
                        SingleInstanceApplication._appSettings.ApplicationRootPath, true);
                    File.Delete(Path.Combine(SingleInstanceApplication._appSettings.ApplicationRootPath, "NewDeployment.xml"));
                    InfoController.GetInfoPublisher().Publish(this,
                        String.Format($"Jobs Copied and {Path.Combine(clickOnceDataDirectoryString, "NewDeployment.xml")} deleted.")
                        , InfoType.NoRegex);
                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Fehler bei CopyJobsIfNecessary: " + ex.Message);
                // throw;
            }
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private bool checkCanRun()
        {
            return true; //17.01.2017 Prüfung deaktiviert.
            /*
            SecureString canUnlock = new SecureString();
            canUnlock.AppendChar('D');
            canUnlock.AppendChar('M');
            canUnlock.AppendChar('e');
            canUnlock.AppendChar('u');
            canUnlock.AppendChar('b');
            canUnlock.AppendChar('r');
            canUnlock.AppendChar('u');
            canUnlock.AppendChar('c');
            canUnlock.AppendChar('g');
            canUnlock.AppendChar('s');

            string tmp1 = Marshal.PtrToStringUni(Marshal.SecureStringToBSTR(App._appSettings.XUnlock));
            return tmp1 == Marshal.PtrToStringUni(Marshal.SecureStringToBSTR(canUnlock));
            */
        }

        private void CopyClickOnceStarter()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
                sb.Append("\\");
                sb.Append(SingleInstanceApplication._appSettings?.VishnuProvider);
                sb.Append("\\");
                sb.Append("ClickOnceStarter.exe");
                string targetPath = sb.ToString();
                if (!File.Exists(targetPath))
                {
                    string? targetDir = Path.GetDirectoryName(targetPath);
                    if (targetDir != null && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                        Path.GetFileName(targetPath)),
                        targetPath, true);
                }
            }
            catch { }
        }

        void mainWindow_Closed(object? sender, EventArgs e)
        {
            if (!SingleInstanceApplication._cleanupDone)
            {
                SingleInstanceApplication.doVishnuCleanup();
            }
            //Application.Current.Shutdown(-1);            // die auskommentierten
            //Dispatcher.BeginInvoke((Action)delegate()    // Versuche
            //{                                            // funktionieren
            //  Application.Current.Shutdown();            // beide NICHT!
            //});
            //doProcessExit();
            Environment.Exit(-1); // TODO: erneut auf Notwendigkeit Testen
        }

        private static AppSettings? _appSettings;
        private static Logger? _logger;
        private static Logger? _statisticsLogger;
        private static LogicalTaskTree.LogicalTaskTree? _businessLogic;
        private SplashWindow? _splashWindow;
        private ViewerAsWrapper? _splashScreenMessageReceiver;
        private static bool _cleanupDone;

        private void ParameterReader_ParametersReloaded(object? sender, EventArgs e)
        {
            SingleInstanceApplication._businessLogic?.Tree.GetTopRootJobList()
                .ProcessTreeEvent("ParametersReloaded", SingleInstanceApplication._appSettings?.UserParameterReaderPath);
        }

        private void handleSplashScreenMessages(object? sender, InfoArgs msgArgs)
        {
            this._splashWindow?.ShowMessage(((SplashScreenMessage)msgArgs.MessageObject).Message);
        }

        // Wird in jedem Fall beim Beenden von Vishnu durchlaufen.
        // Versucht noch, Aufräumarbeiten auszuführen, endet aber u.U. abrupt
        // ohne fertig zu werden.
        private static void OnProcessExit(object? sender, EventArgs e)
        {
            if (!SingleInstanceApplication._cleanupDone)
            {
                SingleInstanceApplication.doVishnuCleanup();
            }
        }

        // Soll in jedem Fall beim Beenden von Vishnu durchlaufen werden.
        // Gibt die BusinessLogic und die AppSettings frei und löscht ggf.
        // das beim Start angelegte WorkingDirectory.
        private static void doVishnuCleanup()
        {
            try
            {
                try
                {
                    LogicalTaskTree.LogicalNode.ResumeTree(); // frees all waiting nodes
                    LogicalTaskTree.LogicalNode.AllowSnapshots(); // to avoid deadlocks.
                    SingleInstanceApplication._businessLogic?.Dispose();
                }
                catch { }
                if (SingleInstanceApplication._appSettings != null)
                {
                    if (!String.IsNullOrEmpty(SingleInstanceApplication._appSettings.ZipRelativeDummyDirectory)
                        && Directory.Exists(SingleInstanceApplication._appSettings.ZipRelativeDummyDirectory))
                    {
                        try
                        {
                            Directory.Delete(SingleInstanceApplication._appSettings.ZipRelativeDummyDirectory, true);
                        }
                        catch { }
                    }
                    try
                    {
                        SingleInstanceApplication._appSettings?.Dispose();
                    }
                    catch { }
                }
                try
                {
                    SingleInstanceApplication._logger?.Dispose();
                }
                catch { }
                Statistics.Stop();
                try
                {
                    SingleInstanceApplication._statisticsLogger?.Dispose();
                }
                catch { }
            }
            finally
            {
                SingleInstanceApplication._cleanupDone = true;
            }
        }
    }
}
