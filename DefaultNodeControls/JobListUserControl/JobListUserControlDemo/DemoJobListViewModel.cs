using NetEti.MVVMini;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Vishnu.Interchange;
using Vishnu.ViewModel;

namespace Vishnu.DemoApplications.JobListUserControlDemo
{
    /// <summary>
    /// Funktion: Demo-JobListViewModel.
    /// Dient zum Test von UserNodeControls außerhalb von Vishnu.
    /// </summary>
    /// <remarks>
    ///
    /// 14.03.2023 Erik Nagel: erstellt
    /// </remarks>
    public class DemoJobListViewModel : VishnuViewModelBase
    {
        /// <summary>
        /// Der Name des zugehörigen LogicalTaskTree-Knotens
        /// für die UI verfügbar gemacht.
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (this._name != value)
                {
                    this._name = value;
                    this.RaisePropertyChanged(nameof(Name));
                }
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
                return this._logical;
            }
            set
            {
                if (this._logical != value)
                {
                    this._logical = value;
                    this.RaisePropertyChanged(nameof(Logical));
                }
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
                return this._lastNotNullLogical;
            }
            set
            {
                if (this._lastNotNullLogical != value)
                {
                    this._lastNotNullLogical = value;
                    this.RaisePropertyChanged(nameof(LastNotNullLogical));
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
                if (this._visualState != value)
                {
                    this._visualState = value;
                    this.RaisePropertyChanged(nameof(VisualState));
                }
            }
        }

        /// <summary>
        /// Kombinierter NodeWorkerState für alle zugeordneten NodeWorker.
        /// </summary>
        public VisualNodeWorkerState WorkersState
        {
            get
            {
                return this._workersState;
            }
            set
            {
                if (this._workersState != value)
                {
                    this._workersState = value;
                    this.RaisePropertyChanged(nameof(WorkersState));
                }
            }
        }

