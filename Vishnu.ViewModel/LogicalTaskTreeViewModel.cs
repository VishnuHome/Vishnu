using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using NetEti.MVVMini;
using LogicalTaskTree;
using Vishnu.Interchange;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEti.CustomControls;
using NetEti.MultiScreen;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für die Darstellung eines logicalTaskTree
    /// in einer TreeView.
    /// </summary>
    /// <remarks>
    /// File: LogicalTaskTreeViewModel
    /// Autor: Erik Nagel
    ///
    /// 05.01.2013 Erik Nagel: erstellt
    /// </remarks>
    public class LogicalTaskTreeViewModel : OrientedTreeViewModelBase
    {
        #region public members

        #region published members

        /// <summary>
        /// ItemsSource für optionale Zusatz-Infos zum Tree.
        /// </summary>
        public ObservableCollection<string> InfoSource { get; private set; }

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
        /// Command für den Run-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand RunLogicalTaskTree { get { return this._btnRunTaskTreeRelayCommand; } }

        /// <summary>
        /// Command für den Break-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand BreakLogicalTaskTree { get { return this._btnBreakTaskTreeRelayCommand; } }

        #endregion published members

        /// <summary>
        /// Konstruktor - übernimmt die Geschäftslogik, das MainWindow, die Start-Ausrichtung des Baums
        /// und einen Filter für die anzuzeigenden NodeTypes.
        /// </summary>
        /// <param name="businessLogic">Die Geschäftslogik der Anwendung.</param>
        /// <param name="uiMain">Das Vishnu-MainWindow.</param>
        /// <param name="startTreeOrientation">Die Startausrichtung des Baums.</param>
        /// <param name="flatNodeListFilter">Ein Filter für anzuzeigende NodeTypes.</param>
        /// <param name="treeParams">Parameter für den gesamten Tree.</param>
        public LogicalTaskTreeViewModel(LogicalTaskTree.LogicalTaskTree businessLogic, FrameworkElement uiMain,
            TreeOrientation startTreeOrientation, NodeTypes flatNodeListFilter, TreeParameters treeParams) : base(treeParams)
        {
            this._businessLogic = businessLogic;
            this._uIMain = uiMain;
            this._root = businessLogic.Tree;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                this._uIDispatcher = this._uIMain.Dispatcher;
            }
            this._btnRunTaskTreeRelayCommand = new RelayCommand(runTaskTreeExecute, canRunTaskTreeExecute);
            this._btnBreakTaskTreeRelayCommand = new RelayCommand(breakTaskTreeExecute, canBreakTaskTreeExecute);
            this.InfoSource = new ObservableCollection<string>();

            this._root.NodeStateChanged -= this.treeElementStateChanged;
            this._root.NodeStateChanged += this.treeElementStateChanged;
            this._root.PropertiesChanged -= this.treePropertiesChanged;
            this._root.PropertiesChanged += this.treePropertiesChanged;
            /* MEMORY //
            this._root.SingleNodeProgressFinished += new CommonProgressChangedEventHandler(
                new Action<object, CommonProgressChangedEventArgs>(
                  (sender, args) =>
                  {
                    if (this._testAusgabeCount < this._maxTestAusgabeCount)
                    {
                      this._uIDispatcher.Invoke(new Action(() =>
                      {
                        if (this._testAusgabeCount == 0)
                        {
                          this.InfoSource.Clear();
                        }
                        if (this._testAusgabeCount < 1)
                        {
                          this.ShowTreeInfos();
                        }
                        //this.InfoSource.AddRange(this._root.Show("#OO#").ToArray()); // es gibt leider kein AddRange
                        this._root.Show("    ").ToArray().FirstOrDefault(a => { this.InfoSource.Add(a); return false; });
                        this.InfoSource.Add("------------------------------------------------------------------------------------------------------------------------");
                        this.InfoSource.Add(this._root.Logical.ToString());
                        this._root.GetResults();
                      }), DispatcherPriority.Normal, null);
                      this._testAusgabeCount++;
                    }
                  }
                )
             );
            */
            this.TreeVM = new JobListViewModel(this, null, this._root, false, this._uIMain);
            this.TreeVM.SetChildOrientation(startTreeOrientation);
            this.TreeVM.ExpandTree(this.TreeVM, false);
            //this._flatNodeListFilter = flatNodeListFilter;
            //this.FlatNodeViewModelList = LogicalTaskTreeViewModel.FlattenTree(this.TreeVM, new List<LogicalNodeViewModel>(), this._flatNodeListFilter);
        }

        /// <summary>
        /// Macht aus einem übergebenen (Teil-)Baum eine flache Liste.
        /// Welche Knotentypen in der Liste angezeigt werden und welche nicht,
        /// kann extern in der app.config über
        ///   "FlatNodeListFilter" 
        ///   value="NodeConnector|ValueModifier|Constant|Checker|NodeList|JobList|Snapshot"
        /// festgelegt werden.
        /// </summary>
        /// <param name="root">Wurzelknoten des (Teil-)Baums.</param>
        /// <param name="flatNodeList">ObservableCollection zur Aufnahme der Knoten.</param>
        /// <param name="flatNodeListFilter">Filter für Knotentypen die nicht in die Liste aufgenommen werden sollen.</param>
        /// <param name="withRoot">Bei True wird der Rootknoten mit in die Liste aufgenommen (Default: True).</param>
        /// <returns>Flache Liste (ObservableCollection) der Knoten des (Teil-)Baums.</returns>
        public static ObservableCollection<LogicalNodeViewModel> FlattenTree(LogicalNodeViewModel root, ObservableCollection<LogicalNodeViewModel> flatNodeList, NodeTypes flatNodeListFilter, bool withRoot = true)
        {
            return LogicalTaskTreeViewModel.flattenTree(root, flatNodeList, flatNodeListFilter, withRoot, true);
        }

        /// <summary>
        /// Speichert den aktuellen Darstellungszustand des Main Windows (parameter) und aller Knoten (collapsed oder expanded).
        /// </summary>
        /// <param name="windowAspects">Aktuelle UI-Eigenschaften (z.B. WindowTop, WindowWidth, ...).</param>
        public void SaveTreeState(WindowAspects windowAspects)
        {
            Task.Factory.StartNew(() =>
            {
                ConfigurationManager.SaveLocalConfiguration(this.TreeVM, this.TreeOrientationState, windowAspects);
            });
            if (this._uIMain is Window)
            {

                // Hier wird direkt vor dem Run TreeParams.LastParentViewAbsoluteScreenPosition neu gesetzt.
                // Hintergrund: Der Benutzer kann in der Zwischenzeit das MainWindow verschoben haben.
                // Achtung: eine direkte Zuweisung geht wegen Thread-übergreifendem Zugriff schief!
                // Hinweis: ParentView ist bei Knoten, die aktuell auf dem Bildschirm nicht dargestellt
                // werden, nicht gefüllt (null).
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if (this._uIMain != null)
                    {
                        // TODO: Beim Hin- Und Herschalten zwischen Tree-und Jobs-Ansicht
                        // wird die korrekte Anfangsposition nicht gehalten (Stand 22.01.2024 Nagel).
                        ScreenInfo actScreenInfo = ScreenInfo.GetMainWindowScreenInfo()
                            ?? throw new NullReferenceException("ScreenInfo darf hier nicht null sein.");
                        AppSettings.ActScreenBounds = actScreenInfo.Bounds;
                        Point lastParentViewAbsoluteScreenPosition =
                            WindowAspects.GetFrameworkElementAbsoluteScreenPosition(this._uIMain, false);
                        if (lastParentViewAbsoluteScreenPosition.X > AppSettings.ActScreenBounds.Right - 50)
                        {
                            lastParentViewAbsoluteScreenPosition.X = AppSettings.ActScreenBounds.Bottom - 50;
                        }
                        if (lastParentViewAbsoluteScreenPosition.Y > AppSettings.ActScreenBounds.Right - 35)
                        {
                            lastParentViewAbsoluteScreenPosition.Y = AppSettings.ActScreenBounds.Bottom - 35;
                        }
                        this.TreeParams.LastParentViewAbsoluteScreenPosition =
                            lastParentViewAbsoluteScreenPosition;
                    }
                }));
                new TimerMessageBox(this._uIMain as Window)
                {
                    Buttons = MessageBoxButtons.None,
                    LifeTimeMilliSeconds = 2000,
                    Icon = MessageBoxIcons.Working,
                    Caption = "Verarbeitung läuft ...",
                    Text = "Die aktuelle Bildschirmdarstellung wird gespeichert.",
                    Position = this.TreeParams.LastParentViewAbsoluteScreenPosition
                }.Show();
            }
            else
            {
                new TimerMessageBox()
                {
                    Buttons = MessageBoxButtons.None,
                    LifeTimeMilliSeconds = 2000,
                    Icon = MessageBoxIcons.Working,
                    Caption = "Verarbeitung läuft ...",
                    Text = "Die aktuelle Bildschirmdarstellung wird gespeichert."
                }.Show();
            }
        }

        #endregion public members

        #region private members

        private class ViewModelCompare : IEqualityComparer<LogicalNodeViewModel>
        {
            public bool Equals(LogicalNodeViewModel? x, LogicalNodeViewModel? y)
            {
                if (x?.Id == y?.Id || x?.Name == y?.Name && x?.OriginalNodeId == y?.OriginalNodeId)
                {
                    return true;
                }
                else { return false; }
            }
            public int GetHashCode(LogicalNodeViewModel x)
            {
                return (x.Id + x.OriginalNodeId).GetHashCode();
            }
        }

        private LogicalTaskTree.LogicalTaskTree _businessLogic;
        private JobList _root;
        private RelayCommand _btnRunTaskTreeRelayCommand;
        private RelayCommand _btnBreakTaskTreeRelayCommand;
        private FrameworkElement _uIMain { get; set; }
        private System.Windows.Threading.Dispatcher? _uIDispatcher { get; set; }

        //private int _testAusgabeCount = 0;
        //private int _maxTestAusgabeCount = 1;
        private bool _isInSleepTime;
        private string? _sleepTimeTo;

        private static void flatNodeListAddIfNew(LogicalNodeViewModel root, ObservableCollection<LogicalNodeViewModel> flatNodeList)
        {
            SingleNodeViewModel rootSvm = (SingleNodeViewModel)root;
            if (flatNodeList.Contains(rootSvm, new ViewModelCompare()))
            {
                LogicalNodeViewModel vm = flatNodeList.Where(x => { return x.Id == rootSvm.Id || x.OriginalNodeId == rootSvm.OriginalNodeId; }).First();
                vm.FreeComment = vm.FreeComment == "" ? "2" : (Convert.ToInt32(vm.FreeComment) + 1).ToString();
            }
            else
            {
                ((SingleNodeViewModel)root).FreeComment = "";
                flatNodeList.Add((SingleNodeViewModel)root);
            }
        }

        private static ObservableCollection<LogicalNodeViewModel> flattenTree(LogicalNodeViewModel root, ObservableCollection<LogicalNodeViewModel> flatNodeList, NodeTypes flatNodeListFilter, bool withRoot, bool isRoot)
        {
            if (!isRoot || withRoot)
            {
                NodeTypes nodeType = root.GetLogicalNode().NodeType;
                switch (nodeType)
                {
                    case NodeTypes.None:
                        break;
                    case NodeTypes.ValueModifier:
                        if ((flatNodeListFilter & NodeTypes.ValueModifier) == 0)
                        {
                            flatNodeListAddIfNew(root, flatNodeList);
                        }
                        break;
                    case NodeTypes.Constant:
                        if ((flatNodeListFilter & NodeTypes.Constant) == 0)
                        {
                            flatNodeList.Add((SingleNodeViewModel)root);
                        }
                        break;
                    case NodeTypes.NodeConnector:
                        if ((flatNodeListFilter & NodeTypes.NodeConnector) == 0)
                        {
                            flatNodeListAddIfNew(root, flatNodeList);
                        }
                        break;
                    case NodeTypes.Checker:
                        if ((flatNodeListFilter & NodeTypes.Checker) == 0)
                        {
                            flatNodeList.Add((SingleNodeViewModel)root);
                        }
                        break;
                    case NodeTypes.Snapshot:
                        if ((flatNodeListFilter & NodeTypes.Snapshot) == 0)
                        {
                            flatNodeList.Add((SnapshotViewModel)root);
                        }
                        break;
                    case NodeTypes.JobList:
                        if ((flatNodeListFilter & NodeTypes.JobList) == 0)
                        {
                            flatNodeList.Add((JobListViewModel)root);
                        }
                        break;
                    case NodeTypes.NodeList:
                        if ((flatNodeListFilter & NodeTypes.NodeList) == 0)
                        {
                            flatNodeList.Add((NodeListViewModel)root);
                        }
                        break;
                    default:
                        break;
                }
            }
            if (root.Children.Count > 0)
            {
                foreach (LogicalNodeViewModel child in root.Children)
                {
                    if (withRoot || !(child is JobListViewModel))
                    {
                        LogicalTaskTreeViewModel.flattenTree(child, flatNodeList, flatNodeListFilter, withRoot, false);
                    }
                    else
                    {
                        flatNodeList.Add((JobListViewModel)child);
                    }
                }
            }
            return flatNodeList;
        }

        private void treeElementStateChanged(object sender, NodeState state)
        {
            // Die Buttons müssen zum Update gezwungen werden, da die Verarbeitung in einem
            // anderen Thread läuft:
            this._btnRunTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
            this._btnBreakTaskTreeRelayCommand.UpdateCanExecuteState(this.Dispatcher);
        }

        private void treePropertiesChanged(object sender, PropertiesChangedEventArgs args)
        {
            if (args.Properties.Contains("IsInSleepTime"))
            {
                this.IsInSleepTime = this._root.IsInSleepTime;
                this.SleepTimeTo = String.Format("Ruhezeit bis {0:c}", this._root.SleepTimeTo);
            }
            foreach (string propertyName in args.Properties)
            {
                this.RaisePropertyChanged(propertyName);
            }
        }

        private void runTaskTreeExecute(object? parameter)
        {
            this._root.ProcessTreeEvent("UserRun", null);
            this._root.UserRun();
        }

        private bool canRunTaskTreeExecute()
        {
            bool canStart = this._root.CanTreeStart;
            return canStart;
        }

        private void breakTaskTreeExecute(object? parameter)
        {
            this._root.UserBreak();
        }

        private bool canBreakTaskTreeExecute()
        {
            //bool canBreak = !this._root.CanTreeStart;
            bool canBreak = true;
            return canBreak;
        }

        //private void ShowTreeInfos()
        //{
        //    this._root.GetAllTreeInfos().ToArray().FirstOrDefault(a => { this.InfoSource.Add(a); return false; });
        //}

        #endregion private members

    }
}
