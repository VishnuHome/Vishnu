namespace Vishnu.Interchange
{
    /// <summary>
    /// Eine Klasse, die als Worker-Prozess (Aktion bei TreeEvent, z.B. Logical-Änderung) 
    /// in einem Endknoten eines LogicalTaskTree arbeiten soll,
    /// muss dieses Interface implementieren.
    /// </summary>
    public interface INodeWorker
    {
        /// <summary>
        /// Startet einen zuständigen Worker, nachdem sich ein definierter Zustand
        /// (TreEvent) im Tree geändert hat.
        /// Intern wird für jeden Exec eine eigene Task gestartet.
        /// </summary>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="nodeId">Id des Knotens, zu dem der Worker gehört.</param>
        /// <param name="eventParameters">Klasse mit Informationen zum Ereignis.</param>
        /// <param name="isResetting">True, wenn der Worker mit einem letzten Aufruf (ok-Meldung) beendet werden soll.</param>
        void Exec(TreeParameters treeParameters, string nodeId, TreeEvent eventParameters, bool isResetting);
    }
}
