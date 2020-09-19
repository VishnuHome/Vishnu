using System.Collections.Generic;

namespace LogicalTaskTree
{
    /// <summary>
    /// Container für einen Job, einen logischen Namen für diesen Job
    /// und einen optionalen Dateipfad zum Job-File.
    /// </summary>
    /// <remarks>
    /// File: JobPackage.cs
    /// Autor: Erik Nagel
    ///
    /// 05.08.2013 Erik Nagel: erstellt
    /// </remarks>
    public class JobPackage
    {
        #region public members

        /// <summary>
        /// Der Dateipfad zum gespeicherten Job.
        /// </summary>
        public string JobFilePath
        {
            get
            {
                return this._jobFilePath;
            }
            set
            {
                if (this._jobFilePath != value)
                {
                    this._jobFilePath = value;
                }
            }
        }

        /// <summary>
        /// Der logische Name des Jobs.
        /// </summary>
        public string JobName
        {
            get
            {
                return this._jobName;
            }
            set
            {
                this._jobName = value;
            }
        }

        /// <summary>
        /// Der jobPackage.Job.
        /// </summary>
        public Job Job
        {
            get
            {
                return this._job;
            }
            set
            {
                this._job = value;
            }
        }

        /// <summary>
        /// JobPackages, die innerhalb des Jobs referenziert werden (inline oder extern).
        /// </summary>
        public Dictionary<string, JobPackage> SubJobPackages;

        /// <summary>
        /// Konstruktor - initialisiert einen leeren Job.
        /// </summary>
        /// <param name="jobFilePath">Dateipfad zur XML-Datei mit der Job-Beschreibung.</param>
        public JobPackage(string jobFilePath) : this(jobFilePath, null) { }

        /// <summary>
        /// Konstruktor - initialisiert einen leeren Job.
        /// </summary>
        /// <param name="jobFilePath">Dateipfad zur XML-Datei mit der Job-Beschreibung.</param>
        /// <param name="jobName">Name der XML-Datei mit der Job-Beschreibung.</param>
        public JobPackage(string jobFilePath, string jobName)
        {
            this.JobFilePath = jobFilePath;
            this.JobName = jobName == null ? "" : jobName;
            this.Job = new Job();
            this.SubJobPackages = new Dictionary<string, JobPackage>();
        }

        #endregion public members

        #region private members

        private string _jobFilePath;
        private Job _job;
        private string _jobName;

        #endregion private members

    }
}
