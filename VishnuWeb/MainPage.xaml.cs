using NetEti.CustomControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vishnu.Interchange;
using Vishnu.ViewModel;
using Vishnu.WPF_UI;

namespace VishnuWeb
{
    /// <summary>
    /// Interaktionslogik für Page1.xaml
    /// </summary>
    public partial class MainPage : Page
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
            "SaveWindowAspectsAndCallViewModelLogicCommand", typeof(MainPage));

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
        /// Konstruktor der Haupt-Seite.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            this.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.MaxHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            this.MinLeft = 0;
            this.MinTop = 0;
            this._lastLoadedLeft = -1;
            this._lastLoadedTop = -1;
            this._lastLoadedWidth = -1;
            this._lastLoadedHeight = -1;
            this._tabManualSize = new bool[this.MainTabControl.Items.Count];
            this._lastWindowMeasures = new Rect[this.MainTabControl.Items.Count];
        }

        /// <summary>
        /// Setzt die Fenstergröße unter Berücksichtigung von Maximalgrenzen auf die
        /// Höhe und Breite des Inhalts und die Property SizeToContent auf WidthAndHeight.
        /// Zentriert das Window dann neu relativ zur letzten Position.
        /// Hier ist die Routine leer und dient lediglich als Kompatibilitätsparameter.
        /// </summary>
        /// <param name="parameter">Ein object-Parameter (kann null sein).</param>
        public void ForceRecalculateWindowMeasures(object parameter)
        {
        }

        #endregion public members

        #region private members

        private bool[] _tabManualSize;
        private Rect[] _lastWindowMeasures;
        private bool _contentRendered;
        private double _lastLoadedLeft;
        private double _lastLoadedTop;
        private double _lastLoadedWidth;
        private double _lastLoadedHeight;
        private ZoomBox _treeZoomBox;
        private ZoomBox _jobGroupZoomBox;
        private WindowAspects _mainWindowStartAspects;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowTitle = (this.DataContext as MainWindowViewModel).WindowTitle;
            this.Page_ContentRendered(sender, e);
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
                WindowLeft = this._lastLoadedLeft >= 0 ? this._lastLoadedLeft : 0,
                WindowTop = this._lastLoadedTop >= 0 ? this._lastLoadedTop : 0,
                WindowWidth = this._lastLoadedWidth >= 0 ? this._lastLoadedWidth : this.ActualWidth,
                WindowHeight = this._lastLoadedHeight >= 0 ? this._lastLoadedHeight : this.ActualHeight,
                WindowScrollLeft = horizontalScroll, // Eigenschaft der beinhalteten ZoomBox
                WindowScrollTop = verticalScroll, // Eigenschaft der beinhalteten ZoomBox
                WindowZoom = scaleTransform.ScaleX, // Eigenschaft der beinhalteten ZoomBox
                IsScrollbarVisible = isScrollbarVisible, // True, wenn mindestens eine Scrollbar sichbar ist
                ActTabControlTab = this.MainTabControl.SelectedIndex // aktuell ausgewählter Tab im TabControl
            };
            (this.DataContext as MainWindowViewModel).SaveTreeState(windowAspects);
        }

        private void Page_ContentRendered(object sender, System.EventArgs e)
        {
            this._contentRendered = true;
            this._treeZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>(sender as FrameworkElement, "ZoomBox1");
            this._jobGroupZoomBox = UIHelper.FindFirstLogicalChildOfTypeAndName<ZoomBox>(sender as FrameworkElement, "ZoomBox2");
            if (this.MainWindowStartAspects != null)
            {
                this._lastLoadedLeft = this.MainWindowStartAspects.WindowLeft;
                this._lastLoadedTop = this.MainWindowStartAspects.WindowTop;
                this._lastLoadedWidth = this.MainWindowStartAspects.WindowWidth;
                this._lastLoadedHeight = this.MainWindowStartAspects.WindowHeight;

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
                if (this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width > 0)
                {
                    this.Width = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width;
                    this.Height = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height;
                }
            }
       }

        /*
        private void TreeTabLoaded(object sender, RoutedEventArgs e)
        {
            this._treeZoomBox = FindFirstLogicalChildContained<ZoomBox>(sender as FrameworkElement);
        }

        private void JobTabLoaded(object sender, RoutedEventArgs e)
        {
            this._jobGroupZoomBox = FindFirstLogicalChildContained<ZoomBox>(sender as FrameworkElement);
        }
        */

        /// <summary>
        /// Setzt die Fenstergröße unter Berücksichtigung von Maximalgrenzen auf die
        /// Höhe und Breite des Inhalts und die Property SizeToContent auf WidthAndHeight.
        /// Zentriert das Window dann neu relativ zur letzten Position.
        /// </summary>
        public void RecalculateWindowMeasures()
        {
            // ScreenInfo actScreenInfo = ScreenInfo.GetActualScreenInfo(this);
            // this.MaxHeight = this.Height; // actScreenInfo.WorkingArea.Height;
            // this.MaxWidth = this.Width; // actScreenInfo.WorkingArea.Width;
            this.MinLeft = 0; // actScreenInfo.WorkingArea.Left;
            this.MinTop = 0; // actScreenInfo.WorkingArea.Top;

            //InfoController.Say(String.Format("MaxWidth: {0}, MaxHeight: {1}, Width: {2}, Height: {3}, Left: {4}, Top: {5}",
            //  this.MaxWidth, this.MaxHeight, this.ActualWidth, this.ActualHeight, this.Left, this.Top));
            if (this._contentRendered)
            {
                double effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                //InfoController.Say(String.Format("MaxWidth: {0}, MaxHeight: {1}, Width: {2}, Height: {3}, Left: {4}, Top: {5}",
                //  this.MaxWidth, this.MaxHeight, this.ActualWidth, effectiveHeight, this.Left, this.Top));
                double widthDiff = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width - this.ActualWidth;
                double heightDiff = this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height - effectiveHeight;
                effectiveHeight = this.ActualHeight + SystemParameters.WindowCaptionHeight;
                this._tabManualSize[this.MainTabControl.SelectedIndex] = false; // this.SizeToContent == System.Windows.SizeToContent.Manual;
                this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Width = this.ActualWidth;
                this._lastWindowMeasures[this.MainTabControl.SelectedIndex].Height = effectiveHeight;
            }
        }

        #endregion private members

    }
}
