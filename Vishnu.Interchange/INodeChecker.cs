using NetEti.Globals;
using System.ComponentModel;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Eine Klasse, die als Prüf-Prozess in einem Endknoten eines
    /// LogicalTaskTree arbeiten soll, muss dieses Interface implementieren.
    /// </summary>
    public interface INodeChecker
    {
        /// <summary>
        /// Kann aufgerufen werden, wenn sich der Verarbeitungs-Fortschritt
        /// des Checkers geändert hat, muss aber zumindest aber einmal zum
        /// Schluss der Verarbeitung aufgerufen werden.
        /// </summary>
        event ProgressChangedEventHandler NodeProgressChanged;

        /// <summary>
        /// Rückgabe-Objekt des Checkers.
        /// </summary>
        object? ReturnObject { get; set; }

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt (oder beobachtet).
        /// </summary>
        /// <param name="checkerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, False oder null</returns>
        bool? Run(object? checkerParameters, TreeParameters treeParameters, TreeEvent source);

    }
}
