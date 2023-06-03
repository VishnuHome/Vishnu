using System;
namespace Vishnu.Interchange
{
    /// <summary>
    /// Interface für eine Klasse, die einen Prozess mehrfach anstoßen kann,
    /// z.B. TimerTrigger oder FileWatcherTrigger.
    /// </summary>
    /// <remarks>
    /// File: INodeTrigger.cs
    /// Autor: Erik Nagel
    ///
    /// 17.07.2013 Erik Nagel: erstellt
    /// </remarks>
    public interface INodeTrigger
    {
        /// <summary>
        /// Enthält Informationen zum besitzenden Trigger.
        /// Implementiert sind NextRun und NextRunInfo. Für das Hinzufügen weiterer
        /// Informationen kann diese Klasse abgeleitet werden.
        /// </summary>
        TriggerInfo? Info { get; set; }

        /// <summary>
        /// Startet den Trigger; vorher sollte sich der Consumer in TriggerIt eingehängt haben.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das Trigger.Start aufruft.</param>
        /// <param name="triggerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        /// <returns>True, wenn der Trigger durch diesen Aufruf tatsächlich gestartet wurde.</returns>
        bool Start(object? triggerController, object? triggerParameters, Action<TreeEvent> triggerIt);

        /// <summary>
        /// Stoppt den Trigger.
        /// </summary>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        /// <param name="triggerController">Das Objekt, das Trigger.Stop aufruft.</param>
        void Stop(object? triggerController, Action<TreeEvent> triggerIt);
    }
}
