using LogicalTaskTree;
using System;
using System.Collections.ObjectModel;
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
        /// Liefert einen string für Debug-Zwecke.
        /// </summary>
        /// <returns>Ein String für Debug-Zwecke.</returns>
        public override string GetDebugNodeInfos()
        {
            return this.GroupJobList.GetDebugNodeInfos();
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
            this.FlatNodeViewModelList = LogicalTaskTreeViewModel.FlattenTree(this.GroupJobList, new ObservableCollection<LogicalNodeViewModel>(), this._flatNodeListFilter, false);
            this.RaisePropertyChanged("FlatNodeViewModelList");
            this.RaisePropertyChanged("DebugMode");
            this.RaisePropertyChanged("DebugNodeInfos");
            LogicalNode.AllNodesStateChanged += this.LogicalNode_AllNodesStateChanged;
        }

        #region context menu

        /// <summary>
        /// Lädt den Tree nach Änderung der JobDescriptions neu.
        /// </summary>
        /// <param name="parameter">Optionaler Parameter, wird hier nicht genutzt.</param>
        public override void ReloadTaskTreeExecute(object parameter)
        {
            this.GroupJobList.ReloadTaskTree();
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
        public override void LogTaskTreeExecute(object parameter)
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
