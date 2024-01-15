using NetEti.ApplicationControl;
using NetEti.MultiScreen;
using System;
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
        public FrameworkElement? ParentView { get; set; }

        /// <summary>
        /// Absolute Bildschirmposition des beinhaltenden Controls.
        /// </summary>
        /// <returns>Absolute Bildschirmposition der Mitte des Parent-Controls.</returns>
        public virtual Point GetParentViewAbsoluteScreenPosition()
        {
            if (this.ParentView == null)
            {
                // Tritt auf, wenn der feuernde Knoten selbst nicht sichtbar ist,
                // also außerhalb des Bildschirms (clipping) oder in einem collapsed SubJob.
                return GetFallbackScreenPosition();
            }
            if (!this.ParentView.Dispatcher.CheckAccess())
            {
                return (Point)this.ParentView.Dispatcher.Invoke(new Func<Point>(GetParentViewAbsoluteScreenPosition),
                    DispatcherPriority.Normal);
            }
            Rect rect = this.ParentView.GetAbsolutePlacement();
            if (!rect.IsEmpty)
            {
                Point point = new Point(rect.Left, rect.Top);
                return NetEti.MultiScreen.ScreenInfo.ClipToAllScreens(point, 50, 150);
            }
            else
            {
                // kann vorkommen: -job=%Vishnu_Root%/VishnuHome/Tests/TestJobs/HeavyDuty/check_Treshold_50_100
                //                 --StartWithJobs=true
                //                 dann sofort ins 'Logical Task Tree'-Fenster umschalten und z.B. Node2 neu starten.
                // Zusatzhinweis:  die DispatcherPriority bei this.ParentView.Dispatcher.Invoke zu ändern, bringt nichts!
                return GetFallbackScreenPosition();
            }
        }

        /// <summary>
        /// Notfallroutine - wird angesprungen, wenn für den aktuellen Knoten
        /// keine Bildschirmposition ermittelt werden kann.
        /// Liefert die zum MainWindow relative Position auf dem aktuellen Bisldschirm.
        /// </summary>
        /// <returns>Zum MainWindow relative Position auf dem aktuellen Bisldschirm.</returns>
        public virtual Point GetFallbackScreenPosition()
        {
            /*
            // Zweitbeste Lösung: auf dem aktuellen Bildschirm zentrieren:
            ScreenInfo mainWindowScreenInfo = ScreenInfo.GetMainWindowScreenInfo();
            Point center = 
                new Point(mainWindowScreenInfo.Bounds.X + mainWindowScreenInfo.WorkingArea.Width / 2.0,
                mainWindowScreenInfo.Bounds.Y + mainWindowScreenInfo.WorkingArea.Height / 2.0);
            */

            // Besser: relativ zum MainWindow zentrieren:
            Rect actMainWindowMeasures = WindowAspects.GetMainWindowMeasures();
            double left = actMainWindowMeasures.Left;
            double top = actMainWindowMeasures.Top;
            double width = actMainWindowMeasures.Width;
            double height = actMainWindowMeasures.Height;
            Point point = new Point(left + width / 2.0, top + height / 2.0);
            return NetEti.MultiScreen.ScreenInfo.ClipToAllScreens(point, 50, 150);
        }

        /// <summary>
        /// Konstruktor - übernimmt das Parent-Control.
        /// </summary>
        /// <param name="name">Der logische Name des Trees.</param>
        /// <param name="parentView">Das Parent-Control.</param>
        public TreeParameters(string name, FrameworkElement? parentView)
        {
            this.Name = name;
            this.ParentView = parentView; // hier immer null, wird später gesetzt.
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
