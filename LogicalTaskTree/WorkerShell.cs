﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using NetEti.ApplicationControl;
using NetEti.Globals;
using Vishnu.Interchange;
using System.Xml.Linq;

namespace LogicalTaskTree
{

    /// <summary>
    /// Kapselt den Aufruf einer externen Arbeitsroutine,
    /// die als Reaktion auf eine definierte Änderung des Tree-Zustands
    /// (TreeEvent) als externe Exe ausgeführt wird (fire and forget)
    /// TODO: später evtl. auch als lightweight Dll-Plugin realisieren.
    /// </summary>
    /// <remarks>
    /// File: WorkerShell.cs
    /// Autor: Erik Nagel
    ///
    /// 01.03.2013 Erik Nagel: erstellt
    /// </remarks>
    public class WorkerShell : NodeShellBase, INodeWorker
    {
        #region public members

        #region INodeWorker implementation

        /// <summary>
        /// Startet einen zuständigen Worker, nachdem sich ein definierter Zustand
        /// (TreEvent) im Tree geändert hat.
        /// Intern wird für jeden Exec eine eigene Task gestartet.
        /// </summary>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="nodeId">Id des Knotens, zu dem der Worker gehört.</param>
        /// <param name="eventParameters">Klasse mit Informationen zum Ereignis.</param>
        /// <param name="isResetting">True, wenn der Worker mit einem letzten Aufruf (ok-Meldung) beendet werden soll.</param>
        public void Exec(TreeParameters treeParameters, string nodeId, TreeEvent eventParameters, bool isResetting)
        {
            WorkerArguments args;
            if (this.Trigger != null)
            {
                args = new WorkerArguments(0, treeParameters.ToString(), nodeId, eventParameters, null);
            }
            else
            {
                args = new WorkerArguments(1, treeParameters.ToString(), nodeId, eventParameters, null);
            }
            if (isResetting)
            {
                if (this.Trigger != null)
                {
                    this.Trigger.Stop(this, this.Trigger_TriggerIt);
                }
                args.Predecessor = this._lastWorkerArguments;
                this._lastWorkerArguments = null;
                this._workerArgumentsEventQueue = new ConcurrentQueue<WorkerArguments>(); // leeren
            }
            else
            {
                this._lastWorkerArguments = args;
                ResultDictionary copiedResults = new ResultDictionary();
                foreach (string id in this._lastWorkerArguments.TreeEventInfo.Results.Keys)
                {
                    Result res = this._lastWorkerArguments.TreeEventInfo.Results[id];
                    if (res != null)
                    {
                        Result newRes = new Result(id, res.Logical, res.State, res.LogicalState, res.ReturnObject);
                        copiedResults.Add(id, newRes);
                    }
                }
                this._lastWorkerArguments.TreeEventInfo.Results = copiedResults;
            }
            this._workerArgumentsEventQueue.Enqueue(args);
            this._nodeCancellationTokenSource = new CancellationTokenSource();
            this._cancellationToken = this._nodeCancellationTokenSource.Token;
            if (this.Trigger != null && !isResetting)
            {
                this.Trigger.Start(this, null, this.Trigger_TriggerIt);
            }
            else
            {
                // TODO: Diese Differenzierung durch eine elegantere Umsetzung überflüssig machen.
                this._asyncWorkerTask = new Task(() => this.execAsync(eventParameters, false));
                InfoController.Say(String.Format("{0}/{1} {2}: starting Thread", nodeId, eventParameters.Name,
                  System.Reflection.MethodBase.GetCurrentMethod().Name));
                this._asyncWorkerTask.Start();
            }
        }

        #endregion INodeWorker implementation

        /// <summary>
        /// Die bei Änderung des Zustands von Logical aufzurufende Exe.
        /// </summary>
        public string SlavePathName
        {
            get
            {
                return this._slavePathName;
            }
            set
            {
                this._slavePathName = value;
            }
        }

        /// <summary>
        /// Bei True werden die Parameter über eine XML-Datei übergeben, ansonsten über die Kommandozeile;
        /// Default: false.
        /// </summary>
        public bool TransportByFile { get; set; }

        /// <summary>
        /// Zustand des Workers.
        /// None, Valid, Invalid.
        /// </summary>
        public NodeWorkerState WorkerState { get; private set; }

