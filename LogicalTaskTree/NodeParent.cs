using NetEti.Globals;
using System;
using System.ComponentModel;
using Vishnu.Interchange;
// using System.Threading;
// using NetEti.ApplicationControl;

namespace LogicalTaskTree
{
    /// <summary>
    /// Funktion: Basisklasse für Knoten mit Kindern: NodeList und NodeConnector.
    /// </summary>
    /// <remarks>
    /// File: NodeParent.cs
    /// Autor: Erik Nagel
    ///
    /// 03.06.2018 Erik Nagel: erstellt
    /// </remarks>
    public abstract class NodeParent : LogicalNode
    {
        #region public members

        /// <summary>
        /// Enthält Komma-separiert TreeParams.Name und IdInfo der Knoten, in deren
        /// Events sich dieser NodeParent eingehängt hat oder den Text "NULL".
        /// </summary>
        public string HookedTo { get; private set; }

        /// <summary>
        /// Konstruktor für ein Snapshot-Dummy-Element - gibt die Verarbeitung an die Basisklasse "LogicalNode" weiter.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public NodeParent(LogicalNode mother, JobList rootJobList, TreeParameters treeParams) : base(mother, rootJobList, treeParams)
        {
            this.LastSingleNodesFinishedLocker = new object();
            this.ThreadRefreshParentNodeLocker = new object();
            this.SubNodeStateChangedLocker = new object();
            this.HookedTo = "NULL";
            this.ThreadUpdateLastSingleNodesFinished(-1); // invalidate
        }

        /// <summary>
        /// Konstruktor - gibt die Verarbeitung an die Basisklasse "LogicalNode" weiter.
        /// </summary>
        /// <param name="id">Eindeutige Kennung des Knotens.</param>
        /// <param name="mother">Der Parent-Knoten.</param>
        /// <param name="rootJobList">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public NodeParent(string id, LogicalNode? mother, JobList? rootJobList, TreeParameters treeParams) : base(id, mother, rootJobList, treeParams)
        {
            this.IsResultDependant = false;
            this.LastSingleNodesFinishedLocker = new object();
            this.ThreadRefreshParentNodeLocker = new object();
            this.SubNodeStateChangedLocker = new object();
            this.HookedTo = "NULL";
            this.ThreadUpdateLastSingleNodesFinished(-1); // invalidate
        }

        /// <summary>
        /// Speichert den Kindknoten am übergebenen Index und hängt sich in die Events des Kindknoten ein.
        /// </summary>
        /// <param name="index">Der Index, an dem der Child-Knoten gespeichert werden soll.</param>
        /// <param name="child">Die zu speichernde Child-Node.</param>
        public virtual void SetChildAt(int index, LogicalNode child)
        {
            this.HookChildEvents(child);
            this.Children[index] = child;
        }

        /// <summary>
        /// Löst die Event-Verknüpfungen mit dem Child-Knoten am Index index und 
        /// ruft danach ggf. Dispose für den Child-Knoten auf.
        /// </summary>
        /// <param name="index">Der Index, an dem der Child-Knoten freigegeben werden soll.</param>
        public virtual void FreeChildAt(int index)
        {
            if (index < this.Children.Count && this.Children[index] != null)
            {
                this.UnhookChildEvents(this.Children[index]);
                if (this.Children[index] is IDisposable)
                {
                    try
                    {
                        (this.Children[index] as IDisposable)?.Dispose();
                    }
                    catch { }
                }
                this.Children[index] = UndefinedLogicalNode;
            }
        }

