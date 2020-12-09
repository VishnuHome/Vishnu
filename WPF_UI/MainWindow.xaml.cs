using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using NetEti.MultiScreen;
using Vishnu.ViewModel;
using System.Windows.Input;
using Vishnu.Interchange;
using NetEti.CustomControls;
using System.Windows.Media;
using System.Collections.Generic;

namespace Vishnu.WPF_UI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region public members

        /// <summary>
        /// Ruft die lokale Routine "SaveWindowAspectsAndCallViewModelLogic" auf.
        /// Diese speichert die aktuellen Window-Darstellungsparameter in einer Instanz von "WindowAspects"
        /// und gibt "WindowAspects" dan als Aufrufparameter an die ViewModel-Routine "SaveTreeState" weiter,
        /// welche ihrerseits die WindowAspects zusammen mit Tree-relevanten Parametern abspeichert.
        /// </summary>
        public static RoutedUICommand SaveWindowAspectsAndCallViewModelLogicCommand =
        new RoutedUICommand("SaveWindowAspectsAndCallViewModelLogicCommand",
            "SaveWindowAspectsAndCallViewModelLogicCommand", typeof(MainWindow));

        /// <summary>
        /// Bei true werden mehrere Bildschirme als ein einziger
        /// großer Bildschirm behandelt, ansonsten zählt für
        /// Größen- und Positionsänderungen der Bildschirm, auf dem
        /// sich das MainWindow hauptsächlich befindet (ActualScreen).
        /// Default: false; muss von außen nach Instanziierung gesetzt
        /// werden.
        /// </summary>
        public bool SizeOnVirtualScreen { get; set; }

        /// <summary>
        /// Ganz links auf dem aktuellen Screen.
        /// </summary>
        public double MinLeft { get; set; }

        /// <summary>
        /// Ganz oben auf dem aktuellen Screen.
        /// </summary>
        public double MinTop { get; set; }

        /// <summary>
        /// Wesentlichen Darstellungsmerkmale des Vishnu-MainWindows.
        /// Werden beim Start der Anwendung aus den AppSettings gefüllt.
        /// </summary>
        public WindowAspects MainWindowStartAspects
        {
            get
            {
                return this._mainWindowStartAspects;
            }
            set
            {
                if (this._mainWindowStartAspects != value)
                {
                    this._mainWindowStartAspects = value;
                    this.MainTabControl.SelectedIndex = this._mainWindowStartAspects.ActTabControlTab;
                }
            }
        }

        /// <summary>
        /// Konstruktor des Haupt-Fensters.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            this.MinLeft = 0;
            this.MinTop = 0;
            this._contentRendered = false;
            this._handleResizeEvents = true;
            this._tabManualSize = new bool[this.MainTabControl.Items.Count];
            this._lastWindowMeasures = new Rect[this.MainTabControl.Items.Count];
            // Registrieren des the Bubble Event Handlers 
            //      this.AddHandler(CustomControls.ZoomTreeView.TreeElementStateChangedEvent,
            // new RoutedEventHandler(TreeElementStateChangedEventHandler));
            //IObservable<SizeChangedEventArgs> ObservableSizeChanges = Observable
            //    .FromEventPattern<SizeChangedEventArgs>(this, "sizeHasChanged")
            //    .Select(x => x.EventArgs)
            //    .Throttle(TimeSpan.FromMilliseconds(200));

            //IDisposable SizeChangedSubscription = ObservableSizeChanges
            //    .ObserveOn(SynchronizationContext.Current)
            //    .Subscribe(x =>
            //    {
            //      sizeHasChanged(x);
            //    });
            // Lösung: absichtlich über DispatcherTimer mit niedriger Priorität (DispatcherPriority.Input). Dadurch soll verhindert werden,
            //         dass der Timer feuert, bevor ein komplexer Grafik-Aufbau abgeschlossen ist.
            this._sizeChangedTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Input, this.sizeHasChanged, this.Dispatcher);
            this._sizeChangedTimer.Stop();
            // 14.10.2015 Erik Nagel+ wieder aktiviert. TEST: 11.10.2015 Erik Nagel+ deaktiviert.
            this._screenRefreshTimerInterval = 2000;
            this._forceScreenRefreshTimer = new System.Timers.Timer();
            this._forceScreenRefreshTimer.Interval = this._screenRefreshTimerInterval;
            // Zwingt die UI alle 2 Sekunden für 2 Millisekunden in eine Größenänderung um ein Pixel und einen Screen-Refresh.
            // Korrigiert den Umstand, dass nach längerer Betriebsdauer Teile des Fensters
            // schwarz bleiben und von selbst auch nicht mehr neu gezeichnet werden (WPF-Bug?).
            this._forceScreenRefreshTimer.Elapsed
              += new System.Timers.ElapsedEventHandler(new Action<object, System.Timers.ElapsedEventArgs>((a, b)
                =>
              {
                  this.refreshByTemporarilyChangingSize();
                  this._forceScreenRefreshTimer.Interval = this._screenRefreshTimerInterval;
              }));
            this._forceScreenRefreshTimer.Enabled = true;
        }

        /// <summary>
        /// Setzt die Fenstergröße unter Berücksichtigung von Maximalgrenzen auf die
        /// Höhe und Breite des Inhalts und die Property SizeToContent auf WidthAndHeight.
        /// Zentriert das Window dann neu relativ zur letzten Position.
        /// </summary>
        public void ForceRecalculateWindowMeasures(object parameter)
        {
            this.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            this.RecalculateWindowMeasures();
        }

        /// <summary>
        /// Setzt die Fenstergröße unter Berücksichtigung von Maximalgrenzen auf die
        /// Höhe und Breite des Inhalts und die Property SizeToContent auf WidthAndHeight.
        /// Zentriert das Window dann neu relativ zur letzten Position.
        /// </summary>
        public void RecalculateWindowMeasures()
        {
            if (this.SizeOnVirtualScreen)
            {
                this.MaxHeight = System.Windows.SystemParameters.VirtualScreenHeight;
                this.MaxWidth = System.Windows.SystemParameters.VirtualScreenWidth;
                this.MinLeft = 0;
                this.MinTop = 0;
            }
            else
            {
                ScreenInfo actScreenInfo = ScreenInfo.GetActualScreenInfo(this);
                this.MaxHeight = actScreenInfo.WorkingArea.Height;
                this.MaxWidth = actScreenInfo.WorkingArea.Width;
                this.MinLeft = actScreenInfo.WorkingArea.Left;
                this.MinTop = actScreenInfo.WorkingArea.Top;
            }
            //InfoController.Say(String.Format("MaxWidth: {0}, MaxHeight: {1}, Width: {2}, Height: {3}, Left: {4}, Top: {5}",
            //  this.MaxWidth, this.MaxHeight, this.ActualWidth, this.ActualHeight, this.Left, this.Top));
            if (this.WindowState == WindowState.Normal && this._contentRendered)
            {
                double effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                //InfoController.Say(String.Format("MaxWidth: {0}, MaxHeight: {1}, Width: {2}, Height: {3}, Left: {4}, Top: {5}",
                //  this.MaxWidth, this.MaxHeight, this.ActualWidth, effectiveHeight, this.Left, this.Top));
                if (this.SizeToContent == System.Windows.SizeToContent.WidthAndHeight)
                {
                    double widthDiff = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width - this.ActualWidth;
                    double heightDiff = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height - effectiveHeight;
                    double newLeft = this.Left + (widthDiff / 2.0);
                    double newTop = this.Top + (heightDiff / 2.0);
                    bool bottomBorderViolated = false;
                    bool rightBorderViolated = false;
                    if (this.Top + this.ActualHeight > this.MaxHeight)
                    {
                        bottomBorderViolated = true;
                    }
                    if (this.Left + this.ActualWidth > this.MaxWidth)
                    {
                        rightBorderViolated = true;
                    }
                    //InfoController.Say(String.Format("widthDiff: {0}, heightDiff: {1}, newLeft: {2}, newTop: {3}, RBV: {4}, BBV: {5}",
                    //  widthDiff, heightDiff, newLeft, newTop, rightBorderViolated, bottomBorderViolated));
                    if (newLeft + this.ActualWidth > this.MaxWidth)
                    {
                        newLeft = this.MaxWidth - this.ActualWidth;
                    }
                    if (newTop + this.ActualHeight > this.MaxHeight)
                    {
                        newTop = this.MaxHeight - effectiveHeight;
                    }
                    if (bottomBorderViolated)
                    {
                        this.Top = newTop >= this.MinTop ? newTop : this.MinTop;
                    }
                    if (rightBorderViolated)
                    {
                        this.Left = newLeft >= this.MinLeft ? newLeft : this.MinLeft;
                    }
                    effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                }
                this._tabManualSize[this.MainTabControl.SelectedIndex] = this.SizeToContent == System.Windows.SizeToContent.Manual;
                this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width = this.ActualWidth;
                this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height = effectiveHeight;
            }
        }

        /// <summary>
        /// Centers Window on Screen.
        /// </summary>
        public void MoveWindowToStartPosition()
        {
            this.CenterWindowOnScreen(humanFeelGood: true);
        }

        #endregion public members

        #region private members

        // 14.10.2015 Erik Nagel+ wieder aktiviert. TEST: 11.10.2015 Erik Nagel+- deaktiviert.
        private System.Timers.Timer _forceScreenRefreshTimer;
        private bool _contentRendered;
        private DispatcherTimer _sizeChangedTimer;
        private Rect[] _lastWindowMeasures;
        private bool[] _tabManualSize;
        private bool _handleResizeEvents;
        private int _screenRefreshTimerInterval;
        private ZoomBox _treeZoomBox;
        private ZoomBox _jobGroupZoomBox;
        private WindowAspects _mainWindowStartAspects;

        // Wenn das Window einige Zeit minimiert war und dann wieder hervorgeholt wird,
        // wird ein Teil grafisch nicht wiederhergestellt, es bleibt ein schwarzer Streifen.
        // Dies wird hier durch eine temporäre Änderung der Window.Width um ein Pixel korrigiert.
        private void refreshByTemporarilyChangingSize()
        {
            // return;
            this.Dispatcher.BeginInvoke(new Action(()
              =>
            {
                this._handleResizeEvents = false;
                System.Windows.SizeToContent oldSizeToContent = this.SizeToContent;
                double initialWidth = this.ActualWidth;
                this.Width = initialWidth - 1;
                this.SizeToContent = System.Windows.SizeToContent.Manual;
                this.UpdateLayout();
                this.Width = initialWidth;
                this.SizeToContent = oldSizeToContent;
                this.UpdateLayout();
                this._handleResizeEvents = true;
            }), DispatcherPriority.Render); // DispatcherPriority.Render ist essenziell.
        }

        private void CenterWindowOnScreen(bool humanFeelGood)
        {
            double windowWidth = this.Width;
            double windowHeight = this.Height + SystemParameters.WindowCaptionHeight;
            if (this.SizeOnVirtualScreen)
            {
                this.MaxHeight = System.Windows.SystemParameters.VirtualScreenHeight;
                this.MaxWidth = System.Windows.SystemParameters.VirtualScreenWidth;
                this.MinLeft = 0;
                this.MinTop = 0;
            }
            else
            {
                ScreenInfo actScreenInfo = ScreenInfo.GetActualScreenInfo(this);
                this.MaxHeight = actScreenInfo.WorkingArea.Height;
                this.MaxWidth = actScreenInfo.WorkingArea.Width;
                this.MinLeft = actScreenInfo.WorkingArea.Left;
                this.MinTop = actScreenInfo.WorkingArea.Top;
            }

            if (windowHeight > this.MaxHeight)
            {
                windowHeight = this.MaxHeight;
            }
            double divisor = humanFeelGood ? 1.7 : 2.0;
            double newLeft = (this.MaxWidth / 2) - (windowWidth / divisor);
            double newTop = (this.MaxWidth / 2) - (windowHeight / divisor);
            this.Left = newLeft >= this.MinLeft ? newLeft : this.MinLeft;
            this.Top = newTop >= this.MinTop ? newTop : this.MinTop;
        }

        private void SaveWindowAspectsAndCallViewModelLogicCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// Speichert die aktuellen Window-Darstellungsparameter in einer Instanz von "WindowAspects"
        /// und gibt "WindowAspects" dan als Aufrufparameter an die ViewModel-Routine "SaveTreeState" weiter,
        /// welche ihrerseits die WindowAspects ergänzt und zusammen mit Tree-relevanten Parametern abspeichert.
        /// </summary>
        /// <param name="target">Auslöser (Besitzer) des Commands.</param>
        /// <param name="e">Das RoutedCommand.</param>
        private void SaveWindowAspectsAndCallViewModelLogicCommandExecuted(object target, ExecutedRoutedEventArgs e)
        {
            double horizontalScroll;
            double verticalScroll;
            ScaleTransform scaleTransform;
            bool isScrollbarVisible;
            if (this.MainTabControl.SelectedIndex == 0)
            {
                horizontalScroll = this._treeZoomBox.HorizontalScroll;
                verticalScroll = this._treeZoomBox.VerticalScroll;
                scaleTransform = this._treeZoomBox.GetScale();
                isScrollbarVisible = this._treeZoomBox.IsHorizontalScrollbarVisible || this._treeZoomBox.IsVerticalScrollbarVisible;
            }
            else
            {
                horizontalScroll = this._jobGroupZoomBox.HorizontalScroll;
                verticalScroll = this._jobGroupZoomBox.VerticalScroll;
                scaleTransform = this._jobGroupZoomBox.GetScale();
                isScrollbarVisible = this._jobGroupZoomBox.IsHorizontalScrollbarVisible || this._jobGroupZoomBox.IsVerticalScrollbarVisible;
            }
            //String command = ((RoutedCommand)e.Command).Name;
            //MessageBox.Show("The \"" + command + "\" command has been invoked. ");
            WindowAspects windowAspects = new WindowAspects()
            {
                WindowLeft = this.Left,
                WindowTop = this.Top,
                WindowWidth = this.ActualWidth,
                WindowHeight = this.ActualHeight,
                WindowScrollLeft = horizontalScroll, // Eigenschaft der beinhalteten ZoomBox
                WindowScrollTop = verticalScroll, // Eigenschaft der beinhalteten ZoomBox
                WindowZoom = scaleTransform.ScaleX, // Eigenschaft der beinhalteten ZoomBox
                IsScrollbarVisible = isScrollbarVisible, // True, wenn mindestens eine Scrollbar sichbar ist
                ActTabControlTab = this.MainTabControl.SelectedIndex // aktuell ausgewählter Tab im TabControl
            };
            (this.DataContext as MainWindowViewModel).SaveTreeState(windowAspects);
        }

        #region EventHandler

        private void window_ContentRendered(object sender, System.EventArgs e)
        {
            this._contentRendered = true;
            // Der auskommentierte Teil funktioniert immer nur zur Hälfte, je nachdem welcher Tab des
            // TabControls zuerst angezeigt wird. Der Visual(Teil-)Tree und wird nämlich erst erzeugt, wenn das
            // zugehörige FrameworkElement auch tatsächlich sichtbar wird.
            // Und der Logical(Teil-) steht hier auch noch nicht zur Verfügung, sondern (anders als bei der
            // Desktop-Anwendung) erst in den Loaded-Handlern der TabControl-Tabs (weiter unten).
            // this._treeZoomBox = UIHelper.FindFirstVisualChildOfTypeAndNameOrAttachedName<ZoomBox>(this, "ZoomBox1");
            // this._jobGroupZoomBox = UIHelper.FindFirstVisualChildOfTypeAndNameOrAttachedName<ZoomBox>(this, "ZoomBox2");
            // this._treeZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>(this, "ZoomBox1");
            // this._treeZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>(this, "ZoomBox2");
            if (this.MainWindowStartAspects != null)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = this.MainWindowStartAspects.WindowLeft;
                this.Top = this.MainWindowStartAspects.WindowTop;
                if (this.MainWindowStartAspects.WindowWidth > 0 && this.MainWindowStartAspects.WindowHeight > 0)
                {
                    this.SizeToContent = SizeToContent.Manual;
                    this.Width = this.MainWindowStartAspects.WindowWidth;
                    this.Height = this.MainWindowStartAspects.WindowHeight;
                }
                if (this.MainWindowStartAspects.ActTabControlTab == 0)
                {
                    this.SetTreeAspects(true);
                }
                else
                {
                    this.SetJobGroupAspects(true);
                }
                if (!this.MainWindowStartAspects.IsScrollbarVisible)
                {
                    this.ForceRecalculateWindowMeasures(null);
                }
                else
                {
                    this.RecalculateWindowMeasures();
                }
            }
            else
            {
                this.RecalculateWindowMeasures();
            }
            //this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void SetTreeAspects(bool fromLoadedSettings)
        {
            if (this._treeZoomBox != null)
            {
                if (fromLoadedSettings)
                {
                    this._treeZoomBox.SetScale(this.MainWindowStartAspects.WindowZoom, this.MainWindowStartAspects.WindowZoom);
                    this._treeZoomBox.HorizontalScroll = this.MainWindowStartAspects.WindowScrollLeft;
                    this._treeZoomBox.VerticalScroll = this.MainWindowStartAspects.WindowScrollTop;
                }
                else
                {
                    this._treeZoomBox.SetScale(this._jobGroupZoomBox.GetScale().ScaleX, this._jobGroupZoomBox.GetScale().ScaleY);
                }
            }
        }

        private void SetJobGroupAspects(bool fromLoadedSettings)
        {
            if (this._jobGroupZoomBox != null)
            {
                if (fromLoadedSettings)
                {
                    this._jobGroupZoomBox.SetScale(this.MainWindowStartAspects.WindowZoom, this.MainWindowStartAspects.WindowZoom);
                    this._jobGroupZoomBox.HorizontalScroll = this.MainWindowStartAspects.WindowScrollLeft;
                    this._jobGroupZoomBox.VerticalScroll = this.MainWindowStartAspects.WindowScrollTop;
                }
                else
                {
                    this._jobGroupZoomBox.SetScale(this._treeZoomBox.GetScale().ScaleX, this._treeZoomBox.GetScale().ScaleY);
                }
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is TabControl) || (e.OriginalSource as TabControl).Name != this.MainTabControl.Name)
            {
                return; // the event was bubbling from an inner control.
            }
            if (this.MainTabControl.SelectedIndex == 0)
            {
                this.SetTreeAspects(false);
            }
            else
            {
                this.SetJobGroupAspects(false);
            }
            if (this._tabManualSize[this.MainTabControl.SelectedIndex])
            {
                this.SizeToContent = System.Windows.SizeToContent.Manual;
                if (this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width > 0)
                {
                    this.Width = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width;
                    this.Height = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height;
                }
            }
            else
            {
                this.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            }
        }

        private void TreeTabLoaded(object sender, RoutedEventArgs e)
        {
            this._treeZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>(sender as FrameworkElement, null);
        }

        private void JobTabLoaded(object sender, RoutedEventArgs e)
        {
            this._jobGroupZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>(sender as FrameworkElement, null);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.refreshByTemporarilyChangingSize();
            this._forceScreenRefreshTimer.Interval = 500; // Kurz umschalten, damit schnell ein Refresh nachgeschoben wird.
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this._forceScreenRefreshTimer.Dispose();
            if (this._sizeChangedTimer != null && this._sizeChangedTimer.IsEnabled)
            {
                this._sizeChangedTimer.Stop();
                this._sizeChangedTimer = null;
            }
            //Application.Current.Shutdown(); // funktioniert nicht
        }

        private void sizeHasChanged(object sender, EventArgs e)
        {
            this._sizeChangedTimer.Stop();
            this.RecalculateWindowMeasures();
        }

        private void TreeElementStateChangedEventHandler(object sender, RoutedEventArgs e)
        {
            if (this._sizeChangedTimer.IsEnabled)
            {
                this._sizeChangedTimer.Stop();
            }
            this._sizeChangedTimer.Start();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this._handleResizeEvents)
            {
                if (this._sizeChangedTimer.IsEnabled)
                {
                    this._sizeChangedTimer.Stop();
                }
                this._sizeChangedTimer.Start();
            }
        }

        #endregion EventHandler

        #endregion private members

    }
}