        /// <summary>
        /// Abbrechen der Task.
        /// Worker Scheduler (nicht den Worker!) über CancellationToken abbrechen.
        /// </summary>
        public void BreakExec()
        {
            //this._lastWorkerArguments = null;
            if (this.Trigger != null)
            {
                this.Trigger.Stop(this, this.Trigger_TriggerIt);
            }
            if (this._nodeCancellationTokenSource != null)
            {
                this._nodeCancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Returnt True, wenn der SlavePathName existiert und Zugriff möglich ist.
        /// </summary>
        /// <returns>True, wenn der SlavePathName existiert und Zugriff möglich ist.</returns>
        public bool Exists()
        {
            if (File.Exists(this.SlavePathName))
            {
                Stream s = null;
                try
                {
                    s = File.Open(this.SlavePathName, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch
                {
                    return false;
                }
                s.Close();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Konstruktor - übernimmt den Pfad zur Worker-Exe, und einen Parameter-String, der beim
        /// Aufruf der Exe zusätzlich übergeben wird. Der Parameter-String kann neben beliebigen, durch
        /// die Programm-Session auflösbaren Platzhaltern folgende Platzhalter
        /// enthalten, die beim Aufruf der Worker-Exe durch aktuelle Laufzeit-Werte ersetzt werden:
        ///   "%Event%" = Name des Ereignisses, das zum Aufruf des Workers geführt hat,
        ///   "%Source%" = Quelle des Ereignisses (Knoten, in dem das Ereignis zuerst aufgetreten ist),
        ///   "%Sender%" = Knoten, der aufgrund des Ereigniss aktuell den Worker aufruft,
        ///   "%Timestamp%" = aktuelles Datum mit aktueller Uhrzeit im Format "dd.MM.yyyy HH.mm.ss",
        ///   "%Logical%" = aktueller logischer Wert des Senders,
        ///   "%Exception%" = Exception.Message, falls %Event% gleich "Exception" ist, ansonsten "".
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Exe.</param>
        /// <param name="slaveParameters">Aufrufparameter der Exe als XML.</param>
        public WorkerShell(string slavePathName, XElement slaveParameters) : this(slavePathName, slaveParameters, false, null) { }

        /// <summary>
        /// Konstruktor - übernimmt den Pfad zur Worker-Exe, und einen Parameter-String, der beim
        /// Aufruf der Exe zusätzlich übergeben wird. Optional kann zusätzlich ein Trigger übergeben werden,
        /// der einen einmal aktivierten Worker, wiederholt aufruft.
        /// Der Parameter-String kann neben beliebigen, durch die Programm-Session auflösbaren Platzhaltern
        /// folgende Platzhalter enthalten, die beim Aufruf der Worker-Exe durch aktuelle Laufzeit-Werte
        /// ersetzt werden:
        ///   "%Event%" = Name des Ereignisses, das zum Aufruf des Workers geführt hat,
        ///   "%Source%" = Quelle des Ereignisses (Knoten, in dem das Ereignis zuerst aufgetreten ist),
        ///   "%Sender%" = Knoten, der aufgrund des Ereigniss aktuell den Worker aufruft,
        ///   "%Timestamp%" = aktuelles Datum mit aktueller Uhrzeit im Format "dd.MM.yyyy HH.mm.ss",
        ///   "%Logical%" = aktueller logischer Wert des Senders,
        ///   "%Exception%" = Exception.Message, falls %Event% gleich "Exception" ist, ansonsten "".
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Exe.</param>
        /// <param name="slaveParameters">Aufrufparameter der Exe als XML.</param>
        /// <param name="transportByFile">Bei True werden die Parameter über eine XML-Datei übergeben, ansonsten über die Kommandozeile.</param>
        /// <param name="workerTrigger">Ein Trigger, der den Job wiederholt aufruft oder null.</param>
        public WorkerShell(string slavePathName, XElement slaveParameters, bool transportByFile, INodeTrigger workerTrigger)
        {
            this.Trigger = workerTrigger;
            this._dontExecute = GenericSingletonProvider.GetInstance<AppSettings>().GetValue<bool>("NoWorkers", false);
            this.SlavePathName = GenericSingletonProvider.GetInstance<AppSettings>().GetStringValue("__NOPPES__", slavePathName);
            // Regex.Replace(slavePathName, @"%HOME%", AppDomain.CurrentDomain.BaseDirectory, RegexOptions.IgnoreCase);
            this._slaveParameters = slaveParameters;
            this.TransportByFile = transportByFile;
            this._nodeCancellationTokenSource = null;
            this.WorkerState = NodeWorkerState.None;
            this._workerLocker = new object();
            this._workerArgumentsEventQueue = new ConcurrentQueue<WorkerArguments>();
            this._lastWorkerArguments = null;
        }

        private void Trigger_TriggerIt(TreeEvent source)
        {
            if (this._lastWorkerArguments != null)
            {
                this._lastWorkerArguments.CallCounter++;
                this._workerArgumentsEventQueue.Enqueue(this._lastWorkerArguments);
            }
            this.execAsync(source, true);
        }

        /// <summary>
        /// Ein optionaler Trigger, der den Job wiederholt aufruft oder null.
        /// Wird vom IJobProvider bei der Instanziierung mitgegeben.
        /// </summary>
        public INodeTrigger Trigger { get; set; }

        #endregion public members

        #region private members

        /// <summary>
        /// Hierüber kann eine Task für einen Loop zum Mehrfach-Start des Workers
        /// von außen abgebrochen werden.
        /// </summary>
        private CancellationTokenSource _nodeCancellationTokenSource { get; set; }

        /// <summary>
        /// Über die CancellationTokenSource kann dieses Token auf
        /// Abbruch gesetzt werden, was die Task mit dem Loop beendet.
        /// </summary>
        private CancellationToken _cancellationToken;

        // Der Start eines Workers läuft immer asynchron.
        private Task _asyncWorkerTask;
        private object _workerLocker;
        private ConcurrentQueue<WorkerArguments> _workerArgumentsEventQueue;
        private Process _externalProcess;

        /// <summary>
        /// Eigene (Timer-)Task Action für den Exec eines Workers.
        /// </summary>
        /// <param name="source">Das auslösende Ereignis.</param>
        /// <param name="calledFromTrigger">True, wenn der Aufruf vom Trigger kommt.</param>
        private void execAsync(TreeEvent source, bool calledFromTrigger)
        {
            lock (this._workerLocker)
            {
                Thread.CurrentThread.Name = "execAsync";
                if (!this._cancellationToken.IsCancellationRequested)
                {
                    WorkerArguments workerArguments;
                    while (!this._workerArgumentsEventQueue.TryDequeue(out workerArguments))
                    {
                        Thread.Sleep(10);
                    }
                    if (workerArguments.Predecessor != null)
                    {
                        workerArguments = workerArguments.Predecessor;
                        workerArguments.CallCounter *= -1; // Ein negativer CallCounter heist Reset. Der Absolutwert entspricht dem bisher höchsten Aufrufwert.
                    }
                    this.exec(workerArguments.CallCounter, new TreeParameters(workerArguments.TreeInfo, null), workerArguments.NodeInfo, workerArguments.TreeEventInfo);
                }
            }
        }

        /// <summary>
        /// Hier wird der (normalerweise externe) Arbeitsprozess ausgeführt.
        /// In den TreeParameters oder SlaveParameters (beim Konstruktor übergeben)
        /// enthaltene Pipes ('|') werden beim Aufruf des Workers als Leerzeichen zwischen
        /// mehreren Kommandozeilenparametern interpretiert.
        /// </summary>
        /// <param name="treeParameters">Für den gesamten Tree gültige Parameter oder null.</param>
        /// <param name="nodeId">Id des Knotens, der diesen Worker besitzt.</param>
        /// <param name="eventParameters">Klasse mit Informationen zum auslösenden Ereignis.</param>
        /// <param name="callCounter">Aufrufzähler (1-n). Bei 0 wird der Worker resettet (Fehler behoben).</param>
        private void exec(int callCounter, TreeParameters treeParameters, string nodeId, TreeEvent eventParameters)
        {
            try
            {
                this._externalProcess = new Process();
                string countString = callCounter.ToString();
                string logicalString = eventParameters.Logical == null ? "null" : eventParameters.Logical.ToString();
                string konvertedSlaveParameters;
                if (!this.TransportByFile)
                {
                    konvertedSlaveParameters = this._slaveParameters.Value;
                }
                else
                {
                    konvertedSlaveParameters = this._slaveParameters.ToString().Replace('\xA0', ' ').Replace('\x09', ' ');
                }
                string delimiter = "";
                string resultsString = "";
                foreach (string id in eventParameters.Results.Keys)
                {
                    Result res = eventParameters.Results[id];
                    resultsString += delimiter + res.ToString();
                    delimiter = ", ";
                }
                string msg = "";
                if (eventParameters.Results != null && eventParameters.Results.Count > 0)
                {
                    foreach (string key in eventParameters.Results.Keys)
                    {
                        if (key.EndsWith(eventParameters.SourceId))
                        {
                            Result res = eventParameters.Results[key];
                            if (res.ReturnObject != null && (res.ReturnObject is Exception))
                            {
                                msg = (res.ReturnObject as Exception).Message;
                            }
                        }
                    }
                }
                konvertedSlaveParameters = konvertedSlaveParameters.Replace("%Event%", eventParameters.Name)
                  .Replace("%Source%", eventParameters.SourceId)
                  .Replace("%Sender%", eventParameters.SenderId)
                  .Replace("%TreePath%", eventParameters.NodePath)
                  .Replace("%Timestamp%", eventParameters.Timestamp.ToString("dd.MM.yyyy HH.mm.ss"))
                  .Replace("%Logical%", logicalString).Replace("%Counter%", countString)
                  .Replace("%Result%", resultsString)
                  .Replace("%Exception%", msg);
                konvertedSlaveParameters = GenericSingletonProvider.GetInstance<AppSettings>().GetStringValue("__NOPPES__", konvertedSlaveParameters);
                this._externalProcess.StartInfo.FileName = this._slavePathName;
                this._externalProcess.StartInfo.Arguments = countString
                                         + " \"" + treeParameters.Name.Replace("|", "\" \"")
                                         + "\" \"" + nodeId;
                if (!this.TransportByFile)
                {
                    this._externalProcess.StartInfo.Arguments += "\" \"" + konvertedSlaveParameters.Replace(" ", " ").Replace("|", "\" \"") + "\"";
                    // Der 1. Replace ersetzt Leerzeichen durch geschützte Leerzeichen (255).
                }
                else
                {
                    //string parameterFilePath
                    //    = Path.Combine(GenericSingletonProvider.GetInstance<AppSettings>().WorkingDirectory,
                    //        eventParameters.SenderId + "_" + eventParameters.Name + "_" + countString + ".para");
                    string parameterFilePath
                        = Path.Combine(GenericSingletonProvider.GetInstance<AppSettings>().WorkingDirectory,
                            eventParameters.SenderId + "_" + eventParameters.Name + ".para");
                    string[] lines = { "<?xml version=\"1.0\" encoding=\"utf-8\"?>", konvertedSlaveParameters };
                    System.IO.File.WriteAllLines(parameterFilePath, lines);
                    this._externalProcess.StartInfo.Arguments += "\" \"" + parameterFilePath + "\"";
                }
                if (!this._dontExecute)
                {
                    this._externalProcess.Start();
                }
                this.WorkerState = NodeWorkerState.Valid;
            }
            catch (Exception)
            {
                this.WorkerState = NodeWorkerState.Invalid;
            }
        }

        // Instanz vom INodeWorker. Macht die Arbeit.
        private INodeWorker Slave
        {
            get
            {
                return this._slave;
            }
            set
            {
                this._slave = value;
            }
        }

        private string _slavePathName;
        private INodeWorker _slave;
        private XElement _slaveParameters;
        private WorkerArguments _lastWorkerArguments;
        private bool _dontExecute;

        #endregion private members
    }

    class WorkerArguments
    {
        public int CallCounter { get; set; }
        public string TreeInfo { get; set; }
        public string NodeInfo { get; set; }
        public TreeEvent TreeEventInfo { get; set; }
        public WorkerArguments Predecessor { get; set; }

        public WorkerArguments(int callCounter, string treeInfo, string nodeInfo, TreeEvent treeEventInfo, WorkerArguments predecessor)
        {
            this.CallCounter = callCounter;
            this.TreeInfo = treeInfo;
            this.NodeInfo = nodeInfo;
            this.TreeEventInfo = treeEventInfo;
            this.Predecessor = predecessor;
        }
    }
}
