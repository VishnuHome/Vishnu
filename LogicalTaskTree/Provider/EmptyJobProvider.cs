using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTaskTree.Provider
{
    /// <summary>
    /// Dummy-JobProvider für Snapshots.
    /// </summary>
    public class EmptyJobProvider : IJobProvider
    {
        #region IJobProvider implementation

        /// <summary>
        /// Liefert eine konkrete Job-Instanz für eine JobList
        /// in einem LogicalTaskTree.
        /// </summary>
        /// <param name="name">Namen/Suchbegriff/Pfad des Jobs oder null</param>
        /// <returns>Instanz des Jobs, der zu dem Namen gehört.</returns>
        public virtual Job GetJob(ref string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retourniert den logischen Namen des Jobs mit dem
        /// physischen Namen des JobPackages oder logischen Namen des Jobs.
        /// </summary>
        /// <param name="name">Logischer oder physischer Name des Jobs oder JobPackages.</param>
        /// <returns>Logischer Name des Jobs oder null.</returns>
        public virtual string? GetLogicalJobName(string name)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retourniert den physischen Namen des JobPackages mit dem
        /// physischen Namen des JobPackages oder logischen Namen des Jobs.
        /// </summary>
        /// <param name="name">Logischer oder physischer Name des Jobs oder JobPackages.</param>
        /// <returns>Physischer Name des JobPackages oder null.</returns>
        public virtual string? GetPhysicalJobPath(string name)
        {
            throw new NotImplementedException();
        }

        #endregion IJobProvider implementation

    }
}
