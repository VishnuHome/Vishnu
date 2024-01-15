using System;
using System.Linq;
using System.Threading;
using NetEti.Globals;
using NetEti.ApplicationControl;
using Vishnu.Interchange;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;

namespace LogicalTaskTree
{
    /// <summary>
    /// Endknoten in einem LogicalTaskTree.
    /// Besitzt einen Checker, der die Arbeit macht und ein logisches
    /// Ergebnis zurück liefert, ggf. auch noch ein Rückgabe-Objekt.
    /// </summary>
    /// <remarks>
    /// File: SingleNode.cs
    /// Autor: Erik Nagel
    ///
    /// 01.12.2012 Erik Nagel: erstellt
    /// </remarks>
    public class SingleNode : LogicalNode
    {
        #region public members

        /// <summary>
        /// Der logische Zustand des Knotens.
        /// </summary>
        public override bool? Logical
        {
            get
            {
                this.ThreadUpdateLastLogical(this._logical);
                return this.LastLogical;
            }
            set
            {
                if ((this.LogicalState == NodeLogicalState.Done || this.LogicalState == NodeLogicalState.Fault || this.LogicalState == NodeLogicalState.None)
                   && (this._logical != value || this._logical == null))
                {
                    bool? lastLogical = this._logical;
                    this._logical = value;
                    if (this.Logical != null)
                    {
                        if (this.LastExceptions.Count > 0)
                        {
                            this.OnExceptionCleared(this.LastExceptions.Keys.First());
                        }
                    }
                    if (this._logical != lastLogical)
                    {
                        this.DoNodeLogicalChanged();
                        this.ThreadUpdateLastLogical(this._logical);
                    }
                }
                // 02.06.2018 Nagel auskommentiert: this.LastLogical = this._logical;
            }
        }

        /// <summary>
        /// Der Verarbeitungszustand des Knotens:
        /// Null, None, Waiting, Working, Finished, Triggered, Ready (= Finished | Triggered), Busy (= Waiting | Working) oder CanStart (= None | Ready).
        /// <see cref="Vishnu.Interchange.NodeState"/>
        /// </summary>
        public override NodeState State
        {
            get
            {
                return this._state;
            }
            set
            {
                if (this._state != value)
                {
                    this._state = value; // kritisch!
                    this.ThreadUpdateLastState(value);
                    this.OnNodeStateChanged();
                }
            }
        }

        /// <summary>
        /// Anzahl Endknoten eines Teilbaumes, hier immer = 100.
        /// Normalerweise kann hier nur die 1 (für eine SingleNode) zurückgegeben werden,
        /// durch die Verhundertfachung wird aber eine granulare Fortschrittsmeldung für einen
        /// Knoten ermöglicht. Dies zieht sich dann aber durch den kompletten Baum, weshalb alle
        /// Zählerwerte gedanklich durch 100 geteilt werden müssen um wieder die Anzahl Endknoten
        /// zu erhalten.
        /// <see cref = "SingleNode.SingleNodesFinished" />
        /// </summary>
        public override int SingleNodes
        {
            get
            {
                return 100;
            }
        }

