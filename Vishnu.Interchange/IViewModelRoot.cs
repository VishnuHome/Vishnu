namespace Vishnu.Interchange
{
    /// <summary>
    /// Definiert Methoden zum Zugriff auf das oberste JobListViewModel des Trees.
    /// Wird intern beim Mergen von veränderten Jobs nach Reload genutzt.
    /// </summary>
    public interface IViewModelRoot
    {
        /// <summary>
        /// Liefert das oberste JobListViewModel des Trees als IVishnuViewModel.
        /// </summary>
        /// <returns>Das oberste JobListViewModel des Trees als IVishnuViewModel.</returns>
        IVishnuViewModel GetTopJobListViewModel();

        /// <summary>
        /// Setzt das oberste JobListViewModel des Trees.
        /// Returnt das bisherige oberste JobListViewModel.
        /// </summary>
        /// <param name="topJobListViewModel">Das neue oberste JobListViewModel des Trees.</param>
        /// <returns>Das bisherige oberste JobListViewModel des Trees.</returns>
        IVishnuViewModel SetTopJobListViewModel(IVishnuViewModel topJobListViewModel);

        /// <summary>
        /// Aktualisiert die ViewModels von eventuellen zusätzliche Ansichten,
        /// die dieselbe BusinessLogic abbilden (hier: JobGroupViewModel).
        /// </summary>
        void RefreshDependentAlternativeViewModels();

    }
}
