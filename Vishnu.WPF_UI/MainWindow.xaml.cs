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
using System.Windows.Media.TextFormatting;
using NetEti.ApplicationControl;
using System.Security.Cryptography.Xml;

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
        /// Wird aktuell (02.02.2024) intern immer auf true gesetzt! Vormals default: false;
        /// Muss von außen nach Instanziierung gesetzt werden.
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
        /// Bei 1 mit der Job-Ansicht gestartet, ansonsten mit der Tree-Ansicht (default: 0).
        /// </summary>
        public int FirstSelectedIndex
        {
            get
            {
                return this._firstSelectedIndex;
            }
            set { this._firstSelectedIndex = value; }
        }

        /// <summary>
        /// Wesentlichen Darstellungsmerkmale des Vishnu-MainWindows.
        /// Werden beim Start der Anwendung aus den AppSettings gefüllt.
        /// </summary>
        public WindowAspects? MainWindowAspects
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
                    if (this.MainTabControl != null)
                    {
                        this.MainTabControl.SelectedIndex = this._mainWindowStartAspects?.ActTabControlTab ?? 0;
                    }
                }
            }
        }

        /// <summary>
        /// Zeigt an, dass das Hauptfenster gerade seinen Standort wechselt
        /// und somit noch nicht die korrekte Endposition an Checker und Worker melden kann.
        /// Wird in App.cs genutzt um beim Start der Applikation auf das erste Positionieren
        /// des Hauptfensters zu warten, bevor ein Autostart durchgeführt wird.
        /// </summary>
        public volatile bool IsRelocating = true;

        /// <summary>
        /// Konstruktor des Haupt-Fensters.
        /// </summary>
        /// <param name="startWithJobs">Bei true wird mit der Jobs-Ansicht gestartet, ansonsten mit der Tree-Ansicht (default: false).</param>
        /// <param name="sizeOnVirtualScreen">Stillgelegter Parameter! Bei true wird über mehrere Bildschirme hinweg skaliert, ansonsten auf einen Bildschirm (aktuell: immer true!).</param>
        /// <param name="mainWindowStartAspects">Eventuell vorher gespeicherte Darstellungseigenschaften des Vishnu-MainWindow oder Defaults.</param>
        public MainWindow(bool startWithJobs, bool sizeOnVirtualScreen, WindowAspects? mainWindowStartAspects) : base()
        {
            this.SizeOnVirtualScreen = sizeOnVirtualScreen;
            this.MainWindowAspects = mainWindowStartAspects;

            this.DataContext = this;
            this.FirstSelectedIndex = startWithJobs ? 1 : 0;

            this.EvalMainWindowStartAspects();

            InitializeComponent();

            this._tabManualSize = new bool[this.MainTabControl.Items.Count];

            this.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            this.MinLeft = 0;
            this.MinTop = 0;
            this._contentRendered = false;
            this._handleResizeEvents = true;
            this._lastWindowMeasures = new Rect[this.MainTabControl.Items.Count];

            this.MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

            // Funktioniert nicht:
            // Registrieren des the Bubble Event Handlers 
            //      this.AddHandler(CustomControls.ZoomTreeView.TreeElementStateChangedEvent,
            // new RoutedEventHandler(TreeElementStateChangedEventHandler));
            //IObservable<SizeChangedEventArgs> ObservableSizeChanges = Observable
            //    .FromEventPattern<SizeChangedEventArgs>(this, "sizeHasChanged")
            //    .Select(x => x.EventArgs)
            //    .Throttle(TimeSpan.FromMilliseconds(200));

            // Funktioniert auch nicht (feuert bei komplexer Grafik zu früh):
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
              += new System.Timers.ElapsedEventHandler(new Action<object?, System.Timers.ElapsedEventArgs>((a, b)
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
        /// <param name="parameter">Optionales Parameter-Objekt, hier ungenutzt.</param>
        public void ForceRecalculateWindowMeasures(object? parameter)
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
            double lastWidth = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width;
            double lastHeight = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height;
            ScreenInfo actScreenInfo = ScreenInfo.GetActualScreenInfo(this);
            if (lastWidth + lastHeight == 0 && this.MainWindowAspects != null)
            {
                // actScreenInfo = ScreenInfo.GetAllScreenInfos(this)[MainWindowAspects.ActScreenIndex];
                if (this.MainWindowAspects.ActScreenIndex != ScreenInfo.GetActualScreenInfoIndex(this))
                {
                    // 09.01.2024 Nagel+- TEST
                    // throw new ApplicationException(
                    // "this.MainWindowAspects.ActScreenIndex != ScreenInfo.GetActualScreenInfoIndex(this)");
                    InfoController.GetInfoPublisher().Publish(this,
                        "Gespeicherte Bildschirmdaten konnten nicht wiederhergestellt werden,"
                        + " der adressierte Bildschirm ist aktuell nicht verfügbar.", InfoType.NoRegex);
                }
            }
            double xFactor = 1.0;
            double yFactor = 1.0;
            if (this.SizeOnVirtualScreen)
            {
                this.MaxHeight = System.Windows.SystemParameters.VirtualScreenHeight;
                this.MaxWidth = System.Windows.SystemParameters.VirtualScreenWidth;
                this.MinLeft = 0;
                this.MinTop = 0;
                // 28.01.2024 Nagel Test+- yFactor = this.MaxHeight / actScreenInfo.WorkingArea.Height;
                // 28.01.2024 Nagel Test+- xFactor = this.MaxWidth / actScreenInfo.WorkingArea.Width;
            }
            //else
            //{
            //    this.MaxHeight = actScreenInfo.WorkingArea.Height;
            //    this.MaxWidth = actScreenInfo.WorkingArea.Width;
            //    this.MinLeft = actScreenInfo.Bounds.Left;
            //    this.MinTop = actScreenInfo.Bounds.Top;
            //}
            AppSettings.MaxHeight = this.MaxHeight;
            AppSettings.MaxWidth = this.MaxWidth;
            if (this.WindowState == WindowState.Normal && this._contentRendered)
            {
                double effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                if (this.SizeToContent == System.Windows.SizeToContent.WidthAndHeight)
                {
                    double widthDiff = 0;
                    double heightDiff = 0;
                    double newLeft = this.Left;
                    double newTop = this.Top;
                    if (lastWidth + lastHeight > 0)
                    {
                        widthDiff = lastWidth - this.ActualWidth;
                        heightDiff = lastHeight - effectiveHeight;
                        newLeft += (widthDiff / 2.0);
                        newTop += (heightDiff / 2.0);
                    }
                    else
                    {
                        if (this.MainWindowAspects != null)
                        {
                            lastWidth = this.MainWindowAspects.WindowWidth;
                            lastHeight = this.MainWindowAspects.WindowHeight + SystemParameters.WindowCaptionHeight;
                            widthDiff = lastWidth - this.ActualWidth;
                            heightDiff = lastHeight - effectiveHeight;
                        }
                        newLeft = (newLeft + this.ActualWidth / 2) * xFactor - (this.ActualWidth / 2);
                        newTop = (newTop + effectiveHeight / 2) * yFactor - (effectiveHeight / 2);
                        newLeft += (widthDiff / 2.0);
                        newTop += (heightDiff / 2.0);
                    }
                    if (newLeft + this.ActualWidth > this.MinLeft + this.MaxWidth)
                    {
                        newLeft = this.MinLeft + this.MaxWidth - this.ActualWidth;
                    }
                    if (newTop + this.ActualHeight > this.MinTop + this.MaxHeight)
                    {
                        newTop = this.MinTop + this.MaxHeight - this.ActualHeight;
                    }
                    if (newLeft < this.MinLeft)
                    {
                        newLeft = this.MinLeft;
                    }
                    if (newTop < this.MinTop)
                    {
                        newTop = this.MinTop;
                    }
                    if (newLeft > this.MaxWidth - 50)
                    {
                        // throw new ArithmeticException("newLeft > this.MaxWidth - 50");
                        newLeft = this.MaxWidth - 50;
                    }
                    if (newTop > MaxHeight - 35)
                    {
                        // throw new ArithmeticException("newTop > this.MaxHeight - 35");
                        newTop = this.MaxHeight - 35;
                    }
                    this.Left = newLeft;
                    this.Top = newTop;
                    effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                }
                this._tabManualSize[this.MainTabControl.SelectedIndex] = this.SizeToContent == System.Windows.SizeToContent.Manual;
                this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width = this.ActualWidth;
                this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height = effectiveHeight;
                this.IsRelocating = false;
            }
        }

        /* // 07.01.2024 Nagel+
        /// <summary>
        /// Setzt die Fenstergröße unter Berücksichtigung von Maximalgrenzen auf die
        /// Höhe und Breite des Inhalts und die Property SizeToContent auf WidthAndHeight.
        /// Zentriert das Window dann neu relativ zur letzten Position.
        /// </summary>
        public void RecalculateWindowMeasures()
        {
            double xFactor = 1.0;
            double yFactor = 1.0;
            ScreenInfo actScreenInfo = ScreenInfo.GetActualScreenInfo(this);
            if (this.SizeOnVirtualScreen)
            {
                this.MaxHeight = System.Windows.SystemParameters.VirtualScreenHeight;
                this.MaxWidth = System.Windows.SystemParameters.VirtualScreenWidth;
                this.MinLeft = 0;
                this.MinTop = 0;
                yFactor = this.MaxHeight / actScreenInfo.WorkingArea.Height;
                xFactor = this.MaxWidth / actScreenInfo.WorkingArea.Width;
            }
            else
            {
                this.MaxHeight = actScreenInfo.WorkingArea.Height;
                this.MaxWidth = actScreenInfo.WorkingArea.Width;
                this.MinLeft = actScreenInfo.WorkingArea.Left;
                this.MinTop = actScreenInfo.WorkingArea.Top;
            }
            if (this.WindowState == WindowState.Normal && this._contentRendered)
            {
                double effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                if (this.SizeToContent == System.Windows.SizeToContent.WidthAndHeight)
                {
                    double lastWidth = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width;
                    double lastHeight = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height;
                    double widthDiff = 0;
                    double heightDiff = 0;
                    double newLeft = this.Left;
                    double newTop = this.Top;
                    if (lastWidth + lastHeight > 0)
                    {
                        widthDiff = lastWidth - this.ActualWidth;
                        heightDiff = lastHeight - effectiveHeight;
                        newLeft += (widthDiff / 2.0);
                        newTop += (heightDiff / 2.0);
                    }
                    else
                    {
                        newLeft = (newLeft + this.ActualWidth / 2) * xFactor - (this.ActualWidth / 2);
                        newTop = (newTop + effectiveHeight / 2) * yFactor - (effectiveHeight / 2);
                    }
                    bool bottomBorderViolated = false;
                    if (this.Top + this.ActualHeight > this.MaxHeight)
                    {
                        bottomBorderViolated = true;
                    }
                    bool rightBorderViolated = false;
                    if (this.Left + this.ActualWidth > this.MaxWidth)
                    {
                        rightBorderViolated = true;
                    }
                    if (newLeft + this.ActualWidth > this.MaxWidth)
                    {
                        newLeft = this.MaxWidth - this.ActualWidth;
                    }
                    if (newTop + this.ActualHeight > this.MaxHeight)
                    {
                        newTop = this.MaxHeight - effectiveHeight;
                    }
                    if (lastWidth + lastHeight == 0 && this.SizeOnVirtualScreen)
                    {
                        this.Left = newLeft;
                        this.Top = newTop;
                    }
                    if (bottomBorderViolated || lastWidth + lastHeight > 0)
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
                this.IsRelocating = false;
            }
        } */ // 07.01.2024 Nagel-

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
        private DispatcherTimer? _sizeChangedTimer;
        private Rect[] _lastWindowMeasures;
        private bool[] _tabManualSize;
        private bool _handleResizeEvents;
        private int _screenRefreshTimerInterval;
        private ZoomBox? _treeZoomBox;
        private ZoomBox? _jobGroupZoomBox;
        private WindowAspects? _mainWindowStartAspects;
        private int _firstSelectedIndex;

        /// <summary>
        /// Trifft Vorbelegungen für das aktuelle Window nach vorher gespeicherten Bildschirmeinstellungen (Strg-s).
        /// Diese Routine wird im Konstruktor von MainWindow noch vor InitializeComponent aufgerufen.
        /// </summary>
        private void EvalMainWindowStartAspects()
        {
            if (this.MainWindowAspects != null)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                if (this.MainWindowAspects.WindowWidth > 0 && this.MainWindowAspects.WindowHeight > 0)
                {
                    this.SizeToContent = SizeToContent.Manual;
                    this.Width = this.MainWindowAspects.WindowWidth;
                    this.Height = this.MainWindowAspects.WindowHeight;
                    this.Left = this.MainWindowAspects.WindowCenterX - MainWindowAspects.WindowWidth / 2;
                    this.Top = this.MainWindowAspects.WindowCenterY - MainWindowAspects.WindowHeight / 2;
                }
                this.FirstSelectedIndex = this.MainWindowAspects.ActTabControlTab;
            }
        }

        /// <summary>
        /// Aktiviert Vorbelegungen für das aktuelle Window nach vorher gespeicherten Bildschirmeinstellungen (Strg-s).
        /// Diese Routine kann erst im Eventhandler window_ContentRendered aufgerufen werden, wenn alle Controls schon
        /// vorhanden sind.
        /// </summary>
        private void ActivateMainWindowStartAspects()
        {
            if (this.MainWindowAspects != null)
            {
                if (this.MainWindowAspects.ActTabControlTab == 0)
                {
                    this.SetTreeAspects(true);
                }
                else
                {
                    this.SetJobGroupAspects(true);
                }
                if (!this.MainWindowAspects.IsScrollbarVisible)
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
        }

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
            //else
            //{
            //    ScreenInfo actScreenInfo = ScreenInfo.GetActualScreenInfo(this);
            //    this.MaxHeight = actScreenInfo.WorkingArea.Height;
            //    this.MaxWidth = actScreenInfo.WorkingArea.Width;
            //    this.MinLeft = actScreenInfo.WorkingArea.Left;
            //    this.MinTop = actScreenInfo.WorkingArea.Top;
            //}

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
            double horizontalScroll = 0.0;
            double verticalScroll = 0.0;
            ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
            bool isScrollbarVisible = false;
            if (this.MainTabControl.SelectedIndex == 0 && this._treeZoomBox != null)
            {
                horizontalScroll = this._treeZoomBox.HorizontalScroll;
                verticalScroll = this._treeZoomBox.VerticalScroll;
                scaleTransform = this._treeZoomBox.GetScale();
                isScrollbarVisible = this._treeZoomBox.IsHorizontalScrollbarVisible || this._treeZoomBox.IsVerticalScrollbarVisible;
            }
            else
            {
                if (this._jobGroupZoomBox != null)
                {
                    horizontalScroll = this._jobGroupZoomBox.HorizontalScroll;
                    verticalScroll = this._jobGroupZoomBox.VerticalScroll;
                    scaleTransform = this._jobGroupZoomBox.GetScale();
                    isScrollbarVisible = this._jobGroupZoomBox.IsHorizontalScrollbarVisible || this._jobGroupZoomBox.IsVerticalScrollbarVisible;
                }
            }
            //String command = ((RoutedCommand)e.Command).Name;
            //MessageBox.Show("The \"" + command + "\" command has been invoked. ");
            WindowAspects windowAspects = new WindowAspects()
            {
                WindowWidth = this.ActualWidth,
                WindowHeight = this.ActualHeight,
                WindowCenterX = this.Left + this.ActualWidth / 2,
                WindowCenterY = this.Top + this.ActualHeight / 2,
                WindowScrollLeft = horizontalScroll, // Eigenschaft der beinhalteten ZoomBox
                WindowScrollTop = verticalScroll, // Eigenschaft der beinhalteten ZoomBox
                WindowZoom = scaleTransform?.ScaleX ?? 1.0, // Eigenschaft der beinhalteten ZoomBox
                IsScrollbarVisible = isScrollbarVisible, // True, wenn mindestens eine Scrollbar sichtbar ist
                ActTabControlTab = this.MainTabControl.SelectedIndex, // aktuell ausgewählter Tab im TabControl
                ActScreenIndex = ScreenInfo.GetActualScreenInfoIndex(this)
            };
            ((MainWindowViewModel)this.DataContext).SaveTreeState(windowAspects);
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

            this.EvalMainWindowStartAspects();
            this.ActivateMainWindowStartAspects();

            //this.SizeToContent = SizeToContent.WidthAndHeight;
        }

        private void SetTreeAspects(bool fromLoadedSettings)
        {
            if (this._treeZoomBox != null)
            {
                if (fromLoadedSettings)
                {
                    if (this.MainWindowAspects != null)
                    {
                        this._treeZoomBox.SetScale(this.MainWindowAspects.WindowZoom, this.MainWindowAspects.WindowZoom);
                        this._treeZoomBox.HorizontalScroll = this.MainWindowAspects.WindowScrollLeft;
                        this._treeZoomBox.VerticalScroll = this.MainWindowAspects.WindowScrollTop;
                    }
                }
                else
                {
                    if (this._jobGroupZoomBox != null)
                    {
                        this._treeZoomBox.SetScale(this._jobGroupZoomBox.GetScale().ScaleX, this._jobGroupZoomBox.GetScale().ScaleY);
                    }
                }
            }
        }

        private void SetJobGroupAspects(bool fromLoadedSettings)
        {
            if (this._jobGroupZoomBox != null)
            {
                if (fromLoadedSettings)
                {
                    if (this.MainWindowAspects != null)
                    {
                        this._jobGroupZoomBox.SetScale(this.MainWindowAspects.WindowZoom, this.MainWindowAspects.WindowZoom);
                        this._jobGroupZoomBox.HorizontalScroll = this.MainWindowAspects.WindowScrollLeft;
                        this._jobGroupZoomBox.VerticalScroll = this.MainWindowAspects.WindowScrollTop;
                    }
                }
                else
                {
                    if (this._treeZoomBox != null)
                    {
                        this._jobGroupZoomBox.SetScale(this._treeZoomBox.GetScale().ScaleX, this._treeZoomBox.GetScale().ScaleY);
                    }
                }
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is TabControl) || ((TabControl)e.OriginalSource).Name != this.MainTabControl.Name)
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
            this._treeZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>((FrameworkElement)sender, null);
        }

        private void JobTabLoaded(object sender, RoutedEventArgs e)
        {
            this._jobGroupZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>((FrameworkElement)sender, null);
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

        private void sizeHasChanged(object? sender, EventArgs e)
        {
            this._sizeChangedTimer?.Stop();
            this.RecalculateWindowMeasures();
        }

        private void TreeElementStateChangedEventHandler(object sender, RoutedEventArgs e)
        {
            if (this._sizeChangedTimer?.IsEnabled == true)
            {
                this._sizeChangedTimer.Stop();
            }
            this._sizeChangedTimer?.Start();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this._handleResizeEvents)
            {
                if (this._sizeChangedTimer?.IsEnabled == true)
                {
                    this._sizeChangedTimer.Stop();
                }
                this._sizeChangedTimer?.Start();
            }
        }

        #endregion EventHandler

        #endregion private members

    }
}
