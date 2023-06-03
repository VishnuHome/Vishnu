using System;
using Vishnu.Interchange;

namespace LogicalTaskTree
{

    /// <summary>
    /// Wird automatisch generiert, wenn der Name eines Jobs
    /// innerhalb eines logischen Ausdrucks mehrfach auftritt. Stellt das
    /// Gruppen-Ergebnis des zuerst aufgetretenen Jobs gleichen Namens ähnlich
    /// wie in einem Einzelknoten dar.
    /// </summary>
    /// <remarks>
    /// File: ValueModifier.cs
    /// Autor: Erik Nagel
    ///
    /// 20.08.2015 Erik Nagel: erstellt
    /// </remarks>
    public class JobConnector : NodeConnector
    {
        #region public members

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public override string UserControlPath
        {
            get
            {
                if (! String.IsNullOrEmpty(this._userControlPath))
                {
                    return this._userControlPath;
                }
                else
                {
                    return this.RootJobList.JobConnectorUserControlPath;
                }
            }
            set
            {
                this._userControlPath = value;
            }
        }

        /// <summary>
        /// Der logische Ausdruck, der durch die referenzierte JobList abgearbeitet wird.
        /// Die Variablen entsprechen Checkern (Tree-Blätter bzw. Endknoten). 
        /// </summary>
        public string LogicalExpression
        {
            get
            {
                if (!this.IsSnapshotDummy)
                {
                    if (this._node != null)
                    {
                        return ((JobList)this._node).LogicalExpression;
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
                else
                {
                    return this._logicalExpression;
                }
            }
            set
            {
                this._logicalExpression = value;
            }
        }

        /// <summary>
        /// Liefert ein Result für diesen Knoten.
        /// </summary>
        /// <returns>Ein Result-Objekten für den Knoten.</returns>
        public override Result? LastResult
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    return this._node.LastResult;
                }
                else
                {
                    return this._lastResult;
                }
            }
            set
            {
                this._lastResult = value;
            }
        }

