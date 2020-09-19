using System.Windows;
using Vishnu.ViewModel;
using NetEti.ApplicationControl;
using NetEti.Globals;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Vishnu.Interchange;
using NetEti.CustomControls;
using LogicalTaskTree.Provider;
using System.Reflection;

namespace Vishnu
{
  /// <summary>
  /// Interaktionslogik für "App.xaml"
  /// </summary>
  /// <remarks>
  /// File: App.xaml.cs
  /// Autor: Erik Nagel
  ///
  /// 04.03.2013 Erik Nagel: erstellt
  /// 14.12.2013 Erik Nagel: Erfolgreicher Memorycheck (memory.txt). 
  /// 14.07.2016 Erik Nagel: Der Pfad "DebugFile" kann jetzt in Vishnu.exe.config.user
  /// überschrieben werden. 
  /// </remarks>
  public partial class App : Application
  {
    /// <summary>
    /// Wird beim Start der Anwendung durchlaufen.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      this._splashWindow = SplashWindow.StartSplashWindow();
      this._splashScreenMessageReceiver = new ViewerAsWraper(this.handleSplashScreenMessages);
      InfoController.GetInfoSource().RegisterInfoReceiver(this._splashScreenMessageReceiver, typeof(SplashScreenMessage), new[] { InfoType.Info });
      InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("initializing system"));

      AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs args) =>
      {
        string exceptionString;
#if DEBUG
        exceptionString = (args.ExceptionObject as Exception).ToString(); // Exception für später retten.
#else
        exceptionString = (args.ExceptionObject as Exception).Message; // Exception für später retten.
#endif
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

			//MessageBox.Show(String.Format("WorkingDirectory: {0}", System.IO.Directory.GetCurrentDirectory()));

      App._appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
      // App._appSettings.SetRegistryBasePath(@"HKEY_LOCAL_MACHINE\SOFTWARE\Vishnu\");
      this._splashWindow.ShowVersion(App._appSettings.ProgrammVersion);
      if (!String.IsNullOrEmpty(App._appSettings.UserParameterReaderPath))
      {
        loadUserParameterReader(App._appSettings.UserParameterReaderPath);
      }
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
      
      // Der Produktions-JobProvider mit extern über XML definierten Jobs:
      App._businessLogic = new LogicalTaskTree.LogicalTaskTree(new TreeParameters("Tree 1", null), new ProductionJobProvider());

      // Das Main-Window
      WPF_UI.MainWindow window = new WPF_UI.MainWindow();
      window.SizeOnVirtualScreen = _appSettings.SizeOnVirtualScreen; // Übergabe des Applikationsparameters für die Skalierung
                                                                     // bei mehreren Bildschirmen.
      window.Closed += mainWindow_Closed;
      // Das LogicalTaskTree-ViewModel
      LogicalTaskTreeViewModel logicalTaskTreeViewModel = new LogicalTaskTreeViewModel(App._businessLogic, window, App._appSettings.StartTreeOrientation, App._appSettings.FlatNodeListFilter);

      // Das Main-ViewModel
      MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(logicalTaskTreeViewModel, window.ForceRecalculateWindowMeasures, App._appSettings.FlatNodeListFilter, App._appSettings.DemoModus ? "-DEMO-" : "");

      // Verbinden von Main-Window mit Main-ViewModel
      window.DataContext = mainWindowViewModel; //mainViewModel;

      // Start der UI
      InfoController.GetInfoPublisher().Publish(new SplashScreenMessage("starting UI"));
      this._splashWindow.FinishAndClose();
      window.Topmost = true; // notwendig, sonst startet das MainWindow im Hintergrund
      window.Show();
      window.Topmost = false;
      //window.BringIntoView(); geht nicht
      //window.Focus(); geht nicht
      window.Activate(); // geht!
      if (App._appSettings.Autostart)
      {
        App._businessLogic.Tree.UserRun();
      }
    }

    private void PrepareStart()
    {
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
      if (App._appSettings.MainJobName == null)
      {
        if (!App._appSettings.AppConfigUserLoaded)
        {
          string userCfgDir = Path.GetDirectoryName(App._appSettings.AppConfigUser);
          if (!Directory.Exists(userCfgDir))
          {
            Directory.CreateDirectory(userCfgDir);
          }
          File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Vishnu.exe.config.user.example"),
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
      InfoController.Say("#AY1# App.xaml.cs.mainWindow_Closed");
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
    private static LogicalTaskTree.LogicalTaskTree _businessLogic;
    private SplashWindow _splashWindow;
    private ViewerAsWraper _splashScreenMessageReceiver;

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
      App._appSettings.RegisterStringValueGetter(new ParameterReaderToIGetStringValue(parameterReader));
    }

    private void handleSplashScreenMessages(object sender, InfoArgs msgArgs)
    {
      this._splashWindow.showMessage((msgArgs.MessageObject as SplashScreenMessage).Message);
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
      InfoController.Say("#AY1# App.xaml.cs.OnProcessExit");
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
      /// Konstruktor - übernimmt eine IParameterReader-Instanz.
      /// </summary>
      /// <param name="parameterReader">Zu kapselnde IParameterReader-Instanz.</param>
      public ParameterReaderToIGetStringValue(IParameterReader parameterReader)
      {
        this._parameterReader = parameterReader;
      }

      private IParameterReader _parameterReader;
    }
  }
}
