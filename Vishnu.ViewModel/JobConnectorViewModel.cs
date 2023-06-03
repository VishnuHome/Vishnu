using LogicalTaskTree;
using System.Windows;
using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für einen JobConnector<br />
    /// Ist von SingleNodeViewModel und darüber von VishnuViewModelBase
    /// abgeleitet.
    /// </summary>
    /// <remarks>
    /// File: JobConnectorViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 17.09.2015 Erik Nagel: erstellt
    /// </remarks>
    public class JobConnectorViewModel : SingleNodeViewModel
    {
        #region public members

        /// <summary>
        /// ReferencedNodePath
        /// </summary>
        public string? ReferencedNodePath
        {
            get
            {
                return ((JobConnector)this._myLogicalNode)?.ReferencedNodePath;
            }
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="logicalTaskTreeViewModel">ViewModel des übergeordneten LogicalTaskTree.</param>
        /// <param name="parent">Der übergeordnete ViewModel-Knoten.</param>
        /// <param name="singleNode">Der zugeordnete Knoten aus dem LogicalTaskTree.</param>
        /// <param name="uIMain">Das Root-FrameworkElement zu diesem ViewModel.</param>
        public JobConnectorViewModel(OrientedTreeViewModelBase logicalTaskTreeViewModel, LogicalNodeViewModel parent, LogicalNode singleNode, FrameworkElement uIMain)
          : base(logicalTaskTreeViewModel, parent, singleNode, uIMain)
        {
        }

        #endregion public members

        #region protected members

        /// <summary>
        /// Wird angesprungen, wenn sich der logische Zustand des zugeordneten
        /// Knotens aus der Business-Logic geändert hat.
        /// </summary>
        /// <param name="sender">Der zugeordnete Knoten aus der Business-Logic.</param>
        /// <param name="state">Logischer Zustand des Knotens aus der Business-Logic.</param>
        protected override void TreeElementLogicalStateChanged(object sender, NodeLogicalState state)
        {
            base.TreeElementLogicalStateChanged(sender, state);
            this.RaisePropertyChanged("ReferencedNodePath"); // kann sich evtl. geändert haben.
        }

        #endregion protected members

    }
}
