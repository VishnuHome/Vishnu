using System;
using System.Threading;
using System.Threading.Tasks;
using NetEti.Globals;
using NetEti.ApplicationControl;
using System.Collections.Generic;
using System.Linq;
using Vishnu.Interchange;
using System.Collections.Concurrent;
using NetEti.MVVMini;
using LogicalTaskTree.Provider;
using System.Text;
// using System.Windows.Threading;

namespace LogicalTaskTree
{

    #region definitions

    /// <summary>
    /// Wird aufgerufen, wenn sich der Verarbeitungszustand eines Knotens geändert hat.
    /// </summary>
    public delegate void AllStatesChangedEventHandler();

    /// <summary>
    /// Wird aufgerufen, wenn sich das logische Ergebnis eines Knotens geändert hat.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    /// <param name="logical">True, False oder NULL</param>
    public delegate void LogicalChangedEventHandler(LogicalNode sender, bool? logical);

    /// <summary>
    /// Wird aufgerufen, wenn sich das logische Ergebnis eines Knotens geändert hat
    /// und ungleich null ist.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    /// <param name="logical">True oder False</param>
    /// <param name="eventId">Eindeutige Kennung des Ereignisses.</param>
    public delegate void LastNotNullLogicalChangedEventHandler(LogicalNode sender, bool? logical, Guid eventId);

    /// <summary>
    /// Wird aufgerufen, wenn sich der Verarbeitungszustand eines Knotens geändert hat.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    /// <param name="state">None, Waiting, Working, Finished, Busy (= Waiting | Working) oder CanStart (= None|Finished).</param>
    public delegate void StateChangedEventHandler(LogicalNode sender, NodeState state);

    /// <summary>
    /// Wird aufgerufen, wenn sich der Ergebnis-Zustand eines Knotens geändert hat.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    /// <param name="logicalState">None, Done, Fault, Timeout, UserAbort</param>
    public delegate void LogicalStateChangedEventHandler(LogicalNode sender, NodeLogicalState logicalState);

    /// <summary>
    /// Wird aufgerufen, wenn sich das Result eines Knotens geändert hat.
    /// Dient dazu, die Berechnung des logischen Zustands des übergeordneten
    /// Knotens neu zu starten.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    /// <param name="result">Das neue Result.</param>
    public delegate void ResultChangedEventHandler(LogicalNode sender, Result result);

    /// <summary>
    /// Wird aufgerufen, wenn eine Exception aufgetreten ist.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    /// <param name="exeption">Die aufgetretene Exception.</param>
    public delegate void ExceptionRaisedEventHandler(LogicalNode sender, Exception exeption);

    /// <summary>
    /// Wird aufgerufen, wenn eine Exception gelöscht wird.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    public delegate void NodeChangedEventHandler(LogicalNode sender);

    #endregion definitions

    /// <summary>
    /// Abstrakte Basisklasse für einen Knoten im LogicalTaskTree.
    /// </summary>
    /// <remarks>
    /// File: LogicalNode.cs
    /// Autor: Erik Nagel
    ///
    /// 01.12.2012 Erik Nagel: erstellt
    /// 06.08.2016 Erik Nagel: IVishnuNode implementiert.
    /// </remarks>
    public abstract class LogicalNode : GenericTree<LogicalNode>, IVishnuNode
    {

        #region events

        /// <summary>
        /// Wird aufgerufen, wenn sich der Verarbeitungszustand eines Knotens geändert hat.
        /// </summary>
        public static event AllStatesChangedEventHandler AllNodesStateChanged;

        /// <summary>
        /// Dieses Event aus IVishnuNode.INotifyPropertiesChanged kann von LogicalNodeViewmodel abonniert werden.
        /// Dieses erhält über die übergebenen PropertiesChangedEventArgs eine String-List mit Property-Namen
        /// und kann seinerseits über INotifyProperyChanged die UI informieren.
        /// </summary>
        public event PropertiesChangedEventHandler PropertiesChanged;

        /// <summary>
        /// Wird aufgerufen, wenn sich das logische Ergebnis eines Knotens geändert hat.
        /// </summary>
        public event LogicalChangedEventHandler NodeLogicalChanged;

        /// <summary>
        /// Wird aufgerufen, wenn sich das logische Ergebnis eines Knotens geändert hat
        /// und ungleich null ist.
        /// </summary>
        public event LastNotNullLogicalChangedEventHandler NodeLastNotNullLogicalChanged;

        /// <summary>
        /// Wird aufgerufen, wenn sich das Result eines Knotens geändert hat.
        /// Dient dazu, die Berechnung des logischen Zustands des übergeordneten
        /// Knotens neu zu starten.
        /// </summary>
        public event ResultChangedEventHandler NodeResultChanged;

        /// <summary>
        /// Wird aufgerufen, wenn sich der Verarbeitungszustand eines Knotens geändert hat.
        /// </summary>
        public event StateChangedEventHandler NodeStateChanged;

        /// <summary>
        /// Wird aufgerufen, wenn eine Exception aufgetreten ist.
        /// </summary>
        public event ExceptionRaisedEventHandler ExceptionRaised;

        /// <summary>
        /// Wird aufgerufen, wenn eine Exception gelöscht wird.
        /// </summary>
        public event NodeChangedEventHandler ExceptionCleared;

        /// <summary>
        /// Wird aufgerufen, wenn sich der Gesamtzustand der dem Knoten
        /// zugeordneten Worker geändert hat.
        /// </summary>
        public event NodeChangedEventHandler NodeWorkersStateChanged;

        /// <summary>
        /// Wird aufgerufen, wenn ein Knoten gestartet wurde.
        /// </summary>
        public event CommonProgressChangedEventHandler NodeProgressStarted;

        /// <summary>
        /// Wird aufgerufen, wenn sich der Verarbeitungs-Fortschritt eines Knotens geändert hat.
        /// </summary>
        public event CommonProgressChangedEventHandler NodeProgressChanged;

        /// <summary>
        /// Wird aufgerufen, wenn die Verarbeitung eines Knotens abgeschlossen wurde (unabhängig vom Ergebnis).
        /// </summary>
        public event CommonProgressChangedEventHandler NodeProgressFinished;

        #endregion events

        #region IVisnuNode implementation

        /// <summary>
        /// Die eindeutige Kennung des Knotens (identisch zur Property Id).
        /// </summary>
        public string IdInfo
        {
            get
            {
                return this.Id;
            }
        }

        /// <summary>
        /// "Menschenfreundliche" Darstellung des Knotens.
        /// </summary>
        public string NameInfo
        {
            get
            {
                return this.TreeParams.Name + ":" + this.Name;
            }
        }

        /// <summary>
        /// Der Pfad zum Knoten (identisch zur Property Path).
        /// </summary>
        public string PathInfo
        {
            get
            {
                return this.Path;
            }
        }

        /// <summary>
        /// Der Knotentyp:
        ///   None, NodeConnector, ValueModifier, Constant, Checker.
        /// <see cref="Vishnu.Interchange.NodeTypes"/>
        /// </summary>
        public NodeTypes TypeInfo
        {
            get
            {
                return this.NodeType;
            }
        }

        /// <summary>
        /// Die Hierarchie-Ebene des Knotens (identisch zur Property Level).
        /// </summary>
        public int LevelInfo
        {
            get
            {
                return this.Level;
            }
        }

        #endregion IVisnuNode implementation

        #region properties (alphabetic)

        /// <summary>
        /// Liefert true, wenn die Verarbeitung im Tree gerade angehalten wurde.
        /// </summary>
        public static volatile bool IsTreePaused;

        /// <summary>
        /// Liefert true, wenn die Verarbeitung im Tree gerade angehalten werden soll
        /// aber schon erzeugte logische Änderungen noch zuende verteilt werden.
        /// </summary>
        public static volatile bool IsTreeFlushing;

        /// <summary>
        /// Liefert true, wenn gerade keine Snapshots erlaubt sind.
        /// </summary>
        public static volatile bool IsSnapshotProhibited;

        /// <summary>
        /// Ein Teilbaum kann u.U. schon ein eindeutiges logisches Ergebnis haben,
        /// bevor alle Kinder ihre Verarbeitung beendet haben.
        /// Bei BreakWithResult=True werden diese dann abgebochen, wenn in dem
        /// bereffenden Ast keine Trigger aktiv sind.
        /// </summary>
        public bool BreakWithResult { get; set; }

        /// <summary>
        /// Gibt an, ob ein (Teil-)Baum gestartet werden kann, d.h. dass bei einem
        /// Knoten mit Kindern alle seine Kinder (rekursiv) gestartet werden können
        /// oder bei einem Endknoten (Checker) ohne Kinder dieser selbst gestartet werden kann.
        /// </summary>
        public bool CanTreeStart
        {
            get
            {
                bool canTreeStart = ((this.State & NodeState.CanStart) > 0 || ((this is NodeList) && this.State == NodeState.Working)) && this.LogicalState != NodeLogicalState.Start;
                // Bei NodeLists kann durch einen untergeordneten NodeConnector der State schon auf Working gehen, bevor der eigentliche Run auf díe NodeList erfolgt ist.
                // Deswegen ist hier bei NodeLists  der NodeState "Working" explizit auch zulässig (28.02.2016 Nagel).
                return canTreeStart;
            }
        }

        /// <summary>
        /// Bei True können zusätzliche Testausgaben erfolgen.
        /// Default: False.
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// Die eindeutige Kennung des Knotens.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Bei True werden alle Knoten im Tree resettet, wenn dieser Knoten gestartet wird.
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: false.
        /// </summary>
        public bool InitNodes { get; set; }

        /// <summary>
        /// Bei true befindet sich der Teilbaum/Knoten in aktivem (gestartetem) Zustand.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Bei true wird dieser Knoten als Referenzknoten angelegt, wenn irgendwo im Tree
        /// (nicht nur im aktuellen Job) der Name des Knotens schon gefunden wurde.
        /// Bei false wird nur im aktuellen Job nach gleichnamigen Knoten gesucht.
        /// Default: false.
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Returns true, wenn gerade eine vom User definierte Ruhezeit
        /// für Vishnu-Akteure (Checker) läuft.
        /// </summary>
        /// <returns>True, wenn gerade eine vom User definierte Ruhezeit
        /// für Vishnu-Akteure (Checker) läuft.</returns>
        public bool IsInSleepTime
        {
            get
            {
                return this._isInSleepTime;
            }
            set
            {
                if (this._isInSleepTime != value)
                {
                    this._isInSleepTime = value;
                }
                // Muss auf jeden Fall feuern, sonst bleibt beim Programmstart innerhalb der SleepTime
                // IsInSleepTime im MainWindow unbemerkt, da das MainWindow erst reagiert, nachdem
                // diese Property schon umgesetzt war und sich der value nicht mehr von _isSleepTime
                // unterscheidet.
                this.OnPropertiesChanged(new List<string> { "IsInSleepTime", "SleepTimeTo" });
            }
        }

        /// <summary>
        /// Bei True befindet sich diese LogicalNode innerhalb eines Snapshots.
        /// </summary>
        public bool IsInSnapshot { get; set; }

        /// <summary>
        /// Bei True dient dieser Knoten nur zur Anzeige und lässt keine weiteren Funktionen zu.
        /// </summary>
        public bool IsSnapshotDummy { get; set; }

        /// <summary>
        /// Beim letzten Lauf aufgetretene Exception oder null;
        /// </summary>
        public ConcurrentDictionary<LogicalNode, Exception> LastExceptions;

        /// <summary>
        /// Das letzte auslösende TreeEvent (bei TreeEvent-getriggerten Knoten)
        /// oder null.
        /// </summary>
        public TreeEvent LastExecutingTreeEvent { get; protected set; }

        /// <summary>
        /// Der letzte logische Zustand eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        public bool? LastLogical { get; set; }

        /// <summary>
        /// Der letzte Ergebniszustand eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        public NodeLogicalState LastLogicalState { get; set; }

        /// <summary>
        /// Merkfeld für den letzten Zustand von Logical, der nicht null war;
        /// wird benötigt, damit Worker nur dann gestartet werden, wenn sich
        /// der Zustand von Logical signifikant geändert hat und nicht jedesmal,
        /// wenn der Checker arbeitet (Logical = null).
        /// </summary>
        public bool? LastNotNullLogical { get; set; }