        /// <summary>
        /// Der logische Zustand des Knotens.
        /// </summary>
        public override bool? Logical
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    return this._node.Logical;
                }
                else
                {
                    return this._logical;
                }
            }
            set
            {
                this._logical = value;
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
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    return this._node.State;
                }
                else
                {
                    return this._state;
                }
            }
            set
            {
                this._state = value;
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
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    SleepIfNecessary();
                    return this._node.LogicalState;
                }
                else
                {
                    return this._logicalState;
                }
            }
            set
            {
                if (!this.IsSnapshotDummy)
                {
                    if (this._node != null && this._node.LogicalState != value)
                    {
                        this._node.LogicalState = value;
                    }
                }
                else
                {
                    this._logicalState = value;
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
                if (!this.IsSnapshotDummy)
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
                else
                {
                    return this._lastRun;
                }
            }
            set
            {
                this._lastRun = value;
            }
        }

        /// <summary>
        /// Zeitpunkt des nächsten Starts des Knotens (wenn bekannt) oder DateTime.MinValue.
        /// </summary>
        public override DateTime NextRun
        {
            get
            {
                if (!this.IsSnapshotDummy)
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
                else
                {
                    return this._nextRun;
                }
            }
            set
            {
                this._nextRun = value;
            }
        }

        /// <summary>
        /// Info-Text über den nächsten Start des Knotens (wenn bekannt) oder null.
        /// </summary>
        public override string? NextRunInfo
        {
            get
            {
                if (!this.IsSnapshotDummy)
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
                else
                {
                    return this._nextRunInfo;
                }
            }
            set
            {
                this._nextRunInfo = value;
            }
        }

        /// <summary>
        /// Name eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public override string? ReferencedNodeName
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    //if (this._node is NodeConnector)
                    if ((this._node.NodeType == NodeTypes.NodeConnector || this._node.NodeType == NodeTypes.JobConnector) && !this._node.IsSnapshotDummy)
                    {
                        return ((NodeConnector)this._node).ReferencedNodeName;
                    }
                    else
                    {
                        return this._node.Name;
                    }
                }
                else
                {
                    return this._referencedNodeName;
                }
            }
            set
            {
                this._referencedNodeName = value;
            }
        }

        /// <summary>
        /// Id des ursprünglich referenzierten Knotens.
        /// </summary>
        public override string? ReferencedNodeId
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    //if (this._node is NodeConnector)
                    if ((this._node.NodeType == NodeTypes.NodeConnector || this._node.NodeType == NodeTypes.JobConnector) && !this._node.IsSnapshotDummy)
                    {
                        return ((NodeConnector)this._node).ReferencedNodeId;
                    }
                    else
                    {
                        return this._node.Id;
                    }
                }
                else
                {
                    return this._referencedNodeId;
                }
            }
            set
            {
                this._referencedNodeId = value;
            }
        }

        /// <summary>
        /// Pfad des ursprünglich referenzierten Knotens.
        /// </summary>
        public override string? ReferencedNodePath
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    if (this._node is NodeConnector)
                    {
                        return ((NodeConnector)this._node).ReferencedNodePath;
                    }
                    else
                    {
                        return this._node.Path;
                    }
                }
                else
                {
                    return this._referencedNodePath;
                }
            }
            set
            {
                this._referencedNodePath = value;
            }
        }

        /// <summary>
        /// Property für die Fortschrittsberechnung.
        /// </summary>
        public override int SingleNodes
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    return this._node.SingleNodes;
                }
                else
                {
                    return 100;
                }
            }
        }

        /// <summary>
        /// Property für die Fortschrittsberechnung.
        /// </summary>
        public override int SingleNodesFinished
        {
            get
            {
                // 19.08.2018+ if (!this.IsSnapshotDummy)
                if (!this.IsSnapshotDummy && this._node != null) // 19.08.2018-
                {
                    return this._node.SingleNodesFinished;
                }
                else
                {
                    return 100;
                }
            }
        }

        /// <summary>
        /// Konstruktor für ein Snapshot-Dummy-Element - übernimmt den Eltern-Knoten.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten.</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public JobConnector(LogicalNode mother, JobList rootJobList, TreeParameters treeParams)
          : base(mother, rootJobList, treeParams)
        {
            this.UserControlPath = String.Empty;
            this._logicalExpression = String.Empty;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id">Eindeutige Kennung des Knotens.</param>
        /// <param name="mother">Id des Parent-Knotens.</param>
        /// <param name="rootJoblist">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="node">Die LogicalNode, zu der sich dieser NodeConnector verbinden soll.</param>
        /// <param name="valueModifier">Ein optionaler ValueModifier oder null.</param>
        public JobConnector(string id, LogicalNode mother, JobList rootJoblist, TreeParameters treeParams, LogicalNode node, NodeCheckerBase? valueModifier)
          : base(id, mother, rootJoblist, treeParams, node, null)
        {
            this._logicalExpression = RootJobList.LogicalExpression; // Vorbelegung
        }

        #endregion public members

        #region private members

        private string _logicalExpression;
        private DateTime _nextRun;
        private string? _referencedNodeName;
        private string? _referencedNodeId;
        private string? _referencedNodePath;
        /// <summary>
        /// Der Verarbeitungszustand des Knotens:
        /// Null, None, Waiting, Working, Finished, Triggered, Ready (= Finished | Triggered), Busy (= Waiting | Working) oder CanStart (= None | Ready)
        /// (internes Feld).
        /// <see cref="Vishnu.Interchange.NodeState"/>
        /// </summary>
        private volatile NodeState _state;
        /// <summary>
        /// Der logische Zustand des Knotens (internes Feld).
        /// </summary>
        private bool? _logical;
        /// <summary>
        /// Der Ergebnis-Zustand des Knotens:
        /// None, Start, Done, Fault, Timeout, UserAbort (internes Feld).
        /// </summary>
        private volatile NodeLogicalState _logicalState;

        #endregion private members

    }
}
