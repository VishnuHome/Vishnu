using System;
using System.Collections.Generic;
using System.Linq;
using NetEti.FileTools;
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

        #region UndefinedJobProvider

        /// <summary>
        /// Klassendefinition für einen undefinierten JobProvider.
        /// Ersetzt null, um die elenden null-Warnungen bei der Verwendung von LogicalNodes und JobLists
        /// zu umgehen, bei denen sichergestellt ist, dass sie zum Zeitpunkt der Verwendung
        /// ungleich null sind, die aber im Konstruktor sonst noch nicht sinnvoll instanziiert
        /// werden könnten.
        /// Bei eventuellen späteren null-Abfragen muss null durch die statische Instanz
        /// 'UndefinedJobProvider' ersetzt werden.
        /// </summary>
        public class UndefinedJobProvider : JobProviderBase, IUndefinedElement
        {
            #region protected members

            /// <summary>
            ///   Fügt dem Dictionary LoadedJobPackages das JobPackage
            ///   mit dem logischen Pfad logicalJobName hinzu.
            ///   Im Fehlerfall wird einfach nichts hinzugefügt.
            /// </summary>
            /// <param name="logicalJobName">Der logische Name des Jobs oder null beim Root-Job.</param>
            /// <param name="physicalJobPath">Der physikalische Pfad zur JobDescription oder zum Job-Verzeichnis oder null beim Root-Job.</param>
            protected override string TryLoadJobPackage(string? logicalJobName = null, string? physicalJobPath = null)
            {
                return String.Empty;
                // throw new NotImplementedException();
            }

            #endregion protected members

            /// <summary>
            /// Statische Instanz für einen undefinierten JobProvider.
            /// Ersetzt null, um die elenden null-Warnungen bei der Verwendung von LogicalNodes und JobLists
            /// zu umgehen, bei denen sichergestellt ist, dass sie zum Zeitpunkt der Verwendung
            /// ungleich null sind, die aber im Konstruktor sonst noch nicht sinnvoll instanziiert
            /// werden könnten.
            /// Bei eventuellen späteren null-Abfragen muss null durch diese Instanz ersetzt werden.
            /// Es kann dann ggf. auf 'is IUndefinedElement' geprüft werden.
            /// </summary>
            public static readonly UndefinedJobProvider undefinedJobProvider = new();
        }

        #endregion UndefinedJobProvider

        #region IJobProvider implementation

        /// <summary>
        /// Liefert eine konkrete Job-Instanz für eine JobList
        /// in einem LogicalTaskTree.
        /// </summary>
        /// <param name="name">Namen/Suchbegriff/Pfad des Jobs oder null</param>
        /// <param name="physicalJobPath">Der physikalische Pfad zur JobDescription oder zum Job-Verzeichnis oder null.</param>
        /// <returns>Instanz des Jobs, der zu dem Namen gehört.</returns>
        public virtual Job GetJob(ref string name, string? physicalJobPath = null)
        {
            if (this is UndefinedJobProvider)
            {
                return new UndefinedJob();
            }
            if (String.IsNullOrEmpty(name) || !this.LoadedJobPackages.ContainsKey(name))
            {
                if (!String.IsNullOrEmpty(physicalJobPath))
                {
                    name = this.TryLoadJobPackage(physicalJobPath, name); // lädt ggf. auch alle SubJobs
                }
                else
                {
                    throw new ArgumentException("Der Pfad zum Job darf hier nicht leer sein.");
                }
            }
            if (!String.IsNullOrEmpty(name) && this.LoadedJobPackages.ContainsKey(name))
            {
                return this.LoadedJobPackages[name].Job;
            }
            else
            {
                throw new ArgumentException(String.Format(
                    "Der Job oder Checker '{0}' wurde nicht gefunden"
                    + " (bitte auch auf Groß- und Kleinschreibung achten).", name));
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
        }

        #endregion public members

        #region protected members

        /// <summary>
        /// Fügt dem Dictionary LoadedJobPackages das JobPackage
        /// zu dem logischen Job-Namen logicalJobName hinzu.
        /// Im Fehlerfall wird einfach nichts hinzugefügt.
        /// Muss überschrieben werden.
        /// </summary>
        /// <param name="physicalJobPath">Der physikalische Pfad zur JobDescription oder zum Job-Verzeichnis.</param>
        /// <param name="logicalJobName">Der logische Name des Jobs oder null beim Root-Job.</param>
        /// <returns>Neu gesetzter, angepasster oder übergebener logicalJobName.</returns>
        protected abstract string TryLoadJobPackage(string physicalJobPath, string? logicalJobName = null);

        /// <summary>
        /// Dictionary mit allen bisher geladenen JobPackages.
        /// </summary>
        protected Dictionary<string, JobPackage> LoadedJobPackages;

        /// <summary>
        /// Diverse Anwendungseinstellungen als Properties.
        /// </summary>
        protected AppSettings _appSettings;

        #endregion protected members

    }
}
