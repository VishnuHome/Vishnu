using NetEti.MVVMini;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// Minimale gemeinsame Basisklasse für LogicalTaskTreeViewModel
    /// und DummyLogicalTaskTreeViewModel.
    /// </summary>
    public class OrientedTreeViewModelBase : ObservableObject
    {
        internal List<string> TempIds;

        /// <summary>
        /// Bestimmt die Ausrichtung bei der Darstellung der Elemente im Tree. 
        /// </summary>
        public TreeOrientation TreeOrientationState
        {
            get
            {
                return this._treeOrientationState;
            }
            set
            {
                if (this._treeOrientationState != value)
                {
                    this._treeOrientationState = value;
                    foreach (LogicalNodeViewModel child in this.MainLogicalNodeView)
                    {
                        child.SetChildOrientation(this.TreeOrientationState);
                    }
                    this.RaisePropertyChanged("TreeOrientationState");
                }
            }
        }

        /// <summary>
        /// Zusätzliche Parameter, die für den gesamten Tree Gültigkeit haben oder null.
        /// </summary>
        public TreeParameters TreeParams { get; private set; }

        /// <summary>
        /// Konstruktor - übernimmt die Parameter für den gesamten Tree.
        /// </summary>
        /// <param name="treeParams">Parameter für den gesamten Tree.</param>
        public OrientedTreeViewModelBase(TreeParameters treeParams)
        {
            this.TreeParams = treeParams;
        }

        /// <summary>
        /// ItemsSource für die TreeView in LogicalTaskTreeControl.
        /// </summary>
        public ReadOnlyCollection<JobListViewModel> MainLogicalNodeView { get; protected set; }

        private TreeOrientation _treeOrientationState;

    }
}
