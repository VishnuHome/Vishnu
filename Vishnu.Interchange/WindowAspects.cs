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
            return (Rect)System.Windows.Application.Current.Dispatcher.Invoke(
                new Func<Rect>(ThreadAccessMainWindowMeasuresOnGuiDispatcher), DispatcherPriority.Normal);
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