        /// <summary>
        /// Löst die Event-Verknüpfungen mit dem Child-Knoten am Index index.
        /// </summary>
        /// <param name="index">Der Index, an dem der Child-Knoten freigegeben werden soll.</param>
        public virtual void ReleaseChildAt(int index)
        {
            if (index < this.Children.Count && this.Children[index] != UndefinedLogicalNode)
            {
                this.UnhookChildEvents(this.Children[index]);
                this.Children[index] = UndefinedLogicalNode;
            }
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Dieses Flag zeigt an, ob eine neue Auswertung des Parent-Knoten bei Veränderungen
        /// des logischen Ergebnisses (LastNotNullLogical) eines seiner Kinder erfolgen soll
        /// (Default), oder bei jeder Result-Veränderung eines seiner Kinder.
        /// </summary>
        internal bool IsResultDependant { get; set; }

        /// <summary>
        /// Speichert den Kindknoten und hängt sich in die Events des Kindknoten ein.
        /// </summary>
        internal virtual void AddChild(LogicalNode child)
        {
            this.HookChildEvents(child);
            this.Children.Add(child);
        }

        /// <summary>
        /// Liefert den Index, an dem das nächste Child eingefügt würde.
        /// </summary>
        internal virtual int GetNextChildIndex()
        {
            return this.Children.Count;
        }

        /// <summary>
        /// Erzeugt eine eindeutige Id.
        /// </summary>
        /// <returns>Eindeutige Id</returns>
        internal string GenerateNextChildId()
        {
            return "Child_" + this.GetNextChildIndex().ToString();
        }


        /// <summary>
        /// Speichert den Kindknoten und hängt sich in die Events des Kindknoten ein.
        /// </summary>
        internal virtual void HookChildEvents(LogicalNode child)
        {
            this.UnhookChildEvents(child);
            child.NodeProgressStarted += this.SubNodeProgressStarted;
            child.NodeProgressChanged += this.SubNodeProgressChanged;
            child.NodeProgressFinished += this.SubNodeProgressFinished;
            child.NodeStateChanged += this.SubNodeStateChanged;
            child.NodeLogicalChanged += this.SubNodeLogicalChanged;
            child.NodeLastNotNullLogicalChanged += SubNodeLastNotNullLogicalChanged;
            child.NodeResultChanged += this.SubNodeResultChanged;
            child.ExceptionRaised += this.SubNodeExceptionRaised;
            child.ExceptionCleared += this.SubNodeExceptionCleared;
            string childInfo = child.TreeParams.Name + ": " + child.NameId;
            if (!this.HookedTo.Contains(childInfo))
            {
                this.HookedTo = (this.HookedTo + "," + childInfo).Replace("NULL", "").TrimStart(',');
            }
        }

        /// <summary>
        /// Löst die Event-Verknüpfungen mit dem Child-Knoten.
        /// </summary>
        /// <param name="child">Der zu lösende Child-Knoten.</param>
        internal virtual void UnhookChildEvents(LogicalNode child)
        {
            if (!(child is UndefinedLogicalNodeClass))
            {
                child.NodeProgressStarted -= this.SubNodeProgressStarted;
                child.NodeProgressChanged -= this.SubNodeProgressChanged;
                child.NodeProgressFinished -= this.SubNodeProgressFinished;
                child.NodeStateChanged -= this.SubNodeStateChanged;
                child.NodeLogicalChanged -= this.SubNodeLogicalChanged;
                child.NodeLastNotNullLogicalChanged -= SubNodeLastNotNullLogicalChanged;
                child.NodeResultChanged -= this.SubNodeResultChanged;
                child.ExceptionRaised -= this.SubNodeExceptionRaised;
                child.ExceptionCleared -= this.SubNodeExceptionCleared;
                string childInfo = child.TreeParams.Name + ": " + child.NameId;
                if (this.HookedTo.Contains(childInfo))
                {
                    this.HookedTo = (this.HookedTo + ",").Replace(childInfo + ",", "").TrimEnd(',');
                }
            }
        }

        /// <summary>
        /// Liefert eine Liste mit allen Result-Objekten des Teilbaums.
        /// </summary>
        /// <returns>Liste von Result-Objekten des Teilbaums.</returns>
        internal override ResultList GetResultList()
        {
            ResultList resultList = new ResultList();
            foreach (LogicalNode node in this.Children)
            {
                ResultList? part = node.GetResultList();
                if (part != null)
                {
                    foreach (string id in part.Keys)
                    {
                        // Qualifizierter Zugriff auf die Results wäre zwar schön, ist aber unpragmatisch, da
                        // ansonsten Checker, die diese Results verwerten, mit der Id des Knotens im Baum
                        // kompiliert werden müssten.
                        //if (!resultList.ContainsKey(this.Id + "." + id))
                        //{
                        //  resultList.TryAdd(this.Id + "." + id, part[id]); // Auf Concurrent umgestellt
                        //}
                        // Konsequenz: Result-Ids müssen über den gesamten Tree eindeutig sein, nur das erste
                        // Result einer Id wird vermerkt.
                        if (!resultList.ContainsKey(id))
                        {
                            resultList.TryAdd(id, part[id]); // Auf Concurrent umgestellt
                        }
                    }
                }
            }

            return resultList;
        }

        /// <summary>
        /// Setzt den Knoten auf Initialwerte zurück.
        /// Das entspricht dem Zustand beim ersten Starten des Trees.
        /// Bei Jobs mit gesetztem 'IsConrolled' ist das entscheidend,
        /// da sonst Knoten gestartet würden, ohne dass die Voraussetzungen
        /// erneut geprüft wurden.
        /// </summary>
        internal override void Reset()
        {
            base.Reset();
            this.Environment?.Clear();
            this.LastExecutingTreeEvent = null;
            this.IsFirstRun = true;
            this.ThreadUpdateLastLogical(null);
            this.DoNodeLogicalChanged();
        }

        /// <summary>
        /// Sorgt dafür, dass die NodeList ihre Werte neu ermittelt und sich neu darstellt.
        /// </summary>
        internal virtual void ThreadRefreshParentNode() { }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Die letzte Anzahl beendeter Endknoten-Kinder eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        protected int LastSingleNodesFinished { get; set; }

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastSingleNodesFinished.
        /// </summary>
        protected object LastSingleNodesFinishedLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren für das Zurücksetzen (Invalidieren)
        /// verschiedener für die Ermittlung des aktuellen Zustands der
        /// ParentNode wichtiger Counter.
        /// </summary>
        protected object ThreadRefreshParentNodeLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren für das Zurücksetzen (Invalidieren)
        /// verschiedener für die Ermittlung des aktuellen Zustands der
        /// ParentNode wichtiger Counter.
        /// </summary>
        protected object SubNodeStateChangedLocker;

        /// <summary>
        /// Wird angesprungen, wenn sich der Verarbeitungszustand eines Kindes geändert hat.
        /// Wird von einer SingleNode ausgelöst.
        /// Setzt sich im Tree nur bis zum direkten Parent-Knoten fort.
        /// </summary>
        /// <param name="sender">Der Kind-Knoten.</param>
        /// <param name="state">Verarbeitungszustand des Kind-Knotens.</param>
        protected virtual void SubNodeStateChanged(object sender, NodeState state)
        {
            lock (this.SubNodeStateChangedLocker)
            {
                NodeState tmpState = this.LastState;
                this.LastState = NodeState.Null;
                if (this.State != tmpState) // durch die Abfrage auf this.State wird this.State aktualisiert
                {                           // und liefert auch das aktualisierte Ergebnis zurück.
                    this.ThreadRefreshParentNode();
                    this.OnNodeStateChanged();
                }
            }
        }

        /// <summary>
        /// Wird angesprungen, wenn sich das aktuelle logische Ergebnis des Child-Knotens geändert hat.
        /// Wird von einer SingleNode ausgelöst.
        /// Setzt sich im Tree nur bis zum direkten Parent-Knoten fort.
        /// </summary>
        /// <param name="sender">Kind-Knoten</param>
        /// <param name="logical">Logisches Ergebnis (null, false, true)</param>
        protected virtual void SubNodeLogicalChanged(LogicalNode sender, bool? logical)
        {
            this.OnNodeLogicalChanged();
        }

        /// <summary>
        /// Wird angesprungen, wenn sich der logische Zustand "LastNotNullLogical" eines Kindes geändert hat.
        /// Dieses Event ist entscheidend für eine mögliche Änderung des logischen Zustandes des Parent-Knoten.
        /// Kann von SingleNodes und ParentNodes bis hin zum Root-Knoten ausgelöst werden.
        /// Ursprünglicher Auslöser ist immer eine SingleNode.
        /// Kaskadiert u.U. im Tree bis zum Root-Knoten nach oben.
        /// </summary>
        /// <param name="sender">Der auslösende Kind-Knoten.</param>
        /// <param name="lastNotNullLogical">Letztes relevantes logisches Ergebnis des Kind-Knotens.</param>
        /// <param name="eventId">Eindeutige Kennung des Events.</param>
        protected virtual void SubNodeLastNotNullLogicalChanged(LogicalNode sender, bool? lastNotNullLogical, Guid eventId)
        {
            lock (this.SubLastNotNullLogicalLocker)
            {
                bool? tmpLogical = this.LastLogical;
                this.LastLogical = null;
                bool? newLogical = this.Logical;
                if (sender.IsResetting || newLogical != tmpLogical)
                {
                    this.DoNodeLogicalChanged();
                    this.OnNodeLogicalChanged(); // notwendig (Test: Check_GT_LT)
                }
            }
        }

        /// <summary>
        /// Wird angesprungen, wenn sich das Result eines Kind-Knotens geändert hat.
        /// Kaskadiert im Tree bis zum Root-Knoten nach oben.
        /// </summary>
        /// <param name="sender">Der Kind-Knoten.</param>
        /// <param name="result">Neues Result des Kind-Knotens.</param>
        protected virtual void SubNodeResultChanged(LogicalNode sender, Result? result)
        {
            if (this.IsResultDependant)
            {
                this.SubNodeLastNotNullLogicalChanged(sender, null, Guid.Empty);
            }
            this.OnNodeResultChanged();
        }

        /// <summary>
        /// Wird angesprungen, wenn in einem Kind eine Exception aufgetreten ist.
        /// Kaskadiert die Exception bis zum Root-Knoten nach oben, ohne die Verarbeitung abzubrechen.
        /// </summary>
        /// <param name="source">Die Quelle der Exception.</param>
        /// <param name="exception">Aufgetretene Exception.</param>
        protected virtual void SubNodeExceptionRaised(LogicalNode source, Exception exception)
        {
            this.OnExceptionRaised(source, exception);
        }

        /// <summary>
        /// Wird angesprungen, wenn in einem Kind eine Exception gelöscht wurde.
        /// Kaskadiert die Auflösung einer vormaligen Exception u.U. bis zum Root-Knoten nach oben.
        /// Parent-Knoten mit mehreren Kindern (NodeList) generieren ihrerseits ein ExceptionCleared-Event
        /// nur dann, wenn kein Child-Knoten mehr eine Exception hält.
        /// </summary>
        /// <param name="source">Die Quelle der Exception.</param>
        protected void SubNodeExceptionCleared(LogicalNode source)
        {
            this.OnExceptionCleared(source);
        }

        /// <summary>
        /// Wird angesprungen, wenn die Verarbeitung des Kind-Knotens gestarted wurde.
        /// Kaskadiert u.U. im Tree bis zum Root-Knoten nach oben.
        /// Parent-Knoten mit mehreren Kindern (NodeList) generieren ihrerseits ein NodeProgressStarted-Event
        /// nur dann, wenn dieses das erste (und bisher einzige) Kind ist, dessen Verarbeitung gestartet wurde.
        /// </summary>
        /// <param name="source">Die Quelle der Exception.</param>
        /// <param name="args">Zusätzliche Event-Parameter (Fortschritts-% und Info-Object).</param>
        protected void SubNodeProgressStarted(object? source, ProgressChangedEventArgs args)
        {
            this.ThreadUpdateLastSingleNodesFinished(-1); // führt zur Neuauswertung.
            this.OnNodeProgressStarted(source, args);
        }

        /// <summary>
        /// Wird angesprungen, wenn sich der Verarbeitungsfortschritt
        /// des Referenzierten Originalknotens geändert hat.
        /// Setzt sich im Tree nur bis zum direkten Parent-Knoten fort.
        /// </summary>
        /// <param name="sender">Der referenzierte Originalknoten (LogicalNode).</param>
        /// <param name="args">Informationen zum Verarbeitungsfortschritt.</param>
        protected virtual void SubNodeProgressChanged(object? sender, ProgressChangedEventArgs args)
        {
            this.OnNodeProgressChanged(this.Id + "." + this.Name + " | ", this.SingleNodes, this.SingleNodesFinished);
        }

        /// <summary>
        /// Wird angesprungen, wenn die Verarbeitung des Kind-Knotens beendet ist.
        /// Kaskadiert u.U. im Tree bis zum Root-Knoten.
        /// Parent-Knoten mit mehreren Kindern (NodeList) generieren ihrerseits ein NodeProgressFinished-Event
        /// nur dann, wenn die Verarbeitung aller Kind-Knoten beendet ist.
        /// </summary>
        /// <param name="sender">Der referenzierte Originalknoten (LogicalNode).</param>
        /// <param name="args">Informationen zum Verarbeitungsfortschritt.</param>
        protected virtual void SubNodeProgressFinished(object? sender, ProgressChangedEventArgs args)
        {
            this.ThreadUpdateLastSingleNodesFinished(-1); // führt zur Neuauswertung.
            if (args.ProgressPercentage < this.Children.Count) // TODO: der IF ist so kompletter Unfug!
            {
                this.OnNodeProgressChanged(this.Id + "." + this.Name + " | ", this.SingleNodes, this.SingleNodesFinished);
            }
            else
            {
                this.OnNodeProgressFinished(this.Id + "." + this.Name + " | ", this.SingleNodes, this.SingleNodesFinished);
            }
        }

        /// <summary>
        /// Setzt threadsafe LastLogicalState.
        /// </summary>
        /// <param name="newLogicalState">Neuer Wert (NodeLogicalState?)</param>
        protected override void ThreadUpdateLastLogicalState(NodeLogicalState newLogicalState)
        {
            lock (this.LastLogicalStateLocker)
            {
                base.ThreadUpdateLastLogicalState(newLogicalState);
            }
        }

        /// <summary>
        /// Setzt threadsafe LastSingleNodesFinished.
        /// </summary>
        /// <param name="singleNodesFinished">Neuer Wert</param>
        protected void ThreadUpdateLastSingleNodesFinished(int singleNodesFinished)
        {
            lock (this.LastSingleNodesFinishedLocker)
            {
                this.LastSingleNodesFinished = singleNodesFinished;
            }
        }

        #endregion protected members

    }
}
