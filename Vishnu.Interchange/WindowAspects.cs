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
    /// </remarks>
    public class WindowAspects
    {
        /// <summary>
        /// Linke Bildschirmposition des Fensters (Einheit: geräteunabhängige Pixel).
        /// </summary>
        public double WindowLeft { get; set; }

        /// <summary>
        /// Obere Bildschirmposition des Fensters (Einheit: geräteunabhängige Pixel).
        /// </summary>
        public double WindowTop { get; set; }

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
        /// 0: Tree-Ansicht, 1: Jobs-Ansicht.
        /// </summary>
        public int ActTabControlTab { get; set; }
    }
}