        /// <summary>
        /// Result für diesen Knoten.
        /// </summary>
        public abstract Result LastResult { get; set; }

        /// <summary>
        /// Zeitpunkt des letzten Starts des Knoten.
        /// </summary>
        public virtual DateTime LastRun
        {
            get
            {
                return this._lastRun;
            }
            set
            {
                this._lastRun = value;
            }
        }

        /// <summary>
        /// Der letzte Verarbeitungszustand eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        public NodeState LastState { get; set; }

        /// <summary>
        /// Die Hierarchie-Ebene des Knotens.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Optionaler zum globalen Sperren verwendeter Name.
        /// Wird verwendet, wenn ThreadLocked gesetzt ist.
        /// </summary>
        public string LockName
        {
            get
            {
                if (this._lockName != null)
                {
                    return this._lockName;
                }
                else
                {
                    if ((this.RootJobList != null) && (this.RootJobList != this) && this.RootJobList.LockName != null)
                    {
                        return this.RootJobList.LockName;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            set
            {
                this._lockName = value;
            }
        }

        /// <summary>
        /// Ein optionaler Logger, der bei bestimmten Ereignissen
        /// aufgerufen wird oder null.
        /// </summary>
        public LoggerShell Logger { get; set; }

        /// <summary>
        /// Der logische Zustand eines Knotens; hierum geht es letztendlich in der
        /// gesamten Verarbeitung.
        /// </summary>
        public abstract bool? Logical { get; set; }

        /// <summary>
        /// Der Ergebnis-Zustand des Knotens:
        /// None, Start, Done, Fault, Timeout, UserAbort.
        /// </summary>
        public virtual NodeLogicalState LogicalState
        {
            get
            {
                SleepIfNecessary();
                return this._logicalState;
            }
            set
            {
                if (this._logicalState != value)
                {
                    this._logicalState = value;
                    this.OnNodeLogicalStateChanged();
                }
            }
        }

        /// <summary>
        /// "Menschenfreundliche" Darstellung des Knotens.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Kombination aus Name und Id des Knotens.
        /// </summary>
        public string NameId
        {
            get
            {
                if (this.Name != this.Id)
                {
                    return String.IsNullOrEmpty(this.Name) ? this.Id : this.Name + "(" + this.Id + ")";
                }
                else
                {
                    return this.Id;
                }
            }
        }

        /// <summary>
        /// Zeitpunkt des nächsten Starts des Knotens (wenn bekannt) oder DateTime.MinValue.
        /// </summary>
        public virtual DateTime NextRun
        {
            get
            {
                if (this.Trigger != null && this.Trigger.Info != null)
                {
                    return this.Trigger.Info.NextRun;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
            set
            {
            }
        }

        /// <summary>
        /// Info-Text über den nächsten Start des Knotens (wenn bekannt) oder null.
        /// </summary>
        public virtual string NextRunInfo
        {
            get
            {
                if (!this.IsSnapshotDummy)
                {
                    if (this.Trigger != null && this.Trigger.Info != null)
                    {
                        return this.Trigger.Info.NextRunInfo;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return this._nextRunInfo;
                }
            }
            set
            {
                if (this.IsSnapshotDummy)
                {
                    this._nextRunInfo = value;
                }
            }
        }

        /// <summary>
        /// Der Knotentyp:
        ///   None, NodeConnector, ValueModifier, JobConnector, Constant, Checker, NodeList, JobList, Snapshot.
        /// </summary>
        public NodeTypes NodeType { get; internal set; }

        /// <summary>
        /// Das Parent-Control, in dem dieser Knoten dargestellt wird.
        /// </summary>
        public DynamicUserControlBase ParentView { get; set; }

        /// <summary>
        /// Der Pfad zum Knoten bestehend aus einer durch '/' getrennte Kette von NameIds:
        /// NameId ist Name + "(" + Id + ")" bei Knoten mit Name != null,
        ///        nur Id bei Name = null.
        /// </summary>
        public string Path
        {
            get
            {
                return this.Mother == null ? this.NameId : (this.Mother as LogicalNode).Path + '/' + this.NameId;
            }
        }

        /// <summary>
        /// Auf eine durch '/' getrennte Kette von Ids reduzierter Pfad zum Knoten.
        /// </summary>
        public string IdPath
        {
            get
            {
                return this.Mother == null ? this.Id : (this.Mother as LogicalNode).IdPath + '/' + this.Id;
            }
        }

        /// <summary>
        /// Id eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public virtual string ReferencedNodeId { get; set; }

        /// <summary>
        /// Name eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public virtual string ReferencedNodeName { get; set; }

        /// <summary>
        /// Pfad eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public virtual string ReferencedNodePath { get; set; }

        /// <summary>
        /// Anzahl der SingleNodes (letztendlich Checker) am Ende eines (Teil-)Baums.
        /// </summary>
        public abstract int SingleNodes { get; }

        /// <summary>
        /// Prozentwert für den Anteil der beendeten SingleNodes
        /// (letztendlich Checker) am Ende eines (Teil-)Baums.
        /// </summary>
        public abstract int SingleNodesFinished { get; }

        /// <summary>
        /// Der Beginn einer möglichen Ruhezeit.
        /// </summary>
        public TimeSpan SleepTimeFrom { get; set; }

        /// <summary>
        /// Das Ende einer möglichen Ruhezeit.
        /// </summary>
        public TimeSpan SleepTimeTo { get; set; }

        /// <summary>
        /// Bei True wird der Job beim Start zusammengeklappt angezeigt, wenn die UI dies unterstützt.
        /// </summary>
        public bool StartCollapsed { get; set; }

        /// <summary>
        /// Der Verarbeitungszustand eines Knotens:
        /// None, Waiting, Working, Finished, Triggered, Ready (= Finished | Triggered), CanStart (= None|Ready), Busy (= Waiting | Working).
        /// </summary>
        public abstract NodeState State { get; set; }

        /// <summary>
        /// Bei True wird jeder Thread über die Klasse gesperrt, so dass
        /// nicht Thread-sichere Checker serialisiert werden;
        /// Default: False;
        /// </summary>
        public bool ThreadLocked
        {
            get
            {
                return this._threadLocked || ((this.RootJobList != null) && (this.RootJobList != this) && this.RootJobList.ThreadLocked);
            }
            set
            {
                this._threadLocked = value;
            }
        }

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft
        /// oder null (setzt intern BreakWithResult außer Kraft).
        /// </summary>
        public INodeTrigger Trigger
        {
            get
            {
                return this._trigger;
            }
            set
            {
                if (this._trigger != value)
                {
                    this._trigger = value;
                }
            }
        }

        /// <summary>
        /// Verzögert den Start eines Knotens (und InitNodes).
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: 0 (Millisekunden).
        /// </summary>
        public int TriggeredRunDelay { get; set; }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public abstract string UserControlPath { get; set; }

        /// <summary>
        /// Ein Sammelstatus für alle zugeordneten Worker.
        /// </summary>
        public NodeWorkerState WorkersState { get; protected set; }

        #endregion properties (alphabetic)

        #region event publisher

        /// <summary>
        /// Löst das NodeStateChanged-Ereignis aus.
        /// </summary>
        internal static void OnAllNodesStateChanged()
        {
            if (AllNodesStateChanged != null)
            {
                AllNodesStateChanged();
            }
        }

        /// <summary>
        /// Verarbeitet die Änderung von Logical für diesen Knoten.
        /// Führt ggf. zugeordnete Worker aus.
        /// </summary>
        internal virtual void DoNodeLogicalChanged()
        {
            LogicalNode.WaitWhileTreePaused(); // vermeidet Deadlocks
            lock (this._nodeLogicalChangedLocker)
            {
                bool? tmpLogical = this.Logical; // mit Thread-entkoppelten
                bool? tmpLastNotNullLogical = this.LastNotNullLogical; // Kopien arbeiten
                int lastExceptionsCount = this.LastExceptions.Count;
                if (tmpLogical != tmpLastNotNullLogical && (tmpLogical != null || this.AppSettings.AcceptNullResults || lastExceptionsCount > 0 || this.IsFirstRun))
                {
                    if (!(this is JobList)) // Neu+- TODO: Lösung in den Derivaten suchen
                    {
                        this.AcceptNewLogical(tmpLogical);
                    }
                    else
                    {
                        // Hier folgt die LogicalChangedDelay-Verarbeitung.
                        if (!((this as JobList).IsChangeEventPending))
                        {
                            if (this.AppSettings.LogicalChangedDelay == 0)
                            {
                                this.AcceptNewLogical(tmpLogical);
                            }
                            else
                            {
                                ((this as JobList).IsChangeEventPending) = true;
                                Task.Factory.StartNew(() =>
                                {
                                    Thread.CurrentThread.Name = "LogicalChangeDelay";
                                    this.ValidateLastNotNullLogical(tmpLastNotNullLogical);
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Löst das NodeLogicalChanged-Ereignis aus.
        /// Dieses gibt die Änderung von Logical im Tree nach oben weiter.
        /// </summary>
        internal virtual void OnNodeLogicalChanged()
        {
            if (NodeLogicalChanged != null)
            {
                NodeLogicalChanged(this, this.Logical);
            }
        }

        /// <summary>
        /// Löst das PropertiesChanged-Ereignis aus.
        /// Dieses gibt die Namen von für die UI relevanten Properties an das ViewModel weiter.
        /// </summary>
        internal virtual void OnPropertiesChanged(List<string> propertyNames)
        {
            if (PropertiesChanged != null)
            {
                PropertiesChanged(this, new PropertiesChangedEventArgs(propertyNames));
            }
        }

        /// <summary>
        /// Löst das NodeStateChanged-Ereignis aus.
        /// </summary>
        internal virtual void OnNodeStateChanged()
        {
            if (NodeStateChanged != null)
            {
                NodeStateChanged(this, this.State);
            }
        }

        /// <summary>
        /// Löst das NodeResultChanged-Ereignis aus.
        /// </summary>
        internal virtual void OnNodeResultChanged()
        {
            if (NodeResultChanged != null)
            {
                NodeResultChanged(this, this.LastResult);
            }
        }

        /// <summary>
        /// Löst das NodeLogicalStateChanged-Ereignis aus.
        /// </summary>
        internal virtual void OnNodeLogicalStateChanged()
        {
            this.ProcessTreeEvent("LogicalStateChanged", null);
            this.OnNodeStateChanged();
        }

        /// <summary>
        /// Löst das ExceptionRaised-Ereignis aus.
        /// </summary>
        /// <param name="source">Die Quelle der Exception.</param>
        /// <param name="exception">Die Exception.</param>
        internal virtual void OnExceptionRaised(LogicalNode source, Exception exception)
        {
            lock (this.ExceptionLocker)
            {
                bool IsFirstException = this.LastExceptions.Count == 0;
                if (this.LastExceptions.ContainsKey(source))
                {
                    if (this.LastExceptions[source].GetType() != exception.GetType())
                    {
                        Exception dummyException = null;
                        this.LastExceptions.TryRemove(source, out dummyException);
                    }
                }
                if (!this.LastExceptions.ContainsKey(source))
                {
                    this.LastExceptions.TryAdd(source, exception);
                    if (IsFirstException)
                    {
                        this.ThreadUpdateLastLogicalState(NodeLogicalState.Fault);
                        this.LastNotNullLogical = null;
                        this.ThreadUpdateLastLogical(null);
                        this.Logical = null;
                        bool? dummy = this.Logical; // erzwingt eine Neuauswertung.
                        this.OnNodeLogicalStateChanged();
                        this.OnNodeLogicalChanged();
                        this.OnNodeResultChanged(); // 30.07.2018
                                                    //this.OnNodeStateChanged(); // 30.07.2018
                        this.OnNodeLastNotNullLogicalChanged(this, this.Logical, Guid.Empty);
                        this.ProcessTreeEvent(this, source, "AnyException", exception);
                        // 18.08.2018+ if (ExceptionRaised != null)
                        //{
                        //    ExceptionRaised(source, exception);
                        //}
                        // Das Ereignis muss in jedem Fall im Tree nach oben weitergegeben werden,
                        // deshalb hier auskommentiert und unterhalb dieses "if" eingefügt.
                        // Testfall für den Fehler ist "TestJobs/SnapshotTest/ShowRefAll".
                    }
                    if (ExceptionRaised != null)
                    {
                        ExceptionRaised(source, exception);
                    } // 18.08.2018-
                }
            }
        }

        /// <summary>
        /// Löscht Exceptions im Tree für diesen Knoten.
        /// </summary>
        /// <param name="source">Die Quelle der Exception.</param>
        internal virtual void OnExceptionCleared(LogicalNode source)
        {
            lock (this.ExceptionLocker)
            {
                if (this.LastExceptions.ContainsKey(source))
                {
                    Exception dummyException = null;
                    this.LastExceptions.TryRemove(source, out dummyException);
                    if (this.LastExceptions.Count == 0)
                    {
                        this.ThreadUpdateLastLogicalState(NodeLogicalState.Done);
                        this.ThreadUpdateLastLogical(null);
                        this.OnNodeLogicalStateChanged();
                        // 18.08.2018+ if (ExceptionCleared != null)
                        //{
                        //    ExceptionCleared(source); // 04.08.2018-
                        //}
                        // Das Ereignis muss in jedem Fall im Tree nach oben weitergegeben werden,
                        // deshalb hier auskommentiert und unterhalb dieses "if" eingefügt.
                        // Testfall für den Fehler ist "TestJobs/SnapshotTest/ShowRefAll".
                    }
                    if (ExceptionCleared != null)
                    {
                        ExceptionCleared(source);
                    } // 18.08.2018-
                }
            }
        }

        private string LastExceptionsToString()
        {
            return String.Join(",", this.LastExceptions.Keys.Select(n => n.Id).ToArray());
        }

        /// <summary>
        /// Löst das NodeStarted-Ereignis aus.
        /// </summary>
        /// <param name="source">Der Knoten , der das Event ausgelöst hat.</param>
        /// <param name="args">CommonProgressChangedEventArgs.</param>
        protected void OnNodeProgressStarted(object source, CommonProgressChangedEventArgs args)
        {
            this.ProcessTreeEvent("Started", null);
            if (NodeProgressStarted != null)
            {
                NodeProgressStarted(source, args);
            }
        }

        /// <summary>
        /// Löst das NodeProgressChanged-Ereignis aus.
        /// </summary>
        /// <param name="itemsName">Name für die Elemente, die für den Verarbeitungsfortschritt gezählt werden.</param>
        /// <param name="countAll">Gesamtanzahl - entspricht 100%.</param>
        /// <param name="countSucceeded">Erreichte Anzahl - kleiner-gleich 100%.</param>
        /// <param name="itemsType">Art der zu zählenden Elemente - Teile eines Ganzen, Ganze Elemente oder Element-Gruppen.</param>
        protected virtual void OnNodeProgressChanged(string itemsName, long countAll, long countSucceeded, ItemsTypes itemsType)
        {
            LogicalNode.WaitWhileTreePaused();
            if (this.IsThreadValid(Thread.CurrentThread))
            {
                CommonProgressChangedEventArgs args = new CommonProgressChangedEventArgs(itemsName, countAll, countSucceeded, itemsType, null);
                this.ProcessTreeEvent("ProgressChanged", args.ProgressPercentage);
                if (NodeProgressChanged != null)
                {
                    NodeProgressChanged(null, args);
                }
            }
        }

        /// <summary>
        /// Löst das NodeProgressFinished-Ereignis aus.
        /// </summary>
        /// <param name="itemsName">Name für die Elemente, die für den Verarbeitungsfortschritt gezählt werden.</param>
        /// <param name="countAll">Gesamtanzahl - entspricht 100%.</param>
        /// <param name="countSucceeded">Erreichte Anzahl - kleiner-gleich 100%.</param>
        /// <param name="itemsType">Art der zu zählenden Elemente - Teile eines Ganzen, Ganze Elemente oder Element-Gruppen.</param>
        public virtual void OnNodeProgressFinished(string itemsName, long countAll, long countSucceeded, ItemsTypes itemsType)
        {
            LogicalNode.WaitWhileTreePaused();
            if (this.IsThreadValid(Thread.CurrentThread))
            {
                this.ProcessTreeEvent("Finished", null);
                if (NodeProgressFinished != null)
                {
                    NodeProgressFinished(null, new CommonProgressChangedEventArgs(itemsName, countAll, countSucceeded, itemsType, null));
                }
            }
            this.UnMarkThreadAsInvalid(Thread.CurrentThread);
        }

        /// <summary>
        /// Löst das NodeBreaked-Ereignis aus.
        /// </summary>
        protected void OnNodeBreaked()
        {
            this.ProcessTreeEvent("Breaked", null);
            this.OnNodeStateChanged();
        }

        /// <summary>
        /// Löst das NodeLastNotNullLogicalChanged-Ereignis aus.
        /// </summary>
        /// <param name="source">Ein Knoten im Tree (LogicalNode).</param>
        /// <param name="logical">Logisches Ergebnis des Knotens.</param>
        /// <param name="eventId">Eindeutige Kennung des Events.</param>
        protected virtual void OnLastNotNullLogicalChanged(LogicalNode source, bool? logical, Guid eventId)
        {
            if (logical != null || this.AppSettings.AcceptNullResults) // 04.08.2018+-
            {
                this.ProcessTreeEvent(this, source, "LastNotNullLogicalChanged", null);
                if (this._lastProvidedEventId != eventId)
                {
                    this._lastProvidedEventId = eventId;
                }
            }
            if (logical == true)
            {
                this.ProcessTreeEvent(this, source, "LastNotNullLogicalToTrue", null);
            }
            else
            {
                if (logical == false)
                {
                    this.ProcessTreeEvent(this, source, "LastNotNullLogicalToFalse", null);
                }
            }
            this.RaiseNodeLastNotNullLogicalChangedWithTreeEvent(source, this.LastNotNullLogical, eventId);
        }

        /// <summary>
        /// Kapselt den Aufruf des nicht vererbbaren Events NodeLogicalChanged für
        /// für die abgeleiteten Klassen NodeList und JobList.
        /// Diese Routine gibt das Event über NodeLogicalChanged letzten Endes an die UI weiter.
        /// </summary>
        /// <param name="source">Die ursprüngliche Quelle der Events.</param>
        /// <param name="logical">Der logische Wert des Senders.</param>
        /// <param name="eventId">Eine optionale Guid zur eindeutigen Identifizierung des Events.</param>
        protected void RaiseNodeLogicalChanged(LogicalNode source, bool? logical, Guid eventId)
        {
            if (this.NodeLogicalChanged != null)
            {
                this.NodeLogicalChanged(source, logical);
            }
        }

        /// <summary>
        /// Triggert das TreeEvent "AnyLastNotNullLogicalHasChanged" und ruft "RaiseNodeLastNotNullLogicalChanged".
        /// </summary>
        /// <param name="source">Die ursprüngliche Quelle der Events.</param>
        /// <param name="lastNotNullLogical">Der logische Wert des Senders.</param>
        /// <param name="eventId">Eine optionale Guid zur eindeutigen Identifizierung des Events.</param>
        protected void RaiseNodeLastNotNullLogicalChangedWithTreeEvent(LogicalNode source, bool? lastNotNullLogical, Guid eventId)
        {
            if (lastNotNullLogical != null || this.AppSettings.AcceptNullResults) // 04.08.2018+-
            {
                this.ProcessTreeEvent(this, source, "AnyLastNotNullLogicalHasChanged", null);
            }
            this.OnNodeLastNotNullLogicalChanged(source, this.LastNotNullLogical, eventId);
        }

        /// <summary>
        /// Kapselt den Aufruf des nicht vererbbaren Events LastNotNullLogicalChanged für
        /// für die abgeleiteten Klassen NodeList und JobList.
        /// Diese Routine gibt das Event über NodeLogicalChanged letzten Endes an die UI weiter.
        /// </summary>
        /// <param name="source">Die ursprüngliche Quelle der Events.</param>
        /// <param name="lastNotNullLogical">Der logische Wert des Senders.</param>
        /// <param name="eventId">Eine optionale Guid zur eindeutigen Identifizierung des Events.</param>
        internal void OnNodeLastNotNullLogicalChanged(LogicalNode source, bool? lastNotNullLogical, Guid eventId)
        {
            if (NodeLastNotNullLogicalChanged != null)
            {
                NodeLastNotNullLogicalChanged(source, this.LastNotNullLogical, eventId);
            }
        }

        /// <summary>
        /// Löst das NodeWorkersStateChanged-Ereignis aus.
        /// </summary>
        protected void OnNodeWorkersStateChanged()
        {
            if (this.NodeWorkersStateChanged != null)
            {
                NodeWorkersStateChanged(this);
            }
        }

        #endregion event publisher

        #region public members

        /// <summary>
        /// Verhindert Snapshots.
        /// </summary>
        public static void ProhibitSnapshots()
        {
            LogicalNode.IsSnapshotProhibited = true;
        }

        /// <summary>
        /// Erlaubt Snapshots.
        /// </summary>
        public static void AllowSnapshots()
        {
            LogicalNode.IsSnapshotProhibited = false;
        }

        /// <summary>
        /// Hält die Verarbeitung im Tree an.
        /// </summary>
        public static void PauseTree()
        {
            LogicalNode.IsTreeFlushing = true;
            LogicalNode.OnAllNodesStateChanged();
            Thread.Sleep(HOLDEDTREELOOPSLEEPTIMEMILLISECONDS);
            Thread.Sleep(HOLDEDTREELOOPSLEEPTIMEMILLISECONDS);
            LogicalNode.IsTreePaused = true;
        }

        /// <summary>
        /// Lässt einen angehaltenen Tree weiterlaufen.
        /// </summary>
        public static void ResumeTree()
        {
            LogicalNode.IsTreeFlushing = false;
            LogicalNode.IsTreePaused = false;
            LogicalNode.OnAllNodesStateChanged();
        }

        /// <summary>
        /// Aktualisiert bei TreeEvent-getriggerten Knoten die Werte
        /// des Knotens, bevor mit ihnen weitergearbeitet wird.
        /// Ist bei Situationen wichtig, in dem entweder der Knoten das
        /// auslösende TreeEvent verpasst hat (beim Programmstart)
        /// oder ein anderes TreeEvent schneller war und zu einer
        /// Reaktion führt, die die aktuellen Werte des Knotens
        /// benötigt (JobSnapshotTrigger).
        /// </summary>
        /// <returns>Feuernder Knoten oder null.</returns>
        public LogicalNode GetlastEventSourceIfIsTreeEventTriggered()
        {
            LogicalNode source = null;
            if (this.Trigger != null && (this.Trigger as TriggerShell).HasTreeEventTrigger)
            {
                TreeEventTrigger trigger = (this.Trigger as TriggerShell).GetTreeEventTrigger();
                TreeEvent triggersLastTreeEvent = trigger.LastTreeEvent;
                string treeEventString = triggersLastTreeEvent == null ? "" : triggersLastTreeEvent.ToString();
                if (triggersLastTreeEvent != null) // prüft nur, ob der Trigger überhaupt schon mal gefeuert hat
                {
                    // TODO: Eventliste komplettieren, elegantere Lösung suchen

                    // 16.05.2019 Nagel+ diese Variante auskommentiert:
                    // source = this.FindNodeById(triggersLastTreeEvent.SourceId);
                    // Grund: TreeEventTrigger auf JobLists funktionierten bei mehrmaligem Run nicht mehr.
                    // Ursache war, dass immer der Zustand der Source des Events abgefragt wurde anstelle
                    // korrekterweise der Zustand des referenzierten Knoten(in diesem Falle der JobList)
                    // wie in der nachfolgenden Variante; Im Extremfall löste so der abfragende Knoten
                    // selbst die Joblist neu aus und fragte dann in Folge seinen eigenen Zustand ab
                    // (Test - Job: CheckAnyTreeEvent).
                    source = this.FindNodeById(trigger.ReferencedNodeId); // 16.05.2019 Nagel- dies ist die korrekte Abfrage
                    if ((trigger.InternalEvents.Contains("LastNotNullLogicalToTrue") && source?.LastNotNullLogical == true)
                        || (trigger.InternalEvents.Contains("LastNotNullLogicalToFalse") && source?.LastNotNullLogical == false)
                        || (trigger.InternalEvents.Contains("AnyException") && source?.LastExceptions.Count > 0)
                        || (trigger.InternalEvents.Contains("LastNotNullLogicalChanged"))
                        || (trigger.InternalEvents.Contains("Breaked") && source?.LastLogicalState == NodeLogicalState.UserAbort))
                    {
                        this.Logical = source.Logical;
                        this.LastExecutingTreeEvent = triggersLastTreeEvent;
                    }
                    else
                    {
                        this.LastExecutingTreeEvent = null;
                    }
                }
            }
            return source;
        }

        /// <summary>
        /// Gibt an, ob ein (Teil-)Baum in einem JobController gestartet werden kann, d.h. dass
        /// der Knoten und alle seine Eltern (rekursiv) gestartet werden können.
        /// </summary>
        /// <param name="calledFromRoot">Bei true wurde die Methode vom Root-Knoten aufgerufen.</param>
        /// <param name="collectedEnvironment">Dictionary mit Ergebnissen der dem Knoten vorausgegangenen Knoten.</param>
        /// <returns>True, wenn der Knoten gestartet werden kann.</returns>
        public bool CanControlledTreeStart(bool calledFromRoot, ResultDictionary collectedEnvironment)
        {
            /* TEST 02.02.2019 Nagel+
            if (!this.RootJobList.IsConrolled || calledFromRoot)
            {
                return this.CanTreeStart;
            }
            */ //TEST 02.02.2019 Nagel -
            if (calledFromRoot)
            {
                return this.CanTreeStart;
            }
            LogicalNode source = this.GetlastEventSourceIfIsTreeEventTriggered();
            // 02.02.2019 TEST Nagel+ if (this.Trigger == null || (this.LastExecutingTreeEvent != null && this.LastExecutingTreeEvent.Results != null)
            //   || this.RootJobList == this)
            if (this.Trigger == null
                || !((this.Trigger as TriggerShell).HasTreeEventTrigger)
                || (this.LastExecutingTreeEvent != null && this.LastExecutingTreeEvent.Results != null)
                || this.RootJobList == this) // 02.02.2019 TEST Nagel-
            {
                if (!(this == this.RootJobList))
                {
                    if (this.LastExecutingTreeEvent != null)
                    {
                        if (this.LastExecutingTreeEvent.Results != null)
                        {
                            foreach (KeyValuePair<string, Result> item in this.LastExecutingTreeEvent.Results)
                            {
                                collectedEnvironment.Add(item.Key, item.Value);
                            }
                        }
                    }
                    bool rtn;
                    if (source != null)
                    {
                        rtn = source.CanControlledTreeStart(false, collectedEnvironment); // Dem Weg der Events folgen.
                    }
                    else
                    {
                        rtn = this.RootJobList.CanControlledTreeStart(false, collectedEnvironment); // Im Tree aufwärts suchen.
                    }
                    if (this.LastExecutingTreeEvent == null)
                    {
                        this.LastExecutingTreeEvent = new TreeEvent("PseudoTreeEvent", this.Id, this.Id, this.Name, this.Path, this.LastNotNullLogical, this.LastLogicalState, null, null);
                    }
                    this.Environment = new ResultList();
                    foreach (KeyValuePair<string, Result> item in collectedEnvironment)
                    {
                        this.Environment.TryAdd(item.Key, item.Value);
                    }
                    this.LastExecutingTreeEvent.Environment = collectedEnvironment;
                    return rtn;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Statischer Konstruktor.
        /// </summary>
        static LogicalNode()
        {
            LogicalNode._invalidThreads = new ConcurrentDictionary<Thread, bool>();
            LogicalNode.IsSnapshotProhibited = false;
            LogicalNode.IsTreeFlushing = false;
            LogicalNode.IsTreePaused = false;
        }

        /// <summary>
        /// Konstruktor für ein Snapshot-Dummy-Element.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public LogicalNode(LogicalNode mother, JobList rootJobList, TreeParameters treeParams)
          : base(mother)
        {
            this.IsSnapshotDummy = true;
            this.TreeParams = treeParams;
            this.AppSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            this.DebugMode = this.AppSettings.DebugMode;
            this.RootJobList = rootJobList;
            this.TreeRootJobList = this.RootJobList.GetTopRootJobList();
            this.ExceptionLocker = new object();
            this.LastLogicalLocker = new object();
            this.SubLastNotNullLogicalLocker = new object();
            this.LastStateLocker = new object();
            this.LastLogicalStateLocker = new object();
            this._runLocker = new object();
            this.ResultLocker = new object();
            this._loggerLocker = new object();
            this.LastExceptions = new ConcurrentDictionary<LogicalNode, Exception>();
            this.IsInSnapshot = true;
            this.LastExecutingTreeEvent = null;
            this.Environment = new ResultList();
            this._threadStartLocker = new object();
            this._validateLastNotNullLogicalLocker = new object();
            this._nodeLogicalChangedLocker = new object();
            this.IsFirstRun = true;
            this.IsRunRequired = false;
            this.IsResetting = false;
            this.InitNodes = false;
            this.IsGlobal = false;
            this._parentViewLocker = new object();
            this.IsInSleepTime = false;
            this.SleepTimeFrom = new TimeSpan(0, 0, 0);
            this.SleepTimeFrom = new TimeSpan(0, 0, 0);
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id">Eindeutige Kennung des Knotens.</param>
        /// <param name="mother">Der Parent-Knoten.</param>
        /// <param name="rootJobList">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public LogicalNode(string id, LogicalNode mother, JobList rootJobList, TreeParameters treeParams)
          : base(mother)
        {
            this.IsSnapshotDummy = false;
            this.AppSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            this.DebugMode = this.AppSettings.DebugMode;
            this.Id = id;
            this.Level = mother == null ? 0 : mother.Level + 1;
            this.Name = id;
            this.Mother = mother;
            this.TreeParams = treeParams;
            if (rootJobList != null)
            {
                this.RootJobList = rootJobList;
            }
            else
            {
                this.RootJobList = (JobList)this;
            }
            this.TreeRootJobList = this.RootJobList == null ? (JobList)this : this.RootJobList.GetTopRootJobList();
            this.Trigger = null;
            this.IsTaskActiveOrScheduled = false;
            this.ExceptionLocker = new object();
            this.LastLogicalLocker = new object();
            this.LastLogicalStateLocker = new object();
            this.LastStateLocker = new object();
            this.SubLastNotNullLogicalLocker = new object();
            this._runLocker = new object();
            this._runAsyncLocker = new object();
            this.ResultLocker = new object();
            this._loggerLocker = new object();
            this._threadStartLocker = new object();
            this._validateLastNotNullLogicalLocker = new object();
            this._nodeLogicalChangedLocker = new object();
            this.ThreadUpdateLastLogical(null);
            this.ThreadUpdateLastState(NodeState.Null);
            this.ThreadUpdateLastLogicalState(NodeLogicalState.None);
            this.LastExceptions = new ConcurrentDictionary<LogicalNode, Exception>();
            this.IsInSnapshot = false;
            this.LastExecutingTreeEvent = null;
            this.Environment = new ResultList();
            this.IsFirstRun = true;
            this.IsRunRequired = false;
            this.IsResetting = false;
            this.InitNodes = false;
            this.IsGlobal = false;
            this._parentViewLocker = new object();
            if (this.Id != null)
            {
                this.SetWorkersState(null);
            }
            this.SleepTimeFrom = this.AppSettings.SleepTimeFrom;
            this.SleepTimeTo = this.AppSettings.SleepTimeTo;
        }

        /// <summary>
        /// Löscht interne Caches, so dass alles neu ausgewertet wird.
        /// </summary>
        public virtual void Invalidate()
        {
        }

        /// <summary>
        /// Setzt bestimmte Eigenschaften auf die Werte der übergebenen LogicalNode "source". 
        /// </summary>
        /// <param name="source">LogicalNode mit zu übernehmenden Eigenschaften.</param>
        public virtual void InitFromNode(LogicalNode source)
        {
        }

        /// <summary>
        /// Sorgt für eine sofortige Neu-Auswertung aller gecashten Zustände.
        /// </summary>
        public virtual void Refresh()
        {
            this.OnNodeStateChanged();
        }

        /// <summary>
        /// Startet die Verarbeitung dieses Knotens nach einem Start
        /// durch den Anwender. Gibt die Information, dass der Start
        /// durch den Anwender erfolgte, im TreeEvent an Run weiter.
        /// </summary>
        public virtual void UserRun()
        {
            this.Break(false);
            this.RegisterTriggeredNodes();
            if (this.LastExecutingTreeEvent != null)
            {
                this.LastExecutingTreeEvent.Name = "UserRun";
                this.LastExecutingTreeEvent.SenderId = this.Id;
            }
            else
            {
                this.LastExecutingTreeEvent = new TreeEvent("UserRun", this.Id, this.Id, this.Name, this.Path, null, NodeLogicalState.None, null, null);
            }
            this.Run(this.LastExecutingTreeEvent);
        }

        /// <summary>
        /// Prüft, ob ein Knoten gestartet werden kann und startet dann den Knoten, seinen Trigger,
        /// oder beide (über StartNodeOrTrigger).
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent.</param>
        public virtual void Run(TreeEvent source)
        {
            this.ProcessTreeEvent(source.Name, source);
            if (this.IsSnapshotDummy)
            {
                this.OnNodeProgressStarted(this, new CommonProgressChangedEventArgs(this.Id, 100, 0, ItemsTypes.itemParts, null));
                return;
            }
            // Bei einem "UserRun" innerhalb eines controlled-Jobs werden alle Knoten des Jobs
            // vor dem Start auf Initialwerte zurückgesetzt. Das gilt auch bei nicht controlled-Jobs,
            // wenn in den AppSettings "InitAtUserRun" gesetzt ist (experimentell).
            // Wird nur von Knoten ausgelöst, die direkt selbst vom User gestartet wurden,
            // oder den Schalter "InitNodes" gesetzt haben (experimentell).
            if (source.Name == "UserRun" /* && (this.RootJobList.IsConrolled || this.AppSettings.InitAtUserRun) */
                && (source.SenderId == this.Id || this.InitNodes))
            {
                /* 15.02.2019 Nagel+
                if (this.RootJobList.IsConrolled)
                {
                    this.ResetAllTreeNodes();
                }
                else
                {
                    // 10.07.2016 Nagel+: Init des Teilbaums anstatt nur des Einzelknotens.
                    //this.initNode(0, this);
                    this.ResetPartTreeNodes(this); // 10.07.2016 Nagel-
                }
                */ // 15.02.2019 Nagel-
                //this.ResetAllTreeEventTriggeringNodes();
                this.ResetPartTreeNodes(this); // 15.02.2019 Nagel+-
            }
            bool canRun = this.CanRun(source);
            if (canRun)
            {
                if (source.Name == "UserRun")
                {
                    // "UserRun": den Knoten auf startbar setzen, egal, wie er vorher stand.
                    if (this.LastExceptions?.Count == 0)
                    {
                        this.ThreadUpdateLastLogicalState(NodeLogicalState.Done);
                        this.LogicalState = NodeLogicalState.Done;
                    }
                    else
                    {
                        this.ThreadUpdateLastLogicalState(NodeLogicalState.Fault);
                        this.LogicalState = NodeLogicalState.Fault;
                    }
                    this.State = NodeState.None; // 03.06.2018 Nagel
                }
                else
                {
                    // "Normaler Run": Wenn in AppSettings der Schalter "BreakTreeBeforeStart" (experimentell) gesetzt ist,
                    // wird jeder Knoten des Teilbaums vor jedem Run abgebrochen, wenn er nicht schon abgebrochen ist.
                    if (this.AppSettings.BreakTreeBeforeStart && !(this.LastLogicalState == NodeLogicalState.UserAbort))
                    {
                        this.Break(false);
                        // nach Break wieder auf startbar zurücksetzen.
                        this.State = NodeState.None; // 03.06.2018 Nagel
                        this.ThreadUpdateLastLogicalState(NodeLogicalState.Done);
                        this.LogicalState = NodeLogicalState.Done;
                    }
                }
            }
            TriggerShell triggerShell = this.Trigger == null ? null : this.Trigger as TriggerShell;
            //if (this.Trigger != null && triggerShell.HasTreeEventTrigger && !triggerShell.GetTreeEventTrigger().IsActive)
            if (this.Trigger != null && triggerShell.HasTreeEventTrigger)
            {
                // TreeEventTrigger werden bei UserRun auf jeden Fall gestartet.
                triggerShell.Start(this, null, this.RunAsyncAsync);
                if (this.LastExecutingTreeEvent == null || this.LastExecutingTreeEvent.Name == "UserRun")
                {
                    TreeEventTrigger trigger = triggerShell.GetTreeEventTrigger();
                    if (trigger.LastTreeEvent != null && canRun)
                    {
                        this.RunAsyncAsync(trigger.LastTreeEvent);
                    }
                }
            }
            if (canRun)
            {
                this.StartNodeOrTrigger(source);
            }
            else
            {
                this.OnNodeProgressStarted(this, new CommonProgressChangedEventArgs(this.Id, 100, 0, ItemsTypes.itemParts, null));
            }
        }

        /// <summary>
        /// Registriert alle getriggerten Knoten eines Teilbaums bei ihren Triggern.
        /// </summary>
        public virtual void RegisterTriggeredNodes()
        {
            if (this.Trigger != null && this.Trigger is TriggerShell)
            {
                (this.Trigger as TriggerShell).RegisterTriggerIt(this.Path, this);
            }
            if (this.Children != null)
            {
                foreach (LogicalNode child in this.Children)
                {
                    child.RegisterTriggeredNodes();
                }
            }
        }

        /// <summary>
        /// Setzt den Teilbaum auf nicht startbar.
        /// </summary>
        public virtual void UnregisterTriggeredNode()
        {
            if (this.Trigger != null && this.Trigger is TriggerShell && this.Path != null)
            {
                (this.Trigger as TriggerShell).UnregisterTriggerIt(this.Path);
            }
            LogicalNode.WaitWhileTreePaused(); // vermeidet Deadlocks
            this.Logical = null; // essenziell
        }

        /// <summary>
        /// Wird aufgerufen, wenn der Teilbaum vom Anwender bewusst gestoppt wurde.
        /// </summary>
        // public abstract void UserBreak()
        public virtual void UserBreak()
        {
            this.UserBreakedNodePath = this.Path;
            this.ProcessTreeEvent("UserBreak", this.Path);
            this.Break(true);
        }

        /// <summary>
        /// Wird aufgerufen, wenn der Teilbaum neu geladen werden soll.
        /// </summary>
        /// <returns>Die RootJobList des neu geladenen Jobs.</returns>
        public virtual JobList Reload()
        {
            this.ProcessTreeEvent("Reload", this.Path);
            return this.ReloadBranch();
        }

        private JobList ReloadBranch()
        {
            JobList rootJobList = this.GetTopRootJobList();
            InfoController.Say(String.Format($"#RELOAD# LogicalNode.ReloadBranch Id/Name: {this.IdInfo}, RootJobList: {rootJobList.IdInfo}"));
            TreeParameters shadowTreeParams = new TreeParameters(String.Format($"ShadowTree {LogicalTaskTree.TreeId++}"),
                this.TreeParams.ParentView) { CheckerDllDirectory = this.TreeParams.CheckerDllDirectory };

            JobList newBranch = new JobList(shadowTreeParams, new ProductionJobProvider());
            return newBranch;
        }

        /// <summary>
        /// Wenn erforderlich, beim Trigger abmelden,
        /// Abbrechen der Task über CancellationToken, Status setzen.
        /// </summary>
        /// <param name="userBreak">Bei True hat der Anwender das Break ausgelöst.</param>
        public virtual void Break(bool userBreak)
        {
            if (this.Trigger != null && (userBreak || !(this.Trigger as TriggerShell).HasTreeEventTrigger))
            {
                this.Trigger.Stop(this, this.RunAsyncAsync);
            }
            if (userBreak)
            {
                this.UnregisterTriggeredNode();
            }
            if (this.IsSnapshotDummy)
            {
                return;
            }
            //if (this.RootJobList.IsConrolled)
            //{
            //  // TODO: besseren Weg finden.
            //  this.LastNotNullLogical = null;
            //}
            this._nextLogicalBreakState = userBreak ? NodeLogicalState.UserAbort : NodeLogicalState.None;
            if (this._nodeCancellationTokenSource != null && !this._nodeCancellationTokenSource.IsCancellationRequested)
            {
                this._nodeCancellationTokenSource.Cancel();
            }
            else
            {
                this.CancelNotification();
            }
        }

        /// <summary>
        /// Liefert die für den Knoten gültige, oberste Root-JobList.
        /// </summary>
        /// <returns>Die für den Knoten gültige, oberste Root-JobList.</returns>
        public virtual JobList GetTopRootJobList()
        {
            return this.RootJobList;
        }

        /// <summary>
        /// Stößt weitere Verarbeitungen für das aktuelle TreeEvent an (Trigger, Logger).
        /// </summary>
        /// <param name="eventName">Name des Events, das verarbeitet werden soll.</param>
        /// <param name="addInfo">Zusätzliche Information (Exception, Progress%, etc.).</param>
        public void ProcessTreeEvent(string eventName, object addInfo)
        {
            this.ProcessTreeEvent(this, this, eventName, addInfo);
        }

        /// <summary>
        /// Setzt die Property StartCollapsed für einen ganzen (Teil-)Baum.
        /// </summary>
        /// <param name="value">Neuer Wert für StartCollapsed.</param>
        public void SetTreeCollapsed(bool value)
        {
            this.StartCollapsed = value;
            foreach (LogicalNode child in this.Children)
            {
                child.SetTreeCollapsed(value);
            }
        }

        /// <summary>
        /// Returniert das ConcurrentDictionary ResultList als einfaches Dictionary Results.
        /// Benutzt dazu die interne Routine GetResultsFromResultList().
        /// </summary>
        /// <returns>Dictionary mit NodeIds und Results</returns>
        public ResultDictionary GetResults()
        {
            return this.GetResultsFromResultList();
        }

        /// <summary>
        /// Returniert das ConcurrentDictionary Environment als einfaches Dictionary Results.
        /// Benutzt dazu die interne Routine GetResultsFromResultList().
        /// </summary>
        /// <returns>Dictionary mit NodeIds und Results</returns>
        public ResultDictionary GetEnvironment()
        {
            return this.GetResultsFromEnvironment();
        }

        /// <summary>
        /// Nächsthöhere JobList für diesen Knoten oder der Knoten selbst,
        /// wenn er eine JobList ist.
        /// </summary>
        public JobList RootJobList;

        /// <summary>
        /// Oberste JobList.
        /// </summary>
        public JobList TreeRootJobList;

        /// <summary>
        /// Zusätzliche Parameter, die für den gesamten Tree Gültigkeit haben oder null.
        /// </summary>
        public TreeParameters TreeParams { get; private set; }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Verkettete Properties als String.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(String.Format($"{this.NodeType}: {this?.NameId ?? ""}"));
            if (this.Children.Count > 0)
            {
                stringBuilder.AppendLine(String.Format($"    Children: {this.Children.Count}"));
            }
            if (this.IsSnapshotDummy)
            {
                stringBuilder.AppendLine(String.Format($"    IsSnapshotDummy"));
            }
            stringBuilder.AppendLine(String.Format($"    Path: {this.Path ?? ""}"));
            stringBuilder.AppendLine(String.Format($"    UserControlPath: {this.UserControlPath ?? ""}"));
            if (this.Trigger != null && this.Trigger is TriggerShell)
            {
                stringBuilder.AppendLine(String.Format(
                    $"    Trigger: {(this.Trigger as TriggerShell).TriggerParameters}"));
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieser LogicalNode nach logischen Gesichtspunkten
        /// mit dem Inhalt einer übergebenen LogicalNode.
        /// </summary>
        /// <param name="obj">Die LogicalNode zum Vergleich.</param>
        /// <returns>True, wenn die übergebene LogicalNode inhaltlich gleich dieser ist.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            if (String.IsNullOrEmpty(this.Path))
            {
                return false;
            }
            return (this.ToString() == obj.ToString());
        }

        /// <summary>
        /// Erzeugt einen Hashcode für diese LogicalNode.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        private const int HOLDEDTREELOOPSLEEPTIMEMILLISECONDS = 100;

        #endregion public members

        #region internal members

        /// <summary>
        /// Bei true verhält sich der Knoten wie beim ersten Start.
        /// </summary>
        internal bool IsFirstRun;

        /// <summary>
        /// True: der Knoten wird gerade auf Start zurückgesetzt.
        /// </summary>
        internal bool IsResetting;

        /// <summary>
        /// Pfad zu dem Knoten, dessen Verarbeitung gerade vom Anwender
        /// über die Oberfläche abgebrochen wurde.
        /// </summary>
        internal string UserBreakedNodePath { get; set; }

        /// <summary>
        /// Setzt für diesen Knoten von außen die Instanz der Basisklasse für SingleNodeViewModels.
        /// </summary>
        /// <param name="parentView"></param>
        internal void ThreadUpdateParentView(DynamicUserControlBase parentView)
        {
            lock (this._parentViewLocker)
            {
                this.ParentView = parentView;
            }
        }

        /// <summary>
        /// Sucht nach zuständigen WorkerShells mit deren zugeordneten Worker-Arrays für ein Event.
        /// </summary>
        /// <param name="eventName">Der Name des zugehörigen Events.</param>
        /// <param name="senderId">Die Id des Knotens, der das Events weitergibt.</param>
        /// <param name="sourceId">Die Id der Quelle des Events.</param>
        /// <param name="allEvents">Bei True werden alle Worker für einen Knoten unabhängig vom event zurückgegeben.</param>
        /// <returns>Eine Liste von für das Event zuständigen Worker-Arrays.</returns>
        internal virtual List<WorkerShell[]> FindEventWorkers(string eventName, string senderId, string sourceId, bool allEvents)
        {
            return this.RootJobList.FindEventWorkers(eventName, senderId, sourceId, allEvents);
        }

        /// <summary>
        /// Liefert eine Liste mit allen Result-Objekten des Teilbaums.
        /// </summary>
        /// <returns>Liste von Result-Objekten des Teilbaums.</returns>
        internal abstract ResultList GetResultList();

        /// <summary>
        /// Returniert das ConcurrentDictionary ResultList als einfaches Dictionary Results.
        /// </summary>
        /// <returns>ResultDictionary mit Ids und zugehörigen Results.</returns>
        internal ResultDictionary GetResultsFromResultList()
        {
            ResultList resultList = this.GetResultList();
            ResultDictionary results = new ResultDictionary();
            foreach (string id in resultList.Keys)
            {
                results.Add(id, resultList[id]);
            }
            return results;
        }

        /// <summary>
        /// Liste mit allen Result-Objekten der Vorgänger des Teilbaums.
        /// </summary>
        internal virtual ResultList Environment
        {
            get
            {
                return this._environment;
            }
            set
            {
                this._environment = value;
            }
        }

        /// <summary>
        /// Returniert das ConcurrentDictionary ResultList als einfaches Dictionary Results.
        /// </summary>
        /// <returns>ResultDictionary mit Ids und zugehörigen Results.</returns>
        internal ResultDictionary GetResultsFromEnvironment()
        {
            ResultList resultList = this.Environment;
            ResultDictionary results = new ResultDictionary();
            foreach (string id in resultList.Keys)
            {
                results.Add(id, resultList[id]);
            }
            return results;
        }

        /// <summary>
        /// Fügt dem Environment des aktuellen Knotens das Environment und die Results
        /// des aufrufenden Knotens hinzu.
        /// </summary>
        /// <param name="source">Einzufügendes TreeEvent.</param>
        internal void AddEnvironment(TreeEvent source)
        {
            if (source != null)
            {
                this.Environment.Clear();
                if (source.Environment != null)
                {
                    foreach (KeyValuePair<string, Result> item in source.Environment)
                    {
                        this.Environment.TryAdd(item.Key, item.Value);
                    }
                }
                if (source.Results != null)
                {
                    foreach (KeyValuePair<string, Result> item in source.Results)
                    {
                        this.Environment.AddOrUpdate(item.Key, item.Value,
                          new Func<string, Result, Result>((key, value) => { return value; }));
                    }
                }
            }
        }

        /// <summary>
        /// Setzt den Knoten auf Initialwerte zurück.
        /// Das entspricht dem Zustand beim ersten Starten des Trees.
        /// Bei Jobs mit gesetztem 'IsConrolled' ist das entscheidend,
        /// da sonst Knoten gestartet würden, ohne dass die Voraussetzungen
        /// erneut geprüft wurden.
        /// </summary>
        internal virtual void Reset()
        {
            this.Break(false);
            if (this._nodeCancellationTokenSource != null)
            {
                this._nodeCancellationTokenSource.Dispose();
                this._nodeCancellationTokenSource = new CancellationTokenSource();
                this.CancellationToken = this._nodeCancellationTokenSource.Token;
                this.CancellationToken.Register(() => CancelNotification());
            }
        }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Applikationseinstellungen.
        /// </summary>
        protected AppSettings AppSettings;

        /// <summary>
        /// Über die CancellationTokenSource kann dieses Token auf
        /// Abbruch gesetzt werden, was in diesem Knoten zum Aufruf
        /// der Routine cancelNotification führt.
        /// </summary>
        protected CancellationToken CancellationToken;

        /// <summary>
        /// True: der Knoten ist gerade aktiv oder durch einen Timer kontrolliert.
        /// </summary>
        protected bool IsTaskActiveOrScheduled;

        /// <summary>
        /// True: der Knoten soll gestartet werden.
        /// </summary>
        protected volatile bool IsRunRequired;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastLogical.
        /// </summary>
        protected object LastLogicalLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastLogical.
        /// </summary>
        protected object SubLastNotNullLogicalLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastState.
        /// </summary>
        protected object LastStateLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastLogicalState.
        /// </summary>
        protected object LastLogicalStateLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von Result.
        /// </summary>
        protected object ResultLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren bei Exceptions.
        /// </summary>
        protected object ExceptionLocker;

        /// <summary>
        /// Dient zur Sperrung für Thread-safen Zugriff auf ParentView.
        /// </summary>
        protected object _parentViewLocker;

        /// <summary>
        /// Zeitpunkt des letzten Starts des Knoten (internes Feld).
        /// </summary>
        protected DateTime _lastRun;

        /// <summary>
        /// Info-Text über den nächsten Start des Knotens (wenn bekannt) oder null (internes Feld).
        /// </summary>
        protected string _nextRunInfo;

        /// <summary>
        /// Die eigentliche, Knotentyp-spezifische Verarbeitung;
        /// muss überschrieben werden.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        protected abstract void DoRun(TreeEvent source);

        /// <summary>
        /// Hierüber kann eine Ableitung von LogicalNode ihren eigenen Thread
        /// zum Abbruch veranlassen.
        /// </summary>
        /// <summary>
        /// Sucht in der aktuellen JobList und allen übergeodneten JobLists
        /// nach der (Single)Node mit der übergebenen 'nodeId'.
        /// Der erste Treffer gewinnt.
        /// </summary>
        /// <param name="nodeId">Die Id der zu suchenden SingleNode.</param>
        /// <returns>Die gefundene LogicalNode oder null.</returns>
        public virtual LogicalNode FindNodeById(string nodeId)
        {
            return this.RootJobList.FindNodeById(nodeId);
        }

        /// <summary>
        /// Sucht nach zuständigen Triggern für ein Event.
        /// </summary>
        /// <param name="eventName">Der Name des zugehörigen Events.</param>
        /// <param name="senderId">Die Id des Verbreiters des Events.</param>
        /// <param name="sourceId">Die Id der Quelle des Events.</param>
        /// <returns>Eine Liste von für das Event zuständigen Triggern.</returns>
        protected virtual List<TreeEventTrigger> FindEventTriggers(string eventName, string senderId, string sourceId)
        {
            return this.RootJobList.FindEventTriggers(eventName, senderId, sourceId);
        }

        /// <summary>
        /// Setz threadsafe LastLogical.
        /// </summary>
        /// <param name="newLogical">Neuer Wert (bool?)</param>
        protected void ThreadUpdateLastLogical(bool? newLogical)
        {
            lock (this.LastLogicalLocker)
            {
                if (newLogical != this.LastLogical)
                {
                    this.LastLogical = newLogical;
                }
            }
        }

        /// <summary>
        /// Setz threadsafe LastState.
        /// </summary>
        /// <param name="newState">Neuer Wert (NodeLogicalState?)</param>
        protected void ThreadUpdateLastState(NodeState newState)
        {
            lock (this.LastStateLocker)
            {
                this.LastState = newState;
            }
        }

        /// <summary>
        /// Setz threadsafe LastLogicalState.
        /// </summary>
        /// <param name="newLogicalState">Neuer Wert (NodeLogicalState?)</param>
        protected virtual void ThreadUpdateLastLogicalState(NodeLogicalState newLogicalState)
        {
            this.LastLogicalState = newLogicalState;
        }

        /// <summary>
        /// Streut System.Sleeps zur Systementlastung ein.
        /// Ist feiner regulierbar, als fixe Sleeps in geschachtelten Inner-Loops.
        /// </summary>
        protected static void SleepIfNecessary()
        {
            if (--_countdownToSleep < 1)
            {
                Thread.Sleep(SLEEPMILLISECONDS);
                _countdownToSleep = COUNTSUNTILSLEEP;
            }
        }

        /// <summary>
        /// Setzt einen gemeinsamen (kombinierten) NodeWorkerState 'WorkersState'
        /// für alle NodeWorker.
        /// Retourniert NodeWorkersState.
        /// </summary>
        /// <param name="treeEvent">Das auslösende treeEvent (interner Name).</param>
        /// <returns>Kombinierter NodeWorkerState für alle NodeWorker.</returns>
        protected NodeWorkerState SetWorkersState(string treeEvent)
        {
            treeEvent = null; // 07.08.2018 Test+-
            this._workersState = NodeWorkerState.None;
            string key = this.Id + ":" + treeEvent ?? "";
            JobList meOrRoot = this.RootJobList;
            if (this is JobList)
            {
                meOrRoot = this as JobList;
            }
            foreach (WorkerShell worker in meOrRoot.GetWorkers(key))
            {
                if (this._workersState == NodeWorkerState.None || this._workersState == NodeWorkerState.Valid)
                {
                    this._workersState = worker.Exists() ? NodeWorkerState.Valid : NodeWorkerState.Invalid;
                }
                if (this._workersState == NodeWorkerState.Invalid)
                {
                    break;
                }
            }
            if (this._workersState == NodeWorkerState.Invalid)
            {
                this.WorkersState = NodeWorkerState.Invalid;
            }
            else
            {
                if (this._workersState == NodeWorkerState.Valid)
                {
                    this.WorkersState = NodeWorkerState.Valid;
                }
                else
                {
                    this.WorkersState = NodeWorkerState.None;
                }
            }
            this.OnNodeWorkersStateChanged();
            return this.WorkersState;
        }

        /// <summary>
        /// Trägt thread in die Liste ungültiger Threads ein,
        /// falls der Thread noch aktiv ist.
        /// </summary>
        /// <param name="thread">Ungültiger Thread.</param>
        protected void MarkThreadAsInvalidIfActive(Thread thread)
        {
            if (thread != null && thread.IsAlive)
            {
                if (thread.IsAlive)
                {
                    LogicalNode._invalidThreads.TryAdd(this._starterThread, true);
                }
                else
                {
                    bool dummy;
                    LogicalNode._invalidThreads.TryRemove(this._starterThread, out dummy); // 03.07.2018 Nagel: Test
                }
            }
        }

        /// <summary>
        /// Prüft, ob ein Thread gültig (nicht als ungültig markiert) ist.
        /// </summary>
        /// <param name="thread">Der zu prüfende Thread.</param>
        /// <returns>True, wenn der Thread gültig ist.</returns>
        protected bool IsThreadValid(Thread thread)
        {
            return !LogicalNode._invalidThreads.ContainsKey(thread);
        }

        /// <summary>
        /// Entfernt thread aus der Liste ungültiger Threads.
        /// </summary>
        /// <param name="thread">Der zu entfernende Thread.</param>
        protected void UnMarkThreadAsInvalid(Thread thread)
        {
            if (thread != null)
            {
                bool dummy;
                LogicalNode._invalidThreads.TryRemove(thread, out dummy);
            }
        }

        /// <summary>
        /// Setzt alle Knoten im gesamten Tree zurück.
        /// </summary>
        protected void ResetAllTreeNodes()
        {
            this.TreeRootJobList.Traverse(this.InitNode);
        }

        /// <summary>
        /// Setzt alle Knoten im Teilbaum ab branch zurück.
        /// </summary>
        /// <param name="branch">Der oberste Knoten des zurückzusetzenden Teilbaums.</param>
        protected virtual void ResetPartTreeNodes(LogicalNode branch)
        {
            branch.Traverse(this.InitNode);
        }

        /// <summary>
        /// Setzt alle Knoten im Teilbaum zurück, von denen andere per TreeEvent abhängen.
        /// </summary>
        protected virtual void ResetAllTreeEventTriggeringNodes()
        {
            Dictionary<string, List<LogicalNode>> triggeringTriggered = this.TreeRootJobList.GetTriggeringNodeIdAndTriggeredNodes();
            foreach (string triggeringNodeId in triggeringTriggered.Keys)
            {
                this.InitNode(0, FindNodeById(triggeringNodeId));
                //this.ResetPartTreeNodes(FindNodeById(triggeringNodeId));
            }
        }

        #endregion protected members

        #region private members

        const long COUNTSUNTILSLEEP = 10000;
        const int SLEEPMILLISECONDS = 10;
        const int MAXWAITINGLOOPS = 20;

        private static long _countdownToSleep = COUNTSUNTILSLEEP;
        private static ConcurrentDictionary<Thread, bool> _invalidThreads;

        // Die Verarbeitung eines Knotens läuft immer asynchron.
        // private Task asyncCheckerTask; Task geht nicht, wegen fehlendem ApartmentState.STA und WPF-Checkern.
        private Thread _starterThread;
        private NodeLogicalState _nextLogicalBreakState;
        private bool _threadLocked;
        private INodeTrigger _trigger;
        private Guid _lastProvidedEventId;
        private object _runLocker;
        private object _loggerLocker;
        private static object _eventLocker = new object();
        private object _runAsyncLocker;
        private ResultList _environment;
        private object _threadStartLocker;
        private object _validateLastNotNullLogicalLocker;
        private object _nodeLogicalChangedLocker;
        private bool _isInSleepTime;

        private CancellationTokenSource _nodeCancellationTokenSource { get; set; }
        /// <summary>
        /// Der Ergebnis-Zustand des Knotens:
        /// None, Start, Done, Fault, Timeout, UserAbort (internes Feld).
        /// </summary>
        private volatile NodeLogicalState _logicalState;

        /// <summary>
        /// Gruppenzustand für die dem Knoten zugeordneten Worker.
        /// </summary>
        private NodeWorkerState _workersState;

        private string _lockName;

        // Startet asynchron runAsync. Dieser Zwischenschritt wurde nötig, um Timer, auf die mehrere
        // Knoten verweisen, vom run eines Knotens zu entkoppeln (gleichzeitige Ausführung mehrerer runs).
        private void RunAsyncAsync(TreeEvent source)
        {
            if (this.AppSettings.IsInSleepTime != this._isInSleepTime)
            {
                this._isInSleepTime = this.AppSettings.IsInSleepTime;
                this.TreeRootJobList.IsInSleepTime = this._isInSleepTime;
                SnapshotManager.RequestSnapshot(this.RootJobList.GetTopRootJobList());
            }
            if (source?.Name != "UserRun" && this.IsInSleepTime)
            {
                return;
            }
            LogicalNode.WaitWhileTreePausedOrFlushing();
            lock (this._threadStartLocker)
            {
                this.LastExecutingTreeEvent = source;
                //* 14.08.2018+
                if ((this is SingleNode) && (this as SingleNode).Checker != null && (this as SingleNode).Checker.IsMirror)
                {
                    LogicalNode lookupSource = null;
                    int i = 0;
                    do
                    {
                        lookupSource = this.GetlastEventSourceIfIsTreeEventTriggered();
                        if (lookupSource == null)
                        {
                            Thread.Sleep(10);
                        }
                    } while (lookupSource == null && ++i < MAXWAITINGLOOPS); // 01.11.2021 Erik Nagel: Loop eingebaut.
                    if (i >= MAXWAITINGLOOPS)
                    {
                        throw new ApplicationException("lookupSource MAXWAITINGLOOPS überschritten!");
                    }
                    this.Logical = this.LastExecutingTreeEvent.Logical;
                    this.OnNodeLogicalChanged();
                }
                //14.08.2018- */
                if ((this.CancellationToken != null && this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    return;
                }
                this.IsRunRequired = true;
                // 11.05.2019 Test+ if (this._starterThread == null || !this._starterThread.IsAlive)
                //if (this._starterThread == null || !this._starterThread.IsAlive || !this.IsThreadValid(this._starterThread)) // 27.04.2019 TEST
                if (this._starterThread == null || !(this._starterThread.IsAlive && this.IsThreadValid(this._starterThread))) // 11.05.2019 TEST-
                {
                    // this.asyncCheckerTask = new Task(() => this.runAsync(source));
                    // Läuft über Thread um ApartmentState.STA setzen zu können. Ansonsten könnten
                    // keine WPF-Dialog-Checker ausgeführt werden.
                    if (this._starterThread != null)
                    {
                        this._starterThread.Abort();
                        this._starterThread = null;
                    }
                    this._starterThread = new Thread((te) =>
                    {
                        Thread.CurrentThread.Name = "tryRunAsyncWhileIsRunRequired";
                        this.TryRunAsyncWhileIsRunRequired(te as TreeEvent);
                    });
                    _starterThread.SetApartmentState(ApartmentState.STA); // Ist wegen WPF-Checkern erforderlich.
                    _starterThread.IsBackground = true;
                    try
                    {
                        _starterThread.Start(source);
                    }
                    catch (InvalidOperationException) { }
                }
                else
                {
                    if (!this.IsThreadValid(this._starterThread))
                    {
                        this.ThreadUpdateLastState(NodeState.Null);
                        this.State = NodeState.Waiting;
                        this.OnNodeStateChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Wartet, falls gerade keine Snapshots erlaubt sind, solange, bis die Sperre wieder aufgehoben wurde.
        /// </summary>
        internal static void WaitWhileSnapshotProhibited()
        {
            while (LogicalNode.IsSnapshotProhibited || LogicalNode.IsTreeFlushing || LogicalNode.IsTreePaused)
            {
                Thread.Sleep(LogicalNode.HOLDEDTREELOOPSLEEPTIMEMILLISECONDS);
            }
        }

        /// <summary>
        /// Wartet, falls der Tree gerade pausiert werden soll oder schon pausiert ist,
        /// solange, bis die Pause wieder aufgehoben wurde.
        /// </summary>
        internal static void WaitWhileTreePausedOrFlushing()
        {
            while (LogicalNode.IsTreeFlushing || LogicalNode.IsTreePaused)
            {
                Thread.Sleep(LogicalNode.HOLDEDTREELOOPSLEEPTIMEMILLISECONDS);
            }
        }

        /// <summary>
        /// Wartet, falls der Tree gerade pausiert wurde, solange, bis die Pause wieder aufgehoben wurde.
        /// </summary>
        internal static void WaitWhileTreePaused()
        {
            while (LogicalNode.IsTreePaused)
            {
                Thread.Sleep(LogicalNode.HOLDEDTREELOOPSLEEPTIMEMILLISECONDS);
            }
        }

        /*
        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.DataBind,
                new DispatcherOperationCallback(
                    delegate (object f)
                    {
                        ((DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
            Dispatcher.PushFrame(frame);
        }
        */

        /// <summary>
        /// Eigene (Timer-)Task Action für den Run eines (Teil-)Baums.
        /// Versucht, den Teil-Baum über runAsync zu starten, solange
        /// der Schalter IsRunRequired gesetzt ist.
        /// </summary>
        private void TryRunAsyncWhileIsRunRequired(TreeEvent source)
        {
            int waitingLoopCounter = 0;
            do
            {
                if ((this.CancellationToken != null && this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    break;
                }
                if ((this.State & NodeState.CanStart) > 0 && !(this.LogicalState == NodeLogicalState.Start))
                {
                    this.IsRunRequired = false;
                    // this.RunAsync(this.LastExecutingTreeEvent); // 13.08.2018
                    this.RunAsync(source);
                }
                Thread.Sleep(this.AppSettings.TryRunAsyncSleepTime);
                if (waitingLoopCounter++ > MAXWAITINGLOOPS)
                {
                    if (this is NodeParent || this.LogicalState == NodeLogicalState.Done)
                    {
                        this.State = NodeState.None; // TODO: State-Mischmasch sauber lösen.
                    }
                    else
                    {
                        InfoController.Say(String.Format($"#TRIGGER# X InternalError Id/Name: {this.IdInfo}, State: {this.State}, LogicalState: {this.LogicalState}"));
                        this.State = NodeState.InternalError;
                    }
                }
            } while (this.IsRunRequired);
        }

        /// <summary>
        /// Action für den Run eines (Teil-)Baums.
        /// </summary>
        private void RunAsync(TreeEvent source)
        {
            lock (this._runAsyncLocker)
            {
                int emergencyHalt = 0;
                while ((this.State & (NodeState.CanStart | NodeState.Working)) == 0
                  && !this.CancellationToken.IsCancellationRequested) // Warten wegen NodeConnectoren
                { // Achtung: wichtig ist, dass auch NodeState.Working abgeprüft wird, da sonst DialogChecker auf Exceptions laufen.
                    Thread.Sleep(10);
                    if (++emergencyHalt > 100)
                    {
                        string msg = String.Format("Vishnu.{0}: möglicher Endlosloop in LogicalNode.runAsync.", this.Id);
                        if (this.AppSettings.DebugMode)
                        {
                            throw new ApplicationException(msg);
                        }
                        else
                        {
                            InfoController.Say(msg);
                            break;
                        }
                    }
                }
                this.LogicalState = NodeLogicalState.None;
                this.LogicalState = NodeLogicalState.Start;
                this.State = NodeState.None;
                if ((this.CancellationToken != null && this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    return;
                }
                if (source != null)
                {
                    this.AddEnvironment(source);
                }
                this.LastRun = DateTime.Now;
                this.DoRun(source);
                if ((this.LogicalState & NodeLogicalState.Start) > 0)
                {
                    this.LogicalState = NodeLogicalState.None;
                }
            }
        }

        /// <summary>
        /// Eigene Task Action für den Aufruf des externen Loggers.
        /// </summary>
        /// <param name="treeEvent">Klasse mit Informationen zum auslösenden Ereignis.</param>
        /// <param name="additionalEventArgs">Enthält z.B. beim Event 'Exception' die zugehörige Exception.</param>
        /// <param name="sender">Der Sender (Weiterleiter oder Auslöser) des Events und Besitzer des Loggers.</param>
        private void LogAsync(LogicalNode sender, TreeEvent treeEvent, object additionalEventArgs)
        {
            Thread.CurrentThread.Name = "logAsync";
            try
            {
                sender.Logger.Log(null, this.TreeParams, treeEvent, additionalEventArgs);
            }
            catch (Exception ex)
            {
                sender.Logger = null;
                // this.OnExceptionRaised(this, ex); // funktioniert an dieser Stelle nicht,
                // deshalb direkte Fehlermeldung und ansonsten weiterarbeiten
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Informiert über den Abbruch der Verarbeitung in einem Teilbaum.
        /// </summary>
        private void CancelNotification()
        {
            this.IsTaskActiveOrScheduled = false;
            this.IsActive = false;
            this.ThreadUpdateLastState(NodeState.Finished);
            this.State = NodeState.Finished;
            this.LogicalState = this._nextLogicalBreakState;
            this.MarkThreadAsInvalidIfActive(this._starterThread);
            if ((this is SingleNode) && (this as SingleNode).Checker != null)
            {
                (this as SingleNode).Checker.IsInvalid = true;
            }
            this.ThreadUpdateLastLogicalState(this.LogicalState);
            this.OnNodeProgressFinished(this.Id + "." + this.Name + " | Item", 100, 0, ItemsTypes.itemGroups);
            this.OnNodeBreaked();
        }

        /// <summary>
        /// Stößt weitere Verarbeitungen für das aktuelle TreeEvent an (Trigger, Logger).
        /// </summary>
        /// <param name="sender">Publisher des Events, das verarbeitet werden soll.</param>
        /// <param name="source">Quelle des Events, das verarbeitet werden soll.</param>
        /// <param name="eventName">Name des Events, das verarbeitet werden soll.</param>
        /// <param name="addInfo">Zusätzliche Information (Exception, Progress%, etc.).</param>
        private void ProcessTreeEvent(LogicalNode sender, LogicalNode source, string eventName, object addInfo)
        {
            // InfoController.Say(String.Format($"#MIRROR# Id/Name: {this.IdInfo}, Event: {eventName}"));
            // Statistics.Inc(String.Format($"#MIRROR# {this.Id}/{this.Name}: LogicalNode.ProcessTreeEvent"));
            lock (LogicalNode._eventLocker)
            {
                if (!(source is NodeConnector))
                {
                    bool workerExecutedOrBreaked = false;
                    // Verarbeitung für "Alles wieder ok"-Meldung an noch laufende Worker.
                    // Geht nur bei "AnyException", "LastNotNullLogicalToTrue", "LastNotNullLogicalToFalse".
                    if (this.TreeRootJobList.SendersOfLastExecutedWorkersContainsNodeAndEvent(sender.Id, eventName))
                    {
                        List<WorkerShell> workers = this.TreeRootJobList.GetLastExecutedWorkersToSenderAndEvent(sender.Id, eventName);
                        foreach (WorkerShell worker in workers)
                        {
                            worker.BreakExec();
                        }
                        foreach (WorkerShell worker in workers)
                        {
                            workerExecutedOrBreaked = true;
                            TreeEvent treeEvent = new TreeEvent(TreeEvent.GetUserEventNamesForInternalEventNames(eventName), source.Id,
                              sender.Id, this.Name, this.Path, source.LastNotNullLogical, source.LastLogicalState,
                              source.GetResultsFromResultList(), source.GetResultsFromEnvironment());
                            worker.Exec(this.TreeParams, this.Id, treeEvent, true);
                        }
                        if (workerExecutedOrBreaked)
                        {
                            this.SetWorkersState(eventName);
                        }
                        this.TreeRootJobList.TryRemoveEventFromSender(sender.Id, eventName);
                    }
                    this.ProcessSingleTreeEvent(sender, source, eventName, addInfo);
                    if (eventName.StartsWith("Any") && eventName != "AnyException"
                        && (!(sender is NodeList) || (sender is JobList)) && sender.Mother != null)
                    {
                        sender.RootJobList.climb2Top(new Action<LogicalNode>(logicalNode =>
                        {
                            if (logicalNode is JobList)
                            {
                                this.ProcessSingleTreeEvent(logicalNode, source, eventName, addInfo);
                            }
                        }));
                    }
                }
            }
        }

        /// <summary>
        /// Hilfsroutine für ProcessTreeEvent, wird bei"Any..."-Events im Loop aufgerufen.
        /// </summary>
        /// <param name="sender">Publisher des Events, das verarbeitet werden soll.</param>
        /// <param name="source">Quelle des Events, das verarbeitet werden soll.</param>
        /// <param name="eventName">Name des Events, das verarbeitet werden soll.</param>
        /// <param name="addInfo">Zusätzliche Information (Exception, Progress%, etc.).</param>
        private void ProcessSingleTreeEvent(LogicalNode sender, LogicalNode source, string eventName, object addInfo)
        {
            if (this.TreeRootJobList.TriggerRelevantEventCache.Contains(eventName))
            {
                foreach (TreeEventTrigger trigger in this.FindEventTriggers(eventName, sender.Id, source.Id))
                {
                    TreeEvent treeEvent = new TreeEvent(TreeEvent.GetUserEventNamesForInternalEventNames(eventName), source.Id,
                      sender.Id, this.Name, this.Path, source.LastNotNullLogical, source.LastLogicalState,
                      source.GetResultsFromResultList(), source.GetResultsFromEnvironment());
                    TreeEventQueue.AddTreeEventTrigger(treeEvent, sender.Id, trigger);
                }
            }
            if (this.TreeRootJobList.WorkerRelevantEventCache.Contains(eventName))
            {
                if (eventName == "Breaked" || eventName == "Started") // erstmal alle noch laufenden, getriggerten Worker für diesen Knoten abbrechen.
                {
                    foreach (WorkerShell[] workerArray in this.FindEventWorkers(eventName, sender.Id, source.Id, true))
                    {
                        foreach (WorkerShell worker in workerArray)
                        {
                            worker.BreakExec();
                        }
                    }
                }

                bool workerExecutedOrBreaked = false;
                foreach (WorkerShell[] workerArray in this.FindEventWorkers(eventName, sender.Id, source.Id, false))
                {
                    foreach (WorkerShell worker in workerArray)
                    {
                        workerExecutedOrBreaked = true;
                        TreeEvent treeEvent = new TreeEvent(TreeEvent.GetUserEventNamesForInternalEventNames(eventName),
                          source.Id, sender.Id, this.Name, this.Path, source.LastNotNullLogical, source.LastLogicalState,
                          source.GetResultsFromResultList(), source.GetResultsFromEnvironment());
                        worker.Exec(this.TreeParams, this.Id, treeEvent, false);
                        if (worker.WorkerState != NodeWorkerState.Invalid)
                        {
                            string resettingEvents = this.GetResettingEventNames(eventName);
                            if (resettingEvents != null)
                            {
                                // sender mit Worker in Liste einfügen.
                                this.TreeRootJobList.TryAddWorkerToEventsOfSender(sender.Id, resettingEvents, worker);
                            }
                        }
                        else
                        {
                        }
                    }
                    if (workerExecutedOrBreaked)
                    {
                        this.SetWorkersState(eventName);
                    }
                }
            }
            if (this.TreeRootJobList.LoggerRelevantEventCache.Contains(eventName))
            {
                if (eventName.StartsWith("Any") || sender.Id == source.Id)
                {
                    TreeEvent treeEvent = new TreeEvent(TreeEvent.GetUserEventNamesForInternalEventNames(eventName), source.Id,
                      sender.Id, this.Name, this.Path, source.LastNotNullLogical, source.LastLogicalState,
                      source.GetResultsFromResultList(), source.GetResultsFromEnvironment());
                    // this.LogIfConfigured(this, eventName, treeEvent, addInfo); // 15.02.2019 Nagel+
                    this.LogIfConfigured(sender, eventName, treeEvent, addInfo); // 15.02.2019 Nagel-
                    // source.Id kann <> sender.Id sein, this.Id ist immer = sender.Id.
                }
            }
        }

        private string GetResettingEventNames(string eventName)
        {
            // Knoten                       A
            // Event            false     true      Exception
            // Worker           X   Y       Z       T   U   V
            // ResettingEvent   true      false    true   false
            // Ein Worker kann zwar für mehrere Events konfiguriert sein ("False|Exception"),
            // ausgelöst wird er aber durch ein konkretes Event. Zu diesem einen Event
            // retourniert diese Routine ein oder zwei durch '|' getrennte zurücksetzende Events.
            switch (eventName)
            {
                case "LastNotNullLogicalToFalse": return "LastNotNullLogicalToTrue";
                case "AnyException": return "LastNotNullLogicalToTrue|LastNotNullLogicalToFalse";
                case "LastNotNullLogicalToTrue": return "LastNotNullLogicalToFalse";
                default: return null;
            }
        }

        /// <summary>
        /// Gibt Logging-Informationen an einen externen Logger weiter,
        /// wenn ein externer Logger für den übergebenen internalEventName eingerichtet ist.
        /// </summary>
        /// <param name="sender">Quelle des Events, das das Logging auslöst.</param>
        /// <param name="treeEvent">Klasse mit Informationen zum Ereignis.</param>
        /// <param name="internalEventName">Interner Name des aktuellen Events.</param>
        /// <param name="addInfo">Zusätzliche Information (Exception, Progress%, etc.).</param>
        private void LogIfConfigured(LogicalNode sender, string internalEventName, TreeEvent treeEvent, object addInfo)
        {
            if (sender.Logger != null)
            {
                lock (this._loggerLocker)
                {
                    if (sender.Logger.GetLogEvents().Contains(internalEventName))
                    {
                        Task asyncLoggerTask = new Task(() => this.LogAsync(sender, treeEvent, addInfo));
                        asyncLoggerTask.Start(); // fire and forget
                    }
                }
            }
        }

        private void AcceptNewLogical(bool? tmpLogical)
        {
            LogicalNode.WaitWhileTreePaused();

            Guid tmpGuid = Guid.NewGuid();
            this.LastNotNullLogical = tmpLogical;
            //this.ThreadUpdateLastLogical(tmpLogical);
            this.Logical = tmpLogical;
            this.OnLastNotNullLogicalChanged(this, tmpLogical, tmpGuid);
            this.IsFirstRun = false;
            //23.05.2018 TEST: this.OnStateChanged();
            //23.05.2018 TEST: this.OnLogicalStateChanged();
            this.OnNodeLogicalChanged();
            // Wenn das Ergebnis schon ausreicht, dann diesen Zweig abbrechen.
            if (this.BreakWithResult && this.Logical != null && ((this.State & NodeState.Busy) != 0))
            {
                this.Break(false);
            }
        }

        private void ValidateLastNotNullLogical(bool? orgLastNotNullLogical,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
          )
        {
            // Diese Routine läuft in einem eigenen Thread, weswegen sie synchronisiert werden muss.
            LogicalNode.WaitWhileTreePaused(); // vermeidet Deadlocks
            lock (this._validateLastNotNullLogicalLocker)
            {
                // Hier kommen nur JobLists hin.
                bool? tmpLastNotNullLogical = orgLastNotNullLogical;
                int timeToSleep = (this as JobList).LogicalChangedDelay;
                Thread.Sleep(timeToSleep);
                bool? tmpLogical = this.Logical; // mit Thread-entkoppelten Kopien arbeiten.
                if (tmpLogical != tmpLastNotNullLogical)
                {
                    this.AcceptNewLogical(tmpLogical);
                }
                this.OnNodeStateChanged();
                (this as JobList).IsChangeEventPending = false;
            }
        }

        /// <summary>
        /// Setzt den Knoten auf die Starteinstellungen zurück.
        /// </summary>
        /// <param name="depth">Interner Parameter - hier immer 0.</param>
        /// <param name="node">Der Knoten, der initialisiert werden soll.</param>
        protected virtual void InitNode(int depth, LogicalNode node)
        {
            node.IsResetting = true;
            node.Reset();
            node.IsResetting = false;
        }

        /// <summary>
        /// Startet die Verarbeitung eines (Teil-)Baums.
        /// Intern wird für jeden Run eine eigene Task gestartet.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent.</param>
        private void StartNodeOrTrigger(TreeEvent source)
        {
            if (source.Name != "PseudoTreeEvent") // "PseudoTreeEvent" wird beim run durch die übergeordnete NodeList gesetzt.
            {
                if (source.Name == "UserRun" && this.LastExecutingTreeEvent != null)
                {
                    this.LastExecutingTreeEvent.Name = source.Name;
                }
                else
                {
                    this.LastExecutingTreeEvent = source;
                }
            }
            lock (this._runLocker)
            {
                if (this._nodeCancellationTokenSource != null)
                {
                    this._nodeCancellationTokenSource.Dispose();
                }
                this._nodeCancellationTokenSource = new CancellationTokenSource();
                this.CancellationToken = this._nodeCancellationTokenSource.Token;
                this.CancellationToken.Register(() => CancelNotification());

                this.IsTaskActiveOrScheduled = true;
                if (this.Trigger != null)
                {
                    // Der Knoten ist getriggert.
                    try
                    {
                        // Erst mal den Trigger starten, außer es ist ein TreeEventTrigger (der wurde schon vorher gestartet).
                        TriggerShell triggerShell = (this.Trigger as TriggerShell);
                        if (!triggerShell.HasTreeEventTrigger)
                        {
                            string internalTriggerParameters = null;
                            if (source.Name == "UserRun" && source.SenderId == this.Id)
                            {
                                internalTriggerParameters = "UserRun";
                            }
                            triggerShell.Start(this, internalTriggerParameters, this.RunAsyncAsync);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Schnell noch ein Result mitgeben, damit die Exception in der GUI angezeigt werden kann.
                        lock (this.ResultLocker)
                        {
                            this.LastResult = new Result(this.Id, this.LastLogical, this.LastState, NodeLogicalState.Fault, ex);
                        }
                        this.OnExceptionRaised(this, ex);
                        return;
                    }
                }
                if (this.Trigger == null || source.Name == "UserRun" && this.ShallDirectRun(source))
                {
                    this.RunAsyncAsync(this.LastExecutingTreeEvent);
                }
                else
                {
                    if (this.LastExceptions.Count == 0)
                    {
                        this.State = NodeState.Triggered;
                        this.OnNodeStateChanged();
                        this.OnNodeProgressFinished(this.Id + "." + this.Name, 100, 100, ItemsTypes.itemGroups);
                    }
                }
            } // lock (this._runLocker)
        }

        /// <summary>
        /// Prüft, ob ein Knoten gestartet werden kann. In dieser Prüfung wird eventuell
        /// eine Abfrage geschaltet, durch die das erste Prüfungsergebnis übersteuert
        /// werden kann.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, wenn der Knoten gestartet werden kann.</returns>
        private bool CanRun(TreeEvent source)
        {
            ResultDictionary collectedEnvironment = new ResultDictionary();
            bool start = false;
            if (((this is NodeList) && !(this is JobList)) || this.CanControlledTreeStart(this == this.RootJobList, collectedEnvironment))
            {
                if (this.LastExecutingTreeEvent != null)
                {
                    this.LastExecutingTreeEvent.Environment = collectedEnvironment;
                }
                start = true;
            }
            else
            {
                // Der Knoten kann eigentlich nicht gestartet werden, bei controlled-Jobs, die gerade durch
                // den User direkt selbst gestartet werden wird aber eine Abfrage oder Meldung ausgegeben.
                // Bei Abfrage kann der User den Knoten dann doch noch auf startbar setzen.
                // 04.05.2019 Nagel+ if (this.RootJobList.IsConrolled && source.Name == "UserRun" && source.SenderId == this.Id)
                if (source.Name == "UserRun" && source.SenderId == this.Id) // 04.05.2019 Nagel-
                {
                        string msg = "Die Startvoraussetzungen für diesen Knoten sind noch nicht gegeben"
                                   + " oder der Knoten wurde zwischenzeitlich gestoppt.";
                    if (this.AppSettings.ControlledNodeUserRunDialog == DialogSettings.Question)
                    {
                        msg += "\nSoll der Knoten trotzdem gestartet werden?";
                        if (System.Windows.MessageBox.Show(msg, "Warnung", System.Windows.MessageBoxButton.YesNo,
                          System.Windows.MessageBoxImage.Exclamation, System.Windows.MessageBoxResult.No)
                          == System.Windows.MessageBoxResult.Yes)
                        {
                            start = true;
                        }
                    }
                    else
                    {
                        if (this.AppSettings.ControlledNodeUserRunDialog == DialogSettings.Info)
                        {
                            msg += "\nDer Knoten kann nicht gestartet werden.";
                            System.Windows.MessageBox.Show(msg, "Warnung", System.Windows.MessageBoxButton.OK,
                              System.Windows.MessageBoxImage.Hand);
                        }
                    }
                }
            }
            return start;
        }

        /// <summary>
        /// Prüft, ob ein Knoten direkt gestartet werden soll, auch wenn er getriggert
        /// ist (gilt nicht für TreeEventTrigger). 
        /// werden kann.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        /// <returns>True, wenn der Knoten auch direkt gestartet werden soll.</returns>
        private bool ShallDirectRun(TreeEvent source)
        {
            bool start = false;
            // Bei UserRun einen getriggerten Knoten ggf. auch direkt starten.
            if (source.Name == "UserRun")
            {
                if (!(
                      ((this.AppSettings.StartTriggeredNodesOnUserRun & TriggeredNodeStartConstraint.None) != 0) ||
                      (((this.AppSettings.StartTriggeredNodesOnUserRun & TriggeredNodeStartConstraint.NoTreeEvents) != 0)
                        && (this.Trigger as TriggerShell).HasTreeEventTrigger) ||
                      ((this.AppSettings.StartTriggeredNodesOnUserRun & TriggeredNodeStartConstraint.Direct) != 0
                        && (source.SenderId != this.Id))
                      )
                    )
                {
                    start = true;
                }
            }
            return start;
        }

        #endregion private members
    }
}
