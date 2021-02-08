using System.Linq;
using System.Collections.Generic;
using System;
using Vishnu.Interchange;
using System.Threading;
using System.Text;
using NetEti.ApplicationControl; // TEST: 01.02.2021 Nagel

namespace LogicalTaskTree
{
    /// <summary>
    /// Knoten mit Kindern in LogicalTaskTree.
    /// </summary>
    /// <remarks>
    /// File: NodeList.cs
    /// Autor: Erik Nagel
    ///
    /// 01.12.2012 Erik Nagel: erstellt
    /// </remarks>
    public class NodeList : NodeParent
    {
        #region public members

        /// <summary>
        /// Der logische Zustand eines Knotens. In einer NodeList wird dieser
        /// bei jeder Anfrage on the fly rekursiv ermittellt:
        /// True wird dann zurückgegeben, wenn die Anzahl der Kinder mit Logical=True
        /// größer als die Schranke nPlus ist und kleiner als die Schranke n- ist und
        /// auch durch die noch nicht fertigen Kinder nicht mehr >= n- werden kann.
        /// False wird dann zurückgegeben, wenn entweder die Anzahl der Kinder mit
        /// Logical=True größer als die Schranke nMinus ist oder
        /// auch durch die noch nicht fertigen Kinder die Schranke nPlus nicht
        /// mehr überschritten werden kann.
        /// Die Schranken nPlus und nMinus werden für diese NodeList in der
        /// zugehörigen JobList beim Aufbau des Teilbaums gesetzt.
        /// Für die Vergleichsoperatoren gilt nur, dass alle Operanden beendet sein müssen,
        /// dann wird der Vergleich ausgeführt und dessen Ergebnis zurückgegeben.
        /// </summary>
        public override bool? Logical
        {
            get
            {
                lock (this.LastLogicalLocker)
                {
                    //InfoController.Say(String.Format($"#Logical# Id/Name: {this.Id}/{this.Name}, LastLogical: {this.LastLogical.ToString()}, IsResetting: {this.IsResetting.ToString()}"));

                    // 09.08.2018+ if (this.LastLogical != null /* 23.05.2018 Nagel Test || this.IsInSnapshot */ || this.IsResetting)
                    if (this.LastLogical != null || this.IsInSnapshot || this.IsResetting)
                    {
                        return this.LastLogical;
                    }
                    int countResults = this.CountResults;
                    if (countResults > 0)
                    {
                        //if (!(new[] { "LT", "LE", "NE", "EQ", "GE", "GT", "XOR" }).Contains(this.Name))
                        if (!this.IsResultDependant && this.Name != "XOR")
                        {
                            int countPositiveResults = this.CountPositiveResults;
                            if (countPositiveResults > this.nPlus)
                            {
                                if (countPositiveResults < this.nMinus)
                                {
                                    if (countPositiveResults + (this.nOperands - countResults) < this.nMinus)
                                    {
                                        this.ThreadUpdateLastLogical(true);
                                    }
                                }
                                else
                                {
                                    //if (this.Id == "SQL-Daten_local_Job") // 02.08.2018 TEST+
                                    //{
                                    //    InfoController.Say(String.Format($"#EXC# (1.) {this.Id}: countResults={countResults.ToString()}, countPositiveResults={countPositiveResults.ToString()}"));
                                    //    //this.RootJobList.PublishAllTreeInfos();
                                    //} // 02.08.2018 TEST-
                                    this.ThreadUpdateLastLogical(false);
                                }
                            }
                            else
                            {
                                if (this.nOperands - countResults <= this.nPlus - countPositiveResults) // !!!
                                {
                                    //if (this.Id == "SQL-Daten_local_Job") // 02.08.2018 TEST+
                                    //{
                                    //    InfoController.Say(String.Format($"#EXC# (2.) {this.Id}: countResults={countResults.ToString()}, countPositiveResults={countPositiveResults.ToString()}"));
                                    //}
                                    this.ThreadUpdateLastLogical(false);
                                }
                            }
                        }
                        else
                        {
                            if (countResults == this.nOperands) // alle Operanden vorhanden, jetzt noch Vergleiche durchführen
                            {
                                if (this.Name != "XOR")
                                {
                                    this.ThreadUpdateLastLogical(
                                      new NodeResultComparer().Compare(this.Name, this.Children.Select(a => a.LastResult).ToList()));
                                }
                                else
                                {
                                    // neu für mehrwertige Logik von XOR
                                    this.ThreadUpdateLastLogical(CountPositiveResults % 2 != 0);
                                }
                            }
                        } // if (!this.IsResultDependant)
                    } // if (countResults > 0)
                    if (this.LastLogical != this.LastReturnedLogical)
                    {
                        //if (this.Id == "SQL-Daten_local_Job") // 02.08.2018 TEST+
                        //{
                        //    InfoController.Say(String.Format($"#EXC#. {this.Id}: LastLogical={this.LastLogical.ToString()}, LastReturnedLogical={this.LastReturnedLogical.ToString()}"));
                        //    //this.RootJobList.PublishAllTreeInfos();
                        //} // 02.08.2018 TEST-
                        this.ThreadUpdateLastReturnedLogical(this.LastLogical);
                        this.OnLastNotNullLogicalChanged(this, this.LastLogical, Guid.Empty);
                    }
                    return this.LastLogical;
                } // 01.08.2018 Test+-
            }
            set
            {
                this.ThreadUpdateLastLogical(null);
            }
        }

