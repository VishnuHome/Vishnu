namespace Vishnu.Interchange
{
    /// <summary>
    /// Stellt die Funktion 'bool CanRun(...)' zur Verfügung.
    /// </summary>
    /// <remarks>
    /// File: ICanRun.cs
    /// Autor: Erik Nagel
    ///
    /// 30.05.2015 Erik Nagel: erstellt
    /// </remarks>
    public interface ICanRun
    {
        /// <summary>
        /// Wird von Vishnu vor jedem Run eines Checkers, Workers oder vor
        /// Start eines Triggers aufgerufen.
        /// Returnt true, wenn der Run/Start ausgeführt werden kann.
        /// </summary>
        /// <param name="parameters">Aufrufparameter des Benutzers.</param>
        /// <param name="treeParameters">Interne Parameter des Trees.</param>
        /// <param name="source">Aufrufendes TreeEvent.</param>
        /// <returns>True, wenn der Run/Start ausgeführt werden kann.</returns>
        bool CanRun(ref object? parameters, TreeParameters treeParameters, TreeEvent source);
    }
}
