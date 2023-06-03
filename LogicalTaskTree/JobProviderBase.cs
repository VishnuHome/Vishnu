using System;
using System.Collections.Generic;
using System.Linq;
using NetEti.Globals;
using Vishnu.Interchange;

namespace LogicalTaskTree
{
    /// <summary>
    /// Basisklasse für alle JobProvider; muss abgeleitet werden.
    ///           implementiert IJobProvider.
    /// </summary>
    /// <remarks>
    /// File: JobProviderBase
    /// Autor: Erik Nagel
    ///
    /// 05.05.2013 Erik Nagel: erstellt
    /// </remarks>
    public abstract class JobProviderBase : IJobProvider
    {
        #region public members

        #region IJobProvider implementation

        /// <summary>
        /// Liefert eine konkrete Job-Instanz für eine JobList
        /// in einem LogicalTaskTree.
        /// </summary>
        /// <param name="name">Namen/Suchbegriff/Pfad des Jobs oder null</param>
        /// <returns>Instanz des Jobs, der zu dem Namen gehört.</returns>
        public virtual Job GetJob(ref string name)
        {
            if (String.IsNullOrEmpty(name) || !this.LoadedJobPackages.ContainsKey(name))
            {
                this.TryLoadJobPackage(ref name); // lädt ggf. auch alle SubJobs
            }
            if (!String.IsNullOrEmpty(name) && this.LoadedJobPackages.ContainsKey(name))
            {
                return this.LoadedJobPackages[name].Job;
            }
            else
            {
                throw new ArgumentException(String.Format("Der Job oder Checker '{0}' wurde nicht gefunden (bitte auch auf Groß- und Kleinschreibung achten).", name));
            }
        }

        /// <summary>
        /// Retourniert den logischen Namen des Jobs mit dem
        /// physischen Namen des JobPackages oder logischen Namen des Jobs.
        /// </summary>
        /// <param name="name">Logischer oder physischer Name des Jobs oder JobPackages.</param>
        /// <returns>Logischer Name des Jobs oder null.</returns>
        public virtual string? GetLogicalJobName(string name)
        {
            KeyValuePair<string, JobPackage> kv = this.LoadedJobPackages.Where(p => { return p.Value.JobName == name || p.Value.JobFilePath == name; }).First();
            if (!kv.Equals(default(KeyValuePair<string, JobPackage>)))
            {
                return kv.Value.JobName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retourniert den physischen Namen des JobPackages mit dem
        /// physischen Namen des JobPackages oder logischen Namen des Jobs.
        /// </summary>
        /// <param name="name">Logischer oder physischer Name des Jobs oder JobPackages.</param>
        /// <returns>Physischer Name des JobPackages oder null.</returns>
        public virtual string? GetPhysicalJobPath(string name)
        {
            KeyValuePair<string, JobPackage> kv = this.LoadedJobPackages.Where(p => { return p.Value.JobName == name || p.Value.JobFilePath == name; }).First();
            if (!kv.Equals(default(KeyValuePair<string, JobPackage>)))
            {
                return kv.Value.JobFilePath;
            }
            else
            {
                return null;
            }
        }

        #endregion IJobProvider implementation

        /// <summary>
        /// Standard Konstruktor.
        /// </summary>
        public JobProviderBase()
        {
            this.LoadedJobPackages = new Dictionary<string, JobPackage>();
            this._appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            this.ReachableServers = new Dictionary<string, bool>();
        }

        #endregion public members

        #region protected members

        /// <summary>
        /// Fügt dem Dictionary LoadedJobPackages das JobPackage
        /// zu dem logischen Job-Namen logicalJobName hinzu.
        /// Im Fehlerfall wird einfach nichts hinzugefügt.
        /// Muss überschrieben werden.
        /// </summary>
        /// <param name="logicalJobName">Der logische Name des Jobs oder null beim Root-Job.</param>
        protected abstract void TryLoadJobPackage(ref string logicalJobName);

        /// <summary>
        /// Dictionary mit allen bisher geladenen JobPackages.
        /// </summary>
        protected Dictionary<string, JobPackage> LoadedJobPackages;

        /// <summary>
        /// Stringliste mit allen bisher erreichbaren oder unerreichbaren Servern (aus diversen Dateipfaden).
        /// Dient zur Optimierung der Ladezeit von Vishnu; Wenn ein Pfad mit einem Server beginnt
        /// (\\Servername), dann wird in diesem Dictionary vermerkt, ob der Server erreichbar ist, oder nicht.
        /// </summary>
        protected Dictionary<string, bool> ReachableServers;

        /// <summary>
        /// Diverse Anwendungseinstellungen als Properties.
        /// </summary>
        protected AppSettings _appSettings;

        #endregion protected members

    }
}
