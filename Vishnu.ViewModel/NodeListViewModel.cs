using System.Windows;
using LogicalTaskTree;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für einen NodeList-Knoten in der TreeView in LogicalTaskTreeControl
    /// Auch wenn hier keine eigene Funktionalität codiert ist, wird diese
    /// Ableitung von LogicalNodeViewModel benötigt, um in der TreeView die
    /// unterschiedliche Darstellung der verschiedenen Knotentypen realisieren
    /// zu können.
    /// </summary>
    /// <remarks>
    /// File: NodeListViewModel.cs
    /// Autor: Erik Nagel
    ///
    /// 05.01.2013 Erik Nagel: erstellt
    /// </remarks>
    public class NodeListViewModel : LogicalNodeViewModel
    {
        #region public members

        #region published members

        #endregion published members

        #endregion public members

        /// <summary>
        /// Liefert das Ergebnis für die Property ToolTipInfo.
        /// Diese Routine zeigt per Default auf NextRunInfoAndResult,
        /// wird aber hier überschrieben.
        /// </summary>
        /// <returns>Die im ToolTip anzuzeigende Information.</returns>
        protected override string GetToolTipInfo()
        {
            if (string.IsNullOrEmpty(this.LastExceptions))
            {
                return base.GetToolTipInfo();
            }
            return this.LastExceptions;
        }

        /// <summary>
        /// Konstruktor - übernimmt das ViewModel des übergeordneten LogicalTaskTree,
        /// den übergeordneten ViewModel-Knoten, den zugeordneten Knoten aus dem LogicalTaskTree,
        /// einen Schalter für Lazy-Loading und das Root-FrameworkElement zu diesem ViewModel.
        /// </summary>
        /// <param name="logicalTaskTreeViewModel">ViewModel des übergeordneten LogicalTaskTree.</param>
        /// <param name="parent">Der übergeordnete ViewModel-Knoten.</param>
        /// <param name="nodeList">Der zugeordnete Knoten aus dem LogicalTaskTree.</param>
        /// <param name="lazyLoadChildren">Bei True werden die Kinder erst beim Öffnen des TreeView-Knotens nachgeladen.</param>
        /// <param name="uIMain">Das Root-FrameworkElement zu diesem ViewModel.</param>
        public NodeListViewModel(OrientedTreeViewModelBase logicalTaskTreeViewModel, LogicalNodeViewModel? parent,
            NodeList nodeList, bool lazyLoadChildren, FrameworkElement uIMain)
          : base(logicalTaskTreeViewModel, parent, nodeList, lazyLoadChildren, uIMain) { }
    }
}
