namespace Vishnu.Interchange
{
    /// <summary>
    /// Interface für eine Klasse, die Logging-Informationen
    /// annehmen und verarbeiten kann.
    /// </summary>
    /// <remarks>
    /// File: INodeLogger.cs
    /// Autor: Erik Nagel
    ///
    /// 25.07.2013 Erik Nagel: erstellt
    /// </remarks>
    public interface INodeLogger
    {
        /// <summary>
        /// Übernahme von diversen Logging-Informationen.
        /// </summary>
        /// <param name="loggerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="treeEvent">Klasse mit Informationen über das Ereignis.</param>
        /// <param name="additionalEventArgs">Enthält z.B. beim Event 'Exception' die zugehörige Exception.</param>
        void Log(object loggerParameters, TreeParameters treeParameters, TreeEvent treeEvent, object additionalEventArgs);
    }
}
