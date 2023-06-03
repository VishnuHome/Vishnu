using System;

namespace Vishnu.Interchange
{
    /// <summary>
    /// Enthält Informationen zum besitzenden Trigger.
    /// Implementiert ist NextRun. Für das Hinzufügen weiterer
    /// Informationen kann diese Klasse abgeleitet werden.
    /// </summary>
    /// <remarks>
    /// File: TriggerInfo.cs
    /// Autor: Erik Nagel
    ///
    /// 12.10.2014 Erik Nagel: erstellt
    /// </remarks>
    public class TriggerInfo
    {
        /// <summary>
        /// Der nächste Trigger-Zeitpunkt.
        /// </summary>
        public DateTime NextRun { get; set; }

        /// <summary>
        /// Info-Text über den nächsten Trigger-Zeitpunkt:
        /// bei TimerTriggern ein DateTime.Now.ToString();
        /// bei sonstigen Triggern eine andere geeignete Information.
        /// </summary>
        public string? NextRunInfo { get; set; }
    }
}
