using System;
using System.Collections.Generic;
using System.Linq;
using NetEti.ExpressionParser;
using Vishnu.Interchange;
using NetEti.ApplicationControl;
using System.Text;
using System.Text.RegularExpressions;

namespace LogicalTaskTree
{
    /// <summary>
    /// Root eines (Teil-)Baums enes LogicalTaskTree.
    /// Hier werden die Logik, Bedingungen, Status für einen (Teil-)Baum verwaltet.
    /// Diese Klasse wird von außen mit der Logik und den Details
    /// (Worker, Namen, boolescher Ausdruck, etc.) bestückt.
    /// </summary>
    /// <remarks>
    /// File: JobList.cs
    /// Autor: Erik Nagel
    ///
    /// 01.12.2012 Erik Nagel: erstellt
    /// 20.04.2019 GetTriggeringNodeIdAndTriggeredNodes implementiert.
    /// </remarks>
    public class JobList : NodeList
    {
        #region public members

        /// <summary>
        /// Der logische Ausdruck, der durch diese JobList abgearbeitet wird.
        /// Die Variablen entsprechen Checker-Knoten. 
        /// </summary>
        public string LogicalExpression
        {
            get
            {
                if (this.Job != null)
                {
                    return this.Job.LogicalExpression;
                }
                else
                {
                    return this._presetLogicalExpression;
                }
            }
            set
            {
                if (this.Job != null)
                {
                    this.Job.LogicalExpression = value;
                }
                else
                {
                    this._presetLogicalExpression = value;
                }
            }
        }

        /// <summary>
        /// Ein optionaler Trigger, der steuert, wann ein Snapshot des Jobs erstellt
        /// werden soll oder null.
        /// </summary>
        public INodeTrigger SnapshotTrigger { get; set; }

