using NetEti.ApplicationControl;
using NetEti.MultiScreen;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Parameter, die Checkern bei jedem Aufruf
    /// von Vishnu mitgegeben werden (nicht User-spezifisch).
    /// </summary>
    /// <remarks>
    /// File: TreeParameters.cs
    /// Autor: Erik Nagel
    ///
    /// 16.06.2015 Erik Nagel: erstellt
    /// </remarks>
    public class TreeParameters
    {
        /// <summary>
        /// Die absolute Bildschirmposition der Mitte des beinhaltenden Controls.
        /// </summary>
        public Point LastParentViewAbsoluteScreenPosition
        {
            get
            {
                if (_lastParentViewAbsoluteScreenPosition == null)
                {
                    _lastParentViewAbsoluteScreenPosition = GetParentViewAbsoluteScreenPosition();
                }
                return (Point)_lastParentViewAbsoluteScreenPosition;
            }
            set
            {
                if (value != _lastParentViewAbsoluteScreenPosition)
                {
                    _lastParentViewAbsoluteScreenPosition = value;
                }
            }
        }

        /// <summary>
        /// Name des Trees.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Pfad zum Verzeichnis der Checker.dll.
        /// </summary>
        public string? CheckerDllDirectory { get; set; }

        /// <summary>
        /// Das zugehörige Control.
        /// </summary>
        public FrameworkElement? ParentView {
            get
            {
                return this._parentView;
            }
            set
            {
                this._parentView = value;
                this._lastParentViewAbsoluteScreenPosition =
                    GetParentViewAbsoluteScreenPosition();
            }
        }

        /// <summary>
        /// Absolute Bildschirmposition der Mitte des beinhaltenden Controls.
        /// </summary>
        /// <returns>Absolute Bildschirmposition der Mitte des Parent-Controls.</returns>
        public virtual Point GetParentViewAbsoluteScreenPosition()
        {
            return WindowAspects.GetFrameworkElementAbsoluteScreenPosition(
                this.ParentView, false);
        }

        /// <summary>
        /// Konstruktor - übernimmt den logischen Namen des Trees und das Parent-Control.
        /// </summary>
        /// <param name="name">Der logische Name des Trees.</param>
        /// <param name="parentView">Das Parent-Control.</param>
        public TreeParameters(string name, FrameworkElement? parentView) : this(name)
        {
            this.ParentView = parentView; // Wird von JobList.ReloadBranch() aufgerufen.
        }

        /// <summary>
        /// Konstruktor - übernimmt den logischen Namen des Trees.
        /// </summary>
        /// <param name="name">Der logische Name des Trees.</param>
        public TreeParameters(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Überschriebene ToString-Methode - returniert nur den Namen.
        /// </summary>
        /// <returns>Name-Property der TreeParameters.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Überschriebene Equals-Methode.
        /// </summary>
        /// <param name="obj">Vergleichs-TreeParameters.</param>
        /// <returns>True, wenn beide Objekte Instanzen von TreeParameters sind und ihr Hashcode übereinstimmt.</returns>
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Überschriebene GetHashCode-Methode.
        /// </summary>
        /// <returns>Ein möglichst eindeutiger Integer für alle Properties einer Instanz zusammen.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Interner, Assembly-übergreifender Parameter.
        /// Enthält den LogicalTaskTree als IBusinessLogicRoot, um hier einen direkten
        /// Assembly-Verweis auf LogicalTaskTree zu vermeiden.
        /// Wird innerhalb der Assembly Vishnu.ViewModel wieder aufgelöst.
        /// Dort existiert ein Verweis auf LogicalTaskTree.
        /// </summary>
        public IBusinessLogicRoot? BusinessLogicRoot { get; set; }

        /// <summary>
        /// Interner, Assembly-übergreifender Parameter.
        /// Enthält das LogicalTaskTreeViewModel als IViewModelRoot, um hier einen direkten
        /// Assembly-Verweis auf Vishnu.ViewModel zu vermeiden.
        /// Wird innerhalb der Assembly Vishnu.ViewModel wieder aufgelöst.
        /// </summary>
        public IViewModelRoot? ViewModelRoot { get; set; }

        /// <summary>
        /// Liefert threadsafe Position und Maße das MainWindow.
        /// </summary>
        /// <returns>Bildschirminformationen zum MainWindow.</returns>
        public ScreenInfo ThreadAccessMainWindowScreenInfo()
        {
            return (ScreenInfo)Application.Current.Dispatcher.Invoke(
                new Func<ScreenInfo>(ThreadAccessMainWindowScreenInfoOnGuiDispatcher), DispatcherPriority.Normal);
        }

        private FrameworkElement? _parentView;
        private Point? _lastParentViewAbsoluteScreenPosition;

        /// <summary>
        /// Liefert threadsafe Position und Maße das MainWindow.
        /// </summary>
        /// <returns>Bildschirminformationen zum MainWindow.</returns>
        private ScreenInfo ThreadAccessMainWindowScreenInfoOnGuiDispatcher()
        {
            Window mainWindow = Application.Current.MainWindow;
            return ScreenInfo.GetActualScreenInfo(mainWindow);
        }
    }
}