        /// <summary>
        /// Der Verarbeitungszustand eines Knotens. In einer NodeList wird dieser
        /// bei jeder Anfrage on the fly rekursiv ermittellt.
        /// </summary>
        public override NodeState State
        {
            get
            {
                lock (this.LastStateLocker)
                {
                    if (this.LastState != NodeState.Null)
                    {
                        return this.LastState;
                    }
                }
                NodeState state = this.ListNodeState;
                if ((state & NodeState.Busy) == 0) // nur wenn der Knoten selbst nicht beschäftigt ist, die Kinder zusätzlich checken.
                {
                    // EmergencyDebug = this.TreeParams + ":" + this.IdInfo; // TEST 06.02.2021 Erik Nagel
                    if (this.Children.Where(c =>
                    {
                        //if (c == null)
                        //{
                        //    InfoController.Say("#RELOAD# " + EmergencyDebug); // TEST 06.02.2021 Erik Nagel
                        //    InfoController.FlushAll();
                        //}
                        return (c.State & NodeState.Busy) > 0;
                    }).FirstOrDefault() != null)
                    {
                        state = NodeState.Working;
                    }
                    else
                    {
                        if (!(this.Children.Where(c =>
                        {
                            return (c.State & NodeState.Ready) == 0;
                        }).FirstOrDefault() != null))
                        {
                            if (this.Trigger != null)
                            {
                                state = NodeState.Triggered;
                            }
                            else
                            {
                                this.IsTaskActiveOrScheduled = false;
                                state = NodeState.Finished;
                            }
                            this.IsActive = false;
                        }
                    }
                }
                this.ThreadUpdateLastState(state);
                return state;
            }
            set
            {
                this.ThreadUpdateLastState(NodeState.Null);
                if (this.ListNodeState != value)
                {
                    if (this.LogicalState == NodeLogicalState.Start && value == NodeState.None)
                    {
                        this.ListNodeState = value;
                    }
                    else
                    {
                        this.ListNodeState = value;
                        this.OnNodeStateChanged();
                    }
                }
                if ((value & NodeState.Busy) != 0)
                {
                    this.ThreadUpdateLastState(value);
                }
            }
        }

