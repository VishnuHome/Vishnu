using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using NetEti.MVVMini;
using LogicalTaskTree;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using NetEti.Globals;
using Vishnu.Interchange;
using System.Collections.Generic;
using System.Text;
using NetEti.ApplicationControl;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Vishnu.ViewModel
{
    #region definitions

    /// <summary>
    /// Kombinierter Status aus State und LogicalState für die UI.
    /// </summary>
    public enum VisualNodeState
    {
        /// <summary>Startbereit, Zustand nach Initialisierung.</summary>
        None,
        /// <summary>Ist zwar nicht busy aber Timer-gesteuert.</summary>
        Scheduled,
        /// <summary>Beschäftigt, wartet auf Starterlaubnis.</summary>
        Waiting,
        /// <summary>Beschäftigt, arbeitet.</summary>
        Working,
        /// <summary>Mit Fehler beendet.</summary>
        Error,
        /// <summary>Durch Abbruch beendet.</summary>
        Aborted,
        /// <summary>Ohne Fehler, Timeout oder Abbruch beendet.</summary>
        Done,
        /// <summary>Ist zwar nicht busy aber Event-gesteuert.</summary>
        EventTriggered,
        /// <summary>Mit internem Fehler beendet (DEBUG).</summary>
        InternalError
    }

    /// <summary>
    /// Stellt die Enum aus der Business-Logic für die UI zur Verfügung.
    /// </summary>
    public enum VisualNodeWorkerState
    {
        /// <summary>Initialwert.</summary>
        None = NodeWorkerState.None,
        /// <summary>Mindestens ein NodeWorker ist invalide.</summary>
        Invalid = NodeWorkerState.Invalid,
        /// <summary>Alle NodeWorker sind valide.</summary>
        Valid = NodeWorkerState.Valid
    }

    #endregion definitions

    /// <summary>
    /// ViewModel für einen Knoten in der TreeView in LogicalTaskTreeControl
    /// </summary>
    /// <remarks>
    /// File: LogicalNodeViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 05.01.2013 Erik Nagel: erstellt
    /// </remarks>
    public class LogicalNodeViewModel : VishnuViewModelBase, IExpandableNode, IDisposable
    {
        #region public members

        #region IDisposable Implementation

        private bool _disposed = false;

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Abschlussarbeiten.
        /// </summary>
        /// <param name="disposing">False, wenn vom eigenen Destruktor aufgerufen.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Aufräumarbeiten durchführen und dann beenden.
                    if (this.Children?.Count > 0)
                    {
                        foreach (LogicalNodeViewModel logicalNodeViewModel in this.Children)
                        {
                            if (logicalNodeViewModel != null && logicalNodeViewModel is IDisposable)
                            {
                                try
                                {
                                    (logicalNodeViewModel as IDisposable).Dispose();
                                }
                                catch { }
                            }
                        }
                        // Der BusinessLogic-Knoten darf hier nur freigegeben aber nicht
                        // komplett ausgenullt werden.
                        // Derselbe BusinessLogic-Knoten kann von mehreren Generationen ViewModels
                        // referenziert werden!
                        try
                        {
                            // falsch, s.o. this.UnsetBLNode();
                            this.ReleaseBLNode();
                        }
                        catch { }
                    }
                }
                this._disposed = true;
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~LogicalNodeViewModel()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        #region published members

        #region commands

        /// <summary>
        /// Dient als erster Bindungsanker zur Attached Property ExpanderBehavior.ExpandedCommandProperty.
        /// </summary>
        public ICommand? ExpandedEventCommand
        { get; set; }

        /// <summary>
        /// Dient als erster Bindungsanker zur Attached Property ExpanderBehavior.CollapsedCommandProperty.
        /// </summary>
        public ICommand? CollapsedEventCommand
        { get; set; }

        /// <summary>
        /// Dient als erster Bindungsanker zur Attached Property ExpanderBehavior.CollapsedCommandProperty.
        /// </summary>
        public ICommand? SizeChangedEventCommand
        { get; set; }

        /// <summary>
        /// Command für den Copy-Button im ToolTip des Controls.
        /// </summary>
        public ICommand CopyToolTipInfoToClipboard { get { return this._btnCopyToolTipInfoToClipboardCommand; } }

        #endregion commands

        #region ViewModel properties

        /// <summary>
        /// Die Kinder des aktuellen Knotens.
        /// </summary>
        public ObservableCollection<LogicalNodeViewModel> Children { get; internal set; }

        /// <summary>
        /// Freitext für beliebige Anwendungen.
        /// </summary>
        public string FreeComment
        {
            get
            {
                return this._freeComment;
            }
            set
            {
                if (this._freeComment != value)
                {
                    this._freeComment = value;
                    this.RaisePropertyChanged("FreeComment");
                }
            }
        }

        /// <summary>
        /// Nur beim Rootnode False.
        /// </summary>
        public bool HasParent
        {
            get
            {
                return this._parent != null;
            }
            private set
            {
                this.RaisePropertyChanged("HasParent");
            }
        }

        /// <summary>
        /// Returns true, wenn gerade eine vom User definierte Ruhezeit
        /// für Vishnu-Akteure (Checker) läuft.
        /// </summary>
        /// <returns>True, wenn gerade eine vom User definierte Ruhezeit
        /// für Vishnu-Akteure (Checker) läuft.</returns>
        public bool IsInSleepTime
        {
            get
            {
                return this._isInSleepTime;
            }
            set
            {
                if (this._isInSleepTime != value)
                {
                    this._isInSleepTime = value;
                    this.RaisePropertyChanged("IsInSleepTime");
                }
            }
        }

        /// <summary>
        /// Returns true, wenn der Tree gerade pausiert wurde.
        /// </summary>
        /// <returns>True, wenn der Tree gerade pausiert wurde.</returns>
        public bool IsTreePaused
        {
            get
            {
                return this._isTreePaused;
            }
            set
            {
                if (this._isTreePaused != value)
                {
                    this._isTreePaused = value;
                    this.RaisePropertyChanged("IsTreePaused");
                }
            }
        }

        //private static void ProcessUITasks()
        //{
        //    DispatcherFrame frame = new DispatcherFrame();
        //    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate (object parameter) {
        //        frame.Continue = false;
        //        return null;
        //    }), null);
        //    Dispatcher.PushFrame(frame);
        //}
        
        /// <summary>
        /// Das Ende einer möglichen Ruhezeit als formatierter String.
        /// </summary>
        public string? SleepTimeTo
        {
            get
            {
                return this._sleepTimeTo;
            }
            set
            {
                if (this._sleepTimeTo != value)
                {
                    this._sleepTimeTo = value;
                    this.RaisePropertyChanged("SleepTimeTo");
                }
            }
        }

        /// <summary>
        /// Der übergeordnete Knoten im ViewModel.
        /// </summary>
        public LogicalNodeViewModel? Parent
        {
            get
            {
                return this._parent;
            }
            set
            {
                this._parent = value;
                this.RaisePropertyChanged("Parent");
                this.RaisePropertyChanged("HasParent");
            }
        }

        /// <summary>
        /// True, wenn der TreeView-Knoten, welcher mit diesem Knoten
        /// assoziiert ist, ausgeklappt ist.
        /// </summary>
        public bool IsExpanded
        {
            get { return this._isExpanded; }
            set
            {
                if (value != this._isExpanded)
                {
                    this._isExpanded = value;
                    if (this._isExpanded && this._hasDummyChild)
                    {
                        // Kinder nachladen, wenn nötig.
                        this.Children.Remove(_dummyChild);
                        this.LoadChildren();
                    }
                    this.RaisePropertyChanged("IsExpanded");
                }
            }
        }

        /// <summary>
        /// Definiert, ob die Kind-Elemente dieses Knotens
        /// horizontal oder vertikal angeordnet werden sollen.
        /// </summary>
        public Orientation ChildOrientation
        {
            get
            {
                return this._childOrientation;
            }
            set
            {
                if (this._childOrientation != value)
                {
                    this._childOrientation = value;
                    this.RaisePropertyChanged("ChildOrientation");
                }
            }
        }

        /// <summary>
        /// True, wenn der TreeView-Knoten, welcher mit diesem Knoten
        /// assoziiert ist, ausgewählt ist.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.RaisePropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Der eindeutige Pfad des zugehörigen LogicalTaskTree-Knotens.
        /// </summary>
        public string Path
        {
            get
            {
                if (this._parent != null && this._parent != this)
                {
                    return this._parent.Path + "/" + this.Id;
                }
                else
                {
                    return this.Id;
                }
            }
        }

        /// <summary>
        /// Gibt an, ob das Element visible, hidden oder collapsed ist.
        /// </summary>
        public Visibility Visibility
        {
            get
            {
                return this._visibility;
            }
            set
            {
                if (this._visibility != value)
                {
                    this._visibility = value;
                    this.RaisePropertyChanged("Visibility");
                }
            }
        }

        /// <summary>
        /// Reicht einen u.U. aus mehreren technischen Quellen kombinierten
        /// Zustand als Aufzählungstyp an die GUI (und den IValueConverter) weiter.
        /// Default: None
        /// </summary>
        public VisualNodeState VisualState
        {
            get
            {
                return this._visualState;
            }
            set
            {
                this._visualState = value;
                this.RaisePropertyChanged("VisualState");
            }
        }

        /// <summary>
        /// Zusätzliche Parameter, die für den gesamten Tree Gültigkeit haben oder null.
        /// </summary>
        public TreeParameters TreeParams
        {
            get
            {
                return this._treeParams;
            }
            set
            {
                if (this._treeParams != value)
                {
                    this._treeParams = value;
                    this.RaisePropertyChanged("TreeParams");
                }
            }
        }

        /// <summary>
        /// Enthält TreeParams.Name und IdInfo des Knotens, in dessen Events sich dieses
        /// LogicalNodeViewModel eingehängt hat oder den Text "NULL".
        /// </summary>
        public string? HookedTo
        {
            get
            {
                return this._hookedTo;
            }
            private set
            {
                if (this._hookedTo != value)
                {
                    this._hookedTo = value;
                    this.RaisePropertyChanged("HookedTo");
                }
            }
        }

        #endregion ViewModel properties

        #region Node properties

        /// <summary>
        /// Die Kennung des zugehörigen LogicalTaskTree-Knotens
        /// für die UI verfügbar gemacht.
        /// </summary>
        public string Id
        {
            get
            {
                return this._myLogicalNode?.Id ?? "NULL";
            }
        }

        /// <summary>
        /// True zeigt an, dass es sich um einen Knoten innerhalb
        /// eines geladenen Snapshots handelt.
        /// </summary>
        public bool IsSnapshotDummy
        {
            get
            {
                return this._myLogicalNode?.IsSnapshotDummy ?? false;
            }
        }

        /// <summary>
        /// Das logische Ergebnis des zugehörigen LogicalTaskTree-Knotens
        /// für die UI verfügbar gemacht.
        /// </summary>
        public bool? Logical
        {
            get
            {
                return this._myLogicalNode.LastNotNullLogical;
            }
        }

        /// <summary>
        /// Merkfeld für den letzten Zustand von Logical, der nicht null war;
        /// Wird benötigt, damit Worker nur dann gestartet werden, und die 
        /// Anzeige wechselt, wenn sich der Zustand von Logical signifikant
        /// geändert hat und nicht jedesmal, wenn der Checker arbeitet (Logical = null).
        /// </summary>
        public bool? LastNotNullLogical
        {
            get
            {
                return this._myLogicalNode.LastNotNullLogical;
            }
        }

        /// <summary>
        /// Der Name des zugehörigen LogicalTaskTree-Knotens
        /// für die UI verfügbar gemacht.
        /// </summary>
        public string Name
        {
            get
            {
                return this._myLogicalNode.Name;
            }
        }

        /// <summary>
        /// Kombinierte Ausgabe von NextRunInfo (wann ist der nächste Durchlauf)
        /// und Result (in voller Länge).
        /// </summary>
        public string NextRunInfoAndResult
        {
            get
            {
                return String.Format("nächster Lauf: {0}\nletztes Ergebnis: {1}", this.NextRunInfo, (this.Result == null ? "" : this.Result.ToString()));
            }
        }

        /// <summary>
        /// Liefert das Ergebnis von GetToolTipInfo().
        /// Diese Routine zeigt per Default auf NextRunInfoAndResult,
        /// kann aber gegebenenfalls überschrieben werden.
        /// </summary>
        public string ToolTipInfo
        {
            get
            {
                return this.GetToolTipInfo();
            }
        }

        /// <summary>
        /// Der Pfad zum aktuell dynamisch zu ladenden UserControl.
        /// </summary>
        public string UserControlPath
        {
            get
            {
                return this._myLogicalNode?.UserControlPath ?? "";
            }
        }

        /// <summary>
        /// Kombinierter NodeWorkerState für alle zugeordneten NodeWorker.
        /// </summary>
        public VisualNodeWorkerState WorkersState
        {
            get
            {
                return (VisualNodeWorkerState)this._myLogicalNode.WorkersState;
            }
        }

        /// <summary>
        /// Die Anzahl der Endknoten dieses Teilbaums
        /// für die UI verfügbar gemacht.
        /// </summary>
        public int SingleNodes
        {
            get
            {
                return this._singleNodes;
            }
            set
            {
                if (this._singleNodes != value)
                {
                    this._singleNodes = value;
                    this.RaisePropertyChanged("SingleNodes");
                    this.RaisePropertyChanged("Progress");
                    this.RaisePropertyChanged("ProgressText");
                }
            }
        }

        /// <summary>
        /// Ein Text für die Anzahl der beendeten Endknoten dieses Teilbaums
        /// zur Anzeige im ProgressBar (i.d.R. nnn%) für die UI verfügbar gemacht.
        /// </summary>
        public int Progress
        {
            get
            {
                if (!this.IsSnapshotDummy)
                {
                    return (this.SingleNodesFinished * 100 / (this.SingleNodes < 1 ? 1 : this.SingleNodes));
                }
                else
                {
                    return 100;
                }
            }
        }

        /// <summary>
        /// Ein Text für die Anzahl der beendeten Endknoten dieses Teilbaums
        /// zur Anzeige im ProgressBar (i.d.R. nnn%) für die UI verfügbar gemacht.
        /// </summary>
        public string ProgressText
        {
            get
            {
                if (!this.IsSnapshotDummy)
                {
                    return (this.Progress).ToString() + " %";
                }
                else
                {
                    return "Snapshot";
                }
            }
        }

        /// <summary>
        /// Die Anzahl der beendeten Endknoten dieses Teilbaums
        /// für die UI verfügbar gemacht.
        /// </summary>
        public int SingleNodesFinished
        {
            get
            {
                if (!this.IsSnapshotDummy)
                {
                    return this._myLogicalNode.SingleNodesFinished;
                }
                else
                {
                    return 100; // TODO (Verhalten bei Exceptions checken)
                }
            }
        }

        /// <summary>
        /// Das ReturnObject der zugeordneten LogicalNode.
        /// </summary>
        public override Result? Result
        {
            get
            {
                return this._myLogicalNode.LastResult;
            }
            set { }
        }

        /// <summary>
        /// Kurztext für Exceptions der zugeordneten LogicalNode.
        /// </summary>
        public string ShortResult
        {
            get
            {
                Result? res = this._myLogicalNode.LastResult;
                object? rtn = res?.ReturnObject;
                if (rtn != null)
                {
                    string msg = "---";
                    if (rtn is Exception)
                    {
                        msg = ((Exception)rtn).Message;
                    }
                    else
                    {
                        msg = rtn.ToString() ?? "";
                    }
                    if (msg.Length > 36)
                    {
                        msg = msg.Substring(0, 33) + "...";
                    }
                    return msg;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Liste von ReturnObjekten der zugeordneten LogicalNode und ihrer Kinder.
        /// </summary>
        public ObservableCollection<Result?> Results
        {
            get
            {
                Dictionary<string, Result?> tmp = this._myLogicalNode.GetResults();
                return new ObservableCollection<Result?>(tmp.Values);
            }
        }

        /// <summary>
        /// Liste mit allen Result-Objekten der Vorgänger des Teilbaums.
        /// </summary>
        public ObservableCollection<Result?> NodeEnvironment
        {
            get
            {
                Dictionary<string, Result?> tmp = this._myLogicalNode.GetEnvironment();
                return new ObservableCollection<Result?>(tmp.Values);
            }
        }

        /// <summary>
        /// Das letzte auslösende TreeEvent (bei TreeEvent-getriggerten Knoten)
        /// oder null.
        /// </summary>
        public TreeEvent? LastExecutingTreeEvent
        {
            get
            {
                return this._myLogicalNode.LastExecutingTreeEvent;
            }
        }

        /// <summary>
        /// Listet in einem String mögliche Exceptions der Child-Knoten
        /// durch Zeilenumbruch getrennt auf.
        /// </summary>
        public string LastExceptions
        {
            get
            {
                StringBuilder info = new StringBuilder("");
                string delimiter = "";
                foreach (LogicalNode node in this._myLogicalNode.LastExceptions.Keys)
                {
                    try
                    {
                        info.Append(delimiter + node.Id + ": " + this._myLogicalNode.LastExceptions[node].Message); // DEBUG ERROR
                    }
                    catch { }
                    delimiter = Environment.NewLine;
                }
                return info.ToString();
            }
        }

        /// <summary>
        /// Zeitpunkt des letzten Starts des Knoten.
        /// </summary>
        public DateTime LastRun
        {
            get
            {
                return this._myLogicalNode.LastRun;
            }
        }

        /// <summary>
        /// Zeitpunkt des letzten Starts des Knoten als String.
        /// </summary>
        public string LastRunInfo
        {
            get
            {
                return this._myLogicalNode.LastRun == DateTime.MinValue ? "---" : this._myLogicalNode.LastRun.ToString();
            }
        }

        /// <summary>
        /// Zeitpunkt des nächsten Starts des Knotens (wenn bekannt) oder DateTime.MinValue.
        /// </summary>
        public DateTime NextRun
        {
            get
            {
                return this._myLogicalNode.NextRun;
            }
        }

        /// <summary>
        /// Info-Text über den nächsten Start des Knotens (wenn bekannt) oder null.
        /// </summary>
        public string NextRunInfo
        {
            get
            {
                return this._myLogicalNode.NextRunInfo ?? "---";
            }
        }

        /// <summary>
        /// Liefert die für den Knoten gültige Root-JobList.
        /// </summary>
        /// <returns>Die für den Knoten gültige Root-JobList.</returns>
        public JobListViewModel? RootJobListViewModel
        {
            get
            {
                if (this.Parent is JobListViewModel)
                {
                    return (JobListViewModel)Parent;
                }
                else
                {
                    // return Parent == null ? null : Parent.RootJobListViewModel;
                    return Parent?.RootJobListViewModel;
                }
            }
        }

        /// <summary>
        /// Id der ursprünglich referenzierten SingleNode.
        /// </summary>
        public string? OriginalNodeId
        {
            get
            {
                if (this._myLogicalNode is NodeConnector)
                {
                    return ((NodeConnector)this._myLogicalNode).ReferencedNodeId;
                }
                else
                {
                    return this._myLogicalNode?.Id ?? "NULL";
                }
            }
        }

        /// <summary>
        /// Name + (Id + gegebenenfalls ReferencedNodeId) der ursprünglich referenzierten SingleNode.
        /// </summary>
        public string DebugNodeInfos
        {
            get
            {
                string debugNodeInfos = this.Name + " (" + this.Id;
                if (this._myLogicalNode is NodeConnector)
                {
                    debugNodeInfos += ", Ref: " + this.OriginalNodeId;
                }
                debugNodeInfos += ") " + this.TreeParams.Name;
                debugNodeInfos += ", " + this.VisualTreeCacheBreaker.Substring(this.VisualTreeCacheBreaker.Length - 8);
                return debugNodeInfos;
            }
        }

        /// <summary>
        /// Bei True können zusätzliche Testausgaben erfolgen.
        /// Default: False.
        /// </summary>
        public bool DebugMode
        {
            get
            {
                return this._myLogicalNode.DebugMode;
            }
        }

        #endregion Node properties

        /// <summary>
        /// Ausrichtung des Trees beim Start der Anwendung.
        ///   AlternatingHorizontal: Alternierender Aufbau, waagerecht beginnend (Default).
        ///   Vertical: Senkrechter Aufbau.
        ///   Horizontal: Waagerechter Aufbau.
        ///   AlternatingVertical: Alternierender Aufbau, senkrecht beginnend.
        /// </summary>
        public TreeOrientation StartTreeOrientation { get; set; }

        #endregion published members

        /// <summary>
        /// ViewModel des übergeordneten LogicalTaskTree.
        /// </summary>
        public OrientedTreeViewModelBase RootLogicalTaskTreeViewModel { get; set; }

        /// <summary>
        /// Liefert das Ergebnis für die Property ToolTipInfo.
        /// Diese Routine zeigt per Default auf NextRunInfoAndResult,
        /// kann aber gegebenenfalls überschrieben werden.
        /// </summary>
        protected virtual string GetToolTipInfo()
        {
            return this.NextRunInfoAndResult;
        }

        /// <summary>
        /// Liefert die für den Knoten gültige, oberste Root-JobList.
        /// </summary>
        /// <returns>Die für den Knoten gültige, oberste Root-JobList.</returns>
        public JobListViewModel? GetTopRootJobListViewModel()
        {
            if (this.RootJobListViewModel != null && this.RootJobListViewModel.HasParent)
            {
                return this.RootJobListViewModel.GetTopRootJobListViewModel();
            }
            else
            {
                return this.RootJobListViewModel;
            }
        }

        /// <summary>
        /// Liefert einen string für Debug-Zwecke.
        /// </summary>
        /// <returns>Ein String für Debug-Zwecke.</returns>
        public override string GetDebugNodeInfos()
        {
            return this.DebugNodeInfos;
        }

        #region context menu

        /// <summary>
        /// Indicates that a ui-trigerred background progress is actually running.
        /// </summary>
        public string? JobInProgress
        {
            get
            {
                return this._jobInProgress;
            }
            private set
            {
                if (this._jobInProgress != value)
                {
                    this._jobInProgress = value;
                    this.RaisePropertyChanged("JobInProgress");
                }
            }
        }

        /// <summary>
        /// Indicates that a ui-trigerred background progress is actually running.
        /// </summary>
        public bool ContextMenuCanOpen
        {
            get
            {
                return this._contextMenuCanOpen;
            }
            private set
            {
                if (this._contextMenuCanOpen != value)
                {
                    this._contextMenuCanOpen = value;
                    this.RaisePropertyChanged("ContextMenuCanOpen");
                }
            }
        }

        /// <summary>
        /// Liefert true, wenn die Funktion ausführbar ist.
        /// </summary>
        /// <returns>True, wenn die Funktion ausführbar ist.</returns>
        public override bool CanReloadTaskTreeExecute()
        {
            return true; // !(this._myLogicalNode is NodeConnector); // && this._myLogicalNode.CanTreeStart;
        }

        /// <summary>
        /// Lädt den Tree nach Änderung der JobDescriptions neu.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter, wird hier nicht genutzt.</param>
        public override void ReloadTaskTreeExecute(object? parameter)
        {
            _ = this.ReloadTaskTree();
        }

        /// <summary>
        /// Lädt den gesamten Tree inklusive JobDescription.xml asynchron neu. 
        /// </summary>
        /// <returns>Task.</returns>
        public async Task ReloadTaskTree()
        {
            this.JobInProgress = "ReloadTaskTree";
            await Task.Run(() => ReloadTaskTreeAsync());
            this.JobInProgress = "";
            _ = this.ResetContextMenu();
        }

        /// <summary>
        /// Liefert true, wenn die Funktion ausführbar ist.
        /// </summary>
        /// <returns>True, wenn die Funktion ausführbar ist.</returns>
        public override bool CanLogTaskTreeExecute()
        {
            bool canLog = true;
            return canLog;
        }

        /// <summary>
        /// Loggt den Tree.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter, wird hier nicht genutzt.</param>
        public override void LogTaskTreeExecute(object? parameter)
        {
            _ = this.LogTaskTree();
        }

        /// <summary>
        /// Lädt den gesamten Tree inklusive JobDescription.xml asynchron neu. 
        /// </summary>
        /// <returns>Task.</returns>
        public async Task LogTaskTree()
        {
            this.JobInProgress = "LogTaskTree";
            await Task.Run(() => LogicalTaskTreeManager.LogTaskTree(this, false));
            this.JobInProgress = "";
            _ = this.ResetContextMenu();
        }

        #endregion context menu

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="logicalTaskTreeViewModel">ViewModel des übergeordneten LogicalTaskTree.</param>
        /// <param name="parent">Der übergeordnete ViewModel-Knoten.</param>
        /// <param name="myLogicalNode">Der zugeordnete Knoten aus dem LogicalTaskTree.</param>
        /// <param name="lazyLoadChildren">Bei True werden die Kinder erst beim Öffnen des TreeView-Knotens nachgeladen.</param>
        /// <param name="uIMain">Das Root-FrameworkElement zu diesem ViewModel.</param>
        public LogicalNodeViewModel(OrientedTreeViewModelBase logicalTaskTreeViewModel, LogicalNodeViewModel? parent,
            LogicalNode myLogicalNode, bool lazyLoadChildren, FrameworkElement uIMain) : base()
        {
            this.RootLogicalTaskTreeViewModel = logicalTaskTreeViewModel;
            this._treeParams = logicalTaskTreeViewModel.TreeParams;
            this.UIMain = uIMain;
            this.UIDispatcher = uIMain.Dispatcher ?? Dispatcher.CurrentDispatcher;
            this.Parent = parent;
            this._lazyLoadChildren = lazyLoadChildren;
            this._canExpanderExpand = true;
            this.Invalidate();

            this._hookedTo = "NULL";
            this.TreeRefreshLocker = new object();
            this.SubStateLocker = new object();
            this.IsRefreshing = false;
            this.JobInProgress = "";
            this._myLogicalNode = UndefinedLogicalNodeClass.UndefinedLogicalNode;
            this.SetBLNode(myLogicalNode, true);
            this.Visibility = System.Windows.Visibility.Visible;
            this.VisualState = VisualNodeState.None;

            Children = new ObservableCollection<LogicalNodeViewModel>();

            if (lazyLoadChildren)
            {
                Children.Add(_dummyChild);
            }
            else
            {
                this.LoadChildren();
            }
            this.ExpandedEventCommand = new RelayCommand(this.HandleExpanderExpandedEvent, this.CanHandleExpanderExpandedEvent);
            this.CollapsedEventCommand = new RelayCommand(this.HandleExpanderCollapsedEvent);
            this.SizeChangedEventCommand = new RelayCommand(this.HandleExpanderSizeChangedEvent);
            this._btnCopyToolTipInfoToClipboardCommand = new RelayCommand(this.CmdBtnCopy_Executed, this.CmdBtnCopy_CanExecute);
            this.RaisePropertyChanged("RootJobListViewModel");
            this._freeComment = "";
        }

        /// <summary>
        /// Setzt die Ausrichtung der Kind-Knoten.
        /// </summary>
        /// <param name="mainTreeOrientation">Ausrichtug des Trees</param>
        public void SetChildOrientation(TreeOrientation mainTreeOrientation)
        {
            this.StartTreeOrientation = mainTreeOrientation;
            switch (mainTreeOrientation)
            {
                case TreeOrientation.Vertical:
                    this.ChildOrientation = Orientation.Vertical; break;
                case TreeOrientation.Horizontal:
                    this.ChildOrientation = Orientation.Horizontal; break;
                case TreeOrientation.AlternatingVertical:
                    if (this._myLogicalNode.Level % 2 != 0)
                    {
                        this.ChildOrientation = Orientation.Horizontal;
                    }
                    else
                    {
                        this.ChildOrientation = Orientation.Vertical;
                    }
                    break;
                case TreeOrientation.AlternatingHorizontal:
                    if (this._myLogicalNode.Level % 2 == 0)
                    {
                        this.ChildOrientation = Orientation.Horizontal;
                    }
                    else
                    {
                        this.ChildOrientation = Orientation.Vertical;
                    }
                    break;
                default:
                    break;
            }
            if (this.Children != null)
            {
                foreach (LogicalNodeViewModel child in this.Children)
                {
                    child.SetChildOrientation(mainTreeOrientation);
                }
            }
        }

        /// <summary>
        /// Event-Handler für das Öffnen des Expanders.
        /// </summary>
        /// <param name="parameter">Ein object-Parameter (kann null sein).</param>
        public void HandleExpanderExpandedEvent(object? parameter)
        {
            if (LogicalNodeViewModel._treeEventSemaphore == 0)
            {
                Interlocked.Increment(ref LogicalNodeViewModel._treeEventSemaphore);
                this._canExpanderExpand = false;
                try
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                    {
                        this.ExpandTree(this, false);
                    }
                    else
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                        {
                            this.ExpandTree(this, true);
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref LogicalNodeViewModel._treeEventSemaphore);
                    if (!this._isExpanded)
                    {
                        this.IsExpanded = true;
                    }
                    this._canExpanderExpand = true;
                }
            }
        }

        /// <summary>
        /// Ausführungserlaubnis für den Event-Handler für das Öffnen des Expanders.
        /// </summary>
        /// <returns>True, wenn die Ausführung erlaubt ist.</returns>
        public bool CanHandleExpanderExpandedEvent()
        {
            return this._canExpanderExpand;
        }

        /// <summary>
        /// Event-Handler für das Schließen des Expanders.
        /// </summary>
        /// <param name="parameter">Ein object-Parameter (kann null sein).</param>
        public void HandleExpanderCollapsedEvent(object? parameter)
        {
            if (LogicalNodeViewModel._treeEventSemaphore == 0)
            {
                try
                {
                    Interlocked.Increment(ref LogicalNodeViewModel._treeEventSemaphore);
                    if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                    {
                        this.CollapseTree(false);
                    }
                    else
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                        {
                            this.CollapseTree(true);
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref LogicalNodeViewModel._treeEventSemaphore);
                    this.IsExpanded = false;
                }
            }
        }

        /// <summary>
        /// Event-Handler für Größenänderung des Expanders.
        /// </summary>
        /// <param name="parameter">Ein object-Parameter (kann null sein).</param>
        public void HandleExpanderSizeChangedEvent(object? parameter)
        {
            //this.RefreshResult(); // Führt dazu, dass in ListBox nicht gescrollt werden kann.
        }

        /// <summary>
        /// Klappt den ViewModel-Tree komplett aus und damit auch die TreeView (wegen der assoziierten Knoten).
        /// </summary>
        /// <param name="root">Knoten, ab dem der Tree ausgeklappt werden soll.</param>
        /// <param name="climbToTop">Bei True wird der gesamte Zweig bis aufwärts zur Root ausgeklappt.</param>
        public void ExpandTree(LogicalNodeViewModel? root, bool climbToTop)
        {
            LogicalNodeViewModel actVM = root ?? this;
            if (climbToTop && actVM.Parent != null)
            {
                // Ausklappen von oben starten.
                actVM.Parent.ExpandTree(actVM.Parent, true);
            }
            else
            {
                bool destroyTempIds = false;
                if (actVM.RootLogicalTaskTreeViewModel.TempIds == null)
                {
                    actVM.RootLogicalTaskTreeViewModel.TempIds = new List<string>();
                    destroyTempIds = true;
                }
                if (!actVM.RootLogicalTaskTreeViewModel.TempIds.Contains(actVM.Path))
                {
                    actVM.RootLogicalTaskTreeViewModel.TempIds.Add(actVM.Path);
                    if (!actVM._myLogicalNode.StartCollapsed)
                    {
                        if (!actVM._isExpanded)
                        {
                            actVM.IsExpanded = true;
                        }
                        foreach (LogicalNodeViewModel child in actVM.Children)
                        {
                            child.ExpandTree(null, false);
                        }
                    }
                    else
                    {
                        actVM._myLogicalNode.SetTreeCollapsed(false);
                    }
                }
                if (destroyTempIds)
                {
                    actVM.RootLogicalTaskTreeViewModel.TempIds = null;
                }
            }
        }

        /// <summary>
        /// Klappt den ViewModel-Zweig ein und damit auch die TreeView (wegen der assoziierten Knoten).
        /// </summary>
        /// <param name="climbToTop">Bei True wird der gesamte Zweig bis aufwärts zur Root eingeklappt.</param>
        public void CollapseTree(bool climbToTop)
        {
            if (climbToTop && this.Parent != null)
            {
                // Ausklappen von oben starten.
                this.Parent.CollapseTree(true);
            }
            else
            {
                foreach (LogicalNodeViewModel child in this.Children)
                {
                    child.CollapseTree(false);
                }
                this.IsExpanded = false;
            }
        }

        /// <summary>
        /// Gibt die zugeordnete logicalNode aus der BusinessLogic zurück.
        /// </summary>
        /// <returns>die zugeordnete logicalNode aus der BusinessLogic</returns>
        public LogicalNode GetLogicalNode()
        {
            return this._myLogicalNode;
        }

        ///// <summary>
        ///// Löst NotifyPropertyChanged auf  auf die Property "Result" aus.
        ///// </summary>
        //public void RefreshResult()
        //{
        //  //this.RaisePropertyChanged("Result"); // Der direkte Aufruf funktioniert nicht immer, Grund:
        //  // wenn mehrere Knoten gleichzeitig das erste mal expandiert werden (z.B. Strg-Maus auf einem Collapsed-Job),
        //  // dann hängt sich Windows auch erst in die PropertyChangedEvents der gerade geöffneten Unterknoten ein.
        //  // Bei einigen Knoten ist dann der direkte Aufruf von this.RaisePropertyChanged("Result") schneller als
        //  // Windows, was dazu führt, dass this.RaisePropertyChanged("Result") ohne Wirkung verpufft.
        //  // Der asynchrone Aufruf mit der niedrigen DispatcherPriority.Loaded behebt dieses Problem.
        //  Dispatcher.BeginInvoke(new Action(() => { this.RaisePropertyChanged("Result"); }), DispatcherPriority.Loaded);
        //}

        /// <summary>
        /// Geht rekursiv durch den Baum und ruft für jeden Knoten die Action auf.
        /// </summary>
        /// <param name="callback">Der für jeden Knoten aufzurufende Callback vom Typ Func&lt;int, IExpandableNode, object, object&gt;.</param>
        /// <returns>Das oberste UserObjekt für den Tree.</returns>
        public object? Traverse(Func<int, IExpandableNode, object?, object?> callback)
        {
            return this.traverse(0, callback, null);
        }

        /// <summary>
        /// Geht rekursiv durch den Baum und ruft für jeden Knoten die Action auf.
        /// </summary>
        /// <param name="callback">Der für jeden Knoten aufzurufende Callback vom Typ Func&lt;int, IExpandableNode, object, object&gt;.</param>
        /// <param name="userObject">Ein User-Object, das rekursiv weitergeleitet und modifiziert wird.</param>
        /// <returns>Das oberste UserObjekt für den Tree.</returns>
        public object? Traverse(Func<int, IExpandableNode, object?, object?> callback, object? userObject)
        {
            return this.traverse(0, callback, userObject);
        }

        /// <summary>
        /// Rekursive Hilfsroutine für die öffentliche Routine 'Traverse'.
        /// </summary>
        /// <param name="depth">Die Hierarchie-Ebene.</param>
        /// <param name="callback">Der für jeden Knoten aufzurufende Callback vom Typ Func&lt;int, IExpandableNode, object, object&gt;.</param>
        /// <param name="userObject">Ein User-Object, das rekursiv weitergeleitet und modifiziert wird.</param>
        /// <returns>Das oberste UserObjekt für den Tree.</returns>
        protected virtual object? traverse(int depth, Func<int, IExpandableNode, object?, object?> callback, object? userObject)
        {
            object? nextUserParent = callback(depth, (IExpandableNode)this, userObject);
            if (nextUserParent != null)
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    (this.Children[i] as LogicalNodeViewModel).traverse(depth + 1, callback, nextUserParent);
                }
            }
            return nextUserParent;
        }

        /// <summary>
        /// Überschriebene ToString()-Methode.
        /// </summary>
        /// <returns>Verkettete Properties als String.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(String.Format($"{this.GetType().Name}: {this.Id ?? "" + "." + this.Name ?? ""}"));
            LogicalNode logicalNode = this.GetLogicalNode();
            if (logicalNode != null)
            {
                stringBuilder.AppendLine(String.Format($"    {logicalNode.NodeType}: {logicalNode?.NameId ?? ""}"));
            }
            stringBuilder.AppendLine(String.Format($"    Source: {this.OriginalNodeId ?? ""}"));
            if (this.Children?.Count > 0)
            {
                stringBuilder.AppendLine(String.Format($"    Children: {this.Children.Count}"));
            }
            if (this.IsSnapshotDummy)
            {
                stringBuilder.AppendLine(String.Format($"    IsSnapshotDummy"));
            }
            stringBuilder.AppendLine(String.Format($"    Path: {this.Path ?? ""}"));
            stringBuilder.AppendLine(String.Format($"    UserControlPath: {this.UserControlPath}"));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Vergleicht den Inhalt dieses LogicalNodeViewModels nach logischen Gesichtspunkten
        /// mit dem Inhalt eines übergebenen LogicalNodeViewModels.
        /// </summary>
        /// <param name="obj">Das LogicalNodeViewModel zum Vergleich.</param>
        /// <returns>True, wenn das übergebene LogicalNodeViewModel inhaltlich gleich diesem ist.</returns>
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
            if (String.IsNullOrEmpty(this.Path) || String.IsNullOrEmpty(this.OriginalNodeId))
            {
                return false;
            }
            if (this.ToString() != obj.ToString())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Erzeugt einen Hashcode für dieses LogicalNodeViewModel.
        /// </summary>
        /// <returns>Integer mit Hashwert.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion public members

        #region internal members

        /// <summary>
        /// Dient zum Serren der Verarbeitung während eines laufenden Tree-Refreshs.
        /// </summary>
        internal object TreeRefreshLocker;

        /// <summary>
        /// Dient zum Serren der Verarbeitung während subStateChanged.
        /// </summary>
        internal object SubStateLocker;

        /// <summary>
        /// Zeigt an, ob gerade ein Tree-Refresh läuft.
        /// </summary>
        internal volatile bool IsRefreshing;

        /// <summary>
        /// Refresht den Visual-Tree nach einer Veränderung in den ViewModels.
        /// Nur für inhaltliche Änderungen bei unveränderter Struktur geeignet,
        /// dafür aber sehr schnell.
        /// </summary>
        /// <returns>False, wenn irgendwas schiefgegangen ist, ansonsten true.</returns>
        internal bool LeanTreeRefresh()
        {
            // return false; // TEST 20.08.2020 Nagel
            bool rtn = true;
            lock (this.TreeRefreshLocker)
            {
                this.IsRefreshing = true;
                using (DispatcherProcessingDisabled d = this.Dispatcher.DisableProcessing())
                {
                    if (!this.RefreshTreeView(this, this._myLogicalNode))
                    {
                        rtn = false;
                    }
                }
                this.IsRefreshing = false;
            }
            return rtn;
        }

        /// <summary>
        /// Refresht den Visual-Tree nach einer Veränderung in den ViewModels.
        /// Für inhaltliche und strukturelle Änderungen im Tree geeignet,
        /// dafür aber etwas langsam (im Dialog aber schnell genug).
        /// </summary>
        internal void FullTreeRefresh()
        {
            using (DispatcherProcessingDisabled d = this.Dispatcher.DisableProcessing())
            {
                lock (this.TreeRefreshLocker)
                {
                    ObservableCollection<LogicalNodeViewModel> shadowTree = new ObservableCollection<LogicalNodeViewModel>();
                    if (this.Children.Count > 0)
                    {
                        shadowTree.Add(this.Children[0]); // Retten für die Restaurierung aktueller Tree-Parameter wie zum Beispiel
                    }
                    // IsExpanded nach Neuerstellung des Trees aus dem Snapshot. 
                    this.Children.Clear();
                    //          this.SetChildOrientation(this.RootLogicalTaskTreeViewModel.TreeOrientationState);
                    this.LoadChildren();
                    this.TransferShadowTreeProperties(shadowTree, this.Children, new Stack<int>());
                    if (shadowTree.Count > 0)
                    {
                        shadowTree[0].Dispose();
                    }
                    shadowTree.Clear();
                    this.IsRefreshing = false;
                }
            }
        }

        /// <summary>
        /// Setzt die Business-Logic Node für dieses LogicalNodeViewModel.
        /// </summary>
        /// <param name="node">Die zuzuordnende Node aus der Business-Logic.</param>
        /// <param name="init">Bei false bricht SetBlNode mit false ab, wenn _myLogicalNode == null ist.</param>
        internal bool SetBLNode(LogicalNode node, bool init)
        {
            if (!(this._myLogicalNode is IUndefinedElement) && this._myLogicalNode.GetType() != node.GetType())
            {
                return false;
            }
            if (!init && (this._myLogicalNode is IUndefinedElement))
            {
                return false;
            }
            this.UnsetBLNode();

            LogicalNode.AllNodesStateChanged += this.LogicalNode_AllNodesStateChanged;

            this._myLogicalNode = node;
            this._myLogicalNode.PropertiesChanged += subPropertiesChanged;
            this._myLogicalNode.NodeLogicalChanged += this.subLogicalChanged;
            this._myLogicalNode.NodeLastNotNullLogicalChanged += subLastNotNullLogicalChanged;
            this._myLogicalNode.NodeStateChanged += this.subStateChanged;
            this._myLogicalNode.NodeProgressStarted += this.SubNodeProgressStarted;
            this._myLogicalNode.NodeProgressChanged += this.subNodeProgressChanged;
            this._myLogicalNode.NodeProgressFinished += this.subNodeProgressFinished;
            this._myLogicalNode.NodeResultChanged += this.subResultChanged; // 30.07.2018
            this._myLogicalNode.NodeWorkersStateChanged += this.SubNodeWorkersStateChanged; // 07.08.2018
            this._myLogicalNode.PropertiesChanged += this.SubNodePropertiesChanged; // 14.03.2020
            this.SingleNodes = this._myLogicalNode.SingleNodes;
            this.HookedTo = this._myLogicalNode.TreeParams.Name + ": " + this._myLogicalNode.NameId;
            return true;
        }

        private void LogicalNode_AllNodesStateChanged()
        {
            if (LogicalNode.IsTreeFlushing || LogicalNode.IsTreePaused)
            {
                this.IsTreePaused = true;
            }
            else
            {
                this.IsTreePaused = false;
            }
        }

        /// <summary>
        /// Gibt die Business-Logic Node für dieses LogicalNodeViewModel frei
        /// und nullt den BusinessLogic-Knoten (ggf. über IDisposable).
        /// </summary>
        internal void UnsetBLNode()
        {
            this.ReleaseBLNode();
            if (this._myLogicalNode is IDisposable)
            {
                ((IDisposable)this._myLogicalNode).Dispose();
            }
            this._myLogicalNode = UndefinedLogicalNodeClass.UndefinedLogicalNode;
        }

        /// <summary>
        /// Gibt die Business-Logic Node für dieses LogicalNodeViewModel frei.
        /// </summary>
        internal void ReleaseBLNode()
        {
            this._myLogicalNode.PropertiesChanged -= subPropertiesChanged;
            this._myLogicalNode.NodeLogicalChanged -= this.subLogicalChanged;
            this._myLogicalNode.NodeLastNotNullLogicalChanged -= subLastNotNullLogicalChanged;
            this._myLogicalNode.NodeStateChanged -= this.subStateChanged;
            this._myLogicalNode.NodeProgressStarted -= this.SubNodeProgressStarted;
            this._myLogicalNode.NodeProgressChanged -= this.subNodeProgressChanged;
            this._myLogicalNode.NodeProgressFinished -= this.subNodeProgressFinished;
            this._myLogicalNode.NodeResultChanged -= this.subResultChanged; // 30.07.2018
            this._myLogicalNode.NodeWorkersStateChanged -= this.SubNodeWorkersStateChanged; // 07.08.2018
            this._myLogicalNode.PropertiesChanged -= this.SubNodePropertiesChanged; // 14.03.2020

            LogicalNode.AllNodesStateChanged -= this.LogicalNode_AllNodesStateChanged;

            this.HookedTo = "NULL";
        }

        /// <summary>
        /// Gibt die Business-Logic Node für dieses LogicalNodeViewModel frei.
        /// </summary>
        internal void InitFromNode(LogicalNodeViewModel source)
        {
            this._myLogicalNode.InitFromNode(source.GetLogicalNode());
            this.SingleNodes = source.SingleNodes;
        }

        #endregion internal members

        #region protected members

        /// <summary>
        /// Der zugeordnete Knoten aus dem LogicalTaskTree.
        /// </summary>
        protected LogicalNode _myLogicalNode;

        /// <summary>
        /// Das MainWindow.
        /// </summary>
        protected FrameworkElement UIMain { get; set; }

        /// <summary>
        /// Der Haupt-Dispatcher-Thread.
        /// </summary>
        protected Dispatcher UIDispatcher { get; set; }

        /// <summary>
        /// Lädt die Kinder eines Knotens. 
        /// </summary>
        protected void LoadChildren()
        {
            if (this._myLogicalNode is NodeList)
            {
                foreach (LogicalNode node in this._myLogicalNode.Children)
                {
                    if (node is JobConnector)
                    {
                        this.Children.Add(new JobConnectorViewModel(this.RootLogicalTaskTreeViewModel, this, (JobConnector)node, this.UIMain));
                    }
                    else
                    {
                        if (node is SingleNode)
                        {
                            this.Children.Add(new SingleNodeViewModel(this.RootLogicalTaskTreeViewModel, this, (SingleNode)node, this.UIMain));
                        }
                        else
                        {
                            if (node is JobList)
                            {
                                this.Children.Add(new JobListViewModel(this.RootLogicalTaskTreeViewModel, this, (JobList)node, this._lazyLoadChildren, this.UIMain));
                            }
                            else
                            {
                                if (node is Snapshot)
                                {
                                    this.Children.Add(new SnapshotViewModel(this.RootLogicalTaskTreeViewModel, this, (Snapshot)node, this._lazyLoadChildren, this.UIMain));
                                }
                                else
                                {
                                    if (node is NodeList)
                                    {
                                        this.Children.Add(new NodeListViewModel(this.RootLogicalTaskTreeViewModel, this, (NodeList)node, this._lazyLoadChildren, this.UIMain));
                                    }
                                    else
                                    {
                                        this.Children.Add(new SingleNodeViewModel(this.RootLogicalTaskTreeViewModel, this, (LogicalNode)node, this.UIMain));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Aktualisiert die Anzeige eines Teilbaums.
        /// </summary>
        /// <param name="nodeViewModel">ViewModel-Root des Teilbaums.</param>
        /// <param name="node">LogicalNode der ViewModel-Root.</param>
        /// <returns>True, wenn der Teilbaum aktualisiert werden konnte;
        /// bei False muss ein Full-TreeRefresh ausgeführt werden.</returns>
        protected bool RefreshTreeView(LogicalNodeViewModel nodeViewModel, LogicalNode node)
        {
            if (!nodeViewModel.SetBLNode(node, false))
            {
                return false;
            }
            if (nodeViewModel is SnapshotViewModel)
            {
                nodeViewModel.RaisePropertyChanged("SnapshotTime");
                nodeViewModel.RaisePropertyChanged("SnapshotPath");
                nodeViewModel.RaisePropertyChanged("IsDefaultSnapshot");
            }
            if (!(nodeViewModel is SingleNodeViewModel))
            {
                for (int i = 0; i < nodeViewModel.Children.Count; i++)
                {
                    if (node.Children.Count > i)
                    {
                        if (!this.RefreshTreeView(nodeViewModel.Children[i], node.Children[i]))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            nodeViewModel.RaisePropertyChanged("VisualState");
            if (node.Children.Count == nodeViewModel.Children.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Überträgt Eigenschaften von einem gesicherten Branch auf einen aktiven Branch.
        /// </summary>
        /// <param name="sourceTree">Der gesicherte Branch.</param>
        /// <param name="destinationTree">Der aktive Branch.</param>
        /// <param name="indices">Ein Stack zum Zwischenspeichern von Indices auf jeder Hierarchieebene.</param>
        protected void TransferShadowTreeProperties(ObservableCollection<LogicalNodeViewModel> sourceTree,
              ObservableCollection<LogicalNodeViewModel> destinationTree, Stack<int> indices)
        {
            for (int i = 0; i < destinationTree.Count; i++)
            {
                indices.Push(i);
                LogicalNodeViewModel node = destinationTree[i];
                LogicalNodeViewModel? sibling = this.SearchSibling(sourceTree, indices);
                if (sibling != null)
                {
                    this.TransferSiblingProperties(sibling, node);
                }
                if ((node is NodeListViewModel) || (node is JobListViewModel) || (node is SnapshotViewModel))
                {
                    this.TransferShadowTreeProperties(sourceTree, node.Children, indices);
                }
                indices.Pop();
            }
        }

        /// <summary>
        /// Sucht einen Knoten in einem Tree-Branch zu einer gegebenen Index-Hierarchie im Stack "indices".
        /// </summary>
        /// <param name="sourceTree">Der zu durchsuchende Branch.</param>
        /// <param name="indices">Ein Stack mit zwischengespeicherten Indices auf jeder Hierarchieebene bis zu einem Zielknoten.</param>
        /// <returns>Ein gefundenes LogicalNodeViewModel oder null.</returns>
        protected LogicalNodeViewModel? SearchSibling(ObservableCollection<LogicalNodeViewModel> sourceTree, Stack<int> indices)
        {
            int[] indexArray = new int[indices.Count];
            indices.CopyTo(indexArray, 0);
            LogicalNodeViewModel? node = null;
            ObservableCollection<LogicalNodeViewModel> source = sourceTree;
            for (int i = indexArray.Length - 1; i >= 0; i--)
            {
                if (indexArray[i] >= source.Count)
                {
                    return null;
                }
                node = source[indexArray[i]];
                source = node.Children;
            }
            return node;
        }

        /// <summary>
        /// Überträgt bestimmte Eigenschaften im VisualTree von sibling auf node.
        /// </summary>
        /// <param name="sibling">Der Quellknoten (LogicalNodeViewModel).</param>
        /// <param name="node">Der Zielknoten (LogicalNodeViewModel)</param>
        protected void TransferSiblingProperties(LogicalNodeViewModel sibling, LogicalNodeViewModel node)
        {
            node.IsExpanded = sibling.IsExpanded;
            node.ChildOrientation = sibling.ChildOrientation;
        }

        private void ReloadTaskTreeAsync()
        {
            JobList? shadowJobList = null;
            try
            {
                shadowJobList = this._myLogicalNode.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format($"Fehler beim neu Laden des Jobs: {ex.Message}"));
            }
            if (shadowJobList != null)
            {
                // Das DummyLogicalTaskTree-ViewModel
                DummyLogicalTaskTreeViewModel dummyLogicalTaskTreeViewModel = new DummyLogicalTaskTreeViewModel(shadowJobList.TreeParams);
                JobListViewModel shadowTopRootJobListViewModel = new JobListViewModel(dummyLogicalTaskTreeViewModel, null, shadowJobList, false,
                    new FrameworkElement());
                // DEBUG: 23.02.2021 Nagel+
                // LogicalTaskTreeManager.LogTaskTree(shadowTopRootJobListViewModel);
                // Thread.Sleep(1005);
                // DEBUG: 23.02.2021 Nagel-
                if (shadowTopRootJobListViewModel != null)
                {
                    shadowTopRootJobListViewModel.SetChildOrientation(this.StartTreeOrientation);
                    shadowTopRootJobListViewModel.ExpandTree(shadowTopRootJobListViewModel, false);
                    JobListViewModel treeTopRootJobListViewModel = this.GetTopRootJobListViewModel() ?? (JobListViewModel)this;

                    LogicalTaskTreeManager.MergeTaskTrees(treeTopRootJobListViewModel,
                        shadowTopRootJobListViewModel, this.UIDispatcher);

                    shadowTopRootJobListViewModel.Dispose();

                    _ = this.LogTaskTree();
                    Thread.Sleep(10);

                }
            }
        }

        /// <summary>
        /// Lädt den gesamten Tree inklusive JobDescription.xml asynchron neu. 
        /// </summary>
        private async Task ResetContextMenu()
        {
            await Task.Run(() => CloseAndFreeContextMenu());
        }

        private void CloseAndFreeContextMenu()
        {
            this.ContextMenuCanOpen = false;
            Thread.Sleep(300);
            this.ContextMenuCanOpen = true;
        }

        /// <summary>
        /// Speichert die Bildschirmposition des zugehörigen
        /// Controls in der Geschäftslogik.
        /// </summary>
        /// <param name="parentView">Das zugehörige Control.</param>
        protected override void ParentViewToBL(FrameworkElement parentView)
        {
            this._myLogicalNode.ParentView = this.ParentView;
        }

        /// <summary>
        /// Wird angesprungen, wenn sich in LogicalTaskTree für die Anzeige relevante Eigenschaften geändert haben.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="args">EventArgs mit einer String-Liste von Namen geänderter Knoteneigenschaften</param>
        protected virtual void subPropertiesChanged(object sender, PropertiesChangedEventArgs args)
        {
            foreach (string property in args.Properties)
            {
                this.RaisePropertyChanged(property);
            }
        }

        /// <summary>
        /// Wird angesprungen, wenn sich irgendwo in diesem Teilbaum ein Logical geändert hat.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="logical">Die geänderte Eigenschaft der Quelle.</param>
        protected virtual void subLogicalChanged(object sender, bool? logical)
        {
            this.RaisePropertyChanged("Logical");
        }

        /// <summary>
        /// Wird angesprungen, wenn sich irgendwo in diesem Teilbaum ein LastNotNullLogical geändert hat.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="lastNotNullLogical">Die geänderte Eigenschaft der Quelle.</param>
        /// <param name="eventId">Eine optionale GUID für Logging-Zwecke.</param>
        protected virtual void subLastNotNullLogicalChanged(LogicalNode sender, bool? lastNotNullLogical, Guid eventId)
        {
            this.RaisePropertyChanged("LastNotNullLogical");
        }

        /// <summary>
        /// Wird angesprungen, wenn sich irgendwo in diesem Teilbaum ein NodeState geändert hat.
        /// Ändert abhängig von den Knoteneigenschaften "LogicalState", "State" und "Trigger"
        /// die Anzeige des Verarbeitungszustandes des Knoten.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="state">Die geänderte Eigenschaft der Quelle.</param>
        protected virtual void subStateChanged(object sender, NodeState state)
        {
            lock (this.SubStateLocker)
            {
                if (this._myLogicalNode.LogicalState == NodeLogicalState.Fault)
                {
                    this.VisualState = VisualNodeState.Error;
                }
                else
                {
                    switch (state)
                    {
                        case NodeState.Finished:
                            if (this._myLogicalNode.LogicalState == NodeLogicalState.UserAbort)
                            {
                                this.VisualState = VisualNodeState.Aborted;
                            }
                            else
                            {
                                this.VisualState = VisualNodeState.Done;
                            }
                            break;
                        case NodeState.None:
                            if (this._myLogicalNode.LogicalState == NodeLogicalState.Done)
                            {
                                this.VisualState = VisualNodeState.Waiting;
                            }
                            else
                            {
                                this.VisualState = VisualNodeState.None;
                            }
                            break;
                        case NodeState.Null:
                            this.VisualState = VisualNodeState.None;
                            break;
                        case NodeState.Triggered:
                            if (this._myLogicalNode.LogicalState == NodeLogicalState.UserAbort)
                            {
                                this.VisualState = VisualNodeState.Aborted;
                            }
                            else
                            {
                                if (this._myLogicalNode.Trigger != null && this._myLogicalNode.Trigger is TriggerShell
                                  && ((TriggerShell)this._myLogicalNode.Trigger).HasTreeEventTrigger)
                                {
                                    this.VisualState = VisualNodeState.EventTriggered;
                                }
                                else
                                {
                                    this.VisualState = VisualNodeState.Scheduled;
                                }
                            }
                            break;
                        case NodeState.Waiting:
                            this.VisualState = VisualNodeState.Waiting;
                            break;
                        case NodeState.Working:
                            this.VisualState = VisualNodeState.Working;
                            break;
                        case NodeState.InternalError:
                            this.VisualState = VisualNodeState.InternalError;
                            break;
                        default:
                            throw new ApplicationException("LogicalNodeViewModel.subStateChanged: unknown state '" + state + "'.");
                    }
                }
            }
        }

        private void subResultChanged(LogicalNode sender, Result?result)
        {
            this.RaisePropertyChanged("NextRunInfoAndResult");
            this.RaisePropertyChanged("Result"); // notwendig wegen eigener UserNodeControls
            this.RaisePropertyChanged("ShortResult");
        }

        /// <summary>
        /// Wird angesprungen, wenn irgendwo in diesem Teilbaum eine Verarbeitung gestartet wurde.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="args">Zusätzliche Event-Parameter (Fortschritts-% und Info-Object).</param>
        protected virtual void SubNodeProgressStarted(object? sender, ProgressChangedEventArgs args)
        {
            this.RaisePropertyChanged("SingleNodesFinished");
            this.RaisePropertyChanged("Progress");
            this.RaisePropertyChanged("ProgressText");
            this.RaisePropertyChanged("LastRunInfo");
            this.RaisePropertyChanged("NextRunInfo");
            this.RaisePropertyChanged("NextRunInfoAndResult");
            this.RaisePropertyChanged("ShortResult");
        }

        /// <summary>
        /// Wird angesprungen, wenn sich irgendwo in diesem Teilbaum ein Verarbeitungsfortschritt geändert hat.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="args">Zusätzliche Event-Parameter (Fortschritts-% und Info-Object).</param>
        protected virtual void subNodeProgressChanged(object? sender, ProgressChangedEventArgs args)
        {
            this.RaisePropertyChanged("SingleNodesFinished");
            this.RaisePropertyChanged("Progress");
            this.RaisePropertyChanged("ProgressText");
        }

        /// <summary>
        /// Wird angesprungen, wenn irgendwo in diesem Teilbaum eine Verarbeitung beendet wurde.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="args">Zusätzliche Event-Parameter (Fortschritts-% und Info-Object).</param>
        protected virtual void subNodeProgressFinished(object? sender, ProgressChangedEventArgs args)
        {
            if (this._myLogicalNode != null && this._myLogicalNode.SingleNodes > 0)
            {
                this.RaisePropertyChanged("SingleNodesFinished");
                this.RaisePropertyChanged("Progress");
                this.RaisePropertyChanged("ProgressText");
                this.RaisePropertyChanged("LastRunInfo");
                this.RaisePropertyChanged("NextRunInfo");
                this.RaisePropertyChanged("NextRunInfoAndResult");
                this.RaisePropertyChanged("ShortResult");
                this.RaisePropertyChanged("Result");
            }
        }

        /// <summary>
        /// Wird angesprungen, wenn für diesen Knoten Worker existieren und sich deren Zustand verändert hat.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        protected virtual void SubNodeWorkersStateChanged(object sender)
        {
            this.RaisePropertyChanged("WorkersState");
        }

        /// <summary>
        /// Wird angesprungen, wenn sich für diesen Knoten sonstige Properties geändert haben,
        /// die an die UI weitergegeben werden sollen.
        /// </summary>
        /// <param name="sender">Quelle des Events (LogicalNode).</param>
        /// <param name="args">StringList mit Namen der betroffenen Properties.</param>
        protected void SubNodePropertiesChanged(object sender, PropertiesChangedEventArgs args)
        {
            if (args.Properties.Contains("IsInSleepTime"))
            {
                this.IsInSleepTime = this._myLogicalNode.IsInSleepTime;
                this.SleepTimeTo = String.Format("Ruhezeit bis {0:c}", this._myLogicalNode.SleepTimeTo);
            }
            foreach (string propertyName in args.Properties)
            {
                this.RaisePropertyChanged(propertyName);
            }
        }

        #endregion protected members

        #region private members

        private static readonly LogicalNodeViewModel _dummyChild = new LogicalNodeViewModel();
        private static int _treeEventSemaphore; // = 0 wird vom System vorbelegt;

        // Bei True werden die Kinder eines TreeView-Knotens erst beim Öffnen nachgeladen.
        private bool _lazyLoadChildren;
        private bool _isExpanded;
        private bool _isSelected;
        private VisualNodeState _visualState;
        private Orientation _childOrientation;
        private int _singleNodes;
        private LogicalNodeViewModel? _parent;
        private bool _canExpanderExpand;

        private System.Windows.Visibility _visibility;
        private string _freeComment;
        private bool _isInSleepTime;
        private string? _sleepTimeTo;
        private volatile bool _isTreePaused;
        private TreeParameters _treeParams;
        private string? _hookedTo;
        private string? _jobInProgress;
        private bool _contextMenuCanOpen;
        private RelayCommand _btnCopyToolTipInfoToClipboardCommand;

        // True, wenn die Kinder dieses Knotens noch nicht geladen wurden.
        private bool _hasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == _dummyChild; }
        }

        // Erzeugt die Instanz des DummyChilds.
        private LogicalNodeViewModel()
        {
            this._canExpanderExpand = true;
            this.Children = new();
            this.TreeRefreshLocker = new object();
            this.SubStateLocker = new object();
            this._myLogicalNode = UndefinedLogicalNodeClass.UndefinedLogicalNode;
            this._treeParams = new TreeParameters("", null);
            this.RootLogicalTaskTreeViewModel = new(this._treeParams);
            this.UIMain = new FrameworkElement();
            this.UIDispatcher = Dispatcher.CurrentDispatcher;
            this._freeComment = "";
            this._btnCopyToolTipInfoToClipboardCommand = new RelayCommand(this.CmdBtnCopy_Executed, this.CmdBtnCopy_CanExecute);
        }

        private void CmdBtnCopy_Executed(object? parameter)
        {
            Clipboard.SetText(this.GetToolTipInfo());
        }

        private bool CmdBtnCopy_CanExecute()
        {
            return true;
        }

        #endregion private members

    }
}
