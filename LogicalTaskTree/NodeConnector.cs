using System;
using System.ComponentModel;
using System.Text;
using NetEti.Globals;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Wird automatisch generiert, wenn der Name eines Einzelknotens
    /// innerhalb eines logischen Ausdrucks mehrfach auftritt. Wirkt nach außen so,
    /// als ob der Knoten zweimal (oder mehrmals) im Tree aufträte, verweist intern
    /// aber nur auf den ersten Knoten dieses Namens. Dadurch wird sichergestellt,
    /// dass die zu dem ursprünglichen Knoten gehörige Verarbeitung in Tree nur
    /// einmal ausgeführt wird.
    /// Filtert das ReturnObject eines INodeCheckers nach Typ und ggf. Format-String.
    /// </summary>
    /// <remarks>
    /// File: NodeConnector.cs
    /// Autor: Erik Nagel
    ///
    /// 27.05.2013 Erik Nagel: erstellt
    /// </remarks>
    public class NodeConnector : NodeParent
    {

        #region public members

        /// <summary>
        /// Der logische Zustand des Knotens.
        /// </summary>
        public override bool? Logical
        {
            get
            {
                return this._node?.Logical;
            }
            set
            {
            }
        }

        /// <summary>
        /// Der Verarbeitungszustand des Knotens:
        /// None, Waiting, Working, Finished, Busy (= Waiting | Working) oder CanStart (= None|Finished).
        /// </summary>
        public override NodeState State
        {
            get
            {
                return this._node?.State ?? NodeState.None;
            }
            set
            {
            }
        }

        /// <summary>
        /// Der Ergebnis-Zustand des Knotens:
        /// None, Done, Fault, Timeout, UserAbort.
        /// </summary>
        public override NodeLogicalState LogicalState
        {
            get
            {
                SleepIfNecessary();
                return this._node?.LogicalState ?? NodeLogicalState.None;
            }
            set
            {
                if (this._node != null && this._node.LogicalState != value)
                {
                    this._node.LogicalState = value;
                }
            }
        }

        /// <summary>
        /// Property für die Fortschrittsberechnung.
        /// </summary>
        public override int SingleNodes
        {
            get
            {
                return this._node?.SingleNodes ?? 0;
            }
        }

        /// <summary>
        /// Property für die Fortschrittsberechnung.
        /// </summary>
        public override int SingleNodesFinished
        {
            get
            {
                return this._node?.SingleNodesFinished ?? 0;
            }
        }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public override string UserControlPath
        {
            get
            {
                return this._userControlPath;
            }
            set
            {
                this._userControlPath = value;
            }
        }

        /// <summary>
        /// Der Arbeitsprozess - hier wird mit der Welt kommuniziert,
        /// externe Prozesse gestartet oder beobachtet.
        /// </summary>
        public NodeCheckerBase? Checker
        {
            get
            {
                if (this._node is SingleNode)
                {
                    return ((SingleNode)this._node).Checker;
                }
                else
                {
                    return null;
                }
            }
            set
            {
            }
        }

        /// <summary>
        /// Das letzte Verarbeitungsergebnis für diesen Knoten.
        /// </summary>
        public override Result? LastResult
        {
            get
            {
                return this._lastResult;
            }
            set
            {
                lock (this.ResultLocker)
                {
                    if (value != null)
                    {
                        NodeLogicalState nodeLogicalState = value.LogicalState == null ? NodeLogicalState.Done : (NodeLogicalState)value.LogicalState;
                        this._lastResult = new Result(value.Id, value.Logical, value.State, nodeLogicalState,
                          this.modifyValue(value.ReturnObject));
                    }
                }
            }
        }

        /// <summary>
        /// Zeitpunkt des letzten Starts des Knoten.
        /// </summary>
        public override DateTime LastRun
        {
            get
            {
                if (this._node != null)
                {
                    return this._node.LastRun;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Zeitpunkt des nächsten Starts des Knotens (wenn bekannt) oder DateTime.MinValue.
        /// </summary>
        public override DateTime NextRun
        {
            get
            {
                if (this._node != null)
                {
                    return this._node.LastRun;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Info-Text über den nächsten Start des Knotens (wenn bekannt) oder null.
        /// </summary>
        public override string? NextRunInfo
        {
            get
            {
                if (this._node != null)
                {
                    return this._node.NextRunInfo;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Name eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public override string? ReferencedNodeName
        {
            get
            {
                if ((this._node?.NodeType == NodeTypes.NodeConnector || this._node?.NodeType == NodeTypes.JobConnector) && !this._node.IsSnapshotDummy)
                {
                    return ((NodeConnector)this._node).ReferencedNodeName;
                }
                else
                {
                    return this._node?.Name;
                }
            }
        }

        /// <summary>
        /// Id des ursprünglich referenzierten Knotens.
        /// </summary>
        public override string? ReferencedNodeId
        {
            get
            {
                if ((this._node?.NodeType == NodeTypes.NodeConnector || this._node?.NodeType == NodeTypes.JobConnector) && !this._node.IsSnapshotDummy)
                {
                    return ((NodeConnector)this._node).ReferencedNodeId;
                }
                else
                {
                    return this._node?.Id;
                }
            }
        }

        /// <summary>
        /// Pfad des ursprünglich referenzierten Knotens.
        /// </summary>
        public override string? ReferencedNodePath
        {
            get
            {
                if (this._node is NodeConnector)
                {
                    return ((NodeConnector)this._node).ReferencedNodePath;
                }
                else
                {
                    return this._node?.Path;
                }
            }
        }

        /// <summary>
        /// Konstruktor für ein Snapshot-Dummy-Element.
        /// Dieser Konstruktor leitet für den Konstruktor von NodeConnector direkt an LogicalNode weiter.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public NodeConnector(LogicalNode mother, JobList rootJobList, TreeParameters treeParams) : base(mother, rootJobList, treeParams)
        {
            this._userControlPath = String.Empty;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id">Eindeutige Kennung des Knotens.</param>
        /// <param name="mother">Der Parent-Knotens.</param>
        /// <param name="rootJoblist">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="node">Die LogicalNode, zu der sich dieser NodeConnector verbinden soll.</param>
        /// <param name="valueModifier">Ein optionaler ValueModifier oder null.</param>
        public NodeConnector(string id, LogicalNode mother, JobList rootJoblist, TreeParameters treeParams, LogicalNode? node, NodeCheckerBase? valueModifier)
          : base(id, mother, rootJoblist, treeParams)
        {
            if (node != null)
            {
                this.InitReferencedNode(node);
            }
            this._valueModifier = valueModifier;
            this._userControlPath = String.Empty;
        }

        /// <summary>
        /// Merkt sich den zu referenzierenden Knoten und hängt sich in dessen Events.
        /// </summary>
        /// <param name="node">Zu referenzierenden Knoten</param>
        public void InitReferencedNode(LogicalNode node)
        {
            this.AddChild(node);
            this._node = node;
        }

        /// <summary>
        /// Startet den Originalknoten.
        /// </summary>
        /// <returns>True, wenn gestartet wurde.</returns>
        public override void UserRun()
        {
            /* // 13.01.2024 Nagel+ auskommentiert
            if (this.ParentView != null)
            {
                this.ThreadUpdateParentView(this.ParentView); // temporär auf die ParentView des aktuellen NodeConnectors umschießen.
            }
            */ // 13.01.2024 Nagel-



            if (!(this._node is NodeConnector))
            {
                if (this._node?.LastResult != null)
                {
                    this._node.Run(new TreeEvent("UserRun", this.Id, this.Id, this.Name, this.Path, null, NodeLogicalState.None,
                      new ResultDictionary() { { String.IsNullOrEmpty(this.Name) ? this.Id : this.Name, this._node.LastResult } }, null));
                }
                else
                {
                    this._node?.Run(new TreeEvent("UserRun", this.Id, this.Id, this.Name, this.Path, null, NodeLogicalState.None, null, null));
                }
            }
            else
            {
                this._node.UserRun();
            }
        }

        /// <summary>
        /// Stoppt den Originalknoten.
        /// </summary>
        public override void UserBreak()
        {
            if (!(this._node is NodeConnector))
            {
                this._node?.Break(true);
            }
            else
            {
                this._node.UserBreak();
            }
        }

        /// <summary>
        /// Lädt den Zweig oder Knoten neu.
        /// </summary>
        /// <returns>Die RootJobList des neu geladenen Jobs.</returns>
        public override JobList? Reload()
        {
            if (this._node != null)
            {
                return this._node.Reload();
            }
            return null;
        }

        /// <summary>
        /// Überschrieben, um das Starten von ValueModifiern zu unterbinden.
        /// Stattdessen wird über UserRun zum Originalknoten weitergeleitet.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        public override void Run(TreeEvent source)
        {
            // keine Weiterleitung - nur bei UserRun.
        }

        /// <summary>
        /// Hier wird der Checker-Thread gestartet.
        /// Diese Routine läuft asynchron.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        protected override void DoRun(TreeEvent? source)
        {
            base.DoRun(source); // Springt direkt in LogicalNode.DoRun, da NodeParent DoRun nicht überschreibt.
            // hier keine Funktion - nur in der referenzierten SingleNode.
        }

        /// <summary>
        /// Überschrieben, um das Abbrechen von ValueModifiern zu unterbinden.
        /// Stattdessen wird zum Originalknoten weitergeleitet.
        /// </summary>
        /// <param name="userBreak">Bei True hat der Anwender das Break ausgelöst.</param>
        public override void Break(bool userBreak)
        {
            // keine Weiterleitung - nur bei UserBreak.
        }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Verkettete Properties als String.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.ToString());
            string slavePathName = "";
            string referencedNodeName = "";
            string? checkerParameters = "";

            if (this.Checker != null)
            {
                if (this.Checker is CheckerShell)
                {
                    slavePathName = ((CheckerShell)this.Checker).SlavePathName ?? "";
                    checkerParameters = (((CheckerShell)this.Checker).OriginalCheckerParameters ?? "").ToString();
                    referencedNodeName = ((CheckerShell)this.Checker).ReferencedNodeName ?? "";
                }
                else
                {
                    slavePathName = ((ValueModifier<object>)this.Checker).SlavePathName ?? "";
                    referencedNodeName = ((ValueModifier<object>)this.Checker).ReferencedNodeName ?? "";
                }
            }
            if (!String.IsNullOrEmpty(slavePathName))
            {
                stringBuilder.AppendLine(String.Format($"    SlavePathName: {slavePathName}"));
            }
            if (!String.IsNullOrEmpty(referencedNodeName))
            {
                stringBuilder.AppendLine(String.Format($"    ReferencedNodeName: {referencedNodeName}"));
            }
            if (!String.IsNullOrEmpty(checkerParameters))
            {
                stringBuilder.AppendLine(String.Format($"    CheckerParameters: {checkerParameters}"));
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieses NodeConnectors nach logischen Gesichtspunkten
        /// mit dem Inhalt eines übergebenen NodeConnectors.
        /// </summary>
        /// <param name="obj">Der NodeConnector zum Vergleich.</param>
        /// <returns>True, wenn der übergebene NodeConnector inhaltlich gleich diesem ist.</returns>
        public override bool Equals(object? obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            return this.ToString() == ((NodeConnector)obj).ToString();
        }

        /// <summary>
        /// Erzeugt einen Hashcode für diesen NodeConnector.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Liste mit allen Result-Objekten der Vorgänger des Teilbaums.
        /// </summary>
        internal override ResultList Environment
        {
            get
            {
                if (this._node != null)
                {
                    return this._node.Environment;
                }
                else
                {
                    return new ResultList();
                }
            }
            set
            {
            }
        }

        //internal string GetReferencedCheckerBaseName()
        //{
        //    return this._valueModifier?.CheckerBaseReferenceName;
        //}

        #endregion internal members

        #region protected members

        /// <summary>
        /// Der Knoten, auf den dieser NodeConnector verweist.
        /// </summary>
        protected LogicalNode? _node; // protected ist erforderlich.

        /// <summary>
        /// Das letzte Verarbeitungsergebnis für diesen Knoten (internes Feld).
        /// </summary>
        protected Result? _lastResult;

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl (internes Feld).
        /// </summary>
        protected string _userControlPath;

        /// <summary>
        /// Wird angesprungen, wenn die Verarbeitung des Kind-Knotens beendet ist.
        /// Kaskadiert u.U. im Tree bis zum Root-Knoten.
        /// Parent-Knoten mit mehreren Kindern (NodeList) generieren ihrerseits ein NodeProgressFinished-Event
        /// nur dann, wenn die Verarbeitung aller Kind-Knoten beendet ist.
        /// </summary>
        /// <param name="sender">Der referenzierte Originalknoten (LogicalNode).</param>
        /// <param name="args">Informationen zum Verarbeitungsfortschritt.</param>
        protected override void SubNodeProgressFinished(object? sender, ProgressChangedEventArgs args)
        {
            base.SubNodeProgressFinished(sender, args);
            this.LastResult = this._node?.LastResult;
        }

        /// <summary>
        /// Wird angesprungen, wenn sich das Result eines Kind-Knotens geändert hat.
        /// Kaskadiert im Tree bis zum Root-Knoten nach oben.
        /// </summary>
        /// <param name="sender">Der Kind-Knoten.</param>
        /// <param name="result">Neues Result des Kind-Knotens.</param>
        protected override void SubNodeResultChanged(LogicalNode sender, Result? result)
        {
            this.LastResult = result;
            base.OnNodeResultChanged();
        }

        #endregion protected members

        #region private members

        private NodeCheckerBase? _valueModifier;

        private object? modifyValue(object? toConvert)
        {
            if (this._valueModifier != null)
            {
                try
                {
                    return this._valueModifier.ModifyValue(toConvert);
                }
                catch (Exception ex)
                {
                    this.LogicalState = NodeLogicalState.Fault;
                    return ex;
                }
            }
            else
            {
                return toConvert;
            }
        }

        #endregion private members

    }
}
