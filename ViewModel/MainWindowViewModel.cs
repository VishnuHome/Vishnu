using System;
using NetEti.MVVMini;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Vishnu.Interchange;
using NetEti.Globals;
using NetEti.ApplicationControl;
using System.Windows.Threading;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für das MainWindow.
    /// </summary>
    /// <remarks>
    /// File: MainWindowViewModel
    /// Autor: Erik Nagel
    ///
    /// 09.01.2014 Erik Nagel: erstellt
    /// </remarks>
    public class MainWindowViewModel : ObservableObject, IViewModelRoot
    {
        #region public members

        #region IViewModelRoot Implementation

        /// <summary>
        /// Liefert das oberste JobListViewModel des Trees als IVishnuViewModel.
        /// </summary>
        /// <returns>Das oberste JobListViewModel des Trees als IVishnuViewModel.</returns>
        public IVishnuViewModel GetTopJobListViewModel()
        {
            return this.TreeVM.TreeVM;
        }

        /// <summary>
        /// Setzt das oberste JobListViewModel des Trees.
        /// Returnt das bisherige oberste JobListViewModel.
        /// </summary>
        /// <param name="topJobListViewModel">Das neue oberste JobListViewModel des Trees.</param>
        /// <returns>Das bisherige oberste JobListViewModel des Trees.</returns>
        public IVishnuViewModel SetTopJobListViewModel(IVishnuViewModel topJobListViewModel)
        {
            JobListViewModel oldTopJobListViewModel = this.TreeVM.TreeVM;
            this.TreeVM.TreeVM = topJobListViewModel as JobListViewModel;
            this.RaisePropertyChanged("TreeVM");
            this.RaisePropertyChanged("JobGroupsVM");
            return oldTopJobListViewModel;
        }

        /// <summary>
        /// Aktualisiert die ViewModels von eventuellen zusätzliche Ansichten,
        /// die dieselbe BusinessLogic abbilden (hier: JobGroupViewModel).
        /// </summary>
        public void RefreshDependentAlternativeViewModels()
        {
            this.SelectJobGroups(this.JobGroupsVM);
        }

        #endregion IViewModelRoot Implementation

        /// <summary>
        /// Der Titel des Haupt-Fensters.
        /// </summary>
        public string WindowTitle
        {
            get
            {
                return this._windowTitle;
            }
            set
            {
                if (this._windowTitle != value)
                {
                    this._windowTitle = value;
                    this.RaisePropertyChanged("WindowTitle");
                }
            }
        }

        /// <summary>
        /// Zusätzliche Parameter, die für den gesamten Tree Gültigkeit haben oder null.
        /// </summary>
        public TreeParameters TreeParams { get; private set; }

        /// <summary>
        /// ViewModel für den LogicalTaskTree.
        /// </summary>
        public LogicalTaskTreeViewModel TreeVM { get; set; }

        private Dispatcher _dispatcher;

        /// <summary>
        /// Liste von JobListViewModels mit ihren Checkern.
        /// </summary>
        public ObservableCollection<JobGroupViewModel> JobGroupsVM {
            get
            {
                return this._jobGroupsVM;
            }
            private set
            {
                if (this._jobGroupsVM != value)
                {
                    this._jobGroupsVM = value;
                    RaisePropertyChanged("JobGroupsVM");
                }
            }
        }

        /// <summary>
        /// Command für den Run-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand RunJobGroups { get { return this.TreeVM.RunLogicalTaskTree; } }

        /// <summary>
        /// Command für den Break-Button im LogicalTaskTreeControl.
        /// </summary>
        public ICommand BreakJobGroups { get { return this.TreeVM.BreakLogicalTaskTree; } }


        /// <summary>
        /// Setzt die Fenstergröße unter Berücksichtigung von Maximalgrenzen auf die
        /// Höhe und Breite des Inhalts und die Property SizeToContent auf WidthAndHeight.
        /// </summary>
        public ICommand InitSizeCommand { get { return this._initSizeRelayCommand; } }

        /// <summary>
        /// Konstruktor - übernimmt das ViewModel für den LogicalTaskTree und eine Methode des
        /// MainWindows zum Restaurieren der Fenstergröße abhängig vom Fensterinhalt.
        /// </summary>
        /// <param name="logicalTaskTreeViewModel">ViewModel für den LogicalTaskTree.</param>
        /// <param name="initWindowSize">Restauriert die Fenstergröße abhängig vom Fensterinhalt.</param>
        /// <param name="flatNodeListFilter">Filter für definierte Knotentypen.</param>
        /// <param name="additionalWindowHeaderInfo">Optionaler Zusatztext für den Fenstertitel.</param>
        /// <param name="treeParams">Parameter für den gesamten Tree.</param>
        public MainWindowViewModel(LogicalTaskTreeViewModel logicalTaskTreeViewModel, Action<object> initWindowSize, NodeTypes flatNodeListFilter,
            string additionalWindowHeaderInfo, TreeParameters treeParams)
        {
            this.TreeVM = logicalTaskTreeViewModel;
            this._dispatcher = Dispatcher;
            this._initSizeRelayCommand = new RelayCommand(initWindowSize);
            this._flatNodeListFilter = flatNodeListFilter;
            treeParams.ViewModelRoot = this;
            this.TreeParams = treeParams;
            this.JobGroupsVM = this.SelectJobGroups(null);
            this.WindowTitle = "Vishnu Logical Process Monitor"
                + " - " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
                + "  " + additionalWindowHeaderInfo;
            //this.MoveWindowToStartPosition();

        }

        /// <summary>
        /// Ruft die gleichnamige Methode im LogicalTaskTreeViewModel zum speichern des aktuellen
        /// Darstellungszustandes des Main Windows (parameter) und aller Knoten (collapsed oder expanded) auf.
        /// </summary>
        /// <param name="windowAspects">Aktuelle UI-Eigenschaften (z.B. WindowTop, WindowWidth, ...).</param>
        public void SaveTreeState(WindowAspects windowAspects)
        {
            this.TreeVM.SaveTreeState(windowAspects);
        }

        #endregion public members

        #region private members

        private ObservableCollection<JobGroupViewModel> _jobGroupsVM;
        private RelayCommand _initSizeRelayCommand;
        private NodeTypes _flatNodeListFilter;
        private string _windowTitle;

        delegate ObservableCollection<JobGroupViewModel> SelectJobGroupsDelegate(ObservableCollection<JobGroupViewModel> selectedJobGroups);
        private ObservableCollection<JobGroupViewModel> SelectJobGroups(ObservableCollection<JobGroupViewModel> selectedJobGroups)
        {
            if (!this._dispatcher.CheckAccess())
            {
                return (ObservableCollection<JobGroupViewModel>)this._dispatcher.Invoke(DispatcherPriority.Background,
                    new SelectJobGroupsDelegate(SelectJobGroups), selectedJobGroups);
            }
            if (selectedJobGroups == null)
            {
                selectedJobGroups = new ObservableCollection<JobGroupViewModel>();
            }
            else
            {
                selectedJobGroups.Clear();
            }
            foreach (LogicalNodeViewModel root in LogicalTaskTreeViewModel.FlattenTree(TreeVM.TreeVM, new ObservableCollection<LogicalNodeViewModel>(), this._flatNodeListFilter))
            {
                // InfoController.Say(String.Format($"#RELOAD# LogicalNodeViewModel from FlattenTree: {root.TreeParams.Name}:{root.Id} {(root.VisualTreeCacheBreaker)}"));
                if (root is JobListViewModel)
                {
                    selectedJobGroups.Add(new JobGroupViewModel(root as JobListViewModel, GenericSingletonProvider.GetInstance<AppSettings>().FlatNodeListFilter));
                }
            }
            return selectedJobGroups;
        }

        #endregion private members

    }
}
