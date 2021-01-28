namespace Vishnu.Interchange
{
    /// <summary>
    /// Definiert Methoden zum Zugriff auf die oberste JobList des Trees.
    /// Wird intern beim Mergen von veränderten Jobs nach Reload genutzt.
    /// </summary>
    public interface IBusinessLogicRoot
    {
        /// <summary>
        /// Liefert die oberste JobList des Trees als IVishnuNode.
        /// </summary>
        /// <returns>Die oberste JobList des Trees.</returns>
        IVishnuNode GetTopJobList();


        /// <summary>
        /// Setzt die oberste JobList des Trees.
        /// Returnt die bisher oberste JobList.
        /// </summary>
        /// <param name="topJobList">Die neue oberste JobList des Trees.</param>
        /// <returns>Die bisher oberste JobList des Trees.</returns>
        IVishnuNode SetTopJobList(IVishnuNode topJobList);
    }
}
