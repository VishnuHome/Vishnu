using System;
using System.Threading;
using System.IO;
using Vishnu.Interchange;
using System.Collections.Concurrent;
using NetEti.ApplicationControl;
using System.Text;

namespace LogicalTaskTree
{
    /// <summary>
    /// Kapselt einen internen (TreeEvent-) oder externen (Dll-) Trigger.
    /// </summary>
    /// <remarks>
    /// File: TriggerShell.cs
    /// Autor: Erik Nagel
    ///
    /// 19.07.2013 Erik Nagel: erstellt.
    /// 18.10.2014 Erik Nagel: komplett überarbeitet.
    /// 20.04.2019 Erik Nagel: Statt des unnützen boolean-Platzhalters wird jetzt der Knoten selbst
    ///                        in RegisteredMayBeTriggeredNodePathes gespeichert;
    /// </remarks>
    public class TriggerShell : INodeTrigger, IDisposable
    {
        #region public members

        #region INodeTrigger members

        /// <summary>
        /// Enthält Informationen zum besitzenden Trigger.
        /// Implementiert sind NextRun und NextRunInfo. Für das Hinzufügen weiterer
        /// Informationen kann die Klasse TriggerInfo abgeleitet werden.
        /// </summary>
        public TriggerInfo? Info
        {
            get
            {
                if (this._slaveTriggerShell == null)
                {
                    if (this.Slave != null)
                    {
                        return this.Slave.Info;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return this._slaveTriggerShell.Info;
                }
            }
            set
            {
            }
        }

        /// <summary>
        /// Startet den Trigger mit der aufzurufenden Callback-Routine als Parameter.
        /// Der Trigger wird nur dann tatsächlich neu gestartet, wenn alle vorher über
        /// RegisterTriggerIt angemeldeten Knoten auch Trigger.Start aufgerufen haben
        /// und der Trigger nicht schon läuft.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das Trigger.Start aufruft.</param>
        /// <param name="internalTriggerParameters">Spezifische Aufrufparameter oder null.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        /// <returns>True, wenn der Trigger durch diesen Aufruf tatsächlich gestartet wurde.</returns>
        public bool Start(object? triggerController, object? internalTriggerParameters, Action<TreeEvent> triggerIt)
        {
            if (this._slaveTriggerShell != null)
            {
                return this._slaveTriggerShell.Start(triggerController, internalTriggerParameters, triggerIt);
            }
            else
            {
                lock (this._startTriggerLocker)
                {
                    if (triggerController != null && !this._triggerControllers.ContainsKey(triggerController))
                    {
                        if (this._triggerControllers.TryAdd(triggerController, triggerIt))
                        {
                            this.TriggerIt += triggerIt;
                            Interlocked.Increment(ref this._registeredHandlersCounter);
                        }
                    }
                    // Thread.Sleep(50); // DEBUG Fehler provozieren
                    if (!this._isStarted && (this._registeredHandlersCounter >= this.RegisteredMayBeTriggeredNodePathes.Count))
                    {
                        StringBuilder info = new StringBuilder();
                        foreach (object item in this._triggerControllers.Keys)
                        {
                            if (item is LogicalNode)
                            {
                                info.Append(((LogicalNode)item).Id + ";");
                            }
                            else
                            {
                                if (item is WorkerShell)
                                {
                                    info.Append(((WorkerShell)item).SlavePathName + ";");
                                }
                            }
                        }
                        if (!this.HasTreeEventTrigger)
                        {
                            // hot plug
                            if (!File.Exists(this._slavePathName) || (File.GetLastWriteTime(this._slavePathName) != this._lastDllWriteTime))
                            {
                                this._slave = null;
                            }
                            if (this._slave == null)
                            {
                                this._slave = this.dynamicLoadSlaveDll(this._slavePathName);
                            }
                        }
                        if (this._slave != null)
                        {
                            string? effectiveTriggerParameters = this.TriggerParameters?.ToString();
                            if (!(this._slave is TreeEventTrigger))
                            {
                                this._lastDllWriteTime = File.GetLastWriteTime(this._slavePathName);
                                if (internalTriggerParameters?.ToString() == "UserRun")
                                {
                                    if (effectiveTriggerParameters != null)
                                    {
                                        effectiveTriggerParameters += "|UserRun";
                                    }
                                    else
                                    {
                                        effectiveTriggerParameters = "UserRun";
                                    }
                                }
                            }
                            this.Slave?.Start(triggerController, effectiveTriggerParameters, this.SlaveTriggersIt);
                            this._isStarted = true;
                            return true;
                        }
                        else
                        {
#if DEBUG
                            InfoController.Say(String.Format("Load failure on assembly '{0}'", this._slavePathName));
#endif
                            throw new ApplicationException(String.Format("Load failure on assembly '{0}'", this._slavePathName));
                        }
                    }
                    else
                    {
                        // der Trigger läuft schon oder wartet noch auf fehlende Consumer.
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Stoppt den Trigger wenn alle zu triggernden Knoten Trigger.Stop aufgerufen haben.
        /// </summary>
        /// <param name="triggerController">Das Objekt, das Trigger.Stop aufruft.</param>
        /// <param name="triggerIt">Die aufzurufende Callback-Routine, wenn der Trigger feuert.</param>
        public void Stop(object? triggerController, Action<TreeEvent> triggerIt)
        {
            if (this._slaveTriggerShell != null)
            {
                this._slaveTriggerShell.Stop(triggerController, triggerIt);
            }
            else
            {
                lock (this._startTriggerLocker)
                {
                    if (triggerController != null && this._triggerControllers.ContainsKey(triggerController))
                    {
                        this._triggerControllers.TryRemove(triggerController, out this._dummyActionTreeEvent);
                        this.TriggerIt -= this._dummyActionTreeEvent;
                        Interlocked.Decrement(ref this._registeredHandlersCounter);
                    }
                    if (this._isStarted && this._triggerControllers.Count == 0)
                    {
                        if (this._slave != null)
                        {
                            this.Slave?.Stop(triggerController, this.SlaveTriggersIt);
                        }
                        this._registeredHandlersCounter = 0;
                        this._isStarted = false;
                    }
                }
            }
        }

        #endregion INodeTrigger members

        #region IDisposable Implementation

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Hier wird aufgeräumt: ruft für alle User-Elemente, die Disposable sind, Dispose() auf.
        /// </summary>
        /// <param name="disposing">Bei true wurde diese Methode von der öffentlichen Dispose-Methode
        /// aufgerufen; bei false vom internen Destruktor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._slave != null && this._slave is IDisposable)
                {
                    try
                    {
                        ((IDisposable)this._slave).Dispose();
                    }
                    catch { };
                }
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~TriggerShell()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        ///// <summary>
        ///// True, wenn die TriggerShell einen TreeEventTrigger kapselt.
        ///// </summary>
        //public bool HasTreeEventTrigger
        //{
        //  get
        //  {
        //    return (this.Slave != null) && (this.Slave is TreeEventTrigger) || this._slaveTriggerShell != null && this._slaveTriggerShell.HasTreeEventTrigger;
        //  }
        //}

        /// <summary>
        /// True, wenn die TriggerShell einen TreeEventTrigger kapselt.
        /// </summary>
        public bool HasTreeEventTrigger { get; private set; }

        /// <summary>
        /// Name eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public string? ReferencedNodeName { get; private set; }

        /// <summary>
        /// Liefert den Trigger, wenn die TriggerShell einen TreeEventTrigger kapselt oder null.
        /// </summary>
        /// <returns>Zugeordneter TreeEventTrigger oder der einer zugeordneten Slave-TriggerShell oder null.</returns>
        public TreeEventTrigger? GetTreeEventTrigger()
        {
            if ((this.Slave != null) && (this.Slave is TreeEventTrigger))
            {
                return (this.Slave as TreeEventTrigger);
            }
            if (this._slaveTriggerShell != null)
            {
                return this._slaveTriggerShell.GetTreeEventTrigger();
            }
            return null;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="slavePathName">Dateipfad und Name einer Dll, die INodeTrigger implementiert.</param>
        /// <param name="triggerParameters">String mit Steuerparametern für den Trigger.</param>
        public TriggerShell(string slavePathName, object? triggerParameters) : this(triggerParameters)
        {
            this._slavePathName = slavePathName;
            string userEventNames = TreeEvent.GetUserEventNamesForInternalEventNames(this._slavePathName);
            if (!String.IsNullOrEmpty(userEventNames))
            {
                this.Slave = this.TriggerParameters as TreeEventTrigger;
                this.HasTreeEventTrigger = true;
                this.ReferencedNodeName = this.TriggerParameters?.ToString();
            }
            this._lastDllWriteTime = DateTime.MinValue;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="triggerShellReference">Name eines benannten Triggers.</param>
        /// <param name="triggerParameters">String mit Zusatzinformationen (Node-Id).</param>
        /// <param name="dummy">Dient nur zur Unterscheidung der Signatur dieses zum anderen Konstruktor.</param>
        public TriggerShell(string triggerShellReference, object? triggerParameters, bool dummy) : this(triggerParameters)
        {
            this.TriggerShellReference = triggerShellReference;
            string internalEventNames = TreeEvent.GetInternalEventNamesForUserEventNames(this.TriggerShellReference);
            if (!String.IsNullOrEmpty(internalEventNames))
            {
                this.HasTreeEventTrigger = true;
                this.ReferencedNodeName = this.TriggerParameters?.ToString();
            }
            this._slaveTriggerShell = null;
        }

        /// <summary>
        /// Liefert den Namen des Triggers, der einem Checker zugeordnet werden soll.
        /// </summary>
        /// <returns>Namen des Triggers, der dem Checker zugeordnet werden soll oder null.</returns>
        public string? GetTriggerReference()
        {
            return this.TriggerShellReference;
        }

        /// <summary>
        /// Liefert die zuletzt übergebenen Parameter dieses Triggers.
        /// </summary>
        /// <returns>die zuletzt übergebenen Parameter dieses Triggers.</returns>
        public object? GetTriggerParameters()
        {
            return this.TriggerParameters;
        }

        /// <summary>
        /// Bei TriggerShells, die bei der Instanziierung nur eine Namensreferenz
        /// mitbekommen haben, wird hier nachträglich der Trigger übergeben.
        /// </summary>
        /// <param name="triggerShell">Referenzierte TriggerShell.</param>
        public void SetSlaveTriggerShell(TriggerShell? triggerShell)
        {
            if (this._slaveTriggerShell != triggerShell)
            {
                this._slaveTriggerShell = triggerShell;
            }
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Enthält die Pfade aller von diesem Trigger zu triggernden Knoten und
        /// die Knoten selbst als LogicalNodes;
        /// Hintergrund: ein Trigger, der mehrere Knoten triggert, feuert erst dann
        /// das erste Mal, wenn alle hier vorangemeldeten Knoten auch tatsächlich
        /// Trigger.Start aufgerufen haben. Damit wird sichergestellt, dass alle
        /// abhängigen Knoten gestartet sind, bevor der Trigger das erste mal
        /// feuert.
        /// </summary>
        internal ConcurrentDictionary<string, LogicalNode> RegisteredMayBeTriggeredNodePathes;

        /// <summary>
        /// Registriert einen Knoten über seinen eindeutigen Path für den Trigger;
        /// Hintergrund: ein Trigger, der mehrere Knoten triggert, feuert erst dann
        /// das erste Mal, wenn alle hier vorangemeldeten Knoten auch tatsächlich
        /// Trigger.Start aufgerufen haben. Damit wird sichergestellt, dass alle
        /// abhängigen Knoten gestartet sind, bevor der Trigger das erste mal
        /// feuert.
        /// </summary>
        /// <param name="path">Eindeutiger Pfad des später zu triggernden Knotens.</param>
        /// <param name="node">Der Knoten selbst als LogicalNode.</param>
        internal void RegisterTriggerIt(string path, LogicalNode node)
        {
            if (this._slaveTriggerShell != null)
            {
                this._slaveTriggerShell.RegisterTriggerIt(path, node);
            }
            else
            {
                lock (this._registerHandlerLocker)
                {
                    //this.unregisterTriggerIt(path);
                    bool couldAdd = this.RegisteredMayBeTriggeredNodePathes.TryAdd(path, node);
#if DEBUG
                    //if (couldAdd)
                    //{
                    //    InfoController.GetInfoPublisher().Publish(null, String.Format("#s# {0}: this.RegisterTriggerIt, Pfad: {1}", this.GetType().Name, path), InfoType.NoRegex);
                    //}
                    //else
                    //{
                    //    InfoController.GetInfoPublisher().Publish(null, String.Format("#s# {0}: this.RegisterTriggerIt not added, Pfad: {1}", this.GetType().Name, path), InfoType.NoRegex);
                    //}
#endif
                }
            }
        }

        /// <summary>
        /// Löscht die Registrierung für einen Knoten (passiert für einen Knoten und
        /// seine Unterknoten beim UserBreak auf den Knoten). Der Knoten wird dann beim
        /// nächsten Aufruf nicht mehr berücksichtigt.
        /// </summary>
        /// <param name="path">Eindeutiger Pfad des Knotens.</param>
        internal void UnregisterTriggerIt(string path)
        {
#if DEBUG
            // InfoController.GetInfoPublisher().Publish(null, String.Format("#11# {0}: this.UnregisterTriggerIt, Pfad: {1}", this.GetType().Name, path), InfoType.NoRegex);
#endif
            this.unregisterTriggerIt(path);
        }

        #endregion internal members

        #region private members

        // Wird ausgelöst, wenn das Trigger-Ereignis (z.B. Timer) eintritt. 
        private event Action<TreeEvent>? TriggerIt;

        // Instanz vom INodeTrigger. Macht die Prüf-Arbeit.
        private INodeTrigger? Slave
        {
            get
            {
                return this._slave;
            }
            set
            {
                if (this._slave != value)
                {
                    this._slave = value;
                }
            }
        }

        /// <summary>
        /// Bei TreeEventTriggern der Knoten, auf den der Trigger reagiert.
        /// </summary>
        internal object? TriggerParameters;

        /// <summary>
        /// Bei TreeEventTriggern die Events, auf die der Trigger feuert.
        /// </summary>
        internal string? TriggerShellReference;

        private string _slavePathName = String.Empty;
        private INodeTrigger? _slave;
        private DateTime _lastDllWriteTime;
        private VishnuAssemblyLoader _assemblyLoader;
        private object _startTriggerLocker;
        private TriggerShell? _slaveTriggerShell;
        private bool _isStarted;
        private ConcurrentDictionary<object, Action<TreeEvent>> _triggerControllers;
        private object _registerHandlerLocker;
        private Action<TreeEvent>? _dummyActionTreeEvent;
        private int _registeredHandlersCounter;

        private TriggerShell(object? triggerParameters)
        {
            this.TriggerParameters = triggerParameters;
            this._isStarted = false;
            this._startTriggerLocker = new object();
            this._triggerControllers = new ConcurrentDictionary<object, Action<TreeEvent>>();
            this._registerHandlerLocker = new object();
            this.RegisteredMayBeTriggeredNodePathes = new ConcurrentDictionary<string, LogicalNode>();
            this._registeredHandlersCounter = 0;
            this._dummyActionTreeEvent = null; // new Action<TreeEvent>(x => { });
            this.HasTreeEventTrigger = false;
            this.ReferencedNodeName = null;
            this.TriggerShellReference = null;
            this._assemblyLoader = VishnuAssemblyLoader.GetAssemblyLoader();
        }

        /// <summary>
        /// Löscht die Registrierung für einen Knoten (passiert für einen Knoten und
        /// seine Unterknoten beim UserBreak auf den Knoten). Der Knoten wird dann beim
        /// nächsten Aufruf nicht mehr berücksichtigt.
        /// </summary>
        /// <param name="path">Eindeutiger Pfad des Knotens.</param>
        private void unregisterTriggerIt(string path)
        {
            if (path != null)
            {
                LogicalNode? dummy;
                this.RegisteredMayBeTriggeredNodePathes.TryRemove(path, out dummy);
                this._slaveTriggerShell?.UnregisterTriggerIt(path);
            }
        }

        // Lädt eine Plugin-Dll dynamisch. Muss keine Namespaces oder Klassennamen
        // aus der Dll kennen, Bedingung ist nur, dass eine Klasse in der Dll das
        // Interface INodeTrigger implementiert. Die erste gefundene Klasse wird
        // als Instanz von INodeTrigger zurückgegeben.
        private INodeTrigger? dynamicLoadSlaveDll(string slavePathName)
        {
            return (INodeTrigger?)this._assemblyLoader.DynamicLoadObjectOfTypeFromAssembly(slavePathName, typeof(INodeTrigger));
        }

        /// <summary>
        /// Wird angesprungen, wenn der zugeordnete INodeTrigger feuert:
        /// gibt das Event weiter.
        /// </summary>
        /// <param name="source">Das Auslösende TreeEvent.</param>
        private void SlaveTriggersIt(TreeEvent source)
        {
            if (this.TriggerIt != null)
            {
                Delegate[] invocations = this.TriggerIt.GetInvocationList();
                string[] invocationStrings = new string[invocations.Length];
                for (int i = 0; i < invocations.Length; i++)
                {
                    string nodeId = "WorkerShell";
                    LogicalNode? targetNode = (LogicalNode?)invocations[i].Target;
                    if (targetNode != null)
                    {
                        // nodeId = (invocations[i].Target as LogicalNode).Id;
                        nodeId = targetNode.Id;
                    }
                    invocationStrings[i] = nodeId;
                }
                Array.Sort(invocationStrings);
                this.TriggerIt(source);
            }
        }

        #endregion private members
    }
}
