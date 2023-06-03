using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace NetEti.CustomControls
{
    /// <summary>
    /// Demo-Logik für SplashWindow
    /// (thanks to amr azab, http://www.codeproject.com/Articles/116875/WPF-Loading-Splash-Screen,
    /// and Nate Lowry, http://blog.dontpaniclabs.com/post/2013/11/14/Dynamic-Splash-Screens-in-WPF).
    /// </summary>
    public partial class SplashWindow : Window, INotifyPropertyChanged
    {

        /// <summary>
        /// Versionsstring.
        /// </summary>
        public string Version { get; set; }

        private Storyboard _showboard;
        private Storyboard _hideboard;
        private Storyboard _fadeoutboard;
        private string _lastMessage;
        private delegate void showDelegate(string txt);
        private showDelegate _showDelegate;
        private showDelegate _showVersionDelegate;
        private static ManualResetEvent? ResetSplashCreated;

        /// <summary>
        /// Erzeugt das SplashWindow, startet es und gibt eine Referenz darauf zurück.
        /// </summary>
        /// <returns>Referenz auf das SplashWindow.</returns>
        public static SplashWindow StartSplashWindow()
        {
            ResetSplashCreated = new ManualResetEvent(false);
            Thread SplashThread = new Thread(showSplash);
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.IsBackground = true;
            SplashThread.Name = "Splash Screen";
            SplashThread.Start();
            ResetSplashCreated.WaitOne();
            return _splashWindow ?? throw new NullReferenceException("_splashWindow konnte nicht instanziiert werden.");
        }

        /// <summary>
        /// Gibt eine Meldung im SplashWindow aus.
        /// </summary>
        /// <param name="message">Meldung, die im SplashWindow ausgegeben werden soll.</param>
        public void ShowMessage(string message)
        {
            if (!this._isClosingOrClosed)
            {
                this.Dispatcher.Invoke(_showDelegate, DispatcherPriority.ApplicationIdle, message);
            }
            // alt ohne if: Deadlock mit ParametersReloaded
        }

        /// <summary>
        /// Gibt die Programmversion im SplashWindow aus.
        /// </summary>
        /// <param name="version">Meldung, die im SplashWindow ausgegeben werden soll.</param>
        public void ShowVersion(string version)
        {
            if (!this._isClosingOrClosed)
            {
                this.Dispatcher.Invoke(_showVersionDelegate, DispatcherPriority.ApplicationIdle, version);
            }
        }

        /// <summary>
        /// Schließt das SplashWindow.
        /// </summary>
        public void FinishAndClose()
        {
            if (!this._isClosingOrClosed) // ohne diese Abfrage wurde eine Exception im Hauptprogramm
            {                             // ohne Ausgabe verschluckt (der Dispatcher.Invoke endete nicht).
                this._isClosingOrClosed = true;
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)delegate () { Close(); });
            }
        }

        private SplashWindow()
        {
            InitializeComponent();
            this._isClosingOrClosed = false;
            this._lastMessage = String.Empty;
            this._showDelegate = new showDelegate(this.showText);
            this._showVersionDelegate = new showDelegate(this.showVersion);
            this._showboard = (Storyboard)this.Resources["showStoryBoard"];
            this._hideboard = (Storyboard)this.Resources["HideStoryBoard"];
            this._fadeoutboard = (Storyboard)this.Resources["SplashWindowFadeOutStoryBoard"];
            this.Version = string.Empty;
        }

        private static SplashWindow? _splashWindow;
        private bool closeStoryBoardCompleted = false;
        private bool _isClosingOrClosed;

        /// <summary>
        /// EventHandler für INotifyPropertyChanged (löst bei Änderungen an öffentlichen Properties aus).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!closeStoryBoardCompleted)
            {
                BeginStoryboard(_fadeoutboard);
                e.Cancel = true;
            }
        }

        private void closeStoryBoard_Completed(object sender, EventArgs e)
        {
            closeStoryBoardCompleted = true;
            this.Close();
        }

        private static void showSplash()
        {
            _splashWindow = new SplashWindow();
            ResetSplashCreated?.Set();
            _splashWindow.ShowDialog();
        }

        private void showText(string txt)
        {
            if (!String.IsNullOrEmpty(this._lastMessage))
            {
                BeginStoryboard(_hideboard);
            }
            this._lastMessage = txt;
            txtLoading.Text = txt;
            BeginStoryboard(_showboard);
            //this.Topmost = false;
        }

        private void showVersion(string version)
        {
            this.Version = version;
            this.RaisePropertyChanged("Version");
            this.Topmost = false;
        }
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