        /// <summary>
        /// Bei true ist diese Joblist ein JobController, das heist u.a.:
        /// Run resettet den Teilbaum (setzt LastNotNullLogical auf null)
        /// und startet den Knoten sofort (nicht nur einen eventuellen Trigger).
        /// Default: false.
        /// </summary>
        public bool IsConrolled { get; set; }

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
                    return this.RootJobList.JobListUserControlPath;
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
        public override string SnapshotUserControlPath
        {
            get
            {
                if (this._snapshotUserControlPath != null)
                {
                    return this._snapshotUserControlPath;
                }
                else
                {
                    return this.RootJobList.SnapshotUserControlPath;
                }
            }
            set
            {
                this._snapshotUserControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden NodeListUserControls.
        /// </summary>
        public override string JobConnectorUserControlPath
        {
            get
            {
                if (this._jobConnectorUserControlPath != null)
                {
                    return this._jobConnectorUserControlPath;
                }
                else
                {
                    return this.RootJobList.JobConnectorUserControlPath;
                }
            }
            set
            {
                this._jobConnectorUserControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden NodeListUserControls.
        /// </summary>
        public string NodeListUserControlPath
        {
            get
            {
                if (this._nodeListUserControlPath != null)
                {
                    return this._nodeListUserControlPath;
                }
                else
                {
                    return this.RootJobList.NodeListUserControlPath;
                }
            }
            set
            {
                this._nodeListUserControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden SingleNodeUserControls.
        /// </summary>
        public override string SingleNodeUserControlPath
        {
            get
            {
                if (this._singleNodeUserControlPath != null)
                {
                    return this._singleNodeUserControlPath;
                }
                else
                {
                    return this.RootJobList.SingleNodeUserControlPath;
                }
            }
            set
            {
                this._singleNodeUserControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden ConstantNodeUserControls.
        /// </summary>
        public override string ConstantNodeUserControlPath
        {
            get
            {
                if (this._constantNodeUserControlPath != null)
                {
                    return this._constantNodeUserControlPath;
                }
                else
                {
                    return this.RootJobList.ConstantNodeUserControlPath;
                }
            }
            set
            {
                this._constantNodeUserControlPath = value;
            }
        }

        /// <summary>
        /// Die höchste Hierarchietiefe von Unter-JobLists zu dieser JobList.
        /// Hat z.B. diese JobList keine untergeordnete JobList, ist
        /// MaxSubJobDepth = 0, bei einer Sub-JobList, die wiederum eine
        /// Sub-JobList hat, wäre MaxSubJobDepth = 2.
        /// </summary>
        public int MaxSubJobDepth
        {
            get
            {
                if (this.Job != null)
                {
                    return this.Job.MaxSubJobDepth;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Verzögerung in Millisekunden, bevor ein LogicalCanged-Event weitergegeben wird.
        /// Default: 0
        /// </summary>
        public int LogicalChangedDelay
        {
            get
            {
                if (this.Job != null)
                {
                    return this.Job.LogicalChangedDelay;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Konstruktor für ein Snapshot-Dummy-Element - übernimmt den Eltern-Knoten.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten.</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public JobList(LogicalNode mother, JobList rootJobList, TreeParameters treeParams)
          : base(mother, rootJobList, treeParams)
        {
            if (this.RootJobList == null)
            {
                this.RootJobList = this;
            }
            this.UserControlPath = rootJobList.JobListUserControlPath;
            this.SnapshotUserControlPath = rootJobList.SnapshotUserControlPath;
            this.NodeListUserControlPath = rootJobList.NodeListUserControlPath;
            this.SingleNodeUserControlPath = rootJobList.SingleNodeUserControlPath;
            this.ConstantNodeUserControlPath = rootJobList.ConstantNodeUserControlPath;
            this.JobConnectorUserControlPath = rootJobList.JobConnectorUserControlPath;
            this.IsConrolled = false;
            this.TreeExternalSingleNodes = new List<SingleNode>();
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="jobProvider">Die Datenquelle für den Job</param>
        public JobList(TreeParameters treeParams, IJobProvider jobProvider)
          : this(null, null, null, treeParams, jobProvider, new List<string>(), new Dictionary<string, JobList>())
        { }

        /// <summary>
        /// Startet alle TreeExternals zu dieser JobList und aller Sub-JobLists.
        /// </summary>
        public void RunTreeExternals()
        {
            foreach (SingleNode node in this.TreeExternalSingleNodes)
            {
                node.Run(new TreeEvent("UserRun", this.Id, this.Id, this.Name, this.Path, null, NodeLogicalState.None, null, null));
            }
        }

        /// <summary>
        /// Registriert alle getriggerten Knoten eines Teilbaums bei ihren Triggern.
        /// </summary>
        public override void RegisterTriggeredNodes()
        {
            foreach (LogicalNode child in this.TreeExternalSingleNodes)
            {
                child.RegisterTriggeredNodes();
            }
            base.RegisterTriggeredNodes();
        }

        ///// <summary>
        ///// Startet die Verarbeitung eines (Teil-)Baums nach einem Start
        ///// durch den Anwender. Gibt die Information, dass der Start
        ///// durch den Anwender erfolgte, im TreeEvent an Run weiter.
        ///// </summary>
        //public override void UserRun()
        //{
        //  base.UserRun();
        //  this.RunTreeExternals();
        //}

        /// <summary>
        /// Überschreibt die Run-Logik aus LogicalNode um ggf.
        /// noch einen vorhandenen SnapshotTrigger und ggf. 
        /// TreeExternals zu starten.
        /// </summary>
        /// <param name="source">Bei abhängigen Checkern das auslösende TreeEvent.</param>
        public override void Run(TreeEvent source)
        {
            if (this.SnapshotTrigger != null)
            {
                this.SnapshotTrigger.Stop(this, this.requestSnapshot);
                this.SnapshotTrigger.Start(this, null, this.requestSnapshot);
            }
            base.Run(source);
        }

        /// <summary>
        /// Stoppt alle TreeExternals zu dieser JobList und aller Sub-JobLists.
        /// </summary>
        /// <param name="userBreak">Bei True hat der Anwender das Break ausgelöst.</param>
        public void BreakTreeExternals(bool userBreak)
        {
            foreach (SingleNode node in this.TreeExternalSingleNodes)
            {
                node.Break(userBreak);
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn der Teilbaum vom Anwender bewusst gestoppt wurde.
        /// </summary>
        /// <param name="userBreak">Bei True hat der Anwender das Break ausgelöst.</param>
        public override void Break(bool userBreak)
        {
            this.BreakTreeExternals(userBreak);
            base.Break(userBreak);
        }

        /// <summary>
        /// Liefert die für den Knoten gültige, oberste Root-JobList.
        /// </summary>
        /// <returns>Die für den Knoten gültige, oberste Root-JobList.</returns>
        public override JobList GetTopRootJobList()
        {
            if (this.RootJobList != this)
            {
                return this.RootJobList.GetTopRootJobList();
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Gibt über InfoController den aktuellen Tree mit den aktuellen Zuständen seiner Knoten aus.
        /// Kann für Debug-Zwecke genutzt werden.
        /// </summary>
        public void PublishAllTreeInfos()
        {
            string message = String.Join(System.Environment.NewLine, this.GetAllTreeInfos());
            IInfoPublisher publisher = InfoController.GetInfoPublisher();
            publisher.Publish(this, message, InfoType.NoRegex);
        }

        /// <summary>
        /// Liefert eine Zusammenfassung des aktuellen Trees
        /// mit den aktuellen Zuständen seiner Knoten als String-List.
        /// Kann für Debug-Zwecke genutzt werden.
        /// </summary>
        /// <returns>Zusammenfassung des aktuellen Trees mit den aktuellen Zuständen seiner Knoten als String-List.</returns>
        public List<string> GetAllTreeInfos()
        {
            List<string> allTreeInfos = new List<string>();
            allTreeInfos.Add("------------------------------------------------------------------------------------------------------------------------");
            allTreeInfos.Add(this.LogicalExpression);
            allTreeInfos.Add("------------------------------------------------------------------------------------------------------------------------");
            allTreeInfos.Add(this.ShowFlatSyntaxTree());
            allTreeInfos.Add("------------------------------------------------------------------------------------------------------------------------");
            //allTreeInfos.AddRange(this._root.ShowSyntaxTree().ToArray()); // es gibt leider kein AddRange
            this.ShowSyntaxTree().ToArray().FirstOrDefault(a => { allTreeInfos.Add(a); return false; });
            allTreeInfos.Add("------------------------------------------------------------------------------------------------------------------------");
            this.Show("  ").ToArray().FirstOrDefault(a => { allTreeInfos.Add(a); return false; });
            return allTreeInfos;
        }

        /// <summary>
        /// Gibt den (Teil-)Baum in eine StringList aus.
        /// </summary>
        /// <param name="indent">Einrückung pro Hierarchiestufe.</param>
        /// <returns>Baumdarstellung in einer StringList</returns>
        public List<string> Show(string indent)
        {
            this._indent = indent;
            this._treeStringList.Clear();
            this.Traverse(this.element2StringList);
            return this._treeStringList;
        }

        /// <summary>
        /// Gibt den zugehörigen Boolean-Tree in eine StringList aus.
        /// </summary>
        /// <returns>Baumdarstellung in einer StringList</returns>
        public List<string> ShowSyntaxTree()
        {
            return this.syntaxTree.Show("  ");
        }

        /// <summary>
        /// Gibt den verarbeiteten booleschen Ausdruck auf Basis des boolean-Trees
        /// wiederum einzeilig als logischen Ausdruck aus.
        /// </summary>
        /// <returns>Ein einzeiliger boolescher Ausdruck</returns>
        public string ShowFlatSyntaxTree()
        {
            return this.syntaxTree.ShowFlat();
        }

        #region tree globals

        /// <summary>
        /// Der externe Job mit logischem Ausdruck und u.a. Dictionary der Worker. 
        /// </summary>
        public Job Job { get; set; }

        /// <summary>
        /// Dictionary von externen Prüfroutinen für einen jobPackage.Job mit Namen als Key.
        /// Wird als Lookup für unaufgelöste JobConnector-Referenzen genutzt.
        /// </summary>
        public Dictionary<string, NodeCheckerBase> AllCheckersForUnreferencingNodeConnectors { get; set; }

        /// <summary>
        /// Liste von NodeConnectoren, die beim Parsen der Jobs noch nicht aufgelöst
        /// werden konnten.
        /// </summary>
        public List<NodeConnector> UnsatisfiedNodeConnectors;

        /// <summary>
        /// Dictionary von externen Prüfroutinen für eine JobList, die nicht in
        /// der LogicalExpression referenziert werden; Checker, die ausschließlich
        /// über ValueModifier angesprochen werden.
        /// </summary>
        public Dictionary<string, NodeCheckerBase> TreeExternalCheckers { get; set; }

        /// <summary>
        /// Liste von externen SingleNodes für die TopRootJobList, die in keiner
        /// der LogicalExpressions referenziert werden; Nodes, die ausschließlich
        /// über NodeConnectoren angesprochen werden.
        /// </summary>
        public List<SingleNode> TreeExternalSingleNodes { get; set; }

        /// <summary>
        /// Cache zur Beschleunigung der Verarbeitung von TreeEvents
        /// bezogen auf EventTrigger.
        /// </summary>
        public List<string> TriggerRelevantEventCache;

        /// <summary>
        /// Cache zur Beschleunigung der Verarbeitung von TreeEvents
        /// bezogen auf Worker.
        /// </summary>
        public List<string> WorkerRelevantEventCache;

        /// <summary>
        /// Cache zur Beschleunigung der Verarbeitung von TreeEvents
        /// bezogen auf Logger.
        /// </summary>
        public List<string> LoggerRelevantEventCache;

        /// <summary>
        /// Dictionary von JobLists mit ihren Namen als Keys.
        /// </summary>
        public Dictionary<string, JobList> JobsByName;

        /// <summary>
        /// Dictionary von LogicalNodes mit ihren Namen als Keys.
        /// </summary>
        public Dictionary<string, LogicalNode> NodesByName;

        /// <summary>
        /// Dictionary von LogicalNodes mit ihren Namen als Keys.
        /// </summary>
        public Dictionary<string, LogicalNode> TreeRootLastChanceNodesByName;

        /// <summary>
        /// Dictionary von LogicalNodes mit ihren Ids als Keys.
        /// </summary>
        public Dictionary<string, LogicalNode> NodesById;

        #endregion tree globals

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Verkettete Properties als String.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.ToString());
            string logicalExpressionReducedSpaces = Regex.Replace(Regex.Replace(this.LogicalExpression ?? "", "\\r?\\n", " "), @"\s{2,}", " ");
            stringBuilder.AppendLine(String.Format($"    LogicalExpression: {logicalExpressionReducedSpaces}"));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieser JobList nach logischen Gesichtspunkten
        /// mit dem Inhalt einer übergebenen JobList.
        /// </summary>
        /// <param name="obj">Die JobList zum Vergleich.</param>
        /// <returns>True, wenn die übergebene JobList inhaltlich gleich dieser ist.</returns>
        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            return this.LogicalExpression == (obj as JobList).LogicalExpression;
        }

        /// <summary>
        /// Erzeugt einen Hashcode für diese JobList.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode() + this.LogicalExpression.GetHashCode();
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Bei True befindet sich diese JobList in einer kurzen Wartephase
        /// vor Veränderung des eigenen logischen Zustands. Hintergrund:
        /// Kinder, die zueinander in der Beziehung Node - NodeConnector
        /// stehen, lösen im Millisekunden-Bereich zu unterschiedlichen
        /// Zeiten aus. Wenn besitzende JobList sofort reagieren würde,
        /// könnten abhängige Worker oder TreeEvents zwischen der
        /// Zustandsänderung von Node und der von NodeConnector auslösen,
        /// obwohl am Ende keine relevante logische Änderung stattgefunden
        /// hat. Die Wartezeit wird über den Parameter "LogicalChangedDelay"
        /// gesetzt.
        /// </summary>
        internal volatile bool IsChangeEventPending;

        /// <summary>
        /// Gibt ein Array von Workern (oder null) zurück, die zum übergebenen Key passen.
        /// Der Key setzt sich aus Node.Id + ":" + Node.Logical.ToString() zusammen.
        /// </summary>
        /// <param name="idLogical">Node.Id + ":" + Node.Logical.ToString()</param>
        /// <returns>Array von Workern (oder null)</returns>
        internal WorkerShell[] GetWorkers(string idLogical)
        {
            if (this.Job != null && this.Job.Workers.ContainsCombinedKey(idLogical))
            {
                return this.Job.Workers[idLogical];
            }
            else
            {
                return new WorkerShell[0];
            }
        }

        /// <summary>
        /// Speichert von abgeschlossenen NodeLists zurückgegebene
        /// ResultLists, die evtl. von danach laufenden Checkern als
        /// Parameter benötigt werden könnten.
        /// </summary>
        /// <param name="results">Kopie der zu speichernden ResultList eines Knotens einer Nodelist.</param>
        internal void RegisterResults(Dictionary<string, Result> results)
        {
            lock (this.ResultLocker)
            {
                foreach (string id in results.Keys)
                {
                    Result res = results[id];
                    string combinedKey = this.Id + "." + id;
                    this.UnregisterResult(combinedKey);
                    this._results.TryAdd(combinedKey, res);
                }
            }
            if (this != this.RootJobList)
            {
                this.RootJobList.RegisterResults(results);
            }
        }

        /// <summary>
        /// Speichert von abgeschlossenen SingleNodes (Workern) zurückgegebene
        /// Result-Objekte, die evtl. von danach laufenden Workern als
        /// Parameter benötigt werden könnten.
        /// </summary>
        /// <param name="result">Das zu speichernde Result-Objekt eines Knotens (enthält die Id des Knotens).</param>
        internal void RegisterResult(Result result)
        {
            lock (this.ResultLocker) // NEU
            {
                string combinedKey = this.Id + "." + result.Id;
                this.UnregisterResult(combinedKey);
                this._results.TryAdd(combinedKey, result);
            }
            if (this != this.RootJobList)
            {
                this.RootJobList.RegisterResults(this._results.ToDictionary(x => x.Key, x => x.Value));
            }
        }

        /// <summary>
        /// Löscht das Result-Objekt aus der Liste, welches zur übergebenen Id gehört.
        /// </summary>
        /// <param name="key">Kennung des Knotens, dem das zu löschende Result-Objekt ggehört.</param>
        internal void UnregisterResult(string key)
        {
            if (this._results.ContainsKey(key))
            {
                Result outDummy;
                this._results.TryRemove(key, out outDummy);
            }
        }

        /// <summary>
        /// Liefert für eine anfragende SingleNode (Checker) das Verarbeitungsergebnis
        /// (object) eines vorher beendeten Checkers als Eingangsparameter.
        /// </summary>
        /// <param name="nodeId">Die Id des Knotens, der ein Result-Objekt hier abgelegt hat.</param>
        /// <returns>Das Result-Objekt des Knotens.</returns>
        internal Result GetResult(string nodeId)
        {
            if (nodeId != null && this._results != null && this._results.ContainsKey(nodeId))
            {
                return this._results[nodeId];
            }
            return null;
        }

        /// <summary>
        /// Liefert ein Dictionary mit allen Ids von auslösenden (triggering) Knoten (Keys)
        /// und zugehörigen, abhängigen (triggered) Knoten (können jeweils mehrere sein).
        /// Wenn keine Trigger exisitieren wird ein leeres Dictionary zurückgegeben.
        /// </summary>
        /// <returns>Ein Dictionary mit allen Ids von auslösenden (triggering) Knoten (Keys)
        /// und zugehörigen, abhängigen (triggered) Knoten (können jeweils mehrere sein).</returns>
        internal Dictionary<string, List<LogicalNode>> GetTriggeringNodeIdAndTriggeredNodes() // 06.04.2019 TEST
        {
            Dictionary<string, List<LogicalNode>> res = new Dictionary<string, List<LogicalNode>>();
            if (this.Job != null && this.Job.EventTriggers != null)
            {
                // Das Event selbst interessiert hier nicht weiter.
                foreach (Dictionary<string, TriggerShell> triggerIdTriggerShell in this.Job.EventTriggers.Values)
                {
                    // Der Key interressiert hier nicht weiter, da er sich im TreeEventTrigger
                    // der TriggerShell als ReferencedNodeId noch mal wiederfindet.
                    foreach (TriggerShell triggerShell in triggerIdTriggerShell.Values)
                    {
                        TreeEventTrigger trigger = triggerShell.GetTreeEventTrigger();
                        if (trigger != null)
                        {
                            string triggeringNodeId = trigger.ReferencedNodeId;
                            // string triggeredNodeId = trigger.OwnerId;
                            if (!res.ContainsKey(triggeringNodeId))
                            {
                                res.Add(triggeringNodeId, new List<LogicalNode>());
                            }
                            foreach (LogicalNode triggeredNode in triggerShell.RegisteredMayBeTriggeredNodePathes.Values)
                            {
                                if (!res[triggeringNodeId].Contains(triggeredNode))
                                {
                                    res[triggeringNodeId].Add(triggeredNode);
                                }
                            }
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Sucht nach zuständigen Triggern für ein Event.
        /// </summary>
        /// <param name="eventName">Der Name des zugehörigen Events.</param>
        /// <param name="senderId">Die Id des Verbreiters des Events.</param>
        /// <param name="triggers">Eine Liste von für das Event zuständigen Triggern.</param>
        internal void FindEventTriggers(string eventName, string senderId, List<TreeEventTrigger> triggers)
        {
            if (this.TreeRootJobList.Job != null && this.TreeRootJobList.Job.EventTriggers != null)
            {
                foreach (string eventsString in this.TreeRootJobList.Job.EventTriggers.Keys)
                {
                    if (eventsString.Contains(eventName))
                    {
                        string fullEventsString = eventsString;
                        if (this.TreeRootJobList.Job.EventTriggers[fullEventsString].ContainsKey(senderId))
                        {
                            if (!triggers.Contains(this.TreeRootJobList.Job.EventTriggers[fullEventsString][senderId].GetTreeEventTrigger()))
                            {
                                triggers.Add(this.TreeRootJobList.Job.EventTriggers[fullEventsString][senderId].GetTreeEventTrigger());
                            }
                        }
                    }
                }
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
        internal override List<WorkerShell[]> FindEventWorkers(string eventName, string senderId, string sourceId, bool allEvents)
        {
            List<WorkerShell[]> workers = new List<WorkerShell[]>();
            if (eventName.StartsWith("Any") || senderId == sourceId)
            {
                this.FindEventWorkers(eventName, senderId, allEvents, workers);
            }
            return workers;
        }

        /// <summary>
        /// Sucht nach zuständigen WorkerShells mit deren zugeordneten Worker-Arrays für ein Event.
        /// </summary>
        /// <param name="eventName">Der Name des zugehörigen Events.</param>
        /// <param name="senderId">Die Id des Knotens, der das Events weitergibt.</param>
        /// <param name="allEvents">Bei True werden alle Worker für einen Knoten unabhängig vom event zurückgegeben.</param>
        /// <param name="workers">Eine Liste von für das/die Event(s) zuständigen Worker-Arrays.</param>
        /// <returns>Eine Liste von für das Event zuständigen Worker-Arrays.</returns>
        internal List<WorkerShell[]> FindEventWorkers(string eventName, string senderId, bool allEvents, List<WorkerShell[]> workers)
        {
            if (this.Job != null && this.Job.Workers != null)
            {
                foreach (string eventsString in this.Job.Workers.Keys)
                {
                    if (eventsString.Contains(eventName) || allEvents)
                    {
                        string fullEventsString = eventsString;
                        if (this.Job.Workers.ContainsCombinedKey(senderId + ":" + fullEventsString))
                        {
                            WorkerShell[] workerArray = this.Job.Workers[senderId + ":" + fullEventsString];
                            workers.Add(workerArray);
                        }
                    }
                }
            }
            if (this.RootJobList != this)
            {
                this.RootJobList.FindEventWorkers(eventName, senderId, allEvents, workers);
            }
            return workers;
        }

        /// <summary>
        /// Gibt true zurück, wenn der Knoten mit SenderId aktuell im Cache
        /// für die laufenden Worker, bzw. zuletzt ausgelösten enthalten ist.
        /// </summary>
        /// <param name="senderId">Id des zu prüfenden Knotens.</param>
        /// <param name="eventName">Name eines zurücksetzenden Events, das laufende Worker mit ok-Meldung beendet.#
        /// oder mit ok-Meldung abschließend startet.</param>
        /// <returns>True, wenn die senderId aktuell im Cache ist.</returns>
        internal bool SendersOfLastExecutedWorkersContainsNodeAndEvent(string senderId, string eventName)
        {
            lock (this._lastExecutedWorkersSendersEventsLocker)
            {
                if (!this._lastExecutedWorkersSendersEvents.ContainsKey(senderId))
                {
                    return false;
                }
                foreach (KeyValuePair<string, List<WorkerShell>> eventsWorkers in this._lastExecutedWorkersSendersEvents[senderId])
                {
                    if (eventsWorkers.Key.Contains(eventName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Fügt die WorkerShell "worker" in die Liste der Worker zu den "eventNames",
        /// die zu der Event-Liste des auslösenden Knotens "senderId" gehören, ein.
        /// "eventNames" sind die Namen von Komplementär-Events zu dem ursprünglich den Worker
        /// auslösenden Event, die den laufenden Worker später wieder zurücksetzen (wieder-ok-Meldung),
        /// bzw. den Worker später abschließend mit ok-Meldung auslösen sollen.
        /// Retourniert true, wenn die WorkerShell neu eingefügt wurde.
        /// </summary>
        /// <param name="senderId">Id des Knoten, der das auslösende Event gesendet hat.</param>
        /// <param name="eventNames">Namen von Komplementär-Events zum auslösenden Event (mit '|' getrennt),
        /// die den laufenden Worker später mit ok-Meldung beenden oder abschließend mit ok-Meldung starten sollen.</param>
        /// <param name="worker">Die WorkerShell des betroffenen Workers.</param>
        /// <returns>True, wenn "worker" zu "eventNames" zu "senderId" hinzugefügt wurde.</returns>
        internal bool TryAddWorkerToEventsOfSender(string senderId, string eventNames, WorkerShell worker)
        {
            lock (this._lastExecutedWorkersSendersEventsLocker)
            {
                if (!this._lastExecutedWorkersSendersEvents.ContainsKey(senderId))
                {
                    this._lastExecutedWorkersSendersEvents.Add(senderId, new Dictionary<string, List<WorkerShell>>());
                }
                if (!this._lastExecutedWorkersSendersEvents[senderId].ContainsKey(eventNames))
                {
                    this._lastExecutedWorkersSendersEvents[senderId].Add(eventNames, new List<WorkerShell>());
                }
                if (!this._lastExecutedWorkersSendersEvents[senderId][eventNames].Contains(worker))
                {
                    this._lastExecutedWorkersSendersEvents[senderId][eventNames].Add(worker);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Retourniert eine Liste der Worker zu dem Event "eventName" des auslösenden Knotens "senderId".
        /// </summary>
        /// <param name="senderId">Id des Knotens, der das zurücksetzende Event sendet.</param>
        /// <param name="eventName">Name eines zurücksetzenden Events, das laufende Worker mit ok-Meldung beendet
        /// oder mit ok-Meldung abschließend startet.</param>
        /// <returns>Liste von WorkerShells zu "senderId" und "eventName".</returns>
        internal List<WorkerShell> GetLastExecutedWorkersToSenderAndEvent(string senderId, string eventName)
        {
            // Knoten                       A
            // Event            false     true      Exception
            // Worker           X   Y       Z       T   U   V
            // ResettingEvent   true      false    true   false
            lock (this._lastExecutedWorkersSendersEventsLocker)
            {
                List<WorkerShell> workers = new List<WorkerShell>();
                if (this._lastExecutedWorkersSendersEvents.ContainsKey(senderId))
                {
                    foreach (KeyValuePair<string, List<WorkerShell>> eventsWorkers in this._lastExecutedWorkersSendersEvents[senderId])
                    {
                        if (eventsWorkers.Key.Contains(eventName))
                        {
                            foreach (WorkerShell worker in eventsWorkers.Value)
                            {
                                if (!workers.Contains(worker))
                                {
                                    workers.Add(worker);
                                }
                            }
                        }
                    }
                }
                return workers;
            }
        }

        /// <summary>
        /// Retourniert eine Liste der Worker zu dem Event "eventName" des auslösenden Knotens "senderId".
        /// </summary>
        /// <param name="senderId">Id des Knotens, der das zurücksetzende Event sendet.</param>
        /// <param name="eventName">Name eines zurücksetzenden Events, das laufende Worker mit ok-Meldung beendet
        /// oder mit ok-Meldung abschließend startet.</param>
        /// <returns>Liste von WorkerShells zu "senderId" und "eventName".</returns>
        internal bool TryRemoveEventFromSender(string senderId, string eventName)
        {
            // Knoten                       A
            // Event            false     true      Exception
            // Worker           X   Y       Z       T   U   V
            // ResettingEvent   true      false    true   false
            lock (this._lastExecutedWorkersSendersEventsLocker)
            {
                if (!this._lastExecutedWorkersSendersEvents.ContainsKey(senderId))
                {
                    return false;
                }
                bool found = false;
                List<string> eventsToRemove = new List<string>();
                foreach (KeyValuePair<string, List<WorkerShell>> eventsWorkers in this._lastExecutedWorkersSendersEvents[senderId])
                {
                    if (eventsWorkers.Key.Contains(eventName))
                    {
                        found = true;
                        eventsToRemove.Add(eventsWorkers.Key);
                    }
                }
                foreach (string eventNames in eventsToRemove)
                {
                    this._lastExecutedWorkersSendersEvents[senderId].Remove(eventNames);
                }
                if (this._lastExecutedWorkersSendersEvents[senderId].Count < 1)
                {
                    this._lastExecutedWorkersSendersEvents.Remove(senderId);
                }
                return found;
            }
        }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Überschriebene RUN-Logik.
        /// Für NodeList bedeutet das: Einhängen in die Events
        /// und weitergabe des Aufrufs an die Kinder.
        /// Diese Routine wird asynchron ausgeführt.
        /// </summary>
        /// <param name="source">Bei abhängigen Checkern das auslösende TreeEvent.</param>
        protected override void DoRun(TreeEvent source)
        {
            this.RunTreeExternals();
            base.DoRun(source);
        }

        /// <summary>
        /// Setzt alle Knoten im Teilbaum zurück.
        /// Setzt auch alle TreeExternals zurück.
        /// </summary>
        /// <param name="branch">Der oberste Knoten des zurückzusetzenden Teilbaums.</param>
        protected override void ResetPartTreeNodes(LogicalNode branch)
        {
            base.ResetPartTreeNodes(branch);
            foreach (SingleNode node in this.TreeExternalSingleNodes)
            {
                base.InitNode(0, node);
            }
        }

        /// <summary>
        /// Sucht in der aktuellen JobList und allen übergeodneten JobLists
        /// nach der Node mit der übergebenen 'nodeId'.
        /// Der erste Treffer gewinnt.
        /// </summary>
        /// <param name="nodeId">Die Id der zu suchenden Node.</param>
        /// <returns>Die gefundene LogicalNode oder null.</returns>
        public override LogicalNode FindNodeById(string nodeId)
        {
            if (this.NodesById.ContainsKey(nodeId))
            {
                return this.NodesById[nodeId];
            }
            else
            {
                if (this != this.RootJobList)
                {
                    return this.RootJobList.FindNodeById(nodeId);
                }
                else
                {
                    return this.Id == nodeId ? this : null;
                }
            }
        }

        /// <summary>
        /// Sucht nach zuständigen Triggern für ein Event.
        /// </summary>
        /// <param name="eventName">Der Name des zugehörigen Events.</param>
        /// <param name="senderId">Die Id des Verbreiters des Events.</param>
        /// <param name="sourceId">Die Id der Quelle des Events.</param>
        /// <returns>Eine Liste von für das Event zuständigen Triggern.</returns>
        protected override List<TreeEventTrigger> FindEventTriggers(string eventName, string senderId, string sourceId)
        {
            List<TreeEventTrigger> triggers = new List<TreeEventTrigger>();
            if (eventName.StartsWith("Any") || senderId == sourceId)
            {
                this.FindEventTriggers(eventName, senderId, triggers);
            }
            return triggers;
        }

        #endregion protected members

        #region private members

        private IJobProvider _jobProvider;

        // Der aus dem logischen Ausdruck des Jobs geparste Boolean-Tree.
        // Dieser dient als Bauplan für den LogicalTaskTree, wird aber darüber hinaus
        // nicht weiterverwendet.
        private SyntaxTree syntaxTree;

        private ResultList _results;
        private string _indent;
        private List<string> _treeStringList;
        private List<string> _parsedJobs;

        private Dictionary<string, Dictionary<string, List<WorkerShell>>> _lastExecutedWorkersSendersEvents;
        private string _presetLogicalExpression;
        private string _userControlPath;
        private string _snapshotUserControlPath;
        private string _jobConnectorUserControlPath;
        private string _nodeListUserControlPath;
        private string _singleNodeUserControlPath;
        private string _constantNodeUserControlPath;
        private object _lastExecutedWorkersSendersEventsLocker;

        /// <summary>
        /// Konstruktor für rekursive Aufrufe
        /// </summary>
        /// <param name="logicalName">Eindeutige Kennung des Knotens</param>
        /// <param name="mother">Id des Parent-Knotens.</param>
        /// <param name="rootJoblist">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="jobProvider">Die Datenquelle für den Job</param>
        /// <param name="parsedJobs">Liste von Namen aller bisher geparsten Jobs.</param>
        /// <param name="subJobs">Liste von Namen aller bisher verarbeiteten JobLists.</param>
        private JobList(string logicalName, LogicalNode mother, JobList rootJoblist, TreeParameters treeParams, IJobProvider jobProvider,
          List<string> parsedJobs, Dictionary<string, JobList> subJobs)
          : base(logicalName, mother, rootJoblist, treeParams)
        {
            this._parsedJobs = parsedJobs;
            this.JobsByName = subJobs;
            this.NodesByName = new Dictionary<string, LogicalNode>();
            this.TreeRootLastChanceNodesByName = new Dictionary<string, LogicalNode>();
            this.NodesById = new Dictionary<string, LogicalNode>();
            if (this._parsedJobs.Contains(logicalName))
            {
                throw new ApplicationException(String.Format("Rekursion bei ID: {0}!", logicalName));
            }
            this._jobProvider = jobProvider;
            this.Job = this._jobProvider.GetJob(ref logicalName);
            this.UserControlPath = this.Job.JobListUserControlPath;
            this.SnapshotUserControlPath = this.Job.SnapshotUserControlPath;
            this.JobConnectorUserControlPath = this.Job.JobConnectorUserControlPath;
            this.NodeListUserControlPath = this.Job.NodeListUserControlPath;
            this.SingleNodeUserControlPath = this.Job.SingleNodeUserControlPath;
            this.ConstantNodeUserControlPath = this.Job.ConstantNodeUserControlPath;
            this.Id = logicalName;
            this._parsedJobs.Add(logicalName);
            this.StartCollapsed = this.Job.StartCollapsed;
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
            this.InitNodes = this.Job.InitNodes;
            this.TriggeredRunDelay = this.Job.TriggeredRunDelay;
            this.ThreadLocked = this.Job.ThreadLocked;
            this.LockName = this.Job.LockName;
            this.IsVolatile = this.Job.IsVolatile;
            this._results = new ResultList();
            this._treeStringList = new List<string>();
            this.TriggerRelevantEventCache = new List<string>();
            //this.WorkerRelevantEventCache = new List<string>() { "Breaked", "Started" };
            this.WorkerRelevantEventCache = new List<string>() { "Breaked" };
            this._lastExecutedWorkersSendersEvents = new Dictionary<string, Dictionary<string, List<WorkerShell>>>();
            this._lastExecutedWorkersSendersEventsLocker = new object();
            this.LoggerRelevantEventCache = new List<string>();
            this.AllCheckersForUnreferencingNodeConnectors = this.Job.Checkers.ToDictionary(c => c.Key, c => c.Value);
            this.UnsatisfiedNodeConnectors = new List<NodeConnector>();
            this.TreeExternalSingleNodes = new List<SingleNode>();
            this.TreeExternalCheckers = this.Job.Checkers.Where(c => { return (c.Value is CheckerShell); }).ToDictionary(c => c.Key, c => c.Value);
            // Erst mal alle rein, werden beim Parsen bis auf diejenigen, die nicht in der LogicalExpression auftreten, entfernt.
            if (this.Job.JobSnapshotTrigger != null)
            {
                this.SnapshotTrigger = this.Job.JobSnapshotTrigger;
            }
            if (this.Job.JobTrigger != null)
            {
                this.Trigger = this.Job.JobTrigger;
                this.climb2Top(this.setBreakWithResult2False);
            }
            else
            {
                this.BreakWithResult = this.Job.BreakWithResult;
            }
            this.Logger = this.Job.JobLogger;
            this.parse();

            if (this.Mother != null)
            {
                foreach (NodeConnector nodeConnector in this.UnsatisfiedNodeConnectors)
                {
                    if (!this.RootJobList.AllCheckersForUnreferencingNodeConnectors.ContainsKey(nodeConnector.Name))
                    {
                        this.RootJobList.AllCheckersForUnreferencingNodeConnectors.Add(nodeConnector.Name, this.AllCheckersForUnreferencingNodeConnectors[nodeConnector.Name]);
                    }
                }
                foreach (string key in this.Job.EventTriggers.Keys)
                {
                    if (!this.RootJobList.Job.EventTriggers.ContainsKey(key))
                    {
                        this.RootJobList.Job.EventTriggers.Add(key, this.Job.EventTriggers[key]);
                    }
                    foreach (string nodeKey in this.Job.EventTriggers[key].Keys)
                    {
                        if (!this.RootJobList.Job.EventTriggers[key].ContainsKey(nodeKey))
                        {
                            this.RootJobList.Job.EventTriggers[key].Add(nodeKey, this.Job.EventTriggers[key][nodeKey]);
                        }
                    }
                }
            }
            foreach (string eventName in this.Job.Workers.Keys)
            {
                this.TreeRootJobList.tryAddEventsToCache(this.TreeRootJobList.WorkerRelevantEventCache, eventName);
            }
            if (this.Mother == null)
            {
                this.NodeType = NodeTypes.JobList;
                if (!this.TreeParams.Name.StartsWith("Shadow"))
                {
                    this.requestSnapshot(null);
                }
            }
        }

        private void requestSnapshot(TreeEvent source)
        {
            // Neu+
            // this.ThreadRefreshNodeList();

            /* // 19.08.2018+ auskommentiert
            if (this.LastExceptions.Count > 0)
            {
                this.LastNotNullLogical = null;
            }
            // Neu-
            */// 19.08.2018- auskommentiert

            SnapshotManager.RequestSnapshot(this);
        }

        /// <summary>
        /// 1. Stößt das Parsen des booleschen Ausdrucks an.
        /// 2. Baut nach dem Vorbild des Boolean-Tree den LogicalTask(Teil-)Tree
        /// </summary>
        private void parse()
        {
            this.syntaxTree = new LogicalParser().Parse(this.LogicalExpression);
            this.buildParallelTree(this, this.syntaxTree);
            foreach (string nodeName in this.TreeExternalCheckers.Keys)
            {
                NodeCheckerBase checker = this.TreeExternalCheckers[nodeName];
                SingleNode singleNode = createSingleNodeFromChecker(nodeName, this, this, this.TreeParams,
                    checker, this.ThreadLocked, this.BreakWithResult);
                this.AddToNodesById(singleNode.Id, (singleNode)); // 03.03.2019 Nagel+-
                this.TreeExternalSingleNodes.Add(singleNode);
            }
            this.resolveUnsatisfiedNodeConnectors();
            foreach (NodeConnector nodeConnector in this.UnsatisfiedNodeConnectors)
            {
                if (this.RootJobList != this)
                {
                    this.RootJobList.UnsatisfiedNodeConnectors.Add(nodeConnector); // nach oben weiterreichen,
                                                                                   // vielleicht am Schluss auflösbar.
                }
                else
                {
                    throw new ApplicationException(String.Format("JobList.Parse: mindestens eine Referenz konnte nicht aufgelöst werden ({0}).",
                      nodeConnector.Name));
                }
            }
            this.fillTriggerReferences();
            this.fillLoggerReferences();
        }

        // Erzeugt eine SingleNode nach einem Checker und zusätzlichen Parametern
        // und gibt diese zurück.
        private SingleNode createSingleNodeFromChecker(string nodeName, LogicalNode mother, JobList rootJobList, TreeParameters treeParams,
          NodeCheckerBase checker, bool threadLocked, bool breakWithResult)
        {
            SingleNode newChild = new SingleNode(nodeName, mother, rootJobList, treeParams);
            checker.ThreadLocked |= threadLocked;
            newChild.Checker = checker;
            newChild.NodeType = NodeTypes.Checker;
            if (newChild.Checker.CheckerTrigger != null)
            {
                newChild.Trigger = newChild.Checker.CheckerTrigger;
                newChild.BreakWithResult = false;
            }
            else
            {
                newChild.BreakWithResult = breakWithResult;
            }
            newChild.Logger = newChild.Checker.CheckerLogger;
            this.TryAddToNodesByName(newChild.Name, newChild);
            return newChild;
        }

        // Sucht nach der Instanz des Triggers mit dem Namen 'triggerReference'
        // in allen Triggers (Dictionaries) des aktuellen Jobs und aller übergeordneten Jobs.
        // Der erste Treffer gewinnt.
        private TriggerShell FindReferencedTrigger(string triggerReference, object triggerParameters, string ownerId)
        {
            JobList jobList = this;
            do
            {
                if (jobList.Job.Triggers.ContainsKey(triggerReference))
                {
                    TriggerShell triggerShell = jobList.Job.Triggers[triggerReference];
                    string transitiveTriggerReference = triggerShell.GetTriggerReference();
                    if (transitiveTriggerReference != null)
                    {
                        object transitiveTriggerParameters = triggerShell.GetTriggerParameters();
                        TriggerShell transitiveTriggerShell = this.createInternalEventsTrigger(transitiveTriggerReference, transitiveTriggerParameters, ownerId);
                        triggerShell.SetSlaveTriggerShell(transitiveTriggerShell);
                    }
                    return triggerShell;
                }
                if (jobList == jobList.RootJobList)
                {
                    break;
                }
                jobList = jobList.RootJobList;
            } while (true);
            return this.createInternalEventsTrigger(triggerReference, triggerParameters, ownerId);
        }

        // Erzeugt einen TreeEventTrigger und retourniert eine TriggerShell mit diesem TreeEventTrigger.
        // Retourniert null, wenn triggerReference keine TreeEvent-Namen enthält.
        private TriggerShell createInternalEventsTrigger(string triggerReference, object triggerParameters, string ownerId)
        {
            if (triggerParameters == null)
            {
                throw new ApplicationException(String.Format("Die Referenz '{0}' konnte nicht aufgelöst werden.", triggerReference));
            }
            string internalEvents = TreeEvent.GetInternalEventNamesForUserEventNames(triggerReference);
            if (!String.IsNullOrEmpty(internalEvents))
            {
                string nodeId = triggerParameters.ToString();
                if (!this.TreeRootJobList.Job.EventTriggers.ContainsKey(internalEvents))
                {
                    this.TreeRootJobList.Job.EventTriggers.Add(internalEvents, new Dictionary<string, TriggerShell>());
                    this.tryAddEventsToCache(this.TreeRootJobList.TriggerRelevantEventCache, internalEvents);
                }
                TreeEventTrigger newTrigger = null;
                TriggerShell newTriggerShell = null;
                if (!this.TreeRootJobList.Job.EventTriggers[internalEvents].ContainsKey(nodeId))
                {
                    newTrigger = new TreeEventTrigger(internalEvents, ownerId, triggerReference, nodeId);
                    newTriggerShell = new TriggerShell(internalEvents, newTrigger);
                    this.TreeRootJobList.Job.EventTriggers[internalEvents].Add(nodeId, newTriggerShell);
                }
                if (!this.Job.EventTriggers.ContainsKey(internalEvents))
                {
                    this.Job.EventTriggers.Add(internalEvents, new Dictionary<string, TriggerShell>());
                    this.tryAddEventsToCache(this.TreeRootJobList.TriggerRelevantEventCache, internalEvents);
                }
                if (!this.Job.EventTriggers[internalEvents].ContainsKey(nodeId))
                {
                    this.Job.EventTriggers[internalEvents].Add(nodeId, newTriggerShell);
                }
                return this.TreeRootJobList.Job.EventTriggers[internalEvents][nodeId];
            }
            return null;
        }

        private void tryAddEventsToCache(List<string> list, string internalEvents)
        {
            foreach (string eventName in internalEvents.Split('|'))
            {
                if (!list.Contains(eventName))
                {
                    list.Add(eventName);
                }
            }
        }

        // Sucht für CheckerShells, die nicht direkt einen Trigger zugeordnet haben, aber eine namentliche
        // Referenz auf einen Trigger besitzen, der woanders instanziiert wurde, diese Trigger-Instanz
        // und ordnet sie nachträglich den betreffenden CheckerShells zu.
        private void fillTriggerReferences()
        {
            string triggerReference = null;
            foreach (string id in this.Job.Checkers.Keys)
            {
                NodeCheckerBase node = this.Job.Checkers[id];
                if (node.CheckerTrigger != null && (triggerReference = node.CheckerTrigger.GetTriggerReference()) != null)
                {
                    node.CheckerTrigger.SetSlaveTriggerShell(this.FindReferencedTrigger(triggerReference, node.CheckerTrigger.GetTriggerParameters(), id));
                }
            }
            if (this.Job.JobSnapshotTrigger != null && (triggerReference = this.Job.JobSnapshotTrigger.GetTriggerReference()) != null)
            {
                this.Job.JobSnapshotTrigger.SetSlaveTriggerShell(this.FindReferencedTrigger(triggerReference, this.Job.JobSnapshotTrigger.GetTriggerParameters(), Id));
            }
            if (this.Job.JobTrigger != null && (triggerReference = this.Job.JobTrigger.GetTriggerReference()) != null)
            {
                this.Job.JobTrigger.SetSlaveTriggerShell(this.FindReferencedTrigger(triggerReference, this.Job.JobTrigger.GetTriggerParameters(), Id));
            }
        }

        // Sucht nach der Instanz des Loggers mit dem Namen 'loggerReference'
        // in allen Loggers (Dictionaries) des aktuellen Jobs und aller übergeordneten Jobs.
        // Der erste Treffer gewinnt.
        private LoggerShell FindReferencedLogger(string loggerReference)
        {
            JobList jobList = this;
            do
            {
                if (jobList.Job.Loggers.ContainsKey(loggerReference))
                {
                    return jobList.Job.Loggers[loggerReference];
                }
                if (jobList == jobList.RootJobList)
                {
                    break;
                }
                jobList = jobList.RootJobList;
            } while (true);
            return null;
        }

        // Sucht für CheckerShells, die nicht direkt einen Logger zugeordnet haben, aber eine namentliche
        // Referenz auf einen Logger besitzen, der woanders instanziiert wurde, diese Logger-Instanz
        // und ordnet sie nachträglich den betreffenden CheckerShells zu.
        private void fillLoggerReferences()
        {
            string loggerReference = null;
            foreach (NodeCheckerBase node in this.Job.Checkers.Values)
            {
                if (node.CheckerLogger != null)
                {
                    if ((loggerReference = node.CheckerLogger.GetLoggerReference()) != null)
                    {
                        node.CheckerLogger.SetSlaveLoggerShell(this.FindReferencedLogger(loggerReference));
                    }
                    string logEvents = node.CheckerLogger.GetLogEvents();
                    if (logEvents != null)
                    {
                        this.tryAddEventsToCache(this.TreeRootJobList.LoggerRelevantEventCache, logEvents);
                    }
                    else
                    {
                        string info = node.CheckerLogger.LoggerShellReference ?? (node.ReferencedNodeName ?? node.GetType().Name);
                        throw new ArgumentException(String.Format($"Fehler bei der Logger-Konfiguration ({info})!"));
                    }
                }
            }
            if (this.Job.JobLogger != null)
            {
                if ((loggerReference = this.Job.JobLogger.GetLoggerReference()) != null)
                {
                    LoggerShell loggerShell = this.FindReferencedLogger(loggerReference);
                    if (loggerShell == null)
                    {
                        throw new ArgumentException(String.Format($"Die Logger-Referenz {loggerReference} konnte nicht aufgelöst werden."));
                    }
                    this.Job.JobLogger.SetSlaveLoggerShell(loggerShell);
                }
                string logEvents = this.Job.JobLogger.GetLogEvents();
                if (logEvents != null)
                {
                    this.tryAddEventsToCache(this.TreeRootJobList.LoggerRelevantEventCache, logEvents);
                }
                else
                {
                    string info = this.Job.JobLogger.LoggerShellReference ?? (this.Job.JobLogger.LoggerParameters ?? this.Job.LogicalExpression);
                    throw new ArgumentException(String.Format($"Fehler bei der Logger-Konfiguration ({info})!"));
                }
            }
        }

        private void resolveUnsatisfiedNodeConnectors()
        {
            for (int i = 0; i < this.UnsatisfiedNodeConnectors.Count; i++)
            {
                NodeConnector actNodeConnector = this.UnsatisfiedNodeConnectors[i];
                string checkerReference = this.AllCheckersForUnreferencingNodeConnectors[actNodeConnector.Name].GetCheckerReference();
                if (checkerReference != null)
                {
                    // Es handelt sich um einen ValueModifier, der auf einen existierenden Checker verweist.
                    LogicalNode ownerOfReferencedChecker = this.FindNodeByName(checkerReference, NodeTypes.Checker | NodeTypes.ValueModifier, actNodeConnector.IsGlobal);
                    if (ownerOfReferencedChecker != null)
                    {
                        actNodeConnector.InitReferencedNode(ownerOfReferencedChecker);
                        this.UnsatisfiedNodeConnectors.RemoveAt(i--);
                    }
                }
            }
        }

        /// <summary>
        /// Baut nach dem Vorbild des Boolean-Tree den LogicalTask(Teil-)Tree.
        /// </summary>
        /// <param name="mother">Der übergeordnete Knoten des Teil-Baums</param>
        /// <param name="template">Der boolesche (Teil-)Baum (Bauplan)</param>
        private void buildParallelTree(NodeList mother, SyntaxTree template)
        {
            mother.nOperands = template.Children.Where(c => c.NodeType == SyntaxElement.NONE || c.NodeType == SyntaxElement.STRUCT).Count();
            for (int i = 0; i < template.Children.Count; i++)
            {
                SyntaxTree child = template.Children[i];
                switch (child.NodeType)
                {
                    case SyntaxElement.NONE:
                        LogicalNode newChild;
                        string userControlPath = null;
                        bool initNodes = false;
                        int triggeredRunDelay = 0;
                        if (child.NodeName.StartsWith(@"@") || this.Job.Checkers.ContainsKey(child.NodeName))
                        {
                            // Es handelt sich um ein Einzelelement (Checker oder Konstante).
                            if (this.Job.Checkers.ContainsKey(child.NodeName))
                            {
                                // Es handelt sich um einen Checker (keine Konstante).
                                userControlPath = (this.Job.Checkers[child.NodeName] as NodeCheckerBase).UserControlPath;
                                initNodes = (this.Job.Checkers[child.NodeName] as NodeCheckerBase).InitNodes;
                                triggeredRunDelay = (this.Job.Checkers[child.NodeName] as NodeCheckerBase).TriggeredRunDelay;
                                if (userControlPath == null)
                                {
                                    userControlPath = mother.SingleNodeUserControlPath;
                                }
                                string checkerReference = this.Job.Checkers[child.NodeName].GetCheckerReference();
                                bool isGlobal = this.Job.Checkers[child.NodeName].IsGlobal;
                                newChild = this.FindNodeByName(child.NodeName, NodeTypes.Checker | NodeTypes.ValueModifier, isGlobal); // Muss vom Knoten abhängen, nur bei Attribut IsGlobal auf true.
                                if (newChild == null)
                                {
                                    // Es gibt noch keinen Knoten mit diesem Checker.
                                    if (checkerReference != null)
                                    {
                                        // Es handelt sich um einen ValueModifier, der auf einen existierenden Checker verweist.
                                        LogicalNode ownerOfReferencedChecker = this.FindNodeByName(checkerReference, NodeTypes.Checker | NodeTypes.ValueModifier, IsGlobal); // ist true erforderlich? TEST 12.2015+-
                                        newChild = new NodeConnector(child.NodeName, mother, this, this.TreeParams, ownerOfReferencedChecker, this.Job.Checkers[child.NodeName]);
                                        newChild.NodeType = NodeTypes.ValueModifier;
                                        newChild.IsGlobal = isGlobal;
                                        if (ownerOfReferencedChecker == null)
                                        {
                                            this.UnsatisfiedNodeConnectors.Add(newChild as NodeConnector);
                                        }
                                        this.TryAddToNodesByName(newChild.Name, newChild);
                                    }
                                    else // if (checkerReference != null)
                                    {
                                        // Es handelt sich um einen normalen Checker.
                                        newChild = new SingleNode(child.NodeName, mother, this, this.TreeParams);
                                        newChild.NodeType = NodeTypes.Checker;
                                        newChild.IsGlobal = IsGlobal;
                                        this.Job.Checkers[child.NodeName].ThreadLocked |= this.ThreadLocked;
                                        (newChild as SingleNode).Checker = this.Job.Checkers[child.NodeName];
                                        if (this.TreeExternalCheckers.ContainsValue(this.Job.Checkers[child.NodeName]))
                                        {
                                            this.TreeExternalCheckers.Remove(this.TreeExternalCheckers.FirstOrDefault(c => c.Value == this.Job.Checkers[child.NodeName]).Key);
                                            // am Ende bleiben im Dictionary nur Checker übrig, die nicht in der LogicalExpression auftreten.
                                        }
                                        if ((newChild as SingleNode).Checker.CheckerTrigger != null)
                                        {
                                            newChild.Trigger = (newChild as SingleNode).Checker.CheckerTrigger;
                                            newChild.climb2Top(this.setBreakWithResult2False);
                                        }
                                        else
                                        {
                                            newChild.BreakWithResult = this.BreakWithResult;
                                        }
                                        newChild.Logger = (newChild as SingleNode).Checker.CheckerLogger;
                                        this.TryAddToNodesByName(newChild.Name, newChild);
                                    }
                                }
                                else // if (newChild == null)
                                {
                                    // Es gibt schon einen Knoten mit diesem Checker (direkte Referenz über den child.NodeName),
                                    // auf den Originalknoten verweisen.
                                    newChild = new NodeConnector(child.NodeName, mother, this, this.TreeParams, newChild, null);
                                    newChild.NodeType = NodeTypes.NodeConnector;
                                    newChild.IsGlobal = IsGlobal;
                                    if (this.TreeExternalCheckers.ContainsValue(this.Job.Checkers[child.NodeName]))
                                    {
                                        this.TreeExternalCheckers.Remove(this.TreeExternalCheckers.FirstOrDefault(c => c.Value == this.Job.Checkers[child.NodeName]).Key);
                                        // am Ende bleiben im Dictionary nur Checker übrig, die nicht in der LogicalExpression auftreten.
                                    }
                                }
                            }
                            else // if (this._job.Checkers.ContainsKey(child.NodeName))
                            {
                                // Es handelt sich um eine Konstante
                                newChild = new SingleNode(child.NodeName, mother, this, this.TreeParams);
                                newChild.NodeType = NodeTypes.Constant;
                                userControlPath = mother.ConstantNodeUserControlPath;
                            }
                            newChild.UserControlPath = userControlPath;
                            newChild.InitNodes = initNodes;
                            newChild.TriggeredRunDelay = triggeredRunDelay;
                            mother.AddChild(newChild);
                        }
                        else // if (child.NodeName.StartsWith(@"@") || this._job.Checkers.ContainsKey(child.NodeName))
                        {
                            // if (!child.NodeName.StartsWith(@"#"))
                            if (!this.Job.SnapshotNames.Contains(child.NodeName))
                            {
                                // Es handelt sich um einen SubJob.
                                newChild = this.FindJobByName(child.NodeName, true);
                                if (newChild == null)
                                {
                                    newChild = new JobList(child.NodeName, mother, this, this.TreeParams, this._jobProvider, this._parsedJobs, this.JobsByName);
                                    newChild.NodeType = NodeTypes.JobList;
                                }
                                else
                                {
                                    // Es gibt schon einen Knoten mit diesem SubJob (direkte Referenz über den child.NodeName),
                                    // auf den Originalknoten verweisen.
                                    newChild = new JobConnector(child.NodeName, mother, this, this.TreeParams, newChild, null);
                                    newChild.NodeType = NodeTypes.JobConnector;
                                    newChild.UserControlPath = mother.JobConnectorUserControlPath;
                                }
                                mother.AddChild(newChild);
                                this.TryAddToJobsByName(newChild.Id, (newChild as JobList));
                            }
                            else
                            {
                                // Es handelt sich um einen Snapshot eines externen LogicalTree.
                                newChild = new Snapshot(child.NodeName, this, this, this.TreeParams, this._jobProvider, this._parsedJobs);
                                newChild.NodeType = NodeTypes.Snapshot;
                                mother.AddChild(newChild);
                            }
                        }
                        this.AddToNodesById(newChild.Id, (newChild));
                        break;
                    case SyntaxElement.OPERATOR:
                        // True wird dann zurückgegeben, wenn die Anzahl der Kinder mit Logical=True
                        // größer als die Schranke nPlus ist und kleiner als die Schranke n- ist und
                        // auch durch die noch nicht fertigen Kinder nicht mehr >= n- werden kann.
                        // False wird dann zurückgegeben, wenn entweder die Anzahl der Kinder mit
                        // Logical=True größer als die Schranke nMinus ist oder
                        // auch durch die noch nicht fertigen Kinder die Schranke nPlus nicht
                        // mehr überschritten werden kann.
                        mother.Name = child.NodeName;
                        switch (child.NodeName)
                        {
                            case "NOT":
                                mother.nPlus = -1;
                                mother.nMinus = 1;
                                break;
                            case "IS":
                                mother.nPlus = 0;
                                mother.nMinus = mother.nOperands + 1;
                                break;
                            case "OR":
                                mother.nPlus = 0;
                                mother.nMinus = mother.nOperands + 1;
                                break;
                            //case "XOR": // nur für zweiwertige Logik ok.
                            //  mother.nPlus = 0;
                            //  mother.nMinus = 2;
                            //  break;
                            case "XOR":
                                mother.nPlus = mother.nOperands - 1;
                                mother.nMinus = mother.nOperands + 1;
                                break;
                            case "AND":
                                mother.nPlus = mother.nOperands - 1;
                                mother.nMinus = mother.nOperands + 1;
                                break;
                            case "LT":
                            case "LE":
                            case "GE":
                            case "GT":
                            case "NE":
                            case "EQ":
                                mother.nPlus = mother.nOperands - 1;
                                mother.nMinus = mother.nOperands + 1;
                                mother.IsResultDependant = true;
                                break;
                            default:
                                break;
                        }
                        break;
                    case SyntaxElement.STRUCT:
                        NodeList newListChild = new NodeList(mother.GenerateNextChildId(), mother, this, this.TreeParams);
                        newListChild.NodeType = NodeTypes.NodeList;
                        this.AddToNodesById(newListChild.Id, (newListChild));
                        newListChild.BreakWithResult = this.BreakWithResult;
                        newListChild.IsVolatile = this.IsVolatile;
                        mother.AddChild(newListChild);
                        this.buildParallelTree(newListChild, child); // Rekursion
                        break;
                    default:
                        break;
                }
            }
            this.SetWorkersState(null);
        }

        /// <summary>
        /// Fügt die LogicalNode mit nodeId in die Liste _nodesById und die aller
        /// übergeordneten _rootJobLists ein.
        /// </summary>
        /// <param name="nodeId">Die Id der einzufügenden Node.</param>
        /// <param name="node">Einzufügende LogicalNode.</param>
        private void AddToNodesById(string nodeId, LogicalNode node)
        {
            if (!this.NodesById.ContainsKey(nodeId))
            {
                this.NodesById.Add(nodeId, node);
            }
            if (!(this == this.RootJobList))
            {
                this.RootJobList.AddToNodesById(nodeId, node);
            }
        }

        /// <summary>
        /// Fügt die LogicalNode mit nodeName in die Liste _nodesByName ein
        /// und fügt die LogicalNode mit nodeName auch in alle übergeordneten
        /// _rootJobLists ein, die den nodeName noch nicht in _nodesByName haben.
        /// </summary>
        /// <param name="nodeName">Der Name der einzufügenden Node.</param>
        /// <param name="node">Einzufügende LogicalNode.</param>
        private void TryAddToNodesByName(string nodeName, LogicalNode node)
        {
            if (!this.NodesByName.ContainsKey(nodeName))
            {
                this.NodesByName.Add(nodeName, node);
            }
            if (!this.TreeRootJobList.TreeRootLastChanceNodesByName.ContainsKey(nodeName))
            {
                this.TreeRootJobList.TreeRootLastChanceNodesByName.Add(nodeName, node);
            }
        }

        /// <summary>
        /// Sucht in der aktuellen JobList und, bei 'recurse' true,
        /// in allen übergeodneten JobLists nach der Node mit dem
        /// übergebenen 'nodeName'. Der erste Treffer gewinnt.
        /// </summary>
        /// <param name="nodeName">Name des gesuchten Knotens.</param>
        /// <param name="nodeTypes">Zulässige Knotentypen für einen Treffer.</param>
        /// <param name="tryLastChance">Bei true wird auch in TreeRootJobList._treeRootLastChanceNodesByName gesucht.</param>
        /// <returns>Die gefundene LogicalNode oder null.</returns>
        private LogicalNode FindNodeByName(string nodeName, NodeTypes nodeTypes, bool tryLastChance)
        {
            if (this.NodesByName.ContainsKey(nodeName) && (nodeTypes & this.NodesByName[nodeName].NodeType) > 0 && !this.NodesByName[nodeName].IsSnapshotDummy)
            {
                return this.NodesByName[nodeName];
            }
            else
            {
                if (tryLastChance && this.TreeRootJobList.TreeRootLastChanceNodesByName.Keys.Contains(nodeName)
                  && (nodeTypes & this.TreeRootJobList.TreeRootLastChanceNodesByName[nodeName].NodeType) > 0
                  && !this.TreeRootJobList.TreeRootLastChanceNodesByName[nodeName].IsSnapshotDummy)
                {
                    return this.TreeRootJobList.TreeRootLastChanceNodesByName[nodeName];
                }
                else
                {
                    return this.Mother == null && (nodeTypes & NodeTypes.JobList) > 0 ? this : null;
                }
            }
        }

        /// <summary>
        /// Fügt die JobList mit jobName in die Liste _jobsByName ein
        /// und fügt die LogicalNode mit nodeName auch in alle übergeordneten
        /// _rootJobLists ein, die den nodeName noch nicht in _jobsByName haben.
        /// </summary>
        /// <param name="jobName">Der Name der einzufügenden JobList.</param>
        /// <param name="jobList">Einzufügende JobList.</param>
        private void TryAddToJobsByName(string jobName, JobList jobList)
        {
            if (!this.JobsByName.ContainsKey(jobName))
            {
                this.JobsByName.Add(jobName, jobList);
            }
            if (!(this == this.RootJobList))
            {
                this.RootJobList.TryAddToJobsByName(jobName, jobList);
            }
        }

        /// <summary>
        /// Sucht in der aktuellen JobList und, bei 'recurse' true,
        /// in allen übergeodneten JobLists nach der JobList mit dem
        /// übergebenen 'jobName'. Der erste Treffer gewinnt.
        /// </summary>
        /// <param name="jobName">Name der gesuchten JobList.</param>
        /// <param name="recurse">Bei true wird bis zur Tree-Root gesucht.</param>
        /// <returns>Die gefundene JobList oder null.</returns>
        private JobList FindJobByName(string jobName, bool recurse)
        {
            if (this.JobsByName.ContainsKey(jobName))
            {
                return this.JobsByName[jobName];
            }
            else
            {
                if (recurse && this != this.RootJobList)
                {
                    return this.RootJobList.FindJobByName(jobName, recurse);
                }
                else
                {
                    return null;
                }
            }
        }

        private void setBreakWithResult2False(LogicalNode node)
        {
            node.BreakWithResult = false;
        }

        private void element2StringList(int depth, LogicalNode node)
        {
            string depthIndent = String.Join(this._indent, new string[++depth]);
            if (node is NodeList)
            {
                NodeList typedNode = (NodeList)node;
                string userControlPath = typedNode.UserControlPath;
                if (node is JobList)
                {
                    userControlPath = ((JobList)node).UserControlPath;
                }
                if (node is Snapshot)
                {
                    userControlPath = ((Snapshot)node).UserControlPath;
                }
                string refString = typedNode.ReferencedNodeName == null ? "" : ", Ref:" + typedNode.ReferencedNodeName;
                string logicalString = typedNode.LastNotNullLogical == null ? "null" : typedNode.LastNotNullLogical.ToString();
                this._treeStringList.Add(String.Concat(depthIndent, node.NodeType.ToString() + ": ", typedNode.Id, " (", typedNode.Name,
                  refString,
                  ", n-: ", typedNode.nMinus, ", n+: ", typedNode.nPlus, "): ", userControlPath, " ", logicalString));
            }
            else
            {
                LogicalNode typedNode = (LogicalNode)node;
                string userControlPath = typedNode.UserControlPath;
                string logicalString = typedNode.LastNotNullLogical == null ? "null" : typedNode.LastNotNullLogical.ToString();
                string refString = typedNode.ReferencedNodeName == null ? "" : ", Ref:" + typedNode.ReferencedNodeName;
                this._treeStringList.Add(String.Concat(depthIndent, node.NodeType.ToString() + ": ", typedNode.Id, " (", typedNode.Name,
                  refString,
                  "): ", userControlPath, " ", logicalString));
            }
        }

        #endregion private members

    }
}
