using System;
using NetEti.MVVMini;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Vishnu.Interchange;
using NetEti.Globals;
using System.Windows.Media;

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
    public class MainWindowViewModel : ObservableObject
    {
        #region public members

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
        /// ViewModel für den LogicalTaskTree.
        /// </summary>
        public LogicalTaskTreeViewModel TreeVM { get; set; }

        /// <summary>
        /// Liste von JobListViewModels mit ihren Checkern.
        /// </summary>
        public ObservableCollection<JobGroupViewModel> JobGroupsVM { get; private set; }

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
        public MainWindowViewModel(LogicalTaskTreeViewModel logicalTaskTreeViewModel, Action<object> initWindowSize, NodeTypes flatNodeListFilter, string additionalWindowHeaderInfo)
        {
            this.TreeVM = logicalTaskTreeViewModel;
            this._initSizeRelayCommand = new RelayCommand(initWindowSize);
            this._flatNodeListFilter = flatNodeListFilter;
            this.JobGroupsVM = this.SelectJobGroups();
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

        private RelayCommand _initSizeRelayCommand;
        private NodeTypes _flatNodeListFilter;
        private string _windowTitle;

        private ObservableCollection<JobGroupViewModel> SelectJobGroups()
        {
            ObservableCollection<JobGroupViewModel> selectedJobGroups = new ObservableCollection<JobGroupViewModel>();
            foreach (LogicalNodeViewModel root in LogicalTaskTreeViewModel.FlattenTree(TreeVM.TreeVM, new ObservableCollection<LogicalNodeViewModel>(), this._flatNodeListFilter))
            {
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
