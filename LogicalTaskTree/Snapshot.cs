using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NetEti.ApplicationControl;
using System.IO;
using System.Threading;
using Vishnu.Interchange;
using NetEti.ObjectSerializer;
using LogicalTaskTree.Provider;

namespace LogicalTaskTree
{
    /// <summary>
    /// Wird aufgerufen, wenn der Snapshot refreshed wurde.
    /// </summary>
    /// <param name="sender">Die Ereignis-Quelle.</param>
    public delegate void SnapshotRefreshedEventHandler(object sender);

    /// <summary>
    /// Knoten in einem LogicalTaskTree, der zur Anzeige
    /// eines Remote-LogicalTaskTree dient.
    /// </summary>
    /// <remarks>
    /// File: Snapshot.cs
    /// Autor: Erik Nagel
    ///
    /// 09.11.2013 Erik Nagel: erstellt
    /// </remarks>
    public class Snapshot : NodeList
    {
        #region public members

        /// <summary>
        /// Wird aufgerufen, wenn sich das logische Ergebnis eines Knotens geändert hat.
        /// </summary>
        public event SnapshotRefreshedEventHandler? SnapshotRefreshed;

        /// <summary>
        /// Anzahl der SingleNodes (letztendlich Checker) am Ende eines (Teil-)Baums;
        /// Achtung: dieser Wert ist, ebenso wie SingleNodesFinished, verhundertfacht
        /// (<see cref="SingleNode.SingleNodes"/>).
        /// </summary>
        public override int SingleNodes
        {
            get
            {
                return 100; // TODO: Verhalten bei Exceptions checken.
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
                return 100; // TODO: Verhalten bei Exceptions checken.
            }
        }

        /// <summary>
        /// Der Zeitpunkt, zu dem dieser Snapshot erstellt wurde.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Die XML-Datei, aus der dieser Snapshot erstellt wurde.
        /// </summary>
        public string SnapshotPath { get; set; }

        private XDocument? _lastXmlDoc;
        private string? _lastXmlDocFilePath;

