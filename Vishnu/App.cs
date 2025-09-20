using Vishnu.ViewModel;
using NetEti.ApplicationControl;
using NetEti.Globals;
using System;
using System.IO;
using NetEti.CustomControls;
using LogicalTaskTree.Provider;
using System.Reflection;
using System.Text;
using Microsoft.Win32; // wegen Registry-Zugriff
using Vishnu.Interchange;
using System.Security.Policy;
using System.Windows.Threading;
using System.Diagnostics;

namespace Vishnu
{
    /// <summary>
    /// Hauptklasse für den Start der WPF-Anwendung.
    /// Enthält die statische "Main"-Methode.
    /// App.cs ersetzt App.xaml und App.xaml.cs einer klassischen WPF-Anwendung.
    /// </summary>
    /// <remarks>
    ///
    /// 04.03.2013 Erik Nagel: erstellt.
    /// 14.12.2013 Erik Nagel: Erfolgreicher Memorycheck (memory.txt). 
    /// 14.07.2016 Erik Nagel: Der Pfad "DebugFile" kann jetzt in Vishnu.exe.config.user überschrieben werden. 
    /// 28.10.2017 Erik Nagel: Implementierung von IsSingleInstance, 
    ///            von App.xaml.cs in App.cs umbenannt und App.xaml rausgeschmissen.
    /// 23.06.2019 Erik Nagel: Aufräumen aus OnProcessExit in mainWindow_Closed und
    ///            AppDomain.CurrentDomain.UnhandledException gezogen, da OnProcessExit
    ///            u.U. nicht mehr ganz durchlaufen wird, sondern z.B. bei Close des MainWindows über "X"
    ///            rechts oben abrupt abbricht.
    /// 02.03.2024 Erik Nagel: Komplett Überarbeitet und auf Mutex umgestellt, da das Aktivieren einer schon
    ///            laufenden Vishnu.Instanz ab .Net 6.0 (eventuell schon früher) nicht mehr funktionierte.
    ///            Dabei die Referenzen auf Microsoft.VisualBasic wieder entfernt und künstliches Beenden
    ///            der Anwendung im Fehlerfall über Environment.Exit(-1) rausgeschmissen.
    ///            Außerdem wurde weitestgehend auf statische Routinen umgestellt.
    ///            Die Startsequenz wurde dabei erheblich gestrafft und deutlich übersichtlicher.
    /// </remarks>
    public class App : System.Windows.Application
    {
        #region variables

        private static App? _mainWpfApp;
        private static Mutex? _mutex;
        private static AppSettings? _appSettings;
        private static NetEti.ApplicationControl.Logger? _logger;
        private static NetEti.ApplicationControl.Logger? _statisticsLogger;
        private static LogicalTaskTree.LogicalTaskTree? _businessLogic;
        private static SplashWindow? _splashWindow;
        private static bool _cleanupStarted;
        private static WPF_UI.MainWindow? _mainWindow;
        private static ViewerAsWrapper? _splashScreenMessageReceiver;

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

        #endregion variables

        #region main

        /// <summary>
        /// Statischer Entry-Point - hier startet die Anwendung.
        /// </summary>
        /// <param name="args">Kommandozeilen-Argumente.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            App._splashWindow = SplashWindow.StartSplashWindow();