        /// <summary>
        /// Der Ergebnis-Zustand eines Knotens. In einer NodeList wird dieser
        /// bei jeder Anfrage on the fly rekursiv ermittellt.
        /// </summary>
        public override NodeLogicalState LogicalState
        {
            get
            {
                //lock (this.LastLogicalStateLocker) würde DeadLocks verursachen
                //{
                // 30.07.2018+ if (this.LastLogicalState != NodeLogicalState.Undefined)
                if (this.LastLogicalState != NodeLogicalState.Undefined && this.LastExceptions.Count == 0) // 30.07.2018-
                {
                    return this.LastLogicalState;
                }
                //}
                NodeLogicalState logicalState = this.ListLogicalState;
                List<NodeLogicalState> logicalStates = this.Children.Select(c => c.LogicalState).ToList();
                if (logicalState == NodeLogicalState.None)
                {
                    if (logicalStates.Contains(NodeLogicalState.Fault))
                    {
                        logicalState = NodeLogicalState.Fault;
                    }
                }
                if (logicalState == NodeLogicalState.None)
                {
                    if (logicalStates.Contains(NodeLogicalState.Timeout))
                    {
                        logicalState = NodeLogicalState.Timeout;
                    }
                }
                if (logicalState == NodeLogicalState.None)
                {
                    if (logicalStates.Contains(NodeLogicalState.UserAbort))
                    {
                        logicalState = NodeLogicalState.UserAbort;
                    }
                }
                if (logicalState == NodeLogicalState.None)
                {
                    if (!(logicalStates.Contains(NodeLogicalState.UserAbort) || logicalStates.Contains(NodeLogicalState.Timeout)
                        || logicalStates.Contains(NodeLogicalState.Fault) || logicalStates.Contains(NodeLogicalState.None)
                        || logicalStates.Contains(NodeLogicalState.Start)))
                    {
                        logicalState = NodeLogicalState.Done;
                    }
                }
                this.ThreadUpdateLastLogicalState(logicalState);
                return logicalState;
            }
            set
            {
                this.ThreadUpdateLastLogicalState(NodeLogicalState.Undefined);
                if (this.ListLogicalState != value)
                {
                    this.ThreadUpdateListLogicalState(value);
                    this.OnNodeStateChanged();
                }
            }
        }

        /// <summary>
        /// Anzahl der SingleNodes (letztendlich Checker) am Ende eines (Teil-)Baums;
        /// Achtung: dieser Wert ist, ebenso wie SingleNodesFinished, verhundertfacht
        /// (<see cref="SingleNode.SingleNodes"/>).
        /// </summary>
        public override int SingleNodes
        {
            get
            {
                lock (this.LastSingleNodesLocker)
                {
                    if (this.LastSingleNodes >= 0)
                    {
                        return this.LastSingleNodes;
                    }
                }
                this.ThreadUpdateLastSingleNodes(
                  this.Children.Sum(c =>
                    {
                        return c.SingleNodes;
                    }));
                return this.LastSingleNodes;
            }
        }