        /// <summary>
        /// True, wenn dieser Snapshot nicht geladen werden konnte und stattdessen
        /// der Default-Snapshot geladen wurde.
        /// </summary>
        public bool IsDefaultSnapshot
        {
            get
            {
                if (this._job != null)
                {
                    return this._job.IsDefaultSnapshot;
                }
                else
                {
                    return this._isDefaultSnapshot;
                }
            }
            set
            {
                if (this._job != null)
                {
                    this._job.IsDefaultSnapshot = value;
                }
                else
                {
                    this._isDefaultSnapshot = value;
                }
            }
        }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public override string UserControlPath
        {
            get
            {
                if (!String.IsNullOrEmpty(this._userControlPath))
                {
                    return this._userControlPath;
                }
                else
                {
                    return this.RootJobList.SnapshotUserControlPath;
                }
            }
            set
            {
                this._userControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden JobListUserControls.
        /// </summary>
        public override string JobListUserControlPath
        {
            get
            {
                if (!String.IsNullOrEmpty(this._jobListUserControlPath))
                {
                    return this._jobListUserControlPath;
                }
                else
                {
                    return this.RootJobList.JobListUserControlPath;
                }
            }
            set
            {
                this._jobListUserControlPath = value;
            }
        }

        /// <summary>
        /// Der Pfad zu aktuell dynamisch zu ladenden NodeListUserControls.
        /// </summary>
        public string NodeListUserControlPath
        {
            get
            {
                if (!String.IsNullOrEmpty(this._nodeListUserControlPath))
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
                if (!String.IsNullOrEmpty(this._singleNodeUserControlPath))
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
        /// True, wenn dieser Snapshot geladen wird und vorher
        /// der Default-Snapshot geladen wurde.
        /// </summary>
        public bool WasDefaultSnapshot
        {
            get { return this._job?.WasDefaultSnapshot == true; }
        }

        /// <summary>
        /// Konstruktor - übernimmt den Eltern-Knoten.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten.</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public Snapshot(LogicalNode mother, JobList rootJobList, TreeParameters treeParams)
          : base(mother, rootJobList, treeParams)
        {
            this._isDefaultSnapshot = false;
            if (this._job != null)
            {
                this._userControlPath = this._job.SnapshotUserControlPath;
                this._jobListUserControlPath = this._job.JobListUserControlPath;
                this._nodeListUserControlPath = this._job.NodeListUserControlPath;
                this._singleNodeUserControlPath = this._job.SingleNodeUserControlPath;
                this.ConstantNodeUserControlPath = this._job.ConstantNodeUserControlPath;
            }
            else
            {
                this._userControlPath = rootJobList.SnapshotUserControlPath;
                this._jobListUserControlPath = rootJobList.UserControlPath;
                this._nodeListUserControlPath = rootJobList.NodeListUserControlPath;
                this._singleNodeUserControlPath = rootJobList.SingleNodeUserControlPath;
                this.ConstantNodeUserControlPath = rootJobList.ConstantNodeUserControlPath;
            }
            this.SnapshotPath = String.Empty;
            this._jobProvider = new EmptyJobProvider();
            this._results = new();
            this._treeStringList = new();
            this._parsedJobs = new();
            this._parsedNodeExceptions = new();
        }

        /// <summary>
        /// Gibt den (Teil-)Baum in eine StringList aus.
        /// </summary>
        /// <param name="indent">Einrück-Weite pro Hierarchie-Tiefe.</param>
        /// <returns>Baumdarstellung in einer StringList</returns>
        public List<string> Show(string indent)
        {
            this._indent = indent;
            this._treeStringList.Clear();
            this.Traverse(this.element2StringList);
            return this._treeStringList;
        }

        /// <summary>
        /// Aktualisiert den Snapshot aus einer externen XML-Datei.
        /// </summary>
        /// <param name="mother">Der besitzende Knoten.</param>
        /// <param name="isConstructor">Default-Parameter: bei true stammt der Aufruf
        /// vom Snapshot-Constructor; Default: false.</param>
        public void RefreshSnapshot(LogicalNode mother, bool isConstructor = false)
        {
            this.parse(mother, isConstructor);
            /*
            if (isConstructor)
            {
                this.LastLogical = null;
                this.LastNotNullLogical = null;
            }
            */
            //this.OnLastNotNullLogicalChanged(this, this.LastNotNullLogical, Guid.Empty);
            //this.DoNodeLogicalChanged();
            //this.OnNodeLogicalChanged();
            this.OnSnapshotRefreshed();
            SnapshotManager.RequestSnapshot(this.RootJobList.GetTopRootJobList());
        }

        /// <summary>
        /// Aktualisiert den Snapshot aus einer externen XML-Datei.
        /// </summary>
        public void RefreshSnapshot()
        {
            // this.RefreshSnapshot(null, false);
            this.RefreshSnapshot(this, false);
        }

        /*
        /// <summary>
        /// Speichert den zuletzt eingelesenen Snapshot zu Debugging-Zwecken.
        /// </summary>
        public void SaveLastSnapshotForDebugging()
        {
            Guid guid = new Guid();
            try
            {
                File.Copy(this._lastXmlDocFilePath,
                    System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this._lastXmlDocFilePath),
                        String.Format($"LastRead_org_{guid}.xml")));
            }
            catch { }
            string lastXmlDocPath = System.IO.Path.Combine(this.AppSettings.ResolvedSnapshotDirectory,
                String.Format($"LastRead_{guid}.xml"));
            this._lastXmlDoc?.Save(lastXmlDocPath);
        }
        */

        #endregion public members

        #region internal members

        /// <summary>
        /// Konstruktor für rekursive Aufrufe.
        /// </summary>
        /// <param name="logicalName">Eindeutige Kennung des Knotens</param>
        /// <param name="mother">Der besitzende Knoten.</param>
        /// <param name="rootJoblist">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="jobProvider">Die Datenquelle für den Job</param>
        /// <param name="parsedJobs">Liste von Namen aller bisher geparsten Jobs.</param>
        internal Snapshot(string logicalName, LogicalNode mother, JobList rootJoblist,
            TreeParameters treeParams, IJobProvider jobProvider, List<string> parsedJobs)
          : base(logicalName, mother, rootJoblist, treeParams)
        {
            this._userControlPath = rootJoblist.SnapshotUserControlPath;
            this.Level = mother.Level + 1;
            this.RootJobList = rootJoblist;
            this._parsedJobs = parsedJobs;
            this._parsedNodeExceptions = new List<KeyValuePair<LogicalNode, Exception>>();
            if (this._parsedJobs.Contains(logicalName))
            {
                throw new ApplicationException(String.Format("Rekursion bei ID: {0}.", logicalName));
            }
            this._jobProvider = jobProvider;
            string tmpJobName = logicalName;
            this._job = this._jobProvider.GetJob(ref tmpJobName);
            if (!String.IsNullOrEmpty(tmpJobName) && tmpJobName != logicalName)
            {
                logicalName = tmpJobName;
            }
            this.SnapshotPath = this._jobProvider.GetPhysicalJobPath(logicalName)
                ?? throw new ArgumentException($"SnapshotPath von {logicalName} ist null.");
            this.Id = logicalName;
            this.Name = this.Id;
            if (this.Id.Length > 1 && this.Id.StartsWith("#"))
            {
                this.Name = this.Id.Substring(1);
            }
            this._parsedJobs.Add(logicalName);
            this.StartCollapsed = this._job.StartCollapsed;
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
            this._results = new ResultList();
            this._treeStringList = new List<string>();
            this.Trigger = this._job.JobTrigger;
            this.Logger = this._job.JobLogger;
            this.Logical = null;
            this.nPlus = 0;
            this.nMinus = 2;
            this.RefreshSnapshot(mother, true);
        }

        /// <summary>
        /// Liefert für eine anfragende SingleNode (Worker) das Verarbeitungsergebnis
        /// (object) eines vorher beendeten Knotens als Eingangsparameter.
        /// </summary>
        /// <param name="nodeId">Die Id des Knotens, der ein Result-Objekt hier abgelegt hat.</param>
        /// <returns>Das Result-Objekt des Knotens.</returns>
        internal Result? GetResult(string nodeId)
        {
            if (nodeId != null && this._results.ContainsKey(nodeId))
            {
                return this._results[nodeId];
            }
            return null;
        }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Überschriebene RUN-Logik.
        /// Diese Routine wird asynchron ausgeführt.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        protected override void DoRun(TreeEvent? source)
        {
            this.RefreshSnapshot();
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
        /// Löst das SnapshotRefreshed-Ereignis aus.
        /// </summary>
        protected virtual void OnSnapshotRefreshed()
        {
            SnapshotRefreshed?.Invoke(this);
        }

        #endregion protected members

        #region private members

        private IJobProvider _jobProvider;

        // Der externe Job mit logischem Ausdruck und Dictionary der Worker.
        private Job? _job;

        private ResultList _results;
        private string? _indent;
        private List<string> _treeStringList;
        private List<string> _parsedJobs;
        private List<KeyValuePair<LogicalNode, Exception>> _parsedNodeExceptions;

        private bool _isDefaultSnapshot;
        private string? _userControlPath;
        private string? _jobListUserControlPath;
        private string? _nodeListUserControlPath;
        private string? _singleNodeUserControlPath;

        // 1. Stößt das Parsen des booleschen Ausdrucks an.
        // 2. Baut nach dem Vorbild des Boolean-Tree den LogicalTask(Teil-)Tree
        //
        // Diese Variante stellt eine vorläufige Notlösung dar: es wird immer
        // mindestens zweimal die XML-Datei eingelesen, solange bis beide
        // Versionen identisch sind. Das soll verhindern, dass eine unfertig
        // geschriebene XML-Datei weiter verwendet wird. Hier muss eine sichere
        // und performante Lösung gefunden werden.
        private void parse(LogicalNode mother, bool isConstructor = false)
        {
            XDocument? xmlDoc = new XDocument();
            string? xmlFilePath = String.Empty;
            try
            {
                string? physicalJobPath = this._jobProvider.GetPhysicalJobPath(this.Id);
                if (physicalJobPath != null)
                {
                    xmlFilePath = File.ReadAllText(physicalJobPath);
                }
                string? snapshotPhysicalName
                    = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(xmlFilePath));
                xmlDoc = XDocument.Load(xmlFilePath);
            }
            catch (IOException)
            {
                xmlDoc = null;
            }
            if (xmlDoc == null)
            {
                // leider verloren
                InfoController.Say(String.Format("Das Laden des Snapshots ist misslungen: {0}",
                    xmlFilePath));
                return;
            }
            // DEBUG 24.08.2020 Nagel+
            this._lastXmlDoc = xmlDoc;
            this._lastXmlDocFilePath = xmlFilePath;
            // DEBUG 24.02.2020 Nagel-
            this.IsDefaultSnapshot = false;
            LogicalNode para = this;
            XElement snapshotXml = xmlDoc.Element("Snapshot")
                ?? throw new ArgumentNullException("XElement Snapshot ist null.");
            this.buildTreeFromRemoteJob(mother, this.Level, ref para, snapshotXml, isConstructor);
            this._parsedNodeExceptions.Clear();
        }

        /// <summary>
        /// Baut den LogicalTask(Teil-)Tree.
        /// </summary>
        /// <param name="level">Die Hierarchie-Stufe des Teil-Baums</param>
        /// <param name="node2parse">Der zu parsende Knoten des Teil-Baums</param>
        /// <param name="xElement">Der aktuelle Knoten des XML-Dokuments.</param>
        /// <param name="isConstructor">Default-Parameter: bei true stammt der Aufruf
        /// vom Snapshot-Constructor; Default: false.</param>
        /// <param name="mother">Der besitzende Knoten.</param>
        private void buildTreeFromRemoteJob(LogicalNode mother, int level, ref LogicalNode node2parse,
            XElement xElement, bool isConstructor = false)
        {
            string type;
            string subType = "";
            XAttribute? xVar = xElement.Attributes("Type").FirstOrDefault()
                ?? throw new ArgumentException("Snapshot: Attribut 'Type' nicht gefunden.");
            type = xVar.Value;
            if (type == "NodeConnector")
            {
                type = "SingleNode";
            }
            if (node2parse.GetType().Name != type)
            {
                if (node2parse is IDisposable)
                {
                    ((IDisposable)node2parse).Dispose();
                }
                node2parse = UndefinedLogicalNodeClass.UndefinedLogicalNode;
            }
            if (node2parse == UndefinedLogicalNodeClass.UndefinedLogicalNode)
            {
                switch (type)
                {
                    case "Snapshot":
                        node2parse = new Snapshot(mother, this.RootJobList, this.TreeParams);
                        node2parse.NodeType = NodeTypes.Snapshot;
                        node2parse.UserControlPath = this.UserControlPath;
                        break;
                    case "JobList":
                        node2parse = new JobList(mother, this.RootJobList, this.TreeParams);
                        node2parse.NodeType = NodeTypes.JobList;
                        node2parse.UserControlPath = this.JobListUserControlPath;
                        break;
                    case "NodeList":
                        node2parse = new NodeList(mother, this.RootJobList, this.TreeParams);
                        node2parse.NodeType = NodeTypes.NodeList;
                        node2parse.UserControlPath = this.NodeListUserControlPath;
                        break;
                    case "SingleNode":
                        //case "JobConnector":
                        //case "NodeConnector":
                        node2parse = new SingleNode(mother, this.RootJobList, this.TreeParams);
                        node2parse.NodeType = NodeTypes.Checker;
                        subType = xElement.Attributes("NodeType").FirstOrDefault()?.Value
                            ?? throw new ArgumentException("SingleNode: Attribut 'SubType' nicht gefunden.");
                        if (subType != "Constant")
                        {
                            node2parse.UserControlPath = this.SingleNodeUserControlPath;
                        }
                        else
                        {
                            node2parse.UserControlPath = this.ConstantNodeUserControlPath;
                        }
                        break;
                    case "JobConnector":
                        node2parse = new JobConnector(mother, this.RootJobList, this.TreeParams);
                        node2parse.NodeType = NodeTypes.JobConnector;
                        subType = xElement.Attributes("NodeType").FirstOrDefault()?.Value
                            ?? throw new ArgumentException("JobConnector: Attribut 'SubType' nicht gefunden.");
                        node2parse.UserControlPath = this.JobConnectorUserControlPath;
                        break;
                    default:
                        throw new ArgumentNullException($"Type '{type}' nicht gefunden.");
                }
            }
            node2parse.Level = level;
            node2parse.LastState = NodeState.Finished;
            node2parse.LastLogicalState = NodeLogicalState.Done;
            bool? lastNotNullLogical = null;
            if (type == "Snapshot")
            {
                // Snapshot-spezifische Werte übernehmen
                ((Snapshot)node2parse).Timestamp = Convert.ToDateTime(xElement.Element("Timestamp")?.Value);
                ((Snapshot)node2parse).SnapshotPath = xElement.Element("Path")?.Value
                    ?? throw new ArgumentNullException("Snapshot mit Path=null.");
                XElement? tmpVar = xElement.Element("SleepTimeTo");
                if (tmpVar != null)
                {
                    node2parse.SleepTimeTo = TimeSpan.Parse(tmpVar.Value);
                    node2parse.IsInSleepTime = true;
                }
                else
                {
                    node2parse.SleepTimeTo = new TimeSpan(0, 0, 0);
                    node2parse.IsInSleepTime = false;
                }
            }
            if (type == "Snapshot" && ((Snapshot)node2parse).IsInSnapshot == false) 
                // 10.05.2018 Nagel: IsInSnapshot muss sein! 
            {
                // Im Snapshot enthaltene JobList parsen
                xElement = xElement.Element("JobDescription")
                    ?? throw new ApplicationException("JobDescription ist null.");
                LogicalNode? subNode2parse = UndefinedLogicalNodeClass.UndefinedLogicalNode;
                bool buildNew = true;
                if (node2parse.Children != null && node2parse.Children.Count > 0)
                {
                    subNode2parse = node2parse.Children[0];
                    buildNew = false;
                }
                // Rekursion - Subtree bauen
                // if (subNode2parse != null)
                // {
                    this.buildTreeFromRemoteJob(this, level + 1, ref subNode2parse, xElement, isConstructor);
                    if (buildNew)
                    {
                        // JobList als Kind-Knoten einbauen
                        node2parse.Children?.Clear();
                        //(node2parse as NodeList).HookChildEvents(subNode2parse);
                        node2parse.Children?.Add(subNode2parse);
                    }
                    // Relevante Werte des Kind-Knotens auf den Snapshot übernehmen.
                    lastNotNullLogical = subNode2parse.LastNotNullLogical;
                //}
            }
            else
            {
                // Ab hier innerhalb der JobList des Snapshots
                node2parse.Id = xElement.Attribute("Id")?.Value ?? "";
                node2parse.Name = xElement.Attribute("Name")?.Value ?? "";
                try
                {
                    node2parse.LastRun = Convert.ToDateTime(xElement.Attribute("LastRun")?.Value);
                    node2parse.NextRunInfo = xElement.Attribute("NextRunInfo")?.Value;
                }
#if DEBUG
                catch (Exception ex)
                {
                    InfoController.Say("Snapshot.buildTreeFromRemoteJob: " + ex.Message);
                    throw;
                }
#else
                catch (Exception)
                {
                    throw;
                }
#endif
                xVar = xElement.Attribute("LastNotNullLogical");
                if (!String.IsNullOrEmpty(xVar?.Value))
                {
                    lastNotNullLogical = Convert.ToBoolean(xVar.Value);
                }
                else
                {
                    lastNotNullLogical = null;
                }
                if (subType == "NodeConnector" | subType == "JobConnector")
                {
                    node2parse.ReferencedNodeId = xElement.Element("ReferencedNodeId")?.Value;
                    node2parse.ReferencedNodeName = xElement.Element("ReferencedNodeName")?.Value;
                    node2parse.ReferencedNodePath = xElement.Element("ReferencedNodePath")?.Value;
                }
                if (subType == "JobConnector")
                {
                    ((JobConnector)node2parse).LogicalExpression
                        = xElement.Element("LogicalExpression")?.Value ?? "";
                }
                if (type == "NodeList" || type == "JobList" || type == "Snapshot")
                {
                    if (type == "JobList")
                    {
                        ((JobList)node2parse).LogicalExpression = xElement.Element("LogicalExpression")?.Value
                            ?? throw new ArgumentNullException("JobList ohne LogicalExpression.");
                    }
                    XElement? tmpVar = xElement.Element("ThreadLocked");
                    node2parse.ThreadLocked = tmpVar != null && Convert.ToBoolean(tmpVar.Value);
                    tmpVar = xElement.Element("BreakWithResult");
                    node2parse.BreakWithResult = tmpVar != null && Convert.ToBoolean(tmpVar.Value);
                    tmpVar = xElement.Element("StartCollapsed");
                    node2parse.StartCollapsed = tmpVar != null && Convert.ToBoolean(tmpVar.Value);
                    if (ConfigurationManager.IsExpanded(node2parse.IdPath) != null) // 15.02.2019 Nagel+
                    {
                        if (ConfigurationManager.IsExpanded(node2parse.IdPath) == true)
                        {
                            node2parse.StartCollapsed = false;
                        }
                        else
                        {
                            node2parse.StartCollapsed = true;
                        }
                    } // 15.02.2019 Nagel-
                    tmpVar = xElement.Element("IsVolatile");
                    ((NodeList)node2parse).IsVolatile = tmpVar != null && Convert.ToBoolean(tmpVar.Value);
                    IEnumerable<XElement> logicalNodes
                        = from item in xElement.Elements("LogicalNode") select item;
                    int i = 0;
                    // 06.07.2016 Nagel+ foreach (XElement xLogicalNode in logicalNodes)
                    foreach (XElement xLogicalNode in logicalNodes.ToList()) // 06.07.2016 Nagel-
                    {
                        LogicalNode subNode2parse = UndefinedLogicalNodeClass.UndefinedLogicalNode;
                        string? lastTypeName = null;
                        if (node2parse.Children != null && node2parse.Children.Count > i)
                        {
                            lastTypeName = node2parse.Children[i].GetType().Name;
                            subNode2parse = node2parse.Children[i];
                        }
                        // Rekursion - Subtree bauen
                        this.buildTreeFromRemoteJob(node2parse, level + 1, ref subNode2parse,
                            xLogicalNode, isConstructor);
                        string subNodeTypeName = subNode2parse.GetType().Name;
                        if (subNodeTypeName == "Snapshot")
                        {
                            ((Snapshot)subNode2parse).IsInSnapshot = true;
                        }
                        if (lastTypeName != null)
                        {
                            if (subNodeTypeName != lastTypeName)
                            {
                                // JobList als Kind-Knoten austauschen
                                // 09.08.2018+- (node2parse as NodeList).UnhookChildEvents(node2parse.Children[i]);
                                node2parse.Children?.RemoveAt(i);
                                // 09.08.2018+- (node2parse as NodeList).HookChildEvents(subNode2parse);
                                node2parse.Children?.Insert(i, subNode2parse);
                            } // anderenfalls nichts machen, der Knoten ist im Typ unverändert
                        }
                        else
                        {
                            // JobList als Kind-Knoten einbauen
                            //(node2parse as NodeList).HookChildEvents(subNode2parse);
                            node2parse.Children?.Add(subNode2parse);
                        }
                        i++;
                    }
                    if (type == "Snapshot")
                    {
                        tmpVar = xElement.Element("IsDefaultSnapshot");
                        ((Snapshot)node2parse).IsDefaultSnapshot = tmpVar != null && Convert.ToBoolean(tmpVar.Value);
                    }
                } // if (type == "NodeList" || type == "JobList")
            }
            if (!isConstructor)
            {
                node2parse.LogicalState = NodeLogicalState.Done;
                node2parse.Logical = lastNotNullLogical; // Die Reihenfolge
                node2parse.LastLogical = lastNotNullLogical; // ist
                node2parse.LastNotNullLogical = lastNotNullLogical; // essenziell!

                XElement? lastExceptionsGroup = xElement.Element("LastExceptions");
                if (lastExceptionsGroup != null)
                {
                    // 06.07.2016 Nagel+ foreach (XElement xException in lastExceptionsGroup.Descendants())
                    foreach (XElement xException in lastExceptionsGroup.Descendants().ToList()) // 06.07.2016 Nagel-
                    {
                        // TODO: hier ggf. noch den Type der Exception übernehmen
                        Exception exception = new ApplicationException(xException.Attribute("Message")?.Value);
                        // 17.08.2018+
                        if (node2parse != this && (node2parse is NodeParent))
                        {
                            if (!node2parse.LastExceptions.ContainsKey(node2parse))
                            {
                                node2parse.LastExceptions.TryAdd(node2parse, exception);
                            }
                            //if (node2parse is JobConnector)
                            //{
                            //    node2parse.LastResult = new Result(node2parse.Id, null, NodeState.Finished,
                            //    NodeLogicalState.Fault, exception);
                            //}
                            node2parse.OnNodeResultChanged();
                        }
                        // 17.08.2018-
                        node2parse.LogicalState = NodeLogicalState.Fault;
                        node2parse.LastLogicalState = NodeLogicalState.Fault;
                        this._parsedNodeExceptions.Add(new KeyValuePair<LogicalNode, Exception>(this, exception));
                        break; // nur die erste Exception wird übernommen
                    }
                }
                else
                {
                    if (node2parse != this && (node2parse is NodeParent))
                    {
                        node2parse.LastExceptions.Clear();
                        //Result dummy = node2parse.LastResult;
                        node2parse.OnNodeResultChanged();
                    }
                }
                if (node2parse is SingleNode)
                {
                    node2parse.State = NodeState.Finished;
                    XElement? xresult = xElement.Descendants("Result").FirstOrDefault();
                    if (xresult != null && !String.IsNullOrEmpty(xresult.Value.ToString()))
                    {
                        string encoded = xresult.Value.ToString();
                        Result? deserializedResult
                            = (Result?)SerializationUtility.DeserializeObjectFromBase64String(encoded);
                        (node2parse as SingleNode)?.SetReturnObject(deserializedResult?.ReturnObject);
                    }
                    else
                    {
                        (node2parse as SingleNode)?.SetReturnObject(null);
                    }
                  (node2parse as SingleNode)?.SetLastResult();
                    node2parse.Logical = lastNotNullLogical;
                }
                //if (node2parse is JobConnector)
                //{
                //    node2parse.State = NodeState.Finished;
                //    XElement xresult = xElement.Descendants("Result").FirstOrDefault();
                //    if (xresult != null && !String.IsNullOrEmpty(xresult.Value.ToString()))
                //    {
                //        string encoded = xresult.Value.ToString();
                //        Result deserializedResult
                //        = (Result)XMLSerializationUtility.DeserializeObjectFromBase64(encoded);
                //        (node2parse as JobConnector).LastResult = deserializedResult;
                //        //(node2parse as JobConnector).SetReturnObject(deserializedResult.ReturnObject);
                //    }
                //    else
                //    {
                //        (node2parse as SingleNode).SetReturnObject(null);
                //    }
                //    node2parse.Logical = lastNotNullLogical;
                //}
                if (node2parse == this)
                {
                    if (this._parsedNodeExceptions.Count > 0)
                    {
                        this.OnExceptionRaised(this, this._parsedNodeExceptions[0].Value);
                    }
                    else
                    {
                        this.OnExceptionCleared(this);
                    }
                }
                node2parse.OnNodeStateChanged();
                node2parse.OnNodeLogicalChanged();
                node2parse.OnNodeLastNotNullLogicalChanged(node2parse, lastNotNullLogical, Guid.Empty);
                node2parse.OnNodeResultChanged();
            }
            else
            {
                XElement? userControlPath = xElement.Element("UserControlPath");
                if (userControlPath != null)
                {
                    node2parse.UserControlPath = userControlPath.Value.ToString();
                }
                /* 09.08.2018+
                if (this != node2parse)
                {
                    (mother as NodeList).HookChildEvents(node2parse);
                }
                09.08.2018- */
                node2parse.Logical = null;
                node2parse.LastLogical = null;
                node2parse.LastNotNullLogical = null;
            }
        }

        private void element2StringList(int depth, LogicalNode node)
        {
            string depthIndent = String.Join(this._indent, new string[++depth]);
            Snapshot snapshot = (Snapshot)node;
            this._treeStringList.Add(String.Concat(depthIndent, "Snapshot: ", snapshot.Id, " (", snapshot.Name,
              ", n-: ", snapshot.nMinus, ", n+: ", snapshot.nPlus, "): ",
              snapshot.LastNotNullLogical.ToString()));
        }

        #endregion private members

    }
}