            InstallAppDomainExceptionHandling();
            App._appSettings = GenericSingletonProvider.GetInstance<AppSettings>()
                ?? throw new ApplicationException("AppSettings konnten nicht instanziiert werden.");
            bool isSingleInstance = GenericSingletonProvider.GetInstance<AppSettings>().SingleInstance;
            if (!isSingleInstance)
            {
                RunApplication(App._appSettings);
            }
            else
            {
                _mutex = new Mutex(true, "Vishnu {DC42D2AB-59ED-4098-B159-730FFC63E88C}");
                if (!_mutex.WaitOne(0, false))
                {
                    App._splashWindow?.FinishAndClose();
                    // MessageBox.Show("Die Anwendung läuft schon, sende Nachricht.");
                    // Special thanks to Matt Davis, https://stackoverflow.com/users/51170/matt-davis
                    // send our Win32 message to make the currently running instance
                    // jump on top of all the other windows
                    Messaging.PostMessage((IntPtr)Messaging.HWND_BROADCAST,
                        Messaging.WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    RunApplication(App._appSettings);
                }
            }
            if (App._appSettings.KillChildProcessesOnApplicationExit)
            {
                ProcessWorker.FinishChildProcesses(Process.GetCurrentProcess());
            }
            // MessageBox.Show("Vishnu beendet sich.");
            // An dieser Stelle wäre die Anwendung ohne zusätzliche Aktionen und auch ohne
            // irgendwelche Fehlermeldungen normal beendet.
            // Allerdings tritt in WPF im nachfolgenden AppDomainExit des Runtime-Systems sporadisch
            // eine Exception auf, die sich dort nicht mehr abfangen lässt, die Anwendung ist ja
            // eigentlich schon beendet:
            //   System.ComponentModel.Win32Exception (1816): Nicht genügend Quoten verfügbar,
            //   um diesen Befehl zu verarbeiten.
            //   at MS.Win32.ManagedWndProcTracker.HookUpDefWindowProc(IntPtr hwnd)
            //   at MS.Win32.ManagedWndProcTracker.OnAppDomainProcessExit()
            // Siehe auch: https://github.com/dotnet/roslyn/issues/9247
            // Trotz intensiver Recherche konnte ich bisher keinen Workaround, geschweige denn eine
            // Lösung finden.
            // Inzwischen habe ich für mich folgenden Workaround entwickelt:
            // WPF-AppDomain-Exit wird gar nicht erst durchlaufen, sondern in der Routine
            // "App.AssistedSuicide" eine externe Exe aufgerufen, welche diese Anwendung
            // hart beendet (killt). Ist zwar "dirty coding", funktioniert aber bisher einwandfrei.
            App.AssistedSuicide();
        }

        /// <summary>
        /// Dieses Event wird durch app.Run() getriggert.
        /// </summary>
        /// <param name="e">Eventparameter.</param>
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            // Das nachfolgende Setzen von OnExplicitShutdown oder OnMainWindowClose ist essenziell:
            // Wenn die aktuelle Bildschirmeinstellung über Strg-s gespeichert wird,
            // erscheint für kurze Zeit eine TimerMessageBox. Wenn sich diese schließt
            // und OnMainWindowClose nicht gesetzt wurde, haut es die ganze app runter.
            // Ich habe mich für OnExplicitShutdown entschieden, um hier klar die Kontrolle zu behalten.
            // Thanks again to Rachel Lim (https://rachel53461.wordpress.com).
            // https://stackoverflow.com/questions/7661315/wpf-closing-child-closes-parent-window

            this.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

            // 11.03.2024 Erik Nagel+- Test: Setzen des HandleDispatcherRequestProcessingFailure-Handlers
            this.Dispatcher.UnhandledException += HandleDispatcherRequestProcessingFailure;
        }

