namespace LogicalTaskTree
{
    /// <summary>
    /// Interface für Lieferanten von Jobs für JobList-Knoten
    ///           im LogicalTaskTree.
    /// </summary>
    /// <remarks>
    /// File: IJobProvider.cs
    /// Autor: Erik Nagel
    ///
    /// 26.01.2012 Erik Nagel: erstellt
    /// </remarks>
    public interface IJobProvider
    {
        /// <summary>
        /// Liefert eine konkrete Job-Instanz für eine JobList
        /// in einem LogicalTaskTree.
        /// </summary>
        /// <param name="name">Der Name des Jobs</param>
        /// <returns>Instanz des Jobs, der zu dem Namen gehört.</returns>
        Job GetJob(ref string name);

        /// <summary>
        /// Retourniert den logischen Namen des Jobs mit dem
        /// physischen Namen des JobPackages oder logischen Namen des Jobs.
        /// </summary>
        /// <param name="name">Logischer oder physischer Name des Jobs oder JobPackages.</param>
        /// <returns>Logischer Name des Jobs oder null.</returns>
        string? GetLogicalJobName(string name);

        /// <summary>
        /// Retourniert den physischen Namen des JobPackages mit dem
        /// physischen Namen des JobPackages oder logischen Namen des Jobs.
        /// </summary>
        /// <param name="name">Logischer oder physischer Name des Jobs oder JobPackages.</param>
        /// <returns>Physischer Name des JobPackages oder null.</returns>
        string? GetPhysicalJobPath(string name);

    }
}
