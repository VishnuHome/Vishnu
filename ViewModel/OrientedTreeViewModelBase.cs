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
        /// <summary>
        /// Liefert das RootJobListViewModel des LogicalTaskTreeViewModels inklusive Setter.
        /// </summary>
        public JobListViewModel TreeVM
        {
            get
            {
                return this.MainLogicalNodeView[0];
            }
            set
            {
                if (this.MainLogicalNodeView.Count > 0)
                {
                    this.MainLogicalNodeView[0] = value;
                }
                else
                {
                    this.MainLogicalNodeView.Add(value);
                }
            }
        }

        /// <summary>
        /// Zusätzliche Parameter, die für den gesamten Tree Gültigkeit haben oder null.
        /// </summary>
        public TreeParameters TreeParams { get; private set; }

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
        /// Konstruktor - übernimmt die Parameter für den gesamten Tree.
        /// </summary>
        /// <param name="treeParams">Parameter für den gesamten Tree.</param>
        public OrientedTreeViewModelBase(TreeParameters treeParams)
        {
            this.TreeParams = treeParams;
            this.MainLogicalNodeView = new List<JobListViewModel>();
        }

        /// <summary>
        /// ItemsSource für die TreeView in LogicalTaskTreeControl.
        /// </summary>
        public List<JobListViewModel> MainLogicalNodeView { get; protected set; }

        /// <summary>
        /// Liste für die temporäre Speicherung von NodeListViewModel-Knoten
        /// als Helfer während des Aus- oder Einklappens des Branches.
        /// </summary>
        internal List<string> TempIds;

        private TreeOrientation _treeOrientationState;

    }
}
