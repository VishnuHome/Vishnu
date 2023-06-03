using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// Ersetzt das LogicalTaskTreeViewModel als Dummy-Parameter beim Reload-Vorgang.
    /// </summary>
    public class DummyLogicalTaskTreeViewModel : OrientedTreeViewModelBase
    {
        /// <summary>
        /// Konstruktor - übernimmt die Parameter für den gesamten Tree.
        /// </summary>
        /// <param name="treeParams">Parameter für den gesamten Tree.</param>
        public DummyLogicalTaskTreeViewModel(TreeParameters treeParams) : base(treeParams)  {  }
    }
}
