using LogicalTaskTree;
using NetEti.ApplicationControl;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für die Darstellung eines logicalTaskTree
    /// als gruppierte und gefilterte Liste von Knoten.
    /// </summary>
    /// <remarks>
    /// File: JobGroupViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 01.09.2014 Erik Nagel: erstellt
    /// </remarks>
    public class JobGroupViewModel : VishnuViewModelBase, IDisposable
    {
        #region IDisposable Implementation

        private bool _disposed; // = false wird vom System vorbelegt;

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
            if (!this._disposed)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                    LogicalNode.AllNodesStateChanged -= this.LogicalNode_AllNodesStateChanged;
                    if (this.FlatNodeViewModelList != null)
                    {
                        for (int i = 0; i < this.FlatNodeViewModelList.Count; i++)
                        {
                            this.FlatNodeViewModelList[i].PropertyChanged -= JobGroupViewModel_PropertyChanged;
                        }

                    }
                }
                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                this._disposed = true;
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~JobGroupViewModel()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Implementation

        /// <summary>
        /// ViewModel für den LogicalTaskTree.
        /// </summary>
        public JobListViewModel GroupJobList { get; set; }

        /// <summary>
        /// ItemsSource für eine einfache Auflistung von Endknoten des Trees.
        /// </summary>
        public ObservableCollection<LogicalNodeViewModel> FlatNodeViewModelList
        {
            get
            {
                return this._flatNodeViewModelList;
            }
            private set
            {
                this._flatNodeViewModelList = value;
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

        /// <summary>
        /// Bei True können zusätzliche Testausgaben erfolgen.
        /// Default: False.
        /// </summary>
        public bool DebugMode
        {
            get
            {
                return this.GroupJobList.DebugMode;
            }
        }

        /// <summary>
        /// Name + Id + GUID
        /// </summary>
        public string DebugNodeInfos
        {
            get
            {
                return this.GetDebugNodeInfos();
            }
        }

        /// <summary>
        /// Liefert oder setzt die Zeilenanzahl für das enthaltende Grid.
        /// </summary>
        /// <returns>die Zeilenanzahl des enthaltenden Grids.</returns>
        public int GridRowCount
        {
            get
            {
                return this._gridRowCount;
            }
            set
            {
                if (this._gridRowCount != value)
                {
                    this._gridRowCount = value;
                }
                this.RaisePropertyChanged("GridRowCount");
            }
        }

        /// <summary>
        /// Liefert oder setzt die Zeilenanzahl für das enthaltende Grid.
        /// </summary>
        /// <returns>die Zeilenanzahl des enthaltenden Grids.</returns>
        public int GridColumnCount
        {
            get
            {
                return this._gridColumnCount;
            }
            set
            {
                if (this._gridColumnCount != value)
                {
                    this._gridColumnCount = value;
                }
                this.RaisePropertyChanged("GridColumnCount");
            }
        }

        /// <summary>
        /// Liefert oder setzt die Zeilenanzahl einer quadratischen Matrix.
        /// Dieser Wert wird zu einem geeigneten Zeitpunkt in die Property GridRowCount geschoben,
        /// um die WPF-GUI zu informieren.
        /// </summary>
        /// <returns>Die Zeilenanzahl einer quadratischen Matrix.</returns>
        public int RowCount;

        /// <summary>
        /// Liefert oder setzt die Spaltenanzahl einer quadratischen Matrix.
        /// Dieser Wert wird zu einem geeigneten Zeitpunkt in die Property GridColumnCount geschoben,
        /// um die WPF-GUI zu informieren.
        /// </summary>
        /// <returns>Die Spaltenanzahl einer quadratischen Matrix.</returns>
        public int ColumnCount;

        /// <summary>
        /// Liefert einen string für Debug-Zwecke.
        /// </summary>
        /// <returns>Ein String für Debug-Zwecke.</returns>
        public override string GetDebugNodeInfos()
        {
            return this.GroupJobList.GetDebugNodeInfos();
        }

        /// <summary>
        /// Wird von DynamicUserControlBase angesprungen, wenn das UserControl vollständig gerendered wurde.
        /// </summary>
        /// <param name="dynamicUserControl">Das aufrufende DynamicUserControlBase als Object.</param>
        public override void UserControlContentRendered(object dynamicUserControl)
        {
            // nein: base.UserControlContentRendered(dynamicUserControl);
            // Thread.Sleep(15000);
            // Dispatcher.BeginInvoke(new Action(() => { SetGridMeasures(); }), DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Konstruktor - übernimmt das anzuzeigende JobListViewModel und
        /// einen Filter für anzuzeigende NodeTypes.
        /// </summary>
        /// <param name="rootJobListViewModel">Anzuzeigendes JobListViewModel.</param>
        /// <param name="flatNodeListFilter">Filter für anzuzeigende NodeTypes.</param>
        public JobGroupViewModel(JobListViewModel rootJobListViewModel, NodeTypes flatNodeListFilter)
        {
            this.GroupJobList = rootJobListViewModel;
            this._flatNodeListFilter = flatNodeListFilter;
            this._flatNodeViewModelList = LogicalTaskTreeViewModel.FlattenTree(this.GroupJobList, new ObservableCollection<LogicalNodeViewModel>(),
                this._flatNodeListFilter, false);
            this._renderedControls = new ConcurrentBag<string>();
            this.PresetGridProperties(this.FlatNodeViewModelList);

            this.RaisePropertyChanged("FlatNodeViewModelList");
            this.RaisePropertyChanged("DebugMode");
            this.RaisePropertyChanged("DebugNodeInfos");
            LogicalNode.AllNodesStateChanged += this.LogicalNode_AllNodesStateChanged;
        }

        /// <summary>
        /// Berechnet die Anzahl Zeilen und Spalten für ein möglichst quadratisches Grid
        /// abhängig von der Gesamtanzahl darzustellender Controls bzw. deren ViewModels.
        /// Weist außerdem jedem ViewModel eines einzelnen Controls seine Zeilen- und Spaltennummer zu.
        /// Alle Zuweisungen werden hier zwar gespeichert, aber erst zu einem späteren Zeitpunkt in die
        /// Properties geschoben, welche über INotifyPropertyChanged die WPF-GUI informieren.
        /// </summary>
        /// <param name="FlatNodeViewModelList"></param>
        private void PresetGridProperties(ObservableCollection<LogicalNodeViewModel> FlatNodeViewModelList)
        {
            int rows = (int)(Math.Sqrt(this.FlatNodeViewModelList.Count));
            int columns = (int)((1.0 * this.FlatNodeViewModelList.Count / rows) + 0.999999);
            this.ColumnCount = columns;
            this.RowCount = rows;

            // 18.11.2022 Test+
            this.SetGridMeasures();
            // 18.11.2022 Test-

            for (int i = 0; i < this.FlatNodeViewModelList.Count; i++)
            {
                // Zeile und Spalte für das aktuelle Element berechnen (null-basiert):
                int rowNumber = i / columns;
                int columnNumber = i - rowNumber * columns;

                this.FlatNodeViewModelList[i].RowNumber = rowNumber;
                this.FlatNodeViewModelList[i].ColumnNumber = columnNumber;

                // 18.11.2022 Test+
                this.FlatNodeViewModelList[i].GridRow = rowNumber;
                this.FlatNodeViewModelList[i].GridColumn = columnNumber;
                // 18.11.2022 Test-

                this.FlatNodeViewModelList[i].PropertyChanged += JobGroupViewModel_PropertyChanged;

            }
        }

        private void SetGridMeasures()
        {
            this.GridColumnCount = this.ColumnCount;
            this.GridRowCount = this.RowCount;
        }

        private void JobGroupViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName?.Equals("IsRendered") == true)
            {
                if (sender is VishnuViewModelBase)
                {
                    VishnuViewModelBase typedSender = (VishnuViewModelBase)sender;
                    if (typedSender != null && typedSender.IsRendered)
                    {
                        if (!this._renderedControls.Contains(typedSender.VisualTreeCacheBreaker))
                        {
                            this._renderedControls.Add(typedSender.VisualTreeCacheBreaker);
                        }
                    }
                }
            }
        }

        #region context menu

        /// <summary>
        /// Lädt den Tree nach Änderung der JobDescriptions neu.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter, wird hier nicht genutzt.</param>
        public override void ReloadTaskTreeExecute(object? parameter)
        {
            _ = this.GroupJobList.ReloadTaskTree();
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
        /// Loggt den Tree.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter, wird hier nicht genutzt.</param>
        public override void LogTaskTreeExecute(object? parameter)
        {
            LogicalTaskTreeManager.LogTaskTree(this.GroupJobList, false);
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

        #endregion context menu

        private int _gridRowCount;
        private int _gridColumnCount;
        private ConcurrentBag<string> _renderedControls;

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

        private ObservableCollection<LogicalNodeViewModel> _flatNodeViewModelList;
        private NodeTypes _flatNodeListFilter;
        private volatile bool _isTreePaused;
    }
}