        /// <summary>
        /// Ein Text für die Anzahl der beendeten Endknoten dieses Teilbaums
        /// zur Anzeige im ProgressBar (i.d.R. nnn%) für die UI verfügbar gemacht.
        /// </summary>
        public string? ProgressText
        {
            get
            {
                return this._progressText;
            }
            set
            {
                if (this._progressText != value)
                {
                    this._progressText = value;
                    this.RaisePropertyChanged(nameof(ProgressText));
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
                return this._singleNodesFinished;
            }
            set
            {
                if (this._singleNodesFinished != value)
                {
                    this._singleNodesFinished = value;
                    this.RaisePropertyChanged(nameof(SingleNodesFinished));
                    this.RaisePropertyChanged(nameof(Progress));
                    this.RaisePropertyChanged(nameof(ProgressText));
                }
            }
        }

        /// <summary>
        /// Die Anzahl der beendeten Endknoten dieses Teilbaums
        /// unter anderem Namen für die UI verfügbar gemacht.
        /// </summary>
        public int Progress
        {
            get
            {
                return this._singleNodesFinished;
            }
        }

        /// <summary>
        /// Das ReturnObject der zugeordneten LogicalNode.
        /// </summary>
        public override Result? Result
        {
            get
            {
                return this._result;
            }
            set
            {
                if (this._result != value)
                {
                    this._result = value;
                    this.RaisePropertyChanged(nameof(Result));
                    this.RaisePropertyChanged(nameof(ShortResult));
                }
            }
        }

        /// <summary>
        /// Das ReturnObject der zugeordneten LogicalNode.
        /// </summary>
        public String? ShortResult
        {
            get
            {
                return this.Result?.ToString();
            }
        }

        /// <summary>
        /// Zeitpunkt des letzten Starts des Knoten als String.
        /// </summary>
        public string LastRunInfo
        {
            get
            {
                return this._lastRun.ToString();
            }
            set
            {
                this.RaisePropertyChanged(nameof(LastRunInfo));
            }
        }

        /// <summary>
        /// Kombinierte Ausgabe von NextRunInfo (wann ist der nächsta Durchlauf)
        /// und Result (in voller Länge).
        /// </summary>
        public string NextRunInfoAndResult
        {
            get
            {
                return String.Format("nächster Lauf: {0}\nletztes Ergebnis: {1}", "- Demo, kein nächster Lauf -", (this.Result == null ? "" : this.Result.ToString()));
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
                return this._isSnapshotDummy;
            }
            set
            {
                if (this._isSnapshotDummy != value)
                {
                    this._isSnapshotDummy = value;
                    this.RaisePropertyChanged(nameof(IsSnapshotDummy));
                }
            }
        }

        /// <summary>
        /// Veröffentlicht einen ButtonText entsprechend this._myLogicalNode.CanTreeStart.
        /// </summary>
        public string ButtonRunText
        {
            get
            {
                return "Run";
            }
            set
            {
                this.RaisePropertyChanged(nameof(ButtonRunText));
            }
        }

        /// <summary>
        /// Veröffentlicht einen ButtonText entsprechend this._myLogicalNode.CanTreeStart.
        /// </summary>
        public string ButtonBreakText
        {
            get
            {
                return "Break";
            }
            set
            {
                this.RaisePropertyChanged(nameof(ButtonBreakText));
            }
        }

        /// <summary>
        /// Command für den Run-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand RunLogicalTaskTree { get { return this._btnRunTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für den Break-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand BreakLogicalTaskTree { get { return this._btnBreakTaskTreeRelayCommand; } }

        /// <summary>
        /// Definiert, ob die Kind-Elemente dieses Knotens
        /// horizontal oder vertikal angeordnet werden sollen.
        /// </summary>
        public Orientation ChildOrientation
        {
            get
            {
                return Orientation.Horizontal;
            }
        }

        /// <summary>
        /// Die Kinder des aktuellen Knotens.
        /// </summary>
        public ObservableCollection<VishnuViewModelBase> Children { get; internal set; }

        /// <summary>
        /// Standard Konstruktor - setzt alle Demo-Properties.
        /// </summary>
        public DemoJobListViewModel()
        {
            this._name = "Demo-View";
            this.RaisePropertyChanged(nameof(Name));
            this.Logical = false;
            this.LastNotNullLogical = false;
            this.VisualState = VisualNodeState.Done;
            this.WorkersState = VisualNodeWorkerState.Valid;
            this.SingleNodesFinished = 100;
            this.ProgressText = "100 %";
            this.IsSnapshotDummy = false;
            this._lastRun = DateTime.Now;
            this._btnRunTaskTreeRelayCommand = new RelayCommand(runTaskTreeExecute, canRunTaskTreeExecute);
            this._btnBreakTaskTreeRelayCommand = new RelayCommand(breakTaskTreeExecute, canBreakTaskTreeExecute);
            this._result = new Result("TestResult", true, NodeState.Finished, NodeLogicalState.Done, null);
            this.Children = new ObservableCollection<VishnuViewModelBase>();
            this.RaisePropertyChanged(nameof(Result));
        }

        public void RaiseAllProperties()
        {
            this.RaisePropertyChanged(nameof(Name));
            this.RaisePropertyChanged(nameof(Logical));
            this.RaisePropertyChanged(nameof(LastNotNullLogical));
            this.RaisePropertyChanged(nameof(VisualState));
            this.RaisePropertyChanged(nameof(WorkersState));
            this.RaisePropertyChanged(nameof(SingleNodesFinished));
            this.RaisePropertyChanged(nameof(Progress));
            this.RaisePropertyChanged(nameof(ProgressText));
            this.RaisePropertyChanged(nameof(IsSnapshotDummy));
            this.RaisePropertyChanged(nameof(Result));
            this.RaisePropertyChanged(nameof(ShortResult));
            this.RaisePropertyChanged(nameof(LastRunInfo));
            this.RaisePropertyChanged(nameof(NextRunInfoAndResult));
            this.RaisePropertyChanged(nameof(ButtonRunText));
            this.RaisePropertyChanged(nameof(ButtonBreakText));
            this.RaisePropertyChanged(nameof(ChildOrientation));
        }

        private Result? _result;
        private bool? _logical;
        private bool? _lastNotNullLogical;
        private VisualNodeState _visualState;
        private string? _progressText;
        private int _singleNodesFinished;
        private string _name;
        private VisualNodeWorkerState _workersState;
        private RelayCommand _btnRunTaskTreeRelayCommand;
        private RelayCommand _btnBreakTaskTreeRelayCommand;
        private DateTime _lastRun;
        private bool _isSnapshotDummy;

        private async void runTaskTreeExecute(object? parameter)
        {
            this._lastRun = DateTime.Now;
            this.Logical = true;
            this.LastNotNullLogical = true;
            this.VisualState = VisualNodeState.Done;
            this.WorkersState = VisualNodeWorkerState.Valid;
            this.RaiseAllProperties();

            Task<int> longRunningTask = LongRunningOperationAsync();
            int result = await longRunningTask;

            if (this.Result?.ReturnObject != null)
            {
                // ...
                this.RaisePropertyChanged(nameof(Result));
                this.RaisePropertyChanged(nameof(ShortResult));
            }
        }
        public async Task<int> LongRunningOperationAsync()
        {
            for (int i = 0; i <= 100; i++)
            {
                this.SingleNodesFinished = i;
                this.ProgressText = this.SingleNodesFinished.ToString();
                await Task.Delay(30);
            }
            return 123;
        }

        private bool canRunTaskTreeExecute()
        {
            return true;
        }

        private void breakTaskTreeExecute(object? parameter)
        {
            this.Logical = null;
            this.LastNotNullLogical = null;
            this.VisualState = VisualNodeState.Aborted;
            this.WorkersState = VisualNodeWorkerState.Valid;
            this.SingleNodesFinished = 0;
            this.ProgressText = "0 %";
            this.RaiseAllProperties();
        }

        private bool canBreakTaskTreeExecute()
        {
            return true;
        }

    }
}
