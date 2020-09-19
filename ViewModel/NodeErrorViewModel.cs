using Vishnu.Interchange;

namespace Vishnu.ViewModel
{
    /// <summary>
    /// ViewModel für einen Ladefehler-Knoten.
    /// </summary>
    /// <remarks>
    /// File: NodeErrorViewModel
    /// Autor: Erik Nagel
    ///
    /// 12.04.2015 Erik Nagel: erstellt
    /// </remarks>
    public class NodeErrorViewModel : DynamicUserControlViewModelBase
    {
        /// <summary>
        /// Pfad zur Dll, bei der der Ladefehler auftrat.
        ///  </summary>
        public string DllPath
        {
            get
            {
                return this._dllPath;
            }
            set
            {
                if (this._dllPath != value)
                {
                    this._dllPath = value;
                }
            }
        }

        /// <summary>
        /// Konstruktor - übernímmt das übergeordnete ViewModel
        /// und den Pfad zur Dll, die nicht geladen werden konnte.
        /// </summary>
        /// <param name="parentViewModel">Das übergeordnete ViewModel.</param>
        /// <param name="dllPath">Pfad zur Dll, die nicht geladen werden konnte.</param>
        public NodeErrorViewModel(IVishnuViewModel parentViewModel, string dllPath)
        {
            this.ParentViewModel = parentViewModel;
            this.DllPath = dllPath;
            this.RaisePropertyChanged("DllPath");
        }

        private string _dllPath;
    }
}