        private void HandleDispatcherRequestProcessingFailure(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Behandeln des Fehlers
            Console.WriteLine("Fehlermeldung: " + e.Exception.Message);
            e.Handled = true;

            // Optionale Aktion: Fehler dem Benutzer anzeigen
            MessageBox.Show("Ein Fehler ist aufgetreten. Die Anwendung wird fortgesetzt.",
               "Fehler", System.Windows.Forms.MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void RunApplication(AppSettings appSettings)
        {
            SetAddRemoveProgramsIcon();
            App._cleanupStarted = false;
            App._splashScreenMessageReceiver = new ViewerAsWrapper(App.handleSplashScreenMessages);
            InfoController.GetInfoSource().RegisterInfoReceiver(App._splashScreenMessageReceiver,
                typeof(SplashScreenMessage), new[] { InfoType.Info });
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("initialisiere das System"));

            App._splashWindow?.ShowVersion(appSettings.ProgrammVersion);
            if (appSettings.FatalInitializationException == null)
            {
                appSettings.UserParametersReloaded += ParameterReader_ParametersReloaded;
                appSettings.InitUserParameterReader();
            }
            App.PrepareStart(appSettings);
            if (appSettings.DemoModus)
            {
                App._splashWindow?.ShowVersion(appSettings.ProgrammVersion + " DEMO");
            }
            App.VishnuRun(appSettings);
        }

        // Die Business-Logic
        private static void VishnuRun(AppSettings appSettings)
        {
            // Die für den gesamten Tree gültigen Parameter
            TreeParameters treeParameters = 
                new TreeParameters(String.Format($"Tree {++LogicalTaskTree.LogicalTaskTree.TreeId}"));

            // Der Produktion-JobProvider mit extern über XML definierten Jobs:
            App._businessLogic = new LogicalTaskTree.LogicalTaskTree(treeParameters, new ProductionJobProvider());

            // Das Main-Window
            App._mainWindow = 
                new WPF_UI.MainWindow(appSettings.StartWithJobs,
                    appSettings.SizeOnVirtualScreen, appSettings.VishnuWindowAspects);
            App._mainWindow.Closed += MainWindow_Closed;

            // Das LogicalTaskTree-ViewModel
            LogicalTaskTreeViewModel logicalTaskTreeViewModel = new LogicalTaskTreeViewModel(
                App._businessLogic, App._mainWindow, appSettings.StartTreeOrientation,
                appSettings.FlatNodeListFilter, treeParameters);

            // Das Main-ViewModel
            MainWindowViewModel mainWindowViewModel
                = new MainWindowViewModel(logicalTaskTreeViewModel, App._mainWindow.ForceRecalculateWindowMeasures,
                appSettings.FlatNodeListFilter, appSettings.DemoModus ? "-DEMO-" : "", treeParameters);

            // Verbinden von Main-Window mit Main-ViewModel
            App._mainWindow.DataContext = mainWindowViewModel;

            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("starte UI"));
            //---------------------------------------------------------------------------------
            // Hier erfolgt der klassische WPF-Start.
            App._mainWpfApp = new(); // Übrigens auch notwendig für System.Windows.Application.Current.
            App._mainWpfApp.InitializeComponent(); // Übernimmt das MainWindow.
            App._mainWpfApp.MainWindow.Show();
            App._splashWindow?.FinishAndClose();
            App._mainWpfApp.MainWindow.Activate(); // Beide erforderlich, da sonst das MainWindow nach dem
            App._mainWpfApp.MainWindow.Focus(); // Schließen des Splash-Screens im Hintergrund verschwindet.
            App._mainWpfApp.AutostartIfSet(appSettings);
            App._mainWpfApp.Run();
            // Kurze Info: Hinter app.Run() wartet dieser Teil der Anwendung. Das funktioniert
            //             aber nur, wenn vor app.Run die Zuweisung von App._mainWindow auf
            //             die Instanzvariable app.MainWindow erfolgt war, ansonsten rasselt
            //             das Programm hier durch und beendet sich.
            //---------------------------------------------------------------------------------
        }

        /// <summary>
        /// InitializeComponent
        /// </summary>
        public void InitializeComponent()
        {
            // this.StartupUri = new System.Uri("MainWindow.xaml", System.UriKind.Relative);
            // this.StartupUri =
            //     new System.Uri("pack://application:,,,/Vishnu.WPF_UI;component/MainWindow.xaml",
            //                    System.UriKind.Absolute);
            this.MainWindow = App._mainWindow;
        }

        #endregion main

        #region helpers

        private static void MainWindow_Closed(object? sender, EventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            if (!App._cleanupStarted)
            {
                App.DoVishnuCleanup();
            }
            //// 17.03.2024 Erik Nagel: Test+
            //System.Windows.Application.Current.Dispatcher.ShutdownFinished += (s, a) =>
            //{
            //    MessageBox.Show("ShutdownFinished");
            //};
            //// 17.03.2024 Erik Nagel: Test-
            System.Windows.Application.Current.Dispatcher.InvokeShutdown();
        }

