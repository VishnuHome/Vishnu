using NetEti.CustomControls;
using System;
using System.Windows;
using Vishnu.ViewModel;
using System.IO;
using Vishnu.Interchange;
using LogicalTaskTree.Provider;
using System.Reflection;
using NetEti.ApplicationControl;
using NetEti.Globals;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Collections.Generic;

namespace VishnuWeb
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        // Das Main-Window
        private MainPage _mainWindow;

        /// <summary>
        /// Wird beim Start der Anwendung durchlaufen.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            this._splashWindow = SplashWindow.StartSplashWindow();
            this._splashScreenMessageReceiver = new ViewerAsWrapper(this.handleSplashScreenMessages);
            InfoController.GetInfoSource().RegisterInfoReceiver(this._splashScreenMessageReceiver, typeof(SplashScreenMessage), new[] { InfoType.Info });
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("initializing system"));

            AppDomain.CurrentDomain.UnhandledException += (object sender, System.UnhandledExceptionEventArgs args) =>
            {
                string exceptionString;
#if DEBUG
                exceptionString = (args.ExceptionObject as Exception).ToString(); // Exception für später retten.
#else
                exceptionString = (args.ExceptionObject as Exception).Message; // Exception für später retten.
#endif
                /* TEST 15.03.2020+ deaktiviert
                if (App._appSettings != null && App._appSettings.DumpAppSettings)
                {
                    SortedDictionary<string, string> allParameters = App._appSettings.GetParametersSources();
                    foreach (string parameter in allParameters.Keys)
                    {
                        InfoController.GetInfoPublisher().Publish(null, parameter + ": " + allParameters[parameter], InfoType.NoRegex);
                    }
                }
                TEST 15.03.2020- */ 

                if (this._splashWindow != null)
                {
                    try
                    {
                        this._splashWindow.FinishAndClose();
                    }
                    catch { }
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

            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
            //MessageBox.Show(String.Format("WorkingDirectory: {0}", System.IO.Directory.GetCurrentDirectory()));
            App._appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            // App._appSettings.SetRegistryBasePath(@"HKEY_LOCAL_MACHINE\SOFTWARE\Vishnu\");
            this._splashWindow.ShowVersion(App._appSettings.ProgrammVersion);
            /* TEST 15.03.2019+ deaktiviert!
                if (!String.IsNullOrEmpty(App._appSettings.UserParameterReaderPath))
                {
                    loadUserParameterReader(App._appSettings.UserParameterReaderPath);
                }
                TEST 15.03.2019- */
            this.PrepareStart();
            if (App._appSettings.DemoModus)
            {
                this._splashWindow.ShowVersion(App._appSettings.ProgrammVersion + "  DEMO-MODUS");
            }

            // Globales Logging installieren:
            // Beispiele - der Suchbegriff steht jeweils zwischen ":" und ")":
            //     string loggingRegexFilter = @"(?:loadChildren)";
            //     string loggingRegexFilter = @"(?:Waiting)";
            //     string loggingRegexFilter = ""; // Alles wird geloggt (ist der Default).
            //     string loggingRegexFilter = @"(?:_NOPPES_)"; // Nichts wird geloggt, bzw. nur Zeilen, die "_NOPPES_" enthalten.
            // analog: Statistics.RegexFilter.
            App._appSettings.AppEnvAccessor.UnregisterKey("DebugFile");
            string logFilePathName = App._appSettings.ReplaceWildcards(App._appSettings.DebugFile);
            App._appSettings.AppEnvAccessor.RegisterKeyValue("DebugFile", logFilePathName);
            App._logger = new Logger(logFilePathName, _appSettings.DebugFileRegexFilter, false);
            InfoController.GetInfoSource().RegisterInfoReceiver(App._logger, InfoTypes.Collection2InfoTypeArray(InfoTypes.All));
            Statistics.IsTimerTriggered = true; // LoggingTriggerCounter gibt die Anzahl Zählvorgänge vor, nach der die Ausgabe erfolgt.
            Statistics.LoggingTriggerCounter = 10000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            string statisticsFilePathName = App._appSettings.ReplaceWildcards(App._appSettings.StatisticsFile);

            Statistics.RegexFilter = App._appSettings.StatisticsFileRegexFilter;
            InfoController.GetInfoSource().RegisterInfoReceiver(new Logger(statisticsFilePathName), new InfoType[] { InfoType.Statistics });

            // Die Business-Logic
            // Demo: hart verdrahteter JobProvider:
            // App._businessLogic = new LogicalTaskTree.LogicalTaskTree(new TreeParameters("Tree 1", null), new MixedTestJobProvider());

            // Die für den gesamten Tree gültigen Parameter
            TreeParameters treeParameters = new TreeParameters(String.Format($"Tree {++LogicalTaskTree.LogicalTaskTree.TreeId}"), null);

            // Der Produktions-JobProvider mit extern über XML definierten Jobs:
            try
            {
                App._businessLogic = new LogicalTaskTree.LogicalTaskTree(treeParameters, new ProductionJobProvider());
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(String.Format(
                "Fehler beim Laden von Vishnu: {0}.\r\nDie Anwendung wird geschlossen.", ex.Message));
                throw;
            }

            // Das Main-Window
            this._mainWindow = new MainPage();

            this._mainWindow.SizeOnVirtualScreen = _appSettings.SizeOnVirtualScreen; // Übergabe des Applikationsparameters für die Skalierung
                                                                                     // bei mehreren Bildschirmen.
            this._mainWindow.MainWindowStartAspects = _appSettings.VishnuWindowAspects; // Vorher gespeicherte Darstellungseigenschaften des
                                                                                        // Vishnu-MainWindow oder Defaults.
            this._mainWindow.Unloaded += mainWindow_Closed;
            //this._mainWindow.Closed += mainWindow_Closed;

            // Das LogicalTaskTree-ViewModel
            LogicalTaskTreeViewModel logicalTaskTreeViewModel = new LogicalTaskTreeViewModel(App._businessLogic, this._mainWindow, App._appSettings.StartTreeOrientation, App._appSettings.FlatNodeListFilter, treeParameters);

            // Das Main-ViewModel
            MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(logicalTaskTreeViewModel, this._mainWindow.ForceRecalculateWindowMeasures, App._appSettings.FlatNodeListFilter, App._appSettings.DemoModus ? "-DEMO-" : "", treeParameters);

            // Verbinden von Main-Window mit Main-ViewModel
            this._mainWindow.DataContext = mainWindowViewModel; //mainViewModel;

            // Start der UI
            InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("starting UI"));
            this._splashWindow.FinishAndClose();
            //this._mainWindow.Topmost = true; // nicht erforderlich

            //            this._mainWindow.Show();
            this.ShowPage(this._mainWindow);

            //this.StartupUri = new System.Uri(@"/WPF_UI;component/MainPage.xaml", System.UriKind.Relative);
            //this.Startup .StartupUri = new System.Uri(@"/MainPage.xaml", System.UriKind.Relative);

            if (App._appSettings.Autostart)
            {
                App._businessLogic.Tree.UserRun();
            }
        }

        private void ShowPage(Page page)
        {
            NavigationWindow mainWindow = Application.Current.MainWindow as NavigationWindow;
            mainWindow.Navigate(page);
        }

        private void PrepareStart()
        {
            this.SetupLogging();

            if (!this.checkCanRun())
            {
                throw new ApplicationException("Vishnu ist vorübergehend gesperrt worden."
                             + Environment.NewLine + " Bitte wenden Sie sich an die Administration.");
            }
            if (!Directory.Exists(App._appSettings.WorkingDirectory))
            {
                Directory.CreateDirectory(App._appSettings.WorkingDirectory);
                App._appSettings.WorkingDirectoryCreated = true;
            }
            this.CopyJobsIfNecessary();
            if (App._appSettings.MainJobName == null)
            {
                if (!App._appSettings.AppConfigUserLoaded)
                {
                    string userCfgDir = Path.GetDirectoryName(App._appSettings.AppConfigUser);
                    if (!Directory.Exists(userCfgDir))
                    {
                        Directory.CreateDirectory(userCfgDir);
                    }
                    File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Vishnu.exe.config.user.default"),
                              Path.Combine(userCfgDir, "Vishnu.exe.config.user"), true);
                    App._appSettings.LoadSettings();
                }
                if (App._appSettings.MainJobName == null)
                {
                    string msg = "Es wurde keine Job-Definition angegeben.";
                    throw new ArgumentException(msg);
                }
            }
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
            App._appSettings.AppEnvAccessor.UnregisterKey("DebugFile");
            string logFilePathName = App._appSettings.ReplaceWildcards(App._appSettings.DebugFile);
            App._appSettings.AppEnvAccessor.RegisterKeyValue("DebugFile", logFilePathName);
            App._logger = new Logger(logFilePathName, _appSettings.DebugFileRegexFilter, false);
            App._logger.DebugArchivingInterval = App._appSettings.DebugArchivingInterval;
            App._logger.DebugArchiveMaxCount = App._appSettings.DebugArchiveMaxCount;
            App._logger.LoggingTriggerCounter = 120000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            InfoController.GetInfoSource().RegisterInfoReceiver(App._logger, InfoTypes.Collection2InfoTypeArray(InfoTypes.All));
            Statistics.IsTimerTriggered = true; // LoggingTriggerCounter gibt die Anzahl Zählvorgänge vor, nach der die Ausgabe erfolgt.
            Statistics.LoggingTriggerCounter = 10000; // Default ist 5000 Zählvorgänge oder Millisekunden.
            string statisticsFilePathName = App._appSettings.ReplaceWildcards(App._appSettings.StatisticsFile);

            Statistics.RegexFilter = App._appSettings.StatisticsFileRegexFilter;
            App._statisticsLogger = new Logger(statisticsFilePathName);
            App._statisticsLogger.MaxBufferLineCount = 1; // Statistics puffert selber
            InfoController.GetInfoSource().RegisterInfoReceiver(App._statisticsLogger, new InfoType[] { InfoType.Statistics });

            if (App._appSettings.FatalInitializationException != null)
            {
                throw App._appSettings.FatalInitializationException;
            }
        }

        private void CopyJobsIfNecessary()
        {
            try
            {
                this.Publish(String.Format($"IsClickOnce: {App._appSettings.IsClickOnce}"));
                string clickOnceDataDirectoryString = App._appSettings.ClickOnceDataDirectory == null ?
                    "null" : App._appSettings.ClickOnceDataDirectory;
                this.Publish(String.Format($"ClickOnceDataDirectory: {clickOnceDataDirectoryString}"));
                this.Publish(String.Format($"NewDeployment.xml: {File.Exists(Path.Combine(App._appSettings.ApplicationRootPath, "NewDeployment.xml"))}"));
                if (App._appSettings.IsClickOnce
                    && Directory.Exists(clickOnceDataDirectoryString)
                    && File.Exists(Path.Combine(App._appSettings.ApplicationRootPath, "NewDeployment.xml")))
                {
                    this.DirectoryCopy(clickOnceDataDirectoryString,
                        App._appSettings.ApplicationRootPath, true);
                    File.Delete(Path.Combine(App._appSettings.ApplicationRootPath, "NewDeployment.xml"));
                    this.Publish(String.Format($"Jobs Copied and {Path.Combine(clickOnceDataDirectoryString, "NewDeployment.xml")} deleted."));
                }

            }
            catch (Exception ex)
            {
                this.Publish("Fehler bei CopyJobsIfNecessary: " + ex.Message);
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

        private void Publish(string message)
        {
            // InfoController.GetInfoPublisher().Publish(this, message, , InfoType.NoRegex);
            // MessageBox.Show(message);
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

        void mainWindow_Closed(object sender, EventArgs e)
        {
            if (App._appSettings != null && App._appSettings.DumpAppSettings)
            {
                SortedDictionary<string, string> allParameters = App._appSettings.GetParametersSources();
                foreach (string parameter in allParameters.Keys)
                {
                    InfoController.GetInfoPublisher().Publish(null, parameter + ": " + allParameters[parameter], InfoType.NoRegex);
                }
            }

            //Application.Current.Shutdown(-1);            // die auskommentierten
            //Dispatcher.BeginInvoke((Action)delegate()    // Versuche
            //{                                            // funktionieren
            //  Application.Current.Shutdown();            // beide NICHT!
            //});
            doProcessExit();
            Environment.Exit(-1);
        }

        private static AppSettings _appSettings;
        private static Logger _logger;
        private static Logger _statisticsLogger;
        private static LogicalTaskTree.LogicalTaskTree _businessLogic;
        private SplashWindow _splashWindow;
        private ViewerAsWrapper _splashScreenMessageReceiver;

        /* TEST 15.03.2019+ deaktiviert!
        // Lädt dynamisch eine optionale zusätzliche Dll, die eine
        // IParameterReader-Instanz zur Verfügung stellt.
        // Der zusätzliche ParameterReader wird an den Anfang der
        // Auswertungskette eingefügt, so dass er höchste Priorität
        // bekommt.
        private void loadUserParameterReader(string userParameterReaderPath)
        {
            IParameterReader parameterReader = (IParameterReader)VishnuAssemblyLoader.GetAssemblyLoader()
              .DynamicLoadObjectOfTypeFromAssembly(userParameterReaderPath, typeof(IParameterReader));
            if (parameterReader == null)
            {
                throw new ApplicationException(String.Format("Die User-Assembly {0} konnte nicht geladen werden.", userParameterReaderPath));
            }
            App._appSettings.RegisterUserStringValueGetter(new ParameterReaderToIGetStringValue(parameterReader, userParameterReaderPath));
        }
        TEST 15.03.2019- */

        private void handleSplashScreenMessages(object sender, InfoArgs msgArgs)
        {
            this._splashWindow.ShowMessage((msgArgs.MessageObject as SplashScreenMessage).Message);
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            doProcessExit();
        }

        // Wird in jedem Fall beim Beenden von Vishnu durchlaufen.
        // Gibt die Instanz der BusinessLogic frei und löscht ggf.
        // das beim Start angelegte WorkingDirectory.
        private static void doProcessExit()
        {
            if (App._businessLogic != null)
            {
                App._businessLogic.Tree.UserBreak();
                App._businessLogic.Dispose();
            }
            if (App._logger != null)
            {
                App._logger.Dispose();
            }
            if (App._appSettings != null && App._appSettings.KillWorkingDirectoryAtShutdown
                && App._appSettings.WorkingDirectoryCreated)
            {
                try
                {
                    Directory.Delete(App._appSettings.WorkingDirectory, true);
                }
                catch { }
            }
            if (App._appSettings != null && !String.IsNullOrEmpty(App._appSettings.ZipRelativeDummyDirectory)
              && Directory.Exists(App._appSettings.ZipRelativeDummyDirectory))
            {
                try
                {
                    Directory.Delete(App._appSettings.ZipRelativeDummyDirectory, true);
                }
                catch { }
            }
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
            public string GetStringValue(string key, string defaultValue)
            {
                string result = this._parameterReader.ReadParameter(key);
                return result ?? defaultValue;
            }

            /// <summary>
            /// Liefert ein string-Array zu einem Key. Wenn es keinen Wert zu dem
            /// Key gibt, wird defaultValue zurückgegeben.
            /// </summary>
            /// <param name="key">Der Zugriffsschlüssel (string)</param>
            /// <param name="defaultValues">Das default-Ergebnis (string[])</param>
            /// <returns>Das Ergebnis-String-Array</returns>
            public string[] GetStringValues(string key, string[] defaultValues)
            {
                throw new NotImplementedException();
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

            /// <summary>
            /// Konstruktor - übernimmt eine IParameterReader-Instanz.
            /// </summary>
            /// <param name="parameterReader">Zu kapselnde IParameterReader-Instanz.</param>
            public ParameterReaderToIGetStringValue(IParameterReader parameterReader, string pathToReader)
            {
                this._parameterReader = parameterReader;
                this._pathToReader = pathToReader;
            }

            private IParameterReader _parameterReader;
            private string _pathToReader;
        }
    }
}
