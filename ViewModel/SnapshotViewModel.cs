using System;
using System.Collections.Generic;
using System.Windows.Input;
using NetEti.MVVMini;
using System.Windows;
using LogicalTaskTree;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für einen Snapshot eines externen Trees.
    /// </summary>
    /// <remarks>
    /// File: SnapshotViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 12.11.2013 Erik Nagel: erstellt
    /// </remarks>
    public class SnapshotViewModel : LogicalNodeViewModel
    {
        #region public members

        #region published members

        /// <summary>
        /// Zeitpunkt der letzten Aktualisierung des Snapshots.
        /// </summary>
        public string SnapshotTime
        {
            get
            {
                return (this._myLogicalNode as Snapshot).Timestamp.ToString();
                // return (this._myLogicalNode as Snapshot).Timestamp.ToString()
                //     + " (" + DateTime.Now.ToString("HH.mm.ss") + ")";
            }
            set
            {
                this.RaisePropertyChanged("SnapshotTime");
            }
        }

        /// <summary>
        /// True, wenn der Snapshot nicht geladen werden konnte
        /// und durch einen Default-Snapshot ersetzt wurde.
        /// </summary>
        public bool IsDefaultSnapshot
        {
            get
            {
                return (this._myLogicalNode as Snapshot).IsDefaultSnapshot;
            }
            set
            {
                this.RaisePropertyChanged("IsDefaultSnapshot");
            }
        }

        /// <summary>
        /// Bei True befindet sich diese NodeList innerhalb eies Snapshots.
        /// </summary>
        public bool IsInSnapshot
        {
            get
            {
                return this._myLogicalNode.IsInSnapshot;
            }
            set
            {
                this.RaisePropertyChanged("IsInSnapshot");
            }
        }

        /// <summary>
        /// Herkunft des Snapshots.
        /// </summary>
        public string SnapshotPath
        {
            get
            {
                return (this._myLogicalNode as Snapshot).SnapshotPath;
            }
            set
            {
                this.RaisePropertyChanged("SnapshotPath");
            }
        }

        /// <summary>
        /// Command für den Refresh des Snapshots.
        /// </summary>
        public ICommand RefreshSnapshot { get { return this._btnRefreshSnapshotRelayCommand; } }

        /// <summary>
        /// Command für das Umschalten der Tree-Darstellung.
        /// </summary>
        public ICommand SwitchTaskTreeView { get { return this._btnSwitchTaskTreeViewRelayCommand; } }

        #endregion published members

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="parent">Der übergeordnete ViewModel-Knoten.</param>
        /// <param name="snapshot">Der zugeordnete Knoten aus dem LogicalTaskTree.</param>
        /// <param name="lazyLoadChildren">Bei True werden die Kinder erst beim Öffnen des TreeView-Knotens nachgeladen.</param>
        /// <param name="uIMain">Das Root-FrameworkElement zu diesem ViewModel.</param>
        /// <param name="logicalTaskTreeViewModel">Das dem Root-Knoten übergeordnete ViewModel (nur beim Root-Job ungleich null).</param>
        public SnapshotViewModel(OrientedTreeViewModelBase logicalTaskTreeViewModel, LogicalNodeViewModel parent, LogicalTaskTree.Snapshot snapshot, bool lazyLoadChildren, FrameworkElement uIMain)
          : base(logicalTaskTreeViewModel, parent, snapshot, lazyLoadChildren, uIMain)
        {
            this._btnRefreshSnapshotRelayCommand = new RelayCommand(refreshSnapshotExecute, canRefreshSnapshotExecute);
            this._btnSwitchTaskTreeViewRelayCommand = new RelayCommand(switchTaskTreeViewExecute, canSwitchTaskTreeViewExecute);
            (this._myLogicalNode as Snapshot).SnapshotRefreshed -= SnapshotViewModel_SnapshotRefreshed;
            (this._myLogicalNode as Snapshot).SnapshotRefreshed += SnapshotViewModel_SnapshotRefreshed;
            this._treeRefreshLocker = new object();
            this._isRefreshing = false;
            this.RaisePropertyChanged("IsSnapshotDummy");
        }

        #endregion public members

        #region private members

        private RelayCommand _btnRefreshSnapshotRelayCommand;
        private RelayCommand _btnSwitchTaskTreeViewRelayCommand;
        private bool _isRefreshing;
        private object _treeRefreshLocker;

        private void refreshSnapshotExecute(object parameter)
        {
            if (!this._isRefreshing)
            {
                this._isRefreshing = true;
                ((this._myLogicalNode) as Snapshot).RefreshSnapshot();
            }
        }

        private void fullTreeRefresh()
        {
            using (DispatcherProcessingDisabled d = this.Dispatcher.DisableProcessing())
            {
                lock (this._treeRefreshLocker)
                {
                    ObservableCollection<LogicalNodeViewModel> shadowTree = new ObservableCollection<LogicalNodeViewModel>();
                    if (this.Children?.Count > 0)
                    {
                        shadowTree.Add(this.Children[0]); // Retten für die Restaurierung aktueller Tree-Parameter wie zum Beispiel
                    }
                    // IsExpanded nach Neuerstellung des Trees aus dem Snapshot. 
                    this.Children.Clear();
                    //          this.SetChildOrientation(this.RootLogicalTaskTreeViewModel.TreeOrientationState);
                    this.loadChildren();
                    this.transferShadowTreeProperties(shadowTree, this.Children, new Stack<int>());
                    if (shadowTree?.Count > 0)
                    {
                        shadowTree[0].Dispose();
                    }
                    shadowTree.Clear();
                    //Thread.Sleep(100); DEBUG
                    this._isRefreshing = false;
                    //Thread.Sleep(100); DEBUG
                }
            }
        }

        private bool leanTreeRefresh()
        {
            // return false; // TEST 20.08.2020 Nagel
            bool rtn = true;
            lock (this._treeRefreshLocker)
            {
                this._isRefreshing = true;
                using (DispatcherProcessingDisabled d = this.Dispatcher.DisableProcessing())
                {
                    if (!this.refreshTreeView(this, this._myLogicalNode))
                    {
                        rtn = false;
                    }
                }
                this._isRefreshing = false;
                // Thread.Sleep(100); DEBUG
            }
            return rtn;
        }

        private void transferShadowTreeProperties(ObservableCollection<LogicalNodeViewModel> sourceTree,
              ObservableCollection<LogicalNodeViewModel> destinationTree, Stack<int> indices)
        {
            for (int i = 0; i < destinationTree.Count; i++)
            {
                indices.Push(i);
                LogicalNodeViewModel node = destinationTree[i];
                LogicalNodeViewModel sibling = this.searchSibling(sourceTree, indices);
                if (sibling != null)
                {
                    this.transferSiblingProperties(sibling, node);
                }
                if ((node is NodeListViewModel) || (node is JobListViewModel) || (node is SnapshotViewModel))
                {
                    this.transferShadowTreeProperties(sourceTree, node.Children, indices);
                }
                indices.Pop();
            }
        }

        private LogicalNodeViewModel searchSibling(ObservableCollection<LogicalNodeViewModel> sourceTree, Stack<int> indices)
        {
            int[] indexArray = new int[indices.Count];
            indices.CopyTo(indexArray, 0);
            LogicalNodeViewModel node = null;
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

        private void transferSiblingProperties(LogicalNodeViewModel sibling, LogicalNodeViewModel node)
        {
            node.IsExpanded = sibling.IsExpanded;
            node.ChildOrientation = sibling.ChildOrientation;
        }

        private bool canRefreshSnapshotExecute()
        {
            return true;
        }

        private void switchTaskTreeViewExecute(object parameter)
        {
            switch (this.RootLogicalTaskTreeViewModel.TreeOrientationState)
            {
                case TreeOrientation.AlternatingHorizontal:
                    this.RootLogicalTaskTreeViewModel.TreeOrientationState = TreeOrientation.Horizontal;
                    break;
                case TreeOrientation.Vertical:
                    this.RootLogicalTaskTreeViewModel.TreeOrientationState = TreeOrientation.AlternatingVertical;
                    break;
                case TreeOrientation.Horizontal:
                    this.RootLogicalTaskTreeViewModel.TreeOrientationState = TreeOrientation.Vertical;
                    break;
                case TreeOrientation.AlternatingVertical:
                    this.RootLogicalTaskTreeViewModel.TreeOrientationState = TreeOrientation.AlternatingHorizontal;
                    break;
                default:
                    this.RootLogicalTaskTreeViewModel.TreeOrientationState = TreeOrientation.AlternatingHorizontal;
                    break;
            }
        }

        private bool canSwitchTaskTreeViewExecute()
        {
            return true;
        }

        private void SnapshotViewModel_SnapshotRefreshed(object sender)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((this._myLogicalNode as Snapshot).WasDefaultSnapshot)
                {
                    this.fullTreeRefresh();
                }
                else
                {
                    if (!this.leanTreeRefresh())
                    {
                        // (this._myLogicalNode as Snapshot).SaveLastSnapshotForDebugging();
                        this.fullTreeRefresh();
                        return;
                    }
                }
                this.RaisePropertyChanged("IsDefaultSnapshot");
                this.RaisePropertyChanged("IsInSnapshot");
            }));
        }

        #endregion private members

    }
}
