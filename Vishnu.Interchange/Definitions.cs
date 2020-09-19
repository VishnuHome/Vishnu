using System;

namespace Vishnu.Interchange
{
    #region definitions

    /// <summary>
    /// Verarbeitungszustände eines Knotens.
    /// </summary>
    [Flags]
    public enum NodeState
    {
        /// <summary>Interner Steuerungs-Zustand.</summary>
        Null = 0,
        /// <summary>Startbereit, Zustand nach Initialisierung.</summary>
        None = 1,
        /// <summary>Beschäftigt, wartet auf Starterlaubnis.</summary>
        Waiting = 2,
        /// <summary>Beschäftigt, arbeitet.</summary>
        Working = 4,
        /// <summary>Startbereit, ist beendet.</summary>
        Finished = 8,
        /// <summary>Ist zwar nicht busy aber Timer-gesteuert.</summary>
        Triggered = 16,
        /// <summary>Interner Fehler (für DEBUG-Zwecke).</summary>
        InternalError = 128,
        /// <summary>Ist fertig oder wartet auf den nächsten Start durch den Timer.</summary>
        Ready = Finished | Triggered,
        /// <summary>Startbereit</summary>
        CanStart = None | Ready,
        /// <summary>Nicht startbereit, wartet oder arbeitet gerade</summary>
        Busy = Waiting | Working
    }

    /// <summary>
    /// Ergebnis-Zustände eines Knotens.
    /// </summary>
    [Flags]
    public enum NodeLogicalState
    {
        /// <summary>Interner, technischer Zustand.</summary>
        Undefined = 1,
        /// <summary>Zustand nach Initialisierung.</summary>
        None = 2,
        /// <summary>Zustand direkt vor dem Start.</summary>
        Start = 4,
        /// <summary>Ohne Fehler, Timeout oder Abbruch beendet.</summary>
        Done = 8,
        /// <summary>Mit Fehler beendet.</summary>
        Fault = 16,
        /// <summary>Mit Timeout beendet.</summary>
        Timeout = 32,
        /// <summary>Durch Benutzerabbruch beendet.</summary>
        UserAbort = 64
    }

    /// <summary>
    /// Zustand evtl. zugeordneter Worker (None, Valid, Invalid).
    /// </summary>
    public enum NodeWorkerState
    {
        /// <summary>Hat keine Worker zugeordnet.</summary>
        None,
        /// <summary>Hat gültige Worker zugeordnet.</summary>
        Valid,
        /// <summary>Hat mindestens einen ungültigen Worker zugeordnet.</summary>
        Invalid
    }

    /// <summary>
    /// Bestimmt die Ausrichtung bei der Darstellung der Elemente im Tree.
    /// </summary>
    public enum TreeOrientation
    {
        /// <summary>Alternierender Aufbau, waagerecht beginnend (Default).</summary>
        AlternatingHorizontal,
        /// <summary>Senkrechter Aufbau.</summary>
        Vertical,
        /// <summary>Waagerechter Aufbau.</summary>
        Horizontal,
        /// <summary>Alternierender Aufbau, senkrecht beginnend.</summary>
        AlternatingVertical
    }

    /// <summary>
    /// Verändert die Ausrichtung bei der Darstellung der Elemente im jeweiligen Control.
    /// </summary>
    public enum OrientationSwitch
    {
        /// <summary>Unverändert (Default).</summary>
        Unchanged,
        /// <summary>Horizontal wird zu Vertical und umgekehrt.</summary>
        Switched,
        /// <summary>Waagerechter Aufbau.</summary>
        Horizontal,
        /// <summary>Senkrechter Aufbau.</summary>
        Vertical
    }

    /// <summary>
    /// Kombinierbare Typenliste der Endknoten des Trees.
    /// </summary>
    [Flags]
    public enum NodeTypes
    {
        /// <summary>Alles wird durchgelassen.</summary>
        None = 1,
        /// <summary>NodeConnectoren.</summary>
        NodeConnector = 2,
        /// <summary>ValueModifier.</summary>
        ValueModifier = 4,
        /// <summary>JobConnectoren.</summary>
        JobConnector = 8,
        /// <summary>Konstanten.</summary>
        Constant = 16,
        /// <summary>Checker.</summary>
        Checker = 32,
        /// <summary>NodeList.</summary>
        NodeList = 64,
        /// <summary>JobList.</summary>
        JobList = 128,
        /// <summary>Snapshot.</summary>
        Snapshot = 256
    }

    /// <summary>
    /// Startverhalten von getriggerten Knoten
    /// beim Start durch den Anwender (UserRun):
    ///   None = kein direkter Start,
    ///   All = alle getriggerten Knoten innerhalb
    ///          eines durch UserRun gestarteten (Teil-)Trees starten direkt
    ///          (wie nicht getriggerte Knoten),
    ///   Direct = alle getriggerten Knoten starten direkt, wenn sie selbst
    ///          durch UserRun gestartet wurden.
    ///   NoTreeEvents = alles andere gilt nicht für durch TreeEvents getriggerte Knoten.
    /// </summary>
    [Flags]
    public enum TriggeredNodeStartConstraint
    {
        /// <summary>Getriggerte Knoten können nur durch ihren Trigger gestartet werden.</summary>
        None = 0,
        /// <summary>Getriggerte Knoten ohne TreeEventTrigger können direkt gestartet werden.</summary>
        NoTreeEvents = 1,
        /// <summary>Getriggerte Knoten können gestartet werden, wenn sie direkt ausgelöst werden.</summary>
        Direct = 2,
        /// <summary>Getriggerte Knoten können gestartet werden, wenn ihr (Teil-)Tree gestartet wurde.</summary>
        All = 4,
        /// <summary>Getriggerte Knoten können gestartet werden, wenn ihr (Teil-)Tree gestartet wurde und ihr Trigger kein TreeEventTrigger ist.</summary>
        AllNoTreeEvents = All | NoTreeEvents,
        /// <summary>Getriggerte Knoten können gestartet werden, wenn sie direkt ausgelöst werden und ihr Trigger kein TreeEventTrigger ist.</summary>
        DirectNoTreeEvents = Direct | NoTreeEvents
    }

    /// <summary>
    /// Schalter für Dialog-Verhalten.
    /// Die konkrete Ausformung der Dialoge wird in der jeweiligen Anwendungssituation festgelegt:
    /// None: kein Dialog,
    /// Info: Es erfolgt eine Meldung, die nur bestätigt werden kann.
    /// Question: Es erfolgt eine Meldung, in der eine Auswahl getroffen werden kann (i.d.R. Ja/Nein).
    /// </summary>
    public enum DialogSettings
    {
        /// <summary>Kein Dialog.</summary>
        None,
        /// <summary>Es erfolgt eine Meldung, die nur bestätigt werden kann.</summary>
        Info,
        /// <summary>Es erfolgt eine Meldung, in der eine Auswahl getroffen werden kann (i.d.R. Ja/Nein).</summary>
        Question
    }

    #endregion definitions
}
