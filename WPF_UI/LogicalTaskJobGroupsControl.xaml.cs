using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Vishnu.Interchange;
using Vishnu.ViewModel;

namespace Vishnu.WPF_UI
{
    /// <summary>
    /// Interaktionslogik für LogicalTaskJobGroupsControl.xaml
    /// </summary>
    /// <remarks>
    /// File: LogicalTaskJobGroupsControl.xaml.cs
    /// Autor: Erik Nagel
    ///
    /// 01.10.2014 Erik Nagel: erstellt
    /// </remarks>
    public partial class LogicalTaskJobGroupsControl : DynamicUserControlBase
    {
        /// <summary>
        /// Konstruktor: erzeugt eine Instanz von LogicalTaskTreeViewModel
        /// und setzt den DataContext darauf.
        /// </summary>
        public LogicalTaskJobGroupsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Hier werden die beim disposing notwendigen Aktionen durchgeführt.
        /// </summary>
        protected override void DoDispose()
        {
            if (this._scrollViewer != null)
            {
                this._scrollViewer.PreviewMouseWheel -= this.mouseWheelDispatcher;
            }
            base.DoDispose();
        }

        /// <summary>
        /// Löst das OnDynamicUserControl_ContentRendered-Ereignis aus.
        /// </summary>
        protected override void OnDynamicUserControl_ContentRendered()
        {
            base.OnDynamicUserControl_ContentRendered();
            this._scrollViewer = UIHelper.FindFirstVisualChildOfTypeAfterVisualChildOfTypeAndName<ScrollViewer, ContentPresenter>(this, "ExpandedSite");
            if (this._scrollViewer != null)
            {
                this._scrollViewer.PreviewMouseWheel += this.mouseWheelDispatcher;
            }
        }

        // Überschreibt OnPreviewMouseWheel für die Implementierung
        // der erweiterten Mausrad-Funtionen.
        // Mausrad gedreht:
        //   bei gedrückter Umschalt-Taste wird horizontal gescrollt,
        //   ansonsten vertikal.
        private void mouseWheelDispatcher(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
            {
                this.horizontalScroll(e);
            }
            else
            {
                this.verticalScroll(e);
            }
        }

        #region scroll

        private ScrollViewer _scrollViewer;

        // Scrollt in dem Control horizontal. Wird über Shift+Mousewheel ausgelöst.
        // Greift, wenn das einzelne Control so groß wird, dass unterhalb des Expanders
        // ein horizontaler Scrollbalken entsteht.
        private void horizontalScroll(MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                this._scrollViewer.LineLeft();
                this._scrollViewer.LineLeft();
                this._scrollViewer.LineLeft();
            }
            else
            {
                this._scrollViewer.LineRight();
                this._scrollViewer.LineRight();
                this._scrollViewer.LineRight();
            }
        }

        // Scrollt in dem Control vertikal. Wird über Mousewheel ausgelöst.
        // Greift, wenn das einzelne Control so groß wird, dass unterhalb des Expanders
        // ein vertikaler Scrollbalken entsteht.
        private void verticalScroll(MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                this._scrollViewer.LineUp();
                this._scrollViewer.LineUp();
                this._scrollViewer.LineUp();
            }
            else
            {
                this._scrollViewer.LineDown();
                this._scrollViewer.LineDown();
                this._scrollViewer.LineDown();
            }
        }

        #endregion scroll

    }
}
