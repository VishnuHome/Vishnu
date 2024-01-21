using NetEti.MultiScreen;
using System.Windows.Threading;
using System.Windows;
using System;

namespace Vishnu.Interchange
{

    /// <summary>
    /// Funktion: Datenklasse mit wesentlichen Darstellungsmerkmalen eines WPF-Windows.
    /// </summary>
    /// <remarks>
    /// File: WindowAspects
    /// Autor: Erik Nagel
    ///
    /// 22.02.2019 Erik Nagel: erstellt
    /// 08.01.2024 Erik Nagel: WindowLeft und WindowTop durch WindowCenter ersetzt.
    /// </remarks>
    public class WindowAspects
    {
        /// <summary>
        /// X-Koordinate der mittleren Bildschirmposition des Fensters (Einheit: geräteunabhängige Pixel).
        /// </summary>
        public double WindowCenterX { get; set; }

        /// <summary>
        /// Y-Koordinate der mittleren Bildschirmposition des Fensters (Einheit: geräteunabhängige Pixel).
        /// </summary>
        public double WindowCenterY { get; set; }

        /// <summary>
        /// Breite des Fensters (Einheit: geräteunabhängige Pixel).
        /// </summary>
        public double WindowWidth { get; set; }

        /// <summary>
        /// Höhe des Fensters (Einheit: geräteunabhängige Pixel).
        /// </summary>
        public double WindowHeight { get; set; }

        /// <summary>
        /// Geräteunabhängige Pixel, die der Fensterinhalt nach links verschoben ist (vorzeichenbehaftet).
        /// </summary>
        public double WindowScrollLeft { get; set; }

        /// <summary>
        /// Geräteunabhängige Pixel, die der Fensterinhalt nach oben verschoben ist (vorzeichenbehaftet).
        /// </summary>
        public double WindowScrollTop { get; set; }

        /// <summary>
        /// Vergrößerungsgrad des Fensterinhalts (vorzeichenbehaftet).
        /// </summary>
        public double WindowZoom { get; set; }

        /// <summary>
        /// True wenn mindestens ein Scrollbar sichtbar ist.
        /// </summary>
        public bool IsScrollbarVisible { get; set; }

        /// <summary>
        /// Der Index des Screens, in dem zum Zeitpunkt des Abspeicherns
        /// das MainWindow angezeigt wird.
        /// </summary>
        public int ActScreenIndex { get; set; }

        /// <summary>
        /// 0: Tree-Ansicht, 1: Jobs-Ansicht.
        /// </summary>
        public int ActTabControlTab { get; set; }

        /// <summary>
        /// Liefert threadsafe Position und Maße das MainWindow.
        /// </summary>
        /// <returns>Bildschirminformationen zum MainWindow.</returns>
        public static Rect GetMainWindowMeasures()
        {
            //return (Rect)System.Windows.Application.Current.Dispatcher.Invoke(
            //    new Func<Rect>(ThreadAccessMainWindowMeasuresOnGuiDispatcher), DispatcherPriority.Normal);
            return ThreadAccessMainWindowMeasuresOnGuiDispatcher();
        }

        /// <summary>
        /// Absolute Bildschirmposition der Mitte des zugehörigen Controls.
        /// </summary>
        /// <param name="frameworkElement">Das Element, dessen Position ermittelt werden soll.</param>
        /// <param name="failOnErrors">Bei false (default) wird bei NullReferenceException eine Ersatzposition zurück geliefert.</param>
        /// <returns>Absolute Bildschirmposition der Mitte des zugehörigen Controls.</returns>
        /// <exception cref="NullReferenceException">Tritt auf, wenn das Element selbst nicht sichtbar ist.</exception>
        public static Point GetFrameworkElementAbsoluteScreenPosition(
            FrameworkElement? frameworkElement, bool failOnErrors = false)
        {
            if (frameworkElement == null)
            {
                // Tritt auf, wenn der feuernde Knoten selbst nicht sichtbar ist,
                // also außerhalb des Bildschirms (clipping) oder in einem collapsed SubJob.
                if (!failOnErrors)
                {
                    return GetFallbackScreenPosition();
                }
                else
                {
                    throw new NullReferenceException(
                        "FrameworkElement ist null und failOnErrors ist true.");
                }
            }
            if (!frameworkElement.Dispatcher.CheckAccess())
            {
                return (Point)frameworkElement.Dispatcher.Invoke(
                    new Func<FrameworkElement?, bool, Point>(
                        GetFrameworkElementAbsoluteScreenPosition),
                        DispatcherPriority.Normal,
                        frameworkElement, failOnErrors);
            }
            Rect rect = frameworkElement.GetAbsolutePlacement();
            if (!rect.IsEmpty)
            {
                // Liefert den Mittelpunkt der ParentView.
                Point point = new Point(rect.Left + rect.Width / 2.0, rect.Top + rect.Height / 2.0);
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
        /// Liefert den Mittelpunkt des MainWindow auf dem aktuellen Bildschirm
        /// oder, wenn System.Windows.Application.Current auch null ist,
        /// dieMitte des ersten (Haupt-) Bildschirms.
        /// </summary>
        /// <returns>Mittelpunkt des MainWindow auf dem aktuellen Bildschirm oder Ersatzkoordinaten.</returns>
        private static Point GetFallbackScreenPosition()
        {
            // Besser: relativ zum MainWindow zentrieren:
            Rect actMainWindowMeasures;
            try
            {
                actMainWindowMeasures = WindowAspects.GetMainWindowMeasures();
            }
            catch (System.NullReferenceException)
            {
                // Kann vorkommen, wenn die die aufrufende Anwendung selbst kein
                // MainWindow hat (z.B. WPFDateDialogDemo).
                // Zweitbeste Lösung: auf dem aktuellen Bildschirm zentrieren:
                ScreenInfo mainWindowScreenInfo = ScreenInfo.GetFirstScreenInfo();
                actMainWindowMeasures = new Rect(
                    mainWindowScreenInfo.Bounds.X,
                    mainWindowScreenInfo.Bounds.Y,
                    mainWindowScreenInfo.WorkingArea.Width,
                    mainWindowScreenInfo.WorkingArea.Height);
            }
            double left = actMainWindowMeasures.Left;
            double top = actMainWindowMeasures.Top;
            double width = actMainWindowMeasures.Width;
            double height = actMainWindowMeasures.Height;
            Point center = new Point(left + width / 2.0, top + height / 2.0);
            return NetEti.MultiScreen.ScreenInfo.ClipToAllScreens(center, 50, 150);
        }

        /// <summary>
        /// Liefert threadsafe Position und Maße das MainWindow.
        /// </summary>
        /// <returns>Bildschirminformationen zum MainWindow.</returns>
        private static Rect ThreadAccessMainWindowMeasuresOnGuiDispatcher()
        {
            Window mainWindow = System.Windows.Application.Current.MainWindow;
            return new Rect(mainWindow.Left, mainWindow.Top, mainWindow.Width, mainWindow.Height);
        }
    }
}