        /// <summary>
        /// Verarbeitungsfortschritt in Prozent.
        /// </summary>
        public override int SingleNodesFinished
        {
            get
            {
                //return (this.LogicalState & NodeLogicalState.Done) > 0 ? (int)this._lastSucceeded : 0;
                return (int)this._lastSucceeded;
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
                    if (!(this.NodeType == NodeTypes.Constant))
                    {
                        return this.RootJobList.SingleNodeUserControlPath;
                    }
                    else
                    {
                        return this.RootJobList.ConstantNodeUserControlPath;
                    }
                }
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
        public virtual NodeCheckerBase? Checker
        {
            get
            {
                return this._checker;
            }
            set
            {
                if (this._checker != value)
                {
                    if (this._checker != null)
                    {
                        this._checker.NodeProgressChanged -= this.checkerProgressChanged;
                    }
                    this._checker = value;
                    if (this._checker != null)
                    {
                        this._checker.ThreadLocked |= this.RootJobList.ThreadLocked;
                        if (this._checker.LockName == null)
                        {
                            this._checker.LockName = this.RootJobList.LockName;
                        }
                        this._checker.NodeProgressChanged += this.checkerProgressChanged;
                    }
                }
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
                return this._lastResult;
            }
            set
            {
                this._lastResult = value;
            }
        }

        /// <summary>
        /// Name eines ursprünglich referenzierten Knotens oder null.
        /// </summary>
        public override string? ReferencedNodeName
        {
            get
            {
                if (this.Checker != null && this.Checker.IsMirror)
                {
                    return this.Checker.ReferencedNodeName;
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
        /// Konstruktor für ein Snapshot-Dummy-Element - übernimmt den Eltern-Knoten.
        /// </summary>
        /// <param name="mother">Der Eltern-Knoten.</param>
        /// <param name="rootJobList">Die Root-JobList</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public SingleNode(LogicalNode mother, JobList rootJobList, TreeParameters treeParams) : base(mother, rootJobList, treeParams)
        {
            this.IsSnapshotDummy = true;
            this._lastSucceeded = 0;
            this._environment = new ResultList();
            this._userControlPath = String.Empty; // Wird nach Instanziierung über die Property gesetzt.
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="id">Eindeutige Kennung des Knotens.</param>
        /// <param name="mother">Id des Parent-Knotens.</param>
        /// <param name="rootJoblist">Die zuständige JobList.</param>
        /// <param name="treeParams">Für den gesamten Tree gültige Parameter oder null.</param>
        public SingleNode(string id, LogicalNode mother, JobList rootJoblist, TreeParameters treeParams)
          : base(id, mother, rootJoblist, treeParams)
        {
            this.IsSnapshotDummy = false;
            this.Checker = null;
            this._returnObject = null;
            this.State = NodeState.None;
            this.LogicalState = NodeLogicalState.None;
            this.Logical = null;
            this._environment = new ResultList();
            this._constantValue = null;
            // Bei allen Konstanten wird nicht der Wert der Konstante, sondern true als
            // Checker-Ergebnis zurückgegeben, außer die Variablen sind selbst null oder boolean.
            this._returnConstantValue = false;
            if (this.Id.StartsWith(@"@"))
            {
                // Konstante
                string name;
                if (this.Id.StartsWith(@"@BOOL."))
                {
                    name = this.Id.Substring(6);
                }
                else
                {
                    name = this.Id.Substring(1);
                }
                if ((new List<string>() { "true", "false", "null" }).Contains(name.ToLower()))
                {
                    // Boolean oder null.
                    this.Name = name;
                    try
                    {
                        if (this.Name.ToLower().Equals("null"))
                        {
                            this._constantValue = null;
                        }
                        else
                        {
                            this._constantValue = Boolean.Parse(this.Name);
                        }
                        this._returnConstantValue = true;
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Erweiterte boolsche Konstanten müssen True, False oder null sein.", ex);
                    }
                }
                else
                {
                    // String.
                    if (this.Id.Length > 1)
                    {
                        this.Name = this.Id.Substring(1);
                    }
                    // leerer String-Wert wird hier zugelassen
                    this._constantValue = this.Id.Substring(1);
                }
                //this.Id = LogicalNode.GenerateInternalId();
                NodeParent parent = (NodeParent?)this.Mother ?? throw new ArgumentNullException("SingleNode: Mother muss vom Typ 'NodeParent' sein.");
                this.Id = ((NodeParent)this.Mother).GenerateNextChildId() + "_" + this.Id;
            }
            this._lastSucceeded = 0;
            this._userControlPath = String.Empty; // Wird nach Instanziierung über die Property gesetzt.
            this._parentViewLocker = new object();
        }

        /// <summary>
        /// Setzt das ReturnObject auf ein Object (für Snapshots).
        /// </summary>
        /// <param name="returnObject">Ein beliebiges Object.</param>
        public void SetReturnObject(object? returnObject)
        {
            lock (this.ResultLocker)
            {
                // 14.08.2020 Erik Nagel+
                if (this._returnObject != null && (this._returnObject is IDisposable))
                {
                    try
                    {
                        ((IDisposable)this._returnObject).Dispose();
                    }
                    catch { }
                }
                // 14.08.2020 Erik Nagel-
                this._returnObject = returnObject;
            }
            this.SetLastResult(); // Nagel: 01.07.2018 aus dem Lock herausgenommen.
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
                stringBuilder.AppendLine(System.IO.Path.GetFileName(String.Format($"    SlavePathName: {slavePathName}")));
            }
            if (!String.IsNullOrEmpty(referencedNodeName))
            {
                stringBuilder.AppendLine(String.Format($"    ReferencedNodeName: {referencedNodeName}"));
            }
            if (!String.IsNullOrEmpty(checkerParameters))
            {
                // stringBuilder.AppendLine(String.Format($"    CheckerParameters: {checkerParameters}"));
                stringBuilder.AppendLine("    CheckerParameters: " + checkerParameters);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieser SingleNode nach logischen Gesichtspunkten
        /// mit dem Inhalt einer übergebenen SingleNode.
        /// </summary>
        /// <param name="obj">Die SingleNode zum Vergleich.</param>
        /// <returns>True, wenn die übergebene SingleNode inhaltlich gleich dieser ist.</returns>
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
            return this.ToString() == (obj as SingleNode)?.ToString();
        }

        /// <summary>
        /// Erzeugt einen Hashcode für diese SingleNode.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Das Result wegen der Kompatibilität zur Liste aufgeblasen.
        /// </summary>
        /// <returns></returns>
        internal override ResultList GetResultList()
        {
            ResultList res = new ResultList();
            res.TryAdd(this.Id, this.LastResult);
            return res;
        }

        /// <summary>
        /// Setzt LastResult auf die aktuellen Werte oder erzeugt
        /// ein new Result mit den aktuellen Werten.
        /// Registriert das LastResult bei der RootJoblist und
        /// schleudert OnLogicalStateChanged.
        /// </summary>
        internal void SetLastResult()
        {
            bool hasChanged = false;
            if (this.LastResult != null)
            {
                if (this.LastResult.Logical != this.LastLogical)
                {
                    this.LastResult.Logical = this.LastLogical;
                    hasChanged = true;
                }
                if (this.LastResult.LogicalState != this.LogicalState)
                {
                    this.LastResult.LogicalState = this.LogicalState;
                    hasChanged = true;
                }
                if (this.LastResult.ReturnObject != this._returnObject)
                {
                    this.LastResult.ReturnObject = this._returnObject;
                    hasChanged = true;
                }
                this.LastResult.Timestamp = DateTime.Now;
            }
            else
            {
                this.LastResult = new Result(this.Id, this.LastLogical, this.LastState, this.LogicalState, this._returnObject);
                //if (this.IsSnapshotDummy) // 17.02.2023 Nagel - TODO: Snapshots testen
                //{
                //    hasChanged = true;
                //}
            }
            this.RootJobList.RegisterResult(this.LastResult);
            this.OnNodeStateChanged(); // damit geänderte Results auch über OnLogicalStateChanged abgegriffen werden können.
            if (hasChanged)
            {
                Guid tmpGuid = Guid.NewGuid();
                this.ProcessTreeEvent("LastResultChanged", null);
                this.RaiseNodeLastResultChangedWithTreeEvent(this, this.LastNotNullLogical, tmpGuid);
            }
        }

        /// <summary>
        /// Triggert das TreeEvent "AnyResultHasChanged" und ruft "OnNodeResultChanged".
        /// </summary>
        /// <param name="source">Die ursprüngliche Quelle der Events.</param>
        /// <param name="lastNotNullLogical">Der logische Wert des Senders.</param>
        /// <param name="eventId">Eine optionale Guid zur eindeutigen Identifizierung des Events.</param>
        protected void RaiseNodeLastResultChangedWithTreeEvent(LogicalNode source, bool? lastNotNullLogical, Guid eventId)
        {
            this.ProcessTreeEvent("AnyResultHasChanged", source);
            this.OnNodeResultChanged(); // Führt bei NodeList zur Neuberechnung des logischen Ergebnisses; bei Vergleichen wichtig.
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
            this.Environment.Clear();
            this.LastExecutingTreeEvent = null;
            this.IsFirstRun = true;
            NodeLogicalState tmpState = this.LogicalState;
            this.LogicalState = NodeLogicalState.Done;
            this.Logical = null; // kommt nur bei NodeLogicalState.Done durch.
            this.LogicalState = tmpState;
            this.DoNodeLogicalChanged();
        }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Hier wird der Checker-Thread gestartet.
        /// Diese Routine läuft asynchron.
        /// </summary>
        /// <param name="source">Auslösendes TreeEvent oder null.</param>
        protected override void DoRun(TreeEvent? source)
        {
            base.DoRun(source);
            if (source != null && source.Name != "UserRun" && this.TriggeredRunDelay > 0)
            {
                Thread.Sleep(this.TriggeredRunDelay);
            }
            if (this.InitNodes)
            {
                this.ResetAllTreeNodes();
            }
            this.State = NodeState.Working;
            this.LogicalState = NodeLogicalState.None;
            this.Logical = null;
            if ((this.CancellationToken.IsCancellationRequested)
              || (this.LastLogicalState == NodeLogicalState.UserAbort))
            {
                return;
            }
            bool? logical = null;
            Exception? runException = null;
            try
            {
                this._lastSucceeded = 0;
                this.OnNodeProgressStarted(this, new ProgressChangedEventArgs(0, null));
                if (this.Checker != null)
                {
                    // 14.07.2022 Nagel+ Wenn die Results oder das Environment des TreeEvents(source) nicht
                    // gesetzt sind, schnell noch die Results und das Environment der Tree-Root mitgeben.
                    if (source == null)
                    {
                        source = new TreeEvent("PseudoTreeEvent", this.Id, this.Id, this.Name, this.Path, null, NodeLogicalState.None,
                            null, null);
                    }
                    if (source.Environment == null || source.Environment.Count == 0)
                    {
                        source.Environment = this.TreeRootJobList.GetEnvironment();
                    }
                    //  18.02.2023 Nagel+- auskommentiert: if (source.Results == null || source.Results.Count == 0)
                    //{
                        source.Results = this.TreeRootJobList.GetResults();
                    //}
                    // 14.07.2022 Nagel-

                    // 14.07.2022 Nagel+
                    /*
                    ResultDictionary resultDictionary = null;
                    if (this.LastResult != null && (source == null || source.NodePath == this.Path))
                    {
                        resultDictionary = new ResultDictionary() { { String.IsNullOrEmpty(this.Name) ? this.Id : this.Name, this.LastResult } };
                    }
                    if (source == null)
                    {
                        source = new TreeEvent("PseudoTreeEvent", this.Id, this.Id, this.Name, this.Path, null, NodeLogicalState.None,
                            null, null);
                    }
                    if (source.NodePath == this.Path)
                    {
                        source.Results = resultDictionary;
                    }
                    */
                    // 14.07.2022 Nagel-

                    logical = this.Checker.Run(null, this.TreeParams, source);
                    if (!this.IsThreadValid(Thread.CurrentThread) || (this.CancellationToken.IsCancellationRequested)
                      || (this.LastLogicalState == NodeLogicalState.UserAbort))
                    {
                        if (this.Trigger != null)
                        {
                            this.State = NodeState.Triggered;
                        }
                        else
                        {
                            this.State = NodeState.Finished;
                        }
                        // DEBUG: InfoController.Say(String.Format("#T# {0}/{1} {2}: unmarking Thread as invalid", this.Id, this.Name,
                        //  System.Reflection.MethodBase.GetCurrentMethod().Name));
                        this.UnMarkThreadAsInvalid(Thread.CurrentThread);
                        return;
                    }
                    if (logical != null || this.AppSettings.AcceptNullResults || this._returnObject == null && this.Checker.ReturnObject != null)
                    {
                        this._returnObject = this.Checker.ReturnObject;
                    }
                    else
                    {
                        if (this.LastExceptions.Count > 0)
                        {
                            this.LogicalState = NodeLogicalState.Fault; // Sorgt dafür, dass bei Ergebnis null der Fehlerstatus erhalten bleibt.
                        }
                    }
                }
                else
                {
                    // Konstante
                    this.checkerProgressChanged(this, new ProgressChangedEventArgs(0, 0));
                    this._returnObject = this._constantValue;
                    this._lastSucceeded = 100;
                    if (this._returnConstantValue)
                    {
                        logical = (bool?)this._constantValue;
                    }
                    else
                    {
                        logical = true; // immer true, wenn der Wert selbst kein BOOLEAN ist.
                    }
                    this.checkerProgressChanged(this, new ProgressChangedEventArgs((int)this._lastSucceeded, 0));
                    if ((!this.CancellationToken.IsCancellationRequested)
                      && (this.LastLogicalState != NodeLogicalState.UserAbort))
                    {
                        this.State = NodeState.None; // 03.06.2018 Nagel
                        this.LogicalState = NodeLogicalState.Done;
                    }
                }
                if ((this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    return;
                }
                if (this.LogicalState == NodeLogicalState.None)
                {
                    this.LogicalState = NodeLogicalState.Done;
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
            catch (OperationCanceledException ex)
            {
                runException = ex;
                if ((this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    return;
                }
                this.State = NodeState.Finished;
            }
            catch (ApplicationException ex)
            {
                runException = ex;
                if ((this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    return;
                }
                this._returnObject = runException;
                this.LogicalState = NodeLogicalState.Fault;
                this.State = NodeState.Finished;
            }
            catch (Exception ex)
            {
                runException = ex; // Achtung, keine direkte Referenz, sondern über new Exception entkoppeln.
                if ((this.CancellationToken.IsCancellationRequested)
                  || (this.LastLogicalState == NodeLogicalState.UserAbort))
                {
                    return;
                }
                this._returnObject = runException;
                this.LogicalState = NodeLogicalState.Fault;
                this.State = NodeState.Finished;
            }
            finally
            {
                if (!this.IsThreadValid(Thread.CurrentThread))
                {
                    if (this.Trigger != null)
                    {
                        this.State = NodeState.Triggered;
                    }
                    else
                    {
                        this.State = NodeState.Finished;
                    }
                    // DEBUG: InfoController.Say(String.Format("#T# {0}/{1} {2}: unmarking Thread as invalid", this.Id, this.Name,
                    //  System.Reflection.MethodBase.GetCurrentMethod().Name));
                    this.UnMarkThreadAsInvalid(Thread.CurrentThread);
                }
            }
            if (this.Trigger == null)
            {
                this.IsTaskActiveOrScheduled = false;
            }
            if (runException == null || !(runException is OperationCanceledException))
            {
                this.SetLastResult();
            }
            this.ThreadUpdateLastLogicalState(this.LogicalState);
            this.Logical = logical;
            /* 17.02.2023 Nagel+ auskommentiert
            if (runException != null && !(runException is OperationCanceledException))
            {
                this._returnObject = runException;
                this.OnExceptionRaised(this, runException);
            }
            17.02.2023 Nagel- */
            if (runException != null) // 17.02.2023 Nagel+
            {
                if (!(runException is OperationCanceledException))
                {
                    this._returnObject = runException;
                    this.OnExceptionRaised(this, runException);
                }
            }
            else
            {
                this.SetLastResult();
            } // 17.02.2023 Nagel-
            this.OnNodeProgressChanged(this.Id + "." + this.Name, 100, this._lastSucceeded);
            this.OnNodeProgressFinished(this.Id + "." + this.Name, 100, this._lastSucceeded);
        }

        /// <summary>
        /// Löst das NodeProgressFinished-Ereignis aus.
        /// </summary>
        /// <param name="itemsName">Name für die Elemente, die für den Verarbeitungsfortschritt gezählt werden.</param>
        /// <param name="countAll">Gesamtanzahl - entspricht 100%.</param>
        /// <param name="countSucceeded">Erreichte Anzahl - kleiner-gleich 100%.</param>
        public override void OnNodeProgressFinished(string itemsName, long countAll, long countSucceeded)
        {
            this._lastSucceeded = countSucceeded;
            base.OnNodeProgressFinished(itemsName, countAll, countSucceeded);
        }

        #endregion protected members

        #region private members

        /// <summary>
        /// Rückgabe-Objekt des letzten Checker-Durchlaufs, nicht öffentliche, interne Representation.
        /// </summary>
        protected object? _returnObject;

        private long _lastSucceeded;
        private object? _constantValue;
        private bool _returnConstantValue;
        private NodeCheckerBase? _checker;
        private ResultList _environment;
        private string _userControlPath;
        private Result? _lastResult;
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

        // Wird ausgelöst, wenn sich der Verarbeitungsfortschritt des Checkers geändert hat.
        private void checkerProgressChanged(object? sender, ProgressChangedEventArgs args)
        {
            this._lastSucceeded = args.ProgressPercentage;
            this.OnNodeProgressChanged(this.Id + "." + this.Name, 100, args.ProgressPercentage);
            //if (this.CancellationToken.IsCancellationRequested)
            //{
            //  this.CancellationToken.ThrowIfCancellationRequested();
            //}
        }

        #endregion private members

    }
}