        /// <summary>
        /// Prozentwert für den Anteil der beendeten SingleNodes
        /// (letztendlich Checker) am Ende eines (Teil-)Baums.
        /// In einer NodeList wird dieser bei jeder Anfrage on the fly
        /// rekursiv ermittellt.
        /// Achtung: dieser Wert ist, ebenso wie SingleNodes, verhundertfacht
        /// (<see cref="SingleNode.SingleNodes"/>).
        /// </summary>
        public override int SingleNodesFinished
        {
            get
            {
                lock (this.LastSingleNodesFinishedLocker)
                {
                    if (this.LastSingleNodesFinished >= 0)
                    {
                        return this.LastSingleNodesFinished;
                    }
                }
                this.ThreadUpdateLastSingleNodesFinished(
                  this.Children.Sum(c =>
                  {
                      return c.SingleNodesFinished;
                  }));
                return this.LastSingleNodesFinished;
            }
        }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public override string UserControlPath
        {
            get
            {
                if (this._userControlPath != null)
                {
                    return this._userControlPath;
                }
                else
                {
                    return this.RootJobList.NodeListUserControlPath;
                }
            }
            set
            {
                this._userControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden SnapshotUserControls.
        /// </summary>
        public virtual string SnapshotUserControlPath
        {
            get
            {
                return this.RootJobList.SnapshotUserControlPath;
            }
            set
            {
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden JobListUserControls.
        /// </summary>
        public virtual string JobListUserControlPath
        {
            get
            {
                return this.RootJobList.UserControlPath;
            }
            set
            {
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden NodeListUserControls.
        /// </summary>
        public virtual string JobConnectorUserControlPath
        {
            get
            {
                return this.RootJobList.JobConnectorUserControlPath;
            }
            set
            {
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden SingleNodeUserControls.
        /// </summary>
        public virtual string SingleNodeUserControlPath
        {
            get
            {
                return this.RootJobList.SingleNodeUserControlPath;
            }
            set
            {
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden ConstantNodeUserControls.
        /// </summary>
        public virtual string ConstantNodeUserControlPath
        {
            get
            {
                return this.RootJobList.ConstantNodeUserControlPath;
            }
            set
            {
            }
        }

        /// <summary>
        /// Anzahl der logisch verknüpften Elemente (SingleNodes oder Teilbäume)
        /// auf der Ebene dieser NodeList.
        /// </summary>
        public int nOperands { get; internal set; }
        /// <summary>
        /// Schranke für die Ermittlung der Property 'Logical', Details <see cref="Logical"/>.
        /// </summary>
        public int nPlus { get; internal set; }
        /// <summary>
        /// Schranke für die Ermittlung der Property 'Logical', Details <see cref="Logical"/>.
        /// </summary>
        public int nMinus { get; internal set; }

        /// <summary>
        /// Bei True wird zur Ergebnisermittlung im Tree Logical benutzt,
        /// bei False LastNotNullLogical;
        /// Default: false.
        /// </summary>
        public bool IsVolatile { get; internal set; }

        /// <summary>
        /// Liefert ein Result für diesen Knoten.
        /// </summary>
        /// <returns>Ein Result-Objekten für den Knoten.</returns>
        public override Result LastResult
        {
            get
            {
                lock (this.ResultLocker)
                {
                    object returnObject = null;
                    if (this.LastExceptions != null && this.LastExceptions.Count > 0)
                    {
                        try
                        {
                            returnObject = this.LastExceptions.First().Value.Message;
                            // InfoController.Say(String.Format($"#EXC# Id/Name: {this.Id}/{this.Name} - 1"));
                        }
                        catch (System.InvalidOperationException)
                        {
                            returnObject = this.LastLogical;
                            // InfoController.Say(String.Format($"#EXC# Id/Name: {this.Id}/{this.Name} - 2"));
                        }
                    }
                    else
                    {
                        // 10.08.2018+ returnObject = this.LastLogical;
                        if (this.IsActive)
                        {
                            returnObject = this.LastLogical;
                        } else
                        {
                            returnObject = this.LastNotNullLogical; // sonst verschwindet bei UserBreak das Result (CheckTreeEventTriggerSnapshot)
                        } // 10.08.2018-
                        // InfoController.Say(String.Format($"#EXC# Id/Name: {this.Id}/{this.Name} - 3"));
                    }
                    // InfoController.Say(String.Format($"#EXC# Id/Name: {this.Id}/{this.Name} - LastResult({returnObject?? "null".ToString()})"));
                    return new Result()
                    {
                        Id = this.Id,
                        State = this.LastState,
                        Logical = this.LastLogical,
                        ReturnObject = returnObject
                    };
                }
            }
            set
            {
            }
        }

        /// <summary>
        /// Konstruktor für ein Snapshot-Dummy-Element - übernimmt den Eltern-Knoten.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten.</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        public NodeList(LogicalNode mother, JobList rootJobList)
          : base(mother, rootJobList)
        {
            this.LastSingleNodesLocker = new object();
            this.LastCountResultsLocker = new object();
            this.LastCountPositiveResultsLocker = new object();
            this.LastReturnedLogicalLocker = new object();
            this.ListLogicalStateLocker = new object();
            this.UserControlPath = rootJobList.NodeListUserControlPath;
            this.SnapshotUserControlPath = rootJobList.SnapshotUserControlPath;
            this.JobListUserControlPath = rootJobList.JobListUserControlPath;
            this.SingleNodeUserControlPath = rootJobList.SingleNodeUserControlPath;
            this.ConstantNodeUserControlPath = rootJobList.ConstantNodeUserControlPath;
            this.JobConnectorUserControlPath = rootJobList.JobConnectorUserControlPath;
            this._childrenString = null;
            if (ConfigurationManager.IsExpanded(this.IdPath) != null) // 15.02.2019 Nagel+
            {
                if (ConfigurationManager.IsExpanded(this.IdPath) == true)
                {
                    this.StartCollapsed = false;
                }
                else
                {
                    this.StartCollapsed = true;
                }
            } // 15.02.2019 Nagel-
        }

        /// <summary>
        /// Konstuktor
        /// </summary>
        /// <param name="id">Eindeutige Kennung des Knotens</param>
        /// <param name="mother">Id des Parent-Knotens.</param>
        /// <param name="rootJobList">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public NodeList(string id, LogicalNode mother, JobList rootJobList, TreeParameters treeParams)
          : base(id, mother, rootJobList, treeParams)
        {
            this.nOperands = 0;
            this.nPlus = 0;
            this.nMinus = 0;
            this.BreakWithResult = true;
            this.ListNodeState = NodeState.None;
            this.ListLogicalState = NodeLogicalState.None;
            this.LastSingleNodesLocker = new object();
            this.ThreadRefreshParentNodeLocker = new object();
            this.LastCountResultsLocker = new object();
            this.LastCountPositiveResultsLocker = new object();
            this.LastReturnedLogicalLocker = new object();
            this.ListLogicalStateLocker = new object();
            this.ThreadUpdateLastReturnedLogical(null);
            this.ThreadUpdateLastSingleNodes(-1); // invalidate
            this.ThreadUpdateLastCountTerminatedElements(-1); // invalidate
            this.ThreadUpdateLastCountResults(-1); // invalidate
            this.ThreadUpdateLastCountPositiveResults(-1); // invalidate
            if (this.Id == this.Name)
            {
                this.Name = "";
            }
            if (rootJobList != null)
            {
                this.UserControlPath = rootJobList.NodeListUserControlPath;
                this.SnapshotUserControlPath = rootJobList.SnapshotUserControlPath;
                this.JobListUserControlPath = rootJobList.JobListUserControlPath;
                this.SingleNodeUserControlPath = rootJobList.SingleNodeUserControlPath;
                this.ConstantNodeUserControlPath = rootJobList.ConstantNodeUserControlPath;
            }
            if (ConfigurationManager.IsExpanded(this.IdPath) != null) // 15.02.2019 Nagel+
            {
                if (ConfigurationManager.IsExpanded(this.IdPath) == true)
                {
                    this.StartCollapsed = false;
                }
                else
                {
                    this.StartCollapsed = true;
                }
            } // 15.02.2019 Nagel-
            this._childrenString = null;
        }

        /// <summary>
        /// Setzt bestimmte Eigenschaften auf die Werte der übergebenen LogicalNode "source". 
        /// </summary>
        /// <param name="source">LogicalNode mit zu übernehmenden Eigenschaften.</param>
        public override void InitFromNode(LogicalNode source)
        {
            base.InitFromNode(source);
            if (source != null && source is NodeList)
            {
                this.ThreadUpdateLastSingleNodes((source as NodeList).LastSingleNodes);
                this.ThreadUpdateLastSingleNodesFinished((source as NodeList).LastSingleNodesFinished);
            }
        }

        /// <summary>
        /// Sorgt für eine sofortige Neu-Auswertung aller gecashten Zustände.
        /// </summary>
        public override void Refresh()
        {
            this.ThreadRefreshParentNode();
            base.Refresh();
        }

        /// <summary>
        /// Abbrechen der Task.
        /// Wenn der Knoten selber beschäftigt ist, dann diesen zum Abbruch veranlassen,
        /// ansonsten die Abbruch-Anforderung an alle Kinder weitergeben.
        /// </summary>
        /// <param name="userBreak">Bei True hat der Anwender das Break ausgelöst.</param>
        public override void Break(bool userBreak)
        {
            this.ThreadUpdateLastSingleNodes(-1); // invalidate
                                                  // zuerst alle Kinder stoppen ...
            foreach (LogicalNode child in this.Children)
            {
                child.Break(userBreak);
            }
            // ... dann die NodeList selbst stoppen.
            base.Break(userBreak);
        }


        #endregion public members

        #region protected members

        /// <summary>
        /// Zusätzlicher NodeState für NodeLists.
        /// Wird gesetzt, wenn sich für die Liste selbst ein veränderter
        /// Zustand ergibt. Dies passiert, wenn eine Sub-JobList auf
        /// Freigabe durch die für sie zuständige JobList wartet.
        /// </summary>
        protected volatile NodeState ListNodeState;

        /// <summary>
        /// Zusätzlicher NodeLogicalState für NodeLists.
        /// Wird gesetzt, wenn sich für die Liste selbst ein veränderter
        /// Zustand ergibt. Dies passiert, wenn eine wartende Sub-JobList
        /// abgebrochen wird.
        /// </summary>
        protected volatile NodeLogicalState ListLogicalState;

        /// <summary>
        /// Die letzte Anzahl Endknoten-Kinder eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        protected int LastSingleNodes { get; set; }

        /// <summary>
        /// Anzahl Kinder mit Ergebnis.
        /// </summary>
        protected int CountResults
        {
            get
            {
                lock (this.LastCountResultsLocker)
                {
                    if (this.LastCountResults >= 0)
                    {
                        return this.LastCountResults;
                    }
                    int lastCountResults =
                      this.Children.Where(c =>
                      {
                          if (this.IsVolatile)
                          {
                              return c.Logical != null;
                          }
                          else
                          {
                              return c.LastNotNullLogical != null;
                          }
                      }).Count();
                    int lastFaultResults = // 02.08.2018+
                      this.Children.Where(c =>
                      {
                          // 08.08.2018+ return c.LogicalState == NodeLogicalState.Fault;
                          return c.LastExceptions.Count > 0; // 08.08.2018- wegen Testfall WrongFalseException
                      }).Count(); // 02.08.2018-
                    this.LastCountResults = lastFaultResults == 0 ? lastCountResults : 0;
                    return this.LastCountResults;
                }
            }
        }

        /// <summary>
        /// Die letzte Anzahl beendeter Kinder eines Knotens mit Ergebnis True oder False.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        protected int LastCountResults { get; set; }

        /// <summary>
        /// Anzahl Kinder mit Ergebnis=True. 
        /// </summary>
        protected int CountPositiveResults
        {
            get
            {
                this.ThreadUpdateLastCountPositiveResults(
                  this.Children.Where(c =>
                  {
                      if (!this.IsVolatile)
                      {
                          return c.LastNotNullLogical == true;
                      }
                      else
                      {
                          return c.Logical == true;
                      }
                  }).Count());
                return this.LastCountPositiveResults;
            }
        }

        /// <summary>
        /// Die letzte Anzahl mit True beendeter Kinder eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        protected int LastCountPositiveResults { get; set; }

        /// <summary>
        /// Anzahl beendete Kinder.
        /// </summary>
        protected int CountTerminatedElements
        {
            get
            {
                this.ThreadUpdateLastCountTerminatedElements(
                  this.Children.Where(c =>
                  {
                      return c.State == NodeState.Finished;
                  }).Count());
                return this.LastCountTerminatedElements;
            }
        }

        /// <summary>
        /// Die letzte Anzahl beendeter Kinder eines Knotens.
        /// Für den Zugriff auf Zustände von Child-Knoten, ohne dort
        /// die Ermittlung der Zustände erneut anzustoßen.
        /// Senkt die Prozessorlast.
        /// </summary>
        protected int LastCountTerminatedElements { get; set; }

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastCountResults.
        /// </summary>
        protected object LastCountResultsLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastSingleNodes.
        /// </summary>
        protected object LastSingleNodesLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastCountResults.
        /// </summary>
        protected object LastCountPositiveResultsLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von LastReturnedLogical.
        /// </summary>
        protected object LastReturnedLogicalLocker;

        /// <summary>
        /// Dient zum kurzzeitigen Sperren von ListLogicalState.
        /// </summary>
        protected object ListLogicalStateLocker;

        /// <summary>
        /// Vergleichswert für die Erkennung von Veränderungen des Logical-Wertes unabhängig
        /// von technisch bedingten Initialisierungen (vor allem für Logging, LastLogical
        /// würde technisch bedingt zu oft auslösen).
        /// </summary>
        protected bool? LastReturnedLogical;

        /// <summary>
        /// Überschriebene RUN-Logik.
        /// Für NodeList bedeutet das: Einhängen in die Events
        /// und weitergabe des Aufrufs an die Kinder.
        /// Diese Routine wird asynchron ausgeführt.
        /// </summary>
        /// <param name="source">Bei abhängigen Checkern das auslösende TreeEvent.</param>
        protected override void DoRun(TreeEvent source)
        {
            if (source != null && source.Name != "UserRun" && this.TriggeredRunDelay > 0)
            {
                Thread.Sleep(this.TriggeredRunDelay);
            }
            if (this.InitNodes)
            {
                this.ResetAllTreeNodes();
            }
            if (source == null || source.Name != "UserRun")
            {
                source = new TreeEvent("PseudoTreeEvent", this.Id, this.Id, this.Name, this.Path, this.LastNotNullLogical, this.LastLogicalState, null, this.GetResultsFromEnvironment());
            }
            if (this._childrenString == null)
            {
                StringBuilder stringBuilder = new StringBuilder("");
                string delimiter = "";
                foreach (LogicalNode child in this.Children)
                {
                    stringBuilder.Append(delimiter + child.ToString());
                    delimiter = ", ";
                }
                this._childrenString = stringBuilder.ToString();
            }
            foreach (LogicalNode child in this.Children)
            {
                if ((this.CancellationToken != null && this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    break;
                }
                child.Run(source);
            }
            if (this.Trigger != null)
            {
                this.State = NodeState.Triggered;
            }
            else
            {
                this.State = NodeState.Finished;
            }
        }

        /// <summary>
        /// Setzt threadsafe LastSingleNodes.
        /// </summary>
        /// <param name="singleNodes">Neuer Wert</param>
        protected void ThreadUpdateLastSingleNodes(int singleNodes)
        {
            lock (this.LastSingleNodesLocker)
            {
                this.LastSingleNodes = singleNodes;
            }
        }

        /// <summary>
        /// Setzt threadsafe LastCountTerminatedElements.
        /// </summary>
        /// <param name="countTerminatedElements">Neuer Wert</param>
        protected void ThreadUpdateLastCountTerminatedElements(int countTerminatedElements)
        {
            lock (this.ThreadRefreshParentNodeLocker)
            {
                this.LastCountTerminatedElements = countTerminatedElements;
            }
        }

        /// <summary>
        /// Setzt threadsafe LastCountResults.
        /// </summary>
        /// <param name="countResults">Neuer Wert</param>
        protected void ThreadUpdateLastCountResults(int countResults)
        {
            lock (this.LastCountResultsLocker)
            {
                this.LastCountResults = countResults;
            }
        }

        /// <summary>
        /// Setzt threadsafe LastCountPositiveResults.
        /// </summary>
        /// <param name="countPositiveResults">Neuer Wert</param>
        protected void ThreadUpdateLastCountPositiveResults(int countPositiveResults)
        {
            lock (this.LastCountPositiveResultsLocker)
            {
                this.LastCountPositiveResults = countPositiveResults;
            }
        }

        /// <summary>
        /// Setz threadsafe ListLogicalState.
        /// </summary>
        /// <param name="newLogicalState">Neuer Wert (NodeLogicalState?)</param>
        protected void ThreadUpdateListLogicalState(NodeLogicalState newLogicalState)
        {
            lock (this.ListLogicalStateLocker)
            {
                this.ListLogicalState = newLogicalState;
            }
        }

        /// <summary>
        /// Setz threadsafe LastReturnedLogical.
        /// </summary>
        /// <param name="newLogical">Neuer Wert (bool?)</param>
        protected void ThreadUpdateLastReturnedLogical(bool? newLogical)
        {
            lock (this.LastReturnedLogicalLocker)
            {
                this.LastReturnedLogical = newLogical;
                this.LastNotNullLogical = newLogical;
            }
        }

        /// <summary>
        /// Sorgt dafür, dass die NodeList ihre Werte neu ermittelt und sich neu darstellt.
        /// </summary>
        internal override void ThreadRefreshParentNode()
        {
            lock (this.ThreadRefreshParentNodeLocker)
            {
                this.LastCountTerminatedElements = -1; // invalidate
                this.LastSingleNodesFinished = -1; // invalidate
                this.LastCountResults = -1; // invalidate
                this.LastCountPositiveResults = -1; // invalidate
                this.LastCountPositiveResults = -1; // invalidate
            }
        }

        /*
        /// <summary>
        /// Wird angesprungen, wenn sich das logische Ergebnis eines Kindes geändert hat.
        /// </summary>
        /// <param name="sender">Kind-Knoten</param>
        /// <param name="logical">Logisches Ergebnis (null, false, true)</param>
        protected override void SubNodeLogicalChanged(LogicalNode sender, bool? logical)
        {
          this.ThreadUpdateLastCountResults(-1); // invalidate
          this.ThreadUpdateLastCountPositiveResults(-1); // invalidate
          base.SubNodeLogicalChanged(sender, logical);
        }
        */

        /// <summary>
        /// Wird angesprungen, wenn sich der logische Zustand "LastNotNullLogical" eines Kindes geändert hat.
        /// Dieses Event ist entscheidend für eine mögliche Änderung des logischen Zustandes des Parent-Knoten.
        /// Kann von SingleNodes und ParentNodes bis hin zum Root-Knoten ausgelöst werden.
        /// Ursprünglicher Auslöser ist immer eine SingleNode.
        /// Kaskadiert u.U. im Tree bis zum Root-Knoten nach oben.
        /// </summary>
        /// <param name="source">Der auslösende Kind-Knoten.</param>
        /// <param name="lastNotNullLogical">Letztes relevantes logisches Ergebnis des Kind-Knotens.</param>
        /// <param name="eventId">Eindeutige Kennung des Events.</param>
        protected override void SubNodeLastNotNullLogicalChanged(LogicalNode source, bool? lastNotNullLogical, Guid eventId)
        {
            this.ThreadRefreshParentNode();
            base.SubNodeLastNotNullLogicalChanged(source, lastNotNullLogical, eventId);
        }

        /// <summary>
        /// Wird aufgerufen, wenn sich das Result eines Knotens geändert hat.
        /// Dient dazu, die Berechnung des logischen Zustands dieser NodeList
        /// neu zu starten; bei &lt;=&gt; Vergleichen wichtig.
        /// </summary>
        /// <param name="sender">Die Ereignis-Quelle.</param>
        /// <param name="result">Das neue Result.</param>
        protected override void SubNodeResultChanged(LogicalNode sender, Result result)
        {
            base.SubNodeResultChanged(sender, result);
            if (sender.LastLogical != null || (sender is NodeConnector))
            {
                this.SubNodeLogicalChanged(sender, sender.LastLogical);
            }
        }

        #endregion protected members

        #region private members

        private string _userControlPath;
        private string _childrenString;

        #endregion private members

        public static string EmergencyDebug;

    }
}
