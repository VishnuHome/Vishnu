using System.Collections.Generic;
using System;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Konkreter Job für eine Joblist in einem LogicalTaskTree.
    /// </summary>
    /// <remarks>
    /// File: jobPackage.Job.cs
    /// Autor: Erik Nagel
    ///
    /// 26.01.2012 Erik Nagel: erstellt
    /// </remarks>
    public class Job
    {
        #region public members

        /// <summary>
        /// Der logische Ausdruck, der durch eine JobList im LogicalTaskTree
        /// abgearbeitet wird. 
        /// </summary>
        public string LogicalExpression { get; set; }

        /// <summary>
        /// Liste von externen Prüfroutinen für einen jobPackage.Job.
        /// </summary>
        public Dictionary<string, NodeCheckerBase> Checkers { get; set; }

        /// <summary>
        /// Liste von externen Triggern für einen jobPackage.Job.
        /// </summary>
        public Dictionary<string, TriggerShell> Triggers { get; set; }

        /// <summary>
        /// Liste von externen Loggern für einen jobPackage.Job.
        /// </summary>
        public Dictionary<string, LoggerShell> Loggers { get; set; }

        /// <summary>
        /// Liste von Snapshot-Namen für einen jobPackage.Job.
        /// </summary>
        public List<string> SnapshotNames { get; set; }

        /// <summary>
        /// Ein optionaler Trigger, der steuert, wann ein Snapshot des Jobs erstellt
        /// werden soll oder null.
        /// </summary>
        public TriggerShell JobSnapshotTrigger { get; set; }

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft
        /// oder null (setzt intern BreakWithResult auf false).
        /// </summary>
        public TriggerShell JobTrigger { get; set; }

        /// <summary>
        /// Ein optionaler Logger, der dem Job zugeordnet
        /// ist oder null.
        /// </summary>
        public LoggerShell JobLogger { get; set; }


        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine JobList.
        /// </summary>
        public string JobListUserControlPath
        {
            get
            {
                return this._jobListUserControlPath;
            }
            set
            {
                if (value != null)
                {
                    this._jobListUserControlPath = value;
                }
            }
        }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für einen JobConnector.
        /// </summary>
        public string JobConnectorUserControlPath
        {
            get
            {
                return this._jobConnectorUserControlPath;
            }
            set
            {
                if (value != null)
                {
                    this._jobConnectorUserControlPath = value;
                }
            }
        }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine NodeList.
        /// </summary>
        public string NodeListUserControlPath
        {
            get
            {
                return this._nodeListUserControlPath;
            }
            set
            {
                if (value != null)
                {
                    this._nodeListUserControlPath = value;
                }
            }
        }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine SingleNode.
        /// </summary>
        public string SingleNodeUserControlPath
        {
            get
            {
                return this._singleNodeUserControlPath;
            }
            set
            {
                if (value != null)
                {
                    this._singleNodeUserControlPath = value;
                }
            }
        }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine Constant-SingleNode.
        /// </summary>
        public string ConstantNodeUserControlPath
        {
            get
            {
                return this._constantNodeUserControlPath;
            }
            set
            {
                if (value != null)
                {
                    this._constantNodeUserControlPath = value;
                }
            }
        }

        /// <summary>
        /// Pfad zum dynamisch zu ladenden UserControl für eine JobList.
        /// </summary>
        public string SnapshotUserControlPath
        {
            get
            {
                return this._snapshotControlPath;
            }
            set
            {
                if (value != null)
                {
                    this._snapshotControlPath = value;
                }
            }
        }

        /// <summary>
        /// Ein Job kann u.U. schon ein eindeutiges logisches Ergebnis haben,
        /// bevor alle Kinder ihre Verarbeitung beendet haben.
        /// Bei BreakWithResult=True werden diese dann abgebochen.
        /// Greift nicht, wenn ein JobTrigger gesetzt wurde.
        /// </summary>
        public bool BreakWithResult { get; set; }

        /// <summary>
        /// Bei True wird jeder Thread über die Klasse gesperrt, so dass
        /// nicht Thread-sichere Checker serialisiert werden;
        /// Default: False;
        /// </summary>
        public bool ThreadLocked { get; set; }

        /// <summary>
        /// Optionaler zum globalen Sperren verwendeter Name.
        /// Wird verwendet, wenn ThreadLocked gesetzt ist.
        /// </summary>
        public string LockName { get; set; }

        /// <summary>
        /// Bei True wird zur Ergebnisermittlung im Tree "Logical" benutzt,
        /// bei False "LastNotNullLogical".
        /// Default: False
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Bei True wird der Job beim Start zusammengeklappt angezeigt, wenn die UI dies unterstützt.
        /// Default: False
        /// </summary>
        public bool StartCollapsed { get; set; }

        /// <summary>
        /// Bei true wird dieser Knoten als Referenzknoten angelegt, wenn irgendwo im Tree
        /// (nicht nur im aktuellen Job) der Name des Knotens schon gefunden wurde.
        /// Bei false wird nur im aktuellen Job nach gleichnamigen Knoten gesucht.
        /// Default: false.
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Bei True werden alle Knoten im Tree resettet, wenn dieser Knoten gestartet wird.
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// </summary>
        public bool InitNodes { get; set; }

        /// <summary>
        /// Verzögert den Start eines Knotens (und InitNodes).
        /// Kann für Loops in Controlled-Jobs verwendet werden.
        /// Default: 0 (Millisekunden).
        /// </summary>
        public int TriggeredRunDelay { get; set; }

        /// <summary>
        /// Die größte Hierarchie-Tiefe von Sub-Jobs dieses Jobs.
        /// Hat dieser Job z.B. einen Sub-Job, ist der Wert 1. Hat der Sub-Job
        /// wiederum einen Sub-Job, dann 2 usw., ansonsten 0.
        /// </summary>
        public int MaxSubJobDepth { get; set; }

        /// <summary>
        /// Verzögerung in Millisekunden, bevor ein LogicalCanged-Event weitergegeben wird.
        /// </summary>
        public int LogicalChangedDelay { get; set; }

        /// <summary>
        /// True, wenn dieser Snapshot nicht geladen werden konnte und stattdessen
        /// der Default-Snapshot geladen wurde.
        /// </summary>
        public bool IsDefaultSnapshot
        {
            get { return this._isDefaultSnapshot; }
            set
            {
                if (this._isDefaultSnapshot && this._isDefaultSnapshot != value)
                {
                    this.WasDefaultSnapshot = true;
                }
                else
                {
                    this.WasDefaultSnapshot = false;
                }
                this._isDefaultSnapshot = value;
            }
        }

        /// <summary>
        /// True, wenn dieser Snapshot geladen werden und vorher
        /// der Default-Snapshot geladen wurde.
        /// </summary>
        public bool WasDefaultSnapshot { get; set; }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        public Job()
        {
            this.Workers = new Workers(this.addWorkers, this.getWorkers, this.containsCombinedKey, this.getKeys, this.getValues);
            this.LogicalExpression = "";
            this.Checkers = new Dictionary<string, NodeCheckerBase>();
            this.Triggers = new Dictionary<string, TriggerShell>();
            this.Loggers = new Dictionary<string, LoggerShell>();
            this._workers = new Dictionary<string, Dictionary<string, WorkerShell[]>>();
            this.SnapshotNames = new List<string>();
            this.EventTriggers = new Dictionary<string, Dictionary<string, TriggerShell>>();
            this.BreakWithResult = false;
            this.JobSnapshotTrigger = null;
            this.JobTrigger = null;
            this.ThreadLocked = false;
            this.IsVolatile = false;
            this.JobListUserControlPath = @"DefaultNodeControls\JobListUserControl.dll";
            this.JobConnectorUserControlPath = @"DefaultNodeControls\JobConnectorUserControl.dll";
            this.NodeListUserControlPath = @"DefaultNodeControls\NodeListUserControl.dll";
            this.SingleNodeUserControlPath = @"DefaultNodeControls\SingleNodeUserControl.dll";
            this.ConstantNodeUserControlPath = @"DefaultNodeControls\ConstantNodeUserControl.dll";
            this.SnapshotUserControlPath = @"DefaultNodeControls\SnapshotUserControl.dll";
        }

        #endregion public members

        #region internal members

        #region tree globals

        /// <summary>
        /// Liste von internen Triggern für einen jobPackage.Job.
        /// </summary>
        internal Dictionary<string, Dictionary<string, TriggerShell>> EventTriggers { get; set; }

        // Hilfsproperty für das Hinzufügen von Workern zum
        // privaten Dictionary _workers.
        // Wird von auße wie ein Dictionary wahrgenommen.
        /// <summary>
        /// Liste von externen Arbeitsroutinen für einen jobPackage.Job.
        /// Ist ein Dictionary mit WorkerShell-Arrays zu aus
        /// Knoten-Id + ":" + TreeEvents-String gebildeten Keys.
        /// </summary>
        internal Workers Workers { get; private set; }

        #endregion tree globals

        #endregion internal members

        #region private members

        // <summary>
        // Liste von externen Arbeitsroutinen für einen jobPackage.Job.
        // </summary>
        private Dictionary<string, Dictionary<string, WorkerShell[]>> _workers { get; set; }

        private bool _isDefaultSnapshot;
        private string _constantNodeUserControlPath;
        private string _snapshotControlPath;
        private string _singleNodeUserControlPath;
        private string _nodeListUserControlPath;
        private string _jobConnectorUserControlPath;
        private string _jobListUserControlPath;

        // Fügt ein Element in das private Dictionary ein.
        // Das Dictionary enthält zu jedem Event ein Dictionary von NodeIDs
        // mit zugeordneten WorkerShell-Arrays.
        private void addWorkers(string node_event, WorkerShell[] workerArray)
        {
            string id;
            string treeEvent;
            this.getSplitKey(node_event, out id, out treeEvent);
            string internalEvents = TreeEvent.GetInternalEventNamesForUserEventNames(treeEvent);
            if (!String.IsNullOrEmpty(internalEvents))
            {
                if (!this._workers.ContainsKey(internalEvents))
                {
                    this._workers.Add(internalEvents, new Dictionary<string, WorkerShell[]>());
                }
                if (!this._workers[internalEvents].ContainsKey(id))
                {
                    this._workers[internalEvents].Add(id, workerArray);
                }
            }
        }

        // Prüft, ob das private Dictionary _workers ein WorkerShell-Array
        // zu den beiden in node_event kombinierten Keys enthält.
        private bool containsCombinedKey(string node_event)
        {
            string[] id_treeEvents = findCombinedKey(node_event);
            if (id_treeEvents != null)
            {
                if (this._workers[id_treeEvents[1]].ContainsKey(id_treeEvents[0]))
                //if ((id_treeEvents[1].StartsWith("Any") && this._workers.ContainsKey(id_treeEvents[1])) || this._workers[id_treeEvents[1]].ContainsKey(id_treeEvents[0])) // 04.08.2018
                {
                    return true;
                }
            }
            return false;
        }

        // Prüft, ob das private Dictionary _workers ein WorkerShell-Array
        // zu den beiden in node_event kombinierten Keys enthält und
        // liefert im Erfolgsfall den erweiterten treeEventsString,
        // ansonsten null. Kann mit node_event null aufgerufen, liefert
        // dann alle für den aktuellen Knoten definierte Worker.
        private string[] findCombinedKey(string node_event)
        {
            string id;
            string treeEvent;
            this.getSplitKey(node_event, out id, out treeEvent);
            foreach (string treeEvents in this._workers.Keys)
            {
                //if (treeEvents.Contains(treeEvent) || treeEvent == "")
                //if (!String.IsNullOrEmpty(treeEvent) && treeEvents.Contains(treeEvent) || this._workers[treeEvents].ContainsKey(id))
                if (!String.IsNullOrEmpty(treeEvent) && treeEvents.Contains(treeEvent) && this._workers[treeEvents].ContainsKey(id)
                    || String.IsNullOrEmpty(treeEvent) && this._workers[treeEvents].ContainsKey(id))
                {
                    return new string[] { id, treeEvents };
                }
            }
            return null;
        }

        // Liefert die Keys des privaten Dictionarys _workers.
        private Dictionary<string, Dictionary<string, WorkerShell[]>>.KeyCollection getKeys()
        {
            return this._workers.Keys;
        }

        // Liefert die Values des privaten Dictionarys _workers.
        private Dictionary<string, Dictionary<string, WorkerShell[]>>.ValueCollection getValues()
        {
            return this._workers.Values;
        }

        // Liefert zu einem aus Knoten-Id + : + TreeEvent-Name zusammengesetzten Key
        // direkt das zugehörige WorkerShell-Array aus dem privaten Dictionarys _workers. 
        private WorkerShell[] getWorkers(string node_event)
        {
            string[] id_treeEvents = findCombinedKey(node_event);
            if (id_treeEvents != null)
            {
                if (this._workers[id_treeEvents[1]].ContainsKey(id_treeEvents[0]))
                {
                    return this._workers[id_treeEvents[1]][id_treeEvents[0]];
                }
            }
            return null;
        }

        private void getSplitKey(string node_event, out string node, out string treeEvent)
        {
            string[] id_treeEvent = (node_event + ':').Split(':');
            node = id_treeEvent[0].Trim();
            treeEvent = id_treeEvent[1].Trim();
            if (node == "")
            {
                throw (new ArgumentException("Aufruf: Job.Add(\"Knoten-Id + : + TreeEvent-Name\", WorkerShell[])"));
            }
        }

        #endregion private members

    }

    // Stellt definierte öffentliche Zugriffsmethoden für das in der
    // besitzenden Klasse (Job) definierte private Dictionary _workers
    // derart zur Verfügung, dass von außen betrachtet, diese Klasse ein
    // entsprechendes öffentliches Pendant zu _workers definiert.
    // Diese Implementierung stellt ein Alternativkonzept zur Ableitung
    // der Dictionary-Klasse dar, welches prinzipiell auch sealed-Klassen
    // quasi "ableiten" kann.
    // Ist zwar hier nicht erforderlich, hat sich aber so entwickelt und
    // bleibt zur Demo auch so.
    /// <summary>
    /// Dictionary, das WorkerShell-Arrays zu aus Knoten-Id + ":" + TreeEvents
    /// gebildeten Keys enthält.
    /// </summary>
    class Workers
    {
        #region public members

        // Prüft, ob das private Dictionary zu einem aus durch ":" getrennten NodeId und TreeEvent zusammengesetzten
        // Key ein WorkerShell-Array enthält.
        /// <summary>
        /// Prüft, ob Workers zu einem aus durch ":" getrennten NodeId und TreeEvent zusammengesetzten
        /// Key ein WorkerShell-Array enthält.
        /// </summary>
        /// <param name="node_id"></param>
        /// <returns></returns>
        public bool ContainsCombinedKey(string node_id)
        {
            return this._jobContainsCombinedKey(node_id);
        }

        // Fügt ein Element in das private Dictionary _workers von Job ein.
        // Das Dictionary enthält zu jedem Event ein Dictionary von NodeIDs
        // mit zugeordneten WorkerShell-Arrays.
        /// <summary>
        /// Fügt ein WorkerShell-Array mit einem aus Knoten-Id + ":" + TreeEventsString
        /// gebildeten Key in Workers ein.
        /// </summary>
        /// <param name="node_event">Ein String mit durch ":" getrennten NodeId und TreeEvents.</param>
        /// <param name="workerArray">Ein Array von WorkerShells.</param>
        public void Add(string node_event, WorkerShell[] workerArray)
        {
            this._jobAddWorkers(node_event, workerArray);
        }

        #endregion public members

        #region internal members

        // Liefert die Keys des in Job privaten Dictionarys '_workers' über die bei der
        // Instanziierung dieser Klasse durch die instanziierenden Klasse (Job)
        // übergebene Funktion 'jobGetKeys'.
        /// <summary>
        /// Liefert die Keys von Workers.
        /// </summary>
        internal Dictionary<string, Dictionary<string, WorkerShell[]>>.KeyCollection Keys
        {
            get
            {
                return this._jobGetKeys();
            }
        }

        // Liefert die Values des in Job privaten Dictionarys '_workers' über die bei der
        // Instanziierung dieser Klasse durch die instanziierenden Klasse (Job)
        // übergebene Funktion 'jobGetValues'.
        /// <summary>
        /// Liefert die Values von Workers.
        /// </summary>
        internal Dictionary<string, Dictionary<string, WorkerShell[]>>.ValueCollection Values
        {
            get
            {
                return this._jobGetValues();
            }
        }

        // Indexer: liefert zu einem aus Knoten-Id + : + TreeEvent-Name zusammengesetzten Key
        // direkt das zugehörige WorkerShell-Array aus dem privaten Dictionarys _workers von Job
        // durch die von Job bei der Instanziierung von Workers übergebene Methode jobGetWorkers.
        /// <summary>
        /// Indexer: liefert zu einem aus Knoten-Id + : + TreeEvent-Name zusammengesetzten Key
        /// das zugehörige WorkerShell-Array aus Workers.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Das WorkerShell-Array zu einem aus Knoten-Id + : + TreeEvent-Name zusammengesetzten Key.</returns>
        internal WorkerShell[] this[string key]
        {
            get
            {
                return this._jobGetWorkers(key);
            }
        }

        /// <summary>
        /// Konstruktor - übernimmt die 'Zugriffsmethoden auf das private Dictionary
        /// "Workers" in der instanziierenden Klasse (hier Job).
        /// </summary>
        /// <param name="jobAddWorkers">Fügt dem privaten Dictionary _workers von Job ein Element hinzu.</param>
        /// <param name="jobGetWorkers">Holt zu einem aus Knoten-Id + : + TreeEvent-Namen zusammengesetzten Key
        /// direkt das zugehörige Worker-Array aus dem privaten Dictionarys _workers von Job.</param>
        /// <param name="jobContainsCombinedKey">Prüft, ob das private Dictionary zu einem aus durch ":" getrennten
        /// NodeId und TreeEvents zusammengesetzten Key ein WorkerShell-Array enthält.</param>
        /// <param name="jobGetKeys">Holt die Liste von Keys des privaten Dictionarys _workers von Job.</param>
        /// <param name="jobGetValues">Holt die Liste von Keys des privaten Dictionarys _workers von Job.</param>
        internal Workers(Action<string, WorkerShell[]> jobAddWorkers,
                        Func<string, WorkerShell[]> jobGetWorkers,
                        Func<string, bool> jobContainsCombinedKey,
                        Func<Dictionary<string, Dictionary<string, WorkerShell[]>>.KeyCollection> jobGetKeys,
                        Func<Dictionary<string, Dictionary<string, WorkerShell[]>>.ValueCollection> jobGetValues)
        {
            this._jobAddWorkers = jobAddWorkers;
            this._jobGetKeys = jobGetKeys;
            this._jobGetWorkers = jobGetWorkers;
            this._jobGetValues = jobGetValues;
            this._jobContainsCombinedKey = jobContainsCombinedKey;
        }

        #endregion internal members

        #region private members

        private Action<string, WorkerShell[]> _jobAddWorkers;
        private Func<Dictionary<string, Dictionary<string, WorkerShell[]>>.KeyCollection> _jobGetKeys;
        private Func<string, WorkerShell[]> _jobGetWorkers;
        private Func<Dictionary<string, Dictionary<string, WorkerShell[]>>.ValueCollection> _jobGetValues;
        private Func<string, bool> _jobContainsCombinedKey;

        #endregion private members

    }

}