        private static void InstallAppDomainExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, System.UnhandledExceptionEventArgs args) =>
            {
                string exceptionString;
#if DEBUG
                exceptionString = ((Exception)args.ExceptionObject).ToString(); // Exception für später retten.
#else
                exceptionString = ((Exception)args.ExceptionObject).Message; // Exception für später retten.
#endif
                if (App._splashWindow != null)
                {
                    try
                    {
                        App._splashWindow.FinishAndClose();
                    }
                    catch { }
                }
                try
                {
                    App._logger?.Log(exceptionString);
                }
                catch { };
                if (!App._cleanupStarted)
                {
                    App.DoVishnuCleanup();
                }
                MessageBox.Show(exceptionString, "Exception",
                    System.Windows.Forms.MessageBoxButtons.OK, MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button2, MessageBoxOptions.DefaultDesktopOnly);
            };
        }

        private static void PrepareStart(AppSettings appSettings)
        {
            App.SetupLogging();

            if (!App.CheckCanRun())
            {
                throw new ApplicationException("Vishnu ist vorübergehend gesperrt worden."
                             + Environment.NewLine + " Bitte wenden Sie sich an die Administration.");
            }
            App.CopyJobsIfNecessary();
            if (!appSettings.AppConfigUserLoaded == true)
            {
                string userCfgDir = Path.GetDirectoryName(appSettings.AppConfigUser) ?? "";
                if (!Directory.Exists(userCfgDir))
                {
                    Directory.CreateDirectory(userCfgDir);
                }
                File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Vishnu.exe.config.user.default"),
                          Path.Combine(userCfgDir, "Vishnu.exe.config.user"), true);
                File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Vishnu.exe.config.user.default.json"),
                          Path.Combine(userCfgDir, "Vishnu.exe.config.user.json"), true);
                appSettings.LoadSettings();
            }
            if (String.IsNullOrEmpty(appSettings.MainJobName))
            {
                throw new ArgumentException("Es wurde keine Job-Definition angegeben.");
            }
            // 30.03.2024 Erik Nagel deaktiviert: App.CopyClickOnceStarter();
        }

        private static void SetupLogging()
        {
            // Globales Logging installieren:
            // Beispiele - der Suchbegriff steht jeweils zwischen ":" und ")":
            //     string loggingRegexFilter = @"(?:loadChildren)";
            //     string loggingRegexFilter = @"(?:Waiting)";
            //     string loggingRegexFilter = ""; // Alles wird geloggt (ist der Default).
            //     string loggingRegexFilter = @"(?:_NOPPES_)"; // Nichts wird geloggt, bzw. nur Zeilen, die "_NOPPES_" enthalten.
            // analog: Statistics.RegexFilter.
            if (App._appSettings == null)
            {
                throw new ApplicationException("Keine AppSettings vorhanden.");
            }
            App._appSettings.AppEnvAccessor.UnregisterKey("DebugFile");
            string logFilePathName = App._appSettings.ReplaceWildcards(App._appSettings.DebugFile);
            App._appSettings.AppEnvAccessor.RegisterKeyValue("DebugFile", logFilePathName);
            App._logger = new NetEti.ApplicationControl.Logger(logFilePathName, _appSettings.DebugFileRegexFilter, false);
            App._logger.DebugArchivingInterval = App._appSettings.DebugArchivingInterval;
            App._logger.DebugArchiveMaxCount = App._appSettings.DebugArchiveMaxCount;
            App._logger.LoggingTriggerCounter = 15000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            InfoController.GetInfoSource().RegisterInfoReceiver(App._logger, InfoTypes.Collection2InfoTypeArray(InfoTypes.All));
            Statistics.IsTimerTriggered = true; // LoggingTriggerCounter gibt die Anzahl Zählvorgänge vor, nach der die Ausgabe erfolgt.
            Statistics.LoggingTriggerCounter = 10000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            string statisticsFilePathName = App._appSettings.ReplaceWildcards(App._appSettings.StatisticsFile ?? "");

            Statistics.RegexFilter = App._appSettings.StatisticsFileRegexFilter;
            App._statisticsLogger = new NetEti.ApplicationControl.Logger(statisticsFilePathName);
            InfoController.GetInfoSource().RegisterInfoReceiver(App._statisticsLogger, new InfoType[] { InfoType.Statistics });

            if (App._appSettings.FatalInitializationException != null)
            {
                throw App._appSettings.FatalInitializationException;
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

        private static void CopyJobsIfNecessary()
        {
            try
            {
                //InfoController.GetInfoPublisher().Publish(this,
                //    String.Format($"IsClickOnce: {SingleInstanceApplication._appSettings.IsClickOnce}"), InfoType.NoRegex);
                string clickOnceDataDirectoryString = App._appSettings?.ClickOnceDataDirectory == null ?
                    "null" : App._appSettings.ClickOnceDataDirectory;
                //InfoController.GetInfoPublisher().Publish(this,
                //    String.Format($"ClickOnceDataDirectory: {clickOnceDataDirectoryString}"), InfoType.NoRegex);
                //InfoController.GetInfoPublisher().Publish(this,
                //    String.Format($"NewDeployment.xml: {File.Exists(Path.Combine(SingleInstanceApplication._appSettings.ApplicationRootPath, "NewDeployment.xml"))}")
                //    , InfoType.NoRegex);
                // MessageBox.Show(String.Format($"Vor ClickOnceAktionen auf {clickOnceDataDirectoryString}"));
                if (App._appSettings?.IsClickOnce == true
                    && Directory.Exists(clickOnceDataDirectoryString)
                    && File.Exists(Path.Combine(App._appSettings.ApplicationRootPath, "NewDeployment.xml")))
                {
                    Global.DirectoryCopy(clickOnceDataDirectoryString,
                        App._appSettings.ApplicationRootPath, true);
                    File.Delete(Path.Combine(App._appSettings.ApplicationRootPath, "NewDeployment.xml"));
                    InfoController.GetInfoPublisher().Publish(App._mainWindow,
                        String.Format($"Jobs Copied and {Path.Combine(clickOnceDataDirectoryString, "NewDeployment.xml")} deleted.")
                        , InfoType.NoRegex);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei CopyJobsIfNecessary: " + ex.Message);
                // throw;
            }
        }

        private static bool CheckCanRun()
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

        private static void CopyClickOnceStarter()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
                sb.Append("\\");
                sb.Append(App._appSettings?.VishnuProvider);
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
                    File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "",
                        Path.GetFileName(targetPath)),
                        targetPath, true);
                }
            }
            catch { }
        }

        private void AutostartIfSet(AppSettings appSettings)
        {
            if (appSettings.Autostart)
            {
                Task task = Task.Factory.StartNew(() =>
                {
                    int sleepCounter = 0;
                    while ((App._mainWindow?.IsRelocating == true) && sleepCounter++ < 50)
                    {
                        Thread.Sleep(100);
                    }
                    Thread.Sleep(500); // Notwendig, das die Synchronisation selbst auch noch Zeit kostet.
                    App._businessLogic?.Tree.UserRun();
                });
            }
        }

        private static void ParameterReader_ParametersReloaded(object? sender, EventArgs e)
        {
            App._businessLogic?.Tree.GetTopRootJobList()
                .ProcessTreeEvent("ParametersReloaded", App._appSettings?.UserParameterReaderPath);
        }

        private static void handleSplashScreenMessages(object? sender, InfoArgs msgArgs)
        {
            App._splashWindow?.ShowMessage(((SplashScreenMessage)msgArgs.MessageObject).Message);
        }

        // Soll in jedem Fall beim Beenden von Vishnu durchlaufen werden.
        // Gibt die BusinessLogic und die AppSettings frei und löscht ggf.
        // das beim Start angelegte WorkingDirectory.
        private static void DoVishnuCleanup()
        {
            App._cleanupStarted = true;
            Statistics.Stop();
            App._logger?.Stop();
            try
            {
                LogicalTaskTree.LogicalNode.ResumeTree(); // frees all waiting nodes
                LogicalTaskTree.LogicalNode.AllowSnapshots(); // to avoid deadlocks.
                App._businessLogic?.Dispose();
            }
            catch { }
            if (App._appSettings != null)
            {
                if (!String.IsNullOrEmpty(App._appSettings.ZipRelativeDummyDirectory)
                    && Directory.Exists(App._appSettings.ZipRelativeDummyDirectory))
                {
                    try
                    {
                        Directory.Delete(App._appSettings.ZipRelativeDummyDirectory, true);
                    }
                    catch { }
                }
                try
                {
                    App._appSettings?.Dispose();
                }
                catch { }
            }
            App._statisticsLogger = null;
            try
            {
                App._logger?.Dispose();
            }
            catch { }
        }

        private static void AssistedSuicide()
        {
            Process externalProcess = new Process();
            externalProcess.StartInfo.FileName = "ProcessTerminator.exe";
            externalProcess.StartInfo.Arguments = Process.GetCurrentProcess().Id.ToString() + " 1000";
            externalProcess.Start();
            externalProcess.WaitForExit();
        }

        #endregion helpers

    }
}
