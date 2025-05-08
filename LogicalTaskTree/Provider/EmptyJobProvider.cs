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
        /// <param name="physicalJobPath">Der physikalische Pfad zur JobDescription oder zum Job-Verzeichnis oder null beim Root-Job.</param>
        /// <returns>Instanz des Jobs, der zu dem Namen gehört.</returns>
        public virtual Job GetJob(ref string name, string? physicalJobPath = null)
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

        /// <summary>
        /// Ersetzt Wildcards und Pfade in einem Parameter-String.
        /// </summary>
        /// <param name="para">String mit Parametern, in denen Wildcards ersetzt werden sollen.</param>
        /// <param name="searchDirectories">Array von Pfaden, die ggf. durchsucht/eingesetzt werden.</param>
        /// <returns>Der "para"-String mit ersetzten Wildcards.</returns>
        public string ReplaceWildcardsAndPathes(string para, string[] searchDirectories)
        {
            throw new NotImplementedException();
        }

        #endregion IJobProvider implementation

    }
}
